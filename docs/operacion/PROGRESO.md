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

## Misión final autónoma de entrega (11/11/2026, 14:45–)

| Hora (COT) | Fase | Qué se hizo | Resultado |
|---|---|---|---|
| 14:45 | Git | integración de `desarrollo/auditoria-tercer-corte` (382fce1) en la rama del PR por avance rápido | sin conflictos; 3 commits (guía ESP32, estadísticas SQL, prueba de visión) |
| 14:48 | Cuentas | consulta de cuentas de personal en Oracle Cloud | 4521 «jorge gutierrez» (Administrador, con contraseña); 4468 operario ficticio del lote; Heyder y Saray **no existen** |
| 14:50 | Roles | rol Operario a 4521 con `ServicioUsuarios.AsignarRolAsync` (actor 4521) | 4521 = roles 2 y 3, verificado; contraseña no cambiada |
| 14:51 | TXT | `%LOCALAPPDATA%\EcoAccessP3\privado\CUENTAS_EQUIPO_LOCAL.txt` con ACL solo del propietario | las tres contraseñas quedan como PROPUESTA — NO CONFIRMADA |
| 14:54 | Pruebas | Debug + unitarias | 1768/0/1 |
| 15:00 | Visión | resultados de la ronda 5 | 77/102 (75,5 %); commit 44d8332 |
| 15:00 | Pruebas | integración aislada | 211/1/7; falló otra vez la carga de 30 personas (408) |
| 15:20 | Corrección | `ServidorRegistro.AsegurarHilos`; prueba nueva que reproduce el agotamiento del pool | sin corrección 3/3 fallan, con corrección 3/3 pasan |
| 15:00–15:30 | Accesos | «Iniciar EcoAccess P3» y «Abrir EcoAccess P3 en Visual Studio» en el Escritorio | probados; Visual Studio abierto con `EcoAccess-P3\EcoAccess.P3.sln` |
| 15:30 | Documentación | fuentes 08, 12–16 y 00 (documento maestro), guía de inicio rápido | pendiente de generar DOCX/PDF |

Bloqueos: la foto real desde el teléfono, la apertura física, el `.pbix` de P3 (contraseña de la cuenta de informes), las
cuentas de Heyder y Saray (faltan sus correos) y el alojamiento 24/7.

## Misión maestra definitiva (11/11/2026, 19:50–21:10)

| Hora (COT) | Fase | Qué se hizo | Resultado |
|---|---|---|---|
| 19:57 | Git | respaldo en `_respaldos/respaldo_integracion_20261111_1957`: parches, bundle y archivos sin seguimiento. Los 4 commits locales de `desarrollo/auditoria-tercer-corte` pasan a la rama del PR sin `scripts/docker` ni `Ejecutar-Integracion-Temporal.ps1`, que son archivos de otra persona | push `a1af3e6..1066d91`; PR #11 abierto, CI verde |
| 20:05 | Visión | causa de `persona-03`: nadie preguntaba si el sujeto era una persona. Se añade una tercera pregunta independiente antes de ofrecer depositar | `19f8aff`; 1 809 unitarias superadas |
| 20:20 | Registro móvil | una columna, campos del mismo ancho, errores junto a su campo, «Mostrar» y script autorizado por hash en la CSP; política ciudadana de 6 a 20 caracteres | `344afe1`; 1 836 superadas; capturas a 375 px |
| 20:35 | Comida | pescado cocinado → comida (no animal); huevos; carne cruda; pregunta «¿Deseas desechar estos alimentos?» | `1fa52ac`; 1 864 superadas |
| 20:40 | Despliegue | compilación Release con solo el código confirmado; la tarea vigilante relanzó el bot (pid 41300) | `/registro` sirve la plantilla nueva |
| 20:45 | Pruebas | contraseñas ficticias de integración ajustadas a la política nueva | `c843ec1`; 26 superadas |
| 21:00 | 408 | 3 pasadas paralelas con diagnóstico de CPU y GC: 212/0, 211/1, 211/1 | `1f8c30b`; **PENDIENTE** (hipótesis IOCP) |
| 20:50 | Visión | ronda 7 independiente, 15 fotos nuevas | 13/15; 0 falsas admisiones; 0 ofertas indebidas; 1 falso rechazo |

Sigue pendiente:
- el 408 con 30 personas en la serie paralela;
- el flujo de 10 s en el bot en vivo (modo software, sin dispositivo);
- la prueba real en el iPhone;
- las cuentas de Heyder y Saray;
- la reparación de Visual Studio;
- el alojamiento 24/7;
- la apertura física.

## Misión nocturna y misión final (12/11/2026, 01:40–03:50 COT)

- Sin movimientos físicos. Informe completo: `docs/operacion/INFORME_MISION_NOCTURNA_Y_FINAL_12-11-2026.md`.
- Hecho:
  - guardia de hardware;
  - descripción concreta;
  - catálogo de 307 tipos con matriz;
  - varios residuos (en Telegram solo orienta);
  - coordinador y seis representaciones (solo simulador);
  - Telegram con bienvenida, registro en 3 pasos y menú de 8 opciones;
  - puntos de Valledupar (V026 solo en el esquema aislado);
  - 408 corregido (3/3 pasadas);
  - banco de 281 fotos, con ruta correcta del 73,8 % al 79,0 % y falsas admisiones de 11 a 5.
- Siguiente acción: la prueba supervisada de `GUIA_PRUEBAS_SUPERVISADAS_13-11-2026.md` y las 5 falsas admisiones.

## Incidente de la bolsa de agua y medición de las tres canecas (12/11/2026, 13:20–14:45 COT)

- Caso real: órdenes 1685 y 1686, una bolsa de agua leída como empaque multicapa abrió la NEGRA. Informe completo con causas,
  cambios y medición: `docs/vision/INCIDENTE_BOLSA_AGUA_CANECA_NEGRA.md`.
- Commits:
  - `c0f8f0c`: verde y negra exigen segunda lectura que corrobore; «packet» ya no es metalizado; las correcciones son incidencias;
  - `460e01f`: la evaluación mide las tres canecas.
- Aperturas automáticas en 350 fotos independientes: antes 85 con 1 equivocada (98,8 %); después 61 con 1 equivocada (98,4 %;
  IC 95 % de 91,3 % a 99,7 %). En entrenamiento: 4 aperturas equivocadas de verde o negra → 0.
- Suite unitaria: 2 096 correctas, 0 con error, 1 omitida.
- Estación reiniciada a las 13:45 con `460e01f`: las 3 canecas escuchan y el bot está en polling.
- Siguiente acción, con el dueño presente:
  - repetir la bolsa de agua (no debe abrir nada);
  - pruebas físicas de `GUIA_PRUEBAS_SUPERVISADAS_13-11-2026.md`.
- Pendiente: ampliar hacia 1 500 imágenes (van 546).

## Misión maestra de perfeccionamiento (14/11/2026) — parcial

Línea base al empezar: rama `desarrollo/telegram-doble-ia` en `53fa268` (PR #11 abierto; el repositorio ahora es
`JorgeGutho21/EcoAccess-2026-2`), compilación sin errores, unitarias 2 096/0/1. Sin bridges activos; el ESP32 conserva el
firmware de calibración desde el 12/11 y la estación sigue detenida. **No se tocó el hardware.**

| Fase | Qué se hizo | Estado | Evidencia |
|---|---|---|---|
| P0 visión | Reglas: bolsa de agua, cubeta con huevos, envase con contenido, limpiadores, mecato, cuchillas (`d656c16`) | Implementado y probado (unitarias) | `CasosCriticosMisionMaestraTests` |
| P0/P1 visión | Pregunta al modelo «¿el envase tiene algo dentro?» (`1e0f93d`) | Medido con el modelo real: independientes 16 → 12 aperturas de la blanca, las 4 retiradas con contenido | `docs/vision/PREGUNTA_CONTENIDO_ENVASE.md` |
| P3 Windows Forms | Fallo de pestañas y ventanas: tres causas raíz (`3820a16`) | Corregido y medido con el arnés: 44 → 38 diferencias, 1 → 0 solapamientos | `docs/evidencias/winforms_ventanas/LEEME.md` |
| Valledupar | Nueva lectura de CORPOCESAR (`6862f37`) | Semilla sin cambios; nada «Confirmado» | `docs/operacion/RESIDUOS_ESPECIALES_VALLEDUPAR.md` |

Unitarias al cierre: 2 146/0/1.

Pendiente, por criticidad:
1. Bot en vivo con el código anterior: falta compilar Release y reiniciarlo para que use las reglas nuevas.
2. `carton-05` (cubetas de huevos cerradas) todavía abriría la blanca.
3. Banco ampliado de 1 000 imágenes y metas de ruta ≥ 95 % y aperturas ≥ 99 %: sin empezar.
4. Telegram, Oracle y carga de 30 usuarios: sin revisar en esta misión.
5. Rediseño visual de Windows Forms, escalado 125 % y 150 %, dashboard y Power BI: sin empezar.
6. Documento maestro Word, manual de sustentación e informe final: sin actualizar.
7. Hardware: calibración a 90° sin la observación del propietario; firmware de estación sin recargar.
