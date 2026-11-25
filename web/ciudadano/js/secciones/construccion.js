// Cómo se construyó. Fechas y commits tomados de la historia de git del repositorio (git log) y de los documentos citados.
import { cabecera, sello, escapar } from './comun.js';

export const titulo = 'Cómo se construyó';

const ETAPAS = [
  { t: 'Problema', f: '01/11/2026', d: 'Se definió el problema (no saber en qué caneca va cada residuo), el objetivo general, el alcance y las limitaciones honestas: sin sensor de ingreso no se puede afirmar un depósito.', e: 'docs/introduccion/00_presentacion.md · commit bdd8c8b (primer commit del repositorio)' },
  { t: 'Requisitos', f: '01/11 – 11/11', d: 'Las reglas de negocio (RN-01, RN-07, RN-16…) quedaron escritas en el diccionario de datos y en las entidades con sus pruebas. El 11/11 se consolidó una matriz de requisitos en el documento maestro.', e: 'database/01_modelo/DICCIONARIO_DATOS.md · commits 7ece9a7 y 890ec74' },
  { t: 'Diseño', f: '01/11 – 09/11', d: 'Arquitectura por capas: Entidades, Lógica (contratos y servicios), Datos y Presentación. El 09/11 se publicaron diez diagramas UML: clases, paquetes, componentes, secuencia, casos de uso, despliegue, entidad-relación y flujos.', e: 'docs/academico/diagramas/*.svg · commits e95eebf y a78e10d' },
  { t: 'Aplicación', f: '01/11/2026', d: 'Aplicación de escritorio en Windows Forms para el operario: registro asistido, catálogo, estadísticas y control de la estación.', e: 'src/EcoAccess.P3.Presentacion · commit 88daf52' },
  { t: 'Base de datos', f: '01/11 – 25/11', d: 'Persistencia Oracle con migraciones reproducibles numeradas. El 09/11 se validó la conexión a Oracle Cloud con wallet. Hoy van de V002 a V033; V030 a V033 cubren la estación y el Challenge.', e: 'database/06_migraciones · commits 79e840d y 3d35793' },
  { t: 'ESP32', f: '01/11 – 24/11', d: 'Del piloto de una tapa con dos servos se pasó el 11/11 a una estación de tres canecas con un ESP32. El 24/11 se agregó un panel local para controlar y calibrar las tres tapas.', e: 'hardware/ · commits 6c4b6d4, 4ee82d8 y 20384bd' },
  { t: 'Telegram', f: '01/11/2026', d: 'Bot que recibe la foto del residuo, responde con la caneca y, para cuentas autorizadas, pide la apertura al coordinador.', e: 'src/EcoAccess.P3.Bot · commit 09d4016' },
  { t: 'Web', f: '25/11/2026', d: 'Esta página: consulta por texto o voz en el navegador y apertura física solo con el QR presencial, a través de una pasarela HTTPS. Ese mismo día se publicó el EcoAccess Challenge.', e: 'web/ciudadano · commits a06622e y 3aa42e7' },
  { t: 'Visión', f: '02/11 – 25/11', d: 'Se reemplazó el modelo inicial por lfm3-vl con un vocabulario cerrado de materiales y se evaluó con fotos reales. Las limitaciones medidas siguen abiertas.', e: 'docs/vision · commit be5bbd0 · informe del 25/11' },
  { t: 'Pruebas', f: '02/11 – 25/11', d: 'Integración continua en GitHub Actions desde el 02/11: compilación Debug y Release y pruebas que no requieren Oracle, Telegram ni hardware. Las 28 aperturas físicas siguen pendientes.', e: '.github/workflows/compilacion.yml · commit 2f9efa7' },
];

export function render() {
  return `${cabecera({ titulo, entrada: 'Diez etapas, del problema a las pruebas, reconstruidas con la historia de git y los documentos del repositorio. La historia en este repositorio empieza el 01/11/2026.' })}

  <section class="bloque" aria-labelledby="k-linea">
    <h2 id="k-linea">Línea de tiempo</h2>
    <p>Toca una etapa para ver qué se hizo y dónde está la evidencia. Las fechas son las de los commits, no las de la planeación.</p>
    <div class="controles"><button class="btn-sec" type="button" id="etapasTodas">Abrir todas</button></div>
    <ol class="linea" id="lineaEtapas">
      ${ETAPAS.map((e, i) => `<li>
        <h3 style="margin:0;font-size:inherit"><button class="etapa-btn" type="button" aria-expanded="${i === 0}" aria-controls="etapa-${i}">
          <span class="n" aria-hidden="true">${i + 1}</span><span>${escapar(e.t)}</span><span class="fecha">${escapar(e.f)}</span>
        </button></h3>
        <div class="etapa-det" id="etapa-${i}" ${i === 0 ? '' : 'hidden'}>
          <p>${escapar(e.d)}</p>
          <p class="evid">Evidencia: <code>${escapar(e.e)}</code></p>
        </div>
      </li>`).join('')}
    </ol>
  </section>

  <section class="bloque prosa" aria-labelledby="k-metodo">
    <h2 id="k-metodo">Metodología</h2>
    <p>El trabajo siguió cuatro fases que se repitieron en cada módulo:</p>
    <ol>
      <li><strong>Análisis:</strong> problema, reglas de negocio y restricciones (por ejemplo, que la IA nunca abre una caneca por sí sola).</li>
      <li><strong>Diseño:</strong> capas, contratos (interfaces) y modelo de datos antes de implementar.</li>
      <li><strong>Implementación:</strong> por módulos pequeños, cada uno en su rama y con su solicitud de cambios.</li>
      <li><strong>Pruebas:</strong> automáticas en cada cambio y manuales con la estación cuando hay hardware.</li>
    </ol>
  </section>

  <section class="bloque" aria-labelledby="k-xp">
    <h2 id="k-xp">Prácticas de XP: solo las que tienen sustento</h2>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Práctica</th><th scope="col">Evidencia en el repositorio</th><th scope="col">Estado</th></tr></thead>
      <tbody>
        <tr><th scope="row">Pruebas automatizadas</th><td>MSTest en <code>tests/</code> (2518 unitarias según el informe del 25/11) y <code>node --test</code> en <code>web/ciudadano/pruebas</code></td><td>${sello('verificado', 'Con sustento')}</td></tr>
        <tr><th scope="row">Integración continua</th><td><code>.github/workflows/compilacion.yml</code> y <code>web-ciudadana.yml</code> se ejecutan en cada solicitud de cambios</td><td>${sello('verificado', 'Con sustento')}</td></tr>
        <tr><th scope="row">Entregas pequeñas</th><td>Cambios integrados por solicitudes numeradas (#1 a #28) y ramas por funcionalidad</td><td>${sello('verificado', 'Con sustento')}</td></tr>
        <tr><th scope="row">Programación en parejas, juego de planificación, cliente en sitio</th><td>No hay registro en el repositorio que lo demuestre</td><td>${sello('pendiente', 'Sin sustento')}</td></tr>
      </tbody>
    </table></div>
    <p class="nota-fuente">Por eso no se presenta el proyecto como «hecho con XP»: se usaron algunas de sus prácticas.</p>
  </section>`;
}

export function montar(raiz) {
  const botones = [...raiz.querySelectorAll('.etapa-btn')];
  const poner = (b, abierto) => { b.setAttribute('aria-expanded', String(abierto)); document.getElementById(b.getAttribute('aria-controls')).hidden = !abierto; };
  botones.forEach((b) => b.addEventListener('click', () => poner(b, b.getAttribute('aria-expanded') !== 'true')));
  const todas = raiz.querySelector('#etapasTodas');
  todas.addEventListener('click', () => {
    const abrir = botones.some((b) => b.getAttribute('aria-expanded') !== 'true');
    botones.forEach((b) => poner(b, abrir));
    todas.textContent = abrir ? 'Cerrar todas' : 'Abrir todas';
  });
}
