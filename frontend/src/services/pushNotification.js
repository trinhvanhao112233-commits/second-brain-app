import { apiFetch } from './api';

function urlBase64ToUint8Array(base64String) {
  const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
  const base64 = (base64String + padding)
    .replace(/\-/g, '+')
    .replace(/_/g, '/');

  const rawData = window.atob(base64);
  const outputArray = new Uint8Array(rawData.length);

  for (let i = 0; i < rawData.length; ++i) {
    outputArray[i] = rawData.charCodeAt(i);
  }
  return outputArray;
}

export const registerPushNotification = async () => {
  if (!('serviceWorker' in navigator) || !('PushManager' in window) || !('Notification' in window)) {
    console.warn('[WebPush] Trình duyệt không hỗ trợ Web Push Notification.');
    return { success: false, reason: 'unsupported' };
  }

  try {
    // 1. Xin quyền người dùng
    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
      return { success: false, reason: 'denied' };
    }

    // 2. Đăng ký Service Worker
    const registration = await navigator.serviceWorker.register('/sw.js');
    await navigator.serviceWorker.ready;

    // 3. Lấy VAPID Public Key từ backend (có fallback key cứng)
    let publicKey = 'BOOl107oDcMlc2_3Uj5hQ63qFVi2vRw0SEpborF1EZGfPAOtnwizjdckuhGylEoPjT_w4_uI50Qy3PWAS37PliI';
    try {
      const keyRes = await apiFetch('/notification/vapid-public-key');
      if (keyRes.ok) {
        const data = await keyRes.json();
        if (data.publicKey) publicKey = data.publicKey;
      }
    } catch (e) {
      console.warn('[WebPush] Không thể lấy key động từ backend, dùng fallback key:', e);
    }

    // 4. Đăng ký Subscription với Push Server của trình duyệt
    let subscription = await registration.pushManager.getSubscription();
    if (!subscription) {
      subscription = await registration.pushManager.subscribe({
        userVisibleOnly: true,
        applicationServerKey: urlBase64ToUint8Array(publicKey)
      });
    }

    // 5. Gửi Subscription lên backend lưu vào Database
    const rawSub = subscription.toJSON();
    const saveRes = await apiFetch('/notification/subscribe', {
      method: 'POST',
      body: JSON.stringify({
        endpoint: rawSub.endpoint,
        keys: {
          p256dh: rawSub.keys.p256dh,
          auth: rawSub.keys.auth
        }
      })
    });

    if (saveRes.ok) {
      return { success: true };
    } else {
      return { success: false, reason: 'server_error' };
    }
  } catch (err) {
    console.error('[WebPush] Lỗi khi kích hoạt thông báo:', err);
    return { success: false, error: err };
  }
};
