# Matriz de trazabilidad (K)

`K_MATRIZ_TRAZABILIDAD_FINAL.xlsx` relaciona cada exigencia del profesor con el archivo que la cumple, el código, la prueba y su estado.

| Hoja | Qué contiene |
|---|---|
| Resumen | Totales por hoja y estado (VERIFICADO, PARCIAL, PENDIENTE). Usa fórmulas CONTAR.SI, así que se recalcula si se cambia un estado. |
| Aula Web 5 puntos | Los cinco puntos del avance, con el capítulo y la página real del informe final, leída del `.docx`. |
| Requisitos RF RNF | Requisitos funcionales y no funcionales → tablas y atributos → objeto PL/SQL → trigger → consulta → evidencia. |
| Contenidos y entregables | Temas de clase y entregables → documento → evidencia → estado. |
| Tareas del profesor | Cada tarea → documento → título y página → script SQL → carpeta → prueba → estado. |
| Documentos | Cada documento Word y PDF, con su número de páginas según Word y su fecha de generación. |
| Cómo leer | Qué significa cada columna de cada hoja. |

También se exportan las hojas como CSV (`MATRIZ_K1` … `MATRIZ_K5`). `MATRIZ_TRAZABILIDAD.md` es la fuente de los requisitos.

## Cómo se regenera

```
pwsh -File 15_MATRIZ_DE_TRAZABILIDAD\generar_matriz_final.ps1
```

Requiere Excel y Word: lee las páginas reales de los documentos.
