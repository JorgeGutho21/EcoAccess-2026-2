// Challenge. Diseño: maqueta «Pon a prueba tus conocimientos» + regla .claude/skills/eco-diseno-web.
// Solo enlaza al juego publicado en /juego/ (web/juego, mantenido aparte): no copia el juego.
// El «Top 5» de la maqueta tenía nombres y puntajes inventados: aquí se lee EN VIVO la clasificación pública del juego
// (GET /clasificacion de su API ORDS, la misma que usa /juego/clasificacion/), una sola vez al abrir y al pulsar
// «Actualizar». El juego no da ecopuntos ni abre canecas.
import { sello, escapar } from './comun.js';

export const titulo = 'EcoAccess Challenge';

const API = 'https://g5ab3c4722e812d-ecoaccess.adb.sa-bogota-1.oraclecloudapps.com/ords/ecoaccess/juego/v1';
const nf = new Intl.NumberFormat('es-CO');

const ico = (d, extra = '') => `<svg viewBox="0 0 24 24" aria-hidden="true" ${extra}>${d}</svg>`;
const I = {
  casa: '<path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10v10h13V10"/>',
  mando: '<rect x="2.5" y="6.5" width="19" height="11" rx="5.5"/><path d="M7.5 10v4M5.5 12h4"/><circle cx="15.5" cy="11" r=".6"/><circle cx="17.8" cy="13.2" r=".6"/>',
  flecha: '<path d="m9 6 6 6-6 6"/>',
  barras: '<path d="M5 20v-5M10 20v-9M15 20v-12M20 20V5"/>',
  hoja: '<path d="M5 19c0-8 5-13 15-14-1 10-6 15-14 15"/><path d="M5 19c3-4 6-7 10-9"/>',
  persona: '<circle cx="10" cy="8" r="4"/><path d="M3 21c0-3.9 3.1-7 7-7 1.4 0 2.7.4 3.8 1.1M18 15v6M15 18h6"/>',
  tablero: '<rect x="5" y="4" width="14" height="17" rx="2"/><path d="M9 4V2.8h6V4M9 13l2 2 4-4"/>',
  trofeo: '<path d="M8 21h8M12 17v4M7 4h10v5a5 5 0 0 1-10 0z"/><path d="M17 5h3v2a4 4 0 0 1-4 4M7 5H4v2a4 4 0 0 0 4 4"/>',
  check: '<path d="m5 12 5 5L20 7"/>',
  refrescar: '<path d="M20 11A8 8 0 0 0 5.6 6.4L4 8M4 4v4h4M4 13a8 8 0 0 0 14.4 4.6L20 16M20 20v-4h-4"/>',
};
const CORONA = '<svg class="ch-corona" viewBox="0 0 48 36" aria-hidden="true"><path d="M4 30 2 8l12 10 10-16 10 16 12-10-2 22z" fill="#FACC15" stroke="#CA8A04" stroke-width="2" stroke-linejoin="round"/><rect x="4" y="29" width="40" height="5" rx="2" fill="#EAB308"/><circle cx="24" cy="20" r="3" fill="#16A34A"/></svg>';

const PASOS = [
  [I.persona, '1. Regístrate', 'Crea tu alias y únete al juego.'],
  [I.tablero, '2. Responde', 'Contesta preguntas sobre residuos y sobre el proyecto.'],
  [I.trofeo, '3. Compite', 'Obtén tu puntaje y sube en la clasificación.'],
];

export function render() {
  return `<div class="challenge">
  <section class="cn-hero ch-hero" aria-labelledby="chTitulo">
    <div class="cn-hero-texto">
      <nav aria-label="Ruta de navegación"><ol class="cn-migas">
        <li><a href="#/">${ico(I.casa)}Inicio</a></li><li aria-current="page">Challenge EcoAccess</li>
      </ol></nav>
      <h1 id="chTitulo" tabindex="-1">Pon a prueba tus conocimientos</h1>
      <p class="cn-entrada">El juego oficial de EcoAccess: responde preguntas sobre residuos y sobre cómo se construyó el proyecto. ¡Aprende, compite y demuestra tu compromiso con un campus más sostenible!</p>
      <div class="hero-acciones">
        <a class="boton boton-principal" id="jugarChallenge" href="./juego/">${ico(I.mando)}Jugar ahora${ico(I.flecha, 'class="flecha"')}</a>
        <a class="boton boton-secundario" href="./juego/clasificacion/">${ico(I.barras)}Ver clasificación</a>
      </div>
    </div>
    <figure class="cn-hero-arte">
      <img src="assets/conoce/challenge-hero.webp" width="618" height="230" alt="Eco celebra junto a tarjetas de preguntas y un trofeo dorado frente a la Universidad Popular del Cesar.">
    </figure>
  </section>

  <section class="como ch-como" aria-labelledby="chComo">
    <h2 id="chComo">${ico(I.hoja)}<span>¿Cómo funciona el Challenge?</span></h2>
    <ol class="como-pasos ch-pasos">
      ${PASOS.map(([i, t, d]) => `<li><span class="icono-circulo" aria-hidden="true">${ico(i)}</span><span><strong>${t}</strong><span>${d}</span></span></li>`).join('')}
    </ol>
  </section>

  <div class="ch-fila">
    <section class="ch-top" aria-labelledby="chTop">
      <div class="ch-top-cab">
        ${CORONA}
        <div><h2 id="chTop">Top 5 del Challenge</h2><p id="chCompetencia">Clasificación en vivo</p></div>
        <button type="button" class="ch-actualizar" id="chActualizar" aria-label="Actualizar la clasificación">${ico(I.refrescar)}</button>
      </div>
      <ol class="ch-podio" id="chPodio" aria-live="polite"><li class="ch-cargando">Consultando la clasificación…</li></ol>
      <p class="ch-resumen" id="chResumen"></p>
    </section>

    <section class="ch-registro" aria-labelledby="chReg">
      <h2 id="chReg"><span class="icono-circulo" aria-hidden="true">${ico(I.persona)}</span>¡Regístrate y participa!</h2>
      <p>Crea tu alias, acepta el reto y compite con la comunidad UPC. El tiempo y el puntaje los calcula el servidor.</p>
      <a class="boton boton-principal ch-registrar" href="./juego/">${ico(I.persona)}Registrarme ahora${ico(I.flecha, 'class="flecha"')}</a>
      <p class="ch-continuar">¿Ya empezaste? Tu partida se guarda en este dispositivo: <a href="./juego/">continúa aquí</a>.</p>
    </section>

    <section class="ch-nivel" aria-labelledby="chNivel">
      <div>
        <h2 id="chNivel">¡Demuestra tu nivel EcoAccess!</h2>
        <ul>
          <li>${ico(I.check)}Pon a prueba lo que sabes</li>
          <li>${ico(I.check)}Compite con otros participantes</li>
          <li>${ico(I.check)}Aprende sobre residuos</li>
          <li>${ico(I.check)}Aparece en la clasificación</li>
        </ul>
      </div>
      <img src="assets/conoce/challenge-medalla.webp" alt="" width="122" height="183" loading="lazy">
    </section>
  </div>

  <section class="bloque cn-bloque prosa" aria-labelledby="r-como">
    <h2 id="r-como">Cómo funciona por dentro</h2>
    <ul>
      <li>La ronda publicada tiene 7 preguntas: 5 sencillas y 2 avanzadas, sobre Oracle y C#.</li>
      <li>La respuesta correcta nunca llega a tu navegador antes de responder; la revisión se abre cuando termina la competencia.</li>
      <li>El juego no da ecopuntos ni abre canecas: usa sus propias tablas en Oracle.</li>
      <li>Hay una ronda de 10 preguntas preparada (migraciones V034 y V035), probada en un esquema aislado y sin aplicar en producción para no alterar la ronda oficial. ${sello('pendiente', 'No publicada')}</li>
      <li>Con jugadores que llegan escalonados terminaron 100 de 100 partidas; con 100 que empiezan en el mismo segundo, 99 de 100. ${sello('verificado', '100/100 escalonados')} ${sello('error', '99/100 simultáneos')} <a href="#/resultados">Ver resultados</a>.</li>
    </ul>
    <p class="nota-fuente">El Challenge se publica en esta misma página, bajo <code>/juego/</code>. Fuente: <code>reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md</code> §4, §5 y §14.2.</p>
  </section>
</div>`;
}

export function montar(raiz) {
  const $ = (s) => raiz.querySelector(s);
  const podio = $('#chPodio'), resumen = $('#chResumen'), comp = $('#chCompetencia'), boton = $('#chActualizar');
  let vivo = true, ocupado = false;
  const medalla = (p) => (p <= 3 ? `<span class="ch-pos" data-p="${p}">${p}</span>` : `<span class="ch-pos">${p}</span>`);
  const segundos = (ms) => (Number(ms) > 0 ? `${(ms / 1000).toLocaleString('es-CO', { maximumFractionDigits: 1 })} s` : '');
  async function cargar() {
    if (ocupado) return;
    ocupado = true; boton.disabled = true;
    try {
      const r = await fetch(`${API}/clasificacion`, { cache: 'no-store', credentials: 'omit', signal: AbortSignal.timeout?.(12000) });
      if (!r.ok) throw new Error(r.status === 429 ? 'mucha gente' : 'sin respuesta');
      const d = await r.json();
      if (!vivo) return;
      const lista = Array.isArray(d.podio) ? d.podio.slice(0, 5) : [];
      comp.textContent = d.competencia?.nombre ? `${d.competencia.nombre} · ${d.competencia.definitiva ? 'resultado definitivo' : 'en vivo'}` : 'Clasificación en vivo';
      podio.innerHTML = lista.length
        ? lista.map((j) => `<li>${medalla(j.posicion)}<span class="ch-avatar" aria-hidden="true">${escapar(String(j.alias || '?').slice(0, 1).toUpperCase())}</span><span class="ch-alias">${escapar(j.alias)}</span><span class="ch-pts"><span><b>${nf.format(j.puntaje)}</b> pts</span><small>${j.aciertos} ${j.aciertos === 1 ? 'acierto' : 'aciertos'}${segundos(j.tiempoMs) ? ' · ' + segundos(j.tiempoMs) : ''}</small></span></li>`).join('')
        : '<li class="ch-cargando">Todavía nadie ha terminado una partida. ¡Puedes ser el primero!</li>';
      const n = (v, uno, varios) => `${nf.format(v ?? 0)} ${(v ?? 0) === 1 ? uno : varios}`;
      resumen.textContent = `${n(d.participantes, 'participante', 'participantes')} · ${n(d.finalizados, 'partida terminada', 'partidas terminadas')} · ${nf.format(d.enCurso ?? 0)} en curso. Actualizado a las ${new Date().toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit' })}.`;
    } catch (e) {
      if (!vivo) return;
      const local = !/github\.io$/.test(location.hostname);
      podio.innerHTML = `<li class="ch-cargando">${local ? 'La clasificación en vivo solo se carga en el sitio publicado.' : e.message === 'mucha gente' ? 'Hay mucha gente jugando: intenta actualizar en unos segundos.' : 'No se pudo consultar la clasificación ahora.'} <a href="./juego/clasificacion/">Abrir la clasificación</a></li>`;
      resumen.textContent = '';
    } finally {
      ocupado = false; boton.disabled = false;
    }
  }
  boton.addEventListener('click', cargar);
  cargar();
  return () => { vivo = false; };
}
