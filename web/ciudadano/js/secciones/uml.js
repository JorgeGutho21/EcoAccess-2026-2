// Mini-generador de diagramas UML en SVG (cajas y relaciones) para las secciones académicas.
import { escapar } from './comun.js';

export const ALTO_LINEA = 17;

/** Caja de clase o interfaz. `miembros`: líneas de texto. Devuelve { svg, x, y, w, h, cx, abajo, arriba }. */
export function caja(x, y, w, nombre, { estereotipo = '', miembros = [], tono = '' } = {}) {
  const cab = estereotipo ? 40 : 28;
  const h = cab + (miembros.length ? 8 + miembros.length * ALTO_LINEA : 0);
  const svg = `<g class="caja ${tono}">
    <rect x="${x}" y="${y}" width="${w}" height="${h}" rx="8"/>
    ${estereotipo ? `<text class="estereo" x="${x + w / 2}" y="${y + 16}" text-anchor="middle">«${escapar(estereotipo)}»</text>` : ''}
    <text class="nombre" x="${x + w / 2}" y="${y + cab - 9}" text-anchor="middle">${escapar(nombre)}</text>
    ${miembros.length ? `<line x1="${x}" x2="${x + w}" y1="${y + cab}" y2="${y + cab}"/>` : ''}
    ${miembros.map((m, i) => `<text class="miembro" x="${x + 10}" y="${y + cab + 4 + (i + 1) * ALTO_LINEA - 4}">${escapar(m)}</text>`).join('')}
  </g>`;
  return { svg, x, y, w, h, cx: x + w / 2, abajo: y + h, arriba: y };
}

/** Relación entre dos puntos. tipo: 'herencia' | 'realiza' | 'usa' | 'flujo'. */
export function relacion(x1, y1, x2, y2, tipo = 'usa', rotulo = '') {
  const marca = { herencia: 'url(#m-triangulo)', realiza: 'url(#m-triangulo)', usa: 'url(#m-flecha)', flujo: 'url(#m-flecha)' }[tipo];
  const mx = (x1 + x2) / 2, my = (y1 + y2) / 2;
  return `<path class="rel ${tipo}" d="M${x1} ${y1} L${x2} ${y2}" marker-end="${marca}"/>${rotulo ? `<text class="rotulo-rel" x="${mx + 6}" y="${my - 4}">${escapar(rotulo)}</text>` : ''}`;
}

/** Marcadores compartidos: se incluyen UNA vez por página (svg oculto) y los usan todos los diagramas. */
export const definiciones = `<svg width="0" height="0" style="position:absolute" aria-hidden="true" focusable="false"><defs>
  <marker id="m-triangulo" viewBox="0 0 12 12" refX="11" refY="6" markerWidth="12" markerHeight="12" orient="auto-start-reverse"><path d="M1 1 L11 6 L1 11 Z" class="m-hueco"/></marker>
  <marker id="m-flecha" viewBox="0 0 12 12" refX="10" refY="6" markerWidth="10" markerHeight="10" orient="auto-start-reverse"><path d="M1 1 L10 6 L1 11" class="m-abierta"/></marker>
</defs></svg>`;

let n = 0;
/** Envuelve el contenido en un SVG accesible, deslizable en pantallas angostas (min = ancho mínimo en px). */
export function lienzo(ancho, alto, tituloSvg, descripcion, contenido, min = 640) {
  const id = `uml${++n}`;
  return `<div class="desliza" tabindex="0" role="group" aria-label="${escapar(tituloSvg)}. Desliza para ver todo."><svg class="diagrama uml" viewBox="0 0 ${ancho} ${alto}" style="min-width:${min}px" role="img" aria-labelledby="${id}-t ${id}-d">
    <title id="${id}-t">${escapar(tituloSvg)}</title><desc id="${id}-d">${escapar(descripcion)}</desc>
    ${contenido}</svg></div>`;
}
