// Cómo se construyó. Diseño: maqueta «Cómo se construyó» + regla .claude/skills/eco-diseno-web.
// Fechas y commits tomados de la historia de git del repositorio (git log) y de los documentos citados; los enlaces
// van al repositorio público. Las ilustraciones de las etapas son vectoriales (nítidas en cualquier pantalla).
import { sello, escapar, movimientoReducido } from './comun.js';

export const titulo = 'Cómo se construyó';

const GH = 'https://github.com/JorgeGutho21/EcoAccess-2026-2';
const commit = (h) => `${GH}/commit/${h}`;
const arbol = (p) => `${GH}/tree/main/${p}`;
const archivo = (p) => `${GH}/blob/main/${p}`;

const ico = (d, extra = '') => `<svg viewBox="0 0 24 24" aria-hidden="true" ${extra}>${d}</svg>`;
const I = {
  casa: '<path d="M3 11.5 12 4l9 7.5"/><path d="M5.5 10v10h13V10"/>',
  analisis: '<path d="M14 3H6v18h12V7z"/><path d="M14 3v4h4M9 12h6M9 16h4"/>',
  diseno: '<path d="m4 20 4-1 11-11-3-3L5 16z"/><path d="m14 6 3 3"/>',
  codigo: '<path d="m8 8-4 4 4 4M16 8l4 4-4 4M14 5l-4 14"/>',
  pruebas: '<circle cx="12" cy="12" r="9"/><path d="m8 12 3 3 5-6"/>',
  git: '<circle cx="6" cy="6" r="2.2"/><circle cx="6" cy="18" r="2.2"/><circle cx="18" cy="9" r="2.2"/><path d="M6 8.2v7.6M18 11.2c0 3-3 3.8-9.8 5.6"/>',
  base: '<ellipse cx="12" cy="6" rx="7" ry="3"/><path d="M5 6v6c0 1.7 3.1 3 7 3s7-1.3 7-3V6M5 12v6c0 1.7 3.1 3 7 3s7-1.3 7-3v-6"/>',
  chip: '<rect x="7" y="7" width="10" height="10" rx="1.5"/><path d="M10 3v4M14 3v4M10 17v4M14 17v4M3 10h4M3 14h4M17 10h4M17 14h4"/>',
  ia: '<path d="M12 3v4M12 17v4M3 12h4M17 12h4"/><path d="m12 8 1.2 2.8L16 12l-2.8 1.2L12 16l-1.2-2.8L8 12l2.8-1.2z"/>',
  web: '<rect x="3" y="4" width="18" height="13" rx="2"/><path d="M8 21h8M12 17v4"/>',
  lupa: '<circle cx="10.5" cy="10.5" r="6.5"/><path d="m20 20-4.8-4.8"/>',
  escudo: '<path d="M12 3 5 6v5c0 4.6 3 8.3 7 10 4-1.7 7-5.4 7-10V6z"/><path d="m9 12 2 2 4-4"/>',
  barras: '<path d="M5 20v-5M10 20v-9M15 20v-12M20 20V5"/>',
  hoja: '<path d="M5 19c0-8 5-13 15-14-1 10-6 15-14 15"/><path d="M5 19c3-4 6-7 10-9"/>',
  ir: '<path d="M7 17 17 7M9 7h8v8"/>',
  chevron: '<path d="m6 9 6 6 6-6"/>',
  doc: '<path d="M14 3H6v18h12V7z"/><path d="M14 3v4h4"/>',
  carpeta: '<path d="M3 7a2 2 0 0 1 2-2h4l2 2h8a2 2 0 0 1 2 2v8a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2z"/>',
  github: '<path d="M9 19c-4 1.2-4-2-6-2.5M15 21v-3.5c0-1 .1-1.4-.5-2 2.8-.3 5.5-1.4 5.5-6a4.6 4.6 0 0 0-1.3-3.2 4.3 4.3 0 0 0-.1-3.2s-1.1-.3-3.5 1.3a12 12 0 0 0-6.2 0C6.5 2.8 5.4 3.1 5.4 3.1a4.3 4.3 0 0 0-.1 3.2A4.6 4.6 0 0 0 4 9.5c0 4.6 2.7 5.7 5.5 6-.6.6-.6 1.2-.5 2V21"/>',
  imagen: '<rect x="3" y="4" width="18" height="16" rx="2"/><circle cx="9" cy="10" r="2"/><path d="m21 16-5-5-9 9"/>',
  capas: '<path d="m12 3 9 5-9 5-9-5z"/><path d="m3 13 9 5 9-5"/>',
};

// Ilustraciones vectoriales de cada etapa (viewBox 120×96): fondo circular suave, objeto y hojas.
const H = '<path d="M12 84c2-14 10-22 24-24-2 14-10 22-24 24z" fill="#4CAF3F"/><path d="M14 82q10-10 20-20" stroke="#D9F99D" stroke-width="1.6" fill="none"/><path d="M100 86c-1-12-8-19-20-20 1 12 8 19 20 20z" fill="#6CC24A"/>';
const FONDO = '<circle cx="60" cy="50" r="40" fill="#E6F4E4"/>';
const ILU = {
  1: `${FONDO}${H}<rect x="38" y="22" width="40" height="54" rx="5" fill="#fff" stroke="#0B5E34" stroke-width="3"/><rect x="50" y="17" width="16" height="9" rx="3" fill="#16A34A"/><path d="m45 38 3 3 5-6M45 50l3 3 5-6M45 62l3 3 5-6" stroke="#16A34A" stroke-width="3" fill="none" stroke-linecap="round" stroke-linejoin="round"/><path d="M58 38h12M58 50h12M58 62h7" stroke="#94A3B8" stroke-width="3" stroke-linecap="round"/><circle cx="80" cy="66" r="11" fill="#fff" fill-opacity=".85" stroke="#0B5E34" stroke-width="3.5"/><path d="m88 74 9 9" stroke="#0B5E34" stroke-width="5" stroke-linecap="round"/>`,
  2: `${FONDO}${H}<rect x="36" y="20" width="44" height="56" rx="5" fill="#fff" stroke="#0B5E34" stroke-width="3"/><path d="M44 32h28M44 41h28M44 50h20M44 59h24" stroke="#94A3B8" stroke-width="3" stroke-linecap="round"/><rect x="44" y="64" width="16" height="6" rx="3" fill="#A7E27D"/><path d="m74 70 14-30 6 3-14 30-7 3z" fill="#16A34A" stroke="#0B5E34" stroke-width="2.5" stroke-linejoin="round"/>`,
  3: `${FONDO}${H}<rect x="44" y="18" width="32" height="16" rx="4" fill="#16A34A"/><rect x="28" y="54" width="28" height="16" rx="4" fill="#fff" stroke="#0B5E34" stroke-width="3"/><rect x="64" y="54" width="28" height="16" rx="4" fill="#fff" stroke="#0B5E34" stroke-width="3"/><path d="M60 34v10M42 54v-10h36v10" stroke="#0B5E34" stroke-width="3" fill="none" stroke-linejoin="round"/><path d="M50 26h20" stroke="#D9F99D" stroke-width="3" stroke-linecap="round"/><path d="M34 62h16M70 62h16" stroke="#A7E27D" stroke-width="3" stroke-linecap="round"/>`,
  4: `${FONDO}${H}<rect x="30" y="24" width="52" height="36" rx="4" fill="#0F3D24" stroke="#0B5E34" stroke-width="3"/><path d="m47 36-6 6 6 6M65 36l6 6-6 6M59 33l-6 18" stroke="#A7E27D" stroke-width="3" fill="none" stroke-linecap="round" stroke-linejoin="round"/><path d="M24 64h64l-6 8H30z" fill="#CBD5E1" stroke="#64748B" stroke-width="2" stroke-linejoin="round"/><circle cx="88" cy="58" r="11" fill="#84CC16" stroke="#0B5E34" stroke-width="3"/><circle cx="88" cy="58" r="4" fill="#fff"/><path d="M88 44v4M88 68v4M74 58h4M98 58h4M78 48l3 3M95 65l3 3M98 48l-3 3M81 65l-3 3" stroke="#0B5E34" stroke-width="3" stroke-linecap="round"/>`,
  5: `${FONDO}${H}<ellipse cx="60" cy="26" rx="22" ry="8" fill="#16A34A"/><path d="M38 26v14c0 4.4 9.8 8 22 8s22-3.6 22-8V26" fill="#22C55E"/><path d="M38 40v14c0 4.4 9.8 8 22 8s22-3.6 22-8V40" fill="#16A34A"/><path d="M38 54v12c0 4.4 9.8 8 22 8s22-3.6 22-8V54" fill="#0B5E34"/><ellipse cx="60" cy="26" rx="22" ry="8" fill="none" stroke="#073E23" stroke-width="2.5"/><circle cx="72" cy="44" r="2.5" fill="#D9F99D"/><circle cx="72" cy="58" r="2.5" fill="#D9F99D"/><circle cx="72" cy="70" r="2.5" fill="#D9F99D"/>`,
  6: `${FONDO}${H}<path d="M76 18a16 16 0 0 1 12 12M78 10a24 24 0 0 1 18 18" stroke="#16A34A" stroke-width="3" fill="none" stroke-linecap="round"/><rect x="34" y="30" width="44" height="40" rx="5" fill="#1E293B" stroke="#0F172A" stroke-width="2.5"/><path d="M40 26v4M48 26v4M56 26v4M64 26v4M72 26v4M40 70v4M48 70v4M56 70v4M64 70v4M72 70v4M30 38h4M30 46h4M30 54h4M30 62h4M78 38h4M78 46h4M78 54h4M78 62h4" stroke="#CBD5E1" stroke-width="2.5" stroke-linecap="round"/><text x="56" y="54" text-anchor="middle" font-family="Segoe UI, Arial, sans-serif" font-size="10" font-weight="800" fill="#fff">ESP32</text><circle cx="42" cy="38" r="2" fill="#84CC16"/>`,
  7: `${FONDO}${H}<rect x="30" y="26" width="50" height="40" rx="6" fill="#fff" stroke="#0B5E34" stroke-width="3"/><circle cx="55" cy="46" r="11" fill="#E6F4E4" stroke="#16A34A" stroke-width="3"/><circle cx="55" cy="46" r="4.5" fill="#0B5E34"/><rect x="38" y="20" width="12" height="7" rx="2" fill="#16A34A"/><path d="m86 22 2.6 6 6 2.6-6 2.6L86 39l-2.6-5.8-6-2.6 6-2.6z" fill="#84CC16"/><path d="m92 46 1.5 3.4 3.4 1.5-3.4 1.5L92 56l-1.5-3.6-3.4-1.5 3.4-1.5z" fill="#16A34A"/>`,
  8: `${FONDO}${H}<rect x="24" y="22" width="72" height="50" rx="6" fill="#fff" stroke="#0B5E34" stroke-width="3"/><path d="M24 32h72" stroke="#0B5E34" stroke-width="3"/><circle cx="31" cy="27" r="1.8" fill="#EF4444"/><circle cx="37" cy="27" r="1.8" fill="#F59E0B"/><circle cx="43" cy="27" r="1.8" fill="#22C55E"/><rect x="36" y="40" width="13" height="24" rx="3" fill="#22A33A"/><rect x="53" y="40" width="13" height="24" rx="3" fill="#E2E8F0" stroke="#94A3B8" stroke-width="1.5"/><rect x="70" y="40" width="13" height="24" rx="3" fill="#1E293B"/><path d="M39 45h7M56 45h7M73 45h7" stroke="#D9F99D" stroke-width="2" stroke-linecap="round"/>`,
  9: `${FONDO}${H}<path d="M60 16 36 25v17c0 15.5 10.2 28 24 33 13.8-5 24-17.5 24-33V25z" fill="#16A34A" stroke="#0B5E34" stroke-width="3" stroke-linejoin="round"/><path d="m48 45 8 8 16-18" stroke="#fff" stroke-width="5" fill="none" stroke-linecap="round" stroke-linejoin="round"/>`,
  10: `${FONDO}${H}<path d="M32 72h56" stroke="#0B5E34" stroke-width="3" stroke-linecap="round"/><rect x="38" y="52" width="10" height="20" rx="2" fill="#A7E27D"/><rect x="53" y="42" width="10" height="30" rx="2" fill="#4CAF3F"/><rect x="68" y="30" width="10" height="42" rx="2" fill="#16A34A"/><path d="M40 44 55 34l13 4 16-16" stroke="#0B5E34" stroke-width="3" fill="none" stroke-linecap="round" stroke-linejoin="round"/><path d="M76 20h9v9" stroke="#0B5E34" stroke-width="3" fill="none" stroke-linecap="round" stroke-linejoin="round"/>`,
};
const ilustracion = (n) => `<svg class="ck-ilu" viewBox="0 0 120 96" aria-hidden="true">${ILU[n]}</svg>`;

// Diez etapas. `tag`: fase (también filtra desde las etiquetas del encabezado). Evidencia verificada con git.
const ETAPAS = [
  { t: 'Problema', tag: 'Análisis', i: I.analisis, f: '01/11/2026', d: 'Se definió el problema (no saber en qué caneca va cada residuo), el objetivo general, el alcance y las limitaciones honestas: sin sensor de ingreso no se puede afirmar un depósito.',
    enlaces: [[I.git, 'Ver evidencia (Git)', commit('bdd8c8b')], [I.carpeta, 'Documento', archivo('docs/introduccion/00_presentacion.md')]] },
  { t: 'Análisis', tag: 'Análisis', i: I.lupa, f: '01/11 – 11/11', d: 'Las reglas de negocio (RN-01, RN-07, RN-16…) quedaron escritas en el diccionario de datos y en las entidades con sus pruebas. El 11/11 se consolidó una matriz de requisitos en el documento maestro.',
    enlaces: [[I.git, 'Ver commits', commit('890ec74')], [I.doc, 'Diccionario de datos', archivo('database/01_modelo/DICCIONARIO_DATOS.md')]] },
  { t: 'Diseño', tag: 'Diseño', i: I.diseno, f: '01/11 – 09/11', d: 'Arquitectura por capas: Entidades, Lógica (contratos y servicios), Datos y Presentación. El 09/11 se publicaron los diagramas UML: clases, paquetes, componentes, secuencia, casos de uso, despliegue, entidad-relación y flujos.',
    enlaces: [[I.git, 'Ver commits', commit('a78e10d')], [I.imagen, 'Diagramas', arbol('docs/academico/diagramas')], [I.capas, 'Arquitectura', '#/tecnologia/arquitectura']] },
  { t: 'Implementación', tag: 'Implementación', i: I.codigo, f: '01/11/2026', d: 'Se desarrolló la aplicación de escritorio en Windows Forms para el operario (registro asistido, catálogo, estadísticas y control de la estación) y el bot de Telegram que recibe la foto del residuo y responde con la caneca. El código se gestionó en el repositorio Git por ramas y solicitudes de cambio.',
    enlaces: [[I.git, 'Ver commits', commit('88daf52')], [I.github, 'Código (GitHub)', arbol('src')]] },
  { t: 'Base de datos', tag: 'Oracle', i: I.base, f: '01/11 – 25/11', d: 'Persistencia Oracle con migraciones reproducibles numeradas. El 09/11 se validó la conexión a Oracle Cloud con wallet. Hoy van de V002 a V035: V030 a V033 cubren la estación y el Challenge y están en producción (V033 desde el 25/11/2026); V034 y V035 están probadas en un esquema aislado y sin aplicar.',
    enlaces: [[I.git, 'Ver commits', commit('3d35793')], [I.carpeta, 'Migraciones', arbol('database/06_migraciones')], [I.base, 'Base de datos', '#/tecnologia/datos']] },
  { t: 'Dispositivo (ESP32)', tag: 'ESP32', i: I.chip, f: '01/11 – 24/11', d: 'Del piloto de una tapa con dos servos se pasó el 11/11 a una estación de tres canecas con un ESP32 y un servo por tapa. El 24/11 se agregó un panel local para controlar y calibrar las tres tapas.',
    enlaces: [[I.codigo, 'Ver código', arbol('hardware')], [I.imagen, 'Esquema', '#/tecnologia/hardware']] },
  { t: 'IA', tag: 'IA', i: I.ia, f: '02/11 – 25/11', d: 'Se reemplazó el modelo inicial por lfm3-vl con un vocabulario cerrado de materiales y se evaluó con fotos reales. La decisión de abrir la toman reglas, no el modelo; las limitaciones medidas siguen abiertas.',
    enlaces: [[I.git, 'Ver commits', commit('be5bbd0')], [I.carpeta, 'Evaluaciones', arbol('docs/vision')], [I.ia, 'Inteligencia artificial', '#/tecnologia/ia']] },
  { t: 'Integración web', tag: 'Integración web', i: I.web, f: '25/11 – 26/11', d: 'Esta página: consulta por texto o voz en el navegador y apertura física solo con el QR de la estación, a través de una pasarela HTTPS. El 25/11 se publicó también el EcoAccess Challenge y el 26/11 la web y Dos Clics abrieron las tres canecas desde el anfitrión.',
    enlaces: [[I.git, 'Ver commits', commit('ab37d8d')], [I.github, 'Código (GitHub)', arbol('web/ciudadano')]] },
  { t: 'Pruebas', tag: 'Pruebas', i: I.escudo, f: '02/11 – 26/11', d: 'Integración continua en GitHub Actions desde el 02/11: compilación Debug y Release y pruebas que no requieren Oracle, Telegram ni hardware. El 26/11 se validaron físicamente las tres canecas desde el controlador, la aplicación y la web.',
    enlaces: [[I.git, 'Ver commits', commit('04d1349')], [I.doc, 'Flujo de CI', archivo('.github/workflows/compilacion.yml')]] },
  { t: 'Cierre y resultados', tag: 'Git', i: I.barras, f: '26/11/2026', d: 'Integración definitiva por solicitudes de cambio (#28: servicios, Telegram, ESP32, Oracle y control remoto; #29: web y Dos Clics con las canecas físicas) y un informe final con cada resultado y cada limitación.',
    enlaces: [[I.git, 'Ver commits', commit('ff10d43')], [I.doc, 'Informe final', archivo('reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md')], [I.barras, 'Resultados', '#/resultados']] },
];
const ABIERTAS = [0, 3, 5];   // como en la maqueta: Problema, Implementación y Dispositivo
const FILTROS = [['Análisis', I.analisis], ['Diseño', I.diseno], ['Implementación', I.codigo], ['Pruebas', I.pruebas], ['Git', I.git], ['Oracle', I.base], ['ESP32', I.chip], ['IA', I.ia], ['Integración web', I.web]];

const enlace = ([icono, texto, href]) => {
  const externo = href.startsWith('http');
  return `<a class="ck-enlace" href="${href}"${externo ? ' target="_blank" rel="noopener"' : ''}>${ico(icono)}<span>${escapar(texto)}</span>${externo ? ico(I.ir, 'class="ck-ext"') + '<span class="sr"> (abre GitHub en otra pestaña)</span>' : ''}</a>`;
};

export function render() {
  return `<div class="construccion">
  <section class="cn-hero ck-hero" aria-labelledby="ckTitulo">
    <div class="cn-hero-texto">
      <nav aria-label="Ruta de navegación"><ol class="cn-migas">
        <li><a href="#/">${ico(I.casa)}Inicio</a></li><li aria-current="page">Cómo se construyó</li>
      </ol></nav>
      <h1 id="ckTitulo" tabindex="-1">Cómo se construyó</h1>
      <p class="cn-entrada">Conoce las diez etapas del desarrollo de EcoAccess, desde la definición del problema hasta las pruebas y la integración final, reconstruidas con la historia de Git y los documentos del repositorio.</p>
      <div class="ck-filtros" role="group" aria-label="Ver etapas por tema">
        ${FILTROS.map(([t, i]) => `<button type="button" class="ck-filtro" data-filtro="${escapar(t)}" aria-pressed="false">${ico(i)}${escapar(t)}</button>`).join('')}
      </div>
    </div>
    <figure class="cn-hero-arte">
      <img src="assets/conoce/construccion-hero.webp" width="528" height="152" alt="Eco junto a un portátil con código y gráficas, frente a la Universidad Popular del Cesar.">
    </figure>
  </section>

  <section class="ck-linea" aria-labelledby="ckLinea">
    <div class="ck-linea-cabecera">
      <h2 id="ckLinea">${ico(I.hoja)}Línea de tiempo del proyecto</h2>
      <p>Explora las 10 etapas del desarrollo: toca cada una para ver qué se hizo, cuándo y la evidencia (commits y documentos). Las fechas son las de los commits.</p>
      <button class="boton boton-secundario ck-todas" type="button" id="etapasTodas">${ico(I.capas)}<span>Abrir todas</span></button>
    </div>
    <ol class="ck-pasos" aria-label="Etapas">
      ${ETAPAS.map((e, n) => `<li><button type="button" class="ck-nodo" data-etapa="${n}" aria-controls="etapa-${n}" aria-expanded="${ABIERTAS.includes(n)}"><span class="ck-num">${n + 1}</span><span class="ck-nombre">${escapar(e.t)}</span></button></li>`).join('')}
    </ol>
  </section>

  <div class="ck-etapas" id="lineaEtapas">
    ${ETAPAS.map((e, n) => `<article class="ck-etapa" data-etapa="${n}" data-tag="${escapar(e.tag)}" style="--orden:${n}" ${ABIERTAS.includes(n) ? 'data-abierta' : ''}>
      <h3 class="ck-etapa-titulo"><button type="button" class="etapa-btn" aria-expanded="${ABIERTAS.includes(n)}" aria-controls="etapa-${n}">
        <span class="ck-num" aria-hidden="true">${n + 1}</span>${ico(e.i, 'class="ck-ico-cerrada"')}<span class="ck-t">${escapar(e.t)}</span>
        <span class="ck-tag">${ico(FILTROS.find(([t]) => t === e.tag)?.[1] ?? I.hoja)}${escapar(e.tag)}</span>
        <span class="ck-chev" aria-hidden="true">${ico(I.chevron)}</span>
      </button></h3>
      <div class="etapa-det" id="etapa-${n}" ${ABIERTAS.includes(n) ? '' : 'hidden'}>
        ${ilustracion(n + 1)}
        <div class="ck-cuerpo">
          <p class="ck-fecha">${escapar(e.f)}</p>
          <p>${escapar(e.d)}</p>
          <div class="ck-enlaces">${e.enlaces.map(enlace).join('')}</div>
        </div>
      </div>
    </article>`).join('')}
  </div>

  <section class="bloque cn-bloque prosa" aria-labelledby="k-metodo">
    <h2 id="k-metodo">Metodología</h2>
    <p>El trabajo siguió cuatro fases que se repitieron en cada módulo:</p>
    <ol>
      <li><strong>Análisis:</strong> problema, reglas de negocio y restricciones (por ejemplo, que la IA nunca abre una caneca por sí sola).</li>
      <li><strong>Diseño:</strong> capas, contratos (interfaces) y modelo de datos antes de implementar.</li>
      <li><strong>Implementación:</strong> por módulos pequeños, cada uno en su rama y con su solicitud de cambios.</li>
      <li><strong>Pruebas:</strong> automáticas en cada cambio y manuales con la estación cuando hay hardware.</li>
    </ol>
  </section>

  <section class="bloque cn-bloque" aria-labelledby="k-xp">
    <h2 id="k-xp">Prácticas de XP: solo las que tienen sustento</h2>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Práctica</th><th scope="col">Evidencia en el repositorio</th><th scope="col">Estado</th></tr></thead>
      <tbody>
        <tr><th scope="row">Pruebas automatizadas</th><td>MSTest en <code>tests/</code> (2589 unitarias correctas según el informe del 25/11, §14.5) y <code>node --test</code> en <code>web/ciudadano/pruebas</code></td><td>${sello('verificado', 'Con sustento')}</td></tr>
        <tr><th scope="row">Integración continua</th><td><code>.github/workflows/compilacion.yml</code> y <code>web-ciudadana.yml</code> se ejecutan en cada solicitud de cambios</td><td>${sello('verificado', 'Con sustento')}</td></tr>
        <tr><th scope="row">Entregas pequeñas</th><td>Cambios integrados por solicitudes numeradas (#1 a #29) y ramas por funcionalidad</td><td>${sello('verificado', 'Con sustento')}</td></tr>
        <tr><th scope="row">Programación en parejas, juego de planificación, cliente en sitio</th><td>No hay registro en el repositorio que lo demuestre</td><td>${sello('pendiente', 'Sin sustento')}</td></tr>
      </tbody>
    </table></div>
    <p class="nota-fuente">Por eso no se presenta el proyecto como «hecho con XP»: se usaron algunas de sus prácticas.</p>
  </section>
</div>`;
}

export function montar(raiz) {
  const etapas = [...raiz.querySelectorAll('.ck-etapa')];
  const nodos = [...raiz.querySelectorAll('.ck-nodo')];
  const filtros = [...raiz.querySelectorAll('.ck-filtro')];
  const todas = raiz.querySelector('#etapasTodas');
  const poner = (n, abierta) => {
    const e = etapas[n];
    e.toggleAttribute('data-abierta', abierta);
    e.querySelector('.etapa-btn').setAttribute('aria-expanded', String(abierta));
    e.querySelector('.etapa-det').hidden = !abierta;
    nodos[n].setAttribute('aria-expanded', String(abierta));
  };
  const actualizarTodas = () => {
    const faltan = etapas.some((e) => !e.hasAttribute('data-abierta'));
    todas.querySelector('span').textContent = faltan ? 'Abrir todas' : 'Cerrar todas';
  };
  const ver = (n) => etapas[n].scrollIntoView({ behavior: movimientoReducido() ? 'auto' : 'smooth', block: 'nearest' });

  etapas.forEach((e, n) => e.querySelector('.etapa-btn').addEventListener('click', () => {
    poner(n, !e.hasAttribute('data-abierta')); filtros.forEach((f) => f.setAttribute('aria-pressed', 'false')); actualizarTodas();
  }));
  // Un nodo de la línea abre su etapa y la lleva a la vista; si ya estaba abierta, solo la muestra.
  nodos.forEach((b, n) => b.addEventListener('click', () => {
    if (!etapas[n].hasAttribute('data-abierta')) poner(n, true);
    actualizarTodas(); ver(n); etapas[n].querySelector('.etapa-btn').focus({ preventScroll: true });
  }));
  todas.addEventListener('click', () => {
    const abrir = etapas.some((e) => !e.hasAttribute('data-abierta'));
    etapas.forEach((_, n) => poner(n, abrir));
    filtros.forEach((f) => f.setAttribute('aria-pressed', 'false'));
    actualizarTodas();
  });
  // Etiquetas del encabezado: abren solo las etapas de ese tema (otra pulsación vuelve a la vista inicial).
  filtros.forEach((f) => f.addEventListener('click', () => {
    const activo = f.getAttribute('aria-pressed') === 'true';
    filtros.forEach((x) => x.setAttribute('aria-pressed', String(!activo && x === f)));
    const tema = f.dataset.filtro;
    etapas.forEach((e, n) => poner(n, activo ? ABIERTAS.includes(n) : e.dataset.tag === tema || (tema === 'Git' && n === 9)));
    actualizarTodas();
    if (!activo) { const p = etapas.findIndex((e) => e.hasAttribute('data-abierta')); if (p >= 0) ver(p); }
  }));
  actualizarTodas();
}
