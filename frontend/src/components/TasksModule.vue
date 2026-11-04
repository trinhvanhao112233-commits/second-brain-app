<template>
  <div class="space-y-5">
    <!-- ==================== 1. HEADER BAR ==================== -->
    <header class="bg-white border border-slate-200 rounded-2xl px-5 py-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm">
      <div class="flex items-center gap-3.5">
        <div class="w-11 h-11 rounded-2xl bg-indigo-50 text-indigo-600 border border-indigo-100 flex items-center justify-center shrink-0">
          <span class="material-symbols-rounded text-2xl">checklist</span>
        </div>
        <div>
          <h2 class="text-lg font-extrabold text-slate-900 tracking-tight">Quản Lý Công Việc & Deadline</h2>
          <p class="text-xs text-slate-500 mt-0.5">Theo dõi công việc hàng ngày, bài tập và hạn chót đồ án</p>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="flex items-center gap-2">
        <div class="flex items-center bg-slate-100 border border-slate-200 rounded-xl p-1">
          <button
            type="button"
            @click="viewMode = 'grouped'"
            class="px-3 py-1.5 rounded-lg text-xs font-bold flex items-center gap-1.5 transition-all"
            :class="viewMode === 'grouped' ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
          >
            <span class="material-symbols-rounded text-base">view_agenda</span>
            <span class="hidden sm:inline">Theo Mốc</span>
          </button>
          <button
            type="button"
            @click="viewMode = 'flat'"
            class="px-3 py-1.5 rounded-lg text-xs font-bold flex items-center gap-1.5 transition-all"
            :class="viewMode === 'flat' ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
          >
            <span class="material-symbols-rounded text-base">format_list_bulleted</span>
            <span class="hidden sm:inline">Tất Cả ({{ localTasks.length }})</span>
          </button>
        </div>

        <button
          type="button"
          @click="openCreateModal()"
          class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold transition-all shadow-sm active:scale-95"
        >
          <span class="material-symbols-rounded text-base">add</span>
          <span>+ Tạo Việc</span>
        </button>
      </div>
    </header>

    <!-- ==================== 2. THANH THÊM VIỆC NHANH 1 CHẠM ==================== -->
    <section class="bg-white border border-slate-200 rounded-2xl p-4 sm:p-5 shadow-sm">
      <form @submit.prevent="handleQuickAddTask" class="flex flex-col sm:flex-row items-stretch sm:items-center gap-2.5">
        <div class="flex-1 relative">
          <input
            v-model="quickTaskTitle"
            type="text"
            placeholder="Nhập nhanh công việc hoặc deadline cần làm..."
            class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-sm font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
          />
        </div>

        <div class="flex items-center gap-2">
          <input
            v-model="quickTaskDueDate"
            type="date"
            class="px-3 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-800 text-xs font-bold focus:bg-white focus:border-indigo-600 focus:outline-none flex-1 sm:flex-initial"
          />

          <button
            type="submit"
            class="px-4 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold shrink-0 shadow-sm transition-all active:scale-95 flex items-center gap-1"
          >
            <span class="material-symbols-rounded text-base">add_task</span>
            <span>Thêm</span>
          </button>
        </div>
      </form>

      <!-- Phím tắt chọn nhanh hạn chót -->
      <div class="flex items-center gap-1.5 mt-2.5 flex-wrap">
        <span class="text-[11px] text-slate-400 font-semibold mr-1">Hạn nhanh:</span>
        <button
          v-for="q in quickDeadlines"
          :key="q.label"
          type="button"
          @click="setQuickBarDate(q.days)"
          class="px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-indigo-50 text-slate-700 hover:text-indigo-700 border border-slate-200 text-xs font-semibold transition-all"
        >
          {{ q.label }}
        </button>
      </div>
    </section>

    <!-- ==================== 3. THẺ THỐNG KÊ THEO MỐC THỜI GIAN ==================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
      <!-- 1. Quá hạn -->
      <div
        @click="filterTag = filterTag === 'OVERDUE' ? 'ALL' : 'OVERDUE'"
        class="p-4 rounded-2xl bg-white border transition-all cursor-pointer shadow-sm"
        :class="filterTag === 'OVERDUE' ? 'border-rose-500 ring-1 ring-rose-500 bg-rose-50/30' : 'border-slate-200 hover:border-rose-300'"
      >
        <div class="flex items-center justify-between text-rose-600 mb-1">
          <span class="text-xs font-bold uppercase tracking-wider">Quá Hạn</span>
          <span class="material-symbols-rounded text-lg">warning</span>
        </div>
        <div class="text-2xl font-black font-mono text-rose-600">
          {{ overdueTasks.length }}
        </div>
        <span class="text-[11px] text-slate-500 mt-1 block">Cần xử lý gấp</span>
      </div>

      <!-- 2. Hôm nay -->
      <div
        @click="filterTag = filterTag === 'TODAY' ? 'ALL' : 'TODAY'"
        class="p-4 rounded-2xl bg-white border transition-all cursor-pointer shadow-sm"
        :class="filterTag === 'TODAY' ? 'border-emerald-600 ring-1 ring-emerald-600 bg-emerald-50/30' : 'border-slate-200 hover:border-emerald-300'"
      >
        <div class="flex items-center justify-between text-emerald-600 mb-1">
          <span class="text-xs font-bold uppercase tracking-wider">Hôm Nay</span>
          <span class="material-symbols-rounded text-lg">today</span>
        </div>
        <div class="text-2xl font-black font-mono text-emerald-600">
          {{ todayTasks.length }}
        </div>
        <span class="text-[11px] text-slate-500 mt-1 block">Hoàn thành trong ngày</span>
      </div>

      <!-- 3. Tuần này -->
      <div
        @click="filterTag = filterTag === 'WEEK' ? 'ALL' : 'WEEK'"
        class="p-4 rounded-2xl bg-white border transition-all cursor-pointer shadow-sm"
        :class="filterTag === 'WEEK' ? 'border-amber-500 ring-1 ring-amber-500 bg-amber-50/30' : 'border-slate-200 hover:border-amber-300'"
      >
        <div class="flex items-center justify-between text-amber-600 mb-1">
          <span class="text-xs font-bold uppercase tracking-wider">Tuần Này</span>
          <span class="material-symbols-rounded text-lg">date_range</span>
        </div>
        <div class="text-2xl font-black font-mono text-slate-900">
          {{ weekTasks.length }}
        </div>
        <span class="text-[11px] text-slate-500 mt-1 block">Trong 7 ngày tới</span>
      </div>

      <!-- 4. Dài Hạn -->
      <div
        @click="filterTag = filterTag === 'LONG_TERM' ? 'ALL' : 'LONG_TERM'"
        class="p-4 rounded-2xl bg-white border transition-all cursor-pointer shadow-sm"
        :class="filterTag === 'LONG_TERM' ? 'border-indigo-600 ring-1 ring-indigo-600 bg-indigo-50/30' : 'border-slate-200 hover:border-indigo-300'"
      >
        <div class="flex items-center justify-between text-indigo-600 mb-1">
          <span class="text-xs font-bold uppercase tracking-wider">Dài Hạn / Đồ Án</span>
          <span class="material-symbols-rounded text-lg">flag</span>
        </div>
        <div class="text-2xl font-black font-mono text-indigo-600">
          {{ longTermTasks.length }}
        </div>
        <span class="text-[11px] text-slate-500 mt-1 block">Mục tiêu lớn</span>
      </div>
    </div>

    <!-- ==================== 4. BỘ LỌC TRẠNG THÁI ==================== -->
    <div class="flex items-center justify-between border-b border-slate-200 pb-2.5">
      <div class="flex items-center gap-1.5">
        <button
          v-for="status in ['Chưa xong', 'Tất cả', 'Đã hoàn thành']"
          :key="status"
          type="button"
          @click="statusFilter = status"
          class="px-3 py-1.5 rounded-xl text-xs font-bold transition-all"
          :class="statusFilter === status
            ? 'bg-indigo-600 text-white shadow-sm'
            : 'bg-white text-slate-600 border border-slate-200 hover:bg-slate-50'"
        >
          {{ status }}
        </button>
      </div>

      <span v-if="filterTag !== 'ALL'" class="text-xs text-slate-500 flex items-center gap-1">
        Đang lọc: <strong class="text-indigo-600">{{ filterTagLabel }}</strong>
        <button type="button" @click="filterTag = 'ALL'" class="text-xs text-rose-600 font-bold hover:underline ml-1">✕ Bỏ lọc</button>
      </span>
    </div>

    <!-- ==================== 5. DANH SÁCH CÔNG VIỆC ==================== -->
    <div v-if="localTasks.length === 0" class="text-center py-14 bg-white border border-slate-200 rounded-2xl shadow-sm">
      <div class="w-12 h-12 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center mx-auto mb-3">
        <span class="material-symbols-rounded text-2xl">checklist</span>
      </div>
      <p class="text-sm font-bold text-slate-900">Chưa có công việc nào!</p>
      <p class="text-xs text-slate-500 mt-1">Nhập trực tiếp vào thanh phía trên để thêm việc cần làm.</p>
    </div>

    <!-- CHẾ ĐỘ XEM THEO NHÓM THỜI GIAN -->
    <div v-else-if="viewMode === 'grouped' && filterTag === 'ALL'" class="space-y-6">
      <section v-for="group in taskGroups" :key="group.id" v-show="group.items.length > 0" class="space-y-2.5">
        <div class="flex items-center gap-2 text-xs font-extrabold uppercase tracking-wider" :class="group.headerColor">
          <span class="material-symbols-rounded text-base">{{ group.icon }}</span>
          <span>{{ group.title }} ({{ group.items.length }})</span>
        </div>

        <div class="space-y-2">
          <div
            v-for="t in group.items"
            :key="t.id"
            class="p-3.5 sm:px-4 sm:py-3 rounded-2xl bg-white border border-slate-200 hover:border-slate-300 flex items-center justify-between gap-3 shadow-sm transition-all"
            :class="{ 'opacity-50 bg-slate-50': t.isCompleted }"
          >
            <div class="flex items-center gap-3 flex-1 min-w-0">
              <button
                type="button"
                @click="toggleTaskLocal(t)"
                class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all shrink-0"
                :class="t.isCompleted ? 'bg-emerald-600 border-emerald-600 text-white' : 'border-slate-300 hover:border-indigo-600 text-transparent'"
              >
                <span class="material-symbols-rounded text-sm font-bold">check</span>
              </button>

              <div class="min-w-0 flex-1">
                <span
                  class="text-xs sm:text-sm font-bold text-slate-900 truncate block"
                  :class="{ 'line-through text-slate-400': t.isCompleted }"
                >
                  {{ t.title }}
                </span>
                <span v-if="t.dueDate" class="text-[11px] text-slate-500 font-mono mt-0.5 block">
                  Hạn: {{ formatDueDate(t.dueDate) }}
                </span>
              </div>
            </div>

            <div class="flex items-center gap-1.5 shrink-0">
              <span
                v-if="t.dueDate && !t.isCompleted"
                class="text-[11px] font-mono font-bold px-2.5 py-0.5 rounded-full border"
                :class="getCountdownClass(t.dueDate)"
              >
                {{ getCountdownText(t.dueDate) }}
              </span>
              <button
                type="button"
                @click="openEditModal(t)"
                class="w-8 h-8 rounded-xl hover:bg-slate-100 text-slate-400 hover:text-indigo-600 flex items-center justify-center"
              >
                <span class="material-symbols-rounded text-base">edit</span>
              </button>
              <button
                type="button"
                @click="deleteTaskLocal(t.id)"
                class="w-8 h-8 rounded-xl hover:bg-rose-50 text-slate-400 hover:text-rose-600 flex items-center justify-center"
              >
                <span class="material-symbols-rounded text-base">delete</span>
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>

    <!-- CHẾ ĐỘ XEM DANH SÁCH PHẲNG -->
    <div v-else class="space-y-2">
      <div
        v-for="t in displayTasks"
        :key="t.id"
        class="p-3.5 sm:px-4 sm:py-3 rounded-2xl bg-white border border-slate-200 hover:border-slate-300 flex items-center justify-between gap-3 shadow-sm transition-all"
        :class="{ 'opacity-50 bg-slate-50': t.isCompleted }"
      >
        <div class="flex items-center gap-3 flex-1 min-w-0">
          <button
            type="button"
            @click="toggleTaskLocal(t)"
            class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all shrink-0"
            :class="t.isCompleted ? 'bg-emerald-600 border-emerald-600 text-white' : 'border-slate-300 hover:border-indigo-600 text-transparent'"
          >
            <span class="material-symbols-rounded text-sm font-bold">check</span>
          </button>

          <div class="min-w-0 flex-1">
            <span
              class="text-xs sm:text-sm font-bold text-slate-900 truncate block"
              :class="{ 'line-through text-slate-400': t.isCompleted }"
            >
              {{ t.title }}
            </span>
            <span v-if="t.dueDate" class="text-[11px] text-slate-500 font-mono mt-0.5 block">
              Hạn: {{ formatDueDate(t.dueDate) }}
            </span>
          </div>
        </div>

        <div class="flex items-center gap-1.5 shrink-0">
          <span
            v-if="t.dueDate && !t.isCompleted"
            class="text-[11px] font-mono font-bold px-2.5 py-0.5 rounded-full border"
            :class="getCountdownClass(t.dueDate)"
          >
            {{ getCountdownText(t.dueDate) }}
          </span>
          <button
            type="button"
            @click="openEditModal(t)"
            class="w-8 h-8 rounded-xl hover:bg-slate-100 text-slate-400 hover:text-indigo-600 flex items-center justify-center"
          >
            <span class="material-symbols-rounded text-base">edit</span>
          </button>
          <button
            type="button"
            @click="deleteTaskLocal(t.id)"
            class="w-8 h-8 rounded-xl hover:bg-rose-50 text-slate-400 hover:text-rose-600 flex items-center justify-center"
          >
            <span class="material-symbols-rounded text-base">delete</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ==================== MODAL THÊM / SỬA CÔNG VIỆC ==================== -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:px-4 bg-slate-900/50 backdrop-blur-sm">
      <div class="w-full max-w-md bg-white border border-slate-200 rounded-t-3xl sm:rounded-3xl p-5 sm:p-6 shadow-xl transition-all">
        <div class="flex items-center justify-between pb-3 mb-4 border-b border-slate-100">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
              <span class="material-symbols-rounded text-xl">{{ isEditing ? 'edit_note' : 'add_task' }}</span>
            </div>
            <div>
              <h3 class="text-base font-extrabold text-slate-900">
                {{ isEditing ? 'Chỉnh Sửa Công Việc' : 'Tạo Công Việc / Deadline' }}
              </h3>
              <p class="text-xs text-slate-500">Đặt lịch hạn chót để hệ thống nhắc nhở</p>
            </div>
          </div>
          <button type="button" @click="showModal = false" class="text-slate-400 hover:text-slate-700">
            <span class="material-symbols-rounded">close</span>
          </button>
        </div>

        <form @submit.prevent="handleSaveTask" class="space-y-4">
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">Tên công việc / Deadline</label>
            <input
              v-model="taskForm.title"
              type="text"
              required
              placeholder="Nhập tên công việc..."
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-sm font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="text-xs font-bold text-slate-700">Hạn chót (Deadline)</label>
              <button
                type="button"
                @click="taskForm.dueDate = ''"
                class="text-xs text-rose-600 font-semibold hover:underline"
              >
                Không đặt hạn
              </button>
            </div>
            <input
              v-model="taskForm.dueDate"
              type="date"
              class="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-bold focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-[11px] font-bold text-slate-500 mb-1.5">Chọn nhanh hạn chót:</label>
            <div class="grid grid-cols-4 gap-2">
              <button
                v-for="q in quickDeadlines"
                :key="q.label"
                type="button"
                @click="setQuickDueDate(q.days)"
                class="py-1.5 px-2 rounded-xl bg-slate-100 hover:bg-indigo-50 border border-slate-200 text-xs font-bold text-slate-700 hover:text-indigo-700 text-center transition-all"
              >
                {{ q.label }}
              </button>
            </div>
          </div>

          <div class="flex items-center justify-end gap-2.5 pt-3 border-t border-slate-100">
            <button
              type="button"
              @click="showModal = false"
              class="px-4 py-2 rounded-xl text-xs font-bold text-slate-600 hover:bg-slate-100"
            >
              Hủy
            </button>
            <button
              type="submit"
              class="flex items-center gap-1.5 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-bold transition-all shadow-sm active:scale-95"
            >
              <span class="material-symbols-rounded text-base">check</span>
              <span>{{ isEditing ? 'Lưu Cập Nhật' : 'Tạo Công Việc' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { apiFetch } from '../services/api';

const props = defineProps({
  tasks: { type: Array, default: () => [] },
});
const emit = defineEmits(['refresh-tasks', 'toggle-task']);

const API_BASE_URL = '/tasks';
const TASKS_STORAGE_KEY = 'second_brain_tasks_v1';

const loadSavedTasks = () => {
  try {
    const raw = localStorage.getItem(TASKS_STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
};

const saveTasksToStorage = (list) => {
  try {
    localStorage.setItem(TASKS_STORAGE_KEY, JSON.stringify(list));
  } catch (e) {}
};

const localTasks = ref(props.tasks.length > 0 ? [...props.tasks] : loadSavedTasks());

watch(
  () => props.tasks,
  (newVal) => {
    if (Array.isArray(newVal) && newVal.length > 0) {
      localTasks.value = [...newVal];
      saveTasksToStorage(localTasks.value);
    }
  },
  { deep: true }
);

const fetchLocalTasks = async () => {
  try {
    const res = await apiFetch(API_BASE_URL);
    if (res.ok) {
      const data = await res.json();
      if (Array.isArray(data) && (data.length > 0 || localTasks.value.length === 0)) {
        localTasks.value = data;
        saveTasksToStorage(data);
      }
    }
  } catch (err) {}
};

onMounted(() => {
  fetchLocalTasks();
});

const viewMode = ref('grouped');
const statusFilter = ref('Chưa xong');
const filterTag = ref('ALL');

const quickTaskTitle = ref('');
const quickTaskDueDate = ref(new Date().toISOString().split('T')[0]);

const showModal = ref(false);
const isEditing = ref(false);

const taskForm = ref({
  id: null,
  title: '',
  dueDate: '',
  priority: 'Medium',
  isCompleted: false,
});

const quickDeadlines = [
  { label: 'Hôm nay', days: 0 },
  { label: 'Ngày mai', days: 1 },
  { label: '1 tuần', days: 7 },
  { label: '1 tháng', days: 30 },
];

const setQuickDueDate = (offsetDays) => {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  taskForm.value.dueDate = d.toISOString().split('T')[0];
};

const setQuickBarDate = (offsetDays) => {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  quickTaskDueDate.value = d.toISOString().split('T')[0];
};

const getDiffDays = (dueDateStr) => {
  if (!dueDateStr) return null;
  const target = new Date(dueDateStr);
  const now = new Date();
  target.setHours(0, 0, 0, 0);
  now.setHours(0, 0, 0, 0);
  const diffTime = target.getTime() - now.getTime();
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
};

const getCountdownText = (dueDateStr) => {
  const diff = getDiffDays(dueDateStr);
  if (diff === null) return '';
  if (diff < 0) return `Quá hạn ${Math.abs(diff)} ngày`;
  if (diff === 0) return 'Hôm nay';
  if (diff === 1) return 'Ngày mai';
  return `Còn ${diff} ngày`;
};

const getCountdownClass = (dueDateStr) => {
  const diff = getDiffDays(dueDateStr);
  if (diff === null) return '';
  if (diff < 0) return 'bg-rose-50 text-rose-700 border-rose-200';
  if (diff === 0) return 'bg-emerald-50 text-emerald-700 border-emerald-200';
  if (diff <= 3) return 'bg-amber-50 text-amber-700 border-amber-200';
  return 'bg-indigo-50 text-indigo-700 border-indigo-200';
};

const formatDueDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`;
};

const statusFilteredTasks = computed(() => {
  if (statusFilter.value === 'Chưa xong') {
    return localTasks.value.filter(t => !t.isCompleted);
  }
  if (statusFilter.value === 'Đã hoàn thành') {
    return localTasks.value.filter(t => t.isCompleted);
  }
  return localTasks.value;
});

const overdueTasks = computed(() =>
  statusFilteredTasks.value.filter(t => t.dueDate && !t.isCompleted && getDiffDays(t.dueDate) < 0)
);

const todayTasks = computed(() =>
  statusFilteredTasks.value.filter(t => t.dueDate && getDiffDays(t.dueDate) === 0)
);

const weekTasks = computed(() =>
  statusFilteredTasks.value.filter(t => {
    const diff = getDiffDays(t.dueDate);
    return diff !== null && diff > 0 && diff <= 7;
  })
);

const longTermTasks = computed(() =>
  statusFilteredTasks.value
    .filter(t => {
      const diff = getDiffDays(t.dueDate);
      return diff !== null && diff > 7;
    })
    .sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate))
);

const noDueDateTasks = computed(() => statusFilteredTasks.value.filter(t => !t.dueDate));

const taskGroups = computed(() => [
  { id: 'overdue', title: 'Cần Làm Gấp / Quá Hạn', icon: 'error', headerColor: 'text-rose-600', items: overdueTasks.value },
  { id: 'today', title: 'Việc Trong Hôm Nay', icon: 'wb_sunny', headerColor: 'text-emerald-600', items: todayTasks.value },
  { id: 'week', title: 'Trong 7 Ngày Tới', icon: 'date_range', headerColor: 'text-amber-600', items: weekTasks.value },
  { id: 'long', title: 'Mục Tiêu & Đồ Án Dài Hạn', icon: 'flag', headerColor: 'text-indigo-600', items: longTermTasks.value },
  { id: 'none', title: 'Chưa Đặt Hạn Chót', icon: 'pending_actions', headerColor: 'text-slate-500', items: noDueDateTasks.value },
]);

const displayTasks = computed(() => {
  if (filterTag.value === 'OVERDUE') return overdueTasks.value;
  if (filterTag.value === 'TODAY') return todayTasks.value;
  if (filterTag.value === 'WEEK') return weekTasks.value;
  if (filterTag.value === 'LONG_TERM') return longTermTasks.value;
  return statusFilteredTasks.value;
});

const filterTagLabel = computed(() => {
  switch (filterTag.value) {
    case 'OVERDUE': return 'Quá hạn';
    case 'TODAY': return 'Hôm nay';
    case 'WEEK': return 'Trong tuần';
    case 'LONG_TERM': return 'Dài hạn / Đồ án';
    default: return '';
  }
});

// Thêm việc 1 chạm từ thanh trên cùng
const handleQuickAddTask = async () => {
  const title = quickTaskTitle.value.trim();
  if (!title) return;

  const newTask = {
    id: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'task_' + Date.now(),
    title,
    priority: 'Medium',
    dueDate: quickTaskDueDate.value ? new Date(quickTaskDueDate.value).toISOString() : null,
    isCompleted: false,
    createdAt: new Date().toISOString(),
  };

  localTasks.value.unshift(newTask);
  saveTasksToStorage(localTasks.value);
  quickTaskTitle.value = '';
  emit('refresh-tasks');

  apiFetch(API_BASE_URL, {
    method: 'POST',
    body: JSON.stringify(newTask),
  })
    .then(async res => {
      if (res.ok) {
        const saved = await res.json();
        const idx = localTasks.value.findIndex(x => x.id === newTask.id);
        if (idx !== -1) {
          localTasks.value[idx] = saved;
          saveTasksToStorage(localTasks.value);
        }
      }
    })
    .catch(() => {});
};

const openCreateModal = () => {
  isEditing.value = false;
  taskForm.value = {
    id: null,
    title: '',
    dueDate: new Date().toISOString().split('T')[0],
    priority: 'Medium',
    isCompleted: false,
  };
  showModal.value = true;
};

const openEditModal = (t) => {
  isEditing.value = true;
  taskForm.value = {
    id: t.id,
    title: t.title,
    dueDate: t.dueDate ? t.dueDate.split('T')[0] : '',
    priority: t.priority || 'Medium',
    isCompleted: t.isCompleted || false,
  };
  showModal.value = true;
};

const handleSaveTask = async () => {
  if (!taskForm.value.title.trim()) return;

  const payload = {
    id: isEditing.value && taskForm.value.id
      ? taskForm.value.id
      : (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'task_' + Date.now()),
    title: taskForm.value.title.trim(),
    priority: taskForm.value.priority,
    dueDate: taskForm.value.dueDate ? new Date(taskForm.value.dueDate).toISOString() : null,
    isCompleted: taskForm.value.isCompleted,
  };

  if (isEditing.value) {
    const idx = localTasks.value.findIndex(x => x.id === payload.id);
    if (idx !== -1) localTasks.value[idx] = payload;
  } else {
    localTasks.value.unshift(payload);
  }

  saveTasksToStorage(localTasks.value);
  showModal.value = false;
  emit('refresh-tasks');

  const url = isEditing.value ? `${API_BASE_URL}/${payload.id}` : API_BASE_URL;
  const method = isEditing.value ? 'PUT' : 'POST';
  apiFetch(url, { method, body: JSON.stringify(payload) })
    .then(async res => {
      if (res.ok) {
        const saved = await res.json();
        const idx = localTasks.value.findIndex(x => x.id === payload.id);
        if (idx !== -1 && saved && saved.id) {
          localTasks.value[idx] = saved;
          saveTasksToStorage(localTasks.value);
          emit('refresh-tasks');
        }
      }
    })
    .catch(() => {});
};

const toggleTaskLocal = async (task) => {
  task.isCompleted = !task.isCompleted;
  saveTasksToStorage(localTasks.value);
  emit('refresh-tasks');

  apiFetch(`${API_BASE_URL}/${task.id}/toggle`, { method: 'PATCH' }).catch(() => {});
};

const deleteTaskLocal = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa công việc này?')) return;
  localTasks.value = localTasks.value.filter(t => t.id !== id);
  saveTasksToStorage(localTasks.value);
  emit('refresh-tasks');

  apiFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' }).catch(() => {});
};
</script>
