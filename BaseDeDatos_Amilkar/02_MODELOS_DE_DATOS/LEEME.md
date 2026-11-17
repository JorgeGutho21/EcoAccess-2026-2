# Modelos de datos

Esta carpeta guarda los **originales**. Para abrirlos con un doble clic, imprimirlos en PDF horizontal o recorrerlos en una galería sin Internet, usa `19_MODELOS_VISUALES/00_ABRIR_DIAGRAMAS.html`. Esa carpeta tiene copias idénticas de estos archivos, con su SHA-256 registrado, el atlas `ATLAS_VISUAL_ECOACCESS.pdf` y las guías con capturas reales para abrir el DMD y el DDL.

| Carpeta | Contenido | Formatos |
|---|---|---|
| `01_MER` | MER conceptual en notación de Chen: 15 entidades y 19 relaciones. | PDF, PNG, SVG |
| `02_MEER` | MEER: las tres especializaciones (roles de la persona, evidencia del depósito, crédito o reverso) y cómo se implementan. | PDF, PNG, SVG |
| `03_MODELO_RELACIONAL_GENERAL` | Modelo relacional completo ordenado por módulos: 19 tablas y 38 FK. | PDF, PNG, SVG |
| `04_MODELO_RELACIONAL_POR_MODULOS` | Un diagrama por módulo: M1 Catálogos, M2 Usuarios, M3 Infraestructura, M4 Operación y M5 EcoPuntos y auditoría. | PDF, PNG, SVG |
| `05_DATA_MODELER_EDITABLE` | El proyecto auténtico de Oracle SQL Developer Data Modeler: `EcoAccess_BD.dmd` y su carpeta `EcoAccess_BD/` (las dos van siempre juntas). Incluye el modelo lógico, el relacional `Relational_1`, cinco subvistas por módulo, las 9 vistas y las exportaciones hechas desde el programa. | DMD y XML, más PDF y PNG en `EXPORTACIONES_DMD` |
| `06_FLUJO_DE_DATOS` | El recorrido del dato en 11 pasos y el documento de interpretación del modelo. | PDF, PNG, SVG, DOCX |
| `07_EVIDENCIAS` | Capturas de Data Modeler (importación, modelo reabierto, PK, FK y subvistas) y la salida de `verificar_dmd.ps1`. | JPG, TXT |
| `08_HERRAMIENTAS_DE_GENERACION` | Scripts que generan los diagramas desde los metadatos reales de Oracle, ordenan el `.dmd` y lo verifican. | PS1 |

## Cómo abrir el DMD

1. Abre SQL Developer.
2. Menú **Archivo → Data Modeler → Abrir**.
3. Elige `02_MODELOS_DE_DATOS/05_DATA_MODELER_EDITABLE/EcoAccess_BD.dmd`.
4. Si pregunta qué modelos abrir, deja marcado `Relational_1` y acepta.
5. En el navegador, **Modelos relacionales → Relational_1 → Subvistas** muestra M1 a M5.

Para modificar un diagrama sin dañar el modelo, cambia solo la disposición (mover cajas o líneas) y guarda con **Archivo → Data Modeler → Guardar**. No borres tablas ni columnas en Data Modeler: el modelo debe seguir coincidiendo con Oracle. Después comprueba con:

```
pwsh -File 02_MODELOS_DE_DATOS\08_HERRAMIENTAS_DE_GENERACION\verificar_dmd.ps1 -Dmd 02_MODELOS_DE_DATOS\05_DATA_MODELER_EDITABLE\EcoAccess_BD.dmd
```

Debe informar 19 tablas, 173 columnas y 38 FK. La guía completa está en `11_GUIAS_PASO_A_PASO/GUIA_DATA_MODELER.pdf`.

## Por qué los diagramas coinciden con Oracle

Los diagramas en imagen se generan con los scripts de `08_HERRAMIENTAS_DE_GENERACION` a partir de `03_DICCIONARIO_DE_DATOS/metadatos.txt`, que es la extracción real de `USER_TAB_COLUMNS` y `USER_CONSTRAINTS`. El `.dmd` se creó importando el DDL que Oracle extrajo de sí mismo (`04_SCRIPTS_ORACLE/DDL_FINAL_EXTRAIDO.sql`).
