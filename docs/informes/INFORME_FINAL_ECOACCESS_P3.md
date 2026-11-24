# INFORME FINAL DE VALIDACIÓN Y CIERRE — ECOACCESS P3

- **Responsable:** Jorge Gutierrez
- **Fecha de ejecución:** 23–24 de noviembre de 2026
- **Principio de evidencia:** Todos los resultados presentados provienen de compilaciones, suites de pruebas y evaluaciones visuales ejecutadas y verificadas. No se confunde cero pruebas fallidas con 100 % de precisión visual, ni simulación con hardware físico real, ni un Pull Request abierto con código ya fusionado a `main`.

---

## 1. Ruta oficial única y solución principal
- **Carpeta oficial definitiva:** `C:\Users\Usuario\OneDrive - unicesar.edu.co\Documentos\02_PROYECTOS_PERSONALES\01_EcoAccess\01_ProyectoDeAula_EcoAccess\EcoAccess-P3`
- **Solución principal ejecutable:** `EcoAccess.P3.sln` (`.NET Framework 4.8`, `C# 9.0`, SDK `10.0.403`).
- **Solución académica complementaria conservada:** `BaseDeDatos_Amilkar/10_CSHARP_VISUAL_STUDIO/SOLUCION_CSHARP/EcoAccessBD.sln`.

## 2. Repositorio local activo e identidad Git
- **Raíz Git activa:** `...\01_ProyectoDeAula_EcoAccess\EcoAccess-P3\.git`
- **Remoto oficial:** `origin = https://github.com/JorgeGutho21/EcoAccess-2026-2.git`
- **Rama principal sincronizada:** `main` en `e7cf7bc7` (`origin/main`, `0/0`).
- **Rama de trabajo publicada:** `feat/ecoaccess-vision-integracion-final` (incluye fusión limpia de `origin/main` en `57267b3`).
- **Configuración Git local exclusiva de este repositorio:** `user.name = Jorge Gutierrez`, `user.email = 263990811+JorgeGutho21@users.noreply.github.com`, `core.longpaths = true`.

## 3 y 4. Estado de Visual Studio y copias redundantes
- **Proceso Visual Studio detectado durante la auditoría:** `devenv.exe` (PID `29372`) en ejecución y referencia detectada en `%LOCALAPPDATA%\Microsoft\VisualStudio\18.0_9a9f4961\ApplicationPrivateSettings.xml`. No se cerró forzosamente Visual Studio para evitar pérdida de trabajo abierto en el IDE.
- **Clon universitario anterior (`C:\Users\Usuario\Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\EcoAccess-P3`):** auditado y verificado limpio (`0` cambios sin confirmar, `main` idéntico a `origin/main` en `e7cf7bc7`). Se conservó sin borrar mientras `devenv.exe` permanezca abierto; una vez cierres Visual Studio y abras únicamente `EcoAccess-P3\EcoAccess.P3.sln` desde OneDrive, puedes retirar esa referencia del selector de repositorios («Quitar de la lista») y eliminar el clon universitario redundante.
- **Carpetas inactivas archivadas y respaldadas (SHA-256 verificado en `Documents\01_UNIVERSIDAD\2026-2\_Respaldos_EcoAccess\Respaldo_Consolidacion_2026-11-22\`):** `EcoAccess-P3-LImpio` (589/589 SHA-256), `EcoAccess-P3-Final` (90/90 SHA-256), `Docs_Previos` y `FaseFinal_Backup` (respaldos verificados de `MotorReglasMultirresiduo.cs` e `imagenes_faltantes.csv`).
- **Clones temporales de validación:** eliminados inmediatamente tras concluir la verificación de clon limpio.

## 5 y 6. Estado de GitHub y auditoría de los commits de Heyder Hedrey Canchila de Angel
- **Commits de Heyder verificados en `origin/main` e integrados sin conflictos mediante `git merge origin/main` (`57267b3`), conservando intacta su autoría original:**
  - `e7cf7bc7` (*Registro manual con selector de canecas y tabla de Canecas y QR sin cortes*): `FormRegistroManual.Diseno.cs`, `FormRegistroManual.cs`, `FormContenedores.Diseno.cs`, `ComponentesGestion.cs`, `ModuloEco.cs` y nuevas imágenes en `assets/imagenes/registro/caneca_{blanca,negra,verde}.png`.
  - `62f0f5fe` (*Merge origin/main en fix/instalador-wallet-tns-admin*): integra PRs #20, #21, #22 junto con `f342e23`, `5707c1b` y `ab7bee0` (*usar el wallet de `TNS_ADMIN` y la cadena guardada solo con alias*, `WalletOracleNube.cs`, `EcoAccess.P3.Instalador/Program.cs` y `WalletOracleNubeTests.cs`).
- **Coexistencia verificada:** el registro manual (selector de una caneca en servicio en `FormRegistroManual`) y el reconocimiento visual multirresiduo (`FormReconocimientoVisual`, 1 a 3 canecas) coexisten sin contradicciones y ambos compilan y pasan las pruebas de regresión y arquitectura.

## 7. Corrección del Bug #4 y demás fallos resueltos
- **Bug #4 (`MotorReglasMultirresiduo.cs`):** las cajas de cartón vacías y limpias cuya descripción incluye `"cable"` o `"medicine"` (p. ej., `"Xidoxx charging cable box"` o `"empty medicine cardboard box"`) ahora clasifican como `B09` (`BLANCA`), manteniendo `GESTION_ESPECIAL` (0 aperturas ordinarias) para `charging cable`, `USB cable`, `blister pack of pills` y `expired medicine`. Comprobado en pruebas unitarias (`PruebasFallosYRecuperacionFase9Tests.Bug4_...`) y en vivo sobre la foto real `#41` (`Obt = C02_Blanca` + cable en `GESTION_ESPECIAL`).
- **Otros fallos corregidos en la rama:** escalado seguro en memoria (`≤ 768 px`) que eliminó `HTTP 400` de Ollama sobre fotos `1200×1600`; compatibilidad de `qwen3-vl:4b` (`format="json"`, `num_predict=768`, timeout `150 s`); precedencia `B10`/`B11` sobre `B09`; protección de `N12` (`used face mask`), `red onion`/`citrus` → VERDE y `tissue paper` → `N01` NEGRA.

## 8. Estado de imágenes faltantes y cobertura del catálogo
- **Catálogo en repositorio (`assets/imagenes/residuos/`):** `139` archivos (`0` corruptos, `0` duplicados SHA-256), `77` fotos reales mapeadas a `30/35` materiales.
- **Los 5 códigos pendientes confirmados honestamente como `FALTA IMAGEN`** en `datasets/ecoaccess35/reportes/imagenes_faltantes.csv` y `02_Imagenes_Reales_EcoAcess/inventario/imagenes_faltantes.csv`: **`B03`**, **`B13`**, **`N10`**, **`N11`** y **`N12`** (la inspección visual directa de las 41 fotografías reales comprobó que `#01` es servilleta/pañuelo, `#11` es esponja usada y `#37` es medio lulo + blíster, por lo que no existen fotografías reales de tapabocas `N12`).
- **Referencias externas verificables:** registradas por separado en `datasets/ecoaccess35/reportes/fuentes_imagenes_web.csv` (Wikimedia Commons, sin mezclarse con fotos originales locales).

## 9 y 10. Compilaciones y pruebas automatizadas verificadas

| Verificación / Suite | Ejecutadas | Aprobadas | Fallidas | Omitidas | Detalle |
|---|---:|---:|---:|---:|---|
| Compilación `Debug` (`EcoAccess.P3.sln`) | 9 proyectos | 9 | **0** | 0 | 0 errores tras integrar Heyder + Bug #4 |
| Compilación `Release` (`EcoAccess.P3.sln`) | 9 proyectos | 9 | **0** | 0 | 0 errores (también confirmado en clon limpio) |
| Unitarias (`EcoAccess.P3.Tests`) | **2.401** | **2.400** | **0** | **1** | Única omitida: `EnlaceSimbolicoSeRechazaAntesDeClasificar` (privilegio Win32 `CreateSymbolicLink` en Windows). |
| Integración Oracle (`EcoAccess.P3.Datos.Tests`) | **254** | **247** | **0** | **7** | Ejecutadas exclusivamente en `ECOACCESS_P3_PRUEBAS@localhost:1521/freepdb1` (incluye `WalletOracleNubeTests` de Heyder). 7 omitidas justificadas: 5 pruebas opt-in de corpus MIT/TrashNet (`ECOACCESS_PROBAR_VISION_*`), 1 exclusiva de PDB Docker `PRUEBAP3`, 1 de base sin administradores. |
| **Total combinado** | **2.655** | **2.647** | **0** | **8** | **0 pruebas fallidas** en todas las suites ejecutables. |

## 11 y 12. Funcionamiento Demo PC, Demo Telegram y compatibilidad entre canales
- **Demo PC (`PruebasDemoPcYTelegramTests` + `MaquinaEstadosYRespaldoOfflineTests`):** verificadas las **7 combinaciones (`C01`–`C07`)** mediante `GestorSesionMultirresiduo` + `SimuladorEstacionTresCanecas` (apertura, cierre, restablecimiento a `FINALIZADO`, rechazo de órdenes vencidas y bloqueo de duplicados por `idOperacion`). Todas en simulación (`EsSimulado = true`).
- **Demo Telegram (`FlujoEcoDefinitivoTests`, `EstacionTresTapasTests`, `PruebasDemoPcYTelegramTests`):** verificado el flujo de conversación, sesión QR, rechazo de órdenes vencidas/no autorizadas, concurrencia PC + Telegram sobre una misma clave de idempotencia (exactamente 1 ejecución `Completada` y 1 `Duplicada`) y coherencia de clasificación sobre el mismo `MotorReglasMultirresiduo`. La sesión interactiva contra el servidor real de Telegram en vivo queda supeditada al token/bot activo en ejecución supervisada.

## 13. Combinaciones matemáticas de canecas (`MotorCombinacionesCanecas`)
- Verificadas exhaustivamente por código para el catálogo vigente (`10` VERDE + `13` BLANCA + `12` NEGRA = `35`):
  - **Sin repetición:** `595` pares (`189` de 1 caneca + `406` de 2 canecas) y `6.545` tríos (`626` de 1 + `4.359` de 2 + `1.560` de 3 canecas) = **`7.140` combinaciones**.
  - **Con repetición:** `630` pares y `7.770` tríos = **`8.400` combinaciones**.
  - **Escenas obligatorias `A`–`I` + `GESTION_ESPECIAL`:** 100 % aprobadas en pruebas deterministas (incluyendo `manzana + papel limpio + hilo dental usado → VERDE + BLANCA + NEGRA` y `papa + papel limpio + cable USB → VERDE + BLANCA + GESTION_ESPECIAL`).

## 14 y 15. Evaluación empírica real de las 41 fotografías (`02_Imagenes_Reales_EcoAcess`) y comparativa de modelos
- **Auditoría visual humana directa:** `41` fotografías válidas (`1200×1600`, `0` duplicados SHA-256, `7` pares perceptualmente similares dHash ≤ 8): **`32` individuales** y **`9` multirresiduo reales** etiquetadas en `datasets/ecoaccess35/reales/manifiesto_41_reales_verificadas.json`.
- **Resultados empíricos medidos en vivo con `lfm3-vl:latest` (`reportes/vision35/eval_41_reales_verificadas_lfm3vl.{json,md}`):**
  - **Procesamiento técnico:** `41/41` procesadas, **`0` fallos de integración**, **`0` fallos de hardware**, latencia media **`5,99 s`** (P95 **`7,17 s`**).
  - **Exactitud en individuales reales (`32` fotos):** **`28/32` (`87,5 %`)**. Los 4 fallos corresponden a papel cuadriculado arrugado (`#02`–`#04`) y bolsa arrugada (`#14`), que el modelo 1.7B clasificó conservadoramente como sucio/sanitario (`NEGRA`).
  - **Exactitud de combinación exacta en multirresiduo reales (`9` fotos):** **`4/9` (`44,4 %`)**:
    - `#32` (manzana + caja `Xialox`) → **`C04_VerdeBlanca` (EXACTO)**
    - `#39` (manzana + caja `Xialox`) → **`C04_VerdeBlanca` (EXACTO)**
    - `#38` (lulo + blíster tabletas) → **`C01_Verde` + `GESTION_ESPECIAL` (EXACTO)**
    - `#41` (cable USB + caja `Xialox`, Bug #4) → **`C02_Blanca` + `GESTION_ESPECIAL` (EXACTO)**
    - Fallos reales en 5 escenas: `#34`, `#35`, `#40` (papel cuadriculado arrugado clasificado como sucio → NEGRA) y `#36`, `#37` (reverso metálico del blíster confundido con envoltura de mecato → NEGRA).
  - **Total combinación exacta en las 41 fotos reales:** **`32/41` (`78,0 %`)** *(el evaluador estricto reportó `30/41 = 73,2 %` porque marcó `EsCasoCriticoSeguridad=true` en las escenas mixtas `#38` y `#41`; por categoría: VERDE `80,0 %`, BLANCA `42,9 %`, NEGRA `100,0 %`)*.
- **Comparativa `lfm3-vl:latest` vs `qwen3-vl:4b` (Ollama `0.40.16`, GTX 1650 4 GB VRAM, 31,84 GB RAM):**
  - En los 3 casos reales base (`comparativa_3base_real_lfm3_vs_qwen3vl.md`): ambos **`3/3` (`100 %`)**, `0` falsas aperturas, `0` errores; `lfm3-vl:latest` **9,02 s** (100 % GPU, predeterminado) vs `qwen3-vl:4b` **84,78 s** (`68 % CPU / 32 % GPU`, alternativa configurable recomendada en GPUs ≥ 6–8 GB VRAM).

## 16 y 17. Registro manual, interfaz y validación Oracle
- **Interfaz y registro manual:** los formularios actualizados por Heyder (`FormRegistroManual`, `FormContenedores`, `FormQrCaneca`) y `FormReconocimientoVisual` compilan con `0` errores en `Debug` y `Release`. La inspección gráfica interactiva completa en distintas escalas DPI dentro de Visual Studio queda lista para comprobación visual directa por el equipo.
- **Oracle aislado (`ECOACCESS_P3_PRUEBAS@localhost:1521/freepdb1`):**
  - **`V028_catalogo_35_prioritarios_y_multirresiduo.sql` y `V028_reversion.sql` validadas en vivo en el esquema aislado:** `2/2` tablas creadas (`CATALOGO_PRIORITARIO_35`, `REGISTRO_MULTIRRESIDUO`), restricción `UQ_REGMULTI_IDEMPOTENCIA` verificada bloqueando duplicados (`True`), y reversión limpia dejando `0` tablas residuales.
  - **`EcoAccess.P3.Datos.Tests`:** **`247` aprobadas, `0` fallidas, `7` omitidas** (incluyendo `WalletOracleNubeTests`).
  - **Oracle Cloud (`ECOACCESS_P3_APP`):** **0 escrituras de prueba y `V028` NO aplicada a producción** (requiere autorización independiente).

## 18 y 19. Simulación ESP32 y pruebas físicas pendientes
- **Simulación ESP32 (`EstacionTresTapasTests`, `SimuladorEstacionTresCanecas`, `PruebasFallosYRecuperacionFase9Tests`):** validados pines (`19` blanca, `23` verde, `18` negra), HMAC-SHA256, apertura/cierre, rechazo de orden cruzada, desconexión/reconexión, recuperación desde `ERROR_SEGURO` y `GuardiaHardware` activo (`0` activaciones físicas).
- **Hardware físico real:** **`PRUEBA FÍSICA PENDIENTE`** (requiere sesión presencial supervisada con el ESP32 y servomotores conectados).

## 20. Estado del Pull Request #23 y verificación desde clon limpio
- **Pull Request oficial actualizado (Draft PR, NO fusionado automáticamente a `main`):**
  **https://github.com/JorgeGutho21/EcoAccess-2026-2/pull/23**
- **Rama:** `feat/ecoaccess-vision-integracion-final` (sincronizada con `origin/main` `e7cf7bc7`).
- **Clon limpio desde GitHub:** verificado en carpeta temporal independiente (descarga, restauración NuGet, compilación `Release` con `0` errores, pruebas unitarias aprobadas y posterior eliminación del clon temporal).