<template>
  <div class="space-y-6">
    <!-- ==================== 1. HEADER BAR (CLEAN REALTIME ORDERS STYLE) ==================== -->
    <header class="bg-white border border-slate-200 rounded-2xl px-6 py-4 flex flex-col lg:flex-row lg:items-center justify-between gap-4 shadow-sm">
      <div class="flex items-center gap-3.5">
        <div class="w-11 h-11 rounded-2xl bg-indigo-50 text-indigo-600 border border-indigo-100 flex items-center justify-center shrink-0">
          <span class="material-symbols-rounded text-2xl">receipt_long</span>
        </div>
        <div>
          <div class="flex items-center gap-2">
            <h2 class="text-lg font-extrabold text-slate-900 tracking-tight">Quản Lý Khoản Nợ & Dòng Tiền Theo Mốc</h2>
            <span class="px-2.5 py-0.5 rounded-full text-[11px] font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
              Tự động xếp ưu tiên
            </span>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Phân bổ tiền vào & nợ đến hạn theo từng giai đoạn (1-10, 10-21, 21-31) • Tính toán dư / thiếu & vay bù đắp
          </p>
        </div>
      </div>

      <!-- Action Buttons (Đã bỏ nút mẫu theo yêu cầu) -->
      <div class="flex items-center gap-2.5 flex-wrap">
        <button
          type="button"
          @click="openModal('INCOME')"
          class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white text-xs font-bold transition-all active:scale-95 shadow-sm"
        >
          <span class="material-symbols-rounded text-base">add_circle</span>
          <span>+ Thêm Tiền Vào</span>
        </button>

        <button
          type="button"
          @click="openModal('DEBT')"
          class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold transition-all active:scale-95 shadow-sm"
        >
          <span class="material-symbols-rounded text-base">notification_important</span>
          <span>+ Nhập Khoản Nợ</span>
        </button>

        <button
          type="button"
          @click="openModal('BORROW_MORE')"
          class="flex items-center gap-1.5 px-4 py-2 rounded-xl bg-violet-600 hover:bg-violet-700 text-white text-xs font-bold transition-all active:scale-95 shadow-sm"
        >
          <span class="material-symbols-rounded text-base">handshake</span>
          <span>+ Vay Thêm (Ở Ngoài)</span>
        </button>
      </div>
    </header>

    <!-- ==================== 2. KPI SUMMARY CARDS ==================== -->
    <div class="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
      <!-- Card 1: Tổng Tiền Vào -->
      <div class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500">Tổng Tiền Vào (Thu)</span>
          <div class="w-9 h-9 rounded-xl bg-emerald-50 text-emerald-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">south_west</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-2xl font-black font-mono text-emerald-600">
            +{{ formatVND(summaryStats.totalIncome) }}
          </div>
          <div class="text-[11px] font-mono text-slate-400">
            (Tương đương {{ formatShortK(summaryStats.totalIncome) }})
          </div>
        </div>
        <div class="text-xs text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Nguồn thu dự kiến</span>
          <span class="font-semibold text-emerald-700">{{ incomeRecords.length }} khoản</span>
        </div>
      </div>

      <!-- Card 2: Tổng Nợ Phải Trả Đến Hạn -->
      <div class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500">Tổng Nợ Phải Trả</span>
          <div class="w-9 h-9 rounded-xl bg-rose-50 text-rose-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">north_east</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-2xl font-black font-mono text-rose-600">
            -{{ formatVND(summaryStats.totalUnpaidDebt) }}
          </div>
          <div class="text-[11px] font-mono text-slate-400">
            Tổng gốc: {{ formatShortK(summaryStats.totalAllDebt) }} • Đã trả: {{ formatShortK(summaryStats.totalPaidDebt) }}
          </div>
        </div>
        <div class="text-xs text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Khoản nợ chưa tất toán</span>
          <span class="font-semibold text-rose-600">{{ unpaidDebtsCount }} khoản nợ</span>
        </div>
      </div>

      <!-- Card 3: Mục Vay Thêm Ở Ngoài -->
      <div class="bg-white border border-slate-200 rounded-2xl p-5 shadow-sm flex flex-col justify-between">
        <div class="flex items-center justify-between">
          <span class="text-xs font-bold uppercase tracking-wider text-slate-500">Vay Thêm Ở Ngoài</span>
          <div class="w-9 h-9 rounded-xl bg-violet-50 text-violet-600 flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">account_balance</span>
          </div>
        </div>
        <div class="my-2">
          <div class="text-2xl font-black font-mono text-violet-600">
            +{{ formatVND(summaryStats.totalBorrowedMore) }}
          </div>
          <div class="text-[11px] text-slate-400">
            <span v-if="summaryStats.neededToBorrow > 0" class="text-rose-600 font-bold">
              Cần vay bù thêm: {{ formatVND(summaryStats.neededToBorrow) }}
            </span>
            <span v-else class="text-emerald-600 font-medium">
              Đủ khả năng chi trả, không cần vay thêm
            </span>
          </div>
        </div>
        <div class="text-xs text-slate-500 pt-2 border-t border-slate-100 flex items-center justify-between">
          <span>Đi vay ngoài đắp vào</span>
          <button
            @click="openModal('BORROW_MORE')"
            class="text-violet-600 font-bold hover:underline"
          >
            + Ghi vay thêm
          </button>
        </div>
      </div>

      <!-- Card 4: Kết Quả Cân Đối (Dư hoặc Thiếu) -->
      <div
        class="rounded-2xl p-5 shadow-sm border flex flex-col justify-between"
        :class="summaryStats.netBalanceAfterBorrow >= 0 
          ? 'bg-emerald-50/60 border-emerald-200' 
          : 'bg-rose-50/70 border-rose-200'"
      >
        <div class="flex items-center justify-between">
          <span
            class="text-xs font-bold uppercase tracking-wider"
            :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'text-emerald-800' : 'text-rose-800'"
          >
            {{ summaryStats.netBalanceAfterBorrow >= 0 ? 'Tiền Dư Còn Lại' : 'Đang Thiếu (Cần Vay Thêm)' }}
          </span>
          <div
            class="w-9 h-9 rounded-xl flex items-center justify-center"
            :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'bg-emerald-600 text-white' : 'bg-rose-600 text-white'"
          >
            <span class="material-symbols-rounded text-lg">
              {{ summaryStats.netBalanceAfterBorrow >= 0 ? 'savings' : 'warning' }}
            </span>
          </div>
        </div>

        <div class="my-2">
          <div
            class="text-2xl font-black font-mono"
            :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'text-emerald-700' : 'text-rose-700'"
          >
            {{ summaryStats.netBalanceAfterBorrow >= 0 ? '+' : '' }}{{ formatVND(summaryStats.netBalanceAfterBorrow) }}
          </div>
          <div class="text-[11px] font-medium" :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'text-emerald-600' : 'text-rose-600'">
            {{ summaryStats.netBalanceAfterBorrow >= 0 
              ? `Sau khi trả hết nợ, bạn dư ${formatShortK(summaryStats.netBalanceAfterBorrow)}` 
              : `Thiếu ${formatShortK(Math.abs(summaryStats.netBalanceAfterBorrow))} -> Cần vay ngoài bù đắp` }}
          </div>
        </div>

        <div class="pt-2 border-t" :class="summaryStats.netBalanceAfterBorrow >= 0 ? 'border-emerald-200/70' : 'border-rose-200/70'">
          <button
            v-if="summaryStats.netBalanceAfterBorrow < 0"
            @click="openQuickBorrowForShortfall(Math.abs(summaryStats.netBalanceAfterBorrow), '1-10')"
            class="w-full py-1.5 px-3 rounded-lg bg-rose-600 hover:bg-rose-700 text-white text-xs font-bold flex items-center justify-center gap-1 shadow-sm transition-all"
          >
            <span class="material-symbols-rounded text-sm">add_card</span>
            <span>Tạo khoản Vay Thêm {{ formatShortK(Math.abs(summaryStats.netBalanceAfterBorrow)) }}</span>
          </button>
          <div v-else class="text-xs text-emerald-700 font-semibold flex items-center gap-1">
            <span class="material-symbols-rounded text-sm">check_circle</span>
            <span>Dòng tiền an toàn</span>
          </div>
        </div>
      </div>
    </div>

    <!-- ==================== 3. BẢNG CHIA MỐC THỜI GIAN (1-10, 10-21, 21-31) ==================== -->
    <section class="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm space-y-5">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-slate-100">
        <div>
          <div class="flex items-center gap-2">
            <span class="material-symbols-rounded text-indigo-600 text-xl">view_timeline</span>
            <h3 class="text-base font-extrabold text-slate-900 uppercase tracking-wide">
              Bảng Cân Đối Dòng Tiền Theo Mốc Thời Gian
            </h3>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Tách theo từng mốc để biết chính xác đợt nào dư hay thiếu tiền
          </p>
        </div>

        <!-- Nút bật/tắt cộng dồn số dư mốc trước sang mốc sau -->
        <label class="flex items-center gap-2 cursor-pointer bg-slate-50 hover:bg-slate-100 px-3.5 py-2 rounded-xl border border-slate-200 text-xs font-semibold text-slate-700 select-none">
          <input
            v-model="carryOverBalance"
            type="checkbox"
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
          class="rounded-2xl border bg-slate-50/50 flex flex-col justify-between overflow-hidden transition-all"
          :class="stage.netResult >= 0 ? 'border-slate-200' : 'border-rose-300 ring-1 ring-rose-200'"
        >
          <!-- Header Mốc -->
          <div>
            <div class="px-5 py-3.5 bg-white border-b border-slate-200 flex items-center justify-between">
              <div class="flex items-center gap-2.5">
                <span class="px-2.5 py-1 rounded-lg font-mono font-black text-xs bg-indigo-600 text-white shadow-sm">
                  Ngày {{ stage.id }}
                </span>
                <div>
                  <h4 class="font-bold text-sm text-slate-900">{{ stage.title }}</h4>
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

            <!-- Khối công thức tóm tắt dòng tiền -->
            <div class="mx-4 mt-4 p-3.5 rounded-xl bg-white border border-slate-200 font-mono text-xs space-y-1.5 shadow-sm">
              <div v-if="carryOverBalance && stage.carriedFromPrev !== 0" class="flex items-center justify-between text-slate-500 pb-1 border-b border-dashed border-slate-200">
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
                  {{ stage.netResult >= 0 ? 'Còn dư:' : 'Thiếu (cần vay):' }}
                </span>
                <span :class="stage.netResult >= 0 ? 'text-emerald-600' : 'text-rose-600'">
                  {{ stage.netResult >= 0 ? '+' : '' }}{{ formatShortK(stage.netResult) }}
                  <span class="text-[11px] font-normal text-slate-500">({{ formatVND(stage.netResult) }})</span>
                </span>
              </div>
            </div>

            <!-- Danh sách chi tiết các khoản trong Mốc này -->
            <div class="p-4 space-y-3">
              <!-- Các khoản TIỀN VÀO -->
              <div>
                <div class="flex items-center justify-between text-[11px] font-bold uppercase tracking-wider text-emerald-700 mb-1.5">
                  <span>Tiền vào trong mốc ({{ stage.incomes.length }})</span>
                  <button @click="openModal('INCOME', stage.id)" class="hover:underline">+ Thêm thu</button>
                </div>

                <div v-if="stage.incomes.length === 0" class="text-xs text-slate-400 italic py-1">
                  Chưa có khoản thu nào ở mốc {{ stage.id }}
                </div>
                <div v-else class="space-y-1.5">
                  <div
                    v-for="inc in stage.incomes"
                    :key="inc.id"
                    class="px-3 py-2 rounded-xl bg-white border border-emerald-200/80 flex items-center justify-between gap-2 text-xs shadow-sm"
                  >
                    <div class="min-w-0">
                      <div class="font-bold text-slate-800 truncate">{{ inc.title }}</div>
                      <div v-if="inc.breakdownNote" class="text-[11px] text-slate-500 font-mono truncate">
                        {{ inc.breakdownNote }}
                      </div>
                    </div>
                    <div class="flex items-center gap-1.5 shrink-0">
                      <span class="font-mono font-bold text-emerald-600">+{{ formatShortK(inc.totalAmount) }}</span>
                      <button @click="openEditModal(inc)" class="text-slate-400 hover:text-indigo-600 p-1">
                        <span class="material-symbols-rounded text-sm">edit</span>
                      </button>
                      <button @click="deleteItem(inc.id)" class="text-slate-400 hover:text-rose-600 p-1">
                        <span class="material-symbols-rounded text-sm">close</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Các khoản NỢ PHẢI TRẢ -->
              <div class="pt-2 border-t border-slate-200/80">
                <div class="flex items-center justify-between text-[11px] font-bold uppercase tracking-wider text-rose-700 mb-1.5">
                  <span>Khoản nợ đến hạn trả ({{ stage.debts.length }})</span>
                  <button @click="openModal('DEBT', stage.id)" class="hover:underline">+ Thêm nợ</button>
                </div>

                <div v-if="stage.debts.length === 0" class="text-xs text-slate-400 italic py-1">
                  Không có khoản nợ nào đến hạn ở mốc {{ stage.id }}
                </div>
                <div v-else class="space-y-1.5">
                  <div
                    v-for="(debt, idx) in stage.debts"
                    :key="debt.id"
                    class="px-3 py-2 rounded-xl bg-white border flex items-center justify-between gap-2 text-xs shadow-sm transition-all"
                    :class="debt.isCompleted ? 'opacity-50 border-slate-200 bg-slate-50' : 'border-rose-200/90'"
                  >
                    <div class="flex items-center gap-2 min-w-0">
                      <button
                        @click="toggleComplete(debt)"
                        class="w-5 h-5 rounded-md border flex items-center justify-center shrink-0 transition-colors"
                        :class="debt.isCompleted ? 'bg-emerald-600 border-emerald-600 text-white' : 'border-slate-300 hover:border-emerald-600 text-transparent'"
                        title="Đánh dấu đã trả xong"
                      >
                        <span class="material-symbols-rounded text-xs font-bold">check</span>
                      </button>
                      <div class="min-w-0">
                        <div class="flex items-center gap-1.5">
                          <span class="px-1.5 py-0.2 rounded text-[10px] font-extrabold bg-slate-100 text-slate-700">
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

                    <div class="flex items-center gap-1.5 shrink-0">
                      <span class="font-mono font-bold text-rose-600">-{{ formatShortK(debt.totalAmount - (debt.paidAmount || 0)) }}</span>
                      <button @click="openEditModal(debt)" class="text-slate-400 hover:text-indigo-600 p-1">
                        <span class="material-symbols-rounded text-sm">edit</span>
                      </button>
                      <button @click="deleteItem(debt.id)" class="text-slate-400 hover:text-rose-600 p-1">
                        <span class="material-symbols-rounded text-sm">close</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>

              <!-- Các khoản VAY THÊM Ở NGOÀI -->
              <div v-if="stage.borrows.length > 0" class="pt-2 border-t border-slate-200/80">
                <div class="flex items-center justify-between text-[11px] font-bold uppercase tracking-wider text-violet-700 mb-1.5">
                  <span>Khoản vay thêm ở ngoài ({{ stage.borrows.length }})</span>
                </div>
                <div class="space-y-1.5">
                  <div
                    v-for="b in stage.borrows"
                    :key="b.id"
                    class="px-3 py-2 rounded-xl bg-violet-50/70 border border-violet-200 flex items-center justify-between gap-2 text-xs"
                  >
                    <div class="min-w-0">
                      <div class="font-bold text-violet-950 truncate">{{ b.title }}</div>
                      <div v-if="b.breakdownNote" class="text-[10px] text-violet-700 truncate">{{ b.breakdownNote }}</div>
                    </div>
                    <div class="flex items-center gap-1.5 shrink-0">
                      <span class="font-mono font-bold text-violet-700">+{{ formatShortK(b.totalAmount) }}</span>
                      <button @click="openEditModal(b)" class="text-violet-400 hover:text-violet-800 p-1">
                        <span class="material-symbols-rounded text-sm">edit</span>
                      </button>
                      <button @click="deleteItem(b.id)" class="text-violet-400 hover:text-rose-600 p-1">
                        <span class="material-symbols-rounded text-sm">close</span>
                      </button>
                    </div>
                  </div>
                </div>
              </div>
            </div>
          </div>

          <!-- Footer Cảnh Báo Thiếu Tiền hoặc Báo Dư -->
          <div class="p-4 border-t border-slate-200 bg-white">
            <div v-if="stage.netResult < 0" class="space-y-2">
              <div class="flex items-center justify-between text-xs text-rose-700 font-bold">
                <span>⚠️ Mốc {{ stage.id }} đang thiếu:</span>
                <span class="font-mono text-sm">{{ formatVND(Math.abs(stage.netResult)) }}</span>
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
              <span class="text-slate-600 font-semibold">✅ Sau khi trả hết nợ mốc {{ stage.id }}, còn dư:</span>
              <span class="font-mono font-black text-sm text-emerald-600">+{{ formatVND(stage.netResult) }}</span>
            </div>
          </div>
        </div>
      </div>
    </section>

    <!-- ==================== 4. DANH SÁCH XẾP HẠNG ƯU TIÊN TRẢ NỢ ==================== -->
    <section class="bg-white border border-slate-200 rounded-2xl p-6 shadow-sm space-y-4">
      <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-3 pb-4 border-b border-slate-100">
        <div>
          <div class="flex items-center gap-2">
            <span class="material-symbols-rounded text-rose-600 text-xl">priority_high</span>
            <h3 class="text-base font-extrabold text-slate-900 uppercase tracking-wide">
              Bảng Xếp Hạng Khoản Nợ Ưu Tiên Trả Trước
            </h3>
          </div>
          <p class="text-xs text-slate-500 mt-0.5">
            Tự động xếp hạng ưu tiên theo: Hạn gần nhất/quá hạn → Mức độ bắt buộc → Khả năng chi trả bằng nguồn thu
          </p>
        </div>

        <div class="flex items-center gap-2">
          <span class="text-xs text-slate-500">Hiển thị:</span>
          <div class="flex bg-slate-100 p-1 rounded-xl border border-slate-200">
            <button
              v-for="tab in [
                { id: 'UNPAID', label: 'Chưa trả (' + unpaidDebtsCount + ')' },
                { id: 'ALL', label: 'Tất cả nợ (' + debtRecords.length + ')' }
              ]"
              :key="tab.id"
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
        <p class="text-sm font-bold text-slate-800">Chưa có khoản nợ nào!</p>
        <p class="text-xs text-slate-500 mt-1">Bấm nút "+ Nhập Khoản Nợ" ở góc trên để thêm khoản nợ cần theo dõi.</p>
      </div>

      <!-- Mobile Card View (Tối ưu cho Điện Thoại - Không cần cuộn ngang) -->
      <div v-else class="space-y-3 md:hidden">
        <div
          v-for="(item, index) in prioritizedDebts"
          :key="'m_' + item.id"
          class="p-4 rounded-2xl border bg-slate-50/60 space-y-3 transition-all"
          :class="item.isCompleted ? 'opacity-50 border-slate-200' : 'border-slate-200'"
        >
          <div class="flex items-start justify-between gap-2">
            <div class="flex items-center gap-2 min-w-0">
              <span
                class="w-7 h-7 rounded-lg font-mono font-black text-xs flex items-center justify-center shrink-0"
                :class="index === 0 && !item.isCompleted
                  ? 'bg-rose-600 text-white shadow-sm'
                  : index === 1 && !item.isCompleted
                  ? 'bg-amber-500 text-white'
                  : 'bg-white text-slate-700 border border-slate-200'"
              >
                #{{ index + 1 }}
              </span>
              <div class="min-w-0">
                <div class="font-bold text-slate-900 text-sm truncate" :class="{ 'line-through': item.isCompleted }">
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
            Chi tiết: {{ item.breakdownNote }}
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
                @click="toggleComplete(item)"
                class="px-3 py-1.5 rounded-xl text-xs font-bold border transition-all"
                :class="item.isCompleted 
                  ? 'bg-white text-slate-600 border-slate-200' 
                  : 'bg-emerald-600 text-white border-emerald-600'"
              >
                {{ item.isCompleted ? 'Hoàn tác' : 'Đã trả' }}
              </button>
              <button
                @click="openEditModal(item)"
                class="p-1.5 rounded-xl bg-white border border-slate-200 text-slate-600 hover:text-indigo-600"
              >
                <span class="material-symbols-rounded text-base">edit</span>
              </button>
              <button
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
            <tr class="border-b border-slate-200 bg-slate-50 text-[11px] font-bold uppercase tracking-wider text-slate-500">
              <th class="py-3 px-4">Thứ tự ưu tiên</th>
              <th class="py-3 px-4">Khoản nợ & Chi tiết</th>
              <th class="py-3 px-4">Mốc trả</th>
              <th class="py-3 px-4">Hạn trả</th>
              <th class="py-3 px-4 text-right">Số tiền phải trả</th>
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
              <!-- Cột 1: Thứ tự ưu tiên -->
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

              <!-- Cột 2: Tên khoản nợ -->
              <td class="py-3.5 px-4">
                <div class="font-bold text-slate-900 text-sm" :class="{ 'line-through': item.isCompleted }">
                  {{ item.title }}
                </div>
                <div v-if="item.breakdownNote" class="text-xs text-slate-500 font-mono mt-0.5">
                  Chi tiết: {{ item.breakdownNote }}
                </div>
              </td>

              <!-- Cột 3: Mốc thời gian -->
              <td class="py-3.5 px-4 whitespace-nowrap">
                <span class="px-2.5 py-1 rounded-lg font-mono font-bold text-xs bg-indigo-50 text-indigo-700 border border-indigo-200">
                  Ngày {{ item.periodLabel }}
                </span>
              </td>

              <!-- Cột 4: Hạn trả & Đếm ngược -->
              <td class="py-3.5 px-4 whitespace-nowrap">
                <div class="font-mono font-semibold text-slate-800">{{ formatDateFull(item.dueDate) }}</div>
                <div class="text-[11px] font-medium" :class="getDueCountdownColor(item.dueDate, item.isCompleted)">
                  {{ getDueCountdownText(item.dueDate, item.isCompleted) }}
                </div>
              </td>

              <!-- Cột 5: Số tiền -->
              <td class="py-3.5 px-4 text-right whitespace-nowrap">
                <div class="font-mono font-black text-sm text-rose-600">
                  {{ formatVND(item.remainingAmount) }}
                </div>
                <div class="text-[11px] font-mono text-slate-400">
                  ({{ formatShortK(item.remainingAmount) }})
                </div>
              </td>

              <!-- Cột 6: Khuyến nghị trả trước hay cần vay -->
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

              <!-- Cột 7: Actions -->
              <td class="py-3.5 px-4 text-right whitespace-nowrap">
                <div class="flex items-center justify-end gap-1.5">
                  <button
                    @click="toggleComplete(item)"
                    class="px-2.5 py-1 rounded-lg text-xs font-bold border transition-all"
                    :class="item.isCompleted 
                      ? 'bg-slate-100 text-slate-600 border-slate-200 hover:bg-slate-200' 
                      : 'bg-emerald-600 text-white border-emerald-600 hover:bg-emerald-700'"
                  >
                    {{ item.isCompleted ? 'Hoàn tác' : 'Đã trả' }}
                  </button>
                  <button
                    @click="openEditModal(item)"
                    class="p-1.5 rounded-lg text-slate-500 hover:text-indigo-600 hover:bg-slate-100"
                    title="Sửa"
                  >
                    <span class="material-symbols-rounded text-base">edit</span>
                  </button>
                  <button
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

    <!-- ==================== MODAL THÊM / SỬA (TỐI ƯU MOBILE BOTTOM SHEET) ==================== -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:px-4 bg-slate-900/50 backdrop-blur-sm">
      <div class="w-full max-w-lg max-h-[92vh] overflow-y-auto bg-white border border-slate-200 rounded-t-3xl sm:rounded-3xl p-5 sm:p-6 shadow-xl transition-all">
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
              <p class="text-xs text-slate-500">Nhập số tiền hoặc phép tính cộng trừ linh hoạt</p>
            </div>
          </div>
          <button @click="showModal = false" class="text-slate-400 hover:text-slate-700">
            <span class="material-symbols-rounded">close</span>
          </button>
        </div>

        <form @submit.prevent="handleSave" class="mt-4 space-y-4">
          <!-- Chọn loại khoản mục -->
          <div class="grid grid-cols-3 gap-2 bg-slate-100 p-1 rounded-xl border border-slate-200">
            <button
              type="button"
              @click="form.recordType = 'DEBT'"
              class="py-2 rounded-lg text-xs font-bold transition-all"
              :class="form.recordType === 'DEBT' ? 'bg-rose-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              Khoản Nợ Phải Trả
            </button>
            <button
              type="button"
              @click="form.recordType = 'INCOME'"
              class="py-2 rounded-lg text-xs font-bold transition-all"
              :class="form.recordType === 'INCOME' ? 'bg-emerald-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              Tiền Vào (Thu Nhập)
            </button>
            <button
              type="button"
              @click="form.recordType = 'BORROW_MORE'"
              class="py-2 rounded-lg text-xs font-bold transition-all"
              :class="form.recordType === 'BORROW_MORE' ? 'bg-violet-600 text-white shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            >
              Vay Thêm Ở Ngoài
            </button>
          </div>

          <!-- Tên khoản mục (Bỏ các phần (VD: ...)) -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">
              {{ form.recordType === 'DEBT' ? 'Tên khoản nợ' : form.recordType === 'INCOME' ? 'Tên nguồn tiền vào' : 'Tên khoản vay thêm ở ngoài' }}
            </label>
            <input
              v-model="form.title"
              type="text"
              required
              :placeholder="form.recordType === 'DEBT' ? 'Nhập tên khoản nợ...' : form.recordType === 'INCOME' ? 'Nhập tên nguồn tiền vào...' : 'Nhập tên khoản vay thêm...'"
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-sm focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <!-- Ô nhập số tiền DUY NHẤT: Thông minh, hỗ trợ cả số tiền lẫn phép tính (1000k, 1850 + 600, 2.000.000, 10tr) -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">
              Số tiền (VNĐ hoặc phép tính cộng)
            </label>
            <input
              v-model="amountRawInput"
              type="text"
              required
              placeholder="Nhập số tiền..."
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 font-mono text-base font-bold focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
            <div class="flex items-center justify-between text-xs mt-1.5 font-mono">
              <span class="text-slate-500">Quy đổi:</span>
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
                class="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-bold focus:bg-white focus:border-indigo-600 focus:outline-none"
              >
                <option value="1-10">Mốc Ngày 1 - 10</option>
                <option value="10-21">Mốc Ngày 10 - 21</option>
                <option value="21-31">Mốc Ngày 21 - 31</option>
              </select>
            </div>

            <div>
              <label class="block text-xs font-bold text-slate-700 mb-1">
                {{ form.recordType === 'DEBT' ? 'Ngày đến hạn phải trả' : 'Ngày dự kiến tiền về' }}
              </label>
              <input
                v-model="form.dueDate"
                type="date"
                required
                class="w-full px-3.5 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
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
                  { level: 3, title: 'Ưu tiên 3', desc: 'Thong thả / Dài hạn' }
                ]"
                :key="p.level"
                type="button"
                @click="form.priorityLevel = p.level"
                class="p-2.5 rounded-xl border text-left transition-all"
                :class="form.priorityLevel === p.level 
                  ? 'border-indigo-600 bg-indigo-50/70 ring-1 ring-indigo-600' 
                  : 'border-slate-200 bg-slate-50 hover:bg-slate-100'"
              >
                <div class="text-xs font-bold text-slate-900">{{ p.title }}</div>
                <div class="text-[10px] text-slate-500 mt-0.5">{{ p.desc }}</div>
              </button>
            </div>
          </div>

          <!-- Ghi chú chi tiết (Bỏ placeholder ví dụ) -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">Ghi chú chi tiết (tùy chọn)</label>
            <input
              v-model="form.breakdownNote"
              type="text"
              placeholder="Ghi chú thêm nếu có..."
              class="w-full px-4 py-2 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
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
              :disabled="isSubmitting"
              class="px-5 py-2.5 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold shadow-sm transition-all disabled:opacity-50"
            >
              {{ isSubmitting ? 'Đang lưu...' : (isEditing ? 'Lưu Cập Nhật' : 'Tạo Khoản Mục') }}
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

const emit = defineEmits(['debts-updated']);

const API_URL = '/debts';
const STORAGE_KEY = 'second_brain_debts_v1';

// Tải bộ nhớ đệm LocalStorage để hiển thị tức thì, không bao giờ bị trắng màn hình
const loadLocal = () => {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
};

const saveLocal = (data) => {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(data));
    emit('debts-updated');
    window.dispatchEvent(new CustomEvent('secondbrain-data-updated', { detail: { action: 'debt' } }));
  } catch (e) {}
};

const records = ref(loadLocal());
const isSubmitting = ref(false);
const carryOverBalance = ref(false);
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
  dueDate: new Date().toISOString().split('T')[0],
  periodLabel: '1-10',
  priorityLevel: 1,
  isCompleted: false,
  categoryTag: 'Khác',
});

// Định dạng tiền tệ
const formatVND = (val) => {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(val || 0);
};

// Định dạng ngắn gọn (VD: 3375k, 4261k)
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

// Phân tích linh hoạt chuỗi số tiền (hỗ trợ 1000k, 10tr, 1850 + 600, 2.000.000, 500000)
const parsedAmount = computed(() => {
  const raw = String(amountRawInput.value || '').trim().toLowerCase();
  if (!raw) return 0;

  // Trường hợp nhập phép cộng (VD: 1850 + 600 + 370)
  if (raw.includes('+')) {
    const parts = raw.split('+').map(p => {
      let pClean = p.trim().replace(/[,. ]/g, '');
      return parseFloat(pClean) || 0;
    });
    const sum = parts.reduce((a, b) => a + b, 0);
    // Nếu tổng các số nhỏ hơn 50.000 (như 1850, 600, 811), tự hiểu là đơn vị nghìn đồng
    if (sum > 0 && sum < 50000) {
      return sum * 1000;
    }
    return sum;
  }

  // Trường hợp có chữ 'tr' hoặc 'triệu'
  if (raw.endsWith('tr') || raw.endsWith('triệu') || raw.endsWith('m')) {
    const val = parseFloat(raw.replace(/(tr|triệu|m)/g, '').replace(/,/g, '.'));
    return isNaN(val) ? 0 : Math.round(val * 1000000);
  }

  // Trường hợp có chữ 'k'
  if (raw.endsWith('k')) {
    const val = parseFloat(raw.replace('k', '').replace(/,/g, '.'));
    return isNaN(val) ? 0 : Math.round(val * 1000);
  }

  // Loại bỏ dấu chấm, phẩy phân cách ngàn thông thường (2.000.000 -> 2000000)
  const cleanNumber = raw.replace(/[,. ]/g, '');
  const val = parseFloat(cleanNumber);
  if (isNaN(val)) return 0;

  // Nếu người dùng nhập số dưới 50.000 (như 1850, 600, 811, 4300), tự quy đổi ra nghìn đồng
  if (val > 0 && val < 50000 && !raw.includes('.') && !raw.includes(',')) {
    return val * 1000;
  }

  return val;
});

// Phân loại records
const incomeRecords = computed(() => records.value.filter(r => r.recordType === 'INCOME'));
const debtRecords = computed(() => records.value.filter(r => r.recordType === 'DEBT'));
const borrowMoreRecords = computed(() => records.value.filter(r => r.recordType === 'BORROW_MORE'));

const unpaidDebtsCount = computed(() => debtRecords.value.filter(d => !d.isCompleted).length);

// Thống kê tổng hợp
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

// Chia theo 3 mốc thời gian: 1-10, 10-21, 21-31
const timelineStages = computed(() => {
  const stageConfigs = [
    { id: '1-10', title: 'Đợt 1 (Đầu tháng)', subtitle: 'Từ ngày 01 đến ngày 10' },
    { id: '10-21', title: 'Đợt 2 (Giữa tháng)', subtitle: 'Từ ngày 10 đến ngày 21' },
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

// Xếp thứ tự ưu tiên các khoản nợ
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

// API Sync
const fetchDebts = async () => {
  try {
    const res = await apiFetch(API_URL);
    if (res.ok) {
      const serverData = await res.json();
      if (Array.isArray(serverData) && serverData.length > 0) {
        records.value = serverData;
        saveLocal(serverData);
      } else if (records.value.length > 0) {
        // Đồng bộ các khoản đã lưu offline lên database
        for (const r of records.value) {
          apiFetch(API_URL, { method: 'POST', body: JSON.stringify(r) }).catch(() => {});
        }
      }
    }
  } catch (err) {
    console.warn('Lỗi kết nối máy chủ, đang dùng dữ liệu lưu cục bộ:', err);
  }
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
    dueDate: new Date().toISOString().split('T')[0],
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
    dueDate: new Date().toISOString().split('T')[0],
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
    dueDate: item.dueDate ? item.dueDate.split('T')[0] : new Date().toISOString().split('T')[0],
    periodLabel: item.periodLabel || '1-10',
    priorityLevel: item.priorityLevel || 2,
    isCompleted: item.isCompleted || false,
    categoryTag: item.categoryTag || 'Khác',
  };
  showModal.value = true;
};

// Lưu dữ liệu: Cập nhật NGAY LẬP TỨC trên giao diện và LocalStorage, rồi gửi lên API chạy nền
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
      id: isEditing.value && form.value.id ? form.value.id : (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'debt_' + Date.now()),
      title: title,
      recordType: form.value.recordType,
      totalAmount: amount,
      paidAmount: Number(form.value.paidAmount) || 0,
      breakdownNote: form.value.breakdownNote?.trim() || (hasMath ? rawExpression : ''),
      dueDate: form.value.dueDate ? new Date(form.value.dueDate).toISOString() : new Date().toISOString(),
      periodLabel: form.value.periodLabel || '1-10',
      priorityLevel: Number(form.value.priorityLevel) || 2,
      isCompleted: form.value.isCompleted || false,
      categoryTag: form.value.categoryTag || 'Khác',
      createdAt: new Date().toISOString(),
    };

    // 1. CẬP NHẬT TỨC THÌ VÀO GIAO DIỆN & LOCALSTORAGE (KHÔNG PHỤ THUỘC MẠNG/API)
    if (isEditing.value) {
      const idx = records.value.findIndex(x => x.id === itemToSave.id);
      if (idx !== -1) {
        records.value[idx] = itemToSave;
      } else {
        records.value.unshift(itemToSave);
      }
    } else {
      records.value.unshift(itemToSave);
    }
    
    // Ghi đè vào LocalStorage
    saveLocal(records.value);
    showModal.value = false;

    // 2. GỬI LÊN SERVER API
    apiFetch(isEditing.value ? `${API_URL}/${itemToSave.id}` : API_URL, {
      method: isEditing.value ? 'PUT' : 'POST',
      body: JSON.stringify(itemToSave),
    }).catch(err => {
      console.warn('API background sync warning:', err);
    });

  } catch (err) {
    console.error('Lỗi lưu khoản nợ:', err);
    alert('Có lỗi xảy ra: ' + (err.message || 'Vui lòng thử lại'));
  } finally {
    isSubmitting.value = false;
  }
};

const toggleComplete = async (item) => {
  item.isCompleted = !item.isCompleted;
  if (item.isCompleted) {
    item.paidAmount = item.totalAmount;
  } else {
    item.paidAmount = 0;
  }
  saveLocal(records.value);

  apiFetch(`${API_URL}/${item.id}/toggle`, { method: 'PATCH' }).catch(() => {});
};

const deleteItem = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa khoản mục này?')) return;
  records.value = records.value.filter(r => r.id !== id);
  saveLocal(records.value);

  apiFetch(`${API_URL}/${id}`, { method: 'DELETE' }).catch(() => {});
};

onMounted(() => {
  records.value = loadLocal();
  fetchDebts();
});
</script>
