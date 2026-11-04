import { ref } from 'vue';
import { apiFetch, flushPending, pendingCount } from './api';
import { accountStorage } from './storage';

export const VAULT_NOTE_TITLE = '__SB_CLOUD_VAULT_V2__';
const KEY = 'second_brain_debts_v1';
const BASE = 'debts_server_snapshot_v3';
const read = (key, fallback) => { try { return JSON.parse(accountStorage.getItem(key)) ?? fallback; } catch { return fallback; } };
export const cloudSyncState = ref('idle');
export const syncError = ref('');
window.addEventListener('api-sync-error', e => { syncError.value = e.detail; });
export const lastSyncTime = ref('');
window.addEventListener('api-sync-state', e => { cloudSyncState.value = e.detail; });
export const getLocalDebts = () => read(KEY, []);
export const setLocalDebts = list => accountStorage.setItem(KEY, JSON.stringify(list));
export const getDeletedDebtIds = () => read('second_brain_deleted_debts_v1', []);
export const addDeletedDebtId = id => accountStorage.setItem('second_brain_deleted_debts_v1', JSON.stringify([...new Set([...getDeletedDebtIds(), String(id)])]));
export const getLocalCarryOver = () => accountStorage.getItem('second_brain_carry_over_v1') === 'true';
export const setLocalCarryOver = value => accountStorage.setItem('second_brain_carry_over_v1', String(!!value));
let running = Promise.resolve();
const serial = fn => { const next = running.catch(() => {}).then(fn); running = next; return next; };
const fingerprint = item => JSON.stringify(['title','recordType','totalAmount','paidAmount','breakdownNote','dueDate','periodLabel','priorityLevel','isCompleted','categoryTag'].map(k => item[k] ?? null));
const saved = () => {
  cloudSyncState.value = pendingCount() ? 'error' : 'synced';
  lastSyncTime.value = new Date().toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit' });
  window.dispatchEvent(new Event('secondbrain-cloud-synced'));
};
async function sendChanges(list) {
  const baseline = read(BASE, []);
  const known = new Map(baseline.map(x => [String(x.id), x]));
  for (const id of getDeletedDebtIds()) {
    const res = await apiFetch(`/debts/${id}`, { method: 'DELETE' });
    if (!res.ok && res.status !== 404) throw new Error('Không xóa được khoản mục.');
  }
  const deleted = new Set(getDeletedDebtIds());
  for (const item of list) {
    if (deleted.has(String(item.id))) continue;
    if (known.has(String(item.id)) && fingerprint(known.get(String(item.id))) === fingerprint(item)) continue;
    const res = await apiFetch(`/debts/${item.id}`, { method: 'PUT', body: JSON.stringify(item) });
    if (!res.ok) throw new Error('Chưa lưu được khoản mục.');
  }
  accountStorage.setItem('second_brain_deleted_debts_v1', '[]');
  accountStorage.setItem(BASE, JSON.stringify(list.filter(x => !deleted.has(String(x.id)))));
}
export const pushDebtsToCloud = (list, carryOver = getLocalCarryOver()) => {
  setLocalDebts(list);
  setLocalCarryOver(carryOver);
  const snapshot = JSON.parse(JSON.stringify(list));
  return serial(async () => {
    cloudSyncState.value = 'syncing';
    try { await sendChanges(snapshot); saved(); return true; }
    catch (error) { syncError.value = error.message || 'Chưa lưu được khoản nợ.'; cloudSyncState.value = 'error'; return false; }
  });
};
export const syncDebtsBidirectional = () => serial(async () => {
  cloudSyncState.value = 'syncing';
  try {
    await flushPending();
    const response = await apiFetch('/debts');
    if (!response.ok) throw new Error(`API /debts trả lỗi ${response.status}. Kiểm tra log backend Render nếu lỗi từ máy chủ.`);
    let server = await response.json();
    if (!accountStorage.getItem('debt_migration_v3')) {
      const notesResponse = await apiFetch('/notes');
      if (!notesResponse.ok) throw new Error(`API /notes trả lỗi ${notesResponse.status}; chưa đọc được bản sao lưu cũ.`);
      const vaults = (await notesResponse.json()).filter(n => n.title === VAULT_NOTE_TITLE);
      const legacy = new Map(getLocalDebts().map(x => [String(x.id), x]));
      const deleted = new Set(getDeletedDebtIds());
      for (const note of vaults) {
        const vault = JSON.parse(note.content);
        for (const id of vault.deletedDebtIds || []) deleted.add(String(id));
        for (const item of vault.debts || []) legacy.set(String(item.id), item);
      }
      for (const item of server) legacy.set(String(item.id), item);
      const imports = [...legacy.values()].filter(x => !deleted.has(String(x.id)));
      accountStorage.setItem('second_brain_deleted_debts_v1', JSON.stringify([...deleted]));
      accountStorage.setItem(BASE, JSON.stringify(server));
      await sendChanges(imports);
      accountStorage.setItem('debt_migration_v3', 'true');
      server = imports;
    } else {
      // Preserve edits waiting on this device; unchanged rows come from the server.
      const baseline = new Map(read(BASE, []).map(x => [String(x.id), fingerprint(x)]));
      const dirty = getLocalDebts().filter(x => baseline.get(String(x.id)) !== fingerprint(x));
      if (dirty.length || getDeletedDebtIds().length) {
        const merged = new Map(server.map(x => [String(x.id), x]));
        for (const row of dirty) merged.set(String(row.id), row);
        const deleted = new Set(getDeletedDebtIds());
        server = [...merged.values()].filter(x => !deleted.has(String(x.id)));
        await sendChanges(server);
      }
    }
    setLocalDebts(server);
    accountStorage.setItem(BASE, JSON.stringify(server));
    saved();
    return server;
  } catch (error) { syncError.value = error.message || 'Chưa tải được khoản nợ.'; cloudSyncState.value = 'error'; return getLocalDebts(); }
});
