<template>
  <div class="space-y-4">
    <!-- ==================== 1. HEADER & THANH ĐIỀU HƯỚNG TUẦN ==================== -->
    <header class="bg-white border border-slate-200 rounded-2xl px-4 sm:px-5 py-3.5 flex flex-wrap items-center justify-between gap-3 shadow-sm">
      <!-- Left: Logo & Week Navigator -->
      <div class="flex items-center gap-2.5 sm:gap-3">
        <div class="flex items-center gap-2 pr-2.5 sm:pr-3 border-r border-slate-200">
          <div class="w-9 h-9 rounded-xl bg-indigo-50 text-indigo-600 border border-indigo-100 flex items-center justify-center shrink-0">
            <span class="material-symbols-rounded text-xl">calendar_month</span>
          </div>
          <div class="hidden sm:block">
            <span class="font-extrabold text-sm text-slate-900 tracking-tight block leading-none">Lịch Trình Tuần</span>
            <span class="text-[11px] text-slate-400">Ca học • Ca làm • Lịch hẹn</span>
          </div>
        </div>

        <!-- Week navigation control -->
        <div class="flex items-center gap-1.5">
          <button
            type="button"
            @click="prevWeek"
            class="w-8 h-8 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-600 hover:text-slate-900 flex items-center justify-center transition-all"
            title="Tuần trước"
          >
            <span class="material-symbols-rounded text-lg">chevron_left</span>
          </button>

          <button
            type="button"
            @click="goToCurrentWeek"
            class="flex items-center gap-2 px-3 py-1.5 rounded-xl bg-slate-50 hover:bg-slate-100 border border-slate-200 text-xs font-semibold text-slate-800 transition-all"
          >
            <span class="material-symbols-rounded text-indigo-600 text-sm">today</span>
            <div class="flex flex-col text-left leading-none">
              <span class="text-[11px] font-bold text-slate-900">{{ isCurrentWeek ? 'Tuần này' : 'Tuần đang chọn' }}</span>
              <span class="text-[10px] text-slate-500 mt-0.5 font-mono">{{ weekRangeLabel }}</span>
            </div>
          </button>

          <button
            type="button"
            @click="nextWeek"
            class="w-8 h-8 rounded-xl bg-slate-100 hover:bg-slate-200 text-slate-600 hover:text-slate-900 flex items-center justify-center transition-all"
            title="Tuần sau"
          >
            <span class="material-symbols-rounded text-lg">chevron_right</span>
          </button>
        </div>
      </div>

      <!-- Right: View Mode Toggle & Add Button -->
      <div class="flex items-center gap-2 ml-auto sm:ml-0">
        <div class="flex items-center bg-slate-100 border border-slate-200 rounded-xl p-1">
          <button
            type="button"
            @click="currentView = 'split'"
            class="px-2.5 py-1 rounded-lg text-xs font-bold flex items-center gap-1 transition-all"
            :class="currentView === 'split' ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            title="Chế độ Chi tiết ngày"
          >
            <span class="material-symbols-rounded text-base">view_agenda</span>
            <span class="hidden sm:inline">Theo Ngày</span>
          </button>

          <button
            type="button"
            @click="currentView = 'table'"
            class="px-2.5 py-1 rounded-lg text-xs font-bold flex items-center gap-1 transition-all"
            :class="currentView === 'table' ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            title="Thời khóa biểu Sáng / Chiều / Tối"
          >
            <span class="material-symbols-rounded text-base">table_chart</span>
            <span class="hidden sm:inline">Sáng/Chiều/Tối</span>
          </button>

          <button
            type="button"
            @click="currentView = 'columns'"
            class="px-2.5 py-1 rounded-lg text-xs font-bold flex items-center gap-1 transition-all"
            :class="currentView === 'columns' ? 'bg-white text-indigo-600 shadow-sm' : 'text-slate-600 hover:text-slate-900'"
            title="Chế độ 7 Cột Tuần"
          >
            <span class="material-symbols-rounded text-base">view_column</span>
            <span class="hidden sm:inline">7 Cột</span>
          </button>
        </div>

        <button
          type="button"
          @click="openAddModalFor(selectedDate)"
          class="flex items-center gap-1 px-3.5 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold transition-all shadow-sm active:scale-95"
        >
          <span class="material-symbols-rounded text-base">add</span>
          <span>+ Thêm Lịch</span>
        </button>
      </div>
    </header>

    <!-- ==================== THANH CHỌN NGÀY NHANH CHO ĐIỆN THOẠI (7 NGÀY T2 -> CN) ==================== -->
    <div class="grid grid-cols-7 gap-1.5 sm:gap-2 bg-white border border-slate-200 rounded-2xl p-2 shadow-sm">
      <button
        v-for="d in weekDays"
        :key="'strip_' + d.dateStr"
        type="button"
        @click="selectDate(d.dateStr)"
        class="flex flex-col items-center justify-center py-2 px-1 rounded-xl border transition-all relative"
        :class="[
          selectedDate === d.dateStr
            ? 'bg-indigo-600 border-indigo-600 text-white shadow-sm'
            : d.isToday
            ? 'bg-indigo-50/70 border-indigo-200 text-indigo-700'
            : 'bg-slate-50/60 border-transparent text-slate-700 hover:bg-slate-100'
        ]"
      >
        <span class="text-[10px] font-bold uppercase opacity-80">{{ d.dayLabel }}</span>
        <span class="text-xs sm:text-sm font-extrabold font-mono mt-0.5">{{ d.dayDateFormatted }}</span>
        <div class="flex items-center gap-0.5 mt-1 h-1.5">
          <span
            v-for="n in Math.min(d.events.length, 3)"
            :key="n"
            class="w-1.5 h-1.5 rounded-full"
            :class="selectedDate === d.dateStr ? 'bg-white' : 'bg-indigo-500'"
          ></span>
        </div>
      </button>
    </div>

    <!-- ======================================================== -->
    <!-- VIEW 1: CHI TIẾT NGÀY & DANH SÁCH 7 NGÀY TRONG TUẦN       -->
    <!-- ======================================================== -->
    <div v-if="currentView === 'split'" class="grid grid-cols-1 lg:grid-cols-12 gap-4">
      <!-- Cột Trái (8/12): Chi tiết lịch trình ngày được chọn -->
      <div class="lg:col-span-8 bg-white border border-slate-200 rounded-2xl p-4 sm:p-5 shadow-sm flex flex-col min-h-[420px]">
        <!-- Header Ngày chọn -->
        <div class="flex flex-wrap items-center justify-between gap-2 pb-4 mb-4 border-b border-slate-100">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-indigo-50 text-indigo-600 border border-indigo-100 flex items-center justify-center shrink-0">
              <span class="material-symbols-rounded text-xl">event</span>
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h3 class="text-base sm:text-lg font-extrabold text-slate-900">{{ selectedDayInfo.fullDayName }}</h3>
                <span class="text-xs px-2 py-0.5 rounded-md bg-slate-100 text-slate-700 font-mono font-bold border border-slate-200">
                  {{ selectedDayInfo.dateShort }}
                </span>
              </div>
              <p class="text-xs text-slate-500 mt-0.5">
                {{ selectedDayEvents.length }} công việc / ca lịch trình trong ngày
              </p>
            </div>
          </div>

          <!-- Nút thêm nhanh theo buổi Sáng / Chiều / Tối -->
          <div class="flex items-center gap-1.5 flex-wrap">
            <button
              v-for="period in ['Sáng', 'Chiều', 'Tối']"
              :key="period"
              type="button"
              @click="openAddModalForPeriod(selectedDate, period)"
              class="px-2.5 py-1.5 rounded-xl bg-slate-100 hover:bg-indigo-50 text-slate-700 hover:text-indigo-700 border border-slate-200 text-xs font-bold transition-all"
            >
              + Ca {{ period }}
            </button>
          </div>
        </div>

        <!-- Danh sách sự kiện trong ngày -->
        <div v-if="selectedDayEvents.length === 0" class="flex-1 flex flex-col items-center justify-center py-12 text-center">
          <div class="w-14 h-14 rounded-2xl bg-slate-100 text-slate-400 flex items-center justify-center mb-3">
            <span class="material-symbols-rounded text-3xl">event_available</span>
          </div>
          <p class="text-sm font-bold text-slate-800">Chưa có lịch trình nào cho {{ selectedDayInfo.fullDayName }}</p>
          <p class="text-xs text-slate-500 mt-1 max-w-xs">Bấm "+ Ca Sáng", "+ Ca Chiều", "+ Ca Tối" hoặc nút bên dưới để thêm lịch</p>
          <button
            type="button"
            @click="openAddModalFor(selectedDate)"
            class="mt-4 px-4 py-2 rounded-xl bg-indigo-600 hover:bg-indigo-700 text-white text-xs font-bold flex items-center gap-1.5 shadow-sm"
          >
            <span class="material-symbols-rounded text-sm">add_circle</span>
            <span>Tạo lịch trình mới</span>
          </button>
        </div>

        <div v-else class="space-y-3">
          <div
            v-for="evt in selectedDayEvents"
            :key="evt.id"
            class="p-4 rounded-2xl border bg-slate-50/50 hover:bg-white transition-all flex items-start justify-between gap-3 shadow-sm"
            :style="{
              borderColor: getCardBorder(evt.category, evt.color),
              borderLeftWidth: '5px',
              borderLeftColor: getBadgeHex(evt.category, evt.color),
            }"
          >
            <div class="space-y-1.5 flex-1 min-w-0">
              <div class="flex flex-wrap items-center gap-2">
                <!-- Checkbox hoàn thành -->
                <button
                  type="button"
                  @click="toggleComplete(evt)"
                  class="w-5 h-5 rounded-md border flex items-center justify-center transition-all shrink-0"
                  :class="evt.isCompleted ? 'bg-emerald-600 border-emerald-600 text-white' : 'border-slate-300 hover:border-indigo-600 text-transparent bg-white'"
                >
                  <span class="material-symbols-rounded text-xs font-bold">check</span>
                </button>

                <!-- Time range -->
                <span class="text-xs font-mono font-bold text-slate-800 bg-white border border-slate-200 px-2.5 py-0.5 rounded-lg">
                  {{ formatTime(evt.startTime) }} - {{ formatTime(evt.endTime) }}
                </span>

                <!-- Category tag -->
                <span
                  class="text-[11px] font-bold px-2.5 py-0.5 rounded-lg text-white"
                  :style="{ backgroundColor: getBadgeHex(evt.category, evt.color) }"
                >
                  {{ evt.category || 'Lịch hẹn' }}
                </span>
              </div>

              <!-- Title -->
              <h4
                class="text-sm sm:text-base font-extrabold text-slate-900 pt-0.5"
                :class="evt.isCompleted ? 'line-through text-slate-400' : ''"
              >
                {{ evt.title }}
              </h4>

              <p v-if="evt.description" class="text-xs text-slate-500 flex items-center gap-1">
                <span class="material-symbols-rounded text-sm text-slate-400">location_on</span>
                <span>{{ evt.description }}</span>
              </p>
            </div>

            <!-- Actions (Edit/Delete) -->
            <div class="flex items-center gap-1 shrink-0">
              <button
                type="button"
                @click="openEditModal(evt)"
                class="w-8 h-8 rounded-xl bg-white border border-slate-200 text-slate-500 hover:text-indigo-600 flex items-center justify-center transition-colors"
                title="Chỉnh sửa"
              >
                <span class="material-symbols-rounded text-base">edit</span>
              </button>
              <button
                type="button"
                @click="deleteEvent(evt.id)"
                class="w-8 h-8 rounded-xl bg-white border border-slate-200 text-slate-500 hover:text-rose-600 flex items-center justify-center transition-colors"
                title="Xóa"
              >
                <span class="material-symbols-rounded text-base">delete</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Cột Phải (4/12): Tổng quan 7 ngày trong tuần -->
      <div class="lg:col-span-4 bg-white border border-slate-200 rounded-2xl p-4 shadow-sm flex flex-col">
        <div class="flex items-center justify-between pb-3 mb-3 border-b border-slate-100">
          <div>
            <h4 class="font-extrabold text-sm text-slate-900">Tổng Quan Cả Tuần</h4>
            <p class="text-[11px] text-slate-500">Chạm vào ngày bất kỳ để xem hoặc thêm lịch</p>
          </div>
          <button
            type="button"
            @click="selectDate(todayStr)"
            class="text-xs px-2.5 py-1 rounded-lg bg-indigo-50 text-indigo-700 font-bold hover:bg-indigo-100 transition-all"
          >
            Hôm nay
          </button>
        </div>

        <div class="space-y-2 flex-1">
          <div
            v-for="d in weekDays"
            :key="d.dateStr"
            @click="selectDate(d.dateStr)"
            class="p-3 rounded-xl border transition-all cursor-pointer"
            :class="[
              selectedDate === d.dateStr
                ? 'bg-indigo-50/50 border-indigo-600 ring-1 ring-indigo-600'
                : 'bg-slate-50/50 border-slate-200 hover:bg-slate-100/70'
            ]"
          >
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span
                  class="font-extrabold text-xs"
                  :class="selectedDate === d.dateStr ? 'text-indigo-700' : 'text-slate-900'"
                >
                  {{ d.fullDayName }} ({{ d.dayDateFormatted }})
                </span>
                <span v-if="d.isToday" class="text-[10px] font-bold px-2 py-0.5 rounded-full bg-emerald-50 text-emerald-700 border border-emerald-200">
                  Hôm nay
                </span>
              </div>
              <span class="text-xs font-mono font-bold px-2 py-0.5 rounded-full bg-white border border-slate-200 text-slate-700">
                {{ d.events.length }} ca
              </span>
            </div>

            <div v-if="d.events.length > 0" class="mt-2 space-y-1">
              <div
                v-for="e in d.events.slice(0, 3)"
                :key="e.id"
                class="flex items-center gap-1.5 text-xs truncate text-slate-700"
              >
                <span class="w-2 h-2 rounded-full shrink-0" :style="{ backgroundColor: getBadgeHex(e.category, e.color) }"></span>
                <span class="font-mono text-[11px] text-slate-500 shrink-0">{{ formatTime(e.startTime) }}</span>
                <span class="truncate font-semibold">{{ e.title }}</span>
              </div>
              <div v-if="d.events.length > 3" class="text-[11px] text-slate-400 font-medium pl-3">
                +{{ d.events.length - 3 }} lịch nữa
              </div>
            </div>
            <div v-else class="text-[11px] text-slate-400 mt-1 italic">
              Trống lịch
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- VIEW 2: THỜI KHÓA BIỂU SÁNG / CHIỀU / TỐI                 -->
    <!-- ======================================================== -->
    <div v-else-if="currentView === 'table'" class="bg-white border border-slate-200 rounded-2xl p-4 sm:p-5 shadow-sm overflow-hidden">
      <div class="flex flex-wrap items-center justify-between gap-2 pb-3 mb-3 border-b border-slate-100">
        <div class="flex items-center gap-2">
          <span class="text-xs font-bold text-slate-700">Thời khóa biểu theo 3 buổi:</span>
          <span class="text-xs px-2.5 py-0.5 rounded-full bg-indigo-50 text-indigo-700 border border-indigo-200 font-bold">
            Sáng • Chiều • Tối
          </span>
        </div>
        <span class="text-[11px] text-slate-400">Tự động sắp xếp theo giờ bắt đầu</span>
      </div>

      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse min-w-[820px]">
          <thead>
            <tr class="border-b border-slate-200 bg-slate-50">
              <th class="p-3 text-xs font-bold text-slate-600 w-24 border-r border-slate-200 text-center">Buổi</th>
              <th
                v-for="d in weekDays"
                :key="d.dateStr"
                class="p-2.5 text-center border-r border-slate-200 last:border-r-0"
                :class="d.isToday ? 'bg-indigo-50/70 text-indigo-700' : 'text-slate-700'"
              >
                <div class="text-xs font-extrabold">{{ d.dayLabel }}</div>
                <div class="text-[11px] font-mono text-slate-500">{{ d.dayDateFormatted }}</div>
              </th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="period in ['Sáng', 'Chiều', 'Tối']"
              :key="period"
              class="border-b border-slate-200 last:border-b-0"
            >
              <td class="p-3 text-center font-bold text-xs text-slate-800 bg-slate-50/70 border-r border-slate-200 align-middle">
                <div class="flex flex-col items-center gap-1">
                  <span class="material-symbols-rounded text-lg text-indigo-600">
                    {{ period === 'Sáng' ? 'wb_sunny' : period === 'Chiều' ? 'wb_twilight' : 'nights_stay' }}
                  </span>
                  <span>{{ period }}</span>
                </div>
              </td>

              <td
                v-for="d in weekDays"
                :key="d.dateStr"
                class="p-2 border-r border-slate-200 last:border-r-0 align-top min-w-[115px]"
              >
                <div class="space-y-1.5 min-h-[85px] flex flex-col justify-start">
                  <div
                    v-for="e in getEventsInPeriod(d.dateStr, period)"
                    :key="e.id"
                    @click="openEditModal(e)"
                    class="p-2 rounded-xl border bg-slate-50 hover:bg-white text-xs cursor-pointer transition-all shadow-sm"
                    :style="{
                      borderColor: getCardBorder(e.category, e.color),
                      borderLeft: `4px solid ${getBadgeHex(e.category, e.color)}`,
                    }"
                  >
                    <div class="text-[10px] font-mono text-indigo-700 font-bold">
                      {{ formatTime(e.startTime) }} - {{ formatTime(e.endTime) }}
                    </div>
                    <div class="text-xs font-bold text-slate-900 line-clamp-2 mt-0.5">{{ e.title }}</div>
                  </div>

                  <button
                    type="button"
                    @click="openAddModalForPeriod(d.dateStr, period)"
                    class="w-full py-1.5 rounded-lg border border-dashed border-slate-200 hover:border-indigo-400 hover:bg-indigo-50/50 text-[10px] text-slate-400 hover:text-indigo-600 flex items-center justify-center gap-0.5 transition-all mt-auto"
                  >
                    <span class="material-symbols-rounded text-xs">add</span>
                    <span>Thêm</span>
                  </button>
                </div>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- VIEW 3: 7 CỘT TUẦN                                        -->
    <!-- ======================================================== -->
    <div v-else-if="currentView === 'columns'" class="overflow-x-auto pb-2">
      <div class="grid grid-cols-7 gap-3 min-w-[860px]">
        <div
          v-for="d in weekDays"
          :key="d.dateStr"
          class="bg-white border rounded-2xl p-3 flex flex-col min-h-[480px] shadow-sm transition-all"
          :class="d.isToday ? 'border-indigo-500 ring-1 ring-indigo-500' : 'border-slate-200'"
        >
          <div
            @click="selectDate(d.dateStr)"
            class="cursor-pointer pb-2.5 mb-2.5 border-b border-slate-100 text-center rounded-xl py-1 hover:bg-slate-50"
          >
            <div class="text-xs font-bold text-slate-500">{{ d.fullDayName }}</div>
            <div class="text-base font-black font-mono mt-0.5" :class="d.isToday ? 'text-indigo-600' : 'text-slate-900'">
              {{ d.dayDateFormatted }}
            </div>
          </div>

          <div class="space-y-2 flex-1">
            <div
              v-for="e in d.events"
              :key="e.id"
              @click="openEditModal(e)"
              class="p-2.5 rounded-xl border bg-slate-50/70 hover:bg-white text-left cursor-pointer transition-all shadow-sm"
              :style="{
                borderColor: getCardBorder(e.category, e.color),
                borderLeftWidth: '4px',
                borderLeftColor: getBadgeHex(e.category, e.color),
              }"
            >
              <div class="text-[10px] font-mono font-bold text-slate-500">
                {{ formatTime(e.startTime) }} - {{ formatTime(e.endTime) }}
              </div>
              <div class="text-xs font-bold text-slate-900 mt-0.5 line-clamp-2">
                {{ e.title }}
              </div>
            </div>

            <button
              type="button"
              @click="openAddModalFor(d.dateStr)"
              class="w-full py-2.5 rounded-xl border border-dashed border-slate-200 hover:border-indigo-400 hover:bg-indigo-50/50 text-xs font-bold text-slate-400 hover:text-indigo-600 flex items-center justify-center gap-1 transition-all"
            >
              <span class="material-symbols-rounded text-sm">add</span>
              <span>Thêm lịch</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- MODAL THÊM / SỬA LỊCH TRÌNH (TỐI ƯU MOBILE BOTTOM SHEET)  -->
    <!-- ======================================================== -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-end sm:items-center justify-center p-0 sm:px-4 bg-slate-900/50 backdrop-blur-sm">
      <div class="w-full max-w-md max-h-[92vh] overflow-y-auto bg-white border border-slate-200 rounded-t-3xl sm:rounded-3xl p-5 sm:p-6 shadow-xl transition-all">
        <div class="flex items-center justify-between pb-3 mb-4 border-b border-slate-100">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-2xl bg-indigo-50 text-indigo-600 flex items-center justify-center">
              <span class="material-symbols-rounded text-xl">{{ isEditing ? 'edit_calendar' : 'add_circle' }}</span>
            </div>
            <div>
              <h3 class="text-base font-extrabold text-slate-900">
                {{ isEditing ? 'Chỉnh Sửa Lịch Trình' : 'Thêm Lịch Trình Mới' }}
              </h3>
              <p class="text-xs text-slate-500">Ca học, ca làm việc hoặc cuộc hẹn</p>
            </div>
          </div>
          <button type="button" @click="showModal = false" class="text-slate-400 hover:text-slate-700">
            <span class="material-symbols-rounded">close</span>
          </button>
        </div>

        <form @submit.prevent="handleSaveEvent" class="space-y-3.5">
          <!-- Title -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">Tên công việc / Ca học / Lịch hẹn</label>
            <input
              v-model="eventForm.title"
              type="text"
              required
              placeholder="Nhập tên lịch trình..."
              class="w-full px-4 py-2.5 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-sm font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <!-- Chọn nhanh khung giờ Ca Sáng / Chiều / Tối -->
          <div>
            <label class="block text-[11px] font-bold text-slate-500 mb-1">Chọn nhanh ca trong ngày:</label>
            <div class="grid grid-cols-3 gap-2">
              <button
                type="button"
                @click="applyQuickShift('08:00', '11:00')"
                class="py-1.5 px-2 rounded-xl bg-slate-100 hover:bg-indigo-50 text-slate-700 hover:text-indigo-700 border border-slate-200 text-xs font-bold transition-all"
              >
                Ca Sáng (8h-11h)
              </button>
              <button
                type="button"
                @click="applyQuickShift('13:30', '16:30')"
                class="py-1.5 px-2 rounded-xl bg-slate-100 hover:bg-indigo-50 text-slate-700 hover:text-indigo-700 border border-slate-200 text-xs font-bold transition-all"
              >
                Ca Chiều (13h30)
              </button>
              <button
                type="button"
                @click="applyQuickShift('18:30', '21:00')"
                class="py-1.5 px-2 rounded-xl bg-slate-100 hover:bg-indigo-50 text-slate-700 hover:text-indigo-700 border border-slate-200 text-xs font-bold transition-all"
              >
                Ca Tối (18h30)
              </button>
            </div>
          </div>

          <!-- Time range -->
          <div class="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-bold text-slate-700 mb-1">Bắt đầu</label>
              <input
                v-model="eventForm.startTime"
                type="datetime-local"
                required
                class="w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
              />
            </div>
            <div>
              <label class="block text-xs font-bold text-slate-700 mb-1">Kết thúc</label>
              <input
                v-model="eventForm.endTime"
                type="datetime-local"
                required
                class="w-full px-3 py-2 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs font-semibold focus:bg-white focus:border-indigo-600 focus:outline-none"
              />
            </div>
          </div>

          <!-- Category Selection -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1.5">Danh mục</label>
            <div class="grid grid-cols-4 gap-2">
              <button
                v-for="cat in ['Học tập', 'Đi làm', 'Họp', 'Khác']"
                :key="cat"
                type="button"
                @click="eventForm.category = cat"
                class="py-2 px-2 rounded-xl text-xs font-bold transition-all border"
                :class="eventForm.category === cat
                  ? 'bg-indigo-600 text-white border-indigo-600 shadow-sm'
                  : 'bg-slate-50 text-slate-700 border-slate-200 hover:bg-slate-100'"
              >
                {{ cat }}
              </button>
            </div>
          </div>

          <!-- Description -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1">Địa điểm / Ghi chú (tùy chọn)</label>
            <input
              v-model="eventForm.description"
              type="text"
              placeholder="Nhập phòng học, địa điểm hoặc ghi chú..."
              class="w-full px-4 py-2 bg-slate-50 border border-slate-300 rounded-xl text-slate-900 text-xs focus:bg-white focus:border-indigo-600 focus:outline-none"
            />
          </div>

          <!-- Color palette picker -->
          <div>
            <label class="block text-xs font-bold text-slate-700 mb-1.5">Màu đánh dấu</label>
            <div class="flex items-center gap-2.5">
              <button
                v-for="c in colorPresets"
                :key="c.name"
                type="button"
                @click="eventForm.color = c.hex"
                class="w-8 h-8 rounded-full border-2 transition-all flex items-center justify-center"
                :class="eventForm.color === c.hex ? 'border-slate-900 scale-110' : 'border-transparent'"
                :style="{ backgroundColor: c.hex }"
              >
                <span v-if="eventForm.color === c.hex" class="material-symbols-rounded text-xs text-white font-bold">check</span>
              </button>
            </div>
          </div>

          <!-- Actions -->
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
              class="flex items-center gap-1.5 px-5 py-2.5 bg-indigo-600 hover:bg-indigo-700 text-white rounded-xl text-xs font-bold transition-all shadow-sm active:scale-95"
            >
              <span class="material-symbols-rounded text-base">check</span>
              <span>{{ isEditing ? 'Lưu Cập Nhật' : 'Tạo Lịch Trình' }}</span>
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch } from 'vue';
import { apiFetch } from '../services/api';

const props = defineProps({
  events: { type: Array, default: () => [] },
});
const emit = defineEmits(['refresh-events']);

const API_BASE_URL = '/events';
const EVENTS_STORAGE_KEY = 'second_brain_events_v1';

const loadLocalEvents = () => {
  try {
    const raw = localStorage.getItem(EVENTS_STORAGE_KEY);
    return raw ? JSON.parse(raw) : [];
  } catch (e) {
    return [];
  }
};

const saveLocalEvents = (list) => {
  try {
    localStorage.setItem(EVENTS_STORAGE_KEY, JSON.stringify(list));
  } catch (e) {}
};

const localEvents = ref(props.events.length > 0 ? [...props.events] : loadLocalEvents());

watch(
  () => props.events,
  (newVal) => {
    if (Array.isArray(newVal) && newVal.length > 0) {
      localEvents.value = [...newVal];
      saveLocalEvents(localEvents.value);
    }
  },
  { deep: true }
);

const currentView = ref('split');
const currentWeekStart = ref(getMondayOf(new Date()));
const selectedDate = ref(formatDateKey(new Date()));

const showModal = ref(false);
const isEditing = ref(false);

const colorPresets = [
  { name: 'Indigo', hex: '#4f46e5' },
  { name: 'Emerald', hex: '#10b981' },
  { name: 'Orange', hex: '#f97316' },
  { name: 'Rose', hex: '#f43f5e' },
  { name: 'Cyan', hex: '#06b6d4' },
  { name: 'Violet', hex: '#8b5cf6' },
];

const eventForm = ref({
  id: null,
  title: '',
  description: '',
  startTime: '',
  endTime: '',
  category: 'Học tập',
  color: '#4f46e5',
  isCompleted: false,
});

function getMondayOf(d) {
  const date = new Date(d);
  const day = date.getDay();
  const diff = date.getDate() - day + (day === 0 ? -6 : 1);
  date.setDate(diff);
  date.setHours(0, 0, 0, 0);
  return date;
}

function formatDateKey(d) {
  const y = d.getFullYear();
  const m = String(d.getMonth() + 1).padStart(2, '0');
  const day = String(d.getDate()).padStart(2, '0');
  return `${y}-${m}-${day}`;
}

const todayStr = computed(() => formatDateKey(new Date()));

const isCurrentWeek = computed(() => {
  const nowMonday = getMondayOf(new Date());
  return currentWeekStart.value.getTime() === nowMonday.getTime();
});

const weekDays = computed(() => {
  const days = [];
  const dayLabels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
  const fullNames = ['Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'];

  for (let i = 0; i < 7; i++) {
    const d = new Date(currentWeekStart.value);
    d.setDate(d.getDate() + i);
    const dateStr = formatDateKey(d);

    const evts = localEvents.value
      .filter(e => e.startTime && e.startTime.startsWith(dateStr))
      .sort((a, b) => new Date(a.startTime) - new Date(b.startTime));

    days.push({
      date: d,
      dateStr,
      dayLabel: dayLabels[i],
      fullDayName: fullNames[i],
      dayDateFormatted: `${d.getDate()}/${d.getMonth() + 1}`,
      isToday: dateStr === todayStr.value,
      events: evts,
    });
  }
  return days;
});

const weekRangeLabel = computed(() => {
  const start = new Date(currentWeekStart.value);
  const end = new Date(currentWeekStart.value);
  end.setDate(end.getDate() + 6);
  return `${start.getDate()}/${start.getMonth() + 1} - ${end.getDate()}/${end.getMonth() + 1}`;
});

const selectedDayInfo = computed(() => {
  const found = weekDays.value.find(d => d.dateStr === selectedDate.value);
  if (found) {
    return {
      fullDayName: found.fullDayName,
      dateShort: `${found.dayLabel} • ${found.dayDateFormatted}`,
    };
  }
  return { fullDayName: 'Ngày đang chọn', dateShort: selectedDate.value };
});

const selectedDayEvents = computed(() => {
  return localEvents.value
    .filter(e => e.startTime && e.startTime.startsWith(selectedDate.value))
    .sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
});

const getBadgeHex = (category, hex) => {
  if (hex) return hex;
  if (category === 'Họp') return '#f43f5e';
  if (category === 'Học tập') return '#4f46e5';
  if (category === 'Đi làm') return '#10b981';
  return '#f97316';
};

const getCardBorder = (category, hex) => {
  return getBadgeHex(category, hex) + '40';
};

const formatTime = (dateStr) => {
  if (!dateStr) return '';
  const d = new Date(dateStr);
  return d.toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', hour12: false });
};

const selectDate = (dateStr) => {
  selectedDate.value = dateStr;
};

const prevWeek = () => {
  const prev = new Date(currentWeekStart.value);
  prev.setDate(prev.getDate() - 7);
  currentWeekStart.value = prev;
  selectedDate.value = formatDateKey(prev);
};

const nextWeek = () => {
  const next = new Date(currentWeekStart.value);
  next.setDate(next.getDate() + 7);
  currentWeekStart.value = next;
  selectedDate.value = formatDateKey(next);
};

const goToCurrentWeek = () => {
  currentWeekStart.value = getMondayOf(new Date());
  selectedDate.value = todayStr.value;
};

const getEventsInPeriod = (dateStr, period) => {
  const dayEvts = localEvents.value.filter(e => e.startTime && e.startTime.startsWith(dateStr));
  return dayEvts
    .filter(e => {
      const hour = new Date(e.startTime).getHours();
      if (period === 'Sáng') return hour < 12;
      if (period === 'Chiều') return hour >= 12 && hour < 18;
      if (period === 'Tối') return hour >= 18;
      return false;
    })
    .sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
};

const applyQuickShift = (startHHMM, endHHMM) => {
  const datePart = (eventForm.value.startTime || '').split('T')[0] || selectedDate.value || todayStr.value;
  eventForm.value.startTime = `${datePart}T${startHHMM}`;
  eventForm.value.endTime = `${datePart}T${endHHMM}`;
};

const openAddModalFor = (dateStr) => {
  isEditing.value = false;
  const targetDate = dateStr || selectedDate.value || todayStr.value;
  eventForm.value = {
    id: null,
    title: '',
    description: '',
    startTime: `${targetDate}T08:00`,
    endTime: `${targetDate}T10:00`,
    category: 'Học tập',
    color: '#4f46e5',
    isCompleted: false,
  };
  showModal.value = true;
};

const openAddModalForPeriod = (dateStr, period) => {
  isEditing.value = false;
  const targetDate = dateStr || selectedDate.value || todayStr.value;
  let start = '08:00';
  let end = '11:00';
  if (period === 'Chiều') {
    start = '13:30';
    end = '16:30';
  } else if (period === 'Tối') {
    start = '18:30';
    end = '21:00';
  }

  eventForm.value = {
    id: null,
    title: '',
    description: '',
    startTime: `${targetDate}T${start}`,
    endTime: `${targetDate}T${end}`,
    category: 'Học tập',
    color: '#4f46e5',
    isCompleted: false,
  };
  showModal.value = true;
};

const openEditModal = (evt) => {
  isEditing.value = true;
  const formatForInput = (isoStr) => {
    if (!isoStr) return '';
    return isoStr.slice(0, 16);
  };
  eventForm.value = {
    id: evt.id,
    title: evt.title,
    description: evt.description || '',
    startTime: formatForInput(evt.startTime),
    endTime: formatForInput(evt.endTime),
    category: evt.category || 'Học tập',
    color: evt.color || '#4f46e5',
    isCompleted: evt.isCompleted || false,
  };
  showModal.value = true;
};

const handleSaveEvent = async () => {
  if (!eventForm.value.title.trim()) return;

  const itemToSave = {
    id: isEditing.value && eventForm.value.id
      ? eventForm.value.id
      : (typeof crypto !== 'undefined' && crypto.randomUUID ? crypto.randomUUID() : 'evt_' + Date.now()),
    title: eventForm.value.title.trim(),
    description: eventForm.value.description?.trim() || '',
    startTime: eventForm.value.startTime,
    endTime: eventForm.value.endTime,
    category: eventForm.value.category,
    color: eventForm.value.color,
    isCompleted: eventForm.value.isCompleted,
  };

  if (isEditing.value) {
    const idx = localEvents.value.findIndex(e => e.id === itemToSave.id);
    if (idx !== -1) localEvents.value[idx] = itemToSave;
  } else {
    localEvents.value.push(itemToSave);
  }

  saveLocalEvents(localEvents.value);
  showModal.value = false;
  emit('refresh-events');

  const url = isEditing.value ? `${API_BASE_URL}/${itemToSave.id}` : API_BASE_URL;
  const method = isEditing.value ? 'PUT' : 'POST';
  apiFetch(url, { method, body: JSON.stringify(itemToSave) })
    .then(async res => {
      if (res.ok) {
        const saved = await res.json();
        const idx = localEvents.value.findIndex(e => e.id === itemToSave.id);
        if (idx !== -1 && saved && saved.id) {
          localEvents.value[idx] = saved;
          saveLocalEvents(localEvents.value);
          emit('refresh-events');
        }
      }
    })
    .catch(() => {});
};

const toggleComplete = async (evt) => {
  evt.isCompleted = !evt.isCompleted;
  saveLocalEvents(localEvents.value);
  emit('refresh-events');

  apiFetch(`${API_BASE_URL}/${evt.id}`, {
    method: 'PUT',
    body: JSON.stringify(evt),
  }).catch(() => {});
};

const deleteEvent = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa lịch trình này?')) return;
  localEvents.value = localEvents.value.filter(e => e.id !== id);
  saveLocalEvents(localEvents.value);
  emit('refresh-events');

  apiFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' }).catch(() => {});
};
</script>
