// Resultados. Diseño: maqueta «Resultados medidos» + regla .claude/skills/eco-diseno-web.
// ÚNICAS fuentes: reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md (§5, §14, §20, §22 y §23, 25–26/11/2026),
// reportes/vision35/eval_final_11_2026-11-25.md y docs/informes/expo11/evaluacion_consolidada_20261125.md.
// Las cifras de la maqueta (314,5 ms, 98,7 %, 1.842/1.865, 5/6, «v0.8–v1.0») no existen en el proyecto: se reemplazaron
// por mediciones reales con su sello. No hay estadísticas ambientales: el proyecto no las midió.
import { sello, leyenda, escapar } from './comun.js';

export const titulo = 'Resultados';

const GH = 'https://github.com/JorgeGutho21/EcoAccess-2026-2';
const INFORME = 'reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md';
const VISION = 'docs/informes/expo11/evaluacion_consolidada_20261125.md';
const VISION11 = 'reportes/vision35/eval_final_11_2026-11-25.md';
const nf = new Intl.NumberFormat('es-CO');

const ico = (d, extra = '') => `<svg viewBox="0 0 24 24" aria-hidden="true" ${extra}>${d}</svg>`;
const I = {
  casa: '<path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10v10h13V10"/>',
  codigo: '<path d="m8 8-4 4 4 4M16 8l4 4-4 4M14 5l-4 14"/>',
  web: '<circle cx="12" cy="12" r="9"/><path d="M3 12h18M12 3c2.5 2.7 3.5 5.7 3.5 9s-1 6.3-3.5 9c-2.5-2.7-3.5-5.7-3.5-9s1-6.3 3.5-9Z"/>',
  rayo: '<path d="M13 2 4 14h7l-1 8 9-12h-7z"/>',
  puerta: '<path d="M4 21h16M6 21V4a1 1 0 0 1 1-1h10a1 1 0 0 1 1 1v17"/><path d="M14 12h.01"/>',
  ojo: '<path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7S2 12 2 12z"/><circle cx="12" cy="12" r="3"/>',
  check: '<path d="m5 12 5 5L20 7"/>',
  alerta: '<path d="M12 3 2 20h20z"/><path d="M12 10v4M12 17h.01"/>',
  doc: '<path d="M14 3H6v18h12V7z"/><path d="M14 3v4h4M9 13h6M9 17h4"/>',
  carpeta: '<path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>',
  github: '<path d="M9 19c-4 1.2-4-2-6-2.5M15 21v-3.5c0-1 .1-1.4-.5-2 2.8-.3 5.5-1.4 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.3 4.3 0 0 0-.1-3.2s-1.1-.3-3.5 1.3a12 12 0 0 0-6.2 0C6.5 2.8 5.4 3.1 5.4 3.1a4.3 4.3 0 0 0-.1 3.2A4.6 4.6 0 0 0 4 9.5c0 4.6 2.7 5.7 5.5 6-.6.6-.6 1.2-.5 2V21"/>',
  ir: '<path d="M7 17 17 7M9 7h8v8"/>',
  engranaje: '<circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.8l.1.1a2 2 0 1 1-2.8 2.8l-.1-.1a1.7 1.7 0 0 0-1.8-.3 1.7 1.7 0 0 0-1 1.5V21a2 2 0 1 1-4 0v-.1a1.7 1.7 0 0 0-1.1-1.5 1.7 1.7 0 0 0-1.8.3l-.1.1a2 2 0 1 1-2.8-2.8l.1-.1a1.7 1.7 0 0 0 .3-1.8 1.7 1.7 0 0 0-1.5-1H3a2 2 0 1 1 0-4h.1a1.7 1.7 0 0 0 1.5-1.1 1.7 1.7 0 0 0-.3-1.8l-.1-.1a2 2 0 1 1 2.8-2.8l.1.1a1.7 1.7 0 0 0 1.8.3H9a1.7 1.7 0 0 0 1-1.5V3a2 2 0 1 1 4 0v.1a1.7 1.7 0 0 0 1 1.5 1.7 1.7 0 0 0 1.8-.3l.1-.1a2 2 0 1 1 2.8 2.8l-.1.1a1.7 1.7 0 0 0-.3 1.8V9a1.7 1.7 0 0 0 1.5 1H21a2 2 0 1 1 0 4h-.1a1.7 1.7 0 0 0-1.5 1z"/>',
  calendario: '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
  matraz: '<path d="M9 3h6M10 3v6L4.5 18.5A1.7 1.7 0 0 0 6 21h12a1.7 1.7 0 0 0 1.5-2.5L14 9V3"/><path d="M7 15h10"/>',
  caja: '<path d="m12 3 8 4.5v9L12 21l-8-4.5v-9z"/><path d="m4 7.5 8 4.5 8-4.5M12 12v9"/>',
  jugador: '<rect x="2.5" y="6.5" width="19" height="11" rx="5.5"/><path d="M7.5 10v4M5.5 12h4"/><circle cx="15.5" cy="11" r=".6"/><circle cx="17.8" cy="13.2" r=".6"/>',
};

// ── Indicadores de cabecera (todos medidos) ──
const KPI = [
  { i: I.codigo, t: 'Pruebas unitarias', v: nf.format(2648), s: 'C# (Release), 0 con error', e: 'verificado', f: `${INFORME} §23` },
  { i: I.web, t: 'Pruebas web', v: '15/15 · 27/27 · 14/14', s: 'Motor, interfaz e integración', e: 'verificado', f: `${INFORME} §14.5 y §23` },
  { i: I.rayo, t: 'Orden web → estación', v: '199–216 ms', s: 'Por Internet hasta el controlador (3/3)', e: 'verificado', f: `${INFORME} §22` },
  { i: I.puerta, t: 'Aperturas físicas', v: '15 / 28', s: 'Combinaciones × canales confirmadas en la estación', e: 'verificado', f: `${INFORME} §22` },
  { i: I.ojo, t: 'Falsas aperturas por foto', v: '0 / 72', s: '11 residuos del catálogo, sin conjunto ciego', e: 'verificado', f: `${VISION11}` },
];

// Pruebas web por componente (§23 y §15).
const WEB = [['Motor de residuos', 15, 15], ['Interfaz (navegador real)', 27, 27], ['Integración (web → pasarela → panel)', 14, 14], ['EcoAccess Dos Clics', 27, 27], ['Challenge: interfaz', 19, 19], ['Challenge: reintentos', 4, 4]];

/** Concurrencia del juego por HTTP contra ORDS de producción (§5 B). */
const JUEGO = [
  ['10 simultáneos', 10, 10, 0, 'verificado', 'Verificado'],
  ['25 simultáneos', 25, 25, 60, 'verificado', 'Verificado, con reintentos'],
  ['50 simultáneos', 50, 50, 332, 'verificado', 'Verificado, con espera alta'],
  ['100 en el mismo instante', 99, 100, 1218, 'error', 'Capacidad no demostrada'],
  ['100 escalonados', 100, 100, 2, 'verificado', 'Verificado'],
];

/** Visión: aperturas incorrectas antes → después, por bloque (evaluación consolidada, §3). */
const VISION_BLOQUES = [
  { n: 'Fotos de ajuste', d: '25 reales, 9 negativas y 10 escenas compuestas', casos: 44, antes: 2, despues: 0 },
  { n: 'Fotos externas con licencia', d: '15 cáscaras de banano, 53 materiales fuera de catálogo y 13 negativas', casos: 81, antes: 9, despues: 0 },
];

/** Migraciones Oracle (§2 y §14.2). */
const MIGRACIONES = [
  ['V030', 'Bitácora OPERACION_ESTACION de cada apertura', 'verificado', 'En producción'],
  ['V031', 'Tablas del EcoAccess Challenge', 'verificado', 'En producción'],
  ['V032', 'Paquete del juego y su API por ORDS', 'verificado', 'En producción'],
  ['V033', 'Invitados y detalle por residuo. Aplicada el 25/11/2026 con respaldo previo; 0 objetos inválidos', 'verificado', 'En producción desde el 25/11/2026'],
  ['V034', 'Bancos de preguntas para la ronda de 10', 'simulado', 'Probada en esquema aislado, no aplicada'],
  ['V035', 'Paquete del juego con bancos', 'simulado', 'Probada en esquema aislado, no aplicada'],
];

// Aperturas físicas confirmadas por la persona en la estación (§22 y §23, 26/11/2026). Las 28 combinaciones también
// están cubiertas por simulación (MatrizCombinacionesCanalesTests).
const CANALES = ['Controlador', 'App (P3)', 'Web / Dos Clics', 'Telegram'];
const COMBINACIONES = [['verde'], ['blanca'], ['negra'], ['verde', 'blanca'], ['verde', 'negra'], ['blanca', 'negra'], ['verde', 'blanca', 'negra']];
const VERIFICADA = (comb, canal) => canal === 0 || (comb.length === 1 && (canal === 1 || canal === 2 || (canal === 3 && comb[0] !== 'negra')));
const NOMBRE = { verde: 'Verde', blanca: 'Blanca', negra: 'Negra' };
const SEGUNDOS = { 1: 7, 2: 14, 3: 21 };
const FISICAS_OK = COMBINACIONES.reduce((s, comb) => s + CANALES.filter((_, k) => VERIFICADA(comb, k)).length, 0);

const FOTOS = [['serum', 'Frasco de sérum'], ['aluminio', 'Papel aluminio'], ['cuaderno', 'Hoja de cuaderno'], ['servilleta', 'Servilleta usada'], ['limon', 'Limón']];
const fuente = (texto) => `<p class="nota-fuente">Fuente: <code>${escapar(texto)}</code></p>`;
const externo = (href, contenido) => `<a href="${href}" target="_blank" rel="noopener">${contenido}<span class="sr"> (abre GitHub en otra pestaña)</span></a>`;

function latencia(ancho = 660) {
  // Escala 0–7 s. Meta 1–2 s; reconocimiento de los 11 residuos: mediana 2,4 s y P95 5,5 s (§20).
  const x = (s) => 24 + (s / 7) * (ancho - 48);
  const marcas = [0, 1, 2, 3, 4, 5, 6, 7].map((s) => `<line class="tic" x1="${x(s)}" x2="${x(s)}" y1="74" y2="80"/><text class="eje-t" x="${x(s)}" y="96" text-anchor="middle">${s} s</text>`).join('');
  return `<svg class="latencia" viewBox="0 0 ${ancho} 104" role="img" aria-labelledby="lat-t lat-d">
    <title id="lat-t">Tiempo de reconocimiento por foto frente a la meta</title>
    <desc id="lat-d">La meta es de 1 a 2 segundos. La mediana medida es de 2,4 segundos y el percentil 95 de 5,5 segundos: ambos quedan fuera de la meta.</desc>
    <rect class="meta" x="${x(1)}" y="22" width="${x(2) - x(1)}" height="46" rx="6"/>
    <text class="meta-t" x="${(x(1) + x(2)) / 2}" y="16" text-anchor="middle">Meta 1–2 s</text>
    <line class="base" x1="${x(0)}" x2="${x(7)}" y1="74" y2="74"/>${marcas}
    <line class="rango" x1="${x(2.4)}" x2="${x(5.5)}" y1="45" y2="45"/>
    <circle class="mediana" cx="${x(2.4)}" cy="45" r="9"/><text class="valor" x="${x(2.4)}" y="68" text-anchor="middle">mediana 2,4 s</text>
    <rect class="p95" x="${x(5.5) - 7}" y="38" width="14" height="14" rx="2" transform="rotate(45 ${x(5.5)} 45)"/><text class="valor" x="${x(5.5)}" y="30" text-anchor="middle">P95 5,5 s</text>
  </svg>`;
}

function dona() {
  const total = COMBINACIONES.length * CANALES.length, p = FISICAS_OK / total, r = 52, c = 2 * Math.PI * r;
  return `<svg class="rs-dona" viewBox="0 0 140 140" role="img" aria-label="${FISICAS_OK} de ${total} aperturas físicas confirmadas en la estación">
    <circle cx="70" cy="70" r="${r}" fill="none" stroke="#E2E8F0" stroke-width="18"/>
    <circle cx="70" cy="70" r="${r}" fill="none" stroke="#16A34A" stroke-width="18" stroke-dasharray="${(c * p).toFixed(1)} ${c.toFixed(1)}" transform="rotate(-90 70 70)" stroke-linecap="butt"/>
    <text x="70" y="70" text-anchor="middle" class="rs-dona-num">${FISICAS_OK}/${total}</text>
    <text x="70" y="88" text-anchor="middle" class="rs-dona-t">confirmadas</text>
  </svg>`;
}

function matrizFisica() {
  return `<div class="desliza" tabindex="0" role="group" aria-label="Matriz de las 28 aperturas físicas por canal. Desliza para ver todo.">
    <table class="matriz-fisica">
      <caption class="sr">28 aperturas físicas: 7 combinaciones de canecas por 4 canales. ${FISICAS_OK} confirmadas en la estación; el resto, pendientes.</caption>
      <thead><tr><th scope="col">Combinación</th><th scope="col">Ventana</th>${CANALES.map((c) => `<th scope="col">${c}</th>`).join('')}</tr></thead>
      <tbody>${COMBINACIONES.map((comb) => `<tr>
        <th scope="row"><span class="mini-canecas" aria-hidden="true">${comb.map((c) => `<i data-c="${c}"></i>`).join('')}</span>${comb.map((c) => NOMBRE[c]).join(' + ')}</th>
        <td class="num">${SEGUNDOS[comb.length]} s</td>
        ${CANALES.map((_, k) => (VERIFICADA(comb, k) ? '<td><span class="casilla" data-e="verificado">Confirmada</span></td>' : '<td><span class="casilla" data-e="pendiente">Pendiente</span></td>')).join('')}
      </tr>`).join('')}</tbody>
    </table>
  </div>`;
}

export function render() {
  const maxJuego = Math.max(...JUEGO.map((j) => j[3]));
  return `<div class="resultados">
  <section class="cn-hero rs-hero" aria-labelledby="rsTitulo">
    <div class="cn-hero-texto">
      <nav aria-label="Ruta de navegación"><ol class="cn-migas">
        <li><a href="#/">${ico(I.casa)}Inicio</a></li><li aria-current="page">Resultados medidos</li>
      </ol></nav>
      <h1 id="rsTitulo" tabindex="-1">Resultados medidos</h1>
      <p class="cn-entrada">Lo que EcoAccess demostró el 25 y 26/11/2026 y lo que todavía no. Cada cifra sale del informe de integración o de la evaluación de visión, con su sello. No se publican cifras ambientales: el proyecto no las midió.</p>
      ${leyenda(['verificado', 'simulado', 'pendiente', 'error'])}
    </div>
    <figure class="cn-hero-arte">
      <img src="assets/conoce/resultados-hero.webp" width="515" height="172" alt="Eco muestra una tableta con una gráfica de barras frente a la Universidad Popular del Cesar.">
    </figure>
  </section>

  <div class="rs-tablero">
    <div class="rs-principal">
      <ul class="rs-kpis" aria-label="Indicadores principales">
        ${KPI.map((k) => `<li class="rs-kpi" data-e="${k.e}" title="Fuente: ${escapar(k.f)}">
          <span class="rs-kpi-cab">${ico(k.i)}<span>${k.t}</span><span class="rs-ok" title="${k.e === 'verificado' ? 'Verificado' : ''}">${ico(I.check)}</span></span>
          <b${k.v.length > 12 ? ' class="largo"' : ''}>${k.v}</b><span class="rs-kpi-sub">${k.s}</span>
        </li>`).join('')}
      </ul>
      <p class="nota-fuente rs-fuentes">Fuentes de los indicadores: <code>${INFORME}</code> §14.5, §22 y §23 · <code>${VISION11}</code></p>

      <div class="rs-fila rs-fila-2">
        <section class="rs-tarjeta" aria-labelledby="rsJuego">
          <h2 id="rsJuego">${ico(I.jugador)}EcoAccess Challenge con muchos jugadores</h2>
          <p class="rs-sub">Respuestas «429, demasiadas solicitudes» por escenario (partidas completas contra ORDS de producción). Toca una barra.</p>
          <div class="rs-barras" role="group" aria-label="Concurrencia del juego">
            ${JUEGO.map(([n, fin, tot, r429, e], k) => `<button type="button" class="rs-barra" data-k="${k}" data-e="${e}" style="--v:${Math.max(0.03, r429 / maxJuego).toFixed(3)}" aria-pressed="${k === 3}">
              <span class="rs-barra-valor">${nf.format(r429)}</span><i></i><span class="rs-barra-n">${n}</span>
            </button>`).join('')}
          </div>
          <p class="rs-detalle" id="rsJuegoDetalle" aria-live="polite"></p>
        </section>
        <section class="rs-tarjeta" aria-labelledby="rsWeb">
          <h2 id="rsWeb">${ico(I.web)}Pruebas web (integración)</h2>
          <p class="rs-sub">Casos por componente</p>
          <ul class="rs-progreso">${WEB.map(([n, a, t]) => `<li><span>${n}</span><b>${a}/${t}</b><i style="--v:${a / t}"></i></li>`).join('')}</ul>
        </section>
      </div>

      <div class="rs-fila rs-fila-3">
        <section class="rs-tarjeta" aria-labelledby="rsLat">
          <h2 id="rsLat">${ico(I.rayo)}Reconocimiento por foto</h2>
          <p class="rs-sub">Tiempo frente a la meta de 1–2 s ${sello('error', 'Fuera de la meta')}</p>
          <div class="desliza" tabindex="0" role="group" aria-label="Gráfica de tiempo de reconocimiento. Desliza para ver todo.">${latencia(380)}</div>
          <p class="rs-nota">Por Telegram, de la foto a la orden: 3,9–4,7 s (descarga 1,0–1,9 s + reconocimiento 2,1–6,1 s).</p>
        </section>
        <section class="rs-tarjeta" aria-labelledby="rsFis">
          <h2 id="rsFis">${ico(I.puerta)}Aperturas físicas confirmadas</h2>
          <div class="rs-dona-caja">
            ${dona()}
            <ul class="rs-dona-ley">
              <li><i data-e="verificado"></i>Confirmadas<b>${FISICAS_OK}</b></li>
              <li><i data-e="pendiente"></i>Pendientes<b>${28 - FISICAS_OK}</b></li>
              <li class="rs-dona-nota">Las 28 también pasan en simulación ${sello('simulado', '28/28')}</li>
            </ul>
          </div>
          <a class="boton boton-texto" href="#res-fisico" data-ir="res-fisico">Ver la matriz por canal</a>
        </section>
        <section class="rs-tarjeta" aria-labelledby="rsVis">
          <h2 id="rsVis">${ico(I.ojo)}Los 11 residuos del catálogo</h2>
          <p class="rs-sub">72 fotos, versión final de las reglas</p>
          <ul class="rs-lista">
            <li data-e="ok">${ico(I.check)}<span><b>0</b> falsas aperturas en 72 fotos</span></li>
            <li data-e="ok">${ico(I.check)}<span><b>22/22</b> fotos sin residuo, sin apertura</span></li>
            <li data-e="ok">${ico(I.check)}<span><b>13/15</b> fotos externas de banano con el material correcto</span></li>
            <li data-e="aviso">${ico(I.alerta)}<span>Servilleta y lulo (<b>0/2</b>) nunca abren solos: piden confirmación</span></li>
          </ul>
        </section>
      </div>
    </div>

    <aside class="rs-lateral" aria-label="Evidencia y contexto">
      <section class="rs-evidencia" aria-labelledby="rsEvid">
        <div class="rs-evidencia-cab"><h2 id="rsEvid">${ico(I.doc)}Evidencia y fuentes</h2><p>Informes y archivos del repositorio que respaldan estas cifras.</p></div>
        <ul>
          ${[[I.doc, 'Informe final de integración', INFORME, `${GH}/blob/main/${INFORME}`],
             [I.doc, 'Evaluación de visión (consolidada)', VISION, `${GH}/blob/main/${VISION}`],
             [I.doc, 'Reconocimiento de los 11 residuos', VISION11, `${GH}/blob/main/${VISION11}`],
             [I.carpeta, 'Pruebas web (código)', 'web/ciudadano/pruebas', `${GH}/tree/main/web/ciudadano/pruebas`]]
            .map(([i, t, p, h]) => `<li>${externo(h, `<span class="rs-ev-ico">${ico(i)}</span><span class="rs-ev-txt"><b>${t}</b><small>${escapar(p)}</small></span>${ico(I.ir, 'class="rs-ev-ir"')}`)}</li>`).join('')}
        </ul>
        ${externo(GH, `${ico(I.github)}Ver todo el repositorio${ico(I.ir)}`).replace('<a ', '<a class="boton boton-secundario rs-repo" ')}
      </section>
      <section class="rs-contexto" aria-labelledby="rsCtx">
        <h2 id="rsCtx">${ico(I.engranaje)}Contexto de medición</h2>
        <ul>
          <li>${ico(I.calendario)}<span><b>Fechas:</b> 25 y 26/11/2026</span></li>
          <li>${ico(I.matraz)}<span><b>Entorno:</b> estación física con ESP32, Oracle Cloud de producción y esquemas aislados para lo simulado</span></li>
          <li>${ico(I.caja)}<span><b>Versión publicada:</b> <span id="rsVersion">consultando…</span></span></li>
        </ul>
      </section>
    </aside>
  </div>

  <section class="bloque cn-bloque" aria-labelledby="res-vision">
    <h2 id="res-vision">Reconocimiento por foto: evaluación ampliada</h2>
    <p>El modelo lfm3-vl se evaluó con <strong>125 imágenes</strong>: 44 propias del proyecto y <strong>81 externas</strong> con licencia libre verificada (CC0, CC BY y CC BY-SA de Wikimedia Commons). Una apertura incorrecta es abrir sin preguntar una caneca que no corresponde.</p>
    <div class="lamina antes-despues">
      <div class="ad-cifra">
        <span class="ad-num" aria-hidden="true"><s>11</s><svg class="ad-flecha" viewBox="0 0 40 24"><path d="M2 12h34M26 3l10 9-10 9"/></svg>0</span>
        <p class="sr">Aperturas incorrectas: de 11 a 0.</p>
        <p class="ad-rotulo">aperturas incorrectas en las 125 imágenes, antes y después de las reglas nuevas ${sello('verificado', 'Medido')}</p>
      </div>
      <div class="ad-bloques">
        ${VISION_BLOQUES.map((b) => `<div class="ad-fila">
          <div class="ad-texto"><b>${escapar(b.n)}</b><span>${escapar(b.d)} · ${b.casos} imágenes</span></div>
          <div class="ad-barras" role="img" aria-label="${escapar(b.n)}: ${b.antes} aperturas incorrectas antes, ${b.despues} después">
            <span class="ad-barra antes" style="--v:${b.antes / 9}"><i></i><em>${b.antes} antes</em></span>
            <span class="ad-barra despues" style="--v:${b.despues / 9}"><i></i><em>${b.despues} después</em></span>
          </div>
        </div>`).join('')}
      </div>
    </div>
    <div class="advertencia" role="note">
      <h3>Por qué ese cero no es una garantía</h3>
      <ul>
        <li>El 0 se midió sobre las mismas fotos que se usaron para escribir las reglas: <strong>ya no hay un conjunto ciego</strong>. ${sello('pendiente', 'Evaluación ciega pendiente')}</li>
        <li>El riesgo pasó a la confirmación humana: los casos «por confirmar» subieron de 17 a 38, y 16 de esos 38 sugieren una caneca equivocada. ${sello('error')}</li>
        <li>La servilleta y el lulo siempre piden confirmación. El catálogo operativo sigue en 11 materiales.</li>
      </ul>
    </div>
    <figure class="fotos-ajuste">
      <ul>${FOTOS.map(([f, n]) => `<li><img src="assets/fotos/${f}.webp" alt="${escapar(n)} sostenido en la mano, foto real del proyecto" width="320" height="320" loading="lazy"><span>${escapar(n)}</span></li>`).join('')}</ul>
      <figcaption>Algunas de las 25 fotos reales de ajuste. Con ellas se diseñó el sistema, por eso acertarlas no mide cómo funciona con fotos nuevas.</figcaption>
    </figure>
    ${fuente(`${VISION} §3 · ${VISION11} · ${INFORME} §20`)}
  </section>

  <section class="bloque cn-bloque" aria-labelledby="res-juego">
    <h2 id="res-juego">EcoAccess Challenge con muchos jugadores</h2>
    <p>Partidas completas por HTTP contra el servicio ORDS de producción, en rondas de prueba que se crearon y eliminaron. El navegador reintenta cuando ORDS responde «429, demasiadas solicitudes».</p>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Escenario</th><th scope="col">Partidas terminadas</th><th scope="col">Respuestas 429</th><th scope="col">Estado</th></tr></thead>
      <tbody>${JUEGO.map(([n, fin, tot, r429, e, et]) => `<tr><th scope="row">${escapar(n)}</th><td class="num">${fin}/${tot}</td><td class="num">${r429}</td><td>${sello(e, et)}</td></tr>`).join('')}</tbody>
    </table></div>
    <p>Con 100 personas que registran e inician en el mismo segundo, una de cada cien agotó los reintentos de la prueba: <strong>no se declara capacidad para 100 simultáneos</strong>. Con llegadas escalonadas, que es lo normal con un QR en un stand, terminaron las 100. ${sello('verificado', 'Base aislada: 100/100')}</p>
    ${fuente(`${INFORME} §5`)}
  </section>

  <section class="bloque cn-bloque" aria-labelledby="res-oracle">
    <h2 id="res-oracle">Oracle en producción</h2>
    <ol class="migraciones">${MIGRACIONES.map(([v, d, e, et]) => `<li data-e="${e}"><b>${v}</b><span>${escapar(d)}</span>${sello(e, et)}</li>`).join('')}</ol>
    <p>ORDS publica un único módulo (<code>eco.juego.v1</code>), ninguna tabla queda expuesta por AutoREST y CORS solo admite la página publicada. ${sello('verificado')}</p>
    ${fuente(`${INFORME} §2, §3 y §14.2`)}
  </section>

  <section class="bloque cn-bloque" aria-labelledby="res-fisico" id="res-fisico-sec">
    <h2 id="res-fisico" tabindex="-1">Aperturas físicas por canal</h2>
    <p>Las canecas no tienen sensores: el sistema sabe que <em>ordenó</em> abrir, no que la tapa se movió. Por eso cada apertura física la confirmó una persona mirando la estación (26/11/2026, tras reparar el servo de la caneca negra). La ventana de 7, 14 o 21 s es idéntica en C#, panel, pasarela y web. ${sello('verificado', 'Ventana 7/14/21 s verificada')}</p>
    ${matrizFisica()}
    <ul class="pendientes">
      <li>${sello('pendiente')} Foto de la caneca negra desde el celular por Telegram, después de la reparación.</li>
      <li>${sello('pendiente')} Dictado por micrófono en español de Colombia con un micrófono real.</li>
      <li>${sello('pendiente')} Fotos reales nuevas para una evaluación ciega del reconocimiento.</li>
      <li>${sello('pendiente')} Instalación completa en los equipos de los otros integrantes y reinicio completo de Windows.</li>
      <li>${sello('simulado', 'Simulado')} Las 28 combinaciones por canal, en la suite de C# (<code>MatrizCombinacionesCanalesTests</code>).</li>
    </ul>
    ${fuente(`${INFORME} §15, §22 y §23`)}
  </section>

  <section class="bloque cn-bloque" aria-labelledby="res-instalacion">
    <h2 id="res-instalacion">Instalación en la estación</h2>
    <p>El instalador deja cuatro accesos directos en el escritorio. Abrirlos no mueve ninguna tapa hasta que el operario activa el control.</p>
    <ul class="accesos">
      <li><b>ECOACCESS P3</b><span>Aplicación de escritorio con Oracle, visión local, Telegram y tres canecas.</span></li>
      <li><b>ECOACCESS ESP32</b><span>Centro de control local de las canecas (solo en el equipo de la estación).</span></li>
      <li><b>ECOACCESS DOS CLICS</b><span>Interfaz ciudadana para tocar la caneca, escribir o hablar. <a href="./dos-clics/">Abrirla</a></span></li>
      <li><b>ECOACCESS WEB</b><span>Esta plataforma: consulta ciudadana, contenido académico y el Challenge.</span></li>
    </ul>
    <p>${sello('verificado', 'Definido en el instalador')} <code>scripts/instalacion/Crear-AccesosDirectos.ps1</code>. El Challenge vive en <a href="#/challenge">#/challenge</a>.</p>
  </section>
</div>`;
}

export function montar(raiz) {
  const $ = (s) => raiz.querySelector(s);
  // Barras del juego: detalle del escenario elegido.
  const barras = [...raiz.querySelectorAll('.rs-barra')];
  const pintar = (k) => {
    barras.forEach((b, j) => b.setAttribute('aria-pressed', String(j === k)));
    const [n, fin, tot, r429, , et] = JUEGO[k];
    $('#rsJuegoDetalle').innerHTML = `<b>${escapar(n)}:</b> ${fin}/${tot} partidas terminadas, ${nf.format(r429)} respuestas 429 · ${escapar(et)}.`;
  };
  barras.forEach((b, k) => b.addEventListener('click', () => pintar(k)));
  pintar(3);
  // «Ver la matriz por canal»: baja a la tabla sin cambiar la ruta.
  raiz.querySelector('[data-ir]')?.addEventListener('click', (ev) => {
    ev.preventDefault();
    const d = $('#res-fisico'); d?.scrollIntoView({ block: 'start' }); d?.focus({ preventScroll: true });
  });
  // Versión publicada: la misma que el pie (version.json lo escribe el workflow de Pages).
  fetch('./version.json', { cache: 'no-store' }).then((r) => (r.ok ? r.json() : null)).then((d) => {
    const sha = typeof d?.commit === 'string' ? d.commit.slice(0, 7) : '';
    $('#rsVersion').textContent = /^[0-9a-f]{7}$/i.test(sha) ? `${sha} (${String(d.fecha || '').slice(0, 10)})` : 'copia local (sin publicar)';
  }).catch(() => { $('#rsVersion').textContent = 'copia local (sin publicar)'; });
}
