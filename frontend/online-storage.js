// Runtime storage adapters. Passwords and bank keys never enter browser storage.
window.lifeosOnline = { token: localStorage.getItem('LIFEOS_SESSION_V1'), revision: 0, bank: {}, pending: null, saving: false, polling: false, conflict: false, generation: 0 };
function getCurrentUser() { return null; }
function loadUsersList() { return []; }
function saveUsersList() { throw new Error('Tài khoản được quản lý trên máy chủ.'); }
function setCurrentUser() {}
function loadAppStateForUser() { return window.lifeosOnline.snapshot; }
function saveAppStateForUser(username, value) { window.queueOnlineSave(value); }
