const online = window.lifeosOnline;
const apiBase = window.LIFEOS_API_URL;
let onlineSaveTimer = null;
let onlineBankKey = '';

function onlineStatus(text, warning = false) { const el = document.getElementById('online-sync-status'); el.textContent = text; el.classList.toggle('warning', warning); }
async function onlineRequest(path, options = {}) {
  let response;
  try { response = await fetch(apiBase + path, { ...options, headers: { 'Content-Type': 'application/json', ...(online.token ? { Authorization: 'Bearer ' + online.token } : {}), ...options.headers }, signal: AbortSignal.timeout(75000) }); }
  catch { throw new Error('Không kết nối được máy chủ. Thay đổi chưa được lưu; máy chủ Render có thể đang khởi động.'); }
  if (!response.headers.get('Content-Type')?.includes('application/json')) throw new Error('Máy chủ chưa sẵn sàng. Hãy thử lại sau.');
  const data = await response.json();
  if (!response.ok) { const error = new Error(data.error || 'Không thực hiện được yêu cầu.'); error.status = response.status; throw error; }
  return data;
}
function acceptOnlineSession(data) {
  online.generation++; clearTimeout(onlineSaveTimer); onlineSaveTimer = null;
  online.token = data.token || online.token; localStorage.setItem('LIFEOS_SESSION_V1', online.token);
  online.revision = data.revision; online.bank = data.bank; online.pending = null; online.conflict = false; online.snapshot = data.state;
  onlineBankKey = ''; document.getElementById('bank-key').value = '';
  currentUser = data.user; state = data.state; activeNoteId = state.notes[0]?.id || null;
  document.getElementById('online-conflict').hidden = true;
  document.getElementById('login-password').value = ''; document.getElementById('reg-password').value = ''; document.getElementById('reg-confirm').value = '';
  updateAuthUI(); applyTheme(); switchTab('dashboard'); renderOnlineBank(); onlineStatus('Đã kết nối · Tự cập nhật mỗi 15 giây');
}
async function handleLogin(event) {
  event.preventDefault(); const button = event.submitter; if (button) button.disabled = true; hideAuthError();
  const attempt = ++online.generation;
  try { const result = await onlineRequest('/auth/login', { method: 'POST', body: JSON.stringify({ username: document.getElementById('login-username').value.trim(), password: document.getElementById('login-password').value }) }); if (attempt === online.generation) acceptOnlineSession(result); }
  catch (error) { if (attempt === online.generation) showAuthError(error.message); } finally { if (button) button.disabled = false; }
}
async function handleRegister(event) {
  event.preventDefault(); hideAuthError();
  const password = document.getElementById('reg-password').value;
  if (password !== document.getElementById('reg-confirm').value) { showAuthError('Mật khẩu xác nhận không khớp.'); return; }
  const button = event.submitter; if (button) button.disabled = true;
  const attempt = ++online.generation;
  try { const result = await onlineRequest('/auth/register', { method: 'POST', body: JSON.stringify({ username: document.getElementById('reg-username').value.trim(), fullName: document.getElementById('reg-fullname').value.trim(), password }) }); if (attempt === online.generation) acceptOnlineSession(result); }
  catch (error) { if (attempt === online.generation) showAuthError(error.message); } finally { if (button) button.disabled = false; }
}
function handleLogout() {
  if ((online.pending || online.saving) && !confirm('Có thay đổi chưa lưu. Hãy tải bản sao lưu trước khi đăng xuất. Tiếp tục và bỏ các thay đổi chưa lưu?')) return;
  online.generation++; online.token = null; online.pending = null; online.conflict = false; onlineBankKey = ''; clearTimeout(onlineSaveTimer);
  localStorage.removeItem('LIFEOS_SESSION_V1'); document.getElementById('bank-key').value = '';
  currentUser = null; state = createSampleState(); activeNoteId = null; updateAuthUI(); applyTheme();
  location.reload();
}
window.queueOnlineSave = value => {
  if (!online.token || !currentUser) return;
  online.pending = structuredClone(value); clearTimeout(onlineSaveTimer); onlineStatus('Đang lưu thay đổi…'); onlineSaveTimer = setTimeout(flushOnlineSave, 300);
};
async function flushOnlineSave() {
  onlineSaveTimer = null;
  if (online.saving || !online.pending || online.conflict || !online.token) return;
  const generation = online.generation, snapshot = online.pending; online.pending = null; online.saving = true;
  try { const result = await onlineRequest('/state', { method: 'PUT', body: JSON.stringify({ state: snapshot, revision: online.revision }) }); if (generation !== online.generation) return; online.revision = result.revision; onlineStatus('Đã lưu trên máy chủ'); }
  catch (error) {
    if (generation !== online.generation) return;
    if (!online.pending) online.pending = snapshot; onlineStatus(error.message, true);
    if ([400,401,403,409,413].includes(error.status)) { online.conflict = true; document.getElementById('online-conflict').hidden = false; }
    else onlineSaveTimer = setTimeout(flushOnlineSave, 15000);
  } finally { online.saving = false; if (online.pending && !online.conflict && !onlineSaveTimer && online.token) onlineSaveTimer = setTimeout(flushOnlineSave, 300); }
}
async function pollOnlineState(manual = false) {
  if (!online.token || !currentUser || online.polling || online.saving || online.pending || online.conflict) { if (manual) showToast('Cần đăng nhập và lưu xong thay đổi trước khi đồng bộ.', 'warning'); return; }
  online.polling = true; const generation = online.generation, revision = online.revision;
  try {
    const result = await onlineRequest('/state');
    if (generation !== online.generation || online.saving || online.pending || online.conflict || revision !== online.revision) return;
    const known = new Set(state.bankSeenIds || []);
    const fresh = result.state.transactions.filter(tx => tx.source === 'sepay' && !known.has(tx.bankTransactionId));
    if (JSON.stringify(state) !== JSON.stringify(result.state)) { const theme = state.theme, color = state.accentColor; state = result.state; if (!state.notes.some(note => note.id === activeNoteId)) activeNoteId = state.notes[0]?.id || null; if (state.theme !== theme || state.accentColor !== color) applyTheme(); renderAll(); }
    online.revision = result.revision; online.bank = result.bank; renderOnlineBank(); onlineStatus('Đã kết nối · Tự cập nhật mỗi 15 giây');
    if (fresh.length) showToast(`Đã nhận ${fresh.length} giao dịch ngân hàng. Thu Chi đã cập nhật.`, 'success');
    else if (manual) showToast('Đã kiểm tra, không có giao dịch mới.');
  } catch (error) { onlineStatus(error.message, true); if (error.status === 401) { online.conflict = true; document.getElementById('online-conflict').hidden = false; } if (manual) showToast(error.message, 'warning'); }
  finally { online.polling = false; }
}
async function handleUpdateAccount(event) {
  event.preventDefault(); if (!currentUser) return;
  await flushOnlineSave(); if (online.saving || online.pending || online.conflict) { showToast('Hãy lưu xong dữ liệu trước khi đổi thông tin tài khoản.', 'warning'); return; }
  const button = event.submitter; if (button) button.disabled = true;
  const generation = online.generation;
  try { const result = await onlineRequest('/account', { method: 'PUT', body: JSON.stringify({ fullName: document.getElementById('acc-fullname').value.trim(), password: document.getElementById('acc-new-password').value }) }); if (generation !== online.generation) return; online.generation++; online.token = result.token; localStorage.setItem('LIFEOS_SESSION_V1', result.token); currentUser = result.user; document.getElementById('acc-new-password').value = ''; updateAuthUI(); showToast('Đã cập nhật tài khoản.', 'success'); }
  catch (error) { showToast(error.message, 'danger'); } finally { if (button) button.disabled = false; }
}
function renderOnlineBank() {
  const bank = online.bank;
  document.getElementById('bank-status').textContent = bank.connectionId ? `Đã cấu hình · ${bank.total} giao dịch · Lần nhận gần nhất: ${bank.lastReceived ? new Date(bank.lastReceived).toLocaleString('vi-VN') : 'Chưa có giao dịch'}` : 'Chưa cấu hình Webhook.';
  document.getElementById('bank-connection-details').hidden = !bank.connectionId;
  document.getElementById('bank-create-btn').disabled = !!bank.connectionId;
  const input = document.getElementById('bank-account'); input.readOnly = !!bank.connectionId; input.value = bank.accountNumber || '';
  document.getElementById('bank-url').value = bank.connectionId ? apiBase + '/bank/sepay/' + bank.connectionId : '';
}
async function createBankConnection(event) {
  event.preventDefault(); const button = event.submitter; if (button) button.disabled = true;
  const generation = online.generation;
  try { const result = await onlineRequest('/bank/connect', { method: 'POST', body: JSON.stringify({ accountNumber: document.getElementById('bank-account').value.trim() }) }); if (generation !== online.generation) return; online.bank = result.bank; onlineBankKey = result.apiKey; document.getElementById('bank-key').value = onlineBankKey; renderOnlineBank(); showToast('Đã tạo kết nối. Sao chép URL và API Key vào SePay.', 'success'); }
  catch (error) { showToast(error.message, 'danger'); if (button) button.disabled = false; }
}
async function rotateBankKey() {
  if (!online.bank.connectionId || !confirm('Khóa cũ sẽ ngừng hoạt động. Bạn cần cập nhật khóa mới trên SePay ngay sau bước này. Tiếp tục?')) return;
  const generation = online.generation;
  try { const result = await onlineRequest('/bank/' + online.bank.connectionId + '/rotate-key', { method: 'POST', body: '{}' }); if (generation !== online.generation) return; onlineBankKey = result.apiKey; document.getElementById('bank-key').value = onlineBankKey; showToast('Đã tạo khóa mới. Hãy cập nhật API Key trên SePay.', 'success'); } catch (error) { if (generation === online.generation) showToast(error.message, 'danger'); }
}
function toggleBankKey() { const input = document.getElementById('bank-key'); if (!onlineBankKey) { showToast('Không lấy lại được khóa cũ. Hãy tạo khóa mới nếu đã quên.', 'info'); return; } input.type = input.type === 'password' ? 'text' : 'password'; }
async function copyBankValue(id) { const input = document.getElementById(id); if (!input.value) { showToast('Chưa có giá trị để sao chép.'); return; } try { await navigator.clipboard.writeText(input.value); showToast('Đã sao chép.'); } catch { input.focus(); input.select(); showToast('Nhấn Ctrl+C để sao chép ô đang chọn.'); } }
function reloadOnline() { if (!online.pending || confirm('Bạn đã tải bản sao lưu chưa? Tải lại sẽ bỏ thay đổi chưa lưu.')) location.reload(); }
window.addEventListener('beforeunload', event => { if (online.pending || online.saving) { event.preventDefault(); event.returnValue = ''; } });
window.addEventListener('online', () => { if (online.pending) flushOnlineSave(); else pollOnlineState(); });
document.addEventListener('visibilitychange', () => { if (!document.hidden) pollOnlineState(); });
document.addEventListener('DOMContentLoaded', async () => {
  const generation = online.generation;
  if (online.token) { try { const result = await onlineRequest('/session'); if (generation === online.generation) acceptOnlineSession(result); } catch (error) { if (generation === online.generation) { if (error.status === 401) { online.token = null; localStorage.removeItem('LIFEOS_SESSION_V1'); } showAuthError(error.message); } } }
  setInterval(() => { if (!document.hidden) pollOnlineState(); }, 15000);
});
