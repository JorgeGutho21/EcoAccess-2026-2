# Power BI

| Archivo o carpeta | Qué es |
|---|---|
| `ARCHIVO_PBIX/EcoAccess_BD.pbix` | **El tablero para abrir.** Tiene 5 páginas, 55 visuales, glosario, botones y filtros. Fue verificado en pantalla, guardado y reabierto, y lee los CSV exportados por la cuenta lectora. |
| `PROYECTO_PBIP/EcoAccess_BD/` | El mismo informe como proyecto de texto (formato PBIR): el modelo `model.bim`, con 6 tablas, 6 relaciones y 15 medidas, y las páginas en JSON. Ya está configurado para leer Oracle directo, con los CSV como respaldo. |
| `DATOS_Y_CONSULTAS/power_query.m` y `power_query_oracle_directo.m` | Las consultas de Power Query: desde CSV y desde las vistas `VW_BI_*` de Oracle. |
| `DATOS_Y_CONSULTAS/medidas.dax` | Las 15 medidas DAX. |
| `DATOS_Y_CONSULTAS/exportar_csv.sql` y `datos_csv/` | La exportación de las 5 vistas `VW_BI_*` con la cuenta lectora; es el origen de respaldo. |
| `verificar_modelo/` | El programa que comprueba la estructura del PBIP; lo usa el paso B02 del instalador. |
| `generar_pbip.ps1`, `convertir_a_pbir.ps1`, `agregar_glosario_inicio.ps1`, `agregar_origen_oracle.ps1` | Los scripts que construyeron el proyecto. |
| `LEEME_POWERBI.md` | Notas técnicas del modelo y de las páginas. |

## Las vistas de Oracle que usa el tablero

Las cinco vistas de solo lectura de `04_SCRIPTS_ORACLE/06_vistas.sql`:

- `VW_BI_CIUDADANOS`
- `VW_BI_DEPOSITOS`
- `VW_BI_CONSULTAS_VISION`
- `VW_BI_ECOPUNTOS`
- `VW_BI_AUDITORIA`

Ninguna muestra nombres, correos ni credenciales: cada ciudadano aparece como un seudónimo, por ejemplo `C-0017`. La cuenta `ECOACCESS_BD_LECTOR` solo puede leer estas vistas.

## Las cinco páginas

1. Inicio, con el glosario.
2. Operación.
3. Ranking y pesos.
4. EcoPuntos y auditoría.
5. Galería de roles.

Todas rotulan los datos como **DATOS SINTÉTICOS**. Los kilos estimados y los medidos se muestran por separado. Las capturas están en `16_EVIDENCIAS_Y_PRUEBAS/capturas_power_bi/`.

## CSV frente a Oracle directo

| Modo | Estado |
|---|---|
| **CSV** (`OrigenDatos = CSV`) | **VERIFICADO.** Es el PBIX entregado; las cifras coinciden con Oracle (187 consultas, 105 depósitos, 76 verificados, 675 EcoPuntos netos). |
| **Oracle directo** (`OrigenDatos = ORACLE`, `ServidorOracle = localhost:1521/FREEPDB1`) | **PARCIAL.** El PBIP ya lee las vistas con `Oracle.Database` y el conector llegó al servidor, pero falta que la persona dueña escriba la contraseña de `ECOACCESS_BD_LECTOR` en Power BI (Transformar datos → Configuración de origen de datos → Editar permisos), pulse Actualizar y guarde el PBIX. Los pasos están en `11_GUIAS_PASO_A_PASO/GUIA_POWER_BI.pdf`. |

**Para abrir el tablero:** doble clic en `ARCHIVO_PBIX/EcoAccess_BD.pbix`. Para ver los botones de navegación dentro de Power BI Desktop, usa Ctrl+clic.
