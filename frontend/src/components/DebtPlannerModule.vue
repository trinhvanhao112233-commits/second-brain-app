<template>
  <div class="space-y-5 sm:space-y-6">
    <!-- ==================== 1. HEADER BAR ==================== -->
    <header class="card-luxury rounded-3xl px-5 py-4 sm:px-6 sm:py-5 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
      <div class="flex items-start sm:items-center gap-3.5">
        <div class="w-12 h-12 rounded-2xl bg-gradient-to-br from-indigo-600 to-violet-600 text-white flex items-center justify-center shadow-md shadow-indigo-500/20 shrink-0">
          <span class="material-symbols-rounded text-2xl">receipt_long</span>
        </div>
        <div>
          <div class="flex items-center gap-2 flex-wrap">
            <h2 class="text-base sm:text-lg font-extrabold text-slate-900 tracking-tight">
              Kế Hoạch Trả Nợ & Dòng Tiền Theo Mốc
            </h2>
            <span
              class="px-2.5 py-0.5 rounded-full text-[11px] font-bold flex items-center gap-1 border"
              :class="cloudSyncState === 'syncing'
                ? 'bg-indigo-50 text-indigo-700 border-indigo-200'
                : 'bg-emerald-50 text-emerald-700 border-emerald-200'"
            >
              <span class="material-symbols-rounded text-xs" :class="{ 'animate-spin': cloudSyncState === 'syncing' }">
                {{ cloudSyncState === 'syncing' ? 'sync' : 'cloud_done' }}
              </span>
              <span>{{ cloudSyncState === 'syncing' ? 'Đang lưu…' : cloudSyncState === 'synced' ? 'Đã đồng bộ' : cloudSyncState === 'error' ? 'Chưa đồng bộ' : 'Chưa kiểm tra' }}</span>
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Chia dòng tiền theo 3 mốc (1–10, 11–20, 21–cuối tháng) • Tự động xếp hạng khoản nợ ưu tiên trả trước
          </p>
        </div>
      </div>

      <!-- Action Buttons -->
      <div class="grid grid-cols-2 sm:flex sm:items-center gap-2">
        <button
          type="button"
          @click="openModal('INCOME')"
          class="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-2xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold transition-all active:scale-95 shadow-sm"
        >
          <span class="material-symbols-rounded text-base">add_circle</span>
          <span>+ Thêm Tiền Vào</span>
        </button>

        <button
          type="button"
          @click="openModal('DEBT')"
          class="flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-2xl bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold transition-all active:scale-95 shadow-sm"
        >
          <span class="material-symbols-rounded text-base">notification_important</span>
          <span>+ Nhập Khoản Nợ</span>
        </button>

        <button
          type="button"
          @click="openModal('BORROW_MORE')"
          class="col-span-2 sm:col-span-1 flex items-center justify-center gap-1.5 px-4 py-2.5 rounded-2xl bg-violet-600 hover:bg-violet-700 text-white text-xs font-bold transition-all active:scale-95 shadow-sm"
        >
          <span class="material-symbols-rounded text-base">handshake</span>
          <span>+ Vay Thêm (Ở Ngoài)</span>
        </button>
      </div>
    </header>

    <div class="flex flex-wrap items-center justify-between gap-3"><label class="text-sm text-slate-600">Kỳ kế hoạch <input v-model="selectedMonth" type="month" class="ml-2 border rounded-lg p-2 bg-white" aria-label="Tháng kế hoạch"></label><button class="primary-button" @click="showPayment=true">Ghi trả nợ từ ví</button></div>
    <DebtPaymentModal v-if="showPayment" :debts="records" @close="showPayment=false" @saved="paymentSaved" />
    <!-- ==================== 2. 4 THẺ TỔNG QUAN DÒNG TIỀN ==================== -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
      <!-- Card 1: Tổng Tiền Vào -->
      <div class="card-luxury rounded-3xl p-4 sm:p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-[11px] font-bold uppercase tracking-wider text-slate-500">Tổng Tiền Vào (Thu)</span>
          <div class="w-8 h-8 sm:w-9 sm:h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">south_west</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-lg sm:text-2xl font-black font-mono text-emerald-600 truncate">
            +{{ formatVND(summaryStats.totalIncome) }}
          </div>
          <div class="text-[11px] font-mono text-slate-400">
            ({{ formatShortK(summaryStats.totalIncome) }})
          </div>
        </div>
        <div class="text-[11px] text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Nguồn thu</span>
          <span class="font-bold text-emerald-700">{{ incomeRecords.length }} khoản</span>
        </div>
      </div>

      <!-- Card 2: Tổng Nợ Phải Trả -->
      <div class="card-luxury rounded-3xl p-4 sm:p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-[11px] font-bold uppercase tracking-wider text-slate-500">Tổng Nợ Phải Trả</span>
          <div class="w-8 h-8 sm:w-9 sm:h-9 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">north_east</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-lg sm:text-2xl font-black font-mono text-rose-600 truncate">
            -{{ formatVND(summaryStats.totalUnpaidDebt) }}
          </div>
          <div class="text-[11px] font-mono text-slate-400 truncate">
            Gốc: {{ formatShortK(summaryStats.totalAllDebt) }} • Đã trả: {{ formatShortK(summaryStats.totalPaidDebt) }}
          </div>
        </div>
        <div class="text-[11px] text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Chưa tất toán</span>
          <span class="font-bold text-rose-600">{{ unpaidDebtsCount }} khoản nợ</span>
        </div>
      </div>

      <!-- Card 3: Vay Thêm Ở Ngoài -->
      <div class="card-luxury rounded-3xl p-4 sm:p-5 flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-[11px] font-bold uppercase tracking-wider text-slate-500">Vay Thêm Ở Ngoài</span>
          <div class="w-8 h-8 sm:w-9 sm:h-9 rounded-xl bg-violet-50 text-violet-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">account_balance</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-lg sm:text-2xl font-black font-mono text-violet-600 truncate">
            +{{ formatVND(summaryStats.totalBorrowedMore) }}
          </div>
          <div class="text-[11px] text-slate-400 truncate">
            <span v-if="summaryStats.neededToBorrow > 0" class="text-rose-600 font-bold">
              Cần vay bù: {{ formatShortK(summaryStats.neededToBorrow) }}
            </span>
            <span v-else class="text-emerald-600 font-medium">
              Đủ khả năng chi trả
            </span>
          </div>
        </div>
        <div class="text-[11px] text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Vay ngoài</span>
          <button
            type="button"
            @click="openModal('BORROW_MORE')"
            class="text-violet-600 font-bold hover:underline"
          >
            + Ghi vay thêm
          </button>
        </div>
      </div>

      <!-- Card 4: Kết Quả Cân Đối (Dư hoặc Thiếu) -->
      <div
        class="rounded-3xl p-4 sm:p-5 border flex flex-col justify-between shadow-sm"
        :class="summaryStats.netBalanceAfterBorrow >= 0
          ? 'bg-gradient-to-br from-emerald-50 to-teal-50/60 border-emerald-200'
          : 'bg-gradient-to-br from-rose-50 to-orange-50/60 border-rose-200'"
      >
        <div class="flex items-center justify-between">
          <span
            class="text-[11px] font-bold uppercase tracking-wider"
            :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'text-emerald-800' : 'text-rose-800'"
          >
            {{ summaryStats.netBalanceAfterBorrow >= 0 ? 'Tiền Dư Còn Lại' : 'Đang Thiếu Tiền' }}
          </span>
          <div
            class="w-8 h-8 sm:w-9 sm:h-9 rounded-xl flex items-center justify-center shadow-sm"
            :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'bg-emerald-600 text-white' : 'bg-rose-600 text-white'"
          >
            <span class="material-symbols-rounded text-lg">
              {{ summaryStats.netBalanceAfterBorrow >= 0 ? 'savings' : 'warning' }}
            </span>
          </div>
        </div>

        <div class="my-2">
          <div
            class="text-lg sm:text-2xl font-black font-mono truncate"
            :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'text-emerald-700' : 'text-rose-700'"
          >
            {{ summaryStats.netBalanceAfterBorrow >= 0 ? '+' : '' }}{{ formatVND(summaryStats.netBalanceAfterBorrow) }}
          </div>
          <div class="text-[11px] font-semibold truncate" :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'text-emerald-600' : 'text-rose-600'">
            {{ summaryStats.netBalanceAfterBorrow >= 0
              ? `Trả hết nợ còn dư ${formatShortK(summaryStats.netBalanceAfterBorrow)}`
              : `Thiếu ${formatShortK(Math.abs(summaryStats.netBalanceAfterBorrow))} cần vay bù` }}
          </div>
        </div>

        <div class="pt-2 border-t" :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'border-emerald-200/70' : 'border-rose-200/70'">
          <button
            v-if="summaryStats.netBalanceAfterBorrow < 0"
            type="button"
            @click="openQuickBorrowForShortfall(Math.abs(summaryStats.netBalanceAfterBorrow), '1-10')"
            class="w-full py-1.5 px-2.5 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-[11px] font-bold flex items-center justify-center gap-1 shadow-sm transition-all"
          >
            <span class="material-symbols-rounded text-xs">add_card</span>
            <span>+ Vay bù {{ formatShortK(Math.abs(summaryStats.netBalanceAfterBorrow)) }}</span>
          </button>
          <div v-else class="text-[11px] text-emerald-700 font-bold flex items-center gap-1">
            <span class="material-symbols-rounded text-sm">check_circle</span>
            <span>Dòng tiền an toàn</span>
          </div>
        </div>
      </div>
    </div>

    <!-- ==================== 3. BẢNG CHIA MỐC THỜI GIAN (1-10, 10-21, 21-31) ==================== -->
    <section class="card-luxury rounded-3xl p-4 sm:p-6 space-y-5">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-slate-100">
        <div>
          <div class="flex items-center gap-2">
            <span class="material-symbols-rounded text-indigo-600 text-xl">view_timeline</span>
            <h3 class="text-sm sm:text-base font-extrabold text-slate-900 uppercase tracking-wide">
              Bảng Cân Đối Dòng Tiền Theo Mốc Thời Gian
            </h3>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Vì tiền vào và nợ đến hạn nằm ở các ngày khác nhau, hệ thống tách làm 3 đợt để biết đợt nào dư hay thiếu
          </p>
        </div>

        <!-- Nút bật/tắt cộng dồn số dư mốc trước sang mốc sau -->
        <label class="flex items-center gap-2 cursor-pointer bg-slate-50 hover:bg-slate-100 px-3.5 py-2 rounded-2xl border border-slate-200 text-xs font-bold text-slate-700 select-none shrink-0">
          <input
            v-model="carryOverBalance"
            type="checkbox"
            @change="handleCarryOverChange"
            class="rounded text-indigo-600 focus:ring-0 w-4 h-4"
          />
          <span>Cộng dồn tiền dư mốc trước sang mốc sau</span>
        </label>
      </div>

      <!-- 3 Cột Mốc Thời Gian: 1-10, 10-21, 21-31 -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-5">
        <div
          v-for="stage in timelineStages"
          :key="stage.id"
          class="rounded-3xl border bg-slate-50/60 flex flex-col justify-between overflow-hidden transition-all"
          :class="stage.netResult >= 0 ? 'border-slate-200/90' : 'border-rose-300 ring-1 ring-rose-200'"
        >
          <div>
            <!-- Header Mốc -->
            <div class="px-5 py-3.5 bg-white border-b border-slate-200/80 flex items-center justify-between">
              <div class="flex items-center gap-2.5">
                <span class="px-3 py-1 rounded-xl font-mono font-black text-xs bg-indigo-600 text-white shadow-sm">
                  Ngày {{ stage.id }}
                </span>
                <div>
                  <h4 class="font-extrabold text-sm text-slate-900">{{ stage.title }}</h4>
                  <span class="text-[11px] text-slate-500">{{ stage.subtitle }}</span>
                </div>
              </div>

              <span
                class="px-2.5 py-1 rounded-full text-[11px] font-bold"
                :class="stage.netResult >= 0
                  ? 'bg-emerald-50 text-emerald-700 border border-emerald-200'
                  : 'bg-rose-50 text-rose-700 border border-rose-200'"
              >
                {{ stage.netResult >= 0 ? 'Đủ trả ✓' : 'Thiếu tiền !' }}
              </span>
            </div>

            <!-- Khối công thức tóm tắt dòng tiền (Giống cách bạn ghi chú) -->
            <div class="mx-4 mt-4 p-3.5 rounded-2xl bg-white border border-slate-200/90 font-mono text-xs space-y-1.5 shadow-sm">
              <div
                v-if="carryOverBalance && stage.carriedFromPrev !== 0"
                class="flex items-center justify-between text-slate-500 pb-1.5 border-b border-dashed border-slate-200"
              >
                <span>Dư đợt trước chuyển sang:</span>
                <span class="font-bold" :class="stage.carriedFromPrev >= 0 ? 'text-indigo-600' : 'text-rose-600'">
                  {{ stage.carriedFromPrev >= 0 ? '+' : '' }}{{ formatShortK(stage.carriedFromPrev) }}
                </span>
              </div>

              <!-- Dòng VÀO -->
              <div class="flex items-start justify-between gap-2">
                <span class="font-bold text-emerald-700 shrink-0">Vào:</span>
                <div class="text-right text-slate-700 break-all">
                  <span>{{ stage.incomeFormula || '0' }}</span>
                  <span class="font-black text-emerald-600"> = {{ formatShortK(stage.incomeTotal) }}</span>
                </div>
              </div>

              <!-- Dòng CHI (NỢ) -->
              <div class="flex items-start justify-between gap-2">
                <span class="font-bold text-rose-600 shrink-0">Chi (Nợ):</span>
                <div class="text-right text-slate-700 break-all">
                  <span>{{ stage.debtFormula || '0' }}</span>
                  <span class="font-black text-rose-600"> = {{ formatShortK(stage.debtTotal) }}</span>
                </div>
              </div>

              <!-- Dòng VAY THÊM NGOÀI -->
              <div v-if="stage.borrowMoreTotal > 0" class="flex items-start justify-between gap-2 text-violet-700">
                <span class="font-bold shrink-0">Vay thêm:</span>
                <div class="text-right font-bold">
                  +{{ formatShortK(stage.borrowMoreTotal) }} ({{ formatVND(stage.borrowMoreTotal) }})
                </div>
              </div>

              <!-- Dòng CÒN LẠI / THIẾU -->
              <div class="pt-2 border-t border-slate-200 flex items-center justify-between text-sm font-black">
                <span :class="stage.netResult >= 0 ? 'text-emerald-700' : 'text-rose-600'">
                  {{ stage.netResult >= 0 ? 'Còn lại:' : 'Đang thiếu:' }}
                </span>
                <span :class="stage.netResult >= 0 ? 'text-emerald-600' : 'text-rose-600'">
                  {{ stage.netResult >= 0 ? '+' : '' }}{{ formatShortK(stage.netResult) }}
                  <span class="text-[11px] font-normal text-slate-500">({{ formatVND(stage.netResult) }})</span>
                </span>
              </div>
            </div>

            <!-- Danh sách chi tiết các khoản trong Mốc này -->
            <div class="p-4 space-y-3.5">
              <!-- Các khoản TIỀN VÀO -->
              <div>
                <div class="flex items-center justify-between text-[11px] font-extrabold uppercase tracking-wider text-emerald-700 mb-2">
                  <span>Tiền vào trong mốc ({{ stage.incomes.length }})</span>
                  <button
                    type="button"
                    @click="openModal('INCOME', stage.id)"
                    class="px-2 py-0.5 rounded-lg bg-emerald-50 hover:bg-emerald-100 text-emerald-700 font-bold transition-colors"
                  >
                    + Thêm thu
                  </button>
                </div>

                <div v-if="stage.incomes.length === 0" class="text-xs text-slate-400 italic py-1">
                  Chưa có khoản thu nào ở mốc {{ stage.id }}
                </div>
                <div v-else class="space-y-2">
                  <div
                    v-for="inc in stage.incomes"
                    :key="inc.id"
                    class="px-3.5 py-2.5 rounded-2xl bg-white border border-emerald-200/80 flex items-center justify-between gap-2 text-xs shadow-sm"
                  >
                    <div class="min-w-0">
                      <div class="font-bold text-slate-900 truncate">{{ inc.title }}</div>
                      <div v-if="inc.breakdownNote" class="text-[11px] text-slate-500 font-mono truncate">
                        {{ inc.breakdownNote }}
                      </div>
                    </div>
                    <div class="flex items-center gap-1 shrink-0">
                      <span class="font-mono font-extrabold text-emerald-600 mr-1">+{{ formatShortK(inc.totalAmount) }}</span>
                      <button type="button" @click="openEditModal(inc)" class="text-slate-400 hover:text-indigo-600 p-1">
                        <span class="material-symbols-rounded text-sm">edit</span>
                      </button>
                      <button type="button" @click="deleteItem(inc.id)" class="text-slate-400 hover:text-rose-600 p-1">
                        <span class="material-symbols-rounded text-sm">close</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Các khoản NỢ PHẢI TRẢ -->
              <div class="pt-2.5 border-t border-slate-200/80">
                <div class="flex items-center justify-between text-[11px] font-extrabold uppercase tracking-wider text-rose-700 mb-2">
                  <span>Khoản nợ đến hạn trả ({{ stage.debts.length }})</span>
                  <button
                    type="button"
                    @click="openModal('DEBT', stage.id)"
                    class="px-2 py-0.5 rounded-lg bg-rose-50 hover:bg-rose-100 text-rose-700 font-bold transition-colors"
                  >
                    + Thêm nợ
                  </button>
                </div>

                <div v-if="stage.debts.length === 0" class="text-xs text-slate-400 italic py-1">
                  Không có khoản nợ nào đến hạn ở mốc {{ stage.id }}
                </div>
                <div v-else class="space-y-2">
                  <div
                    v-for="(debt, idx) in stage.debts"
                    :key="debt.id"
                    class="px-3.5 py-2.5 rounded-2xl bg-white border flex items-center justify-between gap-2 text-xs shadow-sm transition-all"
                    :class="debt.isCompleted ? 'opacity-50 border-slate-200 bg-slate-50' : 'border-rose-200/90'"
                  >
                    <div class="flex items-center gap-2.5 min-w-0">
                      <button
                        type="button"
                        @click="toggleComplete(debt)"
                        class="w-5 h-5 rounded-lg border flex items-center justify-center shrink-0 transition-colors"
                        :class="debt.isCompleted ? 'bg-emerald-600 border-emerald-600 text-white' : 'border-slate-300 hover:border-emerald-600 text-transparent'"
                        title="Đánh dấu đã trả xong"
                      >
                        <span class="material-symbols-rounded text-xs font-bold">check</span>
                      </button>
                      <div class="min-w-0">
                        <div class="flex items-center gap-1.5">
                          <span class="px-1.5 py-0.5 rounded-md text-[10px] font-extrabold bg-rose-50 text-rose-700 border border-rose-200">
                            #{{ idx + 1 }}
                          </span>
                          <span class="font-bold text-slate-900 truncate" :class="{ 'line-through text-slate-400': debt.isCompleted }">
                            {{ debt.title }}
                          </span>
                        </div>
                        <div class="flex items-center gap-1.5 text-[10px] text-slate-500 font-mono mt-0.5">
                          <span>Hạn: {{ formatDateShort(debt.dueDate) }}</span>
                          <span v-if="debt.breakdownNote" class="truncate">• {{ debt.breakdownNote }}</span>
                        </div>
                      </div>
                    </div>

                    <div class="flex items-center gap-1 shrink-0">
                      <span class="font-mono font-extrabold text-rose-600 mr-1">
                        -{{ formatShortK(Math.max(0, (Number(debt.totalAmount) || 0) - (Number(debt.paidAmount) || 0))) }}
                      </span>
                      <button type="button" @click="openEditModal(debt)" class="text-slate-400 hover:text-indigo-600 p-1">
                        <span class="material-symbols-rounded text-sm">edit</span>
                      </button>
                      <button type="button" @click="deleteItem(debt.id)" class="text-slate-400 hover:text-rose-600 p-1">
                        <span class="material-symbols-rounded text-sm">close</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Các khoản VAY THÊM Ở NGOÀI -->
              <div v-if="stage.borrows.length > 0" class="pt-2.5 border-t border-slate-200/80">
                <div class="flex items-center justify-between text-[11px] font-extrabold uppercase tracking-wider text-violet-700 mb-2">
                  <span>Khoản vay thêm ở ngoài ({{ stage.borrows.length }})</span>
                </div>
                <div class="space-y-2">
                  <div
                    v-for="b in stage.borrows"
                    :key="b.id"
                    class="px-3.5 py-2.5 rounded-2xl bg-violet-50/70 border border-violet-200 flex items-center justify-between gap-2 text-xs"
                  >
                    <div class="min-w-0">
                      <div class="font-bold text-violet-950 truncate">{{ b.title }}</div>
                      <div v-if="b.breakdownNote" class="text-[10px] text-violet-700 truncate">{{ b.breakdownNote }}</div>
                    </div>
                    <div class="flex items-center gap-1 shrink-0">
                      <span class="font-mono font-extrabold text-violet-700 mr-1">+{{ formatShortK(b.totalAmount) }}</span>
                      <button type="button" @click="openEditModal(b)" class="text-violet-400 hover:text-violet-800 p-1">
                        <span class="material-symbols-rounded text-sm">edit</span>
                      </button>
                      <button type="button" @click="deleteItem(b.id)" class="text-violet-400 hover:text-rose-600 p-1">
                        <span class="material-symbols-rounded text-sm">close</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Footer Cảnh Báo Thiếu Tiền hoặc Báo Dư -->
          <div class="p-4 border-t border-slate-200/80 bg-white">
            <div v-if="stage.netResult < 0" class="space-y-2">
              <div class="flex items-center justify-between text-xs text-rose-700 font-bold">
                <span>⚠️ Mốc {{ stage.id }} đang thiếu:</span>
                <span class="font-mono text-sm font-black">{{ formatVND(Math.abs(stage.netResult)) }}</span>
              </div>
              <button
                type="button"
                @click="openQuickBorrowForShortfall(Math.abs(stage.netResult), stage.id)"
                class="w-full py-2 px-3 rounded-xl bg-violet-600 hover:bg-violet-700 text-white text-xs font-bold flex items-center justify-center gap-1.5 shadow-sm transition-all"
              >
                <span class="material-symbols-rounded text-sm">handshake</span>
                <span>+ Thêm mục Vay Thêm {{ formatShortK(Math.abs(stage.netResult)) }}</span>
              </button>
            </div>
            <div v-else class="flex items-center justify-between text-xs">
              <span class="text-slate-600 font-semibold">✅ Trả xong mốc {{ stage.id }}, còn dư:</span>
              <span class="font-mono font-black text-sm text-emerald-600">+{{ formatVND(stage.netResult) }}</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==================== 4. DANH SÁCH XẾP HẠNG ƯU TIÊN TRẢ NỢ ==================== -->
    <section class="card-luxury rounded-3xl p-4 sm:p-6 space-y-4">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-slate-100">
        <div>
          <div class="flex items-center gap-2">
            <span class="material-symbols-rounded text-rose-600 text-xl">priority_high</span>
            <h3 class="text-sm sm:text-base font-extrabold text-slate-900 uppercase tracking-wide">
              Thứ Tự Khoản Nợ Ưu Tiên Trả Trước
            </h3>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Tự động xếp hạng: Mức độ ưu tiên bắt buộc → Hạn trả gần nhất → Cân đối với tổng nguồn thu
          </p>
        </div>

        <div class="flex items-center gap-2">
          <div class="flex bg-slate-100 p-1 rounded-xl border border-slate-200">
            <button
              v-for="tab in [
                { id: 'UNPAID', label: 'Chưa trả (' + unpaidDebtsCount + ')' },
                { id: 'ALL', label: 'Tất cả (' + debtRecords.length + ')' }
              ]"
              :key="tab.id"
              type="button"
              @click="debtListFilter = tab.id"
              class="px-3 py-1 rounded-lg text-xs font-bold transition-all"
              :class="debtListFilter === tab.id ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              {{ tab.label }}
            </button>
          </div>
        </div>
      </div>

      <!-- Empty state -->
      <div v-if="prioritizedDebts.length === 0" class="py-12 text-center text-slate-500">
        <span class="material-symbols-rounded text-4xl text-emerald-500 mb-2">verified</span>
        <p class="text-sm font-bold text-slate-800">Không có khoản nợ nào cần trả!</p>
        <p class="text-xs text-slate-500 mt-1">Bấm nút "+ Nhập Khoản Nợ" ở góc trên để thêm khoản nợ theo mốc.</p>
      </div>

      <!-- Mobile Card View (Hiển thị trên Điện Thoại) -->
      <div v-else class="space-y-3 md:hidden">
        <div
          v-for="(item, index) in prioritizedDebts"
          :key="'m_' + item.id"
          class="p-4 rounded-2xl border bg-slate-50/60 space-y-3 transition-all"
          :class="item.isCompleted ? 'opacity-50 border-slate-200' : 'border-slate-200'"
        >
          <div class="flex items-start justify-between gap-2">
            <div class="flex items-center gap-2.5 min-w-0">
              <span
                class="w-7 h-7 rounded-xl font-mono font-black text-xs flex items-center justify-center shrink-0"
                :class="index === 0 && !item.isCompleted
                  ? 'bg-rose-600 text-white shadow-sm'
                  : index === 1 && !item.isCompleted
                  ? 'bg-amber-500 text-white'
                  : 'bg-white text-slate-700 border border-slate-200'"
              >
                #{{ index + 1 }}
              </span>
              <div class="min-w-0">
                <div class="font-extrabold text-slate-900 text-sm truncate" :class="{ 'line-through': item.isCompleted }">
                  {{ item.title }}
                </div>
                <div class="flex items-center gap-1.5 mt-0.5 flex-wrap">
                  <span class="px-2 py-0.5 rounded-md font-mono font-bold text-[10px] bg-indigo-50 text-indigo-700 border border-indigo-200">
                    Mốc {{ item.periodLabel }}
                  </span>
                  <span class="text-[11px] font-medium" :class="getDueCountdownColor(item.dueDate, item.isCompleted)">
                    {{ getDueCountdownText(item.dueDate, item.isCompleted) }}
                  </span>
                </div>
              </div>
            </div>

            <div class="text-right shrink-0">
              <div class="font-mono font-black text-sm text-rose-600">
                {{ formatVND(item.remainingAmount) }}
              </div>
              <div class="text-[10px] font-mono text-slate-400">
                ({{ formatShortK(item.remainingAmount) }})
              </div>
            </div>
          </div>

          <div v-if="item.breakdownNote" class="text-xs text-slate-600 font-mono bg-white px-3 py-1.5 rounded-xl border border-slate-200/80">
            {{ item.breakdownNote }}
          </div>

          <div class="flex items-center justify-between gap-2 pt-2 border-t border-slate-200/80">
            <span
              v-if="item.isCompleted"
              class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200"
            >
              ✓ Đã thanh toán
            </span>
            <span
              v-else-if="item.canPayWithIncome"
              class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200"
            >
              ✓ Đủ tiền trả (Dư {{ formatShortK(item.runningBalanceAfter) }})
            </span>
            <span
              v-else
              class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-rose-50 text-rose-700 border border-rose-200"
            >
              ⚠️ Thiếu {{ formatShortK(item.shortfallAmount) }}
            </span>

            <div class="flex items-center gap-1.5 shrink-0">
              <button
                type="button"
                @click="toggleComplete(item)"
                class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all"
                :class="item.isCompleted
                  ? 'bg-white text-slate-600 border-slate-200'
                  : 'bg-emerald-600 text-white border-emerald-600'"
              >
                {{ item.isCompleted ? 'Hoàn tác' : 'Đã trả' }}
              </button>
              <button
                type="button"
                @click="openEditModal(item)"
                class="p-1.5 rounded-xl bg-white border border-slate-200 text-slate-600 hover:text-indigo-600"
              >
                <span class="material-symbols-rounded text-base">edit</span>
              </button>
              <button
                type="button"
                @click="deleteItem(item.id)"
                class="p-1.5 rounded-xl bg-white border border-slate-200 text-slate-500 hover:text-rose-600"
              >
                <span class="material-symbols-rounded text-base">delete</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Desktop Table View -->
      <div v-if="prioritizedDebts.length > 0" class="hidden md:block overflow-x-auto">
        <table class="w-full text-left border-collapse">
          <thead>
            <tr class="border-b border-slate-200 bg-slate-50/80 text-[11px] font-bold uppercase tracking-wider text-slate-500">
              <th class="py-3 px-4">Thứ tự ưu tiên</th>
              <th class="py-3 px-4">Khoản nợ & Chi tiết</th>
              <th class="py-3 px-4">Mốc trả</th>
              <th class="py-3 px-4">Hạn trả</th>
              <th class="py-3 px-4 text-right">Số tiền cần trả</th>
              <th class="py-3 px-4">Đề xuất thanh toán</th>
              <th class="py-3 px-4 text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody class="divide-y divide-slate-200 text-xs">
            <tr
              v-for="(item, index) in prioritizedDebts"
              :key="item.id"
              class="hover:bg-slate-50/80 transition-colors"
              :class="{ 'opacity-50 bg-slate-50': item.isCompleted }"
            >
              <td class="py-3.5 px-4 whitespace-nowrap">
                <div class="flex items-center gap-2">
                  <span
                    class="w-7 h-7 rounded-lg font-mono font-black text-xs flex items-center justify-center"
                    :class="index === 0 && !item.isCompleted
                      ? 'bg-rose-600 text-white shadow-sm'
                      : index === 1 && !item.isCompleted
                      ? 'bg-amber-500 text-white'
                      : 'bg-slate-100 text-slate-700 border border-slate-200'"
                  >
                    #{{ index + 1 }}
                  </span>
                  <span
                    class="px-2.5 py-0.5 rounded-full text-[11px] font-bold border"
                    :class="getPriorityBadgeClass(item.priorityLevel)"
                  >
                    {{ getPriorityLabel(item.priorityLevel) }}
                  </span>
                </div>
              </td>

              <td class="py-3.5 px-4">
                <div class="font-bold text-slate-900 text-sm" :class="{ 'line-through': item.isCompleted }">
                  {{ item.title }}
                </div>
                <div v-if="item.breakdownNote" class="text-xs text-slate-500 font-mono mt-0.5">
                  {{ item.breakdownNote }}
                </div>
              </td>

              <td class="py-3.5 px-4 whitespace-nowrap">
                <span class="px-2.5 py-1 rounded-lg font-mono font-bold text-xs bg-indigo-50 text-indigo-700 border border-indigo-200">
                  Ngày {{ item.periodLabel }}
                </span>
              </td>

              <td class="py-3.5 px-4 whitespace-nowrap">
                <div class="font-mono font-semibold text-slate-800">{{ formatDateFull(item.dueDate) }}</div>
                <div class="text-[11px] font-medium" :class="getDueCountdownColor(item.dueDate, item.isCompleted)">
                  {{ getDueCountdownText(item.dueDate, item.isCompleted) }}
                </div>
              </td>

              <td class="py-3.5 px-4 text-right whitespace-nowrap">
                <div class="font-mono font-black text-sm text-rose-600">
                  {{ formatVND(item.remainingAmount) }}
                </div>
                <div class="text-[11px] font-mono text-slate-400">
                  ({{ formatShortK(item.remainingAmount) }})
                </div>
              </td>

              <td class="py-3.5 px-4">
                <span
                  v-if="item.isCompleted"
                  class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200"
                >
                  ✓ Đã thanh toán xong
                </span>
                <span
                  v-else-if="item.canPayWithIncome"
                  class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200"
                >
                  ✓ Đủ tiền trả ngay (Còn dư {{ formatShortK(item.runningBalanceAfter) }})
                </span>
                <span
                  v-else
                  class="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-bold bg-rose-50 text-rose-700 border border-rose-200"
                >
                  ⚠️ Thiếu {{ formatShortK(item.shortfallAmount) }} (Cần vay thêm)
                </span>
              </td>

              <td class="py-3.5 px-4 text-right whitespace-nowrap">
                <div class="flex items-center justify-end gap-1.5">
                  <button
                    type="button"
                    @click="toggleComplete(item)"
                    class="px-2.5 py-1 rounded-lg text-xs font-bold border transition-all"
                    :class="item.isCompleted
                      ? 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-200'
                      : 'bg-emerald-600 text-white border-emerald-600 hover:bg-emerald-700'"
                  >
                    {{ item.isCompleted ? 'Hoàn tác' : 'Đã trả' }}
                  </button>
                  <button
                    type="button"
                    @click="openEditModal(item)"
                    class="p-1.5 rounded-lg text-slate-500 hover:text-indigo-600 hover:bg-slate-100"
                    title="Sửa"
                  >
                    <span class="material-symbols-rounded text-base">edit</span>
                  </button>
                  <button
                    type="button"
                    @click="deleteItem(item.id)"
                    class="p-1.5 rounded-lg text-slate-500 hover:text-rose-600 hover:bg-rose-50"
                    title="Xóa"
                  >
                    <span class="material-symbols-rounded text-base">delete</span>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </section>

    <!-- ==================== MODAL THÊM / SỬA (BOTTOM SHEET TRÊN ĐIỆN THOẠI) ==================== -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:px-4 bg-slate-900/50 backdrop-blur-sm">
      <div class="w-full max-w-lg max-h-[92vh] overflow-y-auto bg-white border border-slate-200 rounded-t-3xl sm:rounded-3xl p-5 sm:p-6 shadow-2xl transition-all">
        <div class="flex items-center justify-between pb-4 border-b border-slate-100">
          <div class="flex items-center gap-3">
            <div
              class="w-11 h-11 rounded-2xl flex items-center justify-center"
              :class="form.recordType === 'DEBT'
                ? 'bg-rose-50 text-rose-600'
                : form.recordType === 'INCOME'
                ? 'bg-emerald-50 text-emerald-600'
                : 'bg-violet-50 text-violet-600'"
            >
              <span class="material-symbols-rounded text-2xl">
                {{ form.recordType === 'DEBT' ? 'notification_important' : form.recordType === 'INCOME' ? 'south_west' : 'handshake' }}
              </span>
            </div>
            <div>
              <h3 class="text-base font-extrabold text-slate-900">
                {{ isEditing ? 'Chỉnh Sửa Khoản Mục' : getModalTitle(form.recordType) }}
              </h3>
              <p class="text-xs text-slate-500">Hỗ trợ nhập số tiền hoặc phép tính cộng trực tiếp</p>
            </div>
          </div>
          <button type="button" @click="showModal = false" class="text-slate-400 hover:text-slate-700">
            <span class="material-symbols-rounded">close</span>
          </button>
        </div>

        <form @submit.prevent="handleSave" class="mt-4 space-y-4">
          <!-- Chọn loại khoản mục -->
          <div class="grid grid-cols-3 gap-1.5 bg-slate-100 p-1 rounded-2xl border border-slate-200">
            <button
              type="button"
              @click="form.recordType = 'DEBT'"
              class="py-2 rounded-xl text-xs font-bold transition-all"
              :class="form.recordType === 'DEBT' ? 'bg-rose-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              Khoản Nợ
            </button>
            <button
              type="button"
              @click="form.recordType = 'INCOME'"
              class="py-2 rounded-xl text-xs font-bold transition-all"
              :class="form.recordType === 'INCOME' ? 'bg-emerald-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              Tiền Vào (Thu)
            </button>
            <button
              type="button"
              @click="form.recordType = 'BORROW_MORE'"
              class="py-2 rounded-xl text-xs font-bold transition-all"
              :class="form.recordType === 'BORROW_MORE' ? 'bg-violet-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              Vay Thêm Ngoài
            </button>
          </div>

          <!-- Tên khoản mục -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">
              {{ form.recordType === 'DEBT' ? 'Tên khoản nợ' : form.recordType === 'INCOME' ? 'Tên nguồn tiền vào' : 'Tên khoản vay thêm' }}
            </label>
            <input
              v-model="form.title"
              type="text"
              required
              :placeholder="form.recordType === 'DEBT' ? 'Nhập tên khoản nợ...' : form.recordType === 'INCOME' ? 'Nhập tên nguồn tiền vào...' : 'Nhập tên khoản vay thêm...'"
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-slate-900 text-sm font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <!-- Ô nhập số tiền thông minh (hỗ trợ phép cộng 1850 + 600, 811, 10tr...) -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">
              Số tiền (hoặc phép tính cộng nhiều khoản)
            </label>
            <input
              v-model="amountRawInput"
              type="text"
              required
              placeholder="Nhập số tiền..."
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-slate-900 font-mono text-base font-bold focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
            <div class="flex items-center justify-between text-xs mt-1.5 font-mono">
              <span class="text-slate-500">Thành tiền:</span>
              <span class="font-extrabold text-sm" :class="parsedAmount > 0 ? 'text-emerald-700' : 'text-slate-400'">
                = {{ formatVND(parsedAmount) }} ({{ formatShortK(parsedAmount) }})
              </span>
            </div>
          </div>

          <!-- Chọn Mốc Thời Gian & Ngày Đến Hạn -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-bold text-slate-700 mb-1">Mốc thời gian trong tháng</label>
              <select
                v-model="form.periodLabel"
                class="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-slate-900 text-xs font-bold focus:bg-white focus:border-indigo-600 focus:outline-none"
              >
                <option value="1-10">Mốc Ngày 1 - 10 (Đầu tháng)</option>
                <option value="10-21">Mốc Ngày 10 - 21 (Giữa tháng)</option>
                <option value="21-31">Mốc Ngày 21 - 31 (Cuối tháng)</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-bold text-slate-700 mb-1">
                {{ form.recordType === 'DEBT' ? 'Ngày đến hạn trả' : 'Ngày dự kiến tiền về' }}
              </label>
              <input
                v-model="form.dueDate"
                type="date"
                required
                class="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-2xl text-slate-900 text-xs font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
              />
            </div>
          </div>

          <!-- Mức độ ưu tiên (Dành cho Khoản Nợ) -->
          <div v-if="form.recordType === 'DEBT'">
            <label class="block text-xs font-bold text-slate-700 mb-1.5">Mức độ ưu tiên trả</label>
            <div class="grid grid-cols-3 gap-2">
              <button
                v-for="p in [
                  { level: 1, title: 'Ưu tiên 1', desc: 'Cần trả gấp' },
                  { level: 2, title: 'Ưu tiên 2', desc: 'Quan trọng' },
                  { level: 3, title: 'Ưu tiên 3', desc: 'Dài hạn / Linh hoạt' }
                ]"
                :key="p.level"
                type="button"
                @click="form.priorityLevel = p.level"
                class="p-2.5 rounded-2xl border text-left transition-all"
                :class="form.priorityLevel === p.level
                  ? 'border-indigo-600 bg-indigo-50/70 ring-1 ring-indigo-600'
                  : 'border-slate-200 bg-slate-50 hover:bg-slate-100'"
              >
                <div class="text-xs font-bold text-slate-900">{{ p.title }}</div>
                <div class="text-[10px] text-slate-500 mt-0.5">{{ p.desc }}</div>
              </button>
            </div>
          </div>

          <!-- Ghi chú chi tiết -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">Ghi chú chi tiết (tùy chọn)</label>
            <input
              v-model="form.breakdownNote"
              type="text"
              placeholder="Nhập ghi chú thêm nếu có..."
              class="w-full px-4 py-2 bg-slate-50 border border-slate-300 rounded-2xl text-slate-900 text-xs focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <div class="flex items-center justify-end gap-2.5 pt-3 border-t border-slate-100">
            <button
              type="button"
              @click="showModal = false"
              class="px-4 py-2.5 rounded-xl text-xs font-bold text-slate-600 hover:bg-slate-100"
            >
              Hủy
            </button>
            <button
              type="submit"
              :disabled="isSubmitting"
              class="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold shadow-sm transition-all disabled:opacity-50"
            >
              {{ isSubmitting ? 'Đang lưu...' : (isEditing ? 'Lưu Cập Nhật' : 'Lưu Khoản Mục') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import DebtPaymentModal from './DebtPaymentModal.vue';
const localDateKey = d => [d.getFullYear(), String(d.getMonth()+1).padStart(2,'0'), String(d.getDate()).padStart(2,'0')].join('-');
import { ref, computed, onMounted, onUnmounted } from 'vue';
import {
  cloudSyncState,
  getLocalDebts,
  setLocalDebts,
  addDeletedDebtId,
  getLocalCarryOver,
  setLocalCarryOver,
  pushDebtsToCloud,
  syncDebtsBidirectional,
} from '../services/cloudSync';

const emit = defineEmits(['debts-updated']);

const records = ref(getLocalDebts());
const showPayment = ref(false);
const selectedMonth = ref(localDateKey(new Date()).slice(0,7));
const monthRecords = computed(() => records.value.filter(x => (x.dueDate || '').startsWith(selectedMonth.value)));
const paymentSaved = async () => { records.value = await syncDebtsBidirectional(); emit('debts-updated'); window.dispatchEvent(new CustomEvent('secondbrain-data-updated',{detail:{action:'transaction'}})); };
const carryOverBalance = ref(getLocalCarryOver());
const isSubmitting = ref(false);
const debtListFilter = ref('UNPAID');

const showModal = ref(false);
const isEditing = ref(false);
const amountRawInput = ref('');

const form = ref({
  id: null,
  title: '',
  recordType: 'DEBT',
  totalAmount: 0,
  paidAmount: 0,
  breakdownNote: '',
  dueDate: localDateKey(new Date()),
  periodLabel: '1-10',
  priorityLevel: 1,
  isCompleted: false,
  categoryTag: 'Khác',
});

const formatVND = (val) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(val || 0);
};

const formatShortK = (val) => {
  const num = Number(val) || 0;
  const k = Math.round(num / 1000);
  return `${k.toLocaleString('vi-VN')}k`;
};

const formatDateShort = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return `${d.getDate()}/${d.getMonth() + 1}`;
};

const formatDateFull = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return `${String(d.getDate()).padStart(2, '0')}/${String(d.getMonth() + 1).padStart(2, '0')}/${d.getFullYear()}`;
};

// Phân tích linh hoạt số tiền & biểu thức cộng (1850 + 600 + 370, 811, 10tr...)
const parsedAmount = computed(() => {
  const raw = String(amountRawInput.value || '').trim().toLowerCase();
  if (!raw) return 0;

  if (raw.includes('+')) {
    const parts = raw.split('+').map(p => {
      const pClean = p.trim().replace(/[,. ]/g, '');
      return parseFloat(pClean) || 0;
    });
    const sum = parts.reduce((a, b) => a + b, 0);
    if (sum > 0 && sum < 50000) {
      return sum * 1000;
    }
    return sum;
  }

  if (raw.endsWith('tr') || raw.endsWith('triệu') || raw.endsWith('m')) {
    const val = parseFloat(raw.replace(/(tr|triệu|m)/g, '').replace(/,/g, '.'));
    return isNaN(val) ? 0 : Math.round(val * 1000000);
  }

  if (raw.endsWith('k')) {
    const val = parseFloat(raw.replace('k', '').replace(/,/g, '.'));
    return isNaN(val) ? 0 : Math.round(val * 1000);
  }

  const cleanNumber = raw.replace(/[,. ]/g, '');
  const val = parseFloat(cleanNumber);
  if (isNaN(val)) return 0;

  if (val > 0 && val < 50000 && !raw.includes('.') && !raw.includes(',')) {
    return val * 1000;
  }

  return val;
});

const incomeRecords = computed(() => monthRecords.value.filter(r => r.recordType === 'INCOME'));
const debtRecords = computed(() => monthRecords.value.filter(r => r.recordType === 'DEBT'));
const borrowMoreRecords = computed(() => monthRecords.value.filter(r => r.recordType === 'BORROW_MORE'));

const unpaidDebtsCount = computed(() => debtRecords.value.filter(d => !d.isCompleted).length);

const summaryStats = computed(() => {
  const totalIncome = incomeRecords.value.reduce((s, r) => s + (Number(r.totalAmount) || 0), 0);
  const totalAllDebt = debtRecords.value.reduce((s, r) => s + (Number(r.totalAmount) || 0), 0);
  const totalPaidDebt = debtRecords.value.reduce((s, r) => {
    if (r.isCompleted) return s + (Number(r.totalAmount) || 0);
    return s + (Number(r.paidAmount) || 0);
  }, 0);
  const totalUnpaidDebt = Math.max(0, totalAllDebt - totalPaidDebt);
  const totalBorrowedMore = borrowMoreRecords.value.reduce((s, r) => s + (Number(r.totalAmount) || 0), 0);

  const rawBalance = totalIncome - totalUnpaidDebt;
  const netBalanceAfterBorrow = totalIncome + totalBorrowedMore - totalUnpaidDebt;
  const neededToBorrow = netBalanceAfterBorrow < 0 ? Math.abs(netBalanceAfterBorrow) : 0;

  return {
    totalIncome,
    totalAllDebt,
    totalPaidDebt,
    totalUnpaidDebt,
    totalBorrowedMore,
    rawBalance,
    netBalanceAfterBorrow,
    neededToBorrow,
  };
});

const timelineStages = computed(() => {
  const stageConfigs = [
    { id: '1-10', title: 'Đợt 1 (Đầu tháng)', subtitle: 'Từ ngày 01 đến ngày 10' },
    { id: '10-21', title: 'Đợt 2 (Giữa tháng)', subtitle: 'Từ ngày 11 đến ngày 20' },
    { id: '21-31', title: 'Đợt 3 (Cuối tháng / Dài hạn)', subtitle: 'Từ ngày 21 đến cuối tháng' },
  ];

  let runningCarry = 0;

  return stageConfigs.map(cfg => {
    const incomes = incomeRecords.value.filter(r => r.periodLabel === cfg.id);
    const debts = debtRecords.value
      .filter(r => r.periodLabel === cfg.id)
      .sort((a, b) => {
        if (a.isCompleted !== b.isCompleted) return a.isCompleted ? 1 : -1;
        if (a.priorityLevel !== b.priorityLevel) return a.priorityLevel - b.priorityLevel;
        return new Date(a.dueDate) - new Date(b.dueDate);
      });
    const borrows = borrowMoreRecords.value.filter(r => r.periodLabel === cfg.id);

    const incomeTotal = incomes.reduce((s, r) => s + (Number(r.totalAmount) || 0), 0);
    const debtTotal = debts.reduce((s, r) => {
      if (r.isCompleted) return s;
      return s + Math.max(0, (Number(r.totalAmount) || 0) - (Number(r.paidAmount) || 0));
    }, 0);
    const borrowMoreTotal = borrows.reduce((s, r) => s + (Number(r.totalAmount) || 0), 0);

    const incomeFormula = incomes.map(i => Math.round((Number(i.totalAmount) || 0) / 1000)).join(' + ');
    const debtFormula = debts
      .filter(d => !d.isCompleted)
      .map(d => Math.round(((Number(d.totalAmount) || 0) - (Number(d.paidAmount) || 0)) / 1000))
      .join(' + ');

    const carriedFromPrev = carryOverBalance.value ? runningCarry : 0;
    const netResult = carriedFromPrev + incomeTotal + borrowMoreTotal - debtTotal;

    if (carryOverBalance.value) {
      runningCarry = netResult;
    }

    return {
      ...cfg,
      incomes,
      debts,
      borrows,
      incomeTotal,
      debtTotal,
      borrowMoreTotal,
      incomeFormula,
      debtFormula,
      carriedFromPrev,
      netResult,
    };
  });
});

const prioritizedDebts = computed(() => {
  let list = [...debtRecords.value];
  if (debtListFilter.value === 'UNPAID') {
    list = list.filter(d => !d.isCompleted);
  }

  list.sort((a, b) => {
    if (a.isCompleted !== b.isCompleted) return a.isCompleted ? 1 : -1;
    if (a.priorityLevel !== b.priorityLevel) return a.priorityLevel - b.priorityLevel;
    const dateDiff = new Date(a.dueDate) - new Date(b.dueDate);
    if (dateDiff !== 0) return dateDiff;
    return (Number(a.totalAmount) || 0) - (Number(b.totalAmount) || 0);
  });

  let availablePool = summaryStats.value.totalIncome + summaryStats.value.totalBorrowedMore;

  return list.map(d => {
    const remainingAmount = d.isCompleted ? 0 : Math.max(0, (Number(d.totalAmount) || 0) - (Number(d.paidAmount) || 0));
    let canPayWithIncome = true;
    let shortfallAmount = 0;

    if (!d.isCompleted) {
      if (availablePool >= remainingAmount) {
        availablePool -= remainingAmount;
        canPayWithIncome = true;
      } else {
        shortfallAmount = remainingAmount - Math.max(0, availablePool);
        availablePool -= remainingAmount;
        canPayWithIncome = false;
      }
    }

    return {
      ...d,
      remainingAmount,
      canPayWithIncome,
      runningBalanceAfter: availablePool,
      shortfallAmount,
    };
  });
});

const getPriorityLabel = (level) => {
  if (level === 1) return 'Ưu tiên 1 • Gấp';
  if (level === 2) return 'Ưu tiên 2 • Quan trọng';
  return 'Ưu tiên 3 • Dài hạn';
};

const getPriorityBadgeClass = (level) => {
  if (level === 1) return 'bg-rose-50 text-rose-700 border-rose-200';
  if (level === 2) return 'bg-amber-50 text-amber-700 border-amber-200';
  return 'bg-slate-100 text-slate-600 border-slate-200';
};

const getDueCountdownText = (dueDateStr, isCompleted) => {
  if (isCompleted) return 'Đã xong';
  if (!dueDateStr) return '';
  const target = new Date(dueDateStr);
  const now = new Date();
  target.setHours(0, 0, 0, 0);
  now.setHours(0, 0, 0, 0);
  const diffDays = Math.ceil((target - now) / (1000 * 60 * 60 * 24));
  if (diffDays < 0) return `Quá hạn ${Math.abs(diffDays)} ngày`;
  if (diffDays === 0) return 'Đến hạn hôm nay';
  return `Còn ${diffDays} ngày`;
};

const getDueCountdownColor = (dueDateStr, isCompleted) => {
  if (isCompleted) return 'text-emerald-600';
  if (!dueDateStr) return 'text-slate-500';
  const target = new Date(dueDateStr);
  const now = new Date();
  target.setHours(0, 0, 0, 0);
  now.setHours(0, 0, 0, 0);
  const diffDays = Math.ceil((target - now) / (1000 * 60 * 60 * 24));
  if (diffDays < 0) return 'text-rose-600 font-bold';
  if (diffDays <= 3) return 'text-amber-600 font-bold';
  return 'text-slate-500';
};

const getModalTitle = (type) => {
  if (type === 'DEBT') return 'Thêm Khoản Nợ Phải Trả';
  if (type === 'INCOME') return 'Thêm Khoản Tiền Vào (Thu Nhập)';
  return 'Thêm Khoản Vay Thêm Ở Ngoài';
};

const notifyAndPushCloud = async () => {
  setLocalDebts(records.value);
  emit('debts-updated');
  window.dispatchEvent(new CustomEvent('secondbrain-data-updated', { detail: { action: 'debt' } }));
  await pushDebtsToCloud(records.value, carryOverBalance.value);
};

const handleCarryOverChange = async () => {
  setLocalCarryOver(carryOverBalance.value);
  await pushDebtsToCloud(records.value, carryOverBalance.value);
};

const openModal = (type = 'DEBT', defaultPeriod = '1-10') => {
  isEditing.value = false;
  amountRawInput.value = '';
  form.value = {
    id: null,
    title: '',
    recordType: type,
    totalAmount: 0,
    paidAmount: 0,
    breakdownNote: '',
    dueDate: localDateKey(new Date()),
    periodLabel: defaultPeriod,
    priorityLevel: type === 'DEBT' ? 1 : 2,
    isCompleted: false,
    categoryTag: 'Khác',
  };
  showModal.value = true;
};

const openQuickBorrowForShortfall = (shortfallAmount, periodId) => {
  isEditing.value = false;
  amountRawInput.value = String(shortfallAmount);
  form.value = {
    id: null,
    title: `Vay thêm bù đợt ${periodId}`,
    recordType: 'BORROW_MORE',
    totalAmount: shortfallAmount,
    paidAmount: 0,
    breakdownNote: `Bù khoản thiếu mốc ${periodId}`,
    dueDate: localDateKey(new Date()),
    periodLabel: periodId,
    priorityLevel: 1,
    isCompleted: false,
    categoryTag: 'Vay ngoài',
  };
  showModal.value = true;
};

const openEditModal = (item) => {
  isEditing.value = true;
  amountRawInput.value = String(item.totalAmount);
  form.value = {
    id: item.id,
    title: item.title,
    recordType: item.recordType || 'DEBT',
    totalAmount: item.totalAmount,
    paidAmount: item.paidAmount || 0,
    breakdownNote: item.breakdownNote || '',
    dueDate: item.dueDate ? item.dueDate.split('T')[0] : localDateKey(new Date()),
    periodLabel: item.periodLabel || '1-10',
    priorityLevel: item.priorityLevel || 2,
    isCompleted: item.isCompleted || false,
    categoryTag: item.categoryTag || 'Khác',
  };
  showModal.value = true;
};

const handleSave = async () => {
  const title = form.value.title.trim();
  const amount = parsedAmount.value;

  if (!title) {
    alert('Vui lòng nhập tên khoản mục!');
    return;
  }
  if (!amount || amount <= 0) {
    alert('Vui lòng nhập số tiền hợp lệ lớn hơn 0!');
    return;
  }

  isSubmitting.value = true;
  try {
    const rawExpression = String(amountRawInput.value || '').trim();
    const hasMath = rawExpression.includes('+');

    const itemToSave = {
      id: isEditing.value && form.value.id
        ? form.value.id
        : (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'debt_' + Date.now()),
      title,
      recordType: form.value.recordType,
      totalAmount: amount,
      paidAmount: Number(form.value.paidAmount) || 0,
      breakdownNote: form.value.breakdownNote?.trim() || (hasMath ? rawExpression : ''),
      dueDate: form.value.dueDate ? form.value.dueDate + 'T00:00:00' : new Date().toISOString(),
      periodLabel: form.value.periodLabel || '1-10',
      priorityLevel: Number(form.value.priorityLevel) || 2,
      isCompleted: form.value.isCompleted || false,
      categoryTag: form.value.categoryTag || 'Khác',
      createdAt: new Date().toISOString(),
    };

    if (isEditing.value) {
      const idx = records.value.findIndex(x => String(x.id) === String(itemToSave.id));
      if (idx !== -1) {
        records.value[idx] = itemToSave;
      } else {
        records.value.unshift(itemToSave);
      }
    } else {
      records.value.unshift(itemToSave);
    }

    showModal.value = false;
    await notifyAndPushCloud();
  } finally {
    isSubmitting.value = false;
  }
};

const toggleComplete = async (item) => {
  item.isCompleted = !item.isCompleted;
  item.paidAmount = item.isCompleted ? item.totalAmount : 0;
  await notifyAndPushCloud();
};

const deleteItem = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa khoản mục này?')) return;
  addDeletedDebtId(id);
  records.value = records.value.filter(r => String(r.id) !== String(id));
  await notifyAndPushCloud();
};

const refreshFromLocal = () => {
  records.value = getLocalDebts();
  carryOverBalance.value = getLocalCarryOver();
};

onMounted(async () => {
  refreshFromLocal();
  window.addEventListener('secondbrain-cloud-synced', refreshFromLocal);
  const synced = await syncDebtsBidirectional();
  records.value = synced;
  carryOverBalance.value = getLocalCarryOver();
});

onUnmounted(() => {
  window.removeEventListener('secondbrain-cloud-synced', refreshFromLocal);
});
</script>
