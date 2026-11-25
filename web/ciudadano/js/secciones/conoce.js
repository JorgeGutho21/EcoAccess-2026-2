// Conoce EcoAccess. Fuentes: docs/introduccion/00_presentacion.md, hardware/README.md y
// reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md (mediciones del 25/11/2026). Sin estadísticas ambientales.
import { cabecera, sello, leyenda } from './comun.js';

export const titulo = 'Conoce EcoAccess';

const canecas = (c) => `<span class="mini"><img src="assets/canecas/${c}-cuerpo.webp" alt="" width="540" height="716" loading="lazy"><img src="assets/canecas/${c}-tapa.webp" alt="" width="540" height="716" loading="lazy"></span>`;

export function render() {
  return `${cabecera({
    titulo,
    entrada: 'Una estación de tres canecas que te dice dónde va tu residuo y, cuando estás frente a ella, abre la tapa correcta. Es el proyecto de aula de Programación de Computadores III de la Universidad Popular del Cesar.',
  })}

  <section class="bloque prosa" aria-labelledby="c-problema">
    <h2 id="c-problema">El problema</h2>
    <p>En el momento de botar algo, muchas personas no saben en qué caneca va. Lo aprovechable se mezcla con lo que no lo es y los residuos peligrosos terminan en recipientes comunes.</p>
    <p>EcoAccess orienta justo en ese momento y deja un registro que se puede revisar. No afirma que un residuo se recicló por haber pasado por el sistema, y no publica cifras ambientales que el proyecto no haya medido.</p>
    <p class="nota-fuente">Fuente: <code>docs/introduccion/00_presentacion.md</code></p>
  </section>

  <section class="bloque" aria-labelledby="c-objetivos">
    <h2 id="c-objetivos">Objetivos</h2>
    <p><strong>General:</strong> construir un prototipo inteligente de orientación y registro de residuos con canecas físicas, Telegram, Windows Forms, Oracle y ESP32.</p>
    <ol>
      <li>Identificar el residuo (por foto, texto o voz) y orientar su disposición.</li>
      <li>Autorizar la apertura solo mediante reglas de negocio, nunca por decisión directa del modelo de IA.</li>
      <li>Registrar cada operación en Oracle sin depósitos ni puntos ficticios.</li>
      <li>Ofrecer al operador un registro asistido en la aplicación de escritorio.</li>
      <li>Diseñar para varias canecas sin reconstruir la solución.</li>
    </ol>
    <p>La primera versión tenía una caneca con dos servomotores en la misma tapa. La estación actual tiene tres canecas, con un servo por tapa: verde, blanca y negra.</p>
    <div class="trio" aria-hidden="true">${['verde', 'blanca', 'negra'].map(canecas).join('')}</div>
  </section>

  <section class="bloque" aria-labelledby="c-publico">
    <h2 id="c-publico">Para quién es</h2>
    <div class="rejilla">
      <div class="dato" style="--c:var(--caneca-verde)"><b>Comunidad</b><span>Estudiantes, docentes y visitantes que necesitan saber en qué caneca va su residuo.</span></div>
      <div class="dato" style="--c:var(--caneca-blanca-borde)"><b>Operarios</b><span>Quienes atienden el punto ecológico y registran depósitos de forma asistida.</span></div>
      <div class="dato" style="--c:var(--caneca-negra)"><b>Gestión ambiental</b><span>El equipo de la UPC que consulta los registros y estadísticas en Oracle.</span></div>
    </div>
  </section>

  <section class="bloque" aria-labelledby="c-canales">
    <h2 id="c-canales">Cinco formas de usarlo</h2>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Canal</th><th scope="col">Cómo identifica</th><th scope="col">¿Puede abrir una caneca?</th></tr></thead>
      <tbody>
        <tr><th scope="row">Esta página web</th><td>Texto o voz (es-CO), con un catálogo local en el navegador</td><td>Solo con el QR presencial de la estación; sin él, es una consulta ilustrativa</td></tr>
        <tr><th scope="row"><a href="./dos-clics/">EcoAccess dos clics manual</a></th><td>Tocar la caneca, o escribir o decir el residuo</td><td>Solo con el QR presencial de la estación; sin él, es ilustrativa</td></tr>
        <tr><th scope="row">Bot de Telegram</th><td>Foto del residuo, analizada por un modelo local</td><td>Solo cuentas autorizadas ${sello('pendiente', 'Hoy hay 0 cuentas autorizadas')}</td></tr>
        <tr><th scope="row">Aplicación de escritorio (C#)</th><td>Foto o registro manual del operario</td><td>Sí, con sesión supervisada</td></tr>
        <tr><th scope="row">Panel local de canecas</th><td>No identifica: controla y calibra</td><td>Sí, solo desde el equipo de la estación</td></tr>
      </tbody>
    </table></div>
  </section>

  <section class="bloque" aria-labelledby="c-tec">
    <h2 id="c-tec">Tecnologías</h2>
    <ul>
      <li><strong>C# 9 sobre .NET Framework 4.8</strong>, Windows Forms y MSTest (<code>Directory.Build.props</code>, <code>EcoAccess.P3.sln</code>).</li>
      <li><strong>Oracle Autonomous Database</strong> en Oracle Cloud, con <code>Oracle.ManagedDataAccess</code>, PL/SQL y ORDS para el Challenge.</li>
      <li><strong>Telegram Bot API</strong> por HTTP.</li>
      <li><strong>Ollama</strong> con el modelo de visión <code>lfm3-vl</code>, ejecutado en el equipo de la estación.</li>
      <li><strong>ESP32</strong> programado con Arduino/C++, tres servos por PWM y conexión USB.</li>
      <li><strong>HTML, CSS y JavaScript</strong> sin dependencias ni compilación para esta página; GitHub Actions y GitHub Pages para publicarla.</li>
    </ul>
    <p><a href="#/tecnologia">Ver cómo encaja cada tecnología</a></p>
  </section>

  <section class="bloque" aria-labelledby="c-resultados">
    <h2 id="c-resultados">Resultados medidos</h2>
    <p>Un resumen. El detalle, con cada limitación, está en <a href="#/resultados">Resultados medidos</a>.</p>
    ${leyenda(['verificado', 'simulado', 'pendiente', 'error'])}
    <div class="rejilla">
      <div class="dato"><b>2589</b><span>Pruebas unitarias de C# correctas, 0 con error y 1 omitida (Release).</span>${sello('verificado')}</div>
      <div class="dato"><b>7 / 14 / 21 s</b><span>Ventana de apertura para 1, 2 o 3 canecas, idéntica en C#, panel, pasarela y web.</span>${sello('verificado')}</div>
      <div class="dato"><b>14/14</b><span>Combinaciones de registro de la estación contra un esquema Oracle aislado, sin hardware.</span>${sello('simulado')}</div>
      <div class="dato"><b>11 → 0</b><span>Aperturas incorrectas en 125 fotos, medidas sobre las mismas fotos con que se ajustaron las reglas: no hay conjunto ciego.</span>${sello('verificado', 'Medido, sin evaluación ciega')}</div>
      <div class="dato" style="--c:var(--peligro)"><b>≈ 2,8 s</b><span>Mediana del reconocimiento por foto (P95 ≈ 6,4 s), por encima de la meta de 1 a 2 s.</span>${sello('error')}</div>
      <div class="dato" style="--c:var(--alerta)"><b>28</b><span>Aperturas físicas por probar con la estación conectada (7 combinaciones por 4 canales).</span>${sello('pendiente')}</div>
      <div class="dato"><b>V033</b><span>Invitados y detalle por residuo: aplicada en Oracle de producción el 25/11/2026.</span>${sello('verificado')}</div>
    </div>
    <p class="nota-fuente">Fuentes: <code>reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md</code> §14 y <code>docs/informes/expo11/evaluacion_consolidada_20261125.md</code>. No se volvieron a ejecutar desde esta página.</p>
  </section>

  <section class="bloque prosa" aria-labelledby="c-limites">
    <h2 id="c-limites">Lo que EcoAccess no afirma</h2>
    <ul>
      <li>Las canecas no tienen sensores de posición ni de ingreso: el sistema sabe que <em>ordenó</em> abrir, no que la tapa se movió ni que el residuo entró.</li>
      <li>El reconocimiento por foto es una sugerencia con errores medidos; la decisión de abrir la toman reglas, no el modelo.</li>
      <li>La animación de esta página es ilustrativa: solo hay apertura física cuando la estación la confirma como comandada.</li>
    </ul>
  </section>`;
}
