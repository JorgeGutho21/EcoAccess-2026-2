// EcoAccess · Estación web de canecas (servidor local, sin dependencias).
//
// Navegador ──► este servidor (127.0.0.1) ──► modo «simulado»: ciclo de tapa de demostración
//                                          └► modo «bot»: reenvía la solicitud al servicio de EcoAccess, que crea la
//                                             orden autorizada en Oracle; el bridge y el ESP32 (HMAC) mueven la tapa.
// El navegador NUNCA habla con el ESP32 ni con Oracle, y aquí no hay claves, wallets ni tokens.
//
// Variables de entorno:
//   ECOACCESS_WEB_PORT         puerto local (4173)
//   ECOACCESS_MODO             simulado | bot (simulado)
//   ECOACCESS_URL_APERTURAS    en modo bot: http://127.0.0.1:<puerto>/aperturas/ del servicio de EcoAccess
//   ECOACCESS_FUERA_SERVICIO   colores fuera de servicio, separados por coma (p. ej. "verde,negra")
//   ECOACCESS_ANGULO_APERTURA  ángulo que se informa para la tapa abierta (80)
//   ECOACCESS_ABIERTO_MS       ventana de depósito en modo simulado (7000). En modo bot manda la que informe el bot (firmware).
'use strict';
const http = require('http');
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');

const PUERTO = Number(process.env.ECOACCESS_WEB_PORT || 4173);
const MODO = (process.env.ECOACCESS_MODO || 'simulado').toLowerCase() === 'bot' ? 'bot' : 'simulado';
const URL_APERTURAS = process.env.ECOACCESS_URL_APERTURAS || 'http://127.0.0.1:8190/aperturas/'; // EcoAccess.P3.Bot --estacion-web
const TOKEN_APERTURAS = process.env.ECOACCESS_TOKEN_APERTURAS || ''; // igual a EstacionWeb:Token del bot, si se configuró
const ANGULO = Math.min(Math.max(Number(process.env.ECOACCESS_ANGULO_APERTURA || 80), 10), 120);
const ABIERTO_MS = Math.min(Math.max(Number(process.env.ECOACCESS_ABIERTO_MS || 7000), 3000), 30000);
const MOVIMIENTO_MS = 1200; // tiempo aproximado que tarda el servo en llegar al ángulo
const MAX_COLA = 3;
const RAIZ = path.join(__dirname, 'dist');

// Mismos identificadores que Oracle y el firmware (hardware/estacion_tres_canecas).
const CANECAS = {
  blanca: { id: 1093, nombre: 'Caneca blanca' },
  verde: { id: 1400, nombre: 'Caneca verde' },
  negra: { id: 1401, nombre: 'Caneca negra' },
};
const fueraDeServicio = new Set((process.env.ECOACCESS_FUERA_SERVICIO || '')
  .split(',').map((c) => c.trim().toLowerCase()).filter((c) => c in CANECAS));

if (MODO === 'bot') {
  let valida = false;
  try { const u = new URL(URL_APERTURAS); valida = u.protocol === 'http:' && u.hostname === '127.0.0.1'; } catch { /* inválida */ }
  if (!valida) {
    console.error('Modo bot: ECOACCESS_URL_APERTURAS debe ser http://127.0.0.1:<puerto>/<ruta>/ (solo este equipo).');
    process.exit(2);
  }
}

// ── Estado de la estación ────────────────────────────────────────────────────────────────────────────────────
const estado = {};
for (const color of Object.keys(CANECAS)) {
  estado[color] = { estado: fueraDeServicio.has(color) ? 'fuera_servicio' : 'cerrada', angulo: 0, hasta: 0, mensaje: '' };
}
const cola = [];
let ocupada = null; // color en movimiento; nunca dos tapas a la vez (igual que el firmware)
const clientes = new Set();

function instantanea() {
  return { modo: MODO, angulo: ANGULO, abiertoMs: ABIERTO_MS, ocupada, cola: [...cola], canecas: estado, ahora: Date.now() };
}
function emitir() {
  const datos = `data: ${JSON.stringify(instantanea())}\n\n`;
  for (const r of clientes) r.write(datos);
}
function fijar(color, nuevo, extra = {}) {
  Object.assign(estado[color], { estado: nuevo, mensaje: '', hasta: 0 }, extra);
  emitir();
}
const esperar = (ms) => new Promise((r) => setTimeout(r, ms));

async function cicloSimulado(color) {
  fijar(color, 'abriendo', { angulo: ANGULO, hasta: Date.now() + MOVIMIENTO_MS });
  await esperar(MOVIMIENTO_MS);
  fijar(color, 'abierta', { angulo: ANGULO, hasta: Date.now() + ABIERTO_MS });
  await esperar(ABIERTO_MS);
  fijar(color, 'cerrando', { angulo: 0, hasta: Date.now() + MOVIMIENTO_MS });
  await esperar(MOVIMIENTO_MS);
}

async function cicloBot(color) {
  fijar(color, 'abriendo', { angulo: ANGULO, hasta: Date.now() + MOVIMIENTO_MS, mensaje: 'Enviando la orden a la caneca…' });
  let respuesta;
  try {
    const r = await fetch(URL_APERTURAS, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json', 'X-EcoAccess': 'estacion', ...(TOKEN_APERTURAS ? { 'X-EcoAccess-Token': TOKEN_APERTURAS } : {}) },
      body: JSON.stringify({ solicitud: crypto.randomUUID(), idCaneca: CANECAS[color].id, color, angulo: ANGULO, origen: 'estacion-web' }),
      // El bot responde cuando el bridge confirma la apertura en Oracle (hasta 20 s por defecto).
      signal: AbortSignal.timeout(30000),
      redirect: 'error',
    });
    respuesta = await r.json().catch(() => ({}));
    if (!r.ok || respuesta.aceptada !== true) {
      fijar(color, 'rechazada', { angulo: 0, mensaje: String(respuesta.mensaje || 'La caneca no aceptó la orden.').slice(0, 160) });
      await esperar(4000);
      return;
    }
  } catch {
    fijar(color, 'error', { angulo: 0, mensaje: 'El servicio de canecas no responde.' });
    await esperar(4000);
    return;
  }
  // El bridge confirma la orden; la ventana de 7 s la mantiene el firmware (ABIERTO_MS).
  const ventana = typeof respuesta.abiertoMs === 'number' && respuesta.abiertoMs >= 0 ? Math.min(respuesta.abiertoMs, 30000) : ABIERTO_MS;
  if (ventana > 0) {
    fijar(color, 'abierta', { angulo: ANGULO, hasta: Date.now() + ventana });
    await esperar(ventana);
  }
  fijar(color, 'cerrando', { angulo: 0, hasta: Date.now() + MOVIMIENTO_MS });
  await esperar(MOVIMIENTO_MS);
}

async function procesarCola() {
  if (ocupada) return;
  const color = cola.shift();
  if (!color) return;
  ocupada = color;
  try {
    await (MODO === 'bot' ? cicloBot(color) : cicloSimulado(color));
  } finally {
    fijar(color, fueraDeServicio.has(color) ? 'fuera_servicio' : 'cerrada', { angulo: 0 });
    ocupada = null;
    procesarCola();
  }
}

function solicitarApertura(color) {
  const c = estado[color];
  if (c.estado === 'fuera_servicio') return [423, { ok: false, motivo: 'fuera_servicio', mensaje: `${CANECAS[color].nombre} está fuera de servicio.` }];
  if (ocupada === color || cola.includes(color)) return [200, { ok: true, repetida: true }];
  if (cola.length >= MAX_COLA) return [429, { ok: false, motivo: 'cola_llena', mensaje: 'Espera un momento, hay varias aperturas en curso.' }];
  cola.push(color);
  if (ocupada) fijar(color, 'en_cola');
  procesarCola();
  return [202, { ok: true, enCola: Boolean(ocupada && ocupada !== color) }];
}

// ── HTTP ─────────────────────────────────────────────────────────────────────────────────────────────────────
const TIPOS = {
  '.html': 'text/html; charset=utf-8', '.css': 'text/css; charset=utf-8', '.js': 'application/javascript; charset=utf-8',
  '.json': 'application/json; charset=utf-8', '.webmanifest': 'application/manifest+json', '.png': 'image/png',
  '.jpg': 'image/jpeg', '.jpeg': 'image/jpeg', '.webp': 'image/webp', '.svg': 'image/svg+xml', '.ico': 'image/x-icon',
};
const HOSTS = new Set([`localhost:${PUERTO}`, `127.0.0.1:${PUERTO}`]);

function json(res, codigo, cuerpo) {
  res.writeHead(codigo, { 'Content-Type': 'application/json; charset=utf-8', 'Cache-Control': 'no-store' });
  res.end(JSON.stringify(cuerpo));
}

const servidor = http.createServer((req, res) => {
  res.setHeader('X-Content-Type-Options', 'nosniff');
  res.setHeader('Referrer-Policy', 'no-referrer');
  // Contra «DNS rebinding»: solo se atiende a nombres locales.
  if (!HOSTS.has(req.headers.host || '')) { res.writeHead(421); return res.end(); }
  const url = new URL(req.url, `http://${req.headers.host}`);

  if (url.pathname === '/api/estado' && req.method === 'GET') return json(res, 200, instantanea());

  if (url.pathname === '/api/eventos' && req.method === 'GET') {
    res.writeHead(200, { 'Content-Type': 'text/event-stream', 'Cache-Control': 'no-store', Connection: 'keep-alive' });
    res.write(`retry: 2000\ndata: ${JSON.stringify(instantanea())}\n\n`);
    clientes.add(res);
    const latido = setInterval(() => res.write(': latido\n\n'), 20000);
    req.on('close', () => { clearInterval(latido); clientes.delete(res); });
    return;
  }

  const apertura = url.pathname.match(/^\/api\/canecas\/(blanca|verde|negra)\/abrir$/);
  if (apertura && req.method === 'POST') {
    // El encabezado propio obliga a una verificación CORS que este servidor nunca aprueba: otra página web no puede
    // abrir una tapa desde el navegador de la estación.
    if (req.headers['x-ecoaccess'] !== 'estacion') return json(res, 403, { ok: false });
    const [codigo, cuerpo] = solicitarApertura(apertura[1]);
    return json(res, codigo, cuerpo);
  }
  if (url.pathname.startsWith('/api/')) return json(res, 404, { ok: false });

  if (req.method !== 'GET' && req.method !== 'HEAD') { res.writeHead(405); return res.end(); }
  let relativo;
  try { relativo = url.pathname === '/' ? 'index.html' : decodeURIComponent(url.pathname).replace(/^\/+/, ''); }
  catch { res.writeHead(400); return res.end(); }
  const archivo = path.resolve(RAIZ, relativo);
  if (!archivo.startsWith(RAIZ + path.sep)) { res.writeHead(403); return res.end(); }
  fs.readFile(archivo, (error, contenido) => {
    if (error) { res.writeHead(error.code === 'ENOENT' ? 404 : 500); return res.end(); }
    const ext = path.extname(archivo).toLowerCase();
    res.writeHead(200, {
      'Content-Type': TIPOS[ext] || 'application/octet-stream',
      'Cache-Control': ['.html', '.js', '.css'].includes(ext) ? 'no-store' : 'max-age=3600',
    });
    res.end(req.method === 'HEAD' ? undefined : contenido);
  });
});

servidor.listen(PUERTO, '127.0.0.1', () => {
  console.log(`EcoAccess · Estación lista en http://localhost:${PUERTO}  (modo ${MODO}${MODO === 'bot' ? ` → ${URL_APERTURAS}` : ''})`);
  if (fueraDeServicio.size) console.log(`Fuera de servicio: ${[...fueraDeServicio].join(', ')}`);
});
