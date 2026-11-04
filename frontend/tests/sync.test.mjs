import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { ref, reactive } from 'vue';

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
  globalThis.__testVue = {ref};
  const cloudSource=(await readFile(new URL('../src/services/cloudSync.js',import.meta.url),'utf8'))
    .replace("import { ref } from 'vue';",'const {ref} = globalThis.__testVue;')
    .replace("'./api'",JSON.stringify(url(source)))
    .replace("'./storage'",JSON.stringify(storageUrl));
  const cloud=await import(url(cloudSource));
  const pushSource=(await readFile(new URL('../src/services/pushNotification.js',import.meta.url),'utf8')).replace("'./api'",JSON.stringify(url(source)));
  const pushUrl=url(pushSource);
  const sessionSource=(await readFile(new URL('../src/services/session.js',import.meta.url),'utf8')).replace("'./api'",JSON.stringify(url(source))).replace("'./pushNotification'",JSON.stringify(pushUrl));
  const session=await import(url(sessionSource));
  return {...api,...storage,...cloud,...session};
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
