# EcoAccess — Base de Datos (profesor Amílkar) · 01 Diagnóstico de solo lectura

**Fecha de elaboración:** 10/11/2026 (hora de Colombia).
**Alcance de esta etapa:** solo lectura. No se modificaron Oracle Cloud, Oracle local, el repositorio P3 ni la rama `main`.

**Convención de etiquetas:**
- **[MEDIDO]**: lo comprobé con un comando en este equipo o en Git.
- **[DOCUMENTADO]**: lo afirma un archivo del repositorio, pero no lo pude comprobar.
- **[PENDIENTE]**: falta comprobarlo. Indico el motivo y el comando que lo verifica.

---

## 1. Fuentes revisadas y versión exacta

### 1.1 Repositorio P3 (prioridad A) [MEDIDO]
Hice la consulta con `git ls-remote` contra GitHub a las 23:30 COT. Revisé el código en un clon temporal, sin tocar tu copia local.

| Rama | SHA | Fecha del último commit | Relación |
|---|---|---|---|
| `main` | `d30de6d` | 02/11/2026 | Base. |
| `desarrollo/auditoria-tercer-corte` | `552f2c2` | 10/11/2026 22:18 | Lleva 82 commits por delante de `main` y ninguno por detrás. Es el mismo SHA que en la revisión anterior. |
| `desarrollo/telegram-doble-ia` (PR 11) | `afede69` | 10/11/2026 23:30 | Es **la rama más reciente**: tiene todo lo de auditoría más 2 commits. |
| `recovery/ecoaccess-oracle-cloud` (tu copia local) | `8a14d3b` | 09/11/2026 | **Divergente**: tiene 3 commits propios y le faltan 47 commits de `telegram-doble-ia`. |

Los dos commits nuevos de `telegram-doble-ia` son:
- `3d1899a`: copia V021 y V022 byte a byte, con la misma huella SHA-256 que está registrada en la nube, y documenta la «Fase 0».
- `afede69`: matriz de permisos de los tres roles, probada contra los servicios reales.

**Base usada en este diagnóstico:** `desarrollo/telegram-doble-ia@afede69`, porque contiene a `auditoria-tercer-corte` completa.

**Hallazgo importante sobre la configuración de tres canecas.** La migración V021 crea `CONFIGURACION_CANECA` y está tanto en `telegram-doble-ia` como en `recovery`. Sin embargo, el código C# que la usa (`eeaf6b5`, «la estación admite tres canecas…») solo existe en `recovery/ecoaccess-oracle-cloud`. En `telegram-doble-ia` ningún repositorio C# consulta `CONFIGURACION_CANECA`. P3 tiene, por tanto, **dos líneas de desarrollo que todavía no se han unido**. Para Base de Datos esto no es un problema: el modelo académico debe representar ambas ideas.

### 1.2 Oracle realmente instalado (prioridad B)

**Oracle local [MEDIDO]**
- Producto: Oracle AI Database 26ai Free. El listener informa la versión `23.26.3.0.0`.
- Servicios de Windows: `OracleServiceFREE` y `OracleOraDB23Home1TNSListener` están en ejecución.
- Listener: host `GUTIOME`, puerto `1521`. Publica los servicios `FREE`, `FREEXDB` y `freepdb1` (la PDB).
- Herramientas encontradas: `sqlplus`, `sql` (SQLcl), `tnsping` y `lsnrctl` en `C:\app\Usuario\product\26ai\dbhomeFree\bin`.

**SQL Developer [MEDIDO]**
- Versión `26.2.0.186.2220`, en `C:\Oracle\sqldeveloper-26.2.0.186.2220-x64`. Es el icono de tu captura. Trae Data Modeler integrado.
- Conexiones guardadas (solo nombres, sin claves):
  - `system`, `jorge` y `hr`: las tres en `localhost:1521/FREEPDB1`;
  - `EcoAccess Cloud auditoria`: usuario `ECOACCESS_P3_APP`, tipo CLOUD (wallet), alias `ecoaccess_tp`.

**Power BI Desktop [MEDIDO]**
- Versión `2.159.1231.0` (octubre de 2026), instalado en `C:\Program Files\Microsoft Power BI Desktop`.

**Visual Studio y .NET [MEDIDO]**
- Hay carpetas de Visual Studio y está `dotnet`.

**Herramientas que no están [MEDIDO]**
- Python, pandoc y LibreOffice no se encontraron. Esto influye en cómo generaré el Word y el PDF (ver /9).

**Oracle Cloud [PENDIENTE]**
- No tengo la credencial y no la pediré por chat.
- Según `docs/operacion/PROGRESO.md` [DOCUMENTADO]:
  - el esquema `ECOACCESS_P3_APP` tiene 27 tablas, 21 usuarios (todos Ciudadano), 0 depósitos, 0 reglas y 0 consultas de visión;
  - las migraciones registradas son V001, C001, V002–V008, D001, V009–V020, C002, C003, V021, D002, V022 y V023.
- Cómo comprobarlo: abre la conexión `EcoAccess Cloud auditoria` en SQL Developer y ejecuta `03_GUIAS_Y_VALIDACION/02_DIAGNOSTICO_SOLO_LECTURA.sql`. Así ninguna credencial pasa por mí.

### 1.3 Paquete adjunto (prioridad C) [MEDIDO]
Leí el ZIP completo: 116 archivos, el prompt maestro (idéntico al `.txt` de Descargas), las decisiones, la propuesta de cuatro disparadores, la rúbrica, la V3 saneada, la práctica HR/C# y los ejercicios 32–34.

---

## 2. Inventario de P3 (código vigente)

### 2.1 Las 27 tablas y su origen [MEDIDO en DDL / DOCUMENTADO en la nube]

| # | Tabla | Migración | Para qué existe en P3 | Usos en `EcoAccess.P3.Datos` (SQL) |
|---|---|---|---|---|
| 1 | ROL | V001 | Catálogo de los 3 roles (1/2/3) | 15 |
| 2 | USUARIO | V001 | Identidad mínima y consentimiento | 20 |
| 3 | USUARIO_ROL | V001 | Relación N:M usuario–rol | 14 |
| 4 | MATERIAL | V001 (+V012) | Catálogo validado (categoría 1–4) | 27 |
| 5 | PUNTO_INSTALACION | V001 | Estación o lugar físico | 8 |
| 6 | CONTENEDOR | V001 | Caneca con QR público (estado 1/2/3) | 18 |
| 7 | CONTENEDOR_MATERIAL | V001 | Materiales que admite cada caneca | 11 |
| 8 | CREDENCIAL_ACCESO | V002 | Hash PBKDF2 + sal + bloqueo | 7 |
| 9 | INTERACCION | V003 (+V009, V014, V017) | Visita o sesión a una caneca y sugerencia de la IA | 30 |
| 10 | ORDEN_APERTURA | V003 | Orden idempotente, como máximo 1 vigente por caneca | 25 |
| 11 | EVENTO_DISPOSITIVO | V003 (+V016) | Acuse, apertura, detección, cierre, fallo y cierre sin sensor | 13 |
| 12 | DEPOSITO | V003 (+V011, V016) | Depósito detectado o sin sensor y su verificación | 22 |
| 13 | VINCULO_TELEGRAM | V004 | Cuenta ↔ Telegram, con historial de bajas | 5 |
| 14 | TOKEN_VINCULACION | V004 | Token de registro de un solo uso (hash) | 6 |
| 15 | CONVERSACION_TELEGRAM | V004 | Estado del diálogo del bot, con versión optimista | 5 |
| 16 | UPDATE_TELEGRAM | V004 | Control de updates repetidos | 1 |
| 17 | REGLA_ECOPUNTOS | V005 (+V011) | Tarifa versionada por material | 3 |
| 18 | OPERACION_ECOPUNTOS | V005 | Operación idempotente (crédito, reverso, corrección) | 4 |
| 19 | MOVIMIENTO_ECOPUNTOS | V005 | Libro contable de puntos | 9 |
| 20 | VINCULACION_PENDIENTE | V007 | Vincular una cuenta existente mediante un código | 4 |
| 21 | ANALISIS_IMAGEN | V008 | Trazabilidad del modelo de visión dentro de una interacción | 5 |
| 22 | OPINION | V010 | Estrellas por día (participante anonimizado) | 3 |
| 23 | CONSENTIMIENTO_VISION_EXTERNA | V018 | Permiso para usar una IA externa | 2 |
| 24 | COMENTARIO_CIUDADANO | V020 | Quejas y sugerencias | 1 |
| 25 | CONFIGURACION_CANECA | V021 | Color, ESP32 y ubicación de cada caneca | **0 en esta rama** |
| 26 | CONSULTA_VISION | V023 | Foto atendida por Eco; **no es depósito** | 3 |
| 27 | ECO_MIGRACION_APLICADA | instalador | Registro de migraciones aplicadas | — |

**Objetos programables de P3 [MEDIDO]:**
- Vistas: `EA_R_DEPOSITOS`, `EA_R_PUNTOS`, `EA_R_OPINIONES`, `EA_R_BASE_DEPOSITOS`, `EA_R_BASE_PUNTOS`, `EA_R_SOLICITUDES`, `EA_R_MATERIALES`, `EA_R_PARTICIPACION_MENSUAL`, `EA_R_CONSULTAS_VISION` y `VISTA_CANECAS_ESTACION`.
- Paquete `PKG_ECOACCESS_STATS`, con `consultar_resumen` y un `SYS_REFCURSOR`.
- Función `FN_RESOLVER_CANECA_DISPONIBLE`, con los errores ORA-20810 a 20819.
- Procedimiento `PR_CONSULTAR_ESTADISTICAS`.
- **Un solo trigger:** `TR_ECOP_UNIDADES` (BEFORE INSERT en MOVIMIENTO_ECOPUNTOS, WHEN NEW.tipo=1).
- Ninguna secuencia explícita: todas las claves usan `IDENTITY`.

### 2.2 Enumeraciones C# que el modelo debe respetar [MEDIDO]

| Enum | Valores |
|---|---|
| `CodigoRol` | 1 Ciudadano, 2 Operario, 3 Administrador |
| `CategoriaMaterial` | 1 Aprovechable (**BLANCA**), 2 Orgánico aprovechable (**VERDE**), 3 No aprovechable (**NEGRA**), 4 Peligroso (ruta especial, sin caneca) |
| `EstadoContenedor` | 1 EnServicio, 2 FueraDeServicio, 3 DadoDeBaja |
| `CanalInteraccion` | 1 Telegram, 2 Manual, 3 Simulador, 4 TelegramSimulado, 5 Asistido |
| `EstadoInteraccion` | 1 Iniciada, 2 Autorizada, 3 ConDeposito, 4 SinDeposito, 5 Rechazada |
| `EstadoOrdenApertura` | 1 Emitida … 5 Detectada (vigentes); 6 Cerrada, 7 Vencida, 8 Fallida |
| `TipoEventoDispositivo` | 1 Acuse, 2 Apertura, 3 Deteccion, 4 Cierre, 5 Fallo, 6 CierreSinSensor |

**Ventana de apertura [MEDIDO]:** `MensajesTelegram.VentanaDepositoSegundos = 10`. Los 10 s se cuentan desde la apertura confirmada por el firmware. Sin ESP32 el flujo es simulado (`FLUJO_ECO_DEFINITIVO.md`, pasos 11–12).

### 2.3 Roles y permisos reales [MEDIDO]
`MatrizPermisos.cs` declara 12 operaciones protegidas:
- El Ciudadano solo puede hacer las operaciones «mis …».
- El Operario puede buscar ciudadanos, ver estadísticas y los ecopuntos de otros, y listar reglas.
- El Administrador puede, además, gestionar usuarios y roles, materiales, reglas, el informe institucional y el diagnóstico de Oracle.
- Ningún rol lee hashes ni contraseñas.

Por tanto, `USUARIO_ROL` (N:M) queda **confirmado** en el código.

---

## 3. Inventario de V3 (histórica) [MEDIDO en los scripts saneados]

V3 tiene 27 tablas:
- territorio: COMUNA y BARRIO;
- usuarios: USUARIO, con los subtipos CIUDADANO, SUPERVISOR, OPERARIO y ADMINISTRADOR;
- organizaciones: ORGANIZACION y CIUDADANO_ORGANIZACION;
- RFID: ESTADO_RFID, TARJETA_RFID y CAMBIO_ESTADO_RFID;
- operación: MATERIAL, PUNTO_RECEPCION, PUNTO_MATERIAL, DEPOSITO, RECOLECCION e INCIDENCIA;
- incentivos: REGLA_ECOPUNTOS, RECOMPENSA, CANJE_RECOMPENSA y MOVIMIENTO_ECOPUNTOS;
- seguridad: PERFIL, PERMISO, PERFIL_PERMISO, CUENTA_ACCESO y AUDITORIA.

Objetos programables de V3:
- 12 vistas;
- 2 paquetes: `PKG_ECO_CTRL` y `PKG_ECOACCESS`;
- 22 triggers, de los cuales 9 son `TRG_AUD_*` de auditoría genérica por tabla.

Usa `localhost:1521/XEPDB1` como referencia histórica.

---

## 4. Matriz de brechas V3 ↔ P3 ↔ modelo final

Formato de cada fila: decisión → evidencia → justificación.

| Tema V3 | Equivalente en P3 | Decisión para la versión final de BD | Evidencia y justificación |
|---|---|---|---|
| COMUNA, BARRIO | No existe | **Eliminar** | P3 no filtra por barrio. Un texto `referencia_ubicacion` en la estación basta. |
| Subtipos CIUDADANO, OPERARIO, SUPERVISOR, ADMINISTRADOR (especialización total/solapada) | ROL + USUARIO_ROL (N:M) | **Reemplazar por N:M** | `CodigoRol.cs` y `MatrizPermisos.cs`. Los subtipos de V3 no tenían atributos propios que P3 use. El rol SUPERVISOR no existe en P3. |
| ORGANIZACION, CIUDADANO_ORGANIZACION | No existe | **Eliminar** | No hay requisito ni pantalla en P3. |
| ESTADO_RFID, TARJETA_RFID, CAMBIO_ESTADO_RFID | QR público + VINCULO_TELEGRAM | **Eliminar** | El QR identifica la estación o la caneca; la identidad viene del vínculo con Telegram. |
| PUNTO_RECEPCION, PUNTO_MATERIAL | PUNTO_INSTALACION, CONTENEDOR, CONTENEDOR_MATERIAL | **Reemplazar** | V3 no distinguía la estación de la caneca, y P3 necesita distinguirlas para las tres canecas. |
| DEPOSITO (por RFID) | INTERACCION → ORDEN_APERTURA → EVENTO_DISPOSITIVO → DEPOSITO | **Conservar la cadena de P3 y agregar el estado de verificación** | La foto, la orden, la apertura y el depósito son hechos diferentes (V003, V016, V023). |
| RECOLECCION, INCIDENCIA | No existe | **Eliminar** (queda como trabajo futuro) | No hay flujo de recolección en P3. |
| REGLA_ECOPUNTOS, MOVIMIENTO_ECOPUNTOS | Iguales, más OPERACION_ECOPUNTOS | **Conservar y simplificar** (la clave idempotente pasa al movimiento) | `ServicioEcopuntos` exige verificación. La tabla OPERACION se fusiona: ver /6. |
| RECOMPENSA, CANJE_RECOMPENSA | No existe | **Eliminar** | P3 no tiene canjes. No se inventan. |
| PERFIL, PERMISO, PERFIL_PERMISO, CUENTA_ACCESO | ROL + CREDENCIAL_ACCESO; los permisos están en código | **Reemplazar** | Los permisos viven en `MatrizPermisos.cs`. Llevarlos a tablas duplicaría la regla. |
| AUDITORIA genérica + 9 `TRG_AUD_*` | **No existe tabla de auditoría en P3** (la «auditoría de recorrido» es una consulta) | **Nueva, pero específica**: auditoría de verificación de depósitos e historial de estado de la caneca | Son las dos auditorías que pide la tarea de triggers. Una auditoría genérica por tabla sería una base de datos «más grande sin justificación». |
| No existía | CONSULTA_VISION (V023) | **Conservar y ampliar** con unidades, estado visual y peso estimado | Es el requisito de pesos del prompt (/3 «IA y pesos»). |
| No existía | Catálogo de pesos | **Nueva: PESO_REFERENCIAL** | P3 tiene `peso_estimado_kg`, pero no dice de dónde sale ese número. Sin catálogo, la estimación no se puede defender. |

### Discrepancias detectadas que el diseño corrige

1. **Ningún trigger de auditoría en P3.** La tarea del docente exige triggers del proyecto, así que el esquema académico los incorpora sin tocar P3.
2. **El significado de DEPOSITO en P3** es «existe solo si el sensor detectó un objeto (RN-08)». V016 lo relajó con `sin_sensor=1`. En el modelo final esto queda explícito: el origen del depósito puede ser sensor, cierre sin sensor o simulación, y el estado de verificación es aparte.
3. **El QR está en la caneca y no en la estación.** La caneca piloto 1093 tiene el QR oficial y no está configurada. La estación DEMO (canecas 1, 289 y 290) es la que tiene tres colores. El modelo final pone el QR de estación en `PUNTO_INSTALACION` y conserva el QR legado de la caneca como opcional.
4. **`CONFIGURACION_CANECA` es una tabla 1:1 con CONTENEDOR.** Existe así por la migración aditiva. En el modelo académico sus columnas pasan a CONTENEDOR (3FN, porque dependen solo de `id_contenedor`), y la equivalencia con P3 se documenta.
5. **El DER y el diccionario de P3 están desactualizados.** `database/01_modelo/DICCIONARIO_DATOS.md` solo cubre V001. El diccionario nuevo se generará desde `USER_TAB_COLUMNS`.
6. **El docente está escrito «Amílcar» en V3.** Se usará **Amílkar**, como indicaste.

---

## 5. Flujo vigente (mapa)

```
Ciudadano ──escanea QR──► Telegram «Eco» ──/start <qr>──► INTERACCION (estación, canal, estado=Iniciada)
                                    │
                         envía foto(s)
                                    ▼
                          CONSULTA_VISION  (objeto, material, unidades, estado visual,
                                            peso estimado por PESO_REFERENCIAL) ── NO es depósito
                                    │ material válido y caneca resuelta (FN_RESOLVER_CANECA)
                                    ▼
                          ORDEN_APERTURA (idempotente, 1 vigente por caneca)
                                    │
           ┌──────────── con ESP32 ─┴─ sin ESP32 (modo software) ───────────┐
           ▼                                                                 ▼
 EVENTO_DISPOSITIVO acuse→apertura→(10 s)→detección/cierre       solo orientación: NO se crea
           │                                                     apertura ni depósito, salvo
           ▼                                                     simulación marcada
 DEPOSITO (origen SENSOR / SIN_SENSOR / SIMULACION, estado = PENDIENTE)
           │ operario/administrador distinto del titular
           ▼
 DEPOSITO estado = VERIFICADO / RECHAZADO ──trigger──► AUDITORIA_DEPOSITO
           │ tarifa vigente aprobada
           ▼
 MOVIMIENTO_ECOPUNTOS (crédito idempotente; reverso si se corrige)
```

Separación de evidencias, con los literales (a)–(g) del prompt:

| Literal | Hecho | Dónde queda registrado |
|---|---|---|
| (a) | Consulta visual | CONSULTA_VISION |
| (b) | Sesión QR | INTERACCION |
| (c) | Material sugerido | `INTERACCION.id_material_sugerido` y `CONSULTA_VISION.id_material` |
| (d) | Orden | ORDEN_APERTURA |
| (e) | Apertura | EVENTO_DISPOSITIVO tipo 2 |
| (f) | Depósito declarado o sin sensor | DEPOSITO con `origen` |
| (g) | Depósito verificado | `DEPOSITO.estado_verificacion = 1` |

---

## 6. Propuesta relacional del esquema académico `ECOACCESS_BD` (19 tablas)

El modelo usa los mismos nombres y significados que P3 donde el concepto es el mismo, y es menor que P3 (27 tablas) y que V3 (27 tablas).

| Módulo | Tabla | Origen | Por qué existe |
|---|---|---|---|
| Seguridad | ROL | = P3 | Hay 3 roles fijos con CHECK 1/2/3. |
| | USUARIO | = P3, más `es_sintetico` | Persona. Marca que separa los datos ficticios. |
| | USUARIO_ROL | = P3 | N:M: un operario también puede ser ciudadano. |
| | CREDENCIAL_ACCESO | = P3 | Demuestra que la contraseña nunca se guarda (solo hash, sal e iteraciones). |
| | VINCULO_TELEGRAM | = P3 | Identifica al ciudadano en Telegram. Se conservan las bajas. |
| Infraestructura | PUNTO_INSTALACION | P3, más `codigo_qr_estacion` | Estación con un QR para tres canecas. |
| | CONTENEDOR | P3, más `categoria` e `identificador_dispositivo` (de CONFIGURACION_CANECA) | La caneca VERDE, BLANCA o NEGRA. Hay un índice único para que una estación tenga como máximo una caneca de cada color activa. |
| | MATERIAL | = P3 | Catálogo que valida lo que dice la IA. |
| | CONTENEDOR_MATERIAL | = P3 | Qué admite cada caneca (N:M). |
| | PESO_REFERENCIAL | **Nueva** | Gramos mínimos, típicos y máximos por objeto y estado visual, con su fuente. |
| Operación | INTERACCION | P3, más `id_punto_instalacion` | Sesión QR. La caneca puede quedar NULL hasta que se resuelve. |
| | CONSULTA_VISION | P3 V023, más unidades, estado visual, peso estimado y `id_interaccion` | Lo que vio la IA. Nunca se suma como reciclaje. |
| | ORDEN_APERTURA | = P3 | Idempotencia y 1 orden vigente por caneca. |
| | EVENTO_DISPOSITIVO | = P3 | Hechos que informa la caneca, incluido el tipo 6 «cierre sin sensor». |
| | DEPOSITO | P3, más `origen`, `estado_verificacion`, `motivo_verificacion` y métodos de peso | Separa lo declarado de lo verificado y lo estimado de lo medido. |
| Incentivos | REGLA_ECOPUNTOS | = P3 | Tarifa versionada (vigencia desde y hasta). |
| | MOVIMIENTO_ECOPUNTOS | P3, fusionada con OPERACION | Libro contable: crédito o reverso, con clave idempotente UNIQUE. |
| Auditoría | AUDITORIA_DEPOSITO | **Nueva** | Valores `:OLD` y `:NEW` de cada verificación o corrección (trigger 3). |
| | HISTORIAL_ESTADO_CONTENEDOR | **Nueva** | Cambios de disponibilidad de la caneca (trigger 1). |

**Tablas de P3 que se excluyen de la versión de BD**

Se excluyen porque son infraestructura del bot, no parte del dominio que se defiende en Base de Datos. Se listan en el informe como «fuera de alcance», no como errores:
- TOKEN_VINCULACION, VINCULACION_PENDIENTE, CONVERSACION_TELEGRAM y UPDATE_TELEGRAM: maquinaria interna del bot;
- OPINION y COMENTARIO_CIUDADANO: atención al ciudadano;
- CONSENTIMIENTO_VISION_EXTERNA: el consentimiento ya queda en USUARIO (versión y fecha) y no se guardan fotos;
- ANALISIS_IMAGEN: queda absorbida por CONSULTA_VISION (proveedor y modelo);
- ECO_MIGRACION_APLICADA: es del instalador.

**Esquema separado `ECOACCESS_PRACTICAS`** (para no mezclar la docencia con el negocio):
- `PRODUCTO_PRACTICA`: precio final;
- `BD_EVENTO_ACADEMICO`: secuencia y trigger;
- `NOTA_PRACTICA`: ejemplo 34, ORA-20001;
- `EMPLEADO_PRACTICA`: copia de HR.EMPLOYEES para el CRUD sin DELETE, sin modificar HR;
- `AUDITORIA_SALARIO`: trigger libre.

**Convenciones heredadas de P3:**
- `VARCHAR2(n CHAR)`;
- verdadero/falso como `NUMBER(1)` con CHECK (0, 1);
- fechas en `TIMESTAMP` UTC, convertidas a `America/Bogota` solo al consultar;
- sin `ON DELETE CASCADE`;
- PK `IDENTITY`, salvo la práctica de secuencia.

---

## 7. Los cuatro disparadores

| # | Nombre propuesto | Momento y evento | Qué resuelve que un CHECK no puede | Prueba positiva / negativa | ¿Se duplica con C#? |
|---|---|---|---|---|---|
| 1 | `TRG_CONTENEDOR_AUD_ESTADO` | AFTER UPDATE OF estado ON contenedor, FOR EACH ROW, WHEN (OLD.estado <> NEW.estado) | Guarda el estado anterior, el nuevo, el actor y la fecha en HISTORIAL_ESTADO_CONTENEDOR | Pasar de 2 a 1 crea una fila; un UPDATE con el mismo estado no crea ninguna | No: P3 no guarda historial de estado |
| 2 ★ | `TRG_DEPOSITO_COHERENCIA` | BEFORE INSERT OR UPDATE ON deposito, FOR EACH ROW | Reglas **entre tablas**: (a) el verificador tiene rol 2 o 3; (b) el verificador ≠ el titular (el titular se obtiene de orden → interacción); (c) la caneca admite el material declarado y el verificado; (d) un peso medido exige `metodo_medicion` | Verificación válida pasa; un autoverificador da ORA-20101; un material no admitido da ORA-20102 | Parcial: P3 valida (a) y (b) en el servicio C#. El trigger es la **segunda línea de defensa** si alguien escribe SQL directo. Se documenta como duplicación intencional y acotada. |
| 3 ★ | `TRG_DEPOSITO_AUD_VERIFICACION` | AFTER UPDATE OF estado_verificacion, id_material_verificado, cantidad_verificada, peso_medido_kg ON deposito, FOR EACH ROW | Escribe en AUDITORIA_DEPOSITO los valores `:OLD` y `:NEW`. Solo registra si hubo un cambio real, comparando con un manejo explícito de NULL. | Al verificar, aparece 1 fila de auditoría; un UPDATE sin cambio real no deja fila | No. **No acredita EcoPuntos**: eso lo hace solo `PKG_ECOPUNTOS.acreditar`. |
| 4 | `TRG_DEPOSITO_BLOQUEO_CERRADO` | BEFORE UPDATE ON deposito, FOR EACH ROW, WHEN (OLD.estado_verificacion <> 0) | Impide modificar una verificación cerrada, salvo cuando el procedimiento `PKG_DEPOSITO.corregir_verificacion` (que exige un motivo) activa la variable de paquete | Un UPDATE directo da ORA-20103; la corrección por el procedimiento pasa y queda auditada | No |

★ son los dos recomendados para exponer (validación BEFORE y auditoría AFTER), porque juntos muestran `BEFORE` y `AFTER`, `:OLD` y `:NEW`, `FOR EACH ROW`, `WHEN` sin dos puntos y `RAISE_APPLICATION_ERROR`. Implementaré los cuatro y los probaré, porque el 4 protege los EcoPuntos.

Reglas aplicadas en todos:
- sin `COMMIT` dentro del trigger;
- sin consultar `DEPOSITO` desde su propio trigger, para evitar ORA-04091;
- el actor se toma del procedimiento (variable de paquete) y, si no lo hay, de `SYS_CONTEXT('USERENV','CLIENT_IDENTIFIER')`. Se aclara que `USER` es la cuenta Oracle, no la persona.

---

## 8. Riesgos de compatibilidad

| Riesgo | Mitigación |
|---|---|
| Instalar el esquema académico encima de P3 | Se usa un usuario nuevo `ECOACCESS_BD` en **FREEPDB1 local**. Nunca se usa `ECOACCESS_P3_APP` ni la nube. |
| Valores de enumeración diferentes a los de C# | Se copian exactamente los de /2.2 y la equivalencia queda en el diccionario. |
| Las columnas nuevas (`origen`, `estado_verificacion`) no existen en P3 | Se documentan como **propuesta de migración V024** para P3, que no se aplica. |
| Datos sintéticos que se confunden con datos reales | `es_sintetico = 1` y canal SIMULADOR. En Power BI, rótulo «DATOS SINTÉTICOS». |
| Peso estimado presentado como medido | Columnas, medidas DAX y tarjetas separadas, y unidad g o kg explícita. |
| Exposición de secretos | Ningún script contiene contraseñas: se piden con `ACCEPT … HIDE` o las escribes tú. |
| Ramas P3 divergentes | No se toca P3. El informe cita los SHA. |

---

## 9. Estructura de entrega propuesta

```
EcoAccess_BD_Amilkar/
├─ LEEME_PRIMERO.md
├─ 01_REQUISITOS_Y_ANALISIS/            01_DIAGNOSTICO.md, matriz V3↔P3, evidencias de solo lectura
├─ 07_INFORME_FINAL_WORD_PDF/         Informe_Final_EcoAccess_BD.docx + .pdf
├─ 02_MODELOS_MER_MEER_RELACIONAL/                MER, MEER (justificado o no), relacional; .dmd + carpeta; PNG/SVG/PDF por módulo
├─ 03_SCRIPTS_ORACLE/
│   ├─ 00_preflight_solo_lectura.sql … 13_consultas_exposicion.sql
│   ├─ 14_verificar_invalidos.sql, 99_PLAN_REVERSION.md
│   └─ practicas/ (HR, 32–34, secuencia, precio, triggers libres)
├─ 05_DICCIONARIO_DE_DATOS/            generado desde USER_TAB_COLUMNS (una fila por columna real)
├─ 06_PRUEBAS_Y_EVIDENCIAS/                matriz requisito↔objeto↔prueba, salidas SPOOL reales
├─ 12_GUIA_CSHARP_ORACLE/SOLUCION_CSHARP/                 conexión ODP.NET y CRUD empleado sin DELETE (sin claves)
├─ 13_POWER_BI/                .pbip/.pbix si se logra; si no, DAX/Power Query y pasos PENDIENTES
└─ 08_GUIA_DE_ESTUDIO_WORD_PDF/           Guía didáctica .docx/.pdf + banco de ejercicios numerados
```

**Cómo se generarán el Word y el PDF:** no hay Python, pandoc ni LibreOffice. Usaré .NET (OpenXML) o Word si está instalado. Lo compruebo en la etapa 6.

---

## 10. Plan de ejecución y bloqueos

| Etapa | Qué haré | Necesito de ti |
|---|---|---|
| 2. Diseño | Fijar el DDL de las 19 + 5 tablas, las reglas y los códigos de error | Confirmar la portada (ver abajo) |
| 3. Construcción | Crear los usuarios `ECOACCESS_BD` y `ECOACCESS_PRACTICAS` en FREEPDB1 local, ejecutar los scripts 01–11 y cargar 50 ciudadanos y 100 o más recorridos | **Autorización** para conectarme como administrador local (ver abajo) |
| 4. Validación | Ejecutar `12_pruebas.sql` con SPOOL, consultar objetos inválidos y reconciliar el diccionario | — |
| 5. Integración | C# leyendo vistas y Power BI con vistas de solo lectura | Que abras Power BI si hace falta escribir la credencial |
| 6. Documentos | Informe, guía, diagramas y banco de ejercicios (el total exacto, sin inflar) | — |
| 7. Git | Commits en la rama que me indiques | **Nombre de la rama de destino** |

**Pendientes con motivo:**
- Inventario real de Oracle Cloud: no tengo credencial; lo ejecutas tú con el script de solo lectura.
- ESP32 y 24/7: fuera de alcance de BD. Se documentan como no observados.
- Captura de pantalla real de SQL Developer: requiere que me des acceso a la ventana (computer-use) o que tú tomes las capturas siguiendo la guía.

**Datos de portada (confirmados por el equipo el 11/11/2026):**
- Jorge Alfonso Gutiérrez Thomas;
- Heyder Hedrey Canchila de Ángel;
- Saray Marcela Cadena Palomino;
- docente: Profesor Amílkar (con K). Su apellido no figura en la documentación del curso, por eso no se escribe.
