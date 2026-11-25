// Utilidades de las pruebas reales contra Oracle Cloud (ORDS). Nada de esto se publica en la página.
// La clave de administración y la clave de respuestas se leen de %LOCALAPPDATA%\EcoAccessP3\juego (fuera del repositorio).
import { readFileSync } from 'node:fs';
import { join } from 'node:path';
import { randomBytes } from 'node:crypto';

export const BASE = process.env.ECO_JUEGO_API ?? 'https://g5ab3c4722e812d-ecoaccess.adb.sa-bogota-1.oraclecloudapps.com/ords/ecoaccess/juego/v1';
const privado = join(process.env.LOCALAPPDATA ?? '', 'EcoAccessP3', 'juego');
export const CLAVE_ADMIN = process.env.ECO_JUEGO_ADMIN ?? readFileSync(join(privado, 'clave-admin.txt'), 'utf8').trim();
/** IDs de las opciones correctas, leídos del script privado (no están en el repositorio). */
export const CORRECTAS = new Set((readFileSync(join(privado, 'cargar-clave-respuestas.sql'), 'utf8').match(/id IN \(([\d,\s]+)\)/)?.[1] ?? '')
  .split(',').map((x) => Number(x.trim())).filter(Boolean));

export const secreto = () => randomBytes(32).toString('base64url');
export const ORIGEN = 'https://jorgegutho21.github.io';

export const contadores = { limitadas: 0, reintentos: 0 };
/** Igual que el navegador: reintenta con espera exponencial si ORDS responde 429/502/503/504 (todas las operaciones son idempotentes). */
export async function llamar(ruta, { metodo = 'GET', cuerpo, sesion, admin, ronda, reintentos = 8 } = {}) {
  const url = new URL(BASE + ruta);
  if (ronda) url.searchParams.set('ronda', ronda);
  const t0 = performance.now();
  for (let intento = 0; ; intento++) {
    let r;
    try {
      r = await fetch(url, {
        method: metodo,
        headers: { Origin: ORIGEN, ...(cuerpo !== undefined ? { 'Content-Type': 'application/json' } : {}),
          ...(sesion ? { 'X-Eco-Sesion': sesion } : {}), ...(admin ? { 'X-Eco-Admin': admin } : {}) },
        body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
      });
    } catch (e) { r = { status: 599, text: async () => String(e) }; }
    const reintentable = [429, 502, 503, 504, 599].includes(r.status) && !(r.status === 429 && r.headers?.get?.('content-type')?.includes('json'));
    if (reintentable && intento < reintentos) {
      if (r.status === 429) contadores.limitadas++;
      contadores.reintentos++;
      await new Promise((ok) => setTimeout(ok, Math.min(8000, 400 * 2 ** intento) * (0.6 + Math.random() * 0.8)));
      continue;
    }
    const texto = await r.text();
    let json; try { json = JSON.parse(texto); } catch { json = { texto }; }
    return { status: r.status, json, ms: performance.now() - t0, texto };
  }
}

export const admin = (accion, cuerpo = {}) => llamar('/admin/' + accion, { metodo: 'POST', cuerpo, admin: CLAVE_ADMIN });

export async function crearRondaPrueba(nombre) {
  const c = await admin('crear', { nombre, prueba: true });
  if (c.status !== 200) throw new Error('No se pudo crear la ronda de prueba: ' + c.texto);
  const a = await admin('abrir', { competencia: c.json.id });
  if (a.status !== 200) throw new Error('No se pudo abrir la ronda de prueba: ' + a.texto);
  return { id: c.json.id, codigo: c.json.codigo };
}

export const eliminarRonda = (id) => admin('eliminar_prueba', { competencia: id });

/** Juega un intento completo. `aciertos` = cuántas preguntas responder bien (las primeras). */
export async function jugar(ronda, { nombre, alias, aciertos = 7, pausaMs = 0 }) {
  const sesion = secreto();
  const tiempos = [];
  const reg = await llamar('/registro', { metodo: 'POST', cuerpo: { nombre, alias, acepta: true, sesion, dispositivo: secreto(), ronda } });
  tiempos.push(reg.ms);
  if (reg.status !== 201) return { error: reg, tiempos };
  let e = await llamar('/intento/inicio', { metodo: 'POST', sesion }); tiempos.push(e.ms);
  for (let n = 1; n <= 7 && e.json.estado === 'EN_CURSO'; n++) {
    const p = e.json.pregunta;
    const buena = p.opciones.find((o) => CORRECTAS.has(o.id));
    const mala = p.opciones.find((o) => !CORRECTAS.has(o.id));
    if (pausaMs) await new Promise((r) => setTimeout(r, pausaMs));
    e = await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: p.orden, opcion: (n <= aciertos ? buena : mala).id } });
    tiempos.push(e.ms);
  }
  return { sesion, final: e, tiempos };
}
