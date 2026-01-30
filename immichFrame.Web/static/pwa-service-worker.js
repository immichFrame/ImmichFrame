// Service worker for ImmichFrame PWA
const CACHE_NAME = 'immichframe-v1';

// Store auth secret for video streaming requests
let authSecret = null;
let authSecretResolvers = [];

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
    if (event.data && event.data.type === 'SET_AUTH_SECRET') {
        authSecret = event.data.authSecret;
        // Resolve any pending auth requests
        authSecretResolvers.forEach(resolve => resolve(authSecret));
        authSecretResolvers = [];
    }
});

// Request auth secret from clients if not available
async function getAuthSecret(timeoutMs = 2000) {
    if (authSecret) return authSecret;

    // Request auth from all clients
    const clients = await self.clients.matchAll({ type: 'window' });
    clients.forEach(client => {
        client.postMessage({ type: 'REQUEST_AUTH_SECRET' });
    });

    return new Promise((resolve) => {
        const timeout = setTimeout(() => {
            // Remove this resolver and resolve with current value (may be null)
            const index = authSecretResolvers.indexOf(resolveAuth);
            if (index > -1) authSecretResolvers.splice(index, 1);
            resolve(authSecret);
        }, timeoutMs);

        const resolveAuth = (secret) => {
            clearTimeout(timeout);
            resolve(secret);
        };

        authSecretResolvers.push(resolveAuth);
    });
}

self.addEventListener('fetch', (event) => {
    const url = new URL(event.request.url);

    // Intercept video streaming requests to add Authorization header
    if (url.pathname.match(/^\/api\/Asset\/[^/]+\/Asset$/)) {
        event.respondWith(
            (async () => {
                const secret = await getAuthSecret();
                if (!secret) {
                    // No auth available, let the request fail naturally
                    return fetch(event.request);
                }

                const headers = new Headers();
                for (const [key, value] of event.request.headers) {
                    headers.set(key, value);
                }
                headers.set('Authorization', 'Bearer ' + secret);

                const modifiedRequest = new Request(url.href, {
                    method: event.request.method,
                    headers: headers,
                    mode: 'cors',
                    credentials: 'same-origin'
                });

                return fetch(modifiedRequest);
            })()
        );
        return;
    }

    // Basic network-first strategy for other requests
    event.respondWith(
        fetch(event.request).catch(() => caches.match(event.request))
    );
});