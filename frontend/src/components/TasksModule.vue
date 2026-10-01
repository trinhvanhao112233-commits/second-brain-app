<template>
  <div class="space-y-6">
    <!-- Top Action Bar (Material Design 3 Style) -->
    <header class="bg-[#171c24] border border-[#262a34] rounded-2xl px-5 py-4 flex flex-col sm:flex-row sm:items-center justify-between gap-4 md-elevation-1">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center md-elevation-1">
          <span class="material-symbols-rounded text-2xl">checklist</span>
        </div>
        <div>
          <h2 class="text-lg font-bold text-white tracking-wide">Quản Lý Công Việc & Deadline</h2>
          <p class="text-xs text-[#8b9198]">Theo dõi việc hàng ngày và các hạn chót dài hạn</p>
        </div>
      </div>

      <!-- Quick Actions -->
      <div class="flex items-center gap-2">
        <!-- View mode toggle: Grouped List vs Flat -->
        <div class="flex items-center bg-[#1c2029] border border-[#262a34] rounded-xl p-0.5">
          <button
            @click="viewMode = 'grouped'"
            class="px-2.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all"
            :class="viewMode === 'grouped' ? 'bg-[#005237] text-[#5dfec1] font-bold md-elevation-1' : 'text-[#8b9198] hover:text-[#c1c7ce]'"
            title="Nhóm theo Tầm nhìn Thời gian"
          >
            <span class="material-symbols-rounded text-base">view_agenda</span>
            <span class="hidden sm:inline">Theo Mốc Thời Gian</span>
          </button>
          <button
            @click="viewMode = 'flat'"
            class="px-2.5 py-1.5 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all"
            :class="viewMode === 'flat' ? 'bg-[#005237] text-[#5dfec1] font-bold md-elevation-1' : 'text-[#8b9198] hover:text-[#c1c7ce]'"
            title="Xem toàn bộ danh sách"
          >
            <span class="material-symbols-rounded text-base">format_list_bulleted</span>
            <span class="hidden sm:inline">Toàn Bộ ({{ localTasks.length }})</span>
          </button>
        </div>

        <button
          type="button"
          @click="openCreateModal()"
          class="flex items-center gap-1.5 px-4 py-2 rounded-full bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] text-xs font-bold transition-all md-elevation-1 active:scale-95"
        >
          <span class="material-symbols-rounded text-base">add</span>
          <span>Tạo Việc / Deadline</span>
        </button>
      </div>
    </header>

    <!-- Thống kê tổng quan các mốc thời gian (MD3 Summary Cards) -->
    <div class="grid grid-cols-2 lg:grid-cols-4 gap-3 sm:gap-4">
      <!-- 1. Quá hạn -->
      <div
        @click="filterTag = filterTag === 'OVERDUE' ? 'ALL' : 'OVERDUE'"
        class="p-4 rounded-2xl bg-[#171c24] border transition-all cursor-pointer md-elevation-1"
        :class="filterTag === 'OVERDUE' ? 'border-[#ffb4ab] bg-[#1c2029]' : 'border-[#262a34] hover:border-[#ffb4ab]/40'"
      >
        <div class="flex items-center justify-between text-[#ffb4ab] mb-1">
          <span class="text-[11px] font-semibold uppercase tracking-wider">Quá Hạn</span>
          <span class="material-symbols-rounded text-lg">warning</span>
        </div>
        <div class="text-2xl font-bold font-mono text-[#ffb4ab]">
          {{ overdueTasks.length }}
        </div>
        <span class="text-[11px] text-[#8b9198] mt-1 block">Cần xử lý gấp</span>
      </div>

      <!-- 2. Hôm nay -->
      <div
        @click="filterTag = filterTag === 'TODAY' ? 'ALL' : 'TODAY'"
        class="p-4 rounded-2xl bg-[#171c24] border transition-all cursor-pointer md-elevation-1"
        :class="filterTag === 'TODAY' ? 'border-[#38e1a6] bg-[#1c2029]' : 'border-[#262a34] hover:border-[#38e1a6]/40'"
      >
        <div class="flex items-center justify-between text-[#38e1a6] mb-1">
          <span class="text-[11px] font-semibold uppercase tracking-wider">Hôm Nay</span>
          <span class="material-symbols-rounded text-lg">today</span>
        </div>
        <div class="text-2xl font-bold font-mono text-[#38e1a6]">
          {{ todayTasks.length }}
        </div>
        <span class="text-[11px] text-[#8b9198] mt-1 block">Hoàn thành trong ngày</span>
      </div>

      <!-- 3. Tuần này -->
      <div
        @click="filterTag = filterTag === 'WEEK' ? 'ALL' : 'WEEK'"
        class="p-4 rounded-2xl bg-[#171c24] border transition-all cursor-pointer md-elevation-1"
        :class="filterTag === 'WEEK' ? 'border-[#cee9da] bg-[#1c2029]' : 'border-[#262a34] hover:border-[#cee9da]/40'"
      >
        <div class="flex items-center justify-between text-[#cee9da] mb-1">
          <span class="text-[11px] font-semibold uppercase tracking-wider">Tuần Này</span>
          <span class="material-symbols-rounded text-lg">date_range</span>
        </div>
        <div class="text-2xl font-bold font-mono text-white">
          {{ weekTasks.length }}
        </div>
        <span class="text-[11px] text-[#8b9198] mt-1 block">Hạn trong 7 ngày tới</span>
      </div>

      <!-- 4. Deadline Dài Hạn -->
      <div
        @click="filterTag = filterTag === 'LONG_TERM' ? 'ALL' : 'LONG_TERM'"
        class="p-4 rounded-2xl bg-[#171c24] border transition-all cursor-pointer md-elevation-1"
        :class="filterTag === 'LONG_TERM' ? 'border-[#5dfec1] bg-[#1c2029]' : 'border-[#262a34] hover:border-[#5dfec1]/40'"
      >
        <div class="flex items-center justify-between text-[#5dfec1] mb-1">
          <span class="text-[11px] font-semibold uppercase tracking-wider">Dài Hạn / Đồ Án</span>
          <span class="material-symbols-rounded text-lg">flag</span>
        </div>
        <div class="text-2xl font-bold font-mono text-[#5dfec1]">
          {{ longTermTasks.length }}
        </div>
        <span class="text-[11px] text-[#8b9198] mt-1 block">Đồ án, mục tiêu lớn</span>
      </div>
    </div>

    <!-- Quick Filter Status Tabs -->
    <div class="flex items-center justify-between border-b border-[#262a34] pb-2">
      <div class="flex items-center gap-1.5">
        <button
          v-for="status in ['Chưa xong', 'Tất cả', 'Đã hoàn thành']"
          :key="status"
          @click="statusFilter = status"
          class="px-3 py-1 rounded-full text-xs font-semibold transition-all"
          :class="statusFilter === status 
            ? 'bg-[#005237] text-[#5dfec1] font-bold border border-[#38e1a6]/40' 
            : 'text-[#8b9198] hover:text-[#c1c7ce] hover:bg-[#1c2029]'"
        >
          {{ status }}
        </button>
      </div>

      <span v-if="filterTag !== 'ALL'" class="text-xs text-[#8b9198] flex items-center gap-1">
        Đang lọc: <span class="text-[#38e1a6] font-bold">{{ filterTagLabel }}</span>
        <button @click="filterTag = 'ALL'" class="text-[10px] text-rose-400 hover:underline ml-1">✕ Xóa lọc</button>
      </span>
    </div>

    <!-- ========================================================================= -->
    <!-- DANH SÁCH CÔNG VIỆC CHÍNH (Direct Template Rendering)                      -->
    <!-- ========================================================================= -->
    <div v-if="localTasks.length === 0" class="text-center py-16 bg-[#171c24] border border-[#262a34] rounded-2xl">
      <div class="w-12 h-12 rounded-2xl bg-[#1c2029] text-[#5dfec1] flex items-center justify-center mx-auto mb-3">
        <span class="material-symbols-rounded text-2xl">checklist</span>
      </div>
      <p class="text-sm font-bold text-white">Chưa có công việc nào!</p>
      <p class="text-xs text-[#8b9198] mt-1">Bấm nút "+ Tạo Việc / Deadline" để thêm công việc đầu tiên của bạn.</p>
      <button
        @click="openCreateModal()"
        class="mt-4 px-4 py-2 rounded-full bg-[#005237] text-[#5dfec1] text-xs font-bold"
      >
        + Thêm việc mới
      </button>
    </div>

    <!-- CHẾ ĐỘ 1: XEM THEO NHÓM THỜI GIAN (SMART GROUPED LIST) -->
    <div v-else-if="viewMode === 'grouped'" class="space-y-6">
      <!-- NHÓM 1: CẦN LÀM GẤP / QUÁ HẠN -->
      <section v-if="overdueTasks.length > 0" class="space-y-2.5">
        <div class="flex items-center gap-2 text-xs font-bold text-[#ffb4ab] uppercase tracking-wider">
          <span class="material-symbols-rounded text-base">error</span>
          <span>Cần Làm Gấp / Đã Quá Hạn ({{ overdueTasks.length }})</span>
        </div>
        <div class="space-y-2">
          <div
            v-for="t in overdueTasks"
            :key="t.id"
            class="p-3.5 sm:px-4 sm:py-3 rounded-2xl bg-[#171c24] border border-[#ffb4ab]/40 hover:border-[#ffb4ab] flex items-center justify-between group transition-all md-elevation-1"
            :class="{ 'opacity-50': t.isCompleted }"
          >
            <div class="flex items-center gap-3 flex-1 min-w-0 pr-2">
              <button
                type="button"
                @click="toggleTaskLocal(t)"
                class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all flex-shrink-0"
                :class="t.isCompleted ? 'bg-[#38e1a6] border-[#38e1a6] text-[#003824]' : 'border-[#41474d] hover:border-[#38e1a6] text-transparent'"
              >
                <span class="material-symbols-rounded text-sm font-bold">check</span>
              </button>

              <div class="min-w-0 flex-1">
                <span
                  class="text-xs sm:text-sm font-bold text-white truncate block"
                  :class="{ 'line-through text-[#8b9198]': t.isCompleted }"
                >
                  {{ t.title }}
                </span>
                <span v-if="t.dueDate" class="text-[11px] text-[#ffb4ab] font-mono mt-0.5 block">
                  Hạn chót: {{ formatDueDate(t.dueDate) }}
                </span>
              </div>
            </div>

            <div class="flex items-center gap-2 flex-shrink-0">
              <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded-full bg-[#ffb4ab]/15 text-[#ffb4ab] border border-[#ffb4ab]/30">
                {{ getCountdownText(t.dueDate) }}
              </span>
              <button @click="openEditModal(t)" class="w-7 h-7 rounded-lg hover:bg-[#1c2029] text-[#8b9198] hover:text-white flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">edit</span>
              </button>
              <button @click="deleteTaskLocal(t.id)" class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- NHÓM 2: HÔM NAY (TODAY) -->
      <section v-if="todayTasks.length > 0" class="space-y-2.5">
        <div class="flex items-center gap-2 text-xs font-bold text-[#38e1a6] uppercase tracking-wider">
          <span class="material-symbols-rounded text-base">wb_sunny</span>
          <span>Việc Hôm Nay ({{ todayTasks.length }})</span>
        </div>
        <div class="space-y-2">
          <div
            v-for="t in todayTasks"
            :key="t.id"
            class="p-3.5 sm:px-4 sm:py-3 rounded-2xl bg-[#171c24] border border-[#38e1a6]/40 hover:border-[#38e1a6] flex items-center justify-between group transition-all md-elevation-1"
            :class="{ 'opacity-50': t.isCompleted }"
          >
            <div class="flex items-center gap-3 flex-1 min-w-0 pr-2">
              <button
                type="button"
                @click="toggleTaskLocal(t)"
                class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all flex-shrink-0"
                :class="t.isCompleted ? 'bg-[#38e1a6] border-[#38e1a6] text-[#003824]' : 'border-[#41474d] hover:border-[#38e1a6] text-transparent'"
              >
                <span class="material-symbols-rounded text-sm font-bold">check</span>
              </button>

              <div class="min-w-0 flex-1">
                <span
                  class="text-xs sm:text-sm font-bold text-white truncate block"
                  :class="{ 'line-through text-[#8b9198]': t.isCompleted }"
                >
                  {{ t.title }}
                </span>
                <span class="text-[11px] text-[#38e1a6] font-mono mt-0.5 block">
                  Hạn chót: Hôm nay!
                </span>
              </div>
            </div>

            <div class="flex items-center gap-2 flex-shrink-0">
              <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded-full bg-[#38e1a6]/15 text-[#38e1a6] border border-[#38e1a6]/30">
                Hôm nay!
              </span>
              <button @click="openEditModal(t)" class="w-7 h-7 rounded-lg hover:bg-[#1c2029] text-[#8b9198] hover:text-white flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">edit</span>
              </button>
              <button @click="deleteTaskLocal(t.id)" class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- NHÓM 3: TUẦN NÀY (THIS WEEK) -->
      <section v-if="weekTasks.length > 0" class="space-y-2.5">
        <div class="flex items-center gap-2 text-xs font-bold text-[#cee9da] uppercase tracking-wider">
          <span class="material-symbols-rounded text-base">date_range</span>
          <span>Trong Tuần Này ({{ weekTasks.length }})</span>
        </div>
        <div class="space-y-2">
          <div
            v-for="t in weekTasks"
            :key="t.id"
            class="p-3.5 sm:px-4 sm:py-3 rounded-2xl bg-[#171c24] border border-[#262a34] hover:border-[#41474d] flex items-center justify-between group transition-all md-elevation-1"
            :class="{ 'opacity-50': t.isCompleted }"
          >
            <div class="flex items-center gap-3 flex-1 min-w-0 pr-2">
              <button
                type="button"
                @click="toggleTaskLocal(t)"
                class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all flex-shrink-0"
                :class="t.isCompleted ? 'bg-[#38e1a6] border-[#38e1a6] text-[#003824]' : 'border-[#41474d] hover:border-[#38e1a6] text-transparent'"
              >
                <span class="material-symbols-rounded text-sm font-bold">check</span>
              </button>

              <div class="min-w-0 flex-1">
                <span
                  class="text-xs sm:text-sm font-bold text-white truncate block"
                  :class="{ 'line-through text-[#8b9198]': t.isCompleted }"
                >
                  {{ t.title }}
                </span>
                <span class="text-[11px] text-[#8b9198] font-mono mt-0.5 block">
                  Hạn chót: {{ formatDueDate(t.dueDate) }}
                </span>
              </div>
            </div>

            <div class="flex items-center gap-2 flex-shrink-0">
              <span class="text-[10px] font-mono font-bold px-2 py-0.5 rounded-full bg-amber-500/15 text-amber-300 border border-amber-500/30">
                {{ getCountdownText(t.dueDate) }}
              </span>
              <button @click="openEditModal(t)" class="w-7 h-7 rounded-lg hover:bg-[#1c2029] text-[#8b9198] hover:text-white flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">edit</span>
              </button>
              <button @click="deleteTaskLocal(t.id)" class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- NHÓM 4: DEADLINE DÀI HẠN / ĐỒ ÁN / DỰ ÁN (LONG-TERM) -->
      <section v-if="longTermTasks.length > 0" class="space-y-2.5">
        <div class="flex items-center justify-between">
          <div class="flex items-center gap-2 text-xs font-bold text-[#5dfec1] uppercase tracking-wider">
            <span class="material-symbols-rounded text-base">flag</span>
            <span>Mục Tiêu & Deadline Dài Hạn ({{ longTermTasks.length }})</span>
          </div>
          <span class="text-[11px] text-[#8b9198]">Đếm ngược ngày đến hạn</span>
        </div>

        <div class="space-y-2">
          <div
            v-for="t in longTermTasks"
            :key="t.id"
            class="p-4 rounded-2xl bg-[#171c24] border border-[#262a34] hover:border-[#5dfec1]/40 flex items-center justify-between group transition-all md-elevation-1"
            :class="{ 'opacity-50': t.isCompleted }"
          >
            <div class="flex items-center gap-3 flex-1 min-w-0 pr-2">
              <button
                type="button"
                @click="toggleTaskLocal(t)"
                class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all flex-shrink-0"
                :class="t.isCompleted ? 'bg-[#38e1a6] border-[#38e1a6] text-[#003824]' : 'border-[#41474d] hover:border-[#38e1a6] text-transparent'"
              >
                <span class="material-symbols-rounded text-sm font-bold">check</span>
              </button>

              <div class="min-w-0 flex-1">
                <span
                  class="text-sm font-bold text-white truncate block"
                  :class="{ 'line-through text-[#8b9198]': t.isCompleted }"
                >
                  {{ t.title }}
                </span>
                <span class="text-[11px] text-[#8b9198] font-mono mt-0.5 block flex items-center gap-1">
                  <span class="material-symbols-rounded text-xs text-[#5dfec1]">event</span>
                  <span>Hạn chót: {{ formatDueDate(t.dueDate) }}</span>
                </span>
              </div>
            </div>

            <div class="flex items-center gap-2 flex-shrink-0">
              <!-- Countdown badge lớn cho việc dài hạn -->
              <span class="text-xs font-mono font-bold px-2.5 py-1 rounded-full bg-[#005237] text-[#5dfec1] border border-[#38e1a6]/40">
                ⏳ {{ getCountdownText(t.dueDate) }}
              </span>
              <button @click="openEditModal(t)" class="w-7 h-7 rounded-lg hover:bg-[#1c2029] text-[#8b9198] hover:text-white flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">edit</span>
              </button>
              <button @click="deleteTaskLocal(t.id)" class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>
        </div>
      </section>

      <!-- NHÓM 5: CHƯA ĐẶT HẠN (NO DUE DATE) -->
      <section v-if="noDueDateTasks.length > 0" class="space-y-2.5">
        <div class="flex items-center gap-2 text-xs font-bold text-[#8b9198] uppercase tracking-wider">
          <span class="material-symbols-rounded text-base">pending_actions</span>
          <span>Không Giới Hạn Thời Gian ({{ noDueDateTasks.length }})</span>
        </div>
        <div class="space-y-2">
          <div
            v-for="t in noDueDateTasks"
            :key="t.id"
            class="p-3.5 sm:px-4 sm:py-3 rounded-2xl bg-[#171c24] border border-[#262a34] hover:border-[#41474d] flex items-center justify-between group transition-all md-elevation-1"
            :class="{ 'opacity-50': t.isCompleted }"
          >
            <div class="flex items-center gap-3 flex-1 min-w-0 pr-2">
              <button
                type="button"
                @click="toggleTaskLocal(t)"
                class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all flex-shrink-0"
                :class="t.isCompleted ? 'bg-[#38e1a6] border-[#38e1a6] text-[#003824]' : 'border-[#41474d] hover:border-[#38e1a6] text-transparent'"
              >
                <span class="material-symbols-rounded text-sm font-bold">check</span>
              </button>

              <div class="min-w-0 flex-1">
                <span
                  class="text-xs sm:text-sm font-bold text-white truncate block"
                  :class="{ 'line-through text-[#8b9198]': t.isCompleted }"
                >
                  {{ t.title }}
                </span>
              </div>
            </div>

            <div class="flex items-center gap-2 flex-shrink-0">
              <button @click="openEditModal(t)" class="w-7 h-7 rounded-lg hover:bg-[#1c2029] text-[#8b9198] hover:text-white flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">edit</span>
              </button>
              <button @click="deleteTaskLocal(t.id)" class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center">
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>
        </div>
      </section>
    </div>

    <!-- CHẾ ĐỘ 2: DANH SÁCH PHẲNG (TOÀN BỘ) -->
    <div v-else class="space-y-2">
      <div
        v-for="t in displayTasks"
        :key="t.id"
        class="p-3.5 sm:px-4 sm:py-3 rounded-2xl bg-[#171c24] border border-[#262a34] hover:border-[#41474d] flex items-center justify-between group transition-all md-elevation-1"
        :class="{ 'opacity-50': t.isCompleted }"
      >
        <div class="flex items-center gap-3 flex-1 min-w-0 pr-2">
          <button
            type="button"
            @click="toggleTaskLocal(t)"
            class="w-6 h-6 rounded-lg border-2 flex items-center justify-center transition-all flex-shrink-0"
            :class="t.isCompleted ? 'bg-[#38e1a6] border-[#38e1a6] text-[#003824]' : 'border-[#41474d] hover:border-[#38e1a6] text-transparent'"
          >
            <span class="material-symbols-rounded text-sm font-bold">check</span>
          </button>

          <div class="min-w-0 flex-1">
            <span
              class="text-xs sm:text-sm font-bold text-white truncate block"
              :class="{ 'line-through text-[#8b9198]': t.isCompleted }"
            >
              {{ t.title }}
            </span>
            <span v-if="t.dueDate" class="text-[11px] text-[#8b9198] font-mono mt-0.5 block">
              Hạn chót: {{ formatDueDate(t.dueDate) }}
            </span>
          </div>
        </div>

        <div class="flex items-center gap-2 flex-shrink-0">
          <span
            v-if="t.dueDate && !t.isCompleted"
            class="text-[10px] font-mono font-bold px-2 py-0.5 rounded-full"
            :class="getCountdownClass(t.dueDate)"
          >
            {{ getCountdownText(t.dueDate) }}
          </span>
          <button @click="openEditModal(t)" class="w-7 h-7 rounded-lg hover:bg-[#1c2029] text-[#8b9198] hover:text-white flex items-center justify-center">
            <span class="material-symbols-rounded text-sm">edit</span>
          </button>
          <button @click="deleteTaskLocal(t.id)" class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center">
            <span class="material-symbols-rounded text-sm">delete</span>
          </button>
        </div>
      </div>
    </div>

    <!-- ========================================================================= -->
    <!-- MODAL THÊM / SỬA CÔNG VIỆC (MD3 DIALOG)                                   -->
    <!-- ========================================================================= -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-md bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <!-- Dialog Header -->
        <div class="flex items-center gap-3 mb-4">
          <div class="w-10 h-10 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-xl">{{ isEditing ? 'edit_note' : 'add_task' }}</span>
          </div>
          <div>
            <h3 class="text-base font-bold text-white">
              {{ isEditing ? 'Chỉnh Sửa Công Việc / Deadline' : 'Tạo Việc Mới / Deadline' }}
            </h3>
            <p class="text-xs text-[#8b9198]">Hỗ trợ hạn chót ngắn hạn và đồ án dài hạn</p>
          </div>
        </div>

        <form @submit.prevent="handleSaveTask" class="space-y-3.5">
          <!-- Tên việc -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Tên công việc / Tên đồ án / Deadline</label>
            <input
              v-model="taskForm.title"
              type="text"
              required
              placeholder="Nhập tên công việc hoặc deadline..."
              class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-sm focus:outline-none focus:border-[#38e1a6]"
            />
          </div>



          <!-- Hạn chót (Due Date) -->
          <div>
            <div class="flex items-center justify-between mb-1">
              <label class="text-xs font-semibold text-[#c1c7ce]">Hạn chót (Deadline)</label>
              <button
                type="button"
                @click="taskForm.dueDate = ''"
                class="text-[11px] text-[#8b9198] hover:text-white"
              >
                Không đặt hạn
              </button>
            </div>
            <input
              v-model="taskForm.dueDate"
              type="date"
              class="w-full px-3 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <!-- Phím tắt chọn nhanh Deadline (Hôm nay, 3 ngày nữa, 1 tuần, 1 tháng) -->
          <div>
            <label class="block text-[11px] font-semibold text-[#8b9198] mb-1">Gợi ý nhanh hạn chót:</label>
            <div class="grid grid-cols-4 gap-1.5">
              <button
                v-for="q in quickDeadlines"
                :key="q.label"
                type="button"
                @click="setQuickDueDate(q.days)"
                class="py-1.5 px-1 rounded-lg bg-[#171c24] hover:bg-[#262a34] border border-[#262a34] text-[11px] text-[#c1c7ce] text-center transition-all"
              >
                {{ q.label }}
              </button>
            </div>
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
              <span>{{ isEditing ? 'Cập nhật' : 'Tạo Công Việc' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, onMounted } from 'vue';
import { apiFetch } from '../services/api';

const props = defineProps({
  tasks: { type: Array, default: () => [] }
});
const emit = defineEmits(['refresh-tasks', 'toggle-task']);

const API_BASE_URL = '/tasks';

// Quản lý danh sách tác vụ local để cập nhật tức thì (ngay cả khi props chậm cập nhật)
const localTasks = ref([...props.tasks]);

watch(
  () => props.tasks,
  (newVal) => {
    localTasks.value = [...newVal];
  },
  { deep: true, immediate: true }
);

// Tự động tải từ API khi component mount
const fetchLocalTasks = async () => {
  try {
    const res = await apiFetch(API_BASE_URL);
    if (res.ok) {
      localTasks.value = await res.json();
    }
  } catch (err) {
    console.error('Lỗi tải danh sách việc:', err);
  }
};

onMounted(() => {
  fetchLocalTasks();
});

// View modes
const viewMode = ref('grouped'); // 'grouped' (theo mốc thời gian) | 'flat' (toàn bộ)
const statusFilter = ref('Chưa xong'); // 'Chưa xong' | 'Tất cả' | 'Đã hoàn thành'
const filterTag = ref('ALL'); // 'ALL' | 'OVERDUE' | 'TODAY' | 'WEEK' | 'LONG_TERM'

const showModal = ref(false);
const isEditing = ref(false);

const taskForm = ref({
  id: null,
  title: '',
  dueDate: '',
  priority: 'Medium',
  isCompleted: false
});

const quickDeadlines = [
  { label: 'Hôm nay', days: 0 },
  { label: 'Ngày mai', days: 1 },
  { label: '1 tuần', days: 7 },
  { label: '1 tháng', days: 30 },
];

const setQuickDueDate = (offsetDays) => {
  const d = new Date();
  d.setDate(d.getDate() + offsetDays);
  taskForm.value.dueDate = d.toISOString().split('T')[0];
};

const getDiffDays = (dueDateStr) => {
  if (!dueDateStr) return null;
  const target = new Date(dueDateStr);
  const now = new Date();
  target.setHours(0, 0, 0, 0);
  now.setHours(0, 0, 0, 0);
  const diffTime = target.getTime() - now.getTime();
  return Math.ceil(diffTime / (1000 * 60 * 60 * 24));
};

const getCountdownText = (dueDateStr) => {
  const diff = getDiffDays(dueDateStr);
  if (diff === null) return '';
  if (diff < 0) return `Quá hạn ${Math.abs(diff)} ngày`;
  if (diff === 0) return 'Hôm nay!';
  if (diff === 1) return 'Ngày mai';
  return `Còn ${diff} ngày`;
};

const getCountdownClass = (dueDateStr) => {
  const diff = getDiffDays(dueDateStr);
  if (diff === null) return '';
  if (diff < 0) return 'bg-[#ffb4ab]/15 text-[#ffb4ab] border border-[#ffb4ab]/30';
  if (diff === 0) return 'bg-[#38e1a6]/15 text-[#38e1a6] border border-[#38e1a6]/30';
  if (diff <= 3) return 'bg-amber-500/15 text-amber-300 border border-amber-500/30';
  return 'bg-[#1c2029] text-[#5dfec1] border border-[#262a34]';
};

const getPriorityClass = (priority) => {
  if (priority === 'High') return 'bg-[#ffb4ab]/15 text-[#ffb4ab] border border-[#ffb4ab]/30';
  if (priority === 'Medium') return 'bg-amber-500/15 text-amber-300 border border-amber-500/30';
  return 'bg-[#1c2029] text-[#8b9198] border border-[#262a34]';
};

const formatDueDate = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return `${d.getDate()}/${d.getMonth() + 1}/${d.getFullYear()}`;
};

// Lọc theo trạng thái Hoàn thành / Chưa xong
const statusFilteredTasks = computed(() => {
  if (statusFilter.value === 'Chưa xong') {
    return localTasks.value.filter(t => !t.isCompleted);
  }
  if (statusFilter.value === 'Đã hoàn thành') {
    return localTasks.value.filter(t => t.isCompleted);
  }
  return localTasks.value;
});

// Phân loại nhóm theo Tầm nhìn Thời gian (Time Horizons)
const overdueTasks = computed(() => {
  return statusFilteredTasks.value.filter(t => {
    if (!t.dueDate || t.isCompleted) return false;
    const diff = getDiffDays(t.dueDate);
    return diff < 0;
  });
});

const todayTasks = computed(() => {
  return statusFilteredTasks.value.filter(t => {
    if (!t.dueDate) return false;
    const diff = getDiffDays(t.dueDate);
    return diff === 0;
  });
});

const weekTasks = computed(() => {
  return statusFilteredTasks.value.filter(t => {
    if (!t.dueDate) return false;
    const diff = getDiffDays(t.dueDate);
    return diff > 0 && diff <= 7;
  });
});

const longTermTasks = computed(() => {
  return statusFilteredTasks.value.filter(t => {
    if (!t.dueDate) return false;
    const diff = getDiffDays(t.dueDate);
    return diff > 7;
  }).sort((a, b) => new Date(a.dueDate) - new Date(b.dueDate));
});

const noDueDateTasks = computed(() => {
  return statusFilteredTasks.value.filter(t => !t.dueDate);
});

// Tasks hiển thị khi xem toàn bộ hoặc lọc theo Card bấm nhanh
const displayTasks = computed(() => {
  if (filterTag.value === 'OVERDUE') return overdueTasks.value;
  if (filterTag.value === 'TODAY') return todayTasks.value;
  if (filterTag.value === 'WEEK') return weekTasks.value;
  if (filterTag.value === 'LONG_TERM') return longTermTasks.value;
  return statusFilteredTasks.value;
});

const filterTagLabel = computed(() => {
  switch (filterTag.value) {
    case 'OVERDUE': return 'Quá hạn';
    case 'TODAY': return 'Hôm nay';
    case 'WEEK': return 'Trong tuần';
    case 'LONG_TERM': return 'Dài hạn / Đồ án';
    default: return '';
  }
});

// Modal Openers
const openCreateModal = () => {
  isEditing.value = false;
  taskForm.value = {
    id: null,
    title: '',
    dueDate: '',
    priority: 'Medium',
    isCompleted: false
  };
  showModal.value = true;
};

const openEditModal = (t) => {
  isEditing.value = true;
  taskForm.value = {
    id: t.id,
    title: t.title,
    dueDate: t.dueDate ? t.dueDate.split('T')[0] : '',
    priority: t.priority || 'Medium',
    isCompleted: t.isCompleted || false
  };
  showModal.value = true;
};

// API calls & Local sync
const handleSaveTask = async () => {
  if (!taskForm.value.title.trim()) return;

  try {
    const payload = {
      title: taskForm.value.title.trim(),
      priority: taskForm.value.priority,
      dueDate: taskForm.value.dueDate ? new Date(taskForm.value.dueDate).toISOString() : null,
      isCompleted: taskForm.value.isCompleted
    };

    let res;
    if (isEditing.value && taskForm.value.id) {
      res = await apiFetch(`${API_BASE_URL}/${taskForm.value.id}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
      if (res.ok) {
        const updated = await res.json();
        const idx = localTasks.value.findIndex(x => x.id === updated.id);
        if (idx !== -1) localTasks.value[idx] = updated;
      }
    } else {
      res = await apiFetch(API_BASE_URL, {
        method: 'POST',
        body: JSON.stringify(payload)
      });
      if (res.ok) {
        const created = await res.json();
        localTasks.value.unshift(created);
      }
    }

    if (res.ok) {
      emit('refresh-tasks');
      showModal.value = false;
    } else {
      alert('Không thể lưu công việc. Vui lòng kiểm tra lại!');
    }
  } catch (err) {
    console.error('Lỗi lưu công việc:', err);
    alert('Đã xảy ra lỗi kết nối tới máy chủ.');
  }
};

const toggleTaskLocal = async (task) => {
  try {
    const res = await apiFetch(`${API_BASE_URL}/${task.id}/toggle`, { method: 'PATCH' });
    if (res.ok) {
      const updated = await res.json();
      task.isCompleted = updated.isCompleted;
      emit('toggle-task', task.id);
    }
  } catch (err) {
    console.error('Lỗi toggle task:', err);
  }
};

const deleteTaskLocal = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa công việc này?')) return;
  try {
    const res = await apiFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' });
    if (res.ok) {
      localTasks.value = localTasks.value.filter(x => x.id !== id);
      emit('refresh-tasks');
    }
  } catch (err) {
    console.error(err);
  }
};
</script>
