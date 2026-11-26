// Pruebas de regresión del motor de clasificación ciudadano. Ejecutar: node --test web/ciudadano/pruebas/
// Las etiquetas esperadas salen del catálogo (caneca de cada entrada), nunca del propio motor.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { crearMotor, normalizar, raiz, duracionPolitica } from '../js/motor.js';

const aqui = dirname(fileURLToPath(import.meta.url));
const catalogo = JSON.parse(readFileSync(join(aqui, '../datos/catalogo-residuos.json'), 'utf8'));
const motor = crearMotor(catalogo);
const resumen = { total: 0, fallos: [] };

function esperaCaneca(texto, caneca, id, grupo) {
  resumen.total++;
  const r = motor.clasificar(texto);
  const ok = r.estado === 'reconocido' && r.canecas.length === 1 && r.canecas[0] === caneca
    && (!id || r.items[0]?.residuo?.id === id);
  if (!ok) resumen.fallos.push({ grupo, texto, esperado: `${caneca}${id ? ' ' + id : ''}`, obtenido: `${r.estado} ${r.canecas.join('+')} ${r.items.map((i) => i.residuo?.id ?? i.especial?.id ?? i.tipo).join(',')}` });
  return ok;
}

const plural = (frase) => normalizar(frase).split(' ').map((w, i) => {
  if (i > 0 && ['de', 'del', 'con', 'no'].includes(w)) return w;
  if (/[aeiou]$/.test(w)) return w + 's';
  if (/z$/.test(w)) return w.slice(0, -1) + 'ces';
  if (/[lnrdj]$/.test(w)) return w + 'es';
  return w;
}).join(' ');

function errorTipografico(frase) {
  const palabras = normalizar(frase).split(' ');
  let k = 0; palabras.forEach((w, i) => { if (w.length > palabras[k].length) k = i; });
  const w = palabras[k];
  if (w.length < 6) return null;
  const m = Math.floor(w.length / 2);
  palabras[k] = w.slice(0, m - 1) + w[m] + w[m - 1] + w.slice(m + 1);   // transposición de dos letras centrales
  return palabras.join(' ');
}

test('el catálogo conserva las 90 entradas originales (30 por caneca) y amplía con limón, lulo y pegamento', () => {
  const n = catalogo.residuos.length;
  assert.ok(n >= 90, 'al menos 90 residuos');
  for (const c of ['verde', 'blanca', 'negra']) assert.ok(catalogo.residuos.filter((r) => r.caneca === c).length >= 30, c);
  assert.equal(new Set(catalogo.residuos.map((r) => r.id)).size, n);
  for (const r of catalogo.residuos) assert.match(r.oracle, /^[A-Z_]+$/, r.id);
  const nombres = catalogo.residuos.map((r) => normalizar(r.nombre));
  assert.equal(new Set(nombres).size, n, 'nombres canónicos duplicados');
  for (const id of ['V31', 'V32', 'N31']) assert.ok(catalogo.residuos.some((r) => r.id === id), id);
});

test('expresiones de los 11 residuos visuales y sinónimos colombianos (regresión 25/11/2026)', () => {
  const casos = {
    verde: ['patata', 'papa', 'limon exprimido', 'cascara de limon', 'limon', 'manzana verde', 'corazon de manzana', 'lulo', 'naranjilla', 'banano', 'banana', 'cascara de platano', 'restos de frutas'],
    blanca: ['papel de cuaderno', 'hoja de cuaderno', 'hoja cuadriculada', 'hoja rayada', 'bolsa kraft', 'bolsa de papel', 'papel aluminio', 'aluminio limpio', 'serum', 'sérum', 'frasco rosado', 'gotero de vidrio', 'frasco de vidrio', 'carton'],
    negra: ['servilleta usada', 'servilleta sucia', 'papel higienico', 'papel grasoso', 'panuelo usado', 'gota magica', 'Gota Mágica', 'astro glue', 'superpegamento vacio'],
  };
  for (const [caneca, frases] of Object.entries(casos))
    for (const f of frases) { const r = motor.clasificar(f); assert.equal(r.estado, 'reconocido', f); assert.deepEqual(r.canecas, [caneca], f); }
  assert.equal(motor.clasificar('pegamento').estado, 'ambiguo', 'pegamento sin decir si está vacío pregunta');
  assert.equal(motor.clasificar('papel').estado, 'ambiguo', 'papel sin condición pregunta');
  assert.deepEqual(motor.clasificar('papel limpio').canecas, ['blanca']);
  assert.deepEqual(motor.clasificar('papel sucio').canecas, ['negra']);
  assert.deepEqual(motor.clasificar('envoltura metalizada').canecas, ['negra'], 'envoltura metalizada no es aluminio limpio');
  assert.deepEqual(motor.clasificar('tengo un limon y papel aluminio').canecas.sort(), ['blanca', 'verde']);
  assert.deepEqual(motor.clasificar('papa, papel aluminio y servilleta usada').canecas.sort(), ['blanca', 'negra', 'verde']);
});

test('política 7/14/21 s por canecas distintas (igual que C#, panel y pasarela)', () => {
  assert.equal(duracionPolitica(1), 7000);
  assert.equal(duracionPolitica(2), 14000);
  assert.equal(duracionPolitica(3), 21000);
});

test('todos los nombres canónicos → su caneca y su ID', () => {
  for (const r of catalogo.residuos) esperaCaneca(r.nombre, r.caneca, r.id, 'canonico');
});

test('todos los alias → la caneca de su residuo', () => {
  for (const r of catalogo.residuos) for (const a of r.alias) esperaCaneca(a, r.caneca, null, 'alias');
});

test('plurales, mayúsculas y sin tildes', () => {
  for (const r of catalogo.residuos) {
    esperaCaneca(plural(r.nombre), r.caneca, null, 'plural');
    esperaCaneca(normalizar(r.nombre).toUpperCase(), r.caneca, r.id, 'sin-tildes');
  }
});

test('errores ortográficos razonables (transposición en la palabra más larga)', () => {
  for (const r of catalogo.residuos) {
    const t = errorTipografico(r.nombre);
    if (t) esperaCaneca(t, r.caneca, null, 'error-ortografico');
  }
});

test('frases completas de la vida diaria', () => {
  const plantillas = ['Voy a botar una {x}', 'Quiero desechar {x}', 'Tengo {x}', '¿Dónde boto {x}?', 'eco, voy a desechar {x} por favor'];
  for (const r of catalogo.residuos) for (const p of plantillas) esperaCaneca(p.replace('{x}', r.nombre.toLowerCase()), r.caneca, r.id, 'frase');
});

test('ejemplos del enunciado', () => {
  esperaCaneca('Voy a botar una botella', 'blanca', null, 'enunciado');
  esperaCaneca('Quiero desechar una manzana', 'verde', 'V01', 'enunciado');
  esperaCaneca('Tengo una servilleta sucia', 'negra', 'N02', 'enunciado');
  esperaCaneca('Una bolsa de papel', 'blanca', 'B11', 'enunciado');
  esperaCaneca('Cáscara de guineo', 'verde', 'V02', 'enunciado');
  esperaCaneca('Tengo un empaque de papitas', 'negra', 'N13', 'enunciado');
  esperaCaneca('voy a desechar una cáscara de banano', 'verde', 'V02', 'enunciado');
  esperaCaneca('botella de plástico', 'blanca', 'B01', 'enunciado');
  const multi = motor.clasificar('Una botella plástica y una papa');
  assert.equal(multi.estado, 'reconocido');
  assert.deepEqual(multi.canecas, ['verde', 'blanca']);
});

test('varios materiales: agrupa por caneca y elimina duplicados', () => {
  const casos = [
    ['una manzana, una botella de agua y una servilleta', ['verde', 'blanca', 'negra']],
    ['lata de gaseosa y botella de vidrio', ['blanca']],
    ['cáscara de huevo y borra de café', ['verde']],
    ['periódico, papel higiénico', ['blanca', 'negra']],
    ['botella y botella', ['blanca']],
  ];
  for (const [t, esperado] of casos) {
    resumen.total++;
    const r = motor.clasificar(t);
    if (r.estado !== 'reconocido' || r.canecas.join() !== esperado.join()) resumen.fallos.push({ grupo: 'multiple', texto: t, esperado: esperado.join('+'), obtenido: `${r.estado} ${r.canecas.join('+')}` });
  }
  assert.equal(duracionPolitica(1), 7000); assert.equal(duracionPolitica(2), 14000); assert.equal(duracionPolitica(3), 21000);
});

test('residuos especiales y peligrosos nunca abren una caneca', () => {
  const casos = ['una pila', 'pilas usadas', 'medicamentos vencidos', 'una jeringa', 'vidrio roto', 'una botella rota', 'un bombillo',
    'mi celular viejo', 'un cargador', 'aceite de cocina', 'lata de pintura', 'envase de veneno', 'insecticida en aerosol',
    'una batería', 'un termómetro', 'pastillas', 'botella plástica y una pila', 'cuchilla de afeitar', 'medicamentoz'];
  for (const t of casos) {
    resumen.total++;
    const r = motor.clasificar(t);
    if (r.estado !== 'especial' || r.canecas.length) resumen.fallos.push({ grupo: 'especial', texto: t, esperado: 'especial', obtenido: `${r.estado} ${r.canecas.join('+')}` });
  }
});

test('términos ambiguos piden aclaración', () => {
  for (const t of ['papel', 'una caja', 'icopor', 'un envase', 'empaque', 'plástico']) {
    resumen.total++;
    const r = motor.clasificar(t);
    if (r.estado !== 'ambiguo' || r.canecas.length) resumen.fallos.push({ grupo: 'ambiguo', texto: t, esperado: 'ambiguo', obtenido: `${r.estado} ${r.canecas.join('+')}` });
  }
  // Con contexto suficiente ya no es ambiguo.
  esperaCaneca('papel con comida', 'negra', 'N18', 'contexto');
  esperaCaneca('caja de pizza', 'negra', 'N17', 'contexto');
  esperaCaneca('cartón sucio', 'negra', 'N17', 'contexto');
  esperaCaneca('caja de cartón con grasa', 'negra', 'N17', 'contexto');
  esperaCaneca('papel limpio', 'blanca', 'B14', 'contexto');
});

test('materiales desconocidos no se fuerzan a ninguna caneca', () => {
  for (const t of ['mesa', 'carro', 'xyzw', 'un zapato', 'ladrillo', 'qwerty asdf']) {
    resumen.total++;
    const r = motor.clasificar(t);
    if (!['desconocido', 'mixto'].includes(r.estado) || r.canecas.length) resumen.fallos.push({ grupo: 'desconocido', texto: t, esperado: 'desconocido', obtenido: `${r.estado} ${r.canecas.join('+')} ${r.items.map((i) => i.residuo?.id).join()}` });
  }
});

test('raíz y normalización', () => {
  assert.equal(normalizar('  Cáscara  de PIÑA! '), 'cascara de pina');
  assert.equal(raiz('botellas'), raiz('botella'));
  assert.equal(raiz('plastico'), raiz('plastica'));
  assert.equal(raiz('panales'), raiz('panal'));
});

test('resumen: cero clasificaciones incorrectas', () => {
  const salida = join(aqui, 'resultados');
  mkdirSync(salida, { recursive: true });
  writeFileSync(join(salida, 'catalogo.json'), JSON.stringify({ fecha: new Date().toISOString(), casos: resumen.total, fallos: resumen.fallos }, null, 2));
  console.log(`Casos evaluados: ${resumen.total} · fallos: ${resumen.fallos.length}`);
  for (const f of resumen.fallos.slice(0, 60)) console.log(`  [${f.grupo}] «${f.texto}» esperado ${f.esperado} → ${f.obtenido}`);
  assert.equal(resumen.fallos.length, 0);
});
