// Resultados. ÚNICAS fuentes: reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md (§14, actualización del 25/11/2026) y
// docs/informes/expo11/evaluacion_consolidada_20261125.md. Cada cifra lleva su sello: verificado, simulado o pendiente
// (y «limitación abierta» cuando no cumple la meta). No hay estadísticas ambientales: el proyecto no las midió.
import { cabecera, sello, leyenda, escapar } from './comun.js';

export const titulo = 'Resultados';

const INFORME = 'reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md';
const VISION = 'docs/informes/expo11/evaluacion_consolidada_20261125.md';

/** Cifras de cabecera: [cifra, qué es, sello, texto del sello, fuente]. */
const CIFRAS = [
  ['2589', 'pruebas unitarias de C# correctas, 0 con error y 1 omitida (Release)', 'verificado', 'Verificado', `${INFORME} §14.5`],
  ['15/15 · 27/27 · 14/14', 'pruebas de la web: motor de residuos, interfaz en navegador real e integración web → pasarela → panel', 'verificado', 'Verificado', `${INFORME} §14.5`],
  ['14/14', 'combinaciones de registro de la estación contra un esquema Oracle aislado, sin hardware', 'simulado', 'Verificado por simulación', `${INFORME} §2 y §8`],
  ['0 / 28', 'aperturas físicas probadas: 7 combinaciones de canecas por 4 canales', 'pendiente', 'Pendiente', `${INFORME} §14.6`],
];

/** Visión: aperturas incorrectas antes → después, por bloque (evaluación consolidada, §3). */
const VISION_BLOQUES = [
  { n: 'Fotos de ajuste', d: '25 reales, 9 negativas y 10 escenas compuestas', casos: 44, antes: 2, despues: 0 },
  { n: 'Fotos externas con licencia', d: '15 cáscaras de banano, 53 materiales fuera de catálogo y 13 negativas', casos: 81, antes: 9, despues: 0 },
];

/** Concurrencia del juego por HTTP contra ORDS de producción (§5 B). */
const JUEGO = [
  ['10 simultáneos', '10/10', 0, 'verificado', 'Verificado'],
  ['25 simultáneos', '25/25', 60, 'verificado', 'Verificado, con reintentos'],
  ['50 simultáneos', '50/50', 332, 'verificado', 'Verificado, con espera alta'],
  ['100 simultáneos en el mismo instante', '99/100', 1218, 'error', 'Capacidad no demostrada'],
  ['100 llegando escalonados', '100/100', 2, 'verificado', 'Verificado'],
];

/** Migraciones Oracle (§2 y §14.2). */
const MIGRACIONES = [
  ['V030', 'Bitácora OPERACION_ESTACION de cada apertura', 'verificado', 'En producción'],
  ['V031', 'Tablas del EcoAccess Challenge', 'verificado', 'En producción'],
  ['V032', 'Paquete del juego y su API por ORDS', 'verificado', 'En producción'],
  ['V033', 'Invitados y detalle por residuo. Aplicada el 25/11/2026 con respaldo previo; 0 objetos inválidos', 'verificado', 'En producción desde el 25/11/2026'],
  ['V034', 'Bancos de preguntas para la ronda de 10', 'simulado', 'Probada en esquema aislado, no aplicada'],
  ['V035', 'Paquete del juego con bancos', 'simulado', 'Probada en esquema aislado, no aplicada'],
];

const CANALES = ['PC', 'Telegram', 'Web', 'Panel'];
const COMBINACIONES = [['verde'], ['blanca'], ['negra'], ['verde', 'blanca'], ['verde', 'negra'], ['blanca', 'negra'], ['verde', 'blanca', 'negra']];
const NOMBRE = { verde: 'Verde', blanca: 'Blanca', negra: 'Negra' };
const SEGUNDOS = { 1: 7, 2: 14, 3: 21 };

const FOTOS = [['serum', 'Frasco de sérum'], ['aluminio', 'Papel aluminio'], ['cuaderno', 'Hoja de cuaderno'], ['servilleta', 'Servilleta usada'], ['limon', 'Limón']];

const fuente = (texto) => `<p class="nota-fuente">Fuente: <code>${escapar(texto)}</code></p>`;

function latencia() {
  // Escala 0–7 s. Meta 1–2 s; mediana 2,78 s; P95 6,38 s (total de 125 imágenes, versión final).
  const x = (s) => 24 + (s / 7) * 612;
  const marcas = [0, 1, 2, 3, 4, 5, 6, 7].map((s) => `<line class="tic" x1="${x(s)}" x2="${x(s)}" y1="74" y2="80"/><text class="eje-t" x="${x(s)}" y="96" text-anchor="middle">${s} s</text>`).join('');
  return `<svg class="latencia" viewBox="0 0 660 104" role="img" aria-labelledby="lat-t lat-d">
    <title id="lat-t">Tiempo de reconocimiento por foto frente a la meta</title>
    <desc id="lat-d">La meta es de 1 a 2 segundos. La mediana medida es de 2,78 segundos y el percentil 95 de 6,38 segundos: ambos quedan fuera de la meta.</desc>
    <rect class="meta" x="${x(1)}" y="22" width="${x(2) - x(1)}" height="46" rx="6"/>
    <text class="meta-t" x="${(x(1) + x(2)) / 2}" y="16" text-anchor="middle">Meta 1–2 s</text>
    <line class="base" x1="${x(0)}" x2="${x(7)}" y1="74" y2="74"/>${marcas}
    <line class="rango" x1="${x(2.78)}" x2="${x(6.38)}" y1="45" y2="45"/>
    <circle class="mediana" cx="${x(2.78)}" cy="45" r="9"/><text class="valor" x="${x(2.78)}" y="30" text-anchor="middle">mediana 2,78 s</text>
    <rect class="p95" x="${x(6.38) - 7}" y="38" width="14" height="14" rx="2" transform="rotate(45 ${x(6.38)} 45)"/><text class="valor" x="${x(6.38)}" y="30" text-anchor="middle">P95 6,38 s</text>
  </svg>`;
}

function matrizFisica() {
  return `<div class="desliza" tabindex="0" role="group" aria-label="Matriz de las 28 pruebas físicas pendientes. Desliza para ver todo.">
    <table class="matriz-fisica">
      <caption class="sr">28 pruebas físicas: 7 combinaciones de canecas por 4 canales. Todas pendientes.</caption>
      <thead><tr><th scope="col">Combinación</th><th scope="col">Ventana</th>${CANALES.map((c) => `<th scope="col">${c}</th>`).join('')}</tr></thead>
      <tbody>${COMBINACIONES.map((comb) => `<tr>
        <th scope="row"><span class="mini-canecas" aria-hidden="true">${comb.map((c) => `<i data-c="${c}"></i>`).join('')}</span>${comb.map((c) => NOMBRE[c]).join(' + ')}</th>
        <td class="num">${SEGUNDOS[comb.length]} s</td>
        ${CANALES.map(() => '<td><span class="casilla" data-e="pendiente">Pendiente</span></td>').join('')}
      </tr>`).join('')}</tbody>
    </table>
  </div>`;
}

export function render() {
  return `${cabecera({
    titulo: 'Resultados medidos',
    entrada: 'Lo que EcoAccess demostró el 25/11/2026 y lo que todavía no. Cada cifra sale del informe de integración o de la evaluación de visión, con su sello. No se publican cifras ambientales: el proyecto no las midió.',
  })}
  ${leyenda(['verificado', 'simulado', 'pendiente', 'error'])}

  <section class="bloque" aria-labelledby="res-resumen">
    <h2 id="res-resumen" class="sr">Resumen</h2>
    <ul class="acta">${CIFRAS.map(([c, d, e, et, f]) => `<li data-e="${e}">
      <b>${escapar(c)}</b>
      <span class="acta-que">${escapar(d)}</span>
      <span class="acta-estado">${sello(e, et)}<small>${escapar(f)}</small></span>
    </li>`).join('')}</ul>
  </section>

  <section class="bloque" aria-labelledby="res-vision">
    <h2 id="res-vision">Reconocimiento por foto</h2>
    <p>El modelo lfm3-vl se evaluó con <strong>125 imágenes</strong>: 44 propias del proyecto y <strong>81 externas</strong> con licencia libre verificada (CC0, CC BY y CC BY-SA de Wikimedia Commons). Una apertura incorrecta es abrir sin preguntar una caneca que no corresponde.</p>
    <div class="lamina antes-despues">
      <div class="ad-cifra">
        <span class="ad-num" aria-hidden="true"><s>11</s><svg class="ad-flecha" viewBox="0 0 40 24"><path d="M2 12h34M26 3l10 9-10 9"/></svg>0</span>
        <p class="sr">Aperturas incorrectas: de 11 a 0.</p>
        <p class="ad-rotulo">aperturas incorrectas en las 125 imágenes, antes y después de las reglas nuevas ${sello('verificado', 'Medido')}</p>
      </div>
      <div class="ad-bloques">
        ${VISION_BLOQUES.map((b) => `<div class="ad-fila">
          <div class="ad-texto"><b>${escapar(b.n)}</b><span>${escapar(b.d)} · ${b.casos} imágenes</span></div>
          <div class="ad-barras" role="img" aria-label="${escapar(b.n)}: ${b.antes} aperturas incorrectas antes, ${b.despues} después">
            <span class="ad-barra antes" style="--v:${b.antes / 9}"><i></i><em>${b.antes} antes</em></span>
            <span class="ad-barra despues" style="--v:${b.despues / 9}"><i></i><em>${b.despues} después</em></span>
          </div>
        </div>`).join('')}
      </div>
    </div>
    <div class="advertencia" role="note">
      <h3>Por qué ese cero no es una garantía</h3>
      <ul>
        <li>El 0 se midió sobre las mismas fotos que se usaron para escribir las reglas: <strong>ya no hay un conjunto ciego</strong>. ${sello('pendiente', 'Evaluación ciega pendiente')}</li>
        <li>El riesgo pasó a la confirmación humana: los casos «por confirmar» subieron de 17 a 38, y 16 de esos 38 sugieren una caneca equivocada. ${sello('error')}</li>
        <li>La servilleta y el lulo siempre piden confirmación. El catálogo operativo sigue en 11 materiales: ningún material de los 15 complementarios se reconoció de forma consistente.</li>
      </ul>
    </div>
    <figure class="fotos-ajuste">
      <ul>${FOTOS.map(([f, n]) => `<li><img src="assets/fotos/${f}.webp" alt="${escapar(n)} sostenido en la mano, foto real del proyecto" width="320" height="320" loading="lazy"><span>${escapar(n)}</span></li>`).join('')}</ul>
      <figcaption>Algunas de las 25 fotos reales de ajuste. Con ellas se diseñó el sistema, por eso acertarlas no mide cómo funciona con fotos nuevas.</figcaption>
    </figure>
    <h3>Tiempo de respuesta</h3>
    <div class="lamina"><div class="desliza" tabindex="0" role="group" aria-label="Gráfica de tiempo de respuesta. Desliza para ver todo.">${latencia()}</div>
      <p class="lamina-pie">${sello('error', 'Fuera de la meta')} Mediana ≈ 2,8 s y P95 ≈ 6,4 s con el modelo ya cargado. Los casos lentos son los que hacen una segunda pregunta de verificación.</p>
    </div>
    ${fuente(`${VISION} §3 · ${INFORME} §14.3`)}
  </section>

  <section class="bloque" aria-labelledby="res-juego">
    <h2 id="res-juego">EcoAccess Challenge con muchos jugadores</h2>
    <p>Partidas completas por HTTP contra el servicio ORDS de producción, en rondas de prueba que se crearon y eliminaron. El navegador reintenta cuando ORDS responde «429, demasiadas solicitudes».</p>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Escenario</th><th scope="col">Partidas terminadas</th><th scope="col">Respuestas 429</th><th scope="col">Estado</th></tr></thead>
      <tbody>${JUEGO.map(([n, fin, r429, e, et]) => `<tr><th scope="row">${escapar(n)}</th><td class="num">${fin}</td><td class="num">${r429}</td><td>${sello(e, et)}</td></tr>`).join('')}</tbody>
    </table></div>
    <p>Con 100 personas que registran e inician en el mismo segundo, una de cada cien agotó los reintentos de la prueba: <strong>no se declara capacidad para 100 simultáneos</strong>. Con llegadas escalonadas, que es lo normal con un QR en un stand, terminaron las 100. En la base aislada, 100 jugadores concurrentes completaron 700 de 700 respuestas sin duplicados. ${sello('verificado', 'Base aislada: 100/100')}</p>
    ${fuente(`${INFORME} §5`)}
  </section>

  <section class="bloque" aria-labelledby="res-oracle">
    <h2 id="res-oracle">Oracle en producción</h2>
    <ol class="migraciones">${MIGRACIONES.map(([v, d, e, et]) => `<li data-e="${e}"><b>${v}</b><span>${escapar(d)}</span>${sello(e, et)}</li>`).join('')}</ol>
    <p>ORDS publica un único módulo (<code>eco.juego.v1</code>), ninguna tabla queda expuesta por AutoREST y CORS solo admite la página publicada. ${sello('verificado')}</p>
    ${fuente(`${INFORME} §2, §3 y §14.2`)}
  </section>

  <section class="bloque" aria-labelledby="res-fisico">
    <h2 id="res-fisico">Lo que falta probar en la estación</h2>
    <p>Las canecas no tienen sensores: el sistema sabe que <em>ordenó</em> abrir, no que la tapa se movió. Por eso estas pruebas solo se cierran con la estación conectada y una persona mirando. La ventana de 7, 14 o 21 s es idéntica en C#, panel, pasarela y web. ${sello('verificado', 'Ventana 7/14/21 s verificada en código')}</p>
    ${matrizFisica()}
    <ul class="pendientes">
      <li>${sello('pendiente')} Foto y documento reales enviados al bot de Telegram, y al menos una cuenta autorizada para abrir: hoy hay 0.</li>
      <li>${sello('pendiente')} Dictado por micrófono en español de Colombia con un micrófono real.</li>
      <li>${sello('pendiente')} Fotos reales nuevas para una evaluación ciega del reconocimiento.</li>
      <li>${sello('pendiente')} Instalación completa en los equipos de los otros dos integrantes.</li>
      <li>${sello('simulado', 'Simulado')} Las 7 combinaciones de canecas, registradas contra un esquema Oracle aislado: 14/14.</li>
    </ul>
    ${fuente(`${INFORME} §8 y §14.6`)}
  </section>

  <section class="bloque" aria-labelledby="res-instalacion">
    <h2 id="res-instalacion">Instalación en la estación</h2>
    <p>El instalador deja cuatro accesos directos en el escritorio. Abrirlos no mueve ninguna tapa hasta que el operario activa el control.</p>
    <ul class="accesos">
      <li><b>ECOACCESS P3</b><span>Aplicación de escritorio con Oracle, visión local, Telegram y tres canecas.</span></li>
      <li><b>ECOACCESS ESP32</b><span>Centro de control local de las canecas (solo en el equipo de la estación).</span></li>
      <li><b>ECOACCESS DOS CLICS MANUAL</b><span>Interfaz ciudadana para elegir la caneca, escribir o hablar. <a href="./dos-clics/">Abrirla</a></span></li>
      <li><b>ECOACCESS WEB</b><span>Esta plataforma: consulta ciudadana, contenido académico y el Challenge.</span></li>
    </ul>
    <p>${sello('verificado', 'Definido en el instalador')} <code>scripts/instalacion/Crear-AccesosDirectos.ps1</code>. El Challenge ya no tiene acceso propio: vive en <a href="#/challenge">#/challenge</a>.</p>
  </section>`;
}
