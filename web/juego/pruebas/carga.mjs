// Prueba de concurrencia REAL contra Oracle Cloud: N participantes simultáneos en una ronda de PRUEBA que se elimina al
// terminar. Uso: node web/juego/pruebas/carga.mjs 50
import { crearRondaPrueba, eliminarRonda, jugar, llamar, admin, contadores } from './comun.mjs';

const N = Number(process.argv[2] ?? 50);
const PIENSA = (process.argv[3] ?? '300-1500').split('-').map(Number);   // ms de reflexión por pregunta (mín-máx)
const ESCALONADO = Number(process.argv[4] ?? 0);                         // ms entre llegadas de participantes
const ronda = await crearRondaPrueba(`Carga ${N} simultáneos`);
const pct = (a, p) => a[Math.min(a.length - 1, Math.floor(a.length * p))];
try {
  const t0 = performance.now();
  // Cada participante piensa entre 0,3 y 1,5 s por pregunta, como una persona real, y todos arrancan a la vez.
  const resultados = await Promise.all(Array.from({ length: N }, async (_, k) => {
    if (ESCALONADO) await new Promise((r) => setTimeout(r, k * ESCALONADO));
    return jugar(ronda.codigo, { nombre: `Carga Participante ${String.fromCharCode(65 + (k % 26))}${String.fromCharCode(65 + Math.floor(k / 26))}`,
      alias: `carga${k}`, aciertos: k % 8, pausaMs: PIENSA[0] + Math.floor(Math.random() * (PIENSA[1] - PIENSA[0])) });
  }));
  const total = performance.now() - t0;
  const tiempos = resultados.flatMap((r) => r.tiempos).sort((a, b) => a - b);
  const fallos = resultados.filter((r) => r.error || r.final?.json?.estado !== 'FINALIZADO');
  // Mientras tanto, la clasificación se consulta como lo harían los celulares y el proyector.
  const lecturas = await Promise.all(Array.from({ length: N }, () => llamar('/clasificacion', { ronda: ronda.codigo })));
  const ms = lecturas.map((l) => l.ms).sort((a, b) => a - b);
  const p = await admin('participantes', { competencia: ronda.id });
  const finalizados = p.json.participantes.filter((x) => x.estado === 'FINALIZADO').length;
  const puntajes = p.json.participantes.every((x) => x.puntaje === x.aciertos * 100 && x.aciertos + x.errores === 7);
  console.log(JSON.stringify({
    participantes: N, piensaMs: PIENSA.join('-'), escalonadoMs: ESCALONADO, respuestas429: contadores.limitadas, reintentos: contadores.reintentos, solicitudes: tiempos.length, fallos: fallos.length, finalizadosEnOracle: finalizados, puntajesCoherentes: puntajes,
    duracionTotalS: +(total / 1000).toFixed(1),
    latenciaMs: { p50: Math.round(pct(tiempos, 0.5)), p95: Math.round(pct(tiempos, 0.95)), p99: Math.round(pct(tiempos, 0.99)), max: Math.round(tiempos.at(-1)) },
    clasificacionMs: { p50: Math.round(pct(ms, 0.5)), p95: Math.round(pct(ms, 0.95)), max: Math.round(ms.at(-1)) },
    errores: fallos.slice(0, 3).map((f) => f.error?.json ?? f.final?.json),
  }, null, 2));
  if (fallos.length || finalizados !== N || !puntajes) process.exitCode = 1;
} finally {
  const e = await eliminarRonda(ronda.id);
  console.log('Ronda de prueba eliminada:', e.status === 200);
}
