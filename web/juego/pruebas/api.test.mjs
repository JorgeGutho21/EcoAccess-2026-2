// Pruebas funcionales REALES contra Oracle Cloud, en una ronda de PRUEBA aislada que se elimina al terminar.
// Ejecutar: node --test web/juego/pruebas/api.test.mjs   (requiere la clave local de administración)
import { test, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { llamar, admin, crearRondaPrueba, eliminarRonda, jugar, secreto, CORRECTAS, BASE } from './comun.mjs';

let ronda;
let sesionRapida;
before(async () => { ronda = await crearRondaPrueba('Prueba funcional automatizada'); });
after(async () => { if (ronda) { const r = await eliminarRonda(ronda.id); assert.equal(r.status, 200, 'la ronda de prueba se elimina'); } });

const reg = (cuerpo) => llamar('/registro', { metodo: 'POST', cuerpo: { acepta: true, sesion: secreto(), dispositivo: secreto(), ronda: ronda.codigo, ...cuerpo } });

test('clave de respuestas privada disponible (7 opciones correctas)', () => assert.equal(CORRECTAS.size, 7));

test('administración protegida', async () => {
  assert.equal((await llamar('/admin/competencias', { metodo: 'POST', cuerpo: {}, admin: 'clave-incorrecta-de-prueba-123456' })).status, 401);
  assert.equal((await llamar('/admin/competencias', { metodo: 'POST', cuerpo: {} })).status, 401);
  assert.equal((await admin('competencias')).status, 200);
});

test('registro: validaciones, duplicados y reintento idempotente', async () => {
  assert.equal((await reg({ nombre: 'Ana Prueba', alias: 'Ana', acepta: false })).json.codigo, 'aviso');
  assert.equal((await reg({ nombre: 'Ana', alias: 'Ana1' })).json.codigo, 'nombre');
  assert.equal((await reg({ nombre: 'Ana Prueba', alias: 'x' })).json.codigo, 'alias');
  assert.equal((await reg({ nombre: 'Ana Prueba<script>', alias: 'Ana2' })).json.codigo, 'nombre');
  const sesion = secreto();
  const ok = await llamar('/registro', { metodo: 'POST', cuerpo: { nombre: 'Ana  Prueba', alias: 'AnaQA', acepta: true, sesion, ronda: ronda.codigo } });
  assert.equal(ok.status, 201, ok.texto); assert.equal(ok.json.estado, 'REGISTRADO');
  const otra = await llamar('/registro', { metodo: 'POST', cuerpo: { nombre: 'Ana Prueba', alias: 'AnaQA', acepta: true, sesion, ronda: ronda.codigo } });
  assert.equal(otra.status, 200, 'mismo secreto: devuelve la misma participación');
  assert.equal((await reg({ nombre: 'Beto Prueba', alias: 'anaqa' })).json.codigo, 'alias_ocupado');
  assert.equal((await reg({ nombre: 'ana prueba', alias: 'OtraAna' })).json.codigo, 'ya_registrado');
});

test('intento: cronómetro del servidor, recarga sin reinicio y sin pistas', async () => {
  const sesion = secreto();
  await llamar('/registro', { metodo: 'POST', cuerpo: { nombre: 'Carla Prueba', alias: 'CarlaQA', acepta: true, sesion, ronda: ronda.codigo } });
  assert.equal((await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: 1, opcion: 101 } })).json.codigo, 'no_iniciado');
  const i1 = await llamar('/intento/inicio', { metodo: 'POST', sesion });
  assert.equal(i1.json.estado, 'EN_CURSO'); assert.equal(i1.json.pregunta.orden, 1); assert.equal(i1.json.pregunta.opciones.length, 4);
  assert.ok(!/correct/i.test(i1.texto), 'la pregunta no revela la respuesta');
  await new Promise((r) => setTimeout(r, 1200));
  const i2 = await llamar('/intento/inicio', { metodo: 'POST', sesion });     // recarga + «Estoy listo» otra vez
  assert.ok(i2.json.transcurridoMs >= 1000, 'el cronómetro no se reinicia: ' + i2.json.transcurridoMs);
  assert.deepEqual(i2.json.pregunta.opciones.map((o) => o.id), i1.json.pregunta.opciones.map((o) => o.id), 'mismo orden de opciones');
  // Manipulación: opción de otra pregunta, pregunta futura y campos inventados.
  assert.equal((await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: 1, opcion: 201 } })).json.codigo, 'opcion');
  assert.equal((await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: 3, opcion: 301 } })).json.codigo, 'pregunta');
  const r1 = await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: 1, opcion: 101, puntaje: 700, correcta: true } });
  assert.equal(r1.json.pregunta.orden, 2); assert.ok(!/"correct|aciertos/i.test(r1.texto), 'no revela si acertó');
  const r1b = await llamar('/intento/respuesta', { metodo: 'POST', sesion, cuerpo: { pregunta: 1, opcion: 102 } });
  assert.equal(r1b.json.pregunta.orden, 2, 'una respuesta confirmada no se puede cambiar');
});

test('puntaje máximo 700, desempate por tiempo y podio de cinco', async () => {
  const lento = await jugar(ronda.codigo, { nombre: 'Diego Lento', alias: 'DiegoQA', aciertos: 7, pausaMs: 400 });
  const rapido = await jugar(ronda.codigo, { nombre: 'Elena Rapida', alias: 'ElenaQA', aciertos: 7 });
  const seis = await jugar(ronda.codigo, { nombre: 'Fabio Seis', alias: 'FabioQA', aciertos: 6 });
  for (const [n, a] of [['Gina Cinco', 5], ['Hugo Cuatro', 4], ['Iris Tres', 3]]) await jugar(ronda.codigo, { nombre: n, alias: n.split(' ')[0] + 'QA', aciertos: a });
  sesionRapida = rapido.sesion;
  assert.equal(rapido.final.json.resultado.puntaje, 700); assert.equal(rapido.final.json.resultado.aciertos, 7);
  assert.equal(seis.final.json.resultado.puntaje, 600);
  assert.ok(lento.final.json.resultado.tiempoMs > rapido.final.json.resultado.tiempoMs);
  const c = await llamar('/clasificacion', { ronda: ronda.codigo });
  assert.equal(c.json.podio.length, 5);
  assert.deepEqual(c.json.podio.map((p) => p.alias).slice(0, 3), ['ElenaQA', 'DiegoQA', 'FabioQA']);
  assert.deepEqual(c.json.podio.map((p) => p.posicion), [1, 2, 3, 4, 5]);
  assert.ok(!c.texto.includes('Elena Rapida'), 'la clasificación pública no muestra nombres completos');
  assert.equal(c.json.competencia.definitiva, false);
  // Repetir el envío final no cambia el resultado.
  const otra = await llamar('/intento/respuesta', { metodo: 'POST', sesion: rapido.sesion, cuerpo: { pregunta: 7, opcion: 704 } });
  assert.equal(otra.json.resultado.puntaje, 700);
  assert.equal((await llamar('/intento/revision', { sesion: rapido.sesion })).status, 403, 'sin revisión con el concurso activo');
});

test('cierre de registro, finalización, revisión y exportación', async () => {
  assert.equal((await admin('cerrar_registro', { competencia: ronda.id })).status, 200);
  assert.equal((await reg({ nombre: 'Tardio Prueba', alias: 'TardeQA' })).json.codigo, 'registro_cerrado');
  const sesion = (await jugar(ronda.codigo, { nombre: 'Zoe Pendiente', alias: 'ZoeQA' })).sesion;   // registro ya cerrado
  assert.equal(sesion, undefined);
  const f = await admin('finalizar', { competencia: ronda.id });
  assert.equal(f.status, 200, f.texto);
  const c = await llamar('/clasificacion', { ronda: ronda.codigo });
  assert.equal(c.json.competencia.definitiva, true);
  const p = await admin('participantes', { competencia: ronda.id });
  const elena = p.json.participantes.find((x) => x.alias === 'ElenaQA');
  assert.equal(elena.nombre, 'Elena Rapida'); assert.equal(elena.posicion, 1); assert.match(elena.respuestas, /1:B✓/);
  const csv = await llamar('/admin/exportacion?competencia=' + ronda.id, { admin: process.env.ECO_JUEGO_ADMIN ?? (await import('./comun.mjs')).CLAVE_ADMIN });
  assert.equal(csv.status, 200); assert.match(csv.texto, /^posicion;alias;nombre_completo/); assert.match(csv.texto, /Elena Rapida/);
  assert.equal((await admin('abrir', { competencia: ronda.id })).json.codigo, 'estado', 'una competencia finalizada no se reabre');
});

test('revisión educativa tras finalizar', async () => {
  const r = await jugar(ronda.codigo, { nombre: 'Nadie Mas', alias: 'NadieQA' });
  assert.ok(r.error, 'no se puede jugar en una ronda finalizada');
  const rev = await llamar('/intento/revision', { sesion: sesionRapida });
  assert.equal(rev.status, 200, rev.texto);
  assert.equal(rev.json.revision.length, 7);
  assert.ok(rev.json.revision.every((x) => x.acierto && x.correcta === x.elegida && x.explicacion));
  const c = await admin('participantes', { competencia: ronda.id });
  assert.ok(c.json.participantes.length >= 8);
});

test('la ronda de prueba nunca aparece en la clasificación pública por defecto', async () => {
  const pub = await llamar('/clasificacion');
  assert.ok(!pub.texto.includes('QA"'), 'sin participantes de prueba en la clasificación pública');
  console.log('API:', BASE);
});
