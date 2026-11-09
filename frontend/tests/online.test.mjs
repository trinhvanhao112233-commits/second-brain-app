import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import { createContext, runInContext } from 'node:vm';

const storageSource = await readFile(new URL('../online-storage.js', import.meta.url), 'utf8');
const onlineSource = await readFile(new URL('../online.js', import.meta.url), 'utf8');
const blank = () => ({ theme: 'light', accentColor: '#6366f1', transactions: [], schedules: [], deadlines: [], debts: [], notes: [], bankSeenIds: [] });
function setup() {
  const values = new Map([['LIFEOS_SESSION_V1', 'session-alice']]); const elements = new Map();
  const timers = new Map(); const calls = []; const handlers = {}; let timerId = 0;
  const context = createContext({
    window: { LIFEOS_API_URL: 'https://example.test/api', addEventListener() {} },
    localStorage: { getItem: key => values.get(key) || null, setItem: (key, value) => values.set(key, String(value)), removeItem: key => values.delete(key) },
    document: { hidden: false, addEventListener(name, fn) { handlers[name] = fn; }, getElementById(id) { if (!elements.has(id)) elements.set(id, { value: '', type: 'password', classList: { toggle() {} }, hidden: false, focus() {}, select() {} }); return elements.get(id); } },
    setTimeout(fn) { const id = ++timerId; timers.set(id, fn); return id; }, clearTimeout: id => timers.delete(id), setInterval() {},
    structuredClone, AbortSignal, Response, console, currentUser: { username: 'alice' }, state: blank(), activeNoteId: null,
    updateAuthUI() {}, applyTheme() {}, renderAll() {}, switchTab() {}, showToast() {}, hideAuthError() {}, showAuthError() {},
    createSampleState: blank, confirm: () => true, location: { reload() {} },
    fetch(url, options) { return new Promise(resolve => calls.push({ url, options, resolve })); }
  });
  runInContext(storageSource, context); runInContext(onlineSource, context);
  return { context, values, elements, calls, handlers, timers, takeTimer() { const [id, fn] = timers.entries().next().value; timers.delete(id); return fn; } };
}
test('edits during an in-flight save are queued with the next revision', async () => {
  const s = setup(); runInContext('window.queueOnlineSave(state)', s.context);
  const first = s.takeTimer()();
  runInContext('state.notes.push({id:"new-note"}); window.queueOnlineSave(state)', s.context);
  await s.takeTimer()();
  s.calls[0].resolve(Response.json({ revision: 1 })); await first;
  const second = s.takeTimer()(); const body = JSON.parse(s.calls[1].options.body);
  assert.equal(body.revision, 1); assert.equal(body.state.notes[0].id, 'new-note');
  s.calls[1].resolve(Response.json({ revision: 2 })); await second;
  assert.equal(runInContext('online.pending', s.context), null);
});
test('conflicting changes keep the draft and stop automatic overwrites', async () => {
  const s = setup(); runInContext('window.queueOnlineSave(state)', s.context); const save = s.takeTimer()();
  s.calls[0].resolve(Response.json({ error: 'conflict' }, { status: 409 })); await save;
  assert.equal(runInContext('online.conflict', s.context), true);
  assert.ok(runInContext('online.pending', s.context)); assert.equal(s.elements.get('online-conflict').hidden, false);
  await runInContext('pollOnlineState()', s.context); assert.equal(s.calls.length, 1);
});
test('a poll cannot overwrite edits made while its request was pending', async () => {
  const s = setup(); const poll = runInContext('pollOnlineState()', s.context);
  runInContext('state.notes.push({id:"local-note"}); window.queueOnlineSave(state)', s.context);
  s.calls[0].resolve(Response.json({ state: blank(), revision: 0, bank: {} })); await poll;
  assert.equal(runInContext('state.notes[0].id', s.context), 'local-note');
});
test('bank key is one-time UI data and is never persisted to browser storage', async () => {
  const s = setup(); s.context.testEvent = { preventDefault() {}, submitter: { disabled: false } };
  s.context.document.getElementById('bank-account').value = '1234567890';
  const connect = runInContext('createBankConnection(testEvent)', s.context);
  s.calls[0].resolve(Response.json({ apiKey: 'secret-bank-key', bank: { connectionId: 'bank-1', accountNumber: '1234567890', total: 0 } })); await connect;
  assert.equal(s.elements.get('bank-key').value, 'secret-bank-key');
  assert.ok(![...s.values.values()].some(value => value.includes('secret-bank-key')));
  assert.equal(runInContext('online.pending', s.context), null);
});
test('a bank request cannot disclose the old account key after switching users', async () => {
  const s = setup(); s.context.testEvent = { preventDefault() {}, submitter: { disabled: false } };
  s.context.document.getElementById('bank-account').value = '1234567890';
  const connect = runInContext('createBankConnection(testEvent)', s.context);
  runInContext('online.generation++; online.token="session-bob"; currentUser={username:"bob"}', s.context);
  s.calls[0].resolve(Response.json({ apiKey: 'alice-secret', bank: { connectionId: 'alice-bank' } })); await connect;
  assert.notEqual(s.elements.get('bank-key')?.value, 'alice-secret');
});
test('a delayed session restore cannot log back into the previous account', async () => {
  const s = setup(); const restoring = s.handlers.DOMContentLoaded();
  s.context.bob = { user: { username: 'bob', fullName: 'Bob' }, token: 'bob-session', state: blank(), revision: 0, bank: {} };
  runInContext('acceptOnlineSession(bob)', s.context);
  s.calls[0].resolve(Response.json({ user: { username: 'alice' }, state: blank(), revision: 0, bank: {} })); await restoring;
  assert.equal(runInContext('currentUser.username', s.context), 'bob');
  assert.equal(s.values.get('LIFEOS_SESSION_V1'), 'bob-session');
});
