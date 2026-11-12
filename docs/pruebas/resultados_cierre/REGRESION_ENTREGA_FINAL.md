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


## Actualización del cierre del 11/11/2026, de 19:57 a 21:00 (commits 19f8aff a 1f8c30b)

| PRUEBA | ENTORNO | RESULTADO | COMMIT | EVIDENCIA |
|---|---|---|---|---|
| Compilación Debug y Release de la solución | local; para Release, bot detenido y relanzado por la tarea vigilante | 0 errores | 1fa52ac | consola; el bot pid 41300 sirve la plantilla nueva en `/registro` |
| Pruebas unitarias | sin base de datos | **1 864 superadas, 0 fallidas, 1 omitida** (enlace simbólico) | 1f8c30b | `dotnet test tests\unitarias` |
| Registro, carga y vinculación contra el Oracle aislado | `ECOACCESS_P3_PRUEBAS` | 26 superadas, 0 fallidas, 1 omitida | c843ec1 | contraseñas ficticias de 6 a 20 caracteres |
| Integración paralela completa, 3 pasadas | esquema aislado | 212/0/7, **211/1/7**, **211/1/7** | 1f8c30b | `diagnostico_408/pasada_23` a `pasada_25` |
| Concurrencia de 1, 10 y 20 personas | esquema aislado, en paralelo | superadas en las 3 pasadas | 1f8c30b | igual |
| Concurrencia de 30 personas | esquema aislado, en paralelo | **falla en 2 de 3 pasadas (408)** | 1f8c30b | sección siguiente |
| Ronda visual 7 | Ollama local, 15 fotos nuevas | 13/15; 0 falsas admisiones; 0 ofertas indebidas; 1 falso rechazo | 1f8c30b | `docs/vision/EVALUACION_RONDA7_INDEPENDIENTE.md` |
| Registro móvil (emulación a 375 px) | navegador integrado | una columna; 4 campos de 303 px; sin desplazamiento horizontal | 344afe1 | `docs/evidencias/registro_movil` |

### El 408 de 30 personas: qué se midió (no está resuelto)

| Hipótesis | Medición | Conclusión |
|---|---|---|
| Hilos de trabajo agotados | unos 32 760 libres en cada fallo | descartada |
| Cola del servidor | de 0 a 6 ms entre la aceptación y la lectura | descartada |
| `Expect: 100-continue` | el fallo sigue sin la cabecera | descartada como única causa |
| Nagle | el fallo sigue sin Nagle | descartada como única causa |
| Pausas del GC | 0 recolecciones gen2 en la ventana | descartada |
| Saturación de la concurrencia IOCP por trabajo de CPU | CPU del proceso entre 63 % y 75 % de 8 núcleos; **8 hilos de E/S ocupados = 8 núcleos**; el cliente terminó de enviar a los 39 ms y el servidor no recibió nada en 3 s | **hipótesis principal, no demostrada**: confirmarla requiere trazas ETW con permisos de administrador |

En esta prueba, cliente y servidor comparten el proceso: los 30 hash PBKDF2 de 600 000 iteraciones compiten con la
entrega de las lecturas de red. Con 30 teléfonos reales, el cliente no comparte la CPU del servidor, pero el servidor sí
calcula los 30 hash. **Estado: PENDIENTE.** No se subió el plazo de 3 s.

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
