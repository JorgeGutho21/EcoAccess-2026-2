// Pruebas de /dos-clics/ en un navegador real (Playwright + Chrome del sistema). Mismo patrón que interfaz.test.mjs:
// servidor estático propio sobre web/ciudadano y, para el modo comandado, una pasarela ficticia (https://pasarela.prueba)
// interceptada con route: ninguna petición sale del equipo y nunca se toca la estación real ni el ESP32.
// Ejecutar: node --test web/ciudadano/pruebas/dos-clics.test.mjs
// Capturas: reportes/validacion_final/capturas_dos_clics/ (ECOACCESS_CAPTURAS=0 para omitirlas).
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile, mkdir } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { createRequire } from 'node:module';
import { join, dirname, extname, normalize } from 'node:path';
import { fileURLToPath } from 'node:url';

const aqui = dirname(fileURLToPath(import.meta.url));
const raizWeb = join(aqui, '..');
const carpetaCapturas = join(aqui, '..', '..', '..', 'reportes', 'validacion_final', 'capturas_dos_clics');
const capturar = process.env.ECOACCESS_CAPTURAS !== '0';
const modulos = process.env.ECOACCESS_PLAYWRIGHT || 'C:/Users/Usuario/AppData/Local/EcoAccessDevTools/node_modules';
const chrome = process.env.ECOACCESS_CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
let chromium = null;
try { chromium = createRequire(join(modulos, 'x.js'))('playwright').chromium; } catch { /* sin Playwright: se omite */ }
const disponible = Boolean(chromium) && existsSync(chrome);

const TIPOS = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.webp': 'image/webp', '.png': 'image/png', '.webmanifest': 'application/manifest+json' };
let servidor, base, navegador;
const PERMISO = 'b'.repeat(43);

before(async () => {
  if (!disponible) return;
  servidor = createServer(async (req, res) => {
    let ruta = normalize(decodeURIComponent(new URL(req.url, 'http://x').pathname)).replace(/^([/\\])+/, '');
    if (!ruta || /[/\\]$/.test(ruta)) ruta += 'index.html';
    try { const d = await readFile(join(raizWeb, ruta)); res.writeHead(200, { 'Content-Type': TIPOS[extname(ruta)] || 'application/octet-stream' }); res.end(d); }
    catch { res.writeHead(404); res.end(); }
  });
  await new Promise((ok) => servidor.listen(0, '127.0.0.1', ok));
  base = `http://127.0.0.1:${servidor.address().port}/`;
  navegador = await chromium.launch({ executablePath: chrome });
  if (capturar) await mkdir(carpetaCapturas, { recursive: true });
});
after(async () => { await navegador?.close(); servidor?.close(); });

/**
 * Página de /dos-clics/. Opciones:
 *  - pasarela: 'disponible' | 'desconectada' | null → intercepta https://pasarela.prueba y abre con el QR (#e=…&t=…)
 *  - version: false → sin version.json (404), true → version.json ficticio
 */
async function pagina({ ancho = 390, alto = 844, consulta = '', pasarela = null, version = true, init, plan } = {}) {
  const ctx = await navegador.newContext({ viewport: { width: ancho, height: alto }, serviceWorkers: 'block' });
  await ctx.addInitScript(() => { try { localStorage.setItem('ecoaccess.voz', 'no'); } catch { /* nada */ } });
  if (init) await ctx.addInitScript(init);
  if (version) await ctx.route('**/version.json', (r) => r.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ version: 'prueba-2026.11.25', commit: 'abc1234' }) }));
  const pedidos = [];
  if (pasarela) {
    await ctx.route('https://pasarela.prueba/**', async (ruta) => {
      const r = ruta.request(); const u = new URL(r.url());
      const cors = { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Headers': 'Authorization, Content-Type', 'Access-Control-Allow-Methods': 'GET, POST' };
      const json = (o, status = 200) => ruta.fulfill({ status, contentType: 'application/json', headers: cors, body: JSON.stringify(o) });
      if (r.method() === 'OPTIONS') return ruta.fulfill({ status: 204, headers: cors });
      if (u.pathname === '/api/v1/estado') {
        return pasarela === 'disponible' ? json({ estado: 'disponible', texto: 'Estación disponible.', politica: { una: 7000, dos: 14000, tres: 21000 } })
          : json({ ok: false, error: 'La estación está desconectada.' }, 503);
      }
      if (u.pathname === '/api/v1/aperturas' && r.method() === 'POST') {
        const c = r.postDataJSON(); pedidos.push({ ...c, autorizacion: r.headers().authorization });
        const letras = c.canecas.split('');
        return json({ ok: true, clave: c.clave, modo: 'grupo', movimientoMs: 400, estadoFisico: 'comandado', estadoRegistro: 'PENDIENTE',
          plan: plan ?? letras.map((l, i) => ({ caneca: l, inicioMs: i * 250, abiertaMs: [7000, 14000, 21000][letras.length - 1] })) });
      }
      return json({ ok: false }, 404);
    });
  }
  const p = await ctx.newPage();
  p.pedidos = pedidos;
  p.errores = [];
  p.on('pageerror', (e) => p.errores.push('pageerror: ' + e.message));
  p.on('console', (m) => {
    if (m.type() !== 'error') return;
    const origen = m.location()?.url || '';
    if (!version && /version\.json$/.test(origen)) return;   // 404 esperado: el workflow aún no lo generó
    if (origen.startsWith('https://pasarela.prueba/') && /Failed to load resource/.test(m.text())) return;   // 409/503 de la pasarela ficticia: casos probados a propósito
    p.errores.push('console: ' + m.text());
  });
  const destino = base + 'dos-clics/' + consulta + (pasarela ? `#e=https://pasarela.prueba&t=${PERMISO}` : '');
  await p.goto(destino, { waitUntil: 'networkidle' });
  await p.waitForFunction(() => window.__dosClics?.listo);
  if (pasarela === 'disponible') await p.waitForFunction(() => document.getElementById('estacionTexto').textContent === 'Estación conectada');
  if (pasarela === 'desconectada') await p.waitForFunction(() => document.getElementById('estacionTexto').textContent === 'Desconectado');
  return p;
}
const elegidas = (p) => p.$$eval('.caneca.elegida', (l) => l.map((n) => n.dataset.caneca));
const atenuadas = (p) => p.$$eval('.caneca.atenuada', (l) => l.map((n) => n.dataset.caneca));
const selloCaneca = (p) => p.$eval('.caneca.elegida .sello', (s) => getComputedStyle(s, '::after').content.replace(/"/g, ''));
const desborde = (p) => p.evaluate(() => document.documentElement.scrollWidth - innerWidth);
async function cerrar(p) { const e = p.errores; await p.context().close(); assert.deepEqual(e, [], 'sin errores de consola ni de página'); }
async function foto(p, nombre) { if (capturar) await p.screenshot({ path: join(carpetaCapturas, nombre) }); }

/** Reconocimiento de voz simulado: emite un parcial (que por sí solo sería otra caneca) y luego la frase final. */
const vozFalsa = ({ parcial, final, error, idiomaNoSoportado = false }) => `
  window.__voz = { idiomas: [], eventos: [] };
  window.SpeechRecognition = class {
    start() {
      window.__voz.idiomas.push(this.lang);
      const r = (t, fin) => { const a = [{ transcript: t, confidence: .9 }]; a.isFinal = fin; return { resultIndex: 0, results: [a] }; };
      if (${idiomaNoSoportado} && this.lang === 'es-CO') { setTimeout(() => this.onerror?.({ error: 'language-not-supported' }), 30); setTimeout(() => this.onend?.(), 60); return; }
      ${error ? `setTimeout(() => this.onerror?.({ error: ${JSON.stringify(error)} }), 80); setTimeout(() => this.onend?.(), 120);` : `
      setTimeout(() => { window.__voz.eventos.push('parcial'); this.onresult?.(r(${JSON.stringify(parcial)}, false)); }, 100);
      setTimeout(() => { window.__voz.eventos.push('final'); this.onresult?.(r(${JSON.stringify(final)}, true)); }, 900);
      setTimeout(() => this.onend?.(), 1000);`}
    }
    stop() { setTimeout(() => this.onend?.(), 20); }
  };`;

test('dos clics', { skip: !disponible && 'Sin Playwright o sin Chrome: omitida' }, async (t) => {
  await t.test('pantalla inicial: logotipo, ECO, tres canecas por capas, campo, micrófono, estación, historial y enlace', async () => {
    const p = await pagina();
    assert.equal(await p.textContent('h1'), '¿Qué residuo vas a desechar?');
    assert.equal(await p.textContent('.subtitulo'), 'Selecciona una caneca, escribe o dinos qué residuo quieres desechar.');
    assert.equal(await p.getAttribute('.logo', 'alt'), 'EcoAccess');
    assert.match(await p.getAttribute('#ecoImagen', 'src'), /estacion\/eco\.webp$/);
    const capas = await p.$$eval('.caneca', (l) => l.map((c) => c.querySelector('.arte > img.cuerpo')?.getAttribute('src')));
    assert.deepEqual(capas, ['verde', 'blanca', 'negra'].map((c) => `../assets/estacion/caneca-${c}.webp`));
    assert.ok(await p.isVisible('#residuo')); assert.ok(await p.isVisible('#microfono'));
    assert.equal(await p.isVisible('#estacion'), false, 'sin sesión no se muestra pastilla de estado');
    assert.equal(await p.isVisible('#avisoLinea'), false, 'sin rótulo «Vista ilustrativa»');
    assert.doesNotMatch(await p.textContent('body'), /vista ilustrativa|sin movimiento físico/i);
    assert.match(await p.textContent('#abrirHistorial'), /Mi historial/);
    assert.equal(await p.getAttribute('.enlace-pie', 'href'), '../');
    assert.match(await p.textContent('.enlace-pie'), /Conoce ECOACCESS/);
    assert.equal(await p.textContent('#version'), 'Versión prueba-2026.11.25');
    assert.equal(await p.isVisible('#version'), true);
    // Sin contenido académico.
    assert.doesNotMatch(await p.textContent('body'), /UML|Programación III|arquitectura|grafos/i);
    await foto(p, 'celular_390x844_inicio.png');
    await cerrar(p);
  });

  await t.test('sin version.json no se muestra ningún identificador', async () => {
    const p = await pagina({ version: false });
    assert.equal(await p.isVisible('#version'), false);
    await cerrar(p);
  });

  await t.test('manual: un solo toque en la caneca la abre (sin segundo toque ni botón de confirmación)', async () => {
    const p = await pagina();
    assert.equal(await p.$('#abrir'), null, 'ya no existe el botón «Abrir caneca X»');
    assert.equal(await p.getAttribute('.caneca[data-caneca="negra"]', 'aria-label'), 'Abrir caneca negra, no aprovechables');
    await p.click('.caneca[data-caneca="negra"]');
    await p.waitForSelector('.caneca[data-caneca="negra"].abierta', { timeout: 2000 });
    assert.equal(await p.getAttribute('body', 'data-fase'), 'operacion');
    assert.deepEqual(await elegidas(p), ['negra']);
    assert.deepEqual(await atenuadas(p), ['verde', 'blanca']);
    assert.equal(await selloCaneca(p), 'none');
    await cerrar(p);

    const q = await pagina({ consulta: '?demo' });
    await q.click('.caneca[data-caneca="blanca"]');
    await q.waitForSelector('.caneca[data-caneca="blanca"].abierta', { timeout: 2000 });
    assert.equal(await selloCaneca(q), 'Simulación');
    assert.equal(await q.textContent('#opSello'), 'Simulación');
    assert.equal(await q.textContent('#estacionTexto'), 'Simulación');
    await cerrar(q);
  });

  const casosTexto = [['cáscara de banano', ['verde']], ['media manzana', ['verde']], ['papel aluminio', ['blanca']], ['servilleta usada', ['negra']]];
  for (const [texto, esperado] of casosTexto) {
    await t.test(`texto: «${texto}» → ${esperado.join(', ')} y abre sin confirmaciones extra`, async () => {
      const p = await pagina();
      await p.fill('#residuo', texto); await p.press('#residuo', 'Enter');
      await p.waitForSelector(`.caneca[data-caneca="${esperado[0]}"].abierta`, { timeout: 2000 });
      assert.deepEqual(await elegidas(p), esperado);
      assert.equal(await p.isVisible('#aclaracion'), false);
      await cerrar(p);
    });
  }

  await t.test('texto ambiguo: pregunta breve con las opciones del motor y abre la elegida', async () => {
    const p = await pagina();
    await p.fill('#residuo', 'papel'); await p.press('#residuo', 'Enter');
    await p.waitForSelector('#aclaracion:not([hidden])');
    assert.equal(await p.$('.caneca.elegida'), null, 'no abre nada mientras sea ambiguo');
    const opciones = await p.$$eval('#opciones button', (l) => l.map((b) => ({ texto: b.textContent, caneca: b.dataset.caneca || null })));
    assert.ok(opciones.length >= 2, JSON.stringify(opciones));
    const i = opciones.findIndex((o) => o.caneca);
    await p.click(`#opciones button >> nth=${i}`);
    await p.waitForSelector(`.caneca[data-caneca="${opciones[i].caneca}"].abierta`, { timeout: 2000 });
    await cerrar(p);
  });

  await t.test('texto con sesión de estación válida: una sola orden, sin confirmaciones', async () => {
    const p = await pagina({ pasarela: 'disponible' });
    assert.equal(new URL(p.url()).hash, '', 'el permiso del QR se borra de la URL');
    await p.fill('#residuo', 'cáscara de banano'); await p.press('#residuo', 'Enter');
    await p.waitForSelector('.operacion[data-etapa="deposito"]', { timeout: 3000 });
    assert.equal(p.pedidos.length, 1);
    assert.equal(p.pedidos[0].canecas, 'V');
    assert.equal(p.pedidos[0].metodo, 'TEXTO');
    assert.equal(p.pedidos[0].autorizacion, 'Bearer ' + PERMISO);
    assert.deepEqual(p.pedidos[0].materiales.map((m) => m.caneca), ['verde']);
    await cerrar(p);
  });

  await t.test('voz simulada: el parcial no dispara nada; solo la frase final abre la caneca (es-CO)', async () => {
    const p = await pagina({ init: vozFalsa({ parcial: 'botella', final: 'cáscara de banano' }) });
    await p.evaluate(() => {
      window.__vistas = new Set();
      new MutationObserver(() => document.querySelectorAll('.caneca.elegida').forEach((n) => window.__vistas.add(n.dataset.caneca)))
        .observe(document.getElementById('canecas'), { subtree: true, attributes: true, attributeFilter: ['class'] });
    });
    await p.click('#microfono');
    await p.waitForFunction(() => window.__voz.eventos.includes('parcial'));
    await p.waitForTimeout(300);
    assert.equal(await p.textContent('#escuchaTexto'), '«botella»', 'el parcial solo se muestra');
    assert.equal(await p.$('.caneca.elegida'), null, 'el parcial NO selecciona ni abre');
    assert.equal(await p.getAttribute('body', 'data-fase'), 'inicio');
    await p.waitForSelector('.caneca[data-caneca="verde"].abierta', { timeout: 3000 });
    assert.deepEqual(await p.evaluate(() => [...window.__vistas]), ['verde'], 'la blanca (parcial «botella») nunca se eligió');
    assert.deepEqual(await p.evaluate(() => window.__voz.idiomas), ['es-CO']);
    assert.equal(await p.inputValue('#residuo'), 'cáscara de banano');
    await cerrar(p);
  });

  await t.test('voz: sin es-CO usa es-ES', async () => {
    const p = await pagina({ init: vozFalsa({ parcial: 'servi', final: 'servilleta usada', idiomaNoSoportado: true }) });
    await p.click('#microfono');
    await p.waitForSelector('.caneca[data-caneca="negra"].abierta', { timeout: 3000 });
    assert.deepEqual(await p.evaluate(() => window.__voz.idiomas), ['es-CO', 'es-ES']);
    await cerrar(p);
  });

  await t.test('voz: permiso denegado ofrece la alternativa escrita y no abre nada', async () => {
    const p = await pagina({ init: vozFalsa({ error: 'not-allowed' }) });
    await p.click('#microfono');
    await p.waitForFunction(() => /permiso/.test(document.getElementById('mensaje').textContent));
    assert.match(await p.textContent('#mensaje'), /Escribe tu residuo/);
    assert.equal(await p.evaluate(() => document.activeElement.id), 'residuo', 'el foco pasa al campo de texto');
    await p.waitForFunction(() => document.getElementById('microfono').getAttribute('aria-pressed') === 'false');
    assert.equal(await p.$('.caneca.elegida'), null);
    await cerrar(p);
  });

  await t.test('modo ilustrativo sin sesión: rotulado, sin pedir nada a ninguna pasarela', async () => {
    const p = await pagina();
    const externas = []; p.on('request', (r) => { if (!r.url().startsWith(base)) externas.push(r.url()); });
    await p.click('.caneca[data-caneca="blanca"]'); await p.click('.caneca[data-caneca="blanca"]');
    await p.waitForSelector('.operacion[data-etapa="deposito"]', { timeout: 3000 });
    assert.equal(await p.getAttribute('#canecas', 'data-sello'), null);
    assert.equal(await p.isVisible('#opSello'), false);
    assert.doesNotMatch(await p.textContent('body'), /vista ilustrativa|sin movimiento físico/i);
    assert.doesNotMatch(await p.textContent('body'), /orden enviada|tapa se abrió|se abrió la tapa/i);
    assert.deepEqual(externas, []);
    await cerrar(p);
  });

  await t.test('estación desconectada: sello «Desconectado» y ninguna orden', async () => {
    const p = await pagina({ pasarela: 'desconectada' });
    await p.click('.caneca[data-caneca="verde"]');
    await p.waitForSelector('.caneca[data-caneca="verde"].abierta', { timeout: 3000 });
    assert.equal(p.pedidos.length, 0);
    assert.equal(await selloCaneca(p), 'Desconectado');
    assert.equal(await p.textContent('#opSello'), 'Desconectado');
    await cerrar(p);
  });

  await t.test('modo comandado (pasarela ficticia): orden única, cuenta regresiva de 7 s, cierre del controlador y registro', async () => {
    const p = await pagina({ pasarela: 'disponible' });
    await p.dblclick('.caneca[data-caneca="verde"]');   // un toque abre; el toque de más se ignora: solo una orden
    await p.waitForSelector('.operacion[data-etapa="deposito"]', { timeout: 3000 });
    assert.equal(p.pedidos.length, 1, 'exclusión mutua e idempotencia: una sola orden');
    assert.equal(p.pedidos[0].canecas, 'V');
    assert.match(p.pedidos[0].clave, /^[A-Za-z0-9]{16,64}$/);
    assert.deepEqual(p.pedidos[0].materiales, []);
    assert.equal(await p.textContent('#segundos'), '7', 'la cuenta empieza en 7 s (abiertaMs del plan)');
    assert.match(await p.textContent('#anuncioCuenta'), /7 segundos/);
    assert.equal(await p.textContent('#opTitulo'), 'Deposita tu residuo ahora');
    assert.equal(await p.getAttribute('#canecas', 'data-sello'), 'comandada');
    assert.equal(await selloCaneca(p), 'Orden enviada');
    assert.ok(await p.$('.caneca[data-caneca="verde"].abierta'));
    await p.waitForTimeout(1200);
    await foto(p, 'celular_390x844_apertura_cuenta.png');
    const s = Number(await p.textContent('#segundos'));
    assert.ok(s >= 4 && s <= 6, 'la cuenta avanza: ' + s);
    assert.doesNotMatch(await p.textContent('body'), /tapa se abrió|se abrió la tapa|tapa abierta/i);
    await p.waitForSelector('.operacion[data-etapa="cierre"]', { timeout: 8000 });
    assert.match(await p.textContent('#opDetalle'), /controlador de la estación/);
    await p.waitForSelector('.operacion[data-etapa="registro"]', { timeout: 2000 });
    assert.match(await p.textContent('#opDetalle'), /Registro enviado a la estación/);
    assert.equal(await p.$('.caneca.abierta'), null);
    assert.equal(p.pedidos.length, 1);
    await p.waitForSelector('body[data-fase="inicio"]', { timeout: 9000 });   // vuelve solo al inicio
    await p.click('#abrirHistorial');
    assert.match(await p.textContent('#histLista'), /Orden enviada/);
    await cerrar(p);
  });

  await t.test('modo comandado: error de la pasarela se informa y no se afirma apertura', async () => {
    const ctxErr = await pagina({ pasarela: 'disponible', plan: undefined });
    await ctxErr.context().route('https://pasarela.prueba/api/v1/aperturas', (r) => r.fulfill({ status: 409, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Headers': 'Authorization, Content-Type' }, body: JSON.stringify({ ok: false, codigo: 'ocupada', error: 'Hay una caneca en movimiento. Espera un momento.' }) }));
    await ctxErr.click('.caneca[data-caneca="negra"]');
    await ctxErr.waitForSelector('.operacion[data-etapa="error"]', { timeout: 3000 });
    assert.equal(await ctxErr.textContent('#opTitulo'), 'La caneca no se abrió');
    assert.match(await ctxErr.textContent('#opDetalle'), /Hay una caneca en movimiento/);
    assert.equal(await ctxErr.$('.caneca.abierta'), null);
    await cerrar(ctxErr);
  });

  const tamanos = [[320, 568], [360, 800], [390, 844], [430, 932], [768, 1024], [1024, 768], [1366, 768], [1920, 1080]];
  for (const [ancho, alto] of tamanos) {
    await t.test(`${ancho}×${alto}: sin desborde; canecas, campo y micrófono visibles sin desplazarse`, async () => {
      const p = await pagina({ ancho, alto });
      assert.equal(await desborde(p), 0, 'sin desborde horizontal en el inicio');
      const visibles = await p.evaluate(() => {
        const dentro = (n) => { const r = n.getBoundingClientRect(); return r.width > 40 && r.top >= 0 && r.bottom <= innerHeight + 1 && r.left >= 0 && r.right <= innerWidth + 1; };
        return { canecas: [...document.querySelectorAll('.caneca .arte')].every(dentro), campo: dentro(document.getElementById('residuo')), mic: dentro(document.getElementById('microfono')), titulo: dentro(document.querySelector('h1')) };
      });
      assert.deepEqual(visibles, { canecas: true, campo: true, mic: true, titulo: true });
      if (ancho === 768) await foto(p, 'tablet_768x1024_inicio.png');
      if (ancho === 1366) await foto(p, 'pc_1366x768_inicio.png');
      await p.click('.caneca[data-caneca="blanca"]');
      await p.waitForSelector('.operacion[data-etapa="deposito"]', { timeout: 3000 });
      await p.waitForTimeout(700);
      assert.equal(await desborde(p), 0, 'sin desborde con la tapa abierta y la cuenta regresiva');
      if (ancho === 1366) await foto(p, 'pc_1366x768_apertura.png');
      if (ancho === 1920) await foto(p, 'pc_1920x1080_apertura.png');
      await cerrar(p);
    });
  }

  await t.test('accesibilidad básica: regiones aria-live, foco visible y movimiento reducido', async () => {
    const p = await pagina();
    assert.equal(await p.getAttribute('#anuncio', 'aria-live'), 'polite');
    assert.equal(await p.getAttribute('#anuncioCuenta', 'aria-live'), 'polite');
    await p.keyboard.press('Tab'); await p.keyboard.press('Tab'); await p.keyboard.press('Tab');
    const foco = await p.evaluate(() => { const n = document.activeElement; return { tag: n.tagName, outline: getComputedStyle(n).outlineStyle }; });
    assert.notEqual(foco.outline, 'none', 'foco visible en ' + foco.tag);
    await p.emulateMedia({ reducedMotion: 'reduce' });
    assert.equal(await p.$eval('.reloj svg', (s) => getComputedStyle(s).display), 'none');
    await p.click('.caneca[data-caneca="verde"]');
    await p.waitForFunction(() => /Caneca verde seleccionada/.test(document.getElementById('anuncio').textContent));
    await cerrar(p);
  });
});
