<template>
  <div class="space-y-5">
    <!-- ==================== 1. HEADER & TÌM KIẾM ==================== -->
    <header class="bg-white border border-slate-200 rounded-2xl px-5 py-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm">
      <div class="flex items-center gap-3.5">
        <div class="w-11 h-11 rounded-2xl bg-indigo-50 text-indigo-600 border border-indigo-100 flex items-center justify-center shrink-0">
          <span class="material-symbols-rounded text-2xl">edit_note</span>
        </div>
        <div>
          <h2 class="text-lg font-extrabold text-slate-900 tracking-tight">Sổ Ghi Chú & Tài Liệu</h2>
          <p class="text-xs text-slate-500 mt-0.5">Lưu trữ thông tin quan trọng, số tài khoản, ý tưởng cá nhân</p>
        </div>
      </div>

      <!-- Search & Add -->
      <div class="flex items-center gap-2.5">
        <div class="relative flex-1 sm:flex-initial">
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm kiếm ghi chú..."
            class="w-full sm:w-52 px-3.5 py-2 pl-9 bg-slate-50 border border-slate-200 rounded-xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:border-indigo-600 focus:outline-none transition-all"
          />
          <span class="material-symbols-rounded text-base text-slate-400 absolute left-2.5 inset-y-0 flex items-center pointer-events-none">search</span>
        </div>

        <button
          type="button"
          @click="openAddModal"
          class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold transition-all shadow-sm active:scale-95 shrink-0"
        >
          <span class="material-symbols-rounded text-base">add</span>
          <span>+ Tạo Ghi Chú</span>
        </button>
      </div>
    </header>

    <!-- ==================== 2. LƯỚI GHI CHÚ ==================== -->
    <div v-if="filteredNotes.length === 0" class="text-center py-14 bg-white border border-slate-200 rounded-2xl shadow-sm">
      <div class="w-12 h-12 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center mx-auto mb-3">
        <span class="material-symbols-rounded text-2xl">note_add</span>
      </div>
      <p class="text-sm font-bold text-slate-900">Chưa có ghi chú nào!</p>
      <p class="text-xs text-slate-500 mt-1">Bấm nút "+ Tạo Ghi Chú" để lưu lại thông tin quan trọng của bạn.</p>
      <button
        type="button"
        @click="openAddModal"
        class="mt-4 px-4 py-2 rounded-xl bg-indigo-600 text-white text-xs font-bold shadow-sm"
      >
        + Thêm ghi chú đầu tiên
      </button>
    </div>

    <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="note in filteredNotes"
        :key="note.id"
        class="bg-white border rounded-2xl p-5 shadow-sm hover:shadow-md transition-all flex flex-col justify-between min-h-[190px]"
        :style="{
          borderColor: note.isPinned ? '#4f46e5' : '#e2e8f0',
          borderLeftWidth: '5px',
          borderLeftColor: note.color || '#4f46e5',
        }"
      >
        <div>
          <!-- Card Header -->
          <div class="flex items-center justify-between mb-2.5">
            <span
              v-if="note.isPinned"
              class="flex items-center gap-1 text-[10px] uppercase font-bold tracking-wider px-2.5 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200"
            >
              <span class="material-symbols-rounded text-xs">push_pin</span>
              <span>Đã ghim</span>
            </span>
            <span v-else class="text-[11px] font-semibold text-slate-400 flex items-center gap-1">
              <span class="material-symbols-rounded text-xs">notes</span>
              <span>Ghi chú</span>
            </span>

            <div class="flex items-center gap-1">
              <button
                type="button"
                @click="copyNoteContent(note)"
                class="w-7 h-7 rounded-lg hover:bg-slate-100 text-slate-400 hover:text-indigo-600 flex items-center justify-center transition-colors"
                title="Sao chép nội dung"
              >
                <span class="material-symbols-rounded text-sm">
                  {{ copiedNoteId === note.id ? 'check' : 'content_copy' }}
                </span>
              </button>
              <button
                type="button"
                @click="togglePin(note)"
                class="w-7 h-7 rounded-lg hover:bg-slate-100 flex items-center justify-center transition-colors"
                :class="note.isPinned ? 'text-indigo-600' : 'text-slate-400 hover:text-slate-700'"
                :title="note.isPinned ? 'Bỏ ghim' : 'Ghim lên đầu'"
              >
                <span class="material-symbols-rounded text-sm">push_pin</span>
              </button>
              <button
                type="button"
                @click="deleteNote(note.id)"
                class="w-7 h-7 rounded-lg hover:bg-rose-50 text-slate-400 hover:text-rose-600 flex items-center justify-center transition-colors"
                title="Xóa ghi chú"
              >
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>

          <!-- Title -->
          <h3
            @click="editNote(note)"
            class="font-extrabold text-slate-900 text-base tracking-tight mb-2 line-clamp-1 cursor-pointer hover:text-indigo-600 transition-colors"
          >
            {{ note.title }}
          </h3>

          <!-- Content -->
          <p class="text-xs text-slate-600 whitespace-pre-wrap line-clamp-5 leading-relaxed">
            {{ note.content }}
          </p>
        </div>

        <!-- Card Footer -->
        <div class="text-[11px] font-mono text-slate-400 flex items-center justify-between pt-3 border-t border-slate-100 mt-3">
          <span>{{ formatDate(note.updatedAt || note.createdAt) }}</span>
          <button
            type="button"
            @click="editNote(note)"
            class="text-xs font-sans font-bold text-indigo-600 hover:underline flex items-center gap-0.5"
          >
            <span>Chỉnh sửa</span>
            <span class="material-symbols-rounded text-xs">edit</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ==================== MODAL THÊM / SỬA GHI CHÚ ==================== -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:px-4 bg-slate-900/50 backdrop-blur-sm">
      <div class="w-full max-w-md max-h-[92vh] overflow-y-auto bg-white border border-slate-200 rounded-t-3xl sm:rounded-3xl p-5 sm:p-6 shadow-xl transition-all">
        <div class="flex items-center justify-between pb-3 mb-4 border-b border-slate-100">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
              <span class="material-symbols-rounded text-xl">{{ isEditing ? 'edit_note' : 'note_add' }}</span>
            </div>
            <div>
              <h3 class="text-base font-extrabold text-slate-900">
                {{ isEditing ? 'Chỉnh Sửa Ghi Chú' : 'Tạo Ghi Chú Mới' }}
              </h3>
              <p class="text-xs text-slate-500">Lưu thông tin nhanh chóng, đồng bộ mọi thiết bị</p>
            </div>
          </div>
          <button type="button" @click="showModal = false" class="text-slate-400 hover:text-slate-700">
            <span class="material-symbols-rounded">close</span>
          </button>
        </div>

        <form @submit.prevent="handleSubmit" class="space-y-4">
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">Tiêu đề ghi chú</label>
            <input
              v-model="currentNote.title"
              type="text"
              required
              placeholder="Nhập tiêu đề..."
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-sm font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">Nội dung chi tiết</label>
            <textarea
              v-model="currentNote.content"
              rows="6"
              required
              placeholder="Nhập nội dung ghi chú..."
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs focus:bg-white focus:border-indigo-600 focus:outline-none resize-none leading-relaxed"
            ></textarea>
          </div>

          <div class="flex items-center justify-between pt-1">
            <div class="flex items-center gap-2">
              <button
                v-for="c in colorOptions"
                :key="c.hex"
                type="button"
                @click="currentNote.color = c.hex"
                class="w-7 h-7 rounded-full border-2 transition-all flex items-center justify-center"
                :class="currentNote.color === c.hex ? 'border-slate-900 scale-110' : 'border-transparent'"
                :style="{ backgroundColor: c.hex }"
                :title="c.label"
              >
                <span v-if="currentNote.color === c.hex" class="material-symbols-rounded text-xs text-white font-bold">check</span>
              </button>
            </div>

            <label class="flex items-center gap-1.5 cursor-pointer text-xs font-bold text-slate-700 select-none">
              <input
                v-model="currentNote.isPinned"
                type="checkbox"
                class="rounded text-indigo-600 focus:ring-0 w-4 h-4"
              />
              <span>Ghim lên đầu</span>
            </label>
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
              <span>{{ isEditing ? 'Lưu Cập Nhật' : 'Lưu Ghi Chú' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { apiFetch } from '../services/api';

const props = defineProps({
  notes: { type: Array, default: () => [] },
});
const emit = defineEmits(['refresh-notes']);

const API_BASE_URL = '/notes';
const NOTES_STORAGE_KEY = 'second_brain_notes_v1';

const loadSavedNotes = () => {
  try {
    const raw = localStorage.getItem(NOTES_STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
};

const saveNotesToStorage = (list) => {
  try {
    localStorage.setItem(NOTES_STORAGE_KEY, JSON.stringify(list));
  } catch (e) {}
};

const localNotes = ref(props.notes.length > 0 ? [...props.notes] : loadSavedNotes());

watch(
  () => props.notes,
  (newVal) => {
    if (Array.isArray(newVal) && newVal.length > 0) {
      localNotes.value = [...newVal];
      saveNotesToStorage(localNotes.value);
    }
  },
  { deep: true }
);

const showModal = ref(false);
const isEditing = ref(false);
const searchQuery = ref('');
const copiedNoteId = ref(null);

const currentNote = ref({
  id: null,
  title: '',
  content: '',
  color: '#4f46e5',
  isPinned: false,
});

const colorOptions = [
  { label: 'Indigo', hex: '#4f46e5' },
  { label: 'Emerald', hex: '#10b981' },
  { label: 'Orange', hex: '#f97316' },
  { label: 'Rose', hex: '#f43f5e' },
  { label: 'Cyan', hex: '#06b6d4' },
];

const filteredNotes = computed(() => {
  let list = [...localNotes.value];
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(
      n =>
        (n.title && n.title.toLowerCase().includes(q)) ||
        (n.content && n.content.toLowerCase().includes(q))
    );
  }
  return list.sort((a, b) => {
    if (a.isPinned !== b.isPinned) return b.isPinned ? 1 : -1;
    return new Date(b.updatedAt || b.createdAt || 0) - new Date(a.updatedAt || a.createdAt || 0);
  });
});

const copyNoteContent = async (note) => {
  try {
    await navigator.clipboard.writeText(`${note.title}\n${note.content}`);
    copiedNoteId.value = note.id;
    setTimeout(() => {
      if (copiedNoteId.value === note.id) copiedNoteId.value = null;
    }, 1500);
  } catch (e) {}
};

const openAddModal = () => {
  isEditing.value = false;
  currentNote.value = {
    id: null,
    title: '',
    content: '',
    color: '#4f46e5',
    isPinned: false,
  };
  showModal.value = true;
};

const editNote = (note) => {
  isEditing.value = true;
  currentNote.value = { ...note };
  showModal.value = true;
};

const togglePin = async (note) => {
  note.isPinned = !note.isPinned;
  note.updatedAt = new Date().toISOString();
  saveNotesToStorage(localNotes.value);
  emit('refresh-notes');

  apiFetch(`${API_BASE_URL}/${note.id}`, {
    method: 'PUT',
    body: JSON.stringify(note),
  }).catch(() => {});
};

const handleSubmit = async () => {
  if (!currentNote.value.title.trim()) return;

  const itemToSave = {
    id: isEditing.value && currentNote.value.id
      ? currentNote.value.id
      : (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'note_' + Date.now()),
    title: currentNote.value.title.trim(),
    content: currentNote.value.content,
    color: currentNote.value.color || '#4f46e5',
    isPinned: currentNote.value.isPinned || false,
    updatedAt: new Date().toISOString(),
  };

  if (isEditing.value) {
    const idx = localNotes.value.findIndex(n => n.id === itemToSave.id);
    if (idx !== -1) localNotes.value[idx] = itemToSave;
  } else {
    localNotes.value.unshift(itemToSave);
  }

  saveNotesToStorage(localNotes.value);
  showModal.value = false;
  emit('refresh-notes');

  const url = isEditing.value ? `${API_BASE_URL}/${itemToSave.id}` : API_BASE_URL;
  const method = isEditing.value ? 'PUT' : 'POST';
  apiFetch(url, { method, body: JSON.stringify(itemToSave) }).catch(() => {});
};

const deleteNote = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa ghi chú này?')) return;
  localNotes.value = localNotes.value.filter(n => n.id !== id);
  saveNotesToStorage(localNotes.value);
  emit('refresh-notes');

  apiFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' }).catch(() => {});
};

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('vi-VN', { month: '2-digit', day: '2-digit', hour: '2-digit', minute: '2-digit' });
};
</script>
