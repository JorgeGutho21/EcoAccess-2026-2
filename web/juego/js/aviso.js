// Aviso de reintento compartido por el juego y la clasificación. Recibe lo que emite api.js (alAvisar): el texto completo
// para lectores de pantalla y el detalle { tipo, intento, maximo, ms } para pintar un título claro y una cuenta atrás.
// La cuenta atrás es solo visual (aria-hidden): el lector de pantalla oye el aviso una vez por intento, no cada segundo.
const TITULOS = {
  congestion: 'Hay mucha gente jugando, reintentando…',
  sin_red: 'Sin conexión a internet, reintentando…',
  servidor: 'El servidor está ocupado, reintentando…',
};

export function crearAviso(raiz = document.getElementById('conexion')) {
  if (!raiz) return () => {};
  const titulo = raiz.querySelector('#conexionTitulo');
  const detalle = raiz.querySelector('#conexionDetalle');
  const sr = raiz.querySelector('#conexionSr');
  let reloj = null;
  const parar = () => { clearInterval(reloj); reloj = null; };

  return function avisar(mensaje, d) {
    parar();
    if (!mensaje) { raiz.hidden = true; raiz.removeAttribute('data-tipo'); return; }
    raiz.hidden = false;
    if (sr) sr.textContent = mensaje;
    // Sin estructura (p. ej. el aviso propio de la clasificación): solo texto.
    if (!titulo || !detalle || !d) { if (titulo) { titulo.textContent = mensaje; detalle.textContent = ''; } else raiz.textContent = mensaje; return; }
    raiz.dataset.tipo = d.tipo ?? 'congestion';
    titulo.textContent = TITULOS[d.tipo] ?? TITULOS.congestion;
    const fin = performance.now() + (d.ms ?? 0);
    const guardado = d.tipo === 'servidor' ? '' : 'Tu progreso está guardado. ';
    const pintar = () => {
      const s = Math.max(0, Math.ceil((fin - performance.now()) / 1000));
      detalle.textContent = `${guardado}${s > 0 ? `Nuevo intento en ${s} s` : 'Intentando ahora'} (${d.intento} de ${d.maximo}).`;
    };
    pintar();
    reloj = setInterval(pintar, 500);
  };
}
