<template>
  <div class="fixed bottom-20 md:bottom-6 right-3.5 md:right-6 z-40 flex flex-col items-end">
    <!-- CỬA SỔ CHAT WIDGET (TỐI ƯU SÁNG RÕ & GỌN TRÊN ĐIỆN THOẠI) -->
    <transition
      enter-active-class="transition duration-200 ease-out transform"
      enter-from-class="opacity-0 translate-y-4 scale-95"
      enter-to-class="opacity-100 translate-y-0 scale-100"
      leave-active-class="transition duration-150 ease-in transform"
      leave-from-class="opacity-100 translate-y-0 scale-100"
      leave-to-class="opacity-0 translate-y-4 scale-95"
    >
      <div
        v-if="isOpen"
        class="w-[calc(100vw-1.75rem)] sm:w-[400px] h-[480px] sm:h-[540px] max-h-[calc(100vh-8rem)] bg-white border border-slate-200 rounded-3xl shadow-2xl flex flex-col overflow-hidden mb-3"
      >
        <!-- Header -->
        <div class="px-4 py-3 bg-indigo-600 text-white flex items-center justify-between">
          <div class="flex items-center gap-2.5">
            <div class="w-9 h-9 rounded-xl bg-white/15 flex items-center justify-center">
              <span class="material-symbols-rounded text-xl">smart_toy</span>
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h3 class="text-sm font-extrabold tracking-tight">Trợ Lý AI Second Brain</h3>
                <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-white/20 text-white">
                  Xem trước
                </span>
              </div>
              <p class="text-[11px] text-indigo-100">Kiểm tra đề xuất rồi xác nhận để lưu</p>
            </div>
          </div>

          <div class="flex items-center gap-1">
            <button
              type="button"
              @click="clearMessages"
              title="Làm mới đoạn chat"
              class="w-8 h-8 rounded-xl text-indigo-100 hover:text-white hover:bg-white/15 flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-base">restart_alt</span>
            </button>
            <button
              type="button"
              @click="isOpen = false"
              title="Đóng chat"
              class="w-8 h-8 rounded-xl text-indigo-100 hover:text-white hover:bg-white/15 flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-lg">close</span>
            </button>
          </div>
        </div>

        <!-- Messages Container -->
        <div ref="chatContainer" class="flex-1 overflow-y-auto p-4 space-y-3 bg-slate-50/60 scroll-smooth">
          <div
            v-for="(msg, idx) in messages"
            :key="idx"
            class="flex flex-col"
            :class="msg.role === 'user' ? 'items-end' : 'items-start'"
          >
            <div
              class="max-w-[85%] rounded-2xl px-4 py-2.5 text-xs leading-relaxed"
              :class="msg.role === 'user'
                ? 'bg-indigo-600 text-white rounded-br-none shadow-sm font-medium'
                : 'bg-white text-slate-800 border border-slate-200 rounded-bl-none shadow-sm'"
            >
              <div class="whitespace-pre-wrap">{{ msg.text }}</div>

              <div v-if="msg.actionExecuted === 'preview'" class="mt-3 space-y-3">
                <div class="text-xs whitespace-pre-line bg-indigo-50 p-3 rounded-lg">{{ proposalSummary(msg.actionData.proposal) }}</div>
                <div v-if="!msg.confirmed" class="flex gap-2"><button class="primary-button" :disabled="isLoading" @click="confirmProposal(msg)">Xác nhận lưu</button><button class="secondary-button" @click="msg.confirmed=true">Bỏ qua</button></div>
                <p v-else class="text-xs text-slate-500">Đề xuất đã được xử lý.</p>
              </div>
              <!-- Kết quả -->
              <div
                v-if="msg.actionData && msg.actionExecuted !== 'preview'"
                class="mt-2.5 p-2.5 rounded-xl bg-slate-50 border border-slate-200 space-y-1.5 text-[11px] text-slate-800"
              >
                <div v-if="msg.actionExecuted === 'transaction'" class="flex items-center justify-between">
                  <div class="flex items-center gap-1.5 font-bold" :class="msg.actionData.amount < 0 ? 'text-rose-600' : 'text-emerald-600'">
                    <span class="material-symbols-rounded text-sm">
                      {{ msg.actionData.amount < 0 ? 'trending_down' : 'trending_up' }}
                    </span>
                    <span>{{ msg.actionData.amount < 0 ? 'Đã trừ chi tiêu' : 'Đã cộng ví' }}</span>
                  </div>
                  <span class="font-mono font-bold" :class="msg.actionData.amount < 0 ? 'text-rose-600' : 'text-emerald-600'">
                    {{ formatVND(msg.actionData.amount) }}
                  </span>
                </div>

                <div v-if="msg.actionExecuted === 'transaction'" class="text-[10px] text-slate-500 flex items-center justify-between pt-1 border-t border-slate-200">
                  <span>Ví: <strong class="text-slate-900">{{ msg.actionData.walletName }}</strong></span>
                  <span>Số dư mới: <strong class="text-slate-900 font-mono">{{ formatVND(msg.actionData.newBalance) }}</strong></span>
                </div>

                <div v-else-if="msg.actionExecuted === 'event'" class="space-y-1 text-indigo-600">
                  <div class="flex items-center gap-1.5 font-bold">
                    <span class="material-symbols-rounded text-sm">calendar_month</span>
                    <span class="text-slate-900">{{ msg.actionData.title }}</span>
                  </div>
                  <div class="text-[10px] text-slate-500">
                    {{ formatEventTime(msg.actionData.startTime) }}
                  </div>
                </div>

                <div v-else-if="msg.actionExecuted === 'task'" class="space-y-1 text-indigo-600">
                  <div class="flex items-center gap-1.5 font-bold">
                    <span class="material-symbols-rounded text-sm">task_alt</span>
                    <span class="text-slate-900">{{ msg.actionData.title }}</span>
                  </div>
                </div>
              </div>

              <div class="text-[9px] mt-1 text-right" :class="msg.role === 'user' ? 'text-indigo-200' : 'text-slate-400'">
                {{ msg.time }}
              </div>
            </div>
          </div>

          <!-- Typing Indicator -->
          <div v-if="isLoading" class="flex items-center gap-2 text-xs text-slate-600 bg-white border border-slate-200 px-4 py-2 rounded-2xl rounded-bl-none w-fit shadow-sm">
            <span class="material-symbols-rounded text-sm animate-spin text-indigo-600">sync</span>
            <span>AI đang xử lý yêu cầu...</span>
          </div>
        </div>

        <!-- Input Bar -->
        <div class="p-3 bg-white border-t border-slate-200">
          <form @submit.prevent="sendMessage" class="flex items-center gap-2">
            <input
              ref="inputField"
              v-model="inputText"
              type="text"
              :disabled="isLoading"
              placeholder="Nhắn yêu cầu ghi thu chi hoặc tạo lịch..."
              class="flex-1 bg-slate-50 border border-slate-300 rounded-2xl px-4 py-2.5 text-xs font-semibold text-slate-900 placeholder-slate-400 focus:bg-white focus:outline-none focus:border-indigo-600 transition-colors"
            />
            <button
              type="submit"
              :disabled="!inputText.trim() || isLoading"
              class="w-10 h-10 rounded-2xl bg-indigo-600 hover:bg-indigo-700 disabled:opacity-40 text-white flex items-center justify-center transition-all active:scale-95 shrink-0 shadow-sm"
              title="Gửi tin nhắn"
            >
              <span class="material-symbols-rounded text-xl">send</span>
            </button>
          </form>
        </div>
      </div>
    </transition>

    <!-- NÚT TRÒN TOGGLE CHAT -->
    <button
      type="button"
      @click="toggleChat"
      class="group relative flex items-center justify-center p-3 md:px-4 md:py-3 rounded-full bg-indigo-600 hover:bg-indigo-700 text-white shadow-lg shadow-indigo-600/30 hover:scale-105 active:scale-95 transition-all duration-200"
      title="Mở Trợ lý AI"
    >
      <div class="relative flex items-center justify-center">
        <span class="material-symbols-rounded text-2xl">
          {{ isOpen ? 'expand_more' : 'smart_toy' }}
        </span>
      </div>
      <span class="hidden md:inline font-bold text-xs tracking-wide ml-2">Trợ lý AI</span>
    </button>
  </div>
</template>

<script setup>
import { ref, nextTick } from 'vue';
import { apiFetch } from '../services/api';

const emit = defineEmits(['data-updated']);

const isOpen = ref(false);
const inputText = ref('');
const isLoading = ref(false);
const chatContainer = ref(null);
const inputField = ref(null);

const messages = ref([
  {
    role: 'assistant',
    text: 'Xin chào! Tôi là Trợ lý AI Second Brain. Bạn có thể nhắn yêu cầu ghi thu chi, thêm lịch hẹn hoặc công việc cần làm để tôi tự động cập nhật.',
    time: formatTimeNow(),
  },
]);

function formatTimeNow() {
  const d = new Date();
  return `${d.getHours().toString().padStart(2, '0')}:${d.getMinutes().toString().padStart(2, '0')}`;
}

const formatVND = (val) => {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND', maximumFractionDigits: 0 }).format(val || 0);
};

const formatEventTime = (isoStr) => {
  if (!isoStr) return '';
  const d = new Date(isoStr);
  return d.toLocaleString('vi-VN', {
    hour: '2-digit',
    minute: '2-digit',
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
};

const toggleChat = () => {
  isOpen.value = !isOpen.value;
  if (isOpen.value) {
    scrollToBottom();
    nextTick(() => {
      inputField.value?.focus();
    });
  }
};

const scrollToBottom = () => {
  nextTick(() => {
    if (chatContainer.value) {
      chatContainer.value.scrollTop = chatContainer.value.scrollHeight;
    }
  });
};

const clearMessages = () => {
  messages.value = [
    {
      role: 'assistant',
      text: 'Đoạn hội thoại đã được làm mới. Tôi sẵn sàng hỗ trợ bạn!',
      time: formatTimeNow(),
    },
  ];
};

const proposalSummary = p => {
  const lines = [];
  if (p.actionType === 'add_expense' || p.actionType === 'add_income') {
    lines.push(p.actionType === 'add_expense' ? 'Ghi khoản chi' : 'Ghi khoản thu');
    lines.push('Số tiền: ' + formatVND(p.amount), 'Ví: ' + (p.walletName || 'Chưa xác định — hãy nhập lại tên ví'));
    if (p.category) lines.push('Danh mục: ' + p.category);
  } else if (p.actionType === 'add_event') {
    for (const e of p.events?.length ? p.events : [p]) lines.push(e.title || 'Lịch hẹn', formatEventTime(e.startTime) + ' → ' + formatEventTime(e.endTime));
  } else lines.push(p.title || 'Công việc mới', p.dueDate ? 'Hạn: ' + formatEventTime(p.dueDate) : 'Chưa có hạn hoàn thành');
  return lines.join('\n');
};
const confirmProposal = async msg => {
  if (isLoading.value || msg.confirmed) return;
  isLoading.value = true;
  try {
    const res = await apiFetch('/aiassistant/chat', { method:'POST', body:JSON.stringify({ message:msg.sourceMessage, proposalToken:msg.actionData.proposalToken }) });
    const data = await res.json();
    if (!res.ok || !data.success) throw new Error(data.message || data.reply || 'Chưa lưu được.');
    msg.confirmed = true;
    messages.value.push({ role:'assistant', text:data.reply, actionExecuted:data.actionExecuted, actionData:data.data, time:formatTimeNow() });
    emit('data-updated');
    window.dispatchEvent(new CustomEvent('secondbrain-data-updated', {detail:{action:data.actionExecuted}}));
  } catch(e) { messages.value.push({role:'assistant',text:e.message,time:formatTimeNow()}); }
  finally { isLoading.value=false; scrollToBottom(); }
};
const sendMessage = async () => {
  const text = inputText.value.trim();
  if (!text || isLoading.value) return;

  messages.value.push({
    role: 'user',
    text: text,
    time: formatTimeNow(),
  });

  inputText.value = '';
  isLoading.value = true;
  scrollToBottom();

  try {
    const res = await apiFetch('/aiassistant/chat', {
      method: 'POST',
      body: JSON.stringify({ message: text }),
    });

    if (!res.ok) {
      throw new Error(`Lỗi kết nối (${res.status})`);
    }

    const result = await res.json();

    messages.value.push({
      role: 'assistant',
      text: result.reply || 'Đã thực hiện xong yêu cầu của bạn.',
      sourceMessage: text,
      actionExecuted: result.actionExecuted,
      actionData: result.data,
      time: formatTimeNow(),
    });

    if (result.actionExecuted && !['none','preview'].includes(result.actionExecuted)) {
      emit('data-updated', {
        action: result.actionExecuted,
        data: result.data,
      });
      window.dispatchEvent(
        new CustomEvent('secondbrain-data-updated', {
          detail: { action: result.actionExecuted, data: result.data },
        })
      );
    }
  } catch (err) {
    console.error(err);
    messages.value.push({
      role: 'assistant',
      text: 'Rất tiếc, đã có lỗi xảy ra trong quá trình xử lý. Bạn vui lòng thử lại nhé!',
      time: formatTimeNow(),
    });
  } finally {
    isLoading.value = false;
    scrollToBottom();
  }
};
</script>
