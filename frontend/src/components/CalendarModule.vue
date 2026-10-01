<template>
  <div class="space-y-4">
    <!-- Top App Bar / WeekBoard Navigation Bar -->
    <header class="bg-[#171c24] border border-[#262a34] rounded-2xl px-4 py-3 flex flex-wrap items-center justify-between gap-3 md-elevation-1">
      <!-- Left: Logo & Week Navigator -->
      <div class="flex items-center gap-3">
        <div class="flex items-center gap-2 pr-3 border-r border-[#262a34]">
          <div class="w-8 h-8 rounded-xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-lg">view_week</span>
          </div>
          <span class="font-extrabold text-sm text-[#e1e2ec] tracking-wide">WeekBoard</span>
        </div>

        <!-- Week navigation control -->
        <div class="flex items-center gap-1.5">
          <button
            @click="prevWeek"
            class="w-7 h-7 rounded-lg bg-[#1c2029] hover:bg-[#262a34] text-[#c1c7ce] hover:text-white flex items-center justify-center transition-all"
            title="Tuần trước"
          >
            <span class="material-symbols-rounded text-base">chevron_left</span>
          </button>

          <button
            @click="goToCurrentWeek"
            class="flex items-center gap-2 px-3 py-1 rounded-xl bg-[#1c2029] hover:bg-[#262a34] border border-[#262a34] text-xs font-semibold text-[#e1e2ec] transition-all"
          >
            <span class="material-symbols-rounded text-emerald-400 text-sm">calendar_month</span>
            <div class="flex flex-col text-left leading-none">
              <span class="text-[11px] font-bold text-white">{{ isCurrentWeek ? 'Tuần này' : 'Tuần được chọn' }}</span>
              <span class="text-[10px] text-[#8b9198] mt-0.5">{{ weekRangeLabel }}</span>
            </div>
            <span class="material-symbols-rounded text-xs text-[#8b9198]">cached</span>
          </button>

          <button
            @click="nextWeek"
            class="w-7 h-7 rounded-lg bg-[#1c2029] hover:bg-[#262a34] text-[#c1c7ce] hover:text-white flex items-center justify-center transition-all"
            title="Tuần sau"
          >
            <span class="material-symbols-rounded text-base">chevron_right</span>
          </button>
        </div>
      </div>

      <!-- Right: View Mode Toggle & Actions -->
      <div class="flex items-center gap-2">
        <!-- View mode switcher tabs (Tương tự toolbar trong ảnh: Split Agenda, Week Columns, Grid Matrix) -->
        <div class="flex items-center bg-[#1c2029] border border-[#262a34] rounded-xl p-0.5">
          <button
            @click="currentView = 'split'"
            class="px-2.5 py-1 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all"
            :class="currentView === 'split' ? 'bg-[#005237] text-[#5dfec1] font-bold md-elevation-1' : 'text-[#8b9198] hover:text-[#c1c7ce]'"
            title="Chế độ Chi tiết ngày (Agenda)"
          >
            <span class="material-symbols-rounded text-base">view_sidebar</span>
            <span class="hidden sm:inline">Chi tiết</span>
          </button>

          <button
            @click="currentView = 'columns'"
            class="px-2.5 py-1 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all"
            :class="currentView === 'columns' ? 'bg-[#005237] text-[#5dfec1] font-bold md-elevation-1' : 'text-[#8b9198] hover:text-[#c1c7ce]'"
            title="Chế độ 7 Cột Tuần"
          >
            <span class="material-symbols-rounded text-base">view_column</span>
            <span class="hidden sm:inline">7 Cột</span>
          </button>

          <button
            @click="currentView = 'table'"
            class="px-2.5 py-1 rounded-lg text-xs font-semibold flex items-center gap-1.5 transition-all"
            :class="currentView === 'table' ? 'bg-[#005237] text-[#5dfec1] font-bold md-elevation-1' : 'text-[#8b9198] hover:text-[#c1c7ce]'"
            title="Chế độ Thời khóa biểu Ca / Tiết"
          >
            <span class="material-symbols-rounded text-base">table_chart</span>
            <span class="hidden sm:inline">Thời khóa biểu</span>
          </button>
        </div>

        <!-- Add Event Button -->
        <button
          @click="openAddModalFor(selectedDate)"
          class="flex items-center gap-1 px-3 py-1.5 rounded-xl bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] text-xs font-bold transition-all md-elevation-1"
        >
          <span class="material-symbols-rounded text-base">add</span>
          <span>Thêm</span>
        </button>
      </div>
    </header>

    <!-- ======================================================== -->
    <!-- VIEW 1: SPLIT AGENDA (Ảnh 1)                             -->
    <!-- Trái: Danh sách ca/sự kiện ngày chọn. Phải: Mini 7 ngày  -->
    <!-- ======================================================== -->
    <div v-if="currentView === 'split'" class="grid grid-cols-1 lg:grid-cols-12 gap-4">
      <!-- Cột Trái (8/12): Chi tiết lịch trình ngày được chọn -->
      <div class="lg:col-span-8 bg-[#171c24] border border-[#262a34] rounded-2xl p-5 md-elevation-1 flex flex-col min-h-[500px]">
        <!-- Header Ngày chọn -->
        <div class="flex items-center justify-between pb-4 mb-4 border-b border-[#262a34]">
          <div class="flex items-center gap-3">
            <div class="w-10 h-10 rounded-xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
              <span class="material-symbols-rounded text-xl">event</span>
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h3 class="text-lg font-bold text-white">{{ selectedDayInfo.fullDayName }}</h3>
                <span class="text-xs px-2 py-0.5 rounded-md bg-[#262a34] text-[#cee9da] font-mono">
                  {{ selectedDayInfo.dateShort }}
                </span>
              </div>
              <p class="text-xs text-[#8b9198] mt-0.5">
                {{ selectedDayEvents.length }} công việc / sự kiện
              </p>
            </div>
          </div>

          <div class="flex items-center gap-2">
            <button
              @click="selectDate(todayStr)"
              class="px-2.5 py-1.5 rounded-lg bg-[#1c2029] hover:bg-[#262a34] text-xs font-medium text-[#c1c7ce] flex items-center gap-1 border border-[#262a34]"
            >
              <span class="material-symbols-rounded text-sm">history</span>
              <span>Hôm nay</span>
            </button>
            <button
              @click="openAddModalFor(selectedDate)"
              class="px-3 py-1.5 rounded-lg bg-[#005237] hover:bg-[#006947] text-[#5dfec1] text-xs font-bold flex items-center gap-1"
            >
              <span class="material-symbols-rounded text-sm">add</span>
              <span>Thêm</span>
            </button>
          </div>
        </div>

        <!-- Danh sách sự kiện ngày (Thẻ lớn bo góc chuẩn WeekBoard) -->
        <div v-if="selectedDayEvents.length === 0" class="flex-1 flex flex-col items-center justify-center py-16 text-center">
          <div class="w-14 h-14 rounded-full bg-[#1c2029] text-[#8b9198] flex items-center justify-center mb-3">
            <span class="material-symbols-rounded text-2xl">event_busy</span>
          </div>
          <p class="text-sm font-semibold text-[#c1c7ce]">Không có lịch nào cho ngày này</p>
          <p class="text-xs text-[#8b9198] mt-1 max-w-xs">Bấm "+ Thêm" để ghi chú ca làm việc, học tập hoặc lịch hẹn</p>
          <button
            @click="openAddModalFor(selectedDate)"
            class="mt-4 px-4 py-2 rounded-xl bg-[#005237] text-[#5dfec1] text-xs font-bold flex items-center gap-1.5"
          >
            <span class="material-symbols-rounded text-sm">add_circle</span>
            <span>Tạo sự kiện mới</span>
          </button>
        </div>

        <div v-else class="space-y-3 overflow-y-auto max-h-[620px] pr-1">
          <div
            v-for="evt in selectedDayEvents"
            :key="evt.id"
            class="p-4 rounded-xl border transition-all duration-200 flex items-start justify-between group"
            :style="{
              backgroundColor: getCardBg(evt.category, evt.color),
              borderColor: getCardBorder(evt.category, evt.color),
              borderLeftWidth: '5px'
            }"
          >
            <div class="space-y-2 flex-1 pr-3">
              <!-- Badges: Danh mục, Nguồn, Tần suất, Giờ -->
              <div class="flex flex-wrap items-center gap-2">
                <!-- Checkbox hoàn thành -->
                <button
                  @click="toggleComplete(evt)"
                  class="w-5 h-5 rounded-md border flex items-center justify-center transition-all"
                  :class="evt.isCompleted ? 'bg-emerald-500 border-emerald-500 text-black' : 'border-[#8b9198] hover:border-white text-transparent'"
                >
                  <span class="material-symbols-rounded text-xs font-bold">check</span>
                </button>

                <!-- Category tag -->
                <span
                  class="text-[11px] font-bold px-2 py-0.5 rounded-md text-white"
                  :style="{ backgroundColor: getCategoryBadgeBg(evt.category, evt.color) }"
                >
                  {{ evt.category || 'Khác' }}
                </span>

                <!-- Time range -->
                <span class="text-xs font-mono font-bold text-[#e1e2ec] bg-black/20 px-2 py-0.5 rounded-md">
                  {{ formatTime(evt.startTime) }} - {{ formatTime(evt.endTime) }}
                </span>

                <span v-if="evt.description" class="text-[11px] text-[#8b9198] bg-[#1c2029]/80 px-2 py-0.5 rounded-md">
                  {{ evt.description }}
                </span>
              </div>

              <!-- Title -->
              <h4
                class="text-base font-extrabold text-[#e1e2ec]"
                :class="evt.isCompleted ? 'line-through opacity-50' : ''"
              >
                {{ evt.title }}
              </h4>
            </div>

            <!-- Actions (Edit/Delete) -->
            <div class="flex items-center gap-1 opacity-80 group-hover:opacity-100 transition-opacity">
              <button
                @click="openEditModal(evt)"
                class="w-7 h-7 rounded-lg hover:bg-black/20 text-[#c1c7ce] hover:text-white flex items-center justify-center transition-colors"
                title="Chỉnh sửa"
              >
                <span class="material-symbols-rounded text-sm">edit</span>
              </button>
              <button
                @click="deleteEvent(evt.id)"
                class="w-7 h-7 rounded-lg hover:bg-rose-500/20 text-[#8b9198] hover:text-rose-400 flex items-center justify-center transition-colors"
                title="Xóa"
              >
                <span class="material-symbols-rounded text-sm">delete</span>
              </button>
            </div>
          </div>
        </div>
      </div>

      <!-- Cột Phải (4/12): Danh sách các ngày trong tuần (Ảnh 1 bên phải) -->
      <div class="lg:col-span-4 bg-[#171c24] border border-[#262a34] rounded-2xl p-4 md-elevation-1 flex flex-col">
        <div class="flex items-center justify-between pb-3 mb-3 border-b border-[#262a34]">
          <div>
            <h4 class="font-bold text-sm text-white">Các ngày trong tuần</h4>
            <p class="text-[11px] text-[#8b9198]">Chạm để chọn ngày · xem nhanh</p>
          </div>
          <button
            @click="selectDate(todayStr)"
            class="text-[11px] px-2.5 py-1 rounded-lg bg-[#005237] text-[#5dfec1] font-semibold hover:bg-[#006947] transition-all"
          >
            Về hôm nay
          </button>
        </div>

        <!-- 7 Days mini list -->
        <div class="space-y-2 overflow-y-auto flex-1 max-h-[620px]">
          <div
            v-for="d in weekDays"
            :key="d.dateStr"
            @click="selectDate(d.dateStr)"
            class="p-3 rounded-xl border transition-all cursor-pointer relative"
            :class="[
              selectedDate === d.dateStr 
                ? 'bg-[#1c2029] border-[#38e1a6] md-elevation-2' 
                : 'bg-[#171c24] border-[#262a34] hover:bg-[#1c2029]/60 hover:border-[#41474d]',
              d.isToday ? 'ring-1 ring-[#5dfec1]/40' : ''
            ]"
          >
            <!-- Header Ngày -->
            <div class="flex items-center justify-between">
              <div class="flex items-center gap-2">
                <span
                  class="font-bold text-xs"
                  :class="selectedDate === d.dateStr ? 'text-[#38e1a6]' : 'text-white'"
                >
                  {{ d.dayLabel }} {{ d.dayDateFormatted }}
                </span>
                <span v-if="d.isToday" class="text-[9px] font-bold px-1.5 py-0.2 rounded bg-[#005237] text-[#5dfec1]">
                  Nay
                </span>
              </div>
              <span class="text-xs font-mono font-bold px-2 py-0.5 rounded-full bg-[#262a34] text-[#cee9da]">
                {{ d.events.length }}
              </span>
            </div>

            <!-- Previews của sự kiện trong ngày -->
            <div v-if="d.events.length > 0" class="mt-2 space-y-1">
              <div
                v-for="e in d.events.slice(0, 3)"
                :key="e.id"
                class="flex items-center gap-1.5 text-[11px] truncate text-[#c1c7ce]"
              >
                <span class="w-1.5 h-1.5 rounded-full flex-shrink-0" :style="{ backgroundColor: getBadgeHex(e.category, e.color) }"></span>
                <span class="font-mono text-[10px] text-[#8b9198] flex-shrink-0">{{ formatTime(e.startTime) }}</span>
                <span class="truncate">{{ e.title }}</span>
              </div>
              <div v-if="d.events.length > 3" class="text-[10px] text-[#8b9198] font-medium pl-3">
                +{{ d.events.length - 3 }} việc nữa
              </div>
            </div>
            <div v-else class="text-[11px] text-[#8b9198]/60 mt-1 italic">
              Chưa có sự kiện
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- VIEW 2: 7 WEEK COLUMNS (Ảnh 2)                           -->
    <!-- 7 Cột đứng đại diện cho Thứ 2 -> Chủ Nhật                -->
    <!-- ======================================================== -->
    <div v-else-if="currentView === 'columns'" class="overflow-x-auto pb-2">
      <div class="grid grid-cols-7 gap-2.5 min-w-[900px]">
        <div
          v-for="d in weekDays"
          :key="d.dateStr"
          class="bg-[#171c24] border rounded-2xl p-3 flex flex-col min-h-[580px] transition-all"
          :class="[
            d.isToday ? 'border-[#38e1a6]/60 bg-[#171c24]' : 'border-[#262a34]',
            selectedDate === d.dateStr ? 'ring-1 ring-[#38e1a6]' : ''
          ]"
        >
          <!-- Column Header -->
          <div
            @click="selectDate(d.dateStr)"
            class="cursor-pointer pb-2.5 mb-2.5 border-b border-[#262a34] text-center rounded-xl py-1 transition-colors hover:bg-[#1c2029]"
          >
            <div class="text-xs font-bold text-white">{{ d.dayLabel }}</div>
            <div class="text-sm font-extrabold" :class="d.isToday ? 'text-[#38e1a6]' : 'text-[#c1c7ce]'">
              {{ d.dayDateFormatted }}
            </div>
            <div class="text-[10px] text-[#8b9198]">{{ d.fullDayName }}</div>

            <div class="flex items-center justify-between mt-2 pt-1.5 border-t border-[#262a34]/60 px-1">
              <span class="text-[11px] font-mono font-bold text-[#cee9da]">{{ d.events.length }}</span>
              <button
                @click.stop="openAddModalFor(d.dateStr)"
                class="w-5 h-5 rounded-md bg-[#262a34] hover:bg-[#005237] hover:text-[#5dfec1] text-[#c1c7ce] flex items-center justify-center transition-colors text-xs font-bold"
                title="Thêm sự kiện cho ngày này"
              >
                +
              </button>
            </div>
          </div>

          <!-- Event Cards List in Column -->
          <div class="space-y-2 flex-1 overflow-y-auto pr-0.5">
            <div
              v-for="e in d.events"
              :key="e.id"
              @click="openEditModal(e)"
              class="p-2.5 rounded-xl border text-left cursor-pointer transition-all hover:scale-[1.02] md-elevation-1"
              :style="{
                backgroundColor: getCardBg(e.category, e.color),
                borderColor: getCardBorder(e.category, e.color),
                borderLeftWidth: '4px'
              }"
            >
              <div class="text-[10px] font-mono font-bold text-[#cee9da]">
                {{ formatTime(e.startTime) }} - {{ formatTime(e.endTime) }}
              </div>
              <div class="text-xs font-bold text-[#e1e2ec] mt-0.5 line-clamp-2">
                {{ e.title }}
              </div>
            </div>

            <!-- Empty slot button -->
            <button
              @click="openAddModalFor(d.dateStr)"
              class="w-full py-3 rounded-xl border border-dashed border-[#262a34] hover:border-[#38e1a6]/50 hover:bg-[#1c2029] text-[11px] text-[#8b9198] hover:text-[#5dfec1] flex items-center justify-center gap-1 transition-all mt-2"
            >
              <span class="material-symbols-rounded text-sm">add</span>
              <span>Thêm</span>
            </button>
          </div>
        </div>
      </div>
    </div>

    <!-- ======================================================== -->
    <!-- VIEW 3: TIMETABLE MATRIX CA / TIẾT (Ảnh 3 & Ảnh 4)      -->
    <!-- Ma trận Ca Sáng, Ca Chiều, Ca Tối x Thứ 2..Chủ Nhật      -->
    <!-- ======================================================== -->
    <div v-else-if="currentView === 'table'" class="bg-[#171c24] border border-[#262a34] rounded-2xl p-4 overflow-hidden md-elevation-1">
      <div class="flex items-center justify-between pb-3 mb-3 border-b border-[#262a34]">
        <div class="flex items-center gap-2">
          <span class="text-xs text-[#8b9198]">Thời khóa biểu tuần chia theo 3 buổi:</span>
          <span class="text-xs px-2 py-0.5 rounded-full bg-[#005237] text-[#5dfec1] font-bold">
            Sáng · Chiều · Tối
          </span>
        </div>
        <span class="text-[11px] text-[#8b9198]">Tự động sắp xếp sự kiện sớm nhất lên đầu</span>
      </div>

      <!-- Table Container -->
      <div class="overflow-x-auto">
        <table class="w-full text-left border-collapse min-w-[900px]">
          <thead>
            <tr class="border-b border-[#262a34] bg-[#1c2029]/80">
              <th class="p-3 text-xs font-bold text-[#8b9198] w-24 border-r border-[#262a34] text-center">Buổi</th>
              <th
                v-for="d in weekDays"
                :key="d.dateStr"
                class="p-2.5 text-center border-r border-[#262a34] last:border-r-0"
                :class="d.isToday ? 'bg-[#005237]/20 text-[#5dfec1]' : 'text-[#c1c7ce]'"
              >
                <div class="text-xs font-bold">{{ d.dayLabel }}</div>
                <div class="text-[11px] font-mono text-[#8b9198]">{{ d.dayDateFormatted }}</div>
              </th>
            </tr>
          </thead>
          <tbody>
            <!-- 3 Hàng đơn giản: Sáng, Chiều, Tối -->
            <tr
              v-for="period in ['Sáng', 'Chiều', 'Tối']"
              :key="period"
              class="border-b border-[#262a34] hover:bg-[#1c2029]/20 transition-colors"
            >
              <!-- Cột Buổi -->
              <td class="p-3 text-center font-bold text-sm text-[#cee9da] bg-[#1c2029]/50 border-r border-[#262a34] align-middle">
                <div class="flex flex-col items-center gap-1">
                  <span class="material-symbols-rounded text-lg text-[#38e1a6]">
                    {{ period === 'Sáng' ? 'wb_sunny' : period === 'Chiều' ? 'wb_twilight' : 'nights_stay' }}
                  </span>
                  <span>{{ period }}</span>
                </div>
              </td>

              <!-- 7 Ô Ngày: Danh sách công việc tự động xếp theo giờ sớm nhất trước -->
              <td
                v-for="d in weekDays"
                :key="d.dateStr"
                class="p-2 border-r border-[#262a34] last:border-r-0 align-top min-w-32"
              >
                <div class="space-y-1.5 min-h-[90px] flex flex-col justify-start">
                  <!-- Các sự kiện đã sắp xếp theo giờ trước đến sau -->
                  <div
                    v-for="e in getEventsInPeriod(d.dateStr, period)"
                    :key="e.id"
                    @click="openEditModal(e)"
                    class="p-2 rounded-xl text-xs font-bold cursor-pointer transition-all hover:scale-[1.01] hover:opacity-95 md-elevation-1"
                    :style="{
                      backgroundColor: getCardBg(e.category, e.color),
                      borderColor: getCardBorder(e.category, e.color),
                      borderLeft: `3.5px solid ${getBadgeHex(e.category, e.color)}`,
                      color: '#e1e2ec'
                    }"
                    :title="`${e.title} (${formatTime(e.startTime)} - ${formatTime(e.endTime)})`"
                  >
                    <div class="flex items-center justify-between gap-1 mb-1">
                      <span class="text-[10px] font-mono text-[#5dfec1] font-bold">
                        {{ formatTime(e.startTime) }} - {{ formatTime(e.endTime) }}
                      </span>
                      <span class="text-[9px] px-1.5 py-0.2 rounded font-medium bg-black/40 text-[#c1c7ce]">
                        {{ e.category || 'Lịch' }}
                      </span>
                    </div>
                    <div class="text-xs font-bold line-clamp-2 text-white">{{ e.title }}</div>
                  </div>

                  <!-- Nút thêm công việc nhanh vào buổi này -->
                  <button
                    @click="openAddModalForPeriod(d.dateStr, period)"
                    class="w-full py-1.5 rounded-lg border border-dashed border-[#262a34] hover:border-[#38e1a6]/50 hover:bg-[#1c2029] text-[10px] text-[#8b9198] hover:text-[#5dfec1] flex items-center justify-center gap-1 transition-all mt-auto opacity-40 hover:opacity-100"
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
    <!-- FLOATING ACTION BUTTON (FAB) & MODAL THÊM / SỬA LỊCH      -->
    <!-- ======================================================== -->
    <div v-if="showModal" class="fixed inset-0 z-50 flex items-center justify-center px-4 bg-black/60 backdrop-blur-sm">
      <div class="w-full max-w-md bg-[#1c2029] border border-[#262a34] rounded-[28px] p-6 md-elevation-3 transition-all">
        <!-- Dialog Header -->
        <div class="flex items-center gap-3 mb-4">
          <div class="w-11 h-11 rounded-2xl bg-[#005237] text-[#5dfec1] flex items-center justify-center">
            <span class="material-symbols-rounded text-xl">{{ isEditing ? 'edit_calendar' : 'add_circle' }}</span>
          </div>
          <div>
            <h3 class="text-lg font-bold text-white">
              {{ isEditing ? 'Chỉnh Sửa Lịch Trình' : 'Thêm Lịch Trình Mới' }}
            </h3>
            <p class="text-xs text-[#8b9198]">Ca học tập, công việc hoặc cuộc hẹn</p>
          </div>
        </div>

        <form @submit.prevent="handleSaveEvent" class="space-y-3.5">
          <!-- Title -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Tên công việc / Môn học / Sự kiện</label>
            <input
              v-model="eventForm.title"
              type="text"
              required
              placeholder="Nhập tên sự kiện / công việc..."
              class="w-full px-4 py-2.5 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-sm focus:outline-none focus:border-[#38e1a6] focus:ring-1 focus:ring-[#38e1a6]"
            />
          </div>

          <!-- Description -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Ghi chú / Địa điểm</label>
            <input
              v-model="eventForm.description"
              type="text"
              placeholder="Địa điểm, link họp hoặc ghi chú thêm..."
              class="w-full px-4 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
            />
          </div>

          <!-- Time range -->
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Thời gian bắt đầu</label>
              <input
                v-model="eventForm.startTime"
                type="datetime-local"
                required
                class="w-full px-3 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
              />
            </div>
            <div>
              <label class="block text-xs font-semibold text-[#c1c7ce] mb-1">Thời gian kết thúc</label>
              <input
                v-model="eventForm.endTime"
                type="datetime-local"
                required
                class="w-full px-3 py-2 bg-[#171c24] border border-[#41474d] rounded-xl text-white text-xs focus:outline-none focus:border-[#38e1a6]"
              />
            </div>
          </div>

          <!-- Category Selection (Họp, Học tập, Khác, ...) -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1.5">Loại công việc / Danh mục</label>
            <div class="grid grid-cols-4 gap-2">
              <button
                v-for="cat in ['Họp', 'Học tập', 'Giảng dạy', 'Khác']"
                :key="cat"
                type="button"
                @click="eventForm.category = cat"
                class="py-1.5 px-2 rounded-xl text-xs font-semibold transition-all border"
                :class="eventForm.category === cat 
                  ? 'bg-[#005237] text-[#5dfec1] border-[#38e1a6]' 
                  : 'bg-[#171c24] text-[#c1c7ce] border-[#41474d] hover:bg-[#262a34]'"
              >
                {{ cat }}
              </button>
            </div>
          </div>

          <!-- Color palette picker (tông màu pastel chuẩn như trong ảnh) -->
          <div>
            <label class="block text-xs font-semibold text-[#c1c7ce] mb-1.5">Màu đánh dấu</label>
            <div class="flex items-center gap-2">
              <button
                v-for="c in colorPresets"
                :key="c.name"
                type="button"
                @click="eventForm.color = c.hex"
                class="w-8 h-8 rounded-full border-2 transition-all flex items-center justify-center"
                :class="eventForm.color === c.hex ? 'border-white scale-110' : 'border-transparent'"
                :style="{ backgroundColor: c.hex }"
              >
                <span v-if="eventForm.color === c.hex" class="material-symbols-rounded text-xs text-black font-bold">check</span>
              </button>
            </div>
          </div>

          <!-- Actions -->
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
              class="flex items-center gap-1.5 px-5 py-2.5 bg-[#38e1a6] hover:bg-[#5dfec1] text-[#003824] rounded-full text-xs font-bold transition-all md-elevation-1 active:scale-95"
            >
              <span class="material-symbols-rounded text-base">check</span>
              <span>{{ isEditing ? 'Cập nhật' : 'Tạo lịch hẹn' }}</span>
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

const props = defineProps({
  events: { type: Array, default: () => [] }
});
const emit = defineEmits(['refresh-events']);

const API_BASE_URL = '/events';

// Current view mode: 'split' (ảnh 1), 'columns' (ảnh 2), 'table' (ảnh 3/4)
const currentView = ref('split');

// Current week cursor (anchor Date representing start of selected week)
const currentWeekStart = ref(getMondayOf(new Date()));
const selectedDate = ref(formatDateKey(new Date()));

const showModal = ref(false);
const isEditing = ref(false);
const isEditingShifts = ref(false);

const colorPresets = [
  { name: 'Peach / Cam', hex: '#f97316' },     // Tông cam như IE-PRE, B25
  { name: 'Rose / Hồng', hex: '#f43f5e' },     // Tông đỏ hồng như Họp, VSTEP
  { name: 'Green / Xanh lá', hex: '#10b981' }, // Tông xanh lá như L6, L7, L8
  { name: 'Emerald / Ngọc', hex: '#059669' },  // Tông xanh ngọc như Kèm Khang
  { name: 'Cyan / Xanh biển', hex: '#06b6d4' },
  { name: 'Indigo / Tím', hex: '#6366f1' },
];

const eventForm = ref({
  id: null,
  title: '',
  description: '',
  startTime: '',
  endTime: '',
  category: 'Học tập',
  color: '#f97316',
  isCompleted: false
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

// 7 Ngày của tuần hiện tại
const weekDays = computed(() => {
  const days = [];
  const dayLabels = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];
  const fullNames = ['Thứ Hai', 'Thứ Ba', 'Thứ Tư', 'Thứ Năm', 'Thứ Sáu', 'Thứ Bảy', 'Chủ Nhật'];

  for (let i = 0; i < 7; i++) {
    const d = new Date(currentWeekStart.value);
    d.setDate(d.getDate() + i);
    const dateStr = formatDateKey(d);

    const evts = props.events.filter(e => {
      if (!e.startTime) return false;
      return e.startTime.startsWith(dateStr);
    });

    days.push({
      date: d,
      dateStr,
      dayLabel: dayLabels[i],
      fullDayName: fullNames[i],
      dayDateFormatted: `${d.getDate()}/${d.getMonth() + 1}`,
      isToday: dateStr === todayStr.value,
      events: evts
    });
  }
  return days;
});

const weekRangeLabel = computed(() => {
  const start = new Date(currentWeekStart.value);
  const end = new Date(currentWeekStart.value);
  end.setDate(end.getDate() + 6);
  return `${start.getDate()}-${end.getDate()} Thg ${start.getMonth() + 1}`;
});

const selectedDayInfo = computed(() => {
  const found = weekDays.value.find(d => d.dateStr === selectedDate.value);
  if (found) {
    return {
      fullDayName: found.fullDayName,
      dateShort: `${found.dayLabel} · ${found.dayDateFormatted}`
    };
  }
  return { fullDayName: 'Ngày chọn', dateShort: selectedDate.value };
});

const selectedDayEvents = computed(() => {
  return props.events.filter(e => e.startTime && e.startTime.startsWith(selectedDate.value));
});

// Helpers cho giao diện màu sắc dạng pastel / bảng biểu
const getCardBg = (category, hex) => {
  if (hex) return hex + '1a'; // 10% opacity
  if (category === 'Họp') return '#f43f5e1a';
  if (category === 'Học tập') return '#f973161a';
  return '#10b9811a';
};

const getCardBorder = (category, hex) => {
  if (hex) return hex + '55';
  if (category === 'Họp') return '#f43f5e55';
  if (category === 'Học tập') return '#f9731655';
  return '#10b98155';
};

const getBadgeHex = (category, hex) => {
  if (hex) return hex;
  if (category === 'Họp') return '#f43f5e';
  if (category === 'Học tập') return '#f97316';
  return '#10b981';
};

const getCategoryBadgeBg = (category, hex) => {
  return getBadgeHex(category, hex);
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

// Lọc sự kiện theo Buổi (Sáng, Chiều, Tối) và TỰ ĐỘNG SẮP XẾP SỰ KIỆN SỚM NHẤT TRƯỚC
const getEventsInPeriod = (dateStr, period) => {
  const dayEvts = props.events.filter(e => e.startTime && e.startTime.startsWith(dateStr));
  
  const filtered = dayEvts.filter(e => {
    if (!e.startTime) return false;
    const hour = new Date(e.startTime).getHours();
    if (period === 'Sáng') return hour < 12;            // Sáng: trước 12:00
    if (period === 'Chiều') return hour >= 12 && hour < 18; // Chiều: 12:00 -> 17:59
    if (period === 'Tối') return hour >= 18;           // Tối: 18:00 trở đi
    return false;
  });

  // Sắp xếp: sự kiện nào có thời gian sớm hơn thì xếp trước
  return filtered.sort((a, b) => new Date(a.startTime) - new Date(b.startTime));
};

// Modal Openers
const openAddModalFor = (dateStr) => {
  isEditing.value = false;
  const targetDate = dateStr || selectedDate.value || todayStr.value;
  eventForm.value = {
    id: null,
    title: '',
    description: '',
    startTime: `${targetDate}T09:00`,
    endTime: `${targetDate}T10:30`,
    category: 'Học tập',
    color: '#f97316',
    isCompleted: false
  };
  showModal.value = true;
};

// Mở modal thêm sự kiện với giờ gợi ý chuẩn theo Buổi
const openAddModalForPeriod = (dateStr, period) => {
  isEditing.value = false;
  let startTime = '08:00';
  let endTime = '09:30';

  if (period === 'Chiều') {
    startTime = '14:00';
    endTime = '15:30';
  } else if (period === 'Tối') {
    startTime = '19:00';
    endTime = '20:30';
  }

  eventForm.value = {
    id: null,
    title: '',
    description: '',
    startTime: `${dateStr}T${startTime}`,
    endTime: `${dateStr}T${endTime}`,
    category: 'Học tập',
    color: period === 'Sáng' ? '#f97316' : period === 'Chiều' ? '#10b981' : '#f43f5e',
    isCompleted: false
  };
  showModal.value = true;
};

const openEditModal = (evt) => {
  isEditing.value = true;
  eventForm.value = {
    id: evt.id,
    title: evt.title,
    description: evt.description || '',
    startTime: evt.startTime ? evt.startTime.slice(0, 16) : '',
    endTime: evt.endTime ? evt.endTime.slice(0, 16) : '',
    category: evt.category || 'Học tập',
    color: evt.color || '#f97316',
    isCompleted: evt.isCompleted || false
  };
  showModal.value = true;
};

// API Actions
const handleSaveEvent = async () => {
  if (!eventForm.value.title.trim()) return;

  try {
    const payload = {
      title: eventForm.value.title.trim(),
      description: eventForm.value.description?.trim() || '',
      startTime: eventForm.value.startTime,
      endTime: eventForm.value.endTime,
      category: eventForm.value.category,
      color: eventForm.value.color,
      isCompleted: eventForm.value.isCompleted
    };

    let res;
    if (isEditing.value && eventForm.value.id) {
      res = await apiFetch(`${API_BASE_URL}/${eventForm.value.id}`, {
        method: 'PUT',
        body: JSON.stringify(payload)
      });
    } else {
      res = await apiFetch(API_BASE_URL, {
        method: 'POST',
        body: JSON.stringify(payload)
      });
    }

    if (res.ok) {
      emit('refresh-events');
      showModal.value = false;
    } else {
      alert('Không thể lưu sự kiện. Vui lòng thử lại!');
    }
  } catch (err) {
    console.error('Lỗi lưu sự kiện:', err);
    alert('Đã xảy ra lỗi khi kết nối tới máy chủ.');
  }
};

const toggleComplete = async (evt) => {
  try {
    const payload = {
      title: evt.title,
      description: evt.description,
      startTime: evt.startTime,
      endTime: evt.endTime,
      category: evt.category,
      color: evt.color,
      isCompleted: !evt.isCompleted
    };
    const res = await apiFetch(`${API_BASE_URL}/${evt.id}`, {
      method: 'PUT',
      body: JSON.stringify(payload)
    });
    if (res.ok) emit('refresh-events');
  } catch (err) {
    console.error(err);
  }
};

const deleteEvent = async (id) => {
  if (!confirm('Bạn có chắc muốn xóa sự kiện này?')) return;
  try {
    const res = await apiFetch(`${API_BASE_URL}/${id}`, { method: 'DELETE' });
    if (res.ok) emit('refresh-events');
  } catch (err) {
    console.error(err);
  }
};

onMounted(() => {
  selectedDate.value = todayStr.value;
});
</script>
