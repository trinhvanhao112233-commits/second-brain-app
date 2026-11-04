// Service Worker for Second Brain Web Push Notifications
self.addEventListener('push', function(event) {
  let data = {
    title: '🔔 Second Brain Thông Báo',
    body: 'Bạn có một sự kiện lịch trình sắp diễn ra!',
    icon: '/icon.svg',
    data: { url: '/?tab=calendar' }
  };

  try {
    if (event.data) {
      data = event.data.json();
    }
  } catch (e) {
    if (event.data) {
      data.body = event.data.text();
    }
  }

  const options = {
    body: data.body,
    icon: data.icon || '/icon.svg',
    badge: '/icon.svg',
    vibrate: [200, 100, 200, 100, 200],
    data: data.data || { url: '/?tab=calendar' },
    actions: [
      { action: 'open', title: 'Xem lịch' }
    ]
  };

  event.waitUntil(
    self.registration.showNotification(data.title, options)
  );
});

self.addEventListener('message', function(event) {
  if (event.data && event.data.type === 'LOCAL_NOTIFICATION') {
    self.registration.showNotification(event.data.title || '🔔 Second Brain', {
      body: event.data.body || 'Thông báo thử nghiệm',
      icon: '/icon.svg',
      badge: '/icon.svg',
      vibrate: [200, 100, 200]
    });
  }
});

self.addEventListener('notificationclick', function(event) {
  event.notification.close();
  const urlToOpen = (event.notification.data && event.notification.data.url) || '/?tab=calendar';

  event.waitUntil(
    clients.matchAll({ type: 'window', includeUncontrolled: true }).then(function(clientList) {
      for (let i = 0; i < clientList.length; i++) {
        let client = clientList[i];
        if (new URL(client.url).origin === self.location.origin && 'focus' in client) {
          return client.navigate(urlToOpen).then(() => client.focus());
        }
      }
      if (clients.openWindow) {
        return clients.openWindow(urlToOpen);
      }
    })
  );
});
