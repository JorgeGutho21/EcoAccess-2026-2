// Pruebas de interfaz del juego con Playwright y la API INTERCEPTADA (route mocks): ninguna solicitud llega a Oracle.
// Recorre registro (con un 429 + Retry-After) → instrucciones → preguntas → resultado → clasificación (y proyector) en
// rondas simuladas de 7 y de 10 preguntas y en ocho tamaños de pantalla, sin desbordes ni errores de consola.
// Ejecutar: node --test web/juego/pruebas/interfaz.test.mjs
// Playwright se busca en el proyecto o en %LOCALAPPDATA%\EcoAccessDevTools\node_modules (o ECO_DEVTOOLS_NODE_MODULES).
// Usa el Chrome del sistema (channel 'chrome') y, si no está, el Chromium de Playwright. Si no hay Playwright, se omite.
// Con ECO_CAPTURAS=1 guarda capturas en reportes/validacion_final/capturas_juego/.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile, mkdir } from 'node:fs/promises';
import { createRequire } from 'node:module';
import { join, extname, normalize, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const AQUI = dirname(fileURLToPath(import.meta.url));
const WEB = normalize(join(AQUI, '..', '..'));                       // carpeta web/ (para que «../» sea ECOACCESS WEB)
const CAPTURAS = join(WEB, '..', 'reportes', 'validacion_final', 'capturas_juego');
const GUARDAR = process.env.ECO_CAPTURAS === '1';

async function cargarPlaywright() {
  try { return await import('playwright'); } catch { /* no está en el proyecto */ }
  const dirs = [process.env.ECO_DEVTOOLS_NODE_MODULES,
    process.env.LOCALAPPDATA && join(process.env.LOCALAPPDATA, 'EcoAccessDevTools', 'node_modules')].filter(Boolean);
  for (const d of dirs) { try { return createRequire(join(d, 'noop.js'))('playwright'); } catch { /* sigue */ } }
  return null;
}
const pw = await cargarPlaywright();
const OMITIR = pw ? false : 'Playwright no está disponible';

const VISTAS = [[320, 568], [360, 800], [390, 844], [430, 932], [768, 1024], [1024, 768], [1366, 768], [1920, 1080]];

// ───────── Datos simulados (mismos textos e imágenes públicos de V031/V034) ─────────
const R7 = [
  ['BASICA', 'Separación', '¿En qué caneca debes depositar una cáscara de banano?', 'img/preguntas/p1-banano.webp', false, ['Caneca blanca', 'Caneca verde', 'Caneca negra', 'Cualquier caneca']],
  ['BASICA', 'Separación', '¿Dónde se deposita un envase de vidrio vacío y limpio?', 'img/preguntas/p2-serum-vidrio.webp', false, ['Caneca verde', 'Caneca negra', 'Caneca blanca', 'Ninguna de las anteriores']],
  ['INTERMEDIA', 'Separación', '¿Cuál de estos residuos corresponde a la caneca negra?', null, true, [['Servilleta usada', 'img/preguntas/p3-servilleta.webp'], ['Papel de cuaderno limpio', 'img/preguntas/p3-cuaderno.webp'], ['Papel aluminio limpio', 'img/preguntas/p3-aluminio.webp'], ['Bolsa de papel kraft limpia', 'img/preguntas/p3-kraft.svg']]],
  ['INTERMEDIA', 'EcoAccess', '¿Cuál es el propósito principal de EcoAccess?', 'img/preguntas/p4-eco-prototipo.webp', false, ['Triturar automáticamente los residuos', 'Fabricar productos reciclados', 'Medir únicamente el peso de la basura', 'Orientar, facilitar y registrar la disposición de residuos']],
  ['INTERMEDIA', 'Reconocimiento', 'Si EcoAccess identifica un limón y papel aluminio limpio en una fotografía, ¿qué canecas debe abrir?', 'img/preguntas/p5-limon-aluminio.webp', false, ['Verde y negra', 'Verde y blanca', 'Blanca y negra', 'Las tres canecas']],
  ['AVANZADA', 'Oracle Cloud', '¿Qué función cumple Oracle Cloud dentro de EcoAccess?', 'img/preguntas/p6-oracle.svg', false, ['Controlar directamente los servomotores', 'Capturar fotografías desde Telegram', 'Almacenar operaciones, usuarios y datos para estadísticas', 'Diseñar la interfaz gráfica de las canecas']],
  ['AVANZADA', 'C# y Visual Studio', '¿Qué responsabilidad tiene la lógica desarrollada en C# mediante Visual Studio?', 'img/preguntas/p7-csharp.svg', false, ['Procesar solicitudes, aplicar reglas y coordinar los servicios del sistema', 'Sustituir físicamente al ESP32', 'Alimentar eléctricamente los servomotores', 'Almacenar todos los datos sin utilizar Oracle']],
];
const R10 = [
  ['BASICA', 'EcoAccess', '¿Cuál es el propósito de EcoAccess?', ['Orientar a la persona, abrir la caneca que corresponde y registrar la disposición de residuos aprovechables', 'Vender canecas inteligentes para los hogares', 'Triturar y compactar los residuos dentro de la caneca', 'Reemplazar la recolección de residuos de la ciudad']],
  ['BASICA', 'Separación', 'Una botella plástica pequeña, vacía y limpia, ¿en qué caneca va?', ['Caneca verde', 'Caneca negra', 'Caneca blanca', 'En cualquiera, porque el plástico no se aprovecha']],
  ['INTERMEDIA', 'ESP32', 'En la estación de EcoAccess, ¿qué pin del ESP32 envía la señal al servomotor de la caneca verde?', ['GPIO 18', 'GPIO 19', 'GPIO 23', 'GPIO 2']],
  ['INTERMEDIA', 'Servomotores', 'Según la calibración guardada en el ESP32, ¿entre qué ángulos se mueve la tapa de la caneca blanca (cerrada → abierta)?', ['De 0° a 50°', 'De 0° a 70°', 'De 0° a 40°', 'De 90° a 180°']],
  ['INTERMEDIA', 'SOLID', 'En EcoAccess, un nuevo proveedor de reconocimiento visual implementa IClasificadorMaterial sin modificar ServicioClasificacionImagen. ¿Qué principio SOLID se aplica?', ['Responsabilidad única', 'Abierto/cerrado', 'Segregación de interfaces', 'Inversión de dependencias']],
  ['AVANZADA', 'Grafos', 'Si los puntos de disposición de Valledupar fueran nodos y las calles aristas con su distancia en metros, ¿qué algoritmo calcula la ruta más corta desde la estación hasta un punto?', ['Algoritmo de Dijkstra', 'Ordenamiento burbuja', 'Búsqueda binaria', 'Búsqueda en anchura, que ignora las distancias']],
  ['AVANZADA', 'Oracle Cloud', '¿Dónde se guardan las operaciones de la estación y los resultados de este concurso?', ['En una hoja de cálculo del portátil de la estación', 'En Oracle Autonomous Database, en Oracle Cloud', 'En la memoria del ESP32', 'En el historial del chat de Telegram']],
  ['AVANZADA', 'Wallet', '¿Para qué usa EcoAccess el Wallet de Oracle?', ['Para pagar los ecopuntos de los usuarios', 'Para conectarse a Oracle Autonomous con TLS mutuo (mTLS) mediante certificados', 'Para guardar las fotografías de los residuos', 'Para calibrar los ángulos de los servomotores']],
  ['AVANZADA', 'Arquitectura web', 'Cuando alguien abre una caneca desde la página pública de EcoAccess, ¿qué camino sigue la orden?', ['GitHub Pages → pasarela HTTPS → panel local → ESP32', 'GitHub Pages → ESP32, directamente por wifi', 'Telegram → Oracle Cloud → servomotor', 'ESP32 → GitHub Pages → panel local']],
  ['AVANZADA', 'Política de apertura', 'Si una persona va a depositar residuos en dos canecas distintas a la vez, ¿cuánto tiempo permanecen abiertas según la política de EcoAccess?', ['7 segundos', '14 segundos', '21 segundos', '60 segundos']],
];
const PASO_MS = 31000;   // el servidor simulado «adelanta» 31 s por pregunta: el reloj debe mostrar SU tiempo

function pregunta(n, orden) {
  if (n === 7) {
    const [nivel, tema, enunciado, imagen, conImg, ops] = R7[orden - 1];
    return { orden, total: 7, nivel, tema, enunciado, imagen, opcionesConImagen: conImg,
      opciones: ops.map((o, i) => ({ id: orden * 100 + i + 1, texto: conImg ? o[0] : o, imagen: conImg ? o[1] : null })) };
  }
  const [nivel, tema, enunciado, ops] = R10[orden - 1];
  return { orden, total: 10, nivel, tema, enunciado, imagen: null, opcionesConImagen: false,
    opciones: ops.map((texto, i) => ({ id: 1000 + orden * 10 + i + 1, texto, imagen: null })) };
}

/** Servidor simulado de la API: estado en memoria por página. */
function crearMock(n) {
  const limiteMs = n === 7 ? 900000 : 300000;
  const comp = { nombre: n === 7 ? 'Ronda simulada de siete' : 'Ronda simulada de diez', estado: 'ABIERTA', registroAbierto: true, definitiva: false, prueba: true, avisoVersion: 1, limiteMs };
  const s = { n, alias: null, actual: 0, inicio: 0, registros: [], llamadas: [], limiteMs };
  const transcurrido = (orden) => (n === 10 && orden === 10 ? limiteMs - 30000 : (orden - 1) * PASO_MS + Math.min(900, Date.now() - s.inicio));
  const enCurso = () => ({ ok: true, estado: 'EN_CURSO', alias: s.alias, competencia: comp, respondidas: s.actual - 1, transcurridoMs: transcurrido(s.actual), pregunta: pregunta(n, s.actual) });
  const final = () => ({ ok: true, estado: 'FINALIZADO', alias: s.alias, competencia: comp, respondidas: n,
    resultado: { puntaje: (n - 2) * 100, aciertos: n - 2, errores: 2, tiempoMs: 84300, posicion: 2, participantes: 18, definitiva: false, revisionDisponible: false } });
  const podio = () => [
    { posicion: 1, alias: 'LunaVerde', puntaje: n * 100, aciertos: n, tiempoMs: 61200 },
    { posicion: 2, alias: s.alias ?? 'Ana_QA', puntaje: (n - 2) * 100, aciertos: n - 2, tiempoMs: 84300 },
    { posicion: 3, alias: 'ReciclaYa', puntaje: (n - 2) * 100, aciertos: n - 2, tiempoMs: 97800 },
    { posicion: 4, alias: 'Caneca_Blanca_2026', puntaje: (n - 3) * 100, aciertos: n - 3, tiempoMs: 70100 },
    { posicion: 5, alias: 'Tomás', puntaje: (n - 4) * 100, aciertos: n - 4, tiempoMs: 125500 },
  ];
  async function responder(route) {
    const req = route.request();
    const cors = { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Headers': '*', 'Access-Control-Allow-Methods': 'GET, POST, OPTIONS', 'Access-Control-Expose-Headers': 'Retry-After' };
    if (req.method() === 'OPTIONS') return route.fulfill({ status: 204, headers: cors });
    const ruta = new URL(req.url()).pathname.replace(/^.*\/juego\/v1/, '');
    s.llamadas.push(ruta);
    const json = (status, cuerpo) => route.fulfill({ status, headers: { ...cors, 'Content-Type': 'application/json' }, body: JSON.stringify(cuerpo) });
    if (ruta === '/estado') return json(200, { ok: true, competencia: comp, participantes: 23, preguntas: n, puntajeMaximo: n * 100 });
    if (ruta === '/registro') {
      s.registros.push(Date.now());
      // Primer envío: el 429 de ORDS (HTML, sin JSON del juego) con Retry-After de 1 s.
      if (s.registros.length === 1) return route.fulfill({ status: 429, headers: { ...cors, 'Retry-After': '1', 'Content-Type': 'text/html' }, body: '<html><body>Too Many Requests</body></html>' });
      s.alias = JSON.parse(req.postData()).alias;
      return json(201, { ok: true, estado: 'REGISTRADO', alias: s.alias, competencia: comp, respondidas: 0 });
    }
    if (ruta === '/intento/inicio') { if (!s.actual) { s.actual = 1; s.inicio = Date.now(); } return json(200, enCurso()); }
    if (ruta === '/intento') return json(200, s.actual > n ? final() : s.actual ? enCurso() : { ok: true, estado: 'REGISTRADO', alias: s.alias, competencia: comp, respondidas: 0 });
    if (ruta === '/intento/respuesta') {
      const { pregunta: p } = JSON.parse(req.postData());
      if (p === s.actual) s.actual++;
      return json(200, s.actual > n ? final() : enCurso());
    }
    if (ruta === '/clasificacion') return json(200, { ok: true, competencia: comp, podio: podio(), participantes: 23, finalizados: 18, enCurso: 5 });
    return json(404, { ok: false, codigo: 'ruta', error: 'Ruta no simulada' });
  }
  return { s, responder };
}

// ───────── Servidor estático de web/ ─────────
const TIPOS = { '.html': 'text/html; charset=utf-8', '.css': 'text/css', '.js': 'text/javascript', '.mjs': 'text/javascript', '.webp': 'image/webp', '.png': 'image/png', '.svg': 'image/svg+xml', '.json': 'application/json' };
let servidor, origen, navegador;

before(async () => {
  if (OMITIR) return;
  servidor = createServer(async (req, res) => {
    let ruta = decodeURIComponent(new URL(req.url, 'http://x').pathname);
    if (ruta.endsWith('/')) ruta += 'index.html';
    const archivo = normalize(join(WEB, ruta));
    if (!archivo.startsWith(WEB)) { res.writeHead(403).end(); return; }
    try { const b = await readFile(archivo); res.writeHead(200, { 'Content-Type': TIPOS[extname(archivo)] ?? 'application/octet-stream' }).end(b); }
    catch { res.writeHead(404).end('no encontrado'); }
  });
  await new Promise((ok) => servidor.listen(0, '127.0.0.1', ok));
  origen = `http://127.0.0.1:${servidor.address().port}`;
  try { navegador = await pw.chromium.launch({ channel: 'chrome' }); }
  catch { navegador = await pw.chromium.launch(); }
  if (GUARDAR) await mkdir(CAPTURAS, { recursive: true });
});
after(async () => { await navegador?.close(); servidor?.close(); });

// ───────── Utilidades ─────────
async function nuevaPagina([ancho, alto], n, extra = {}) {
  const ctx = await navegador.newContext({ viewport: { width: ancho, height: alto }, deviceScaleFactor: 1, locale: 'es-CO', ...extra });
  const mock = crearMock(n);
  await ctx.route(/oraclecloudapps\.com\//, (r) => (/\/ords\/ecoaccess\/juego\/v1\//.test(r.request().url()) ? mock.responder(r) : r.abort()));
  const page = await ctx.newPage();
  const errores = [];
  page.on('pageerror', (e) => errores.push('pageerror: ' + e.message));
  page.on('console', (m) => {
    if (m.type() !== 'error') return;
    const t = m.text();
    // Esperados por la API simulada: el 429 de ORDS se registra como recurso fallido. Fuentes de Google sin red: no es del juego.
    if (/status of 429/.test(t) || /fonts\.(googleapis|gstatic)\.com/.test(t + (m.location()?.url ?? ''))) return;
    errores.push('console: ' + t);
  });
  return { ctx, page, mock, errores };
}

/** Desbordes: scroll horizontal de la página o elementos visibles fuera de la ventana o de su caja. */
const revisarDesbordes = (page) => page.evaluate(() => {
  const w = document.documentElement.clientWidth; const malos = [];
  if (document.documentElement.scrollWidth > w + 1) malos.push(`scroll horizontal ${document.documentElement.scrollWidth} > ${w}`);
  for (const el of document.querySelectorAll('body *')) {
    if (el.closest('[hidden], dialog:not([open]), .hojas-caen, .sr, .saltar, script, style')) continue;
    const r = el.getBoundingClientRect();
    if (!r.width || !r.height) continue;
    if (r.right > w + 1 || r.left < -1) malos.push(`${el.tagName.toLowerCase()}.${[...el.classList].join('.')} [${Math.round(r.left)}, ${Math.round(r.right)}]`);
  }
  for (const el of document.querySelectorAll('.btn, .opcion, .datos li, .cifras div, .siguientes li, .mini-podio li, .puesto, .hud, .volver-web, .tema-visual, .aviso-conexion, .etiqueta-tema')) {
    if (el.closest('[hidden]') || !el.getClientRects().length) continue;
    if (el.scrollWidth > el.clientWidth + 1) malos.push(`contenido desbordado en .${[...el.classList].join('.')} (${el.scrollWidth} > ${el.clientWidth})`);
  }
  return malos.slice(0, 8);
});
async function sinDesbordes(page, donde) { assert.deepEqual(await revisarDesbordes(page), [], `desbordes en ${donde}`); }
async function captura(page, nombre, opciones = {}) { if (GUARDAR) await page.screenshot({ path: join(CAPTURAS, nombre), ...opciones }); }
const visible = (page, pantalla) => page.waitForSelector(`[data-pantalla="${pantalla}"]:not([hidden])`, { timeout: 15000 });

// Capturas elegidas (≤ 10): celular, tableta, PC, proyector y podio.
const CAPTURA = {
  '390x844:7:bienvenida': '01_celular_bienvenida.png',
  '390x844:7:aviso': '02_celular_429_reintentando.png',
  '390x844:7:p1': '03_celular_pregunta_imagen.png',
  '390x844:7:p3': '04_celular_opciones_con_imagen.png',
  '390x844:7:resultado': '05_celular_resultado.png',
  '768x1024:10:p4': '06_tableta_ronda10_pregunta.png',
  '1366x768:7:p5': '07_pc_pregunta_canecas.png',
  '1366x768:10:resultado': '08_pc_resultado_top5.png',
  '390x844:7:clasificacion': '09_celular_podio.png',
  '1920x1080:7:proyector': '10_proyector_podio.png',
};

async function recorrer(vista, n) {
  const clave = (paso) => `${vista[0]}x${vista[1]}:${n}:${paso}`;
  const cap = (page, paso, op) => (CAPTURA[clave(paso)] ? captura(page, CAPTURA[clave(paso)], op) : null);
  const { ctx, page, mock, errores } = await nuevaPagina(vista, n);
  try {
    await page.goto(`${origen}/juego/`);
    await visible(page, 'bienvenida');
    await page.waitForFunction(() => !document.getElementById('comenzar').disabled);
    assert.equal(await page.textContent('#datoPreguntas'), String(n), 'N viene del servidor');
    assert.equal(await page.textContent('#datoPuntos'), String(n * 100));
    assert.equal(await page.getAttribute('.volver-web', 'href'), '../');
    await page.evaluate(() => document.fonts?.ready);
    await sinDesbordes(page, 'bienvenida'); await cap(page, 'bienvenida');

    await page.click('#comenzar'); await visible(page, 'registro');
    await page.fill('#nombre', 'Ana Prueba'); await page.fill('#alias', 'Ana_QA'); await page.check('#acepta');
    await sinDesbordes(page, 'registro');
    await page.click('#registrar');
    // 429 con Retry-After: aviso claro y reintento automático sin perder nada.
    await page.waitForSelector('#conexion:not([hidden])', { timeout: 5000 });
    assert.match(await page.textContent('#conexionTitulo'), /Hay mucha gente jugando, reintentando…/);
    assert.match(await page.textContent('#conexionDetalle'), /Tu progreso está guardado/);
    await sinDesbordes(page, 'aviso 429'); await cap(page, 'aviso');
    await visible(page, 'instrucciones');
    assert.ok(await page.isHidden('#conexion'), 'el aviso se oculta al recuperarse');
    assert.equal(mock.s.registros.length, 2, 'un solo reintento del registro');
    assert.ok(mock.s.registros[1] - mock.s.registros[0] >= 1000, `respeta Retry-After (${mock.s.registros[1] - mock.s.registros[0]} ms)`);
    assert.match(await page.textContent('#saludo'), /Ana_QA/);
    await sinDesbordes(page, 'instrucciones');

    await page.click('#listo');
    await visible(page, 'pregunta');
    for (let i = 1; i <= n; i++) {
      await page.waitForFunction(([i, n]) => document.getElementById('numero').textContent === `Pregunta ${i} de ${n}`, [i, n], { timeout: 10000 });
      assert.equal(await page.locator('#hojas li').count(), n, 'un segmento por pregunta');
      assert.equal(await page.getAttribute('#progreso', 'aria-valuenow'), String(i - 1));
      assert.equal(await page.locator('#opciones .opcion').count(), 4, 'cuatro opciones');
      // El reloj muestra el tiempo del servidor (31 s por pregunta en el simulador), no el del navegador.
      const reloj = await page.textContent('#reloj');
      if (!(n === 10 && i === 10)) {
        const esperado = (i - 1) * PASO_MS;
        const [m, s] = reloj.split(':').map(Number);
        assert.ok(Math.abs(m * 60000 + s * 1000 - esperado) <= 2000, `reloj ${reloj} ≈ ${esperado} ms del servidor`);
      } else {
        assert.equal(await page.getAttribute('#temporizador', 'data-estado'), 'alerta', 'menos de un minuto: aviso visual');
      }
      if (n === 7 && i === 3) assert.equal(await page.locator('#opciones.con-imagen .opcion img').count(), 4, 'opciones con imágenes reales');
      else if (n === 7) assert.ok(await page.isVisible('#imagenPregunta'), 'imagen real de la pregunta');
      else assert.ok(await page.isVisible('#temaVisual'), 'ronda de diez sin imagen: tarjeta del tema con ECO');
      await page.evaluate(() => Promise.all([...document.images].filter((im) => !im.complete).map((im) => new Promise((ok) => { im.onload = im.onerror = ok; }))));
      await sinDesbordes(page, `pregunta ${i}`);
      await page.locator('#opciones .opcion').nth(1).click();
      assert.equal(await page.getAttribute('#opciones .opcion:nth-child(2)', 'aria-checked'), 'true');
      await cap(page, `p${i}`);
      await page.click('#confirmar');
    }

    await visible(page, 'resultado');
    assert.equal(await page.textContent('#puntaje'), String((n - 2) * 100));
    assert.equal(await page.textContent('#puntajeMaximo'), String(n * 100));
    assert.equal(await page.textContent('#aciertos'), `${n - 2} de ${n}`);
    assert.equal(await page.locator('#aciertosHojas li.bien').count(), n - 2);
    await page.waitForSelector('#miniClasificacion:not([hidden])', { timeout: 8000 });
    assert.equal(await page.locator('#miniPodio li').count(), 5, 'los cinco primeros');
    assert.equal(await page.locator('#miniPodio li.tu').count(), 1, 'se resalta al participante');
    await page.waitForTimeout(300);
    await sinDesbordes(page, 'resultado'); await cap(page, 'resultado', { fullPage: vista[0] >= 1024 });

    await page.goto(`${origen}/juego/clasificacion/`);
    await page.waitForFunction(() => document.querySelector('.puesto.p1 .alias').textContent === 'LunaVerde', null, { timeout: 8000 });
    assert.equal(await page.textContent('.puesto.p2 .alias'), 'Ana_QA');
    assert.equal(await page.textContent('.puesto.p3 .alias'), 'ReciclaYa');
    assert.equal(await page.locator('#siguientes li').count(), 2, 'cuarto y quinto');
    assert.equal(await page.getAttribute('.volver-web', 'href'), '../../');
    await page.waitForTimeout(900);
    await sinDesbordes(page, 'clasificación'); await cap(page, 'clasificacion', { fullPage: true });

    if (vista[0] >= 1024) {
      await page.goto(`${origen}/juego/clasificacion/?proyector`);
      await page.waitForFunction(() => document.body.classList.contains('proyector') && document.querySelector('.puesto.p1 .alias').textContent === 'LunaVerde', null, { timeout: 8000 });
      await page.waitForTimeout(900);
      assert.ok(await page.isHidden('.barra-plataforma'), 'el proyector no muestra controles');
      await sinDesbordes(page, 'proyector'); await cap(page, 'proyector');
    }
    assert.ok(!mock.s.llamadas.includes('/intento/revision'), 'sin revisión mientras el concurso sigue');
    assert.deepEqual(errores, [], 'sin errores de consola');
  } finally { await ctx.close(); }
}

for (const n of [7, 10]) {
  test(`ronda simulada de ${n} preguntas en ${VISTAS.length} tamaños`, { skip: OMITIR, timeout: 300000 }, async (t) => {
    for (const vista of VISTAS) await t.test(`${vista[0]}×${vista[1]}`, () => recorrer(vista, n));
  });
}

test('movimiento reducido: sin hojas cayendo ni animaciones largas', { skip: OMITIR }, async () => {
  const { ctx, page, errores } = await nuevaPagina([390, 844], 7, { reducedMotion: 'reduce' });
  try {
    await page.goto(`${origen}/juego/`);
    await visible(page, 'bienvenida');
    const dur = await page.evaluate(() => getComputedStyle(document.querySelector('.eco-centro')).animationDuration);
    assert.ok(parseFloat(dur) < 0.01, `animación anulada (${dur})`);
    assert.deepEqual(errores, []);
  } finally { await ctx.close(); }
});
