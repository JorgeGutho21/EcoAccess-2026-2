// Historial local del dispositivo (sin cuenta). Solo guarda lo consultado, la caneca y el tipo de registro: nada personal.
// Tipos: 'consulta' (educativa, sin movimiento), 'apertura' (orden física comandada) y 'deposito' (confirmado por la persona).
const CLAVE = 'ecoaccess.historial.v1';
const MAX = 300;

export function leer() {
  try { const l = JSON.parse(localStorage.getItem(CLAVE) || '[]'); return Array.isArray(l) ? l : []; } catch { return []; }
}
function escribir(l) { try { localStorage.setItem(CLAVE, JSON.stringify(l.slice(0, MAX))); } catch { /* almacenamiento lleno o bloqueado */ } }

export function agregar(entrada) {
  const l = leer();
  l.unshift({ id: crypto.randomUUID?.() ?? String(Date.now()), fecha: new Date().toISOString(), ...entrada });
  escribir(l);
}
export function actualizar(clave, cambios) {
  const l = leer(); const e = l.find((x) => x.clave === clave);
  if (e) { Object.assign(e, cambios); escribir(l); }
}
export function borrar() { try { localStorage.removeItem(CLAVE); } catch { /* nada */ } }
