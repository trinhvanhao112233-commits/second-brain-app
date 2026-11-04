<template>
  <!-- Màn hình Đăng nhập / Đăng ký -->
  <AuthView v-if="!currentUser" @auth-success="handleAuthSuccess" />

  <!-- Khung Ứng Dụng Chính (Responsive Desktop Sidebar + Mobile Top/Bottom Bars) -->
  <div v-else class="min-h-screen bg-[#f4f7fb] text-slate-900 flex flex-col md:flex-row">
    <!-- ==================== 1. SIDEBAR MÁY TÍNH (DESKTOP) ==================== -->
    <aside class="hidden md:flex flex-col w-68 bg-white border-r border-slate-200/90 fixed inset-y-0 left-0 z-30 select-none shadow-sm">
      <!-- Brand Header -->
      <div class="px-5 py-5 flex items-center justify-between border-b border-slate-100">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-2xl bg-gradient-to-br from-indigo-600 to-violet-600 text-white flex items-center justify-center shadow-md shadow-indigo-500/25 shrink-0">
            <span class="material-symbols-rounded text-2xl">space_dashboard</span>
          </div>
          <div>
            <h1 class="font-extrabold text-base tracking-tight text-slate-900 leading-none">Personal OS</h1>
            <p class="text-[11px] text-slate-400 font-semibold mt-1">Tài chính • Dòng tiền • Lịch</p>
          </div>
        </div>

        <!-- Nút báo trạng thái Cloud Sync trực tiếp -->
        <button
          type="button"
          @click="triggerManualCloudSync"
          class="px-2.5 py-1 rounded-full text-[10px] font-bold flex items-center gap-1 border transition-all"
          :class="cloudSyncState === 'syncing'
            ? 'bg-indigo-50 text-indigo-600 border-indigo-200'
            : cloudSyncState === 'synced'
            ? 'bg-emerald-50 text-emerald-700 border-emerald-200 hover:bg-emerald-100'
            : 'bg-amber-50 text-amber-700 border-amber-200'"
          title="Bấm để đồng bộ dữ liệu ngay với Điện thoại / Máy tính"
        >
          <span
            class="material-symbols-rounded text-xs"
            :class="{ 'animate-spin': cloudSyncState === 'syncing' }"
          >
            {{ cloudSyncState === 'syncing' ? 'sync' : cloudSyncState === 'synced' ? 'cloud_done' : 'cloud_sync' }}
          </span>
          <span>{{ cloudSyncState === 'syncing' ? 'Đang lưu...' : 'Cloud OK' }}</span>
        </button>
      </div>

      <!-- Thẻ tóm tắt tài chính nhanh trên Sidebar -->
      <div class="mx-3.5 mt-4 p-4 rounded-2xl bg-gradient-to-br from-slate-900 via-indigo-950 to-slate-900 text-white shadow-lg space-y-2.5">
        <div class="flex items-center justify-between">
          <span class="text-[10px] font-bold uppercase tracking-wider text-indigo-200">Số dư các ví</span>
          <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-emerald-500/20 text-emerald-300 border border-emerald-400/30">
            Định mức {{ formatShortK(safeDailySpend) }}/ngày
          </span>
        </div>
        <div class="text-xl font-black font-mono text-white tracking-tight">
          {{ formatVND(totalNetWorth) }}
        </div>
        <div class="pt-2 border-t border-white/10 flex items-center justify-between text-[11px]">
          <span class="text-slate-300">Nợ chưa trả:</span>
          <span class="font-mono font-bold text-rose-300">
            -{{ formatShortK(totalUnpaidDebt) }} ({{ unpaidDebtCount }} khoản)
          </span>
        </div>
      </div>

      <!-- Menu Điều Hướng -->
      <nav class="flex-1 px-3 py-4 space-y-1 overflow-y-auto">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          @click="activeTab = item.id"
          class="w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-bold transition-all duration-150"
          :class="activeTab === item.id
            ? 'bg-indigo-600 text-white shadow-sm shadow-indigo-500/20'
            : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'"
        >
          <div class="flex items-center gap-3">
            <span class="material-symbols-rounded text-[20px]">{{ item.iconSymbol }}</span>
            <span>{{ item.label }}</span>
          </div>
          <span
            v-if="item.badge"
            class="px-2 py-0.5 rounded-full text-[10px] font-extrabold font-mono"
            :class="activeTab === item.id ? 'bg-white/20 text-white' : item.badgeClass"
          >
            {{ item.badge }}
          </span>
        </button>
      </nav>

      <!-- Khu vực Tài khoản ở đáy Sidebar -->
      <div class="p-3.5 border-t border-slate-100 space-y-2 bg-slate-50/60">
        <div class="flex items-center justify-between px-1">
          <div class="flex items-center gap-2.5 min-w-0">
            <div class="w-8 h-8 rounded-xl bg-indigo-100 text-indigo-700 flex items-center justify-center font-extrabold text-xs shrink-0">
              {{ (currentUser.fullName || currentUser.username || 'U').charAt(0).toUpperCase() }}
            </div>
            <div class="min-w-0">
              <p class="text-xs font-bold text-slate-900 truncate">
                {{ currentUser.fullName || currentUser.username }}
              </p>
              <p class="text-[10px] text-slate-400 truncate">@{{ currentUser.username }}</p>
            </div>
          </div>

          <div class="flex items-center gap-1 shrink-0">
            <button
              type="button"
              @click="toggleNotificationSubscription"
              class="w-7 h-7 rounded-lg flex items-center justify-center transition-colors"
              :class="isNotificationEnabled ? 'bg-emerald-50 text-emerald-600' : 'text-slate-400 hover:bg-slate-200 hover:text-slate-700'"
              title="Bật thông báo nhắc lịch"
            >
              <span class="material-symbols-rounded text-base">
                {{ isNotificationEnabled ? 'notifications_active' : 'notifications' }}
              </span>
            </button>
            <button
              type="button"
              @click="showChangePasswordModal = true"
              class="w-7 h-7 rounded-lg text-slate-400 hover:bg-slate-200 hover:text-slate-700 flex items-center justify-center transition-colors"
              title="Đổi mật khẩu"
            >
              <span class="material-symbols-rounded text-base">key</span>
            </button>
            <button
              type="button"
              @click="handleLogout"
              class="w-7 h-7 rounded-lg text-slate-400 hover:bg-rose-50 hover:text-rose-600 flex items-center justify-center transition-colors"
              title="Đăng xuất"
            >
              <span class="material-symbols-rounded text-base">logout</span>
            </button>
          </div>
        </div>
      </div>
    </aside>

    <!-- ==================== 2. HEADER ĐIỆN THOẠI (MOBILE TOP BAR) ==================== -->
    <header class="md:hidden sticky top-0 z-30 bg-white/95 backdrop-blur-md border-b border-slate-200/80 px-3.5 py-2.5 flex items-center justify-between safe-top shadow-sm">
      <div class="flex items-center gap-2.5 min-w-0">
        <div class="w-9 h-9 rounded-xl bg-gradient-to-br from-indigo-600 to-violet-600 text-white flex items-center justify-center shadow-sm shrink-0">
          <span class="material-symbols-rounded text-lg">space_dashboard</span>
        </div>
        <div class="min-w-0">
          <h1 class="font-extrabold text-sm text-slate-900 leading-tight truncate">{{ currentTabTitle }}</h1>
          <div class="flex items-center gap-1.5 text-[10px] text-slate-500">
            <span class="truncate">@{{ currentUser.username }}</span>
            <span>•</span>
            <button
              type="button"
              @click="triggerManualCloudSync"
              class="inline-flex items-center gap-0.5 font-bold"
              :class="cloudSyncState === 'syncing' ? 'text-indigo-600' : 'text-emerald-600'"
            >
              <span class="material-symbols-rounded text-[12px]" :class="{ 'animate-spin': cloudSyncState === 'syncing' }">
                {{ cloudSyncState === 'syncing' ? 'sync' : 'cloud_done' }}
              </span>
              <span>{{ cloudSyncState === 'syncing' ? 'Đang đồng bộ...' : 'Đã đồng bộ' }}</span>
            </button>
          </div>
        </div>
      </div>

      <!-- Mobile Quick Actions -->
      <div class="flex items-center gap-1.5 shrink-0">
        <button
          type="button"
          @click="activeTab = 'finance'"
          class="px-2.5 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-emerald-700 font-mono font-extrabold text-xs"
        >
          {{ formatShortK(totalNetWorth) }}
        </button>

        <button
          type="button"
          @click="triggerManualCloudSync"
          class="w-8 h-8 rounded-full bg-slate-100 text-slate-600 hover:text-indigo-600 flex items-center justify-center border border-slate-200"
          title="Đồng bộ dữ liệu ngay"
        >
          <span class="material-symbols-rounded text-base" :class="{ 'animate-spin text-indigo-600': cloudSyncState === 'syncing' }">sync</span>
        </button>

        <button
          type="button"
          @click="handleLogout"
          class="w-8 h-8 rounded-full bg-slate-100 text-slate-600 hover:text-rose-600 flex items-center justify-center border border-slate-200"
          title="Đăng xuất"
        >
          <span class="material-symbols-rounded text-base">logout</span>
        </button>
      </div>
    </header>

    <!-- ==================== 3. NỘI DUNG CHÍNH (MAIN WORKSPACE) ==================== -->
    <main class="flex-1 md:ml-68 pb-24 md:pb-10">
      <div class="max-w-7xl mx-auto px-3.5 sm:px-6 lg:px-8 py-4 sm:py-6">
        <!-- Tab 1: Bảng Điều Khiển Hôm Nay -->
        <DailyHub
          v-if="activeTab === 'hub'"
          :wallets="wallets"
          :debts="debts"
          :events="events"
          :tasks="tasks"
          :notes="notes"
          @navigate="activeTab = $event"
          @toggle-task="toggleTask"
          @refresh-all="triggerManualCloudSync"
        />

        <!-- Tab 2: Quản lý Khoản Nợ & Dòng Tiền Theo Mốc -->
        <DebtPlannerModule
          v-else-if="activeTab === 'debts'"
          @debts-updated="syncDebtsFromStorage"
        />

        <!-- Tab 3: Sổ Thu Chi & Ví Tiền 1 Chạm -->
        <FinanceDashboard
          v-else-if="activeTab === 'finance'"
          @wallets-updated="syncWalletsFromStorage"
        />

        <!-- Tab 4: Lịch Trình Tuần & Ca Học/Làm -->
        <CalendarModule
          v-else-if="activeTab === 'calendar'"
          :events="events"
          @refresh-events="fetchEvents"
        />

        <!-- Tab 5: Công Việc & Deadline -->
        <TasksModule
          v-else-if="activeTab === 'tasks'"
          :tasks="tasks"
          @refresh-tasks="fetchTasks"
          @toggle-task="toggleTask"
        />

        <!-- Tab 6: Sổ Ghi Chú -->
        <NotesModule
          v-else-if="activeTab === 'notes'"
          :notes="notes"
          @refresh-notes="fetchNotes"
        />
      </div>
    </main>

    <!-- ==================== 4. THANH ĐIỀU HƯỚNG ĐÁY ĐIỆN THOẠI (MOBILE BOTTOM NAV) ==================== -->
    <nav class="md:hidden fixed bottom-0 inset-x-0 z-30 bg-white/95 backdrop-blur-lg border-t border-slate-200/90 px-1 py-1 safe-bottom shadow-[0_-4px_20px_rgba(15,23,42,0.06)]">
      <div class="grid grid-cols-6 gap-0.5 max-w-md mx-auto">
        <button
          v-for="item in navItems"
          :key="item.id"
          type="button"
          @click="activeTab = item.id"
          class="relative flex flex-col items-center justify-center py-1.5 px-0.5 rounded-xl transition-all"
          :class="activeTab === item.id ? 'text-indigo-600' : 'text-slate-500 hover:text-slate-800'"
        >
          <div
            class="w-9 h-7 rounded-full flex items-center justify-center transition-all"
            :class="activeTab === item.id ? 'bg-indigo-50 text-indigo-600' : ''"
          >
            <span class="material-symbols-rounded text-[20px]">{{ item.iconSymbol }}</span>
          </div>
          <span class="text-[10px] font-bold tracking-tight mt-0.5 truncate max-w-full">
            {{ item.mobileLabel }}
          </span>
          <!-- Badge báo số nợ / việc -->
          <span
            v-if="item.badge"
            class="absolute top-0.5 right-1.5 min-w-[15px] h-[15px] px-1 rounded-full bg-rose-600 text-white text-[9px] font-extrabold flex items-center justify-center shadow-sm"
          >
            {{ item.badge }}
          </span>
        </button>
      </div>
    </nav>

    <!-- Modal Đổi Mật Khẩu -->
    <ChangePasswordModal
      :is-open="showChangePasswordModal"
      @close="showChangePasswordModal = false"
    />

    <!-- Trợ Lý AI Thông Minh -->
    <AiChatWidget @data-updated="handleAiDataUpdated" />
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted, onUnmounted } from 'vue';
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
  VAULT_NOTE_TITLE,
  getLocalDebts,
  syncDebtsBidirectional,
} from './services/cloudSync';
import { registerPushNotification } from './services/pushNotification';

const currentUser = ref(getStoredUser());
const showChangePasswordModal = ref(false);
const isNotificationEnabled = ref(false);
const activeTab = ref('hub');

const loadJson = (key, fallback = []) => {
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch {
    return fallback;
  }
};

const saveJson = (key, val) => {
  try {
    localStorage.setItem(key, JSON.stringify(val));
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
  { id: 'hub', label: 'Bảng điều khiển Hôm nay', mobileLabel: 'Hôm nay', iconSymbol: 'dashboard', badge: 0 },
  {
    id: 'debts',
    label: 'Tiền nợ & Mốc dòng tiền',
    mobileLabel: 'Tiền nợ',
    iconSymbol: 'receipt_long',
    badge: unpaidDebtCount.value,
    badgeClass: 'bg-rose-100 text-rose-700',
  },
  { id: 'finance', label: 'Ví tiền & Thu chi', mobileLabel: 'Ví tiền', iconSymbol: 'account_balance_wallet', badge: 0 },
  { id: 'calendar', label: 'Lịch tuần & Ca học', mobileLabel: 'Lịch', iconSymbol: 'calendar_month', badge: 0 },
  {
    id: 'tasks',
    label: 'Công việc & Deadline',
    mobileLabel: 'Việc',
    iconSymbol: 'check_circle',
    badge: pendingTaskCount.value,
    badgeClass: 'bg-amber-100 text-amber-800',
  },
  { id: 'notes', label: 'Sổ Ghi chú nhanh', mobileLabel: 'Ghi chú', iconSymbol: 'edit_note', badge: 0 },
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
  fetchAllData();
};

const handleLogout = () => {
  if (confirm('Bạn có chắc muốn đăng xuất khỏi tài khoản?')) {
    setStoredUser(null);
    currentUser.value = null;
  }
};

const fetchWallets = async () => {
  syncWalletsFromStorage();
  try {
    const res = await apiFetch('/wallets');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        wallets.value = data;
        saveJson('second_brain_wallets_v1', data);
      }
    }
  } catch {}
};

const fetchDebts = async () => {
  syncDebtsFromStorage();
  const synced = await syncDebtsBidirectional();
  debts.value = synced;
};

const fetchEvents = async () => {
  events.value = loadJson('second_brain_events_v1', events.value);
  try {
    const res = await apiFetch('/events');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data)) {
        events.value = data;
        saveJson('second_brain_events_v1', data);
      }
    }
  } catch {}
};

const fetchTasks = async () => {
  tasks.value = loadJson('second_brain_tasks_v1', tasks.value);
  try {
    const res = await apiFetch('/tasks');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data)) {
        tasks.value = data;
        saveJson('second_brain_tasks_v1', data);
      }
    }
  } catch {}
};

const toggleTask = async (id) => {
  const t = tasks.value.find(x => x.id === id);
  if (t) {
    t.isCompleted = !t.isCompleted;
    saveJson('second_brain_tasks_v1', tasks.value);
  }
  try {
    await apiFetch(`/tasks/${id}/toggle`, { method: 'PATCH' });
  } catch {}
};

const fetchNotes = async () => {
  notes.value = loadJson('second_brain_notes_v1', notes.value).filter(n => n.title !== VAULT_NOTE_TITLE);
  try {
    const res = await apiFetch('/notes');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data)) {
        const visibleNotes = data.filter(n => n.title !== VAULT_NOTE_TITLE);
        notes.value = visibleNotes;
        saveJson('second_brain_notes_v1', visibleNotes);
      }
    }
  } catch {}
};

const handleAiDataUpdated = () => {
  fetchAllData();
};

const fetchAllData = async () => {
  if (!currentUser.value) return;
  await Promise.all([
    fetchDebts(),
    fetchWallets(),
    fetchEvents(),
    fetchTasks(),
    fetchNotes(),
  ]);
};

const triggerManualCloudSync = async () => {
  await fetchAllData();
};

// Tự động đồng bộ lại khi người dùng chuyển tab trên web
watch(activeTab, () => {
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
  window.removeEventListener('secondbrain-data-updated', handleGlobalStorageSync);
  window.removeEventListener('secondbrain-cloud-synced', handleGlobalStorageSync);
  window.removeEventListener('focus', handleVisibilityOrFocus);
  document.removeEventListener('visibilitychange', handleVisibilityOrFocus);
  if (autoSyncTimer) clearInterval(autoSyncTimer);
});
</script>
