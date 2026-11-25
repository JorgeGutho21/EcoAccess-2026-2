// Grafos. El código de EcoAccess NO implementa algoritmos de grafos: se buscó «BFS», «DFS», «grafo», «adyacencia» y
// «topológico» en src/ (25/11/2026) sin resultados. Este grafo es el diagrama de arquitectura modelado como grafo, y
// BFS/DFS se muestran como demostración educativa sobre él.
import { cabecera, sello, escapar, movimientoReducido } from './comun.js';
import { definiciones } from './uml.js';

export const titulo = 'Grafos';
const MIGAS = [['Tecnología', '#/tecnologia']];

const NODOS = [
  { id: 'usuario', n: 'Usuario', x: 380, y: 40, r: 'La persona que va a desechar un residuo. Inicia todo: escribe, habla o envía una foto.' },
  { id: 'web', n: 'Web', x: 120, y: 132, r: 'Esta página. Clasifica por texto o voz en el navegador; con el QR presencial pide la apertura a la pasarela.' },
  { id: 'telegram', n: 'Telegram', x: 380, y: 132, r: 'Bot que recibe fotos, consulta el reconocimiento y, para cuentas autorizadas, pide la apertura al coordinador.' },
  { id: 'app', n: 'App C#', x: 640, y: 132, r: 'Aplicación de escritorio del operario (Windows Forms): registro asistido y control de la estación.' },
  { id: 'pasarela', n: 'Pasarela + panel', x: 120, y: 240, r: 'Pasarela HTTPS de la página y panel local (127.0.0.1:8090), único dueño del puerto USB del ESP32.' },
  { id: 'coordinador', n: 'Coordinador', x: 380, y: 240, r: 'Lógica de la estación: autoriza, evita duplicados por idempotencia y ejecuta una operación a la vez.' },
  { id: 'ollama', n: 'Ollama', x: 640, y: 240, r: 'Servidor local del modelo de visión lfm3-vl. Propone qué hay en la foto; nunca decide abrir.' },
  { id: 'esp32', n: 'ESP32', x: 250, y: 344, r: 'Microcontrolador que genera la señal PWM de cada servo. Sin sensores: confirma la orden, no la posición.' },
  { id: 'oracle', n: 'Oracle Cloud', x: 560, y: 344, r: 'Base de datos: catálogo, usuarios y la bitácora OPERACION_ESTACION de cada apertura.' },
  { id: 'verde', n: 'Caneca verde', x: 92, y: 440, r: 'Orgánicos aprovechables. Servo en GPIO 23.' },
  { id: 'blanca', n: 'Caneca blanca', x: 250, y: 440, r: 'Aprovechables. Servo en GPIO 19.' },
  { id: 'negra', n: 'Caneca negra', x: 408, y: 440, r: 'No aprovechables. Servo en GPIO 18.' },
];
const ARISTAS = [
  ['usuario', 'web'], ['usuario', 'telegram'], ['usuario', 'app'],
  ['web', 'pasarela'], ['telegram', 'ollama'], ['app', 'ollama'], ['telegram', 'coordinador'], ['app', 'coordinador'],
  ['pasarela', 'esp32'], ['coordinador', 'esp32'], ['coordinador', 'oracle'], ['pasarela', 'oracle'],
  ['esp32', 'verde'], ['esp32', 'blanca'], ['esp32', 'negra'],
];
const POR_ID = Object.fromEntries(NODOS.map((n) => [n.id, n]));
const W = 142, H = 38;

function vecinos(id, noDirigido) {
  const s = ARISTAS.filter(([a]) => a === id).map(([, b]) => b);
  if (noDirigido) ARISTAS.filter(([, b]) => b === id).forEach(([a]) => { if (!s.includes(a)) s.push(a); });
  return s.sort((a, b) => NODOS.indexOf(POR_ID[a]) - NODOS.indexOf(POR_ID[b]));
}

function borde(a, b) {   // punto donde la recta a→b corta el rectángulo del nodo b
  const dx = a.x - b.x, dy = a.y - b.y;
  const k = Math.min((W / 2 + 4) / Math.abs(dx || 1e-9), (H / 2 + 4) / Math.abs(dy || 1e-9));
  return [b.x + dx * k, b.y + dy * k];
}

function svgGrafo() {
  const aristas = ARISTAS.map(([a, b], i) => {
    const A = POR_ID[a], B = POR_ID[b];
    const [x1, y1] = borde(B, A), [x2, y2] = borde(A, B);
    return `<line class="arista" data-i="${i}" data-a="${a}" data-b="${b}" x1="${x1.toFixed(1)}" y1="${y1.toFixed(1)}" x2="${x2.toFixed(1)}" y2="${y2.toFixed(1)}" marker-end="url(#m-flecha)"/>`;
  }).join('');
  const nodos = NODOS.map((n) => `<g class="nodo" data-id="${n.id}" role="button" tabindex="0" aria-pressed="false" aria-label="${escapar(n.n)}">
    <rect x="${n.x - W / 2}" y="${n.y - H / 2}" width="${W}" height="${H}" rx="10"/>
    ${['verde', 'blanca', 'negra'].includes(n.id) ? `<rect class="marca" x="${n.x - W / 2 + 8}" y="${n.y - 5}" width="10" height="10" rx="2" style="fill:var(--caneca-${n.id});stroke:var(--caneca-blanca-borde);stroke-width:1"/>` : ''}
    <text x="${n.x + (['verde', 'blanca', 'negra'].includes(n.id) ? 7 : 0)}" y="${n.y + 4.5}" text-anchor="middle">${escapar(n.n)}</text></g>`).join('');
  return `<div class="desliza" tabindex="-1"><svg class="grafo" viewBox="0 0 760 470" style="min-width:560px" role="group" aria-label="Grafo de EcoAccess: selecciona un nodo">${aristas}${nodos}</svg></div>`;
}

function listaAdyacencia(noDirigido) {
  return NODOS.map((n) => `<li><b>${escapar(n.n)}</b> → ${vecinos(n.id, noDirigido).map((v) => escapar(POR_ID[v].n)).join(', ') || '∅'}</li>`).join('');
}
function matriz(noDirigido) {
  const corto = (n) => n.n.replace('Caneca ', '').replace('Pasarela + panel', 'Pasarela').replace('Oracle Cloud', 'Oracle');
  return `<table class="matriz"><caption class="sr">Matriz de adyacencia: 1 si hay arista de la fila a la columna</caption>
    <thead><tr><th scope="col"><span class="sr">De / a</span></th>${NODOS.map((n) => `<th scope="col">${escapar(corto(n))}</th>`).join('')}</tr></thead>
    <tbody>${NODOS.map((f) => `<tr><th scope="row">${escapar(corto(f))}</th>${NODOS.map((c) => { const v = vecinos(f.id, noDirigido).includes(c.id) ? 1 : 0; return `<td data-v="${v}">${v}</td>`; }).join('')}</tr>`).join('')}</tbody></table>`;
}

/** Recorridos precalculados: cada paso dice qué nodo se visita y cómo queda la cola o la pila. */
function recorrido(tipo, inicio, noDirigido) {
  const pasos = [], visitados = [];
  if (tipo === 'bfs') {
    const cola = [inicio]; const vistos = new Set([inicio]);
    while (cola.length) {
      const actual = cola.shift(); visitados.push(actual);
      for (const v of vecinos(actual, noDirigido)) if (!vistos.has(v)) { vistos.add(v); cola.push(v); }
      pasos.push({ actual, visitados: [...visitados], estructura: [...cola] });
    }
  } else {
    const pila = [inicio]; const vistos = new Set();
    while (pila.length) {
      const actual = pila.pop(); if (vistos.has(actual)) continue;
      vistos.add(actual); visitados.push(actual);
      [...vecinos(actual, noDirigido)].reverse().forEach((v) => { if (!vistos.has(v)) pila.push(v); });
      pasos.push({ actual, visitados: [...visitados], estructura: [...pila].reverse() });
    }
  }
  return pasos;
}

export function render() {
  return `${definiciones}${cabecera({ titulo, migas: MIGAS, entrada: 'Un grafo es un conjunto de nodos unidos por aristas. Aquí se usa para leer la arquitectura de EcoAccess: quién habla con quién.' })}

  <section class="bloque" aria-labelledby="g-aviso">
    <h2 id="g-aviso">Antes de empezar</h2>
    <p>${sello('educativo')} El código de EcoAccess <strong>no implementa algoritmos de grafos</strong>: se buscaron BFS, DFS, «grafo» y «adyacencia» en <code>src/</code> sin resultados. El grafo de abajo es el diagrama de arquitectura escrito como grafo, y BFS y DFS se ejecutan sobre él solo para aprender. Un diagrama de arquitectura no es un algoritmo implementado.</p>
  </section>

  <section class="bloque prosa" aria-labelledby="g-conceptos">
    <h2 id="g-conceptos">Conceptos</h2>
    <ul>
      <li><strong>Nodo (vértice):</strong> un componente, por ejemplo «ESP32».</li>
      <li><strong>Arista:</strong> una relación entre dos nodos, por ejemplo «el ESP32 mueve la caneca verde».</li>
      <li><strong>Grafo dirigido:</strong> la arista tiene sentido (A → B no implica B → A). Las órdenes de apertura van en un solo sentido.</li>
      <li><strong>Grafo no dirigido:</strong> la relación vale en ambos sentidos, como «está conectado con».</li>
      <li><strong>Adyacencia:</strong> dos nodos son adyacentes si una arista los une. Se guarda como <em>lista</em> (cada nodo con sus vecinos) o como <em>matriz</em> (1 si hay arista, 0 si no).</li>
    </ul>
  </section>

  <section class="bloque" aria-labelledby="g-ecoaccess">
    <h2 id="g-ecoaccess">El grafo de EcoAccess</h2>
    <p>Selecciona un nodo (clic, o Tab y Enter) para resaltar sus conexiones y ver su responsabilidad. Debajo puedes recorrerlo con BFS o DFS.</p>
    <div class="lamina">
      <label class="controles" style="margin-top:0"><input type="checkbox" id="noDirigido"> Tratar como no dirigido</label>
      <div class="grafo-env">
        ${svgGrafo()}
        <div style="display:grid;gap:14px;min-width:0">
          <div class="ficha-nodo" id="fichaNodo" aria-live="polite"><h3>Ningún nodo seleccionado</h3><p>Elige un nodo del grafo.</p></div>
          <div id="recorridos">
            <h3 id="g-recorridos" style="margin:0 0 4px">BFS y DFS, animados ${sello('educativo')}</h3>
            <p style="margin:0"><strong>BFS</strong> (anchura) usa una <em>cola</em>: primero todos los vecinos, luego los vecinos de los vecinos. <strong>DFS</strong> (profundidad) usa una <em>pila</em>: sigue un camino hasta el final antes de retroceder.</p>
            <div class="controles">
              <fieldset style="border:0;padding:0;margin:0;display:flex;gap:14px;align-items:center"><legend class="sr">Algoritmo</legend>
                <label><input type="radio" name="alg" value="bfs" checked> BFS</label>
                <label><input type="radio" name="alg" value="dfs"> DFS</label>
              </fieldset>
              <label>Desde <select id="inicioRec" style="min-height:40px;border-radius:10px;border:1.5px solid var(--linea);padding:0 8px;background:var(--superficie)">${NODOS.map((n) => `<option value="${n.id}">${escapar(n.n)}</option>`).join('')}</select></label>
            </div>
            <div class="controles">
              <button class="btn-principal" type="button" data-accion="play">Animar</button>
              <button class="btn-sec" type="button" data-accion="paso">Un paso</button>
              <button class="btn-sec" type="button" data-accion="reiniciar">Reiniciar</button>
            </div>
            <p style="margin:10px 0 0"><b id="nombreEstructura">Cola</b>:</p><ul class="estructura" id="estructura" aria-label="Contenido de la estructura"></ul>
            <p style="margin:4px 0 0">Orden de visita: <span class="orden" id="ordenVisita">—</span></p>
            <p class="sr" aria-live="polite" id="recSr"></p>
            <p class="lamina-pie">Naranja: nodo actual. Verde claro: ya visitados.</p>
          </div>
        </div>
      </div>
    </div>
  </section>

  <section class="bloque" aria-labelledby="g-adyacencia">
    <h2 id="g-adyacencia">Lista y matriz de adyacencia</h2>
    <p>La misma información en dos estructuras. La lista ocupa espacio según las aristas; la matriz responde «¿hay arista de A a B?» en un solo acceso, pero ocupa n × n celdas (aquí 12 × 12 = 144).</p>
    <div class="dos">
      <div class="lamina"><h3>Lista</h3><ul id="listaAdy" style="list-style:none;padding:0;margin:0;display:grid;gap:4px;font-size:.9375rem"></ul></div>
      <div class="lamina"><h3>Matriz</h3><div class="tabla-env" id="matrizAdy" tabindex="0" role="group" aria-label="Matriz de adyacencia, desliza para ver todo"></div></div>
    </div>
  </section>`;
}

export function montar(raiz) {
  const svg = raiz.querySelector('svg.grafo');
  const nodosEl = [...svg.querySelectorAll('.nodo')];
  const aristasEl = [...svg.querySelectorAll('.arista')];
  const ficha = raiz.querySelector('#fichaNodo');
  const chk = raiz.querySelector('#noDirigido');
  let seleccion = null;

  const pintarAdyacencia = () => {
    raiz.querySelector('#listaAdy').innerHTML = listaAdyacencia(chk.checked);
    raiz.querySelector('#matrizAdy').innerHTML = matriz(chk.checked);
  };
  const elegir = (id) => {
    seleccion = id;
    const vec = id ? vecinos(id, chk.checked) : [];
    nodosEl.forEach((g) => {
      const n = g.dataset.id;
      g.classList.toggle('activo', n === id); g.classList.toggle('vecino', vec.includes(n)); g.classList.toggle('tenue', !!id && n !== id && !vec.includes(n));
      g.setAttribute('aria-pressed', String(n === id));
      g.classList.remove('visitado', 'actual');
    });
    aristasEl.forEach((l) => {
      const toca = id && (l.dataset.a === id || (chk.checked && l.dataset.b === id));
      l.classList.toggle('activa', !!toca); l.classList.toggle('tenue', !!id && !toca);
    });
    if (!id) { ficha.innerHTML = '<h3>Ningún nodo seleccionado</h3><p>Elige un nodo del grafo.</p>'; return; }
    const n = POR_ID[id];
    const entrantes = ARISTAS.filter(([, b]) => b === id).map(([a]) => POR_ID[a].n);
    ficha.innerHTML = `<h3>${escapar(n.n)}</h3><p>${escapar(n.r)}</p>
      <ul><li>Sale hacia: ${vecinos(id, false).map((v) => escapar(POR_ID[v].n)).join(', ') || 'ninguno'}</li>
      <li>Recibe de: ${entrantes.map(escapar).join(', ') || 'ninguno'}</li>
      <li>Grado de salida ${vecinos(id, false).length}, grado de entrada ${entrantes.length}</li></ul>`;
  };
  nodosEl.forEach((g) => {
    g.addEventListener('click', () => elegir(seleccion === g.dataset.id ? null : g.dataset.id));
    g.addEventListener('keydown', (ev) => { if (ev.key === 'Enter' || ev.key === ' ') { ev.preventDefault(); g.dispatchEvent(new MouseEvent('click')); } });
  });
  chk.addEventListener('change', () => {
    aristasEl.forEach((l) => (chk.checked ? l.removeAttribute('marker-end') : l.setAttribute('marker-end', 'url(#m-flecha)')));
    pintarAdyacencia(); elegir(seleccion); reiniciar();
  });

  // Recorridos
  const caja = raiz.querySelector('#recorridos');
  const est = caja.querySelector('#estructura'), orden = caja.querySelector('#ordenVisita'), sr = caja.querySelector('#recSr');
  const play = caja.querySelector('[data-accion="play"]');
  let pasos = [], i = -1, t = 0;
  const alg = () => caja.querySelector('input[name="alg"]:checked').value;
  const preparar = () => { pasos = recorrido(alg(), caja.querySelector('#inicioRec').value, chk.checked); i = -1; };
  const pintar = () => {
    caja.querySelector('#nombreEstructura').textContent = alg() === 'bfs' ? 'Cola (sale por la izquierda)' : 'Pila (sale por la izquierda, la cima)';
    const p = pasos[i];
    nodosEl.forEach((g) => { g.classList.remove('activo', 'vecino', 'tenue'); g.classList.toggle('visitado', !!p && p.visitados.includes(g.dataset.id)); g.classList.toggle('actual', !!p && p.actual === g.dataset.id); });
    aristasEl.forEach((l) => l.classList.remove('activa', 'tenue'));
    est.innerHTML = p ? p.estructura.map((v) => `<li>${escapar(POR_ID[v].n)}</li>`).join('') : '';
    orden.textContent = p ? p.visitados.map((v) => POR_ID[v].n).join(' → ') : '—';
    if (p) sr.textContent = `Visita ${i + 1}: ${POR_ID[p.actual].n}. ${p.estructura.length ? 'Pendientes: ' + p.estructura.map((v) => POR_ID[v].n).join(', ') : 'No quedan pendientes'}.`;
  };
  const detener = () => { clearInterval(t); t = 0; play.textContent = 'Animar'; };
  const paso = () => { if (!pasos.length) preparar(); if (i < pasos.length - 1) { i++; pintar(); } if (i >= pasos.length - 1) detener(); };
  function reiniciar() { detener(); preparar(); seleccion = null; pintar(); }
  play.addEventListener('click', () => {
    if (t) return detener();
    if (i >= pasos.length - 1) preparar();
    paso();
    if (movimientoReducido()) return;
    play.textContent = 'Pausar'; t = setInterval(paso, 1100);
  });
  caja.querySelector('[data-accion="paso"]').addEventListener('click', () => { detener(); paso(); });
  caja.querySelector('[data-accion="reiniciar"]').addEventListener('click', reiniciar);
  caja.querySelectorAll('input[name="alg"], #inicioRec').forEach((x) => x.addEventListener('change', reiniciar));

  pintarAdyacencia(); preparar();
  return detener;
}
