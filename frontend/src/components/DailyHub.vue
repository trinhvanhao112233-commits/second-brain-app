<template>
  <div class="space-y-5">
    <!-- ==================== 1. HERO WELCOME & FINANCIAL OVERVIEW ==================== -->
    <header class="card-luxury p-5 sm:p-6 flex flex-col lg:flex-row lg:items-center justify-between gap-4 relative overflow-hidden">
      <div class="space-y-1.5 relative z-10">
        <div class="flex items-center gap-2 flex-wrap">
          <span class="px-2.5 py-0.5 rounded-full text-[11px] font-bold uppercase tracking-wider bg-indigo-50 text-indigo-700 border border-indigo-200/80">
            {{ greeting }}
          </span>
          <span class="text-xs text-slate-500 capitalize font-semibold">{{ currentDateStr }}</span>
        </div>
        <h1 class="text-xl sm:text-2xl font-extrabold text-slate-900 tracking-tight">
          {{ user ? `Xin chào, ${user.fullName || user.username}` : 'Trung Tâm Điều Hành Hôm Nay' }}
        </h1>
        <p class="text-xs text-slate-500">
          Theo dõi dòng tiền, các mốc trả nợ 1-10 • 10-21 • 21-31, lịch tuần và công việc đồng bộ tức thì giữa máy tính & điện thoại
        </p>
      </div>

      <!-- Right: Live Clock + Quick Sync -->
      <div class="flex items-center gap-2.5 self-start lg:self-auto relative z-10">
        <button
          @click="$emit('refresh-all')"
          class="px-3.5 py-2.5 rounded-2xl bg-indigo-50 hover:bg-indigo-100 text-indigo-700 border border-indigo-200/80 text-xs font-extrabold flex items-center gap-1.5 transition-all"
          title="Đồng bộ dữ liệu ngay lập tức"
        >
          <span class="material-symbols-rounded text-base">cloud_sync</span>
          <span>Đồng bộ</span>
        </button>

        <div class="bg-slate-900 text-white px-4 py-2.5 rounded-2xl flex items-center gap-3 shadow-sm">
          <div class="w-8 h-8 rounded-xl bg-indigo-500/20 border border-indigo-400/30 text-indigo-300 flex items-center justify-center shrink-0">
            <span class="material-symbols-rounded text-lg">schedule</span>
          </div>
          <div>
            <span class="font-mono text-lg sm:text-xl font-black tracking-wider block leading-none">
              {{ currentTimeStr }}
            </span>
            <span class="text-[9px] text-slate-400 uppercase tracking-wider font-bold mt-0.5 block">Giờ hiện tại</span>
          </div>
        </div>
      </div>
    </header>

    <!-- ==================== 2. 4 KPI CARDS (2x2 TRÊN ĐIỆN THOẠI, 4 CỘT TRÊN PC) ==================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
      <!-- 1. Ví tiền & Định mức ngày -->
      <div
        @click="$emit('switch-tab', 'finance')"
        class="card-luxury cursor-pointer group p-4 sm:p-5 hover:border-emerald-400 transition-all flex flex-col justify-between"
      >
        <div>
          <div class="flex items-center justify-between mb-1.5">
            <span class="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Số Dư Ví</span>
            <div class="w-8 h-8 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <span class="material-symbols-rounded text-lg">account_balance_wallet</span>
            </div>
          </div>
          <div class="text-lg sm:text-2xl font-black font-mono text-emerald-600 truncate">
            {{ formatVND(totalNetWorth) }}
          </div>
        </div>
        <div class="text-[11px] text-slate-500 mt-3 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span class="truncate">An toàn: <strong class="text-slate-700">{{ formatShortK(dailySafeSpend) }}/ngày</strong></span>
          <span class="material-symbols-rounded text-sm text-emerald-600 shrink-0">arrow_forward</span>
        </div>
      </div>

      <!-- 2. Tiền nợ & Mốc trả -->
      <div
        @click="$emit('switch-tab', 'debts')"
        class="card-luxury cursor-pointer group p-4 sm:p-5 hover:border-rose-400 transition-all flex flex-col justify-between"
      >
        <div>
          <div class="flex items-center justify-between mb-1.5">
            <span class="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Nợ Phải Trả</span>
            <div class="w-8 h-8 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center">
              <span class="material-symbols-rounded text-lg">receipt_long</span>
            </div>
          </div>
          <div class="text-lg sm:text-2xl font-black font-mono text-rose-600 truncate">
            {{ unpaidDebts.length > 0 ? '-' + formatVND(totalUnpaidDebt) : '0 ₫' }}
          </div>
        </div>
        <div class="text-[11px] text-slate-500 mt-3 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span class="truncate">{{ unpaidDebts.length }} khoản chưa tất toán</span>
          <span class="material-symbols-rounded text-sm text-rose-600 shrink-0">arrow_forward</span>
        </div>
      </div>

      <!-- 3. Việc cần làm -->
      <div
        @click="$emit('switch-tab', 'tasks')"
        class="card-luxury cursor-pointer group p-4 sm:p-5 hover:border-amber-400 transition-all flex flex-col justify-between"
      >
        <div>
          <div class="flex items-center justify-between mb-1.5">
            <span class="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Việc Cần Làm</span>
            <div class="w-8 h-8 rounded-xl bg-amber-50 text-amber-600 flex items-center justify-center">
              <span class="material-symbols-rounded text-lg">checklist</span>
            </div>
          </div>
          <div class="text-lg sm:text-2xl font-black font-mono text-slate-900">
            {{ pendingTasks.length }}
            <span class="text-xs font-sans font-medium text-slate-500">việc</span>
          </div>
        </div>
        <div class="text-[11px] text-slate-500 mt-3 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span class="truncate">{{ completedTasksCount }} việc đã xong</span>
          <span class="material-symbols-rounded text-sm text-amber-600 shrink-0">arrow_forward</span>
        </div>
      </div>

      <!-- 4. Lịch hôm nay -->
      <div
        @click="$emit('switch-tab', 'calendar')"
        class="card-luxury cursor-pointer group p-4 sm:p-5 hover:border-indigo-400 transition-all flex flex-col justify-between"
      >
        <div>
          <div class="flex items-center justify-between mb-1.5">
            <span class="text-[11px] font-extrabold text-slate-500 uppercase tracking-wider">Lịch Hôm Nay</span>
            <div class="w-8 h-8 rounded-xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
              <span class="material-symbols-rounded text-lg">calendar_month</span>
            </div>
          </div>
          <div class="text-lg sm:text-2xl font-black font-mono text-indigo-600">
            {{ todayEvents.length }}
            <span class="text-xs font-sans font-medium text-slate-500">ca / lịch</span>
          </div>
        </div>
        <div class="text-[11px] text-slate-500 mt-3 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span class="truncate">Xem thời khóa biểu</span>
          <span class="material-symbols-rounded text-sm text-indigo-600 shrink-0">arrow_forward</span>
        </div>
      </div>
    </div>

    <!-- ==================== 3. ƯU TIÊN TRẢ NỢ TIẾP THEO ==================== -->
    <section
      v-if="topPriorityDebts.length > 0"
      class="card-luxury border-rose-200 p-4 sm:p-5 space-y-3"
    >
      <div class="flex items-center justify-between pb-2.5 border-b border-slate-100">
        <div class="flex items-center gap-2">
          <span class="w-2.5 h-2.5 rounded-full bg-rose-600 animate-pulse"></span>
          <h3 class="text-xs sm:text-sm font-extrabold uppercase tracking-wider text-slate-900">
            Khoản Nợ Ưu Tiên Cần Trả Sớm
          </h3>
        </div>
        <button
          @click="$emit('switch-tab', 'debts')"
          class="text-xs font-bold text-rose-600 hover:underline flex items-center gap-1"
        >
          <span>Quản lý mốc nợ</span>
          <span class="material-symbols-rounded text-xs">arrow_forward</span>
        </button>
      </div>

      <div class="grid grid-cols-1 md:grid-cols-3 gap-3">
        <div
          v-for="(d, idx) in topPriorityDebts"
          :key="d.id"
          @click="$emit('switch-tab', 'debts')"
          class="cursor-pointer p-3.5 rounded-xl bg-rose-50/40 hover:bg-rose-50 border border-rose-200/80 flex items-center justify-between gap-2 transition-colors"
        >
          <div class="min-w-0">
            <div class="flex items-center gap-1.5">
              <span class="px-1.5 py-0.5 rounded text-[10px] font-black bg-rose-600 text-white">#{{ idx + 1 }}</span>
              <span class="font-bold text-xs sm:text-sm text-slate-900 truncate">{{ d.title }}</span>
            </div>
            <div class="text-[11px] text-slate-500 font-mono mt-1">
              Mốc {{ d.periodLabel }} • Hạn {{ formatDateShort(d.dueDate) }}
            </div>
          </div>
          <div class="text-right shrink-0">
            <span class="font-mono font-black text-sm text-rose-600 block">
              -{{ formatShortK((Number(d.totalAmount) || 0) - (Number(d.paidAmount) || 0)) }}
            </span>
            <span class="text-[10px] font-semibold text-slate-500">{{ formatVND((Number(d.totalAmount) || 0) - (Number(d.paidAmount) || 0)) }}</span>
          </div>
        </div>
      </div>
    </section>

    <!-- ==================== 4. CỘT ĐÔI: LỊCH HÔM NAY & NHIỆM VỤ ƯU TIÊN ==================== -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-5">
      <!-- Cột Trái: Lịch trình hôm nay -->
      <section class="card-luxury p-4 sm:p-5 flex flex-col justify-between">
        <div>
          <div class="flex items-center justify-between pb-3 mb-3 border-b border-slate-100">
            <div class="flex items-center gap-2">
              <span class="material-symbols-rounded text-lg text-indigo-600">event</span>
              <h3 class="text-xs sm:text-sm font-extrabold uppercase tracking-wider text-slate-900">Lịch Trình Hôm Nay</h3>
            </div>
            <button
              @click="$emit('switch-tab', 'calendar')"
              class="text-xs font-bold text-indigo-600 hover:underline flex items-center gap-1"
            >
              <span>Mở lịch</span>
              <span class="material-symbols-rounded text-xs">arrow_forward</span>
            </button>
          </div>

          <div v-if="todayEvents.length === 0" class="py-10 text-center text-slate-400 text-xs">
            <span class="material-symbols-rounded text-3xl mb-1 text-slate-300">event_available</span>
            <p>Hôm nay chưa có lịch hẹn nào.</p>
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="evt in todayEvents"
              :key="evt.id"
              class="p-3 rounded-xl bg-slate-50 border border-slate-200 flex items-center justify-between gap-2"
              :style="{ borderLeft: `4px solid ${evt.color || '#4f46e5'}` }"
            >
              <div class="min-w-0">
                <h4 class="font-bold text-slate-900 text-xs sm:text-sm truncate">{{ evt.title }}</h4>
                <p class="text-[11px] font-mono text-slate-500 mt-0.5">
                  {{ formatTime(evt.startTime) }} - {{ formatTime(evt.endTime) }}
                  <span v-if="evt.description"> • {{ evt.description }}</span>
                </p>
              </div>
              <span class="text-[10px] px-2.5 py-0.5 rounded-full bg-white border border-slate-200 text-slate-700 font-bold shrink-0">
                {{ evt.category || 'Lịch' }}
              </span>
            </div>
          </div>
        </div>
      </section>

      <!-- Cột Phải: Công việc cần làm -->
      <section class="card-luxury p-4 sm:p-5 flex flex-col justify-between">
        <div>
          <div class="flex items-center justify-between pb-3 mb-3 border-b border-slate-100">
            <div class="flex items-center gap-2">
              <span class="material-symbols-rounded text-lg text-amber-600">task_alt</span>
              <h3 class="text-xs sm:text-sm font-extrabold uppercase tracking-wider text-slate-900">Việc Cần Hoàn Thành</h3>
            </div>
            <button
              @click="$emit('switch-tab', 'tasks')"
              class="text-xs font-bold text-indigo-600 hover:underline flex items-center gap-1"
            >
              <span>Tất cả việc</span>
              <span class="material-symbols-rounded text-xs">arrow_forward</span>
            </button>
          </div>

          <div v-if="pendingTasks.length === 0" class="py-10 text-center text-slate-400 text-xs">
            <span class="material-symbols-rounded text-3xl mb-1 text-emerald-500">check_circle</span>
            <p>Tuyệt vời! Bạn đã hoàn thành hết các đầu việc.</p>
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="task in pendingTasks.slice(0, 6)"
              :key="task.id"
              @click="$emit('toggle-task', task.id)"
              class="cursor-pointer group p-3 rounded-xl bg-slate-50 hover:bg-slate-100 border border-slate-200 flex items-center justify-between gap-2 transition-all"
            >
              <div class="flex items-center gap-2.5 min-w-0">
                <div class="w-5 h-5 rounded-md border-2 border-slate-300 group-hover:border-emerald-600 flex items-center justify-center transition-colors shrink-0 bg-white">
                  <span v-if="task.isCompleted" class="material-symbols-rounded text-xs text-emerald-600 font-bold">check</span>
                </div>
                <span class="text-xs sm:text-sm font-bold text-slate-800 truncate">{{ task.title }}</span>
              </div>
              <span v-if="task.dueDate" class="text-[10px] font-mono font-bold text-slate-500 shrink-0">
                {{ formatDateShort(task.dueDate) }}
              </span>
            </div>
          </div>
        </div>
      </section>
    </div>
  </div>
</template>

<script setup>
import { computed, ref, onMounted, onUnmounted } from 'vue';

const props = defineProps({
  user: { type: Object, default: () => null },
  wallets: { type: Array, default: () => [] },
  debts: { type: Array, default: () => [] },
  events: { type: Array, default: () => [] },
  tasks: { type: Array, default: () => [] },
  notes: { type: Array, default: () => [] },
});

defineEmits(['switch-tab', 'toggle-task', 'refresh-all']);

const now = ref(new Date());
let timer = null;

onMounted(() => {
  timer = setInterval(() => {
    now.value = new Date();
  }, 1000);
});

onUnmounted(() => {
  if (timer) clearInterval(timer);
});

const currentTimeStr = computed(() => {
  return now.value.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit', hour12: false });
});

const currentDateStr = computed(() => {
  return now.value.toLocaleDateString('vi-VN', { weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' });
});

const greeting = computed(() => {
  const hr = now.value.getHours();
  if (hr < 12) return 'Chào buổi sáng';
  if (hr < 18) return 'Chào buổi chiều';
  return 'Chào buổi tối';
});

const totalNetWorth = computed(() => {
  return props.wallets.reduce((sum, w) => sum + (Number(w.balance) || 0), 0);
});

const dailySafeSpend = computed(() => {
  const d = now.value;
  const daysInMonth = new Date(d.getFullYear(), d.getMonth() + 1, 0).getDate();
  const remainingDays = Math.max(1, daysInMonth - d.getDate() + 1);
  return totalNetWorth.value > 0 ? Math.floor(totalNetWorth.value / remainingDays) : 0;
});

const unpaidDebts = computed(() => {
  return props.debts.filter(d => d.recordType === 'DEBT' && !d.isCompleted);
});

const totalUnpaidDebt = computed(() => {
  return unpaidDebts.value.reduce((sum, d) => sum + Math.max(0, (Number(d.totalAmount) || 0) - (Number(d.paidAmount) || 0)), 0);
});

const topPriorityDebts = computed(() => {
  return [...unpaidDebts.value]
    .sort((a, b) => {
      if (a.priorityLevel !== b.priorityLevel) return a.priorityLevel - b.priorityLevel;
      return new Date(a.dueDate) - new Date(b.dueDate);
    })
    .slice(0, 3);
});

const formatVND = (val) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(val || 0);
};

const formatShortK = (num) => {
  const val = Number(num) || 0;
  return `${Math.round(val / 1000).toLocaleString('vi-VN')}k`;
};

const formatDateShort = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return `${d.getDate()}/${d.getMonth() + 1}`;
};

const pendingTasks = computed(() => props.tasks.filter(t => !t.isCompleted));
const completedTasksCount = computed(() => props.tasks.filter(t => t.isCompleted).length);

const todayDateKey = computed(() => {
  const d = now.value;
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
});

const todayEvents = computed(() => {
  return props.events
    .filter(e => e.startTime && e.startTime.startsWith(todayDateKey.value))
    .sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
});

const formatTime = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
};
</script>
