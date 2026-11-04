<template>
  <AuthView v-if="!currentUser" @auth-success="handleAuthSuccess" />
  <div v-else class="app-shell">
    <aside class="app-sidebar">
      <a class="app-brand" href="?tab=hub" @click.prevent="activeTab='hub'"><span class="brand-icon material-symbols-rounded">space_dashboard</span><span>Personal OS<small>Không gian của bạn</small></span></a>
      <p class="sidebar-caption">KHÔNG GIAN LÀM VIỆC</p>
      <nav class="sidebar-nav" aria-label="Điều hướng chính"><button v-for="item in navItems" :key="item.id" :class="['nav-link',{selected:activeTab===item.id}]" @click="activeTab=item.id"><span class="material-symbols-rounded">{{ item.iconSymbol }}</span><span>{{ item.label }}</span><span v-if="item.badge" class="nav-count">{{ item.badge }}</span></button></nav>
      <div class="sidebar-bottom"><button class="sync-button" @click="triggerManualCloudSync"><span class="material-symbols-rounded" :class="{'animate-spin':cloudSyncState==='syncing'}">{{ cloudSyncState==='error'?'cloud_off':'cloud_sync' }}</span><span>{{ syncLabel }}<small v-if="lastSyncTime">Lần cuối {{ lastSyncTime }}</small></span></button><div class="user-panel"><span class="user-avatar">{{ (currentUser.fullName || currentUser.username).charAt(0).toUpperCase() }}</span><span class="min-w-0 flex-1"><strong class="block truncate">{{ currentUser.fullName || currentUser.username }}</strong><small>@{{ currentUser.username }}</small></span><button title="Đăng xuất" aria-label="Đăng xuất" @click="handleLogout"><span class="material-symbols-rounded">logout</span></button></div></div>
    </aside>
    <main class="app-main">
      <header class="workspace-header"><div class="flex items-center gap-3"><span class="mobile-brand material-symbols-rounded">space_dashboard</span><span class="text-sm text-slate-500">Không gian cá nhân <span class="mx-2 text-slate-300">/</span><strong class="text-slate-800">{{ currentTabTitle }}</strong></span></div><div class="flex items-center gap-2"><button class="icon-button" aria-label="Thông báo và cài đặt" title="Thông báo và cài đặt" @click="activeTab='settings'"><span class="material-symbols-rounded">notifications</span><span v-if="unpaidDebtCount" class="notification-dot"></span></button><button class="icon-button" aria-label="Đổi mật khẩu" title="Đổi mật khẩu" @click="showChangePasswordModal=true"><span class="material-symbols-rounded">key</span></button><button class="icon-button md:hidden" aria-label="Đăng xuất" @click="handleLogout"><span class="material-symbols-rounded">logout</span></button></div></header>
      <div v-if="cloudSyncState==='error'" class="sync-warning" role="status"><span>{{ syncError || 'Chưa tải hoặc đồng bộ đủ dữ liệu. Kiểm tra kết nối và thử lại.' }}</span><button @click="triggerManualCloudSync">Thử lại</button></div>
      <div class="workspace-content" :key="currentUser.id">
        <DailyHub v-if="activeTab==='hub'" :user="currentUser" :wallets="wallets" :debts="debts" :events="events" :tasks="tasks" :notes="notes" @switch-tab="activeTab=$event" @toggle-task="toggleTask" @refresh-all="triggerManualCloudSync" />
        <DebtPlannerModule v-else-if="activeTab==='debts'" @debts-updated="syncDebtsFromStorage" />
        <FinanceDashboard v-else-if="activeTab==='finance'" @wallets-updated="syncWalletsFromStorage" />
        <CalendarModule v-else-if="activeTab==='calendar'" :events="events" @refresh-events="handleGlobalStorageSync" />
        <TasksModule v-else-if="activeTab==='tasks'" :tasks="tasks" @refresh-tasks="handleGlobalStorageSync" @toggle-task="toggleTask" />
        <NotesModule v-else-if="activeTab==='notes'" :notes="notes" @refresh-notes="handleGlobalStorageSync" />
        <NotificationSettings v-else-if="activeTab==='settings'" />
      </div>
    </main>
    <nav class="mobile-nav" aria-label="Điều hướng điện thoại"><button v-for="item in navItems" :key="item.id" :class="{selected:activeTab===item.id}" @click="activeTab=item.id"><span class="material-symbols-rounded">{{ item.iconSymbol }}</span><span>{{ item.mobileLabel }}</span></button></nav>
    <ChangePasswordModal :is-open="showChangePasswordModal" @close="showChangePasswordModal=false" />
    <AiChatWidget @data-updated="handleAiDataUpdated" />
  </div>
  <div v-if="notice" role="status" class="app-toast"><span>{{ notice }}</span><button @click="notice=''" aria-label="Đóng thông báo">×</button></div>
</template>

<script setup>
import { accountStorage } from './services/storage';
import { signOut } from './services/session';

import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
import NotificationSettings from './components/NotificationSettings.vue';
import DailyHub from './components/DailyHub.vue';
import DebtPlannerModule from './components/DebtPlannerModule.vue';
import FinanceDashboard from './components/FinanceDashboard.vue';
import CalendarModule from './components/CalendarModule.vue';
import TasksModule from './components/TasksModule.vue';
import NotesModule from './components/NotesModule.vue';
import AuthView from './components/AuthView.vue';
import ChangePasswordModal from './components/ChangePasswordModal.vue';
import AiChatWidget from './components/AiChatWidget.vue';
import { apiFetch, getStoredUser, setStoredUser } from './services/api';
import {
  cloudSyncState,
  syncError,
  lastSyncTime,
  VAULT_NOTE_TITLE,
  getLocalDebts,
  syncDebtsBidirectional,
} from './services/cloudSync';
import { unregisterPushNotification, registerPushNotification } from './services/pushNotification';

const currentUser = ref(getStoredUser());
const showChangePasswordModal = ref(false);
const isNotificationEnabled = ref(false);
const allowedTabs = ['hub','finance','debts','calendar','tasks','notes','settings'];
const initialTab = new URLSearchParams(location.search).get('tab');
const activeTab = ref(allowedTabs.includes(initialTab) ? initialTab : 'hub');
const notice = ref('');
let noticeTimer;
const showNotice = e => { notice.value = e.detail.message; clearTimeout(noticeTimer); noticeTimer = setTimeout(() => { notice.value = ''; }, 9000); };
const syncLabel = computed(() => ({idle:'Chưa kiểm tra',syncing:'Đang đồng bộ…',synced:'Đã đồng bộ',error:'Chưa đồng bộ'})[cloudSyncState.value]);
const sessionExpired = () => { currentUser.value = null; setStoredUser(null); notice.value = 'Phiên đăng nhập hết hạn. Vui lòng đăng nhập lại.'; };
const readLocation = () => { const tab = new URLSearchParams(location.search).get('tab'); activeTab.value = allowedTabs.includes(tab) ? tab : 'hub'; };

const loadJson = (key, fallback = []) => {
  try {
    const raw = accountStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch {
    return fallback;
  }
};

const saveJson = (key, val) => {
  try {
    accountStorage.setItem(key, JSON.stringify(val));
  } catch {}
};

const wallets = ref(loadJson('second_brain_wallets_v1', []));
const debts = ref(getLocalDebts());
const events = ref(loadJson('second_brain_events_v1', []));
const tasks = ref(loadJson('second_brain_tasks_v1', []));
const notes = ref(loadJson('second_brain_notes_v1', []).filter(n => n.title !== VAULT_NOTE_TITLE));

const totalNetWorth = computed(() => {
  return wallets.value.reduce((sum, w) => sum + (Number(w.balance) || 0), 0);
});

const safeDailySpend = computed(() => {
  if (totalNetWorth.value <= 0) return 0;
  const now = new Date();
  const daysInMonth = new Date(now.getFullYear(), now.getMonth() + 1, 0).getDate();
  const remaining = Math.max(1, daysInMonth - now.getDate() + 1);
  return Math.floor(totalNetWorth.value / remaining);
});

const totalUnpaidDebt = computed(() => {
  return debts.value
    .filter(d => d.recordType === 'DEBT' && !d.isCompleted)
    .reduce((sum, d) => sum + Math.max(0, (Number(d.totalAmount) || 0) - (Number(d.paidAmount) || 0)), 0);
});

const unpaidDebtCount = computed(() => {
  return debts.value.filter(d => d.recordType === 'DEBT' && !d.isCompleted).length;
});

const pendingTaskCount = computed(() => {
  return tasks.value.filter(t => !t.isCompleted).length;
});

const navItems = computed(() => [
  { id: 'hub', label: 'Hôm nay', mobileLabel: 'Hôm nay', iconSymbol: 'dashboard', badge: 0 },
  {
    id: 'debts',
    label: 'Khoản nợ',
    mobileLabel: 'Tiền nợ',
    iconSymbol: 'receipt_long',
    badge: unpaidDebtCount.value,
    badgeClass: 'bg-rose-100 text-rose-700',
  },
  { id: 'finance', label: 'Tài chính', mobileLabel: 'Ví tiền', iconSymbol: 'account_balance_wallet', badge: 0 },
  { id: 'calendar', label: 'Lịch trình', mobileLabel: 'Lịch', iconSymbol: 'calendar_month', badge: 0 },
  {
    id: 'tasks',
    label: 'Công việc',
    mobileLabel: 'Việc',
    iconSymbol: 'check_circle',
    badge: pendingTaskCount.value,
    badgeClass: 'bg-amber-100 text-amber-800',
  },
  { id: 'notes', label: 'Ghi chú', mobileLabel: 'Ghi chú', iconSymbol: 'edit_note', badge: 0 },
  { id: 'settings', label: 'Cài đặt', mobileLabel: 'Cài đặt', iconSymbol: 'tune', badge: 0 },
]);

const currentTabTitle = computed(() => {
  const found = navItems.value.find(x => x.id === activeTab.value);
  return found ? found.label : 'Personal OS';
});

const formatVND = (val) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(val || 0);
};

const formatShortK = (num) => {
  const val = Number(num) || 0;
  if (Math.abs(val) >= 1000000) {
    return `${(val / 1000000).toFixed(val % 1000000 === 0 ? 0 : 1)}tr`;
  }
  return `${Math.round(val / 1000).toLocaleString('vi-VN')}k`;
};

const syncWalletsFromStorage = () => {
  wallets.value = loadJson('second_brain_wallets_v1', []);
};

const syncDebtsFromStorage = () => {
  debts.value = getLocalDebts();
};

const checkNotificationStatus = async () => {
  try {
    if ('serviceWorker' in navigator && 'PushManager' in window) {
      const reg = await navigator.serviceWorker.getRegistration();
      if (reg) {
        const sub = await reg.pushManager.getSubscription();
        isNotificationEnabled.value = !!sub;
        return;
      }
    }
  } catch {}
  isNotificationEnabled.value = false;
};

const toggleNotificationSubscription = async () => {
  const res = await registerPushNotification();
  if (res.success) {
    isNotificationEnabled.value = true;
    alert('🔔 Đã bật thông báo nhắc lịch trình trên thiết bị!');
  } else {
    alert('⚠️ Vui lòng cấp quyền thông báo trong cài đặt trình duyệt.');
  }
};

const handleAuthSuccess = (user) => {
  currentUser.value = user;
  wallets.value = loadJson('second_brain_wallets_v1', []);
  debts.value = getLocalDebts();
  events.value = loadJson('second_brain_events_v1', []);
  tasks.value = loadJson('second_brain_tasks_v1', []);
  notes.value = loadJson('second_brain_notes_v1', []).filter(n => n.title !== VAULT_NOTE_TITLE);
  fetchAllData();
};

const handleLogout = async () => {
  if (confirm('Bạn có chắc muốn đăng xuất khỏi tài khoản?')) {
    // Browser unsubscribe prevents further delivery. Server cleanup must never block logout.
    void signOut().then(cleaned => {
      if (!cleaned) {
        notice.value = 'Đã đăng xuất. Nếu còn thông báo, hãy tắt quyền thông báo của trang trong trình duyệt.';
      }
    });
    currentUser.value = null;
    wallets.value = []; debts.value = []; events.value = []; tasks.value = []; notes.value = [];
    showChangePasswordModal.value = false;
    cloudSyncState.value = 'idle';
    syncError.value = '';
  }
};

const fetchWallets = async () => {
  syncWalletsFromStorage();
  try {
    const res = await apiFetch('/wallets');
    if (!res.ok) throw new Error('Không tải được dữ liệu.');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data)) {
        wallets.value = data;
        saveJson('second_brain_wallets_v1', data);
      }
    }
  return true;
  } catch { cloudSyncState.value = 'error'; return false; }
};

const fetchDebts = async () => {
  syncDebtsFromStorage();
  const synced = await syncDebtsBidirectional();
  debts.value = synced;
  return cloudSyncState.value !== 'error';
};

const fetchEvents = async () => {
  events.value = loadJson('second_brain_events_v1', events.value);
  try {
    const res = await apiFetch('/events');
    if (!res.ok) throw new Error('Không tải được dữ liệu.');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data)) {
        events.value = data;
        saveJson('second_brain_events_v1', data);
      }
    }
  return true;
  } catch { cloudSyncState.value = 'error'; return false; }
};

const fetchTasks = async () => {
  tasks.value = loadJson('second_brain_tasks_v1', tasks.value);
  try {
    const res = await apiFetch('/tasks');
    if (!res.ok) throw new Error('Không tải được dữ liệu.');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data)) {
        tasks.value = data;
        saveJson('second_brain_tasks_v1', data);
      }
    }
  return true;
  } catch { cloudSyncState.value = 'error'; return false; }
};

const toggleTask = async (id) => {
  const t = tasks.value.find(x => x.id === id);
  if (!t) return;
  if (t) {
    t.isCompleted = !t.isCompleted;
    saveJson('second_brain_tasks_v1', tasks.value);
  }
  try {
    await apiFetch(`/tasks/${id}`, { method: 'PUT', body: JSON.stringify(t) });
  } catch {}
};

const fetchNotes = async () => {
  notes.value = loadJson('second_brain_notes_v1', notes.value).filter(n => n.title !== VAULT_NOTE_TITLE);
  try {
    const res = await apiFetch('/notes');
    if (!res.ok) throw new Error('Không tải được dữ liệu.');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data)) {
        const visibleNotes = data.filter(n => n.title !== VAULT_NOTE_TITLE);
        notes.value = visibleNotes;
        saveJson('second_brain_notes_v1', visibleNotes);
      }
    }
  return true;
  } catch { cloudSyncState.value = 'error'; return false; }
};

const handleAiDataUpdated = () => {
  fetchAllData();
};

const fetchAllData = async () => {
  if (!currentUser.value) return;
  syncError.value = '';
  const results = await Promise.all([
    fetchDebts(),
    fetchWallets(),
    fetchEvents(),
    fetchTasks(),
    fetchNotes(),
  ]);
  cloudSyncState.value = results.every(Boolean) ? 'synced' : 'error';
};

const triggerManualCloudSync = async () => {
  await fetchAllData();
};

// Tự động đồng bộ lại khi người dùng chuyển tab trên web
watch(activeTab, tab => {
  const url = new URL(location.href); url.searchParams.set('tab', tab); history.replaceState(null, '', url);
  if (currentUser.value) {
    syncDebtsBidirectional().then(list => {
      debts.value = list;
    });
  }
});

const handleGlobalStorageSync = () => {
  syncWalletsFromStorage();
  syncDebtsFromStorage();
  events.value = loadJson('second_brain_events_v1', events.value);
  tasks.value = loadJson('second_brain_tasks_v1', tasks.value);
  notes.value = loadJson('second_brain_notes_v1', notes.value).filter(n => n.title !== VAULT_NOTE_TITLE);
};

// Tự động kéo dữ liệu mới nhất khi mở điện thoại / quay lại trình duyệt
const handleVisibilityOrFocus = () => {
  if (document.visibilityState === 'visible' && currentUser.value) {
    fetchAllData();
  }
};

let autoSyncTimer = null;

onMounted(() => {
  window.addEventListener('app-notice', showNotice);
  window.addEventListener('session-expired', sessionExpired);
  window.addEventListener('popstate', readLocation);
  window.addEventListener('online', handleVisibilityOrFocus);
  fetchAllData();
  checkNotificationStatus();
  window.addEventListener('secondbrain-data-updated', handleGlobalStorageSync);
  window.addEventListener('secondbrain-cloud-synced', handleGlobalStorageSync);
  window.addEventListener('focus', handleVisibilityOrFocus);
  document.addEventListener('visibilitychange', handleVisibilityOrFocus);

  // Tự động kiểm tra đồng bộ ngầm mỗi 25 giây
  autoSyncTimer = setInterval(() => {
    if (document.visibilityState === 'visible' && currentUser.value) {
      syncDebtsBidirectional().then(list => {
        debts.value = list;
      });
    }
  }, 25000);
});

onUnmounted(() => {
  clearTimeout(noticeTimer);
  window.removeEventListener('app-notice', showNotice);
  window.removeEventListener('session-expired', sessionExpired);
  window.removeEventListener('popstate', readLocation);
  window.removeEventListener('online', handleVisibilityOrFocus);
  window.removeEventListener('secondbrain-data-updated', handleGlobalStorageSync);
  window.removeEventListener('secondbrain-cloud-synced', handleGlobalStorageSync);
  window.removeEventListener('focus', handleVisibilityOrFocus);
  document.removeEventListener('visibilitychange', handleVisibilityOrFocus);
  if (autoSyncTimer) clearInterval(autoSyncTimer);
});
</script>
