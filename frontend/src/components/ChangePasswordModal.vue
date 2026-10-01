<template>
  <div v-if="isOpen" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
    <div class="w-full max-w-sm bg-[#1c2029] border border-[#262a34] rounded-3xl p-6 md-elevation-3 transition-all animate-in fade-in duration-200">
      <!-- Modal Header -->
      <div class="flex items-center gap-3 mb-4">
        <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
          <span class="material-symbols-rounded text-xl">key</span>
        </div>
        <div>
          <h3 class="text-base font-bold text-white">Đổi Mật Khẩu</h3>
          <p class="text-xs text-[#8b9198]">Cập nhật mật khẩu bảo vệ tài khoản</p>
        </div>
      </div>

      <!-- Alert messages -->
      <div
        v-if="errorMessage"
        class="mb-4 p-3 rounded-2xl bg-[#ffb4ab]/15 border border-[#ffb4ab]/30 text-[#ffb4ab] text-xs flex items-center gap-2"
      >
        <span class="material-symbols-rounded text-sm">error</span>
        <span>{{ errorMessage }}</span>
      </div>

      <div
        v-if="successMessage"
        class="mb-4 p-3 rounded-2xl bg-[#005237]/40 border border-[#38e1a6]/40 text-[#5dfec1] text-xs flex items-center gap-2"
      >
        <span class="material-symbols-rounded text-sm">check_circle</span>
        <span>{{ successMessage }}</span>
      </div>

      <!-- Form -->
      <form @submit.prevent="handleChangePassword" class="space-y-3.5">
        <div>
          <label class="block text-xs font-bold text-[#c1c7ce] mb-1">Mật khẩu hiện tại</label>
          <input
            v-model="oldPassword"
            type="password"
            required
            placeholder="••••••••"
            class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-2xl text-xs text-[#e1e2ec] placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6]"
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-[#c1c7ce] mb-1">Mật khẩu mới</label>
          <input
            v-model="newPassword"
            type="password"
            required
            placeholder="••••••••"
            class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-2xl text-xs text-[#e1e2ec] placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6]"
          />
        </div>

        <div>
          <label class="block text-xs font-bold text-[#c1c7ce] mb-1">Xác nhận mật khẩu mới</label>
          <input
            v-model="confirmPassword"
            type="password"
            required
            placeholder="••••••••"
            class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-2xl text-xs text-[#e1e2ec] placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6]"
          />
        </div>

        <!-- Buttons -->
        <div class="flex items-center justify-end gap-2 pt-3 border-t border-[#262a34]">
          <button
            type="button"
            @click="closeModal"
            class="px-4 py-2 rounded-full text-xs font-semibold text-[#c1c7ce] hover:bg-[#262a34]"
          >
            Hủy
          </button>
          <button
            type="submit"
            :disabled="isLoading"
            class="px-5 py-2 rounded-full bg-[#005237] hover:bg-[#006a48] text-[#5dfec1] text-xs font-bold transition-all disabled:opacity-50 md-elevation-1 flex items-center gap-1.5"
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
  isOpen: { type: Boolean, default: false }
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
        newPassword: newPassword.value
      })
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
