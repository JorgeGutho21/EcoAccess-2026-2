// Clasificación pública en vivo. Solo alias, nunca nombres. Para no saturar ORDS cuando cien celulares la miran:
// - intervalo adaptativo: 10 s (5 s en modo proyector) mientras algo cambia; si nada cambia se alarga ×1,5 hasta 30 s
//   (20 s en el proyector) y vuelve al mínimo en cuanto cambia el podio o el número de participantes;
// - desfase aleatorio (±20 %) en cada consulta para que los teléfonos no pregunten al mismo tiempo;
// - pausa total con la pestaña oculta (visibilitychange) y reanudación al volver;
// - retroceso ante HTTP 429 u otro fallo: el intervalo se duplica (hasta 2 min) y la última clasificación sigue visible;
// - con la clasificación definitiva deja de consultar.
import { llamar, alAvisar } from './api.js?v=20261126';
import { crearAviso } from './aviso.js?v=20261126';

const $ = (id) => document.getElementById(id);
const proyector = new URLSearchParams(location.search).has('proyector');
if (proyector) document.body.classList.add('proyector');
const MINIMO = proyector ? 5000 : 10000;
const MAXIMO = proyector ? 20000 : 30000;
const MAXIMO_ERROR = 120000;
const avisar = crearAviso();
alAvisar(avisar);

const tiempo = (ms) => { const s = ms / 1000; return s < 60 ? `${s.toFixed(1).replace('.', ',')} s` : `${Math.floor(s / 60)}:${String(Math.round(s % 60)).padStart(2, '0')} min`; };
let ultimo = '';
let actividad = '';
let intervalo = MINIMO;
let temporizador = null;
let consultando = false;
let ultimaLectura = 0;
let definitiva = false;
let hayDatos = false;

/** Pinta y devuelve true si cambió algo (podio o participantes). */
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
  const nueva = `${firma}|${d.participantes}|${d.finalizados}|${d.enCurso}|${c.estado}`;
  const cambio = nueva !== actividad;
  actividad = nueva;
  return cambio;
}

function programar(ms) {
  clearTimeout(temporizador); temporizador = null;
  if (document.hidden || definitiva) return;
  temporizador = setTimeout(actualizar, Math.round(ms * (0.8 + Math.random() * 0.4)));
}

async function actualizar() {
  if (document.hidden || consultando || definitiva) return;
  consultando = true;
  try {
    // Pocos reintentos aquí: la propia consulta periódica es el reintento.
    const d = await llamar('/clasificacion', { paciencia: 15000, maxIntentos: 2 });
    ultimaLectura = Date.now(); hayDatos = true;
    intervalo = pintar(d) ? MINIMO : Math.min(MAXIMO, Math.round(intervalo * 1.5));
    definitiva = d.competencia.definitiva;
  } catch (e) {
    if (e.codigo === 'sin_competencia') {
      $('resumen').textContent = 'El concurso aún no ha comenzado.';
      intervalo = MAXIMO;
    } else {
      // Retroceso: ante 429 (muchos jugadores) u otro fallo se espera el doble, hasta 2 minutos.
      intervalo = Math.min(MAXIMO_ERROR, Math.max(MINIMO, intervalo) * 2);
      const aviso = `Hay mucha gente jugando: la clasificación se actualizará en unos ${Math.round(intervalo / 1000)} s.`;
      if (hayDatos) avisar(aviso); else $('resumen').textContent = aviso;
    }
  } finally {
    consultando = false;
    programar(intervalo);
  }
}

document.addEventListener('visibilitychange', () => {
  if (document.hidden) { clearTimeout(temporizador); temporizador = null; return; }
  // Al volver: si la última lectura ya es vieja, se consulta pronto (con un desfase breve); si no, se sigue el ritmo.
  programar(Date.now() - ultimaLectura >= MINIMO ? 300 + Math.random() * 1200 : intervalo);
});
// Primera consulta con un desfase breve: si el proyector y cien teléfonos abren a la vez, no llegan juntos.
programar(200 + Math.random() * 1000);
