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

## Siguiente acción

Prueba del propietario desde el iPhone. Fotos reales tuyas para la siguiente evaluación; agregar la familia «tapabocas»
al catálogo.
