<template>
  <div class="min-h-screen bg-[#f4f6fa] flex items-center justify-center p-4">
    <!-- Auth Card Surface Container -->
    <div class="relative w-full max-w-md bg-white border border-slate-200 rounded-3xl p-6 sm:p-8 shadow-xl">
      <!-- App Brand & Icon -->
      <div class="flex flex-col items-center text-center mb-6">
        <div class="w-14 h-14 rounded-2xl bg-indigo-600 text-white flex items-center justify-center mb-3 shadow-md shadow-indigo-500/20">
          <span class="material-symbols-rounded text-3xl font-bold">psychology</span>
        </div>
        <h1 class="text-2xl font-black tracking-tight text-slate-900">Second Brain</h1>
        <p class="text-xs text-slate-500 mt-1">
          {{ isRegisterMode ? 'Tạo tài khoản quản lý tài chính & công việc cá nhân' : 'Đăng nhập vào không gian quản lý của bạn' }}
        </p>
      </div>

      <!-- Mode Switcher Tabs -->
      <div class="flex p-1 bg-slate-100 border border-slate-200 rounded-2xl mb-6">
        <button
          type="button"
          @click="switchMode(false)"
          class="flex-1 py-2 text-xs font-bold rounded-xl transition-all"
          :class="!isRegisterMode ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
        >
          Đăng Nhập
        </button>
        <button
          type="button"
          @click="switchMode(true)"
          class="flex-1 py-2 text-xs font-bold rounded-xl transition-all"
          :class="isRegisterMode ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
        >
          Đăng Ký Mới
        </button>
      </div>

      <!-- Error / Success Alert -->
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
      <form @submit.prevent="handleSubmit" class="space-y-4">
        <!-- Full Name (Only in Register mode) -->
        <div v-if="isRegisterMode">
          <label class="block text-xs font-bold text-slate-700 mb-1.5">Họ và tên</label>
          <div class="relative flex items-center">
            <span class="absolute left-3.5 inset-y-0 flex items-center pointer-events-none text-slate-400 material-symbols-rounded text-lg">person</span>
            <input
              v-model="form.fullName"
              type="text"
              placeholder="Nhập họ và tên..."
              class="w-full pl-11 pr-4 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600"
            />
          </div>
        </div>

        <!-- Username -->
        <div>
          <label class="block text-xs font-bold text-slate-700 mb-1.5">Tên đăng nhập</label>
          <div class="relative flex items-center">
            <span class="absolute left-3.5 inset-y-0 flex items-center pointer-events-none text-slate-400 material-symbols-rounded text-lg">badge</span>
            <input
              v-model="form.username"
              type="text"
              required
              placeholder="Nhập tên đăng nhập..."
              autocomplete="username"
              class="w-full pl-11 pr-4 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600"
            />
          </div>
        </div>

        <!-- Password -->
        <div>
          <label class="block text-xs font-bold text-slate-700 mb-1.5">Mật khẩu</label>
          <div class="relative flex items-center">
            <span class="absolute left-3.5 inset-y-0 flex items-center pointer-events-none text-slate-400 material-symbols-rounded text-lg">lock</span>
            <input
              v-model="form.password"
              :type="showPassword ? 'text' : 'password'"
              required
              placeholder="••••••••"
              autocomplete="current-password"
              class="w-full pl-11 pr-11 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600"
            />
            <button
              type="button"
              @click="showPassword = !showPassword"
              class="absolute right-3.5 inset-y-0 flex items-center text-slate-400 hover:text-slate-700"
            >
              <span class="material-symbols-rounded text-lg">{{ showPassword ? 'visibility_off' : 'visibility' }}</span>
            </button>
          </div>
        </div>

        <!-- Confirm Password (Only in Register mode) -->
        <div v-if="isRegisterMode">
          <label class="block text-xs font-bold text-slate-700 mb-1.5">Xác nhận mật khẩu</label>
          <div class="relative flex items-center">
            <span class="absolute left-3.5 inset-y-0 flex items-center pointer-events-none text-slate-400 material-symbols-rounded text-lg">lock_reset</span>
            <input
              v-model="form.confirmPassword"
              :type="showPassword ? 'text' : 'password'"
              required
              placeholder="••••••••"
              class="w-full pl-11 pr-4 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600"
            />
          </div>
        </div>

        <!-- Submit Button -->
        <button
          type="submit"
          :disabled="isLoading"
          class="w-full py-3 px-4 rounded-2xl bg-indigo-600 hover:bg-indigo-700 text-white font-bold text-xs tracking-wide transition-all active:scale-[0.98] disabled:opacity-50 shadow-sm flex items-center justify-center gap-2 mt-2"
        >
          <span v-if="isLoading" class="material-symbols-rounded text-sm animate-spin">progress_activity</span>
          <span>{{ isLoading ? 'Đang kết nối...' : (isRegisterMode ? 'Đăng Ký Tài Khoản' : 'Đăng Nhập') }}</span>
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

    setStoredUser(data);
    successMessage.value = isRegisterMode.value ? 'Đăng ký thành công! Đang chuyển tiếp...' : 'Đăng nhập thành công!';

    setTimeout(() => {
      emit('auth-success', data);
    }, 300);
  } catch (err) {
    console.error(err);
    errorMessage.value = err.message || 'Có lỗi xảy ra, vui lòng thử lại!';
  } finally {
    isLoading.value = false;
  }
};
</script>
