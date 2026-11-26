// Portada pública (Inicio). Solo presentación e interacción: no clasifica ni abre canecas (eso es js/app.js).
// Regla de diseño: .claude/skills/eco-diseno-web. Las cifras son reales: catálogo publicado y historial de este
// dispositivo; nunca números de maqueta.
import * as estacion from './estacion.js';
import * as historial from './historial.js';

const $ = (id) => document.getElementById(id);
const portada = $('portada');
const movimientoReducido = () => matchMedia('(prefers-reduced-motion: reduce)').matches;
const formato = new Intl.NumberFormat('es-CO');
const NOMBRE = { verde: 'verde', blanca: 'blanca', negra: 'negra' };

// ── Vinculada a la estación (QR o acceso del anfitrión): la parte operativa va primero y la portada se oculta. ──
// js/app.js ya leyó el QR del fragmento antes que este módulo (los módulos se ejecutan en orden).
if (estacion.sesionActual()) {
  document.body.dataset.enlace = 'estacion';
  const p = $('pregunta');
  p?.setAttribute('aria-level', '1');   // sin portada, la pregunta es el título principal de la página
}

// ── Desplazamiento a «Identifica tu residuo» sin cambiar el fragmento de la URL ──
function irAIdentificar() {
  $('identifica')?.scrollIntoView({ behavior: movimientoReducido() ? 'auto' : 'smooth', block: 'start' });
  const campo = $('residuo');
  if (campo && !campo.disabled) setTimeout(() => campo.focus({ preventScroll: true }), movimientoReducido() ? 0 : 450);
}
portada?.addEventListener('click', (ev) => {
  const a = ev.target.closest('[data-desplazar]');
  if (!a) return;
  ev.preventDefault(); irAIdentificar();
});

// ── Cifras reales ──
function pintarHistorial() {
  const n = historial.leer().length;
  const c = $('cifraHistorial');
  if (c) c.textContent = formato.format(n);
}
let catalogo = null;
async function cargarCatalogo() {
  try {
    const r = await fetch('datos/catalogo-residuos.json', { cache: 'no-cache' });
    catalogo = await r.json();
  } catch { return; }
  const lista = Array.isArray(catalogo?.residuos) ? catalogo.residuos : [];
  $('cifraCatalogo').textContent = formato.format(lista.length);
  const por = { verde: 0, blanca: 0, negra: 0 };
  for (const r of lista) if (r.caneca in por) por[r.caneca]++;
  const max = Math.max(1, ...Object.values(por));
  const barras = $('barrasCatalogo');
  for (const [c, n] of Object.entries(por)) {
    const b = barras?.querySelector(`[data-caneca="${c}"]`);
    if (!b) continue;
    b.style.setProperty('--alto', (n / max).toFixed(3));
    b.title = `Caneca ${NOMBRE[c]}: ${n} residuos del catálogo`;
    b.querySelector('b').textContent = `${c[0].toUpperCase()}${c.slice(1)} · ${n}`;
  }
  barras?.setAttribute('aria-label', `Residuos del catálogo por caneca: verde ${por.verde}, blanca ${por.blanca}, negra ${por.negra}.`);
}
pintarHistorial();
cargarCatalogo();
addEventListener('storage', (e) => { if (e.key === 'ecoaccess.historial.v1') pintarHistorial(); });
addEventListener('focus', pintarHistorial);
// Las consultas hechas aquí mismo (texto, voz o atajos) actualizan la cifra cuando terminan de guardarse.
for (const id of ['formulario', 'atajos', 'microfono', 'resOpciones']) {
  $(id)?.addEventListener(id === 'formulario' ? 'submit' : 'click', () => setTimeout(pintarHistorial, 900));
}
$('borrarHistorial')?.addEventListener('click', () => setTimeout(pintarHistorial, 300));

// ── «Conoce nuestras canecas»: ejemplos reales del catálogo para la caneca elegida ──
const opciones = [...document.querySelectorAll('.caneca-opcion')];
const ejemplos = $('ejemplosCaneca');
function escapar(t) { return String(t).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c]); }
function mostrarEjemplos(c) {
  const actual = opciones.find((b) => b.getAttribute('aria-pressed') === 'true');
  const cerrar = actual?.dataset.caneca === c;
  opciones.forEach((b) => b.setAttribute('aria-pressed', String(!cerrar && b.dataset.caneca === c)));
  if (cerrar || !catalogo) { ejemplos.hidden = true; ejemplos.innerHTML = ''; return; }
  const info = catalogo.canecas?.[c] ?? {};
  const todos = catalogo.residuos.filter((r) => r.caneca === c);
  const muestra = todos.slice().sort(() => Math.random() - 0.5).slice(0, 6);
  ejemplos.dataset.caneca = c;
  ejemplos.innerHTML = `<p><strong>Caneca ${escapar(NOMBRE[c])}</strong> · ${escapar(info.categoria ?? '')} · ${todos.length} residuos en el catálogo</p>
    <ul>${muestra.map((r) => `<li>${escapar(r.nombre)}</li>`).join('')}</ul>
    <button type="button" class="boton boton-texto" data-identificar>¿Tienes otro? Identifícalo con Eco<svg class="flecha" viewBox="0 0 24 24" aria-hidden="true"><path d="M5 12h14M13 6l6 6-6 6"/></svg></button>`;
  ejemplos.hidden = false;
}
opciones.forEach((b) => b.addEventListener('click', () => mostrarEjemplos(b.dataset.caneca)));
ejemplos?.addEventListener('click', (ev) => { if (ev.target.closest('[data-identificar]')) irAIdentificar(); });

// ── Revelado suave al desplazarse (sin efecto con movimiento reducido: el CSS lo anula) ──
const revelables = portada ? [...portada.querySelectorAll('.como, .accesos > *, .cifras, .canecas-info')] : [];
if ('IntersectionObserver' in window && !movimientoReducido()) {
  revelables.forEach((n, i) => { n.classList.add('revelar'); n.style.setProperty('--retraso', `${(i % 4) * 60}ms`); });
  const io = new IntersectionObserver((entradas) => {
    for (const e of entradas) if (e.isIntersecting) { e.target.classList.add('visible'); io.unobserve(e.target); }
  }, { rootMargin: '0px 0px -8% 0px', threshold: 0.08 });
  revelables.forEach((n) => io.observe(n));
}
