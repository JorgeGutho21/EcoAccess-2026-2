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

## Siguiente acción

Prueba del propietario desde el iPhone; luego credencial de Power BI y capturas de dashboards. Informe:
`docs/operacion/INFORME_CIERRE_SOFTWARE.md`.
