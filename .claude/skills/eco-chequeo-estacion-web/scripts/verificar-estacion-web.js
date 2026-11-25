#!/usr/bin/env node
/* Regla de chequeo de la estación web de canecas (estacion-web; si no existe, sites/ecoaccess-estacion).
   Uso, desde la raíz del repositorio:  node .claude/skills/eco-chequeo-estacion-web/scripts/verificar-estacion-web.js [--estricto]
   Sale con código 1 si hay errores (o advertencias con --estricto). Sin dependencias: solo Node. */
'use strict';
const fs = require('fs');
const path = require('path');
const { spawnSync } = require('child_process');

const estricto = process.argv.includes('--estricto');
const raiz = path.resolve(__dirname, '..', '..', '..', '..');
const sitio = [path.join(raiz, 'estacion-web'), path.join(raiz, 'sites', 'ecoaccess-estacion')].find((p) => fs.existsSync(path.join(p, 'dist', 'index.html'))) || path.join(raiz, 'estacion-web');
const dist = path.join(sitio, 'dist');
const leer = (p) => fs.readFileSync(p, 'utf8');

if (!fs.existsSync(path.join(dist, 'index.html'))) {
  console.error(`No se encontró ${path.join(dist, 'index.html')}. Corre el chequeo desde el repositorio de EcoAccess.`);
  process.exit(2);
}
const html = leer(path.join(dist, 'index.html'));
const css = leer(path.join(dist, 'styles.css'));
const js = leer(path.join(dist, 'app.js'));
const servidor = leer(path.join(sitio, 'server-local.js'));

const resultados = [];
const regla = (id, nivel, descripcion, cumple, detalle = '') => resultados.push({ id, nivel, descripcion, cumple: Boolean(cumple), detalle });

/* ── D. Diseño final (banner + tarjetas) ─────────────────────────────────────────────────────────── */
regla('D1', 'error', 'Banner blanco con pastilla «EcoAccess» y ondas verdes',
  /<header class="banner">/.test(html) && /class="marca"[\s\S]*?EcoAccess/.test(html) && (html.match(/banner-onda /g) || []).length >= 2);
regla('D2', 'error', 'Título «¿Dónde vas a depositar?» con «depositar?» resaltado y brote',
  /<h1 id="titulo">¿Dónde vas a <em>depositar\?<\/em><\/h1>/.test(html) && /class="banner-brote"/.test(html));
regla('D3', 'error', 'Buscador «Escribe o di tu residuo» con etiqueta accesible',
  /id="formResiduo"/.test(html) && /<label[^>]+for="residuo"/.test(html) && /<input id="residuo"/.test(html));
regla('D4', 'error', 'Micrófono verde redondo con «Dictado por voz» y estado «Escuchando…»',
  /id="botonVoz" class="microfono"/.test(html) && /Dictado por voz/.test(html) && /id="estadoVoz"/.test(html));
regla('D5', 'error', 'Ayuda de voz con los ejemplos de frases', /class="ayuda-voz"[\s\S]*?caneca verde[\s\S]*?caneca blanca[\s\S]*?caneca negra/.test(html));
const orden = [...html.matchAll(/<button class="caneca" type="button" data-color="(\w+)"/g)].map((m) => m[1]).join(',');
const titulos = [...html.matchAll(/<span class="caneca-tipo">([^<]+)<\/span>/g)].map((m) => m[1]).join(',');
regla('D6', 'error', 'Tres tarjetas en orden verde, blanca, negra', orden === 'verde,blanca,negra', `encontrado: ${orden || 'ninguna'}`);
regla('D7', 'error', 'Títulos Orgánicos / Aprovechables / No aprovechables (sin la palabra del color)',
  titulos === 'Orgánicos,Aprovechables,No aprovechables', `encontrado: ${titulos}`);
regla('D8', 'error', 'Cada tarjeta muestra la caneca moderna (cuerpo y tapa) y la flecha circular',
  (html.match(/class="cuerpo" src="assets\/canecas\//g) || []).length >= 3 && (html.match(/class="caneca-flecha"/g) || []).length === 3);
regla('D9', 'error', 'Sin «modo demostración» ni textos de demo visibles',
  !/demostraci[oó]n|modo demo/i.test(html + js.replace(/\/\*[\s\S]*?\*\/|\/\/.*$/gm, '')));
regla('D10', 'error', 'Sin la franja «Así de fácil» ni imágenes de residuos (minimalista)', !/class="pasos"|assets\/residuos\//.test(html));

/* ── T. Tokens y estilo ──────────────────────────────────────────────────────────────────────────── */
const PALETA = new Set(('#04140C #0B3A24 #0C4A2F #105C3B #12360F #15803D #168A55 #1D5A12 #1D6B17 #1E293B #1F242B #23803F #2A3038 ' +
  '#2E7D1A #2E8B1E #2E9B2E #2FA14A #353C47 #3F9B1E #4FB52A #556274 #5B6472 #5BC85A #7AC143 #8A97A6 #925A07 #94A3B8 #B42318 #B7E4CB ' +
  '#C8EB9E #E2E8E5 #E3ECE6 #E3F4EA #E8F6EE #EEF1F4 #EFF4F1 #F0F5F1 #F1FAF2 #F5FAF6 #F6F8F7 #FDECEA #FECACA #FEF4E2 #FFF #FFFFFF').split(' '));
const colores = [...new Set((css.match(/#[0-9a-f]{6}\b|#[0-9a-f]{3}\b/gi) || []).map((c) => c.toUpperCase()))];
const fuera = colores.filter((c) => !PALETA.has(c));
regla('T1', 'advertencia', 'Colores dentro de la paleta EcoAccess (TemaEcoAccess + verdes de las canecas)', !fuera.length, fuera.join(' '));
const tokens = ['--verde-oscuro: #105C3B', '--verde: #168A55', '--verde-suave: #E8F6EE', '--texto: #1E293B', '--suave: #556274', '--lima: #7AC143'];
const faltan = tokens.filter((t) => !css.includes(t));
regla('T2', 'error', 'Tokens de TemaEcoAccess intactos en :root', !faltan.length, faltan.join(', '));
regla('T3', 'error', 'Sin recursos externos (fuentes, CDN, imágenes remotas)', !/https?:\/\//i.test(html.replace(/<!--[\s\S]*?-->/g, '') + css));
regla('T4', 'error', 'Botones grandes: micrófono ≥ 58px y botones del banner ≥ 44px',
  /\.microfono \{[^}]*width: clamp\((\d+)px/.test(css) && Number(css.match(/\.microfono \{[^}]*width: clamp\((\d+)px/)[1]) >= 58 &&
  /\.buscador button \{[^}]*width: (\d+)px/.test(css) && Number(css.match(/\.buscador button \{[^}]*width: (\d+)px/)[1]) >= 44);
const pequenas = (css.match(/font-size:\s*(\d*\.?\d+)px/g) || []).filter((d) => parseFloat(d.split(':')[1]) < 14);
regla('T5', 'advertencia', 'Ningún texto por debajo de 14 px', !pequenas.length, pequenas.join(', '));
regla('T6', 'error', 'Diseño adaptable: filas en vertical, banner en dos filas y celular', /@media \(max-aspect-ratio: 4\/5\), \(max-width: 600px\)/.test(css) &&
  /@media \(max-width: 980px\)/.test(css) && /@media \(max-width: 600px\)/.test(css));
regla('T7', 'error', 'Respeta «reducir movimiento»', /prefers-reduced-motion: reduce/.test(css));
const vCss = html.match(/styles\.css\?v=(\d+)/); const vJs = html.match(/app\.js\?v=(\d+)/);
regla('T8', 'error', 'Versión de caché igual en styles.css y app.js', vCss && vJs && vCss[1] === vJs[1], `css ${vCss?.[1]} · js ${vJs?.[1]}`);

/* ── F. Funcionalidad ────────────────────────────────────────────────────────────────────────────── */
regla('F1', 'error', 'Micrófono abierto al iniciar (sin depender de una preferencia guardada)',
  /^activarVoz\(false\);/m.test(js) && !/eco-voz/.test(js));
regla('F2', 'error', 'Residuo → caneca → apertura automática, sin pedir «sí»',
  /function seleccionarCaneca/.test(js) && /abrir\(r\.c, origen, null, r\.n\)/.test(js) && !/Di «sí»/.test(js));
regla('F3', 'error', 'El momento de depósito dice «Tu caneca es la …»', /id="momentoTuCaneca"/.test(html) && /Tu caneca es la /.test(js));
regla('F4', 'error', 'Ventana de depósito de 7 s (reloj, simulación del navegador y servidor)',
  /<span id="relojNumero">7<\/span>/.test(html) && /abiertoMs: 7000/.test(js) && /ECOACCESS_ABIERTO_MS \|\| 7000/.test(servidor));
regla('F5', 'error', 'La apertura exige el encabezado X-EcoAccess', /'X-EcoAccess': 'estacion'/.test(js) && /x-ecoaccess/i.test(servidor));
regla('F6', 'error', 'Sin secretos en la web (HMAC, wallets, contraseñas, claves)',
  !/hmac|wallet|password|contrase[ñn]a\s*[:=]|BEGIN [A-Z ]*PRIVATE KEY|api[_-]?key/i.test(html + js));
regla('F7', 'error', 'Textos dinámicos sin innerHTML con datos del usuario',
  !/innerHTML\s*=\s*[^;]*(value|transcript)/.test(js));
for (const archivo of [path.join(dist, 'app.js'), path.join(sitio, 'server-local.js')]) {
  const r = spawnSync(process.execPath, ['--check', archivo], { encoding: 'utf8' });
  regla('F8', 'error', `Sintaxis válida de ${path.basename(archivo)}`, r.status === 0, (r.stderr || '').split('\n')[0]);
}

/* ── Informe ─────────────────────────────────────────────────────────────────────────────────────── */
let errores = 0; let advertencias = 0;
for (const r of resultados) {
  const marca = r.cumple ? 'OK ' : r.nivel === 'error' ? 'ERR' : 'ADV';
  if (!r.cumple) { if (r.nivel === 'error') errores++; else advertencias++; }
  console.log(`${marca}  ${r.id.padEnd(4)} ${r.descripcion}${!r.cumple && r.detalle ? `  →  ${r.detalle}` : ''}`);
}
console.log(`\n${resultados.length} reglas · ${errores} errores · ${advertencias} advertencias`);
process.exit(errores || (estricto && advertencias) ? 1 : 0);
