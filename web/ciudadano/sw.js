// Caché de la plataforma EcoAccess. Solo recursos propios (mismo origen); las llamadas a la pasarela de la estación
// nunca se almacenan (tampoco el historial oficial, que vive en la pasarela: otro origen). El juego (/juego/) y la demo
// del panel (/panel-demo/) quedan fuera de este service worker: tienen su propio alcance y datos en vivo.
// HTML y catálogo: red primero (para recibir actualizaciones); imágenes y código: caché primero.
const VERSION = 'ecoaccess-ciudadano-v5';   // subir en cada publicación: JS e imágenes son «caché primero»
const SECCIONES = ['comun', 'uml', 'conoce', 'construccion', 'tecnologia', 'programacion', 'arquitectura', 'grafos', 'hardware', 'datos', 'ia', 'integraciones', 'futuro', 'challenge'];
const BASE = ['./', 'index.html', 'css/estilos.css', 'css/plataforma.css', 'js/app.js', 'js/rutas.js', 'js/motor.js', 'js/voz.js', 'js/estacion.js', 'js/historial.js',
  ...SECCIONES.map((s) => `js/secciones/${s}.js`),
  'datos/catalogo-residuos.json', 'manifest.webmanifest',
  ...['verde', 'blanca', 'negra'].flatMap((c) => ['cuerpo', 'tapa', 'boca', 'tapa-interior'].map((p) => `assets/canecas/${c}-${p}.webp`)),
  'assets/marca/ecoaccess.webp', 'assets/marca/upc.webp', 'assets/marca/eco-saluda-200.webp', 'assets/marca/eco-tablet-200.webp', 'assets/marca/eco-celebra-200.webp'];
self.addEventListener('install', (e) => { e.waitUntil(caches.open(VERSION).then((c) => c.addAll(BASE)).then(() => self.skipWaiting())); });
self.addEventListener('activate', (e) => { e.waitUntil(caches.keys().then((k) => Promise.all(k.filter((n) => n.startsWith('ecoaccess-ciudadano-') && n !== VERSION).map((n) => caches.delete(n)))).then(() => self.clients.claim())); });
self.addEventListener('fetch', (e) => {
  const u = new URL(e.request.url);
  if (e.request.method !== 'GET' || u.origin !== location.origin) return;
  const alcance = new URL(self.registration.scope).pathname;
  const ruta = u.pathname.slice(alcance.length);
  if (ruta.startsWith('juego/') || ruta.startsWith('panel-demo/')) return;   // otras aplicaciones del mismo sitio
  const redPrimero = e.request.mode === 'navigate' || u.pathname.endsWith('.json') || u.pathname.endsWith('.html');
  e.respondWith(redPrimero
    ? fetch(e.request).then((r) => { const c = r.clone(); caches.open(VERSION).then((x) => x.put(e.request, c)); return r; }).catch(() => caches.match(e.request))
    : caches.match(e.request).then((m) => m || fetch(e.request).then((r) => { const c = r.clone(); caches.open(VERSION).then((x) => x.put(e.request, c)); return r; })));
});
