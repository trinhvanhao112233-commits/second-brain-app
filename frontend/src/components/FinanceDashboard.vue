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
      <div class="flex items-center gap-2 flex-wrap">
        <button
          type="button"
          @click="openTransferModal()"
          class="flex items-center gap-1.5 px-4 py-2 rounded-full bg-[#005237] hover:bg-[#006947] border border-[#38e1a6]/40 text-[#5dfec1] font-bold text-xs transition-all active:scale-95 shadow-sm"
          title="Phân bổ tiền từ Ví Tổng sang các ví con hoặc luân chuyển giữa các ví"
        >
          <span class="material-symbols-rounded text-base">swap_horiz</span>
          <span>Phân Bổ / Chuyển Tiền</span>
        </button>

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

        <button
          type="button"
          @click="confirmResetAllData()"
          class="flex items-center gap-1.5 px-3 py-2 rounded-full bg-[#1c2029] hover:bg-[#ffb4ab]/10 border border-[#41474d] hover:border-[#ffb4ab]/40 text-[#8b9198] hover:text-[#ffb4ab] font-medium text-xs transition-all active:scale-95"
          title="Xóa trắng toàn bộ ví tiền và lịch sử giao dịch để cài đặt lại từ đầu"
        >
          <span class="material-symbols-rounded text-base">restart_alt</span>
          <span>Reset Dữ Liệu Ví</span>
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
    <section class="space-y-4">
      <div class="flex items-center justify-between">
        <div class="flex items-center gap-2">
          <span class="material-symbols-rounded text-[#38e1a6] text-lg">credit_card</span>
          <h3 class="text-sm font-bold uppercase tracking-wider text-white">Danh Sách Ví Tiền</h3>
        </div>
        <span class="text-xs text-[#8b9198]">{{ wallets.length }} ví đang hoạt động</span>
      </div>

      <!-- Banner Ví Tổng (Master Wallet Spotlight) -->
      <div
        v-if="masterWallet"
        class="p-4 sm:p-5 rounded-2xl bg-gradient-to-r from-[#2a2310] via-[#1f1b13] to-[#171c24] border border-amber-500/50 shadow-lg flex flex-col md:flex-row md:items-center justify-between gap-4"
      >
        <div class="flex items-center gap-3.5">
          <div class="w-12 h-12 rounded-2xl bg-amber-500/20 text-amber-400 border border-amber-500/40 flex items-center justify-center shrink-0 shadow-inner">
            <span class="material-symbols-rounded text-2xl">workspace_premium</span>
          </div>
          <div>
            <div class="flex items-center gap-2">
              <span class="px-2 py-0.5 rounded-full text-[10px] font-black uppercase tracking-wider bg-amber-500 text-stone-900">
                Ví Tổng Trung Tâm
              </span>
              <h4 class="text-base sm:text-lg font-bold text-white">{{ masterWallet.name }}</h4>
            </div>
            <p class="text-xs text-[#a0a5ad] mt-0.5">
              Ví nguồn nhận thu nhập/lương tháng và cấp phát tiền cho các ví con
            </p>
          </div>
        </div>

        <div class="flex items-center gap-4 self-end md:self-auto">
          <div class="text-right">
            <span class="text-[10px] uppercase font-bold text-amber-300 block">Số dư Ví Tổng</span>
            <span class="text-xl sm:text-2xl font-black font-mono text-amber-400">
              {{ formatVND(masterWallet.balance) }}
            </span>
          </div>
          <button
            type="button"
            @click="openTransactionModal(masterWallet, 'deposit')"
            class="px-4 py-2.5 rounded-xl bg-amber-500 hover:bg-amber-400 text-stone-900 font-extrabold text-xs transition-all active:scale-95 flex items-center gap-1.5 shadow-md shrink-0"
          >
            <span class="material-symbols-rounded text-base">add_card</span>
            <span>Nạp Vào Ví Tổng</span>
          </button>
        </div>
      </div>

      <!-- Gợi ý nếu tài khoản chưa chỉ định Ví Tổng nào -->
      <div
        v-else-if="wallets.length > 0"
        class="p-4 rounded-2xl bg-[#1c2029] border border-dashed border-amber-500/40 flex flex-col sm:flex-row sm:items-center justify-between gap-3 text-xs"
      >
        <div class="flex items-center gap-3">
          <span class="material-symbols-rounded text-amber-400 text-2xl shrink-0">info</span>
          <div>
            <strong class="text-amber-300 block">Bạn chưa thiết lập Ví Tổng!</strong>
            <span class="text-[#8b9198]">Hãy chọn 1 ví làm Ví Tổng để nạp lương vào đó và trích tiền sang các ví nhỏ.</span>
          </div>
        </div>
        <button
          type="button"
          @click="handleToggleMaster(wallets[0])"
          class="px-3.5 py-1.5 rounded-xl bg-amber-500/20 hover:bg-amber-500/30 text-amber-300 border border-amber-500/40 font-bold shrink-0 self-start sm:self-auto"
        >
          Đặt "{{ wallets[0].name }}" làm Ví Tổng ⭐
        </button>
      </div>

      <!-- Grid Cards -->
      <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
        <!-- Wallet Card -->
        <article
          v-for="wallet in wallets"
          :key="wallet.id"
          class="bg-[#171c24] border rounded-2xl p-5 md-elevation-1 transition-all flex flex-col justify-between relative overflow-hidden"
          :class="wallet.isMaster ? 'border-amber-500/50 bg-gradient-to-b from-[#221f18] to-[#171c24]' : 'border-[#262a34] hover:border-[#41474d]'"
        >
          <!-- Master Wallet Ribbon/Badge -->
          <div
            v-if="wallet.isMaster"
            class="absolute top-0 right-0 bg-gradient-to-l from-amber-500 to-amber-600 text-[#241300] text-[10px] font-black uppercase tracking-wider px-3 py-0.5 rounded-bl-xl flex items-center gap-1 shadow-md"
          >
            <span class="material-symbols-rounded text-xs">workspace_premium</span>
            <span>VÍ TỔNG</span>
          </div>

          <!-- Top Wallet Card -->
          <div>
            <div class="flex items-center justify-between mb-2">
              <div class="flex items-center gap-2">
                <div
                  class="w-8 h-8 rounded-xl flex items-center justify-center border"
                  :class="wallet.isMaster ? 'bg-amber-500/20 text-amber-400 border-amber-500/30' : 'bg-[#1c2029] text-[#5dfec1] border-[#262a34]'"
                >
                  <span class="material-symbols-rounded text-base">{{ wallet.isMaster ? 'stars' : 'savings' }}</span>
                </div>
                <h4 class="font-bold text-base text-white truncate max-w-[140px]">{{ wallet.name }}</h4>
              </div>

              <div class="flex items-center gap-1.5" :class="{ 'pr-14': wallet.isMaster }">
                <!-- Nút Đặt làm Ví Tổng / Hủy Ví Tổng -->
                <button
                  type="button"
                  @click.stop="handleToggleMaster(wallet)"
                  class="w-7 h-7 rounded-lg border flex items-center justify-center transition-colors"
                  :class="wallet.isMaster ? 'bg-amber-500/20 text-amber-400 border-amber-500/40 hover:bg-amber-500/30' : 'bg-[#1c2029] text-[#8b9198] border-[#262a34] hover:text-amber-400 hover:border-amber-500/30'"
                  :title="wallet.isMaster ? 'Ví này hiện là Ví Tổng (Click để hủy)' : 'Đặt ví này làm Ví Tổng'"
                >
                  <span class="material-symbols-rounded text-sm">stars</span>
                </button>

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
              <span class="text-[10px] uppercase tracking-wider font-semibold" :class="wallet.isMaster ? 'text-amber-300' : 'text-[#8b9198]'">
                {{ wallet.isMaster ? 'Số dư Ví Tổng' : 'Số dư khả dụng' }}
              </span>
              <div
                class="text-2xl font-black font-mono mt-0.5"
                :class="wallet.isMaster ? 'text-amber-400' : 'text-[#38e1a6]'"
              >
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
              class="flex items-center justify-center gap-1 py-2 px-3 rounded-xl text-xs font-bold transition-all"
              :class="wallet.isMaster ? 'bg-amber-500 hover:bg-amber-400 text-stone-900 font-extrabold shadow-sm' : 'bg-[#005237] hover:bg-[#006947] text-[#5dfec1]'"
            >
              <span class="material-symbols-rounded text-sm">{{ wallet.isMaster ? 'add_card' : 'call_received' }}</span>
              <span>{{ wallet.isMaster ? 'Nạp Ví Tổng' : (masterWallet ? 'Nạp Từ Ví Tổng' : 'Nạp tiền') }}</span>
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
      <div class="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-[#262a34]">
        <div class="flex items-center gap-2">
          <span class="material-symbols-rounded text-[#38e1a6] text-lg">receipt_long</span>
          <div>
            <h3 class="text-sm font-bold uppercase tracking-wider text-white">Lịch Sử Giao Dịch</h3>
            <span class="text-[11px] text-[#8b9198]">
              Hiển thị: <strong class="text-[#cee9da]">{{ filteredTransactions.length }}</strong> giao dịch
            </span>
          </div>
        </div>

        <!-- Filter Controls -->
        <div class="flex flex-wrap items-center gap-2">
          <!-- Quick Range Tabs -->
          <div class="flex items-center bg-[#1c2029] p-1 rounded-xl border border-[#262a34]">
            <button
              v-for="period in [
                { id: 'today', label: 'Hôm nay' },
                { id: 'single_date', label: 'Theo ngày' },
                { id: 'all', label: 'Tất cả' }
              ]"
              :key="period.id"
              type="button"
              @click="dateFilterMode = period.id"
              class="px-2.5 py-1 text-xs font-semibold rounded-lg transition-all"
              :class="dateFilterMode === period.id 
                ? 'bg-[#005237] text-[#5dfec1] shadow-sm' 
                : 'text-[#8b9198] hover:text-[#e1e2ec]'"
            >
              {{ period.label }}
            </button>
          </div>

          <!-- Specific Date Picker (khi chọn single_date) -->
          <div v-if="dateFilterMode === 'single_date'" class="flex items-center gap-1.5 bg-[#1c2029] px-2 py-1 rounded-xl border border-[#41474d]">
            <span class="material-symbols-rounded text-sm text-[#38e1a6]">event</span>
            <input
              v-model="customDateFilter"
              type="date"
              class="bg-transparent text-xs text-white focus:outline-none"
            />
          </div>

          <!-- Filter Danh Mục -->
          <div class="flex items-center gap-1.5">
            <select
              v-model="categoryFilter"
              class="bg-[#1c2029] border border-[#41474d] text-xs text-[#e1e2ec] font-semibold rounded-xl px-3 py-1.5 focus:outline-none focus:border-[#38e1a6]"
            >
              <option value="ALL">Tất cả danh mục</option>
              <option v-for="cat in expenseCategories" :key="cat" :value="cat">{{ cat }}</option>
              <option value="Nạp tiền">Nạp tiền</option>
              <option value="Chuyển tiền đi">Chuyển tiền đi</option>
              <option value="Nạp tiền từ ví khác">Nạp từ ví khác</option>
              <option value="Phân bổ ví con">Phân bổ ví con</option>
            </select>
          </div>
        </div>
      </div>

      <!-- Thống kê nhanh trong khoảng thời gian đang lọc (Ngày / Toàn thời gian) -->
      <div class="grid grid-cols-3 gap-3 p-3 rounded-xl bg-[#1c2029]/80 border border-[#262a34]">
        <div class="text-left">
          <span class="text-[10px] uppercase font-bold text-[#8b9198] block">Tổng nạp trong kỳ</span>
          <span class="text-xs sm:text-sm font-black font-mono text-[#38e1a6]">
            +{{ formatVND(periodStats.income) }}
          </span>
        </div>
        <div class="text-left border-x border-[#262a34] px-3">
          <span class="text-[10px] uppercase font-bold text-[#8b9198] block">Tổng chi trong kỳ</span>
          <span class="text-xs sm:text-sm font-black font-mono text-[#ffb4ab]">
            -{{ formatVND(periodStats.expense) }}
          </span>
        </div>
        <div class="text-left">
          <span class="text-[10px] uppercase font-bold text-[#8b9198] block">Chênh lệch ròng</span>
          <span
            class="text-xs sm:text-sm font-black font-mono"
            :class="periodStats.net >= 0 ? 'text-[#5dfec1]' : 'text-rose-400'"
          >
            {{ periodStats.net >= 0 ? '+' : '' }}{{ formatVND(periodStats.net) }}
          </span>
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
            <div class="relative flex items-center">
              <input
                v-model.number="budgetForm.monthlyBudget"
                type="number"
                min="0"
                step="any"
                required
                placeholder="5000000"
                class="w-full px-4 py-3 bg-[#171c24] border border-[#41474d] rounded-2xl text-white font-mono text-lg font-bold placeholder-slate-600 focus:outline-none focus:border-[#38e1a6]"
              />
              <span class="absolute right-4 inset-y-0 flex items-center text-xs font-bold text-[#8b9198] pointer-events-none">VNĐ</span>
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
            <div class="relative flex items-center">
              <input
                v-model.number="expenseForm.amount"
                type="number"
                min="0"
                step="any"
                required
                placeholder="50000"
                class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white font-mono text-base font-bold placeholder-slate-600 focus:outline-none focus:border-[#38e1a6]"
              />
              <span class="absolute right-4 inset-y-0 flex items-center text-xs font-bold text-[#8b9198] pointer-events-none">VNĐ</span>
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

          <!-- Checkbox Đặt làm Ví Tổng -->
          <label class="flex items-center gap-2 cursor-pointer p-2.5 rounded-xl bg-[#171c24] border border-[#262a34]">
            <input
              v-model="newWalletIsMaster"
              type="checkbox"
              class="w-4 h-4 rounded text-amber-500 focus:ring-0 bg-[#1c2029] border-[#41474d]"
            />
            <div class="text-xs">
              <span class="font-bold text-amber-300 block">Đặt làm Ví Tổng</span>
              <span class="text-[11px] text-[#8b9198]">Ví nguồn nhận thu nhập chính & cấp tiền cho các ví con</span>
            </div>
          </label>

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
          <div
            class="w-10 h-10 rounded-2xl flex items-center justify-center"
            :class="activeTxWallet.isMaster ? 'bg-amber-500/20 text-amber-400' : 'bg-[#005237] text-[#5dfec1]'"
          >
            <span class="material-symbols-rounded text-xl">{{ activeTxWallet.isMaster ? 'stars' : 'add' }}</span>
          </div>
          <div>
            <h3 class="text-base font-bold text-white truncate">
              Nạp tiền vào "{{ activeTxWallet.name }}"
            </h3>
            <p class="text-xs text-[#8b9198]">
              Số dư hiện tại: <span class="text-white font-mono">{{ formatVND(activeTxWallet.balance) }}</span>
            </p>
          </div>
        </div>

        <form @submit.prevent="handleDepositSubmit" class="mt-4 space-y-3">
          <div class="relative flex items-center">
            <input
              v-model.number="txAmountInput"
              type="number"
              min="0"
              step="any"
              required
              placeholder="0"
              class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white font-mono text-lg font-bold placeholder-slate-600 focus:outline-none focus:border-[#38e1a6]"
            />
            <span class="absolute right-4 inset-y-0 flex items-center text-xs font-bold text-[#8b9198] pointer-events-none">VNĐ</span>
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

          <!-- Tùy chọn trích từ Ví Tổng khi nạp vào ví con -->
          <div v-if="!activeTxWallet.isMaster && masterWallet" class="p-2.5 rounded-xl bg-[#171c24] border border-[#262a34]">
            <label class="flex items-center gap-2 cursor-pointer">
              <input
                v-model="deductFromMasterOption"
                type="checkbox"
                class="w-4 h-4 rounded text-[#38e1a6] focus:ring-0 bg-[#1c2029] border-[#41474d]"
              />
              <div class="text-xs leading-tight">
                <span class="font-bold text-[#e1e2ec] block">Trích tiền từ Ví Tổng "{{ masterWallet.name }}"</span>
                <span class="text-[11px] text-[#8b9198]">
                  Số dư Ví Tổng: <strong class="text-amber-400 font-mono">{{ formatVND(masterWallet.balance) }}</strong>
                </span>
              </div>
            </label>
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

    <!-- ==================== MODAL 5: PHÂN BỔ / CHUYỂN TIỀN GIỮA CÁC VÍ ==================== -->
    <div v-if="showTransferModal" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-md bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <div class="flex items-center gap-3.5 mb-2">
          <div class="w-11 h-11 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-2xl">swap_horiz</span>
          </div>
          <div>
            <h3 class="text-lg font-bold text-white">Phân Bổ & Chuyển Tiền</h3>
            <p class="text-xs text-[#8b9198]">Cấp tiền từ Ví Tổng sang ví con hoặc giữa các ví</p>
          </div>
        </div>

        <form @submit.prevent="handleTransferSubmit" class="mt-4 space-y-3.5">
          <!-- Ví nguồn -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Từ ví nguồn</label>
            <select
              v-model="transferForm.sourceWalletId"
              required
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            >
              <option value="" disabled>-- Chọn ví nguồn --</option>
              <option v-for="w in wallets" :key="w.id" :value="w.id">
                {{ w.isMaster ? '⭐ [VÍ TỔNG] ' : '' }}{{ w.name }} (Số dư: {{ formatVND(w.balance) }})
              </option>
            </select>
          </div>

          <!-- Ví đích -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Chuyển sang ví đích</label>
            <select
              v-model="transferForm.targetWalletId"
              required
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            >
              <option value="" disabled>-- Chọn ví đích --</option>
              <option
                v-for="w in wallets.filter(w => w.id !== transferForm.sourceWalletId)"
                :key="w.id"
                :value="w.id"
              >
                {{ w.isMaster ? '⭐ [VÍ TỔNG] ' : '' }}{{ w.name }} (Số dư: {{ formatVND(w.balance) }})
              </option>
            </select>
          </div>

          <!-- Số tiền chuyển -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Số tiền chuyển (VNĐ)</label>
            <div class="relative flex items-center">
              <input
                v-model.number="transferForm.amount"
                type="number"
                min="1"
                step="any"
                required
                placeholder="1000000"
                class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white font-mono text-base font-bold placeholder-slate-600 focus:outline-none focus:border-[#38e1a6]"
              />
              <span class="absolute right-4 inset-y-0 flex items-center text-xs font-bold text-[#8b9198] pointer-events-none">VNĐ</span>
            </div>
          </div>

          <!-- Gợi ý nhanh -->
          <div class="grid grid-cols-4 gap-1.5">
            <button
              v-for="amt in [200000, 500000, 1000000, 2000000]"
              :key="amt"
              type="button"
              @click="transferForm.amount = amt"
              class="py-1 px-1 bg-[#262a34] hover:bg-[#31353f] text-[11px] font-semibold text-[#c1c7ce] rounded-lg text-center transition-all"
            >
              {{ formatQuick(amt) }}
            </button>
          </div>

          <!-- Ghi chú chuyển tiền -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Ghi chú phân bổ</label>
            <input
              v-model="transferForm.note"
              type="text"
              placeholder="Ví dụ: Cấp tiền ăn uống tuần 1, nạp ví con..."
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <div class="flex items-center justify-end gap-2.5 pt-3 border-t border-[#262a34]">
            <button
              type="button"
              @click="showTransferModal = false"
              class="px-4 py-2 rounded-full text-xs font-semibold text-[#c1c7ce] hover:bg-[#262a34]"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmitting || !transferForm.amount || transferForm.amount <= 0 || !transferForm.sourceWalletId || !transferForm.targetWalletId"
              class="px-5 py-2 bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] rounded-full text-xs font-bold transition-all disabled:opacity-40 flex items-center gap-1.5"
            >
              <span class="material-symbols-rounded text-base">check</span>
              <span>{{ isSubmitting ? 'Đang chuyển...' : 'Xác Nhận Phân Bổ' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue';
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
const newWalletIsMaster = ref(false);

// Modal Nạp tiền
const activeTxWallet = ref(null);
const txAmountInput = ref(null);
const deductFromMasterOption = ref(true); // Mặc định bật trích tiền từ ví tổng nếu nạp ví con
const quickAmounts = [100000, 200000, 500000, 2000000];

// Modal Phân Bổ / Chuyển Tiền
const showTransferModal = ref(false);
const transferForm = ref({
  sourceWalletId: '',
  targetWalletId: '',
  amount: null,
  note: '',
});

// Lọc thời gian theo Ngày
const dateFilterMode = ref('all'); // 'all', 'today', 'single_date'
const customDateFilter = ref(new Date().toISOString().split('T')[0]);

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
  const time = d.toLocaleTimeString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    hour12: false
  });
  const date = d.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric'
  });
  return `${time} • ${date}`;
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
const masterWallet = computed(() => {
  return wallets.value.find(w => w.isMaster) || null;
});

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
  return w ? (w.isMaster ? `⭐ ${w.name}` : w.name) : 'Chung';
};

// Transactions Lọc
const filteredTransactions = computed(() => {
  let list = allTransactions.value;

  // Lọc theo ví
  if (selectedWalletFilterId.value !== 'ALL') {
    list = list.filter(t => t.walletId === selectedWalletFilterId.value);
  }

  // Lọc theo danh mục
  if (categoryFilter.value !== 'ALL') {
    if (categoryFilter.value === 'Nạp tiền') {
      list = list.filter(t => t.amount > 0);
    } else {
      list = list.filter(t => t.category === categoryFilter.value);
    }
  }

  // Lọc theo ngày
  if (dateFilterMode.value === 'today') {
    const todayStr = new Date().toISOString().split('T')[0];
    list = list.filter(t => {
      const d = (t.transactionDate || t.createdAt || '').split('T')[0];
      return d === todayStr;
    });
  } else if (dateFilterMode.value === 'single_date' && customDateFilter.value) {
    list = list.filter(t => {
      const d = (t.transactionDate || t.createdAt || '').split('T')[0];
      return d === customDateFilter.value;
    });
  }

  return list;
});

// Thống kê theo khoảng thời gian đang lọc
const periodStats = computed(() => {
  let income = 0;
  let expense = 0;

  for (const t of filteredTransactions.value) {
    const val = Number(t.amount) || 0;
    if (val > 0) {
      income += val;
    } else {
      expense += Math.abs(val);
    }
  }

  return {
    income,
    expense,
    net: income - expense,
  };
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

const handleToggleMaster = async (wallet) => {
  if (!wallet) return;
  const newStatus = !wallet.isMaster;
  const confirmMsg = newStatus
    ? `Đặt "${wallet.name}" làm Ví Tổng (nhận lương/thu nhập và cấp tiền cho các ví con)?`
    : `Hủy trạng thái Ví Tổng của "${wallet.name}"?`;

  if (!window.confirm(confirmMsg)) return;

  isSubmitting.value = true;
  try {
    const res = await apiFetch(`${API_BASE_URL}/${wallet.id}/master`, {
      method: 'PUT',
      body: JSON.stringify({ isMaster: newStatus }),
    });

    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.message || `Lỗi server (${res.status})`);
    }

    await fetchWallets();
  } catch (err) {
    console.error('Lỗi đổi trạng thái ví tổng:', err);
    alert(`Không thể thay đổi: ${err.message || 'Lỗi không xác định'}`);
  } finally {
    isSubmitting.value = false;
  }
};

const openTransferModal = () => {
  const master = wallets.value.find(w => w.isMaster);
  const firstChild = wallets.value.find(w => !w.isMaster) || (wallets.value.length > 1 ? wallets.value[1] : null);

  transferForm.value = {
    sourceWalletId: master ? master.id : (wallets.value[0]?.id || ''),
    targetWalletId: firstChild ? firstChild.id : (wallets.value[1]?.id || ''),
    amount: null,
    note: '',
  };
  showTransferModal.value = true;
};

const handleTransferSubmit = async () => {
  if (!transferForm.value.sourceWalletId || !transferForm.value.targetWalletId) {
    alert('Vui lòng chọn ví nguồn và ví đích.');
    return;
  }
  if (!transferForm.value.amount || transferForm.value.amount <= 0) {
    alert('Số tiền chuyển phải lớn hơn 0.');
    return;
  }
  if (transferForm.value.sourceWalletId === transferForm.value.targetWalletId) {
    alert('Ví nguồn và ví đích không được trùng nhau.');
    return;
  }

  isSubmitting.value = true;
  try {
    const res = await apiFetch(`${API_BASE_URL}/transfer`, {
      method: 'POST',
      body: JSON.stringify({
        sourceWalletId: transferForm.value.sourceWalletId,
        targetWalletId: transferForm.value.targetWalletId,
        amount: Number(transferForm.value.amount),
        note: transferForm.value.note || '',
        transactionDate: new Date().toISOString(),
      }),
    });

    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.message || `Lỗi (${res.status})`);
    }

    await fetchWallets();
    showTransferModal.value = false;
  } catch (err) {
    console.error('Lỗi chuyển tiền:', err);
    alert(`Không thể chuyển tiền: ${err.message || 'Lỗi không xác định'}`);
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
        monthlyBudget: Number(newWalletBudget.value) || 0,
        isMaster: newWalletIsMaster.value,
      }),
    });

    if (!res.ok) throw new Error('Không thể tạo ví');
    const createdWallet = await res.json();
    wallets.value.unshift(createdWallet);

    newWalletName.value = '';
    newWalletBudget.value = 0;
    newWalletIsMaster.value = false;
    showAddWalletModal.value = false;
    await fetchWallets();
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
  deductFromMasterOption.value = !wallet.isMaster && !!masterWallet.value;
};

const handleDepositSubmit = async () => {
  if (!activeTxWallet.value || !txAmountInput.value || txAmountInput.value <= 0) return;

  isSubmitting.value = true;
  try {
    const shouldDeduct = !activeTxWallet.value.isMaster && deductFromMasterOption.value;
    const res = await apiFetch(`${API_BASE_URL}/${activeTxWallet.value.id}/transactions`, {
      method: 'POST',
      body: JSON.stringify({
        amount: Number(txAmountInput.value),
        category: 'Nạp tiền',
        note: shouldDeduct ? `Nạp tiền (trích từ ${masterWallet.value?.name || 'Ví Tổng'})` : 'Nạp tiền vào ví',
        transactionDate: new Date().toISOString(),
        deductFromMaster: shouldDeduct,
      }),
    });

    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.message || 'Không thể nạp tiền');
    }

    await fetchWallets();
    activeTxWallet.value = null;
  } catch (err) {
    console.error(err);
    alert(`Lỗi khi nạp tiền: ${err.message || 'Lỗi không xác định'}`);
  } finally {
    isSubmitting.value = false;
  }
};

const confirmResetAllData = async () => {
  const confirmed = window.confirm(
    'CẢNH BÁO: Thao tác này sẽ xóa sạch toàn bộ các ví tiền và tất cả lịch sử giao dịch để bạn thiết lập lại từ đầu.\n\nTài khoản đăng nhập, công việc và ghi chú sẽ được giữ nguyên.\n\nBạn có chắc chắn muốn reset không?'
  );
  if (!confirmed) return;

  try {
    isSubmitting.value = true;
    const res = await apiFetch('/wallets/reset-all', {
      method: 'DELETE',
    });

    if (!res.ok) {
      const errData = await res.json().catch(() => null);
      throw new Error(errData?.message || 'Không thể reset dữ liệu');
    }

    alert('Đã xóa sạch toàn bộ ví và giao dịch! Trang sẽ tự động tải lại.');
    await fetchWallets();
    window.location.reload();
  } catch (err) {
    console.error(err);
    alert(`Lỗi khi reset: ${err.message || 'Lỗi không xác định'}`);
  } finally {
    isSubmitting.value = false;
  }
};

const handleDataUpdated = (e) => {
  if (e.detail?.action === 'transaction') {
    fetchWallets();
  }
};

onMounted(() => {
  fetchWallets();
  window.addEventListener('secondbrain-data-updated', handleDataUpdated);
});

onUnmounted(() => {
  window.removeEventListener('secondbrain-data-updated', handleDataUpdated);
});
</script>
