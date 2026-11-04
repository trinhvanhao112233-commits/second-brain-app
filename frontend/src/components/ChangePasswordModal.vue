<template>
  <div v-if="isOpen" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:px-4 bg-slate-900/50 backdrop-blur-sm">
    <div class="w-full max-w-sm bg-white border border-slate-200 rounded-t-3xl sm:rounded-3xl p-6 shadow-xl transition-all">
      <!-- Modal Header -->
      <div class="flex items-center justify-between pb-3 mb-4 border-b border-slate-100">
        <div class="flex items-center gap-3">
          <div class="w-10 h-10 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-xl">key</span>
          </div>
          <div>
            <h3 class="text-base font-extrabold text-slate-900">Đổi Mật Khẩu</h3>
            <p class="text-xs text-slate-500">Cập nhật mật khẩu bảo vệ tài khoản</p>
          </div>
        </div>
        <button type="button" @click="closeModal" class="text-slate-400 hover:text-slate-700">
          <span class="material-symbols-rounded">close</span>
        </button>
      </div>

      <!-- Alert messages -->
      <div
        v-if="errorMessage"
        class="mb-4 p-3 rounded-2xl bg-rose-50 border border-rose-200 text-rose-700 text-xs font-semibold flex items-center gap-2"
      >
        <span class="material-symbols-rounded text-sm">error</span>
        <span>{{ errorMessage }}</span>
      </div>

      <div
        v-if="successMessage"
        class="mb-4 p-3 rounded-2xl bg-emerald-50 border border-emerald-200 text-emerald-700 text-xs font-semibold flex items-center gap-2"
      >
        <span class="material-symbols-rounded text-sm">check_circle</span>
        <span>{{ successMessage }}</span>
      </div>

      <!-- Form -->
      <form @submit.prevent="handleChangePassword" class="space-y-3.5">
        <div>
          <label class="block text-xs font-bold text-slate-700 mb-1">Mật khẩu hiện tại</label>
          <input
            v-model="oldPassword"
            type="password"
            required
            placeholder="••••••••"
            class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600"
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-slate-700 mb-1">Mật khẩu mới</label>
          <input
            v-model="newPassword"
            type="password"
            required
            placeholder="••••••••"
            class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600"
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-slate-700 mb-1">Xác nhận mật khẩu mới</label>
          <input
            v-model="confirmPassword"
            type="password"
            required
            placeholder="••••••••"
            class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600"
          />
        </div>

        <!-- Buttons -->
        <div class="flex items-center justify-end gap-2 pt-3 border-t border-slate-100">
          <button
            type="button"
            @click="closeModal"
            class="px-4 py-2 rounded-xl text-xs font-bold text-slate-600 hover:bg-slate-100"
          >
            Hủy
          </button>
          <button
            type="submit"
            :disabled="isLoading"
            class="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold transition-all disabled:opacity-50 shadow-sm flex items-center gap-1.5"
          >
            <span v-if="isLoading" class="material-symbols-rounded text-sm animate-spin">progress_activity</span>
            <span>{{ isLoading ? 'Đang lưu...' : 'Lưu Mật Khẩu' }}</span>
          </button>
        </div>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue';
import { apiFetch } from '../services/api';

const props = defineProps({
  isOpen: { type: Boolean, default: false },
});

const emit = defineEmits(['close']);

const oldPassword = ref('');
const newPassword = ref('');
const confirmPassword = ref('');
const isLoading = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

const closeModal = () => {
  oldPassword.value = '';
  newPassword.value = '';
  confirmPassword.value = '';
  errorMessage.value = '';
  successMessage.value = '';
  emit('close');
};

const handleChangePassword = async () => {
  errorMessage.value = '';
  successMessage.value = '';

  if (newPassword.value.length < 6) {
    errorMessage.value = 'Mật khẩu mới phải từ 6 ký tự trở lên!';
    return;
  }

  if (newPassword.value !== confirmPassword.value) {
    errorMessage.value = 'Mật khẩu xác nhận không khớp!';
    return;
  }

  isLoading.value = true;
  try {
    const res = await apiFetch('/auth/change-password', {
      method: 'POST',
      body: JSON.stringify({
        oldPassword: oldPassword.value,
        newPassword: newPassword.value,
      }),
    });

    const data = await res.json();
    if (!res.ok) {
      throw new Error(data.message || 'Đổi mật khẩu thất bại!');
    }

    successMessage.value = 'Đổi mật khẩu thành công!';
    setTimeout(() => {
      closeModal();
    }, 1000);
  } catch (err) {
    console.error(err);
    errorMessage.value = err.message || 'Có lỗi xảy ra, vui lòng thử lại!';
  } finally {
    isLoading.value = false;
  }
};
</script>
