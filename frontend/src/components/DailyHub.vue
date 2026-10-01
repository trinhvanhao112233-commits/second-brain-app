<template>
  <div class="space-y-6">
    <!-- Header Welcome & Live Clock (Material Design 3 Surface) -->
    <header class="bg-[#171c24] border border-[#262a34] rounded-2xl p-5 sm:p-6 md-elevation-1 flex flex-col md:flex-row md:items-center justify-between gap-4">
      <div class="space-y-1">
        <div class="flex items-center gap-2">
          <span class="px-2.5 py-0.5 rounded-full text-[11px] font-bold tracking-wider uppercase bg-[#005237] text-[#5dfec1] border border-[#38e1a6]/30">
            {{ greeting }}
          </span>
          <span class="text-xs text-[#8b9198] capitalize">{{ currentDateStr }}</span>
        </div>
        <h1 class="text-2xl sm:text-3xl font-extrabold text-white tracking-tight">
          {{ user ? `Chào bạn, ${user.fullName || user.username}` : 'Bảng Điều Khiển Hôm Nay' }}
        </h1>
        <p class="text-xs text-[#8b9198]">Trung tâm điều phối công việc, tài chính và lịch trình</p>
      </div>

      <!-- Live Digital Clock -->
      <div class="bg-[#1c2029] border border-[#262a34] px-5 py-3 rounded-2xl md-elevation-1 flex items-center gap-3">
        <div class="w-10 h-10 rounded-xl bg-[#005237]/40 text-[#5dfec1] flex items-center justify-center">
          <span class="material-symbols-rounded text-2xl">schedule</span>
        </div>
        <div>
          <span class="font-mono text-2xl sm:text-3xl font-bold text-white tracking-wider block leading-none">
            {{ currentTimeStr }}
          </span>
          <span class="text-[10px] text-[#8b9198] uppercase tracking-wider font-semibold mt-0.5 block">Giờ địa phương</span>
        </div>
      </div>
    </header>

    <!-- Quick Stats Grid (MD3 Cards) -->
    <div class="grid grid-cols-1 sm:grid-cols-3 gap-4">
      <!-- 1. Finance Card Summary -->
      <div
        @click="$emit('switch-tab', 'finance')"
        class="cursor-pointer group p-5 rounded-2xl bg-[#171c24] border border-[#262a34] hover:border-[#38e1a6]/60 md-elevation-1 hover:md-elevation-2 transition-all duration-200 flex flex-col justify-between"
      >
        <div>
          <div class="flex items-center justify-between mb-2">
            <span class="text-[11px] font-bold text-[#8b9198] uppercase tracking-wider">Tổng Tài Sản</span>
            <div class="w-9 h-9 rounded-xl bg-[#005237] text-[#5dfec1] flex items-center justify-center group-hover:scale-105 transition-transform">
              <span class="material-symbols-rounded text-lg">account_balance_wallet</span>
            </div>
          </div>
          <div class="text-2xl font-black font-mono text-[#38e1a6]">{{ formatVND(totalNetWorth) }}</div>
        </div>
        <div class="text-xs text-[#8b9198] mt-3 pt-2.5 border-t border-[#262a34] flex items-center justify-between">
          <span>{{ walletsCount }} ví hoạt động</span>
          <span class="material-symbols-rounded text-sm text-[#38e1a6]">arrow_forward</span>
        </div>
      </div>

      <!-- 2. Tasks Summary -->
      <div
        @click="$emit('switch-tab', 'tasks')"
        class="cursor-pointer group p-5 rounded-2xl bg-[#171c24] border border-[#262a34] hover:border-[#5dfec1]/60 md-elevation-1 hover:md-elevation-2 transition-all duration-200 flex flex-col justify-between"
      >
        <div>
          <div class="flex items-center justify-between mb-2">
            <span class="text-[11px] font-bold text-[#8b9198] uppercase tracking-wider">Việc Cần Làm</span>
            <div class="w-9 h-9 rounded-xl bg-[#1c2029] text-[#cee9da] flex items-center justify-center group-hover:scale-105 transition-transform border border-[#262a34]">
              <span class="material-symbols-rounded text-lg">checklist</span>
            </div>
          </div>
          <div class="text-2xl font-black font-mono text-white">
            {{ pendingTasks.length }}
            <span class="text-xs font-normal text-[#8b9198]">việc chưa xong</span>
          </div>
        </div>
        <div class="text-xs text-[#8b9198] mt-3 pt-2.5 border-t border-[#262a34] flex items-center justify-between">
          <span>{{ completedTasksCount }} việc đã hoàn thành</span>
          <span class="material-symbols-rounded text-sm text-[#5dfec1]">arrow_forward</span>
        </div>
      </div>

      <!-- 3. Events Summary -->
      <div
        @click="$emit('switch-tab', 'calendar')"
        class="cursor-pointer group p-5 rounded-2xl bg-[#171c24] border border-[#262a34] hover:border-[#b2ccbe]/60 md-elevation-1 hover:md-elevation-2 transition-all duration-200 flex flex-col justify-between"
      >
        <div>
          <div class="flex items-center justify-between mb-2">
            <span class="text-[11px] font-bold text-[#8b9198] uppercase tracking-wider">Lịch Hôm Nay</span>
            <div class="w-9 h-9 rounded-2xl bg-[#1c2029] text-[#5dfec1] flex items-center justify-center group-hover:scale-105 transition-transform border border-[#262a34]">
              <span class="material-symbols-rounded text-lg">calendar_month</span>
            </div>
          </div>
          <div class="text-2xl font-black font-mono text-[#cee9da]">
            {{ todayEvents.length }}
            <span class="text-xs font-normal text-[#8b9198]">sự kiện</span>
          </div>
        </div>
        <div class="text-xs text-[#8b9198] mt-3 pt-2.5 border-t border-[#262a34] flex items-center justify-between">
          <span>Xem thời gian biểu chi tiết</span>
          <span class="material-symbols-rounded text-sm text-[#cee9da]">arrow_forward</span>
        </div>
      </div>
    </div>

    <!-- Main Content Dual Columns (MD3 Surface Containers) -->
    <div class="grid grid-cols-1 lg:grid-cols-2 gap-6">
      <!-- Cột Trái: Lịch trình hôm nay -->
      <section class="bg-[#171c24] border border-[#262a34] rounded-2xl p-5 md-elevation-1 flex flex-col justify-between">
        <div>
          <div class="flex items-center justify-between pb-3 mb-3 border-b border-[#262a34]">
            <div class="flex items-center gap-2">
              <span class="material-symbols-rounded text-base text-[#38e1a6]">event</span>
              <h3 class="text-sm font-bold uppercase tracking-wider text-white">Lịch Trình Hôm Nay</h3>
            </div>
            <button
              @click="$emit('switch-tab', 'calendar')"
              class="text-xs font-semibold text-[#5dfec1] hover:underline flex items-center gap-1"
            >
              <span>Xem lịch biểu</span>
              <span class="material-symbols-rounded text-xs">arrow_forward</span>
            </button>
          </div>

          <div v-if="todayEvents.length === 0" class="py-12 text-center text-[#8b9198] text-xs">
            <span class="material-symbols-rounded text-3xl mb-1 text-[#41474d]">free_cancellation</span>
            <p>Không có lịch hẹn nào trong hôm nay. Thư giãn thôi!</p>
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="evt in todayEvents"
              :key="evt.id"
              class="p-3 rounded-xl bg-[#1c2029] border border-[#262a34] flex items-center justify-between"
              :style="{ borderLeft: `4px solid ${evt.color || '#38e1a6'}` }"
            >
              <div class="min-w-0 pr-2">
                <h4 class="font-bold text-white text-xs truncate">{{ evt.title }}</h4>
                <p class="text-[11px] font-mono text-[#8b9198] mt-0.5">
                  {{ formatTime(evt.startTime) }} - {{ formatTime(evt.endTime) }}
                </p>
              </div>
              <span class="text-[10px] px-2 py-0.5 rounded-full bg-[#171c24] border border-[#262a34] text-[#c1c7ce] font-medium shrink-0">
                {{ evt.category || 'Khác' }}
              </span>
            </div>
          </div>
        </div>
      </section>

      <!-- Cột Phải: Nhiệm vụ ưu tiên & Deadline sắp tới -->
      <section class="bg-[#171c24] border border-[#262a34] rounded-2xl p-5 md-elevation-1 flex flex-col justify-between">
        <div>
          <div class="flex items-center justify-between pb-3 mb-3 border-b border-[#262a34]">
            <div class="flex items-center gap-2">
              <span class="material-symbols-rounded text-base text-[#f59e0b]">priority_high</span>
              <h3 class="text-sm font-bold uppercase tracking-wider text-white">Nhiệm Vụ Ưu Tiên</h3>
            </div>
            <button
              @click="$emit('switch-tab', 'tasks')"
              class="text-xs font-semibold text-[#5dfec1] hover:underline flex items-center gap-1"
            >
              <span>Quản lý việc</span>
              <span class="material-symbols-rounded text-xs">arrow_forward</span>
            </button>
          </div>

          <div v-if="pendingTasks.length === 0" class="py-12 text-center text-[#8b9198] text-xs">
            <span class="material-symbols-rounded text-3xl mb-1 text-[#38e1a6]">check_circle</span>
            <p>Tuyệt vời! Bạn đã hoàn thành hết các đầu việc ưu tiên.</p>
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="task in pendingTasks.slice(0, 5)"
              :key="task.id"
              @click="$emit('toggle-task', task.id)"
              class="cursor-pointer group p-3 rounded-xl bg-[#1c2029] border border-[#262a34] hover:border-[#41474d] flex items-center justify-between transition-all"
            >
              <div class="flex items-center gap-2.5 min-w-0 pr-2">
                <div class="w-5 h-5 rounded-md border-2 border-[#41474d] group-hover:border-[#38e1a6] flex items-center justify-center transition-colors shrink-0">
                  <span v-if="task.isCompleted" class="material-symbols-rounded text-xs text-[#38e1a6] font-bold">check</span>
                </div>
                <span class="text-xs font-semibold text-[#e1e2ec] truncate">{{ task.title }}</span>
              </div>
              <span
                class="text-[9px] px-2 py-0.5 rounded-md font-bold uppercase shrink-0"
                :class="{
                  'bg-[#ffb4ab]/15 text-[#ffb4ab] border border-[#ffb4ab]/30': task.priority === 'High',
                  'bg-amber-500/15 text-amber-300 border border-amber-500/30': task.priority === 'Medium',
                  'bg-[#171c24] text-[#8b9198] border border-[#262a34]': task.priority === 'Low',
                }"
              >
                {{ task.priority }}
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
  events: { type: Array, default: () => [] },
  tasks: { type: Array, default: () => [] },
});

defineEmits(['switch-tab', 'toggle-task']);

// Realtime Clock
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

// Finance Overview
const totalNetWorth = computed(() => {
  return props.wallets.reduce((sum, w) => sum + (Number(w.balance) || 0), 0);
});
const walletsCount = computed(() => props.wallets.length);

const formatVND = (val) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(val || 0);
};

// Tasks
const pendingTasks = computed(() => props.tasks.filter(t => !t.isCompleted));
const completedTasksCount = computed(() => props.tasks.filter(t => t.isCompleted).length);

// Today's Events
const todayDateKey = computed(() => now.value.toISOString().split('T')[0]);
const todayEvents = computed(() => {
  return props.events.filter(e => e.startTime && e.startTime.startsWith(todayDateKey.value));
});

const formatTime = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
};
</script>
