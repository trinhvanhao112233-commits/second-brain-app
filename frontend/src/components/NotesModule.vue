<template>
  <div class="space-y-6">
    <!-- Top Action Bar (Material Design 3 Style) -->
    <header class="bg-[#171c24] border border-[#262a34] rounded-2xl px-5 py-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 md-elevation-1">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center md-elevation-1">
          <span class="material-symbols-rounded text-2xl">edit_note</span>
        </div>
        <div>
          <h2 class="text-lg font-bold text-white tracking-wide">Sổ Ghi Chú & Ý Tưởng</h2>
          <p class="text-xs text-[#8b9198]">Lưu trữ nhanh thông tin quan trọng và kiến thức cá nhân</p>
        </div>
      </div>

      <!-- Action Buttons & Search -->
      <div class="flex items-center gap-3">
        <!-- Search bar -->
        <div class="relative">
          <input
            v-model="searchQuery"
            type="text"
            placeholder="Tìm ghi chú..."
            class="px-3.5 py-1.5 pl-8 bg-[#1c2029] border border-[#262a34] rounded-full text-xs text-white placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6] w-36 sm:w-48 transition-all"
          />
          <span class="material-symbols-rounded text-base text-[#8b9198] absolute left-2.5 top-1.5 pointer-events-none">search</span>
        </div>

        <button
          type="button"
          @click="openAddModal"
          class="flex items-center gap-1.5 px-4 py-2 rounded-full bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] text-xs font-bold transition-all md-elevation-1 active:scale-95"
        >
          <span class="material-symbols-rounded text-base">add</span>
          <span>Tạo Ghi Chú</span>
        </button>
      </div>
    </header>

    <!-- Notes Grid / Empty State -->
    <div v-if="filteredNotes.length === 0" class="text-center py-16 bg-[#171c24] border border-[#262a34] rounded-2xl">
      <div class="w-12 h-12 rounded-2xl bg-[#1c2029] text-[#5dfec1] flex items-center justify-center mx-auto mb-3">
        <span class="material-symbols-rounded text-2xl">note_add</span>
      </div>
      <p class="text-sm font-bold text-white">Chưa có ghi chú nào!</p>
      <p class="text-xs text-[#8b9198] mt-1">Bấm nút "Tạo Ghi Chú" để lưu lại ý tưởng hoặc thông tin quan trọng.</p>
      <button
        @click="openAddModal"
        class="mt-4 px-4 py-2 rounded-full bg-[#005237] text-[#5dfec1] text-xs font-bold"
      >
        + Thêm ghi chú mới
      </button>
    </div>

    <div v-else class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
      <div
        v-for="note in filteredNotes"
        :key="note.id"
        class="bg-[#171c24] border rounded-2xl p-5 md-elevation-1 hover:md-elevation-2 transition-all duration-200 flex flex-col justify-between group relative overflow-hidden min-h-[190px]"
        :style="{
          borderColor: note.isPinned ? '#38e1a6' : '#262a34',
          borderLeftWidth: '4px',
          borderLeftColor: note.color || '#38e1a6'
        }"
      >
        <div>
          <!-- Header Card: Pin Badge & Actions -->
          <div class="flex items-center justify-between mb-2.5">
            <span
              v-if="note.isPinned"
              class="flex items-center gap-1 text-[10px] uppercase font-bold tracking-wider px-2 py-0.5 rounded-full bg-[#005237] text-[#5dfec1] border border-[#38e1a6]/30"
            >
              <span class="material-symbols-rounded text-xs">push_pin</span>
              <span>Đã ghim</span>
            </span>
            <span v-else class="text-[10px] text-[#8b9198] flex items-center gap-1">
              <span class="material-symbols-rounded text-xs">notes</span>
              <span>Ghi chú</span>
            </span>

            <div class="flex items-center gap-1 opacity-70 group-hover:opacity-100 transition-opacity">
              <button
                @click="togglePin(note)"
                class="w-7 h-7 rounded-lg hover:bg-[#1c2029] text-[#8b9198] hover:text-white flex items-center justify-center transition-colors"
                :title="note.isPinned ? 'Bỏ ghim' : 'Ghim lên đầu'"
              >
                <span class="material-symbols-rounded text-sm">{{ note.isPinned ? 'keep_off' : 'push_pin' }}</span>
              </button>
              <button
                @click="deleteNote(note.id)"
                class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center transition-colors"
                title="Xóa ghi chú"
              >
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>

          <!-- Note Title -->
          <h3 class="font-bold text-white text-base tracking-tight mb-2 line-clamp-1 group-hover:text-[#5dfec1] transition-colors">
            {{ note.title }}
          </h3>

          <!-- Note Content -->
          <p class="text-xs text-[#c1c7ce] whitespace-pre-wrap line-clamp-4 leading-relaxed font-sans">
            {{ note.content }}
          </p>
        </div>

        <!-- Note Footer -->
        <div class="text-[11px] font-mono text-[#8b9198] flex items-center justify-between pt-3 border-t border-[#262a34] mt-3">
          <span>{{ formatDate(note.updatedAt) }}</span>
          <button
            @click="editNote(note)"
            class="text-xs font-semibold text-[#5dfec1] hover:underline flex items-center gap-0.5"
          >
            <span>Chỉnh sửa</span>
            <span class="material-symbols-rounded text-xs">edit</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ==================== MODAL THÊM / SỬA GHI CHÚ (MD3 DIALOG) ==================== -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-md bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <!-- Dialog Header -->
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-xl">{{ isEditing ? 'edit_note' : 'note_add' }}</span>
          </div>
          <div>
            <h3 class="text-base font-bold text-white">
              {{ isEditing ? 'Chỉnh Sửa Ghi Chú' : 'Tạo Ghi Chú Mới' }}
            </h3>
            <p class="text-xs text-[#8b9198]">Lưu lại suy nghĩ, công thức hoặc tài liệu</p>
          </div>
        </div>

        <form @submit.prevent="handleSubmit" class="space-y-3.5">
          <!-- Title -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Tiêu đề ghi chú</label>
            <input
              v-model="currentNote.title"
              type="text"
              required
              placeholder="Tiêu đề ghi chú..."
              class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-sm focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <!-- Content -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Nội dung chi tiết</label>
            <textarea
              v-model="currentNote.content"
              rows="5"
              required
              placeholder="Nhập nội dung ghi chú tại đây..."
              class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6] resize-none leading-relaxed font-sans"
            ></textarea>
          </div>

          <!-- Color palette & Pin Toggle -->
          <div class="flex items-center justify-between pt-1">
            <div class="flex items-center gap-1.5">
              <button
                v-for="c in colorOptions"
                :key="c.hex"
                type="button"
                @click="currentNote.color = c.hex"
                class="w-6 h-6 rounded-full border-2 transition-all flex items-center justify-center"
                :class="currentNote.color === c.hex ? 'border-white scale-110' : 'border-transparent'"
                :style="{ backgroundColor: c.hex }"
                :title="c.label"
              >
                <span v-if="currentNote.color === c.hex" class="material-symbols-rounded text-xs text-black font-bold">check</span>
              </button>
            </div>

            <!-- Pin toggle -->
            <label class="flex items-center gap-1.5 cursor-pointer text-xs text-[#c1c7ce] select-none hover:text-white">
              <input
                type="checkbox"
                v-model="currentNote.isPinned"
                class="rounded border-[#41474d] bg-[#171c24] text-[#38e1a6] focus:ring-0"
              />
              <span class="flex items-center gap-0.5">
                <span class="material-symbols-rounded text-xs">push_pin</span>
                <span>Ghim lên đầu</span>
              </span>
            </label>
          </div>

          <!-- Dialog Actions -->
          <div class="flex items-center justify-end gap-2.5 pt-3 border-t border-[#262a34]">
            <button
              type="button"
              @click="showModal = false"
              class="px-4 py-2 rounded-full text-xs font-semibold text-[#c1c7ce] hover:bg-[#262a34]"
            >
              Hủy
            </button>
            <button
              type="submit"
              class="flex items-center gap-1.5 px-5 py-2 bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] rounded-full text-xs font-bold transition-all md-elevation-1 active:scale-95"
            >
              <span class="material-symbols-rounded text-base">check</span>
              <span>{{ isEditing ? 'Cập nhật' : 'Lưu Ghi Chú' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue';
import { apiFetch } from '../services/api';

const props = defineProps({
  notes: { type: Array, default: () => [] }
});
const emit = defineEmits(['refresh-notes']);

const API_BASE_URL = '/notes';

const showModal = ref(false);
const isEditing = ref(false);
const searchQuery = ref('');

const currentNote = ref({
  id: null,
  title: '',
  content: '',
  color: '#38e1a6',
  isPinned: false
});

const colorOptions = [
  { label: 'Xanh ngọc', hex: '#38e1a6' },
  { label: 'Xanh lá', hex: '#10b981' },
  { label: 'Cam', hex: '#f97316' },
  { label: 'Hồng', hex: '#f43f5e' },
  { label: 'Xanh biển', hex: '#06b6d4' },
];

const filteredNotes = computed(() => {
  let list = [...props.notes];
  if (searchQuery.value.trim()) {
    const q = searchQuery.value.toLowerCase().trim();
    list = list.filter(n => 
      (n.title && n.title.toLowerCase().includes(q)) || 
      (n.content && n.content.toLowerCase().includes(q))
    );
  }
  // Sắp xếp: Ghi chú đã ghim lên đầu, sau đó đến thời gian sửa đổi gần nhất
  return list.sort((a, b) => {
    if (a.isPinned !== b.isPinned) return b.isPinned ? 1 : -1;
    return new Date(b.updatedAt || 0) - new Date(a.updatedAt || 0);
  });
});

const openAddModal = () => {
  isEditing.value = false;
  currentNote.value = {
    id: null,
    title: '',
    content: '',
    color: '#38e1a6',
    isPinned: false
  };
  showModal.value = true;
};

const editNote = (note) => {
  isEditing.value = true;
  currentNote.value = { ...note };
  showModal.value = true;
};

const togglePin = async (note) => {
  try {
    const updated = {
      title: note.title,
      content: note.content,
      color: note.color,
      isPinned: !note.isPinned
    };
    const res = await apiFetch(`${API_BASE_URL}/${note.id}`, {
      method: 'PUT',
      body: JSON.stringify(updated)
    });
    if (res.ok) emit('refresh-notes');
  } catch (err) {
    console.error('Lỗi toggle pin ghi chú:', err);
  }
};

const handleSubmit = async () => {
  if (!currentNote.value.title.trim()) return;

  try {
    if (isEditing.value && currentNote.value.id) {
      await apiFetch(`${API_BASE_URL}/${currentNote.value.id}`, {
        method: 'PUT',
        body: JSON.stringify(currentNote.value)
      });
    } else {
      await apiFetch(API_BASE_URL, {
        method: 'POST',
        body: JSON.stringify(currentNote.value)
      });
    }
    showModal.value = false;
    emit('refresh-notes');
  } catch (err) {
    console.error('Lỗi lưu ghi chú:', err);
    alert('Đã xảy ra lỗi khi lưu ghi chú.');
  }
};

const deleteNote = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa ghi chú này?')) return;
  try {
    await apiFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' });
    emit('refresh-notes');
  } catch (err) {
    console.error(err);
  }
};

const formatDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('vi-VN', { month: 'numeric', day: 'numeric', hour: '2-digit', minute: '2-digit' });
};
</script>
