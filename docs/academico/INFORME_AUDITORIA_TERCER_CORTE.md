# Informe final de auditoría — tercer corte

Proyecto EcoAccess P3 · rama `desarrollo/auditoria-tercer-corte` · 09/11/2026 · auditoría ejecutada por Jorge Gutierrez
(identidad Git del repositorio). Sin porcentajes ni calificaciones inventadas.

## A. Diagnóstico

Estado inicial (base `desarrollo/telegram-doble-ia`, 8c7ed50): solución .NET Framework 4.8 con seis proyectos, capas
correctamente separadas y pruebas de arquitectura existentes. Problemas encontrados:

1. **No existía App.config**: la cadena de conexión solo venía de User Secrets, mientras el curso exige
   App.config + `ConfigurationManager` (PDF p. 13–15; sitio §06).
2. **Ventana del simulador en 0 s por defecto**: sin `Bridge:VentanaSimuladaSegundos`, el simulador cerraba al instante,
   en contra de la fuente única de 10 s (`MensajesTelegram.VentanaDepositoSegundos`).
3. **Prueba de diagnóstico ligada al contenedor local**: `ConsultaIdentificaEsquemaRealYTablasSinEscribir` deducía el
   PDB del `Data Source` y fallaba con el alias TNS de Autonomous Database.
4. **Fuga de datos de prueba**: si una persona del escenario de carga fallaba, `Task.WhenAll` impedía registrar las
   demás cuentas para la limpieza (quedaron 19 usuarios ficticios `@example.invalid`), y los `update_id` simulados nunca
   se borraban (600 filas acumuladas en `update_telegram` por ejecuciones de hoy).
5. El texto «20 s» ya no estaba en README ni firmware; solo aparece en documentos históricos que describen el cambio
   20 s → 10 s y en una pausa de la API visual (no relacionada con la caneca).

## B. Cumplimiento académico

Fuentes: sitio del docente capadatos.vercel.app (leído completo), PDF entregado (solo 4 de 16 páginas con contenido),
repositorio del docente `john-patino/SistemaProductos` (rama `master`). No hay rúbrica con ponderaciones disponible.
Detalle por criterio en `MATRIZ_CUMPLIMIENTO_TERCER_CORTE.md`: 18 criterios obligatorios verificables cumplen (4 con
adaptación justificada) y 1 no aplica (WPF, opcional).

## C. Arquitectura

Presentación (WinForms) y Bot son raíces de composición; Lógica define contratos (`IRepositorio*`, `IClasificadorMaterial`,
`ICanalDispositivo`) y reglas; Datos implementa los contratos con ODP.NET; Entidades no depende de nadie. Diferencia con
la guía: Datos referencia a Lógica (inversión de dependencias) en vez de Lógica → Datos; así la Lógica no conoce Oracle
y se prueba con dobles. Verificado por `PruebasArquitectura` (referencias declaradas y compiladas). Diagramas 2 y 3 de
`DIAGRAMAS_UML.md`.

## D. Patrones

- **DAO/Repository**: interfaz Repository en Lógica + implementación Oracle que cumple el papel de DAO (justificación en
  la matriz §3). No se crearon clases `*DAO` de paso porque no añadirían comportamiento.
- **SOLID**: S `ComandosOracle`, `TraductorErroresOracle`, `CadenaConexionOracle`; O/L proveedores `IClasificadorMaterial`
  intercambiables; I interfaces por agregado; D contratos en Lógica.
- **GRASP**: Experto (`Material.Habilitar()`), Creador (`ServicioInteracciones` crea interacción y orden), Controlador
  (`Servicio*`), Indirección (`ICanalDispositivo`), Bajo acoplamiento/Alta cohesión (capas y repositorios por agregado).

## E. ADO.NET

`FabricaConexionesOracle` abre y libera conexiones (`using`); `ComandosOracle.Crear` fija `BindByName = true`; todos los
valores van como `OracleParameter` tipado; lectura con `DbDataReader`; `RETURNING … INTO :id` para claves generadas;
`OracleTransaction` en operaciones de varias filas; pool de ODP.NET por defecto. Lo vigilan
`PruebasTercerCorte.Datos_CreaComandosSoloConBindByNameYSinConcatenarValores` y la batería de integración.

## F. Oracle

24 tablas con claves foráneas (diagrama 7), migraciones versionadas V002–V020, vistas `EA_R_*` para informes. La
auditoría **no** cambió esquema ni migraciones. CRUD, unicidad, concurrencia optimista e idempotencia se comprobaron
contra Oracle Autonomous Database real (§J).

## G. Interfaces

WinForms: los formularios reciben servicios por constructor y no contienen SQL (prueba automática). La app ahora lee
App.config y pide la contraseña solo si no existe la cadena privada. Telegram: el bot compone los mismos servicios; sus
adaptadores no usan Datos (prueba automática).

## H. Integración física

Ventana de 10 s con fuente única (`MensajesTelegram.VentanaDepositoSegundos`) y firmware `ABIERTO_MS = 10000`
comprobados por prueba; el simulador ya usa 10 s por defecto. El aviso de 10 s solo se envía tras el evento de apertura
registrado en Oracle y el cierre lo hace el firmware. **No verificado con ESP32 físico** en esta auditoría: las pruebas de
bridge usan el simulador HTTP y el ejecutable real del bot contra Oracle.

## I. Documentación

Creados en `docs/academico/`: `MATRIZ_CUMPLIMIENTO_TERCER_CORTE.md`, `DIAGRAMAS_UML.md` (10 diagramas Mermaid) y
`diagramas/*.svg` (exportados), `PASO_A_PASO_TERCER_CORTE.md`, `GUIA_SUSTENTACION.md` (25 preguntas) y este informe.

## J. Pruebas

Entorno: Windows 11 Pro, .NET SDK 10.0.100 compilando net48, Oracle Autonomous Database (wallet mTLS, alias
`ecoaccess_tp`), 09/11/2026.

| Batería | Comando | Resultado |
|---|---|---|
| Compilación | `dotnet build EcoAccess.P3.sln -c Debug` | 0 errores |
| Unitarias | `dotnet test tests/unitarias/EcoAccess.P3.Tests` | 1 627 aprobadas, 0 fallidas, 1 omitida |
| Arquitectura y tercer corte | filtro `Arquitectura` | 12/12 (4 existentes + 8 nuevas) |
| App.config en ejecución | `ConfigurationManager.OpenExeConfiguration` sobre el .exe (PowerShell 5.1, .NET Framework 4.8) | lee `OracleP3`, proveedor ODP.NET, sin contraseña |
| Integración Oracle, 1.ª ejecución | `dotnet test tests/integracion/EcoAccess.P3.Datos.Tests` (12,5 min) | 2 fallos: diagnóstico de PDB (defecto de la prueba, corregido) y carga de 20 personas (intermitente bajo 8 hilos; pasó 2 veces aislada); dejó 19 usuarios ficticios y 373 updates simulados |
| Carga concurrente tras la corrección | filtro `CargaUsuariosOracleTests` | 8/8: 10 y 20 personas nuevas, 10 y 20 cuentas existentes simultáneas; sin residuos nuevos |
| Integración Oracle, 2.ª ejecución | misma orden, tras las correcciones | **208 pruebas: 203 aprobadas, 0 fallidas, 5 omitidas** (11 min, 15:43–15:54); conteo de 20 tablas antes y después idéntico: sin residuos |

Las pruebas que dependen de visión real o de fotos locales quedan **omitidas** (no se cuentan como aprobadas).

## K. Git

Rama `desarrollo/auditoria-tercer-corte` creada desde `desarrollo/telegram-doble-ia` en un worktree aparte para no tocar
los archivos sin versionar de la copia de trabajo principal. Commits en español con identidad Jorge Gutierrez
(`263990811+JorgeGutho21@users.noreply.github.com`). El PR único se abre contra `main` cuando `gh` vuelva a tener sesión
(`gh auth login`); la CLI perdió la autenticación durante la sesión. Como la rama base aún no está en `main`, el PR
incluirá también sus commits previos hasta que se fusione `desarrollo/telegram-doble-ia`.

## L. Riesgos

- Residuos de pruebas en Oracle: 19 usuarios ficticios (`Ciudadano ficticio de carga`, `@example.invalid`) con sus
  roles y credenciales, y 600 filas simuladas de `update_telegram` (id por *ticks*, distintos de los 26 updates reales).
  La causa está corregida; **borrarlos requiere autorización** del propietario.
- La prueba de 20 personas simultáneas puede fallar bajo la ejecución paralela completa contra la nube (latencia).
- ESP32 físico y WPF fuera de esta verificación.
- El docente podría exigir literalmente `*DAO` o la cadena completa en App.config: respuesta preparada en la guía.

## M. Preparación para sustentación

`GUIA_SUSTENTACION.md`: 25 respuestas con archivo, clase y método.

## N. Conclusión

| Aspecto | Estado |
|---|---|
| Tres capas y dependencias | VERIFICADO |
| ADO.NET parametrizado, transacciones y manejo de errores Oracle | VERIFICADO |
| DAO/Repository | VERIFICADO (adaptación justificada) |
| App.config + ConfigurationManager | VERIFICADO (sin contraseña, por seguridad) |
| CRUD y concurrencia contra Oracle real | VERIFICADO (203/208, 0 fallos en la 2.ª ejecución; un fallo intermitente previo documentado) |
| Ventana de 10 s, fuente única | VERIFICADO en software y simulador; NO VERIFICADO con ESP32 físico |
| Diagramas UML del código real | VERIFICADO (10 renderizados sin errores) |
| WPF | NO APLICA (opcional) |
| PR único | BLOQUEADO hasta `gh auth login` |
| Limpieza de residuos de prueba en Oracle | BLOQUEADO hasta autorización |
