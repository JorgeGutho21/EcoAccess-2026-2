// Integraciones. Fuentes: web/ciudadano/README.md (pasarela y seguridad), .github/workflows/*.yml,
// reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md §3, §7 y §9.
import { cabecera, sello } from './comun.js';

export const titulo = 'Integraciones';
const MIGAS = [['Tecnología', '#/tecnologia']];

export function render() {
  return `${cabecera({ titulo, migas: MIGAS, entrada: 'Cómo se conectan las piezas: Telegram, esta página con la estación física, Oracle Cloud y la publicación automática en GitHub.' })}

  <section class="bloque" aria-labelledby="n-web">
    <h2 id="n-web">Esta página y la estación física</h2>
    <p>La página es estática y no conoce ninguna dirección de la estación. La dirección de la pasarela y un permiso temporal llegan en el <strong>código QR</strong> que se escanea frente a las canecas, dentro del fragmento <code>#…</code> de la URL: ese fragmento no viaja al servidor de la página y se borra apenas se lee.</p>
    <ol class="recorrido">
      <li><span><b>GitHub Pages</b>Clasifica en el navegador. Sin QR, todo es consulta ilustrativa.</span></li>
      <li><span><b>Túnel HTTPS</b>Expone solo la pasarela (<code>/api/v1/…</code>), nunca el panel, Oracle ni Ollama.</span></li>
      <li><span><b>Pasarela ciudadana</b>Valida el permiso, limita el ritmo y evita repetir una apertura por doble clic.</span></li>
      <li><span><b>Panel de canecas</b>Exige la sesión supervisada del operario y es el único que habla con el ESP32.</span></li>
    </ol>
    <h3>Protecciones de la apertura remota</h3>
    <ul>
      <li>Permiso presencial de 2 horas; un QR nuevo o «Terminar» invalida el anterior. Solo se guarda su huella SHA-256.</li>
      <li>Origen permitido: únicamente la página publicada (y <code>localhost:5173</code> para desarrollo). Cuerpo de máximo 4 KB.</li>
      <li>Una apertura cada 6 s y 40 cada 10 minutos, una operación a la vez; las solicitudes de más de 60 s se rechazan.</li>
      <li>La detención por software no sustituye cortar la alimentación física.</li>
    </ul>
    <p>${sello('verificado', 'Pasarela y túnel en marcha')} ${sello('pendiente', 'Flujo completo con QR sin probar')}</p>
  </section>

  <section class="bloque" aria-labelledby="n-telegram">
    <h2 id="n-telegram">Telegram</h2>
    <p>El bot <a href="https://t.me/EcoAccessP3_JG_bot" target="_blank" rel="noopener">@EcoAccessP3_JG_bot</a> funciona por sondeo (polling): un único proceso consulta la API de Telegram por HTTP. Recibe fotos, responde con la caneca y, para cuentas autorizadas, pide la apertura al coordinador. El historial oficial de esta página se vincula con un código de un solo uso que se confirma en el bot.</p>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Punto</th><th scope="col">Estado</th></tr></thead>
      <tbody>
        <tr><td>Un solo bot en sondeo (no se inicia un segundo consumidor)</td><td>${sello('verificado')}</td></tr>
        <tr><td>Corrección de «No pude descargar la foto» (conexiones y reintento acotado)</td><td>${sello('verificado', 'En código y pruebas')}</td></tr>
        <tr><td>Orden al ESP32 y confirmación solo si el controlador confirma</td><td>${sello('simulado')}</td></tr>
        <tr><td>Foto real enviada desde un celular</td><td>${sello('pendiente')}</td></tr>
        <tr><td>Cuentas autorizadas para abrir canecas desde Telegram</td><td>${sello('pendiente', 'Hoy 0')}</td></tr>
      </tbody>
    </table></div>
  </section>

  <section class="bloque" aria-labelledby="n-oracle">
    <h2 id="n-oracle">Oracle Cloud</h2>
    <ul>
      <li><strong>Aplicación y bot:</strong> se conectan con ODP.NET y el wallet de la base (ver <a href="#/tecnologia/datos">Base de datos</a>).</li>
      <li><strong>Aperturas desde la web:</strong> la pasarela anota cada apertura en una bitácora local (<code>PENDIENTE_ORACLE</code>) y un sincronizador la lleva a Oracle sin volver a mover nada.</li>
      <li><strong>Challenge:</strong> ORDS publica un único módulo (<code>eco.juego.v1</code>); ninguna tabla queda expuesta por AutoREST y CORS solo admite la página publicada. ${sello('verificado')}</li>
    </ul>
    <p>${sello('propuesta', 'Mejora')} Los procedimientos de ORDS corren con el esquema de la aplicación. Un esquema dedicado al juego aislaría por completo sus permisos.</p>
  </section>

  <section class="bloque" aria-labelledby="n-github">
    <h2 id="n-github">GitHub Actions y GitHub Pages</h2>
    <ul>
      <li><code>compilacion.yml</code>: compila la solución en Debug y Release en Windows y ejecuta las pruebas que no necesitan Oracle, Telegram ni hardware.</li>
      <li><code>web-ciudadana.yml</code>: prueba el motor de residuos, busca secretos en la página y publica en <code>gh-pages</code> esta página en la raíz, el Challenge en <code>/juego/</code> y la demostración del panel en <code>/panel-demo/</code>. Publica solo cuando el cambio llega a <code>main</code>.</li>
    </ul>
    <p>${sello('verificado', 'En código')} La publicación corre al fusionar con <code>main</code>.</p>
  </section>`;
}
