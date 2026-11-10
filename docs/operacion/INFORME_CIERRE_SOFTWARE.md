# Informe de cierre de la fase de software — EcoAccess P3

Universidad Popular del Cesar · Programación de Computadores III · Jorge Gutierrez Thomas y Heyder Canchila · 09/11/2026.
Estados: **REAL** (verificado en entorno real), **SIMULADOR** (verificado con simulador), **SIN VALIDAR** (implementado
sin validación), **PENDIENTE** (pendiente o bloqueado). Todo porcentaje indica su denominador.

## 1. Resumen ejecutivo

EcoAccess P3 funciona como sistema de software en el computador del propietario contra Oracle Cloud: QR oficial con su
estación, bot de Telegram P3 como único consumidor, registro HTTPS publicado por Tailscale Funnel, registro y vinculación
probados con 30 personas simultáneas sin duplicados, reconocimiento visual local medido en fotos independientes y modo
software que no emite órdenes físicas. Falta la prueba desde el teléfono, la credencial de Power BI y todo el hardware.

## 2. Estado por componente

| Componente | Estado | Evidencia |
|---|---|---|
| Compilación Debug/Release net48 | REAL | 0 errores |
| Pruebas unitarias | REAL | 1648/1649 (1 omitida) |
| Oracle Cloud: conexión, consultas, transacciones | REAL | `Validar-Entorno.ps1 -ProbarOracle`; integración 203/208 sin fallos (5 omitidas por visión real) |
| QR oficial → estación piloto 1093 | REAL | `QrOficialIdentificaLaEstacion…` contra Oracle; archivo sin cambios (`QrOficialTests`) |
| Bot de Telegram P3 único consumidor | REAL | identidad `@EcoAccessP3_JG_bot`, sin webhook; bot de V2 detenido sin tocar datos |
| Conversación real desde el teléfono | PENDIENTE | sin actividad del teléfono en el registro del bot |
| Registro HTTPS (Funnel, solo `/registro`) | REAL | local y pública HTTP 400 sin token, certificado válido; otras rutas 404 |
| Registro y vinculación, 30 simultáneos | REAL (servidor de registro en proceso, Oracle Cloud real) | `docs/pruebas/CARGA_30_USUARIOS.md`: 13/13, 270 recorridos, 0 errores, sin residuos |
| Registro desde otra red por Funnel | PENDIENTE | requiere el teléfono con datos móviles |
| Modo software (sin órdenes ni aperturas) | REAL | prueba contra Oracle: 0 interacciones y 0 órdenes nuevas |
| Reconocimiento visual P3 | REAL (TrashNet, no fotos de teléfono) | `docs/vision/EVALUACION_P3_TRASHNET.md`: destino 87/100, 0 aperturas con material equivocado |
| Productos colombianos y casos especiales (perros, sanitarios, RAEE…) | PENDIENTE | no hay fotos con licencia verificada en el equipo; no se inventaron resultados |
| Windows Forms (inicio, catálogo, búsqueda, QR, caneca piloto) | REAL | `docs/evidencias/ejecucion_real/01…05` |
| Dashboards de Windows Forms (institucional, estadísticas) | SIN VALIDAR en esta sesión | existen (`FormDashboardInstitucional`, `FormEstadisticas`); la captura con administrador ficticio se canceló porque se negó el acceso a la ventana del arnés |
| «Mi actividad» en Telegram | SIN VALIDAR en esta sesión | existe con datos solo personales; cubierto por pruebas unitarias del repositorio |
| Power BI | PENDIENTE | PBIP en V2 sobre las mismas vistas `EA_R_*`; alias y TLS correctos (ORA-01017), falta la credencial del propietario |
| SeaChat | PENDIENTE | no hay ninguna referencia ni credencial en el repositorio; no se integró |
| Puntos especiales en Valledupar | PENDIENTE (parcial en V2) | V2 tiene EcoCómputo y Punto Azul verificados; P3 orienta de forma genérica |
| Escalabilidad de canecas | SIMULADOR / diseño | Oracle admite varias canecas por estación; verde y negra solo lógicas; ninguna orden a hardware inexistente |
| ESP32 y servos | PENDIENTE (fuera de esta fase) | sin puerto COM; guía `docs/hardware/GUIA_ARDUINO_PRINCIPIANTES.md` |
| Disponibilidad 24/7 | PENDIENTE | todo corre en este PC; Funnel depende de que esté encendido |

## 3. Cambios de esta misión

Ver `PROGRESO.md` y el PR #11. Principales: caneca piloto del QR oficial (script idempotente), modo software, lanzador
`Iniciar-EcoAccess-Nube.ps1`, publicación HTTPS sin administrador, pruebas de 30 usuarios con verificación directa en
Oracle, corrección del intérprete de latas medida en fotos nuevas, evaluación que ya no reutiliza resultados de otra
versión y guía de ESP32 para principiantes.

## 4. Despliegue

Oracle Autonomous Database está en la nube. El bot, el registro, el modelo visual y la aplicación corren en el PC del
propietario. P3 es .NET Framework 4.8: no corre en un contenedor Linux; un servidor permanente requiere Windows (de pago) o
portar el bot a .NET moderno (V2 ya compila en net10) y usar una VM Always Free de Oracle. No se contrató ni desplegó nada.

## 5. Intervenciones humanas pendientes, en orden

1. Prueba desde el iPhone con datos móviles: escanear el QR, `/vincular` → crear cuenta, volver, `/start`, enviar una foto.
2. Escribir la credencial de Oracle en Power BI (Transformar datos → Configuración de origen de datos → `ecoaccess_tp` →
   Editar permisos) y actualizar.
3. Permitir la captura de los dashboards de Windows Forms (con el arnés de administrador ficticio o iniciando sesión).
4. Decidir el alojamiento permanente del bot.
5. Fotos de residuos reales (productos colombianos, sanitarios, RAEE) para la siguiente evaluación.
6. Fase de hardware según la guía de ESP32.
