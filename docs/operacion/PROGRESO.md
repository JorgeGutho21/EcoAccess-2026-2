# Progreso de la misión de cierre de software

Archivo para retomar el trabajo sin repetir pasos. Cada bloque indica pruebas, resultado y siguiente acción.

## Estado de partida (fase 0, 09/11/2026 20:30 COT)

- Rama `desarrollo/auditoria-tercer-corte` = cabeza del PR #11 (`desarrollo/telegram-doble-ia`), borrador; árbol limpio.
- Copia principal `EcoAccess-P3`: archivos sin versionar de otra persona (`scripts/docker/`, `scripts/pruebas/Ejecutar-Integracion-Temporal.ps1`), no se tocan.
- Bot activo: solo P3 (`Iniciar-EcoAccess-Nube.ps1`), registro público `https://gutiome.tailec56db.ts.net/registro/` → 400 sin token (correcto).
- Oracle Cloud responde. No se borran los 19 usuarios ficticios ni los 600 updates simulados anteriores.
- Prueba desde el iPhone: aún sin actividad en el registro del bot.

## Bloque 1 — 30 usuarios (terminado)

`CargaUsuariosOracleTests` amplía los escenarios a 1, 10, 20 y 30 personas simultáneas, 30 altas consecutivas y 10/20/30
cuentas existentes, y verifica en Oracle cada correo: 1 usuario, 1 rol, 1 credencial, 1 vínculo activo y ese vínculo
apunta a la cuenta del mismo correo.

- Comando: `dotnet test tests/integracion/EcoAccess.P3.Datos.Tests -c Debug --filter "FullyQualifiedName~CargaUsuariosOracleTests"` con `ECOACCESS_CARGA_SALIDA=docs/pruebas/resultados_carga_30`.
- Resultado 09/11/2026 20:36–20:41: **13/13 aprobadas**, 0 errores, conteo de 20 tablas idéntico antes y después.
- Métricas: `docs/pruebas/resultados_carga_30/*.json` (resumen en `docs/pruebas/CARGA_30_USUARIOS.md`).

## Siguiente acción

Bloque 2: visión (latas 2/10 en la evaluación TrashNet del motor de P3).

## Bloque 2 — visión (terminado)

TrashNet completo descargado y verificado por SHA-256; conjuntos de ajuste (45) y final (100) disjuntos. Corrección del
intérprete de latas y aluminio. Final: destino 84→87/100, metal 12→15/20, 0 aperturas con material equivocado. Se detectó
y corrigió que la evaluación reanudaba resultados de otra versión. Detalle: `docs/vision/EVALUACION_P3_TRASHNET.md`.

## Bloque 3 — dashboards, SeaChat, hardware (parcial)

- Dashboards de Windows Forms y «Mi actividad» ya existen; la captura con administrador ficticio se canceló (acceso a la
  ventana del arnés denegado). El administrador ficticio 3141 se creó y se borró.
- SeaChat: sin referencias ni credenciales; no se integró.
- Guía de ESP32 para principiantes: `docs/hardware/GUIA_ARDUINO_PRINCIPIANTES.md`.

## Bloque 4 — Power BI, dashboards y alojamiento (10/11/2026, terminado)

- Power BI: el propietario escribió la credencial de Oracle en el diálogo de Power BI y el informe se actualizó contra
  Oracle Cloud. Con 0 depósitos, Calendario quedaba en blanco y rompía todos los visuales; se corrigió en V2 (`37f0719`).
  Capturas en V2: `docs/evidencias/powerbi/`.
- Dashboards de WinForms capturados contra Oracle con el arnés. Los administradores ficticios 3142–3146 se crearon y se
  borraron. Se corrigieron los títulos cortados de las tablas del dashboard institucional (`0256ece`). Capturas:
  `docs/evidencias/ejecucion_real/06…08`.
- Alojamiento: `docs/operacion/ALOJAMIENTO_PERMANENTE.md`, más el vigilante y la tarea de arranque automático (`948bec6`,
  tarea **no instalada**). Prueba real: el vigilante relanzó el bot en 13 s.

## Bloque 5 — productos de Colombia y residuos comunes (10/11/2026, terminado)

- 543 candidatas de Wikimedia Commons con licencia libre, revisadas a mano. Quedaron 195 fotos, de ellas 177 inéditas.
  Detalle: `docs/vision/EVALUACION_COLOMBIA.md`.
- Corrección del intérprete (caja de huevos, bandeja de cartón, pilas, cáscaras, bolsa de papas):
  - objeto 134→149/195 y contexto 147→156;
  - 15 fotos mejoran y ninguna empeora;
  - TrashNet final contra la piloto 1093: 72/100, sin cambios y con 0 aperturas equivocadas.
- Error de método encontrado: la evaluación medía la admisión contra la caneca DEMO de `Telegram:QrDemo`. Corregido, y se
  enmendó `EVALUACION_P3_TRASHNET.md`. El mismo fallo afectaba el botón «caneca piloto» del bot real; se corrigió con
  prueba.
- Vigilante y lanzador: un proceso de evaluación se contaba como «otro bot» y el bot quedó caído unos 5 minutos. Ahora
  solo cuentan `--polling` y `--webhook`.

## Bloque 6 — arranque automático (10/11/2026, terminado)

Con la aprobación del propietario se instaló la tarea «EcoAccess P3 - vigilante del bot», que se ejecuta al iniciar
sesión y cada 5 minutos. Prueba: con el bot detenido, la tarea lo relanzó en 11 s.

## Bloque 7 — cierre definitivo (10/11/2026)

- Recorrido completo de un ciudadano nuevo contra Oracle Cloud y el modelo real. Pasó con los conteos iguales antes y después.
  - Registro, QR oficial leído del PNG, foto, destino, historial, estadísticas y reinicio.
  - Sin duplicados.
- Migración V023 (consultas de visión): escrita y probada con pruebas unitarias, pero **no aplicada**. La política del entorno bloqueó modificar Oracle compartido; queda para decisión del propietario.
- Visión
  - El tapabocas tiene clave propia (sanitario).
  - «Tissue paper» → pañuelo usado.
- Tercera ronda independiente: 131 fotos nuevas, etiquetadas antes de medir, 93/131 correctas. Los fallos quedan sin ajustar en `docs/vision/EVALUACION_RONDA3_INDEPENDIENTE.md`.
- Windows Forms
  - Arnés de capturas fuera de pantalla con formularios, servicios y Oracle reales.
  - Correcciones:
    - textos y botones cortados al 100 %;
    - fila de filtros de Depósitos;
    - tabla de cuentas en 1366×768;
    - textos de «caneca DEMO».
- Guía final de hardware: `docs/hardware/GUIA_FINAL_HARDWARE.md`. Ningún motor se movió.
- Informe con las 18 preguntas: `docs/operacion/INFORME_FINAL_CIERRE.md`.

## Siguiente acción

1. Aprobar o rechazar V023.
2. Prueba del propietario desde el iPhone.
3. Decidir «Eco» o «Echo» en el saludo.

---

# Misión maestra de cierre definitivo (10/11/2026)

## Fase 0 — auditoría y línea base (10/11/2026, terminada)

**Repositorio.** PR #11 `desarrollo/telegram-doble-ia` → `main`, abierto y sin conflictos, cabeza `552f2c2` (82 commits).
Autores reales en el historial: «Jorge Gutierrez» y «JorgeGutho21». No se reescribe historia (sin force push).
Sin versionar y ajenos, no se tocan: `scripts/docker/`, `scripts/pruebas/Ejecutar-Integracion-Temporal.ps1`.
El bot en vivo corre desde `EcoAccess-P3-auditoria\src\EcoAccess.P3.Bot\bin\Release` con la tarea «EcoAccess P3 -
vigilante del bot»; ahí no se compila Release mientras esté en uso.

**Línea base.** `dotnet build EcoAccess.P3.sln --no-incremental -c Debug`: 0 errores, 11 advertencias (analizadores
MSTest en pruebas). Pruebas unitarias: 1702 aprobadas, 1 omitida (`EnlaceSimbolicoSeRechazaAntesDeClasificar`, requiere
privilegio de enlaces simbólicos), 0 fallidas.

**Oracle Cloud (solo lectura, esquema `ECOACCESS_P3_APP`).**

- Migraciones registradas en `ECO_MIGRACION_APLICADA`: V001, C001, V002–V008, D001, V009–V020, C002, C003, V021, D002, V022, V023.
- V021 (tres canecas) y V022 (paquete PL/SQL de estadísticas) se escribieron en la línea V2 y estaban aplicadas pero
  **faltaban en este repositorio**. Se copiaron byte a byte; su SHA-256 coincide con el registrado (`91d88156…`, `59610c70…`).
- V023 en esta copia tenía fin de línea CRLF (por `core.autocrlf`) y su huella no coincidía (`e1f7…` frente a `cb84…`).
  El contenido era el mismo. Se marcaron V021–V023 como `-text` en `.gitattributes` y ahora las tres coinciden.
- 27 tablas. 21 usuarios, todos activos y con rol Ciudadano: **no hay cuentas Operario ni Administrador**. 0 reglas de
  ecopuntos, 0 depósitos, 0 consultas de visión.

**Las cuatro canecas (no se borra ninguna).**

| Id | Nombre | QR | Punto | Configuración | Origen |
|---|---|---|---|---|---|
| 1 | DEMO - Caneca ficticia | `DEMO_qr_ficticio_0001` | 1 (DEMO) | blanca, `esp32-demo-blanca` | D001 + D002 |
| 289 | DEMO - Caneca verde | — | 1 | verde | D002 (línea V2) |
| 290 | DEMO - Caneca negra | — | 1 | negra | D002 (línea V2) |
| 1093 | EcoAccess · Caneca piloto | `EcoAccessCanecaPiloto1` (**QR oficial**) | 1015 Estación piloto | **sin configurar** | instalación del piloto |

La «cuarta caneca» es la 1093: es la caneca oficial del piloto, con el QR compartido. No es un duplicado. Las
canecas 1, 289 y 290 son la estación DEMO de tres canecas. Se conservan todas. El modelo de tres destinos para el QR
oficial se agrega con una migración nueva (fase 2), sin modificar ni borrar filas.

**Integraciones.** No hay referencias a Google Forms en `src`, `docs` ni `scripts`. Existen vistas para Power BI (V019) y `AccesoPowerBi`.

### Matriz de riesgos

| Riesgo | Prob. | Impacto | Mitigación |
|---|---|---|---|
| Borrar o alterar datos históricos en Oracle compartido | Media | Alto | Solo migraciones nuevas y aditivas; prueba previa en esquema aislado; conteos antes y después |
| Huella de migración distinta por fin de línea | Alta | Medio | `-text` en `.gitattributes` para las migraciones aplicadas |
| Datos sintéticos confundidos con depósitos reales | Media | Alto | Marca `SIMULACION`, lote identificado, exclusión por defecto en consultas y paneles |
| Interrumpir el bot en vivo | Media | Medio | No compilar sobre `bin\Release` de la auditoría; pruebas en Debug |
| Cupo gratuito de Groq (~60 fotos/día) | Alta | Medio | Pausa entre fotos; respaldo LFM3-VL; política que no abre sin corroboración |
| Sin VM gratuita en OCI Bogotá («Out of host capacity») | Alta | Medio | El PC sigue como servidor; reintento documentado; sin gasto |
| Sin cuentas Operario/Administrador reales | Cierta | Medio | Pruebas de permisos con dobles; el propietario crea las cuentas y fija su contraseña |
| Apertura física en caneca equivocada | Baja | Alto | Solo adaptador ESP32 simulado en esta misión; no se carga firmware ni se mueven motores |
| Exposición de secretos | Baja | Alto | User Secrets fuera del repositorio; revisión de diff antes de cada commit |

### Fases ejecutables y bloqueos

- Se pueden ejecutar en local: 1 (roles), 2 (modelo de tres destinos), 3 (flujo Eco con adaptador simulado), 4–6
  (catálogo, pesos estimados, tarifas configurables), 9 (panel), 12 (pruebas) y 13–14 (documentación).
- Las fases 7–8 (migraciones nuevas y lote SIMULACION en Oracle Cloud) se prueban primero en el esquema aislado.
- Bloqueos externos:
  - fase 11, disponibilidad sin el PC: falta capacidad de OCI A1;
  - fase 10, Google Forms: no existe formulario ni integración y no se inventa;
  - cuentas reales de operario y administrador: las crea el propietario.

**Instaladores.**
- `Instalar-Esquema-Aislado-Local.ps1` toma ahora V021 y V022 del repositorio. Si también llegan por `-MigracionesExternas`, se usa una sola vez la copia local.
- V022 sigue exigiendo `-RegistroMigraciones`, que crea `ECO_MIGRACION_APLICADA`.
- `Preparar-BaseDatos.ps1` es la ruta antigua con Docker y no es obligatoria. Fallaría en V022 porque usa `registro_migracion`; queda documentado y no se usa en esta misión.

## Fase 1 — tres roles (10/11/2026, terminada en lógica)

- `MatrizPermisos` declara 12 operaciones protegidas y los roles que pueden ejecutarlas.
- `MatrizPermisosTests` (8 pruebas) recorre la matriz con los servicios reales para Ciudadano, Operario y
  Administrador, una cuenta desactivada y un actor inexistente.
- Prueba de mutación: con la matriz alterada a propósito (RegistrarReglaEcopuntos abierto al operario), la prueba
  falló y señaló la diferencia. Después se restauró.
- Suite unitaria: 1710 aprobadas, 1 omitida, 0 fallidas.
- Documento: `docs/seguridad/MATRIZ_ROLES.md`.
- Pendiente del propietario: crear en Oracle Cloud una cuenta Operario y una Administrador reales con su propia contraseña.

## Fase 2 — tres canecas con el QR oficial (10/11/2026, terminada)

- `ResolutorDestino` aplica la Resolución 2184: blanca, verde, negra, posconsumo y «no es residuo».
- `IRepositorioEstaciones` y `RepositorioEstacionesOracle` leen la estación.
- El bot indica el color y la ubicación de la caneca.
- V024 se aplicó en el esquema aislado: dos veces, revertida y reaplicada. Después se aplicó en **Oracle Cloud**. Solo cambiaron las tablas esperadas.
- La caneca 1093 conserva su QR y su estado. Las canecas DEMO quedan intactas.
- Pruebas: 18 unitarias, más 2 de integración en el esquema aislado y en la nube.
- Detalle en `docs/base_datos/V024_ESTACION_TRES_CANECAS.md`.
- **Pendiente:** el bot en vivo usa la compilación anterior. El resolutor entra al reiniciar con esta versión.

## Fase 3 — flujo Eco con ventana de 10 s y estación simulada (10/11/2026, terminada en simulación)

Ya existían:
- la ventana lógica de 10 s (`MensajesTelegram.VentanaDepositoSegundos`, igual que `ABIERTO_MS` del firmware);
- el evento `CierreSinSensor` → depósito con `sin_sensor = 1` (V016);
- la idempotencia por clave y por evento repetido.

Se agregó:
- `EstadoRegistroDeposito` con los códigos estables `DECLARADA_SIN_SENSOR`, `DETECTADA_POR_SENSOR` y `VERIFICADA`. Se deduce del depósito y no se guarda aparte.
- El escenario `SinSensor` en `CanecaSimulada`.
- `EstacionSimulada`: una tapa por contenedor y un ESP32 por tapa. Una orden solo llega a la tapa de su caneca.

`EstacionSimuladaTests` (7) cubre:
- cada orden abre solo su tapa;
- una orden para otro contenedor no se entrega;
- la tapa sin sensor deja `DECLARADA_SIN_SENSOR`, sin peso;
- la tapa con sensor deja `DETECTADA_POR_SENSOR`;
- repetir la orden no crea otro depósito;
- la ventana es de 10 s;
- no se aceptan dos tapas con el mismo ESP32.

Suite: 1735 aprobadas, 1 omitida. No se cargó firmware ni se movió ningún motor.

## Fases 5 y 6 — pesos estimados y ecopuntos (10/11/2026)

- `CatalogoPesosEstimados` (versionado) llena `peso_estimado_kg` cuando no hay báscula, tanto con sensor como sin él. Nunca llena `peso_medido_kg`.
- `MatrizEcopuntosPropuesta` contiene la matriz de la misión y los estados Provisional, Confirmado y Anulado.
- T001 aplica la matriz en Oracle con un administrador real.
- Se actualizó una prueba anterior: decía «sin peso» y ahora muestra el peso **estimado** del catálogo.
- Suite: 1753 aprobadas, 1 omitida.
- **Pendiente:** ejecutar T001 en la nube cuando exista un administrador real. Detalle en `docs/negocio/PESOS_Y_ECOPUNTOS.md`.

## Fases 7 y 8 — Oracle y lote de SIMULACIÓN (11/11/2026)

**Oracle (fase 7):**
- V025 añade `LOTE_SIMULACION`, `REGISTRO_SIMULACION`, `VISTA_DEPOSITOS_ESTADO` y `VISTA_LOTES_SIMULACION`.
- Se probó en el esquema aislado, ejecutada dos veces, y después se aplicó en la nube: solo aparecieron las tablas nuevas.
- Consultas de demostración: `database/04_consultas/11_consultas_cierre.sql`. Las 8 corren sin errores en la nube.

**Lote de simulación (fase 8):**
- `SIM-2026-11-CIERRE` se generó en la nube con 50 ciudadanos y 100 depósitos, 0 rechazos. La segunda ejecución no creó nada.
- Los informes reales siguen en 0 depósitos. El modo experimental ve 100.
- Detalle en `docs/base_datos/V025_LOTE_SIMULACION.md`.

## Fases 4, 9, 10 y 11 (11/11/2026)

- **Visión (fase 4):** la última ronda independiente da 119/141 (84 %) con 0 falsas admisiones. La meta del 90 % **no se alcanza**.
  - Una ronda nueva exige descargar un corpus nuevo con licencia, y no se hizo sin autorización.
  - Varios objetos: el resultado `VariosObjetos` pide una foto de uno solo.
- **Panel (fase 9):** capturas reales contra la nube en los dos alcances. El operativo da 0. El experimental da 96 depósitos más 4 del periodo anterior, y 50 ciudadanos.
- **Power BI y Google Forms (fase 10):**
  - Power BI excluye el lote por `es_ensayo`. Las páginas con el lote no se validaron en Power BI Desktop.
  - Google Forms no existe y queda PENDIENTE.
- **Disponibilidad (fase 11):** bloqueada por la capacidad de OCI A1. El bot sigue en el PC con el vigilante.
- Detalle en `docs/operacion/PANEL_INTEGRACIONES_DISPONIBILIDAD.md`.

## Fase 12 — pruebas (11/11/2026)

- Unitarias: 1760 aprobadas, 1 omitida, 0 fallidas.
- Integración en el esquema aislado: 211 aprobadas, 7 omitidas por diseño, 0 fallidas.
- Concurrencia con 1, 10, 20 y 30 personas: 0 errores.
- Nueva prueba de secretos en el repositorio.
- Detalle en `docs/pruebas/INFORME_PRUEBAS_CIERRE.md`.

## Fases 13 a 18 — documentación e informe final (11/11/2026)

- Documento maestro en Word y PDF generado con Word: 16 páginas, 7 tablas y 1 imagen. Revisión visual página por página (11/11/2026): la matriz de requisitos se salía del margen; se fijaron los anchos de columna, el encabezado se repite en cada página y ninguna fila se parte. La fuente HTML está en `docs/entrega/fuente/`.
- 15 documentos especializados, listados en `docs/entrega/INDICE_DOCUMENTAL_CIERRE.md`.
- 113 preguntas de sustentación en `docs/academico/PREGUNTAS_SUSTENTACION_CIERRE.md`.
- Informe final con la matriz de requisitos en `docs/operacion/INFORME_FINAL_MISION_MAESTRA.md`.
- Revisión visual del PDF hecha con la API PDF de Windows (páginas convertidas a imagen).

## Puesta en marcha de la versión de cierre (11/11/2026)

- Compilación Release de `EcoAccess-P3`: 0 errores.
- **Error corregido en los scripts:** `Iniciar-EcoAccess-Nube.ps1` y `Vigilar-Bot.ps1` comparaban la ruta del bot con la carpeta sin «\». Por eso `EcoAccess-P3` tomaba como propio el bot de `EcoAccess-P3-auditoria`: el lanzador se negaba a iniciar y el vigilante no habría levantado el bot. Ahora se compara con la carpeta más «\».
- **Bot en vivo:** se detuvo el de `EcoAccess-P3-auditoria` y quedó en polling el de `EcoAccess-P3`, con identidad comprobada y la página de registro en 8081.
- **Comprobaciones:** `--comprobar-oracle` encontró 41 materiales y `--auditar-recorrido 30` no halló duplicados. `EstacionTresCanecasOracleTests` contra la nube: 2 de 2 aprobadas.
- **Pendiente del propietario:** la tarea vigilante sigue apuntando a `EcoAccess-P3-auditoria`. No se cambió porque es configuración persistente del equipo. Hay que ejecutar `Instalar-Arranque-Automatico.ps1` desde `EcoAccess-P3`.
- **Sin verificar:** no se probó una conversación real por Telegram (QR, foto y respuesta de las tres canecas), porque requiere un teléfono.
