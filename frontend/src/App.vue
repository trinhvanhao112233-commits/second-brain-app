<template>
  <!-- If not logged in, render AuthView -->
  <AuthView v-if="!currentUser" @auth-success="handleAuthSuccess" />

  <div v-else class="min-h-screen bg-[#0f141c] text-[#e1e2ec] flex flex-col md:flex-row">
    <!-- MD3 NAVIGATION RAIL (DESKTOP) -->
    <aside class="hidden md:flex flex-col justify-between w-72 bg-[#171c24] border-r border-[#262a34] p-5 sticky top-0 h-screen z-30">
      <div class="space-y-6">
        <!-- MD3 Header & Brand -->
        <div class="flex items-center gap-3 px-2 pt-2">
          <div class="w-11 h-11 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center md-elevation-1">
            <span class="material-symbols-rounded text-2xl font-bold">psychology</span>
          </div>
          <div>
            <h1 class="font-bold text-base tracking-tight text-[#e1e2ec]">Second Brain</h1>
          </div>
        </div>

        <!-- User Profile Card -->
        <div class="p-3 bg-[#1c2029] border border-[#262a34] rounded-2xl flex items-center justify-between">
          <div class="flex items-center gap-2.5 overflow-hidden">
            <div class="w-8 h-8 rounded-xl bg-[#005237]/60 text-[#5dfec1] flex items-center justify-center font-bold text-xs uppercase flex-shrink-0">
              {{ currentUser.fullName ? currentUser.fullName.charAt(0) : currentUser.username.charAt(0) }}
            </div>
            <div class="overflow-hidden leading-tight">
              <p class="text-xs font-bold text-white truncate">{{ currentUser.fullName || currentUser.username }}</p>
              <p class="text-[10px] text-[#8b9198] truncate">@{{ currentUser.username }}</p>
            </div>
          </div>
          <div class="flex items-center gap-1">
            <button
              @click="showChangePasswordModal = true"
              title="Đổi mật khẩu"
              class="w-7 h-7 rounded-lg text-[#8b9198] hover:text-[#5dfec1] hover:bg-[#262a34] flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-base">key</span>
            </button>
            <button
              @click="handleLogout"
              title="Đăng xuất"
              class="w-7 h-7 rounded-lg text-[#8b9198] hover:text-[#f43f5e] hover:bg-[#262a34] flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-base">logout</span>
            </button>
          </div>
        </div>

        <!-- MD3 Navigation Items -->
        <nav class="space-y-1.5 pt-1">
          <button
            v-for="item in navItems"
            :key="item.id"
            @click="activeTab = item.id"
            class="w-full flex items-center gap-3.5 px-4 py-3 rounded-full text-xs font-semibold transition-all duration-200"
            :class="activeTab === item.id 
              ? 'bg-[#005237] text-[#5dfec1] md-elevation-1 font-bold' 
              : 'text-[#c1c7ce] hover:bg-[#262a34] hover:text-white'"
          >
            <span class="material-symbols-rounded text-xl">{{ item.iconSymbol }}</span>
            <span class="tracking-wide">{{ item.label }}</span>
          </button>
        </nav>
      </div>

      <!-- MD3 Surface Card: Net Worth Mini-Widget -->
      <div class="p-4 rounded-3xl bg-[#1c2029] border border-[#262a34] md-elevation-1">
        <div class="flex items-center gap-2 text-[#8b9198]">
          <span class="material-symbols-rounded text-sm">savings</span>
          <span class="text-[11px] uppercase font-bold tracking-wider">Tổng tài sản ví</span>
        </div>
        <div class="text-lg font-black text-[#38e1a6] mt-1 truncate">{{ formatVND(totalNetWorth) }}</div>
      </div>
    </aside>

    <!-- MAIN CONTENT AREA -->
    <main class="flex-1 px-4 py-6 sm:px-8 sm:py-8 pb-24 md:pb-8 pt-[env(safe-area-inset-top,1.5rem)] max-w-7xl mx-auto w-full">
      <!-- Top Mobile Bar -->
      <div class="md:hidden flex items-center justify-between mb-6 pb-4 border-b border-[#262a34]">
        <div class="flex items-center gap-2.5">
          <div class="w-9 h-9 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center md-elevation-1">
            <span class="material-symbols-rounded text-lg">psychology</span>
          </div>
          <div>
            <span class="font-bold text-sm text-[#e1e2ec] block leading-tight">Second Brain</span>
            <span class="text-[10px] text-[#8b9198]">@{{ currentUser.username }}</span>
          </div>
        </div>
        <div class="flex items-center gap-2">
          <button
            @click="showChangePasswordModal = true"
            title="Đổi mật khẩu"
            class="w-8 h-8 rounded-xl bg-[#1c2029] border border-[#262a34] text-[#8b9198] hover:text-[#5dfec1] flex items-center justify-center"
          >
            <span class="material-symbols-rounded text-base">key</span>
          </button>
          <button
            @click="handleLogout"
            title="Đăng xuất"
            class="w-8 h-8 rounded-xl bg-[#1c2029] border border-[#262a34] text-[#8b9198] hover:text-[#f43f5e] flex items-center justify-center"
          >
            <span class="material-symbols-rounded text-base">logout</span>
          </button>
          <div class="px-3 py-1 rounded-full bg-[#1c2029] border border-[#262a34]">
            <span class="text-xs font-bold text-[#38e1a6]">{{ formatVND(totalNetWorth) }}</span>
          </div>
        </div>
      </div>

      <!-- VIEW TAB 1: DAILY HUB (TỔNG QUAN) -->
      <div v-show="activeTab === 'hub'">
        <DailyHub
          :user="currentUser"
          :wallets="wallets"
          :events="events"
          :tasks="tasks"
          @switch-tab="activeTab = $event"
          @toggle-task="toggleTask"
        />
      </div>

      <!-- VIEW TAB 2: VÍ TIỀN (FINANCE) -->
      <div v-show="activeTab === 'finance'">
        <FinanceDashboard />
      </div>

      <!-- VIEW TAB 3: LỊCH BIỂU (CALENDAR) -->
      <div v-show="activeTab === 'calendar'">
        <CalendarModule
          :events="events"
          @refresh-events="fetchEvents"
        />
      </div>

      <!-- VIEW TAB 4: CÔNG VIỆC (TASKS & HABITS) -->
      <div v-show="activeTab === 'tasks'">
        <TasksModule
          :tasks="tasks"
          @refresh-tasks="fetchTasks"
          @toggle-task="toggleTask"
        />
      </div>

      <!-- VIEW TAB 5: GHI CHÚ (NOTES) -->
      <div v-show="activeTab === 'notes'">
        <NotesModule
          :notes="notes"
          @refresh-notes="fetchNotes"
        />
      </div>
    </main>

    <!-- MD3 MOBILE BOTTOM NAVIGATION BAR -->
    <nav class="md:hidden fixed bottom-0 left-0 right-0 z-50 bg-[#171c24]/95 border-t border-[#262a34] backdrop-blur-xl px-2 py-2 pb-[env(safe-area-inset-bottom,0.5rem)] flex items-center justify-around md-elevation-2">
      <button
        v-for="item in navItems"
        :key="item.id"
        @click="activeTab = item.id"
        class="flex flex-col items-center gap-1 px-3 py-1.5 rounded-2xl transition-all duration-200"
        :class="activeTab === item.id 
          ? 'text-[#5dfec1] font-bold' 
          : 'text-[#8b9198] hover:text-[#c1c7ce]'"
      >
        <div 
          class="px-3.5 py-0.5 rounded-full flex items-center justify-center transition-all duration-200"
          :class="activeTab === item.id ? 'bg-[#005237]' : ''"
        >
          <span class="material-symbols-rounded text-xl leading-none">{{ item.iconSymbol }}</span>
        </div>
        <span class="text-[11px] tracking-tight">{{ item.label }}</span>
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
import { ref, computed, onMounted } from 'vue';
import DailyHub from './components/DailyHub.vue';
import FinanceDashboard from './components/FinanceDashboard.vue';
import CalendarModule from './components/CalendarModule.vue';
import TasksModule from './components/TasksModule.vue';
import NotesModule from './components/NotesModule.vue';
import AuthView from './components/AuthView.vue';
import ChangePasswordModal from './components/ChangePasswordModal.vue';
import AiChatWidget from './components/AiChatWidget.vue';
import { apiFetch, getStoredUser, setStoredUser } from './services/api';

const currentUser = ref(getStoredUser());
const showChangePasswordModal = ref(false);
const activeTab = ref('hub');

const navItems = [
  { id: 'hub', label: 'Hôm nay', iconSymbol: 'dashboard' },
  { id: 'calendar', label: 'Lịch biểu', iconSymbol: 'calendar_month' },
  { id: 'finance', label: 'Ví tiền', iconSymbol: 'account_balance_wallet' },
  { id: 'tasks', label: 'Công việc', iconSymbol: 'check_circle' },
  { id: 'notes', label: 'Ghi chú', iconSymbol: 'edit_note' },
];

const wallets = ref([]);
const events = ref([]);
const tasks = ref([]);
const notes = ref([]);

const totalNetWorth = computed(() => {
  return wallets.value.reduce((sum, w) => sum + (Number(w.balance) || 0), 0);
});

const formatVND = (val) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(val || 0);
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
    wallets.value = [];
    events.value = [];
    tasks.value = [];
    notes.value = [];
  }
};

// API Calls
const fetchWallets = async () => {
  try {
    const res = await apiFetch('/wallets');
    if (res.ok) wallets.value = await res.json();
  } catch (err) {
    console.error(err);
  }
};

const fetchEvents = async () => {
  try {
    const res = await apiFetch('/events');
    if (res.ok) events.value = await res.json();
  } catch (err) {
    console.error(err);
  }
};

const fetchTasks = async () => {
  try {
    const res = await apiFetch('/tasks');
    if (res.ok) tasks.value = await res.json();
  } catch (err) {
    console.error(err);
  }
};

const toggleTask = async (id) => {
  try {
    const res = await apiFetch(`/tasks/${id}/toggle`, { method: 'PATCH' });
    if (res.ok) {
      const updated = await res.json();
      const t = tasks.value.find(x => x.id === id);
      if (t) t.isCompleted = updated.isCompleted;
    }
  } catch (err) {
    console.error(err);
  }
};

const fetchNotes = async () => {
  try {
    const res = await apiFetch('/notes');
    if (res.ok) notes.value = await res.json();
  } catch (err) {
    console.error(err);
  }
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
    fetchEvents();
    fetchTasks();
    fetchNotes();
  }
};

onMounted(() => {
  fetchAllData();
});
</script>
