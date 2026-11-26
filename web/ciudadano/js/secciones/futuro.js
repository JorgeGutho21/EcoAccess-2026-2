// Visión a futuro. Diseño: maqueta «Visión a futuro» + regla .claude/skills/eco-diseno-web.
// Todo lo de esta página son PROPUESTAS: nada está instalado ni probado. Cada tarjeta dice desde dónde se parte hoy
// y cuál sería el siguiente paso. No se promete impacto: el proyecto no lo ha medido.
import { sello, escapar, movimientoReducido } from './comun.js';

export const titulo = 'Visión a futuro';

const ico = (d, extra = '') => `<svg viewBox="0 0 24 24" aria-hidden="true" ${extra}>${d}</svg>`;
const I = {
  casa: '<path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10v10h13V10"/>',
  hoja: '<path d="M5 19c0-8 5-13 15-14-1 10-6 15-14 15"/><path d="M5 19c3-4 6-7 10-9"/>',
  flecha: '<path d="m9 6 6 6-6 6"/>',
  barras: '<path d="M5 20v-5M10 20v-9M15 20v-12M20 20V5"/>',
  calendario: '<rect x="3" y="5" width="18" height="16" rx="2"/><path d="M3 10h18M8 3v4M16 3v4"/>',
  diana: '<circle cx="12" cy="12" r="9"/><circle cx="12" cy="12" r="5"/><circle cx="12" cy="12" r="1.5"/>',
  cohete: '<path d="M5 15c-1.5 1.3-2 4-2 6 2 0 4.7-.5 6-2M9 15l-3-3c1-4 4-8 11-9-1 7-5 10-9 11z"/><circle cx="14.5" cy="9.5" r="1.6"/>',
};

// Ilustraciones vectoriales de cada propuesta (viewBox 140×100).
const LUZ = '<ellipse cx="70" cy="90" rx="44" ry="7" fill="#DDEFD9"/>';
const ILU = {
  tapa: `${LUZ}<rect x="40" y="38" width="56" height="50" rx="8" fill="#16A34A"/><rect x="44" y="44" width="48" height="6" rx="3" fill="#86EFAC"/><path d="M38 38c2-14 16-24 34-24" stroke="#0B5E34" stroke-width="7" stroke-linecap="round" fill="none"/><ellipse cx="68" cy="38" rx="30" ry="6" fill="#0B5E34"/><path d="M104 22a24 24 0 0 1 6 26" stroke="#22C55E" stroke-width="4" fill="none" stroke-linecap="round"/><path d="m104 50 6-2 2 6" stroke="#22C55E" stroke-width="4" fill="none" stroke-linecap="round" stroke-linejoin="round"/><circle cx="68" cy="66" r="9" fill="#D9F99D"/>`,
  deposito: `${LUZ}<rect x="38" y="22" width="64" height="66" rx="10" fill="#1E293B"/><rect x="46" y="30" width="48" height="36" rx="6" fill="#334155"/><path d="M50 34h8M50 34v8M90 34h-8M90 34v8M50 62h8M50 62v-8M90 62h-8M90 62v-8" stroke="#86EFAC" stroke-width="3" stroke-linecap="round"/><rect x="64" y="34" width="12" height="26" rx="5" fill="#BAE6FD" stroke="#7DD3FC" stroke-width="1.5" transform="rotate(25 70 47)"/><path d="M58 72h24l4 12H54z" fill="#22C55E"/><text x="70" y="82" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="8" font-weight="800" fill="#073E23">kg</text>`,
  llenado: `${LUZ}<rect x="42" y="30" width="56" height="58" rx="8" fill="#CBD5E1" stroke="#94A3B8" stroke-width="2"/><rect x="48" y="58" width="44" height="24" rx="4" fill="#22C55E"/><rect x="48" y="48" width="44" height="8" rx="3" fill="#86EFAC"/><path d="M42 30h56" stroke="#64748B" stroke-width="4" stroke-linecap="round"/><path d="M86 14a14 14 0 0 1 14 0M82 9a22 22 0 0 1 22 0" stroke="#22C55E" stroke-width="3.5" fill="none" stroke-linecap="round"/><circle cx="93" cy="20" r="2.5" fill="#22C55E"/>`,
  estaciones: `${LUZ}<path d="M28 30h84" stroke="#0B5E34" stroke-width="6" stroke-linecap="round"/><path d="M34 30v58M106 30v58" stroke="#475569" stroke-width="4"/><rect x="42" y="56" width="16" height="30" rx="3" fill="#16A34A"/><rect x="62" y="56" width="16" height="30" rx="3" fill="#E2E8F0" stroke="#94A3B8" stroke-width="1.5"/><rect x="82" y="56" width="16" height="30" rx="3" fill="#1E293B"/><rect x="76" y="34" width="18" height="18" rx="2" fill="#fff" stroke="#0F172A" stroke-width="1.5"/><path d="M79 37h5v5h-5zM87 37h4v4h-4zM79 45h4v4h-4zM86 45h5v4h-5z" fill="#0F172A"/><circle cx="118" cy="70" r="12" fill="#4CAF3F"/><path d="M118 82v8" stroke="#475569" stroke-width="3"/>`,
  analitica: `${LUZ}<rect x="28" y="22" width="84" height="54" rx="6" fill="#0F3D24"/><rect x="34" y="28" width="72" height="42" rx="3" fill="#F2FBF3"/><rect x="40" y="52" width="8" height="14" rx="1.5" fill="#86EFAC"/><rect x="52" y="44" width="8" height="22" rx="1.5" fill="#4ADE80"/><rect x="64" y="36" width="8" height="30" rx="1.5" fill="#16A34A"/><path d="M40 46 52 38l12-4" stroke="#0B5E34" stroke-width="2.5" fill="none" stroke-linecap="round"/><circle cx="90" cy="46" r="11" fill="#E6F4E4"/><path d="M90 46V35a11 11 0 0 1 10.4 14.6z" fill="#16A34A"/><path d="M22 78h96l-6 8H28z" fill="#CBD5E1" stroke="#94A3B8" stroke-width="1.5" stroke-linejoin="round"/>`,
  accesibilidad: `${LUZ}<rect x="34" y="14" width="40" height="72" rx="8" fill="#1E293B"/><rect x="38" y="20" width="32" height="58" rx="4" fill="#F2FBF3"/><rect x="44" y="32" width="20" height="20" rx="2" fill="#fff" stroke="#0F172A" stroke-width="1.5"/><path d="M47 35h5v5h-5zM56 35h5v5h-5zM47 44h5v5h-5zM56 44h5v5h-5z" fill="#0F172A"/><path d="M46 62c0-6 4-9 10-10-1 6-4 9-10 10z" fill="#22C55E"/><rect x="72" y="46" width="44" height="30" rx="5" fill="#fff" stroke="#16A34A" stroke-width="2.5"/><text x="102" y="68" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="9" font-weight="800" fill="#0B5E34">UPC</text><path d="M78 56c0-4 3-6 7-7-1 4-3 6-7 7z" fill="#22C55E"/><path d="M100 30a10 10 0 0 1 10 0M96 25a17 17 0 0 1 18 0" stroke="#22C55E" stroke-width="3" fill="none" stroke-linecap="round"/>`,
};

// Seis propuestas principales (las de la maqueta) y su fase en la hoja de ruta.
const PRINCIPALES = [
  { id: 'tapa', fase: 'corto', t: 'Sensores de posición de la tapa', d: 'Un final de carrera o un sensor angular por tapa permitiría decir «la tapa se abrió», y no solo «se ordenó abrir».', hoy: 'No hay ningún sensor: toda apertura es comandada.', paso: 'Integrar un final de carrera o un sensor angular en una caneca.' },
  { id: 'deposito', fase: 'corto', t: 'Confirmación física del depósito', d: 'Un sensor de paso o una celda de carga confirmaría que el residuo entró, para registrar depósitos verificados y no solo declarados.', hoy: 'El modelo de datos ya prevé un depósito verificado y un peso medido (columnas anulables de DEPOSITO).', paso: 'Instalar un sensor de paso o una celda de carga y conectarlo al ESP32.' },
  { id: 'llenado', fase: 'corto', t: 'Sensores de llenado', d: 'Medir el nivel de cada caneca para avisar al operario antes de que se llene.', hoy: 'No existe en el hardware actual.', paso: 'Integrar un sensor de nivel (ultrasónico o ToF) y definir alertas de llenado.' },
  { id: 'estaciones', fase: 'mediano', t: 'Estaciones distribuidas', d: 'Varias estaciones en el campus, cada una con su QR y su ESP32, reportando a la misma base.', hoy: 'Las tablas PUNTO_INSTALACION y CONTENEDOR ya admiten varios puntos; falta el despliegue.', paso: 'Instalar un piloto en otra zona del campus y validar la operación multisitio.' },
  { id: 'analitica', fase: 'mediano', t: 'Analítica avanzada', d: 'Análisis de patrones, horarios y tipos de residuo para planear rutas de recolección, capacidad y campañas de reciclaje.', hoy: 'La app de escritorio ya muestra estadísticas y un tablero institucional; no hay análisis de patrones.', paso: 'Construir tableros y modelos de análisis sobre los registros de Oracle.' },
  { id: 'accesibilidad', fase: 'largo', t: 'Accesibilidad extendida', d: 'Más formas de acceso y uso (por ejemplo, app móvil, carné institucional o lectura NFC) para un campus más inclusivo.', hoy: 'Se accede con el QR de la estación, la web con texto y voz, Dos Clics y Telegram.', paso: 'Prototipar el acceso con carné institucional o NFC y evaluar su integración.' },
];

// Propuestas técnicas que ya estaban en esta página (sin ilustración en la maqueta).
const OTRAS = [
  { fase: 'mediano', t: 'Backend independiente', d: 'Llevar la pasarela, el coordinador y el registro a un servidor propio, para que la estación no dependa del computador donde corre el panel.', hoy: 'Hoy la pasarela y el panel corren en el equipo de la estación.' },
  { fase: 'largo', t: 'Aplicación móvil', d: 'Una app con cámara para identificar por foto sin pasar por Telegram.', hoy: 'Hoy la foto se envía por el bot o desde la app de escritorio.' },
  { fase: 'corto', t: 'Reconocimiento más rápido y probado con fotos nuevas', d: 'Bajar la mediana de ≈ 2,4 s (P95 ≈ 5,5 s) a la meta de 1 a 2 s y evaluar con fotos nuevas, sin las que se usaron para ajustar las reglas.', hoy: 'Ver Resultados e Inteligencia artificial.' },
  { fase: 'corto', t: 'Esquema dedicado para el Challenge', d: 'Un usuario de base propio para el juego, con ORDS habilitado solo para él.', hoy: 'Recomendado en el informe del 25/11/2026; requiere la cuenta administradora.' },
  { fase: 'mediano', t: 'Interfaces más pequeñas', d: 'Separar la lectura del catálogo de su administración y sacar la configuración del coordinador.', hoy: 'Ver SOLID en Programación III.' },
];

const FASES = [
  { id: 'corto', t: 'Corto plazo', d: 'Validación de sensores clave (tapa, depósito y llenado)', i: I.cohete },
  { id: 'mediano', t: 'Mediano plazo', d: 'Despliegue multisitio, analítica y alertas operativas', i: I.calendario },
  { id: 'largo', t: 'Largo plazo', d: 'Accesibilidad extendida y optimización integral del sistema', i: I.diana },
];

export function render() {
  return `<div class="futuro">
  <section class="cn-hero fu-hero" aria-labelledby="fuTitulo">
    <div class="cn-hero-texto">
      <nav aria-label="Ruta de navegación"><ol class="cn-migas">
        <li><a href="#/">${ico(I.casa)}Inicio</a></li><li aria-current="page">Visión a futuro</li>
      </ol></nav>
      <h1 id="fuTitulo" tabindex="-1">Visión a futuro</h1>
      <p class="cn-entrada">Hacia dónde podría crecer EcoAccess. Estas son propuestas de futuro que amplían sus capacidades, mejoran la confiabilidad y facilitan su despliegue en el campus. Ninguna está construida todavía: cada una dice desde dónde se parte hoy.</p>
      <div class="hero-acciones">
        <a class="boton boton-principal" href="#/futuro" data-ir="fuRuta">${ico(I.hoja)}Ver hoja de ruta${ico(I.flecha, 'class="flecha"')}</a>
        <a class="boton boton-secundario" href="#/futuro" data-ir="fuOtras">${ico(I.barras)}Más propuestas técnicas</a>
      </div>
    </div>
    <figure class="cn-hero-arte">
      <img src="assets/conoce/futuro-hero.webp" width="626" height="167" alt="Eco señala un camino con hitos de reciclaje, configuración, analítica y comunidad hacia la Universidad Popular del Cesar.">
    </figure>
  </section>

  <section class="fu-propuestas" aria-label="Propuestas principales" id="fuPropuestas">
    ${PRINCIPALES.map((p) => `<article class="fu-tarjeta" data-fase="${p.fase}">
      <div class="fu-cab">
        ${sello('propuesta')}
        <h2>${escapar(p.t)}</h2>
        <p>${escapar(p.d)}</p>
      </div>
      <svg class="fu-ilu" viewBox="0 0 140 100" aria-hidden="true">${ILU[p.id]}</svg>
      <dl class="fu-estado">
        <div><dt>${ico(I.calendario)}Hoy:</dt><dd>${escapar(p.hoy)}</dd></div>
        <div><dt>${ico(I.diana)}Siguiente paso:</dt><dd>${escapar(p.paso)}</dd></div>
      </dl>
    </article>`).join('')}
  </section>

  <section class="fu-ruta" id="fuRuta" tabindex="-1" aria-labelledby="fuRutaTitulo">
    <h2 id="fuRutaTitulo">${ico(I.hoja)}<span>Hoja de ruta <small>(visión general)</small></span></h2>
    <ol class="fu-fases">
      ${FASES.map((f) => `<li><button type="button" class="fu-fase" data-fase="${f.id}" aria-pressed="false" aria-controls="fuPropuestas fuOtrasLista">
        <span class="icono-circulo" aria-hidden="true">${ico(f.i)}</span><span><strong>${f.t}</strong><span>${f.d}</span></span>
      </button></li>`).join('')}
    </ol>
    <p class="fu-ayuda" id="fuAyuda" aria-live="polite">Toca una fase para resaltar sus propuestas.</p>
  </section>

  <section class="bloque cn-bloque" id="fuOtras" tabindex="-1" aria-labelledby="fuOtrasTitulo">
    <h2 id="fuOtrasTitulo">Más propuestas técnicas</h2>
    <div class="rejilla" id="fuOtrasLista">
      ${OTRAS.map((p) => `<div class="dato fu-otra" data-fase="${p.fase}" style="--c:var(--e-propuesta)">
        ${sello('propuesta')}
        <h3>${escapar(p.t)}</h3>
        <p>${escapar(p.d)}</p>
        <p class="nota-fuente"><strong>Hoy:</strong> ${escapar(p.hoy)}</p>
      </div>`).join('')}
    </div>
  </section>
</div>`;
}

export function montar(raiz) {
  const fases = [...raiz.querySelectorAll('.fu-fase')];
  const items = [...raiz.querySelectorAll('.fu-tarjeta, .fu-otra')];
  const ayuda = raiz.querySelector('#fuAyuda');
  fases.forEach((b) => b.addEventListener('click', () => {
    const activa = b.getAttribute('aria-pressed') === 'true';
    fases.forEach((x) => x.setAttribute('aria-pressed', String(!activa && x === b)));
    const f = activa ? null : b.dataset.fase;
    items.forEach((n) => n.classList.toggle('atenuada', !!f && n.dataset.fase !== f));
    const n = f ? items.filter((x) => x.dataset.fase === f).length : 0;
    ayuda.textContent = f ? `${b.querySelector('strong').textContent}: ${n} propuestas resaltadas. Toca otra vez para ver todas.` : 'Toca una fase para resaltar sus propuestas.';
    if (f) raiz.querySelector(`.fu-tarjeta[data-fase="${f}"]`)?.scrollIntoView({ behavior: movimientoReducido() ? 'auto' : 'smooth', block: 'nearest' });
  }));
  raiz.querySelectorAll('[data-ir]').forEach((a) => a.addEventListener('click', (ev) => {
    ev.preventDefault();
    const d = raiz.querySelector('#' + a.dataset.ir);
    d?.scrollIntoView({ behavior: movimientoReducido() ? 'auto' : 'smooth', block: 'start' });
    d?.focus({ preventScroll: true });
  }));
}
