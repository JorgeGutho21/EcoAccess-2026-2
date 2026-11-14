# Diccionario de datos (19 tablas, 173 atributos, 38 relaciones)

## Documentos para leer

| Archivo | Qué contiene |
|---|---|
| `GUIA_COMPLETA_173_ATRIBUTOS_Y_38_RELACIONES.pdf` / `.docx` | La guía principal de estudio, en diez partes. Para cada tabla: propósito, relaciones y qué pasaría sin ella. Para cada atributo: qué significa, por qué existe, qué guarda, qué regla lo protege, quién lo modifica, quién lo consulta y un ejemplo. Incluye las 38 FK, las 19 PK, las 9 vistas, los 4 disparadores, el recorrido de un residuo, 12 consultas SQL ejecutadas en Oracle y las preguntas de sustentación. |
| `DICCIONARIO_FICHAS_DE_24_CAMPOS.pdf` / `.docx` | Una ficha de 24 campos por atributo: ejemplos válidos e inválidos, problema que evita, objetos que lo usan, uso en C#, Telegram y Power BI, y un SELECT de comprobación. |
| `VERIFICACION_DICCIONARIO_173_ATRIBUTOS.md` | La comprobación atributo por atributo contra Oracle: matriz por tabla, los 173 atributos con sus palabras y oraciones, las 38 FK y las observaciones técnicas encontradas. |
| `DICCIONARIO_DATOS.md`, `DICCIONARIO_COMPLETO.md`, `diccionario.csv`, `diccionario_completo.csv` | El diccionario en texto y CSV, para buscar o abrir en Excel. |
| `MATRIZ_ATRIBUTOS_vs_ORACLE.csv` | Correspondencia de cada atributo documentado con su tipo, nulabilidad, PK y FK leídos de Oracle. |

## Restricciones

Cada ficha indica las restricciones de su columna (PK, FK, UNIQUE, CHECK e IDENTITY) con el nombre exacto que tienen en Oracle. El texto de cada CHECK está al final de `DICCIONARIO_DATOS.md`. Los 173 SELECT de `comprobar_173_select.sql` consultan cada columna real; su salida está en `16_EVIDENCIAS_Y_PRUEBAS/salidas/diccionario_173_select.txt`.

## Cómo se comprueba

```
pwsh -File 03_DICCIONARIO_DE_DATOS\generar_diccionario_ampliado.ps1
```

Este script cruza `metadatos.txt` (columnas reales de Oracle), `ampliado/restricciones_oracle.txt` (FK, PK, UNIQUE y CHECK leídos de `USER_CONSTRAINTS`) y las explicaciones de `ampliado/M*.txt`. Se detiene si falta o sobra un atributo, una tabla o una FK, y si alguna explicación queda por debajo de 90 palabras o 5 oraciones.

| Archivo de origen | Qué guarda |
|---|---|
| `ampliado/M1_M2_*.txt` … `M5_*.txt` | Explicación de cada tabla (`T|`) y de cada atributo (`C|`). |
| `ampliado/relaciones.txt` | Por qué existe cada FK, con un ejemplo. |
| `ampliado/fuentes/` | Fragmentos de la guía: triggers, ejemplos SQL, preguntas y observaciones. |
| `fuente_diccionario.txt` | Los campos breves del diccionario: ejemplo, quién lo registra, dónde se usa, si es NULL y por qué existe. |
| `extraer_metadatos.sql` | Vuelve a extraer los metadatos desde Oracle. |
