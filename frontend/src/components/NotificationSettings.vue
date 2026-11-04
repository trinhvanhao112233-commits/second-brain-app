<template>
  <div class="space-y-6">
    <div><p class="eyebrow">CÁ NHÂN HÓA</p><h1 class="page-title">Thông báo & dữ liệu</h1><p class="page-description">Chọn những điều bạn muốn được nhắc, trên các thiết bị của mình.</p></div>
    <div class="settings-grid">
      <section class="surface p-6 space-y-5">
        <div class="flex items-center justify-between"><h2 class="section-title">Thiết bị này</h2><span class="status-pill">{{ enabled ? 'Đã bật' : 'Chưa bật' }}</span></div>
        <p class="text-sm text-slate-500">Thông báo nợ, công việc và lịch hẹn sẽ gửi đến các thiết bị đã đăng ký cùng tài khoản.</p>
        <div class="flex flex-wrap gap-3"><button class="primary-button" :disabled="busy" @click="toggle">{{ enabled ? 'Tắt trên thiết bị này' : 'Bật thông báo' }}</button><button class="secondary-button" :disabled="busy || !enabled" @click="test">Gửi thử</button></div>
        <p class="text-sm text-slate-500">{{ devices.length }} thiết bị đã đăng ký.</p>
        <p class="text-sm text-slate-500">iPhone/iPad: thêm web vào Màn hình chính rồi mở từ biểu tượng đó để bật thông báo. Thiết bị cần hỗ trợ Web Push.</p>
        <p v-if="message" role="status" class="text-sm text-indigo-700">{{ message }}</p>
      </section>
      <section class="surface p-6 space-y-5">
        <h2 class="section-title">Nội dung nhắc nhở</h2>
        <label v-for="item in types" :key="item.key" class="flex gap-3 items-start cursor-pointer"><input v-model="preferences[item.key]" type="checkbox" class="mt-1 accent-indigo-600"><span><span class="block text-sm font-semibold">{{ item.label }}</span><span class="text-sm text-slate-500">{{ item.description }}</span></span></label>
        <div class="border-t border-slate-100 pt-4"><label class="flex items-center gap-3 text-sm font-semibold"><input v-model="preferences.quietEnabled" type="checkbox" class="accent-indigo-600">Giờ yên lặng</label><div v-if="preferences.quietEnabled" class="flex items-center gap-3 mt-3 text-sm"><select v-model.number="preferences.quietStart" aria-label="Bắt đầu giờ yên lặng"><option v-for="h in 24" :key="h" :value="h-1">{{ h-1 }}:00</option></select><span>đến</span><select v-model.number="preferences.quietEnd" aria-label="Kết thúc giờ yên lặng"><option v-for="h in 24" :key="h" :value="h-1">{{ h-1 }}:00</option></select></div><p class="text-xs text-slate-500 mt-2">Theo giờ Việt Nam (UTC+7). Nhắc lịch trong giờ yên lặng có thể bị bỏ qua.</p></div>
        <button class="primary-button" :disabled="busy || !loaded" @click="save">Lưu tùy chọn</button>
      </section>
      <section class="surface p-6 space-y-4">
        <h2 class="section-title">Sao lưu cá nhân</h2><p class="text-sm text-slate-500">Tải dữ liệu đã đồng bộ về máy. Tệp có thể chứa thông tin tài chính riêng tư.</p><button class="secondary-button" :disabled="busy" @click="backup">Tải bản sao lưu JSON</button>
      </section>
      <section class="surface p-6 space-y-4"><h2 class="section-title">Thao tác chờ gửi</h2><p class="text-sm text-slate-500">{{ count }} thao tác đang chờ. Dữ liệu chỉ được xác nhận khi máy chủ lưu thành công.</p><button class="secondary-button" :disabled="busy" @click="retry">Thử đồng bộ lại</button><button v-if="count" class="text-link block" :disabled="busy" @click="discard">Bỏ thao tác đầu hàng đợi</button></section>
    </div>
  </div>
</template>
<script setup>
import { ref, onMounted } from 'vue';
import { apiFetch, flushPending, pendingCount, discardFirstPending } from '../services/api';
import { registerPushNotification, unregisterPushNotification } from '../services/pushNotification';
const preferences = ref({ events:true, tasks:true, debts:true, quietEnabled:false, quietStart:22, quietEnd:7 });
const enabled=ref(false), devices=ref([]), busy=ref(false), message=ref(''), loaded=ref(false), count=ref(pendingCount());
const types=[{key:'events',label:'Lịch sắp bắt đầu',description:'Nhắc trong vòng 1 giờ trước lịch hẹn.'},{key:'tasks',label:'Công việc & deadline',description:'Nhắc mỗi ngày khi sắp đến hạn hoặc quá hạn.'},{key:'debts',label:'Khoản nợ cần trả',description:'Nhắc trước 3 ngày và khi quá hạn.'}];
async function run(fn){busy.value=true;message.value='';try{await fn();}catch(e){message.value=e.message||'Không thực hiện được. Vui lòng thử lại.';}finally{busy.value=false;count.value=pendingCount();}}
async function load(){const res=await apiFetch('/notification/preferences');if(!res.ok)throw new Error('Không tải được tùy chọn.');preferences.value=await res.json();loaded.value=true;const d=await apiFetch('/notification/devices');if(d.ok)devices.value=await d.json();const reg=await navigator.serviceWorker?.getRegistration();enabled.value=!!(await reg?.pushManager?.getSubscription());}
const toggle=()=>run(async()=>{if(enabled.value)await unregisterPushNotification();else {const result=await registerPushNotification();if(!result.success)throw new Error(result.reason==='unsupported'?'Thiết bị chưa hỗ trợ. Trên iPhone hãy mở web từ Màn hình chính.':result.reason==='denied'?'Quyền thông báo chưa được cấp. Kiểm tra cài đặt trình duyệt.':'Chưa kết nối được máy chủ thông báo.');}await load();message.value=enabled.value?'Đã bật thông báo trên thiết bị này.':'Đã tắt thông báo trên thiết bị này.';});
const test=()=>run(async()=>{const res=await apiFetch('/notification/test-push',{method:'POST'});const data=await res.json();if(!res.ok)throw new Error(data.message);message.value=data.message;});
const save=()=>run(async()=>{const res=await apiFetch('/notification/preferences',{method:'PUT',body:JSON.stringify(preferences.value)});if(!res.ok)throw new Error('Không lưu được tùy chọn.');message.value='Đã lưu tùy chọn.';});
const discard=()=>run(async()=>{if(!confirm('Bỏ thao tác đầu đang chờ? Bản sao thao tác vẫn được giữ trên thiết bị. Các thao tác sau có thể phụ thuộc vào nó.'))return;discardFirstPending();message.value='Đã bỏ thao tác. Hãy thử đồng bộ lại để tải trạng thái từ máy chủ.';});
const retry=()=>run(async()=>{await flushPending();message.value='Đã gửi xong các thao tác đang chờ.';window.dispatchEvent(new Event('secondbrain-data-updated'));});
const backup=()=>run(async()=>{await flushPending();const data={version:1,exportedAt:new Date().toISOString()};for(const name of ['wallets','debts','events','tasks','notes']){const res=await apiFetch('/'+name);if(!res.ok)throw new Error('Chưa tải đủ dữ liệu để sao lưu.');data[name]=(await res.json()).filter(x=>x.title!=='__SB_CLOUD_VAULT_V2__');}data.transactions=[];for(const wallet of data.wallets){const res=await apiFetch(`/wallets/${wallet.id}/transactions`);if(!res.ok)throw new Error('Không tải được giao dịch.');data.transactions.push(...await res.json());}const url=URL.createObjectURL(new Blob([JSON.stringify(data,null,2)],{type:'application/json'}));const link=document.createElement('a');link.href=url;link.download=`personal-os-${new Date().toISOString().slice(0,10)}.json`;link.click();setTimeout(()=>URL.revokeObjectURL(url),1000);message.value='Đã tải bản sao lưu.';});
onMounted(()=>run(load));
</script>
