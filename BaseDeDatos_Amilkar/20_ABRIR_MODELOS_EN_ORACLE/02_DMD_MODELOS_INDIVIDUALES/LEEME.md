# 02 · Diseños DMD independientes por módulo

Cinco diseños **reales** de Data Modeler, uno por módulo. Cada uno se creó en SQL Developer 26.2 (Data Modeler 24.3.1 integrado): **Importar → Archivo DDL** con el DDL del módulo (`../04_DDL_POR_MODULOS`) en un diseño nuevo → **Fusionar** → **Guardar como**. Después se cerró y se **reabrió** cada uno para comprobarlo. No son copias renombradas del modelo completo ni de un PDF.

Cada `.dmd` va **junto con su carpeta** del mismo nombre; no los separes.

| Módulo | Subvista equivalente | Diseño | Tablas | Columnas | FK |
|---|---|---|---|---|---|
| [M1](M1_CATALOGOS/LEEME.md) | M1 Catálogos base | `M1_CATALOGOS/EcoAccess_M1.dmd` | 7 | 81 | 10 |
| [M2](M2_USUARIOS/LEEME.md) | M2 Usuarios e identidad | `M2_USUARIOS/EcoAccess_M2.dmd` | 5 | 26 | 4 |
| [M3](M3_INFRAESTRUCTURA/LEEME.md) | M3 Infraestructura y QR | `M3_INFRAESTRUCTURA/EcoAccess_M3.dmd` | 4 | 23 | 3 |
| [M4](M4_OPERACION/LEEME.md) | M4 Operación del sistema | `M4_OPERACION/EcoAccess_M4.dmd` | 10 | 106 | 20 |
| [M5](M5_ECOPUNTOS_AUDITORIA/LEEME.md) | M5 EcoPuntos y auditoría | `M5_ECOPUNTOS_AUDITORIA/EcoAccess_M5.dmd` | 8 | 86 | 10 |
