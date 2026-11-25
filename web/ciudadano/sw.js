// Caché de la interfaz ciudadana. Solo recursos propios (mismo origen); las llamadas a la pasarela de la estación
// nunca se almacenan. HTML y catálogo: red primero (para recibir actualizaciones); imágenes y código: caché primero.
const VERSION = 'ecoaccess-ciudadano-v2';   // subir en cada publicación: JS e imágenes son «caché primero»
const BASE = ['./', 'index.html', 'css/estilos.css', 'js/app.js', 'js/motor.js', 'js/voz.js', 'js/estacion.js', 'js/historial.js',
  'datos/catalogo-residuos.json', 'manifest.webmanifest',
  ...['verde', 'blanca', 'negra'].flatMap((c) => ['cuerpo', 'tapa', 'boca'].map((p) => `assets/canecas/${c}-${p}.webp`)),
  'assets/marca/ecoaccess.webp', 'assets/marca/upc.webp', 'assets/marca/eco-saluda.webp', 'assets/marca/eco-tablet.webp', 'assets/marca/eco-celebra.webp'];
self.addEventListener('install', (e) => { e.waitUntil(caches.open(VERSION).then((c) => c.addAll(BASE)).then(() => self.skipWaiting())); });
self.addEventListener('activate', (e) => { e.waitUntil(caches.keys().then((k) => Promise.all(k.filter((n) => n !== VERSION).map((n) => caches.delete(n)))).then(() => self.clients.claim())); });
self.addEventListener('fetch', (e) => {
  const u = new URL(e.request.url);
  if (e.request.method !== 'GET' || u.origin !== location.origin) return;
  const redPrimero = e.request.mode === 'navigate' || u.pathname.endsWith('.json') || u.pathname.endsWith('.html');
  e.respondWith(redPrimero
    ? fetch(e.request).then((r) => { const c = r.clone(); caches.open(VERSION).then((x) => x.put(e.request, c)); return r; }).catch(() => caches.match(e.request))
    : caches.match(e.request).then((m) => m || fetch(e.request).then((r) => { const c = r.clone(); caches.open(VERSION).then((x) => x.put(e.request, c)); return r; })));
});
