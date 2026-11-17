# 20 · Abrir los modelos de EcoAccess en Oracle

**Universidad Popular del Cesar · Base de Datos · Profesor Amílkar**
**Integrantes:** Jorge Alfonso Gutiérrez Thomas, Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino

Esta carpeta reúne **todo lo necesario para abrir el modelo de datos en Oracle SQL Developer** y explica cada clic con capturas reales.

**Modelo:**

- Esquema ECOACCESS_BD: 19 tablas, 173 atributos, 38 FK y 4 disparadores.
- 5 módulos: M1 Catálogos base, M2 Usuarios e identidad, M3 Infraestructura y QR, M4 Operación del sistema y M5 EcoPuntos y auditoría.

**Para abrir un `.dmd` no se necesita contraseña ni conexión a la base.**

## ¿Qué quieres hacer?

| Quiero… | Abre |
|---|---|
| Ver el modelo completo y las subvistas M1–M5 | [`05_GUIA_SQL_DEVELOPER/GUIA_ABRIR_MODELOS.md`](05_GUIA_SQL_DEVELOPER/GUIA_ABRIR_MODELOS.md) (PDF al lado) |
| Entender qué hay dentro de un diseño de Data Modeler | [`06_GUIA_DATA_MODELER/GUIA_ABRIR_MODELOS.md`](06_GUIA_DATA_MODELER/GUIA_ABRIR_MODELOS.md) (PDF al lado) |
| Leer el DDL o importarlo en un diseño nuevo | [`07_GUIA_IMPORTAR_DDL/GUIA_IMPORTACION_PASO_A_PASO.md`](07_GUIA_IMPORTAR_DDL/GUIA_IMPORTACION_PASO_A_PASO.md) (PDF al lado) |
| Abrir un solo módulo como diseño independiente | [`02_DMD_MODELOS_INDIVIDUALES/LEEME.md`](02_DMD_MODELOS_INDIVIDUALES/LEEME.md) |
| Ver todo en un solo PDF | [`00_INDICE_PARA_ABRIR_MODELOS.pdf`](00_INDICE_PARA_ABRIR_MODELOS.pdf) |
| Revisar qué se probó | [`09_VERIFICACIONES/INFORME_DE_PRUEBAS.md`](09_VERIFICACIONES/INFORME_DE_PRUEBAS.md) |

## Contenido

| Carpeta | Qué contiene |
|---|---|
| `01_DMD_MODELO_COMPLETO` | `EcoAccess_BD.dmd` **y su carpeta** `EcoAccess_BD/`: modelo lógico, Relational_1 y subvistas M1–M5. Copia exacta del modelo académico de `02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE`. |
| `02_DMD_MODELOS_INDIVIDUALES` | 5 diseños reales e independientes (`EcoAccess_M1.dmd` … `EcoAccess_M5.dmd`, cada uno con su carpeta), creados importando el DDL de cada módulo. |
| `03_DDL_MODELO_COMPLETO` | `DDL_FINAL_EXTRAIDO.sql`, el DDL completo extraído de la base real (idéntico a `04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql`). |
| `04_DDL_POR_MODULOS` | Un DDL por módulo, **solo para importar en Data Modeler**. Cada uno dice «No ejecutar sobre ECOACCESS_BD». |
| `05_GUIA_SQL_DEVELOPER` | Guía de clics para abrir el modelo y leer PK, FK y cardinalidad. |
| `06_GUIA_DATA_MODELER` | Qué es Data Modeler, subvista o diseño independiente, y propiedades. |
| `07_GUIA_IMPORTAR_DDL` | Procedimiento A (abrir el DDL como texto) y B (importarlo en un diseño nuevo), con los resultados reales. |
| `08_CAPTURAS_PASO_A_PASO` | 36 capturas reales: A (abrir DMD y subvistas), B (importar DDL), C (reabrir los DMD por módulo) y D (abrir el DMD desde un ZIP extraído en otra ubicación). |
| `09_VERIFICACIONES` | Scripts de generación y verificación, e informe de pruebas. |

## Reglas

1. **El `.dmd` y su carpeta van siempre juntos.** Si copias el modelo, copia los dos.
2. Para practicar, usa **Guardar como** en otra carpeta; no guardes encima de `01_DMD_MODELO_COMPLETO`.
3. Los DDL por módulo **no se ejecutan** en ninguna base: se importan en Data Modeler.
4. Todo se hizo dentro de **SQL Developer** (Data Modeler integrado), no en el Data Modeler independiente.
