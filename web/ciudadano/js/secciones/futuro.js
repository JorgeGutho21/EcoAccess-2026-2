// Visión a futuro: todo lo de esta página son PROPUESTAS; nada está instalado ni probado.
import { cabecera, sello, escapar } from './comun.js';

export const titulo = 'Visión a futuro';

const PROPUESTAS = [
  ['Sensores de posición de la tapa', 'Un final de carrera o un sensor angular por tapa permitiría decir «la tapa se abrió», y no solo «se ordenó abrir».', 'Hoy no hay ningún sensor: toda apertura es comandada.'],
  ['Confirmación física del depósito', 'Un sensor de paso o una celda de carga confirmaría que el residuo entró, para registrar depósitos verificados y no solo declarados.', 'El modelo de datos ya prevé un depósito verificado y un peso medido (columnas anulables de DEPOSITO).'],
  ['Sensores de llenado', 'Medir el nivel de cada caneca para avisar al operario antes de que se llene.', 'No existe en el hardware actual.'],
  ['Estaciones distribuidas', 'Varias estaciones en el campus, cada una con su QR y su ESP32, reportando a la misma base.', 'Las tablas PUNTO_INSTALACION y CONTENEDOR ya admiten varios puntos; falta el despliegue.'],
  ['Backend independiente', 'Llevar la pasarela, el coordinador y el registro a un servidor propio, para que la estación no dependa del computador donde corre el panel.', 'Hoy la pasarela y el panel corren en el equipo de la estación.'],
  ['Aplicación móvil', 'Una app con cámara para identificar por foto sin pasar por Telegram.', 'Hoy la foto se envía por el bot o desde la app de escritorio.'],
  ['Reconocimiento más rápido y probado con fotos nuevas', 'Bajar la mediana de 2,37 s a la meta de 1 a 2 s y evaluar con fotos independientes de las de ajuste.', 'Ver las limitaciones medidas en Inteligencia artificial.'],
  ['Esquema dedicado para el Challenge', 'Un usuario de base propio para el juego, con ORDS habilitado solo para él.', 'Recomendado en el informe del 25/11/2026; requiere la cuenta administradora.'],
  ['Interfaces más pequeñas', 'Separar la lectura del catálogo de su administración y sacar la configuración del coordinador.', 'Ver SOLID en Programación III.'],
];

export function render() {
  return `${cabecera({ titulo, entrada: 'Hacia dónde podría crecer EcoAccess. Nada de esta página está construido: cada punto es una propuesta y dice desde dónde se parte hoy.' })}
  <section class="bloque" aria-labelledby="f-lista">
    <h2 id="f-lista" class="sr">Propuestas</h2>
    <div class="rejilla">
      ${PROPUESTAS.map(([t, d, hoy]) => `<div class="dato" style="--c:var(--e-propuesta)">
        ${sello('propuesta')}
        <h3 style="margin:10px 0 4px">${escapar(t)}</h3>
        <p style="margin:0">${escapar(d)}</p>
        <p class="nota-fuente" style="margin:8px 0 0"><strong>Hoy:</strong> ${escapar(hoy)}</p>
      </div>`).join('')}
    </div>
  </section>`;
}
