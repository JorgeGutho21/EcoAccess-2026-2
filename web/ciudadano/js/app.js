// EcoAccess · interfaz ciudadana. Tres modos, siempre identificados en pantalla:
//  - CONSULTA (por defecto): clasifica y muestra la caneca; animación ilustrativa, sin movimiento físico.
//  - FÍSICO (QR presencial): pide la apertura a la pasarela de la estación; la cuenta regresiva empieza solo cuando la
//    estación confirma la orden comandada y pasa el tiempo de movimiento informado. Sin sensores no se afirma depósito.
//  - DEMOSTRACIÓN (?demo=1): igual que el físico pero simulado y rotulado como tal.
import { crearMotor, duracionPolitica, normalizar } from './motor.js';
import { dictadoDisponible, escuchar, hablar, callar } from './voz.js';
import * as estacion from './estacion.js';
import * as historial from './historial.js';

const $ = (id) => document.getElementById(id);
const el = {
  principal: $('principal'), form: $('formulario'), input: $('residuo'), mic: $('microfono'), identificar: $('identificar'),
  sugerencias: $('sugerencias'), vozEstado: $('vozEstado'), vozEtiqueta: $('vozEtiqueta'), vozTexto: $('vozTexto'), detenerVoz: $('detenerVoz'),
  eco: $('ecoMensaje'), ecoImg: $('ecoImagen'), alternarVoz: $('alternarVoz'), modo: $('modo'),
  resultado: $('resultado'), chip: $('resChip'), material: $('resMaterial'), indicacion: $('resIndicacion'),
  cuenta: $('resCuenta'), anillo: $('anillo'), segundos: $('segundos'), segundosSr: $('segundosSr'), pasos: $('resPasos'), opciones: $('resOpciones'),
  confirmar: $('confirmar'), nueva: $('nuevaConsulta'), estadoBtn: $('estadoEstacion'), estadoTexto: $('estadoTexto'),
  dialogo: $('historial'), abrirHist: $('abrirHistorial'), histLista: $('histLista'), histResumen: $('histResumen'), filtroFecha: $('filtroFecha'), borrarHist: $('borrarHistorial'),
  aviso: $('avisoRed'),
};
const canecasEl = Object.fromEntries([...document.querySelectorAll('.caneca')].map((n) => [n.dataset.caneca, n]));
const LETRA = { verde: 'V', blanca: 'B', negra: 'N' };
const TEXTO_CANECA = {
  verde: { nombre: 'verde', tipo: 'orgánico aprovechable' },
  blanca: { nombre: 'blanca', tipo: 'aprovechable' },
  negra: { nombre: 'negra', tipo: 'no aprovechable' },
};
const NUMEROS = ['cero', 'un', 'dos', 'tres', 'cuatro', 'cinco', 'seis', 'siete', 'ocho', 'nueve', 'diez'];
const demo = new URLSearchParams(location.search).has('demo');

let motor = null;
let ocupado = false;            // hay una operación en curso: no se aceptan nuevas órdenes
let generacion = 0;             // invalida temporizadores de operaciones anteriores
let temporizadores = [];
let estadoEstacion = { estado: demo ? 'demo' : 'consulta' };
let vozActiva = leerPreferenciaVoz();
let escucha = null;
let pendienteAmbiguo = null;
let claveActual = null;

// ───────────────────────── Utilidades ─────────────────────────
const esperar = (ms, gen) => new Promise((ok) => { const t = setTimeout(() => ok(gen === generacion), ms); temporizadores.push(t); });
function limpiarTemporizadores() { temporizadores.forEach(clearTimeout); temporizadores = []; }
function aviso(texto, ms = 4200) {
  el.aviso.textContent = texto; el.aviso.hidden = false;
  clearTimeout(aviso.t); aviso.t = setTimeout(() => { el.aviso.hidden = true; }, ms);
}
function decirEco(html, voz, imagen = 'saluda') {
  el.eco.innerHTML = html;
  el.ecoImg.src = `assets/marca/eco-${imagen}.webp`;
  hablar(voz ?? el.eco.textContent, vozActiva);
}
const escapar = (s) => String(s).replace(/[&<>"']/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
function leerPreferenciaVoz() { try { return localStorage.getItem('ecoaccess.voz') !== 'no'; } catch { return true; } }
function nuevaClave() { return crypto.randomUUID?.().replace(/-/g, '') ?? (Date.now().toString(36) + Math.random().toString(36).slice(2)); }

// ───────────────────────── Modo y estado de la estación ─────────────────────────
function modoActual() {
  if (demo) return 'demo';
  if (estacion.sesionActual()) return 'fisico';
  return 'consulta';
}
function pintarEstado() {
  const m = modoActual();
  const e = estadoEstacion.estado;
  let clase = 'consulta', texto = 'Solo consulta';
  if (m === 'demo') { clase = 'demo'; texto = 'Demostración'; }
  else if (m === 'fisico') {
    if (e === 'disponible') { clase = 'disponible'; texto = 'Estación disponible'; }
    else if (e === 'ocupada') { clase = 'ocupada'; texto = 'Estación ocupada'; }
    else if (e === 'sin-sesion-supervisada') { clase = 'ocupada'; texto = 'Estación en pausa'; }
    else if (e === 'verificando') { clase = 'consulta'; texto = 'Conectando…'; }
    else { clase = 'desconectada'; texto = 'Estación desconectada'; }
  }
  el.estadoBtn.dataset.estado = clase; el.estadoTexto.textContent = texto;
  const etiquetas = {
    consulta: ['consulta', 'Consulta · vista ilustrativa, sin movimiento físico'],
    demo: ['demo', 'Demostración · sin movimiento físico'],
    fisico: ['fisico', 'Estación física vinculada'],
  };
  const [tipo, rotulo] = etiquetas[m];
  el.modo.dataset.tipo = tipo; el.modo.textContent = rotulo; el.modo.hidden = false;
}
async function refrescarEstacion() {
  if (modoActual() !== 'fisico' || document.hidden) return;
  try { estadoEstacion = await estacion.consultarEstado(); }
  catch (e) { estadoEstacion = { estado: e.codigo === 'permiso' ? 'consulta' : 'desconectada', texto: e.message }; if (e.codigo === 'permiso') aviso(e.message); }
  pintarEstado();
}

// ───────────────────────── Fases de la interfaz ─────────────────────────
function irInicio({ enfocar = true } = {}) {
  generacion++; limpiarTemporizadores(); ocupado = false; pendienteAmbiguo = null; claveActual = null;
  el.principal.dataset.fase = 'inicio';
  for (const n of Object.values(canecasEl)) n.classList.remove('elegida', 'oculta', 'abierta', 'cerrando');
  el.resultado.hidden = true; el.cuenta.hidden = true; el.confirmar.hidden = true; el.opciones.innerHTML = ''; el.pasos.innerHTML = '';
  bloquear(false);
  el.input.value = '';
  decirEco('¿Qué vas a desechar ahora? Escríbelo o tócame el micrófono.', '', 'saluda');
  callar();
  if (enfocar && matchMedia('(pointer: fine)').matches) el.input.focus();
}
function bloquear(v) { el.input.disabled = v; el.identificar.disabled = v; el.mic.disabled = v; document.querySelectorAll('.atajos .chip').forEach((c) => { c.disabled = v; }); }

function mostrarCanecas(lista) {
  el.principal.dataset.fase = 'resultado';
  el.principal.style.setProperty('--ancho-elegida', ['46%', '40%', '31%'][lista.length - 1]);
  el.principal.style.setProperty('--max-elegida', ['240px', '200px', '180px'][lista.length - 1]);
  for (const [c, n] of Object.entries(canecasEl)) {
    n.classList.toggle('elegida', lista.includes(c));
    n.classList.toggle('oculta', !lista.includes(c));
    n.classList.remove('abierta', 'cerrando');
  }
}
function mostrarResultado({ chip, chipCaneca, material, indicacion }) {
  el.chip.textContent = chip; el.chip.dataset.caneca = chipCaneca;
  el.material.textContent = material;
  el.indicacion.innerHTML = indicacion;
  el.opciones.innerHTML = ''; el.pasos.innerHTML = ''; el.cuenta.hidden = true; el.confirmar.hidden = true;
  el.resultado.hidden = false;
  // En teléfonos y tabletas verticales el escenario queda bajo el buscador: se lleva a la vista.
  if (innerWidth < 1024) requestAnimationFrame(() => $('escenario').scrollIntoView({ behavior: matchMedia('(prefers-reduced-motion: reduce)').matches ? 'auto' : 'smooth', block: 'start' }));
}
const PASOS = {
  fisico: ['Residuo identificado', 'Apertura solicitada', 'Apertura comandada', 'Deposita tu residuo', 'Cierre', 'Operación completada', 'Registro'],
  demo: ['Residuo identificado', 'Apertura simulada', 'Deposita tu residuo', 'Cierre simulado', 'Operación completada'],
};
function pasos(modo) { el.pasos.innerHTML = PASOS[modo].map((p) => `<li>${p}</li>`).join(''); }
function paso(i, estado = 'hecho', texto) {
  const li = el.pasos.children[i]; if (!li) return;
  li.className = estado; if (texto) li.textContent = texto;
  for (let k = 0; k < i; k++) if (!el.pasos.children[k].classList.contains('fallo')) el.pasos.children[k].className = 'hecho';
}

function abrirTapa(c, movMs) { const n = canecasEl[c]; n.style.setProperty('--t-mov', movMs + 'ms'); n.classList.remove('cerrando'); n.classList.add('abierta'); }
function cerrarTapa(c, movMs) { const n = canecasEl[c]; n.style.setProperty('--t-mov', movMs + 'ms'); n.classList.add('cerrando'); n.classList.remove('abierta'); }

/** Cuenta regresiva visual (anillo + segundos). Resuelve al terminar o si la operación fue reemplazada. */
function cuentaRegresiva(ms, gen) {
  return new Promise((ok) => {
    el.cuenta.hidden = false;
    const total = 326.73, inicio = performance.now();
    el.segundosSr.textContent = `Tienes ${Math.round(ms / 1000)} segundos.`;
    const cuadro = (t) => {
      if (gen !== generacion) return ok(false);
      const pasado = Math.min(ms, t - inicio);
      el.segundos.textContent = Math.max(0, Math.ceil((ms - pasado) / 1000));
      el.anillo.style.strokeDashoffset = String(total * (pasado / ms));
      if (pasado >= ms) { el.segundosSr.textContent = 'Tiempo terminado.'; return ok(true); }
      requestAnimationFrame(cuadro);
    };
    requestAnimationFrame(cuadro);
  });
}

// ───────────────────────── Identificación ─────────────────────────
function identificar(texto, origen = 'texto', alternativas = []) {
  if (ocupado || !motor) return;
  cerrarSugerencias();
  let r = motor.clasificar(texto);
  // Con voz, si la mejor transcripción no se entiende, prueba las alternativas del reconocedor.
  if (origen === 'voz' && r.estado !== 'reconocido') {
    for (const alt of alternativas.slice(1)) { const r2 = motor.clasificar(alt); if (r2.estado === 'reconocido' || r2.estado === 'especial') { r = r2; texto = alt; el.input.value = alt; break; } }
  }
  if (r.estado === 'vacio') { decirEco('Escribe o dime qué residuo vas a desechar.'); el.input.focus(); return; }
  if (r.estado === 'especial') return mostrarEspecial(r, texto);
  if (r.estado === 'ambiguo') return mostrarAmbiguo(r, texto, origen);
  if (r.estado === 'desconocido' || r.estado === 'mixto') return mostrarDesconocido(r, texto);
  ejecutar(r.items.map((i) => i.residuo), texto, origen, r.items.some((i) => i.avisoLimpieza));
}

function mostrarEspecial(r, texto) {
  const e = r.items.find((i) => i.tipo === 'especial').especial;
  el.principal.dataset.fase = 'aviso';
  for (const n of Object.values(canecasEl)) n.classList.remove('elegida', 'oculta', 'abierta');
  mostrarResultado({ chip: 'Residuo especial · no va en estas canecas', chipCaneca: 'especial', material: e.nombre, indicacion: escapar(e.guia) });
  decirEco(`<strong>${escapar(e.nombre)}</strong>: no abriré ninguna caneca. ${escapar(e.guia)}`, `${e.nombre}. No abriré ninguna caneca. ${e.guia}`, 'tablet');
  historial.agregar({ tipo: 'consulta', consulta: texto, material: e.nombre, caneca: null, especial: true });
  el.resultado.focus({ preventScroll: true });
  programarRegreso(20000);
}

function mostrarAmbiguo(r, texto, origen) {
  const amb = r.items.find((i) => i.tipo === 'ambiguo');
  pendienteAmbiguo = { r, texto, origen, amb };
  el.principal.dataset.fase = 'aviso';
  mostrarResultado({ chip: 'Necesito un detalle', chipCaneca: 'ambiguo', material: amb.pregunta, indicacion: 'Elige la opción que mejor describe tu residuo.' });
  el.opciones.innerHTML = amb.opciones.map((o, i) => `<button type="button" class="chip" data-i="${i}" ${o.residuo ? `data-caneca="${o.residuo.caneca}"` : ''}>${escapar(o.texto)}</button>`).join('');
  decirEco(escapar(amb.pregunta), amb.pregunta + ' ' + amb.opciones.map((o) => o.texto).join(', o ') + '.', 'tablet');
  el.opciones.querySelector('button')?.focus({ preventScroll: true });
}

function mostrarDesconocido(r, texto) {
  el.principal.dataset.fase = 'aviso';
  const que = r.sinReconocer?.length ? r.sinReconocer.join(', ') : texto;
  mostrarResultado({ chip: 'No lo reconocí', chipCaneca: 'desconocido', material: `No reconozco «${que}»`,
    indicacion: r.sugerencias.length ? '¿Quisiste decir alguno de estos?' : 'Prueba con otro nombre, por ejemplo «botella de plástico» o «cáscara de papa». Si tienes dudas, pregunta al operario.' });
  el.opciones.innerHTML = r.sugerencias.map((s) => `<button type="button" class="chip" data-id="${s.id}" data-caneca="${s.caneca}">${escapar(s.nombre)}</button>`).join('');
  decirEco(`No reconozco <strong>«${escapar(que)}»</strong>. ${r.sugerencias.length ? 'Elige una sugerencia o escríbelo de otra forma.' : 'Escríbelo de otra forma.'}`, undefined, 'tablet');
  historial.agregar({ tipo: 'consulta', consulta: texto, material: null, caneca: null, desconocido: true });
}

el.opciones.addEventListener('click', (ev) => {
  const b = ev.target.closest('button'); if (!b || ocupado) return;
  if (b.dataset.id) { const r = motor.porId.get(b.dataset.id); el.input.value = r.nombre; return ejecutar([r], r.nombre, 'sugerencia'); }
  if (pendienteAmbiguo && b.dataset.i != null) {
    const { r, texto, origen, amb } = pendienteAmbiguo; const o = amb.opciones[+b.dataset.i];
    if (!o.residuo) { mostrarResultado({ chip: 'Sin caneca en esta estación', chipCaneca: 'especial', material: o.texto, indicacion: escapar(o.sinCaneca) }); decirEco(escapar(o.sinCaneca)); programarRegreso(15000); return; }
    const otros = r.items.filter((i) => i !== amb && i.tipo === 'residuo').map((i) => i.residuo);
    ejecutar([...otros, o.residuo], texto, origen);
  }
});

// ───────────────────────── Ejecución de la operación ─────────────────────────
async function ejecutar(residuos, texto, origen, avisoLimpieza = false) {
  if (ocupado) return;
  const unicos = [...new Map(residuos.map((r) => [r.id, r])).values()];
  const lista = ['verde', 'blanca', 'negra'].filter((c) => unicos.some((r) => r.caneca === c));
  const gen = ++generacion; limpiarTemporizadores(); ocupado = true; bloquear(true); pendienteAmbiguo = null;
  const modo = modoActual();
  const nombres = unicos.map((r) => r.nombre).join(' · ');
  const frase = lista.map((c) => TEXTO_CANECA[c].nombre).join(lista.length === 2 ? ' y ' : ', ').replace(/, ([^,]*)$/, ' y $1');
  const tipoTxt = lista.length === 1 ? `Tu residuo es ${TEXTO_CANECA[lista[0]].tipo}` : 'Tus residuos van en varias canecas';
  const chip = lista.length === 1 ? `Caneca ${TEXTO_CANECA[lista[0]].nombre} · ${motor.catalogo.canecas[lista[0]].categoria}` : `Canecas ${frase}`;
  const condiciones = unicos.map((r) => r.condicion).filter(Boolean);
  const nota = (avisoLimpieza ? 'Vacíalo y enjuágalo antes de depositarlo. ' : '') + (condiciones.length === 1 ? condiciones[0] : '');
  mostrarCanecas(lista);
  mostrarResultado({ chip, chipCaneca: lista.length === 1 ? lista[0] : 'blanca', material: nombres,
    indicacion: `Deposita tu residuo en la caneca <strong>${frase}</strong>.${nota ? ` <span class="nota">${escapar(nota)}</span>` : ''}` });

  const registro = { consulta: texto, material: nombres, materiales: unicos.map((r) => ({ id: r.id, oracle: r.oracle })), caneca: lista.length === 1 ? lista[0] : lista.join('+'), canal: 'WEB', origen };

  if (modo === 'consulta') {
    decirEco(`<strong>${tipoTxt}.</strong> Va en la caneca ${frase}. Para abrir la caneca física, escanea el código QR de la estación.`,
      `${tipoTxt}. Va en la caneca ${frase}.`, 'tablet');
    historial.agregar({ ...registro, tipo: 'consulta' });
    // Animación ilustrativa (rotulada como consulta): no representa movimiento físico.
    await esperar(500, gen) && lista.forEach((c) => abrirTapa(c, 900));
    if (!(await esperar(2600, gen))) return;
    lista.forEach((c) => cerrarTapa(c, 800));
    ocupado = false; bloquear(false);
    programarRegreso(9000);
    return;
  }

  // FÍSICO o DEMOSTRACIÓN
  pasos(modo); paso(0); paso(1, 'actual');
  claveActual = nuevaClave();
  let plan;
  if (modo === 'demo') {
    const dur = duracionPolitica(lista.length);
    plan = { modo: 'demostracion', movimientoMs: 900, plan: lista.map((c) => ({ caneca: c, inicioMs: 0, abiertaMs: dur })), clave: claveActual };
    if (!(await esperar(350, gen))) return;
  } else {
    decirEco(`${tipoTxt}. Solicitando la apertura de la caneca ${frase}…`, `${tipoTxt}. Se abrirá la caneca ${frase}.`, 'tablet');
    try {
      plan = await estacion.solicitarApertura({ clave: claveActual, canecas: lista.map((c) => LETRA[c]).join(''), materiales: registro.materiales, consulta: texto });
    } catch (e) {
      if (gen !== generacion) return;
      paso(1, 'fallo', 'Apertura no realizada');
      el.indicacion.innerHTML = `La estación no abrió la caneca: ${escapar(e.message)} Puedes depositar tu residuo en la caneca <strong>${frase}</strong> abriéndola manualmente o pedir ayuda al operario.`;
      decirEco(`No pude abrir la caneca: ${escapar(e.message)}`, undefined, 'tablet');
      historial.agregar({ ...registro, tipo: 'consulta', error: e.message });
      ocupado = false; bloquear(false); refrescarEstacion();
      programarRegreso(15000);
      return;
    }
    if (gen !== generacion) return;
    paso(2, 'hecho');
    historial.agregar({ ...registro, tipo: 'apertura', clave: plan.clave, estado: 'comandada' });
  }
  if (modo === 'demo') paso(1, 'hecho');

  // Sincronía con el hardware: cada tapa se anima cuando su orden empieza (inicioMs) y durante el movimiento calibrado.
  const mov = Math.max(300, Math.min(3000, plan.movimientoMs ?? 900));
  const segundosTxt = (ms) => NUMEROS[Math.round(ms / 1000)] ?? String(Math.round(ms / 1000));
  const ventana = Math.max(...plan.plan.map((p) => p.abiertaMs));
  decirEco(`<strong>${tipoTxt}.</strong> Se ${lista.length > 1 ? 'abren las canecas' : 'abre la caneca'} ${frase}. Tienes ${Math.round(ventana / 1000)} segundos para depositarlo.`,
    `${tipoTxt}. Se ${lista.length > 1 ? 'abren las canecas' : 'abrirá la caneca'} ${frase}. Tienes ${segundosTxt(ventana)} segundos para depositarlo.`, 'tablet');
  const t0 = performance.now();
  const fin = Math.max(...plan.plan.map((p) => p.inicioMs + mov + p.abiertaMs));
  for (const p of plan.plan) {
    const c = typeof p.caneca === 'string' && p.caneca.length === 1 ? Object.keys(LETRA).find((k) => LETRA[k] === p.caneca) : p.caneca;
    esperar(p.inicioMs, gen).then((vigente) => vigente && abrirTapa(c, mov));
    esperar(p.inicioMs + mov + p.abiertaMs, gen).then((vigente) => vigente && cerrarTapa(c, mov));
  }
  const primero = Math.min(...plan.plan.map((p) => p.inicioMs));
  if (!(await esperar(primero + mov, gen))) return;
  paso(modo === 'demo' ? 2 : 3, 'actual');
  if (!(await cuentaRegresiva(fin - (performance.now() - t0), gen))) return;
  paso(modo === 'demo' ? 3 : 4, 'actual');
  if (!(await esperar(mov, gen))) return;
  el.cuenta.hidden = true;
  paso(modo === 'demo' ? 4 : 5, 'hecho');
  if (modo === 'fisico') paso(6, 'hecho', 'Registro enviado a la estación');
  el.indicacion.innerHTML = `<strong>Operación completada.</strong> ${modo === 'demo' ? 'Demostración sin movimiento físico.' : 'Si ya depositaste tu residuo, confírmalo para tu historial.'}`;
  decirEco('<strong>Operación completada.</strong> Puedes consultar tu historial.', 'Operación completada. Puedes consultar tu historial.', 'celebra');
  if (modo === 'demo') historial.agregar({ ...registro, tipo: 'consulta', demostracion: true });
  el.confirmar.hidden = modo !== 'fisico';
  ocupado = false; bloquear(false);
  programarRegreso(15000);
}

el.confirmar.addEventListener('click', async () => {
  const clave = claveActual; if (!clave) return;
  el.confirmar.disabled = true;
  try {
    await estacion.confirmarDeposito(clave);
    historial.actualizar(clave, { tipo: 'deposito', estado: 'confirmado por la persona' });
    el.indicacion.innerHTML = '<strong>¡Gracias!</strong> Registramos tu disposición confirmada.';
    decirEco('¡Gracias! Registré tu disposición.', undefined, 'celebra');
    el.confirmar.hidden = true;
  } catch (e) { aviso('No se pudo registrar la confirmación: ' + e.message); }
  finally { el.confirmar.disabled = false; }
  programarRegreso(6000);
});

function programarRegreso(ms) {
  const gen = generacion;
  esperar(ms, gen).then((vigente) => { if (vigente && !ocupado && document.activeElement !== el.input) irInicio({ enfocar: false }); });
}

// ───────────────────────── Entrada: texto, sugerencias y voz ─────────────────────────
el.form.addEventListener('submit', (ev) => { ev.preventDefault(); const s = el.sugerencias.querySelector('[aria-selected="true"]'); if (s) { el.input.value = s.dataset.nombre; } identificar(el.input.value, 'texto'); });
document.querySelectorAll('.atajos .chip').forEach((c) => c.addEventListener('click', () => { el.input.value = c.dataset.residuo; identificar(c.dataset.residuo, 'atajo'); }));
el.nueva.addEventListener('click', () => irInicio());

let tSug = 0;
el.input.addEventListener('input', () => {
  if (el.principal.dataset.fase !== 'inicio' && !ocupado) { generacion++; limpiarTemporizadores(); el.principal.dataset.fase = 'inicio'; el.resultado.hidden = true; for (const n of Object.values(canecasEl)) n.classList.remove('elegida', 'oculta', 'abierta', 'cerrando'); }
  clearTimeout(tSug); tSug = setTimeout(mostrarSugerencias, 70);
});
function mostrarSugerencias() {
  const q = el.input.value.trim();
  if (!motor || q.length < 2) return cerrarSugerencias();
  const lista = motor.sugerir(q, 6);
  if (!lista.length) return cerrarSugerencias();
  el.sugerencias.innerHTML = lista.map((r, i) => `<li role="option" id="sug-${i}" data-nombre="${escapar(r.nombre)}" aria-selected="false"><span class="tono" style="background:${r.caneca === 'verde' ? 'var(--c-verde)' : r.caneca === 'negra' ? 'var(--c-negra)' : '#fff;border:2px solid var(--c-blanca)'}"></span>${escapar(r.nombre)}<span class="cat">${TEXTO_CANECA[r.caneca].nombre}</span></li>`).join('');
  el.sugerencias.hidden = false; el.input.setAttribute('aria-expanded', 'true');
}
function cerrarSugerencias() { el.sugerencias.hidden = true; el.sugerencias.innerHTML = ''; el.input.setAttribute('aria-expanded', 'false'); el.input.removeAttribute('aria-activedescendant'); }
el.sugerencias.addEventListener('mousedown', (ev) => ev.preventDefault());
el.sugerencias.addEventListener('click', (ev) => { const li = ev.target.closest('li'); if (!li) return; el.input.value = li.dataset.nombre; identificar(li.dataset.nombre, 'sugerencia'); });
el.input.addEventListener('keydown', (ev) => {
  const items = [...el.sugerencias.children];
  if (ev.key === 'Escape') { if (!el.sugerencias.hidden) cerrarSugerencias(); else el.input.value = ''; return; }
  if (!items.length || !['ArrowDown', 'ArrowUp'].includes(ev.key)) return;
  ev.preventDefault();
  let i = items.findIndex((x) => x.getAttribute('aria-selected') === 'true');
  i = ev.key === 'ArrowDown' ? (i + 1) % items.length : (i <= 0 ? items.length - 1 : i - 1);
  items.forEach((x, k) => x.setAttribute('aria-selected', String(k === i)));
  el.input.setAttribute('aria-activedescendant', items[i].id);
});
el.input.addEventListener('blur', () => setTimeout(cerrarSugerencias, 120));

function detenerEscucha() { escucha?.detener(); }
el.detenerVoz.addEventListener('click', detenerEscucha);
el.mic.addEventListener('click', () => {
  if (ocupado) return;
  if (escucha) return detenerEscucha();
  if (!dictadoDisponible()) { aviso('Tu navegador no ofrece dictado por voz. Escribe tu residuo; en Chrome, Edge o Safari puedes usar el micrófono.'); el.input.focus(); return; }
  callar();
  el.mic.setAttribute('aria-pressed', 'true'); el.mic.setAttribute('aria-label', 'Dejar de escuchar');
  el.vozEstado.hidden = false; el.vozEtiqueta.textContent = 'Escuchando…'; el.vozTexto.textContent = 'Dime qué residuo vas a desechar';
  escucha = escuchar({
    onParcial: (t) => { el.vozTexto.textContent = `«${t}»`; },
    onFinal: (t, alternativas) => {
      el.vozEtiqueta.textContent = 'Procesando…'; el.vozTexto.textContent = `«${t}»`;
      el.input.value = t;
      setTimeout(() => identificar(t, 'voz', alternativas), 150);
    },
    onError: (_, mensaje) => { aviso(mensaje, 6000); },
    onFin: () => {
      escucha = null; el.mic.setAttribute('aria-pressed', 'false'); el.mic.setAttribute('aria-label', 'Hablar con ECO');
      setTimeout(() => { el.vozEstado.hidden = true; }, 900);
    },
  });
});

el.alternarVoz.setAttribute('aria-pressed', String(vozActiva));
el.alternarVoz.addEventListener('click', () => {
  vozActiva = !vozActiva; el.alternarVoz.setAttribute('aria-pressed', String(vozActiva));
  try { localStorage.setItem('ecoaccess.voz', vozActiva ? 'si' : 'no'); } catch { /* nada */ }
  if (!vozActiva) callar(); else hablar('Respuestas habladas activadas.', true);
});

el.estadoBtn.addEventListener('click', () => {
  const m = modoActual();
  if (m === 'consulta') aviso('Modo consulta: te muestro la caneca correcta. Para abrir la caneca física, escanea el código QR de la estación EcoAccess.', 7000);
  else if (m === 'demo') aviso('Demostración: la animación no mueve ninguna caneca física.', 5000);
  else aviso(estadoEstacion.texto || 'Estación física vinculada por código QR.', 6000);
});

// ───────────────────────── Historial ─────────────────────────
let filtro = 'todos';
function pintarHistorial() {
  const todo = historial.leer();
  const fecha = el.filtroFecha.value;
  const lista = todo.filter((h) => (filtro === 'todos' || (h.caneca || '').includes(filtro)) && (!fecha || h.fecha.slice(0, 10) === fecha || new Date(h.fecha).toLocaleDateString('en-CA') === fecha));
  const n = (t) => todo.filter((h) => h.tipo === t).length;
  el.histResumen.innerHTML = `<div><b>${n('consulta')}</b><span>Consultas</span></div><div><b>${n('apertura') + n('deposito')}</b><span>Aperturas físicas</span></div><div><b>${n('deposito')}</b><span>Depósitos confirmados</span></div>`;
  const etiquetas = { consulta: 'Consulta', apertura: 'Apertura', deposito: 'Depósito confirmado' };
  el.histLista.innerHTML = lista.length ? lista.map((h) => {
    const f = new Date(h.fecha);
    const cuando = f.toLocaleDateString('es-CO', { day: 'numeric', month: 'short' }) + ' · ' + f.toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit' });
    const canecas = h.caneca ? h.caneca.split('+').map((c) => TEXTO_CANECA[c]?.nombre).join(' y ') : (h.especial ? 'residuo especial' : 'sin caneca');
    return `<li><span class="tono" data-caneca="${(h.caneca || '').split('+')[0]}"></span><strong>${escapar(h.material || h.consulta)}</strong><span class="tipo" data-tipo="${h.tipo}">${etiquetas[h.tipo] || h.tipo}${h.demostracion ? ' (demo)' : ''}</span><small>${cuando} · ${escapar(canecas)}${h.error ? ' · no se abrió' : ''}</small></li>`;
  }).join('') : '<li class="vacio">Aún no hay registros con este filtro.</li>';
}
el.abrirHist.addEventListener('click', () => { pintarHistorial(); pintarOficial(); el.dialogo.showModal(); });

// Historial oficial: la cuenta se vincula con un código de un solo uso confirmado en el bot (/start web_CODIGO).
const ofi = { nota: $('oficialNota'), lista: $('oficialLista'), vincular: $('vincularCuenta'), comprobar: $('comprobarVinculo'), desvincular: $('desvincularCuenta') };
async function pintarOficial() {
  const c = estacion.cuentaActual();
  ofi.vincular.hidden = c.vinculada; ofi.desvincular.hidden = !c.vinculada; ofi.comprobar.hidden = c.vinculada || !c.codigo;
  if (!c.vinculada) { ofi.lista.hidden = true; return; }
  ofi.nota.textContent = 'Consultando Oracle Cloud…';
  try {
    const ops = await estacion.historialOficial();
    if (ops === null) { ofi.nota.textContent = 'La vinculación venció. Vuelve a vincular con Telegram.'; ofi.vincular.hidden = false; ofi.desvincular.hidden = true; ofi.lista.hidden = true; return; }
    ofi.nota.textContent = ops.length ? 'Tus operaciones registradas en Oracle Cloud (web, Telegram y PC):' : 'Aún no tienes operaciones registradas en Oracle.';
    ofi.lista.hidden = !ops.length;
    ofi.lista.innerHTML = ops.map((o) => {
      const f = new Date(o.fechaUtc);
      const cuando = f.toLocaleDateString('es-CO', { day: 'numeric', month: 'short', year: 'numeric' }) + ' · ' + f.toLocaleTimeString('es-CO', { hour: '2-digit', minute: '2-digit' });
      const titulo = o.residuos && o.residuos.length ? o.residuos.join(' · ') : (o.tipo === 'APERTURA_MANUAL' ? 'Apertura manual — ' + o.canecas : o.canecas);
      const estado = o.estado === 'DISPOSICION_CONFIRMADA' ? 'Depósito declarado' : o.estado === 'ERROR' ? 'No se abrió' : 'Registrada';
      return `<li><span class="tono" data-caneca="${escapar((o.canecas || '').split(' ')[0])}"></span><strong>${escapar(titulo)}</strong><span class="tipo">${escapar(estado)}</span><small>${cuando} · ${escapar(o.canal)}</small></li>`;
    }).join('');
  } catch (e) { ofi.nota.textContent = e.message; ofi.lista.hidden = true; }
}
ofi.vincular.addEventListener('click', async () => {
  try {
    const d = await estacion.iniciarVinculo();
    window.open(d.enlace, '_blank', 'noopener');
    ofi.nota.textContent = 'Se abrió Telegram: pulsa «Iniciar» en el bot EcoAccess (debes tener tu cuenta vinculada allí). Luego vuelve y toca «Ya confirmé en Telegram». El código vence en 10 minutos.';
    ofi.comprobar.hidden = false;
  } catch (e) { ofi.nota.textContent = e.message; }
});
ofi.comprobar.addEventListener('click', async () => {
  try {
    const r = await estacion.completarVinculo();
    if (r === 'vinculado') { aviso('Página vinculada con tu cuenta de Telegram.'); pintarOficial(); }
    else ofi.nota.textContent = r === 'pendiente' ? 'Todavía no confirmaste en Telegram. Abre el bot y pulsa «Iniciar».' : 'El código venció. Vuelve a tocar «Vincular con Telegram».';
  } catch (e) { ofi.nota.textContent = e.message; }
});
ofi.desvincular.addEventListener('click', async () => { if (confirm('¿Desvincular este dispositivo de tu cuenta?')) { await estacion.desvincular(); ofi.nota.textContent = 'Dispositivo desvinculado.'; pintarOficial(); } });
document.querySelectorAll('.filtro').forEach((b) => b.addEventListener('click', () => {
  filtro = b.dataset.filtro;
  document.querySelectorAll('.filtro').forEach((x) => { x.classList.toggle('activo', x === b); x.setAttribute('aria-pressed', String(x === b)); });
  pintarHistorial();
}));
el.filtroFecha.addEventListener('change', pintarHistorial);
el.borrarHist.addEventListener('click', () => { if (confirm('¿Borrar el historial guardado en este dispositivo?')) { historial.borrar(); pintarHistorial(); } });
el.dialogo.addEventListener('click', (ev) => { if (ev.target === el.dialogo) el.dialogo.close(); });

// ───────────────────────── Atajos de teclado ─────────────────────────
document.addEventListener('keydown', (ev) => {
  if (ev.key === '/' && document.activeElement !== el.input && !el.dialogo.open) { ev.preventDefault(); el.input.focus(); }
  if (ev.key === 'Escape' && el.principal.dataset.fase !== 'inicio' && !ocupado && !el.dialogo.open) irInicio();
});
addEventListener('online', () => { el.aviso.hidden = true; refrescarEstacion(); });
addEventListener('offline', () => aviso('Sin conexión: puedes seguir consultando residuos; la estación física no está disponible.', 6000));
document.addEventListener('visibilitychange', () => { if (!document.hidden) refrescarEstacion(); });

// ───────────────────────── Inicio ─────────────────────────
async function iniciar() {
  const sesion = estacion.iniciarSesionDesdeUrl();
  if (sesion) estadoEstacion = { estado: 'verificando' };
  pintarEstado();
  try {
    const r = await fetch('datos/catalogo-residuos.json', { cache: 'no-cache' });
    motor = crearMotor(await r.json());
  } catch {
    decirEco('No pude cargar el catálogo de residuos. Revisa tu conexión y recarga la página.');
    bloquear(true); return;
  }
  if (!dictadoDisponible()) el.mic.title = 'El dictado por voz no está disponible en este navegador';
  if (sesion) { await refrescarEstacion(); setInterval(refrescarEstacion, 5000); if (estadoEstacion.estado === 'disponible') decirEco('<strong>Estación vinculada.</strong> Dime qué vas a desechar y abriré la caneca correcta.', 'Estación vinculada. Dime qué vas a desechar.'); }
  if ('serviceWorker' in navigator && location.protocol === 'https:') navigator.serviceWorker.register('sw.js').catch(() => {});
  window.__ecoaccess = { motor, normalizar, version: motor.catalogo.version };   // apoyo a pruebas automatizadas
}
iniciar();
