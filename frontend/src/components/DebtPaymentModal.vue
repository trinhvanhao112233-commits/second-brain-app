<template>
  <div class="fixed inset-0 z-50 bg-slate-900/30 backdrop-blur-sm flex items-center justify-center p-4" @click.self="$emit('close')"><section role="dialog" aria-modal="true" aria-labelledby="payment-title" class="surface w-full max-w-md p-6 space-y-5"><div class="flex justify-between items-center"><h2 id="payment-title" class="section-title">Ghi nhận trả nợ từ ví</h2><button aria-label="Đóng" @click="$emit('close')">×</button></div><p class="text-sm text-slate-500">Ghi sổ khoản trả nợ và trừ số dư ví tương ứng.</p><form class="space-y-4" @submit.prevent="save"><label class="block text-sm">Khoản nợ<select v-model="debtId" required class="mt-2 w-full border rounded-lg p-3"><option v-for="debt in unpaid" :key="debt.id" :value="debt.id">{{ debt.title }}</option></select></label><label class="block text-sm">Ví thanh toán<select v-model="walletId" required class="mt-2 w-full border rounded-lg p-3"><option v-for="wallet in wallets" :key="wallet.id" :value="wallet.id">{{ wallet.name }} · {{ money(wallet.balance) }}</option></select></label><label class="block text-sm">Số tiền (₫)<input v-model.number="amount" type="number" min="1" :max="remaining" required class="mt-2 w-full border rounded-lg p-3"></label><p class="text-xs text-slate-500">Còn phải trả: {{ money(remaining) }}</p><p v-if="error" role="alert" class="text-sm text-rose-600">{{ error }}</p><button class="primary-button w-full" :disabled="busy || !wallets.length || !unpaid.length">{{ busy?'Đang lưu…':'Ghi nhận thanh toán' }}</button></form></section></div>
</template>
<script setup>
import {ref,computed,watch,onMounted} from 'vue';
import {apiFetch} from '../services/api';
const props=defineProps({debts:Array});const emit=defineEmits(['close','saved']);
const unpaid=computed(()=>props.debts.filter(x=>x.recordType==='DEBT'&&!x.isCompleted));
const debtId=ref(unpaid.value[0]?.id||''),walletId=ref(''),wallets=ref([]),amount=ref(0),busy=ref(false),error=ref('');
const remaining=computed(()=>{const d=unpaid.value.find(x=>x.id===debtId.value);return d?Math.max(0,Number(d.totalAmount)-Number(d.paidAmount||0)):0;});
watch(remaining,value=>{amount.value=value;},{immediate:true});
const money=n=>new Intl.NumberFormat('vi-VN',{style:'currency',currency:'VND',maximumFractionDigits:0}).format(n||0);
onMounted(async()=>{try{const res=await apiFetch('/wallets');if(!res.ok)throw new Error('Không tải được ví.');wallets.value=await res.json();walletId.value=wallets.value[0]?.id||'';}catch(e){error.value=e.message;}});
async function save(){if(busy.value)return;busy.value=true;error.value='';try{const res=await apiFetch(`/debts/${debtId.value}/payments`,{method:'POST',body:JSON.stringify({id:crypto.randomUUID(),walletId:walletId.value,amount:amount.value})});if(!res.ok)throw new Error('Chưa ghi nhận được thanh toán.');emit('saved');emit('close');}catch(e){error.value=e.message;}finally{busy.value=false;}}
</script>
