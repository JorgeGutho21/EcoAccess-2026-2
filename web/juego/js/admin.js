// Panel administrativo. Toda acción la valida Oracle con la clave (solo su SHA-256 vive en la base). La clave se guarda en
// sessionStorage de esta pestaña y desaparece al cerrarla.
import { llamar, alAvisar } from './api.js?v=20261125';

const $ = (id) => document.getElementById(id);
alAvisar((m) => { $('conexion').hidden = !m; $('conexion').textContent = m ?? ''; });
let clave = sessionStorage.getItem('ecoaccess.juego.admin');
let actual = null;
const ESTADOS = { PREPARADA: 'Preparada', ABIERTA: 'Abierta', REGISTRO_CERRADO: 'Registro cerrado', FINALIZADA: 'Finalizada' };
const tiempo = (ms) => (ms == null ? '–' : `${(ms / 1000).toFixed(1).replace('.', ',')} s`);
const accion = (nombre, cuerpo = {}) => llamar('/admin/' + nombre, { metodo: 'POST', cuerpo, admin: clave });
const celda = (fila, texto) => { const td = document.createElement('td'); td.textContent = texto ?? ''; fila.appendChild(td); return td; };

async function entrar() {
  try {
    await cargarCompetencias();
    sessionStorage.setItem('ecoaccess.juego.admin', clave);
    $('formClave').hidden = true; $('panel').hidden = false; $('salir').hidden = false;
  } catch (e) {
    sessionStorage.removeItem('ecoaccess.juego.admin'); clave = null;
    $('errorClave').textContent = e.message;
  }
}
$('formClave').addEventListener('submit', (ev) => { ev.preventDefault(); clave = $('clave').value.trim(); $('clave').value = ''; entrar(); });
$('salir').addEventListener('click', () => { sessionStorage.removeItem('ecoaccess.juego.admin'); location.reload(); });

async function cargarCompetencias() {
  const d = await accion('competencias');
  const tb = $('competencias'); tb.innerHTML = '';
  d.competencias.forEach((c) => {
    const tr = document.createElement('tr');
    const nom = celda(tr, ''); nom.innerHTML = '<strong></strong><br><small></small>';
    nom.querySelector('strong').textContent = c.nombre; nom.querySelector('small').textContent = `${c.codigo} · creada ${c.creada}`;
    if (c.prueba) { const e = document.createElement('span'); e.className = 'etiqueta prueba'; e.textContent = 'Prueba'; nom.append(' ', e); }
    celda(tr, ESTADOS[c.estado]); celda(tr, c.participantes); celda(tr, c.finalizados);
    const acc = celda(tr, ''); acc.className = 'fila-acciones';
    const boton = (texto, fn, clase = 'secundario') => { const b = document.createElement('button'); b.type = 'button'; b.className = 'btn ' + clase; b.textContent = texto; b.onclick = fn; acc.appendChild(b); };
    boton('Ver participantes', () => verParticipantes(c));
    if (c.estado === 'PREPARADA' || c.estado === 'REGISTRO_CERRADO') boton(c.estado === 'PREPARADA' ? 'Abrir' : 'Reabrir registro', () => ejecutar('abrir', c), 'primario');
    if (c.estado === 'ABIERTA') boton('Cerrar registro', () => ejecutar('cerrar_registro', c));
    if (c.estado !== 'FINALIZADA') boton('Finalizar concurso', () => {
      if (confirm(`¿Finalizar «${c.nombre}»? Los intentos en curso se cerrarán con lo respondido y la clasificación quedará definitiva.`)) ejecutar('finalizar', c);
    });
    if (c.prueba) boton('Eliminar ronda de prueba', () => { if (confirm('¿Eliminar esta ronda de prueba y sus participantes?')) ejecutar('eliminar_prueba', c); });
    tb.appendChild(tr);
  });
  if (!d.competencias.length) { const tr = document.createElement('tr'); celda(tr, 'Aún no hay competencias. Crea una ronda oficial.').colSpan = 5; tb.appendChild(tr); }
}

async function ejecutar(nombre, c) {
  $('errorAccion').textContent = '';
  try { await accion(nombre, { competencia: c.id }); await cargarCompetencias(); if (actual?.id === c.id && nombre !== 'eliminar_prueba') verParticipantes(c); else if (nombre === 'eliminar_prueba') $('detalle').hidden = true; }
  catch (e) { $('errorAccion').textContent = e.message; }
}
async function crear(prueba) {
  const nombre = prompt('Nombre de la ronda', prueba ? 'Ronda de prueba' : 'EcoAccess Challenge · ExpoSoftware 2026');
  if (nombre == null) return;
  try { await accion('crear', { nombre, prueba }); await cargarCompetencias(); }
  catch (e) { $('errorAccion').textContent = e.message; }
}
$('crear').addEventListener('click', () => crear(false));
$('crearPrueba').addEventListener('click', () => crear(true));

async function verParticipantes(c) {
  actual = c;
  const d = await accion('participantes', { competencia: c.id });
  $('detalle').hidden = false;
  $('tituloDetalle').textContent = `Participantes de «${c.nombre}» (${ESTADOS[d.competencia.estado]})`;
  const tb = $('participantes'); tb.innerHTML = '';
  d.participantes.forEach((p) => {
    const tr = document.createElement('tr'); if (p.posicion && p.posicion <= 3) tr.className = 'ganador';
    [p.posicion ?? '–', p.alias, p.nombre, p.estado, p.puntaje, `${p.aciertos}/7`, tiempo(p.tiempoMs), p.respuestas, p.registrado].forEach((v) => celda(tr, v));
    tb.appendChild(tr);
  });
  const top = d.participantes.filter((p) => p.posicion && p.posicion <= 3).sort((a, b) => a.posicion - b.posicion);
  $('ganadores').textContent = top.length
    ? `${d.competencia.definitiva ? 'Ganadores' : 'Primeros lugares provisionales'}: ` + top.map((p) => `${p.posicion}.º ${p.nombre} (${p.alias}, ${p.puntaje} pts, ${tiempo(p.tiempoMs)})`).join('; ')
    : 'Aún no hay participantes que hayan terminado.';
}
$('exportar').addEventListener('click', async () => {
  try {
    const r = await llamar('/admin/exportacion?competencia=' + actual.id, { admin: clave, crudo: true });
    const blob = new Blob(['﻿', await r.text()], { type: 'text/csv;charset=utf-8' });
    const a = Object.assign(document.createElement('a'), { href: URL.createObjectURL(blob), download: `ecoaccess-challenge-${actual.codigo}.csv` });
    a.click(); setTimeout(() => URL.revokeObjectURL(a.href), 2000);
  } catch (e) { $('errorAccion').textContent = e.message; }
});

if (clave) entrar();
