// Conoce EcoAccess. Diseño: maqueta «Conoce EcoAccess» + regla .claude/skills/eco-diseno-web.
// Fuentes del contenido: docs/introduccion/00_presentacion.md, hardware/README.md y
// reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md (mediciones del 25/11/2026). Sin estadísticas ambientales:
// los bloques de la maqueta que afirmaban impacto se redactan como propósito, no como resultado.
import { sello, leyenda, movimientoReducido } from './comun.js';

export const titulo = 'Conoce EcoAccess';

const canecas = (c) => `<span class="mini"><img src="assets/canecas/${c}-cuerpo.webp" alt="" width="540" height="716" loading="lazy"><img src="assets/canecas/${c}-tapa.webp" alt="" width="540" height="716" loading="lazy"></span>`;
const ico = (d, extra = '') => `<svg viewBox="0 0 24 24" aria-hidden="true" ${extra}>${d}</svg>`;
const I = {
  casa: '<path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10v10h13V10"/>',
  hoja: '<path d="M5 19c0-8 5-13 15-14-1 10-6 15-14 15"/><path d="M5 19c3-4 6-7 10-9"/>',
  flecha: '<path d="m9 6 6 6-6 6"/>',
  play: '<circle cx="12" cy="12" r="9"/><path d="m10 8.5 5 3.5-5 3.5z"/>',
  alerta: '<path d="M12 3 2 20h20z"/><path d="M12 10v4M12 17h.01"/>',
  foco: '<path d="M9 18h6M10 21h4M12 3a6 6 0 0 0-3.5 10.9c.6.5 1 1.2 1 2V16h5v-.1c0-.8.4-1.5 1-2A6 6 0 0 0 12 3Z"/><path d="M12 0v1M4 4 3 3M20 4l1-1"/>',
  chispa: '<path d="M12 3v4M12 17v4M3 12h4M17 12h4M6 6l2.5 2.5M15.5 15.5 18 18M18 6l-2.5 2.5M8.5 15.5 6 18"/>',
  cerebro: '<path d="M9 4a3 3 0 0 0-3 3 3 3 0 0 0-2 5 3 3 0 0 0 2 5 3 3 0 0 0 3 3h1V4Z"/><path d="M15 4a3 3 0 0 1 3 3 3 3 0 0 1 2 5 3 3 0 0 1-2 5 3 3 0 0 1-3 3h-1V4Z"/><path d="M7 9h3M14 15h3M7 15h3M14 9h3"/>',
  barras: '<path d="M5 20v-5M10 20v-9M15 20v-12M20 20V5"/>',
  reciclar: '<path d="M7 19H4.5a1.5 1.5 0 0 1-1.3-2.3L5 13.5M11 19h8.5a1.5 1.5 0 0 0 1.3-2.3l-1.4-2.4M14 16l-3 3 3 3M8.6 10.5 11 6.3a1.5 1.5 0 0 1 2.6 0l1.6 2.8M5.3 9.6 8.6 10.5l.9-3.3M18.4 11.7l-3.2.9-.9-3.3"/>',
  base: '<ellipse cx="12" cy="6" rx="7" ry="3"/><path d="M5 6v6c0 1.7 3.1 3 7 3s7-1.3 7-3V6M5 12v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6"/>',
  personas: '<circle cx="9" cy="8" r="3"/><circle cx="17" cy="9" r="2.5"/><path d="M3 20c0-3.3 2.7-6 6-6s6 2.7 6 6M15 14.5c3 0 6 2 6 5.5"/>',
  mundo: '<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c2.5 2.7 3.5 5.7 3.5 9s-1 6.3-3.5 9c-2.5-2.7-3.5-5.7-3.5-9s1-6.3 3.5-9Z"/>',
  pausa: '<path d="M9 6v12M15 6v12"/>',
  ant: '<path d="m15 6-6 6 6 6"/>',
  cerrar: '<path d="M6 6l12 12M18 6 6 18"/>',
};

// «Ver en 1 minuto»: cinco ideas de 12 s cada una (60 s). Texto breve, verificado en las fuentes de esta sección.
const MINUTO = [
  { t: 'El problema', d: 'Al botar algo, muchas personas no saben en qué caneca va: lo aprovechable se mezcla con lo que no lo es.', i: I.alerta },
  { t: 'La solución', d: 'Una estación de tres canecas (verde, blanca y negra) te dice dónde va tu residuo y, frente a ella, abre la tapa correcta.', i: I.foco },
  { t: 'Cómo se usa', d: 'Escanea el QR de la estación, identifica tu residuo con Eco por foto, texto o voz, y deposítalo.', i: I.hoja },
  { t: 'Qué queda registrado', d: 'Cada operación se guarda en Oracle Cloud, sin depósitos ni puntos ficticios. Las reglas, no la IA, deciden si se abre.', i: I.base },
  { t: 'Pruébalo ahora', d: 'Identifica un residuo en el inicio o toca una caneca en EcoAccess Dos Clics.', i: I.chispa, fin: true },
];
const SEG = 12000;

export function render() {
  return `<div class="conoce">
  <section class="cn-hero" aria-labelledby="cnTitulo">
    <div class="cn-hero-texto">
      <nav aria-label="Ruta de navegación"><ol class="cn-migas">
        <li><a href="#/">${ico(I.casa)}Inicio</a></li><li aria-current="page">Conoce EcoAccess</li>
      </ol></nav>
      <h1 id="cnTitulo" tabindex="-1">Conoce EcoAccess</h1>
      <p class="cn-entrada">EcoAccess es un proyecto académico inteligente de la Universidad Popular del Cesar que orienta dónde depositar tus residuos y promueve un campus más limpio y sostenible.</p>
      <div class="hero-acciones">
        <a class="boton boton-principal" href="#/conoce" data-ir="c-objetivos">${ico(I.hoja)}Explorar el proyecto${ico(I.flecha, 'class="flecha"')}</a>
        <button class="boton boton-secundario" type="button" id="cnMinuto" aria-expanded="false" aria-controls="cnResumen">${ico(I.play)}Ver en 1 minuto</button>
      </div>
    </div>
    <figure class="cn-hero-arte">
      <picture>
        <source media="(max-width: 699px)" srcset="assets/portada/hero-movil.webp" width="700" height="310">
        <img src="assets/portada/hero.webp" width="827" height="310" alt="Eco presenta las tres canecas inteligentes de EcoAccess en el campus de la Universidad Popular del Cesar.">
      </picture>
    </figure>
  </section>

  <section class="cn-resumen" id="cnResumen" aria-label="EcoAccess en 1 minuto" hidden>
    <div class="cn-progreso" aria-hidden="true">${MINUTO.map(() => '<span><i></i></span>').join('')}</div>
    <div class="cn-diapositiva" aria-live="polite">
      <span class="icono-circulo" id="cnIcono" aria-hidden="true"></span>
      <div><p class="cn-paso" id="cnPaso"></p><h2 id="cnResTitulo"></h2><p id="cnResTexto"></p><div class="cn-fin" id="cnFin" hidden>
        <a class="boton boton-principal" href="#/">${ico(I.hoja)}Identificar un residuo</a>
        <a class="boton boton-secundario" href="./dos-clics/">Ir a Dos clics</a>
      </div></div>
    </div>
    <div class="cn-controles">
      <button type="button" class="cn-control" id="cnAnterior" aria-label="Idea anterior">${ico(I.ant)}</button>
      <button type="button" class="cn-control" id="cnPausa" aria-label="Pausar">${ico(I.pausa)}</button>
      <button type="button" class="cn-control" id="cnSiguiente" aria-label="Idea siguiente">${ico(I.flecha)}</button>
      <button type="button" class="cn-control" id="cnCerrar" aria-label="Cerrar el resumen">${ico(I.cerrar)}</button>
    </div>
  </section>

  <div class="cn-par">
    <section class="cn-tarjeta" aria-labelledby="c-problema">
      <div><h2 id="c-problema"><span class="icono-circulo" aria-hidden="true">${ico(I.alerta)}</span>El problema</h2>
      <p>En el momento de botar algo, muchas personas no saben en qué caneca va: se mezcla lo aprovechable con lo que no lo es y los residuos peligrosos terminan en recipientes comunes.</p></div>
      <img src="assets/conoce/problema.webp" alt="" width="162" height="114" loading="lazy">
    </section>
    <section class="cn-tarjeta" aria-labelledby="c-solucion">
      <div><h2 id="c-solucion"><span class="icono-circulo" aria-hidden="true">${ico(I.foco)}</span>La solución</h2>
      <p>EcoAccess orienta en tiempo real sobre dónde va cada residuo, mediante una estación inteligente de tres canecas que identifica, registra y gestiona los depósitos.</p></div>
      <img src="assets/conoce/solucion.webp" alt="" width="230" height="76" loading="lazy">
    </section>
  </div>

  <div class="cn-par cn-par-b">
    <section class="cn-diferente" aria-labelledby="c-diferente">
      <h2 id="c-diferente">${ico(I.chispa)}¿Qué hace diferente a EcoAccess?</h2>
      <div class="cn-diferente-cuerpo">
        <ul class="cn-rasgos">
          <li>${ico(I.cerebro)}<strong>Inteligente y guiado</strong><span>Te dice la caneca correcta.</span></li>
          <li>${ico(I.barras)}<strong>Con registro de datos</strong><span>Identifica y guarda cada depósito en Oracle.</span></li>
          <li>${ico(I.hoja)}<strong>Pensado para el campus</strong><span>Fomenta hábitos sostenibles en la UPC.</span></li>
        </ul>
        <img class="cn-eco" src="assets/conoce/eco-pulgar.webp" alt="" width="142" height="152" loading="lazy">
      </div>
    </section>
    <section class="cn-tarjeta cn-campus" aria-labelledby="c-campus">
      <h2 id="c-campus"><span class="icono-circulo" aria-hidden="true">${ico(I.barras)}</span>Cómo ayuda al campus</h2>
      <div class="cn-campus-cuerpo">
        <img src="assets/conoce/campus.webp" alt="" width="184" height="124" loading="lazy">
        <ul>
          <li>${ico(I.reciclar)}Orienta la separación justo al botar el residuo.</li>
          <li>${ico(I.base)}Deja un registro de cada depósito que se puede revisar.</li>
          <li>${ico(I.personas)}Involucra a toda la comunidad universitaria.</li>
          <li>${ico(I.mundo)}Busca un campus más limpio y sostenible.</li>
        </ul>
      </div>
    </section>
  </div>

  <p class="cn-aclaracion">EcoAccess no afirma que un residuo se recicló por haber pasado por el sistema ni publica cifras ambientales que el proyecto no haya medido. <span class="nota-fuente">Fuente: <code>docs/introduccion/00_presentacion.md</code></span></p>

  <section class="bloque cn-bloque" aria-labelledby="c-objetivos" id="c-objetivos-sec">
    <h2 id="c-objetivos" tabindex="-1">Objetivos</h2>
    <p><strong>General:</strong> construir un prototipo inteligente de orientación y registro de residuos con canecas físicas, Telegram, Windows Forms, Oracle y ESP32.</p>
    <ol>
      <li>Identificar el residuo (por foto, texto o voz) y orientar su disposición.</li>
      <li>Autorizar la apertura solo mediante reglas de negocio, nunca por decisión directa del modelo de IA.</li>
      <li>Registrar cada operación en Oracle sin depósitos ni puntos ficticios.</li>
      <li>Ofrecer al operador un registro asistido en la aplicación de escritorio.</li>
      <li>Diseñar para varias canecas sin reconstruir la solución.</li>
    </ol>
    <p>La primera versión tenía una caneca con dos servomotores en la misma tapa. La estación actual tiene tres canecas, con un servo por tapa: verde, blanca y negra.</p>
    <div class="trio" aria-hidden="true">${['verde', 'blanca', 'negra'].map(canecas).join('')}</div>
  </section>

  <section class="bloque cn-bloque" aria-labelledby="c-publico">
    <h2 id="c-publico">Para quién es</h2>
    <div class="rejilla">
      <div class="dato" style="--c:var(--caneca-verde)"><b>Comunidad</b><span>Estudiantes, docentes y visitantes que necesitan saber en qué caneca va su residuo.</span></div>
      <div class="dato" style="--c:var(--caneca-blanca-borde)"><b>Operarios</b><span>Quienes atienden el punto ecológico y registran depósitos de forma asistida.</span></div>
      <div class="dato" style="--c:var(--caneca-negra)"><b>Gestión ambiental</b><span>El equipo de la UPC que consulta los registros y estadísticas en Oracle.</span></div>
    </div>
  </section>

  <section class="bloque cn-bloque" aria-labelledby="c-canales">
    <h2 id="c-canales">Cinco formas de usarlo</h2>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Canal</th><th scope="col">Cómo identifica</th><th scope="col">¿Puede abrir una caneca?</th></tr></thead>
      <tbody>
        <tr><th scope="row"><a href="#/">Esta página web</a></th><td>Texto o voz (es-CO), con un catálogo local en el navegador</td><td>Solo con el QR presencial de la estación; sin él, te indica la caneca correcta sin abrirla</td></tr>
        <tr><th scope="row"><a href="./dos-clics/">EcoAccess Dos Clics</a></th><td>Tocar la caneca, o escribir o decir el residuo</td><td>Solo con el QR presencial de la estación; sin él, te indica la caneca correcta sin abrirla</td></tr>
        <tr><th scope="row"><a href="https://t.me/EcoAccessP3_JG_bot" target="_blank" rel="noopener">Bot de Telegram</a></th><td>Foto del residuo, analizada por un modelo local</td><td>Solo cuentas autorizadas ${sello('pendiente', 'Hoy hay 0 cuentas autorizadas')}</td></tr>
        <tr><th scope="row">Aplicación de escritorio (C#)</th><td>Foto o registro manual del operario</td><td>Sí, con sesión supervisada</td></tr>
        <tr><th scope="row">Panel local de canecas</th><td>No identifica: controla y calibra</td><td>Sí, solo desde el equipo de la estación</td></tr>
      </tbody>
    </table></div>
  </section>

  <section class="bloque cn-bloque" aria-labelledby="c-tec">
    <h2 id="c-tec">Tecnologías</h2>
    <ul>
      <li><strong>C# 9 sobre .NET Framework 4.8</strong>, Windows Forms y MSTest (<code>Directory.Build.props</code>, <code>EcoAccess.P3.sln</code>).</li>
      <li><strong>Oracle Autonomous Database</strong> en Oracle Cloud, con <code>Oracle.ManagedDataAccess</code>, PL/SQL y ORDS para el Challenge.</li>
      <li><strong>Telegram Bot API</strong> por HTTP.</li>
      <li><strong>Ollama</strong> con el modelo de visión <code>lfm3-vl</code>, ejecutado en el equipo de la estación.</li>
      <li><strong>ESP32</strong> programado con Arduino/C++, tres servos por PWM y conexión USB.</li>
      <li><strong>HTML, CSS y JavaScript</strong> sin dependencias ni compilación para esta página; GitHub Actions y GitHub Pages para publicarla.</li>
    </ul>
    <p><a href="#/tecnologia">Ver cómo encaja cada tecnología</a></p>
  </section>

  <section class="bloque cn-bloque" aria-labelledby="c-resultados">
    <h2 id="c-resultados">Resultados medidos</h2>
    <p>Un resumen. El detalle, con cada limitación, está en <a href="#/resultados">Resultados medidos</a>.</p>
    ${leyenda(['verificado', 'simulado', 'pendiente', 'error'])}
    <div class="rejilla">
      <div class="dato"><b>2589</b><span>Pruebas unitarias de C# correctas, 0 con error y 1 omitida (Release).</span>${sello('verificado')}</div>
      <div class="dato"><b>7 / 14 / 21 s</b><span>Ventana de apertura para 1, 2 o 3 canecas, idéntica en C#, panel, pasarela y web.</span>${sello('verificado')}</div>
      <div class="dato"><b>14/14</b><span>Combinaciones de registro de la estación contra un esquema Oracle aislado, sin hardware.</span>${sello('simulado')}</div>
      <div class="dato"><b>11 → 0</b><span>Aperturas incorrectas en 125 fotos, medidas sobre las mismas fotos con que se ajustaron las reglas: no hay conjunto ciego.</span>${sello('verificado', 'Medido, sin evaluación ciega')}</div>
      <div class="dato" style="--c:var(--peligro)"><b>≈ 2,8 s</b><span>Mediana del reconocimiento por foto (P95 ≈ 6,4 s), por encima de la meta de 1 a 2 s.</span>${sello('error')}</div>
      <div class="dato" style="--c:var(--alerta)"><b>28</b><span>Aperturas físicas por probar con la estación conectada (7 combinaciones por 4 canales).</span>${sello('pendiente')}</div>
      <div class="dato"><b>V033</b><span>Invitados y detalle por residuo: aplicada en Oracle de producción el 25/11/2026.</span>${sello('verificado')}</div>
    </div>
    <p class="nota-fuente">Fuentes: <code>reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md</code> §14 y <code>docs/informes/expo11/evaluacion_consolidada_20261125.md</code>. No se volvieron a ejecutar desde esta página.</p>
  </section>

  <section class="bloque cn-bloque prosa" aria-labelledby="c-limites">
    <h2 id="c-limites">Lo que EcoAccess no afirma</h2>
    <ul>
      <li>Las canecas no tienen sensores de posición ni de ingreso: el sistema sabe que <em>ordenó</em> abrir, no que la tapa se movió ni que el residuo entró.</li>
      <li>El reconocimiento por foto es una sugerencia con errores medidos; la decisión de abrir la toman reglas, no el modelo.</li>
      <li>Sin el QR de la estación, esta página no abre ninguna caneca: solo hay apertura física cuando la estación la confirma como comandada.</li>
    </ul>
  </section>
</div>`;
}

export function montar(raiz) {
  const $ = (id) => raiz.querySelector('#' + id);
  const limpiezas = [];

  // «Explorar el proyecto»: baja a los objetivos sin cambiar la ruta.
  raiz.querySelector('[data-ir]')?.addEventListener('click', (ev) => {
    ev.preventDefault();
    const destino = $(ev.currentTarget.dataset.ir);
    destino?.scrollIntoView({ behavior: movimientoReducido() ? 'auto' : 'smooth', block: 'start' });
    destino?.focus({ preventScroll: true });
  });

  // «Ver en 1 minuto»: cinco ideas de 12 s con barra de avance, pausa y navegación manual.
  const boton = $('cnMinuto'), panel = $('cnResumen');
  const barras = [...panel.querySelectorAll('.cn-progreso i')];
  let i = 0, inicio = 0, transcurrido = 0, pausado = false, cuadro = 0;
  const pintar = () => {
    const m = MINUTO[i];
    $('cnIcono').innerHTML = ico(m.i);
    $('cnPaso').textContent = `${i + 1} de ${MINUTO.length}`;
    $('cnResTitulo').textContent = m.t;
    $('cnResTexto').textContent = m.d;
    $('cnFin').hidden = !m.fin;
    barras.forEach((b, k) => b.style.transform = `scaleX(${k < i ? 1 : 0})`);
  };
  const ir = (n) => { i = Math.max(0, Math.min(MINUTO.length - 1, n)); transcurrido = 0; inicio = performance.now(); pintar(); };
  const avanzar = (t) => {
    if (!pausado) {
      const p = Math.min(1, (transcurrido + t - inicio) / SEG);
      barras[i].style.transform = `scaleX(${p})`;
      if (p >= 1) { if (i < MINUTO.length - 1) ir(i + 1); else { fijarPausa(true); } }
    }
    cuadro = requestAnimationFrame(avanzar);
  };
  const fijarPausa = (si) => {
    if (si && !pausado) transcurrido += performance.now() - inicio;
    if (!si && pausado) inicio = performance.now();
    pausado = si;
    const b = $('cnPausa');
    b.setAttribute('aria-label', si ? 'Reanudar' : 'Pausar');
    b.innerHTML = ico(si ? '<path d="m8 5 11 7-11 7z"/>' : I.pausa);
  };
  const abrir = () => {
    panel.hidden = false; boton.setAttribute('aria-expanded', 'true');
    ir(0); fijarPausa(movimientoReducido());   // con movimiento reducido no avanza solo
    cancelAnimationFrame(cuadro); cuadro = requestAnimationFrame(avanzar);
    panel.scrollIntoView({ behavior: movimientoReducido() ? 'auto' : 'smooth', block: 'nearest' });
    $('cnPausa').focus({ preventScroll: true });
  };
  const cerrar = () => { panel.hidden = true; boton.setAttribute('aria-expanded', 'false'); cancelAnimationFrame(cuadro); boton.focus(); };
  boton.addEventListener('click', () => (panel.hidden ? abrir() : cerrar()));
  $('cnCerrar').addEventListener('click', cerrar);
  $('cnPausa').addEventListener('click', () => fijarPausa(!pausado));
  $('cnAnterior').addEventListener('click', () => ir(i - 1));
  $('cnSiguiente').addEventListener('click', () => ir(i + 1));
  panel.addEventListener('keydown', (ev) => { if (ev.key === 'Escape') { ev.stopPropagation(); cerrar(); } });
  limpiezas.push(() => cancelAnimationFrame(cuadro));

  return () => limpiezas.forEach((f) => f());
}
