// API Client helper that automatically attaches X-User-Id header if logged in
const API_BASE = import.meta.env.VITE_API_URL || 
  (typeof window !== 'undefined' && window.location.hostname !== 'localhost'
    ? 'https://second-brain-app-21j2.onrender.com/api'
    : 'http://localhost:5000/api');

export const getStoredUser = () => {
  try {
    const raw = localStorage.getItem('second_brain_user');
    return raw ? JSON.parse(raw) : null;
  } catch (e) {
    console.error('Error parsing stored user:', e);
    return null;
  }
};

export const setStoredUser = (user) => {
  if (user) {
    localStorage.setItem('second_brain_user', JSON.stringify(user));
  } else {
    localStorage.removeItem('second_brain_user');
  }
};

export const apiFetch = async (endpoint, options = {}) => {
  const user = getStoredUser();
  const headers = {
    'Content-Type': 'application/json',
    ...(options.headers || {})
  };

  if (user && user.id) {
    headers['X-User-Id'] = user.id;
  }

  const url = endpoint.startsWith('http') ? endpoint : `${API_BASE}${endpoint.startsWith('/') ? '' : '/'}${endpoint}`;

  return fetch(url, {
    ...options,
    headers
  });
};
