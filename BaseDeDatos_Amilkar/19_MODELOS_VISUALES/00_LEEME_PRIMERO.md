# Modelos visuales de EcoAccess — léeme primero

Esta carpeta reúne todos los diagramas de la base de datos de EcoAccess, listos para abrir desde Windows, imprimir o estudiar. Son **copias** de los originales de `02_MODELOS_DE_DATOS` sin ningún cambio. Su origen y su SHA-256 están en `09_GUIAS_Y_VERIFICACIONES/ORIGENES_Y_HASHES.csv`.

Esquema `ECOACCESS_BD` en Oracle Database 26ai Free: **19 tablas, 173 columnas y 38 claves foráneas**.

> **QUIERO ABRIR LOS MODELOS EN ORACLE → CARPETA 20:** [`../20_ABRIR_MODELOS_EN_ORACLE/00_LEEME_PRIMERO.md`](../20_ABRIR_MODELOS_EN_ORACLE/00_LEEME_PRIMERO.md). Allí están el DMD completo, cinco DMD por módulo, el DDL por módulo y la guía de importación paso a paso, probados en SQL Developer.
>
> **QUIERO ESTUDIAR Y EJECUTAR SQL/PLSQL/TRIGGERS → CARPETA 21:** [`../21_GUIA_SQL_PLSQL_Y_TRIGGERS/00_LEEME_PRIMERO.md`](../21_GUIA_SQL_PLSQL_Y_TRIGGERS/00_LEEME_PRIMERO.md).

## Para empezar, en cinco pasos

1. **Abre esta carpeta** (`19_MODELOS_VISUALES`).
2. **Haz doble clic en `00_ABRIR_DIAGRAMAS.html`.** Se abre en tu navegador, sin Internet ni programas extra.
3. **Elige un modelo** en el menú de arriba: modelos conceptuales, modelo relacional, módulos relacionales, exportaciones de Data Modeler o flujo de datos. Cada tarjeta tiene su PDF horizontal, su PNG y, cuando existe, su SVG.
4. **Para verlo en Data Modeler**, abre SQL Developer y sigue **Archivo → Data Modeler → Abrir**. La guía con capturas está en `05_DATA_MODELER/04_ABRIR_DMD/GUIA_ABRIR_DMD_EN_SQL_DEVELOPER.pdf`.
5. **Para ver el DDL**, abre `07_DDL_Y_CODIGO/DDL_FINAL_EXTRAIDO.sql` con **Archivo → Abrir** de SQL Developer y **no lo ejecutes**. Guía: `07_DDL_Y_CODIGO/COMO_VER_EL_DDL_Y_GENERAR_UN_MODELO.pdf`.

Todos los diagramas en un solo documento: **`ATLAS_VISUAL_ECOACCESS.pdf`**, con 35 páginas horizontales y una página explicativa antes de cada diagrama. El resumen con miniaturas está en **`00_INDICE_VISUAL.pdf`**.

## Qué hay en cada carpeta

| Carpeta | Contenido | PDF horizontal |
|---|---|---|
| `01_MER_CONCEPTUAL` | MER en notación de Chen: 15 entidades y 19 relaciones | A2 |
| `02_MEER_EXTENDIDO` | MEER: las tres especializaciones | A2 |
| `03_MODELO_RELACIONAL_COMPLETO` | Las 19 tablas y las 38 FK: póster A1 y versión de lectura en 4 hojas A3 | A1 y A3 |
| `04_MODELO_RELACIONAL_POR_MODULOS` | M1 Catálogos, M2 Usuarios, M3 Infraestructura, M4 Operación y M5 EcoPuntos y auditoría, cada uno con todas sus columnas | A2 o A3 |
| `05_DATA_MODELER` | Las 7 exportaciones hechas desde Data Modeler: modelo lógico, modelo relacional general y 5 subvistas. Cada una trae el PDF vectorial original del programa y una versión grande para imprimir. En `04_ABRIR_DMD`, las guías y una copia de consulta del diseño. | A0, A1 o A2 |
| `06_FLUJO_DE_DATOS` | El recorrido del dato en 11 pasos | A2 |
| `07_DDL_Y_CODIGO` | Copia del DDL definitivo y la guía para verlo y para generar un modelo desde él | — |
| `08_CAPTURAS_REALES` | Capturas tomadas el 15/11/2026 en SQL Developer: apertura del DMD, subvistas, PK y FK, DDL e importación de prueba | — |
| `09_GUIAS_Y_VERIFICACIONES` | El generador de esta carpeta, el registro de orígenes y hashes, y la verificación | — |

Las siete exportaciones de Data Modeler **no son siete bases de datos distintas**: son vistas del mismo diseño `EcoAccess_BD.dmd`. Lo mismo pasa con el MER, el MEER y los módulos relacionales, que describen el mismo esquema desde distintos niveles.

## Si algo no funciona

| Lo que pasa | Qué hacer |
|---|---|
| El HTML abre pero no se ven las imágenes | Copiaste solo el HTML. Copia la carpeta `19_MODELOS_VISUALES` completa: los enlaces son relativos. |
| El PDF se ve diminuto | Usa el zoom del lector de PDF. Los diagramas son vectoriales, o imágenes de alta resolución en el caso de Data Modeler, y no pierden nitidez. Para imprimir, usa el tamaño indicado (A0, A1, A2 o A3) o «Ajustar a la página». |
| El DMD no abre | Falta su carpeta `EcoAccess_BD/` al lado del `.dmd`. Ver la sección 10 de la guía de SQL Developer. |
| SQL Developer pide contraseña | Solo para conectarse a Oracle. Ver los diagramas no la necesita. |

## Cómo se regenera

```
pwsh -File 19_MODELOS_VISUALES\09_GUIAS_Y_VERIFICACIONES\generar_biblioteca_visual.ps1
```

El script vuelve a copiar los originales, recalcula los hashes y genera de nuevo los PDF, el atlas, el índice y la galería. Toma los números de tablas, columnas y FK de los metadatos extraídos de Oracle (`03_DICCIONARIO_DE_DATOS/metadatos.txt`) y se detiene si no son 19, 173 y 38.
