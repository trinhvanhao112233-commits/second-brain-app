<template>
  <div class="space-y-6">
    <!-- Top Action Bar (Material Design 3 Style) -->
    <header class="bg-[#171c24] border border-[#262a34] rounded-2xl px-5 py-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 md-elevation-1">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center md-elevation-1">
          <span class="material-symbols-rounded text-2xl">account_balance_wallet</span>
        </div>
        <div>
          <h2 class="text-lg font-bold text-white tracking-wide">Quản Lý Ví & Chi Tiêu</h2>
          <p class="text-xs text-[#8b9198]">Hạn mức ngân sách tháng & giao dịch thực tế</p>
        </div>
      </div>

      <!-- Quick Action Buttons -->
      <div class="flex items-center gap-2">
        <button
          type="button"
          @click="openAddExpenseModal()"
          class="flex items-center gap-1.5 px-4 py-2 rounded-full bg-[#f43f5e] hover:bg-[#fb7185] text-white font-bold text-xs transition-all active:scale-95 md-elevation-1"
        >
          <span class="material-symbols-rounded text-base">remove</span>
          <span>Ghi Khoản Chi</span>
        </button>

        <button
          type="button"
          @click="openBudgetModal()"
          class="flex items-center gap-1.5 px-4 py-2 rounded-full bg-[#1c2029] hover:bg-[#262a34] border border-[#262a34] text-[#5dfec1] font-bold text-xs transition-all active:scale-95"
        >
          <span class="material-symbols-rounded text-base">track_changes</span>
          <span>Hạn Mức Ngân Sách</span>
        </button>
      </div>
    </header>

    <!-- 1. TỔNG QUAN NGÂN SÁCH (MATERIAL SURFACE CONTAINER) -->
    <section class="bg-[#171c24] border border-[#262a34] rounded-2xl p-5 sm:p-6 md-elevation-1 space-y-5">
      <!-- Title & Bộ lọc ví -->
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-[#262a34]">
        <div>
          <div class="flex items-center gap-2 text-xs font-semibold text-[#8b9198]">
            <span class="material-symbols-rounded text-sm text-[#38e1a6]">calendar_today</span>
            <span>Ngân sách Tháng {{ new Date().getMonth() + 1 }}/{{ new Date().getFullYear() }}</span>
          </div>
          <h3 class="text-xl font-bold text-white mt-0.5">
            {{ selectedWallet ? `Ví: ${selectedWallet.name}` : 'Tất cả các ví' }}
          </h3>
        </div>

        <div class="flex items-center gap-2">
          <span class="text-xs text-[#8b9198]">Bộ lọc ví:</span>
          <select
            v-model="selectedWalletFilterId"
            class="bg-[#1c2029] border border-[#41474d] text-xs text-[#e1e2ec] font-semibold rounded-xl px-3 py-1.5 focus:outline-none focus:border-[#38e1a6]"
          >
            <option value="ALL">Tất cả ví (Tổng hợp)</option>
            <option v-for="w in wallets" :key="w.id" :value="w.id">{{ w.name }}</option>
          </select>
        </div>
      </div>

      <!-- 4 Ô Thống Kê Số Liệu (MD3 Surface Cards) -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
        <!-- 1. Tổng ngân sách -->
        <div class="p-4 rounded-2xl bg-[#1c2029] border border-[#262a34]">
          <div class="flex items-center justify-between text-[#8b9198] mb-1">
            <span class="text-[11px] font-semibold uppercase tracking-wider">Hạn Mức Tháng</span>
            <span class="material-symbols-rounded text-base text-[#38e1a6]">track_changes</span>
          </div>
          <div class="text-xl font-black text-white font-mono">
            {{ formatVND(currentOverview.monthlyBudget) }}
          </div>
          <span class="text-[11px] text-[#8b9198] mt-1 block">Ngân sách tối đa</span>
        </div>

        <!-- 2. Đã tiêu -->
        <div class="p-4 rounded-2xl bg-[#1c2029] border border-[#262a34]">
          <div class="flex items-center justify-between text-[#8b9198] mb-1">
            <span class="text-[11px] font-semibold uppercase tracking-wider">Đã Tiêu Tháng</span>
            <span class="material-symbols-rounded text-base text-[#f43f5e]">trending_down</span>
          </div>
          <div class="text-xl font-black text-[#f43f5e] font-mono">
            {{ formatVND(currentOverview.monthlySpent) }}
          </div>
          <span class="text-[11px] text-[#8b9198] mt-1 block">
            {{ currentOverview.budgetPercentage }}% ngân sách
          </span>
        </div>

        <!-- 3. Hạn mức còn lại -->
        <div class="p-4 rounded-2xl bg-[#1c2029] border border-[#262a34]">
          <div class="flex items-center justify-between text-[#8b9198] mb-1">
            <span class="text-[11px] font-semibold uppercase tracking-wider">Còn Được Chi</span>
            <span class="material-symbols-rounded text-base text-[#38e1a6]">verified_user</span>
          </div>
          <div
            class="text-xl font-black font-mono"
            :class="currentOverview.budgetRemaining >= 0 ? 'text-[#38e1a6]' : 'text-[#ffb4ab]'"
          >
            {{ formatVND(currentOverview.budgetRemaining) }}
          </div>
          <span class="text-[11px] text-[#8b9198] mt-1 block">
            {{ currentOverview.budgetRemaining >= 0 ? 'Trong hạn mức' : 'Đã vượt định mức' }}
          </span>
        </div>

        <!-- 4. Số dư thực tế -->
        <div class="p-4 rounded-2xl bg-[#1c2029] border border-[#262a34]">
          <div class="flex items-center justify-between text-[#8b9198] mb-1">
            <span class="text-[11px] font-semibold uppercase tracking-wider">Số Dư Thực Tế</span>
            <span class="material-symbols-rounded text-base text-[#5dfec1]">account_balance</span>
          </div>
          <div class="text-xl font-black text-[#5dfec1] font-mono">
            {{ formatVND(currentOverview.balance) }}
          </div>
          <span class="text-[11px] text-[#8b9198] mt-1 block">Tiền hiện có trong ví</span>
        </div>
      </div>

      <!-- Thanh tiến độ ngân sách (Linear Progress Bar) -->
      <div class="space-y-2 pt-1">
        <div class="flex items-center justify-between text-xs font-semibold">
          <span class="text-[#c1c7ce] flex items-center gap-2">
            <span>Tiến độ tiêu hao:</span>
            <span
              class="px-2 py-0.5 rounded-full text-[10px] font-bold"
              :class="getBudgetBadgeClass(currentOverview.budgetPercentage)"
            >
              {{ currentOverview.budgetPercentage }}%
            </span>
          </span>
          <span class="text-[11px] font-mono text-[#8b9198]">
            {{ formatVND(currentOverview.monthlySpent) }} / {{ formatVND(currentOverview.monthlyBudget) }}
          </span>
        </div>

        <div class="w-full h-2.5 bg-[#1c2029] rounded-full overflow-hidden border border-[#262a34]">
          <div
            class="h-full rounded-full transition-all duration-500 ease-out"
            :class="getProgressBarColor(currentOverview.budgetPercentage)"
            :style="{ width: `${Math.min(currentOverview.budgetPercentage, 100)}%` }"
          ></div>
        </div>
      </div>
    </section>

    <!-- 2. DANH SÁCH CÁC VÍ TIỀN (MD3 CARDS) -->
    <section class="space-y-3">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="material-symbols-rounded text-[#38e1a6] text-lg">credit_card</span>
          <h3 class="text-sm font-bold uppercase tracking-wider text-white">Danh Sách Ví Tiền</h3>
        </div>
        <span class="text-xs text-[#8b9198]">{{ wallets.length }} ví đang hoạt động</span>
      </div>

      <!-- Grid Cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <!-- Wallet Card -->
        <article
          v-for="wallet in wallets"
          :key="wallet.id"
          class="bg-[#171c24] border border-[#262a34] hover:border-[#41474d] rounded-2xl p-5 md-elevation-1 transition-all flex flex-col justify-between"
        >
          <!-- Top Wallet Card -->
          <div>
            <div class="flex items-center justify-between mb-2">
              <div class="flex items-center gap-2">
                <div class="w-8 h-8 rounded-xl bg-[#1c2029] text-[#5dfec1] flex items-center justify-center border border-[#262a34]">
                  <span class="material-symbols-rounded text-base">savings</span>
                </div>
                <h4 class="font-bold text-base text-white truncate">{{ wallet.name }}</h4>
              </div>

              <div class="flex items-center gap-1.5">
                <!-- Nút chỉnh sửa hạn mức -->
                <button
                  type="button"
                  @click.stop="openBudgetModal(wallet)"
                  class="px-2 py-1 rounded-lg bg-[#1c2029] hover:bg-[#262a34] text-[11px] font-mono font-semibold text-[#8b9198] hover:text-[#5dfec1] border border-[#262a34] flex items-center gap-1 transition-colors"
                  title="Sửa hạn mức ví này"
                >
                  <span>NS: {{ formatQuick(wallet.monthlyBudget || 0) }}</span>
                  <span class="material-symbols-rounded text-xs">edit</span>
                </button>

                <!-- Nút Xóa / Hủy Ví -->
                <button
                  type="button"
                  @click.stop="handleDeleteWallet(wallet)"
                  class="w-7 h-7 rounded-lg bg-[#1c2029] hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 border border-[#262a34] flex items-center justify-center transition-colors"
                  title="Hủy ví này"
                >
                  <span class="material-symbols-rounded text-sm">delete</span>
                </button>
              </div>
            </div>

            <!-- Balance Number -->
            <div class="mt-3">
              <span class="text-[10px] text-[#8b9198] uppercase tracking-wider font-semibold">Số dư khả dụng</span>
              <div class="text-2xl font-black text-[#38e1a6] font-mono mt-0.5">
                {{ formatVND(wallet.balance) }}
              </div>
            </div>

            <!-- Tháng này đã tiêu -->
            <div class="mt-3 pt-3 border-t border-[#262a34] text-xs space-y-1">
              <div class="flex justify-between items-center text-[11px]">
                <span class="text-[#8b9198]">Đã chi tháng này:</span>
                <span class="font-mono font-bold text-[#ffb4ab]">{{ formatVND(wallet.monthlySpent || 0) }}</span>
              </div>
              <div class="w-full h-1.5 bg-[#1c2029] rounded-full overflow-hidden border border-[#262a34]/60">
                <div
                  class="h-full rounded-full bg-[#f43f5e] transition-all"
                  :style="{ width: `${calcBudgetPercent(wallet.monthlySpent, wallet.monthlyBudget)}%` }"
                ></div>
              </div>
            </div>
          </div>

          <!-- Bottom Action Buttons -->
          <div class="grid grid-cols-2 gap-2 mt-4 pt-3 border-t border-[#262a34]">
            <button
              type="button"
              @click="openAddExpenseModal(wallet)"
              class="flex items-center justify-center gap-1 py-2 px-3 rounded-xl bg-[#1c2029] hover:bg-[#262a34] text-[#ffb4ab] hover:text-white border border-[#262a34] text-xs font-bold transition-all"
            >
              <span class="material-symbols-rounded text-sm">remove</span>
              <span>Chi tiêu</span>
            </button>
            <button
              type="button"
              @click="openTransactionModal(wallet, 'deposit')"
              class="flex items-center justify-center gap-1 py-2 px-3 rounded-xl bg-[#005237] hover:bg-[#006947] text-[#5dfec1] text-xs font-bold transition-all"
            >
              <span class="material-symbols-rounded text-sm">add</span>
              <span>Nạp tiền</span>
            </button>
          </div>
        </article>

        <!-- Nút Tạo Ví Mới -->
        <button
          type="button"
          @click="showAddWalletModal = true"
          class="min-h-[190px] rounded-2xl border border-dashed border-[#262a34] hover:border-[#38e1a6]/60 bg-[#171c24]/50 hover:bg-[#171c24] transition-all flex flex-col items-center justify-center gap-2 text-[#8b9198] hover:text-[#5dfec1] group p-6"
        >
          <div class="w-10 h-10 rounded-2xl bg-[#1c2029] group-hover:bg-[#005237] group-hover:text-[#5dfec1] flex items-center justify-center transition-colors">
            <span class="material-symbols-rounded text-xl">add_card</span>
          </div>
          <span class="font-bold text-xs tracking-wide">+ Thêm ví mới</span>
        </button>
      </div>
    </section>

    <!-- 3. LỊCH SỬ CHI TIÊU & GIAO DỊCH (MD3 TABLE / LIST) -->
    <section class="bg-[#171c24] border border-[#262a34] rounded-2xl p-5 md-elevation-1 space-y-4">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-3 border-b border-[#262a34]">
        <div class="flex items-center gap-2">
          <span class="material-symbols-rounded text-[#38e1a6] text-lg">receipt_long</span>
          <h3 class="text-sm font-bold uppercase tracking-wider text-white">Lịch Sử Giao Dịch</h3>
          <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded-full bg-[#1c2029] text-[#cee9da]">
            {{ filteredTransactions.length }}
          </span>
        </div>

        <!-- Filter Danh Mục -->
        <div class="flex items-center gap-2">
          <span class="text-xs text-[#8b9198]">Danh mục:</span>
          <select
            v-model="categoryFilter"
            class="bg-[#1c2029] border border-[#41474d] text-xs text-[#e1e2ec] font-semibold rounded-xl px-3 py-1.5 focus:outline-none focus:border-[#38e1a6]"
          >
            <option value="ALL">Tất cả danh mục</option>
            <option v-for="cat in expenseCategories" :key="cat" :value="cat">{{ cat }}</option>
            <option value="Nạp tiền">Nạp tiền</option>
          </select>
        </div>
      </div>

      <!-- Empty state -->
      <div v-if="filteredTransactions.length === 0" class="py-12 text-center text-[#8b9198] text-xs">
        <span class="material-symbols-rounded text-3xl mb-1 text-[#41474d]">receipt</span>
        <p>Chưa có khoản chi tiêu hay giao dịch nào được ghi nhận.</p>
        <button
          type="button"
          @click="openAddExpenseModal()"
          class="mt-2 text-xs font-bold text-[#5dfec1] hover:underline"
        >
          + Ghi lại khoản chi đầu tiên ngay
        </button>
      </div>

      <!-- Transaction Rows -->
      <div v-else class="divide-y divide-[#262a34]">
        <div
          v-for="tx in filteredTransactions"
          :key="tx.id"
          class="py-3 px-2 flex items-center justify-between hover:bg-[#1c2029]/50 transition-colors rounded-xl"
        >
          <div class="flex items-center gap-3 min-w-0">
            <!-- Icon Avatar -->
            <div
              class="w-9 h-9 rounded-xl flex items-center justify-center shrink-0 border"
              :class="tx.amount < 0 ? 'bg-[#ffb4ab]/10 text-[#ffb4ab] border-[#ffb4ab]/20' : 'bg-[#005237]/40 text-[#5dfec1] border-[#38e1a6]/30'"
            >
              <span class="material-symbols-rounded text-lg">
                {{ tx.amount < 0 ? getCategorySymbol(tx.category) : 'add_circle' }}
              </span>
            </div>

            <!-- Info -->
            <div class="min-w-0">
              <div class="flex items-center gap-2">
                <span class="font-bold text-xs text-white truncate">
                  {{ tx.note || tx.category || 'Giao dịch' }}
                </span>
                <span
                  class="text-[10px] px-1.5 py-0.2 rounded font-semibold shrink-0"
                  :class="tx.amount < 0 ? 'bg-[#262a34] text-[#cee9da]' : 'bg-[#005237] text-[#5dfec1]'"
                >
                  {{ tx.category || 'Khác' }}
                </span>
              </div>
              <div class="flex items-center gap-2 text-[11px] text-[#8b9198] mt-0.5 font-mono">
                <span>{{ formatDateTime(tx.transactionDate || tx.createdAt) }}</span>
                <span>•</span>
                <span class="text-[#c1c7ce]">Ví: {{ getWalletName(tx.walletId) }}</span>
              </div>
            </div>
          </div>

          <!-- Amount -->
          <div class="text-right shrink-0 ml-3">
            <span
              class="text-sm sm:text-base font-bold font-mono tracking-tight block"
              :class="tx.amount < 0 ? 'text-[#ffb4ab]' : 'text-[#38e1a6]'"
            >
              {{ tx.amount < 0 ? '-' : '+' }}{{ formatVND(Math.abs(tx.amount)) }}
            </span>
            <span class="text-[10px] text-[#8b9198] uppercase">
              {{ tx.amount < 0 ? 'Chi tiêu' : 'Nạp tiền' }}
            </span>
          </div>
        </div>
      </div>
    </section>

    <!-- ==================== MODAL 1: THIẾT LẬP NGÂN SÁCH (MD3 DIALOG) ==================== -->
    <div v-if="showBudgetModal" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-md bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <div class="flex items-center gap-3.5 mb-2">
          <div class="w-11 h-11 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-2xl">track_changes</span>
          </div>
          <div>
            <h3 class="text-lg font-bold text-[#e1e2ec]">Thiết Lập Ngân Sách Tháng</h3>
            <p class="text-xs text-[#8b9198]">Hạn mức chi tiêu tối đa trong 1 tháng</p>
          </div>
        </div>

        <form @submit.prevent="handleSaveBudget" class="mt-4 space-y-4">
          <!-- Chọn ví -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1.5 flex items-center gap-1.5">
              <span class="material-symbols-rounded text-sm text-[#38e1a6]">account_balance_wallet</span>
              Áp dụng cho Ví
            </label>
            <select
              v-model="budgetForm.walletId"
              class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-2xl text-[#e1e2ec] text-sm focus:outline-none focus:border-[#38e1a6]"
            >
              <option value="" disabled>-- Vui lòng chọn ví --</option>
              <option v-for="w in wallets" :key="w.id" :value="w.id">
                {{ w.name }} (Ngân sách hiện tại: {{ formatVND(w.monthlyBudget || 0) }})
              </option>
            </select>
          </div>

          <!-- Nhập số tiền -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1.5 flex items-center gap-1.5">
              <span class="material-symbols-rounded text-sm text-[#38e1a6]">payments</span>
              Tổng ngân sách trong tháng (VNĐ)
            </label>
            <div class="relative">
              <input
                v-model.number="budgetForm.monthlyBudget"
                type="number"
                min="0"
                step="any"
                required
                placeholder="5000000"
                class="w-full px-4 py-3 bg-[#171c24] border border-[#41474d] rounded-2xl text-white font-mono text-lg font-bold placeholder-slate-600 focus:outline-none focus:border-[#38e1a6]"
              />
              <span class="absolute right-4 top-3 text-xs font-bold text-[#8b9198]">VNĐ</span>
            </div>
          </div>

          <!-- Filter Chips gợi ý nhanh -->
          <div>
            <label class="block text-[11px] font-semibold text-[#8b9198] mb-1.5">Gợi ý nhanh:</label>
            <div class="grid grid-cols-4 gap-2">
              <button
                v-for="amt in [2000000, 5000000, 10000000, 20000000]"
                :key="amt"
                type="button"
                @click="budgetForm.monthlyBudget = amt"
                class="py-1.5 px-1.5 rounded-full text-xs font-medium text-center transition-all"
                :class="budgetForm.monthlyBudget === amt 
                  ? 'bg-[#005237] text-[#5dfec1] font-bold border border-[#38e1a6]/40' 
                  : 'bg-[#262a34] text-[#c1c7ce] hover:bg-[#31353f] hover:text-white'"
              >
                {{ formatQuick(amt) }}
              </button>
            </div>
          </div>

          <!-- Dialog Actions -->
          <div class="flex items-center justify-end gap-3 pt-3 border-t border-[#262a34]">
            <button
              type="button"
              @click="showBudgetModal = false"
              class="px-4 py-2 rounded-full text-xs font-semibold text-[#c1c7ce] hover:bg-[#262a34]"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmitting || !budgetForm.walletId"
              class="flex items-center gap-1.5 px-5 py-2 bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] rounded-full text-xs font-bold transition-all disabled:opacity-40"
            >
              <span class="material-symbols-rounded text-base">{{ isSubmitting ? 'sync' : 'check' }}</span>
              <span>{{ isSubmitting ? 'Đang lưu...' : 'Lưu Ngân Sách' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ==================== MODAL 2: GHI KHOẢN CHI TIÊU (MD3 DIALOG) ==================== -->
    <div v-if="showExpenseModal" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-md bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <div class="flex items-center gap-3 mb-2">
          <div class="w-11 h-11 rounded-2xl bg-[#ffb4ab]/15 text-[#ffb4ab] flex items-center justify-center">
            <span class="material-symbols-rounded text-2xl">trending_down</span>
          </div>
          <div>
            <h3 class="text-lg font-bold text-white">Ghi Nhận Khoản Chi Tiêu</h3>
            <p class="text-xs text-[#8b9198]">Cập nhật tức thì vào hạn mức ngân sách</p>
          </div>
        </div>

        <form @submit.prevent="handleCreateExpense" class="mt-4 space-y-3.5">
          <!-- 1. Chọn ví -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Trừ từ ví</label>
            <select
              v-model="expenseForm.walletId"
              required
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            >
              <option v-for="w in wallets" :key="w.id" :value="w.id">
                {{ w.name }} (Số dư: {{ formatVND(w.balance) }})
              </option>
            </select>
          </div>

          <!-- 2. Số tiền -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Số tiền chi (VNĐ)</label>
            <div class="relative">
              <input
                v-model.number="expenseForm.amount"
                type="number"
                min="0"
                step="any"
                required
                placeholder="50000"
                class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white font-mono text-base font-bold placeholder-slate-600 focus:outline-none focus:border-[#38e1a6]"
              />
              <span class="absolute right-4 top-2.5 text-xs font-bold text-[#8b9198]">VNĐ</span>
            </div>
          </div>

          <!-- Gợi ý nhanh -->
          <div class="grid grid-cols-4 gap-1.5">
            <button
              v-for="amt in [30000, 50000, 100000, 200000]"
              :key="amt"
              type="button"
              @click="expenseForm.amount = amt"
              class="py-1 px-1 bg-[#262a34] hover:bg-[#31353f] text-[11px] font-semibold text-[#c1c7ce] rounded-lg text-center transition-all"
            >
              {{ formatQuick(amt) }}
            </button>
          </div>

          <!-- 4. Ghi chú -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Ghi chú</label>
            <input
              v-model="expenseForm.note"
              type="text"
              placeholder="Ghi chú chi tiêu (tùy chọn)..."
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <!-- 5. Ngày giao dịch -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Ngày chi</label>
            <input
              v-model="expenseForm.transactionDate"
              type="date"
              required
              class="w-full px-3 py-1.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <div class="flex items-center justify-end gap-2.5 pt-3 border-t border-[#262a34]">
            <button
              type="button"
              @click="showExpenseModal = false"
              class="px-4 py-2 rounded-full text-xs font-semibold text-[#c1c7ce] hover:bg-[#262a34]"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmitting || !expenseForm.amount || expenseForm.amount <= 0"
              class="px-5 py-2 bg-[#f43f5e] hover:bg-[#fb7185] text-white rounded-full text-xs font-bold transition-all disabled:opacity-40"
            >
              {{ isSubmitting ? 'Đang lưu...' : 'Ghi Nhận Khoản Chi' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ==================== MODAL 3: TẠO VÍ MỚI (MD3 DIALOG) ==================== -->
    <div v-if="showAddWalletModal" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-sm bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <div class="flex items-center gap-3 mb-3">
          <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-xl">add_card</span>
          </div>
          <div>
            <h3 class="text-base font-bold text-white">Tạo Ví Tài Chính Mới</h3>
            <p class="text-xs text-[#8b9198]">Thêm nguồn tiền mới để theo dõi</p>
          </div>
        </div>

        <form @submit.prevent="handleCreateWallet" class="space-y-3.5">
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Tên ví</label>
            <input
              v-model="newWalletName"
              type="text"
              required
              placeholder="Nhập tên ví (tiền mặt, thẻ ngân hàng...)"
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Hạn mức ngân sách tháng (tùy chọn)</label>
            <input
              v-model.number="newWalletBudget"
              type="number"
              min="0"
              step="any"
              placeholder="0"
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white font-mono text-xs focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <div class="flex items-center justify-end gap-2 pt-2 border-t border-[#262a34]">
            <button
              type="button"
              @click="showAddWalletModal = false"
              class="px-4 py-2 rounded-full text-xs font-semibold text-[#c1c7ce] hover:bg-[#262a34]"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmitting || !newWalletName.trim()"
              class="px-5 py-2 bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] rounded-full text-xs font-bold transition-all disabled:opacity-40"
            >
              {{ isSubmitting ? 'Đang tạo...' : 'Tạo Ví' }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- ==================== MODAL 4: NẠP TIỀN VÀO VÍ ==================== -->
    <div v-if="activeTxWallet" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-sm bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <div class="flex items-center gap-3 mb-2">
          <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-xl">add</span>
          </div>
          <div>
            <h3 class="text-base font-bold text-white truncate">Nạp tiền vào "{{ activeTxWallet.name }}"</h3>
            <p class="text-xs text-[#8b9198]">
              Số dư hiện tại: <span class="text-white font-mono">{{ formatVND(activeTxWallet.balance) }}</span>
            </p>
          </div>
        </div>

        <form @submit.prevent="handleDepositSubmit" class="mt-4 space-y-3">
          <div class="relative">
            <input
              v-model.number="txAmountInput"
              type="number"
              min="0"
              step="any"
              required
              placeholder="0"
              class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white font-mono text-lg font-bold placeholder-slate-600 focus:outline-none focus:border-[#38e1a6]"
            />
            <span class="absolute right-4 top-2.5 text-xs font-bold text-[#8b9198]">VNĐ</span>
          </div>

          <div class="grid grid-cols-4 gap-1.5">
            <button
              v-for="amt in quickAmounts"
              :key="amt"
              type="button"
              @click="txAmountInput = amt"
              class="py-1 px-1 bg-[#262a34] hover:bg-[#31353f] text-[11px] font-semibold text-[#c1c7ce] rounded-lg text-center transition-all"
            >
              {{ formatQuick(amt) }}
            </button>
          </div>

          <div class="flex items-center justify-end gap-2 pt-2 border-t border-[#262a34]">
            <button
              type="button"
              @click="activeTxWallet = null"
              class="px-4 py-2 rounded-full text-xs font-semibold text-[#c1c7ce] hover:bg-[#262a34]"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmitting || !txAmountInput || txAmountInput <= 0"
              class="px-5 py-2 bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] rounded-full text-xs font-bold transition-all disabled:opacity-40"
            >
              {{ isSubmitting ? 'Đang nạp...' : 'Xác nhận Nạp' }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue';
import { apiFetch } from '../services/api';

const API_BASE_URL = '/wallets';

// State chính
const wallets = ref([]);
const allTransactions = ref([]);
const isLoading = ref(true);
const isSubmitting = ref(false);

// Filter
const selectedWalletFilterId = ref('ALL');
const categoryFilter = ref('ALL');

// Danh mục chi tiêu chuẩn
const expenseCategories = [
  'Ăn uống',
  'Đi lại',
  'Mua sắm',
  'Hóa đơn',
  'Giải trí',
  'Y tế',
];

// Helper biểu tượng Material Symbols chuẩn
const getCategorySymbol = (category) => {
  switch (category) {
    case 'Ăn uống': return 'restaurant';
    case 'Đi lại': return 'directions_bike';
    case 'Mua sắm': return 'shopping_bag';
    case 'Hóa đơn': return 'receipt';
    case 'Giải trí': return 'movie';
    case 'Y tế': return 'medication';
    case 'Nạp tiền': return 'attach_money';
    default: return 'label';
  }
};

// Modal Ngân Sách
const showBudgetModal = ref(false);
const budgetForm = ref({
  walletId: '',
  monthlyBudget: 5000000,
});

// Modal Thêm Chi Tiêu
const showExpenseModal = ref(false);
const expenseForm = ref({
  walletId: '',
  amount: null,
  category: 'Ăn uống',
  note: '',
  transactionDate: new Date().toISOString().split('T')[0],
});

// Modal Thêm Ví
const showAddWalletModal = ref(false);
const newWalletName = ref('');
const newWalletBudget = ref(0);

// Modal Nạp tiền
const activeTxWallet = ref(null);
const txAmountInput = ref(null);
const quickAmounts = [100000, 200000, 500000, 2000000];

// Formats
const formatVND = (value) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0
  }).format(value || 0);
};

const formatQuick = (num) => {
  if (!num) return '0đ';
  if (num >= 1000000) return `${(num / 1000000).toFixed(num % 1000000 === 0 ? 0 : 1)}M`;
  return `${Math.round(num / 1000)}k`;
};

const formatDateTime = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
};

const calcBudgetPercent = (spent, budget) => {
  if (!budget || budget <= 0) return spent > 0 ? 100 : 0;
  return Math.min(Math.round(((spent || 0) / budget) * 100), 100);
};

const getProgressBarColor = (percentage) => {
  if (percentage >= 100) return 'bg-[#f43f5e]';
  if (percentage >= 80) return 'bg-[#f59e0b]';
  return 'bg-[#38e1a6]';
};

const getBudgetBadgeClass = (percentage) => {
  if (percentage >= 100) return 'bg-[#ffb4ab]/20 text-[#ffb4ab] border border-[#ffb4ab]/30';
  if (percentage >= 80) return 'bg-[#f59e0b]/20 text-amber-300 border border-amber-500/30';
  return 'bg-[#005237] text-[#5dfec1] border border-[#38e1a6]/30';
};

// Computed Overview
const selectedWallet = computed(() => {
  if (selectedWalletFilterId.value === 'ALL') return null;
  return wallets.value.find(w => w.id === selectedWalletFilterId.value) || null;
});

const currentOverview = computed(() => {
  if (selectedWallet.value) {
    const budget = Number(selectedWallet.value.monthlyBudget) || 0;
    const spent = Number(selectedWallet.value.monthlySpent) || 0;
    const balance = Number(selectedWallet.value.balance) || 0;
    const remaining = budget - spent;
    const pct = budget > 0 ? Math.round((spent / budget) * 100) : (spent > 0 ? 100 : 0);

    return {
      monthlyBudget: budget,
      monthlySpent: spent,
      budgetRemaining: remaining,
      balance,
      budgetPercentage: pct,
    };
  }

  // Tổng hợp tất cả ví
  const totalBudget = wallets.value.reduce((sum, w) => sum + (Number(w.monthlyBudget) || 0), 0);
  const totalSpent = wallets.value.reduce((sum, w) => sum + (Number(w.monthlySpent) || 0), 0);
  const totalBalance = wallets.value.reduce((sum, w) => sum + (Number(w.balance) || 0), 0);
  const totalRemaining = totalBudget - totalSpent;
  const totalPct = totalBudget > 0 ? Math.round((totalSpent / totalBudget) * 100) : (totalSpent > 0 ? 100 : 0);

  return {
    monthlyBudget: totalBudget,
    monthlySpent: totalSpent,
    budgetRemaining: totalRemaining,
    balance: totalBalance,
    budgetPercentage: totalPct,
  };
});

const getWalletName = (walletId) => {
  const w = wallets.value.find(x => x.id === walletId);
  return w ? w.name : 'Chung';
};

// Transactions Lọc
const filteredTransactions = computed(() => {
  let list = allTransactions.value;

  if (selectedWalletFilterId.value !== 'ALL') {
    list = list.filter(t => t.walletId === selectedWalletFilterId.value);
  }

  if (categoryFilter.value !== 'ALL') {
    if (categoryFilter.value === 'Nạp tiền') {
      list = list.filter(t => t.amount > 0);
    } else {
      list = list.filter(t => t.category === categoryFilter.value);
    }
  }

  return list;
});

// API Calls
const fetchWallets = async () => {
  isLoading.value = true;
  try {
    const res = await apiFetch(API_BASE_URL);
    if (!res.ok) throw new Error('Không thể tải danh sách ví');
    wallets.value = await res.json();
    await fetchAllTransactions();
  } catch (err) {
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

const fetchAllTransactions = async () => {
  try {
    const txPromises = wallets.value.map(w =>
      apiFetch(`${API_BASE_URL}/${w.id}/transactions`)
        .then(r => r.ok ? r.json() : [])
        .catch(() => [])
    );
    const results = await Promise.all(txPromises);
    const flattened = results.flat();
    flattened.sort((a, b) => new Date(b.transactionDate || b.createdAt) - new Date(a.transactionDate || a.createdAt));
    allTransactions.value = flattened;
  } catch (err) {
    console.error('Lỗi khi tải lịch sử giao dịch:', err);
  }
};

// HANDLERS
const openBudgetModal = (wallet = null) => {
  if (wallet) {
    budgetForm.value.walletId = wallet.id;
    budgetForm.value.monthlyBudget = wallet.monthlyBudget || 5000000;
  } else if (wallets.value.length > 0) {
    budgetForm.value.walletId = wallets.value[0].id;
    budgetForm.value.monthlyBudget = wallets.value[0].monthlyBudget || 5000000;
  }
  showBudgetModal.value = true;
};

const handleSaveBudget = async () => {
  if (!budgetForm.value.walletId) {
    alert('Vui lòng chọn ví cần thiết lập ngân sách.');
    return;
  }

  isSubmitting.value = true;
  try {
    const res = await apiFetch(`${API_BASE_URL}/${budgetForm.value.walletId}/budget`, {
      method: 'PUT',
      body: JSON.stringify({ monthlyBudget: Number(budgetForm.value.monthlyBudget) }),
    });

    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.message || `Lỗi server (${res.status})`);
    }
    const updatedWallet = await res.json();

    const index = wallets.value.findIndex(w => w.id === updatedWallet.id);
    if (index !== -1) {
      wallets.value[index] = { ...wallets.value[index], ...updatedWallet };
    }
    showBudgetModal.value = false;
  } catch (err) {
    console.error('Lỗi lưu ngân sách:', err);
    alert(`Không thể lưu ngân sách: ${err.message || 'Lỗi không xác định'}.`);
  } finally {
    isSubmitting.value = false;
  }
};

const openAddExpenseModal = (wallet = null) => {
  if (wallet) {
    expenseForm.value.walletId = wallet.id;
  } else if (wallets.value.length > 0) {
    expenseForm.value.walletId = wallets.value[0].id;
  }
  expenseForm.value.amount = null;
  expenseForm.value.note = '';
  showExpenseModal.value = true;
};

const handleCreateExpense = async () => {
  if (!expenseForm.value.walletId || !expenseForm.value.amount) return;

  isSubmitting.value = true;
  try {
    const expenseAmount = -Math.abs(Number(expenseForm.value.amount));
    const res = await apiFetch(`${API_BASE_URL}/${expenseForm.value.walletId}/transactions`, {
      method: 'POST',
      body: JSON.stringify({
        amount: expenseAmount,
        category: 'Chi tiêu',
        note: expenseForm.value.note || '',
        transactionDate: expenseForm.value.transactionDate ? new Date(expenseForm.value.transactionDate).toISOString() : new Date().toISOString()
      }),
    });

    if (!res.ok) throw new Error('Không thể ghi nhận khoản chi');
    const newTx = await res.json();

    allTransactions.value.unshift(newTx);
    await fetchWallets();
    showExpenseModal.value = false;
  } catch (err) {
    console.error(err);
    alert('Đã xảy ra lỗi khi tạo khoản chi.');
  } finally {
    isSubmitting.value = false;
  }
};

const handleCreateWallet = async () => {
  if (!newWalletName.value.trim()) return;

  isSubmitting.value = true;
  try {
    const res = await apiFetch(API_BASE_URL, {
      method: 'POST',
      body: JSON.stringify({
        name: newWalletName.value.trim(),
        monthlyBudget: Number(newWalletBudget.value) || 0
      }),
    });

    if (!res.ok) throw new Error('Không thể tạo ví');
    const createdWallet = await res.json();
    wallets.value.unshift(createdWallet);

    newWalletName.value = '';
    newWalletBudget.value = 0;
    showAddWalletModal.value = false;
  } catch (err) {
    console.error(err);
    alert('Đã xảy ra lỗi khi tạo ví mới.');
  } finally {
    isSubmitting.value = false;
  }
};

const handleDeleteWallet = async (wallet) => {
  if (!wallet) return;
  const confirmed = window.confirm(`Bạn có chắc chắn muốn hủy ví "${wallet.name}" không?\nToàn bộ lịch sử giao dịch thuộc ví này cũng sẽ bị xóa vĩnh viễn.`);
  if (!confirmed) return;

  isSubmitting.value = true;
  try {
    const res = await apiFetch(`${API_BASE_URL}/${wallet.id}`, {
      method: 'DELETE',
    });
    if (!res.ok) throw new Error('Không thể hủy ví này.');

    wallets.value = wallets.value.filter(w => w.id !== wallet.id);
    allTransactions.value = allTransactions.value.filter(t => t.walletId !== wallet.id);
    if (selectedWalletFilterId.value === wallet.id) {
      selectedWalletFilterId.value = 'ALL';
    }
  } catch (err) {
    console.error(err);
    alert(`Lỗi khi hủy ví: ${err.message || 'Không xác định'}`);
  } finally {
    isSubmitting.value = false;
  }
};

const openTransactionModal = (wallet) => {
  activeTxWallet.value = wallet;
  txAmountInput.value = null;
};

const handleDepositSubmit = async () => {
  if (!activeTxWallet.value || !txAmountInput.value || txAmountInput.value <= 0) return;

  isSubmitting.value = true;
  try {
    const res = await apiFetch(`${API_BASE_URL}/${activeTxWallet.value.id}/transactions`, {
      method: 'POST',
      body: JSON.stringify({
        amount: Number(txAmountInput.value),
        category: 'Nạp tiền',
        note: 'Nạp tiền vào ví',
        transactionDate: new Date().toISOString()
      }),
    });

    if (!res.ok) throw new Error('Không thể nạp tiền');
    const newTx = await res.json();

    allTransactions.value.unshift(newTx);
    await fetchWallets();
    activeTxWallet.value = null;
  } catch (err) {
    console.error(err);
    alert('Đã xảy ra lỗi khi nạp tiền.');
  } finally {
    isSubmitting.value = false;
  }
};

onMounted(() => {
  fetchWallets();
});
</script>
