# 12. Entrega

## Criterios

Cada criterio obligatorio se valoró 1 si está comprobado, 0,5 si lo está parcialmente y 0 si está pendiente.

| # | Criterio | Valor | Evidencia o pendiente |
|---|---|---|---|
| 1 | Solución net48 | 1 | compilación Release sin avisos |
| 2 | Arquitectura por capas | 1 | pruebas de arquitectura |
| 3 | Oracle reproducible | 1 | esquema limpio, repetición sin cambios |
| 4 | Windows Forms funcional | 0,5 | abre y carga datos; sin revisión completa de clics ni escalas |
| 5 | Registro manual | 0,5 | servicios reales; formulario por captura |
| 6 | Telegram operativo | 0 | sin prueba real |
| 7 | Vinculación persistente | 0,5 | Oracle y reinicio simulado; falta teléfono |
| 8 | QR asociado | 1 | pruebas de QR y lectura |
| 9 | Reconocimiento probado | 1 | documento 07: LFM3-VL 140/198 (70,7 %); Qwen 3.8 en Groq sin medir |
| 10 | Clasificación segura | 1 | abstención, admisión y confirmación |
| 11 | Comunicación con ESP32 | 0 | sin hardware |
| 12 | Apertura física | 0 | sin hardware |
| 13 | Temporizador de 20 s físico | 0 | medido solo en simulador |
| 14 | Cierre seguro físico | 0 | sin hardware |
| 15 | Persistencia | 1 | Oracle |
| 16 | Depósitos sin registros ficticios | 1 | `CierreSinSensor` |
| 17 | Ecopuntos consistentes | 1 | idempotencia concurrente |
| 18 | Consultas compartidas | 1 | mismos servicios |
| 19 | Varias canecas | 1 | contenedor en toda la ruta |
| 20 | Pruebas automatizadas | 1 | documento 10 |
| 21 | Otro computador | 0 | pendiente |
| 22 | Documentación | 1 | documentos 00 a 12 |
| 23 | Historial Git auténtico | 1 | GitHub |
| 24 | Pull requests revisados | 0,5 | verificados y fusionados sin revisión de otra persona |
| 25 | Sin errores críticos conocidos | 1 | ninguno en software |

**Cumplimiento: 17 de 25 (68 %).** No es 100 %: quedan requisitos físicos y externos.

**Alcance de software, sin los criterios físicos ni externos 6, 11 a 14 y 21: 17 de 19 (89 %).**

## Pendientes

Necesitan a una persona o hardware:

1. **Telegram real:** `scripts\instalacion\Configurar-Credenciales.ps1 -Telegram` y una prueba desde un teléfono. Detener antes cualquier otro consumidor del bot.
1. **Groq real:** `scripts\instalacion\Configurar-Credenciales.ps1 -Groq`, una inferencia y la comparación. Ver `docs\telegram\INFORME_FINAL_IMPLEMENTACION.md`.
2. **Página HTTPS** de registro para teléfonos externos.
3. **ESP32:** carga, calibración, medición de la fuente, 20 s reales, desconexión y reinicio.
4. **Fotos originales** de la lata de bebida energética y del envase Celema.
5. **Segundo computador:** seguir el documento 11.
6. **Revisión de Heider** de los pull requests y sus propias contribuciones, desde su cuenta.
7. **Escalas de Windows** de 125 % y 150 % y pantalla de 1366×768 real.
8. **Power BI Desktop** para generar el .pbix.