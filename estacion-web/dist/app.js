/* EcoAccess · Estación de canecas.
 * Un toque (o una frase) = una caneca abierta. El navegador solo habla con el servidor local; nunca con el ESP32.
 * Si no hay servidor (archivo abierto directamente), funciona en simulación dentro del navegador. */
'use strict';

const CANECAS = {
  blanca: { titulo: 'Aprovechables', nombre: 'Caneca blanca', tipo: 'aprovechables', imagen: 'assets/canecas/blanca.webp' },
  verde: { titulo: 'Orgánicos', nombre: 'Caneca verde', tipo: 'orgánicos', imagen: 'assets/canecas/verde.webp' },
  negra: { titulo: 'No aprovechables', nombre: 'Caneca negra', tipo: 'no aprovechables', imagen: 'assets/canecas/negra.webp' },
};
const COLORES = Object.keys(CANECAS);

// Residuos frecuentes en el campus. f = foto (se muestran en la cuadrícula); c = null → no va en estas canecas.
const RESIDUOS = [
  { n: 'Botella de plástico', c: 'blanca', f: 'botella-agua.jpg', p: 'botella botellas plastico pet agua gaseosa envase' },
  { n: 'Lata', c: 'blanca', f: 'lata-aluminio-1.jpg', p: 'lata latas aluminio gaseosa cerveza' },
  { n: 'Caja de jugo', c: 'blanca', f: 'caja-jugo.jpg', p: 'caja jugo tetrapak leche' },
  { n: 'Papel limpio', c: 'blanca', f: 'papel.jpg', p: 'papel hoja hojas cuaderno fotocopia periodico revista' },
  { n: 'Cartón', c: 'blanca', f: 'carton-limpio-1.jpg', p: 'carton caja cajas cartulina' },
  { n: 'Vidrio', c: 'blanca', f: 'vidrio-1.jpg', p: 'vidrio frasco botella de vidrio' },
  { n: 'Cáscara de banano', c: 'verde', f: 'banano.jpg', p: 'banano guineo platano cascara cascaras' },
  { n: 'Restos de comida', c: 'verde', f: 'restos-comida-1.jpg', p: 'comida restos almuerzo arroz sobras' },
  { n: 'Frutas', c: 'verde', f: 'naranja.jpg', p: 'fruta frutas naranja mandarina limon manzana mango' },
  { n: 'Hojas y poda', c: 'verde', f: 'hojas.jpg', p: 'hojas hoja seca pasto poda flores' },
  { n: 'Café y té', c: 'verde', f: 'cafe-te-1.jpg', p: 'cafe te borra bolsita aromatica' },
  { n: 'Servilletas', c: 'negra', f: 'servilletas.jpg', p: 'servilleta servilletas usadas sucias' },
  { n: 'Papel higiénico', c: 'negra', f: 'papel-higienico.jpg', p: 'papel higienico usado bano' },
  { n: 'Icopor', c: 'negra', f: 'plato-icopor.jpg', p: 'icopor plato poliestireno bandeja' },
  { n: 'Envolturas', c: 'negra', f: 'envolturas-1.jpg', p: 'envoltura envolturas paquete paquetes papas mecato empaque metalizado chito' },
  { n: 'Colillas', c: 'negra', f: 'colilla-1.jpg', p: 'colilla colillas cigarrillo' },
  { n: 'Vaso desechable', c: 'blanca', p: 'vaso vasos desechable plastico limpio' },
  { n: 'Cubiertos desechables', c: 'negra', p: 'cubiertos tenedor cuchara pitillo pitillos' },
  { n: 'Tapas plásticas', c: 'blanca', p: 'tapa tapas tapita' },
  { n: 'Pilas y baterías', c: null, p: 'pila pilas bateria baterias', nota: 'Las pilas no van en estas canecas. Llévalas a un punto de recolección de pilas.' },
  { n: 'Medicamentos', c: null, p: 'medicamento medicamentos pastillas jeringa jeringas', nota: 'Los medicamentos y jeringas van a un punto especial, no a estas canecas.' },
  { n: 'Celulares y aparatos', c: null, p: 'celular telefono cargador cable audifonos electronico computador', nota: 'Los aparatos electrónicos van a un punto de reciclaje especial.' },
];
const normalizar = (t) => t.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/[^a-z0-9ñ ]/g, ' ').replace(/\s+/g, ' ').trim();
for (const r of RESIDUOS) r.claves = normalizar(`${r.n} ${r.p}`).split(' ');

const $ = (s, el = document) => el.querySelector(s);
const $$ = (s, el = document) => [...el.querySelectorAll(s)];
const reducido = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

/* ── Sonido (WebAudio, sin archivos) ─────────────────────────────────────────────────────────────── */
let audio;
function tono(notas, tipo = 'sine', vol = 0.12) {
  try {
    audio = audio || new (window.AudioContext || window.webkitAudioContext)();
    let t = audio.currentTime;
    for (const [frecuencia, dur] of notas) {
      const o = audio.createOscillator(); const g = audio.createGain();
      o.type = tipo; o.frequency.value = frecuencia;
      g.gain.setValueAtTime(0.0001, t); g.gain.exponentialRampToValueAtTime(vol, t + 0.02); g.gain.exponentialRampToValueAtTime(0.0001, t + dur);
      o.connect(g).connect(audio.destination); o.start(t); o.stop(t + dur + 0.02); t += dur * 0.8;
    }
  } catch { /* sin audio */ }
}
const sonidoAbrir = () => tono([[523, .12], [659, .12], [784, .22]]);
const sonidoListo = () => tono([[784, .1], [988, .1], [1319, .3]], 'triangle', .1);
const sonidoError = () => tono([[330, .18], [262, .3]], 'square', .05);
const sonidoTic = () => tono([[880, .05]], 'sine', .05);

/* ── Voz que habla (síntesis) ────────────────────────────────────────────────────────────────────── */
let hablandoHasta = 0;
let vozElegida;
function elegirVoz() {
  const voces = speechSynthesis.getVoices().filter((v) => v.lang && v.lang.toLowerCase().startsWith('es'));
  const orden = ['es-co', 'es-mx', 'es-us', 'es-419', 'es-es'];
  voces.sort((a, b) => (orden.indexOf(a.lang.toLowerCase()) + 99) % 99 - (orden.indexOf(b.lang.toLowerCase()) + 99) % 99);
  vozElegida = voces[0];
}
if ('speechSynthesis' in window) { elegirVoz(); speechSynthesis.onvoiceschanged = elegirVoz; }
function decir(texto, { siempre = false } = {}) {
  $('#anunciador').textContent = texto;
  if (!('speechSynthesis' in window) || (!vozActiva && !siempre)) return;
  speechSynthesis.cancel();
  const u = new SpeechSynthesisUtterance(texto);
  u.lang = vozElegida?.lang || 'es-CO'; if (vozElegida) u.voice = vozElegida; u.rate = 1; u.pitch = 1.05;
  hablandoHasta = Date.now() + 60000;
  u.onend = u.onerror = () => { hablandoHasta = Date.now() + 700; };
  speechSynthesis.speak(u);
}

/* ── Conexión con la estación ────────────────────────────────────────────────────────────────────── */
let instantanea = null;
let desfase = 0; // reloj del servidor − reloj local
let local = null; // motor de simulación del navegador si no hay servidor

function pintarConexion(tipo, texto) {
  const p = $('#conexion');
  // La pastilla solo aparece si hay un problema de conexión; con la estación lista no se muestra nada.
  p.hidden = tipo !== 'error';
  p.className = 'pastilla pastilla-error';
  $('#conexionTexto').textContent = texto;
}
function recibir(datos) {
  desfase = datos.ahora - Date.now();
  const anterior = instantanea;
  instantanea = datos;
  document.documentElement.style.setProperty('--angulo', String(datos.angulo || 80));
  pintarConexion('ok', '');
  for (const color of COLORES) {
    const antes = anterior?.canecas[color]?.estado;
    const ahora = datos.canecas[color].estado;
    if (antes !== ahora) transicion(color, antes, ahora, datos.canecas[color]);
  }
  pintarCanecas();
}

function conectar() {
  if (location.protocol === 'file:' || !('EventSource' in window)) return iniciarLocal();
  const fuente = new EventSource('api/eventos');
  let abierto = false;
  const espera = setTimeout(() => { if (!abierto) { fuente.close(); iniciarLocal(); } }, 4000);
  fuente.onopen = () => { abierto = true; clearTimeout(espera); };
  fuente.onmessage = (e) => { abierto = true; clearTimeout(espera); recibir(JSON.parse(e.data)); };
  fuente.onerror = () => { if (abierto) pintarConexion('error', 'Reconectando…'); };
}

async function pedirApertura(color) {
  if (local) return local.abrir(color);
  try {
    const r = await fetch(`api/canecas/${color}/abrir`, { method: 'POST', headers: { 'X-EcoAccess': 'estacion' } });
    const cuerpo = await r.json().catch(() => ({}));
    if (!r.ok) return { ok: false, mensaje: cuerpo.mensaje || 'No se pudo abrir la caneca.', motivo: cuerpo.motivo };
    return cuerpo;
  } catch {
    pintarConexion('error', 'Sin conexión');
    return { ok: false, mensaje: 'No hay conexión con la estación.' };
  }
}

// Simulación en el navegador: mismas etapas y tiempos que el servidor (una tapa a la vez, cola corta).
function iniciarLocal() {
  const est = {}; for (const c of COLORES) est[c] = { estado: 'cerrada', angulo: 0, hasta: 0, mensaje: '' };
  const cola = []; let ocupada = null;
  const emitir = () => recibir({ modo: 'local', angulo: 80, abiertoMs: 7000, ocupada, cola: [...cola], canecas: JSON.parse(JSON.stringify(est)), ahora: Date.now() });
  const fijar = (c, e, extra = {}) => { Object.assign(est[c], { estado: e, hasta: 0, mensaje: '' }, extra); emitir(); };
  const esperar = (ms) => new Promise((r) => setTimeout(r, ms));
  async function procesar() {
    if (ocupada || !cola.length) return;
    const c = cola.shift(); ocupada = c;
    fijar(c, 'abriendo', { angulo: 80, hasta: Date.now() + 1200 }); await esperar(1200);
    fijar(c, 'abierta', { angulo: 80, hasta: Date.now() + 7000 }); await esperar(7000);
    fijar(c, 'cerrando', { angulo: 0, hasta: Date.now() + 1200 }); await esperar(1200);
    ocupada = null; fijar(c, 'cerrada', { angulo: 0 }); procesar();
  }
  local = {
    abrir(c) {
      if (ocupada === c || cola.includes(c)) return { ok: true, repetida: true };
      cola.push(c); if (ocupada) fijar(c, 'en_cola'); procesar(); return { ok: true };
    },
  };
  emitir();
  pintarConexion('ok', '');
}

/* ── Tarjetas ────────────────────────────────────────────────────────────────────────────────────── */
const TEXTO_ACCION = { cerrada: 'Abrir', en_cola: 'En espera', abriendo: 'Abriendo…', abierta: 'Abierta', cerrando: 'Cerrando…', fuera_servicio: 'Fuera de servicio', error: 'Sin conexión', rechazada: 'No disponible' };
const segundosRestantes = (c) => Math.max(0, Math.ceil((c.hasta - (Date.now() + desfase)) / 1000));

function pintarCanecas() {
  if (!instantanea) return;
  for (const boton of $$('.caneca')) {
    const color = boton.dataset.color;
    const c = instantanea.canecas[color];
    boton.dataset.estado = c.estado;
    $('.caneca-estado', boton).textContent = c.estado === 'cerrada' ? '' : c.estado === 'abierta' ? `Abierta · ${segundosRestantes(c)} s` : TEXTO_ACCION[c.estado] || '';
    boton.setAttribute('aria-label', `${CANECAS[color].nombre}, ${CANECAS[color].tipo}. ${c.estado === 'cerrada' ? 'Toca para abrir.' : TEXTO_ACCION[c.estado] || ''}`);
    boton.setAttribute('aria-disabled', String(c.estado === 'fuera_servicio'));
  }
}

function ondaToque(boton, evento) {
  if (reducido) return;
  const caja = boton.getBoundingClientRect();
  const lado = Math.max(caja.width, caja.height) * 2;
  const x = (evento?.clientX ?? caja.left + caja.width / 2) - caja.left - lado / 2;
  const y = (evento?.clientY ?? caja.top + caja.height / 2) - caja.top - lado / 2;
  const o = document.createElement('span');
  o.className = 'onda-toque'; Object.assign(o.style, { width: `${lado}px`, height: `${lado}px`, left: `${x}px`, top: `${y}px` });
  boton.appendChild(o); setTimeout(() => o.remove(), 750);
}

let enfocada = null; // caneca que esta pantalla está siguiendo en el momento de depósito
const ultimaOrden = {};
let residuoActual = null; // nombre del residuo que llevó a esta caneca (se muestra en el momento de depósito)
async function abrir(color, origen = 'toque', evento, residuo = null) {
  if (!CANECAS[color]) return;
  const ahora = Date.now();
  if (ultimaOrden[color] && ahora - ultimaOrden[color] < 1500) return; // doble toque accidental
  ultimaOrden[color] = ahora;
  residuoActual = residuo;
  ocultarAviso();
  const boton = $(`.caneca[data-color="${color}"]`);
  if (evento || origen !== 'toque') ondaToque(boton, evento);
  if (navigator.vibrate && navigator.userActivation?.hasBeenActive !== false) navigator.vibrate(30); // solo tras un toque real
  const c = instantanea?.canecas[color];
  if (c?.estado === 'fuera_servicio') {
    sonidoError();
    mostrarMomento(color, 'fuera_servicio', `${CANECAS[color].nombre} está fuera de servicio. Usa otra caneca o pide ayuda.`);
    decir(`La ${CANECAS[color].nombre.toLowerCase()} está fuera de servicio.`);
    return;
  }
  enfocada = color;
  mostrarMomento(color, c?.estado && c.estado !== 'cerrada' ? c.estado : 'abriendo');
  const r = await pedirApertura(color);
  if (!r.ok) {
    sonidoError();
    mostrarMomento(color, r.motivo === 'fuera_servicio' ? 'fuera_servicio' : 'error', r.mensaje);
    decir(r.mensaje);
  }
}

/* ── Momento de depósito ─────────────────────────────────────────────────────────────────────────── */
const momento = $('#momento');
let temporizadorCierre = null;
let enlaceAnterior = null;
const TEXTOS = {
  en_cola: ['En espera', 'Otra caneca se está cerrando. Abrimos la tuya enseguida.'],
  abriendo: ['Abriendo…', 'Un momento, la tapa se está levantando.'],
  abierta: ['¡Deposita ahora!', 'La tapa está abierta. Suelta tu residuo dentro.'],
  cerrando: ['Cerrando…', 'Retira la mano de la tapa, por favor.'],
  lista: ['¡Gracias por reciclar!', 'Tu residuo quedó en su lugar. ¡Eres parte del cambio!'],
  error: ['No se pudo abrir', 'Intenta de nuevo en un momento.'],
  rechazada: ['No se pudo abrir', 'La caneca no aceptó la orden.'],
  fuera_servicio: ['Fuera de servicio', 'Usa otra caneca o pide ayuda al personal.'],
};
const ETAPAS = ['abriendo', 'abierta', 'cerrando', 'lista'];

function mostrarMomento(color, estado, detalle) {
  clearTimeout(temporizadorCierre);
  const info = CANECAS[color];
  momento.dataset.estado = estado;
  momento.dataset.color = color;
  $('#momentoEtiqueta').textContent = info.titulo;
  const tuCaneca = $('#momentoTuCaneca');
  tuCaneca.textContent = 'Tu caneca es la ';
  const negrita = document.createElement('b'); negrita.textContent = color; tuCaneca.appendChild(negrita);
  const chipResiduo = $('#momentoResiduo');
  chipResiduo.hidden = !residuoActual;
  chipResiduo.textContent = residuoActual ? `♻ ${residuoActual}` : '';
  for (const id of ['#momentoCuerpo', '#momentoTapa']) if (!$(id).src.endsWith(info.imagen)) $(id).src = info.imagen;
  const [titulo, texto] = TEXTOS[estado] || TEXTOS.abriendo;
  $('#momentoTitulo').textContent = titulo;
  $('#momentoDetalle').textContent = detalle || texto;
  $('.momento-figura').classList.toggle('abierta-visual', estado === 'abriendo' || estado === 'abierta');
  if (momento.hidden) {
    enlaceAnterior = document.activeElement;
    momento.classList.remove('saliendo');
    momento.hidden = false;
    $('#cerrarMomento').focus({ preventScroll: true });
  }
  if (estado === 'lista') { temporizadorCierre = setTimeout(ocultarMomento, 3800); }
  if (['error', 'rechazada', 'fuera_servicio'].includes(estado)) temporizadorCierre = setTimeout(ocultarMomento, 5000);
}
function ocultarMomento() {
  if (momento.hidden) return;
  clearTimeout(temporizadorCierre);
  momento.classList.add('saliendo');
  setTimeout(() => { momento.hidden = true; momento.classList.remove('saliendo'); enlaceAnterior?.focus?.({ preventScroll: true }); }, 330);
  quitarSeleccion();
}
$('#cerrarMomento').addEventListener('click', ocultarMomento);
momento.addEventListener('click', (e) => { if (e.target === momento) ocultarMomento(); });

// Cambios de estado que llegan del servidor (o de la simulación).
function transicion(color, antes, ahora, c) {
  const nombre = CANECAS[color].nombre.toLowerCase();
  const siguiendo = color === enfocada;
  if (ahora === 'abierta') {
    sonidoAbrir();
    if (siguiendo) decir(`Tu caneca es la ${color}. Está abierta: deposita tu residuo en ${Math.round((instantanea?.abiertoMs || 7000) / 1000)} segundos.`);
  }
  if (ahora === 'cerrando' && siguiendo) decir('Cerrando.');
  if (ahora === 'cerrada' && (antes === 'cerrando' || antes === 'abierta')) {
    if (siguiendo) { sonidoListo(); lanzarConfeti(color); decir('¡Listo! Gracias por reciclar.'); mostrarMomento(color, 'lista'); enfocada = null; }
    return;
  }
  if ((ahora === 'error' || ahora === 'rechazada') && siguiendo) { sonidoError(); decir(c.mensaje || 'No se pudo abrir la caneca.'); }
  if (siguiendo && !momento.hidden && TEXTOS[ahora] && ahora !== 'cerrada') mostrarMomento(color, ahora, c.mensaje || undefined);
  if (siguiendo && momento.hidden && (ahora === 'abriendo' || ahora === 'abierta')) mostrarMomento(color, ahora);
  if (ahora === 'en_cola' && siguiendo) decir(`La ${nombre} se abre en un momento.`);
}

// Reloj: anima el arco y los segundos sin depender de que lleguen eventos.
const ARCO = 2 * Math.PI * 52;
let ultimoSegundo = null;
function reloj() {
  if (instantanea) {
    const color = momento.dataset.color;
    const c = color && instantanea.canecas[color];
    if (c && c.estado === 'abierta' && !momento.hidden) {
      const total = instantanea.abiertoMs || 7000;
      const resta = Math.max(0, c.hasta - (Date.now() + desfase));
      const s = Math.ceil(resta / 1000);
      $('#relojNumero').textContent = String(s);
      $('#relojArco').style.strokeDashoffset = String(ARCO * (1 - resta / total));
      $('#relojArco').style.stroke = s <= 3 ? '#D97706' : '';
      if (s !== ultimoSegundo && s <= 3 && s > 0) sonidoTic();
      $('#momentoDetalle').textContent = s <= 3 ? 'La tapa se cerrará en un momento.' : TEXTOS.abierta[1];
      ultimoSegundo = s;
    } else if (c && c.estado !== 'abierta') {
      $('#relojNumero').textContent = String(Math.round((instantanea.abiertoMs || 7000) / 1000));
      $('#relojArco').style.strokeDashoffset = '0';
      $('#relojArco').style.stroke = '';
    }
    for (const boton of $$('.caneca[data-estado="abierta"]')) {
      $('.caneca-estado', boton).textContent = `Abierta · ${segundosRestantes(instantanea.canecas[boton.dataset.color])} s`;
    }
  }
  requestAnimationFrame(reloj);
}
$('#relojArco').style.strokeDasharray = String(ARCO);

function lanzarConfeti(color) {
  if (reducido) return;
  const capa = $('#confeti');
  const tonos = color === 'verde' ? ['#3F9B1E', '#7BC144', '#B7E4CB', '#168A55'] : color === 'negra' ? ['#1E293B', '#168A55', '#7BC144', '#B7E4CB'] : ['#168A55', '#B7E4CB', '#7BC144', '#94A3B8'];
  for (let i = 0; i < 46; i++) {
    const h = document.createElement('i');
    h.style.left = `${Math.random() * 100}%`;
    h.style.setProperty('--c', tonos[i % tonos.length]);
    h.style.setProperty('--x', `${(Math.random() - 0.5) * 300}px`);
    h.style.setProperty('--r', `${(Math.random() - 0.5) * 1440}deg`);
    h.style.setProperty('--d', `${1.8 + Math.random() * 1.6}s`);
    h.style.animationDelay = `${Math.random() * 0.4}s`;
    h.style.transform = `scale(${0.6 + Math.random() * 0.7})`;
    capa.appendChild(h);
    setTimeout(() => h.remove(), 4200);
  }
}

/* ── ¿Dónde va? (por voz: «¿dónde va la botella?») ───────────────────────────────────────────────── */
function buscarResiduo(texto) {
  const palabras = normalizar(texto).split(' ').filter((p) => p.length > 2);
  if (!palabras.length) return null;
  let mejor = null; let puntos = 0;
  for (const r of RESIDUOS) {
    let p = 0;
    for (const w of palabras) for (const k of r.claves) {
      if (k === w || (w.length > 3 && (k.startsWith(w) || w.startsWith(k) && k.length > 3))) p += k === w ? 2 : 1;
    }
    if (p > puntos) { puntos = p; mejor = r; }
  }
  return mejor;
}

let avisoTiempo = null;
function mostrarAviso(html, acciones = []) {
  const a = $('#aviso');
  clearTimeout(avisoTiempo);
  a.innerHTML = `<span>${html}</span>`;
  for (const { texto, clase, accion } of acciones) {
    const b = document.createElement('button'); b.type = 'button'; b.textContent = texto; if (clase) b.className = clase;
    b.addEventListener('click', accion); a.appendChild(b);
  }
  a.hidden = false;
  avisoTiempo = setTimeout(ocultarAviso, 12000);
}
function ocultarAviso() { $('#aviso').hidden = true; }

/* Residuo → caneca → apertura automática.
   1) Se busca el residuo (escrito o dictado). 2) Su caneca se destaca con una animación y la etiqueta del residuo.
   3) Sin más toques se pide la apertura (digital y física) y aparece «Tu caneca es la …» con la cuenta de 7 s. */
const PAUSA_SELECCION_MS = 900; // lo justo para ver qué caneca se eligió antes de que salga el momento de depósito
let seleccionTiempo = null;
function quitarSeleccion() {
  clearTimeout(seleccionTiempo);
  $('#canecas').classList.remove('eligiendo');
  for (const c of $$('.caneca.elegida')) c.classList.remove('elegida');
  for (const chip of $$('.caneca-chip')) chip.remove();
}
function seleccionarCaneca(color, etiqueta) {
  quitarSeleccion();
  const caneca = $(`.caneca[data-color="${color}"]`);
  $('#canecas').classList.add('eligiendo');
  void caneca.offsetWidth; // reinicia la animación si se elige la misma caneca dos veces seguidas
  caneca.classList.add('elegida');
  if (etiqueta) {
    const chip = document.createElement('span');
    chip.className = 'caneca-chip'; chip.setAttribute('aria-hidden', 'true'); chip.textContent = `♻ ${etiqueta}`;
    caneca.appendChild(chip);
  }
  seleccionTiempo = setTimeout(quitarSeleccion, 9000 + PAUSA_SELECCION_MS); // por si el momento no llega a mostrarse
}

function elegirResiduo(r, origen) {
  if (!r.c) {
    quitarSeleccion();
    sonidoError();
    mostrarAviso(r.nota, [{ texto: 'Entendido', clase: 'secundario', accion: ocultarAviso }]);
    decir(r.nota, { siempre: origen === 'voz' });
    return;
  }
  seleccionarCaneca(r.c, r.n);
  sonidoTic();
  decir(`${r.n}: tu caneca es la ${r.c}, ${CANECAS[r.c].tipo}. La estoy abriendo.`);
  setTimeout(() => abrir(r.c, origen, null, r.n), reducido ? 0 : PAUSA_SELECCION_MS);
}

// Texto libre (escrito o dictado): primero el residuo; si no hay residuo, un color o tipo de caneca.
function resolverTexto(texto, origen) {
  const t = normalizar(texto);
  if (!t) return false;
  const r = buscarResiduo(t);
  if (r) { elegirResiduo(r, origen); return true; }
  const color = colorEn(t);
  if (color) { seleccionarCaneca(color, null); setTimeout(() => abrir(color, origen), reducido ? 0 : 500); return true; }
  return false;
}

const formResiduo = $('#formResiduo');
const campoResiduo = $('#residuo');
formResiduo.addEventListener('submit', (e) => {
  e.preventDefault();
  const texto = campoResiduo.value;
  if (!normalizar(texto)) { campoResiduo.focus(); return; }
  if (!resolverTexto(texto, 'texto')) noReconocido(texto);
  else campoResiduo.blur();
});
function noReconocido(texto) {
  formResiduo.classList.remove('sin-resultado'); void formResiduo.offsetWidth; formResiduo.classList.add('sin-resultado');
  setTimeout(() => formResiduo.classList.remove('sin-resultado'), 700);
  const limpio = texto.trim().slice(0, 40);
  const aviso = document.createElement('span');
  aviso.append('No reconocí «', Object.assign(document.createElement('b'), { textContent: limpio }), '». Prueba con otra palabra o toca la caneca.');
  mostrarAviso(aviso.innerHTML, [{ texto: 'Entendido', clase: 'secundario', accion: ocultarAviso }]);
  decir(`No reconocí ${limpio}. Prueba con otra palabra o toca la caneca.`);
}


/* ── Comandos de voz (reconocimiento) ────────────────────────────────────────────────────────────── */
const Reconocimiento = window.SpeechRecognition || window.webkitSpeechRecognition;
let vozActiva = false;
let reconocedor = null;
const botonVoz = $('#botonVoz');

function colorEn(t) {
  if (/\bno aprovechable/.test(t)) return 'negra';
  if (/\b(blanc[ao]s?|aprovechables?|reciclables?)\b/.test(t)) return 'blanca';
  if (/\b(verdes?|organic[ao]s?)\b/.test(t)) return 'verde';
  if (/\b(negr[ao]s?)\b/.test(t)) return 'negra';
  return null;
}
const DISPARO = /\b(canecas?|caneca|tachos?|botes?|basureros?|contenedor|abr[ea]n?|abrir|abreme|abrela|abra|abrame)\b/;

function interpretar(textoCrudo, final) {
  if (Date.now() < hablandoHasta) return; // no escucharse a sí misma
  const t = normalizar(textoCrudo);
  if (!t) return;
  // Lo que se oye aparece en el buscador, así la persona ve que el micrófono la entendió.
  if (document.activeElement !== campoResiduo) campoResiduo.value = textoCrudo.trim();
  const color = colorEn(t);
  if (color && DISPARO.test(t)) {
    // En resultados parciales solo actuamos con la frase completa «caneca <color>» para responder rápido sin errores.
    if (!final && !new RegExp(`(caneca|abr[a-z]*) ${color === 'blanca' ? 'blanc' : color === 'negra' ? 'negr' : 'verd'}`).test(t)) return;
    seleccionarCaneca(color, null);
    abrir(color, 'voz');
    return;
  }
  if (!final) { pintarEstadoVoz('oido'); return; }
  pintarEstadoVoz(vozActiva ? 'activa' : 'inactiva');
  if (/\b(ayuda|como funciona|que hago|instrucciones)\b/.test(t)) { decirInstrucciones(); return; }
  if (!resolverTexto(t, 'voz') && t.split(' ').length <= 6) noReconocido(textoCrudo);
}

function decirInstrucciones() {
  decir('Hola, soy Eco. Dime qué vas a botar, por ejemplo: botella de agua, y abro su caneca. También puedes decir: caneca blanca, caneca verde o caneca negra.', { siempre: true });
}

// Estado del dictado: «Escuchando…» (micrófono abierto), «Te oí…» (llega voz), apagado o no disponible.
const TEXTO_VOZ = { activa: 'Escuchando…', oido: 'Te oí…', inactiva: 'Toca para hablar', 'sin-soporte': 'No disponible aquí' };
function pintarEstadoVoz(estado) {
  $('#estadoVoz').dataset.estado = estado;
  $('#botonVozTexto').textContent = TEXTO_VOZ[estado] || TEXTO_VOZ.activa;
}
function pintarVoz(estado) {
  botonVoz.setAttribute('aria-pressed', String(estado === 'activa'));
  botonVoz.classList.toggle('sin-soporte', estado === 'sin-soporte');
  botonVoz.setAttribute('aria-label', estado === 'activa' ? 'Dictado por voz activo. Pulsa para apagar el micrófono' : 'Activar el dictado por voz');
  pintarEstadoVoz(estado);
}

// De las alternativas que propone el navegador, se queda con la primera que nombra un residuo o una caneca.
function mejorAlternativa(res) {
  for (let a = 0; a < res.length; a++) {
    const t = normalizar(res[a].transcript);
    if (buscarResiduo(t) || colorEn(t)) return res[a].transcript;
  }
  return res[0].transcript;
}

function iniciarReconocimiento() {
  if (!Reconocimiento) return false;
  if (!reconocedor) {
    reconocedor = new Reconocimiento();
    reconocedor.lang = 'es-CO';
    reconocedor.continuous = true;
    reconocedor.interimResults = true;
    reconocedor.maxAlternatives = 3;
    reconocedor.onresult = (e) => {
      for (let i = e.resultIndex; i < e.results.length; i++) {
        const res = e.results[i];
        interpretar(res.isFinal ? mejorAlternativa(res) : res[0].transcript, res.isFinal);
      }
    };
    reconocedor.onspeechstart = () => botonVoz.classList.add('oyendo');
    reconocedor.onspeechend = () => { botonVoz.classList.remove('oyendo'); if (vozActiva) pintarEstadoVoz('activa'); };
    reconocedor.onerror = (e) => {
      if (e.error === 'not-allowed' || e.error === 'service-not-allowed') {
        vozActiva = false; pintarVoz('inactiva');
        mostrarAviso('Para dictar tu residuo, permite el micrófono en el navegador. También puedes escribirlo.', [{ texto: 'Entendido', clase: 'secundario', accion: ocultarAviso }]);
      } else if (e.error === 'network') {
        vozActiva = false; pintarVoz('inactiva');
        mostrarAviso('El dictado por voz necesita internet en este navegador. Puedes escribir tu residuo.', [{ texto: 'Entendido', clase: 'secundario', accion: ocultarAviso }]);
      }
    };
    reconocedor.onend = () => { botonVoz.classList.remove('oyendo'); if (vozActiva) setTimeout(() => { try { reconocedor.start(); } catch { /* ya inició */ } }, 250); };
  }
  try { reconocedor.start(); } catch { /* ya estaba escuchando */ }
  return true;
}

function activarVoz(anunciar = true) {
  if (!Reconocimiento) {
    pintarVoz('sin-soporte');
    if (anunciar) mostrarAviso('Este navegador no reconoce la voz. Escribe tu residuo o usa Google Chrome o Microsoft Edge.', [{ texto: 'Entendido', clase: 'secundario', accion: ocultarAviso }]);
    return;
  }
  vozActiva = true;
  pintarVoz('activa'); iniciarReconocimiento();
  if (anunciar) decir('Te escucho. Dime qué vas a botar.');
}
function desactivarVoz() {
  vozActiva = false;
  try { reconocedor?.stop(); } catch { /* nada */ }
  speechSynthesis?.cancel?.();
  pintarVoz('inactiva');
}
botonVoz.addEventListener('click', () => (vozActiva ? desactivarVoz() : activarVoz()));

/* ── Teclado, texto grande y arranque ────────────────────────────────────────────────────────────── */
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') { ocultarMomento(); ocultarAviso(); return; }
  if (e.target.closest('input, textarea') || e.ctrlKey || e.metaKey || e.altKey) return;
  const mapa = { 1: 'blanca', 2: 'verde', 3: 'negra' };
  const color = mapa[e.key];
  if (color) { e.preventDefault(); abrir(color, 'teclado'); }
  else if (e.key === '?') decirInstrucciones();
  else if (e.key.length === 1 && /\p{L}/u.test(e.key) && momento.hidden) campoResiduo.focus(); // escribir en cualquier parte va al buscador
});

for (const boton of $$('.caneca')) boton.addEventListener('click', (e) => abrir(boton.dataset.color, 'toque', e.detail ? e : null));

const botonTexto = $('#botonTexto');
function textoGrande(activo) {
  document.documentElement.classList.toggle('texto-grande', activo);
  botonTexto.setAttribute('aria-pressed', String(activo));
  botonTexto.setAttribute('aria-label', activo ? 'Volver al texto normal' : 'Agrandar el texto');
  localStorage.setItem('eco-texto', activo ? '1' : '0');
}
botonTexto.addEventListener('click', () => textoGrande(!document.documentElement.classList.contains('texto-grande')));
textoGrande(localStorage.getItem('eco-texto') === '1');

conectar();
requestAnimationFrame(reloj);
// El micrófono es lo primero: arranca abierto en cada visita. Si el navegador ya tiene permiso empieza a escuchar
// solo; si no, lo pide, y el primer toque en cualquier parte lo vuelve a intentar (algunos navegadores exigen un gesto).
activarVoz(false);
if (Reconocimiento) {
  const primerGesto = (e) => {
    if (!vozActiva && !e.target.closest('#botonVoz')) activarVoz(false);
    else if (vozActiva) iniciarReconocimiento();
  };
  document.addEventListener('pointerdown', primerGesto, { once: true });
}
