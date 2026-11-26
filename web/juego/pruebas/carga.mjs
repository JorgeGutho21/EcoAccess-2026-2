// Prueba de concurrencia REAL contra Oracle Cloud: N participantes en una ronda de PRUEBA que se crea y se elimina al
// terminar (nunca toca la ronda oficial). Imita al navegador: misma política de reintentos (../js/reintentos.js), pausa
// aleatoria antes de registrar e iniciar, y pantallas de clasificación con intervalo adaptativo y retroceso ante 429.
// Uso: node web/juego/pruebas/carga.mjs N [piensa-ms mín-máx] [escalonado-ms] [banco]
//   N = participantes · piensa = reflexión por pregunta (300-1500) · escalonado = ms entre llegadas (0 = todos a la vez)
//   banco = código del banco de preguntas (vacío = 7 preguntas; «EXPO2026-10» solo cuando V034/V035 estén en producción)
import { crearRondaPrueba, eliminarRonda, jugar, llamar, admin, contadores } from './comun.mjs';
import { dormir } from '../js/reintentos.js';

const N = Number(process.argv[2] ?? 50);
const PIENSA = (process.argv[3] ?? '300-1500').split('-').map(Number);
const ESCALONADO = Number(process.argv[4] ?? 0);
const BANCO = process.argv[5] || undefined;
const pct = (a, p) => a.length ? a[Math.min(a.length - 1, Math.max(0, Math.ceil(a.length * p) - 1))] : 0;
const resumen = (a) => { const s = [...a].sort((x, y) => x - y); return { n: s.length, p50: Math.round(pct(s, 0.5)), p95: Math.round(pct(s, 0.95)), p99: Math.round(pct(s, 0.99)), max: Math.round(s.at(-1) ?? 0) }; };

const ronda = await crearRondaPrueba(`Carga ${N} ${ESCALONADO ? 'escalonados' : 'simultáneos'}`, BANCO);
const base = { ...contadores };   // la creación de la ronda no cuenta
try {
  // Pantallas de clasificación mientras se juega: 1 proyector (5 s) y un teléfono por cada 10 jugadores (10 s), con el
  // mismo intervalo adaptativo de js/clasificacion.js (×1,5 sin cambios hasta 20/30 s; ×2 ante fallo, hasta 2 min).
  let jugando = true;
  const lecturasClas = []; let fallosClas = 0;
  async function pantalla(minimo, maximo) {
    let intervalo = minimo, firma = '';
    await dormir(200 + Math.random() * 1000);
    while (jugando) {
      const r = await llamar('/clasificacion', { ronda: ronda.codigo, maxIntentos: 2, paciencia: 15000 });
      if (r.status === 200) {
        lecturasClas.push(r.ms);
        const f = JSON.stringify([r.json.podio, r.json.participantes, r.json.finalizados, r.json.enCurso]);
        intervalo = f !== firma ? minimo : Math.min(maximo, Math.round(intervalo * 1.5)); firma = f;
      } else { fallosClas++; intervalo = Math.min(120000, Math.max(minimo, intervalo) * 2); }
      const fin = performance.now() + intervalo * (0.8 + Math.random() * 0.4);
      while (jugando && performance.now() < fin) await dormir(250);
    }
  }
  const pantallas = [pantalla(5000, 20000), ...Array.from({ length: Math.ceil(N / 10) }, () => pantalla(10000, 30000))];

  const t0 = performance.now();
  const resultados = await Promise.all(Array.from({ length: N }, async (_, k) => {
    if (ESCALONADO) await dormir(k * ESCALONADO);
    return jugar(ronda.codigo, { nombre: `Carga Participante ${String.fromCharCode(65 + (k % 26))}${String.fromCharCode(65 + Math.floor(k / 26))}`,
      alias: `carga${k}`, aciertos: k % 8, escalonar: true, pausaMs: PIENSA[0] + Math.floor(Math.random() * (PIENSA[1] - PIENSA[0])) });
  }));
  const total = performance.now() - t0;
  jugando = false; await Promise.all(pantallas);

  const tiempos = resultados.flatMap((r) => r.tiempos);
  const fallos = resultados.filter((r) => r.error || r.final?.json?.estado !== 'FINALIZADO');
  const p = await admin('participantes', { competencia: ronda.id });
  const finalizados = p.json.participantes.filter((x) => x.estado === 'FINALIZADO').length;
  const preguntas = p.json.participantes[0] ? p.json.participantes[0].aciertos + p.json.participantes[0].errores : 0;
  const puntajes = p.json.participantes.every((x) => x.puntaje === x.aciertos * 100 && x.aciertos + x.errores === preguntas);
  const d = Object.fromEntries(Object.keys(contadores).map((k) => [k, contadores[k] - base[k]]));
  console.log(JSON.stringify({
    participantes: N, preguntas, piensaMs: PIENSA.join('-'), escalonadoMs: ESCALONADO, completos: N - fallos.length, fallos: fallos.length,
    finalizadosEnOracle: finalizados, puntajesCoherentes: puntajes,
    respuestas429: d.limitadas, limitesDelJuego429: d.limitesJuego, con429RetryAfter: d.retryAfter, con429Cors: d.corsEn429, sinRespuesta: d.sinRespuesta,
    reintentos: d.reintentos, agotadas: d.agotadas, solicitudesJuego: tiempos.length,
    duracionTotalS: +(total / 1000).toFixed(1),
    latenciaMs: resumen(tiempos),
    clasificacion: { pantallas: pantallas.length, lecturas: lecturasClas.length, fallos: fallosClas, ms: resumen(lecturasClas) },
    errores: fallos.slice(0, 3).map((f) => (f.error ? { status: f.error.status, cuerpo: String(f.error.texto).slice(0, 160) } : f.final?.json)),
  }, null, 2));
  if (fallos.length || finalizados !== N || !puntajes) process.exitCode = 1;
} finally {
  const e = await eliminarRonda(ronda.id);
  console.log('Ronda de prueba eliminada:', e.status === 200);
}
