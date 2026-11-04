import { ref } from 'vue';
import { apiFetch } from './api';

export const VAULT_NOTE_TITLE = '__SB_CLOUD_VAULT_V2__';
const DEBTS_KEY = 'second_brain_debts_v1';
const DELETED_DEBTS_KEY = 'second_brain_deleted_debts_v1';
const CARRY_OVER_KEY = 'second_brain_carry_over_v1';

// Trạng thái đồng bộ toàn cục hiển thị trên Header (PC & Mobile)
// 'synced' | 'syncing' | 'error'
export const cloudSyncState = ref('synced');
export const lastSyncTime = ref('');

const updateTimestamp = () => {
  const now = new Date();
  lastSyncTime.value = `${String(now.getHours()).padStart(2, '0')}:${String(now.getMinutes()).padStart(2, '0')}`;
};

export const getLocalDebts = () => {
  try {
    const raw = localStorage.getItem(DEBTS_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
};

export const setLocalDebts = (list) => {
  try {
    localStorage.setItem(DEBTS_KEY, JSON.stringify(list));
  } catch {}
};

export const getDeletedDebtIds = () => {
  try {
    const raw = localStorage.getItem(DELETED_DEBTS_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch {
    return [];
  }
};

export const addDeletedDebtId = (id) => {
  try {
    const list = getDeletedDebtIds();
    if (!list.includes(String(id))) {
      list.push(String(id));
      localStorage.setItem(DELETED_DEBTS_KEY, JSON.stringify(list));
    }
  } catch {}
};

export const getLocalCarryOver = () => {
  try {
    return localStorage.getItem(CARRY_OVER_KEY) === 'true';
  } catch {
    return false;
  }
};

export const setLocalCarryOver = (val) => {
  try {
    localStorage.setItem(CARRY_OVER_KEY, val ? 'true' : 'false');
  } catch {}
};

/**
 * Tạo khóa định danh nội dung để chống trùng lặp khi gộp dữ liệu giữa PC và Điện thoại
 */
const getItemSignature = (item) => {
  return `${(item.title || '').trim().toLowerCase()}|${item.recordType || 'DEBT'}|${Number(item.totalAmount) || 0}|${item.periodLabel || '1-10'}`;
};

/**
 * Đẩy trạng thái Khoản Nợ & Dòng Tiền hiện tại lên Cloud Vault (lưu trên TiDB Cloud qua /api/notes + /api/debts)
 */
export const pushDebtsToCloud = async (debtsList, carryOver = getLocalCarryOver()) => {
  cloudSyncState.value = 'syncing';
  try {
    setLocalDebts(debtsList);
    setLocalCarryOver(carryOver);

    const deletedIds = getDeletedDebtIds();
    const vaultPayload = {
      version: 2,
      updatedAt: Date.now(),
      carryOverBalance: Boolean(carryOver),
      deletedDebtIds: deletedIds,
      debts: debtsList,
    };

    // 1. Tìm xem bản ghi Cloud Vault đã tồn tại trong /api/notes của user chưa
    const notesRes = await apiFetch('/notes');
    if (notesRes.ok) {
      const allNotes = await notesRes.json();
      const existingVault = Array.isArray(allNotes)
        ? allNotes.find(n => n.title === VAULT_NOTE_TITLE)
        : null;

      if (existingVault && existingVault.id) {
        await apiFetch(`/notes/${existingVault.id}`, {
          method: 'PUT',
          body: JSON.stringify({
            title: VAULT_NOTE_TITLE,
            content: JSON.stringify(vaultPayload),
            color: '#4f46e5',
            isPinned: false,
          }),
        });
      } else {
        await apiFetch('/notes', {
          method: 'POST',
          body: JSON.stringify({
            title: VAULT_NOTE_TITLE,
            content: JSON.stringify(vaultPayload),
            color: '#4f46e5',
            isPinned: false,
          }),
        });
      }
    }

    cloudSyncState.value = 'synced';
    updateTimestamp();
    window.dispatchEvent(new CustomEvent('secondbrain-cloud-synced'));
    return true;
  } catch (err) {
    console.warn('Cloud push warning:', err);
    cloudSyncState.value = 'error';
    return false;
  }
};

/**
 * Đồng bộ 2 chiều thông minh giữa Máy tính (LocalStorage) và Đám mây (TiDB Cloud):
 * - Nếu Máy tính đang có sẵn dữ liệu mà Cloud chưa có -> Tự động gộp và đẩy lên Cloud để Điện thoại nhận được ngay.
 * - Nếu Cloud đã có dữ liệu mới từ thiết bị kia -> Tải về cập nhật cho máy hiện tại.
 */
export const syncDebtsBidirectional = async () => {
  cloudSyncState.value = 'syncing';
  try {
    const localDebts = getLocalDebts();
    const localDeleted = getDeletedDebtIds();
    let cloudDebts = [];
    let cloudDeleted = [];
    let cloudCarryOver = null;
    let vaultNote = null;

    // 1. Đọc từ Cloud Vault (/api/notes) - Kênh ổn định 100% trên TiDB Cloud
    const notesRes = await apiFetch('/notes');
    if (notesRes.ok) {
      const allNotes = await notesRes.json();
      if (Array.isArray(allNotes)) {
        vaultNote = allNotes.find(n => n.title === VAULT_NOTE_TITLE);
        if (vaultNote && vaultNote.content) {
          try {
            const parsed = JSON.parse(vaultNote.content);
            if (Array.isArray(parsed.debts)) cloudDebts = parsed.debts;
            if (Array.isArray(parsed.deletedDebtIds)) cloudDeleted = parsed.deletedDebtIds;
            if (typeof parsed.carryOverBalance === 'boolean') cloudCarryOver = parsed.carryOverBalance;
          } catch {}
        }
      }
    }

    // 2. Đọc bổ sung từ /api/debts nếu có
    try {
      const debtsApiRes = await apiFetch('/debts');
      if (debtsApiRes.ok) {
        const apiDebts = await debtsApiRes.json();
        if (Array.isArray(apiDebts) && apiDebts.length > 0) {
          for (const d of apiDebts) {
            const sig = getItemSignature(d);
            const alreadyInCloud = cloudDebts.some(
              c => String(c.id) === String(d.id) || getItemSignature(c) === sig
            );
            if (!alreadyInCloud) {
              cloudDebts.push(d);
            }
          }
        }
      }
    } catch {}

    // 3. Hợp nhất danh sách ID đã xóa
    const mergedDeletedIds = Array.from(new Set([...localDeleted, ...cloudDeleted].map(String)));
    try {
      localStorage.setItem(DELETED_DEBTS_KEY, JSON.stringify(mergedDeletedIds));
    } catch {}

    // 4. Hợp nhất danh sách Khoản Nợ & Thu Nhập giữa Local và Cloud (Loại bỏ các mục đã xóa)
    const mergedMap = new Map();
    const seenSignatures = new Set();

    // Ưu tiên dữ liệu từ Cloud trước (để lấy trạng thái cập nhật mới nhất)
    for (const item of cloudDebts) {
      if (!item || mergedDeletedIds.includes(String(item.id))) continue;
      const sig = getItemSignature(item);
      mergedMap.set(String(item.id), item);
      seenSignatures.add(sig);
    }

    // Kiểm tra xem trên máy hiện tại (VD: Máy tính) có khoản nào mới nhập mà chưa lên Cloud không
    let hasUnsyncedLocalChanges = false;
    for (const localItem of localDebts) {
      if (!localItem || mergedDeletedIds.includes(String(localItem.id))) continue;
      const idStr = String(localItem.id);
      const sig = getItemSignature(localItem);

      if (!mergedMap.has(idStr) && !seenSignatures.has(sig)) {
        mergedMap.set(idStr, localItem);
        seenSignatures.add(sig);
        hasUnsyncedLocalChanges = true;
      }
    }

    const finalDebts = Array.from(mergedMap.values());
    setLocalDebts(finalDebts);

    if (cloudCarryOver !== null) {
      setLocalCarryOver(cloudCarryOver);
    }

    // Nếu máy hiện tại có dữ liệu chưa lên Cloud (hoặc chưa từng có Vault trên Cloud), đẩy ngay lên Cloud!
    if (hasUnsyncedLocalChanges || (!vaultNote && finalDebts.length > 0)) {
      await pushDebtsToCloud(finalDebts, getLocalCarryOver());
    } else {
      cloudSyncState.value = 'synced';
      updateTimestamp();
    }

    window.dispatchEvent(new CustomEvent('secondbrain-cloud-synced'));
    return finalDebts;
  } catch (err) {
    console.warn('Bidirectional sync warning:', err);
    cloudSyncState.value = 'error';
    return getLocalDebts();
  }
};
