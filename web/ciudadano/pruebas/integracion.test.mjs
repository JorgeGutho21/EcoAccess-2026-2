// Integración WEB → pasarela ciudadana → panel (simulado). No mueve motores ni toca el COM.
// Requiere la pasarela compilada (dotnet build src/EcoAccess.P3.PasarelaCiudadana -c Release) y Python.
// Ejecutar: node --test web/ciudadano/pruebas/integracion.test.mjs
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { existsSync, mkdtempSync, readFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const aqui = dirname(fileURLToPath(import.meta.url));
const raiz = join(aqui, '..', '..', '..');
const exe = join(raiz, 'src', 'EcoAccess.P3.PasarelaCiudadana', 'bin', 'Release', 'net48', 'EcoAccess.P3.PasarelaCiudadana.exe');
const PANEL = 8191, PUB = 8197, OPE = 8198;
const ORIGEN = 'http://localhost:5173';
const temporal = mkdtempSync(join(tmpdir(), 'ecoaccess-integracion-'));
const bitacora = join(temporal, 'operaciones.jsonl');
const procesos = [];
let permiso = '';

const dormir = (ms) => new Promise((r) => setTimeout(r, ms));
const clave = () => crypto.randomUUID().replace(/-/g, '');
async function esperarPuerto(url) { for (let i = 0; i < 60; i++) { try { await fetch(url); return; } catch { await dormir(250); } } throw new Error('No arrancó ' + url); }
function iniciarPanel(firmware) {
  const p = spawn('python', [join(aqui, 'panel-simulado.py'), '--puerto', String(PANEL), '--firmware', firmware], { stdio: 'ignore' });
  procesos.push(p); return p;
}
const api = (ruta, { metodo = 'GET', cuerpo, token = permiso, origen = ORIGEN } = {}) => fetch(`http://127.0.0.1:${PUB}${ruta}`, {
  method: metodo, headers: { ...(token ? { Authorization: 'Bearer ' + token } : {}), ...(origen ? { Origin: origen } : {}), ...(cuerpo ? { 'Content-Type': 'application/json' } : {}) },
  body: cuerpo ? JSON.stringify(cuerpo) : undefined,
});
const ordenes = () => fetch(`http://127.0.0.1:${PANEL}/__ordenes`).then((r) => r.json());
const apertura = (canecas, extra = {}) => api('/api/v1/aperturas', { metodo: 'POST', cuerpo: { clave: clave(), canecas, materiales: [{ id: 'B01', oracle: 'PET' }], consulta: 'botella', creadoEn: new Date().toISOString(), ...extra } });

const disponible = existsSync(exe);
before(async () => {
  if (!disponible) return;
  let panel = iniciarPanel('v1');
  await esperarPuerto(`http://127.0.0.1:${PANEL}/api/estacion/estado`);
  const g = spawn(exe, ['--sin-tunel', '--panel', `http://127.0.0.1:${PANEL}`, '--publico', String(PUB), '--operador', String(OPE), '--bitacora', bitacora, '--cuentas', 'http://127.0.0.1:8199'], { stdio: 'ignore' });
  procesos.push(g);
  await esperarPuerto(`http://127.0.0.1:${OPE}/estado`);
});
after(() => { for (const p of procesos) try { p.kill(); } catch { /* ya terminó */ } });

test('integración web → pasarela → panel', { skip: !disponible && 'Compila primero la pasarela en Release' }, async (t) => {
  await t.test('sin permiso presencial no hay acceso (401)', async () => {
    assert.equal((await api('/api/v1/estado', { token: '' })).status, 401);
    assert.equal((await api('/api/v1/aperturas', { metodo: 'POST', token: 'x'.repeat(43), cuerpo: {} })).status, 401);
  });

  await t.test('el operador exige su encabezado y emite un QR', async () => {
    assert.equal((await fetch(`http://127.0.0.1:${OPE}/habilitar`, { method: 'POST' })).status, 403);
    const d = await fetch(`http://127.0.0.1:${OPE}/habilitar`, { method: 'POST', headers: { 'X-EcoAccess-Operador': '1' } }).then((r) => r.json());
    assert.ok(d.ok);
    permiso = new URLSearchParams(d.enlaceLocal.split('#')[1]).get('t');
    assert.ok(permiso.length >= 40);
  });

  await t.test('CORS restringido a los orígenes autorizados', async () => {
    assert.equal((await api('/api/v1/estado', { origen: 'https://sitio-ajeno.example' })).status, 403);
    const r = await api('/api/v1/estado');
    assert.equal(r.headers.get('access-control-allow-origin'), ORIGEN);
    const pre = await fetch(`http://127.0.0.1:${PUB}/api/v1/aperturas`, { method: 'OPTIONS', headers: { Origin: ORIGEN, 'Access-Control-Request-Method': 'POST' } });
    assert.equal(pre.status, 204);
  });

  await t.test('estado real: disponible con firmware v1 (ciclo calibrado)', async () => {
    const e = await api('/api/v1/estado').then((r) => r.json());
    assert.equal(e.estado, 'disponible'); assert.equal(e.modo, 'ciclo'); assert.equal(e.sensores, false);
  });

  let primera;
  await t.test('una caneca (blanca) → CICLO B, estado comandado y plan con tiempo calibrado', async () => {
    primera = { clave: clave(), canecas: 'B', materiales: [{ id: 'B01', oracle: 'PET' }], consulta: 'botella de plástico', creadoEn: new Date().toISOString() };
    const r = await api('/api/v1/aperturas', { metodo: 'POST', cuerpo: primera });
    const d = await r.json();
    assert.equal(r.status, 200, JSON.stringify(d));
    assert.equal(d.estadoFisico, 'comandado'); assert.equal(d.modo, 'ciclo');
    assert.equal(d.plan.length, 1); assert.equal(d.plan[0].caneca, 'B'); assert.equal(d.plan[0].abiertaMs, 1300);
    const o = await ordenes(); assert.equal(o.length, 1); assert.equal(o[0].ruta, '/api/canecas/B/abrir');
  });

  await t.test('doble clic / reintento con la misma clave NO repite la apertura', async () => {
    const d = await api('/api/v1/aperturas', { metodo: 'POST', cuerpo: primera }).then((r) => r.json());
    assert.equal(d.repetida, true);
    assert.equal((await ordenes()).length, 1);
  });

  await t.test('solicitud inmediata nueva: rechazada (ocupada o límite)', async () => {
    const r = await apertura('V');
    assert.ok([409, 429].includes(r.status), String(r.status));
    assert.equal((await ordenes()).length, 1);
  });

  await t.test('solicitud antigua (recarga o reenvío): no se ejecuta', async () => {
    await dormir(6500);
    const r = await apertura('N', { creadoEn: new Date(Date.now() - 5 * 60e3).toISOString() });
    assert.equal(r.status, 409); assert.equal((await r.json()).codigo, 'antigua');
  });

  await t.test('varias canecas con firmware v1 → SEC, una tras otra, sin omitir ninguna', async () => {
    const d = await apertura('NBV').then((r) => r.json());
    assert.ok(d.ok, JSON.stringify(d));
    assert.deepEqual(d.plan.map((p) => p.caneca), ['V', 'B', 'N']);
    assert.ok(d.plan[1].inicioMs > d.plan[0].inicioMs + d.plan[0].abiertaMs);
    const o = await ordenes(); assert.equal(o.at(-1).ruta, '/api/pruebas/secuencial'); assert.equal(o.at(-1).cuerpo.canecas, 'VBN');
  });

  await t.test('confirmación de depósito y bitácora idempotente pendiente de Oracle', async () => {
    assert.equal((await api(`/api/v1/aperturas/${primera.clave}/confirmacion`, { metodo: 'POST', cuerpo: { deposito: true } })).status, 200);
    assert.equal((await api(`/api/v1/aperturas/${clave()}/confirmacion`, { metodo: 'POST', cuerpo: { deposito: true } })).status, 404);
    const lineas = readFileSync(bitacora, 'utf8').trim().split('\n').map((l) => JSON.parse(l));
    const op = lineas.find((l) => l.idOperacion === 'web:' + primera.clave);
    assert.equal(op.canal, 'WEB'); assert.equal(op.tipo, 'OPERACION_AUTOMATICA'); assert.equal(op.estadoRegistro, 'PENDIENTE_ORACLE');
    assert.deepEqual(op.contenedores, [1093]); assert.equal(op.verificacionFisica, 'SIN_SENSOR');
    assert.ok(lineas.some((l) => l.tipo === 'DISPOSICION_CONFIRMADA_CIUDADANO' && l.operacionOrigen === 'web:' + primera.clave));
    assert.equal(lineas.filter((l) => l.idOperacion === 'web:' + primera.clave).length, 1, 'la clave repetida no duplicó el registro');
  });

  await t.test('ESP32 desconectado: estado desconectada y apertura rechazada', async () => {
    await fetch(`http://127.0.0.1:${PANEL}/__conectado`, { method: 'POST', body: JSON.stringify({ valor: false }), headers: { 'Content-Type': 'application/json' } });
    assert.equal((await api('/api/v1/estado').then((r) => r.json())).estado, 'desconectada');
    await dormir(6500);
    assert.equal((await apertura('B')).status, 503);
    await fetch(`http://127.0.0.1:${PANEL}/__conectado`, { method: 'POST', body: JSON.stringify({ valor: true }), headers: { 'Content-Type': 'application/json' } });
  });

  await t.test('cuenta e historial: sin permiso presencial, con CORS restringido y sin abrir nada', async () => {
    const r = await api('/api/v1/cuenta/historial', { metodo: 'POST', cuerpo: { token: 'x'.repeat(43) }, token: null });
    assert.equal(r.status, 503, 'sin el bot de cuentas la pasarela responde 503, no 401');
    const ajeno = await api('/api/v1/cuenta/codigo', { metodo: 'POST', cuerpo: {}, token: null, origen: 'https://evil.example' });
    assert.equal(ajeno.status, 403);
    const otra = await api('/api/v1/cuenta/abrir', { metodo: 'POST', cuerpo: {}, token: null });
    assert.equal(otra.status, 404, 'solo existen las rutas de cuenta conocidas');
    const sinPermiso = await api('/api/v1/aperturas', { metodo: 'POST', cuerpo: { clave: clave(), canecas: 'V', creadoEn: new Date().toISOString() }, token: null });
    assert.equal(sinPermiso.status, 401, 'abrir sigue exigiendo el permiso presencial');
  });

  await t.test('terminar la sesión invalida el QR al instante', async () => {
    await fetch(`http://127.0.0.1:${OPE}/terminar`, { method: 'POST', headers: { 'X-EcoAccess-Operador': '1' } });
    assert.equal((await api('/api/v1/estado')).status, 401);
  });
});
