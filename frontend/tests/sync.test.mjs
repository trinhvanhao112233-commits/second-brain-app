import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { ref, reactive, computed, onMounted, onUnmounted, nextTick, createRenderer } from 'vue';
import { parse, compileScript } from '@vue/compiler-sfc';

class MemoryStorage {
  getItem(key) { return Object.hasOwn(this,key) ? this[key] : null; }
  setItem(key,value) { this[key]=String(value); }
  removeItem(key) { delete this[key]; }
}
async function setup(fetcher) {
  globalThis.localStorage=new MemoryStorage();
  globalThis.window=new EventTarget();
  globalThis.fetch=fetcher;
  const nonce=crypto.randomUUID();
  const url=s=>'data:text/javascript;base64,'+Buffer.from(s).toString('base64')+'#'+nonce;
  const storageUrl=url(await readFile(new URL('../src/services/storage.js',import.meta.url),'utf8'));
  const storage=await import(storageUrl);
  const source=(await readFile(new URL('../src/services/api.js',import.meta.url),'utf8')).replace("'./storage'",JSON.stringify(storageUrl)).replace('import.meta.env.VITE_API_URL',"'https://test.invalid/api'");
  const api=await import(url(source));
  api.setStoredUser({id:'alice',token:'signed-session'});
  globalThis.__testVue = {ref, computed, onMounted, onUnmounted};
  const cloudSource=(await readFile(new URL('../src/services/cloudSync.js',import.meta.url),'utf8'))
    .replace("import { ref } from 'vue';",'const {ref} = globalThis.__testVue;')
    .replace("'./api'",JSON.stringify(url(source)))
    .replace("'./storage'",JSON.stringify(storageUrl));
  const cloud=await import(url(cloudSource));
  const pushSource=(await readFile(new URL('../src/services/pushNotification.js',import.meta.url),'utf8')).replace("'./api'",JSON.stringify(url(source)));
  const pushUrl=url(pushSource);
  const sessionSource=(await readFile(new URL('../src/services/session.js',import.meta.url),'utf8')).replace("'./api'",JSON.stringify(url(source))).replace("'./pushNotification'",JSON.stringify(pushUrl));
  const session=await import(url(sessionSource));
  return {...api,...storage,...cloud,...session, cloudModuleUrl:url(cloudSource)};
}
const json=(body,status=200)=>new Response(JSON.stringify(body),{status,headers:{'Content-Type':'application/json'}});

test('isolates local data and outbox by account',async()=>{
  const api=await setup(async()=>json([]));
  api.accountStorage.setItem('wallets','private');
  api.accountStorage.setItem('outbox','[{"id":"pending"}]');
  api.setStoredUser({id:'bob',token:'other-session'});
  assert.equal(api.accountStorage.getItem('wallets'),null);
  assert.equal(api.pendingCount(),0);
  api.setStoredUser({id:'alice',token:'signed-session'});
  assert.equal(api.accountStorage.getItem('wallets'),'private');
  assert.equal(api.pendingCount(),1);
});
test('persists failed write and retries with the same resource ID',async()=>{
  const sent=[];let online=false;
  const api=await setup(async(url,options)=>{sent.push({url,options});if(!online)throw new TypeError('offline');return json(JSON.parse(options.body));});
  await assert.rejects(api.apiFetch('/tasks',{method:'POST',body:JSON.stringify({id:'stable-id',title:'Task'})}));
  assert.equal(api.pendingCount(),1);
  online=true;await api.flushPending();
  assert.equal(api.pendingCount(),0);
  assert.equal(JSON.parse(sent[1].options.body).id,'stable-id');
  assert.equal(sent[1].options.headers.Authorization,'Bearer signed-session');
  assert.equal(sent[1].options.headers['X-User-Id'],undefined);
});
test('does not overwrite offline edits with an older GET',async()=>{
  let gets=0;
  const api=await setup(async(url,options)=>{if(options.method==='PUT')throw new TypeError('offline');gets++;return json([]);});
  await assert.rejects(api.apiFetch('/tasks/stable-id',{method:'PUT',body:'{"title":"new"}'}));
  await assert.rejects(api.apiFetch('/tasks'));
  assert.equal(gets,0);
  assert.equal(api.pendingCount(),1);
});
test('serializes concurrent changes and returns each server response',async()=>{
  const sent=[];
  const api=await setup(async(url,options)=>{sent.push(url);await new Promise(resolve=>setTimeout(resolve,5));return json({...JSON.parse(options.body),saved:true});});
  const responses=await Promise.all([api.apiFetch('/wallets',{method:'POST',body:'{"id":"wallet"}'}),api.apiFetch('/wallets/wallet/transactions',{method:'POST',body:'{"id":"tx","amount":100}'})]);
  assert.deepEqual(sent,['https://test.invalid/api/wallets','https://test.invalid/api/wallets/wallet/transactions']);
  assert.equal((await responses[0].json()).id,'wallet');
  assert.equal((await responses[1].json()).id,'tx');
  assert.equal(api.pendingCount(),0);
});
test('rejects a response that arrives after account switch',async()=>{
  let release;
  const api=await setup(()=>new Promise(resolve=>{release=resolve;}));
  const pending=api.apiFetch('/notes');
  api.setStoredUser({id:'bob',token:'other-session'});
  release(json([{title:'Alice private note'}]));
  await assert.rejects(pending,/Tài khoản đã thay đổi/);
});
test('a server rejection stays pending and never reports sync success',async()=>{
  const api=await setup(()=>json({message:'Invalid wallet'},400));
  const states=[];window.addEventListener('api-sync-state',e=>states.push(e.detail));
  await assert.rejects(api.apiFetch('/wallets',{method:'POST',body:'{"id":"bad"}'}));
  assert.equal(api.pendingCount(),1);
  assert.equal(states.at(-1),'error');
});
test('saves Vue reactive debt arrays and keeps equal-content records separate',async()=>{
  const sent=[];
  const api=await setup(async(url,options)=>{sent.push(url);return json(JSON.parse(options.body));});
  const records=reactive([{id:'one',title:'Tiền nhà',totalAmount:100,recordType:'DEBT'},{id:'two',title:'Tiền nhà',totalAmount:100,recordType:'DEBT'}]);
  assert.equal(await api.pushDebtsToCloud(records),true);
  assert.equal(sent.length,2);
  assert.equal(api.getLocalDebts().length,2);
});
test('accepts an empty cloud list after records were removed on another device',async()=>{
  const api=await setup(()=>json([]));
  const old=[{id:'old',title:'Removed',totalAmount:100}];
  api.setLocalDebts(old);
  api.accountStorage.setItem('debt_migration_v3','true');
  api.accountStorage.setItem('debts_server_snapshot_v3',JSON.stringify(old));
  assert.deepEqual(await api.syncDebtsBidirectional(),[]);
  assert.deepEqual(api.getLocalDebts(),[]);
});
test('migrates vault debts once and retains legacy notes as a backup',async()=>{
  const writes=[];
  const api=await setup(async(url,options)=>{
    if(options.method==='PUT'){writes.push(url);return json(JSON.parse(options.body));}
    if(url.endsWith('/notes'))return json([{title:'__SB_CLOUD_VAULT_V2__',content:JSON.stringify({debts:[{id:'legacy',title:'Old debt',totalAmount:100}]})}]);
    return json([]);
  });
  const debts=await api.syncDebtsBidirectional();
  assert.equal(debts[0].id,'legacy');
  assert.deepEqual(writes,['https://test.invalid/api/debts/legacy']);
  assert.equal(api.accountStorage.getItem('debt_migration_v3'),'true');
});
test('logout clears credentials and keeps pending data when browser unsubscribe fails',async()=>{
  let requests=0;
  const api=await setup(()=>{requests++;throw new Error('offline');});
  api.accountStorage.setItem('outbox','[{"id":"pending"}]');
  Object.defineProperty(globalThis,'navigator',{configurable:true,value:{serviceWorker:{getRegistration:async()=>({pushManager:{getSubscription:async()=>({unsubscribe:async()=>{throw new Error('browser failed');}})}})}}});
  const cleanup=api.signOut();
  assert.equal(api.getStoredUser(),null);
  assert.equal(await cleanup,false);
  assert.equal(requests,0);
  api.setStoredUser({id:'alice',token:'new-session'});
  assert.equal(api.pendingCount(),1);
});
test('logout cancels browser delivery without waiting for backend',async()=>{
  let unsubscribed=false;
  const api=await setup(()=>{throw new Error('Must not contact server on logout');});
  Object.defineProperty(globalThis,'navigator',{configurable:true,value:{serviceWorker:{getRegistration:async()=>({pushManager:{getSubscription:async()=>({unsubscribe:async()=>{unsubscribed=true;}})}})}}});
  assert.equal(await api.signOut(),true);
  assert.equal(api.getStoredUser(),null);
  assert.equal(unsubscribed,true);
});
test('missing notification configuration does not mark data sync as failed',async()=>{
  const api=await setup(()=>json({message:'Not configured'},503));
  const states=[];window.addEventListener('api-sync-state',e=>states.push(e.detail));
  await api.apiFetch('/notification/vapid-public-key');
  assert.equal(states.includes('error'),false);
});

async function mountDebtPlanner(api) {
  const source=await readFile(new URL('../src/components/DebtPlannerModule.vue',import.meta.url),'utf8');
  const {descriptor}=parse(source);
  const script=compileScript(descriptor,{id:'debt-planner-regression'}).content
    .replace(/import DebtPaymentModal from '[^']+';/,'const DebtPaymentModal = {};')
    .replace(/import \{([^}]+)\} from 'vue';/,'const {$1} = globalThis.__testVue;')
    .replace("'../services/cloudSync'",JSON.stringify(api.cloudModuleUrl));
  const {default:component}=await import('data:text/javascript;base64,'+Buffer.from(script).toString('base64')+'#'+crypto.randomUUID());
  component.render=()=>null;
  const renderer=createRenderer({
    createElement:()=>({}),createText:()=>({}),createComment:()=>({}),
    insert(){},remove(){},setText(){},setElementText(){},patchProp(){},
    parentNode:()=>null,nextSibling:()=>null,
  });
  const app=renderer.createApp(component);
  const vm=app.mount({});
  await api.syncDebtsBidirectional();
  await nextTick();
  return {state:vm.$.setupState,unmount:()=>app.unmount()};
}

test('Paid button updates canonical debt from a computed display row, persists and survives reload',async()=>{
  const dueDate=new Date().toISOString().slice(0,7)+'-10T00:00:00';
  let server=[{id:'debt-button',title:'Tiền nhà',recordType:'DEBT',totalAmount:1000,paidAmount:200,isCompleted:false,dueDate,periodLabel:'1-10',priorityLevel:1}];
  const writes=[];
  const api=await setup(async(url,options)=>{
    if(options.method==='PUT'){const row=JSON.parse(options.body);writes.push(row);server=[row];return json(row);}
    return json(url.endsWith('/debts')?server:[]);
  });
  api.accountStorage.setItem('debt_migration_v3','true');
  api.setLocalDebts(server);
  api.accountStorage.setItem('debts_server_snapshot_v3',JSON.stringify(server));
  const planner=await mountDebtPlanner(api);
  try {
    const displayRow=planner.state.prioritizedDebts[0];
    assert.notEqual(displayRow,planner.state.records[0]);
    await planner.state.toggleComplete(displayRow);
    assert.equal(planner.state.records[0].isCompleted,true);
    assert.equal(planner.state.records[0].paidAmount,1000);
    assert.equal(writes.at(-1).isCompleted,true);
    assert.equal(planner.state.prioritizedDebts.length,0);
    assert.equal((await api.syncDebtsBidirectional())[0].isCompleted,true);
    planner.state.debtListFilter='ALL';
    await planner.state.toggleComplete(planner.state.prioritizedDebts[0]);
    assert.equal(planner.state.records[0].isCompleted,false);
    assert.equal(writes.at(-1).isCompleted,false);
  } finally { planner.unmount(); }
});

test('editing a debt below 50,000 VND keeps its amount instead of multiplying by 1,000',async()=>{
  let server=[{id:'small-debt',title:'Gửi xe',recordType:'DEBT',totalAmount:25000,paidAmount:0,isCompleted:false,dueDate:new Date().toISOString().slice(0,7)+'-10T00:00:00',periodLabel:'1-10',priorityLevel:2}];
  const api=await setup(async(url,options)=>{
    if(options.method==='PUT'){server=[JSON.parse(options.body)];return json(server[0]);}
    return json(url.endsWith('/debts')?server:[]);
  });
  api.accountStorage.setItem('debt_migration_v3','true');api.setLocalDebts(server);
  api.accountStorage.setItem('debts_server_snapshot_v3',JSON.stringify(server));
  const planner=await mountDebtPlanner(api);
  try {
    planner.state.openEditModal(planner.state.prioritizedDebts[0]);
    assert.equal(planner.state.parsedAmount,25000);
    await planner.state.handleSave();
    assert.equal(server[0].totalAmount,25000);
  } finally { planner.unmount(); }
});

test('Paid button ignores repeated clicks while its write is pending',async()=>{
  let server=[{id:'double-click',title:'Nợ',recordType:'DEBT',totalAmount:1000,paidAmount:0,isCompleted:false,dueDate:new Date().toISOString().slice(0,7)+'-10T00:00:00',periodLabel:'1-10',priorityLevel:1}];
  let writes=0;
  const api=await setup(async(url,options)=>{
    if(options.method==='PUT'){writes++;await new Promise(resolve=>setTimeout(resolve,10));server=[JSON.parse(options.body)];return json(server[0]);}
    return json(url.endsWith('/debts')?server:[]);
  });
  api.accountStorage.setItem('debt_migration_v3','true');api.setLocalDebts(server);
  api.accountStorage.setItem('debts_server_snapshot_v3',JSON.stringify(server));
  const planner=await mountDebtPlanner(api);
  try {
    const row=planner.state.prioritizedDebts[0];
    await Promise.all([planner.state.toggleComplete(row),planner.state.toggleComplete(row)]);
    assert.equal(server[0].isCompleted,true);
    assert.equal(planner.state.records[0].isCompleted,true);
    assert.equal(writes,1);
  } finally { planner.unmount(); }
});

for (const migrated of [true,false]) test(`in-flight debt refresh cannot undo Paid, including legacy migration=${!migrated}`,async()=>{
  const old={id:'in-flight',title:'Nợ',recordType:'DEBT',totalAmount:1000,paidAmount:0,isCompleted:false,dueDate:'2026-11-10T00:00:00',periodLabel:'1-10',priorityLevel:1};
  let server=[{...old}],release,started;
  const fetched=new Promise(resolve=>{started=resolve;});
  const api=await setup(async(url,options)=>{
    if(options.method==='PUT'){server=[JSON.parse(options.body)];return json(server[0]);}
    if(url.endsWith('/debts')){started();return new Promise(resolve=>{release=()=>resolve(json([{...old}]));});}
    return json([]);
  });
  if(migrated)api.accountStorage.setItem('debt_migration_v3','true');
  api.setLocalDebts([{...old}]);api.accountStorage.setItem('debts_server_snapshot_v3',JSON.stringify([old]));
  const refresh=api.syncDebtsBidirectional();await fetched;
  const payment=api.pushDebtsToCloud([{...old,isCompleted:true,paidAmount:1000}]);
  release();
  await refresh;assert.equal(await payment,true);
  assert.equal(api.getLocalDebts()[0].isCompleted,true);
  assert.equal(server[0].isCompleted,true);
});

test('Paid remains visible as a pending local edit when offline and is sent on retry',async()=>{
  const row={id:'offline-paid',title:'Nợ',recordType:'DEBT',totalAmount:1000,paidAmount:0,isCompleted:false,dueDate:new Date().toISOString().slice(0,7)+'-10T00:00:00',periodLabel:'1-10',priorityLevel:1};
  let server=[row],online=true;
  const api=await setup(async(url,options)=>{
    if(!online)throw new TypeError('offline');
    if(options.method==='PUT'){server=[JSON.parse(options.body)];return json(server[0]);}
    return json(url.endsWith('/debts')?server:[]);
  });
  api.accountStorage.setItem('debt_migration_v3','true');api.setLocalDebts(server);
  api.accountStorage.setItem('debts_server_snapshot_v3',JSON.stringify(server));
  const planner=await mountDebtPlanner(api);
  try {
    online=false;await planner.state.toggleComplete(planner.state.prioritizedDebts[0]);
    assert.equal(planner.state.records[0].isCompleted,true);
    assert.equal(api.cloudSyncState.value,'error');assert.equal(api.pendingCount(),1);
    online=true;await api.syncDebtsBidirectional();
    assert.equal(server[0].isCompleted,true);assert.equal(api.pendingCount(),0);
  } finally { planner.unmount(); }
});
