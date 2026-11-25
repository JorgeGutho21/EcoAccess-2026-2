# Evidencias · interfaz ciudadana (25 de noviembre de 2026)

| Captura | Qué muestra |
|---|---|
| `escritorio-inicio.webp` | 1366×768, una sola pantalla: pregunta, buscador con micrófono, atajos, ECO y las tres canecas originales. |
| `escritorio-resultado-tapa-abierta.webp` | «voy a desechar una cáscara de banano» → caneca verde centrada, tapa abierta sobre la bisagra trasera, cuenta regresiva. |
| `fisico-dos-canecas-7s.webp` | Modo físico (pasarela + panel simulado v2): «una botella plástica y una papa» → verde y blanca, 7 s, pasos reales hasta «Apertura comandada». |
| `tablet-resultado.webp` | 768×1024: servilleta usada → caneca negra abierta y tarjeta de resultado. |
| `movil-inicio.webp`, `movil-resultado.webp` | 390×844: inicio con las tres canecas visibles y resultado con desplazamiento automático. |
| `movil-residuo-especial.webp` | «una pila» → residuo especial: ninguna caneca se abre y se explica dónde llevarla. |

Las capturas en modo demostración llevan el rótulo «Demostración · sin movimiento físico».

## Resultados de pruebas
- Catálogo y motor: **1 312 casos, 0 fallos** (`node --test web/ciudadano/pruebas/motor.test.mjs`).
- Integración web → pasarela → panel simulado: **13/13** (`node --test web/ciudadano/pruebas/integracion.test.mjs`).
- Pruebas unitarias C# de la solución: **2 440 correctas, 0 fallos, 1 omitida** (incluye 8 nuevas de la pasarela).
- Responsive: sin desplazamiento horizontal en 320×568, 360×800, 390×844, 430×932, 768×1024, 1024×768, 1366×768 y 1920×1080;
  caneca elegida, tarjeta y cuenta regresiva visibles tras identificar en todas.
- Panel real (solo lectura): la pasarela detecta firmware `ecoaccess-calibracion-v2` y modo `grupo`; sin sesión supervisada
  informa «estación en pausa» y no envía órdenes.
