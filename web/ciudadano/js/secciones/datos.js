// Base de datos. Tablas y relaciones leídas de database/02_tablas, database/06_migraciones (V003, V005, V030–V033),
// database/01_modelo/DER.md y DICCIONARIO_DATOS.md. Solo estructura: ningún dato real de usuarios.
// El recorrido de Oracle Cloud usa valores FICTICIOS evidentes (región, base, alias, usuario, host).
import { cabecera, sello, escapar, pestanas } from './comun.js';

export const titulo = 'Base de datos';
const MIGAS = [['Tecnología', '#/tecnologia']];

const T = {
  USUARIO: [['id_usuario', 'NUMBER', 'PK'], ['nombre', 'VARCHAR2'], ['correo_electronico', 'VARCHAR2', 'UK'], ['fecha_registro_utc', 'TIMESTAMP'], ['activo', 'NUMBER(1)'], ['version_consentimiento', 'VARCHAR2']],
  ROL: [['id_rol', 'NUMBER', 'PK'], ['codigo', 'NUMBER(2)', 'UK'], ['descripcion', 'VARCHAR2']],
  USUARIO_ROL: [['id_usuario', 'NUMBER', 'PK FK'], ['id_rol', 'NUMBER', 'PK FK'], ['fecha_asignacion_utc', 'TIMESTAMP']],
  MATERIAL: [['id_material', 'NUMBER', 'PK'], ['codigo', 'VARCHAR2', 'UK'], ['nombre', 'VARCHAR2'], ['categoria', 'NUMBER 1–4'], ['habilitado', 'NUMBER(1)']],
  PUNTO_INSTALACION: [['id_punto_instalacion', 'NUMBER', 'PK'], ['nombre', 'VARCHAR2', 'UK'], ['referencia_ubicacion', 'VARCHAR2'], ['activo', 'NUMBER(1)']],
  CONTENEDOR: [['id_contenedor', 'NUMBER', 'PK'], ['id_punto_instalacion', 'NUMBER', 'FK'], ['nombre', 'VARCHAR2'], ['identificador_qr', 'VARCHAR2', 'UK'], ['estado', 'NUMBER 1–3']],
  CONTENEDOR_MATERIAL: [['id_contenedor', 'NUMBER', 'PK FK'], ['id_material', 'NUMBER', 'PK FK']],
  INTERACCION: [['id_interaccion', 'NUMBER', 'PK'], ['id_contenedor', 'NUMBER', 'FK'], ['id_usuario', 'NUMBER nulo', 'FK'], ['canal', 'NUMBER'], ['estado', 'NUMBER 1–5'], ['id_material_confirmado', 'NUMBER', 'FK']],
  ORDEN_APERTURA: [['id_orden', 'NUMBER', 'PK'], ['id_interaccion', 'NUMBER', 'FK UK'], ['id_contenedor', 'NUMBER', 'FK'], ['clave_idempotencia', 'VARCHAR2', 'UK'], ['estado', 'NUMBER 1–8'], ['fecha_vencimiento_utc', 'TIMESTAMP']],
  EVENTO_DISPOSITIVO: [['id_evento', 'NUMBER', 'PK'], ['id_contenedor', 'NUMBER', 'FK'], ['id_externo', 'VARCHAR2'], ['tipo', 'NUMBER 1–5'], ['id_orden', 'NUMBER nulo', 'FK'], ['resultado', 'NUMBER']],
  DEPOSITO: [['id_deposito', 'NUMBER', 'PK'], ['id_orden', 'NUMBER', 'FK UK'], ['id_evento', 'NUMBER', 'FK UK'], ['id_material_declarado', 'NUMBER', 'FK'], ['id_material_verificado', 'NUMBER nulo', 'FK'], ['peso_medido_kg', 'NUMBER nulo']],
  REGLA_ECOPUNTOS: [['id_regla', 'NUMBER', 'PK'], ['id_material', 'NUMBER', 'FK'], ['desde_utc', 'TIMESTAMP'], ['hasta_utc', 'TIMESTAMP'], ['id_creador', 'NUMBER', 'FK']],
  OPERACION_ECOPUNTOS: [['id_operacion', 'NUMBER', 'PK'], ['clave', 'VARCHAR2'], ['tipo', 'NUMBER 1–3'], ['id_deposito', 'NUMBER', 'FK'], ['id_actor', 'NUMBER', 'FK'], ['id_regla', 'NUMBER', 'FK']],
  MOVIMIENTO_ECOPUNTOS: [['id_movimiento', 'NUMBER', 'PK'], ['id_operacion', 'NUMBER', 'FK'], ['id_deposito', 'NUMBER', 'FK'], ['id_usuario', 'NUMBER', 'FK'], ['id_regla', 'NUMBER', 'FK'], ['puntos', 'NUMBER(7)'], ['id_credito_revertido', 'NUMBER', 'FK']],
  OPERACION_ESTACION: [['id_operacion', 'NUMBER', 'PK'], ['clave', 'VARCHAR2(100)', 'UK'], ['fecha_utc', 'TIMESTAMP'], ['canal', 'PC | TELEGRAM | WEB'], ['tipo', 'VARCHAR2(30)'], ['id_usuario', 'NUMBER nulo', 'FK'], ['canecas', 'VARCHAR2(30)'], ['duracion_ms', 'NUMBER(6)'], ['estado_hardware', 'VARCHAR2(30)'], ['estado_operacion', 'VARCHAR2(30)'], ['tipo_usuario', 'INVITADO | REGISTRADO', 'V033'], ['metodo_reconocimiento', 'FOTO | TEXTO | VOZ | MANUAL', 'V033'], ['confianza', 'NUMBER(4,3)', 'V033'], ['sesion_anonima', 'VARCHAR2(40)', 'V033']],
  OPERACION_ESTACION_DETALLE: [['id_detalle', 'NUMBER', 'PK'], ['clave_operacion', 'VARCHAR2(100)', 'FK'], ['orden', 'NUMBER(2)'], ['codigo_residuo', 'VARCHAR2(10)', 'UK*'], ['nombre_residuo', 'VARCHAR2(80)'], ['caneca', 'VARCHAR2(10)'], ['cantidad', 'NUMBER(2)'], ['metodo', 'VARCHAR2(10)']],
  ECO_JUEGO_COMPETENCIAS: [['id', 'NUMBER', 'PK'], ['codigo', 'VARCHAR2(40)', 'UK'], ['nombre', 'VARCHAR2(120)'], ['estado', 'VARCHAR2(20)'], ['es_prueba', 'CHAR(1)'], ['duracion_max_seg', 'NUMBER(5)']],
  ECO_JUEGO_PREGUNTAS: [['id', 'NUMBER(3)', 'PK'], ['orden', 'NUMBER(2)', 'UK'], ['nivel', 'VARCHAR2(20)'], ['tema', 'VARCHAR2(40)'], ['enunciado', 'VARCHAR2(400)'], ['puntos', 'NUMBER(4)']],
  ECO_JUEGO_OPCIONES: [['id', 'NUMBER(4)', 'PK'], ['pregunta_id', 'NUMBER(3)', 'FK'], ['letra', 'CHAR(1)', 'UK*'], ['texto', 'VARCHAR2(200)']],
  ECO_JUEGO_PARTICIPANTES: [['id', 'NUMBER', 'PK'], ['competencia_id', 'NUMBER', 'FK'], ['alias', 'VARCHAR2(24)', 'UK*'], ['nombre_normalizado', 'VARCHAR2(120)', 'UK*'], ['dispositivo_sha256', 'VARCHAR2(64)']],
  ECO_JUEGO_INTENTOS: [['id', 'NUMBER', 'PK'], ['participante_id', 'NUMBER', 'FK UK'], ['competencia_id', 'NUMBER', 'FK'], ['sesion_sha256', 'VARCHAR2(64)', 'UK'], ['estado', 'VARCHAR2(20)'], ['aciertos', 'NUMBER(2)'], ['puntaje', 'NUMBER(4)'], ['tiempo_ms', 'NUMBER(12)']],
  ECO_JUEGO_RESPUESTAS: [['id', 'NUMBER', 'PK'], ['intento_id', 'NUMBER', 'FK'], ['pregunta_id', 'NUMBER(3)', 'FK'], ['opcion_id', 'NUMBER(4) nulo', 'FK'], ['ms_desde_inicio', 'NUMBER(12)']],
};

// Grupos del modelo: posiciones (x, y) de cada entidad y relaciones [a, b, cardinalidad en a, en b, verbo].
const GRUPOS = [
  { id: 'nucleo', n: 'Núcleo', m: 'V001', d: 'Usuarios, roles, materiales y canecas (CONTENEDOR) con los materiales que admite cada una.',
    e: { ROL: [20, 20], USUARIO_ROL: [255, 20], USUARIO: [490, 20], MATERIAL: [20, 150], CONTENEDOR_MATERIAL: [255, 150], CONTENEDOR: [490, 150], PUNTO_INSTALACION: [490, 270] },
    r: [['ROL', 'USUARIO_ROL', '1', 'N', 'se asigna'], ['USUARIO', 'USUARIO_ROL', '1', '1..N', 'tiene'], ['MATERIAL', 'CONTENEDOR_MATERIAL', '1', 'N', 'admitido'], ['CONTENEDOR', 'CONTENEDOR_MATERIAL', '1', 'N', 'admite'], ['PUNTO_INSTALACION', 'CONTENEDOR', '1', 'N', 'aloja']] },
  { id: 'disposicion', n: 'Disposiciones', m: 'V003', d: 'Cada visita a una caneca (INTERACCION) puede autorizar una orden de apertura; el dispositivo informa eventos y, si hay sensor, un depósito.',
    e: { CONTENEDOR: [20, 20], INTERACCION: [255, 20], USUARIO: [490, 20], ORDEN_APERTURA: [255, 140], EVENTO_DISPOSITIVO: [490, 140], MATERIAL: [20, 260], DEPOSITO: [255, 260] },
    r: [['CONTENEDOR', 'INTERACCION', '1', 'N', 'recibe'], ['USUARIO', 'INTERACCION', '0..1', 'N', 'realiza'], ['INTERACCION', 'ORDEN_APERTURA', '1', '0..1', 'autoriza'], ['ORDEN_APERTURA', 'EVENTO_DISPOSITIVO', '1', 'N', 'recibe'], ['ORDEN_APERTURA', 'DEPOSITO', '1', '0..1', 'produce'], ['EVENTO_DISPOSITIVO', 'DEPOSITO', '1', '0..1', 'detecta'], ['MATERIAL', 'DEPOSITO', '1', 'N', 'declarado']] },
  { id: 'ecopuntos', n: 'Ecopuntos', m: 'V005', d: 'Libro contable: el saldo se calcula de los movimientos; una corrección registra un reverso y un sucesor, nunca edita.',
    e: { MATERIAL: [20, 20], REGLA_ECOPUNTOS: [255, 20], USUARIO: [490, 20], DEPOSITO: [20, 160], OPERACION_ECOPUNTOS: [255, 160], MOVIMIENTO_ECOPUNTOS: [490, 160] },
    r: [['MATERIAL', 'REGLA_ECOPUNTOS', '1', 'N', 'tarifa'], ['DEPOSITO', 'OPERACION_ECOPUNTOS', '1', 'N', 'origina'], ['OPERACION_ECOPUNTOS', 'MOVIMIENTO_ECOPUNTOS', '1', '1..N', 'confirma'], ['REGLA_ECOPUNTOS', 'MOVIMIENTO_ECOPUNTOS', '1', 'N', 'respalda'], ['USUARIO', 'MOVIMIENTO_ECOPUNTOS', '1', 'N', 'titular']] },
  { id: 'estacion', n: 'Estación', m: 'V030 · V033', d: 'Bitácora de cada operación de la estación de tres canecas (PC, Telegram o web) y, con V033, invitados y el detalle por residuo.',
    e: { USUARIO: [20, 40], OPERACION_ESTACION: [300, 40], OPERACION_ESTACION_DETALLE: [300, 190] },
    r: [['USUARIO', 'OPERACION_ESTACION', '0..1', 'N', 'solicita'], ['OPERACION_ESTACION', 'OPERACION_ESTACION_DETALLE', '1', 'N', 'por clave']] },
  { id: 'juego', n: 'Challenge', m: 'V031 · V032', d: 'Tablas propias del EcoAccess Challenge, sin relación con canecas ni ecopuntos. El paquete PKG_ECO_JUEGO las expone por ORDS.',
    e: { ECO_JUEGO_COMPETENCIAS: [20, 20], ECO_JUEGO_PARTICIPANTES: [255, 20], ECO_JUEGO_INTENTOS: [490, 20], ECO_JUEGO_PREGUNTAS: [20, 170], ECO_JUEGO_OPCIONES: [255, 170], ECO_JUEGO_RESPUESTAS: [490, 170] },
    r: [['ECO_JUEGO_COMPETENCIAS', 'ECO_JUEGO_PARTICIPANTES', '1', 'N', 'inscribe'], ['ECO_JUEGO_PARTICIPANTES', 'ECO_JUEGO_INTENTOS', '1', '0..1', 'juega'], ['ECO_JUEGO_PREGUNTAS', 'ECO_JUEGO_OPCIONES', '1', 'N', 'ofrece'], ['ECO_JUEGO_INTENTOS', 'ECO_JUEGO_RESPUESTAS', '1', 'N', 'registra'], ['ECO_JUEGO_OPCIONES', 'ECO_JUEGO_RESPUESTAS', '0..1', 'N', 'elegida'], ['ECO_JUEGO_PREGUNTAS', 'ECO_JUEGO_RESPUESTAS', '1', 'N', 'responde']] },
];
const EW = 210, EH = 40;

function svgGrupo(g) {
  const centro = (t) => [g.e[t][0] + EW / 2, g.e[t][1] + EH / 2];
  const rels = g.r.map(([a, b, ca, cb, v], i) => {
    const [x1, y1] = centro(a), [x2, y2] = centro(b);
    // Une por los bordes: horizontal si están en la misma fila, vertical si no.
    let p1, p2;
    if (Math.abs(y1 - y2) < 10) { const s = x2 > x1 ? 1 : -1; p1 = [x1 + s * EW / 2, y1]; p2 = [x2 - s * EW / 2, y2]; }
    else { const s = y2 > y1 ? 1 : -1; p1 = [x1, y1 + s * EH / 2]; p2 = [x2, y2 - s * EH / 2]; }
    const t1 = [p1[0] + (p2[0] - p1[0]) * 0.12, p1[1] + (p2[1] - p1[1]) * 0.12 - 4], t2 = [p1[0] + (p2[0] - p1[0]) * 0.88, p1[1] + (p2[1] - p1[1]) * 0.88 - 4];
    return `<g data-rel="${a} ${b}"><path class="rel" d="M${p1[0]} ${p1[1]} L${p2[0]} ${p2[1]}"/>
      <text class="card" x="${t1[0] + 4}" y="${t1[1]}">${ca}</text><text class="card" x="${t2[0] + 4}" y="${t2[1]}">${cb}</text>
      <text class="card" style="font-weight:400" x="${(p1[0] + p2[0]) / 2 + 4}" y="${(p1[1] + p2[1]) / 2 + 14}">${escapar(v)}</text></g>`;
  }).join('');
  const ents = Object.entries(g.e).map(([t, [x, y]]) => `<g class="ent" data-t="${t}" role="button" tabindex="0" aria-pressed="false" aria-label="Tabla ${t}">
    <rect x="${x}" y="${y}" width="${EW}" height="${EH}" rx="8"/><text x="${x + EW / 2}" y="${y + 25}" text-anchor="middle">${t}</text></g>`).join('');
  const alto = Math.max(...Object.values(g.e).map(([, y]) => y)) + EH + 20;
  return `<div class="desliza" tabindex="-1"><svg class="er" viewBox="0 0 720 ${alto}" style="min-width:600px;width:100%;height:auto;display:block" role="group" aria-label="Diagrama entidad-relación: ${escapar(g.n)}">${rels}${ents}</svg></div>`;
}

function fichaTabla(t, g) {
  const rel = g.r.filter(([a, b]) => a === t || b === t).map(([a, b, ca, cb, v]) => `${a} (${ca}) ${v} ${b} (${cb})`);
  return `<div class="tabla-er"><h4>${t}</h4><ul>${T[t].map(([c, tipo, k]) => `<li>${k ? k.split(' ').map((x) => `<span class="clave" data-k="${x}">${x}</span>`).join(' ') : '<span class="clave" aria-hidden="true"></span>'}<span>${escapar(c)}</span><span class="t">${escapar(tipo)}</span></li>`).join('')}</ul></div>
    ${rel.length ? `<p class="lamina-pie">Relaciones en este grupo:</p><ul class="lamina-pie">${rel.map((r) => `<li>${escapar(r)}</li>`).join('')}</ul>` : ''}`;
}

const ORACLE = [
  ['Entrar a la consola', 'Iniciar sesión en Oracle Cloud y elegir la región donde vive la base.', '$ consola › región: region-ejemplo-1\n✔ sesión iniciada como usuario.ejemplo@correo.invalid'],
  ['Revisar la base', 'Oracle Database › Autonomous Database › la base del proyecto. Su estado debe ser «Available». Una base Always Free se detiene sola tras 7 días sin uso; arrancarla no cuesta.', 'Base: BaseEjemplo\nEstado: STOPPED → [Start] → AVAILABLE\nAlways Free: Yes'],
  ['Descargar el wallet', 'Database connection › Instance wallet › Download wallet. Oracle pide una contraseña que protege ewallet.p12: va a un gestor de contraseñas, nunca al repositorio.', '⬇ Wallet_Ejemplo.zip  (contraseña del wallet: ************)'],
  ['Guardarlo fuera del repositorio', 'Descomprimir el wallet en una carpeta del equipo que Git no vea. El archivo .gitignore del proyecto también lo excluye.', 'C:\\Ejemplo\\Wallets\\Wallet_Ejemplo\\\n  cwallet.sso   ewallet.p12   tnsnames.ora\n  sqlnet.ora    keystore.jks  truststore.jks'],
  ['Leer tnsnames.ora', 'Cada alias es un servicio con distinta prioridad (high, medium, low, tp, tpurgent). Una aplicación de transacciones como EcoAccess usa el de tipo «tp».', 'ejemplo_high = (description= … (host=servidor.ejemplo.invalid) …)\nejemplo_tp   = (description= … (service_name=ejemplo_tp.servicio.invalid) …)'],
  ['Crear la conexión en SQL Developer', 'Nueva conexión de tipo «Cloud Wallet»: archivo de configuración = el zip del wallet, servicio = el alias tp.', 'Nombre: ejemplo-admin\nTipo: Cloud Wallet\nConfiguración: Wallet_Ejemplo.zip\nServicio: ejemplo_tp\nUsuario: ADMIN   Contraseña: (no se guarda)'],
  ['Probar la conexión', 'Test debe responder «Status: Success». Después, Connect.', '[Test] → Status: Success\n[Connect] ✔'],
  ['Consulta sin escritura', 'Primero se confirma con una consulta que no cambia nada.', 'SQL> SELECT USER, SYS_CONTEXT(\'USERENV\',\'DB_NAME\') FROM dual;\nUSER          DB_NAME\n------------- -----------\nADMIN         BASEEJEMPLO'],
  ['Esquema y roles', 'Con la cuenta administradora se crea el esquema dueño de las tablas y dos roles: uno de solo consulta y otro de edición (scripts de database/00_administracion).', 'SQL> @04_crear_esquema_oracle_cloud.sql\n✔ esquema ESQUEMA_DEMO creado\n✔ rol de consulta  ✔ rol de edición'],
  ['Migraciones en orden', 'Se aplican V002, V003… hasta V033 en orden (V034 y V035 quedan preparadas). Cada una se anota en ECO_MIGRACION_APLICADA y puede volver a ejecutarse sin duplicar objetos.', 'V030 operacion_estacion ........ aplicada\nV031 juego ..................... aplicada\nV032 paquete del juego (ORDS) .. aplicada\nV033 detalle e invitados ....... aplicada\nV034 bancos del juego ......... preparada, sin aplicar'],
  ['Conectar la aplicación', 'La aplicación lee la carpeta del wallet desde TNS_ADMIN y el usuario y la contraseña desde secretos de usuario o variables de entorno. Nada de esto está en el código.', 'TNS_ADMIN=C:\\Ejemplo\\Wallets\\Wallet_Ejemplo\nUsuario: (secretos de usuario)   Contraseña: (secretos de usuario)\n✔ conexión de la app: OK'],
  ['Compartir y revocar acceso', 'Cada integrante tiene su propia cuenta con contraseña temporal que se cambia en el primer ingreso. Para retirar un wallet entregado basta con «Rotate wallet» en la consola.', 'Integrante: ALUMNO_DEMO → debe cambiar la contraseña\n[Rotate wallet] → los wallets anteriores dejan de servir'],
];

export function render() {
  return `${cabecera({ titulo, migas: MIGAS, entrada: 'Oracle Autonomous Database en Oracle Cloud. Aquí está el modelo real: tablas, claves y relaciones, sin ningún dato de personas.' })}

  <section class="bloque" aria-labelledby="d-er">
    <h2 id="d-er">Modelo entidad-relación</h2>
    <p>Elige un grupo y luego una tabla para ver sus columnas principales. <span class="clave" data-k="PK">PK</span> clave primaria, <span class="clave" data-k="FK">FK</span> clave foránea, <span class="clave" data-k="UK">UK</span> valor único (UK* único combinado con otra columna). Las cardinalidades están en los extremos de cada línea.</p>
    <div id="er">
      <div class="pestanas" role="tablist" aria-label="Grupos del modelo">
        ${GRUPOS.map((g, i) => `<button role="tab" id="tab-er-${i}" aria-controls="panel-er-${i}" aria-selected="${i === 0}" type="button">${g.n} <small>${g.m}</small></button>`).join('')}
      </div>
      ${GRUPOS.map((g, i) => `<div class="lamina" role="tabpanel" id="panel-er-${i}" aria-labelledby="tab-er-${i}" ${i ? 'hidden' : ''} data-grupo="${i}">
        <p style="margin-top:0">${escapar(g.d)}</p>
        <div class="er-env">${svgGrupo(g)}<div class="ficha-er" aria-live="polite">${fichaTabla(Object.keys(g.e)[0], g)}</div></div>
      </div>`).join('')}
    </div>
    <p class="nota-fuente">Fuentes: <code>database/02_tablas/01_crear_tablas_iniciales.sql</code>, <code>database/06_migraciones/V003…V033</code>, <code>database/01_modelo/DER.md</code>. Se muestran las columnas principales; el detalle completo está en <code>DICCIONARIO_DATOS.md</code>.</p>
  </section>

  <section class="bloque" aria-labelledby="d-mig">
    <h2 id="d-mig">Migraciones recientes</h2>
    <p>Cada cambio de estructura es un archivo numerado y repetible, con su reversión (R030 a R035). Nunca se edita una migración ya aplicada: por eso la de invitados pasó de V031 a V033 cuando V031 y V032 ya eran las del juego en producción.</p>
    <div class="tabla-env"><table>
      <thead><tr><th scope="col">Migración</th><th scope="col">Qué agrega</th><th scope="col">Estado</th></tr></thead>
      <tbody>
        <tr><th scope="row">V030</th><td>Tabla <code>OPERACION_ESTACION</code>, vista <code>EA_R_OPERACIONES_ESTACION</code> e índices por fecha y por usuario</td><td>${sello('verificado', 'En uso en producción')}</td></tr>
        <tr><th scope="row">V031</th><td>Ocho tablas <code>ECO_JUEGO_*</code> y la vista <code>VW_JUEGO_CLASIFICACION</code></td><td>${sello('verificado', 'Aplicada en producción')}</td></tr>
        <tr><th scope="row">V032</th><td>Paquete <code>PKG_ECO_JUEGO</code> y su API por ORDS</td><td>${sello('verificado', 'Aplicada en producción')} El bloque ORDS solo se probó allí.</td></tr>
        <tr><th scope="row">V033</th><td>Cuatro columnas anulables en <code>OPERACION_ESTACION</code> (invitado, método, confianza, sesión anónima), la tabla <code>OPERACION_ESTACION_DETALLE</code> y la vista <code>EA_R_ESTADISTICAS_ESTACION</code></td><td>${sello('verificado', 'Aplicada en producción el 25/11/2026')} Antes, 14/14 en un esquema aislado.</td></tr>
        <tr><th scope="row">V034</th><td>Bancos de preguntas del Challenge para una ronda de 10 preguntas</td><td>${sello('simulado', 'Probada en un esquema aislado')} ${sello('pendiente', 'No aplicada en producción')}</td></tr>
        <tr><th scope="row">V035</th><td>Paquete del juego con bancos de preguntas (<code>PKG_ECO_JUEGO</code> ampliado)</td><td>${sello('simulado', 'Probada en un esquema aislado')} ${sello('pendiente', 'No aplicada en producción')}</td></tr>
      </tbody>
    </table></div>
    <p class="nota-fuente">Fuente: <code>reportes/ECOACCESS_INFORME_FINAL_INTEGRACION.md</code> §2, §3 y §14.2. V033 se aplicó con autorización escrita y respaldo previo, en 8,3 s, sin errores y con 0 objetos inválidos (<code>docs/database/evidencias/v033_produccion_20261125.md</code>). V034 y V035 no se aplican para no alterar la ronda oficial de 7 preguntas.</p>
  </section>

  <section class="bloque" aria-labelledby="d-objetos">
    <h2 id="d-objetos">Vistas, índices, paquetes y roles</h2>
    <div class="dos">
      <div>
        <h3>Vistas</h3>
        <ul>
          <li><code>EA_R_OPERACIONES_ESTACION</code>: operaciones por día, canal y estado del hardware (V030).</li>
          <li><code>EA_R_ESTADISTICAS_ESTACION</code>: una fila por residuo, con invitado o registrado y método (V033).</li>
          <li><code>VW_JUEGO_CLASIFICACION</code>: ranking del Challenge (V031).</li>
          <li><code>VW_SALDO_ECOPUNTOS</code>, <code>VW_DEPOSITO_DETALLE</code> y las vistas <code>VW_BI_*</code> para Power BI.</li>
        </ul>
        <h3>Índices</h3>
        <p>Se crean donde hay consultas frecuentes. Ejemplo: <code>IX_ECO_JUEGO_INT_RANKING (competencia_id, estado, puntaje DESC, tiempo_ms, fin, id)</code> sigue exactamente el orden del ranking, y <code>IX_OPERACION_ESTACION_USUARIO (id_usuario, fecha_utc)</code> sirve el historial de cada persona.</p>
      </div>
      <div>
        <h3>Paquetes PL/SQL</h3>
        <ul>
          <li><code>PKG_DEPOSITO</code>, <code>PKG_ECOPUNTOS</code>, <code>PKG_ESTADISTICAS</code>, <code>PKG_ECOACCESS_STATS</code> y <code>PKG_CONTEXTO_APP</code>.</li>
          <li><code>PKG_ECO_JUEGO</code> (V032): registro, preguntas, respuestas y clasificación del Challenge; el tiempo y el puntaje se calculan en el servidor.</li>
        </ul>
        <h3>Roles</h3>
        <ul>
          <li>De la aplicación (tabla <code>ROL</code>): Ciudadano, Operario y Administrador.</li>
          <li>De la base: un rol de solo consulta y otro de edición para los integrantes. La aplicación no los usa: se conecta con el esquema dueño de las tablas.</li>
        </ul>
        <p class="nota-fuente">El inventario exportado el 18/11/2026 (<code>database/schema/INVENTARIO.md</code>) registra 33 tablas, 21 vistas, 98 índices, 5 paquetes y 0 objetos inválidos, antes de V030–V033.</p>
      </div>
    </div>
  </section>

  <section class="bloque" aria-labelledby="d-oracle">
    <h2 id="d-oracle">Oracle Cloud, wallet y SQL Developer en 12 pasos</h2>
    <p>${sello('educativo', 'Simulación con valores ficticios')} Ningún dato de abajo es real: la región, la base, los alias, el servidor y los usuarios son inventados. Las credenciales reales nunca se publican.</p>
    <div class="lamina" id="oracleSim">
      <div class="controles" style="margin-top:0">
        <button class="btn-sec" type="button" data-accion="anterior">Paso anterior</button>
        <button class="btn-principal" type="button" data-accion="siguiente">Siguiente paso</button>
        <span id="oracleCuenta" aria-live="polite"></span>
      </div>
      <div class="dos" style="margin-top:12px">
        <ol class="recorrido" style="margin-top:0">${ORACLE.map(([t, d]) => `<li><span><b>${escapar(t)}</b><span class="oracle-det">${escapar(d)}</span></span></li>`).join('')}</ol>
        <div class="consola" role="region" aria-label="Consola simulada"><div class="barra"><span>Simulación</span><span>valores ficticios</span></div><pre id="oracleConsola"></pre></div>
      </div>
    </div>
  </section>`;
}

export function montar(raiz) {
  const er = raiz.querySelector('#er');
  pestanas(er);
  er.querySelectorAll('[data-grupo]').forEach((panel) => {
    const g = GRUPOS[Number(panel.dataset.grupo)];
    const ficha = panel.querySelector('.ficha-er');
    const ents = [...panel.querySelectorAll('.ent')];
    const elegir = (t) => {
      ents.forEach((e) => { const si = e.dataset.t === t; e.classList.toggle('sel', si); e.setAttribute('aria-pressed', String(si)); });
      panel.querySelectorAll('[data-rel]').forEach((r) => r.querySelector('.rel').classList.toggle('sel', r.dataset.rel.split(' ').includes(t)));
      ficha.innerHTML = fichaTabla(t, g);
    };
    ents.forEach((e) => {
      e.addEventListener('click', () => elegir(e.dataset.t));
      e.addEventListener('keydown', (ev) => { if (ev.key === 'Enter' || ev.key === ' ') { ev.preventDefault(); elegir(e.dataset.t); } });
    });
    elegir(ents[0].dataset.t);
  });

  const sim = raiz.querySelector('#oracleSim');
  const items = [...sim.querySelectorAll('.recorrido li')];
  const consola = sim.querySelector('#oracleConsola');
  let i = 0;
  const pintar = () => {
    items.forEach((li, k) => { li.className = k < i ? 'hecho' : k === i ? 'actual' : ''; li.querySelector('.oracle-det').hidden = k !== i; if (k === i) li.setAttribute('aria-current', 'step'); else li.removeAttribute('aria-current'); });
    consola.textContent = ORACLE[i][2];
    sim.querySelector('#oracleCuenta').textContent = `Paso ${i + 1} de ${ORACLE.length}`;
    sim.querySelector('[data-accion="anterior"]').disabled = i === 0;
    sim.querySelector('[data-accion="siguiente"]').textContent = i === ORACLE.length - 1 ? 'Volver al paso 1' : 'Siguiente paso';
  };
  sim.querySelector('[data-accion="siguiente"]').addEventListener('click', () => { i = (i + 1) % ORACLE.length; pintar(); });
  sim.querySelector('[data-accion="anterior"]').addEventListener('click', () => { i = Math.max(0, i - 1); pintar(); });
  pintar();
}
