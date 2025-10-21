// Service worker for ImmichFrame PWA
const CACHE_NAME = 'immichframe-v1';

self.addEventListener('install', (event) => {
    console.log('Service worker installing...');
    self.skipWaiting(); // activate immediately
});

self.addEventListener('activate', (event) => {
    console.log('Service worker activating...');
    event.waitUntil(
        caches.keys().then((cacheNames) => {
            return Promise.all(
                cacheNames.map((cacheName) => {
                    if (cacheName !== CACHE_NAME) {
                        return caches.delete(cacheName);
                    }
                })
            );
        }).then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    // Basic network-first strategy
    event.respondWith(
        fetch(event.request).catch(() => caches.match(event.request))
    );
});
