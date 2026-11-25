// Enrutador de la plataforma. Rutas por hash (#/seccion): funcionan en GitHub Pages sin configuración del servidor.
// Cualquier otro fragmento (#residuo, o el #e=…&t=… del QR presencial de la estación) pertenece a la página de inicio:
// este módulo nunca lo lee ni lo modifica (de eso se encarga js/estacion.js).
const RUTAS = {
  conoce: 'conoce', construccion: 'construccion', tecnologia: 'tecnologia',
  'tecnologia/programacion': 'programacion', 'tecnologia/arquitectura': 'arquitectura', 'tecnologia/grafos': 'grafos',
  'tecnologia/hardware': 'hardware', 'tecnologia/datos': 'datos', 'tecnologia/ia': 'ia',
  'tecnologia/integraciones': 'integraciones', futuro: 'futuro', challenge: 'challenge',
};
const $ = (id) => document.getElementById(id);
const principal = $('principal');
const paginas = $('paginas');
const menu = $('menuPrincipal');
const botonMenu = $('abrirMenu');
const botonTec = $('botonTecnologia');
const tituloInicio = document.title;
let limpiar = null;
let primera = true;

function rutaActual() {
  const h = location.hash;
  if (!h.startsWith('#/')) return '';
  return decodeURIComponent(h.slice(2)).replace(/\/+$/, '').split('?')[0];
}

function marcarNavegacion(ruta) {
  document.querySelectorAll('.nav a[data-ruta]').forEach((a) => {
    const si = a.dataset.ruta === (ruta || 'inicio');
    if (si) a.setAttribute('aria-current', 'page'); else a.removeAttribute('aria-current');
  });
  botonTec.toggleAttribute('data-actual', ruta.startsWith('tecnologia'));
}

async function mostrar() {
  const ruta = rutaActual();
  cerrarMenu(); cerrarSubmenu();
  marcarNavegacion(ruta);
  limpiar?.(); limpiar = null;
  if (!ruta || ruta === 'inicio') {
    const veniaDeSeccion = document.body.dataset.vista === 'pagina';
    document.body.dataset.vista = 'inicio';
    paginas.hidden = true; paginas.innerHTML = ''; principal.hidden = false;
    document.title = tituloInicio;
    if (veniaDeSeccion) { const h = $('pregunta'); h.tabIndex = -1; h.focus({ preventScroll: true }); scrollTo(0, 0); }
    primera = false;
    return;
  }
  document.body.dataset.vista = 'pagina';
  principal.hidden = true; paginas.hidden = false; paginas.setAttribute('aria-busy', 'true');
  const nombre = RUTAS[ruta];
  try {
    if (!nombre) throw Object.assign(new Error('ruta'), { noEncontrada: true });
    const m = await import(`./secciones/${nombre}.js`);
    if (rutaActual() !== ruta) return;   // otra navegación llegó primero
    paginas.innerHTML = m.render();
    document.title = `${m.titulo} | EcoAccess`;
    limpiar = m.montar?.(paginas) ?? null;
  } catch (e) {
    paginas.innerHTML = e.noEncontrada
      ? '<header class="pag-cabecera"><h1 tabindex="-1">Esta sección no existe</h1><p class="entrada">Revisa el enlace o vuelve al <a href="#/">inicio</a> para usar EcoAccess.</p></header>'
      : '<header class="pag-cabecera"><h1 tabindex="-1">No se pudo cargar esta sección</h1><p class="entrada">Revisa tu conexión y vuelve a intentarlo. Mientras tanto puedes <a href="#/">usar EcoAccess</a>: la consulta de residuos funciona sin conexión.</p></header>';
    document.title = 'Sección no disponible | EcoAccess';
  }
  paginas.setAttribute('aria-busy', 'false');
  scrollTo(0, 0);
  // Al llegar por un enlace, el foco va al título de la sección (los lectores de pantalla anuncian el cambio).
  if (!primera) paginas.querySelector('h1')?.focus({ preventScroll: true });
  primera = false;
}

// ── Menú (móvil y tableta) ──
function abrirMenu() {
  const alto = document.querySelector('.cabecera').getBoundingClientRect().bottom;
  document.documentElement.style.setProperty('--alto-cabecera', Math.max(0, Math.round(alto)) + 'px');
  menu.setAttribute('data-abierto', ''); document.body.setAttribute('data-menu', '');
  botonMenu.setAttribute('aria-expanded', 'true'); $('menuTexto').textContent = 'Cerrar menú';
  menu.querySelector('a[aria-current="page"], a')?.focus();
}
function cerrarMenu(devolverFoco = false) {
  if (!menu.hasAttribute('data-abierto')) return;
  menu.removeAttribute('data-abierto'); document.body.removeAttribute('data-menu');
  botonMenu.setAttribute('aria-expanded', 'false'); $('menuTexto').textContent = 'Abrir menú';
  if (devolverFoco) botonMenu.focus();
}
botonMenu.addEventListener('click', () => (menu.hasAttribute('data-abierto') ? cerrarMenu() : abrirMenu()));

// ── Submenú «Tecnología» (escritorio) ──
function cerrarSubmenu(devolverFoco = false) {
  if (botonTec.getAttribute('aria-expanded') !== 'true') return;
  botonTec.setAttribute('aria-expanded', 'false');
  if (devolverFoco) botonTec.focus();
}
botonTec.addEventListener('click', () => {
  const abrir = botonTec.getAttribute('aria-expanded') !== 'true';
  botonTec.setAttribute('aria-expanded', String(abrir));
});
botonTec.closest('li').addEventListener('focusout', (ev) => { if (!ev.currentTarget.contains(ev.relatedTarget)) cerrarSubmenu(); });
document.addEventListener('click', (ev) => { if (!botonTec.closest('li').contains(ev.target)) cerrarSubmenu(); });

document.addEventListener('keydown', (ev) => {
  if (ev.key !== 'Escape') return;
  if (menu.hasAttribute('data-abierto')) { ev.stopPropagation(); cerrarMenu(true); }
  else if (botonTec.getAttribute('aria-expanded') === 'true') cerrarSubmenu(true);
}, true);
// Si se pasa a escritorio con el menú abierto, se cierra (la barra ya está visible).
matchMedia('(min-width: 1200px)').addEventListener('change', (e) => { if (e.matches) cerrarMenu(); });

// «Mi historial» dentro del menú abre el mismo diálogo del inicio.
$('historialMenu').addEventListener('click', () => { cerrarMenu(); $('abrirHistorial').click(); });

// Enlace para saltar: en el inicio va al buscador; en una sección, al título de la sección.
$('saltar').addEventListener('click', (ev) => {
  if (document.body.dataset.vista !== 'pagina') return;
  ev.preventDefault(); paginas.querySelector('h1')?.focus();
});

addEventListener('hashchange', mostrar);
mostrar();
