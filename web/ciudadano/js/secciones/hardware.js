// Hardware. Fuentes: hardware/CalibracionTresCanecas/CalibracionTresCanecas.ino, hardware/README.md y los valores
// leídos en solo lectura de la placa el 25/11/2026 (firmware ecoaccess-calibracion-v2, calibración guardada en NVS).
import { cabecera, sello, codigo } from './comun.js';

export const titulo = 'Hardware';
const MIGAS = [['Tecnología', '#/tecnologia']];
const CANECAS = [
  { c: 'verde', n: 'Verde', gpio: 23, cerrado: 0, abierto: 50 },
  { c: 'blanca', n: 'Blanca', gpio: 19, cerrado: 0, abierto: 70 },
  { c: 'negra', n: 'Negra', gpio: 18, cerrado: 0, abierto: 40 },
];
// Misma fórmula del firmware: 0° = 500 µs, 180° = 2500 µs, periodo de 20 ms, resolución de 16 bits.
const pulso = (a) => 500 + Math.floor((Math.min(180, Math.max(0, a)) * 2000) / 180);   // map() de Arduino trunca
const duty = (a) => Math.floor((pulso(a) * 65535) / 20000);

export function render() {
  return `${cabecera({ titulo, migas: MIGAS, entrada: 'Un ESP32 mueve tres servomotores, uno por tapa. Esta página distingue siempre lo que el sistema <em>ordena</em> de lo que se puede <em>medir</em>.' })}

  <section class="bloque" aria-labelledby="h-valores">
    <h2 id="h-valores">Valores de la estación</h2>
    <p>${sello('verificado', 'Leídos de la placa en solo lectura')} Firmware <code>ecoaccess-calibracion-v2</code>, conectado por USB (COM3) a 115 200 baudios. La calibración vive en la memoria permanente del ESP32 (NVS), no en el código: los valores por defecto del archivo <code>.ino</code> son otros y no son los que usa la estación.</p>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Caneca</th><th scope="col">Pin</th><th scope="col">Cerrada</th><th scope="col">Abierta</th><th scope="col">Movimiento</th><th scope="col">Sensor de posición</th></tr></thead>
      <tbody>${CANECAS.map((k) => `<tr><th scope="row">${k.n}</th><td class="num">GPIO ${k.gpio}</td><td class="num">${k.cerrado}°</td><td class="num">${k.abierto}°</td><td class="num">2 s</td><td>No tiene</td></tr>`).join('')}</tbody>
    </table></div>
  </section>

  <section class="bloque" aria-labelledby="h-pwm">
    <h2 id="h-pwm">Cómo se mueve un servo: PWM</h2>
    <p>Un servo no recibe «grados»: recibe un pulso cada 20 ms (50 Hz). El <strong>ancho del pulso</strong> indica el ángulo. El firmware traduce 0° a 500 µs y 180° a 2500 µs, y lo escribe con 16 bits de resolución.</p>
    <div class="lamina" id="pwmDemo">
      <div class="controles" style="margin-top:0" role="group" aria-label="Llevar el simulador a una posición calibrada">
        ${CANECAS.map((k) => `<button class="btn-sec" type="button" data-a="${k.abierto}">${k.n} abierta (${k.abierto}°)</button>`).join('')}
        <button class="btn-sec" type="button" data-a="0">Cerrada (0°)</button>
      </div>
      <div class="campo-rango">
        <label for="anguloPwm">Ángulo ordenado: <b id="anguloTxt">0°</b></label>
        <input type="range" id="anguloPwm" min="0" max="180" step="1" value="0">
      </div>
      <svg class="pwm" viewBox="0 0 640 150" role="img" aria-labelledby="pwmT"><title id="pwmT">Señal PWM de un periodo de 20 ms</title>
        <line class="eje" x1="20" x2="620" y1="120" y2="120"/><line class="eje" x1="20" x2="20" y1="20" y2="124"/>
        <path class="senal" id="senalPwm" d=""/>
        <text x="20" y="140">0 ms</text><text x="590" y="140">20 ms</text><text x="26" y="16" id="pulsoTxt"></text>
      </svg>
      <p class="lamina-pie" aria-live="polite" id="pwmResumen"></p>
      <p class="lamina-pie">Simulación educativa: este control no está conectado a ninguna caneca.</p>
    </div>
    ${codigo('hardware/CalibracionTresCanecas/CalibracionTresCanecas.ino', `
static uint32_t duty(int angulo) {             // 0° = 500 µs, 180° = 2500 µs, periodo 20 ms, 16 bits
  int us = map(constrain(angulo, 0, 180), 0, 180, 500, 2500);
  return (uint32_t)((uint64_t)us * 65535 / 20000);
}`, 'C++ (Arduino)')}
  </section>

  <section class="bloque" aria-labelledby="h-comando">
    <h2 id="h-comando">Comando no es posición</h2>
    <p>Cuando el ESP32 responde que aplicó el PWM, sabemos que <strong>envió la orden</strong>. No sabemos si la tapa llegó al ángulo: no hay sensor de posición ni de ingreso del residuo. Por eso EcoAccess dice «apertura comandada» y nunca «la tapa se abrió». Una tapa trabada o un servo sin corriente recibirían la misma orden.</p>
    ${codigo('hardware/CalibracionTresCanecas/CalibracionTresCanecas.ino', `
// Eventos espontáneos: {"evt":"...","c":"V"}. Ningún recibo de PWM prueba que la tapa se movió: no hay sensores.`, 'C++ (Arduino)')}
    <p>Al encender, el firmware no mueve ningún motor, y cuando una tapa termina de cerrar suelta su PWM (la tapa queda en reposo sin señal).</p>
  </section>

  <section class="bloque" aria-labelledby="h-conexion">
    <h2 id="h-conexion">Conexión, alimentación y calibración</h2>
    <ul>
      <li><strong>USB (COM3):</strong> el panel local es el único programa que abre el puerto; así dos procesos nunca compiten por la placa.</li>
      <li><strong>Protocolo:</strong> una orden por línea y una respuesta JSON por línea: <code>ESTADO</code>, <code>CAL</code>, <code>GUARDAR</code>, <code>ABRIR</code>, <code>CERRAR</code>, <code>GRUPO</code> (varias tapas con arranque escalonado) y <code>DETENER</code>.</li>
      <li><strong>Calibración:</strong> se prueba cada tapa sin carga, se ajustan los ángulos cerrado y abierto con el panel y se guardan en la memoria del ESP32; sobreviven a reinicios.</li>
      <li><strong>Tierra común:</strong> la tierra de la fuente de los servos, la de los servos y la del ESP32 deben estar unidas; si no, la señal PWM no tiene referencia.</li>
      <li><strong>Alimentación:</strong> los servos usan una fuente externa, nunca los 3,3 V del ESP32. ${sello('pendiente', 'No verificado')} El esquema eléctrico definitivo de alimentación y el consumo bajo carga no están medidos, así que aquí no se dibuja uno.</li>
    </ul>
    <p class="nota-fuente">Fuentes: <code>hardware/README.md</code> y <code>hardware/CalibracionTresCanecas/CalibracionTresCanecas.ino</code>. La detención por software no sustituye cortar la alimentación física.</p>
  </section>`;
}

export function montar(raiz) {
  const r = raiz.querySelector('#anguloPwm');
  const camino = raiz.querySelector('#senalPwm');
  const poner = () => {
    const a = Number(r.value); const us = pulso(a);
    const ancho = (us / 20000) * 600;   // 600 px = 20 ms
    camino.setAttribute('d', `M20 120 V30 H${(20 + ancho).toFixed(1)} V120 H620`);
    raiz.querySelector('#anguloTxt').textContent = `${a}°`;
    raiz.querySelector('#pulsoTxt').textContent = `pulso alto: ${us} µs`;
    raiz.querySelector('#pwmResumen').textContent = `${a}° → pulso de ${us} µs cada 20 ms → valor de 16 bits ${duty(a)} de 65 535.`;
  };
  r.addEventListener('input', poner);
  raiz.querySelectorAll('#pwmDemo [data-a]').forEach((b) => b.addEventListener('click', () => { r.value = b.dataset.a; poner(); }));
  poner();
}
