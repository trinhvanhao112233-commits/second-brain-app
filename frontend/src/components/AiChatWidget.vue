<template>
  <div class="fixed bottom-6 right-6 z-50 flex flex-col items-end">
    <!-- CỬA SỔ CHAT WIDGET -->
    <transition
      enter-active-class="transition duration-300 ease-out transform"
      enter-from-class="opacity-0 translate-y-6 scale-95"
      enter-to-class="opacity-100 translate-y-0 scale-100"
      leave-active-class="transition duration-200 ease-in transform"
      leave-from-class="opacity-100 translate-y-0 scale-100"
      leave-to-class="opacity-0 translate-y-6 scale-95"
    >
      <div
        v-if="isOpen"
        class="w-[370px] sm:w-[420px] max-w-[calc(100vw-2rem)] h-[560px] max-h-[calc(100vh-6rem)] bg-[#171c24] border border-[#262a34] rounded-3xl shadow-2xl flex flex-col overflow-hidden mb-3 md-elevation-3 backdrop-blur-xl"
      >
        <!-- Header -->
        <div class="px-5 py-3.5 bg-[#1c2029] border-b border-[#262a34] flex items-center justify-between">
          <div class="flex items-center gap-3">
            <div class="relative">
              <div class="w-10 h-10 rounded-2xl bg-gradient-to-tr from-[#005237] to-[#10b981] text-white flex items-center justify-center shadow-lg shadow-emerald-950/50">
                <span class="material-symbols-rounded text-2xl animate-pulse">smart_toy</span>
              </div>
              <span class="absolute -bottom-0.5 -right-0.5 w-3 h-3 bg-[#38e1a6] border-2 border-[#1c2029] rounded-full"></span>
            </div>
            <div>
              <div class="flex items-center gap-2">
                <h3 class="text-sm font-bold text-white tracking-wide">Second Brain AI</h3>
                <span class="px-2 py-0.5 rounded-full text-[10px] font-bold bg-[#005237]/60 text-[#5dfec1] border border-[#38e1a6]/30">
                  Auto-Sync
                </span>
              </div>
              <p class="text-[11px] text-[#8b9198]">Tự động trừ ví & xếp lịch trình</p>
            </div>
          </div>

          <div class="flex items-center gap-1">
            <button
              @click="clearMessages"
              title="Xóa đoạn chat"
              class="w-8 h-8 rounded-xl text-[#8b9198] hover:text-white hover:bg-[#262a34] flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-base">restart_alt</span>
            </button>
            <button
              @click="isOpen = false"
              title="Đóng chat"
              class="w-8 h-8 rounded-xl text-[#8b9198] hover:text-[#f43f5e] hover:bg-[#262a34] flex items-center justify-center transition-colors"
            >
              <span class="material-symbols-rounded text-lg">close</span>
            </button>
          </div>
        </div>

        <!-- Suggestion Chips Bar -->
        <div class="px-4 py-2 bg-[#12161f] border-b border-[#262a34]/60 flex items-center gap-1.5 overflow-x-auto no-scrollbar text-xs">
          <span class="text-[10px] text-[#8b9198] font-bold uppercase tracking-wider flex items-center gap-1 shrink-0 mr-1">
            <span class="material-symbols-rounded text-xs text-[#38e1a6]">bolt</span> Gợi ý:
          </span>
          <button
            v-for="chip in quickChips"
            :key="chip"
            @click="sendQuickPrompt(chip)"
            class="shrink-0 px-2.5 py-1 rounded-full bg-[#1c2029] hover:bg-[#262a34] border border-[#262a34] text-[11px] text-[#c1c7ce] hover:text-white transition-all active:scale-95"
          >
            {{ chip }}
          </button>
        </div>

        <!-- Messages Container -->
        <div ref="chatContainer" class="flex-1 overflow-y-auto p-4 space-y-3.5 scroll-smooth">
          <div
            v-for="(msg, idx) in messages"
            :key="idx"
            class="flex flex-col"
            :class="msg.role === 'user' ? 'items-end' : 'items-start'"
          >
            <div
              class="max-w-[85%] rounded-2xl px-4 py-2.5 text-xs leading-relaxed"
              :class="msg.role === 'user' 
                ? 'bg-[#005237] text-white rounded-br-none shadow-md font-medium' 
                : 'bg-[#1c2029] text-[#e1e2ec] border border-[#262a34] rounded-bl-none shadow-sm'"
            >
              <div class="whitespace-pre-wrap">{{ msg.text }}</div>

              <!-- Thẻ xác nhận hành động tự động -->
              <div
                v-if="msg.actionData"
                class="mt-2.5 p-2.5 rounded-xl bg-[#12161f] border border-[#262a34] space-y-1.5 text-[11px]"
              >
                <!-- Trừ / Nạp Ví -->
                <div v-if="msg.actionExecuted === 'transaction'" class="flex items-center justify-between">
                  <div class="flex items-center gap-1.5 font-bold" :class="msg.actionData.amount < 0 ? 'text-[#ffb4ab]' : 'text-[#5dfec1]'">
                    <span class="material-symbols-rounded text-sm">
                      {{ msg.actionData.amount < 0 ? 'trending_down' : 'trending_up' }}
                    </span>
                    <span>{{ msg.actionData.amount < 0 ? 'Đã trừ chi tiêu' : 'Đã cộng ví' }}</span>
                  </div>
                  <span class="font-mono font-bold" :class="msg.actionData.amount < 0 ? 'text-[#ffb4ab]' : 'text-[#38e1a6]'">
                    {{ formatVND(msg.actionData.amount) }}
                  </span>
                </div>

                <div v-if="msg.actionExecuted === 'transaction'" class="text-[10px] text-[#8b9198] flex items-center justify-between pt-1 border-t border-[#262a34]/60">
                  <span>Ví: <strong class="text-white">{{ msg.actionData.walletName }}</strong></span>
                  <span>Số dư mới: <strong class="text-white font-mono">{{ formatVND(msg.actionData.newBalance) }}</strong></span>
                </div>

                <!-- Lịch hẹn -->
                <div v-else-if="msg.actionExecuted === 'event'" class="space-y-1 text-emerald-400">
                  <div class="flex items-center gap-1.5 font-bold">
                    <span class="material-symbols-rounded text-sm">calendar_month</span>
                    <span class="text-white">{{ msg.actionData.title }}</span>
                  </div>
                  <div class="text-[10px] text-[#8b9198]">
                    {{ formatEventTime(msg.actionData.startTime) }}
                  </div>
                </div>

                <!-- Công việc -->
                <div v-else-if="msg.actionExecuted === 'task'" class="space-y-1 text-indigo-400">
                  <div class="flex items-center gap-1.5 font-bold">
                    <span class="material-symbols-rounded text-sm">task_alt</span>
                    <span class="text-white">{{ msg.actionData.title }}</span>
                  </div>
                  <span class="text-[10px] px-2 py-0.5 rounded bg-indigo-500/20 text-indigo-300">
                    Ưu tiên: {{ msg.actionData.priority }}
                  </span>
                </div>
              </div>

              <div class="text-[9px] text-[#8b9198] mt-1 text-right">
                {{ msg.time }}
              </div>
            </div>
          </div>

          <!-- Typing Indicator -->
          <div v-if="isLoading" class="flex items-center gap-2 text-xs text-[#8b9198] bg-[#1c2029] border border-[#262a34] px-4 py-2 rounded-2xl rounded-bl-none w-fit">
            <span class="material-symbols-rounded text-sm animate-spin text-[#38e1a6]">sync</span>
            <span>AI đang phân tích & tự động cập nhật Database...</span>
          </div>
        </div>

        <!-- Input Bar -->
        <div class="p-3 bg-[#1c2029] border-t border-[#262a34]">
          <form @submit.prevent="sendMessage" class="flex items-center gap-2">
            <input
              ref="inputField"
              v-model="inputText"
              type="text"
              :disabled="isLoading"
              placeholder="VD: Đã ăn sáng hết 70k, 9h sáng mai họp..."
              class="flex-1 bg-[#171c24] border border-[#262a34] rounded-2xl px-4 py-2.5 text-xs text-white placeholder-slate-500 focus:outline-none focus:border-[#38e1a6] transition-colors"
            />
            <button
              type="submit"
              :disabled="!inputText.trim() || isLoading"
              class="w-10 h-10 rounded-2xl bg-[#005237] hover:bg-[#10b981] disabled:opacity-40 text-[#5dfec1] hover:text-white flex items-center justify-center transition-all active:scale-95 shrink-0"
              title="Gửi tin nhắn"
            >
              <span class="material-symbols-rounded text-xl">send</span>
            </button>
          </form>
        </div>
      </div>
    </transition>

    <!-- NÚT TRÒN TOGGLE CHAT (FLOATING ACTION BUTTON) -->
    <button
      @click="toggleChat"
      class="group relative flex items-center gap-2.5 px-4 py-3 rounded-full bg-gradient-to-r from-[#005237] to-[#10b981] text-white shadow-xl shadow-emerald-950/60 hover:shadow-emerald-900/80 hover:scale-105 active:scale-95 transition-all duration-300 md-elevation-2"
      title="Mở Trợ lý AI"
    >
      <div class="relative">
        <span class="material-symbols-rounded text-2xl transition-transform group-hover:rotate-12">
          {{ isOpen ? 'expand_more' : 'smart_toy' }}
        </span>
        <span
          v-if="!isOpen"
          class="absolute -top-1 -right-1 w-2.5 h-2.5 bg-[#38e1a6] rounded-full animate-ping"
        ></span>
      </div>
      <span class="font-bold text-xs tracking-wide">Trợ lý AI</span>
    </button>
  </div>
</template>

<script setup>
import { ref, nextTick, onMounted } from 'vue';
import { apiFetch } from '../services/api';

const emit = defineEmits(['data-updated']);

const isOpen = ref(false);
const inputText = ref('');
const isLoading = ref(false);
const chatContainer = ref(null);
const inputField = ref(null);

const quickChips = [
  'Đã ăn sáng hết 70k',
  'Đổ xăng 50k',
  'Mua cafe hết 35k',
  '9h sáng mai họp team',
  'Nhớ nộp báo cáo tuần'
];

const messages = ref([
  {
    role: 'assistant',
    text: 'Xin chào! Tôi là Trợ lý AI Second Brain. Bạn chỉ cần nhắn tự nhiên như "Đã ăn sáng hết 70k" hoặc "Chiều mai 14h gặp đối tác", tôi sẽ tự động trừ ví và lên lịch trình ngay lập tức!',
    time: formatTimeNow(),
  }
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
    year: 'numeric'
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

const sendQuickPrompt = (promptText) => {
  inputText.value = promptText;
  sendMessage();
};

const clearMessages = () => {
  messages.value = [
    {
      role: 'assistant',
      text: 'Đoạn hội thoại đã được làm mới. Tôi sẵn sàng hỗ trợ bạn cập nhật ví và lịch trình!',
      time: formatTimeNow(),
    }
  ];
};

const sendMessage = async () => {
  const text = inputText.value.trim();
  if (!text || isLoading.value) return;

  // Add user message
  messages.value.push({
    role: 'user',
    text: text,
    time: formatTimeNow()
  });

  inputText.value = '';
  isLoading.value = true;
  scrollToBottom();

  try {
    const res = await apiFetch('/aiassistant/chat', {
      method: 'POST',
      body: JSON.stringify({ message: text })
    });

    if (!res.ok) {
      throw new Error(`Lỗi kết nối (${res.status})`);
    }

    const result = await res.json();

    messages.value.push({
      role: 'assistant',
      text: result.reply || 'Đã thực hiện xong yêu cầu của bạn.',
      actionExecuted: result.actionExecuted,
      actionData: result.data,
      time: formatTimeNow()
    });

    // Bắn sự kiện ra ngoài để App.vue và các component (Ví, Lịch, Task) tự động refresh dữ liệu
    if (result.actionExecuted && result.actionExecuted !== 'none') {
      emit('data-updated', {
        action: result.actionExecuted,
        data: result.data
      });
      // Phát event toàn cục qua window để FinanceDashboard hoặc Calendar bắt ngay nếu không qua props
      window.dispatchEvent(new CustomEvent('secondbrain-data-updated', {
        detail: { action: result.actionExecuted, data: result.data }
      }));
    }
  } catch (err) {
    console.error(err);
    messages.value.push({
      role: 'assistant',
      text: 'Rất tiếc, đã có lỗi xảy ra trong quá trình xử lý. Bạn vui lòng thử lại nhé!',
      time: formatTimeNow()
    });
  } finally {
    isLoading.value = false;
    scrollToBottom();
  }
};
</script>

<style scoped>
.no-scrollbar::-webkit-scrollbar {
  display: none;
}
.no-scrollbar {
  -ms-overflow-style: none;
  scrollbar-width: none;
}
</style>
