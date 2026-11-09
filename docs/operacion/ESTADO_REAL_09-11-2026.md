# Estado real de EcoAccess P3 en el equipo del propietario — 09/11/2026

Proyecto EcoAccess P3 · Universidad Popular del Cesar · Programación de Computadores III · Jorge Gutierrez Thomas y
Heyder Canchila. Rama `desarrollo/telegram-doble-ia` (PR #11, borrador). Este documento separa lo **implementado**, lo
**probado con simulador**, lo **verificado en el entorno real** y lo **pendiente**.

## 1. Diagnóstico del entorno

| Elemento | Hallazgo verificado |
|---|---|
| GitHub CLI | sesión activa como `JorgeGutho21` |
| PR #11 | borrador, cabeza `desarrollo/telegram-doble-ia`. La auditoría del tercer corte (3 commits) se integró por avance rápido, sin forzar ni abrir otro PR |
| Copia de trabajo principal | archivos sin versionar ajenos a esta tarea (`scripts/docker/`, `scripts/pruebas/Ejecutar-Integracion-Temporal.ps1`): no se tocaron; el trabajo se hizo en un worktree aparte |
| Windows / .NET | Windows 11 Pro, .NET Framework 4.8.09221, Developer Pack 4.8, .NET SDK 10.0.403, Visual Studio 18 Community |
| Oracle Cloud | wallet en `TNS_ADMIN`, alias `ecoaccess_tp`, esquema `ECOACCESS_P3_APP`; responde a consultas reales |
| Oracle local | servicio `OracleServiceFREE` activo en 1521; **no** existe el contenedor `PRUEBAP3` (1522) ni Docker |
| Telegram | **el mismo token** está en los User Secrets de EcoAccess P3 y de EcoAccess V2 (comparado por SHA-256, sin mostrarlo); hoy atiende el bot de V2, así que el de P3 no debe iniciarse a la vez |
| Ollama | activo con `lfm3-vl` |
| ESP32 | **ningún puerto COM** en Windows: la placa no está conectada |

## 2. Base de datos compartida y QR oficial

- P3 y V2 usan el **mismo esquema** de Oracle Cloud. En él hay tres canecas de V2 (`DEMO - Caneca ficticia/verde/negra`,
  estación `DEMO - Bloque ficticio`), las tres en estado 2 (`FueraDeServicio` para P3).
- El QR oficial `assets/identidad/QR_Caneca_piloto.png` (SHA-256 `2e72526b…0024`) se leyó con ZXing: abre
  `https://t.me/EcoAccessP3_JG_bot?start=EcoAccessCanecaPiloto1`. **Ninguna caneca de Oracle Cloud tiene ese
  identificador**: la caneca piloto que lo usa la crea `database/03_datos_prueba/04_piloto_simulado.sql`, que por diseño
  solo corre en la base de pruebas `PRUEBAP3`. Consecuencia: hoy, escanear el QR oficial contra la nube **no identifica
  una estación** y Eco solo puede orientar. No se insertó nada en Oracle: requiere decisión del propietario (§6).
- Corregido en la aplicación: «Continuar con QR» ya no genera un QR a partir de la base; muestra, guarda e imprime el
  **archivo oficial** incrustado y avisa si la base conectada no tiene la caneca con ese QR. Pruebas `QrOficialTests`
  fijan el hash del archivo y su contenido decodificado.

## 3. Ejecución real de Windows Forms

Evidencias en `docs/evidencias/ejecucion_real/` (capturas reales de esta sesión):

1. `01_bienvenida_tras_comprobar_oracle.jpg`: la aplicación (Release) pasó la comprobación de Oracle y abrió la bienvenida.
2. `02_invitado_catalogo_desde_oracle_cloud.jpg`: modo invitado con el catálogo leído de Oracle Cloud.
3. `03_busqueda_botella_en_oracle.jpg`: búsqueda «botella» → 2 materiales desde Oracle.
4. `04_qr_oficial_sin_regenerar.jpg`: ventana del QR oficial con Guardar PNG, Imprimir, Copiar enlace y Abrir en Telegram.

Cierre y reapertura: la aplicación terminó sin procesos colgados y volvió a abrir en 5,4 s con la comprobación de
Oracle. No se probó el inicio de sesión de administrador ni el registro desde la interfaz: necesitan credenciales del
propietario o crear datos en la base compartida. Hallazgo: las tarjetas muestran «No se pudo comprobar la admisión en
la caneca piloto» porque la caneca piloto no existe en la nube (§2).

## 4. Hardware

Según el firmware y `hardware/README.md`: **una caneca, una tapa, dos servos** (GPIO 18 y 19) que mueven la misma tapa,
`ABIERTO_MS = 10000`, `CALIBRACION_VERIFICADA = false`, sin sensor de depósito, fuente de 5 V / 1 A no validada. No hay
puerto COM. No se movió ningún actuador ni se cambió una protección. Tres canecas físicas independientes requerirían
al menos un actuador (o par de servos) por tapa, GPIO libres, fuente dimensionada por la corriente de arranque de todos
los motores y una identidad/caneca por orden en el firmware; hoy no existen.

## 5. Tabla de componentes

Porcentaje solo cuando hay una medición definida; si no, N/D.

| Componente | Implementado | Pruebas | Funcionamiento real | % verificable | Pendientes | Evidencia |
|---|---|---|---|---|---|---|
| C# y arquitectura | Sí (4 capas + raíces de composición) | 1630/1631 unitarias; 12/12 arquitectura | compila Debug y Release net48 | 99,9 % de pruebas unitarias aprobadas | — | `docs/academico/` |
| Oracle Cloud | Sí | 203/208 integración (0 fallos, 5 omitidas por visión real), sin residuos | consultas reales desde la app y el validador | 97,6 % de la batería (las omitidas no cuentan como aprobadas) | limpieza autorizada de 19 usuarios y 600 updates de ensayo | `INFORME_AUDITORIA_TERCER_CORTE.md` §J |
| Windows Forms | Sí | arnés visual; pruebas de capas | abierta, catálogo y búsqueda contra Oracle, QR oficial, reinicio | N/D | sesión de administrador y registro desde la UI | `docs/evidencias/ejecucion_real/` |
| Telegram | Sí | unitarias e integración | el bot activo es el de V2 (mismo token) | N/D | recorrido real con el bot de P3 | §1 |
| Reconocimiento visual | Sí | ver `docs/vision/` | modelo local activo | N/D en P3 en esta sesión (192/217 es otra metodología, ver §7) | fotos reales de teléfono | `RECONOCIMIENTO_GENERAL.md` |
| Caneca blanca | Lógica y datos | simulador | sin hardware | N/D | caneca física | §4 |
| Caneca verde | Solo datos en la base compartida (V2) | — | no existe físicamente | N/D | hardware y reglas en P3 | §2, §4 |
| Caneca negra | Solo datos en la base compartida (V2) | — | no existe físicamente | N/D | hardware y reglas en P3 | §2, §4 |
| QR | Archivo oficial intacto; app lo muestra e imprime | 3 pruebas `QrOficialTests` | decodificado; no identifica estación en la nube | N/D | caneca con `EcoAccessCanecaPiloto1` en la base elegida | §2 |
| ESP32 | Firmware y protocolo HMAC | banco del bridge con simulador | placa no conectada | N/D | conexión, calibración, medición de corriente | `hardware/README.md` |
| Puerto y comunicación | `PuertoSerial` configurable | simulador | sin puerto COM | N/D | identificar el COM real | §1 |
| Temporizador | 10 s, fuente única | `PruebasTercerCorte`, `BridgeOracleTests` | no medido con cronómetro | N/D | medición física | — |
| Infraestructura cloud | Solo Oracle Autonomous DB | — | el bot corre en este PC | N/D | decidir alojamiento (§6) | — |
| Power BI | Vistas `EA_R_*` y modelo PBIP en V2 | — | alias y TLS correctos; falta la credencial | N/D | credencial del propietario | `EcoAccess_V2/powerbi/README.md` |
| Documentación académica | Sí | revisión contra el código | — | N/D | — | `docs/academico/` |
| GitHub y PR | PR #11 actualizado | — | — | N/D | revisión y paso de borrador a listo | PR #11 |

## 6. Decisiones que necesita el propietario

1. **Qué base respalda el QR oficial.** Opciones: (a) agregar en Oracle Cloud una caneca piloto con
   `EcoAccessCanecaPiloto1` (inserción nueva, sin borrar nada, pero en la base que también usa V2); (b) volver a la base de
   pruebas local `PRUEBAP3` (requiere Docker). Sin una de las dos, el QR oficial no abre ninguna caneca.
2. **Qué bot atiende el token** (P3 o V2): solo uno a la vez.
3. **Alojamiento permanente del bot.** El bot de P3 es .NET Framework 4.8: necesita Windows (no corre en un
   contenedor Linux). Alternativas: un servidor Windows (de pago), o portar el bot a .NET 10 (V2 ya compila en net10) y
   usar una VM Always Free de Oracle Cloud con Linux. Ninguna se contrató ni se desplegó.
4. **Limpieza de datos de ensayo** (19 usuarios ficticios y 600 updates simulados): no autorizada todavía.

## 7. Dos evaluaciones visuales que no se deben mezclar

- **192/217 (88,5 %)** — EcoAccess P3, `docs/vision/RECONOCIMIENTO_GENERAL.md`: 217 fotos públicas de Wikimedia, tarea
  «qué objeto es» en tipos amplios (animal, persona, electrónico, aprovechable…), código `af53938`, validación ejecutada
  una vez.
- **19/50 (38 %)** — EcoAccess V2, `EVALUACION_DOBLE_IA.md`: 50 fotos TrashNet de cinco materiales, tarea «código exacto
  de material» con el contrato `objeto-v2`; papel 0/10. Mide otra cosa con otro repositorio y otras fotos.

Ninguna prueba con fotos de teléfono reales permite todavía afirmar una exactitud de campo.

## 8. Pasos manuales, en orden

1. Conectar el ESP32 por USB y anotar el puerto que aparece en el Administrador de dispositivos (o con
   `Validar-Entorno.ps1`). No alimentar los servos desde el ESP32.
2. Medir la fuente de los servos (corriente de arranque y de bloqueo) antes de montarlos; calibrar **sin tapa** y solo
   entonces poner `CALIBRACION_VERIFICADA = true` en `configuracion_privada.h`.
3. Decidir §6.1 y §6.2. Con eso se puede hacer el recorrido real: escanear el QR, `/vincular`, foto, apertura.
4. Escribir la credencial de Oracle en Power BI (V2) y actualizar.
5. Pedir a Heyder que siga `docs/instalacion/INSTALACION_COMPANEROS.md` en su computador y comparta la salida de
   `Validar-Entorno.ps1 -ProbarOracle`.
