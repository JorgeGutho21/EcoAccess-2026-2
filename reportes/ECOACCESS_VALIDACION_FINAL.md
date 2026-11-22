# EcoAccess P3 — Validación de la versión final

**Fecha:** 22/11/2026 · **Mantenimiento y publicación:** Jorge Gutiérrez · **Institución:** Universidad Popular del Cesar
**Versión:** etiqueta `v2026.11.22` en `main` de [JorgeGutho21/EcoAccess-2026-2](https://github.com/JorgeGutho21/EcoAccess-2026-2).

Las evidencias en texto e imagen están en [`reportes/validacion_final/`](validacion_final/). Todo lo marcado **NO EJECUTADA**
no se ha comprobado y no cuenta como aprobado.

---

## A. Ubicación final

| Uso | Ruta | Qué es |
|---|---|---|
| **Fuente oficial** (Git, Visual Studio, compilación) | `%USERPROFILE%\Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\EcoAccess-P3` | Clon único de GitHub. Aquí se trabaja, se compila y se hace `git pull`/`push`. |
| **Entrega en OneDrive** | `%OneDrive%\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3-Final` | Exportación completa de la etiqueta `v2026.11.22`, con `VERSION.txt` y el commit exacto. Es una copia de entrega, **no** se edita. |
| Aplicación instalada | `%LOCALAPPDATA%\Programs\EcoAccess P3` | Compilación Release de esta versión; la abre el acceso directo «EcoAccess P3». |

**Por qué Git no vive en OneDrive:** la sincronización de OneDrive y los archivos de `.git` y `bin/obj` se bloquean entre sí, sobre todo
mientras se compila. Por eso se trabaja con una copia local y en OneDrive solo queda la entrega identificada.

## B. Origen de los cambios

**Copias encontradas (diagnóstico del 22/11/2026):**

| Copia | Git | Estado | Decisión |
|---|---|---|---|
| `Documents\…\EcoAccess-P3` | `EcoAccess-2026-2` | Rama `feature/catalogo-fotos-ecopuntos`, limpia, publicada | **Fuente oficial** |
| OneDrive `…\01_ProyectoDeAula_EcoAccess\EcoAccess-P3-LImpio` | no | Instantánea del **03/11/2026** (588 archivos fuente). Visual Studio la tenía abierta. | Obsoleta. Respaldada con inventario SHA-256. No se integra. |
| OneDrive `…\01_UNIVERSIDAD\…\ProyectoDeAula_EcoAccess\EcoAccess-P3-LImpio` | no | 03/11/2026 | Obsoleta; sin cambios |
| `source\repos\JorgeGutho21\EcoAccess-P3` | `EcoAccess-P3` (repositorio anterior) | 01/11/2026 | Histórico; sin cambios |
| `_Respaldos_EcoAccess\Respaldo_EcoAccess_2026-11-16` | — | Respaldos anteriores | Se conservan |

**Comparación por contenido, no por fecha:** cada archivo fuente de la copia de OneDrive se buscó en todo el historial de Git
(`git hash-object` frente a `git rev-list --all --objects`):

- 572 de 588 ya estaban en algún commit.
- Los 16 restantes son obsoletos o personales:
  - `scripts/docker/*`: Oracle en Docker, sustituido por Oracle Cloud;
  - tres lanzadores del bot sustituidos por `Iniciar-EcoAccess-Nube.ps1` y `Vigilar-Bot.ps1`;
  - registros `comparacion_groq.*`;
  - un `.csproj.user`.
- Quedaron respaldados, no se integraron.

**Ramas y pull requests:**

| Elemento | Estado final |
|---|---|
| PR #21 `feature/oracle-cloud-integrantes` (cuentas ECO_*, permisos mínimos) | **Fusionado** el 22/11/2026, CI correcto |
| PR #20 `feature/catalogo-fotos-ecopuntos` (catálogo por caneca, láminas recortadas, estación demo/física, RN-16) | **Fusionado** el 22/11/2026, CI correcto |
| PR de esta entrega `mejora/version-final-validada` | Búsqueda corregida, validación Oracle, metadatos, informe |
| PR #17 `feature/interfaces-observaciones-patino` (borrador) | **No fusionado.** Está en conflicto con `main` (`Program.cs`, `FormExplorarEcoAccess.cs`, `ServiciosAplicacion.cs`, `.csproj`), cambia la interfaz predeterminada por «Moderno» y su propia descripción lo declara incompleto. Se conserva intacto en su rama. |
| `recovery/ecoaccess-oracle-cloud`, `desarrollo/mision-nocturna-ia`, `desarrollo/auditoria-tercer-corte` | Commits del 08 al 14/11, anteriores a la unificación del 17/11 (PR #14 a #16), que rehízo esos temas. No se integraron; siguen en GitHub y en el respaldo. |

Antes de fusionar se comprobó que `main` + #20 + #21 compila y pasa todas las pruebas. El árbol fusionado en GitHub es
**idéntico** al probado (`HEAD^{tree}` igual).

**Respaldo verificable:** `Documents\01_UNIVERSIDAD\2026-2\_Respaldos_EcoAccess\Respaldo_EcoAccess_2026-11-22\`

- `EcoAccess-2026-2_todas_las_ramas.bundle`, comprobado con `git bundle verify`;
- `referencias.txt`;
- copia de las fuentes de OneDrive: 588 de 588 archivos con el mismo SHA-256;
- `inventario_OneDrive_LImpio.csv`.

## C. Estado de Visual Studio

- **Solución:** `EcoAccess.P3.sln`. Compila para .NET Framework 4.8 con C# 9 (`Directory.Build.props`), interfaz WinForms y Krypton.
- **SDK:** `global.json` pide .NET SDK 10.0.100 o superior; en este equipo está 10.0.403. Visual Studio 2026 (v18).
- **Datos:** ODP.NET administrado contra Oracle Autonomous Database con wallet mTLS.
- **Proyectos:** Entidades, Lógica, Datos, Presentación, Bot (Telegram y bridge de las canecas), Instalador y Compatibilidad,
  más pruebas unitarias, de integración y visuales.

| Comprobación | Resultado |
|---|---|
| Restauración NuGet | Correcta |
| Compilación Debug (`--no-incremental`) | **0 errores**, 72 advertencias |
| Compilación Release (`--no-incremental`) | **0 errores**, 72 advertencias |
| Advertencias | 69 de analizadores MSTest (estilo de aserciones en pruebas) y 3 de nulabilidad: `ServicioRegistroCiudadanos.cs:47`, `EstacionTresTapasTests.cs:341` y `CatalogoFotosTests.cs:211`. Ninguna impide ejecutar. |
| Recursos e imágenes | 157 imágenes incrustadas desde `assets/imagenes` con rutas relativas (`EcoAccess.Img.*`). Ninguna dañada, duplicada ni referenciada sin archivo (`validacion_final/auditoria_imagenes.txt`). |
| Rutas absolutas de un solo equipo | Se encontró y corrigió una: `BaseDeDatos_Amilkar/14_…/comparar_modelos_p3.ps1`. El código de la aplicación no tiene ninguna. |
| Ejecutable | Versión de archivo 2026.11.22.0. La versión de producto incluye el commit. Empresa: Universidad Popular del Cesar. Copyright: «Mantenimiento: Jorge Gutiérrez». |
| Ejecución real | La aplicación instalada abre desde el acceso directo («EcoAccess · Acceso al sistema»). |

## D. Estado de Oracle Cloud

Se probó con `tests/integracion/ValidacionFinal`, que usa los **mismos servicios que la aplicación** y la conexión del equipo
(User Secrets, cuenta técnica `ECOACCESS_P3_APP`, alias `ecoaccess_tp`, wallet). No imprime contraseñas ni la cadena de conexión.
Resultado: **55 de 55 correctas**, completo en `validacion_final/oracle_validacion_real.txt`.

| Área | Comprobado |
|---|---|
| Conexión | Oracle 19.33 en la nube con wallet, alrededor de 1,2 s |
| Lectura | Conteos de usuario, material (41), contenedor (9), depósito, orden de apertura, ecopuntos y reglas (7) |
| Integridad referencial | 53 claves foráneas, todas `ENABLED` y `VALIDATED`; 0 objetos inválidos |
| Roles `ECOACCESS_ROL_CONSULTA` y `ECOACCESS_ROL_EDICION` | Existen y tienen permisos. CONSULTA: SELECT en 39 objetos. EDICION: SELECT en 39, INSERT y UPDATE en 12, EXECUTE en 3. Ningún DELETE y sin acceso a tablas sensibles. |
| Escritura | Registro de ciudadano, creación de Operario y Supervisor de prueba, depósito, verificación y acreditación |
| Transacción | Un UPDATE con ROLLBACK no persiste |
| Persistencia | Se cierran todas las conexiones (`ClearAllPools`), se vuelven a crear los servicios y se inicia sesión: el depósito sigue ahí |
| Duplicados | Correo repetido rechazado (sin distinguir mayúsculas). La misma clave de apertura devuelve la misma orden. Una segunda verificación se rechaza. Repetir la acreditación no suma dos veces. |
| Errores de conexión | Con Oracle inalcanzable falla de forma controlada en unos 2 s, sin iniciar sesión |

**Datos de prueba:** las cuentas `validacion.final.*@ecoaccess.test` quedaron **desactivadas** al terminar. No se borran porque
sus depósitos forman parte del historial auditable, igual que los de las demás pruebas. No se tocó ningún dato real.

## E. Estado del catálogo

| Medida | Valor real |
|---|---|
| Residuos registrados en el catálogo de la aplicación | **54**: verde 22, blanca 19, negra 13 |
| Residuos con imagen (visibles en las tarjetas) | **49**: verde 21, blanca 18, negra 10 |
| Residuos sin imagen (solo texto en la búsqueda, sin tarjeta rota) | 5: semillas y tusas, botella PET de otras bebidas, toalla higiénica, tampón, pañal |
| Imágenes de residuos integradas | **127**: 112 fotos recortadas o individuales y 15 ilustraciones; más 12 «por clasificar» que no se muestran |
| Imágenes en `assets/imagenes` | 157 en total; también ECO y la pantalla de inicio |
| Imágenes originales de partida (OneDrive `05_Imagenes_EcoAccess`) | 162 archivos |
| Dañadas, duplicadas exactas o referencias rotas | 0, 0 y 0 |
| Materiales en Oracle | 41 |

La meta de unas 100 imágenes se cumple en número (127), pero **no** hay 100 materiales distintos: hay 54 residuos.

**Búsqueda (corregida en esta entrega):** antes no encontraba «Cartón contaminado con grasa», «Condón usado correctamente
envuelto» ni «Residuos orgánicos de alimentos». Ahora:

- ignora palabras de enlace;
- si no hay coincidencia exacta, propone el residuo que comparte al menos dos palabras y la mitad de la consulta;
- los residuos peligrosos (pilas, medicamentos…) siguen marcados como «fuera de catálogo»;
- funciona sin distinguir mayúsculas ni tildes.

Resultado de cada ejemplo pedido:

| Búsqueda | Residuo | Caneca |
|---|---|---|
| Cáscara de aguacate | Cáscara de aguacate | verde |
| Botella plástica | Botella plástica pequeña de agua | blanca |
| Cartón limpio | Cartón limpio y seco | blanca |
| Cartón contaminado con grasa | Papel o cartón con grasa o comida | negra |
| Colilla de cigarrillo | Colilla de cigarrillo apagada | negra |
| Condón usado correctamente envuelto | Condón usado, envuelto en papel | negra |
| Residuos orgánicos de alimentos | Restos de comida sin empaque | verde |

Las tres canecas siguen el código de colores de la Resolución 2184 de 2019: blanca para aprovechables limpios y secos, verde para
orgánicos aprovechables y negra para no aprovechables.

## F. Pruebas

| Categoría | Aprobadas | Fallidas | Omitidas o no ejecutadas | Evidencia |
|---|---|---|---|---|
| Unitarias (`EcoAccess.P3.Tests`) | **2309** | 0 | 1 omitida (enlace simbólico, requiere permisos) | `pruebas_automaticas.txt` |
| Datos sin base (`EcoAccess.P3.Datos.Tests`) | 59 | 0 | **192 NO EJECUTADAS**: exigen un esquema aislado `*_PRUEBAS` y se niegan a correr contra la base compartida | `pruebas_automaticas.txt` |
| Oracle real (`ValidacionFinal`) | **55** | 0 | 0 | `oracle_validacion_real.txt` |
| Visuales del catálogo (`CapturasExpo catalogo`) | **13** | 0 | 0 | `visual_catalogo.txt` |
| Visuales de pantallas (1366×768, 1600×900, 1920×1080) | 41 capturas | 0 controles fuera de su contenedor | — | `visual_pantallas.txt`, `captura_*.png` |
| Estación de canecas con 4 usuarios (21/11/2026) | 5 escenarios | 0 | — | `docs/hardware/GUIA_ESTACION_DEMO_Y_FISICA.md` |
| CI de GitHub (compilar y hardware) | ver el PR de esta entrega | — | — | GitHub Actions |

Flujo del ciudadano comprobado de punta a punta en Oracle real:

1. registro;
2. inicio de sesión;
3. búsqueda y caneca;
4. depósito por la caneca blanca demo (abre, registra y cierra en 14–15 s);
5. historial;
6. verificación por el Operario;
7. acreditación de ecopuntos (0 → 4);
8. persistencia.

El depósito de la demostración es una **simulación**: no mueve ninguna tapa y queda sin verificar hasta que lo revisa un Operario.

**NO EJECUTADAS:**
- Caneca física con el ESP32 (no había ningún puerto COM conectado).
- Flujos de Telegram.
- Pruebas desde los computadores de los compañeros.
- `comprobar-cuenta` y `prueba-colaborativa` con las cuentas personales ECO_*, que requieren sus contraseñas.
- `auditar-admin`, que requiere ADMIN.
- Escalado de Windows al 125 % y 150 %.
- Inicio en otro perfil de Windows.

## G. GitHub

- **Repositorio:** `JorgeGutho21/EcoAccess-2026-2`, rama oficial `main`.
- **Fusionados el 22/11/2026:** #21 (`15f425c`) y #20 (`d061355`), ambos con CI correcto.
- **Commits de esta entrega**, rama `mejora/version-final-validada`, autor «Jorge Gutiérrez»:
  - Corrige la búsqueda del catálogo con frases largas y palabras de enlace;
  - Agrega la validación real de EcoAccess contra Oracle Cloud;
  - Usa una ruta relativa en la comparación de modelos de Base de Datos;
  - Prepara la versión final de EcoAccess (metadatos, informe y evidencias).
- **Secretos:** se revisaron todos los archivos versionados. No hay wallet, `secrets.json`, contraseñas, tokens ni claves.
  Solo hay marcadores de ejemplo (`docs/equipo/secrets.ejemplo.json`).

## H. Estado del equipo: qué deben hacer Heyder y Saray

La guía completa es [`docs/equipo/GUIA_INSTALACION_TRES_INTEGRANTES.md`](../docs/equipo/GUIA_INSTALACION_TRES_INTEGRANTES.md).
En resumen:

1. **Requisitos:** Git, Visual Studio 2022 o 2026 con «Desarrollo de escritorio de .NET», .NET SDK 10
   (`winget install Microsoft.DotNet.SDK.10`) y SQL Developer.
2. **Obtener la versión:**

   ```powershell
   git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git
   ```

   Si ya lo tienen:

   ```powershell
   git checkout main
   git pull
   ```

   Comprueben que `git describe --tags` muestre `v2026.11.22` o una versión posterior.
3. **Configurar el equipo con un solo comando**, que hace los pasos 4 a 10:

   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1
   ```

4. Restaura NuGet y compila.
5. Pide el `Wallet_*.zip` y lo instala en `%USERPROFILE%\.oracle\wallets\EcoAccess`, fuera del repositorio.
6. Instala la aplicación y el acceso directo.
7. Configura SQL Developer con su cuenta personal (`ECO_HEYDER` o `ECO_SARAY`).
8. Ejecuta `comprobar-cuenta` y `prueba-colaborativa`, la escritura compartida en la misma base.
9. La conexión de la aplicación (cuenta técnica) la escribe Jorge en ese equipo; queda en User Secrets del perfil de Windows.
10. Ejecuta las pruebas unitarias.
11. **Diagnóstico de lectura**, que no crea nada:

    ```powershell
    dotnet run --project tests\integracion\ValidacionFinal -c Release -- --solo-lectura
    ```

12. **Errores frecuentes:**
    - ORA-12154 o alias no encontrado: el wallet no está en la carpeta indicada.
    - ORA-01017: usuario o contraseña incorrectos.
    - Error al compilar porque el bot está abierto: cerrar EcoAccess y el bot.
    - El acceso directo no abre: volver a ejecutar `scripts\instalacion\Instalar-EcoAccess.ps1`.

Todos apuntan a la **misma** base de Oracle Cloud; no se crea ninguna base por computador.

## I. Pendientes

| Pendiente | Impacto | Cómo completarlo |
|---|---|---|
| Probar la caneca física (ESP32) | La apertura física no está demostrada; la demo sí | Conectar el ESP32 y ejecutar `scripts\estacion\EcoAccess-Estacion.ps1 -Accion Iniciar -Modo Fisica -PuertoSerial COM3`, luego un depósito |
| Pruebas en los equipos de Heyder y Saray | No se sabe aún si su conexión funciona | Sección H, pasos 2, 3 y 11 |
| Telegram | No verificado en esta sesión | Enviar `/start` al bot y hacer un depósito como invitado |
| PR #17 (interfaces «Moderno» y estación táctil) | Trabajo del 18/11 sin integrar | Resolver los 4 conflictos con `main`, terminar los paneles de Operario y Supervisor y decidir si «Moderno» será la predeterminada |
| Ecopuntos en producción | 59 depósitos verificados y **0 movimientos**: nadie ha usado «Acreditar depósito» | Que Operario o Supervisor acrediten desde «Ecopuntos». Cargar la matriz de tarifas T002 (requiere ADMIN) |
| Esquema aislado de pruebas en la nube | 192 pruebas de integración no ejecutadas | Crear `ECOACCESS_P3_PRUEBAS` en Oracle Cloud (requiere ADMIN) y ejecutar `EcoAccess.P3.Datos.Tests` |
| Copia vieja `EcoAccess-P3-LImpio` en OneDrive | Visual Studio la tenía abierta: se podía estar editando una versión del 03/11 | Cerrar Visual Studio, abrir `EcoAccess.P3.sln` de la fuente oficial y mover la carpeta vieja al respaldo |
| Límite de contraseña | El registro público acepta hasta 20 caracteres; la contraseña inicial que pone el Supervisor, más | Unificar el límite |
| Advertencia CS8604 | Ninguno en ejecución (la contraseña ya se valida antes) | Anotar la nulabilidad en `ServicioRegistroCiudadanos` |

## J. Versión final

- **Etiqueta:** `v2026.11.22` en `main` (commit con `git rev-list -n 1 v2026.11.22`). La versión de producto del `.exe`
  incluye el mismo commit.
- **Base validada:** `d061355` (`main` con #20 y #21), con los cambios de esta entrega encima.
- **Ejecutable:** `%LOCALAPPDATA%\Programs\EcoAccess P3\EcoAccess.P3.Presentacion.exe` (Release), abierto por el acceso
  directo «EcoAccess P3» del escritorio. Si no hay ninguna estación activa, el acceso directo inicia las canecas demo.
- **Estado honesto:** compila, pasa sus pruebas, lee y escribe en Oracle Cloud y respeta los permisos de cada perfil. **No**
  se puede declarar completamente validada mientras sigan pendientes el ESP32, Telegram y los equipos de los compañeros.
