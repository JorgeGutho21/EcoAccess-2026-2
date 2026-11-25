// Cliente de la API del juego. Todas las operaciones son idempotentes en el servidor (registro con el mismo secreto,
// inicio, respuesta ya confirmada), así que ante una red inestable o un límite momentáneo de Oracle se reintenta con
// espera exponencial durante un minuto, avisando a la persona, sin perder lo ya confirmado.
const BASE = (window.ECO_JUEGO?.api ?? '').replace(/\/$/, '');
const PARAMS = new URLSearchParams(location.search);
export const RONDA = PARAMS.get('ronda');   // solo para rondas de prueba; la pública no lleva código

let avisar = () => {};
export function alAvisar(fn) { avisar = fn; }

export class ErrorApi extends Error {
  constructor(status, cuerpo) { super(cuerpo?.error || `Error ${status}`); this.status = status; this.codigo = cuerpo?.codigo; this.cuerpo = cuerpo; }
}

const REINTENTABLE = new Set([0, 429, 500, 502, 503, 504]);

export async function llamar(ruta, { metodo = 'GET', cuerpo, sesion, admin, paciencia = 60000, crudo = false } = {}) {
  const url = new URL(BASE + ruta);
  if (RONDA && !url.searchParams.has('ronda')) url.searchParams.set('ronda', RONDA);
  const limite = performance.now() + paciencia;
  for (let intento = 0; ; intento++) {
    let status = 0, json = null, texto = '';
    try {
      const ctrl = new AbortController(); const t = setTimeout(() => ctrl.abort(), 20000);
      const r = await fetch(url, {
        method: metodo, signal: ctrl.signal, mode: 'cors', cache: 'no-store', credentials: 'omit', referrerPolicy: 'no-referrer',
        headers: { ...(cuerpo !== undefined ? { 'Content-Type': 'application/json' } : {}),
          ...(sesion ? { 'X-Eco-Sesion': sesion } : {}), ...(admin ? { 'X-Eco-Admin': admin } : {}) },
        body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
      });
      clearTimeout(t);
      status = r.status;
      if (crudo && r.ok) { avisar(null); return r; }
      texto = await r.text();
      try { json = JSON.parse(texto); } catch { json = null; }
    } catch { status = 0; }
    // Un 429 con JSON propio es una regla del juego (límite de registros): no se reintenta. El 429 de Oracle es HTML.
    const deOracle = status === 429 ? !json || json.ok === undefined : true;
    const reintentar = REINTENTABLE.has(status) && deOracle && (json?.ok === undefined) && performance.now() < limite;
    if (reintentar) {
      avisar(navigator.onLine === false
        ? 'Sin conexión a internet. Tus respuestas confirmadas están guardadas; seguimos intentando…'
        : 'El servidor está ocupado. Reintentando…');
      await new Promise((ok) => setTimeout(ok, Math.min(6000, 500 * 2 ** intento) * (0.6 + Math.random() * 0.8)));
      continue;
    }
    avisar(null);
    if (status >= 200 && status < 300 && json) return json;
    throw new ErrorApi(status, json ?? { error: status === 0 ? 'No hay conexión con el servidor. Revisa tu internet e inténtalo de nuevo.' : 'El servidor no respondió como se esperaba.' });
  }
}

addEventListener('online', () => avisar(null));
