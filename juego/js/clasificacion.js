// Clasificación pública en vivo. Consulta cada 10 s (5 s en modo proyector), solo con la pestaña visible y con un
// pequeño desfase aleatorio para que cien celulares no pregunten al mismo tiempo. Solo alias, nunca nombres.
import { llamar, alAvisar } from './api.js?v=20261125';

const $ = (id) => document.getElementById(id);
const proyector = new URLSearchParams(location.search).has('proyector');
if (proyector) document.body.classList.add('proyector');
const INTERVALO = proyector ? 5000 : 10000;
alAvisar((m) => { $('conexion').hidden = !m; $('conexion').textContent = m ?? ''; });

const tiempo = (ms) => { const s = ms / 1000; return s < 60 ? `${s.toFixed(1).replace('.', ',')} s` : `${Math.floor(s / 60)}:${String(Math.round(s % 60)).padStart(2, '0')} min`; };
let ultimo = '';

function pintar(d) {
  const c = d.competencia;
  $('sello').textContent = c.definitiva ? 'Resultados definitivos' : 'Provisional';
  $('sello').classList.toggle('definitivo', c.definitiva);
  $('resumen').textContent = c.definitiva
    ? `${c.nombre}. Concurso finalizado con ${d.finalizados} participantes.`
    : `${c.nombre}. ${d.finalizados} ${d.finalizados === 1 ? 'participante ha terminado' : 'participantes han terminado'}${d.enCurso ? ` y ${d.enCurso} ${d.enCurso === 1 ? 'está jugando' : 'están jugando'}` : ''}.`;
  const firma = JSON.stringify(d.podio);
  if (firma !== ultimo) {
    ultimo = firma;
    for (const pos of [1, 2, 3]) {
      const el = document.querySelector(`.puesto[data-pos="${pos}"]`);
      const p = d.podio.find((x) => x.posicion === pos);
      el.classList.toggle('vacio', !p);
      el.querySelector('.alias').textContent = p ? p.alias : 'Por definir';
      el.querySelector('.pts').textContent = p ? `${p.puntaje} pts en ${tiempo(p.tiempoMs)}` : '';
      el.setAttribute('aria-label', p ? `Puesto ${pos}: ${p.alias}, ${p.puntaje} puntos, ${tiempo(p.tiempoMs)}` : `Puesto ${pos} por definir`);
      // Reinicia la animación del bloque cuando cambia el podio.
      const b = el.querySelector('.bloque'); b.style.animation = 'none'; void b.offsetWidth; b.style.animation = '';
    }
    const resto = d.podio.filter((x) => x.posicion > 3);
    $('siguientes').innerHTML = '';
    resto.forEach((p) => {
      const li = document.createElement('li');
      li.innerHTML = '<span class="num"></span><span class="alias"></span><span class="pts"></span><span class="t"></span>';
      li.children[0].textContent = `${p.posicion}.º`; li.children[1].textContent = p.alias;
      li.children[2].textContent = `${p.puntaje} pts`; li.children[3].textContent = tiempo(p.tiempoMs);
      $('siguientes').appendChild(li);
    });
  }
  $('vacia').hidden = d.podio.length > 0;
  $('actualizado').textContent = `Actualizado a las ${new Date().toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit', second: '2-digit' })}${c.definitiva ? '' : '. Se actualiza sola.'}`;
}

async function actualizar() {
  if (document.hidden) return;
  try { pintar(await llamar('/clasificacion', { paciencia: 20000 })); }
  catch (e) {
    $('resumen').textContent = e.codigo === 'sin_competencia' ? 'El concurso aún no ha comenzado.' : e.message;
  }
}
async function ciclo() {
  await actualizar();
  setTimeout(ciclo, INTERVALO + Math.random() * 1500);
}
document.addEventListener('visibilitychange', () => { if (!document.hidden) actualizar(); });
ciclo();
