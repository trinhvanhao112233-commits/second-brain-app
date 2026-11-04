<template>
  <div class="space-y-6">
    <!-- ==================== 1. HEADER GỌN GÀNG ==================== -->
    <header class="bg-white border border-slate-200 rounded-2xl px-6 py-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 shadow-sm">
      <div class="flex items-center gap-3.5">
        <div class="w-11 h-11 rounded-2xl bg-emerald-50 text-emerald-600 border border-emerald-100 flex items-center justify-center shrink-0">
          <span class="material-symbols-rounded text-2xl">account_balance_wallet</span>
        </div>
        <div>
          <h2 class="text-lg font-extrabold text-slate-900 tracking-tight">Sổ Thu Chi & Quản Lý Ví</h2>
          <p class="text-xs text-slate-500 mt-0.5">
            Ghi thu chi 1 chạm • Tự động tính định mức tiêu mỗi ngày trong tháng {{ currentMonthLabel }}
          </p>
        </div>
      </div>

      <div class="flex items-center gap-2">
        <button
          type="button"
          @click="showNewWalletBox = !showNewWalletBox"
          class="flex items-center gap-1.5 px-3.5 py-2 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-700 font-bold text-xs transition-all"
        >
          <span class="material-symbols-rounded text-base">add_card</span>
          <span>+ Thêm Ví Mới</span>
        </button>

        <button
          type="button"
          @click="confirmResetAllData"
          class="flex items-center gap-1 px-3 py-2 rounded-xl bg-white hover:bg-rose-50 border border-slate-200 hover:border-rose-200 text-slate-500 hover:text-rose-600 font-semibold text-xs transition-all"
          title="Xóa toàn bộ ví và giao dịch để làm lại từ đầu"
        >
          <span class="material-symbols-rounded text-base">restart_alt</span>
          <span>Làm mới</span>
        </button>
      </div>
    </header>

    <!-- ==================== 2. 3 CON SỐ QUAN TRỌNG NHẤT (BỎ CÁC CHỈ SỐ CHỒNG CHÉO) ==================== -->
    <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
      <!-- Ô 1: Tổng Số Dư Hiện Có -->
      <div class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500">Số Dư Hiện Có</span>
          <div class="w-9 h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">account_balance</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-2xl sm:text-3xl font-black font-mono text-emerald-600">
            {{ formatVND(overviewStats.totalBalance) }}
          </div>
          <div class="text-xs font-mono text-slate-400 mt-0.5">
            ({{ formatShortK(overviewStats.totalBalance) }} đang có trong {{ wallets.length }} ví)
          </div>
        </div>
        <div class="text-xs text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Tổng thu tháng này:</span>
          <span class="font-mono font-bold text-emerald-700">+{{ formatShortK(overviewStats.monthlyIncome) }}</span>
        </div>
      </div>

      <!-- Ô 2: Đã Chi Trong Tháng -->
      <div class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500">Đã Chi Tháng Này</span>
          <div class="w-9 h-9 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">trending_down</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-2xl sm:text-3xl font-black font-mono text-rose-600">
            -{{ formatVND(overviewStats.monthlySpent) }}
          </div>
          <div class="text-xs font-mono text-slate-400 mt-0.5">
            (Trung bình {{ formatShortK(overviewStats.avgDailySpent) }} / ngày)
          </div>
        </div>
        <div class="text-xs text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Chi tiêu hôm nay:</span>
          <span class="font-mono font-bold text-rose-600">-{{ formatShortK(overviewStats.todaySpent) }}</span>
        </div>
      </div>

      <!-- Ô 3: Định Mức Được Tiêu Mỗi Ngày -->
      <div
        class="rounded-2xl p-5 shadow-sm border flex flex-col justify-between"
        :class="overviewStats.safeDailyAllowance > 0 ? 'bg-indigo-50/50 border-indigo-200' : 'bg-rose-50/60 border-rose-200'"
      >
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-indigo-900">
            Được Tiêu Mỗi Ngày
          </span>
          <div class="w-9 h-9 rounded-xl bg-indigo-600 text-white flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">today</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-2xl sm:text-3xl font-black font-mono text-indigo-700">
            {{ formatVND(overviewStats.safeDailyAllowance) }}<span class="text-xs font-sans font-semibold text-slate-500">/ngày</span>
          </div>
          <div class="text-xs text-slate-600 mt-0.5">
            Còn <strong>{{ overviewStats.daysRemainingInMonth }} ngày</strong> nữa hết tháng {{ new Date().getMonth() + 1 }}
          </div>
        </div>
        <div class="text-xs text-indigo-800 pt-2 border-t border-indigo-200/70 flex items-center justify-between font-medium">
          <span>Giữ mức này để cuối tháng không thiếu tiền</span>
        </div>
      </div>
    </div>

    <!-- ==================== 3. THANH GHI THU / CHI 1 CHẠM (KHÔNG CẦN MỞ MODAL) ==================== -->
    <section class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm">
      <div class="flex items-center justify-between mb-3">
        <div class="flex items-center gap-2">
          <span class="material-symbols-rounded text-indigo-600 text-lg">bolt</span>
          <h3 class="text-sm font-extrabold text-slate-900 uppercase tracking-wide">Ghi Thu Chi Nhanh (1 Chạm)</h3>
        </div>
        <span v-if="parsedQuickAmount > 0" class="text-xs font-mono font-extrabold" :class="quickForm.type === 'EXPENSE' ? 'text-rose-600' : 'text-emerald-600'">
          {{ quickForm.type === 'EXPENSE' ? 'Trừ:' : 'Cộng:' }} {{ formatVND(parsedQuickAmount) }}
        </span>
      </div>

      <form @submit.prevent="handleQuickSubmit" class="grid grid-cols-1 md:grid-cols-12 gap-3 items-center">
        <!-- 1. Chuyển đổi Chi / Thu (2 cols) -->
        <div class="md:col-span-2 flex bg-slate-100 p-1 rounded-xl border border-slate-200">
          <button
            type="button"
            @click="quickForm.type = 'EXPENSE'"
            class="flex-1 py-2 rounded-lg text-xs font-bold transition-all flex items-center justify-center gap-1"
            :class="quickForm.type === 'EXPENSE' ? 'bg-rose-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
          >
            <span>- Chi ra</span>
          </button>
          <button
            type="button"
            @click="quickForm.type = 'INCOME'"
            class="flex-1 py-2 rounded-lg text-xs font-bold transition-all flex items-center justify-center gap-1"
            :class="quickForm.type === 'INCOME' ? 'bg-emerald-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
          >
            <span>+ Tiền vào</span>
          </button>
        </div>

        <!-- 2. Ô nhập số tiền thông minh (3 cols) -->
        <div class="md:col-span-3 relative">
          <input
            ref="quickAmountInputRef"
            v-model="quickForm.rawAmount"
            type="text"
            required
            placeholder="Số tiền (50k, 2tr, 30+20...)"
            class="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 font-mono text-sm font-bold focus:bg-white focus:border-indigo-600 focus:outline-none"
          />
        </div>

        <!-- 3. Chọn Ví (2 cols) -->
        <div class="md:col-span-2">
          <select
            v-model="quickForm.walletId"
            class="w-full px-3 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-bold focus:bg-white focus:border-indigo-600 focus:outline-none"
          >
            <option v-for="w in wallets" :key="w.id" :value="w.id">
              Ví: {{ w.name }} ({{ formatShortK(w.balance) }})
            </option>
          </select>
        </div>

        <!-- 4. Chọn Danh Mục (2 cols) -->
        <div class="md:col-span-2">
          <select
            v-if="quickForm.type === 'EXPENSE'"
            v-model="quickForm.category"
            class="w-full px-3 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
          >
            <option v-for="cat in expenseCategories" :key="cat" :value="cat">{{ cat }}</option>
          </select>
          <select
            v-else
            v-model="quickForm.incomeCategory"
            class="w-full px-3 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
          >
            <option value="Tiền vào">Tiền vào / Nạp ví</option>
            <option value="Lương">Lương / Thu nhập</option>
            <option value="Trợ cấp">Tiền ăn / Tiền học</option>
            <option value="Khác">Khoản thu khác</option>
          </select>
        </div>

        <!-- 5. Ghi chú + Nút Lưu (3 cols) -->
        <div class="md:col-span-3 flex items-center gap-2">
          <input
            v-model="quickForm.note"
            type="text"
            placeholder="Ghi chú (tùy chọn)..."
            class="w-full px-3 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs focus:bg-white focus:border-indigo-600 focus:outline-none"
          />
          <button
            type="submit"
            :disabled="isSubmitting"
            class="px-4 py-2.5 rounded-xl font-bold text-xs text-white shrink-0 shadow-sm transition-all active:scale-95"
            :class="quickForm.type === 'EXPENSE' ? 'bg-rose-600 hover:bg-rose-700' : 'bg-emerald-600 hover:bg-emerald-700'"
          >
            Lưu
          </button>
        </div>
      </form>

      <!-- Mức tiền gợi ý 1 chạm -->
      <div class="flex items-center gap-1.5 mt-3 flex-wrap">
        <span class="text-[11px] text-slate-400 font-semibold mr-1">Chọn nhanh:</span>
        <button
          v-for="preset in ['25k', '35k', '50k', '100k', '200k', '500k', '1tr', '2tr']"
          :key="preset"
          type="button"
          @click="quickForm.rawAmount = preset"
          class="px-2.5 py-1 rounded-lg bg-slate-100 hover:bg-indigo-50 text-slate-700 hover:text-indigo-700 border border-slate-200 text-xs font-mono font-bold transition-all"
        >
          {{ preset }}
        </button>
      </div>
    </section>

    <!-- ==================== 4. DANH SÁCH VÍ GỌN GÀNG ==================== -->
    <section class="space-y-3">
      <!-- Khung tạo ví mới (Inline, không cần bật Modal) -->
      <div v-if="showNewWalletBox" class="bg-indigo-50/60 border border-indigo-200 rounded-2xl p-4">
        <form @submit.prevent="handleCreateWallet" class="flex flex-col sm:flex-row items-center gap-3">
          <div class="flex-1 w-full">
            <label class="block text-xs font-bold text-indigo-950 mb-1">Tên ví mới</label>
            <input
              v-model="newWalletForm.name"
              type="text"
              required
              placeholder="Nhập tên ví (Tiền mặt, Ngân hàng, MoMo...)"
              class="w-full px-3.5 py-2 bg-white border border-indigo-200 rounded-xl text-slate-900 text-xs font-semibold focus:border-indigo-600 focus:outline-none"
            />
          </div>
          <div class="w-full sm:w-56">
            <label class="block text-xs font-bold text-indigo-950 mb-1">Số dư ban đầu (nếu có)</label>
            <input
              v-model="newWalletForm.initialBalance"
              type="text"
              placeholder="0 (hoặc 500k, 2tr...)"
              class="w-full px-3.5 py-2 bg-white border border-indigo-200 rounded-xl text-slate-900 font-mono text-xs font-bold focus:border-indigo-600 focus:outline-none"
            />
          </div>
          <div class="flex items-center gap-2 self-end sm:pt-5">
            <button
              type="button"
              @click="showNewWalletBox = false"
              class="px-3 py-2 rounded-xl text-xs font-bold text-slate-600 hover:bg-slate-200"
            >
              Đóng
            </button>
            <button
              type="submit"
              class="px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold shadow-sm"
            >
              Tạo Ví
            </button>
          </div>
        </form>
      </div>

      <!-- Hàng Thẻ Ví -->
      <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 gap-4">
        <div
          v-for="wallet in wallets"
          :key="wallet.id"
          @click="selectedWalletFilterId = selectedWalletFilterId === wallet.id ? 'ALL' : wallet.id"
          class="cursor-pointer bg-white border rounded-2xl p-4 shadow-sm transition-all flex flex-col justify-between"
          :class="selectedWalletFilterId === wallet.id ? 'border-indigo-600 ring-1 ring-indigo-600' : 'border-slate-200 hover:border-slate-300'"
        >
          <div class="flex items-center justify-between">
            <div class="flex items-center gap-2.5 min-w-0">
              <div class="w-9 h-9 rounded-xl bg-slate-100 text-slate-700 flex items-center justify-center shrink-0">
                <span class="material-symbols-rounded text-lg">savings</span>
              </div>
              <div class="min-w-0">
                <h4 class="font-bold text-sm text-slate-900 truncate">{{ wallet.name }}</h4>
                <span class="text-[11px] text-slate-400">
                  Đã chi tháng này: <strong class="text-rose-600 font-mono">-{{ formatShortK(getWalletMonthlySpent(wallet.id)) }}</strong>
                </span>
              </div>
            </div>

            <button
              v-if="wallets.length > 1"
              type="button"
              @click.stop="handleDeleteWallet(wallet)"
              class="p-1.5 rounded-lg text-slate-400 hover:text-rose-600 hover:bg-rose-50 transition-colors"
              title="Xóa ví này"
            >
              <span class="material-symbols-rounded text-base">delete</span>
            </button>
          </div>

          <div class="mt-3 pt-3 border-t border-slate-100 flex items-center justify-between">
            <div>
              <span class="text-[10px] uppercase font-bold text-slate-400 block">Số dư</span>
              <span class="text-lg font-black font-mono" :class="wallet.balance >= 0 ? 'text-emerald-600' : 'text-rose-600'">
                {{ formatVND(wallet.balance) }}
              </span>
            </div>

            <div class="flex items-center gap-1.5" @click.stop>
              <button
                type="button"
                @click="prefillQuickForm(wallet.id, 'EXPENSE')"
                class="px-2.5 py-1.5 rounded-lg bg-rose-50 hover:bg-rose-100 text-rose-700 text-xs font-bold transition-colors"
              >
                - Chi
              </button>
              <button
                type="button"
                @click="prefillQuickForm(wallet.id, 'INCOME')"
                class="px-2.5 py-1.5 rounded-lg bg-emerald-50 hover:bg-emerald-100 text-emerald-700 text-xs font-bold transition-colors"
              >
                + Nạp
              </button>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==================== 5. LỊCH SỬ GIAO DỊCH (CÓ NÚT XÓA TỪNG DÒNG) ==================== -->
    <section class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm space-y-4">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-slate-100">
        <div class="flex items-center gap-2">
          <span class="material-symbols-rounded text-indigo-600 text-lg">receipt_long</span>
          <div>
            <h3 class="text-sm font-extrabold uppercase tracking-wide text-slate-900">Lịch Sử Thu Chi</h3>
            <span class="text-xs text-slate-500">
              Đang hiển thị {{ filteredTransactions.length }} giao dịch
              <button
                v-if="selectedWalletFilterId !== 'ALL'"
                @click="selectedWalletFilterId = 'ALL'"
                class="ml-1 text-indigo-600 font-bold hover:underline"
              >
                (Xem tất cả ví)
              </button>
            </span>
          </div>
        </div>

        <!-- Bộ lọc nhanh đơn giản -->
        <div class="flex flex-wrap items-center gap-2">
          <div class="flex bg-slate-100 p-1 rounded-xl border border-slate-200">
            <button
              v-for="period in [
                { id: 'today', label: 'Hôm nay' },
                { id: 'month', label: 'Tháng này' },
                { id: 'all', label: 'Tất cả' }
              ]"
              :key="period.id"
              type="button"
              @click="dateFilterMode = period.id"
              class="px-3 py-1 text-xs font-bold rounded-lg transition-all"
              :class="dateFilterMode === period.id ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              {{ period.label }}
            </button>
          </div>

          <select
            v-model="selectedWalletFilterId"
            class="bg-slate-50 border border-slate-200 text-xs text-slate-800 font-bold rounded-xl px-3 py-1.5 focus:outline-none focus:border-indigo-600"
          >
            <option value="ALL">Tất cả ví</option>
            <option v-for="w in wallets" :key="w.id" :value="w.id">{{ w.name }}</option>
          </select>
        </div>
      </div>

      <!-- Empty State -->
      <div v-if="filteredTransactions.length === 0" class="py-10 text-center text-slate-400 text-xs">
        <span class="material-symbols-rounded text-3xl mb-1 text-slate-300">receipt</span>
        <p>Chưa có giao dịch nào trong khoảng thời gian này.</p>
      </div>

      <!-- Danh sách giao dịch -->
      <div v-else class="divide-y divide-slate-100">
        <div
          v-for="tx in filteredTransactions"
          :key="tx.id"
          class="py-3 px-2 flex items-center justify-between hover:bg-slate-50 rounded-xl transition-colors group"
        >
          <div class="flex items-center gap-3 min-w-0">
            <div
              class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 border"
              :class="tx.amount < 0 
                ? 'bg-rose-50 text-rose-600 border-rose-100' 
                : 'bg-emerald-50 text-emerald-600 border-emerald-100'"
            >
              <span class="material-symbols-rounded text-lg">
                {{ tx.amount < 0 ? getCategorySymbol(tx.category) : 'add_circle' }}
              </span>
            </div>

            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <span class="font-bold text-xs sm:text-sm text-slate-900 truncate">
                  {{ tx.note || tx.category || 'Giao dịch' }}
                </span>
                <span
                  class="text-[10px] px-2 py-0.5 rounded-full font-bold shrink-0"
                  :class="tx.amount < 0 ? 'bg-slate-100 text-slate-700' : 'bg-emerald-50 text-emerald-700'"
                >
                  {{ tx.category || 'Khác' }}
                </span>
              </div>
              <div class="flex items-center gap-2 text-[11px] text-slate-400 mt-0.5 font-mono">
                <span>{{ formatDateTime(tx.transactionDate || tx.createdAt) }}</span>
                <span>•</span>
                <span class="text-slate-600 font-sans font-semibold">{{ getWalletName(tx.walletId) }}</span>
              </div>
            </div>
          </div>

          <div class="flex items-center gap-3 shrink-0 ml-3">
            <div class="text-right">
              <span
                class="text-sm sm:text-base font-black font-mono block"
                :class="tx.amount < 0 ? 'text-rose-600' : 'text-emerald-600'"
              >
                {{ tx.amount < 0 ? '-' : '+' }}{{ formatVND(Math.abs(tx.amount)) }}
              </span>
            </div>

            <!-- Nút xóa giao dịch nếu nhập nhầm -->
            <button
              type="button"
              @click="handleDeleteTransaction(tx)"
              class="p-1.5 rounded-lg text-slate-300 hover:text-rose-600 hover:bg-rose-50 transition-colors"
              title="Xóa giao dịch này"
            >
              <span class="material-symbols-rounded text-base">close</span>
            </button>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { apiFetch } from '../services/api';

const API_BASE_URL = '/wallets';
const WALLETS_STORAGE_KEY = 'second_brain_wallets_v1';
const TX_STORAGE_KEY = 'second_brain_tx_v1';

// Bộ nhớ đệm LocalStorage giúp giao diện phản hồi 0ms
const loadLocalWallets = () => {
  try {
    const raw = localStorage.getItem(WALLETS_STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
};

const saveLocalWallets = (list) => {
  try {
    localStorage.setItem(WALLETS_STORAGE_KEY, JSON.stringify(list));
  } catch (e) {}
};

const loadLocalTx = () => {
  try {
    const raw = localStorage.getItem(TX_STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
};

const saveLocalTx = (list) => {
  try {
    localStorage.setItem(TX_STORAGE_KEY, JSON.stringify(list));
  } catch (e) {}
};

const wallets = ref(loadLocalWallets());
const allTransactions = ref(loadLocalTx());
const isSubmitting = ref(false);

const selectedWalletFilterId = ref('ALL');
const dateFilterMode = ref('month'); // 'today' | 'month' | 'all'
const showNewWalletBox = ref(false);
const quickAmountInputRef = ref(null);

const expenseCategories = [
  'Ăn uống',
  'Đi lại',
  'Mua sắm',
  'Hóa đơn',
  'Học tập',
  'Giải trí',
  'Khác',
];

const quickForm = ref({
  type: 'EXPENSE', // 'EXPENSE' | 'INCOME'
  rawAmount: '',
  walletId: '',
  category: 'Ăn uống',
  incomeCategory: 'Tiền vào',
  note: '',
});

const newWalletForm = ref({
  name: '',
  initialBalance: '',
});

const currentMonthLabel = computed(() => {
  const now = new Date();
  return `${now.getMonth() + 1}/${now.getFullYear()}`;
});

// Hàm phân tích chuỗi tiền thông minh (50k, 2tr, 30+20, 50000...)
const parseSmartAmount = (input) => {
  const raw = String(input || '').trim().toLowerCase();
  if (!raw) return 0;

  if (raw.includes('+')) {
    const parts = raw.split('+').map(p => {
      const c = p.trim().replace(/[,. ]/g, '');
      return parseFloat(c) || 0;
    });
    const sum = parts.reduce((a, b) => a + b, 0);
    return sum > 0 && sum < 50000 ? sum * 1000 : sum;
  }

  if (raw.endsWith('tr') || raw.endsWith('triệu') || raw.endsWith('m')) {
    const val = parseFloat(raw.replace(/(tr|triệu|m)/g, '').replace(/,/g, '.'));
    return isNaN(val) ? 0 : Math.round(val * 1000000);
  }

  if (raw.endsWith('k')) {
    const val = parseFloat(raw.replace('k', '').replace(/,/g, '.'));
    return isNaN(val) ? 0 : Math.round(val * 1000);
  }

  const clean = raw.replace(/[,. ]/g, '');
  const val = parseFloat(clean);
  if (isNaN(val)) return 0;

  if (val > 0 && val < 50000 && !raw.includes('.') && !raw.includes(',')) {
    return val * 1000;
  }
  return val;
};

const parsedQuickAmount = computed(() => parseSmartAmount(quickForm.value.rawAmount));

// Định dạng tiền
const formatVND = (value) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(value || 0);
};

const formatShortK = (num) => {
  const val = Number(num) || 0;
  return `${Math.round(val / 1000).toLocaleString('vi-VN')}k`;
};

const formatDateTime = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  const time = d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
  const date = d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' });
  return `${time} • ${date}`;
};

const getCategorySymbol = (category) => {
  switch (category) {
    case 'Ăn uống': return 'restaurant';
    case 'Đi lại': return 'two_wheeler';
    case 'Mua sắm': return 'shopping_bag';
    case 'Hóa đơn': return 'receipt';
    case 'Học tập': return 'school';
    case 'Giải trí': return 'movie';
    default: return 'payments';
  }
};

const getWalletName = (walletId) => {
  const w = wallets.value.find(x => x.id === walletId);
  return w ? w.name : 'Ví chính';
};

// Tính lại số dư từng ví dựa trên giao dịch nếu cần
const recalculateWalletBalances = () => {
  wallets.value = wallets.value.map(w => {
    const txs = allTransactions.value.filter(t => t.walletId === w.id);
    if (txs.length > 0) {
      const sum = txs.reduce((acc, t) => acc + (Number(t.amount) || 0), 0);
      return { ...w, balance: sum };
    }
    return w;
  });
  saveLocalWallets(wallets.value);
};

const getWalletMonthlySpent = (walletId) => {
  const now = new Date();
  const ym = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  return allTransactions.value
    .filter(t => t.walletId === walletId && Number(t.amount) < 0 && (t.transactionDate || t.createdAt || '').startsWith(ym))
    .reduce((sum, t) => sum + Math.abs(Number(t.amount) || 0), 0);
};

// 3 Chỉ số tổng quan thực tế nhất
const overviewStats = computed(() => {
  const now = new Date();
  const year = now.getFullYear();
  const month = now.getMonth();
  const todayDate = now.getDate();
  const daysInMonth = new Date(year, month + 1, 0).getDate();
  const daysRemainingInMonth = Math.max(1, daysInMonth - todayDate + 1);

  const ymPrefix = `${year}-${String(month + 1).padStart(2, '0')}`;
  const todayPrefix = `${ymPrefix}-${String(todayDate).padStart(2, '0')}`;

  const totalBalance = wallets.value.reduce((sum, w) => sum + (Number(w.balance) || 0), 0);

  let monthlySpent = 0;
  let monthlyIncome = 0;
  let todaySpent = 0;

  for (const tx of allTransactions.value) {
    const dStr = tx.transactionDate || tx.createdAt || '';
    const amt = Number(tx.amount) || 0;
    if (dStr.startsWith(ymPrefix)) {
      if (amt < 0) {
        monthlySpent += Math.abs(amt);
      } else {
        monthlyIncome += amt;
      }
    }
    if (dStr.startsWith(todayPrefix) && amt < 0) {
      todaySpent += Math.abs(amt);
    }
  }

  const avgDailySpent = Math.round(monthlySpent / Math.max(1, todayDate));
  const safeDailyAllowance = totalBalance > 0 ? Math.floor(totalBalance / daysRemainingInMonth) : 0;

  return {
    totalBalance,
    monthlySpent,
    monthlyIncome,
    todaySpent,
    avgDailySpent,
    daysRemainingInMonth,
    safeDailyAllowance,
  };
});

// Lọc lịch sử giao dịch
const filteredTransactions = computed(() => {
  let list = [...allTransactions.value];

  if (selectedWalletFilterId.value !== 'ALL') {
    list = list.filter(t => t.walletId === selectedWalletFilterId.value);
  }

  const now = new Date();
  const ymPrefix = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`;
  const todayPrefix = `${ymPrefix}-${String(now.getDate()).padStart(2, '0')}`;

  if (dateFilterMode.value === 'today') {
    list = list.filter(t => (t.transactionDate || t.createdAt || '').startsWith(todayPrefix));
  } else if (dateFilterMode.value === 'month') {
    list = list.filter(t => (t.transactionDate || t.createdAt || '').startsWith(ymPrefix));
  }

  return list.sort((a, b) => new Date(b.transactionDate || b.createdAt) - new Date(a.transactionDate || a.createdAt));
});

// Đảm bảo luôn có ít nhất 1 ví mặc định để người dùng ghi thu chi ngay mà không bị chặn
const ensureDefaultWallet = async () => {
  if (wallets.value.length === 0) {
    const defaultWallet = {
      id: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'wallet_' + Date.now(),
      name: 'Ví chính',
      balance: 0,
      monthlyBudget: 0,
      monthlySpent: 0,
      isMaster: true,
      createdAt: new Date().toISOString(),
    };
    wallets.value = [defaultWallet];
    saveLocalWallets(wallets.value);
    quickForm.value.walletId = defaultWallet.id;

    // Tạo ngầm trên server
    try {
      const res = await apiFetch(API_BASE_URL, {
        method: 'POST',
        body: JSON.stringify({ name: 'Ví chính', monthlyBudget: 0, isMaster: true }),
      });
      if (res.ok) {
        const created = await res.json();
        wallets.value[0].id = created.id;
        quickForm.value.walletId = created.id;
        saveLocalWallets(wallets.value);
      }
    } catch (e) {}
  } else if (!quickForm.value.walletId) {
    quickForm.value.walletId = wallets.value[0].id;
  }
};

// Tải dữ liệu từ API
const fetchWallets = async () => {
  try {
    const res = await apiFetch(API_BASE_URL);
    if (res.ok) {
      const serverWallets = await res.json();
      if (Array.isArray(serverWallets) && serverWallets.length > 0) {
        wallets.value = serverWallets;
        saveLocalWallets(serverWallets);
        if (!quickForm.value.walletId || !wallets.value.some(w => w.id === quickForm.value.walletId)) {
          quickForm.value.walletId = wallets.value[0].id;
        }
        await fetchAllTransactions();
      } else {
        await ensureDefaultWallet();
      }
    } else {
      await ensureDefaultWallet();
    }
  } catch (err) {
    await ensureDefaultWallet();
  }
};

const fetchAllTransactions = async () => {
  try {
    const txPromises = wallets.value.map(w =>
      apiFetch(`${API_BASE_URL}/${w.id}/transactions`)
        .then(r => (r.ok ? r.json() : []))
        .catch(() => [])
    );
    const results = await Promise.all(txPromises);
    const flattened = results.flat();
    if (flattened.length > 0 || allTransactions.value.length === 0) {
      flattened.sort((a, b) => new Date(b.transactionDate || b.createdAt) - new Date(a.transactionDate || a.createdAt));
      allTransactions.value = flattened;
      saveLocalTx(flattened);
    }
  } catch (err) {}
};

// Ghi Thu / Chi 1 chạm (Cập nhật tức thì 0ms)
const handleQuickSubmit = async () => {
  const amount = parsedQuickAmount.value;
  if (!amount || amount <= 0) {
    alert('Vui lòng nhập số tiền hợp lệ!');
    return;
  }

  await ensureDefaultWallet();
  const targetWalletId = quickForm.value.walletId || wallets.value[0]?.id;
  const signedAmount = quickForm.value.type === 'EXPENSE' ? -Math.abs(amount) : Math.abs(amount);
  const category = quickForm.value.type === 'EXPENSE' ? quickForm.value.category : quickForm.value.incomeCategory;
  const note = quickForm.value.note.trim();

  const newTx = {
    id: typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'tx_' + Date.now(),
    walletId: targetWalletId,
    amount: signedAmount,
    category,
    note,
    transactionDate: new Date().toISOString(),
    createdAt: new Date().toISOString(),
  };

  // 1. Cập nhật ngay lập tức trên UI và LocalStorage
  allTransactions.value.unshift(newTx);
  saveLocalTx(allTransactions.value);

  const wIdx = wallets.value.findIndex(w => w.id === targetWalletId);
  if (wIdx !== -1) {
    wallets.value[wIdx].balance = (Number(wallets.value[wIdx].balance) || 0) + signedAmount;
    saveLocalWallets(wallets.value);
  }

  // Xóa ô nhập để sẵn sàng nhập khoản tiếp theo
  quickForm.value.rawAmount = '';
  quickForm.value.note = '';

  // 2. Đồng bộ ngầm lên Server
  apiFetch(`${API_BASE_URL}/${targetWalletId}/transactions`, {
    method: 'POST',
    body: JSON.stringify({
      amount: signedAmount,
      category,
      note,
      transactionDate: newTx.transactionDate,
      deductFromMaster: false,
    }),
  })
    .then(async res => {
      if (res.ok) {
        const saved = await res.json();
        const idx = allTransactions.value.findIndex(t => t.id === newTx.id);
        if (idx !== -1) {
          allTransactions.value[idx].id = saved.id;
          saveLocalTx(allTransactions.value);
        }
      }
    })
    .catch(() => {});
};

// Chọn nhanh ví và đưa con trỏ vào ô nhập tiền
const prefillQuickForm = (walletId, type) => {
  quickForm.value.walletId = walletId;
  quickForm.value.type = type;
  if (quickAmountInputRef.value) {
    quickAmountInputRef.value.focus();
  }
};

// Tạo ví mới gọn nhẹ
const handleCreateWallet = async () => {
  const name = newWalletForm.value.name.trim();
  if (!name) return;

  const initAmt = parseSmartAmount(newWalletForm.value.initialBalance);
  const tempId = typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'wallet_' + Date.now();

  const newWallet = {
    id: tempId,
    name,
    balance: initAmt,
    monthlyBudget: 0,
    monthlySpent: 0,
    isMaster: false,
    createdAt: new Date().toISOString(),
  };

  wallets.value.push(newWallet);
  saveLocalWallets(wallets.value);
  quickForm.value.walletId = newWallet.id;

  newWalletForm.value.name = '';
  newWalletForm.value.initialBalance = '';
  showNewWalletBox.value = false;

  try {
    const res = await apiFetch(API_BASE_URL, {
      method: 'POST',
      body: JSON.stringify({ name, monthlyBudget: 0, isMaster: false }),
    });
    if (res.ok) {
      const created = await res.json();
      const idx = wallets.value.findIndex(w => w.id === tempId);
      if (idx !== -1) {
        wallets.value[idx].id = created.id;
        quickForm.value.walletId = created.id;
        saveLocalWallets(wallets.value);
      }

      if (initAmt > 0) {
        await apiFetch(`${API_BASE_URL}/${created.id}/transactions`, {
          method: 'POST',
          body: JSON.stringify({
            amount: initAmt,
            category: 'Số dư đầu kỳ',
            note: 'Số dư ban đầu',
            transactionDate: new Date().toISOString(),
          }),
        });
        await fetchAllTransactions();
      }
    }
  } catch (e) {}
};

// Xóa giao dịch nhập nhầm
const handleDeleteTransaction = async (tx) => {
  if (!confirm(`Xóa giao dịch "${tx.note || tx.category}" (${formatVND(Math.abs(tx.amount))})?`)) return;

  allTransactions.value = allTransactions.value.filter(t => t.id !== tx.id);
  saveLocalTx(allTransactions.value);

  const wIdx = wallets.value.findIndex(w => w.id === tx.walletId);
  if (wIdx !== -1) {
    wallets.value[wIdx].balance = (Number(wallets.value[wIdx].balance) || 0) - (Number(tx.amount) || 0);
    saveLocalWallets(wallets.value);
  }

  apiFetch(`${API_BASE_URL}/transactions/${tx.id}`, { method: 'DELETE' }).catch(() => {});
};

// Xóa ví
const handleDeleteWallet = async (wallet) => {
  if (!confirm(`Bạn có chắc muốn xóa ví "${wallet.name}"?`)) return;

  wallets.value = wallets.value.filter(w => w.id !== wallet.id);
  allTransactions.value = allTransactions.value.filter(t => t.walletId !== wallet.id);
  saveLocalWallets(wallets.value);
  saveLocalTx(allTransactions.value);

  if (selectedWalletFilterId.value === wallet.id) {
    selectedWalletFilterId.value = 'ALL';
  }
  if (quickForm.value.walletId === wallet.id) {
    quickForm.value.walletId = wallets.value[0]?.id || '';
  }

  apiFetch(`${API_BASE_URL}/${wallet.id}`, { method: 'DELETE' }).catch(() => {});
};

// Làm mới toàn bộ dữ liệu ví
const confirmResetAllData = async () => {
  if (!confirm('Bạn có chắc muốn xóa toàn bộ lịch sử thu chi và đặt lại ví từ đầu không?')) return;

  wallets.value = [];
  allTransactions.value = [];
  saveLocalWallets([]);
  saveLocalTx([]);

  await apiFetch('/wallets/reset-all', { method: 'DELETE' }).catch(() => {});
  await ensureDefaultWallet();
};

const handleDataUpdated = (e) => {
  if (e.detail?.action === 'transaction') {
    fetchWallets();
  }
};

onMounted(async () => {
  await ensureDefaultWallet();
  await fetchWallets();
  window.addEventListener('secondbrain-data-updated', handleDataUpdated);
});

onUnmounted(() => {
  window.removeEventListener('secondbrain-data-updated', handleDataUpdated);
});
</script>
