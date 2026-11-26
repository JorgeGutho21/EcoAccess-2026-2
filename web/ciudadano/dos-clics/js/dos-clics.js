// EcoAccess · Desechar en dos clics. Página independiente (/dos-clics/), sin contenido académico.
// Tres formas de elegir la caneca: tocarla (manual), escribir el residuo (motor determinista + catálogo) o decirlo (voz,
// solo con la transcripción FINAL). Reutiliza sin modificar los módulos compartidos de ../js/.
//
// Modos, siempre rotulados en pantalla (sello sobre la caneca y texto):
//  - Vista ilustrativa: sin sesión de estación (no se escaneó el QR presencial). La animación no mueve nada.
//  - Simulación (?demo): como la estación, pero simulado.
//  - Orden enviada: con QR presencial (#e=…&t=… → estacion.js) y después de que la pasarela confirma la orden comandada.
//  - Desconectado: hay sesión pero la estación no responde; la animación vuelve a ser ilustrativa.
// La política 7/14/21 s llega en el plan de la pasarela; el cierre físico lo hace el controlador, nunca el navegador.
// Nunca se afirma que la tapa física se abrió: no hay sensor de tapa.
import { crearMotor, duracionPolitica } from '../../js/motor.js';
import { hablar, callar } from '../../js/voz.js';
import * as estacion from '../../js/estacion.js';
import * as historial from '../../js/historial.js';

const $ = (id) => document.getElementById(id);
const el = {
  estacion: $('estacion'), estacionTexto: $('estacionTexto'), avisoModo: $('avisoModo'), canecas: $('canecas'), eco: $('ecoImagen'),
  entrada: $('entrada'),
  aclaracion: $('aclaracion'), aclaracionTexto: $('aclaracionTexto'), opciones: $('opciones'), cancelarAclaracion: $('cancelarAclaracion'),
  form: $('formulario'), input: $('residuo'), enviar: $('enviar'), mic: $('microfono'), escucha: $('escucha'), escuchaTexto: $('escuchaTexto'), mensaje: $('mensaje'),
  operacion: $('operacion'), anillo: $('anillo'), segundos: $('segundos'), opSello: $('opSello'), opTitulo: $('opTitulo'), opDetalle: $('opDetalle'), pasos: $('pasos'), volver: $('volver'),
  abrirHistorial: $('abrirHistorial'), dialogo: $('historial'), histLista: $('histLista'), alternarVoz: $('alternarVoz'), version: $('version'),
  anuncio: $('anuncio'), anuncioCuenta: $('anuncioCuenta'),
};
const botones = Object.fromEntries([...document.querySelectorAll('.caneca')].map((b) => [b.dataset.caneca, b]));
const ORDEN = ['verde', 'blanca', 'negra'];
const LETRA = { verde: 'V', blanca: 'B', negra: 'N' };
const DE_LETRA = { V: 'verde', B: 'blanca', N: 'negra' };
const CATEGORIA = { verde: 'orgánicos aprovechables', blanca: 'aprovechables', negra: 'no aprovechables' };
const NUMEROS = ['cero', 'un', 'dos', 'tres', 'cuatro', 'cinco', 'seis', 'siete', 'ocho', 'nueve', 'diez', 'once', 'doce', 'trece', 'catorce', 'quince', 'dieciséis', 'diecisiete', 'dieciocho', 'diecinueve', 'veinte', 'veintiún'];
const demo = new URLSearchParams(location.search).has('demo');

let motor = null;
let ocupado = false;          // exclusión mutua en la página: una sola operación a la vez
let generacion = 0;           // invalida esperas de operaciones anteriores
let temporizadores = [];
let pendiente = null;         // aclaración en curso { r, texto, origen, amb }
let estadoEstacion = { estado: demo ? 'demo' : 'ilustrativa' };
let vozActiva = leerPreferenciaVoz();
let escucha = null;
let escuchaCancelada = false;

// ───────────── Utilidades ─────────────
const esperar = (ms, gen) => new Promise((ok) => { const t = setTimeout(() => ok(gen === generacion), Math.max(0, ms)); temporizadores.push(t); });
function limpiarTemporizadores() { temporizadores.forEach(clearTimeout); temporizadores = []; }
const escapar = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
const listaY = (l) => (l.length < 2 ? l.join('') : `${l.slice(0, -1).join(', ')} y ${l.at(-1)}`);
const enFrase = (t) => (/^\p{Lu}\p{Ll}/u.test(t) ? t[0].toLocaleLowerCase('es-CO') + t.slice(1) : t);
const nombreCanecas = (lista) => `${lista.length > 1 ? 'canecas' : 'caneca'} ${listaY(lista)}`;
const enLetras = (s) => NUMEROS[s] ?? String(s);
function leerPreferenciaVoz() { try { return localStorage.getItem('ecoaccess.voz') !== 'no'; } catch { return true; } }
function nuevaClave() { return crypto.randomUUID?.().replace(/-/g, '') ?? (Date.now().toString(36) + Math.random().toString(36).slice(2) + Math.random().toString(36).slice(2)); }
function anunciar(texto) { el.anuncio.textContent = ''; requestAnimationFrame(() => { el.anuncio.textContent = texto; }); }
function decir(texto) { hablar(texto, vozActiva); }
function mensaje(texto, tono) { el.mensaje.textContent = texto || ''; if (tono) el.mensaje.dataset.tono = tono; else delete el.mensaje.dataset.tono; }
const fase = (f) => { document.body.dataset.fase = f; };

// ───────────── Estación ─────────────
function modoActual() {
  if (demo) return 'demo';
  if (estacion.sesionActual()) return 'fisico';
  return 'ilustrativa';
}
/** Cómo se representará la próxima apertura. */
function modoOperacion() {
  const m = modoActual();
  if (m !== 'fisico') return m;
  return estadoEstacion.estado === 'desconectada' ? 'desconectada' : 'comandada';
}
function pintarEstacion() {
  const m = modoActual(); const e = estadoEstacion.estado;
  let clase = 'ilustrativa', texto = 'Vista ilustrativa', aviso = 'Vista ilustrativa — sin movimiento físico', cuerpo = 'ilustrativa';
  if (m === 'demo') { clase = 'demo'; texto = 'Simulación'; aviso = 'Simulación — sin movimiento físico'; cuerpo = 'demo'; }
  else if (m === 'fisico') {
    cuerpo = 'comandada'; aviso = 'Estación vinculada por código QR';
    if (e === 'disponible') { clase = 'disponible'; texto = 'Estación conectada'; }
    else if (e === 'ocupada') { clase = 'ocupada'; texto = 'Estación ocupada'; }
    else if (e === 'sin-sesion-supervisada') { clase = 'ocupada'; texto = 'Estación en mantenimiento'; }
    else if (e === 'verificando') { clase = 'ilustrativa'; texto = 'Conectando…'; }
    else { clase = 'desconectada'; texto = 'Desconectado'; aviso = 'Vista ilustrativa — sin movimiento físico'; cuerpo = 'desconectada'; }
  }
  el.estacion.dataset.estado = clase; el.estacionTexto.textContent = texto;
  if (!ocupado) { el.avisoModo.textContent = aviso; document.body.dataset.modo = cuerpo; }
}
async function refrescarEstacion() {
  if (modoActual() !== 'fisico' || document.hidden) return;
  try { estadoEstacion = await estacion.consultarEstado(); }
  catch (e) {
    estadoEstacion = { estado: e.codigo === 'permiso' ? 'ilustrativa' : 'desconectada', texto: e.message };
    if (e.codigo === 'permiso' && !ocupado) mensaje(e.message, 'error');
  }
  pintarEstacion();
}
el.estacion.addEventListener('click', () => {
  const m = modoActual();
  if (m === 'ilustrativa') mensaje('Vista ilustrativa: te muestro la caneca correcta sin mover nada. Para abrir la caneca física, escanea el código QR de la estación EcoAccess.');
  else if (m === 'demo') mensaje('Simulación: la animación no mueve ninguna caneca física.');
  else mensaje(estadoEstacion.texto || 'Estación vinculada por código QR.', estadoEstacion.estado === 'desconectada' ? 'error' : undefined);
});

// ───────────── Canecas ─────────────
function marcar(lista) {
  for (const [c, b] of Object.entries(botones)) {
    const si = lista.includes(c);
    b.classList.toggle('elegida', si); b.classList.toggle('atenuada', lista.length > 0 && !si);
    b.setAttribute('aria-pressed', String(si));
  }
}
function limpiarCanecas() {
  for (const b of Object.values(botones)) { b.classList.remove('elegida', 'atenuada', 'abierta', 'cerrando'); b.setAttribute('aria-pressed', 'false'); }
  delete el.canecas.dataset.sello;
}
function abrirTapa(c, ms) { const b = botones[c]; b.style.setProperty('--t-mov', ms + 'ms'); b.classList.remove('cerrando'); b.classList.add('abierta'); }
function cerrarTapa(c, ms) { const b = botones[c]; b.style.setProperty('--t-mov', ms + 'ms'); b.classList.add('cerrando'); b.classList.remove('abierta'); }
function etiquetarCanecas() {
  for (const [c, b] of Object.entries(botones)) b.setAttribute('aria-label', `Abrir caneca ${c}, ${CATEGORIA[c]}`);
}

function bloquear(v) {
  for (const n of [el.input, el.enviar, el.mic, ...Object.values(botones)]) n.disabled = v;
}

function irInicio({ enfocar = false } = {}) {
  generacion++; limpiarTemporizadores(); ocupado = false; pendiente = null;
  fase('inicio'); limpiarCanecas(); etiquetarCanecas();
  el.operacion.hidden = true; el.entrada.hidden = false; el.aclaracion.hidden = true; el.volver.hidden = true;
  el.opciones.innerHTML = ''; mensaje('');
  el.eco.src = '../assets/estacion/eco.webp';
  bloquear(!motor);
  pintarEstacion();
  if (enfocar) el.input.focus();
}

// Modalidad A — manual: un solo toque en la caneca solicita la apertura (sin segundo toque ni botón de confirmación).
// Mientras hay una operación en curso los toques se ignoran (exclusión mutua), así un doble toque no abre dos veces.
for (const [c, b] of Object.entries(botones)) {
  b.addEventListener('click', () => {
    if (ocupado) return;
    if (document.body.dataset.fase !== 'inicio') irInicio();
    ejecutar([c], { residuos: [], consulta: `Apertura manual: caneca ${c}`, origen: 'manual' });
  });
}
el.cancelarAclaracion.addEventListener('click', () => irInicio({ enfocar: true }));

// ───────────── Modalidad B — texto (y C, voz, que llega aquí con la frase final) ─────────────
function identificar(texto, origen = 'texto', alternativas = []) {
  if (ocupado || !motor) return;
  let r = motor.clasificar(texto);
  if (origen === 'voz' && r.estado !== 'reconocido') {
    for (const alt of alternativas.slice(1)) { const r2 = motor.clasificar(alt); if (r2.estado === 'reconocido') { r = r2; texto = alt; el.input.value = alt; break; } }
  }
  pendiente = null; limpiarCanecas(); fase('inicio');
  if (r.estado === 'vacio') { mensaje('Escribe o di qué residuo vas a desechar.'); el.input.focus(); return; }
  if (r.estado === 'especial') {
    const e = r.items.find((i) => i.tipo === 'especial').especial;
    mensaje(`${e.nombre}: no va en estas canecas. ${e.guia}`, 'error');
    decir(`${e.nombre}. No va en estas canecas. ${e.guia}`);
    historial.agregar({ tipo: 'consulta', consulta: texto, material: e.nombre, caneca: null, especial: true, canal: 'WEB', origen });
    return;
  }
  if (r.estado === 'ambiguo') {
    const amb = r.items.find((i) => i.tipo === 'ambiguo');
    pendiente = { r, texto, origen, amb };
    mostrarAclaracion(amb.pregunta, amb.opciones.map((o, i) => ({ i, texto: o.texto, caneca: o.residuo?.caneca })));
    decir(`${amb.pregunta} ${amb.opciones.map((o) => o.texto).join(', o ')}.`);
    return;
  }
  if (r.estado === 'desconocido' || r.estado === 'mixto') {
    const que = r.sinReconocer?.length ? r.sinReconocer.join(', ') : texto;
    if (r.sugerencias.length) {
      pendiente = { sugerencias: r.sugerencias, texto, origen };
      mostrarAclaracion(`No reconozco «${que}». ¿Quisiste decir alguno de estos?`, r.sugerencias.map((s, i) => ({ i, texto: s.nombre, caneca: s.caneca })));
    } else mensaje(`No reconozco «${que}». Prueba con otro nombre o toca la caneca que necesitas.`, 'error');
    decir(`No reconozco ${que}.`);
    historial.agregar({ tipo: 'consulta', consulta: texto, material: null, caneca: null, desconocido: true, canal: 'WEB', origen });
    return;
  }
  ejecutar(r.canecas, { residuos: r.items.map((i) => i.residuo), consulta: texto, origen, avisoLimpieza: r.items.some((i) => i.avisoLimpieza) });
}
function mostrarAclaracion(pregunta, opciones) {
  fase('aclaracion'); mensaje('');
  el.aclaracionTexto.textContent = pregunta;
  el.opciones.innerHTML = opciones.map((o) => `<button type="button" data-i="${o.i}"${o.caneca ? ` data-caneca="${o.caneca}"` : ''}>${escapar(o.texto)}</button>`).join('');
  el.aclaracion.hidden = false;
  el.opciones.querySelector('button')?.focus({ preventScroll: true });
  anunciar(pregunta);
}
el.opciones.addEventListener('click', (ev) => {
  const b = ev.target.closest('button'); if (!b || ocupado || !pendiente) return;
  const i = Number(b.dataset.i);
  if (pendiente.sugerencias) {
    const s = pendiente.sugerencias[i]; el.input.value = s.nombre;
    return ejecutar([s.caneca], { residuos: [s], consulta: s.nombre, origen: 'sugerencia' });
  }
  const { r, texto, origen, amb } = pendiente; const o = amb.opciones[i];
  if (!o.residuo) { el.aclaracion.hidden = true; fase('inicio'); mensaje(o.sinCaneca || 'Este residuo no va en estas canecas. Pregunta al operario.', 'error'); pendiente = null; return; }
  const residuos = [...r.items.filter((x) => x !== amb && x.tipo === 'residuo').map((x) => x.residuo), o.residuo];
  ejecutar(ORDEN.filter((c) => residuos.some((x) => x.caneca === c)), { residuos, consulta: texto, origen });
});
el.form.addEventListener('submit', (ev) => { ev.preventDefault(); identificar(el.input.value, 'texto'); });
el.input.addEventListener('input', () => {
  if (ocupado) return;
  if (document.body.dataset.fase === 'aclaracion') { pendiente = null; el.aclaracion.hidden = true; limpiarCanecas(); etiquetarCanecas(); fase('inicio'); }
  if (el.mensaje.textContent) mensaje('');
});

// ───────────── Operación: resaltar → abrir → cuenta regresiva → depositar → cierre → registro → inicio ─────────────
const PASOS = ['caneca', 'apertura', 'deposito', 'cierre', 'registro'];
function paso(nombre, estado) {
  const i = PASOS.indexOf(nombre);
  [...el.pasos.children].forEach((li, k) => {
    if (k < i && !li.classList.contains('fallo')) li.className = 'hecho';
    else if (k === i) li.className = estado;
    else if (k > i) li.className = '';
  });
}
const SELLO = { ilustrativa: 'Vista ilustrativa — sin movimiento físico', demo: 'Simulación — sin movimiento físico', comandada: 'Orden enviada', desconectada: 'Desconectado — vista ilustrativa, sin movimiento físico', error: 'No se abrió', espera: 'Solicitando apertura…' };
function sello(modo) {
  el.opSello.dataset.modo = modo; el.opSello.textContent = SELLO[modo];
  if (['ilustrativa', 'demo', 'comandada', 'desconectada'].includes(modo)) el.canecas.dataset.sello = modo; else delete el.canecas.dataset.sello;
}

function cuentaRegresiva(ms, gen) {
  return new Promise((ok) => {
    const total = 326.73, inicio = performance.now();
    el.segundos.textContent = String(Math.ceil(ms / 1000));
    el.anillo.style.strokeDashoffset = '0';
    const cuadro = (t) => {
      if (gen !== generacion) return ok(false);
      const pasado = Math.min(ms, t - inicio);
      el.segundos.textContent = String(Math.max(0, Math.ceil((ms - pasado) / 1000)));
      el.anillo.style.strokeDashoffset = String(total * (pasado / ms));
      if (pasado >= ms) { el.anuncioCuenta.textContent = 'Tiempo terminado.'; return ok(true); }
      requestAnimationFrame(cuadro);
    };
    requestAnimationFrame(cuadro);
  });
}

async function ejecutar(lista, { residuos = [], consulta = '', origen = 'texto', avisoLimpieza = false } = {}) {
  if (ocupado || !lista.length) return;
  ocupado = true; const gen = ++generacion; limpiarTemporizadores(); bloquear(true);
  pendiente = null; el.aclaracion.hidden = true;
  const modo = modoOperacion();
  const unicos = [...new Map(residuos.map((r) => [r.id, r])).values()];
  const varios = unicos.length > 1;
  const frase = nombreCanecas(lista);
  const nombres = listaY(unicos.map((r, i) => (i ? enFrase(r.nombre) : r.nombre)));
  const tu = varios ? 'tus residuos' : 'tu residuo';
  const registro = { consulta, material: nombres || `Caneca ${listaY(lista)}`, materiales: unicos.map((r) => ({ id: r.id, oracle: r.oracle, nombre: r.nombre, caneca: r.caneca })),
    caneca: lista.join('+'), canal: 'WEB', origen };

  // 1. Resaltar
  fase('operacion'); marcar(lista); etiquetarCanecas();
  el.entrada.hidden = true; el.operacion.hidden = false; el.volver.hidden = true;
  el.operacion.dataset.etapa = 'inicio';
  el.segundos.textContent = String(Math.round(duracionPolitica(lista.length) / 1000)); el.anillo.style.strokeDashoffset = '0';
  paso('caneca', 'hecho'); paso('apertura', 'actual');
  el.opTitulo.textContent = frase[0].toUpperCase() + frase.slice(1);
  const condicion = !varios && unicos[0]?.condicion ? ` ${unicos[0].condicion}` : '';
  const limpieza = avisoLimpieza ? ' Vacíalo y enjuágalo antes.' : '';
  el.opDetalle.innerHTML = unicos.length ? `<strong>${escapar(nombres)}</strong> va en la ${escapar(frase)}.${escapar(condicion + limpieza)}` : `Elegiste la ${escapar(frase)}: ${escapar(CATEGORIA[lista[0]])}.`;
  el.opTitulo.focus({ preventScroll: true });
  el.eco.src = '../assets/marca/eco-tablet-200.webp';
  document.body.dataset.modo = modo === 'comandada' ? 'comandada' : modo;

  // 2. Abrir (orden a la pasarela o plan local para las vistas sin estación)
  let plan, mov = 700, registroTxt;
  if (modo === 'comandada') {
    sello('espera');
    anunciar(`${unicos.length ? nombres + ' va en la ' : ''}${frase}. Solicitando la apertura.`);
    decir(`${unicos.length ? nombres + '. ' : ''}Abriendo la ${frase}.`);
    try {
      plan = await estacion.solicitarApertura({ clave: nuevaClave(), canecas: lista.map((c) => LETRA[c]).join(''), materiales: registro.materiales, consulta, metodo: origen === 'voz' ? 'VOZ' : 'TEXTO' });
    } catch (e) {
      if (gen !== generacion) return;
      sello('error'); paso('apertura', 'fallo'); el.operacion.dataset.etapa = 'error';
      el.opTitulo.textContent = 'La caneca no se abrió';
      el.opDetalle.innerHTML = `${escapar(e.message)} Deposita ${tu} en la <strong>${escapar(frase)}</strong> abriéndola a mano o pide ayuda al operario.`;
      anunciar(`La caneca no se abrió. ${e.message}`); decir(`La caneca no se abrió. ${e.message}`);
      historial.agregar({ ...registro, tipo: 'consulta', error: e.message });
      terminar(gen, 12000); refrescarEstacion();
      return;
    }
    if (gen !== generacion) return;
    sello('comandada');
    mov = Math.max(300, Math.min(3000, Number(plan.movimientoMs) || 700));
    historial.agregar({ ...registro, tipo: 'apertura', clave: plan.clave, estado: 'comandada', sincronizacion: plan.estadoRegistro === 'GUARDADO_ORACLE' ? 'oracle' : 'pendiente' });
    registroTxt = plan.estadoRegistro === 'GUARDADO_ORACLE' ? 'Registro guardado en Oracle.' : 'Registro enviado a la estación; se sincroniza con Oracle.';
  } else {
    sello(modo);
    const dur = duracionPolitica(lista.length);
    plan = { plan: lista.map((c) => ({ caneca: c, inicioMs: 0, abiertaMs: dur })) };
    registroTxt = modo === 'demo' ? 'Simulación: no se guardó ningún registro oficial.' : 'Consulta guardada en Mi historial de este dispositivo.';
    if (modo !== 'demo') historial.agregar({ ...registro, tipo: 'consulta' });
    if (!(await esperar(350, gen))) return;
  }
  const tramos = (plan.plan || []).map((p) => ({ caneca: DE_LETRA[p.caneca] || p.caneca, inicioMs: Number(p.inicioMs) || 0, abiertaMs: Number(p.abiertaMs) || duracionPolitica(lista.length) }))
    .filter((p) => botones[p.caneca]);
  if (!tramos.length) tramos.push(...lista.map((c) => ({ caneca: c, inicioMs: 0, abiertaMs: duracionPolitica(lista.length) })));
  el.operacion.dataset.etapa = 'apertura'; paso('apertura', 'actual');
  el.opDetalle.textContent = modo === 'comandada' ? `Orden de apertura enviada a la ${frase}.`
    : modo === 'demo' ? `Simulación: la ${frase} se abre solo en pantalla.`
      : `Vista ilustrativa — sin movimiento físico. Abre la ${frase} a mano.`;
  const t0 = performance.now();
  for (const p of tramos) {
    esperar(p.inicioMs, gen).then((v) => v && abrirTapa(p.caneca, mov));
    esperar(p.inicioMs + mov + p.abiertaMs, gen).then((v) => v && cerrarTapa(p.caneca, mov));
  }
  const primero = Math.min(...tramos.map((p) => p.inicioMs));
  const fin = Math.max(...tramos.map((p) => p.inicioMs + mov + p.abiertaMs));
  if (!(await esperar(primero + mov, gen))) return;

  // 3. Cuenta regresiva: «Deposita tu residuo ahora»
  const restante = fin - (performance.now() - t0);
  const s = Math.round(restante / 1000);
  el.operacion.dataset.etapa = 'deposito'; paso('deposito', 'actual');
  el.opTitulo.textContent = varios ? 'Deposita tus residuos ahora' : 'Deposita tu residuo ahora';
  el.opDetalle.textContent = modo === 'comandada' ? `Cuando termine el tiempo, la estación cierra la ${frase} sola.`
    : `Deposita ${tu} en la ${frase}. ${modo === 'demo' ? 'Es una simulación: la tapa física no se mueve.' : 'La tapa física no se mueve.'}`;
  el.anuncioCuenta.textContent = `Tienes ${s} segundos para depositar ${tu}.`;
  decir(`Deposita ${tu} ahora. Tienes ${enLetras(s)} segundos.`);
  if (!(await cuentaRegresiva(restante, gen))) return;

  // 4. Cierre (lo hace el controlador de la estación; aquí solo se representa)
  el.operacion.dataset.etapa = 'cierre'; paso('cierre', 'actual');
  el.opTitulo.textContent = `Cerrando la ${frase}`;
  el.opDetalle.textContent = modo === 'comandada' ? 'El controlador de la estación cierra la tapa.' : 'Cierre en pantalla, sin movimiento físico.';
  if (!(await esperar(mov, gen))) return;

  // 5. Estado del registro
  el.operacion.dataset.etapa = 'registro'; paso('registro', 'hecho');
  el.opTitulo.textContent = 'Listo, gracias';
  el.opDetalle.textContent = registroTxt;
  el.eco.src = '../assets/marca/eco-celebra-200.webp';
  anunciar(`Listo. ${registroTxt}`);
  terminar(gen, 7000);
}
function terminar(gen, regresoMs) {
  ocupado = false; bloquear(false); el.input.disabled = true; el.enviar.disabled = true; el.mic.disabled = true;
  el.volver.hidden = false;
  esperar(regresoMs, gen).then((v) => { if (v && !ocupado) irInicio(); });
}
el.volver.addEventListener('click', () => irInicio({ enfocar: true }));

// ───────────── Modalidad C — voz (Web Speech API, es-CO con respaldo es-ES) ─────────────
const SR = globalThis.SpeechRecognition || globalThis.webkitSpeechRecognition;
const ERRORES_VOZ = {
  'not-allowed': 'No tengo permiso para usar el micrófono. Escribe tu residuo aquí o toca la caneca.',
  'service-not-allowed': 'El dictado no está habilitado en este navegador. Escribe tu residuo aquí o toca la caneca.',
  'audio-capture': 'No encontré un micrófono. Escribe tu residuo aquí o toca la caneca.',
  'no-speech': 'No te escuché. Toca el micrófono de nuevo o escribe tu residuo.',
  network: 'El dictado necesita internet. Escribe tu residuo aquí o toca la caneca.',
  aborted: '',
};
/** Escucha una frase. Solo onFinal entrega texto utilizable; los parciales son solo para mostrarlos. */
function escucharFrase(h, idioma = 'es-CO') {
  const r = new SR();
  r.lang = idioma; r.interimResults = true; r.continuous = false; r.maxAlternatives = 3;
  let entregado = false, fallo = false, otroIdioma = false, siguiente = null;
  const limite = setTimeout(() => { try { r.stop(); } catch { /* ya detenido */ } }, 12000);
  r.onresult = (e) => {
    let parcial = '';
    for (let i = e.resultIndex; i < e.results.length; i++) {
      const res = e.results[i];
      if (res.isFinal && !entregado) {
        const alternativas = Array.from(res).filter((a) => a.transcript.trim()).sort((a, b) => (b.confidence || 0) - (a.confidence || 0)).map((a) => a.transcript.trim());
        if (!alternativas.length) continue;
        entregado = true; h.onFinal(alternativas[0], alternativas);
      } else if (!res.isFinal) parcial += res[0].transcript;
    }
    if (parcial && !entregado) h.onParcial(parcial.trim());
  };
  r.onerror = (e) => {
    if (e.error === 'language-not-supported' && idioma === 'es-CO') { otroIdioma = true; return; }
    fallo = true; const m = ERRORES_VOZ[e.error] ?? 'No pude usar el dictado. Escribe tu residuo, por favor.';
    if (m) h.onError(e.error, m);
  };
  r.onend = () => {
    clearTimeout(limite);
    if (otroIdioma) { siguiente = escucharFrase(h, 'es-ES'); return; }
    h.onFin(entregado, fallo);
  };
  try { r.start(); } catch { clearTimeout(limite); h.onError('inicio', 'No pude iniciar el micrófono. Escribe tu residuo, por favor.'); h.onFin(false, true); }
  return { detener() { try { (siguiente ?? r).stop(); } catch { /* ya detenido */ } } };
}
function micActivo(si) { el.mic.setAttribute('aria-pressed', String(si)); el.mic.setAttribute('aria-label', si ? 'Dejar de escuchar' : 'Decir el residuo por voz'); el.escucha.hidden = !si; }
el.mic.addEventListener('click', () => {
  if (ocupado || !motor) return;
  if (escucha) { escucha.detener(); return; }
  if (!SR || globalThis.isSecureContext === false) { mensaje('Tu navegador no ofrece dictado por voz. Escribe tu residuo aquí o toca la caneca.', 'error'); el.input.focus(); return; }
  callar(); escuchaCancelada = false; mensaje('');
  if (document.body.dataset.fase !== 'inicio') irInicio();
  micActivo(true); el.escuchaTexto.textContent = 'Te escucho… di qué residuo vas a desechar';
  anunciar('Escuchando.');
  escucha = escucharFrase({
    onParcial: (t) => { if (!escuchaCancelada) el.escuchaTexto.textContent = `«${t}»`; },   // nunca se actúa sobre un parcial
    onFinal: (t, alternativas) => {
      if (escuchaCancelada) return;
      el.input.value = t; el.escuchaTexto.textContent = `«${t}»`;
      identificar(t, 'voz', alternativas);
    },
    onError: (codigo, m) => {
      if (escuchaCancelada) return;
      mensaje(m, 'error'); anunciar(m);
      if (codigo === 'not-allowed' || codigo === 'service-not-allowed' || codigo === 'audio-capture') el.input.focus();
    },
    onFin: (entregado, fallo) => {
      escucha = null; micActivo(false);
      if (!entregado && !fallo && !escuchaCancelada) mensaje('No te escuché. Toca el micrófono de nuevo o escribe tu residuo.');
    },
  });
});

// Respuesta hablada (speechSynthesis), desactivable; la preferencia se comparte con la página principal.
el.alternarVoz.setAttribute('aria-pressed', String(vozActiva));
el.alternarVoz.addEventListener('click', () => {
  vozActiva = !vozActiva; el.alternarVoz.setAttribute('aria-pressed', String(vozActiva));
  try { localStorage.setItem('ecoaccess.voz', vozActiva ? 'si' : 'no'); } catch { /* sin almacenamiento */ }
  if (vozActiva) hablar('Respuesta hablada activada.', true); else callar();
});

// ───────────── Mi historial (local, el mismo que usa la página principal) ─────────────
const TIPO = { consulta: 'Vista ilustrativa', apertura: 'Orden enviada', deposito: 'Depósito confirmado' };
function pintarHistorial() {
  const lista = historial.leer().slice(0, 40);
  el.histLista.innerHTML = lista.length ? lista.map((h) => {
    const canecas = (h.caneca || '').split('+').filter((c) => botones[c]);
    const tono = canecas.length === 1 ? `style="--c:var(--caneca-${canecas[0]});--c-borde:${canecas[0] === 'blanca' ? 'var(--caneca-blanca-borde)' : 'transparent'}"` : '';
    const tipo = h.error ? 'No se abrió' : h.demostracion ? 'Simulación' : h.especial ? 'Residuo especial' : h.desconocido ? 'No reconocido' : TIPO[h.tipo] || 'Consulta';
    const f = new Date(h.fecha);
    const cuando = isNaN(f) ? '' : `${f.toLocaleDateString('es-CO', { day: 'numeric', month: 'short' })}, ${f.toLocaleTimeString('es-CO', { hour: 'numeric', minute: '2-digit' })}`;
    const donde = canecas.length ? `Caneca ${listaY(canecas)}` : 'Sin caneca';
    return `<li><span class="tono" ${tono} aria-hidden="true"></span><span><strong>${escapar(h.material || h.consulta || 'Residuo')}</strong><span class="meta">${escapar(donde)} · <time datetime="${isNaN(f) ? '' : f.toISOString()}">${escapar(cuando)}</time></span></span><span class="tipo" data-tipo="${escapar(h.tipo || '')}">${escapar(tipo)}</span></li>`;
  }).join('') : '<li class="vacio">Aún no hay registros en este dispositivo.</li>';
}
el.abrirHistorial.addEventListener('click', () => { pintarHistorial(); el.dialogo.showModal(); });
el.dialogo.addEventListener('click', (ev) => { if (ev.target === el.dialogo) el.dialogo.close(); });

// ───────────── Teclado y entorno ─────────────
document.addEventListener('keydown', (ev) => {
  if (ev.key === 'Escape' && !ocupado && !el.dialogo.open && document.body.dataset.fase !== 'inicio') { escucha?.detener(); escuchaCancelada = true; irInicio({ enfocar: true }); }
});
document.addEventListener('visibilitychange', () => { if (!document.hidden) refrescarEstacion(); });
addEventListener('online', refrescarEstacion);

/** Versión publicada (la genera el workflow). Si no existe, no se muestra nada. */
async function mostrarVersion() {
  try {
    const r = await fetch('../version.json', { cache: 'no-store' });
    if (!r.ok) return;
    const d = await r.json();
    const v = String(d.version || d.commit || d.sha || '').trim();
    if (!v || v.length > 60) return;
    el.version.textContent = `Versión ${/^[0-9a-f]{40}$/i.test(v) ? v.slice(0, 7) : v}`;
    el.version.hidden = false;
  } catch { /* sin version.json */ }
}

async function iniciar() {
  const sesion = estacion.iniciarSesionDesdeUrl();   // QR presencial: #e=…&t=… (se borra de la URL)
  if (sesion) estadoEstacion = { estado: 'verificando' };
  irInicio();
  bloquear(true);
  mostrarVersion();
  try {
    const r = await fetch('../datos/catalogo-residuos.json', { cache: 'no-cache' });
    motor = crearMotor(await r.json());
  } catch {
    mensaje('No pude cargar el catálogo de residuos. Puedes tocar la caneca que necesitas.', 'error');
  }
  bloquear(false);
  if (!motor) { el.input.disabled = true; el.enviar.disabled = true; el.mic.disabled = true; }
  if (!SR) el.mic.title = 'El dictado por voz no está disponible en este navegador';
  if (sesion) { await refrescarEstacion(); setInterval(refrescarEstacion, 5000); }
  if ('serviceWorker' in navigator && location.protocol === 'https:') navigator.serviceWorker.register('../sw.js', { scope: '../' }).catch(() => {});
  window.__dosClics = { listo: true, version: motor?.catalogo.version ?? null };   // apoyo a pruebas automatizadas
}
iniciar();
