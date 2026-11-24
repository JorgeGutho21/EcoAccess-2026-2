# INFORME FINAL DE INSTALACIÓN MULTIEQUIPO, TELEGRAM MÓVIL Y PREPARACIÓN EXPOSOFTWARE — ECOACCESS P3

- **Responsable:** Jorge Gutiérrez (`Jorge Gutierrez <263990811+JorgeGutho21@users.noreply.github.com>`)
- **Fecha de verificación:** 2026-11-24
- **Versión distribuible:** `3.1.0-exposoftware`
- **Repositorio remoto:** `https://github.com/JorgeGutho21/EcoAccess-2026-2`
- **Rama activa:** `feat/ecoaccess-vision-integracion-final` (commits `ae1ef8a` → `2ac1d3e` → `56361d0` → `d8864bb` + commit de cierre de este informe)
- **Estado del Pull Request:** Draft PR #23 (`https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/23`) — actualizado sin fusión automática a `main`
- **Ruta oficial única:** `C:\Users\Usuario\OneDrive - unicesar.edu.co\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3`
- **Solución principal:** `EcoAccess.P3.sln` (`.NET Framework 4.8`, `C# 9.0`, 9 proyectos)

---

## 1. Unificación del Proyecto y Limpieza de Instancias Obsoletas (Fase 1)

Durante la inspección se detectaron y resolvieron **todas las causas raíz** de la duplicación de procesos:

1. **Procesos obsoletos detenidos de forma segura (sin borrar archivos ni respaldos):**
   - Watchdog heredado PID `20892` (`EcoAccess-Estacion.ps1 -Accion Vigilar`) ejecutándose desde el clon universitario antiguo (`01_UNIVERSIDAD\...`), que relanzaba 6 bots simulados desde `C:\ProgramData\EcoAccessP3Framework\Estacion\bin`.
   - Scripts heredados PID `16268` (`Iniciar-EcoAccess-Nube.ps1`) y PID `28684` (`Validar-Entorno.ps1`) del clon antiguo.
   - Los 7 procesos antiguos de `EcoAccess.P3.Bot.exe` (incluido PID `39920` que retenía el polling de Telegram desde el clon antiguo).
2. **Tarea programada heredada neutralizada:**
   - `EcoAccess P3 - vigilante del bot` (apuntaba al `Vigilar-Bot.ps1` del clon universitario) fue **deshabilitada** para evitar que reaparezcan procesos antiguos al reiniciar.
3. **Estado actual verificado:** **exactamente 1 instancia oficial** de `EcoAccess.P3.Bot.exe` ejecutándose desde `EcoAccess-P3` (`--polling --sin-dispositivo`), sin ningún proceso activo en clones anteriores.

---

## 2. Estado del Escritorio y Prueba Real del Acceso Directo (Fase 2)

- **Hallazgo previo:** el acceso directo antiguo `EcoAccess P3.lnk` ejecutaba `Abrir-EcoAccess.ps1` apuntando al clon universitario (`01_UNIVERSIDAD\...`) y abría binarios obsoletos.
- **Corrección aplicada:** reinstalación oficial mediante `scripts/instalacion/Instalar-EcoAccess.ps1 -Perfil EstacionPrincipal`:
  - **Acceso directo oficial:** `C:\Users\Usuario\OneDrive - unicesar.edu.co\Escritorio\ECOACCESS P3.lnk`
  - **Objetivo:** `C:\Users\Usuario\AppData\Local\Programs\EcoAccess P3\EcoAccess.P3.Presentacion.exe` (última compilación `Release`, sin consola, sin Visual Studio, sin duplicar bots y sin abrir canecas físicas).
  - **Icono:** `C:\Users\Usuario\AppData\Local\Programs\EcoAccess P3\EcoAccess.ico` (icono original del proyecto).
- **Prueba real de doble clic ejecutada:**
  - Invocado vía `Start-Process` sobre `ECOACCESS P3.lnk`.
  - Resultado verificado: **`VENTANA_OK | PID=29888 | Path=C:\Users\Usuario\AppData\Local\Programs\EcoAccess P3\EcoAccess.P3.Presentacion.exe | Responding=True`**, cierre limpio sin errores.

---

## 3. Estado de Visual Studio (Fase 3)

- `EcoAccess.P3.sln` verificada con sus **9 proyectos** (`Presentacion` como primer proyecto / proyecto de inicio, `Compatibilidad`, `Entidades`, `Logica`, `Datos`, `Bot`, `Instalador`, `Tests`, `Datos.Tests`).
- Restauración NuGet (`dotnet restore`), compilación `Debug` (**0 errores**) y `Release` (**0 errores**) comprobadas.
- Selector de soluciones/repositorios recientes de Visual Studio (`ApplicationPrivateSettings.xml`, con respaldo previo `ApplicationPrivateSettings.xml.bak_20261124`) depurado de las 10 entradas antiguas (`01_UNIVERSIDAD`, `EcoAccess-P3-Final`, `EcoAccess-P3-LImpio`, `source\repos`), dejando fijada como favorita (`Fav=True`) la solución oficial `EcoAccess-P3\EcoAccess.P3.sln`.
- Configuración global de Git intacta; configuración local de `EcoAccess-P3` como `Jorge Gutierrez`.

---

## 4. Controlador ESP32, Autodescubrimiento de Puerto y las 7 Combinaciones (Fase 4)

- **Puerto detectado automáticamente:** **`COM3`** (`Silicon Labs CP210x USB to UART Bridge`, `USB\VID_10C4&PID_EA60`).
- **Firmware confirmado (solo lectura `?`):** `estacion_tres_canecas` (`{"ecoaccessDesafio":"<32 hex>"}`).
- **Autodescubrimiento seguro implementado:** `DescubridorPuertoEsp32.cs` + `EjecutorEstacionSerialHmac.cs` + `Iniciar-Modo-ExpoSoftware.ps1` prueban primero el puerto configurado y, si Windows reasigna el número de puerto COM, escanean puertos disponibles usando únicamente el desafío de solo lectura `?` (nunca mueven motores).
- **Mapeo verificado:**
  - Caneca **Verde** (Orgánicos): ID `1400` · Pin `23` · `bridge-verde.json`
  - Caneca **Blanca** (Aprovechables): ID `1093` · Pin `19` · `bridge-blanca.json`
  - Caneca **Negra** (No aprovechables): ID `1401` · Pin `18` · `bridge-negra.json`
- **Seguridad verificada:** `--probar-estacion-fisica verde` sin `--confirmar-presencia-supervisada` es bloqueado inmediatamente (`Exit 1`, `0` tráfico a `COM3`). `GuardiaHardware` permanece activo.

### Resultados diferenciados: Simulación vs Validación Física

| Código | Combinación | Simulación verificada (`--simulado`) | Validación física real (`COM3`) |
|---|---|---|---|
| `C01` | Verde (`1400`) | **APROBADA** (`Simulacion`, 31 ms, `VERDE:1400:Completada`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| `C02` | Blanca (`1093`) | **APROBADA** (`Simulacion`, 51 ms, `BLANCA:1093:Completada`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| `C03` | Negra (`1401`) | **APROBADA** (`Simulacion`, 59 ms, `NEGRA:1401:Completada`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| `C04` | Verde + Blanca | **APROBADA** (`Simulacion`, 27 ms, secuencial `1400 -> 1093`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| `C05` | Verde + Negra | **APROBADA** (`Simulacion`, 41 ms, secuencial `1400 -> 1401`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| `C06` | Blanca + Negra | **APROBADA** (`Simulacion`, 33 ms, secuencial `1093 -> 1401`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |
| `C07` | Verde + Blanca + Negra | **APROBADA** (`Simulacion`, 32 ms, secuencial `1400 -> 1093 -> 1401`) | **PENDIENTE CONFIRMACIÓN PRESENCIAL** |

> **Nota de honestidad:** el hardware ESP32 no cuenta con sensores físicos de posición de tapa; las confirmaciones reales acreditan recepción autenticada HMAC de aplicación PWM (`pwm-apertura-aplicada` / `pwm-completado-sin-sensor`) y requieren verificación visual presencial.

---

## 5. Chatbot de Telegram y Preparación para Teléfonos Android / iPhone (Fase 5)

- **Bot oficial único:** `@EcoAccessP3_JG_bot` (`https://t.me/EcoAccessP3_JG_bot`).
- **Estado en vivo verificado:** **1 sola instancia oficial activa** desde `EcoAccess-P3` (`--polling --sin-dispositivo`):
  - `[19:15:12Z] Identidad comprobada: @EcoAccessP3_JG_bot. Consumidor local exclusivo.`
  - `[19:15:12Z] Página de registro escuchando en http://127.0.0.1:8081/registro/`
  - `[19:15:12Z] Bot en polling con avisos automáticos.`
  - `Consultas de fotos: se anotan en Oracle (CONSULTA_VISION), sin guardar la foto.`
- **Comandos verificados por pruebas automatizadas:** `/start`, `/ayuda`, `/estado`, `/canecas`, `/modo`, `/historial`, `/materiales`, `/separar`, `/verde`, `/blanca`, `/negra`, `/abrir verde blanca`, `/abrir verde negra`, `/abrir blanca negra`, `/abrir verde blanca negra`, clasificación por texto y fotografías con el catálogo oficial.
- **Control de acceso móvil:** cualquier teléfono Android o iPhone con Telegram e Internet puede abrir `@EcoAccessP3_JG_bot` y consultar información/clasificación; los comandos de apertura están bloqueados por `CoordinadorEstacionUnificado` para chats no autorizados y en modo físico requieren sesión supervisada activa.
- **Estado de prueba con teléfono físico real:** la instancia oficial ya está escuchando en vivo lista para que Jorge envíe un mensaje desde su teléfono; este informe **no** declara validada una interacción humana desde un teléfono hasta que Jorge la ejecute presencialmente.

---

## 6. Instalador Multiequipo y Paquete para Compañeros (Fase 6)

- **Archivos creados/actualizados en Git:**
  - `scripts/instalacion/Instalar-EcoAccess.ps1` (perfiles `EstacionPrincipal` / `EstacionCompanero`, verificación `.NET 4.8`, cámara, Ollama, wallet individual, acceso directo `ECOACCESS P3.lnk`, `-Desinstalar` preservando wallet y credenciales personales).
  - `scripts/instalacion/Verificar-Equipo-EcoAccess.ps1` (diagnóstico autónomo sin rutas de OneDrive ni dependencias de `COM3` en perfil compañero).
  - `scripts/instalacion/Publicar-Paquete.ps1` (empaquetador versionado en `dist\EcoAccess-P3-3.1.0-exposoftware-<commit>.zip`, sin wallets ni secretos).
  - `docs/instalacion/GUIA_INSTALACION_COMPANEROS_EXPOSOFTWARE.md`.
- **Prueba de instalación limpia ejecutada (entorno aislado en Windows):**
  - Se generó el paquete `.zip`, se descomprimió en `%TEMP%\EcoAccess_CleanTest_Pkg`, se instaló con `-Perfil EstacionCompanero -Destino %LOCALAPPDATA%\EcoAccessP3_PruebaLimpia`, se ejecutó `Verificar-Equipo-EcoAccess.ps1` (todos los checks OK) y se desinstaló limpiamente conservando wallet y credenciales.
- **Nota honesta sobre computadores de compañeros:** no se accedió de forma remota a los PC físicos de los compañeros; el paquete `.zip`, el instalador y el verificador quedan listos para que ellos los ejecuten siguiendo la guía.

---

## 7. Estado de Oracle Cloud y Esquema Aislado (Fase 7)

- **Oracle Cloud (`--comprobar-oracle`, solo lectura):** **CONECTADO** (`41 materiales habilitados`, `0` escrituras sobre producción `ECOACCESS_P3_APP`).
- **Wallet local verificado:** `C:\Users\Usuario\.oracle\wallets\EcoAccess` con alias `ecoaccess_tp` (nunca incluido en Git ni en el `.zip`).
- **Pruebas de wallet y cargador SQL (`EcoAccess.P3.Datos.Tests --filter WalletOracleNubeTests|LectorScriptSqlTests`):** **26/26 superadas** (672 ms).
- **Dependencia de migración `V028`:** verificada como **no bloqueante** para producción (`ECOACCESS_P3_APP` opera normalmente; la tabla extendida `V028` ya validada en `ECOACCESS_P3_PRUEBAS` es opcional y la app usa `ColaLocalOperaciones` sin error si no está aplicada).

---

## 8. Reconocimiento Visual y Corpus de 41 Fotos Reales (Fase 8)

- **Modelos locales disponibles en Ollama `v0.40.16`:** `lfm3-vl:latest` (predeterminado) y `qwen3-vl:4b` (configurable).
- **Pruebas deterministas de las 9 combinaciones obligatorias + fallos (`PruebasEscenariosExpoSoftwareFase8Tests`):** 100 % aprobadas (manzana→VERDE, papel limpio arrugado→BLANCA, hilo dental→NEGRA, combinaciones V+B / V+B+N, manzana+papa+cebolla→1 sola VERDE, papa+papel+cable USB→V+B+`GESTION_ESPECIAL`, cable USB solo y blíster de medicamentos→`GESTION_ESPECIAL`, imágenes dañadas/dudosas→sin apertura automática).
- **Comparativa honesta sobre el corpus de 41 fotografías reales (`lfm3-vl:latest`, sin alterar etiquetas verdaderas):**
  - **Línea base (`eval_41_reales_verificadas_lfm3vl.md`):** `30/41` exactas (**73,2 %**), **10 falsas aperturas**, 2 confirmaciones (`VERDE 80,0 %`, `BLANCA 42,9 %`, `NEGRA 100,0 %`, latencia media `5,99 s`).
  - **Reevaluación con reglas de seguridad y distinción ampliada (`eval_41_reales_verificadas_lfm3vl_v2.md`):** `26/41` exactas (**63,4 %**), **falsas aperturas reducidas de 10 a 6 (-40 %)** gracias al aumento de solicitudes de confirmación humana (`8` vs `2`), `VERDE` subió a **84,0 %**, y en las 5 fotos problemáticas (`REAL_02`, `REAL_03`, `REAL_04`, `REAL_41`) el papel de cuaderno limpio pasó a `B08 BLANCA` y `cable USB + caja de cartón` clasificó ambos correctamente (`GESTION_ESPECIAL` + `B09 BLANCA`), pero el prompt más extenso incrementó la cautela en papel sanitario (`NEGRA 40,0 %`).
  - **Conclusión honesta:** el modelo local `lfm3-vl` **no alcanza 100 % de precisión**; por ello se mantiene la confirmación humana y el registro manual ante cualquier duda o clasificación incierta.
- **Códigos pendientes sin imagen real (`COBERTURA_35_MATERIALES.md`):** se conservan honestamente como `FALTA IMAGEN` (`0` fotos) los 5 códigos **`B03`, `B13`, `N10`, `N11`, `N12`**.

---

## 9. Pruebas Automatizadas y Verificación de Clonación Limpia (Fases 11 y 12)

- `dotnet restore EcoAccess.P3.sln`: **OK**
- `dotnet build EcoAccess.P3.sln -c Debug`: **0 errores**
- `dotnet build EcoAccess.P3.sln -c Release`: **0 errores**
- `dotnet test EcoAccess.P3.Tests -c Debug`: **2.406 superadas, 0 fallidas, 1 omitida** (enlace simbólico sin elevación Windows), total 2.407 (14 s)
- `dotnet test EcoAccess.P3.Tests -c Release`: **2.406 superadas, 0 fallidas, 1 omitida**, total 2.407 (14 s)
- `EcoAccess.P3.Datos.Tests` (`WalletOracleNubeTests|LectorScriptSqlTests`): **26/26 superadas** (672 ms)
- **Clonación limpia y compilación desde GitHub (`feat/ecoaccess-vision-integracion-final`):** ejecutada y verificada al cierre del push (ver sección de cierre en PR #23).

---

## 10. Instrucciones de Arranque, Parada y Revisión de PR #23

- **Abrir aplicación Windows (usuario):** doble clic en el acceso directo **`ECOACCESS P3`** del Escritorio.
- **Diagnóstico completo sin abrir canecas:**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\desarrollo\Iniciar-Modo-ExpoSoftware.ps1 -SoloDiagnostico
  ```
- **Iniciar modo ExpoSoftware con bot seguro (sin hardware):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\desarrollo\Iniciar-Modo-ExpoSoftware.ps1 -IniciarBotDemo
  ```
- **Iniciar estación física supervisada (solo con presencia física y tapas despejadas):**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\desarrollo\Iniciar-Modo-ExpoSoftware.ps1 -HabilitarModoFisico
  ```
- **Detener todos los servicios del bot / estación:**
  ```powershell
  powershell -ExecutionPolicy Bypass -File .\scripts\desarrollo\Iniciar-Modo-ExpoSoftware.ps1 -DetenerTodo
  ```
- **Revisión y fusión del PR #23 a `main` (manual por Jorge):** revisar commits en `https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/23`, quitar estado Draft ("Ready for review") y fusionar mediante Merge Commit para preservar la autoría histórica de todos los integrantes.

---

## 11. Limitaciones y Pendientes Reales

1. **Pruebas físicas C01–C07 sobre `COM3`:** listas para ejecutarse una por una en cuanto Jorge confirme presencia física, tapas despejadas, alimentación estable y parada inmediata.
2. **Prueba interactiva en teléfono real (Android/iPhone):** el bot `@EcoAccessP3_JG_bot` ya está activo en polling exclusivo; resta el envío manual de prueba desde el celular de Jorge.
3. **Instalación en los computadores físicos de los compañeros:** validada localmente en una instalación limpia aislada; resta ejecutar el `.zip` en cada equipo de los compañeros con su wallet privado.
4. **Precisión visual local:** `lfm3-vl:latest` logra entre 63,4 % y 73,2 % en las 41 fotos reales (con 40 % menos falsas aperturas en v2) y existen 5 códigos `FALTA IMAGEN` (`B03`, `B13`, `N10`, `N11`, `N12`); ante incertidumbre el usuario confirma o usa registro manual.
