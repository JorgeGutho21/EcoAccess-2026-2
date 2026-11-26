// Caché de la plataforma EcoAccess. Solo recursos propios (mismo origen); las llamadas a la pasarela de la estación
// nunca se almacenan (tampoco el historial oficial, que vive en la pasarela: otro origen). El juego (/juego/) y la demo
// del panel (/panel-demo/) quedan fuera de este service worker: tienen su propio alcance y datos en vivo.
// HTML, JSON, CSS y JS: red primero (para recibir actualizaciones); imágenes: caché primero.
const VERSION = 'ecoaccess-ciudadano-v10';   // subir cuando cambien imágenes («caché primero»)
const SECCIONES = ['comun', 'uml', 'conoce', 'construccion', 'tecnologia', 'programacion', 'arquitectura', 'grafos', 'hardware', 'datos', 'ia', 'integraciones', 'resultados', 'futuro', 'challenge'];
const BASE = ['./', 'index.html', 'css/estilos.css', 'css/plataforma.css', 'js/app.js', 'js/rutas.js', 'js/motor.js', 'js/voz.js', 'js/estacion.js', 'js/historial.js',
  ...SECCIONES.map((s) => `js/secciones/${s}.js`),
  'datos/catalogo-residuos.json', 'manifest.webmanifest',
  ...['verde', 'blanca', 'negra'].flatMap((c) => ['cuerpo', 'tapa', 'boca', 'tapa-interior'].map((p) => `assets/canecas/${c}-${p}.webp`)),
  'assets/marca/ecoaccess.webp', 'assets/marca/upc.webp', 'assets/marca/eco-saluda-200.webp', 'assets/marca/eco-tablet-200.webp', 'assets/marca/eco-celebra-200.webp',
  ...['serum', 'aluminio', 'cuaderno', 'servilleta', 'limon'].map((f) => `assets/fotos/${f}.webp`)];
// «EcoAccess dos clics manual» (/dos-clics/) se mantiene aparte y puede no existir todavía en una copia: es opcional.
const OPCIONALES = ['dos-clics/', 'dos-clics/index.html', 'dos-clics/css/dos-clics.css', 'dos-clics/js/dos-clics.js'];
// Nunca se precachea: siempre se pide a la red (ver «fetch»).
const SOLO_RED = ['version.json'];

// cache.addAll falla entero si falta un archivo; aquí cada recurso se agrega por separado y un fallo no bloquea la
// instalación (queda en la consola del service worker, no en la página).
const agregar = (cache, url) => cache.add(new Request(url, { cache: 'reload' })).catch((e) => console.warn('[sw] sin precache:', url, e?.message ?? e));

self.addEventListener('install', (e) => {
  e.waitUntil(caches.open(VERSION)
    .then((c) => Promise.all([...BASE, ...OPCIONALES].map((u) => agregar(c, u))))
    .then(() => self.skipWaiting()));
});
self.addEventListener('activate', (e) => { e.waitUntil(caches.keys().then((k) => Promise.all(k.filter((n) => n.startsWith('ecoaccess-ciudadano-') && n !== VERSION).map((n) => caches.delete(n)))).then(() => self.clients.claim())); });

const guardar = (req, r) => { if (r.ok) { const c = r.clone(); caches.open(VERSION).then((x) => x.put(req, c)); } return r; };

self.addEventListener('fetch', (e) => {
  const u = new URL(e.request.url);
  if (e.request.method !== 'GET' || u.origin !== location.origin) return;
  const alcance = new URL(self.registration.scope).pathname;
  const ruta = u.pathname.slice(alcance.length);
  if (ruta.startsWith('juego/') || ruta.startsWith('panel-demo/')) return;   // otras aplicaciones del mismo sitio
  if (SOLO_RED.includes(ruta)) {   // identificador de versión: red primero, sin caché HTTP; la copia solo sirve sin conexión
    e.respondWith(fetch(e.request, { cache: 'no-store' }).then((r) => guardar(e.request, r)).catch(() => caches.match(e.request).then((m) => m || Response.error())));
    return;
  }
  // HTML, JSON, CSS y JS: red primero (una publicación nueva se ve al recargar, sin depender de subir VERSION); la copia
  // en caché solo sirve sin conexión. Imágenes: caché primero.
  const redPrimero = e.request.mode === 'navigate' || /\.(json|html|css|m?js)$/.test(u.pathname);
  // «no-cache»: se revalida con el servidor (GitHub Pages sirve con max-age=600); sin esto el navegador entregaba hasta
  // 10 minutos la página anterior con el script anterior aunque ya hubiera una publicación nueva.
  const deRed = () => fetch(e.request.mode === 'navigate'
    ? new Request(e.request.url, { cache: 'no-cache', credentials: 'same-origin' })
    : new Request(e.request, { cache: 'no-cache' }));
  e.respondWith(redPrimero
    // Sin conexión, «ignoreSearch»: index.html pide los CSS/JS con «?v=…» y la copia precacheada no lo lleva.
    ? deRed().then((r) => guardar(e.request, r)).catch(() => caches.match(e.request, { ignoreSearch: true }).then((m) => m || Response.error()))
    : caches.match(e.request).then((m) => m || fetch(e.request).then((r) => guardar(e.request, r))));
});
