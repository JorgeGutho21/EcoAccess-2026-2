# Documento final de entrega

`INFORME_FINAL_BASE_DE_DATOS_ECOACCESS.docx` y `.pdf` son el informe universitario oficial de la asignatura Base de Datos (profesor Amílkar). La portada lleva a los tres integrantes del proyecto.

## Cómo está organizado

- **Tabla inicial:** dice en qué capítulo y página está cada uno de los cinco puntos del Aula Web. Las páginas las calcula Word con campos de referencia.
- **Parte I (los cinco puntos del Aula Web, en su orden):**
  1. Introducción.
  2. Objetivos.
  3. Modelo entidad-relación (con el MEER como complemento).
  4. Modelo relacional (con la normalización).
  5. Diccionario de datos.
- **Parte II (complementaria):** requisitos, roles, arquitectura, antecedentes, restricciones, PL/SQL, disparadores, vistas y seguridad, conexiones, datos sintéticos, pruebas, Power BI, documentos complementarios, relación con Programación III, observaciones técnicas, limitaciones, conclusiones y referencias.
- **Anexos:**
  - **A:** diccionario completo; las 19 tablas explicadas y los 173 atributos con su significado ampliado.
  - **A.1:** interpretación de las 38 relaciones.
  - **A.2:** los cuatro disparadores explicados por bloques.
  - **B:** código de los disparadores.
  - **C:** salida real de las 153 pruebas.
  - **D:** consultas de exposición.
  - **E:** matriz de trazabilidad.

## Por qué esta es la versión definitiva

Se compararon dos versiones del informe:

- **Informe A original (97 páginas):** tenía el diccionario como una tabla horizontal con significados de una línea.
- **Informe A ampliado (226 páginas):** conserva todo el contenido del original y reemplaza el Anexo A por las 19 tablas explicadas y los 173 atributos con explicaciones de 90 a 165 palabras. Agrega la interpretación de las 38 FK y de los cuatro disparadores.

La versión definitiva parte del ampliado. Le agrega un capítulo de observaciones técnicas y la tabla de documentos según la estructura nueva. Las dos versiones anteriores no se incluyen en la entrega para no repetir documentos; siguen en el respaldo local verificado.

## Cómo se regenera

```
pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\generar_todos.ps1 -Solo INFORME
```

La fuente es `18_ENTREGA_Y_RESPALDOS/fuentes_documentos/fuente_informe.html`. El generador se detiene si el diccionario no coincide con Oracle.
