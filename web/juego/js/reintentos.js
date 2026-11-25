// Política de reintentos del juego, compartida por el navegador (js/api.js) y las pruebas de carga reales
// (pruebas/comun.mjs): así la carga imita exactamente al cliente. Sin DOM ni dependencias.
//
// Cuándo se reintenta: el límite de conexiones de ORDS (HTTP 429 con su página HTML, sin el JSON del juego), errores
// 5xx de la pasarela/balanceador y la red caída (status 0; en el navegador, un 429 de ORDS sin cabeceras CORS también
// llega como status 0). Un 429 con JSON del juego (p. ej., límite de registros por red) es una regla: NO se reintenta.
// Todas las operaciones son idempotentes en Oracle (registro con el mismo secreto, inicio, respuesta ya confirmada), así
// que reintentar nunca duplica ni cambia nada.

export const POLITICA = Object.freeze({
  base: 700,            // ms de la primera espera (se duplica en cada intento)
  tope: 12000,          // espera máxima calculada
  maxIntentos: 8,       // reintentos como máximo (además del primer envío)
  maxRetryAfter: 30000, // nunca se espera más de esto aunque el servidor pida más
});

const REINTENTABLE = new Set([0, 429, 500, 502, 503, 504]);

/** ¿Merece reintento? `json` es el cuerpo ya interpretado (null si no era JSON). */
export function esReintentable(status, json) {
  return REINTENTABLE.has(status) && (json == null || json.ok === undefined);
}

/** Milisegundos que pide la cabecera Retry-After (segundos o fecha HTTP); null si no llegó o no es válida. */
export function leerRetryAfter(valor, ahora = Date.now()) {
  if (valor == null || String(valor).trim() === '') return null;
  const s = Number(valor);
  if (Number.isFinite(s)) return s >= 0 ? s * 1000 : null;
  const fecha = Date.parse(valor);
  return Number.isNaN(fecha) ? null : Math.max(0, fecha - ahora);
}

/**
 * Espera antes del reintento número `intento` (0 = el primero).
 * - Con Retry-After: se respeta (acotado a maxRetryAfter) y se le suma un jitter pequeño para no volver todos a la vez.
 * - Sin Retry-After: exponencial con jitter «igual»: un valor al azar entre la mitad y el total de base·2^intento.
 */
export function calcularEspera(intento, retryAfterMs = null, politica = POLITICA, azar = Math.random) {
  if (retryAfterMs != null) return Math.min(politica.maxRetryAfter, retryAfterMs) + Math.floor(azar() * 500);
  const techo = Math.min(politica.tope, politica.base * 2 ** intento);
  return Math.floor(techo / 2 + azar() * (techo / 2));
}

/** Pausa aleatoria breve (0..maxMs) para repartir en el tiempo a quienes pulsan a la vez «Registrarme» o «Estoy listo». */
export const escalonamiento = (maxMs, azar = Math.random) => Math.floor(azar() * maxMs);

export const dormir = (ms) => new Promise((ok) => setTimeout(ok, ms));
