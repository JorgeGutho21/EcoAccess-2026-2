# Módulo M3 · M3 Infraestructura y QR

| Dato | Valor |
|---|---|
| Subvista equivalente en el modelo completo | **M3 Infraestructura y QR** (`../../01_DMD_MODELO_COMPLETO/EcoAccess_BD.dmd` → Relational_1 → Subvistas) |
| Diseño independiente | [`EcoAccess_M3.dmd`](EcoAccess_M3.dmd) + carpeta `EcoAccess_M3/` (van juntos) |
| Tablas del módulo | PUNTO_INSTALACION, CONTENEDOR, CONTENEDOR_MATERIAL |
| Tablas de contexto (de otros módulos, para que las FK tengan destino) | MATERIAL |
| Total en el diseño | 4 tablas, 23 columnas, 3 FK |
| DDL de origen | [`../../04_DDL_POR_MODULOS/M3_INFRAESTRUCTURA/DDL_M3_INFRAESTRUCTURA.sql`](../../04_DDL_POR_MODULOS/M3_INFRAESTRUCTURA/DDL_M3_INFRAESTRUCTURA.sql) (solo para importar; no ejecutar sobre ECOACCESS_BD) |
| PDF y PNG de la subvista | [`DMD_M3_infraestructura_qr.pdf`](../../../02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/EXPORTACIONES_DMD/DMD_M3_infraestructura_qr.pdf) · [`DMD_M3_infraestructura_qr.png`](../../../02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/EXPORTACIONES_DMD/DMD_M3_infraestructura_qr.png) |

## Cómo abrirlo

SQL Developer → **Archivo → Data Modeler → Abrir** → `EcoAccess_M3.dmd` → **Abrir**. Para ver tablas y FK: **Ver → Data Modeler → Explorador** → Relational_1. Guía completa: [`05_GUIA_SQL_DEVELOPER`](../../05_GUIA_SQL_DEVELOPER/GUIA_ABRIR_MODELOS.md).

La distribución del diagrama es la automática de la importación; la subvista del modelo completo tiene la distribución acomodada por el equipo.

## Capturas reales

| Subvista en el modelo completo | Diseño independiente reabierto |
|---|---|
| ![subvista](../../08_CAPTURAS_PASO_A_PASO/A_ABRIR_DMD_Y_SUBVISTAS/08_subvista_M3_infraestructura_qr.jpg) | ![reabierto](../../08_CAPTURAS_PASO_A_PASO/C_REABRIR_DMD_POR_MODULO/03_EcoAccess_M3_reabierto_4_tablas_3_fk.jpg) |