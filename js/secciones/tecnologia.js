// Tecnología (índice). Diseño: maqueta «Tecnología que impulsa EcoAccess» + regla .claude/skills/eco-diseno-web.
// Los textos de la maqueta que no coincidían con el sistema (sensores, «procesa la clasificación» en el ESP32,
// canecas que «identifican») se reescribieron con lo que de verdad hace cada pieza; cada tarjeta lleva a su página.
import { leyenda, movimientoReducido } from './comun.js';

export const titulo = 'Tecnología';

const GH = 'https://github.com/JorgeGutho21/EcoAccess-2026-2';
const ico = (d, extra = '') => `<svg viewBox="0 0 24 24" aria-hidden="true" ${extra}>${d}</svg>`;
const I = {
  casa: '<path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10v10h13V10"/>',
  capas: '<path d="m12 3 9 5-9 5-9-5z"/><path d="m3 13 9 5 9-5"/>',
  github: '<path d="M9 19c-4 1.2-4-2-6-2.5M15 21v-3.5c0-1 .1-1.4-.5-2 2.8-.3 5.5-1.4 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.3 4.3 0 0 0-.1-3.2s-1.1-.3-3.5 1.3a12 12 0 0 0-6.2 0C6.5 2.8 5.4 3.1 5.4 3.1a4.3 4.3 0 0 0-.1 3.2A4.6 4.6 0 0 0 4 9.5c0 4.6 2.7 5.7 5.5 6-.6.6-.6 1.2-.5 2V21"/>',
  flecha: '<path d="m9 6 6 6-6 6"/>',
  flechaLarga: '<path d="M5 12h14M13 6l6 6-6 6"/>',
  codigo: '<path d="m8 8-4 4 4 4M16 8l4 4-4 4M14 5l-4 14"/>',
  grafo: '<circle cx="12" cy="5" r="2.5"/><circle cx="5" cy="18" r="2.5"/><circle cx="19" cy="18" r="2.5"/><path d="m10.8 7.2-4.6 8.6M13.2 7.2l4.6 8.6M7.5 18h9"/>',
  chip: '<rect x="7" y="7" width="10" height="10" rx="1.5"/><path d="M10 3v4M14 3v4M10 17v4M14 17v4M3 10h4M3 14h4M17 10h4M17 14h4"/>',
  base: '<ellipse cx="12" cy="6" rx="7" ry="3"/><path d="M5 6v6c0 1.7 3.1 3 7 3s7-1.3 7-3V6M5 12v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6"/>',
  ia: '<rect x="5" y="5" width="14" height="14" rx="2"/><path d="M9 2v3M15 2v3M9 19v3M15 19v3M2 9h3M2 15h3M19 9h3M19 15h3"/><path d="M9.5 15V9M12.5 15l1.5-6 1.5 6M13 13h2"/>',
  pieza: '<path d="M10 3h4v3a2 2 0 1 0 4 0V3h3v7h-3a2 2 0 1 0 0 4h3v7h-7v-3a2 2 0 1 0-4 0v3H3v-7h3a2 2 0 1 0 0-4H3V3h7z"/>',
  red: '<circle cx="6" cy="6" r="2.5"/><circle cx="18" cy="6" r="2.5"/><circle cx="12" cy="18" r="2.5"/><path d="M8.5 6h7M7.3 8.2l3.5 7.6M16.7 8.2l-3.5 7.6"/>',
  hoja: '<path d="M5 19c0-8 5-13 15-14-1 10-6 15-14 15"/><path d="M5 19c3-4 6-7 10-9"/>',
  play: '<path d="m8 5 11 7-11 7z"/>',
};

const TEMAS = [
  ['programacion', 'Programación III', I.codigo, 'Clases, herencia, interfaces, colecciones, async y SOLID: qué está aplicado, qué es parcial y qué es oportunidad.'],
  ['arquitectura', 'Arquitectura', I.capas, 'Los nueve proyectos de la solución, sus dependencias y el recorrido animado de la foto al historial.'],
  ['grafos', 'Grafos', I.grafo, 'El diagrama de arquitectura leído como grafo, y BFS/DFS como demostración educativa (EcoAccess no los implementa).'],
  ['hardware', 'Hardware', I.chip, 'ESP32, tres servos por PWM, calibración y por qué un comando no es una posición medida.'],
  ['datos', 'Base de datos', I.base, 'Modelo entidad-relación navegable, migraciones V030–V035 y Oracle Cloud, wallet y SQL Developer en 12 pasos.'],
  ['ia', 'Inteligencia artificial', I.ia, 'Texto, voz e imagen; el proceso con Ollama y lfm3-vl, y sus limitaciones medidas.'],
  ['integraciones', 'Integraciones', I.pieza, 'Telegram, la pasarela de esta página hacia la estación, Oracle Cloud y GitHub Actions.'],
];

// Ilustraciones vectoriales del flujo (viewBox 120×90): pedestal + objeto. Las canecas son las imágenes reales.
const PEDESTAL = '<ellipse cx="60" cy="78" rx="40" ry="9" fill="#DDEFD9"/><ellipse cx="60" cy="74" rx="40" ry="9" fill="#F2FBF3" stroke="#CDE8CC" stroke-width="1.5"/>';
const ILU = {
  web: `${PEDESTAL}<rect x="32" y="18" width="56" height="38" rx="4" fill="#1E293B"/><rect x="36" y="22" width="48" height="30" rx="2" fill="#16A34A"/><circle cx="60" cy="37" r="10" fill="none" stroke="#fff" stroke-width="2.5"/><path d="M50 37h20M60 27c3.5 3 3.5 17 0 20M60 27c-3.5 3-3.5 17 0 20" stroke="#fff" stroke-width="2" fill="none"/><path d="M26 58h68l-5 8H31z" fill="#CBD5E1" stroke="#94A3B8" stroke-width="1.5" stroke-linejoin="round"/>`,
  telegram: `${PEDESTAL}<circle cx="60" cy="38" r="24" fill="#229ED9"/><path d="m48 38 22-9c1-.4 2 .4 1.7 1.5L67 50c-.3 1-1.4 1.4-2.2.8l-6-4.4-3 2.8c-.4.4-1.2.2-1.3-.4L52.5 43 48 41.5c-1-.3-1-1.6 0-1.9z" fill="#fff"/><path d="m54 43 11-9" stroke="#229ED9" stroke-width="1.6"/>`,
  ia: `${PEDESTAL}<rect x="40" y="18" width="40" height="40" rx="6" fill="#16A34A" stroke="#0B5E34" stroke-width="2.5"/><path d="M48 12v6M60 12v6M72 12v6M48 58v6M60 58v6M72 58v6M34 26h6M34 38h6M34 50h6M80 26h6M80 38h6M80 50h6" stroke="#0B5E34" stroke-width="2.5" stroke-linecap="round"/><text x="60" y="45" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="17" font-weight="800" fill="#fff">IA</text>`,
  oracle: `${PEDESTAL}<ellipse cx="60" cy="22" rx="20" ry="7" fill="#F1F5F9" stroke="#94A3B8" stroke-width="2"/><path d="M40 22v34c0 3.9 9 7 20 7s20-3.1 20-7V22" fill="#F8FAFC" stroke="#94A3B8" stroke-width="2"/><path d="M40 36c0 3.9 9 7 20 7s20-3.1 20-7M40 48c0 3.9 9 7 20 7s20-3.1 20-7" fill="none" stroke="#CBD5E1" stroke-width="1.6"/><rect x="45" y="27" width="30" height="9" rx="2" fill="#C74634"/><text x="60" y="34" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="6.5" font-weight="800" fill="#fff" letter-spacing=".5">ORACLE</text>`,
  esp32: `${PEDESTAL}<rect x="30" y="24" width="60" height="34" rx="4" fill="#1E293B" stroke="#0F172A" stroke-width="2"/><path d="M36 20v4M44 20v4M52 20v4M60 20v4M68 20v4M76 20v4M84 20v4M36 58v4M44 58v4M52 58v4M60 58v4M68 58v4M76 58v4M84 58v4" stroke="#CBD5E1" stroke-width="2.2" stroke-linecap="round"/><rect x="38" y="30" width="26" height="20" rx="2" fill="#334155"/><text x="51" y="43.5" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="7" font-weight="800" fill="#fff">ESP32</text><circle cx="76" cy="36" r="2.5" fill="#F59E0B"/><circle cx="82" cy="46" r="2.5" fill="#84CC16"/>`,
};
const CANECAS = `<span class="tc-canecas" aria-hidden="true">${['verde', 'blanca', 'negra'].map((c) => `<img src="assets/estacion/caneca-${c}.webp" alt="" width="104" height="131" loading="lazy">`).join('')}</span>`;

// Flujo real: qué hace cada pieza (fuentes: páginas Integraciones, IA, Base de datos y Hardware de esta web).
const FLUJO = [
  { id: 'web', t: 'Web', r: 'Interfaz principal de usuarios', d: 'Esta página y Dos Clics identifican el residuo por texto o voz con el catálogo del navegador. Con el QR de la estación piden la apertura a la pasarela HTTPS.', ruta: 'integraciones' },
  { id: 'telegram', t: 'Telegram', r: 'Foto, consulta e historial', d: 'El bot @EcoAccessP3_JG_bot recibe la foto del residuo, responde con la caneca y muestra tu historial; la apertura desde Telegram solo es para cuentas autorizadas.', ruta: 'integraciones' },
  { id: 'ia', t: 'Inteligencia artificial', r: 'Sugiere el material de la foto', d: 'El modelo de visión lfm3-vl, en Ollama y en el equipo de la estación, sugiere el material con un vocabulario cerrado. La caneca y la apertura las deciden reglas, nunca el modelo.', ruta: 'ia' },
  { id: 'oracle', t: 'Oracle', r: 'Registros, usuarios y estadísticas', d: 'Oracle Autonomous Database en Oracle Cloud guarda usuarios, depósitos, ecopuntos y estadísticas; cada operación queda registrada sin depósitos ni puntos ficticios.', ruta: 'datos' },
  { id: 'esp32', t: 'ESP32', r: 'Mueve el servo de cada tapa', d: 'Recibe la orden del controlador central por USB y mueve por PWM el servo de la caneca indicada. No tiene sensores: el sistema sabe que ordenó abrir, no que el residuo entró.', ruta: 'hardware' },
  { id: 'canecas', t: 'Canecas físicas', r: 'Abren la tapa correcta', d: 'Verde, blanca y negra: la tapa indicada se abre 7 s (14 o 21 s si son dos o tres canecas) y se cierra sola.', ruta: 'hardware' },
];

export function render() {
  return `<div class="tecnologia">
  <section class="cn-hero tc-hero" aria-labelledby="tcTitulo">
    <div class="cn-hero-texto">
      <nav aria-label="Ruta de navegación"><ol class="cn-migas">
        <li><a href="#/">${ico(I.casa)}Inicio</a></li><li aria-current="page">Tecnología</li>
      </ol></nav>
      <h1 id="tcTitulo" tabindex="-1">Tecnología que impulsa EcoAccess</h1>
      <p class="cn-entrada">Conjunto de tecnologías, metodologías e integraciones que permiten identificar, clasificar y gestionar residuos de forma inteligente, conectando usuarios, datos y canecas físicas en un campus más sostenible.</p>
      <div class="hero-acciones">
        <a class="boton boton-principal" href="#/tecnologia/arquitectura">${ico(I.capas)}Ver arquitectura${ico(I.flecha, 'class="flecha"')}</a>
        <a class="boton boton-secundario" href="${GH}" target="_blank" rel="noopener">${ico(I.github)}Ver repositorio<span class="sr"> (abre GitHub en otra pestaña)</span></a>
      </div>
    </div>
    <figure class="cn-hero-arte">
      <img src="assets/conoce/tecnologia-hero.webp" width="619" height="212" alt="Eco junto a un portátil con código, un servidor y la nube, frente a la Universidad Popular del Cesar.">
    </figure>
  </section>

  <section class="tc-temas" aria-label="Temas de tecnología">
    ${TEMAS.map(([r, t, i, d]) => `<a class="tarjeta-acceso tc-tarjeta" href="#/tecnologia/${r}">
      <span class="icono-circulo tc-icono" aria-hidden="true">${ico(i)}</span>
      <span class="tarjeta-texto"><strong>${t}</strong><span>${d}</span></span>
      <span class="tarjeta-ir" aria-hidden="true">${ico(I.flecha)}</span>
    </a>`).join('')}
    <div class="tarjeta-reto tc-destacada">
      <div class="reto-texto">
        <p class="reto-titulo">${ico(I.hoja)}Arquitectura de la solución</p>
        <p class="reto-desc">Conoce el flujo completo de tecnologías y cómo se integran.</p>
        <a class="boton boton-claro" href="#/tecnologia/arquitectura">Ver diagrama${ico(I.flechaLarga, 'class="flecha"')}</a>
      </div>
      <svg class="tc-capas" viewBox="0 0 120 110" aria-hidden="true"><path d="M60 64 104 78 60 92 16 78z" fill="#16A34A" opacity=".55"/><path d="M60 48 104 62 60 76 16 62z" fill="#22C55E" opacity=".75"/><path d="M60 32 104 46 60 60 16 46z" fill="#86EFAC"/><path d="M60 32 104 46 60 60 16 46z" fill="none" stroke="#D9F99D" stroke-width="2"/><path d="M60 30c0-10 6-17 16-18-1 10-7 16-16 18z" fill="#4ADE80"/><path d="M60 30c0-8-5-13-12-14 1 8 5 12 12 14z" fill="#84CC16"/><path d="M60 30V40" stroke="#166534" stroke-width="2"/></svg>
    </div>
  </section>

  <section class="tc-flujo" aria-labelledby="tcFlujo">
    <div class="tc-flujo-cabecera">
      <h2 id="tcFlujo"><span aria-hidden="true">${ico(I.red)}</span>Cómo se integra EcoAccess</h2>
      <p>Flujo de tecnologías desde la persona hasta la caneca y el registro. Toca cada pieza para ver qué hace de verdad.</p>
      <button type="button" class="boton boton-secundario tc-recorrer" id="tcRecorrer">${ico(I.play)}<span>Ver el recorrido</span></button>
    </div>
    <ol class="tc-pasos" role="list">
      ${FLUJO.map((p, n) => `<li><button type="button" class="tc-paso" data-paso="${n}" aria-pressed="${n === 0}" aria-controls="tcDetalle">
        ${p.id === 'canecas' ? `<span class="tc-ilu tc-ilu-canecas">${CANECAS}<svg viewBox="0 0 120 90" aria-hidden="true">${PEDESTAL}</svg></span>` : `<svg class="tc-ilu" viewBox="0 0 120 90" aria-hidden="true">${ILU[p.id]}</svg>`}
        <strong>${p.t}</strong><span>${p.r}</span>
      </button></li>`).join('')}
    </ol>
    <div class="tc-detalle" id="tcDetalle" aria-live="polite"></div>
  </section>

  <section class="bloque cn-bloque" aria-labelledby="t-sellos">
    <h2 id="t-sellos">Cómo leer los sellos</h2>
    <p>Cada página de tecnología cita los archivos del repositorio y marca qué está verificado y qué no.</p>
    ${leyenda()}
  </section>
</div>`;
}

export function montar(raiz) {
  const pasos = [...raiz.querySelectorAll('.tc-paso')];
  const detalle = raiz.querySelector('#tcDetalle');
  const boton = raiz.querySelector('#tcRecorrer');
  let temporizador = 0;
  const elegir = (n) => {
    pasos.forEach((b, k) => { b.setAttribute('aria-pressed', String(k === n)); b.classList.toggle('pasado', k < n); });
    const p = FLUJO[n];
    detalle.innerHTML = `<p class="tc-detalle-paso">${n + 1} de ${FLUJO.length}</p><h3>${p.t}</h3><p>${p.d}</p><a class="boton boton-texto" href="#/tecnologia/${p.ruta}">Ver la página de ${p.ruta === 'datos' ? 'Base de datos' : p.ruta === 'ia' ? 'Inteligencia artificial' : p.ruta === 'hardware' ? 'Hardware' : 'Integraciones'}${ico(I.flechaLarga, 'class="flecha"')}</a>`;
  };
  const detener = () => { clearInterval(temporizador); temporizador = 0; boton.querySelector('span').textContent = 'Ver el recorrido'; };
  pasos.forEach((b, n) => b.addEventListener('click', () => { detener(); elegir(n); }));
  // «Ver el recorrido»: resalta pieza por pieza (2,5 s cada una); con movimiento reducido avanza solo al pulsar.
  boton.addEventListener('click', () => {
    if (temporizador) { detener(); return; }
    let n = 0; elegir(0);
    if (movimientoReducido()) return;
    boton.querySelector('span').textContent = 'Detener recorrido';
    temporizador = setInterval(() => { n++; if (n >= FLUJO.length) { detener(); return; } elegir(n); }, 2500);
  });
  elegir(0);
  return detener;
}
