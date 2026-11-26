// Utilidades de las pruebas reales contra Oracle Cloud (ORDS). Nada de esto se publica en la página.
// La clave de administración y la clave de respuestas se leen de %LOCALAPPDATA%\EcoAccessP3\juego (fuera del repositorio).
// Los reintentos usan la MISMA política que el navegador (../js/reintentos.js): las pruebas de carga imitan al cliente.
import { readFileSync, existsSync } from 'node:fs';
import { join } from 'node:path';
import { randomBytes } from 'node:crypto';
import { POLITICA, esReintentable, leerRetryAfter, calcularEspera, escalonamiento, dormir } from '../js/reintentos.js';

export const BASE = process.env.ECO_JUEGO_API ?? 'https://g5ab3c4722e812d-ecoaccess.adb.sa-bogota-1.oraclecloudapps.com/ords/ecoaccess/juego/v1';
const privado = join(process.env.LOCALAPPDATA ?? '', 'EcoAccessP3', 'juego');
export const CLAVE_ADMIN = process.env.ECO_JUEGO_ADMIN ?? readFileSync(join(privado, 'clave-admin.txt'), 'utf8').trim();
/** IDs de las opciones correctas, leídos del script privado (no están en el repositorio). */
const leerClave = (archivo) => (existsSync(join(privado, archivo)) ? readFileSync(join(privado, archivo), 'utf8') : '')
  .match(/id IN \(([\d,\s]+)\)/)?.[1]?.split(',').map((x) => Number(x.trim())).filter(Boolean) ?? [];
export const CORRECTAS = new Set(leerClave('cargar-clave-respuestas.sql'));
/** Las 7 de la ronda oficial más las 10 de la ronda de diez (script privado aparte; ids distintos), si existe. */
export const CORRECTAS_TODAS = new Set([...CORRECTAS, ...leerClave('cargar-clave-respuestas-ronda10.sql')]);

export const secreto = () => randomBytes(32).toString('base64url');
export const ORIGEN = 'https://jorgegutho21.github.io';
/** Pausa aleatoria del cliente antes de registrar e iniciar (igual que js/juego.js). */
export const ESCALON_MS = 1200;

export const contadores = { limitadas: 0, limitesJuego: 0, reintentos: 0, sinRespuesta: 0, retryAfter: 0, corsEn429: 0, agotadas: 0 };
/**
 * Igual que el navegador (js/api.js): reintenta 429 de ORDS (HTML), 5xx y red caída con espera exponencial + jitter,
 * respeta Retry-After, como máximo `maxIntentos` veces y dentro de `paciencia` ms. `ms` = tiempo total percibido.
 */
export async function llamar(ruta, { metodo = 'GET', cuerpo, sesion, admin, ronda, maxIntentos = POLITICA.maxIntentos, paciencia = 90000 } = {}) {
  const url = new URL(BASE + ruta);
  if (ronda) url.searchParams.set('ronda', ronda);
  const t0 = performance.now();
  for (let intento = 0; ; intento++) {
    let status = 0, texto = '', json = null, retryAfter = null;
    try {
      const r = await fetch(url, {
        method: metodo, signal: AbortSignal.timeout(20000),
        headers: { Origin: ORIGEN, ...(cuerpo !== undefined ? { 'Content-Type': 'application/json' } : {}),
          ...(sesion ? { 'X-Eco-Sesion': sesion } : {}), ...(admin ? { 'X-Eco-Admin': admin } : {}) },
        body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
      });
      status = r.status;
      retryAfter = leerRetryAfter(r.headers.get('retry-after'));
      texto = await r.text();
      try { json = JSON.parse(texto); } catch { json = null; }
      if (status === 429 && json?.ok === undefined) {   // 429 de ORDS (HTML); el 429 con JSON es una regla del juego
        contadores.limitadas++;
        if (retryAfter != null) contadores.retryAfter++;
        if (r.headers.get('access-control-allow-origin')) contadores.corsEn429++;
      } else if (status === 429) contadores.limitesJuego++;
    } catch (e) { status = 0; texto = String(e); json = null; contadores.sinRespuesta++; }
    if (esReintentable(status, json) && intento < maxIntentos) {
      const ms = calcularEspera(intento, retryAfter);
      if (performance.now() - t0 + ms < paciencia) { contadores.reintentos++; await dormir(ms); continue; }
    }
    if (esReintentable(status, json)) contadores.agotadas++;
    return { status, json: json ?? { texto }, ms: performance.now() - t0, texto, intentos: intento + 1 };
  }
}

export const admin = (accion, cuerpo = {}) => llamar('/admin/' + accion, { metodo: 'POST', cuerpo, admin: CLAVE_ADMIN });

export async function crearRondaPrueba(nombre, banco) {
  const c = await admin('crear', { nombre, prueba: true, ...(banco ? { banco } : {}) });
  if (c.status !== 200) throw new Error('No se pudo crear la ronda de prueba: ' + c.texto);
  const a = await admin('abrir', { competencia: c.json.id });
  if (a.status !== 200) throw new Error('No se pudo abrir la ronda de prueba: ' + a.texto);
  return { id: c.json.id, codigo: c.json.codigo };
}

export const eliminarRonda = (id) => admin('eliminar_prueba', { competencia: id });

/**
 * Juega un intento completo como el navegador. `aciertos` = cuántas preguntas responder bien (las primeras).
 * `escalonar` = pausa aleatoria del cliente antes de registrar e iniciar (desactivable para pruebas funcionales).
 */
export async function jugar(ronda, { nombre, alias, aciertos = 10, pausaMs = 0, escalonar = false, correctas = CORRECTAS_TODAS }) {
  const sesion = secreto();
  const tiempos = [];
  if (escalonar) await dormir(escalonamiento(ESCALON_MS));
  const reg = await llamar('/registro', { metodo: 'POST', cuerpo: { nombre, alias, acepta: true, sesion, dispositivo: secreto(), ronda } });
  tiempos.push(reg.ms);
  if (reg.status !== 201) return { error: reg, tiempos };
  if (escalonar) await dormir(escalonamiento(ESCALON_MS));
  let e = await llamar('/intento/inicio', { metodo: 'POST', sesion }); tiempos.push(e.ms);
  for (let n = 1; e.json.estado === 'EN_CURSO'; n++) {
    const p = e.json.pregunta;
    const buena = p.opciones.find((o) => correctas.has(o.id));
    const mala = p.opciones.find((o) => !correctas.has(o.id));
    if (pausaMs) await dormir(pausaMs);
    e = await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: p.orden, opcion: (n <= aciertos ? buena ?? mala : mala).id } });
    tiempos.push(e.ms);
  }
  return { sesion, final: e, tiempos };
}
