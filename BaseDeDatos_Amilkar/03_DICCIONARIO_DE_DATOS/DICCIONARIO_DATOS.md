# Diccionario de datos — EcoAccess BD (esquema ECOACCESS_BD)

Generado el 2026-11-11 20:23 desde los metadatos reales de Oracle (`USER_TAB_COLUMNS`, `USER_CONSTRAINTS`) más la explicación humana de `fuente_diccionario.txt`. **173 columnas en 19 tablas, 0 sin explicar.**

Columnas: **Tipo** exacto · **Oblig.** (NOT NULL) · **Default** · **Restricciones** (PK, FK, UNIQUE, CHECK, IDENTITY) · **Significado** · **Ejemplo ficticio** · **Quién lo registra** · **Dónde se usa** · **Si es NULL** · **Por qué existe / qué anomalía previene**.

## Módulo: M1 · Catálogos base

### MATERIAL (6 columnas)

Catálogo validado de materiales; la IA solo puede apuntar a estos códigos.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_MATERIAL` | NUMBER(10) | Sí |  | PK; UNIQUE: UQ_MATERIAL_CATEGORIA; IDENTITY BY DEFAULT | Número del material. | 101 | Catálogo | FK en todo el flujo | Nunca es NULL (PK). | Identificar el material. |
| 2 | `CODIGO` | VARCHAR2(30 CHAR) | Sí |  | UNIQUE: UQ_MATERIAL_CODIGO; CHECK CK_MATERIAL_CODIGO | Código corto en MAYÚSCULAS. | PET | Administrador | Lo usa la IA y los reportes | Nunca es NULL. | UNIQUE y formato fijo: la IA no puede inventar materiales fuera del catálogo. |
| 3 | `NOMBRE` | VARCHAR2(100 CHAR) | Sí |  | CHECK CK_MATERIAL_NOMBRE | Nombre para personas. | Botella plástica PET | Administrador | Mensajes del bot y reportes | Nunca es NULL. | Lectura humana. |
| 4 | `CATEGORIA` | NUMBER(1) | Sí |  | UNIQUE: UQ_MATERIAL_CATEGORIA; CHECK CK_MATERIAL_CATEGORIA | 1 aprovechable, 2 orgánico, 3 no aprovechable, 4 peligroso (ruta especial). | 1 | Administrador | Resolver caneca, FK compuesta | Nunca es NULL. | Decide el color de caneca según la Resolución 2184 de 2019. |
| 5 | `DESCRIPCION` | VARCHAR2(500 CHAR) | No |  |  | Instrucción de separación. | Botellas de bebidas marcadas con el número 1. | Administrador | Orientación del bot | NULL = sin descripción. | Educar al ciudadano. |
| 6 | `HABILITADO` | NUMBER(1) | Sí | 1 | CHECK CK_MATERIAL_HABILITADO | 1 si se puede usar. | 1 | Administrador | Resolver caneca | Nunca es NULL. | Retirar un material sin borrar su historia. |

### PESO_REFERENCIAL (9 columnas)

Pesos típicos por objeto y estado visual para ESTIMAR (nunca medir) el peso de una foto.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_PESO_REFERENCIAL` | NUMBER(10) | Sí |  | PK; IDENTITY BY DEFAULT | Número de la fila del catálogo de pesos. | 1 | Catálogo | FK desde CONSULTA_VISION y DEPOSITO | Nunca es NULL (PK). | Saber de dónde salió cada estimación. |
| 2 | `ID_MATERIAL` | NUMBER(10) | Sí |  | FK: FK_PESO_MATERIAL->MATERIAL | Material del objeto. | 101 | Administrador | Agrupar pesos por material | Nunca es NULL. | Relación 1:N material-objetos. |
| 3 | `CLAVE_OBJETO` | VARCHAR2(40 CHAR) | Sí |  | UNIQUE: UQ_PESO_OBJETO_ESTADO; CHECK CK_PESO_CLAVE | Objeto que reconoce la IA. | BOTELLA_PET_600ML | Administrador | Unir la respuesta de la IA con el peso | Nunca es NULL. | Un material (PET) tiene objetos de pesos muy distintos. |
| 4 | `DESCRIPCION_OBJETO` | VARCHAR2(150 CHAR) | Sí |  | CHECK CK_PESO_DESCRIPCION | Nombre legible del objeto. | Botella PET de 600 ml | Administrador | Reportes | Nunca es NULL. | Lectura humana. |
| 5 | `ESTADO_VISUAL` | NUMBER(1) | Sí | 0 | UNIQUE: UQ_PESO_OBJETO_ESTADO; CHECK CK_PESO_ESTADO_VISUAL | 0 no aplica, 1 vacío, 2 parcialmente lleno, 3 lleno. | 1 | Administrador | Estimación según lo que se ve en la foto | Nunca es NULL (DEFAULT 0). | Una botella llena pesa 30 veces más que una vacía. |
| 6 | `GRAMOS_MINIMO` | NUMBER(8,1) | Sí |  | CHECK CK_PESO_ORDEN | Peso mínimo esperado de una unidad. | 18 | Administrador | Validar estimaciones (trigger 2) | Nunca es NULL. | Define el rango de incertidumbre. |
| 7 | `GRAMOS_TIPICO` | NUMBER(8,1) | Sí |  | CHECK CK_PESO_ORDEN | Peso más habitual de una unidad. | 22 | Administrador | fn_peso_estimado_kg | Nunca es NULL. | Base de la estimación. |
| 8 | `GRAMOS_MAXIMO` | NUMBER(8,1) | Sí |  | CHECK CK_PESO_ORDEN | Peso máximo esperado. | 28 | Administrador | Validar estimaciones | Nunca es NULL. | CHECK mínimo <= típico <= máximo. |
| 9 | `FUENTE` | VARCHAR2(200 CHAR) | Sí |  | CHECK CK_PESO_FUENTE | De dónde sale el dato. | Rango típico de referencia académica; pendiente de medición propia | Administrador | Defensa del dato ante el profesor | Nunca es NULL. | Obliga a declarar el origen: no hay pesos «mágicos». |

## Módulo: M2 · Usuarios e identidad

### ROL (3 columnas)

Catálogo cerrado de los tres roles de negocio: Ciudadano, Operario y Administrador.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_ROL` | NUMBER(10) | Sí |  | PK; IDENTITY BY DEFAULT | Número interno que identifica cada rol. | 1 | Catálogo (10_catalogos.sql) | FK en USUARIO_ROL | Nunca es NULL (PK). | Permite relacionar roles sin repetir su texto. |
| 2 | `CODIGO` | NUMBER(2) | Sí |  | UNIQUE: UQ_ROL_CODIGO; CHECK CK_ROL_CODIGO | Código de negocio del rol, igual al enum CodigoRol de C#: 1 Ciudadano, 2 Operario, 3 Administrador. | 2 | Catálogo | Permisos en PKG_CONTEXTO_APP y MatrizPermisos de P3 | Nunca es NULL. | Evita inventar un cuarto rol (CHECK 1-3) y mantiene el mismo número que el código C#. |
| 3 | `DESCRIPCION` | VARCHAR2(200 CHAR) | Sí |  | CHECK CK_ROL_DESCRIPCION | Nombre legible del rol. | Operario | Catálogo | Pantallas y reportes | Nunca es NULL. | Mostrar el rol con palabras sin depender del número. |

### USUARIO (8 columnas)

Cada persona registrada. No guarda contraseñas.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_USUARIO` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número único de cada persona registrada (IDENTITY ALWAYS: nadie lo escribe). | 17 | Oracle al insertar | FK en casi todo el modelo; seudónimo C-0017 en Power BI | Nunca es NULL. | Identifica a la persona aunque cambie su nombre o correo. |
| 2 | `NOMBRE` | VARCHAR2(100 CHAR) | Sí |  | CHECK CK_USUARIO_NOMBRE | Nombre de la persona, sin espacios sobrantes. | María Gómez | Registro (formulario o bot) | Listados del operario, auditoría | Nunca es NULL. | CHECK impide nombres vacíos o de 1 letra. |
| 3 | `CORREO_ELECTRONICO` | VARCHAR2(254 CHAR) | Sí |  | UNIQUE: UQ_USUARIO_CORREO; CHECK CK_USUARIO_CORREO_FORMA; CHECK CK_USUARIO_CORREO_MIN | Correo en minúsculas; es único. | maria@ejemplo.test | Registro | Inicio de sesión de escritorio, búsqueda del operario | Nunca es NULL. | UNIQUE + minúsculas impide dos cuentas con el mismo correo escrito distinto. |
| 4 | `FECHA_REGISTRO_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Momento (UTC) en que se creó la cuenta. | 2026-09-10 14:00 | Oracle (DEFAULT) | Reportes de altas | Nunca es NULL. | Guardar en UTC evita confusiones de zona horaria; se muestra en hora Colombia. |
| 5 | `ACTIVO` | NUMBER(1) | Sí | 1 | CHECK CK_USUARIO_ACTIVO | 1 si la cuenta puede usarse, 0 si está desactivada. | 1 | Administrador | tiene_rol() rechaza cuentas inactivas | Nunca es NULL (DEFAULT 1). | Desactivar sin borrar: se conserva la historia de depósitos. |
| 6 | `VERSION_CONSENTIMIENTO` | VARCHAR2(20 CHAR) | No |  | CHECK CK_USUARIO_CONSENTIMIENTO | Versión del aviso de tratamiento de datos que aceptó. | v1-2026 | Registro | Cumplimiento de protección de datos | NULL = aún no aceptó. | Saber qué texto aceptó la persona (Ley 1581 de 2012). |
| 7 | `FECHA_CONSENTIMIENTO_UTC` | TIMESTAMP(6) | No |  | CHECK CK_USUARIO_CONSENTIMIENTO | Cuándo aceptó ese aviso. | 2026-09-10 14:00 | Registro | Cumplimiento | NULL junto con la versión. | CHECK: versión y fecha van juntas o no van. |
| 8 | `ES_SINTETICO` | NUMBER(1) | Sí | 0 | CHECK CK_USUARIO_SINTETICO | 1 si la cuenta es de datos ficticios de prueba. | 1 | Script 11 | Filtro «DATOS SINTÉTICOS» en vistas BI | Nunca es NULL (DEFAULT 0). | Impide confundir datos de prueba con personas reales. |

### USUARIO_ROL (3 columnas)

Relación muchos a muchos: una persona puede tener varios roles.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_USUARIO` | NUMBER(10) | Sí |  | PK; FK: FK_USUARIO_ROL_USUARIO->USUARIO | Persona que tiene el rol. | 1 | Administrador | Control de permisos | Nunca es NULL (PK). | Parte de la relación N:M usuario-rol. |
| 2 | `ID_ROL` | NUMBER(10) | Sí |  | PK; FK: FK_USUARIO_ROL_ROL->ROL | Rol que tiene esa persona. | 2 | Administrador | Control de permisos | Nunca es NULL (PK). | Una persona puede tener varios roles (operario que también es ciudadano). |
| 3 | `FECHA_ASIGNACION_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Cuándo se asignó el rol. | 2026-08-01 10:00 | Oracle (DEFAULT) | Consulta administrativa | Nunca es NULL. | Metadato técnico, igual que en P3. |

### CREDENCIAL_ACCESO (7 columnas)

Contraseña de la aplicación de escritorio guardada solo como hash PBKDF2 (relación 1:1 con USUARIO).

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_USUARIO` | NUMBER(10) | Sí |  | PK; FK: FK_CREDENCIAL_USUARIO->USUARIO | Cuenta dueña de la contraseña (relación 1:1 con USUARIO). | 3 | Administrador / registro | Inicio de sesión de la app de escritorio | Nunca es NULL (PK y FK). | Separar la contraseña de los datos personales: nadie consulta USUARIO y ve hashes. |
| 2 | `HASH_CONTRASENA` | RAW(32) | Sí |  | CHECK CK_CREDENCIAL_HASH | Resultado PBKDF2-SHA256 (32 bytes) de la contraseña; nunca la contraseña. | (32 bytes binarios) | Servicio C# | Verificar la contraseña al iniciar sesión | Nunca es NULL. | Aunque roben la tabla, no se conocen las contraseñas. |
| 3 | `SAL` | RAW(16) | Sí |  | CHECK CK_CREDENCIAL_SAL | 16 bytes aleatorios propios de cada contraseña. | (16 bytes) | Servicio C# | Cálculo del hash | Nunca es NULL. | Dos personas con la misma contraseña tienen hashes distintos. |
| 4 | `ITERACIONES` | NUMBER(7) | Sí |  | CHECK CK_CREDENCIAL_ITER | Veces que se repitió el cálculo PBKDF2. | 210000 | Servicio C# | Cálculo del hash | Nunca es NULL. | CHECK >= 100000: hace lento adivinar contraseñas por fuerza bruta. |
| 5 | `INTENTOS_FALLIDOS` | NUMBER(3) | Sí | 0 | CHECK CK_CREDENCIAL_INTENTOS | Errores seguidos al iniciar sesión. | 0 | Servicio C# | Bloqueo temporal | Nunca es NULL (DEFAULT 0). | Frena ataques de adivinación. |
| 6 | `BLOQUEADA_HASTA_UTC` | TIMESTAMP(6) | No |  |  | Hasta cuándo no se permite entrar. | 2026-11-10 15:30 | Servicio C# | Inicio de sesión | NULL = no está bloqueada. | Bloqueo temporal sin borrar la cuenta. |
| 7 | `FECHA_CAMBIO_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Último cambio de contraseña. | 2026-10-01 09:00 | Servicio C# | Política de cambio | Nunca es NULL. | Auditar renovaciones de contraseña. |

### VINCULO_TELEGRAM (5 columnas)

Une la cuenta EcoAccess con la cuenta de Telegram; las bajas se conservan.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_VINCULO` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número del vínculo. | 5 | Oracle | Historial de vínculos | Nunca es NULL (PK). | Permite guardar varios vínculos (bajas) de la misma persona. |
| 2 | `ID_USUARIO` | NUMBER(10) | Sí |  | FK: FK_VINCULO_USUARIO->USUARIO | Cuenta EcoAccess vinculada. | 17 | Bot Eco al registrar | Identificar al ciudadano que escribe en Telegram | Nunca es NULL. | Une el chat con la persona. |
| 3 | `TELEGRAM_USER_ID` | NUMBER(19) | Sí |  | CHECK CK_VINCULO_TELEGRAM_ID | Número de la cuenta de Telegram (from.id). | 900000017 | Bot Eco | Reconocer a quien envía la foto | Nunca es NULL. | Índice único: una cuenta de Telegram activa no puede pertenecer a dos personas. |
| 4 | `FECHA_VINCULO_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) | CHECK CK_VINCULO_BAJA | Cuándo se vinculó. | 2026-09-26 08:00 | Bot Eco | Trazabilidad | Nunca es NULL. | Saber desde cuándo vale el vínculo. |
| 5 | `FECHA_BAJA_UTC` | TIMESTAMP(6) | No |  | CHECK CK_VINCULO_BAJA | Cuándo se desvinculó. | 2026-11-01 08:00 | Bot / administrador | Vínculo activo = fecha NULL | NULL = vínculo vigente. | Se conserva la historia en vez de borrar. |

## Módulo: M3 · Infraestructura y QR

### PUNTO_INSTALACION (6 columnas)

Estación física con un QR compartido para sus tres canecas.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_PUNTO_INSTALACION` | NUMBER(10) | Sí |  | PK; IDENTITY BY DEFAULT | Número de la estación. | 1 | Catálogo | FK de CONTENEDOR e INTERACCION | Nunca es NULL (PK). | Distinguir la estación (lugar) de la caneca (recipiente). |
| 2 | `NOMBRE` | VARCHAR2(100 CHAR) | Sí |  | UNIQUE: UQ_PUNTO_NOMBRE; CHECK CK_PUNTO_NOMBRE | Nombre único de la estación. | Estación UPC Sabanas | Administrador | Pantallas y reportes | Nunca es NULL. | UNIQUE evita dos estaciones con el mismo nombre. |
| 3 | `REFERENCIA_UBICACION` | VARCHAR2(200 CHAR) | No |  |  | Descripción del lugar. | Pasillo principal, primer piso | Administrador | Orientar al ciudadano | NULL = sin descripción. | Reemplaza las tablas COMUNA y BARRIO de V3 que P3 no usa. |
| 4 | `CODIGO_QR_ESTACION` | VARCHAR2(64 CHAR) | No |  | UNIQUE: UQ_PUNTO_QR_ESTACION; CHECK CK_PUNTO_QR_ESTACION | Texto del QR compartido por las tres canecas de la estación (parámetro start de Telegram). | EcoAccessEstacionUPC01 | Administrador | Inicio de sesión QR en el bot | NULL = estación sin QR de estación (modelo legado). | Un solo QR para tres canecas; la caneca la decide el material. |
| 5 | `ACTIVO` | NUMBER(1) | Sí | 1 | CHECK CK_PUNTO_ACTIVO | 1 si la estación funciona. | 1 | Administrador | fn_resolver_caneca_disponible | Nunca es NULL. | Desactivar una estación sin borrarla. |
| 6 | `ES_SINTETICO` | NUMBER(1) | Sí | 0 | CHECK CK_PUNTO_SINTETICO | 1 si la estación es de simulación. | 1 | Script 10 | Reportes | Nunca es NULL. | Separar simulación de instalación real. |

### CONTENEDOR (8 columnas)

Caneca de un color (blanca, verde o negra) dentro de una estación.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_CONTENEDOR` | NUMBER(10) | Sí |  | PK; UNIQUE: UQ_CONTENEDOR_CATEGORIA, UQ_CONTENEDOR_PUNTO; IDENTITY BY DEFAULT | Número de la caneca. | 12 | Catálogo | FK de órdenes, eventos y CONTENEDOR_MATERIAL | Nunca es NULL (PK). | Identificar cada recipiente físico. |
| 2 | `ID_PUNTO_INSTALACION` | NUMBER(10) | Sí |  | FK: FK_CONTENEDOR_PUNTO->PUNTO_INSTALACION; UNIQUE: UQ_CONTENEDOR_PUNTO | Estación donde está la caneca. | 1 | Administrador | Agrupar canecas por estación | Nunca es NULL. | Relación 1:N estación-canecas. |
| 3 | `NOMBRE` | VARCHAR2(100 CHAR) | Sí |  | CHECK CK_CONTENEDOR_NOMBRE | Nombre de la caneca. | UPC01 - Caneca verde | Administrador | Pantallas | Nunca es NULL. | Reconocerla en pantalla. |
| 4 | `CATEGORIA` | NUMBER(1) | Sí |  | UNIQUE: UQ_CONTENEDOR_CATEGORIA; CHECK CK_CONTENEDOR_CATEGORIA | Color: 1 BLANCA aprovechables, 2 VERDE orgánicos, 3 NEGRA no aprovechables. | 2 | Administrador | FK compuesta en CONTENEDOR_MATERIAL; resolver caneca | Nunca es NULL. | El color se DERIVA en vistas; no se guarda texto repetido. |
| 5 | `IDENTIFICADOR_QR` | VARCHAR2(64 CHAR) | No |  | UNIQUE: UQ_CONTENEDOR_QR; CHECK CK_CONTENEDOR_QR | QR propio de la caneca (modelo legado de P3: un QR por caneca). | EcoAccessCanecaLegado01 | Administrador | Compatibilidad con el QR piloto de P3 | NULL = la caneca usa el QR de la estación. | No rompe el QR antiguo de P3. |
| 6 | `IDENTIFICADOR_DISPOSITIVO` | VARCHAR2(40 CHAR) | No |  | UNIQUE: UQ_CONTENEDOR_DISPOSITIVO; CHECK CK_CONTENEDOR_DISPOSITIVO | Nombre del ESP32 que abre la tapa. | esp32-upc01-verde | Administrador | Enviar la orden al dispositivo correcto | NULL = sin hardware instalado. | UNIQUE: un ESP32 no puede controlar dos canecas. |
| 7 | `FECHA_ALTA_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Cuándo se registró la caneca. | 2026-11-01 00:00 | Oracle (DEFAULT) | Inventario | Nunca es NULL. | Trazabilidad del inventario. |
| 8 | `ESTADO` | NUMBER(1) | Sí | 2 | CHECK CK_CONTENEDOR_ESTADO | 1 En servicio, 2 Fuera de servicio, 3 Dada de baja. | 1 | Administrador (PR_CAMBIAR_ESTADO_CONTENEDOR) | Trigger 1 y resolver caneca | Nunca es NULL (DEFAULT 2). | Nace fuera de servicio para no recibir órdenes antes de configurarse. |

### CONTENEDOR_MATERIAL (3 columnas)

Qué materiales recibe cada caneca (N:M), con el color como garantía de coherencia.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_CONTENEDOR` | NUMBER(10) | Sí |  | PK; FK: FK_CONT_MAT_CONTENEDOR->CONTENEDOR | Caneca. | 11 | Administrador | Saber qué recibe | Nunca es NULL (PK). | Relación N:M caneca-material. |
| 2 | `ID_MATERIAL` | NUMBER(10) | Sí |  | PK; FK: FK_CONT_MAT_MATERIAL->MATERIAL | Material que esa caneca recibe. | 101 | Administrador | Validar el depósito (trigger 2) | Nunca es NULL (PK). | Lista blanca de materiales por caneca. |
| 3 | `CATEGORIA` | NUMBER(1) | Sí |  | FK: FK_CONT_MAT_CONTENEDOR->CONTENEDOR, FK_CONT_MAT_MATERIAL->MATERIAL; CHECK CK_CONT_MAT_CATEGORIA | Color compartido por la caneca y el material. | 1 | Administrador | Dos FK compuestas | Nunca es NULL. | Redundancia CONTROLADA: Oracle impide poner vidrio en la caneca verde sin necesidad de un trigger. |

## Módulo: M4 · Operación (QR → foto → orden → evento → depósito)

### INTERACCION (15 columnas)

Sesión iniciada al escanear el QR (o apertura manual): estación, ciudadano, sugerencia y confirmación.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_INTERACCION` | NUMBER(10) | Sí |  | PK; UNIQUE: UQ_INTERACCION_CONTENEDOR; IDENTITY ALWAYS | Número de la sesión. | 57 | Oracle | Une fotos, orden y depósito | Nunca es NULL (PK). | Cada escaneo de QR es una sesión. |
| 2 | `ID_PUNTO_INSTALACION` | NUMBER(10) | Sí |  | FK: FK_INTERACCION_CONTENEDOR->CONTENEDOR, FK_INTERACCION_PUNTO->PUNTO_INSTALACION | Estación donde se escaneó el QR. | 1 | Bot Eco | Resolver caneca | Nunca es NULL. | Con QR de estación se sabe el lugar antes que la caneca. |
| 3 | `ID_CONTENEDOR` | NUMBER(10) | No |  | FK: FK_INTERACCION_CONTENEDOR->CONTENEDOR; UNIQUE: UQ_INTERACCION_CONTENEDOR; CHECK CK_INTERACCION_AUTORIZADA | Caneca elegida para el material. | 12 | Bot (fn_resolver_caneca) | Orden de apertura | NULL = todavía no se sabe qué caneca. | FK compuesta: la caneca debe ser de esa misma estación. |
| 4 | `ID_USUARIO` | NUMBER(10) | No |  | FK: FK_INTERACCION_USUARIO->USUARIO | Ciudadano de la sesión. | 17 | Bot Eco | Historial personal | NULL = visitante sin cuenta. | Permite atender visitantes sin dar puntos. |
| 5 | `CANAL` | NUMBER(1) | Sí |  | CHECK CK_INTERACCION_CANAL; CHECK CK_INTERACCION_MANUAL | 1 Telegram, 2 Manual, 3 Simulador, 4 Telegram simulado, 5 Asistido. | 4 | Bot / escritorio | Separar pruebas de uso real | Nunca es NULL. | Mismos valores que CanalInteraccion de P3. |
| 6 | `ESTADO` | NUMBER(1) | Sí | 1 | CHECK CK_INTERACCION_AUTORIZADA; CHECK CK_INTERACCION_CIERRE; CHECK CK_INTERACCION_ESTADO; CHECK CK_INTERACCION_RECHAZO | 1 Iniciada, 2 Autorizada, 3 Con depósito, 4 Sin depósito, 5 Rechazada. | 3 | Bot / servicios | Seguimiento del flujo | Nunca es NULL (DEFAULT 1). | Sabe en qué paso quedó cada sesión. |
| 7 | `ID_MATERIAL_SUGERIDO` | NUMBER(10) | No |  | FK: FK_INTERACCION_MAT_SUG->MATERIAL; CHECK CK_INTERACCION_MANUAL; CHECK CK_INTERACCION_SUGERENCIA | Material que propuso la IA. | 101 | Bot (visión) | Comparar IA vs realidad | NULL = la IA no propuso. | La sugerencia NO es verificación. |
| 8 | `CONFIANZA_SUGERIDA` | NUMBER(4,3) | No |  | CHECK CK_INTERACCION_CONFIANZA; CHECK CK_INTERACCION_SUGERENCIA | Confianza de la IA entre 0 y 1. | 0,873 | Bot (visión) | Calidad del reconocimiento | NULL junto con el material sugerido. | CHECK: sugerencia y confianza van juntas. |
| 9 | `ID_MATERIAL_CONFIRMADO` | NUMBER(10) | No |  | FK: FK_INTERACCION_MAT_CONF->MATERIAL; CHECK CK_INTERACCION_AUTORIZADA | Material que la persona confirmó. | 101 | Bot | Orden y depósito declarado | NULL = aún no confirmó. | Obligatorio desde el estado Autorizada (CHECK). |
| 10 | `CANTIDAD_DECLARADA` | NUMBER(4) | No |  | CHECK CK_INTERACCION_CANTIDAD | Unidades que la persona dice depositar. | 2 | Bot | Estimación de peso | NULL = no se preguntó. | Nunca se toma como conteo verificado. |
| 11 | `ID_OPERARIO` | NUMBER(10) | No |  | FK: FK_INTERACCION_OPERARIO->USUARIO; CHECK CK_INTERACCION_MANUAL | Operario que hizo una apertura manual. | 2 | Escritorio (canal 2) | Auditoría de aperturas manuales | NULL = no fue manual. | CHECK: solo canal 2 lleva operario. |
| 12 | `MOTIVO` | VARCHAR2(300 CHAR) | No |  | CHECK CK_INTERACCION_MANUAL; CHECK CK_INTERACCION_MOTIVO; CHECK CK_INTERACCION_RECHAZO | Razón de una apertura manual, rechazo o cierre sin depósito. | Residuo peligroso: se orientó al punto posconsumo | Bot / operario | Reportes | NULL = sin motivo. | Obligatorio en rechazo y apertura manual. |
| 13 | `FECHA_INICIO_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) | CHECK CK_INTERACCION_CIERRE | Cuándo empezó la sesión. | 2026-11-05 15:20 | Bot | Filtros por fecha | Nunca es NULL. | Base de los reportes de uso. |
| 14 | `FECHA_CIERRE_UTC` | TIMESTAMP(6) | No |  | CHECK CK_INTERACCION_CIERRE | Cuándo terminó. | 2026-11-05 15:21 | Bot | Duración | NULL = sesión abierta (estados 1-2). | CHECK coherente con el estado. |
| 15 | `ES_SINTETICO` | NUMBER(1) | Sí | 0 | CHECK CK_INTERACCION_SINTETICO | 1 si la sesión es simulada. | 1 | Script 11 | Rótulo DATOS SINTÉTICOS | Nunca es NULL. | No confundir simulación con uso real. |

### CONSULTA_VISION (20 columnas)

Cada foto analizada por Eco. Es orientación: NUNCA cuenta como depósito.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_CONSULTA` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número de la foto analizada. | 120 | Oracle | Reportes de visión | Nunca es NULL (PK). | Cada foto atendida es un registro. |
| 2 | `CLAVE` | VARCHAR2(80 CHAR) | Sí |  | UNIQUE: UQ_CONSULTA_CLAVE | Clave única del análisis (idempotencia). | SIM-CV-57-B | Bot | Evitar registrar dos veces la misma foto | Nunca es NULL. | UNIQUE: un reintento de Telegram no duplica la consulta. |
| 3 | `ID_USUARIO` | NUMBER(10) | No |  | FK: FK_CONSULTA_USUARIO->USUARIO | Persona que envió la foto. | 17 | Bot | Historial personal | NULL = visitante. | Consultar sin cuenta está permitido. |
| 4 | `ID_INTERACCION` | NUMBER(10) | No |  | FK: FK_CONSULTA_INTERACCION->INTERACCION | Sesión QR en la que se envió. | 57 | Bot | Recorrido completo | NULL = foto sin QR (solo orientación). | Distinguir consultas sueltas de sesiones de depósito. |
| 5 | `CANAL` | NUMBER(2) | Sí |  | CHECK CK_CONSULTA_CANAL | 1 Telegram, 2 Escritorio, 3 Evaluación. | 1 | Bot | Reportes por canal | Nunca es NULL. | Igual que CanalConsultaVision de P3. |
| 6 | `MODO` | NUMBER(2) | Sí |  | CHECK CK_CONSULTA_MODO | 1 Software (sin ESP32), 2 Con dispositivo. | 2 | Bot | Saber si hubo hardware | Nunca es NULL. | En modo software no se inventan aperturas. |
| 7 | `PROVEEDOR` | VARCHAR2(80 CHAR) | Sí |  | CHECK CK_CONSULTA_PROVEEDOR | Motor de visión usado. | SIMULADOR | Bot | Comparar motores | Nunca es NULL. | Trazabilidad del resultado de IA. |
| 8 | `MODELO` | VARCHAR2(80 CHAR) | No |  |  | Versión del modelo de IA. | sin-modelo | Bot | Comparar versiones | NULL = no informado. | Absorbe la tabla ANALISIS_IMAGEN de P3. |
| 9 | `RESULTADO` | NUMBER(2) | Sí |  | CHECK CK_CONSULTA_IDENTIFICADO; CHECK CK_CONSULTA_RESULTADO | 1 Identificado, 2 Pidió otra foto, 3 No es residuo, 4 Error. | 1 | Bot | Calidad del servicio | Nunca es NULL. | Simplificación académica del código 1-10 de P3. |
| 10 | `CLAVE_OBJETO` | VARCHAR2(40 CHAR) | No |  | CHECK CK_CONSULTA_IDENTIFICADO | Objeto reconocido. | BOTELLA_PET_600ML | Bot (IA) | Buscar el peso de referencia | NULL si no se identificó. | CHECK: resultado 1 exige objeto y material. |
| 11 | `ID_MATERIAL` | NUMBER(10) | No |  | FK: FK_CONSULTA_MATERIAL->MATERIAL; CHECK CK_CONSULTA_IDENTIFICADO | Material del objeto. | 101 | Bot (IA validada con catálogo) | Caneca indicada | NULL si no se identificó. | La IA solo puede apuntar a materiales del catálogo. |
| 12 | `UNIDADES_ESTIMADAS` | NUMBER(4) | No |  | CHECK CK_CONSULTA_PESO; CHECK CK_CONSULTA_UNIDADES | Cuántos objetos se ven en la foto. | 3 | Bot (IA) | Estimación de peso | NULL = no contó. | Una foto no equivale siempre a una unidad. |
| 13 | `ESTADO_VISUAL` | NUMBER(1) | No |  | CHECK CK_CONSULTA_ESTADO_VIS | Vacío, parcial o lleno según la foto. | 1 | Bot (IA) | Elegir el peso de referencia | NULL = no aplica. | El contenido cambia mucho el peso. |
| 14 | `ID_PESO_REFERENCIAL` | NUMBER(10) | No |  | FK: FK_CONSULTA_PESO->PESO_REFERENCIAL; CHECK CK_CONSULTA_PESO | Fila del catálogo usada para estimar. | 1 | Bot | Explicar la estimación | NULL = sin estimación. | CHECK: no hay peso estimado sin catálogo. |
| 15 | `PESO_ESTIMADO_KG` | NUMBER(9,4) | No |  | CHECK CK_CONSULTA_PESO | Peso ESTIMADO (no medido) en kg. | 0,066 | Bot (fn_peso_estimado_kg) | Dashboard «kg estimados» | NULL = sin estimación. | Nunca se suma como reciclaje real. |
| 16 | `ADMISION` | NUMBER(2) | No |  | CHECK CK_CONSULTA_ADMISION | 1 Sin caneca, 2 Admitido, 3 No admitido, 4 Caneca no disponible. | 2 | Bot | Orientación | NULL = no se evaluó. | Igual que AdmisionMaterial de P3. |
| 17 | `ID_CONTENEDOR_SUGERIDO` | NUMBER(10) | No |  | FK: FK_CONSULTA_CONTENEDOR->CONTENEDOR | Caneca que se le indicó al ciudadano. | 11 | Bot | Reportes de orientación | NULL = ninguna (peligroso o no identificado). | Saber a qué caneca se envió a la persona. |
| 18 | `LATENCIA_MS` | NUMBER(7) | Sí |  | CHECK CK_CONSULTA_LATENCIA | Milisegundos que tardó el análisis. | 1850 | Bot | Rendimiento | Nunca es NULL. | Medir la experiencia del usuario. |
| 19 | `FECHA_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Cuándo se analizó. | 2026-11-05 15:20 | Bot | Filtros por fecha | Nunca es NULL. | Reportes diarios. |
| 20 | `ES_SINTETICO` | NUMBER(1) | Sí | 0 | CHECK CK_CONSULTA_SINTETICO | 1 si es simulada. | 1 | Script 11 | Rótulo DATOS SINTÉTICOS | Nunca es NULL. | No confundir simulación. |

### ORDEN_APERTURA (8 columnas)

Orden idempotente de abrir una caneca; máximo una vigente por caneca.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_ORDEN` | NUMBER(10) | Sí |  | PK; UNIQUE: UQ_ORDEN_CONTENEDOR; IDENTITY ALWAYS | Número de la orden de abrir. | 40 | Oracle | Eventos y depósito | Nunca es NULL (PK). | Abrir es un hecho distinto de depositar. |
| 2 | `ID_INTERACCION` | NUMBER(10) | Sí |  | FK: FK_ORDEN_INTERACCION->INTERACCION; UNIQUE: UQ_ORDEN_INTERACCION | Sesión que pidió abrir. | 57 | Servicio | Trazabilidad | Nunca es NULL; UNIQUE. | Una orden por sesión. |
| 3 | `ID_CONTENEDOR` | NUMBER(10) | Sí |  | FK: FK_ORDEN_CONTENEDOR->CONTENEDOR, FK_ORDEN_INTERACCION->INTERACCION; UNIQUE: UQ_ORDEN_CONTENEDOR | Caneca que debe abrirse. | 11 | Servicio | Índice de orden vigente | Nunca es NULL. | FK compuesta: es la misma caneca de la sesión. |
| 4 | `CLAVE_IDEMPOTENCIA` | VARCHAR2(64 CHAR) | Sí |  | UNIQUE: UQ_ORDEN_CLAVE; CHECK CK_ORDEN_CLAVE | Clave del pedido; repetirla devuelve la misma orden. | SIM-ORD-0057 | Servicio | Evitar dobles aperturas | Nunca es NULL; UNIQUE. | Un reintento de red no abre dos veces. |
| 5 | `ESTADO` | NUMBER(1) | Sí | 1 | CHECK CK_ORDEN_CIERRE; CHECK CK_ORDEN_ESTADO | 1 Emitida … 5 Detectada (vigentes); 6 Cerrada, 7 Vencida, 8 Fallida. | 6 | Servicio / dispositivo | Índice único de orden vigente | Nunca es NULL (DEFAULT 1). | Solo UNA orden vigente por caneca (concurrencia). |
| 6 | `FECHA_EMISION_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) | CHECK CK_ORDEN_CIERRE; CHECK CK_ORDEN_VENCIMIENTO | Cuándo se emitió. | 2026-11-05 15:20:30 | Oracle | Ventana de 10 s | Nunca es NULL. | Medir tiempos. |
| 7 | `FECHA_VENCIMIENTO_UTC` | TIMESTAMP(6) | Sí |  | CHECK CK_ORDEN_VENCIMIENTO | Después de esta hora la orden ya no vale. | 2026-11-05 15:22:30 | Servicio | Vencer órdenes | Nunca es NULL. | CHECK: vence después de emitirse. |
| 8 | `FECHA_CIERRE_UTC` | TIMESTAMP(6) | No |  | CHECK CK_ORDEN_CIERRE | Cuándo terminó la orden. | 2026-11-05 15:21:14 | Servicio | Duración | NULL = sigue vigente. | CHECK coherente con el estado. |

### EVENTO_DISPOSITIVO (8 columnas)

Lo que informa la caneca (acuse, apertura, detección, cierre, fallo, cierre sin sensor).

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_EVENTO` | NUMBER(10) | Sí |  | PK; UNIQUE: UQ_EVENTO_ORDEN; IDENTITY ALWAYS | Número del evento. | 300 | Oracle | Depósito | Nunca es NULL (PK). | Cada mensaje de la caneca se guarda. |
| 2 | `ID_CONTENEDOR` | NUMBER(10) | Sí |  | FK: FK_EVENTO_CONTENEDOR->CONTENEDOR, FK_EVENTO_ORDEN->ORDEN_APERTURA; UNIQUE: UQ_EVENTO_EXTERNO | Caneca que informó. | 11 | Adaptador del ESP32 | Trazabilidad | Nunca es NULL. | Saber qué caneca habló. |
| 3 | `ID_EXTERNO` | VARCHAR2(64 CHAR) | Sí |  | UNIQUE: UQ_EVENTO_EXTERNO; CHECK CK_EVENTO_EXTERNO | Id que pone el dispositivo. | SIM-57-DETECCION | ESP32 / simulador | Detectar retransmisiones | Nunca es NULL. | UNIQUE por caneca: un mensaje repetido no se cuenta dos veces. |
| 4 | `TIPO` | NUMBER(1) | Sí |  | CHECK CK_EVENTO_TIPO | 1 Acuse, 2 Apertura, 3 Detección, 4 Cierre, 5 Fallo, 6 Cierre sin sensor. | 3 | ESP32 / simulador | Evidencia física | Nunca es NULL. | Separa «se abrió» de «algo entró». |
| 5 | `ID_ORDEN` | NUMBER(10) | No |  | FK: FK_EVENTO_ORDEN->ORDEN_APERTURA; UNIQUE: UQ_EVENTO_ORDEN; CHECK CK_EVENTO_APLICADO | Orden a la que pertenece. | 40 | Adaptador | Depósito | NULL = evento en revisión sin orden. | CHECK: un evento aplicado siempre tiene orden. |
| 6 | `RESULTADO` | NUMBER(1) | Sí |  | CHECK CK_EVENTO_APLICADO; CHECK CK_EVENTO_RESULTADO | 1 Aplicado, 2 Revisión (tardío o sin orden). | 1 | Adaptador | Diagnóstico | Nunca es NULL. | Los eventos dudosos no crean depósitos. |
| 7 | `FECHA_DISPOSITIVO_UTC` | TIMESTAMP(6) | No |  |  | Hora según el reloj de la caneca. | 2026-11-05 15:21:10 | ESP32 | Diagnóstico de reloj | NULL = el dispositivo no la envió. | El reloj del ESP32 puede no ser fiable. |
| 8 | `FECHA_RECEPCION_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Hora del servidor al recibir. | 2026-11-05 15:21:10 | Oracle | Ventana de la orden | Nunca es NULL. | La hora que cuenta es la del servidor. |

### DEPOSITO (18 columnas)

Residuo detectado por sensor o declarado sin sensor, y su verificación independiente.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_DEPOSITO` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número del depósito. | 25 | Oracle | Verificación, puntos, auditoría | Nunca es NULL (PK). | Identificar cada residuo depositado. |
| 2 | `ID_ORDEN` | NUMBER(10) | Sí |  | FK: FK_DEPOSITO_EVENTO->EVENTO_DISPOSITIVO; UNIQUE: UQ_DEPOSITO_ORDEN | Orden que lo originó. | 40 | Servicio | Titular y caneca (por la orden) | Nunca es NULL; UNIQUE. | Un depósito por orden. |
| 3 | `ID_EVENTO` | NUMBER(10) | Sí |  | FK: FK_DEPOSITO_EVENTO->EVENTO_DISPOSITIVO; UNIQUE: UQ_DEPOSITO_EVENTO | Evento de detección o cierre sin sensor. | 300 | Servicio | Evidencia física | Nunca es NULL; UNIQUE. | FK compuesta: el evento es de ESA orden. |
| 4 | `SIN_SENSOR` | NUMBER(1) | Sí | 0 | CHECK CK_DEPOSITO_SIN_SENSOR | 1 si no hubo sensor (depósito DECLARADO); 0 si el sensor lo detectó. | 1 | Servicio | Separar declarado de detectado | Nunca es NULL (DEFAULT 0). | Igual que P3 V016: QR + foto + 10 s no prueban el ingreso. |
| 5 | `ID_MATERIAL_DECLARADO` | NUMBER(10) | Sí |  | FK: FK_DEPOSITO_MAT_DECL->MATERIAL | Material confirmado por la persona. | 101 | Servicio | Comparar con lo verificado | Nunca es NULL. | Trigger 2: la caneca debe admitirlo. |
| 6 | `CANTIDAD_DECLARADA` | NUMBER(4) | No |  | CHECK CK_DEPOSITO_CANT_DECL | Unidades declaradas. | 2 | Servicio | Estimación | NULL = no declaradas. | No es conteo verificado. |
| 7 | `PESO_ESTIMADO_KG` | NUMBER(9,4) | No |  | CHECK CK_DEPOSITO_ESTIMADO | Peso estimado con el catálogo. | 0,044 | Servicio | Dashboard kg estimados | NULL = sin estimación. | Trigger 2: debe caber en el rango del catálogo. |
| 8 | `ID_PESO_REFERENCIAL` | NUMBER(10) | No |  | FK: FK_DEPOSITO_PESO->PESO_REFERENCIAL; CHECK CK_DEPOSITO_ESTIMADO | Fila del catálogo usada. | 1 | Servicio | Explicar la estimación | NULL = sin estimación. | CHECK: no hay estimado sin catálogo. |
| 9 | `ESTADO_VERIFICACION` | NUMBER(1) | Sí | 0 | CHECK CK_DEPOSITO_ESTADO; CHECK CK_DEPOSITO_VERIFICACION | 0 Pendiente, 1 Verificado, 2 Rechazado. | 1 | Operario/administrador vía PKG_DEPOSITO | Puntos y reportes | Nunca es NULL (DEFAULT 0). | Solo los verificados cuentan como reciclaje y dan puntos. |
| 10 | `ID_MATERIAL_VERIFICADO` | NUMBER(10) | No |  | FK: FK_DEPOSITO_MAT_VERIF->MATERIAL; CHECK CK_DEPOSITO_VERIFICACION | Material que comprobó el operario. | 101 | Operario | Reportes verificados | NULL si pendiente o rechazado. | Puede diferir del declarado (mal separado). |
| 11 | `CANTIDAD_VERIFICADA` | NUMBER(4) | No |  | CHECK CK_DEPOSITO_CANT_VERIF; CHECK CK_DEPOSITO_VERIFICACION | Unidades contadas por el operario. | 2 | Operario | Puntos por unidad | NULL = no contó. | Los puntos por unidad usan este conteo. |
| 12 | `ID_VERIFICADOR` | NUMBER(10) | No |  | FK: FK_DEPOSITO_VERIFICADOR->USUARIO; CHECK CK_DEPOSITO_VERIFICACION | Quién verificó o rechazó. | 2 | Operario | Auditoría | NULL si pendiente. | Trigger 2: nunca el propio titular, siempre rol 2 o 3. |
| 13 | `FECHA_VERIFICACION_UTC` | TIMESTAMP(6) | No |  | CHECK CK_DEPOSITO_VERIFICACION | Cuándo se verificó. | 2026-11-06 09:00 | PKG_DEPOSITO | Tiempos de respuesta | NULL si pendiente. | CHECK: no antes de la detección. |
| 14 | `MOTIVO_VERIFICACION` | VARCHAR2(300 CHAR) | No |  | CHECK CK_DEPOSITO_MOTIVO; CHECK CK_DEPOSITO_VERIFICACION | Explicación (rechazo, residuo mal separado, corrección). | Se encontró papel sucio en la caneca blanca | Operario | Auditoría | NULL si no hace falta. | Obligatorio al rechazar y al verificar otro color. |
| 15 | `PESO_MEDIDO_KG` | NUMBER(9,4) | No |  | CHECK CK_DEPOSITO_MEDIDO; CHECK CK_DEPOSITO_VERIFICACION | Peso REAL medido con báscula. | 0,045 | Operario | Dashboard kg medidos | NULL = no se midió. | Nunca se mezcla con el estimado. |
| 16 | `INSTRUMENTO_MEDICION` | VARCHAR2(60 CHAR) | No |  | CHECK CK_DEPOSITO_MEDIDO | Báscula usada. | Báscula digital (SIMULADA) | Operario | Credibilidad del dato | NULL si no hubo medición. | CHECK: no hay peso medido sin instrumento. |
| 17 | `PRECISION_MEDICION_G` | NUMBER(6,1) | No |  | CHECK CK_DEPOSITO_MEDIDO | Precisión de la báscula en gramos. | 1 | Operario | Límite de exactitud | NULL si no hubo medición. | Saber cuánto se puede confiar en la medida. |
| 18 | `FECHA_DETECCION_UTC` | TIMESTAMP(6) | Sí |  | CHECK CK_DEPOSITO_VERIFICACION | Cuándo ocurrió el depósito. | 2026-11-05 15:21:10 | Servicio o trigger 2 (si llega NULL) | Ventanas de 7 días, rankings | Nunca es NULL. | Base de todos los reportes por fecha. |

## Módulo: M5 · EcoPuntos y auditoría

### REGLA_ECOPUNTOS (10 columnas)

Tarifa de EcoPuntos por material, versionada en el tiempo.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_REGLA` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número de la versión de tarifa. | 3 | Oracle | Movimientos | Nunca es NULL (PK). | Cada movimiento sabe qué tarifa usó. |
| 2 | `ID_MATERIAL` | NUMBER(10) | Sí |  | FK: FK_REGLA_MATERIAL->MATERIAL | Material al que aplica. | 101 | Administrador | regla_vigente() | Nunca es NULL. | Tarifas distintas por material. |
| 3 | `PUNTOS` | NUMBER(7) | Sí |  | CHECK CK_REGLA_PUNTOS | Puntos que se dan. | 12 | Administrador | Cálculo del crédito | Nunca es NULL. | CHECK 1..100000. |
| 4 | `POR_UNIDAD` | NUMBER(1) | Sí | 0 | CHECK CK_REGLA_POR_UNIDAD | 1 = puntos × unidades verificadas; 0 = puntos por depósito. | 1 | Administrador | Cálculo del crédito | Nunca es NULL (DEFAULT 0). | Algunos residuos se premian por unidad. |
| 5 | `DESCRIPCION` | VARCHAR2(200 CHAR) | Sí |  | CHECK CK_REGLA_DESCRIPCION | Explicación de la tarifa. | Tarifa sintética v2: 12 puntos por botella | Administrador | Reportes | Nunca es NULL. | Transparencia. |
| 6 | `DESDE_UTC` | TIMESTAMP(6) | Sí |  | CHECK CK_REGLA_INTERVALO | Inicio de la vigencia. | 2026-10-22 00:00 | Administrador | regla_vigente() | Nunca es NULL. | Versionado: una tarifa usada nunca se edita. |
| 7 | `HASTA_UTC` | TIMESTAMP(6) | No |  | CHECK CK_REGLA_INTERVALO | Fin de la vigencia. | 2026-10-22 00:00 | PKG_ECOPUNTOS al crear otra versión | regla_vigente() | NULL = versión vigente. | CHECK: fin después del inicio. |
| 8 | `ID_CREADOR` | NUMBER(10) | Sí |  | FK: FK_REGLA_CREADOR->USUARIO | Administrador que la creó. | 3 | PKG_ECOPUNTOS | Auditoría | Nunca es NULL. | Solo el rol 3 crea tarifas. |
| 9 | `CREADA_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Cuándo se registró. | 2026-07-13 00:00 | Oracle | Auditoría | Nunca es NULL. | Trazabilidad. |
| 10 | `ES_SINTETICA` | NUMBER(1) | Sí | 0 | CHECK CK_REGLA_SINTETICA | 1 si la tarifa es de prueba (no aprobada). | 1 | Script 11 | Rótulo TARIFA SINTÉTICA | Nunca es NULL. | Sin tarifa aprobada el saldo real no aumenta. |

### MOVIMIENTO_ECOPUNTOS (11 columnas)

Libro contable de EcoPuntos: créditos y reversos; el saldo es su suma.

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_MOVIMIENTO` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número del movimiento. | 10 | Oracle | Saldo | Nunca es NULL (PK). | Libro contable: nunca se borra. |
| 2 | `CLAVE_IDEMPOTENCIA` | VARCHAR2(80 CHAR) | Sí |  | UNIQUE: UQ_MOV_CLAVE | Clave única de la operación. | CRED-25-1 | PKG_ECOPUNTOS | Evitar doble crédito | Nunca es NULL; UNIQUE. | Repetir «acreditar» no duplica puntos. |
| 3 | `TIPO` | NUMBER(1) | Sí |  | CHECK CK_MOV_TIPO | 1 Crédito, 2 Reverso. | 1 | PKG_ECOPUNTOS | Saldo | Nunca es NULL. | Las correcciones se hacen con reversos. |
| 4 | `ID_DEPOSITO` | NUMBER(10) | Sí |  | FK: FK_MOV_DEPOSITO->DEPOSITO | Depósito que originó los puntos. | 25 | PKG_ECOPUNTOS | Trazabilidad | Nunca es NULL. | Todo punto tiene un depósito detrás. |
| 5 | `ID_USUARIO` | NUMBER(10) | Sí |  | FK: FK_MOV_USUARIO->USUARIO | Ciudadano titular. | 17 | PKG_ECOPUNTOS | Saldo | Nunca es NULL. | A quién pertenecen los puntos. |
| 6 | `ID_REGLA` | NUMBER(10) | Sí |  | FK: FK_MOV_REGLA->REGLA_ECOPUNTOS | Tarifa aplicada. | 3 | PKG_ECOPUNTOS | Explicación del cálculo | Nunca es NULL. | Se sabe con qué tarifa se calculó. |
| 7 | `PUNTOS` | NUMBER(8) | Sí |  | CHECK CK_MOV_TIPO | Puntos (+ crédito, − reverso). | 24 | PKG_ECOPUNTOS | Saldo = SUMA | Nunca es NULL. | CHECK de signo según el tipo. |
| 8 | `ID_MOVIMIENTO_REVERTIDO` | NUMBER(10) | No |  | FK: FK_MOV_REVERTIDO->MOVIMIENTO_ECOPUNTOS; UNIQUE: UQ_MOV_REVERTIDO; CHECK CK_MOV_TIPO | Crédito que este reverso anula. | 10 | PKG_ECOPUNTOS | Correcciones | NULL en créditos. | UNIQUE: un crédito se revierte una sola vez. |
| 9 | `ID_ACTOR` | NUMBER(10) | Sí |  | FK: FK_MOV_ACTOR->USUARIO | Operario o administrador que ejecutó. | 2 | PKG_ECOPUNTOS | Auditoría | Nunca es NULL. | Rendición de cuentas. |
| 10 | `MOTIVO` | VARCHAR2(300 CHAR) | Sí |  | CHECK CK_MOV_MOTIVO | Razón del movimiento. | Crédito por depósito verificado | PKG_ECOPUNTOS | Auditoría | Nunca es NULL. | Explicar cada cambio de saldo. |
| 11 | `FECHA_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Cuándo se registró. | 2026-11-06 09:00 | Oracle | Reportes | Nunca es NULL. | Línea de tiempo del saldo. |

### AUDITORIA_DEPOSITO (17 columnas)

Valores :OLD y :NEW de cada verificación, rechazo, corrección o reapertura (trigger 3).

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_AUDITORIA` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número de la fila de auditoría. | 1 | Trigger 3 | Consulta de auditoría | Nunca es NULL (PK). | Historia completa de verificaciones. |
| 2 | `ID_DEPOSITO` | NUMBER(10) | Sí |  | FK: FK_AUD_DEP_DEPOSITO->DEPOSITO | Depósito afectado. | 25 | Trigger 3 | Consulta por depósito | Nunca es NULL. | Saber qué depósito cambió. |
| 3 | `OPERACION` | VARCHAR2(12 CHAR) | Sí |  | CHECK CK_AUD_DEP_OPERACION | VERIFICACION, RECHAZO, CORRECCION o REAPERTURA. | VERIFICACION | Trigger 3 | Filtros de auditoría | Nunca es NULL. | Clasifica el cambio. |
| 4 | `ESTADO_ANTERIOR` | NUMBER(1) | Sí |  | CHECK CK_AUD_DEP_ESTADOS | :OLD.estado_verificacion. | 0 | Trigger 3 | Antes/después | Nunca es NULL. | Valor previo, que no cambia ni en BEFORE ni en AFTER. |
| 5 | `ESTADO_NUEVO` | NUMBER(1) | Sí |  | CHECK CK_AUD_DEP_ESTADOS | :NEW.estado_verificacion. | 1 | Trigger 3 | Antes/después | Nunca es NULL. | Valor final guardado. |
| 6 | `ID_MATERIAL_VERIFICADO_ANTERIOR` | NUMBER(10) | No |  |  | :OLD del material verificado. | NULL | Trigger 3 | Antes/después | NULL si no había. | Detectar correcciones de material. |
| 7 | `ID_MATERIAL_VERIFICADO_NUEVO` | NUMBER(10) | No |  |  | :NEW del material verificado. | 101 | Trigger 3 | Antes/después | NULL en rechazo o reapertura. | Idem. |
| 8 | `CANTIDAD_ANTERIOR` | NUMBER(4) | No |  |  | :OLD de la cantidad verificada. | 2 | Trigger 3 | Antes/después | NULL si no había. | Detectar correcciones de conteo. |
| 9 | `CANTIDAD_NUEVA` | NUMBER(4) | No |  |  | :NEW de la cantidad verificada. | 1 | Trigger 3 | Antes/después | NULL si se quitó. | Idem. |
| 10 | `PESO_MEDIDO_ANTERIOR_KG` | NUMBER(9,4) | No |  |  | :OLD del peso medido. | NULL | Trigger 3 | Antes/después | NULL si no había. | Detectar cambios de medición. |
| 11 | `PESO_MEDIDO_NUEVO_KG` | NUMBER(9,4) | No |  |  | :NEW del peso medido. | 0,045 | Trigger 3 | Antes/después | NULL si no hay medición. | Idem. |
| 12 | `ID_VERIFICADOR_ANTERIOR` | NUMBER(10) | No |  |  | :OLD del verificador. | NULL | Trigger 3 | Antes/después | NULL si nadie había verificado. | Quién había decidido antes. |
| 13 | `ID_VERIFICADOR_NUEVO` | NUMBER(10) | No |  |  | :NEW del verificador. | 2 | Trigger 3 | Antes/después | NULL en reapertura. | Quién decide ahora. |
| 14 | `ID_ACTOR` | NUMBER(10) | No |  |  | Persona de la aplicación que actuó (PKG_CONTEXTO_APP). | 2 | Trigger 3 | Rendición de cuentas | NULL = cambio sin actor de aplicación (SQL directo). | USER es la cuenta Oracle compartida; el actor es la persona. |
| 15 | `USUARIO_BD` | VARCHAR2(128 CHAR) | Sí |  |  | Cuenta Oracle de la sesión (USER). | ECOACCESS_BD | Trigger 3 | Diagnóstico técnico | Nunca es NULL. | Saber desde qué cuenta técnica se hizo. |
| 16 | `MOTIVO` | VARCHAR2(300 CHAR) | No |  |  | Razón del cambio. | Revisión de cámara: solo 1 unidad | Trigger 3 | Auditoría | NULL en verificaciones normales sin motivo. | Justificar correcciones. |
| 17 | `FECHA_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Cuándo ocurrió. | 2026-11-06 09:00 | Oracle | Línea de tiempo | Nunca es NULL. | Orden de los hechos. |

### HISTORIAL_ESTADO_CONTENEDOR (8 columnas)

Cada cambio de disponibilidad de una caneca (trigger 1).

| # | Columna | Tipo | Oblig. | Default | Restricciones | Significado | Ejemplo | Quién lo registra | Dónde se usa | Si es NULL | Por qué existe |
|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | `ID_HISTORIAL` | NUMBER(10) | Sí |  | PK; IDENTITY ALWAYS | Número del cambio. | 1 | Trigger 1 | Consulta | Nunca es NULL (PK). | Historia de disponibilidad. |
| 2 | `ID_CONTENEDOR` | NUMBER(10) | Sí |  | FK: FK_HIST_CONTENEDOR->CONTENEDOR | Caneca que cambió. | 13 | Trigger 1 | Consulta por caneca | Nunca es NULL. | Saber qué caneca. |
| 3 | `ESTADO_ANTERIOR` | NUMBER(1) | Sí |  | CHECK CK_HIST_ESTADOS | :OLD.estado. | 1 | Trigger 1 | Antes/después | Nunca es NULL. | CHECK: distinto del nuevo. |
| 4 | `ESTADO_NUEVO` | NUMBER(1) | Sí |  | CHECK CK_HIST_ESTADOS | :NEW.estado. | 2 | Trigger 1 | Antes/después | Nunca es NULL. | Idem. |
| 5 | `ID_ACTOR` | NUMBER(10) | No |  | FK: FK_HIST_ACTOR->USUARIO | Administrador que lo cambió. | 3 | Trigger 1 | Rendición de cuentas | NULL = cambio sin actor de aplicación. | Igual que en la auditoría de depósitos. |
| 6 | `USUARIO_BD` | VARCHAR2(128 CHAR) | Sí |  |  | Cuenta Oracle. | ECOACCESS_BD | Trigger 1 | Diagnóstico | Nunca es NULL. | Idem. |
| 7 | `MOTIVO` | VARCHAR2(300 CHAR) | No |  |  | Razón del cambio. | Mantenimiento de la tapa | Trigger 1 (desde el procedimiento) | Reportes de mantenimiento | NULL si no se informó. | Explicar paradas de servicio. |
| 8 | `FECHA_UTC` | TIMESTAMP(6) | Sí | SYS_EXTRACT_UTC(SYSTIMESTAMP) |  | Cuándo cambió. | 2026-11-10 08:00 | Oracle | Línea de tiempo | Nunca es NULL. | Medir disponibilidad. |

## Anexo: texto exacto de cada CHECK con nombre

| Restricción | Condición |
|---|---|
| `CK_AUD_DEP_ESTADOS` | `estado_anterior IN (0, 1, 2) AND estado_nuevo IN (0, 1, 2)` |
| `CK_AUD_DEP_OPERACION` | `operacion IN ('VERIFICACION', 'RECHAZO', 'CORRECCION', 'REAPERTURA')` |
| `CK_CONSULTA_ADMISION` | `admision IS NULL OR admision IN (1, 2, 3, 4)` |
| `CK_CONSULTA_CANAL` | `canal IN (1, 2, 3)` |
| `CK_CONSULTA_ESTADO_VIS` | `estado_visual IS NULL OR estado_visual IN (0, 1, 2, 3)` |
| `CK_CONSULTA_IDENTIFICADO` | `resultado <> 1 OR (clave_objeto IS NOT NULL AND id_material IS NOT NULL)` |
| `CK_CONSULTA_LATENCIA` | `latencia_ms >= 0` |
| `CK_CONSULTA_MODO` | `modo IN (1, 2)` |
| `CK_CONSULTA_PESO` | `peso_estimado_kg IS NULL OR (peso_estimado_kg > 0 AND id_peso_referencial IS NOT NULL AND unidades_estimadas IS NOT NULL)` |
| `CK_CONSULTA_PROVEEDOR` | `TRIM(proveedor) IS NOT NULL AND proveedor = TRIM(proveedor)` |
| `CK_CONSULTA_RESULTADO` | `resultado IN (1, 2, 3, 4)` |
| `CK_CONSULTA_SINTETICO` | `es_sintetico IN (0, 1)` |
| `CK_CONSULTA_UNIDADES` | `unidades_estimadas IS NULL OR unidades_estimadas BETWEEN 1 AND 1000` |
| `CK_CONT_MAT_CATEGORIA` | `categoria IN (1, 2, 3)` |
| `CK_CONTENEDOR_CATEGORIA` | `categoria IN (1, 2, 3)` |
| `CK_CONTENEDOR_DISPOSITIVO` | `identificador_dispositivo IS NULL OR (LENGTH(identificador_dispositivo) BETWEEN 3 AND 40 AND TRANSLATE(identificador_dispositivo, '#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', '#') IS NULL)` |
| `CK_CONTENEDOR_ESTADO` | `estado IN (1, 2, 3)` |
| `CK_CONTENEDOR_NOMBRE` | `TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2` |
| `CK_CONTENEDOR_QR` | `identificador_qr IS NULL OR (LENGTH(identificador_qr) >= 16 AND TRANSLATE(identificador_qr, '#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', '#') IS NULL)` |
| `CK_CREDENCIAL_HASH` | `LENGTH(RAWTOHEX(hash_contrasena)) = 64` |
| `CK_CREDENCIAL_INTENTOS` | `intentos_fallidos BETWEEN 0 AND 100` |
| `CK_CREDENCIAL_ITER` | `iteraciones >= 100000` |
| `CK_CREDENCIAL_SAL` | `LENGTH(RAWTOHEX(sal)) = 32` |
| `CK_DEPOSITO_CANT_DECL` | `cantidad_declarada IS NULL OR cantidad_declarada BETWEEN 1 AND 1000` |
| `CK_DEPOSITO_CANT_VERIF` | `cantidad_verificada IS NULL OR cantidad_verificada BETWEEN 1 AND 1000` |
| `CK_DEPOSITO_ESTADO` | `estado_verificacion IN (0, 1, 2)` |
| `CK_DEPOSITO_ESTIMADO` | `peso_estimado_kg IS NULL OR (peso_estimado_kg > 0 AND id_peso_referencial IS NOT NULL)` |
| `CK_DEPOSITO_MEDIDO` | ` (peso_medido_kg IS NULL AND instrumento_medicion IS NULL AND precision_medicion_g IS NULL) OR (peso_medido_kg > 0 AND TRIM(instrumento_medicion) IS NOT NULL AND precision_medicion_g > 0)` |
| `CK_DEPOSITO_MOTIVO` | `motivo_verificacion IS NULL OR (TRIM(motivo_verificacion) IS NOT NULL AND motivo_verificacion = TRIM(motivo_verificacion))` |
| `CK_DEPOSITO_SIN_SENSOR` | `sin_sensor IN (0, 1)` |
| `CK_DEPOSITO_VERIFICACION` | ` (estado_verificacion = 0 AND id_material_verificado IS NULL AND cantidad_verificada IS NULL AND id_verificador IS NULL AND fecha_verificacion_utc IS NULL AND peso_medido_kg IS NULL) OR (estado_verificacion = 1 AND id_material_verificado IS NOT NULL AND id_verificador IS NOT NULL AND fecha_verificacion_utc IS NOT NULL AND fecha_verificacion_utc >= fecha_deteccion_utc) OR (estado_verificacion = 2 AND id_material_verificado IS NULL AND cantidad_verificada IS NULL AND peso_medido_kg IS NULL AND id_verificador IS NOT NULL AND motivo_verificacion IS NOT NULL AND fecha_verificacion_utc IS NOT NULL AND fecha_verificacion_utc >= fecha_deteccion_utc)` |
| `CK_EVENTO_APLICADO` | `resultado = 2 OR id_orden IS NOT NULL` |
| `CK_EVENTO_EXTERNO` | `TRIM(id_externo) IS NOT NULL AND id_externo = TRIM(id_externo)` |
| `CK_EVENTO_RESULTADO` | `resultado IN (1, 2)` |
| `CK_EVENTO_TIPO` | `tipo BETWEEN 1 AND 6` |
| `CK_HIST_ESTADOS` | `estado_anterior IN (1, 2, 3) AND estado_nuevo IN (1, 2, 3) AND estado_anterior <> estado_nuevo` |
| `CK_INTERACCION_AUTORIZADA` | `estado NOT IN (2, 3) OR (id_material_confirmado IS NOT NULL AND id_contenedor IS NOT NULL)` |
| `CK_INTERACCION_CANAL` | `canal IN (1, 2, 3, 4, 5)` |
| `CK_INTERACCION_CANTIDAD` | `cantidad_declarada IS NULL OR cantidad_declarada BETWEEN 1 AND 1000` |
| `CK_INTERACCION_CIERRE` | ` (estado IN (1, 2) AND fecha_cierre_utc IS NULL) OR (estado IN (3, 4, 5) AND fecha_cierre_utc IS NOT NULL AND fecha_cierre_utc >= fecha_inicio_utc)` |
| `CK_INTERACCION_CONFIANZA` | `confianza_sugerida BETWEEN 0 AND 1` |
| `CK_INTERACCION_ESTADO` | `estado IN (1, 2, 3, 4, 5)` |
| `CK_INTERACCION_MANUAL` | ` (canal = 2 AND id_operario IS NOT NULL AND motivo IS NOT NULL AND id_material_sugerido IS NULL) OR (canal <> 2 AND id_operario IS NULL)` |
| `CK_INTERACCION_MOTIVO` | `motivo IS NULL OR (TRIM(motivo) IS NOT NULL AND motivo = TRIM(motivo))` |
| `CK_INTERACCION_RECHAZO` | `estado <> 5 OR motivo IS NOT NULL` |
| `CK_INTERACCION_SINTETICO` | `es_sintetico IN (0, 1)` |
| `CK_INTERACCION_SUGERENCIA` | `(id_material_sugerido IS NULL AND confianza_sugerida IS NULL) OR (id_material_sugerido IS NOT NULL AND confianza_sugerida IS NOT NULL)` |
| `CK_MATERIAL_CATEGORIA` | `categoria IN (1, 2, 3, 4)` |
| `CK_MATERIAL_CODIGO` | `LENGTH(codigo) >= 2 AND INSTR('ABCDEFGHIJKLMNOPQRSTUVWXYZ', SUBSTR(codigo, 1, 1)) > 0 AND TRANSLATE(codigo, '#ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_', '#') IS NULL` |
| `CK_MATERIAL_HABILITADO` | `habilitado IN (0, 1)` |
| `CK_MATERIAL_NOMBRE` | `TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2` |
| `CK_MOV_MOTIVO` | `TRIM(motivo) IS NOT NULL AND motivo = TRIM(motivo)` |
| `CK_MOV_TIPO` | ` (tipo = 1 AND puntos > 0 AND id_movimiento_revertido IS NULL) OR -- 1 Crédito (tipo = 2 AND puntos < 0 AND id_movimiento_revertido IS NOT NULL)` |
| `CK_ORDEN_CIERRE` | ` (estado BETWEEN 1 AND 5 AND fecha_cierre_utc IS NULL) OR (estado BETWEEN 6 AND 8 AND fecha_cierre_utc IS NOT NULL AND fecha_cierre_utc >= fecha_emision_utc)` |
| `CK_ORDEN_CLAVE` | `LENGTH(clave_idempotencia) >= 8 AND clave_idempotencia = TRIM(clave_idempotencia)` |
| `CK_ORDEN_ESTADO` | `estado BETWEEN 1 AND 8` |
| `CK_ORDEN_VENCIMIENTO` | `fecha_vencimiento_utc > fecha_emision_utc` |
| `CK_PESO_CLAVE` | `LENGTH(clave_objeto) >= 2 AND TRANSLATE(clave_objeto, '#ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_', '#') IS NULL` |
| `CK_PESO_DESCRIPCION` | `TRIM(descripcion_objeto) IS NOT NULL AND descripcion_objeto = TRIM(descripcion_objeto)` |
| `CK_PESO_ESTADO_VISUAL` | `estado_visual IN (0, 1, 2, 3)` |
| `CK_PESO_FUENTE` | `TRIM(fuente) IS NOT NULL AND fuente = TRIM(fuente)` |
| `CK_PESO_ORDEN` | `gramos_minimo > 0 AND gramos_minimo <= gramos_tipico AND gramos_tipico <= gramos_maximo` |
| `CK_PUNTO_ACTIVO` | `activo IN (0, 1)` |
| `CK_PUNTO_NOMBRE` | `TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2` |
| `CK_PUNTO_QR_ESTACION` | `codigo_qr_estacion IS NULL OR (LENGTH(codigo_qr_estacion) >= 16 AND TRANSLATE(codigo_qr_estacion, '#ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_-', '#') IS NULL)` |
| `CK_PUNTO_SINTETICO` | `es_sintetico IN (0, 1)` |
| `CK_REGLA_DESCRIPCION` | `TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion)` |
| `CK_REGLA_INTERVALO` | `hasta_utc IS NULL OR hasta_utc > desde_utc` |
| `CK_REGLA_POR_UNIDAD` | `por_unidad IN (0, 1)` |
| `CK_REGLA_PUNTOS` | `puntos BETWEEN 1 AND 100000` |
| `CK_REGLA_SINTETICA` | `es_sintetica IN (0, 1)` |
| `CK_ROL_CODIGO` | `codigo IN (1, 2, 3)` |
| `CK_ROL_DESCRIPCION` | `TRIM(descripcion) IS NOT NULL AND descripcion = TRIM(descripcion)` |
| `CK_USUARIO_ACTIVO` | `activo IN (0, 1)` |
| `CK_USUARIO_CONSENTIMIENTO` | ` (version_consentimiento IS NULL AND fecha_consentimiento_utc IS NULL) OR (version_consentimiento IS NOT NULL AND fecha_consentimiento_utc IS NOT NULL)` |
| `CK_USUARIO_CORREO_FORMA` | `INSTR(correo_electronico, '@') > 1 AND INSTR(correo_electronico, '.', INSTR(correo_electronico, '@')) > INSTR(correo_electronico, '@') + 1 AND INSTR(correo_electronico, ' ') = 0` |
| `CK_USUARIO_CORREO_MIN` | `correo_electronico = LOWER(correo_electronico)` |
| `CK_USUARIO_NOMBRE` | `TRIM(nombre) IS NOT NULL AND nombre = TRIM(nombre) AND LENGTH(nombre) >= 2` |
| `CK_USUARIO_SINTETICO` | `es_sintetico IN (0, 1)` |
| `CK_VINCULO_BAJA` | `fecha_baja_utc IS NULL OR fecha_baja_utc >= fecha_vinculo_utc` |
| `CK_VINCULO_TELEGRAM_ID` | `telegram_user_id > 0` |
