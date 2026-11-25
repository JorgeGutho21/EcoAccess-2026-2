// Cliente de la API del juego. Todas las operaciones son idempotentes en el servidor (registro con el mismo secreto,
// inicio, respuesta ya confirmada), así que ante el límite de conexiones de ORDS (HTTP 429), un 5xx o una red inestable
// se reintenta con espera exponencial y jitter, respetando Retry-After si llega, con un número acotado de intentos y
// avisando a la persona sin perder lo ya confirmado. La política vive en reintentos.js (la misma de las pruebas de carga).
import { POLITICA, esReintentable, leerRetryAfter, calcularEspera, dormir } from './reintentos.js?v=20261125b';

const BASE = (window.ECO_JUEGO?.api ?? '').replace(/\/$/, '');
const PARAMS = new URLSearchParams(location.search);
export const RONDA = PARAMS.get('ronda');   // solo para rondas de prueba; la pública no lleva código

let avisar = () => {};
/** `fn(mensaje | null, detalle)`: muestra u oculta el estado de reanudación («Muchos jugadores a la vez, reintentando…»). */
export function alAvisar(fn) { avisar = fn; }

export class ErrorApi extends Error {
  constructor(status, cuerpo) { super(cuerpo?.error || `Error ${status}`); this.status = status; this.codigo = cuerpo?.codigo; this.cuerpo = cuerpo; }
}

function mensajeReintento(status, intento, maximo, ms) {
  const cuando = `nuevo intento en ${Math.max(1, Math.round(ms / 1000))} s (${intento} de ${maximo})`;
  if (status === 0 && navigator.onLine === false)
    return `Sin conexión a internet. Tus respuestas confirmadas están guardadas; ${cuando}.`;
  // 429 de ORDS, 503 y la red «caída» con internet (un 429 sin CORS llega así al navegador) son casi siempre congestión.
  if (status === 0 || status === 429 || status === 503)
    return `Muchos jugadores a la vez, reintentando… Tu progreso está guardado; ${cuando}.`;
  return `El servidor está ocupado, reintentando… ${cuando}.`;
}

/**
 * Llama a la API. `paciencia` acota el tiempo total de reintentos y `maxIntentos` su número.
 * Lanza ErrorApi con el status final si no se logra (0 = sin respuesta).
 */
export async function llamar(ruta, { metodo = 'GET', cuerpo, sesion, admin, paciencia = 90000, maxIntentos = POLITICA.maxIntentos, crudo = false } = {}) {
  const url = new URL(BASE + ruta);
  if (RONDA && !url.searchParams.has('ronda')) url.searchParams.set('ronda', RONDA);
  const limite = performance.now() + paciencia;
  for (let intento = 0; ; intento++) {
    let status = 0, json = null, texto = '', retryAfter = null;
    try {
      const ctrl = new AbortController(); const t = setTimeout(() => ctrl.abort(), 20000);
      try {
        const r = await fetch(url, {
          method: metodo, signal: ctrl.signal, mode: 'cors', cache: 'no-store', credentials: 'omit', referrerPolicy: 'no-referrer',
          headers: { ...(cuerpo !== undefined ? { 'Content-Type': 'application/json' } : {}),
            ...(sesion ? { 'X-Eco-Sesion': sesion } : {}), ...(admin ? { 'X-Eco-Admin': admin } : {}) },
          body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
        });
        status = r.status;
        // Solo legible si ORDS la expone por CORS; si no, se usa la espera exponencial.
        retryAfter = leerRetryAfter(r.headers.get('Retry-After'));
        if (crudo && r.ok) { avisar(null); return r; }
        texto = await r.text();
      } finally { clearTimeout(t); }
      try { json = JSON.parse(texto); } catch { json = null; }
    } catch { status = 0; }

    if (esReintentable(status, json) && intento < maxIntentos) {
      const ms = calcularEspera(intento, retryAfter);
      if (performance.now() + ms < limite) {
        avisar(mensajeReintento(status, intento + 1, maxIntentos, ms), { status, intento: intento + 1, ms });
        await dormir(ms);
        continue;
      }
    }
    avisar(null);
    if (status >= 200 && status < 300 && json) return json;
    if (json?.ok !== undefined) throw new ErrorApi(status, json);
    throw new ErrorApi(status, {
      codigo: 'conexion',
      error: status === 0 && navigator.onLine === false ? 'No hay conexión con el servidor. Revisa tu internet e inténtalo de nuevo.'
        : status === 0 || status === 429 || status === 503 ? 'Hay muchos jugadores a la vez. Espera unos segundos y vuelve a intentarlo: tu progreso está guardado.'
        : 'El servidor no respondió como se esperaba.',
    });
  }
}

addEventListener('online', () => avisar(null));
