# Regresión de la entrega final (11/11/2026)

**Equipo:** PC del propietario (Windows 11, 8 núcleos), .NET Framework 4.8, SDK .NET 10.0.403.
**Rama:** `desarrollo/telegram-doble-ia`.
**Código probado:** commits `e185682` (roles y primer administrador) y `751de35`, más la corrección de `ServidorRegistro` del commit siguiente.

Las pruebas omitidas **no** se cuentan como aprobadas.

| PRUEBA | ENTORNO | RESULTADO | DURACIÓN | COMMIT | EVIDENCIA |
|---|---|---|---|---|---|
| Compilación Debug de la solución | local | correcta, 0 errores | 16 s | e185682 + corrección | consola `dotnet build -c Debug` |
| Compilación Release de la solución | local, bot detenido | correcta, 0 errores | 13 s | e185682 + corrección | consola `dotnet build -c Release` |
| Pruebas unitarias | sin base de datos | **1 769 superadas, 0 fallidas, 1 omitida** | 8 s | e185682 + corrección | `dotnet test tests\unitarias` |
| Integración, paralela (configuración del repositorio) | esquema aislado `ECOACCESS_P3_PRUEBAS` | 211 superadas, **1 fallida**, 7 omitidas | 228 s | e185682 + corrección | `UsuariosNuevosSimultaneos… (30)`: 408 |
| Integración, en serie (`MSTest.Parallelize.Workers=1`) | esquema aislado | **212 superadas, 0 fallidas, 7 omitidas** | 221 s | igual | 1, 10, 20 y 30 personas superadas |
| Integración, paralela sin `BridgeOracleTests` | esquema aislado | **196 superadas, 0 fallidas, 5 omitidas** | 174 s | igual | 30 personas en 8 s |
| Carga `CargaUsuariosOracleTests` sola | esquema aislado | 13/13 superadas | — | antes de la corrección | — |
| Agotamiento del pool (prueba nueva `Post_ConElPoolOcupado…`) | unitaria | sin la corrección 0/3; con la corrección 3/3 | < 1 s | corrección | ver la sección siguiente |
| Diagnóstico de Telegram (`--comprobar-telegram`) | Telegram real | `@EcoAccessP3_JG_bot`, 0 pendientes, sin webhook | < 5 s | Release | consola |
| Diagnóstico de Oracle (`--comprobar-oracle`) | Oracle Cloud | 41 materiales habilitados, solo lectura | < 10 s | Release | consola |
| Estado de la visión (`--estado-vision`) | Ollama local | `lfm3-vl:latest` instalado y residente (Ollama 0.40.16); Groq sin clave | < 5 s | Release | consola |
| Vigilante: reinicio controlado | Windows | el bot se detuvo, la tarea lo relanzó desde `EcoAccess-P3\...\Release`; un solo consumidor | 20 s | Release | `vigilante.log`, 15:19:23 |
| Ronda visual 5 | Ollama local, 102 fotos públicas | 77/102 (75,5 %) | ~5 min | 44d8332 | `docs/vision/EVALUACION_RONDA5_INDEPENDIENTE.md` |
| Roles de la cuenta 4521 | Oracle Cloud | 2 y 3 (Operario y Administrador) | — | 1fd5b7d | `roles_equipo_oracle_cloud.txt` |

## Omitidas y por qué

| Prueba omitida | Requiere |
|---|---|
| `EnlaceSimbolicoSeRechazaAntesDeClasificar` (unitaria) | permiso de Windows para crear enlaces simbólicos |
| `RecorridoCompletoDeEco_FotoReal…`, `FotoVisionReal_…`, `CiudadanoNuevo_QrOficial_Foto_…`, `ParticionFijada24FotosPublicas_…`, `TresFotosPublicasNuevas_…` | ejecución explícita con el modelo real (`ECOACCESS_PROBAR_VISION_INTEGRADA=1`) y el corpus de fotos |
| `InformeInstitucional_CanecasHomonimas…`, `Usuario_UltimoAdministradorDesactivado_…` | condiciones de datos que la prueba no crea en esta base; se declaran inconclusas en lugar de inventarlas |

**Requieren hardware:** apertura física de las tres tapas (no automatizada; prohibido mover motores sin el equipo presente).
**Requieren credenciales:** Groq (clave del propietario), Power BI (contraseña de la cuenta de informes) y la foto real desde el teléfono del propietario.

## El fallo intermitente de 30 personas

**Síntoma.** `UsuariosNuevosSimultaneosQuedanAisladosYSobrevivenAlReinicio(30)`: la página de registro responde **408**
al formulario de alguna persona. Pasó en la regresión anterior (`REGRESION_CIERRE_2026-11-11.md`) y aquí volvió a pasar
en las dos ejecuciones paralelas completas.

**Lo que se averiguó:**

1. El 408 sale de `ServidorRegistro` cuando el cuerpo del formulario no se lee en 3 s.
2. **Agotamiento del pool de hilos (demostrado y corregido).** Si los registros ocupan hilos (PBKDF2 y ODP.NET son síncronos), el envío y la lectura del formulario quedan sin turno. La prueba unitaria nueva lo reproduce: sin `ServidorRegistro.AsegurarHilos` falla 3 de 3 veces; con él pasa 3 de 3. La corrección asegura tantos hilos mínimos como cupos del servidor (64).
3. **Con esa corrección el fallo de la serie paralela completa persiste**, así que no era la única causa.
4. La prueba pasa en serie (212/0), pasa en paralelo si se excluye `BridgeOracleTests` (196/0) y pasa sola (13/13). `CargaUsuariosOracleTests` y `BridgeOracleTests` ya están marcadas `[DoNotParallelize]`, y al terminar no quedan procesos del bot huérfanos.
5. **Hipótesis pendiente:** las pruebas del Bridge lanzan y matan procesos hijos que dejan sesiones y carga en el Oracle FREE local, limitado a 2 hilos de CPU. Justo después, 30 registros simultáneos encuentran la base lenta y los hilos del proceso de prueba (cliente y servidor comparten proceso) quedan bloqueados.

**Estado: PENDIENTE.** No se declara corregido. No se cambió la prueba ni el plazo de 3 s para hacerla pasar. El
comportamiento con 1, 10, 20 y 30 personas simultáneas sí quedó verificado en las ejecuciones en serie y sin el Bridge.
