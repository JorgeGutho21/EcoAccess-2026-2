// Arquitectura. Capas y dependencias leídas de EcoAccess.P3.sln, de cada .csproj y de Directory.Build.props.
import { cabecera, sello, codigo, escapar, movimientoReducido } from './comun.js';
import { caja, relacion, lienzo, definiciones } from './uml.js';

export const titulo = 'Arquitectura';
const MIGAS = [['Tecnología', '#/tecnologia']];

function capas() {
  const pres = caja(20, 14, 200, 'Presentacion', { estereotipo: 'WinExe · Windows Forms', tono: 'capa' });
  const bot = caja(250, 14, 200, 'Bot', { estereotipo: 'Exe · Telegram y visión', tono: 'capa' });
  const inst = caja(480, 14, 170, 'Instalador', { estereotipo: 'Exe' });
  const datos = caja(250, 120, 200, 'Datos', { estereotipo: 'Oracle.ManagedDataAccess' });
  const logica = caja(20, 226, 430, 'Logica', { estereotipo: 'contratos (interfaces) y servicios' });
  const ent = caja(20, 332, 430, 'Entidades', { estereotipo: 'dominio, sin dependencias' });
  const panel = caja(560, 226, 180, 'PanelCanecas', { estereotipo: 'Exe · 127.0.0.1:8090' });
  const pasa = caja(560, 120, 180, 'PasarelaCiudadana', { estereotipo: 'Exe · 127.0.0.1:8092' });
  const esp = caja(560, 332, 180, 'ESP32', { estereotipo: 'USB · COM3', tono: 'externo' });
  return lienzo(760, 400, 'Proyectos de la solución y sus referencias',
    'Presentacion y Bot referencian a Logica, Entidades y Datos. Datos referencia a Logica y Entidades. Logica referencia a Entidades. Instalador referencia a Datos. PasarelaCiudadana y PanelCanecas no referencian otros proyectos: se comunican por HTTP local, y solo el panel habla con el ESP32 por USB.',
    [pres, bot, inst, datos, logica, ent, panel, pasa, esp].map((c) => c.svg).join('')
    + relacion(pres.cx, pres.abajo, pres.cx, logica.arriba, 'usa')
    + relacion(pres.cx + 40, pres.abajo, datos.x, datos.y + 20, 'usa')
    + relacion(bot.cx, bot.abajo, datos.cx, datos.arriba, 'usa')
    + relacion(inst.cx - 40, inst.abajo, datos.x + datos.w - 10, datos.arriba, 'usa')
    + relacion(datos.cx, datos.abajo, datos.cx, logica.arriba, 'usa')
    + relacion(logica.cx, logica.abajo, logica.cx, ent.arriba, 'usa')
    + relacion(pasa.cx, pasa.abajo, panel.cx, panel.arriba, 'flujo', 'HTTP')
    + relacion(panel.cx, panel.abajo, esp.cx, esp.arriba, 'flujo', 'USB'), 640);
}

const ACTORES = ['Persona', 'Bot o app C#', 'Ollama', 'Reglas (Logica)', 'Coordinador', 'Panel + ESP32', 'Oracle Cloud'];
const PASOS = [
  { de: 0, a: 1, r: 'foto', t: 'Foto', d: 'La persona envía la foto al bot de Telegram o la toma la aplicación de escritorio.' },
  { de: 1, a: 2, r: 'imagen 512 px', t: 'Reconocimiento', d: 'La imagen se reduce a 512 px y se envía a Ollama (lfm3-vl) en el equipo de la estación. Responde un JSON con lo que ve.' },
  { de: 1, a: 3, r: 'propuesta JSON', t: 'Clasificación', d: 'Las reglas de EcoAccess comprueban que el material exista en el catálogo y deciden la categoría. Si hay duda o el residuo es especial, no se abre nada.' },
  { de: 3, a: 4, r: 'caneca(s)', t: 'Coordinador', d: 'El coordinador verifica la autorización, bloquea duplicados por clave de idempotencia y atiende una operación a la vez.' },
  { de: 4, a: 5, r: 'orden', t: 'ESP32', d: 'La orden llega al panel local, único dueño del puerto USB (COM3), que la envía al ESP32.' },
  { de: 5, a: 5, r: 'PWM: abrir', t: 'Apertura', d: 'El ESP32 genera la señal PWM hacia el servo. Es una apertura comandada: no hay sensor que confirme la posición de la tapa.', e: 'comandada' },
  { de: 5, a: 5, r: 'PWM: cerrar', t: 'Cierre', d: 'Pasada la ventana (7, 14 o 21 s según cuántas canecas), el ESP32 ordena el cierre.', e: 'comandada' },
  { de: 4, a: 6, r: 'OPERACION_ESTACION', t: 'Oracle', d: 'La operación se registra en la tabla OPERACION_ESTACION (migración V030), con el canal y el estado del hardware.' },
  { de: 6, a: 0, r: 'historial', t: 'Historial', d: 'La persona consulta su operación con /historial en el bot o en «Historial oficial» de esta página, si vinculó su cuenta.' },
];
const X = (i) => 60 + i * 130;
const Y = (i) => 92 + i * 40;

function secuencia() {
  const cab = ACTORES.map((a, i) => `<g class="caja externo"><rect x="${X(i) - 58}" y="10" width="116" height="34" rx="8"/><text class="nombre" x="${X(i)}" y="32" text-anchor="middle">${escapar(a)}</text></g>
    <line x1="${X(i)}" x2="${X(i)}" y1="44" y2="${Y(PASOS.length) - 10}" stroke="#C9D6CD" stroke-width="1.5" stroke-dasharray="4 4"/>`).join('');
  const flechas = PASOS.map((p, i) => {
    const y = Y(i);
    const linea = p.de === p.a
      ? `<path class="rel flujo" d="M${X(p.de)} ${y - 8} h36 v16 h-34" marker-end="url(#m-flecha)"/><text class="rotulo-rel" x="${X(p.de) + 42}" y="${y + 4}">${escapar(p.r)}</text>`
      : `${relacion(X(p.de) + (p.a > p.de ? 4 : -4), y, X(p.a) + (p.a > p.de ? -6 : 6), y, p.a < p.de ? 'usa' : 'flujo')}<text class="rotulo-rel" x="${(X(p.de) + X(p.a)) / 2}" y="${y - 6}" text-anchor="middle">${escapar(p.r)}</text>`;
    return `<g class="paso-anim" data-paso="${i}"><circle cx="${Math.min(X(p.de), X(p.a)) - 30}" cy="${y}" r="10" fill="var(--bosque)"/><text x="${Math.min(X(p.de), X(p.a)) - 30}" y="${y + 4}" text-anchor="middle" style="fill:#fff;font-size:11px;font-weight:700">${i + 1}</text>${linea}</g>`;
  }).join('');
  return lienzo(900, Y(PASOS.length) + 4, 'Diagrama de secuencia: de la foto al historial', PASOS.map((p, i) => `${i + 1}. ${p.t}: ${p.d}`).join(' '), cab + flechas, 720);
}

export function render() {
  return `${definiciones}${cabecera({ titulo, migas: MIGAS, entrada: 'Cómo está organizada la solución <code>EcoAccess.P3.sln</code>: qué hace cada proyecto, de qué depende y cómo viaja una foto hasta abrir una caneca.' })}

  <section class="bloque" aria-labelledby="a-capas">
    <h2 id="a-capas">Capas reales de la solución</h2>
    <p>Nueve proyectos de código en C# 9 sobre .NET Framework 4.8, más dos de pruebas (unitarias y de integración). Las flechas son referencias entre proyectos tomadas de cada <code>.csproj</code>: la lógica no conoce la base de datos ni la interfaz.</p>
    <div class="lamina">${capas()}<p class="lamina-pie">Flecha punteada: «referencia a» (para no saturar el dibujo se omiten algunas referencias directas a Logica y Entidades; la tabla las lista todas). Flecha verde: comunicación en ejecución (HTTP local o USB). Todos los proyectos referencian además <code>Compatibilidad</code> (tipos que .NET Framework 4.8 no trae), por <code>Directory.Build.props</code>.</p></div>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Proyecto</th><th scope="col">Responsabilidad</th><th scope="col">Depende de</th></tr></thead>
      <tbody>
        <tr><th scope="row">Entidades</th><td>Clases del dominio con sus validaciones (Material, Contenedor, OrdenApertura…)</td><td>Nada del proyecto</td></tr>
        <tr><th scope="row">Logica</th><td>Contratos (interfaces), reglas de negocio, coordinador de la estación, catálogos</td><td>Entidades; QRCoder</td></tr>
        <tr><th scope="row">Datos</th><td>Repositorios Oracle que implementan los contratos de Logica</td><td>Logica, Entidades; Oracle.ManagedDataAccess</td></tr>
        <tr><th scope="row">Presentacion</th><td>Aplicación de escritorio (Windows Forms) del operario y administrador</td><td>Logica, Entidades, Datos</td></tr>
        <tr><th scope="row">Bot</th><td>Bot de Telegram, clasificadores de visión, ejecutores de dispositivo</td><td>Logica, Entidades, Datos; SkiaSharp</td></tr>
        <tr><th scope="row">PanelCanecas</th><td>Panel local que controla y calibra las tres tapas; único dueño del puerto USB</td><td>Ningún proyecto</td></tr>
        <tr><th scope="row">PasarelaCiudadana</th><td>Recibe las aperturas de esta página (con QR presencial) y las pasa al panel</td><td>Ningún proyecto</td></tr>
        <tr><th scope="row">Instalador</th><td>Instala y verifica la estación en otro equipo</td><td>Datos</td></tr>
      </tbody>
    </table></div>
    <p class="nota-fuente">Fuentes: <code>EcoAccess.P3.sln</code>, <code>src/*/*.csproj</code>, <code>Directory.Build.props</code>.</p>
  </section>

  <section class="bloque" aria-labelledby="a-casos">
    <h2 id="a-casos">Casos de uso</h2>
    <div class="rejilla">
      <div class="dato" style="--c:var(--caneca-verde)"><b>Ciudadano</b><span>Consultar dónde va un residuo (web, texto o voz) · Abrir la caneca con el QR presencial · Enviar una foto al bot · Ver su historial</span></div>
      <div class="dato" style="--c:var(--caneca-blanca-borde)"><b>Operario</b><span>Registrar un depósito asistido · Abrir una caneca manualmente · Habilitar la sesión supervisada y el QR de la estación</span></div>
      <div class="dato" style="--c:var(--caneca-negra)"><b>Administrador</b><span>Gestionar materiales, usuarios y roles · Consultar estadísticas en Oracle</span></div>
      <div class="dato"><b>Participante</b><span>Jugar el EcoAccess Challenge y ver la clasificación</span></div>
    </div>
  </section>

  <section class="bloque" aria-labelledby="a-secuencia">
    <h2 id="a-secuencia">De la foto al historial, paso a paso</h2>
    <p>El recorrido del canal con foto (Telegram o aplicación de escritorio). Pulsa «Recorrer» para animarlo o avanza con «Siguiente paso».</p>
    <div class="lamina" id="recorridoArq">
      ${secuencia()}
      <div class="controles">
        <button class="btn-principal" type="button" data-accion="play">Recorrer paso a paso</button>
        <button class="btn-sec" type="button" data-accion="siguiente">Siguiente paso</button>
        <button class="btn-sec" type="button" data-accion="reiniciar">Reiniciar</button>
      </div>
      <ol class="recorrido" aria-label="Pasos del recorrido">
        ${PASOS.map((p) => `<li><span><b>${escapar(p.t)}</b>${escapar(p.d)}${p.e ? ` ${sello('pendiente', 'Comandada, sin confirmación física')}` : ''}</span></li>`).join('')}
      </ol>
      <p class="sr" aria-live="polite" id="pasoActualSr"></p>
    </div>
    <p>En el código, el estado que devuelve el hardware se nombra para no exagerar:</p>
    ${codigo('src/EcoAccess.P3.Logica/Vision35/Expo/EstacionExpo.cs', `
/// <summary>Qué confirmó el hardware. PWM_CONFIRMADO NO significa que la tapa se movió: no hay sensores.</summary>
public enum EstadoHardwareEstacion { PWM_CONFIRMADO, SIMULADO, RECHAZADO, SIN_RESPUESTA, NO_SOLICITADO, DUPLICADA_BLOQUEADA }`)}
  </section>

  <section class="bloque" aria-labelledby="a-web">
    <h2 id="a-web">El camino de esta página</h2>
    <p>La consulta por texto o voz ocurre en tu navegador, sin servidor ni Ollama. Solo la apertura física sale del navegador, y únicamente después de escanear el QR presencial:</p>
    <ol class="recorrido">
      <li><span><b>GitHub Pages (HTTPS)</b>Esta página estática, sin secretos.</span></li>
      <li><span><b>Pasarela HTTPS</b>Un túnel hacia la pasarela de la estación; el permiso temporal viaja en el QR y vence en 2 horas.</span></li>
      <li><span><b>Panel local</b>Exige además la sesión supervisada del operario.</span></li>
      <li><span><b>ESP32</b>Mueve el servo de la caneca indicada.</span></li>
    </ol>
  </section>`;
}

export function montar(raiz) {
  const caja = raiz.querySelector('#recorridoArq');
  const grupos = [...caja.querySelectorAll('.paso-anim')];
  const items = [...caja.querySelectorAll('.recorrido li')];
  const sr = caja.querySelector('#pasoActualSr');
  const play = caja.querySelector('[data-accion="play"]');
  let paso = -1, t = 0;
  const pintar = () => {
    grupos.forEach((g, i) => g.classList.toggle('on', paso < 0 || i <= paso));   // en reposo se ve completo
    items.forEach((li, i) => { li.className = i < paso ? 'hecho' : i === paso ? 'actual' : ''; if (i === paso) li.setAttribute('aria-current', 'step'); else li.removeAttribute('aria-current'); });
    if (paso >= 0) sr.textContent = `Paso ${paso + 1} de ${PASOS.length}: ${PASOS[paso].t}. ${PASOS[paso].d}`;
  };
  const detener = () => { clearInterval(t); t = 0; play.textContent = 'Recorrer paso a paso'; };
  const siguiente = () => { paso = Math.min(PASOS.length - 1, paso + 1); pintar(); if (paso === PASOS.length - 1) detener(); };
  play.addEventListener('click', () => {
    if (t) return detener();
    if (paso >= PASOS.length - 1) paso = -1;
    siguiente();
    if (movimientoReducido()) return;   // con movimiento reducido se avanza a mano
    play.textContent = 'Pausar'; t = setInterval(siguiente, 1800);
  });
  caja.querySelector('[data-accion="siguiente"]').addEventListener('click', () => { detener(); siguiente(); });
  caja.querySelector('[data-accion="reiniciar"]').addEventListener('click', () => { detener(); paso = -1; sr.textContent = 'Recorrido reiniciado.'; pintar(); });
  pintar();
  return detener;
}
