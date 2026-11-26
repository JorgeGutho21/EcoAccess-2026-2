// Pruebas unitarias (sin red) de la política de reintentos compartida por el navegador y las pruebas de carga.
// Ejecutar: node --test web/juego/pruebas/reintentos.test.mjs
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { POLITICA, esReintentable, leerRetryAfter, calcularEspera, escalonamiento } from '../js/reintentos.js';

test('se reintenta el 429 de ORDS (HTML), 5xx y la red caída; no las reglas del juego', () => {
  assert.ok(esReintentable(429, null));
  assert.ok(esReintentable(0, null));
  for (const s of [500, 502, 503, 504]) assert.ok(esReintentable(s, null));
  assert.ok(!esReintentable(429, { ok: false, codigo: 'limite' }), 'límite de registros del juego');
  assert.ok(!esReintentable(409, { ok: false }));
  assert.ok(!esReintentable(400, null));
  assert.ok(!esReintentable(200, { ok: true }));
});

test('Retry-After en segundos o fecha HTTP', () => {
  assert.equal(leerRetryAfter('3'), 3000);
  assert.equal(leerRetryAfter('0'), 0);
  assert.equal(leerRetryAfter(null), null);
  assert.equal(leerRetryAfter(''), null);
  assert.equal(leerRetryAfter('-1'), null);
  assert.equal(leerRetryAfter('basura'), null);
  const ahora = Date.parse('2026-11-25T15:00:00Z');
  assert.equal(leerRetryAfter('Wed, 25 Nov 2026 15:00:05 GMT', ahora), 5000);
  assert.equal(leerRetryAfter('Wed, 25 Nov 2026 14:00:00 GMT', ahora), 0);
});

test('espera exponencial con jitter, acotada, y Retry-After respetado', () => {
  for (let i = 0; i < 12; i++) {
    const techo = Math.min(POLITICA.tope, POLITICA.base * 2 ** i);
    assert.equal(calcularEspera(i, null, POLITICA, () => 0), Math.floor(techo / 2));
    assert.ok(calcularEspera(i, null, POLITICA, () => 0.999999) <= techo);
  }
  assert.ok(calcularEspera(3, null, POLITICA, () => 0.5) > calcularEspera(1, null, POLITICA, () => 0.5), 'crece con los intentos');
  assert.equal(calcularEspera(0, 4000, POLITICA, () => 0), 4000);
  assert.equal(calcularEspera(0, 999999, POLITICA, () => 0), POLITICA.maxRetryAfter, 'Retry-After acotado');
  const total = Array.from({ length: POLITICA.maxIntentos }, (_, i) => calcularEspera(i, null, POLITICA, () => 0.999999)).reduce((a, b) => a + b);
  assert.ok(total < 90000, `los ${POLITICA.maxIntentos} reintentos caben en la paciencia por defecto (${total} ms)`);
});

test('escalonamiento aleatorio breve', () => {
  assert.equal(escalonamiento(1200, () => 0), 0);
  assert.ok(escalonamiento(1200, () => 0.999999) < 1200);
});
