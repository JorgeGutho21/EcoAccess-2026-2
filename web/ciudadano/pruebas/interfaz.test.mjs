// Pruebas de la interfaz ciudadana en un navegador real (Playwright + Chrome del sistema). No toca la estación real:
// sirve web/ciudadano con un servidor estático propio y, para el modo físico, intercepta una pasarela ficticia
// (https://pasarela.prueba) con page.route, de modo que ninguna petición sale del equipo.
// Ejecutar: node --test web/ciudadano/pruebas/interfaz.test.mjs
// Playwright se toma de ECOACCESS_PLAYWRIGHT (carpeta node_modules) o de la carpeta de herramientas de desarrollo.
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { existsSync } from 'node:fs';
import { createRequire } from 'node:module';
import { join, dirname, extname, normalize } from 'node:path';
import { fileURLToPath } from 'node:url';

const raizWeb = join(dirname(fileURLToPath(import.meta.url)), '..');
const modulos = process.env.ECOACCESS_PLAYWRIGHT || 'C:/Users/Usuario/AppData/Local/EcoAccessDevTools/node_modules';
const chrome = process.env.ECOACCESS_CHROME || 'C:/Program Files/Google/Chrome/Application/chrome.exe';
let chromium = null;
try { chromium = createRequire(join(modulos, 'x.js'))('playwright').chromium; } catch { /* sin Playwright: se omite */ }
const disponible = Boolean(chromium) && existsSync(chrome);

const TIPOS = { '.html': 'text/html; charset=utf-8', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json', '.webp': 'image/webp', '.png': 'image/png', '.webmanifest': 'application/manifest+json' };
let servidor, base, navegador;

before(async () => {
  if (!disponible) return;
  servidor = createServer(async (req, res) => {
    const ruta = normalize(decodeURIComponent(new URL(req.url, 'http://x').pathname)).replace(/^([/\\])+/, '') || 'index.html';
    try { const d = await readFile(join(raizWeb, ruta)); res.writeHead(200, { 'Content-Type': TIPOS[extname(ruta)] || 'application/octet-stream' }); res.end(d); }
    catch { res.writeHead(404); res.end(); }
  });
  await new Promise((ok) => servidor.listen(0, '127.0.0.1', ok));
  base = `http://127.0.0.1:${servidor.address().port}/`;
  navegador = await chromium.launch({ executablePath: chrome });
});
after(async () => { await navegador?.close(); servidor?.close(); });

/** Reconocimiento de voz simulado: emite un parcial y luego (si «final») la frase completa. */
const vozFalsa = (frase, { final = true } = {}) => `
  window.__parcialesActuados = 0;
  window.SpeechRecognition = class {
    start() {
      const r = (t, fin) => { const a = [{ transcript: t, confidence: .9 }]; a.isFinal = fin; return { resultIndex: 0, results: [a] }; };
      setTimeout(() => this.onresult?.(r(${JSON.stringify(frase)}.split(' ').slice(0, 2).join(' '), false)), 120);
      ${final ? `setTimeout(() => this.onresult?.(r(${JSON.stringify(frase)}, true)), 350);` : `setTimeout(() => this.onerror?.({ error: 'no-speech' }), 350);`}
      setTimeout(() => this.onend?.(), 450);
    }
    stop() { setTimeout(() => this.onend?.(), 20); }
  };`;

async function pagina({ ancho = 390, alto = 844, url = '?demo', init } = {}) {
  const ctx = await navegador.newContext({ viewport: { width: ancho, height: alto }, serviceWorkers: 'block' });
  await ctx.addInitScript(() => { try { localStorage.setItem('ecoaccess.voz', 'no'); } catch { /* nada */ } });
  if (init) await ctx.addInitScript(init);
  const p = await ctx.newPage();
  p.errores = []; p.on('pageerror', (e) => p.errores.push(e.message));
  await p.goto(base + url, { waitUntil: 'networkidle' });
  await p.waitForFunction(() => window.__ecoaccess?.motor);
  return p;
}
const elegidas = (p) => p.$$eval('.caneca.elegida', (l) => l.map((n) => n.dataset.caneca));
const cerrar = (p) => p.context().close();

test('interfaz ciudadana', { skip: !disponible && 'Sin Playwright o sin Chrome: omitida' }, async (t) => {
  await t.test('flujo de texto: escribir + Enter identifica, resalta y abre la caneca (demostración)', async () => {
    const p = await pagina();
    await p.fill('#residuo', 'botella de plástico'); await p.press('#residuo', 'Enter');
    await p.waitForSelector('.caneca[data-caneca="blanca"].abierta', { timeout: 3000 });
    assert.deepEqual(await elegidas(p), ['blanca']);
    assert.equal(await p.getAttribute('#principal', 'data-fase'), 'resultado');
    assert.match(await p.textContent('#resChip'), /Caneca blanca/);
    assert.equal(await p.textContent('#ecoEstado'), 'Caneca seleccionada');
    assert.match(await p.textContent('#modo'), /sin movimiento físico/);
    assert.deepEqual(p.errores, []);
    await cerrar(p);
  });

  const casosVoz = [
    ['Voy a desechar una manzana', ['verde']], ['Tengo una botella de plástico', ['blanca']], ['Quiero botar una servilleta', ['negra']],
    ['Cáscara de guineo', ['verde']], ['Tengo una papa y una botella', ['verde', 'blanca']],
  ];
  for (const [frase, esperado] of casosVoz) {
    await t.test(`voz simulada: «${frase}» → ${esperado.join(' + ')}`, async () => {
      const p = await pagina({ init: vozFalsa(frase) });
      await p.click('#microfono');
      assert.equal(await p.getAttribute('#microfono', 'aria-pressed'), 'true');
      assert.equal(await p.textContent('#ecoEstado'), 'Escuchando');
      await p.waitForFunction(() => document.querySelector('#principal').dataset.fase === 'resultado', null, { timeout: 3000 });
      assert.deepEqual(await elegidas(p), esperado);
      assert.equal(await p.inputValue('#residuo'), frase);
      await cerrar(p);
    });
  }

  await t.test('voz: nunca actúa con una transcripción parcial (sin resultado final)', async () => {
    const p = await pagina({ init: vozFalsa('Tengo una botella de plástico', { final: false }) });
    await p.click('#microfono');
    await p.waitForTimeout(900);
    assert.equal(await p.getAttribute('#principal', 'data-fase'), 'inicio');
    assert.equal(await p.textContent('#ecoEstado'), 'No comprendido');
    assert.equal(await p.evaluate(() => JSON.parse(localStorage.getItem('ecoaccess.historial.v1') || '[]').length), 0);
    await cerrar(p);
  });

  await t.test('voz: cancelar descarta la frase', async () => {
    const p = await pagina({ init: vozFalsa('Tengo una botella de plástico').replace('350);', '700);') });
    await p.click('#microfono'); await p.waitForTimeout(200); await p.click('#detenerVoz');
    await p.waitForTimeout(900);
    assert.equal(await p.getAttribute('#principal', 'data-fase'), 'inicio');
    await cerrar(p);
  });

  await t.test('ambigüedad: «papel» pide una aclaración breve con opciones y no abre nada', async () => {
    const p = await pagina();
    await p.fill('#residuo', 'papel'); await p.click('#identificar');
    await p.waitForSelector('#resOpciones button');
    assert.equal(await p.getAttribute('#principal', 'data-fase'), 'aviso');
    assert.ok((await p.$$('#resOpciones button')).length >= 2);
    assert.equal(await p.$$eval('.caneca.abierta', (l) => l.length), 0);
    await p.click('#resOpciones button[data-caneca="blanca"]');
    await p.waitForSelector('.caneca[data-caneca="blanca"].elegida');
    await cerrar(p);
  });

  for (const peligroso of ['Quiero desechar una pila', 'jeringa', 'medicamentos vencidos']) {
    await t.test(`peligroso / gestión especial: «${peligroso}» no abre ninguna caneca`, async () => {
      const p = await pagina({ init: vozFalsa(peligroso) });
      await p.click('#microfono');
      await p.waitForFunction(() => document.querySelector('#principal').dataset.fase !== 'inicio', null, { timeout: 3000 });
      await p.waitForTimeout(1500);
      assert.equal(await p.$$eval('.caneca.abierta', (l) => l.length), 0);
      assert.equal(await p.getAttribute('#resChip', 'data-caneca'), 'especial');
      await cerrar(p);
    });
  }

  for (const [residuo, segundos] of [['cáscara de banano', 7], ['cáscara de banano y botella de plástico', 14], ['cáscara de banano, botella de plástico y servilleta usada', 21]]) {
    await t.test(`demostración: ${segundos} s para ${segundos === 7 ? 'una caneca' : segundos === 14 ? 'dos canecas' : 'tres canecas'}`, async () => {
      const p = await pagina();
      await p.fill('#residuo', residuo); await p.click('#identificar');
      await p.waitForSelector('#resCuenta:not([hidden])', { timeout: 4000 });
      assert.equal(Number(await p.textContent('#segundos')), segundos);
      assert.match(await p.textContent('#segundosSr'), new RegExp(`Tienes ${segundos} segundos`));
      assert.equal((await elegidas(p)).length, segundos === 7 ? 1 : segundos === 14 ? 2 : 3);
      await cerrar(p);
    });
  }

  await t.test('modo físico: una sola orden, la cuenta sigue al plan de la pasarela y recargar no repite la apertura', async () => {
    const ctx = await navegador.newContext({ viewport: { width: 390, height: 844 }, serviceWorkers: 'block' });
    await ctx.addInitScript(() => { try { localStorage.setItem('ecoaccess.voz', 'no'); } catch { /* nada */ } });
    const pedidos = [];
    await ctx.route('https://pasarela.prueba/**', async (ruta) => {
      const r = ruta.request(); const u = new URL(r.url());
      const json = (o) => ruta.fulfill({ status: 200, contentType: 'application/json', headers: { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Headers': 'Authorization, Content-Type' }, body: JSON.stringify(o) });
      if (r.method() === 'OPTIONS') return ruta.fulfill({ status: 204, headers: { 'Access-Control-Allow-Origin': '*', 'Access-Control-Allow-Headers': 'Authorization, Content-Type', 'Access-Control-Allow-Methods': 'GET, POST' } });
      if (u.pathname === '/api/v1/estado') return json({ estado: 'disponible', texto: 'Estación disponible' });
      if (u.pathname === '/api/v1/aperturas') {
        const c = r.postDataJSON(); pedidos.push(c);
        return json({ ok: true, clave: c.clave, modo: 'grupo', movimientoMs: 400, estadoFisico: 'comandado',
          plan: [{ caneca: 'V', inicioMs: 0, abiertaMs: 7000 }, { caneca: 'B', inicioMs: 250, abiertaMs: 7000 }] });
      }
      return ruta.fulfill({ status: 404, body: '{}' });
    });
    const p = await ctx.newPage();
    await p.goto(base + '#e=https://pasarela.prueba&t=' + 'a'.repeat(43), { waitUntil: 'networkidle' });
    await p.waitForFunction(() => window.__ecoaccess?.motor);
    assert.equal(new URL(p.url()).hash, '', 'el fragmento con el permiso se borra de la URL');
    await p.waitForFunction(() => document.querySelector('#estadoTexto').textContent === 'Estación disponible');
    await p.fill('#residuo', 'cáscara de banano y botella de plástico'); await p.click('#identificar');
    await p.waitForSelector('#resCuenta:not([hidden])', { timeout: 4000 });
    assert.equal(pedidos.length, 1);
    assert.equal(pedidos[0].canecas, 'VB');
    assert.ok(Number(await p.textContent('#segundos')) >= 7, 'la cuenta usa abiertaMs del plan (7 s) más el escalonamiento');
    assert.match(await p.textContent('#resPasos'), /Apertura comandada/);
    assert.doesNotMatch(await p.textContent('body'), /tapa se abrió|movimiento verificado/i);
    await p.reload({ waitUntil: 'networkidle' });
    await p.waitForTimeout(1200);
    assert.equal(pedidos.length, 1, 'recargar no repite la orden');
    assert.equal(await p.getAttribute('#principal', 'data-fase'), 'inicio');
    assert.match(await p.textContent('#modo'), /vinculada/);
    await ctx.close();
  });

  for (const [ancho, alto] of [[320, 568], [360, 800], [390, 844], [430, 932]]) {
    await t.test(`${ancho}×${alto}: sin desborde horizontal y las tres canecas visibles sobre el dock`, async () => {
      const p = await pagina({ ancho, alto, url: '' });
      assert.equal(await p.evaluate(() => document.documentElement.scrollWidth - innerWidth), 0);
      const visibles = await p.evaluate(() => {
        const dock = document.getElementById('dock').getBoundingClientRect().top;
        return [...document.querySelectorAll('.caneca .arte')].every((a) => { const r = a.getBoundingClientRect(); return r.top >= 0 && r.bottom <= dock + 1; });
      });
      assert.ok(visibles, 'las tres canecas completas entran entre la cabecera y el dock');
      await p.fill('#residuo', 'servilleta usada'); await p.click('#identificar');
      await p.waitForTimeout(1200);
      assert.equal(await p.evaluate(() => document.documentElement.scrollWidth - innerWidth), 0);
      await cerrar(p);
    });
  }

  for (const [ancho, alto] of [[1024, 768], [1366, 768], [1920, 1080]]) {
    await t.test(`${ancho}×${alto}: el resultado de tres residuos cabe en la pantalla, con concordancia y titular en frase`, async () => {
      const p = await pagina({ ancho, alto });
      await p.fill('#residuo', 'cáscara de banano, botella de plástico y servilleta usada'); await p.click('#identificar');
      await p.waitForSelector('#resCuenta:not([hidden])', { timeout: 4000 });
      const medidas = await p.evaluate(() => {
        const abajo = (s) => document.querySelector(s).getBoundingClientRect().bottom;
        return { escenario: abajo('.escenario'), rotulos: Math.max(...[...document.querySelectorAll('.caneca figcaption')].map((f) => f.getBoundingClientRect().bottom)),
          resultado: abajo('#resultado'), alto: innerHeight, scroll: scrollY };
      });
      assert.equal(medidas.scroll, 0);
      assert.ok(medidas.rotulos <= medidas.alto && medidas.escenario <= medidas.alto && medidas.resultado <= medidas.alto, JSON.stringify(medidas));
      assert.equal(await p.textContent('#resMaterial'), 'Cáscara de banano, botella plástica PET y servilleta usada');
      assert.match(await p.textContent('#ecoMensaje'), /segundos para depositarlos\./);
      assert.match(await p.textContent('#resPasos'), /Deposita tus residuos/);
      await cerrar(p);
    });
  }

  await t.test('historial: distingue consulta educativa y demostración, con miniatura y estado de sincronización', async () => {
    const p = await pagina();
    await p.fill('#residuo', 'lata de gaseosa'); await p.click('#identificar');
    await p.waitForTimeout(400);
    await p.click('#nuevaConsulta');
    await p.click('#abrirHistorial');
    assert.equal(await p.$$eval('#histLista .registro', (l) => l.length), 0, 'la demostración se registra al completar');
    await p.keyboard.press('Escape');
    await p.fill('#residuo', 'cáscara de papa'); await p.click('#identificar');
    await p.waitForTimeout(10500);
    await p.click('#abrirHistorial');
    const texto = await p.textContent('#histLista');
    assert.match(texto, /Demostración, sin movimiento/);
    assert.match(texto, /Solo en este dispositivo/);
    assert.ok(await p.$('#histLista .mini img'), 'miniatura de la caneca');
    await p.click('#pestanaOficial');
    assert.equal(await p.isVisible('#vincularCuenta'), true);
    assert.equal(await p.isVisible('#oficialLista'), false);
    await cerrar(p);
  });
});
