# EcoAccess — Entrega de Base de Datos (profesor Amílkar)

**Universidad Popular del Cesar · Asignatura: Base de Datos · Docente: profesor Amílkar**

EcoAccess — Sistema Inteligente de Gestión y Clasificación de Residuos.

Integrantes: Jorge Alfonso Gutiérrez Thomas, Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino.

Esta carpeta contiene **solo la entrega académica de Base de Datos**. La aplicación de Programación III (carpetas `src/`, `database/`, `hardware/`, `tests/` del repositorio) es independiente y esta carpeta no la modifica.

## Empezar

- **Leer primero:** [`00_EMPEZAR_AQUI/LEEME_PRIMERO.md`](00_EMPEZAR_AQUI/LEEME_PRIMERO.md).
- **Documento que se entrega:** [`01_DOCUMENTO_FINAL/INFORME_FINAL_BASE_DE_DATOS_ECOACCESS.pdf`](01_DOCUMENTO_FINAL/INFORME_FINAL_BASE_DE_DATOS_ECOACCESS.pdf).
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
