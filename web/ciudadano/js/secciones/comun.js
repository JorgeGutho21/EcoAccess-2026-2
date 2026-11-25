// Piezas compartidas de las secciones académicas: sellos de evidencia, cabecera, código citado y pestañas accesibles.
export const escapar = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));

const TEXTO_SELLO = {
  verificado: 'Verificado', simulado: 'Verificado por simulación', pendiente: 'Pendiente', propuesta: 'Propuesta',
  educativo: 'Demostración educativa', error: 'Limitación abierta',
};
/** Sello de evidencia: forma + palabra. */
export const sello = (e, texto = TEXTO_SELLO[e]) => `<span class="sello" data-e="${e}">${escapar(texto)}</span>`;

export const leyenda = (tipos = ['verificado', 'simulado', 'pendiente', 'error', 'educativo', 'propuesta']) =>
  `<ul class="leyenda" aria-label="Significado de los sellos">${tipos.map((t) => `<li>${sello(t)}</li>`).join('')}</ul>`;

/** Cabecera de página con migas de pan. `migas`: [[texto, href], …] sin incluir la página actual. */
export function cabecera({ titulo, entrada, migas = [] }) {
  const m = [['Inicio', '#/'], ...migas];
  return `<header class="pag-cabecera">
    <nav aria-label="Ruta de navegación"><ol class="migas">${m.map(([t, h]) => `<li><a href="${h}">${escapar(t)}</a></li>`).join('')}<li aria-current="page">${escapar(titulo)}</li></ol></nav>
    <h1 tabindex="-1">${escapar(titulo)}</h1>
    ${entrada ? `<p class="entrada">${entrada}</p>` : ''}
  </header>`;
}

/** Fragmento BREVE de código real, con el archivo de origen. Las líneas que empiezan con // o /// se atenúan. */
export function codigo(archivo, texto, lenguaje = 'C#') {
  const html = escapar(texto.replace(/^\n|\n\s*$/g, '')).split('\n')
    .map((l) => (/^\s*(\/\/|--)/.test(l) ? `<span class="c">${l}</span>` : l)).join('\n');
  return `<figure class="codigo"><figcaption><code>${escapar(archivo)}</code><span>${escapar(lenguaje)}, fragmento</span></figcaption><pre tabindex="0"><code>${html}</code></pre></figure>`;
}

/**
 * Pestañas accesibles (patrón WAI-ARIA): flechas, Inicio y Fin. `alCambiar(i)` se llama al elegir.
 * Espera dentro de `raiz` un [role=tablist] con [role=tab] y paneles [role=tabpanel] enlazados por aria-controls.
 */
export function pestanas(raiz, alCambiar) {
  const lista = raiz.querySelector('[role="tablist"]');
  const tabs = [...lista.querySelectorAll('[role="tab"]')];
  const elegir = (t, enfocar) => {
    tabs.forEach((x) => {
      const si = x === t;
      x.setAttribute('aria-selected', String(si)); x.tabIndex = si ? 0 : -1;
      const p = document.getElementById(x.getAttribute('aria-controls')); if (p) p.hidden = !si;
    });
    if (enfocar) t.focus();
    alCambiar?.(tabs.indexOf(t));
  };
  tabs.forEach((t, i) => {
    t.addEventListener('click', () => elegir(t));
    t.addEventListener('keydown', (ev) => {
      const k = { ArrowRight: i + 1, ArrowLeft: i - 1, Home: 0, End: tabs.length - 1 }[ev.key];
      if (k === undefined) return;
      ev.preventDefault(); elegir(tabs[(k + tabs.length) % tabs.length], true);
    });
  });
  elegir(tabs.find((t) => t.getAttribute('aria-selected') === 'true') ?? tabs[0]);
  return { elegir: (i) => elegir(tabs[i]) };
}

export const movimientoReducido = () => matchMedia('(prefers-reduced-motion: reduce)').matches;
