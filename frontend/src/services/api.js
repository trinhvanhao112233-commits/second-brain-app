import { accountStorage, migrateLegacyStorage } from './storage';

const API_BASE = (import.meta.env.VITE_API_URL || 'https://second-brain-app-21j2.onrender.com/api').replace(/\/$/, '');
export const getStoredUser = () => {
  try {
    const user = JSON.parse(localStorage.getItem('second_brain_user') || 'null');
    return user?.token ? user : null;
  } catch { return null; }
};
export const setStoredUser = user => {
  if (user) {
    migrateLegacyStorage(user);
    localStorage.setItem('second_brain_user', JSON.stringify(user));
  } else localStorage.removeItem('second_brain_user');
};
const notice = message => window.dispatchEvent(new CustomEvent('app-notice', { detail: { message, type: 'error' } }));
const status = state => window.dispatchEvent(new CustomEvent('api-sync-state', { detail: state }));
const reportError = message => window.dispatchEvent(new CustomEvent('api-sync-error', { detail: message }));
export const pendingCount = () => JSON.parse(accountStorage.getItem('outbox') || '[]').length;
let flushing = null;
const responses = new Map();
const liveRequests = new Set();
async function request(endpoint, options = {}) {
  const user = getStoredUser();
  const owner = user?.id;
  const revision = accountStorage.getItem('data_revision');
  let response;
  try {
    response = await fetch(`${API_BASE}${endpoint}`, {
    ...options,
    headers: { 'Content-Type': 'application/json', ...options.headers, ...(user ? { Authorization: `Bearer ${user.token}` } : {}) },
    signal: AbortSignal.timeout(30000),
    });
  } catch (error) {
    if (!/^\/(auth|notification|aiassistant)\//.test(endpoint)) {
      reportError(`Không kết nối được API ${endpoint}. Máy chủ có thể đang khởi động hoặc kết nối bị chặn.`);
      status('error');
    }
    throw error;
  }
  if (getStoredUser()?.id !== owner && !endpoint.startsWith('/auth/')) throw new Error('Tài khoản đã thay đổi.');
  if ((!options.method || options.method === 'GET') && revision !== accountStorage.getItem('data_revision')) throw new Error('Dữ liệu đã thay đổi trong khi tải. Vui lòng làm mới.');
  if (response.status === 401 && !endpoint.startsWith('/auth/')) {
    window.dispatchEvent(new Event('session-expired'));
    throw new Error('Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.');
  }
  if (!response.ok && !/^\/(auth|notification|aiassistant)\//.test(endpoint)) {
    reportError(`API ${endpoint} trả lỗi ${response.status}. ${response.status >= 500 ? 'Cần kiểm tra log backend Render.' : 'Mở Cài đặt để kiểm tra thao tác chờ gửi.'}`);
  }
  return response;
}
export async function flushPending() {
  if (flushing) return flushing;
  const owner = getStoredUser()?.id;
  flushing = (async () => {
    status('syncing');
    try {
      while (getStoredUser()?.id === owner) {
        const queue = JSON.parse(accountStorage.getItem('outbox') || '[]');
        if (!queue.length) { status('synced'); return; }
        const op = queue[0];
        const res = await request(op.endpoint, op.options);
        if (!res.ok && !(op.options.method === 'DELETE' && res.status === 404)) {
          const error = await res.json().catch(() => ({}));
          throw new Error(error.message || `Không lưu được dữ liệu (${res.status}).`);
        }
        if (getStoredUser()?.id !== owner) return;
        if (liveRequests.has(op.id)) responses.set(op.id, res);
        const latest = JSON.parse(accountStorage.getItem('outbox') || '[]');
        accountStorage.setItem('outbox', JSON.stringify(latest.filter(x => x.id !== op.id)));
      }
    } catch (error) { status('error'); throw error; }
  })().finally(() => { flushing = null; });
  return flushing;
}
export async function apiFetch(endpoint, options = {}) {
  if (!endpoint.startsWith('/') || endpoint.startsWith('//')) throw new Error('Địa chỉ API không hợp lệ.');
  const method = (options.method || 'GET').toUpperCase();
  const queued = method !== 'GET' && !/^\/(auth|notification|aiassistant)\//.test(endpoint);
  if (queued) {
    if (!getStoredUser()) throw new Error('Vui lòng đăng nhập.');
    const payload = options.body ? JSON.parse(options.body) : {};
    if (method === 'POST' && !payload.id) payload.id = crypto.randomUUID();
    const op = { id: crypto.randomUUID(), endpoint, options: { ...options, method, body: JSON.stringify(payload) } };
    const queue = JSON.parse(accountStorage.getItem('outbox') || '[]');
    accountStorage.setItem('data_revision', crypto.randomUUID());
    queue.push(op);
    accountStorage.setItem('outbox', JSON.stringify(queue));
    // Only live callers need response bodies; persisted operations do not.
    liveRequests.add(op.id);
    accountStorage.setItem('outbox', JSON.stringify([...queue.slice(0, -1), op]));
    try {
      await flushPending();
      const response = responses.get(op.id);
      responses.delete(op.id);
      liveRequests.delete(op.id);
      return response || new Response(JSON.stringify(payload), { status: 200 });
    } catch (error) {
      liveRequests.delete(op.id);
      notice(`${error.message} Thay đổi đang chờ gửi lại; chưa được đồng bộ.`);
      throw error;
    }
  }
  if (method === 'GET' && !endpoint.startsWith('/notification/') && getStoredUser() && pendingCount()) await flushPending();
  const res = await request(endpoint, options);
  if (!res.ok && !/^\/(auth|notification|aiassistant)\//.test(endpoint)) status('error');
  return res;
}

export function discardFirstPending() {
  const queue = JSON.parse(accountStorage.getItem('outbox') || '[]');
  if (!queue.length) return;
  const history = JSON.parse(accountStorage.getItem('discarded_outbox') || '[]');
  history.push({ ...queue.shift(), discardedAt: new Date().toISOString() });
  accountStorage.setItem('discarded_outbox', JSON.stringify(history));
  accountStorage.setItem('outbox', JSON.stringify(queue));
  status(queue.length ? 'error' : 'idle');
}
