<template>
  <div class="min-h-screen bg-[#0f141c] flex items-center justify-center p-4">
    <!-- Background glow decoration -->
    <div class="fixed inset-0 overflow-hidden pointer-events-none">
      <div class="absolute -top-40 -left-40 w-96 h-96 bg-[#005237]/30 rounded-full blur-3xl"></div>
      <div class="absolute -bottom-40 -right-40 w-96 h-96 bg-[#38e1a6]/10 rounded-full blur-3xl"></div>
    </div>

    <!-- Auth Card Surface Container -->
    <div class="relative w-full max-w-md bg-[#171c24] border border-[#262a34] rounded-3xl p-6 sm:p-8 md-elevation-2 backdrop-blur-xl">
      <!-- App Brand & Icon -->
      <div class="flex flex-col items-center text-center mb-6">
        <div class="w-14 h-14 rounded-3xl bg-[#005237] text-[#5dfec1] flex items-center justify-center mb-3 md-elevation-1">
          <span class="material-symbols-rounded text-3xl font-bold">psychology</span>
        </div>
        <h1 class="text-2xl font-black tracking-tight text-[#e1e2ec]">Second Brain</h1>
        <p class="text-xs text-[#8b9198] mt-1">
          {{ isRegisterMode ? 'Tạo tài khoản không gian cá nhân của bạn' : 'Đăng nhập vào không gian cá nhân của bạn' }}
        </p>
      </div>

      <!-- Mode Switcher Tabs -->
      <div class="flex p-1 bg-[#1c2029] border border-[#262a34] rounded-2xl mb-6">
        <button
          type="button"
          @click="switchMode(false)"
          class="flex-1 py-2 text-xs font-bold rounded-xl transition-all"
          :class="!isRegisterMode ? 'bg-[#005237] text-[#5dfec1] md-elevation-1' : 'text-[#8b9198] hover:text-[#e1e2ec]'"
        >
          Đăng Nhập
        </button>
        <button
          type="button"
          @click="switchMode(true)"
          class="flex-1 py-2 text-xs font-bold rounded-xl transition-all"
          :class="isRegisterMode ? 'bg-[#005237] text-[#5dfec1] md-elevation-1' : 'text-[#8b9198] hover:text-[#e1e2ec]'"
        >
          Đăng Ký Mới
        </button>
      </div>

      <!-- Error / Success Alert -->
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
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <!-- Full Name (Only in Register mode) -->
        <div v-if="isRegisterMode">
          <label class="block text-xs font-bold text-[#c1c7ce] mb-1.5">Họ và tên</label>
          <div class="relative">
            <span class="absolute left-3.5 top-3 text-[#8b9198] material-symbols-rounded text-lg">person</span>
            <input
              v-model="form.fullName"
              type="text"
              placeholder="Nguyễn Văn A"
              class="w-full pl-11 pr-4 py-2.5 bg-[#1c2029] border border-[#41474d] rounded-2xl text-xs text-[#e1e2ec] placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6]"
            />
          </div>
        </div>

        <!-- Username -->
        <div>
          <label class="block text-xs font-bold text-[#c1c7ce] mb-1.5">Tên đăng nhập</label>
          <div class="relative">
            <span class="absolute left-3.5 top-3 text-[#8b9198] material-symbols-rounded text-lg">badge</span>
            <input
              v-model="form.username"
              type="text"
              required
              placeholder="username"
              autocomplete="username"
              class="w-full pl-11 pr-4 py-2.5 bg-[#1c2029] border border-[#41474d] rounded-2xl text-xs text-[#e1e2ec] placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6]"
            />
          </div>
        </div>

        <!-- Password -->
        <div>
          <label class="block text-xs font-bold text-[#c1c7ce] mb-1.5">Mật khẩu</label>
          <div class="relative">
            <span class="absolute left-3.5 top-3 text-[#8b9198] material-symbols-rounded text-lg">lock</span>
            <input
              v-model="form.password"
              :type="showPassword ? 'text' : 'password'"
              required
              placeholder="••••••••"
              autocomplete="current-password"
              class="w-full pl-11 pr-11 py-2.5 bg-[#1c2029] border border-[#41474d] rounded-2xl text-xs text-[#e1e2ec] placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6]"
            />
            <button
              type="button"
              @click="showPassword = !showPassword"
              class="absolute right-3.5 top-2.5 text-[#8b9198] hover:text-white"
            >
              <span class="material-symbols-rounded text-lg">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
            </button>
          </div>
        </div>

        <!-- Confirm Password (Only in Register mode) -->
        <div v-if="isRegisterMode">
          <label class="block text-xs font-bold text-[#c1c7ce] mb-1.5">Xác nhận mật khẩu</label>
          <div class="relative">
            <span class="absolute left-3.5 top-3 text-[#8b9198] material-symbols-rounded text-lg">lock_reset</span>
            <input
              v-model="form.confirmPassword"
              :type="showPassword ? 'text' : 'password'"
              required
              placeholder="••••••••"
              class="w-full pl-11 pr-4 py-2.5 bg-[#1c2029] border border-[#41474d] rounded-2xl text-xs text-[#e1e2ec] placeholder-[#8b9198] focus:outline-none focus:border-[#38e1a6]"
            />
          </div>
        </div>

        <!-- Submit Button -->
        <button
          type="submit"
          :disabled="isLoading"
          class="w-full py-3 px-4 rounded-2xl bg-[#005237] hover:bg-[#006a48] text-[#5dfec1] font-bold text-xs tracking-wide transition-all active:scale-[0.98] disabled:opacity-50 md-elevation-1 flex items-center justify-center gap-2 mt-2"
        >
          <span v-if="isLoading" class="material-symbols-rounded text-sm animate-spin">progress_activity</span>
          <span>{{ isLoading ? 'Đang xử lý...' : (isRegisterMode ? 'Đăng Ký Tài Khoản' : 'Đăng Nhập') }}</span>
        </button>
      </form>
    </div>
  </div>
</template>

<script setup>
import { ref, reactive } from 'vue';
import { apiFetch, setStoredUser } from '../services/api';

const emit = defineEmits(['auth-success']);

const isRegisterMode = ref(false);
const isLoading = ref(false);
const showPassword = ref(false);
const errorMessage = ref('');
const successMessage = ref('');

const form = reactive({
  username: '',
  fullName: '',
  password: '',
  confirmPassword: '',
});

const switchMode = (isRegister) => {
  isRegisterMode.value = isRegister;
  errorMessage.value = '';
  successMessage.value = '';
};

const handleSubmit = async () => {
  errorMessage.value = '';
  successMessage.value = '';

  if (isRegisterMode.value) {
    if (form.password.length < 6) {
      errorMessage.value = 'Mật khẩu phải có từ 6 ký tự trở lên!';
      return;
    }
    if (form.password !== form.confirmPassword) {
      errorMessage.value = 'Mật khẩu xác nhận không khớp!';
      return;
    }
  }

  isLoading.value = true;

  try {
    const endpoint = isRegisterMode.value ? '/auth/register' : '/auth/login';
    const payload = isRegisterMode.value
      ? { username: form.username.trim(), password: form.password, fullName: form.fullName.trim() }
      : { username: form.username.trim(), password: form.password };

    const res = await apiFetch(endpoint, {
      method: 'POST',
      body: JSON.stringify(payload),
    });

    const data = await res.json();

    if (!res.ok) {
      throw new Error(data.message || 'Thao tác không thành công. Vui lòng thử lại!');
    }

    // Lưu session
    setStoredUser(data);
    successMessage.value = isRegisterMode.value ? 'Đăng ký thành công! Đang chuyển tiếp...' : 'Đăng nhập thành công!';
    
    setTimeout(() => {
      emit('auth-success', data);
    }, 400);

  } catch (err) {
    console.error(err);
    errorMessage.value = err.message || 'Có lỗi xảy ra, vui lòng thử lại!';
  } finally {
    isLoading.value = false;
  }
};
</script>
