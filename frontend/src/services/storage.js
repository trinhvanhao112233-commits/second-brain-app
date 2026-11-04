export const accountStorage = {
  key(key) {
    let user;
    try { user = JSON.parse(localStorage.getItem('second_brain_user') || 'null'); } catch { user = null; }
    return `personalos:${user?.id || 'guest'}:${key}`;
  },
  getItem(key) { return localStorage.getItem(this.key(key)); },
  setItem(key, value) { localStorage.setItem(this.key(key), value); },
  removeItem(key) { localStorage.removeItem(this.key(key)); },
};

export function migrateLegacyStorage(user) {
  const previous = JSON.parse(localStorage.getItem('second_brain_user') || 'null');
  if (previous?.id !== user.id) return;
  for (const key of Object.keys(localStorage)) {
    if (key.startsWith('second_brain_') && key !== 'second_brain_user') {
      const target = `personalos:${user.id}:${key}`;
      if (localStorage.getItem(target) === null) localStorage.setItem(target, localStorage.getItem(key));
      localStorage.removeItem(key);
    }
  }
}
