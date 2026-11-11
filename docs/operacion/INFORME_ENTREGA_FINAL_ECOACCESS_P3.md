# Informe de entrega final — EcoAccess P3 (11/11/2026)

**Equipo:** Jorge Gutiérrez Thomas, Heyder Canchila y Saray Cadena.
**Repositorio:** `JorgeGutho21/EcoAccess-P3`, rama `desarrollo/telegram-doble-ia`, PR #11.

Estados que se usan en este informe:

- VERIFICADO REAL
- VERIFICADO EN SIMULACIÓN
- IMPLEMENTADO SIN VALIDACIÓN EXTERNA
- BLOQUEADO
- PENDIENTE

**No se declara «100 % de los criterios de aceptación cumplidos»**: varios criterios aplicables siguen pendientes o
bloqueados (ver la sección 27).

## 1–5. Cuentas, roles y autenticación

| Integrante | Código | Cuenta Oracle | Roles | Contraseña | Autenticación verificada |
|---|---|---|---|---|---|
| Jorge Gutiérrez Thomas | ADM-0001 / OP-0001 | 4521 | Administrador + Operario (VERIFICADO REAL) | la que eligió el propietario al crear la cuenta; no se sustituyó | la cuenta se creó con la aplicación; el agente no inició sesión con ella (no conoce la contraseña, ni debe conocerla) |
| Heyder Canchila | ADM-0002 / OP-0002 | no existe | — | propuesta, no configurada | PENDIENTE: falta su correo real |
| Saray Cadena | ADM-0003 / OP-0003 | no existe | — | propuesta, no configurada | PENDIENTE: falta su correo real |

- El rol Operario de la cuenta 4521 se asignó con `ServicioUsuarios.AsignarRolAsync`. La evidencia está en `docs/pruebas/resultados_cierre/roles_equipo_oracle_cloud.txt`.
- La regla «nadie verifica su propio depósito» y la denegación al ciudadano siguen cubiertas por `MatrizPermisosTests` y las pruebas de los servicios. Las pruebas unitarias dan 1 769 superadas y 0 fallidas.

## 6–8. Oracle, tarifas y datos experimentales

- **Oracle Cloud:** VERIFICADO REAL. `--comprobar-oracle` respondió con 41 materiales habilitados. Las migraciones V023, V024 y V025 están aplicadas con su huella SHA-256. No se reaplicó ninguna.
- **Siete tarifas:** VERIFICADO REAL. T001 se probó primero en el esquema aislado, terminando con `ROLLBACK`, y luego se ejecutó en la nube con el administrador 4521: lata 5, PET 4, cartón 3, metal 3, vidrio 3, multicapa 2 y papel 2.
- **Lote `SIM-2026-11-CIERRE`:** VERIFICADO REAL, como datos sintéticos.
  - 100 depósitos, 0 reales y 50 ciudadanos ficticios. No se crearon más.
  - 14,476 kg estimados y 0 kg medidos.
  - 0 ecopuntos, a propósito.

## 9–10. Canecas y QR

- **Tres canecas:** VERIFICADO REAL en datos (`VISTA_CANECAS_ESTACION`):
  - 1093 blanca, en servicio;
  - 1400 verde y 1401 negra, fuera de servicio hasta validarlas físicamente.

  La selección de caneca está VERIFICADA EN SIMULACIÓN. La apertura física está PENDIENTE.
- **QR oficial:** `EcoAccessCanecaPiloto1`, sin cambios.

## 11–13. Telegram, Ollama y visión

- **Telegram:** VERIFICADO REAL.
  - Identidad `@EcoAccessP3_JG_bot`, sin webhook y un solo consumidor.
  - El bot corre con la compilación Release del 11/11/2026 a las 15:19, desde `EcoAccess-P3`.
  - **La prueba con una foto real desde el teléfono está PENDIENTE**: hoy hay 0 updates y 0 consultas de visión en Oracle.
- **Ollama:** VERIFICADO REAL. `lfm3-vl:latest` está instalado y residente (Ollama 0.40.16). Groq está sin clave (BLOQUEADO).
- **Visión, ronda 5:** VERIFICADO EN SIMULACIÓN, con 102 fotos públicas.
  - 77 correctas (75,5 %), 2 falsas admisiones y 10 ofertas indebidas.
  - 0 ofertas a personas o animales.
  - No se ajustó nada con esta ronda y no hubo entrenamiento.
  - Detalle en `docs/vision/EVALUACION_RONDA5_INDEPENDIENTE.md`.

## 14. Power BI

- **BLOQUEADO.** El `.pbix` de P3 no existe: construirlo exige la contraseña de la cuenta de informes, que solo conoce el propietario.
- Los recursos están preparados (`reportes/powerbi`).
- El cruce SQL confirma que debe mostrar 0 depósitos operativos.
- No se tocó el informe EcoAccess_BD, que es de otra asignatura.
- Google Forms no está implementado.

## 15. Visual Studio

- **IMPLEMENTADO SIN VALIDACIÓN EXTERNA.** Visual Studio 2026 está abierto con `EcoAccess-P3\EcoAccess.P3.sln`.
- La compilación por consola está VERIFICADA (Debug y Release, 0 errores).
- **Observación:** en el IDE los 9 proyectos aparecen como «Descargado». Ya estaban así antes de esta sesión; no se recuperaron ni al reabrir ni al quitar el estado `.suo` (respaldado en `.vs\EcoAccess.P3\v18\.suo.respaldo-20261111`).
- El agente solo tiene permiso de clic simple en el IDE. El propietario debe, en el Explorador de soluciones, hacer clic derecho en la solución > **Volver a cargar todos los proyectos**, o revisar la ventana Salida.
- La solución ahora lista primero `EcoAccess.P3.Presentacion`, para que sea el proyecto de inicio por defecto.
- **Aplicación:** abierta en la pantalla de bienvenida e inicio de sesión, con la compilación nueva y sin contraseñas.

## 16–17. Vigilante y ESP32

- **Vigilante:** VERIFICADO REAL. La tarea apunta a `EcoAccess-P3`. Se detuvo el bot y la tarea lo relanzó (15:19:23); quedó un solo proceso. No es disponibilidad 24/7.
- **ESP32:** IMPLEMENTADO SIN VALIDACIÓN EXTERNA. La guía está en `docs/hardware/GUIA_INTEGRACION_ESP32_TRES_CANECAS.md` y en el documento 16. No se movió ningún motor.

## 18. Pruebas

Detalle en `docs/pruebas/resultados_cierre/REGRESION_ENTREGA_FINAL.md`.

| Bloque | Resultado |
|---|---|
| Compilación Debug y Release | 0 errores |
| Unitarias | 1 769 superadas, 0 fallidas, 1 omitida |
| Integración aislada, en serie | 212 superadas, 0 fallidas, 7 omitidas |
| Integración aislada, paralela | 211 superadas, **1 fallida** (30 personas, 408), 7 omitidas |
| Concurrencia de 1, 10, 20 y 30 personas | superada en serie y en paralelo sin el Bridge; **falla en la serie paralela completa** (PENDIENTE, no se declara corregido) |
| Corrección del agotamiento del pool | demostrada con una prueba nueva (sin la corrección 0/3, con la corrección 3/3) |
| Integración contra Oracle Cloud | no se repitió completa en esta sesión; la regresión de cierre anterior la ejecutó (`REGRESION_CIERRE_2026-11-11.md`) |

## 19–23. Documentos

- **Documento maestro:** `docs/entrega/academica/ECOACCESS_P3_DOCUMENTACION_TECNICA_COMPLETA.docx` y `.pdf`, 481 páginas y 44 capítulos.
- **Documentos especializados 01–15 y guía ESP32 (16):** en Word y PDF en `docs/entrega/academica`.
- **Instalación:** `docs/instalacion/INSTALACION_COMPANEROS.md`, incluida en el documento 14.
- **Inicio rápido:** `docs/operacion/GUIA_INICIO_RAPIDO_ECOACCESS.md`, incluida en Word y PDF en el documento 14.
- **TXT local de cuentas:** `%LOCALAPPDATA%\EcoAccessP3\privado\CUENTAS_EQUIPO_LOCAL.txt`.
  - Su ACL solo da acceso al usuario propietario.
  - Está fuera del repositorio y de OneDrive.
  - Las tres contraseñas figuran como PROPUESTA — NO CONFIRMADA.

## 24–25. Commits y PR

| Commit | Contenido |
|---|---|
| 702def2, d37175d, 382fce1 | rama de auditoría integrada por avance rápido |
| 44d8332 | ronda visual 5 |
| 1fd5b7d | T001 en la nube y roles de la cuenta 4521 |
| e185682 | primer administrador y acceso del operario al informe institucional |
| 751de35 | accesos directos y guía de inicio rápido |
| 775f5b8 | `ServidorRegistro.AsegurarHilos` y regresión final |
| ea38d3b | documento maestro y 16 documentos |

- No se usó `force push`.
- El PR #11 se actualizó con la descripción técnica de la entrega.
- **No se fusiona**: hay pendientes abiertos (prueba de 30 personas en paralelo, prueba con teléfono, hardware).

## 26. Disponibilidad sin PC

BLOQUEADO. Depende del PC del propietario. La VM gratuita de Oracle está bloqueada: no hay capacidad, no hay claves de
OCI y el bot es .NET Framework 4.8, solo para Windows. No se contrató nada.

## 27. Bloqueos y pendientes

| Pendiente | Quién | Cómo |
|---|---|---|
| Foto real desde el teléfono por Telegram | propietario | escanear el QR oficial y enviar una foto de una botella PET |
| Cuentas de Heyder y Saray | administrador 4521 | `FormUsuarios` con sus correos reales; contraseñas iniciales del TXT local |
| Recargar los proyectos en Visual Studio | propietario | clic derecho en la solución > Volver a cargar todos los proyectos |
| Informe `.pbix` de P3 | propietario | `reportes/powerbi/LEEME.md`, con la contraseña de la cuenta de informes |
| Apertura física de las tres tapas | equipo presente | guía ESP32 (documento 16) |
| Fallo de 30 personas en la serie paralela completa | desarrollo | investigar la carga residual del Oracle FREE tras `BridgeOracleTests` |
| Alojamiento 24/7 | propietario | capacidad gratuita compatible con Windows, o migrar el bot |

## Matriz de requisitos

| REQUISITO | IMPLEMENTADO | PROBADO | EVIDENCIA | COMMIT | ESTADO |
|---|---|---|---|---|---|
| Cuenta de Jorge con Administrador + Operario | sí | consulta en Oracle | roles_equipo_oracle_cloud.txt | 1fd5b7d | VERIFICADO REAL |
| Cuentas de Heyder y Saray | no (faltan sus correos) | — | — | — | PENDIENTE |
| TXT local de cuentas con ACL | sí | ACL revisada | icacls: solo el propietario | — (fuera de Git) | VERIFICADO REAL |
| Primer administrador seguro | sí | 8 pruebas unitarias y uso real | cuenta 4521 | e185682 | VERIFICADO REAL |
| Operario consulta el informe institucional | sí | MatrizPermisosTests | unitarias | e185682 | VERIFICADO REAL |
| Siete tarifas | sí | ensayo aislado y nube | t001_*.txt | 1fd5b7d | VERIFICADO REAL |
| Lote sintético separado de los datos reales | sí | consultas SQL | estadisticas_sql_nube.txt | d37175d | VERIFICADO REAL |
| Tres canecas y QR | sí | Oracle y simulación | VISTA_CANECAS_ESTACION, EstacionSimuladaTests | anteriores | VERIFICADO EN SIMULACIÓN |
| Apertura física | firmware sí | no | — | — | PENDIENTE |
| Bot en vivo y vigilante | sí | reinicio controlado | vigilante.log | 86f36dc | VERIFICADO REAL |
| Foto real por Telegram | sí | no | 0 updates hoy | — | PENDIENTE |
| Visión local | sí | ronda 5 | ronda5_independiente.json | 44d8332 | VERIFICADO EN SIMULACIÓN |
| Groq | sí | no | sin clave | — | BLOQUEADO |
| Dashboard institucional | sí | integración y tiempos | InformeInstitucionalOracleTests | e185682 | VERIFICADO REAL |
| Power BI P3 | recursos | cruce SQL | ESTADISTICAS_CIERRE_SQL.md | d37175d | BLOQUEADO |
| Concurrencia de 30 personas | sí | en serie sí; en paralelo completo no | REGRESION_ENTREGA_FINAL.md | 775f5b8 | PENDIENTE |
| Compilación y pruebas unitarias | sí | sí | regresión | 775f5b8 | VERIFICADO REAL |
| Documentación (maestro + 16) | sí | QA visual | docs/entrega/academica | ea38d3b | VERIFICADO REAL |
| Accesos directos | sí | ejecutados | Escritorio | 751de35 | VERIFICADO REAL |
| Visual Studio preparado | abierto | proyectos descargados en el IDE | observación del §15 | ea38d3b | PENDIENTE |
| Disponibilidad sin PC | no | — | ALOJAMIENTO_PERMANENTE.md | — | BLOQUEADO |
