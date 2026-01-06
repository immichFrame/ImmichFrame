// Service worker for ImmichFrame PWA
const CACHE_NAME = 'immichframe-v1';

// Store auth secret for video streaming requests
let authSecret = null;

self.addEventListener('install', (event) => {
    console.log('Service worker installing...');
    event.waitUntil(self.skipWaiting());
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

// Listen for auth secret updates from the main app
self.addEventListener('message', (event) => {
    console.log('Service worker received message:', event.data);
    if (event.data && event.data.type === 'SET_AUTH_SECRET') {
        authSecret = event.data.authSecret;
    }
});

self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);

    // Intercept video streaming requests to add Authorization header
    if (authSecret && url.pathname.match(/^\/api\/Asset\/[^/]+\/Asset$/)) {

        const headers = new Headers();
        for (const [key, value] of event.request.headers) {
            headers.set(key, value);
        }
        headers.set('Authorization', 'Bearer ' + authSecret);

        const modifiedRequest = new Request(url.href, {
            method: event.request.method,
            headers: headers,
            mode: 'cors',
            credentials: 'same-origin'
        });

        event.respondWith(fetch(modifiedRequest));
        return;
    }

    // Basic network-first strategy for other requests
    event.respondWith(
        fetch(event.request).catch(() => caches.match(event.request))
    );
});
