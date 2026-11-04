<template>
  <!-- If not logged in, render AuthView -->
  <AuthView v-if="!currentUser" @auth-success="handleAuthSuccess" />

  <div v-else class="min-h-screen bg-[#f4f6fa] text-slate-800 flex flex-col md:flex-row">
    <!-- ==================== DESKTOP SIDEBAR (NAVIGATION RAIL) ==================== -->
    <aside class="hidden md:flex flex-col justify-between w-64 lg:w-72 bg-white border-r border-slate-200 p-5 sticky top-0 h-screen z-30 shrink-0 shadow-sm">
      <div class="space-y-5">
        <!-- Brand Header -->
        <div class="flex items-center gap-3 px-2 pt-1">
          <div class="w-10 h-10 rounded-2xl bg-indigo-600 text-white flex items-center justify-center shadow-sm">
            <span class="material-symbols-rounded text-2xl">psychology</span>
          </div>
          <div>
            <h1 class="font-extrabold text-base tracking-tight text-slate-900 leading-none">Second Brain</h1>
            <span class="text-[11px] text-slate-500 font-medium">Quản lý Tài chính & Cá nhân</span>
          </div>
        </div>

        <!-- User Profile Card -->
        <div class="p-3 bg-slate-50 border border-slate-200 rounded-2xl flex items-center justify-between">
          <div class="flex items-center gap-2.5 overflow-hidden">
            <div class="w-9 h-9 rounded-xl bg-indigo-100 text-indigo-700 flex items-center justify-center font-extrabold text-xs uppercase shrink-0">
              {{ currentUser.fullName ? currentUser.fullName.charAt(0) : currentUser.username.charAt(0) }}
            </div>
            <div class="overflow-hidden leading-tight">
              <p class="text-xs font-bold text-slate-900 truncate">{{ currentUser.fullName || currentUser.username }}</p>
              <p class="text-[11px] text-slate-500 truncate">@{{ currentUser.username }}</p>
            </div>
          </div>
          <div class="flex items-center gap-0.5 shrink-0">
            <button
              @click="toggleNotificationSubscription"
              :title="isNotificationEnabled ? 'Thông báo đẩy đang BẬT' : 'Bật thông báo nhắc lịch trước 1 tiếng'"
              class="w-7 h-7 rounded-lg flex items-center justify-center transition-colors"
              :class="isNotificationEnabled ? 'text-emerald-600 bg-emerald-50' : 'text-slate-400 hover:text-indigo-600 hover:bg-slate-200/60'"
            >
              <span class="material-symbols-rounded text-base">{{ isNotificationEnabled ? 'notifications_active' : 'notifications' }}</span>
            </button>
            <button
              @click="showChangePasswordModal = true"
              title="Đổi mật khẩu"
              class="w-7 h-7 rounded-lg text-slate-400 hover:text-indigo-600 hover:bg-slate-200/60 flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-base">key</span>
            </button>
            <button
              @click="handleLogout"
              title="Đăng xuất"
              class="w-7 h-7 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-base">logout</span>
            </button>
          </div>
        </div>

        <!-- Navigation Menu -->
        <nav class="space-y-1">
          <button
            v-for="item in navItems"
            :key="item.id"
            @click="activeTab = item.id"
            class="w-full flex items-center justify-between px-3.5 py-2.5 rounded-xl text-xs font-bold transition-all duration-150"
            :class="activeTab === item.id 
              ? 'bg-indigo-600 text-white shadow-sm' 
              : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900'"
          >
            <div class="flex items-center gap-3">
              <span class="material-symbols-rounded text-xl">{{ item.iconSymbol }}</span>
              <span>{{ item.label }}</span>
            </div>
            <span
              v-if="item.badge && item.badge > 0"
              class="px-2 py-0.5 rounded-full text-[10px] font-extrabold"
              :class="activeTab === item.id ? 'bg-white/20 text-white' : item.badgeClass"
            >
              {{ item.badge }}
            </span>
          </button>
        </nav>
      </div>

      <!-- Bottom Summary Widget -->
      <div class="space-y-2.5">
        <div
          @click="activeTab = 'finance'"
          class="cursor-pointer p-3.5 rounded-2xl bg-emerald-50/70 border border-emerald-200 hover:border-emerald-300 transition-all"
        >
          <div class="flex items-center justify-between text-emerald-800">
            <span class="text-[10px] uppercase font-extrabold tracking-wider">Số dư các ví</span>
            <span class="material-symbols-rounded text-base text-emerald-600">account_balance_wallet</span>
          </div>
          <div class="text-base font-black font-mono text-emerald-700 mt-0.5 truncate">{{ formatVND(totalNetWorth) }}</div>
        </div>

        <div
          v-if="totalUnpaidDebt > 0"
          @click="activeTab = 'debts'"
          class="cursor-pointer p-3.5 rounded-2xl bg-rose-50/70 border border-rose-200 hover:border-rose-300 transition-all"
        >
          <div class="flex items-center justify-between text-rose-800">
            <span class="text-[10px] uppercase font-extrabold tracking-wider">Nợ cần trả</span>
            <span class="material-symbols-rounded text-base text-rose-600">receipt_long</span>
          </div>
          <div class="text-base font-black font-mono text-rose-600 mt-0.5 truncate">-{{ formatVND(totalUnpaidDebt) }}</div>
        </div>
      </div>
    </aside>

    <!-- ==================== MOBILE TOP APP BAR (STICKY) ==================== -->
    <header class="md:hidden sticky top-0 z-30 bg-white/95 backdrop-blur-md border-b border-slate-200 px-3.5 py-2.5 pt-[max(env(safe-area-inset-top),0.65rem)] flex items-center justify-between shadow-sm">
      <div class="flex items-center gap-2.5 min-w-0">
        <div class="w-8 h-8 rounded-xl bg-indigo-600 text-white flex items-center justify-center shrink-0 shadow-sm">
          <span class="material-symbols-rounded text-lg">psychology</span>
        </div>
        <div class="min-w-0">
          <span class="font-extrabold text-xs text-slate-900 block leading-tight truncate">
            {{ currentTabTitle }}
          </span>
          <span class="text-[10px] text-slate-500 block truncate">@{{ currentUser.username }}</span>
        </div>
      </div>

      <div class="flex items-center gap-1.5 shrink-0">
        <!-- Quick Balance Pill on Mobile -->
        <button
          @click="activeTab = 'finance'"
          class="px-2.5 py-1 rounded-full bg-emerald-50 border border-emerald-200 text-[11px] font-black font-mono text-emerald-700"
        >
          {{ formatShortK(totalNetWorth) }}
        </button>

        <button
          @click="toggleNotificationSubscription"
          class="w-8 h-8 rounded-xl flex items-center justify-center border transition-colors"
          :class="isNotificationEnabled ? 'text-emerald-600 bg-emerald-50 border-emerald-200' : 'bg-slate-50 border-slate-200 text-slate-500'"
        >
          <span class="material-symbols-rounded text-base">{{ isNotificationEnabled ? 'notifications_active' : 'notifications' }}</span>
        </button>

        <button
          @click="showChangePasswordModal = true"
          class="w-8 h-8 rounded-xl bg-slate-50 border border-slate-200 text-slate-500 flex items-center justify-center"
        >
          <span class="material-symbols-rounded text-base">key</span>
        </button>

        <button
          @click="handleLogout"
          class="w-8 h-8 rounded-xl bg-slate-50 border border-slate-200 text-slate-500 hover:text-rose-600 flex items-center justify-center"
        >
          <span class="material-symbols-rounded text-base">logout</span>
        </button>
      </div>
    </header>

    <!-- ==================== MAIN CONTENT AREA ==================== -->
    <main class="flex-1 px-3 py-4 sm:px-6 sm:py-6 pb-24 md:pb-8 max-w-7xl mx-auto w-full overflow-x-hidden">
      <!-- VIEW TAB 1: DAILY HUB (TỔNG QUAN) -->
      <div v-show="activeTab === 'hub'">
        <DailyHub
          :user="currentUser"
          :wallets="wallets"
          :debts="debts"
          :events="events"
          :tasks="tasks"
          :notes="notes"
          @switch-tab="activeTab = $event"
          @toggle-task="toggleTask"
        />
      </div>

      <!-- VIEW TAB 2: TIỀN NỢ & DÒNG TIỀN THEO MỐC (DEBTS) -->
      <div v-show="activeTab === 'debts'">
        <DebtPlannerModule @debts-updated="syncDebtsFromStorage" />
      </div>

      <!-- VIEW TAB 3: VÍ TIỀN (FINANCE) -->
      <div v-show="activeTab === 'finance'">
        <FinanceDashboard @wallets-updated="syncWalletsFromStorage" />
      </div>

      <!-- VIEW TAB 4: LỊCH BIỂU (CALENDAR) -->
      <div v-show="activeTab === 'calendar'">
        <CalendarModule
          :events="events"
          @refresh-events="fetchEvents"
        />
      </div>

      <!-- VIEW TAB 5: CÔNG VIỆC (TASKS & DEADLINES) -->
      <div v-show="activeTab === 'tasks'">
        <TasksModule
          :tasks="tasks"
          @refresh-tasks="fetchTasks"
          @toggle-task="toggleTask"
        />
      </div>

      <!-- VIEW TAB 6: GHI CHÚ (NOTES) -->
      <div v-show="activeTab === 'notes'">
        <NotesModule
          :notes="notes"
          @refresh-notes="fetchNotes"
        />
      </div>
    </main>

    <!-- ==================== MOBILE BOTTOM NAVIGATION BAR (6 TABS COMPACT) ==================== -->
    <nav class="md:hidden fixed bottom-0 left-0 right-0 z-40 bg-white/95 border-t border-slate-200 backdrop-blur-lg px-1 pt-1.5 pb-[max(env(safe-area-inset-bottom),0.4rem)] grid grid-cols-6 gap-0.5 shadow-[0_-2px_10px_rgba(15,23,42,0.05)]">
      <button
        v-for="item in navItems"
        :key="item.id"
        @click="activeTab = item.id"
        class="relative flex flex-col items-center justify-center py-1 rounded-xl transition-all"
        :class="activeTab === item.id ? 'text-indigo-600 font-extrabold' : 'text-slate-500 font-medium'"
      >
        <div
          class="px-2.5 py-0.5 rounded-full flex items-center justify-center transition-all relative"
          :class="activeTab === item.id ? 'bg-indigo-50 text-indigo-600' : ''"
        >
          <span class="material-symbols-rounded text-[20px] leading-none">{{ item.iconSymbol }}</span>
          <span
            v-if="item.badge && item.badge > 0"
            class="absolute -top-1 -right-1 min-w-[15px] h-[15px] px-1 rounded-full text-[9px] font-black flex items-center justify-center bg-rose-600 text-white"
          >
            {{ item.badge > 9 ? '9+' : item.badge }}
          </span>
        </div>
        <span class="text-[10px] tracking-tight mt-0.5 truncate max-w-full px-0.5">{{ item.mobileLabel }}</span>
      </button>
    </nav>

    <!-- Change Password Modal -->
    <ChangePasswordModal
      :is-open="showChangePasswordModal"
      @close="showChangePasswordModal = false"
    />

    <!-- AI Assistant Floating Widget -->
    <AiChatWidget @data-updated="handleAiDataUpdated" />
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import DailyHub from './components/DailyHub.vue';
import FinanceDashboard from './components/FinanceDashboard.vue';
import DebtPlannerModule from './components/DebtPlannerModule.vue';
import CalendarModule from './components/CalendarModule.vue';
import TasksModule from './components/TasksModule.vue';
import NotesModule from './components/NotesModule.vue';
import AuthView from './components/AuthView.vue';
import ChangePasswordModal from './components/ChangePasswordModal.vue';
import AiChatWidget from './components/AiChatWidget.vue';
import { apiFetch, getStoredUser, setStoredUser } from './services/api';
import { registerPushNotification } from './services/pushNotification';

const currentUser = ref(getStoredUser());
const showChangePasswordModal = ref(false);
const isNotificationEnabled = ref(false);
const activeTab = ref('hub');

const loadJson = (key, fallback = []) => {
  try {
    const raw = localStorage.getItem(key);
    return raw ? JSON.parse(raw) : fallback;
  } catch (e) {
    return fallback;
  }
};

const saveJson = (key, val) => {
  try {
    localStorage.setItem(key, JSON.stringify(val));
  } catch (e) {}
};

const wallets = ref(loadJson('second_brain_wallets_v1', []));
const debts = ref(loadJson('second_brain_debts_v1', []));
const events = ref(loadJson('second_brain_events_v1', []));
const tasks = ref(loadJson('second_brain_tasks_v1', []));
const notes = ref(loadJson('second_brain_notes_v1', []));

const totalNetWorth = computed(() => {
  return wallets.value.reduce((sum, w) => sum + (Number(w.balance) || 0), 0);
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
  { id: 'hub', label: 'Tổng quan Hôm nay', mobileLabel: 'Hôm nay', iconSymbol: 'dashboard', badge: 0 },
  {
    id: 'debts',
    label: 'Tiền nợ & Mốc trả',
    mobileLabel: 'Tiền nợ',
    iconSymbol: 'receipt_long',
    badge: unpaidDebtCount.value,
    badgeClass: 'bg-rose-100 text-rose-700'
  },
  { id: 'finance', label: 'Ví tiền & Thu chi', mobileLabel: 'Ví tiền', iconSymbol: 'account_balance_wallet', badge: 0 },
  { id: 'calendar', label: 'Lịch biểu & Ca', mobileLabel: 'Lịch', iconSymbol: 'calendar_month', badge: 0 },
  {
    id: 'tasks',
    label: 'Công việc & Hạn',
    mobileLabel: 'Việc',
    iconSymbol: 'check_circle',
    badge: pendingTaskCount.value,
    badgeClass: 'bg-amber-100 text-amber-800'
  },
  { id: 'notes', label: 'Sổ Ghi chú', mobileLabel: 'Ghi chú', iconSymbol: 'edit_note', badge: 0 },
]);

const currentTabTitle = computed(() => {
  const found = navItems.value.find(x => x.id === activeTab.value);
  return found ? found.label : 'Second Brain';
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
  debts.value = loadJson('second_brain_debts_v1', []);
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
  } catch (e) {}
  isNotificationEnabled.value = false;
};

const toggleNotificationSubscription = async () => {
  if ('Notification' in window && Notification.permission === 'granted') {
    try {
      if ('serviceWorker' in navigator && navigator.serviceWorker.controller) {
        navigator.serviceWorker.controller.postMessage({
          type: 'LOCAL_NOTIFICATION',
          title: '🔔 Second Brain',
          body: 'Thông báo thiết bị đã sẵn sàng!'
        });
      }
    } catch (e) {}
  }

  const res = await registerPushNotification();
  if (res.success) {
    isNotificationEnabled.value = true;
    try {
      const testRes = await apiFetch('/notification/test-push', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' }
      });
      if (testRes.ok) {
        alert('🔔 Đã bật thông báo nhắc lịch trên điện thoại / trình duyệt thành công!');
      } else {
        alert('Đã lưu thiết bị nhận thông báo thành công!');
      }
    } catch (e) {
      alert('Đã bật thông báo trên thiết bị thành công!');
    }
  } else if (res.reason === 'denied') {
    alert('⚠️ Bạn đang chặn quyền thông báo. Hãy mở cài đặt trình duyệt và cho phép thông báo.');
  } else {
    alert('⚠️ Không thể kết nối dịch vụ thông báo lúc này.');
  }
};

// Auth Handlers
const handleAuthSuccess = (user) => {
  currentUser.value = user;
  fetchAllData();
};

const handleLogout = () => {
  if (confirm('Bạn có chắc muốn đăng xuất khỏi Second Brain?')) {
    setStoredUser(null);
    currentUser.value = null;
  }
};

// API Calls with LocalStorage sync
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
  } catch (err) {}
};

const fetchDebts = async () => {
  syncDebtsFromStorage();
  try {
    const res = await apiFetch('/debts');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        debts.value = data;
        saveJson('second_brain_debts_v1', data);
      }
    }
  } catch (err) {}
};

const fetchEvents = async () => {
  events.value = loadJson('second_brain_events_v1', events.value);
  try {
    const res = await apiFetch('/events');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        events.value = data;
        saveJson('second_brain_events_v1', data);
      }
    }
  } catch (err) {}
};

const fetchTasks = async () => {
  tasks.value = loadJson('second_brain_tasks_v1', tasks.value);
  try {
    const res = await apiFetch('/tasks');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        tasks.value = data;
        saveJson('second_brain_tasks_v1', data);
      }
    }
  } catch (err) {}
};

const toggleTask = async (id) => {
  const t = tasks.value.find(x => x.id === id);
  if (t) {
    t.isCompleted = !t.isCompleted;
    saveJson('second_brain_tasks_v1', tasks.value);
  }
  try {
    await apiFetch(`/tasks/${id}/toggle`, { method: 'PATCH' });
  } catch (err) {}
};

const fetchNotes = async () => {
  notes.value = loadJson('second_brain_notes_v1', notes.value);
  try {
    const res = await apiFetch('/notes');
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && data.length > 0) {
        notes.value = data;
        saveJson('second_brain_notes_v1', data);
      }
    }
  } catch (err) {}
};

const handleAiDataUpdated = (event) => {
  if (event?.action === 'transaction') {
    fetchWallets();
  } else if (event?.action === 'event') {
    fetchEvents();
  } else if (event?.action === 'task') {
    fetchTasks();
  } else {
    fetchAllData();
  }
};

const fetchAllData = () => {
  if (currentUser.value) {
    fetchWallets();
    fetchDebts();
    fetchEvents();
    fetchTasks();
    fetchNotes();
  }
};

const handleGlobalStorageSync = () => {
  syncWalletsFromStorage();
  syncDebtsFromStorage();
  events.value = loadJson('second_brain_events_v1', events.value);
  tasks.value = loadJson('second_brain_tasks_v1', tasks.value);
  notes.value = loadJson('second_brain_notes_v1', notes.value);
};

onMounted(() => {
  fetchAllData();
  checkNotificationStatus();
  window.addEventListener('secondbrain-data-updated', handleGlobalStorageSync);
});

onUnmounted(() => {
  window.removeEventListener('secondbrain-data-updated', handleGlobalStorageSync);
});
</script>
