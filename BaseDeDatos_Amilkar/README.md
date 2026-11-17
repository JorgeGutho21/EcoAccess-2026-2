# EcoAccess — Entrega de Base de Datos (profesor Amílkar)

**Universidad Popular del Cesar · Asignatura: Base de Datos · Docente: profesor Amílkar**

EcoAccess — Sistema Inteligente de Gestión y Clasificación de Residuos.

Integrantes: Jorge Alfonso Gutiérrez Thomas, Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino.

Esta carpeta contiene la **entrega académica de Base de Datos** del mismo sistema EcoAccess P3 cuya aplicación (Programación III) está en `src/`, `database/`, `hardware/` y `tests/` de este repositorio. Es un solo proyecto con dos componentes académicos:

- **Base oficial (producción):** Oracle Cloud, alias `ecoaccess_tp`, esquema `ECOACCESS_P3_APP`. La usan la aplicación de escritorio, el bot de Telegram, Power BI de la aplicación y SQL Developer.
- **Esquema académico de esta carpeta:** `ECOACCESS_BD` en `localhost:1521/FREEPDB1`, con datos sintéticos. Comparte las tablas del núcleo (`USUARIO`, `MATERIAL`, `CONTENEDOR`, `DEPOSITO`, `ORDEN_APERTURA`…) y las extiende con `AUDITORIA_DEPOSITO`, `HISTORIAL_ESTADO_CONTENEDOR`, `PESO_REFERENCIAL`, los paquetes `PKG_DEPOSITO`, `PKG_ECOPUNTOS`, `PKG_ESTADISTICAS`, `PKG_CONTEXTO_APP` y cuatro triggers.

La correspondencia tabla por tabla y columna por columna, y el plan para llevar estos objetos a la base oficial, están en [`../docs/UNIFICACION_ECOACCESS_P3.md`](../docs/UNIFICACION_ECOACCESS_P3.md). Los scripts de esta carpeta no modifican la base oficial.

## Empezar

**¿Es su primera vez en esta entrega?** Empiece por la [guía de descarga y primer uso](00_EMPEZAR_AQUI/GUIA_DESCARGAS_Y_PRIMER_USO.md). Explica cómo descargar el informe Word/PDF, obtener el paquete completo desde la rama correcta, leer los diagramas, conectarse a SQL Developer y localizar las tareas del profesor. **El repositorio es privado: se necesita una cuenta autorizada.**

**¿Busca los ejercicios de disparadores?** Abra la [tarea de cuatro puntos explicada paso a paso](06_TAREAS_PROFESOR_AMILKAR/TAREA_DISPARADORES_CUATRO_PUNTOS/GUIA_PRACTICA_TRIGGERS_Y_ENTREGA.md): dos triggers de EcoAccess, secuencia y PK, dos de tema libre, precio final, :OLD/:NEW, pruebas y archivos exactos.

### Accesos directos

- **QUIERO ABRIR LOS MODELOS EN ORACLE → CARPETA 20:** [`20_ABRIR_MODELOS_EN_ORACLE/00_LEEME_PRIMERO.md`](20_ABRIR_MODELOS_EN_ORACLE/00_LEEME_PRIMERO.md) · [índice PDF](20_ABRIR_MODELOS_EN_ORACLE/00_INDICE_PARA_ABRIR_MODELOS.pdf). Abre el DMD completo, las subvistas M1–M5, los DMD por módulo y el DDL, con capturas reales de cada clic.
- **QUIERO ESTUDIAR Y EJECUTAR SQL/PLSQL/TRIGGERS → CARPETA 21:** [`21_GUIA_SQL_PLSQL_Y_TRIGGERS/00_LEEME_PRIMERO.md`](21_GUIA_SQL_PLSQL_Y_TRIGGERS/00_LEEME_PRIMERO.md) · [manual PDF](21_GUIA_SQL_PLSQL_Y_TRIGGERS/00_MANUAL_COMPLETO_SQL_PLSQL.pdf). Conexión, SELECT, JOIN, PL/SQL, cursores, procedimientos, funciones, paquetes, los 4 triggers y qué residuos deposita cada usuario.
- [Descargar informe final APA 7 PDF](01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7.pdf) · [versión Word](01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7.docx) · [documento detallado PDF](01_DOCUMENTO_FINAL/02_DOCUMENTO_DETALLADO_ECOACCESS.pdf).
- [Abrir la galería de diagramas](19_MODELOS_VISUALES/00_ABRIR_DIAGRAMAS.html) · [atlas visual PDF](19_MODELOS_VISUALES/ATLAS_VISUAL_ECOACCESS.pdf).
- [Leer diccionario de 173 atributos PDF](03_DICCIONARIO_DE_DATOS/GUIA_COMPLETA_173_ATRIBUTOS_Y_38_RELACIONES.pdf).
- [Ver modelo relacional general PDF](02_MODELOS_DE_DATOS/03_MODELO_RELACIONAL_GENERAL/MR_00_general_por_modulos.pdf).
- [Abrir guía de instalación y pruebas PDF](11_GUIAS_PASO_A_PASO/PASO_A_PASO_INSTALAR_PROBAR_Y_ENTREGAR.pdf).

### Rutas y estado

- **Leer primero:** [`00_EMPEZAR_AQUI/LEEME_PRIMERO.md`](00_EMPEZAR_AQUI/LEEME_PRIMERO.md).
- **Documento que se entrega:** [`01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7.pdf`](01_DOCUMENTO_FINAL/01_INFORME_FINAL_ECOACCESS_APA7.pdf), informe breve en formato APA 7.
- **Explicación completa:** [`01_DOCUMENTO_FINAL/02_DOCUMENTO_DETALLADO_ECOACCESS.pdf`](01_DOCUMENTO_FINAL/02_DOCUMENTO_DETALLADO_ECOACCESS.pdf), con las 19 tablas, los 173 atributos, las 38 relaciones, los triggers, el código, las pruebas y los anexos.
- **Todos los diagramas:** [`19_MODELOS_VISUALES/00_ABRIR_DIAGRAMAS.html`](19_MODELOS_VISUALES/00_ABRIR_DIAGRAMAS.html) (galería sin Internet), el atlas en PDF y las guías para abrir el DMD y el DDL.
- **Estado verificado y pendientes:** [`00_EMPEZAR_AQUI/ESTADO_DEL_PROYECTO.md`](00_EMPEZAR_AQUI/ESTADO_DEL_PROYECTO.md).

## Qué contiene

- Un esquema Oracle de **19 tablas, 173 columnas y 38 claves foráneas** en cinco módulos, con 9 vistas, 4 paquetes y 4 disparadores.
- MER, MEER, modelo relacional y el proyecto editable de Data Modeler.
- El diccionario de los 173 atributos, verificado contra Oracle.
- Las prácticas de SQL y PL/SQL de clase y un banco de 309 ejercicios con su salida real.
- Un tablero de Power BI y una aplicación C# con CRUD sin eliminación.
- Las guías de estudio y de sustentación.

## Probar

Requiere Oracle Database Free en `localhost:1521/FREEPDB1` y PowerShell 7:

```
pwsh -File .\INSTALAR_Y_PROBAR.ps1
```

Instala lo que falte y ejecuta todas las comprobaciones sin borrar nada. Las contraseñas se piden por teclado y se guardan solo en el PC, en `_LOCAL_NO_PUBLICAR/`, que no se publica.

Todos los datos son sintéticos.
