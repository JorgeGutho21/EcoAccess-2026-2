// Índice de Tecnología.
import { cabecera, leyenda } from './comun.js';

export const titulo = 'Tecnología';
const TEMAS = [
  ['programacion', 'Programación III', 'Clases, herencia, interfaces, colecciones, async y SOLID: qué está aplicado, qué es parcial y qué es oportunidad.'],
  ['arquitectura', 'Arquitectura', 'Los nueve proyectos de la solución, sus dependencias y el recorrido animado de la foto al historial.'],
  ['grafos', 'Grafos', 'El diagrama de arquitectura leído como grafo, y BFS/DFS como demostración educativa (EcoAccess no los implementa).'],
  ['hardware', 'Hardware', 'ESP32, servos, PWM, calibración y por qué un comando no es una posición medida.'],
  ['datos', 'Base de datos', 'Modelo entidad-relación navegable, migraciones V030–V035 y Oracle Cloud, wallet y SQL Developer en 12 pasos.'],
  ['ia', 'Inteligencia artificial', 'Texto, voz e imagen; el proceso con Ollama y lfm3-vl, y sus limitaciones medidas.'],
  ['integraciones', 'Integraciones', 'Telegram, la pasarela de esta página hacia la estación, Oracle Cloud y GitHub Actions.'],
];

export function render() {
  return `${cabecera({ titulo, entrada: 'Cómo está hecho EcoAccess, tema por tema. Cada página cita los archivos del repositorio y marca qué está verificado y qué no.' })}
  <section class="bloque" aria-labelledby="t-temas">
    <h2 id="t-temas" class="sr">Temas</h2>
    <ul class="indice">${TEMAS.map(([r, t, d]) => `<li><a href="#/tecnologia/${r}"><b>${t}</b><span>${d}</span></a></li>`).join('')}</ul>
    <h3>Cómo leer los sellos</h3>
    ${leyenda()}
  </section>`;
}
