# Consolidación de carpetas locales (15/11/2026)

Carpeta de la asignatura en el computador: `OneDrive - unicesar.edu.co\Documentos\01_UNIVERSIDAD\2026-2\Base de Datos-SS463-`.

Estado de OneDrive antes de comparar: los archivos de las tres carpetas de EcoAccess estaban disponibles localmente, con 0 archivos solo en la nube.

## Inventario y decisión

| Carpeta o archivo | Archivos | Qué tenía | Decisión |
|---|---|---|---|
| `BaseDeDatos_Amilkar` | 683 antes de este trabajo | La entrega organizada (00 a 18), igual a GitHub | **Carpeta académica activa y definitiva.** Aquí se agregaron `19_MODELOS_VISUALES` y el informe APA 7. |
| `EcoAccess_Amilkar_Exposoftware\BaseDeDatos_Amilkar` | 423 | Extracción parcial del ZIP de entrega del 14/11/2026: 415 archivos idénticos (SHA-256) a la carpeta activa y 8 distintos. Los 8 son el informe `.docx` guardado de nuevo en Word, con el **texto idéntico**, y 7 archivos `*.local` de sesión de Data Modeler. | Sin archivos únicos. Se retiró a `RESPALDOS_LOCALES/CARPETAS_RETIRADAS_20261115/`. |
| `EcoAccessBD_Exposoftware` | 987 | La estructura anterior a la reorganización del 14/11/2026 | Su respaldo completo (`RESPALDOS_LOCALES/RESPALDO_EcoAccessBD_Exposoftware_COMPLETO_20261114_0657.zip`) se volvió a comprobar: 987 de 987 archivos con el mismo SHA-256. Los 420 archivos que no pasaron a la entrega están listados en `ARCHIVOS_EXCLUIDOS_DE_LA_ENTREGA.csv`. Se retiró a `RESPALDOS_LOCALES/CARPETAS_RETIRADAS_20261115/`. |
| `BaseDeDatos_Amilkar_EcoAccess.zip` | 1 | El ZIP de entrega del 14/11/2026 con otro nombre; mismo SHA-256 (`11C0BDFE…`) | Lo reemplaza el ZIP nuevo. Se conservó en `RESPALDOS_LOCALES/CARPETAS_RETIRADAS_20261115/`. |
| `INFORME_FINAL_BASE_DE_DATOS_ECOACCESS.docx` y `.pdf` | 2 | El informe ampliado con su nombre anterior | Ahora es `02_DOCUMENTO_DETALLADO_ECOACCESS`. Copia de la versión anterior en `RESPALDOS_LOCALES/VERSIONES_ANTERIORES_20261115/`. |

Ninguna carpeta se borró. Se movieron dentro de `RESPALDOS_LOCALES`, y después de moverlas se comprobó que el SHA-256 de cada archivo era el mismo:

| Elemento | Archivos antes | Archivos después | Distintos |
|---|---|---|---|
| `EcoAccess_Amilkar_Exposoftware` | 423 | 423 | 0 |
| `EcoAccessBD_Exposoftware` | 987 | 987 | 0 |
| `BaseDeDatos_Amilkar_EcoAccess.zip` | 1 | 1 | 0 |

Para recuperar cualquiera de ellos basta con moverlo de vuelta desde `RESPALDOS_LOCALES/CARPETAS_RETIRADAS_20261115/`.

## Carpetas que no se tocaron

`1er_Corte`, `2do_Corte`, `3er_Corte`, `Descarga_BD` y `Extra_Asignatura_Archivos` conservan su fecha de modificación anterior: 04/11/2026 y 10/11/2026. Tampoco se tocaron Programación III, Oracle Cloud ni otras asignaturas.

`RESPALDOS_LOCALES` queda fuera de la entrega pública: no está en GitHub ni en el ZIP.
