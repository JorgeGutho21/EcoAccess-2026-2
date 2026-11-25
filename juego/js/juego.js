// EcoAccess Challenge · flujo del participante. El navegador solo muestra lo que decide el servidor (Oracle): puntaje,
// tiempo y corrección nunca se calculan aquí. La sesión es un secreto aleatorio local (crypto.getRandomValues); al
// recargar, la página consulta el intento y continúa exactamente donde iba, con el mismo cronómetro.
import { llamar, alAvisar, ErrorApi, RONDA } from './api.js?v=20261125';

const $ = (id) => document.getElementById(id);
const CLAVE_SESION = 'ecoaccess.juego.sesion' + (RONDA ? '.' + RONDA : '');
const CLAVE_DISPOSITIVO = 'ecoaccess.juego.dispositivo';
const IMAGENES = ['p1-banano.webp', 'p2-serum-vidrio.webp', 'p3-servilleta.webp', 'p3-cuaderno.webp', 'p3-aluminio.webp', 'p3-kraft.svg',
  'p4-eco-prototipo.webp', 'p5-limon-aluminio.webp', 'p6-oracle.svg', 'p7-csharp.svg'].map((f) => 'img/preguntas/' + f);

const almacen = {
  leer(k) { try { return localStorage.getItem(k); } catch { return null; } },
  guardar(k, v) { try { v == null ? localStorage.removeItem(k) : localStorage.setItem(k, v); } catch { /* modo privado */ } },
};
function aleatorio(bytes = 32) {
  const b = crypto.getRandomValues(new Uint8Array(bytes));
  return btoa(String.fromCharCode(...b)).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}
const dispositivo = almacen.leer(CLAVE_DISPOSITIVO) ?? (() => { const d = aleatorio(18); almacen.guardar(CLAVE_DISPOSITIVO, d); return d; })();
let sesion = almacen.leer(CLAVE_SESION);
let estado = null;          // último estado del intento devuelto por el servidor
let reloj = { base: 0, en: 0, t: 0 };
let seleccion = null;
let enviando = false;

// En rondas de prueba, los enlaces conservan el código de la ronda.
if (RONDA) document.querySelectorAll('a[href^="clasificacion"]').forEach((a) => { a.href = 'clasificacion/?ronda=' + encodeURIComponent(RONDA); });
alAvisar((m) => { $('conexion').hidden = !m; $('conexion').textContent = m ?? ''; });

// ───────── Navegación entre pantallas ─────────
function mostrar(nombre) {
  document.querySelectorAll('.pantalla').forEach((s) => { s.hidden = s.dataset.pantalla !== nombre; });
  scrollTo({ top: 0 });
  const h = document.querySelector(`[data-pantalla="${nombre}"] h1, [data-pantalla="${nombre}"] h2`);
  h?.setAttribute('tabindex', '-1'); h?.focus({ preventScroll: true });
  if (nombre !== 'pregunta') detenerReloj();
}
document.querySelectorAll('[data-ir]').forEach((b) => b.addEventListener('click', () => mostrar(b.dataset.ir)));

function mensaje(titulo, texto) { $('tituloMensaje').textContent = titulo; $('textoMensaje').textContent = texto; mostrar('mensaje'); }

const formato = (ms) => { const s = Math.max(0, Math.floor(ms / 1000)); return `${Math.floor(s / 60)}:${String(s % 60).padStart(2, '0')}`; };
const formatoLargo = (ms) => { const s = ms / 1000; return s < 60 ? `${s.toFixed(1).replace('.', ',')} s` : `${Math.floor(s / 60)} min ${Math.round(s % 60)} s`; };

// ───────── Dirigir según el estado del servidor ─────────
function dirigir(e) {
  estado = e;
  if (e.estado === 'REGISTRADO') { $('saludo').textContent = `Hola, ${e.alias}`; mostrar('instrucciones'); }
  else if (e.estado === 'EN_CURSO') pintarPregunta(e);
  else if (e.estado === 'FINALIZADO') pintarResultado(e);
  else mensaje('Participación cerrada', 'Esta participación ya no está activa. Pide ayuda en el stand de EcoAccess.');
}

async function reanudar() {
  if (!sesion) return false;
  try { dirigir(await llamar('/intento', { sesion })); return true; }
  catch (e) {
    if (e instanceof ErrorApi && e.status === 401) { sesion = null; almacen.guardar(CLAVE_SESION, null); return false; }
    mensaje('Sin conexión', e.message); return true;
  }
}

// ───────── Bienvenida ─────────
let competencia = null;
async function cargarEstado() {
  try {
    const e = await llamar('/estado', { paciencia: 15000 });
    competencia = e.competencia;
    const c = competencia;
    $('estadoConcurso').textContent = c.estado === 'ABIERTA' ? '' :
      c.estado === 'PREPARADA' ? 'El concurso aún no ha comenzado. Vuelve en unos minutos.' :
      c.estado === 'REGISTRO_CERRADO' ? 'El registro está cerrado. Consulta la clasificación.' :
      'El concurso terminó. Consulta la clasificación definitiva.';
    $('comenzar').disabled = c.estado !== 'ABIERTA';
  } catch (e) {
    $('estadoConcurso').textContent = e.codigo === 'sin_competencia' ? 'El concurso aún no ha comenzado.' : e.message;
    $('comenzar').disabled = e.codigo === 'sin_competencia';
  }
}
$('comenzar').addEventListener('click', () => { mostrar('registro'); $('nombre').focus(); });

// ───────── Registro ─────────
$('alias').addEventListener('input', () => { $('vistaAlias').textContent = $('alias').value.trim() || 'tu alias'; });
$('verAviso').addEventListener('click', () => $('aviso').showModal());
function errorCampo(id, texto) {
  $('error' + id).textContent = texto ?? '';
  const campo = { Nombre: 'nombre', Alias: 'alias', Acepta: 'acepta' }[id];
  if (campo) $(campo).setAttribute('aria-invalid', texto ? 'true' : 'false');
}
$('formRegistro').addEventListener('submit', async (ev) => {
  ev.preventDefault();
  if (enviando) return;
  const nombre = $('nombre').value.trim().replace(/\s+/g, ' ');
  const alias = $('alias').value.trim().replace(/\s+/g, ' ');
  ['Nombre', 'Alias', 'Acepta'].forEach((c) => errorCampo(c)); $('errorRegistro').textContent = '';
  let ok = true;
  if (!/^\p{L}{2,}([ .'-]+\p{L}+)+$/u.test(nombre) || nombre.length > 80) { errorCampo('Nombre', 'Escribe tu nombre y apellido (solo letras).'); ok = false; }
  if (!/^[\p{L}\p{N} _.-]{3,20}$/u.test(alias)) { errorCampo('Alias', 'Entre 3 y 20 letras o números.'); ok = false; }
  if (!$('acepta').checked) { errorCampo('Acepta', 'Necesitamos tu aceptación para participar.'); ok = false; }
  if (!ok) { document.querySelector('[aria-invalid="true"]')?.focus(); return; }
  // El secreto se guarda ANTES de enviarlo: si la red falla, el reintento usa el mismo y no crea un segundo registro.
  if (!sesion) { sesion = aleatorio(32); almacen.guardar(CLAVE_SESION, sesion); }
  enviando = true; $('registrar').disabled = true; $('registrar').textContent = 'Registrando…';
  try {
    dirigir(await llamar('/registro', { metodo: 'POST', cuerpo: { nombre, alias, acepta: true, sesion, dispositivo, ronda: RONDA ?? undefined } }));
  } catch (e) {
    const campos = { alias_ocupado: 'Alias', alias: 'Alias', nombre: 'Nombre', ya_registrado: 'Nombre', aviso: 'Acepta' };
    if (campos[e.codigo]) errorCampo(campos[e.codigo], e.message); else $('errorRegistro').textContent = e.message;
    // Si el servidor rechazó el registro, ese secreto no quedó asociado a nadie: se descarta.
    if (e instanceof ErrorApi && e.status >= 400 && e.status < 500) { sesion = null; almacen.guardar(CLAVE_SESION, null); }
  } finally { enviando = false; $('registrar').disabled = false; $('registrar').textContent = 'Registrarme'; }
});

// ───────── Inicio oficial ─────────
$('listo').addEventListener('click', async () => {
  if (enviando) return;
  enviando = true; $('listo').disabled = true; $('listo').textContent = 'Preparando…';
  try { dirigir(await llamar('/intento/inicio', { metodo: 'POST', sesion })); precargar(); }
  catch (e) { mensaje('No se pudo iniciar', e.message); }
  finally { enviando = false; $('listo').disabled = false; $('listo').textContent = 'Estoy listo'; }
});
function precargar() { IMAGENES.forEach((src) => { const i = new Image(); i.decoding = 'async'; i.src = src; }); }

// ───────── Pregunta ─────────
function iniciarReloj(transcurridoMs) {
  reloj = { base: transcurridoMs, en: performance.now(), t: reloj.t };
  clearInterval(reloj.t);
  const pintar = () => { $('reloj').textContent = formato(reloj.base + performance.now() - reloj.en); };
  pintar(); reloj.t = setInterval(pintar, 250);
}
function detenerReloj() { clearInterval(reloj.t); }

function pintarProgreso(respondidas, actual) {
  const hojas = $('hojas');
  if (!hojas.children.length) hojas.innerHTML = Array.from({ length: 7 }, () => '<li></li>').join('');
  [...hojas.children].forEach((h, i) => { h.className = i < respondidas ? 'hecha' : i === actual - 1 ? 'actual' : ''; });
  const tallo = $('talloLleno'); const largo = tallo.getTotalLength?.() || 300;
  tallo.style.strokeDasharray = largo; tallo.style.strokeDashoffset = largo * (1 - respondidas / 7);
  $('progreso').setAttribute('aria-valuenow', respondidas);
  $('progreso').setAttribute('aria-valuetext', `${respondidas} de 7 respondidas`);
}

function pintarPregunta(e) {
  const p = e.pregunta;
  mostrar('pregunta');
  iniciarReloj(e.transcurridoMs);
  $('numero').textContent = `Pregunta ${p.orden} de ${p.total}`;
  pintarProgreso(e.respondidas, p.orden);
  $('enunciado').textContent = p.enunciado;
  const fig = $('ilustracion');
  fig.hidden = !p.imagen;
  if (p.imagen) {
    $('imagenPregunta').src = p.imagen; $('imagenPregunta').alt = descripcionImagen(p.orden);
    fig.classList.toggle('contener', /\.svg$|p5-/.test(p.imagen));
  }
  seleccion = null; $('confirmar').disabled = true; $('errorPregunta').textContent = '';
  const cont = $('opciones');
  cont.classList.toggle('con-imagen', p.opcionesConImagen);
  cont.innerHTML = '';
  p.opciones.forEach((o, i) => {
    const b = document.createElement('button');
    b.type = 'button'; b.className = 'opcion'; b.setAttribute('role', 'radio'); b.setAttribute('aria-checked', 'false');
    b.tabIndex = i === 0 ? 0 : -1; b.dataset.id = o.id;
    const letra = 'ABCD'[i];
    if (p.opcionesConImagen && o.imagen) {
      b.innerHTML = `<img src="${o.imagen}" alt="" width="480" height="480"><span class="pie"><span class="letra">${letra}</span><span></span></span>`;
      b.querySelector('.pie span:last-child').textContent = o.texto;
    } else {
      b.innerHTML = `<span class="letra">${letra}</span><span></span>`;
      b.lastElementChild.textContent = o.texto;
    }
    b.setAttribute('aria-label', `${letra}. ${o.texto}`);
    b.addEventListener('click', () => elegir(b));
    cont.appendChild(b);
  });
}
function descripcionImagen(orden) {
  return ['', 'Cáscara de banano sostenida por la mano de Eco', 'Envase rosado de sérum de vidrio con tapa blanca',
    '', 'Eco junto al prototipo de las tres canecas de EcoAccess', 'Un limón partido y una bola de papel aluminio limpio',
    'EcoAccess conectado con una base de datos en la nube', 'Código C# conectado con servicios, Oracle y el ESP32'][orden] ?? '';
}
function elegir(b) {
  if (enviando) return;
  $('opciones').querySelectorAll('.opcion').forEach((x) => { x.setAttribute('aria-checked', String(x === b)); x.tabIndex = x === b ? 0 : -1; });
  seleccion = Number(b.dataset.id);
  $('confirmar').disabled = false;
}
// Flechas para moverse entre opciones (patrón radiogroup).
$('opciones').addEventListener('keydown', (ev) => {
  if (!['ArrowDown', 'ArrowUp', 'ArrowLeft', 'ArrowRight'].includes(ev.key)) return;
  ev.preventDefault();
  const ops = [...$('opciones').querySelectorAll('.opcion')];
  const i = ops.indexOf(document.activeElement);
  const sig = ops[(i + (ev.key === 'ArrowDown' || ev.key === 'ArrowRight' ? 1 : ops.length - 1)) % ops.length];
  sig.focus(); elegir(sig);
});
$('confirmar').addEventListener('click', async () => {
  if (enviando || seleccion == null) return;
  enviando = true; $('confirmar').disabled = true; $('confirmar').textContent = 'Guardando…';
  try {
    const e = await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: estado.pregunta.orden, opcion: seleccion } });
    dirigir(e);
  } catch (e) {
    $('errorPregunta').textContent = e.message;
    $('confirmar').disabled = false;
  } finally { enviando = false; $('confirmar').textContent = 'Confirmar respuesta'; }
});

// ───────── Resultado ─────────
function pintarResultado(e) {
  const r = e.resultado;
  mostrar('resultado');
  $('puntaje').textContent = r.puntaje;
  $('aciertos').textContent = `${r.aciertos} de 7`;
  $('tiempo').textContent = formatoLargo(r.tiempoMs);
  $('etiquetaPosicion').textContent = r.definitiva ? 'Posición final' : 'Posición provisional';
  $('posicion').textContent = `${r.posicion}.º de ${r.participantes}`;
  $('mensajeEco').textContent = r.puntaje === 700 ? '¡Puntaje perfecto! Sabes separar como un experto en EcoAccess.'
    : r.puntaje >= 500 ? '¡Muy bien! Ya sabes cómo usar EcoAccess y cuidar el planeta.'
    : r.puntaje >= 300 ? '¡Buen intento! En el stand te mostramos cómo funciona cada caneca.'
    : 'Gracias por jugar. Acércate al stand y descubre cómo EcoAccess te ayuda a separar.';
  $('verRevision').hidden = !r.revisionDisponible;
  if (!matchMedia('(prefers-reduced-motion: reduce)').matches) celebrar();
}
function celebrar() {
  const c = document.querySelector('.hojas-caen');
  c.innerHTML = Array.from({ length: 28 }, (_, i) => `<i style="left:${Math.random() * 100}%;animation-duration:${2.4 + Math.random() * 2.2}s;animation-delay:${i * 60}ms"></i>`).join('');
  setTimeout(() => { c.innerHTML = ''; }, 6000);
}
$('verRevision').addEventListener('click', async () => {
  try {
    const r = await llamar('/intento/revision', { sesion });
    $('listaRevision').innerHTML = '';
    r.revision.forEach((q) => {
      const li = document.createElement('li');
      li.innerHTML = '<strong></strong><p class="marca"></p><p></p><p class="explicacion"></p>';
      li.children[0].textContent = `${q.orden}. ${q.enunciado}`;
      li.children[1].textContent = q.acierto ? `Correcta: ${q.correcta}` : `Elegiste: ${q.elegida ?? 'sin respuesta'}`;
      li.children[1].className = 'marca ' + (q.acierto ? 'bien' : 'mal');
      li.children[2].textContent = q.acierto ? '' : `Respuesta correcta: ${q.correcta}`;
      li.children[3].textContent = q.explicacion;
      $('listaRevision').appendChild(li);
    });
    mostrar('revision');
  } catch (e) { $('mensajeEco').textContent = e.message; }
});
$('otraPersona').addEventListener('click', () => {
  if (!confirm('Esta participación ya quedó guardada. ¿Preparar el juego para otra persona en este dispositivo?')) return;
  sesion = null; almacen.guardar(CLAVE_SESION, null);
  $('formRegistro').reset(); $('vistaAlias').textContent = 'tu alias';
  mostrar('bienvenida'); cargarEstado();
});

// Al volver a la pestaña o recuperar internet, se sincroniza con el servidor (el cronómetro también).
document.addEventListener('visibilitychange', () => { if (!document.hidden && estado?.estado === 'EN_CURSO') reanudar(); });
addEventListener('online', () => { if (estado?.estado === 'EN_CURSO') reanudar(); });

// ───────── Arranque ─────────
(async () => {
  if (!(await reanudar())) { mostrar('bienvenida'); await cargarEstado(); }
  else cargarEstado();
})();
