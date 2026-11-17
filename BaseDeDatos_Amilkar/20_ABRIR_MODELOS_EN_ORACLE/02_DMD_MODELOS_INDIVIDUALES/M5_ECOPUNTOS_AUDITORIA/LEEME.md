# Módulo M5 · M5 EcoPuntos y auditoría

| Dato | Valor |
|---|---|
| Subvista equivalente en el modelo completo | **M5 EcoPuntos y auditoría** (`../../01_DMD_MODELO_COMPLETO/EcoAccess_BD.dmd` → Relational_1 → Subvistas) |
| Diseño independiente | [`EcoAccess_M5.dmd`](EcoAccess_M5.dmd) + carpeta `EcoAccess_M5/` (van juntos) |
| Tablas del módulo | REGLA_ECOPUNTOS, MOVIMIENTO_ECOPUNTOS, AUDITORIA_DEPOSITO, HISTORIAL_ESTADO_CONTENEDOR |
| Tablas de contexto (de otros módulos, para que las FK tengan destino) | USUARIO, MATERIAL, CONTENEDOR, DEPOSITO |
| Total en el diseño | 8 tablas, 86 columnas, 10 FK |
| DDL de origen | [`../../04_DDL_POR_MODULOS/M5_ECOPUNTOS_AUDITORIA/DDL_M5_ECOPUNTOS_AUDITORIA.sql`](../../04_DDL_POR_MODULOS/M5_ECOPUNTOS_AUDITORIA/DDL_M5_ECOPUNTOS_AUDITORIA.sql) (solo para importar; no ejecutar sobre ECOACCESS_BD) |
| PDF y PNG de la subvista | [`DMD_M5_ecopuntos_auditoria.pdf`](../../../02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/EXPORTACIONES_DMD/DMD_M5_ecopuntos_auditoria.pdf) · [`DMD_M5_ecopuntos_auditoria.png`](../../../02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/EXPORTACIONES_DMD/DMD_M5_ecopuntos_auditoria.png) |

## Cómo abrirlo

SQL Developer → **Archivo → Data Modeler → Abrir** → `EcoAccess_M5.dmd` → **Abrir**. Para ver tablas y FK: **Ver → Data Modeler → Explorador** → Relational_1. Guía completa: [`05_GUIA_SQL_DEVELOPER`](../../05_GUIA_SQL_DEVELOPER/GUIA_ABRIR_MODELOS.md).

La distribución del diagrama es la automática de la importación; la subvista del modelo completo tiene la distribución acomodada por el equipo.

## Capturas reales

| Subvista en el modelo completo | Diseño independiente reabierto |
|---|---|
| ![subvista](../../08_CAPTURAS_PASO_A_PASO/A_ABRIR_DMD_Y_SUBVISTAS/10_subvista_M5_ecopuntos_auditoria.jpg) | ![reabierto](../../08_CAPTURAS_PASO_A_PASO/C_REABRIR_DMD_POR_MODULO/05_EcoAccess_M5_reabierto_8_tablas_10_fk.jpg) |