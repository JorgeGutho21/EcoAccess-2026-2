# Cómo se construyó

`COMO_SE_CONSTRUYO_PASO_A_PASO.pdf` / `.docx` cuenta, en orden cronológico, cómo se construyó la base de datos:

- el modelo heredado de las versiones anteriores y de Programación III;
- las tablas, las restricciones, PL/SQL y los disparadores;
- los modelos, el diccionario, Power BI, C# y las pruebas;
- la entrega.

Explica las decisiones, las correcciones y las dificultades, y cada paso cita su evidencia.

## Registros históricos (`historial/`)

Estos archivos se conservan **sin modificar**, como se escribieron en su momento. Pueden mencionar rutas de la estructura anterior (`EcoAccessBD_Exposoftware`); la correspondencia entre rutas antiguas y nuevas está en `18_ENTREGA_Y_RESPALDOS/CORRESPONDENCIA_RUTAS_ANTIGUAS_NUEVAS.csv`.

| Archivo | Qué registra |
|---|---|
| `DIAGNOSTICO_INICIAL_20261110.md` | El diagnóstico del 10/11/2026: P3 frente al modelo académico y las decisiones de diseño. |
| `INFORME_DE_CIERRE_20261111.md` | El cierre del 11/11/2026 y la ampliación del diccionario del 13–14/11/2026: correcciones, pruebas y pendientes. |
| `LEEME_PRIMERO_20261114.md` | La guía de entrada de la estructura anterior, antes de esta reorganización. |

## La reorganización del 14/11/2026

La entrega se reorganizó en la carpeta `BaseDeDatos_Amilkar`, con la estructura de 19 carpetas descrita en `00_EMPEZAR_AQUI/LEEME_PRIMERO.md`.

- **Cómo se hizo:** `18_ENTREGA_Y_RESPALDOS/herramientas/reorganizar_entrega.ps1` copió cada archivo y reescribió sus rutas internas. No movió ni borró nada.
- **Respaldo previo:** antes de empezar se creó un respaldo completo, verificado archivo por archivo con SHA-256.
- **Registros de la reorganización:** la correspondencia de rutas, la lista de archivos excluidos y el historial de commits están en `18_ENTREGA_Y_RESPALDOS/`.
