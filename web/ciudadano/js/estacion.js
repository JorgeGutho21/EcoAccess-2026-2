// Cliente de la estación física. La página pública NO conoce ninguna dirección fija: la pasarela (HTTPS) y el permiso
// temporal llegan en el código QR que se escanea presencialmente en la estación:
//   https://…/ciudadano/#e=<url de la pasarela>&t=<permiso temporal>
// El fragmento (#) no viaja al servidor de la página ni en el Referer; se guarda en sessionStorage y se borra de la URL,
// de modo que recargar la página nunca repite una apertura.

const CLAVE = 'ecoaccess.estacion.v1';
const TIEMPO_MAX_MS = 15000;

function leerGuardada() {
  try { const d = JSON.parse(sessionStorage.getItem(CLAVE) || 'null'); return d && d.expira > Date.now() ? d : null; } catch { return null; }
}
function guardar(d) { try { d ? sessionStorage.setItem(CLAVE, JSON.stringify(d)) : sessionStorage.removeItem(CLAVE); } catch { /* sin almacenamiento */ } }

function urlValida(u) {
  try {
    const x = new URL(u);
    if (x.protocol === 'https:') return x.origin;
    if (x.protocol === 'http:' && ['localhost', '127.0.0.1', '[::1]'].includes(x.hostname)) return x.origin;   // pruebas locales
  } catch { /* inválida */ }
  return null;
}

/** Toma la sesión del QR (si viene en el fragmento) y limpia la URL. */
export function iniciarSesionDesdeUrl() {
  const h = new URLSearchParams(location.hash.slice(1));
  const e = h.get('e'), t = h.get('t');
  if (e || t) history.replaceState(null, '', location.pathname + location.search);
  if (e && t && /^[A-Za-z0-9_-]{20,200}$/.test(t)) {
    const origen = urlValida(e);
    if (origen) {
      const d = { pasarela: origen, permiso: t, expira: Date.now() + 2 * 3600e3 }; guardar(d);
      try { const c = JSON.parse(localStorage.getItem('ecoaccess.cuenta.v1') || 'null') || {}; c.pasarela = origen; localStorage.setItem('ecoaccess.cuenta.v1', JSON.stringify(c)); } catch { /* sin almacenamiento */ }
      return d;
    }
  }
  return leerGuardada();
}

export const sesionActual = () => leerGuardada();
export const cerrarSesion = () => guardar(null);

async function pedir(ruta, { metodo = 'GET', cuerpo } = {}) {
  const s = leerGuardada();
  if (!s) throw Object.assign(new Error('Sin sesión de estación'), { codigo: 'sin-sesion' });
  const ctrl = new AbortController(); const temporizador = setTimeout(() => ctrl.abort(), TIEMPO_MAX_MS);
  try {
    const r = await fetch(s.pasarela + ruta, {
      method: metodo, signal: ctrl.signal, mode: 'cors', credentials: 'omit', cache: 'no-store', referrerPolicy: 'no-referrer',
      headers: { Authorization: 'Bearer ' + s.permiso, ...(cuerpo ? { 'Content-Type': 'application/json' } : {}) },
      body: cuerpo ? JSON.stringify(cuerpo) : undefined,
    });
    const datos = await r.json().catch(() => ({}));
    if (r.status === 401 || r.status === 403) { guardar(null); throw Object.assign(new Error(datos.error || 'El permiso de la estación expiró. Escanea de nuevo el código QR.'), { codigo: 'permiso' }); }
    if (!r.ok) throw Object.assign(new Error(datos.error || `La estación respondió ${r.status}.`), { codigo: datos.codigo || 'http-' + r.status, datos });
    if (datos.sesion?.expira) { const g = leerGuardada(); if (g) { g.expira = Date.parse(datos.sesion.expira) || g.expira; guardar(g); } }
    return datos;
  } catch (e) {
    if (e.name === 'AbortError') throw Object.assign(new Error('La estación no respondió a tiempo.'), { codigo: 'tiempo' });
    if (e instanceof TypeError) throw Object.assign(new Error('No hay comunicación con la estación.'), { codigo: 'red' });
    throw e;
  } finally { clearTimeout(temporizador); }
}

/** Estado real: { estado: 'disponible'|'ocupada'|'desconectada'|'sin-sesion-supervisada', texto, modo, politica, ... } */
export const consultarEstado = () => pedir('/api/v1/estado');

/**
 * Solicita la apertura. «clave» es única por intento (idempotencia: un doble clic o un reintento de red no abre dos veces).
 * Respuesta: { ok, clave, modo, plan: [{ caneca, inicioMs, abiertaMs }], movimientoMs, totalMs, estadoFisico: 'comandado' }
 */
export const solicitarApertura = ({ clave, canecas, materiales, consulta }) =>
  pedir('/api/v1/aperturas', { metodo: 'POST', cuerpo: { clave, canecas, materiales, consulta: String(consulta).slice(0, 160), creadoEn: new Date().toISOString(),
    ...(tokenCuenta() ? { cuenta: tokenCuenta() } : {}) } });

export const confirmarDeposito = (clave) => pedir(`/api/v1/aperturas/${encodeURIComponent(clave)}/confirmacion`, { metodo: 'POST', cuerpo: { deposito: true } });

// ───────────── Cuenta vinculada con Telegram e historial oficial (Oracle) ─────────────
// La pasarela de la estación se recuerda en este dispositivo (sin el permiso presencial) para consultar el historial
// después; el token de cuenta lo entrega el bot tras confirmar «/start web_CODIGO» desde un chat con cuenta EcoAccess.
// Nada de esto abre canecas: abrir sigue exigiendo el QR presencial y la sesión supervisada de la estación.
const CLAVE_CUENTA = 'ecoaccess.cuenta.v1';
function leerCuenta() { try { return JSON.parse(localStorage.getItem(CLAVE_CUENTA) || 'null') || {}; } catch { return {}; } }
function guardarCuenta(c) { try { localStorage.setItem(CLAVE_CUENTA, JSON.stringify(c)); } catch { /* sin almacenamiento */ } }
export function recordarPasarela() { const s = leerGuardada(); if (s) { const c = leerCuenta(); c.pasarela = s.pasarela; guardarCuenta(c); } }
export const cuentaActual = () => { const c = leerCuenta(); return { pasarela: c.pasarela || leerGuardada()?.pasarela || null, vinculada: !!c.token, codigo: c.codigo || null }; };
export const tokenCuenta = () => leerCuenta().token || null;

async function pedirCuenta(ruta, cuerpo) {
  const { pasarela } = cuentaActual();
  if (!pasarela) throw Object.assign(new Error('Primero escanea el código QR de la estación EcoAccess.'), { codigo: 'sin-pasarela' });
  const ctrl = new AbortController(); const t = setTimeout(() => ctrl.abort(), TIEMPO_MAX_MS);
  try {
    const r = await fetch(pasarela + '/api/v1/cuenta/' + ruta, { method: 'POST', mode: 'cors', credentials: 'omit', cache: 'no-store', referrerPolicy: 'no-referrer',
      signal: ctrl.signal, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(cuerpo || {}) });
    const d = await r.json().catch(() => ({}));
    if (!r.ok && r.status !== 401) throw Object.assign(new Error(d.error || `La estación respondió ${r.status}.`), { codigo: 'http-' + r.status });
    return { status: r.status, ...d };
  } catch (e) {
    if (e.name === 'AbortError' || e instanceof TypeError) throw Object.assign(new Error('La estación EcoAccess no está en línea ahora. El historial oficial también está en Telegram (/historial).'), { codigo: 'red' });
    throw e;
  } finally { clearTimeout(t); }
}

/** Pide un código de un solo uso y devuelve el enlace t.me para confirmarlo en el bot. */
export async function iniciarVinculo() {
  const d = await pedirCuenta('codigo');
  const c = leerCuenta(); c.codigo = d.codigo; guardarCuenta(c);
  return d;   // { codigo, enlace, expiraS }
}
/** Canjea el código confirmado en Telegram. Devuelve 'vinculado' | 'pendiente' | 'vencido'. */
export async function completarVinculo() {
  const c = leerCuenta(); if (!c.codigo) return 'vencido';
  const d = await pedirCuenta('canjear', { codigo: c.codigo });
  if (d.token) { c.token = d.token; delete c.codigo; guardarCuenta(c); return 'vinculado'; }
  if (d.estado === 'vencido') { delete c.codigo; guardarCuenta(c); }
  return d.estado || 'pendiente';
}
export async function historialOficial() {
  const c = leerCuenta(); if (!c.token) return null;
  const d = await pedirCuenta('historial', { token: c.token });
  if (d.status === 401) { delete c.token; guardarCuenta(c); return null; }
  return d.operaciones || [];
}
export async function desvincular() {
  const c = leerCuenta(); if (c.token) { try { await pedirCuenta('desvincular', { token: c.token }); } catch { /* se borra igual en este dispositivo */ } }
  delete c.token; delete c.codigo; guardarCuenta(c);
}
