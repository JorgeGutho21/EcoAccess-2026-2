# Documento final de entrega

Esta carpeta tiene dos documentos con propósitos distintos. Los dos llevan en la portada a los tres integrantes: Jorge Alfonso Gutiérrez Thomas, Heyder Hedrey Canchila de Ángel y Saray Marcela Cadena Palomino.

| Documento | Para qué sirve |
|---|---|
| `01_INFORME_FINAL_ECOACCESS_APA7.pdf` / `.docx` | **El informe que se entrega al profesor Amílkar.** Breve (40 páginas) y en formato APA 7: Times New Roman 12, interlineado doble, márgenes de 2,54 cm, figuras y tablas numeradas y referencias. |
| `02_DOCUMENTO_DETALLADO_ECOACCESS.pdf` / `.docx` | **La explicación completa**, para estudiar y consultar: las 19 tablas, los 173 atributos, las 38 relaciones, los cuatro disparadores, PL/SQL, Power BI, C#, Programación III, pruebas y anexos. |

## Informe final APA 7

- **Capítulos 1 a 5:** los cinco puntos del avance del Aula Web (18 al 22 de septiembre de 2026), en el orden pedido: introducción, objetivos, modelo entidad-relación, modelo relacional y diccionario de datos.
- **Capítulos 6 a 13:** lo desarrollado hasta noviembre de 2026: arquitectura, SQL y PL/SQL, disparadores, tareas del profesor, integridad y seguridad, pruebas, Power BI y conclusiones.
- **Figuras:** el MER, el MEER, el modelo relacional, los cinco módulos y el flujo, en páginas horizontales y sin deformar. Las versiones grandes, para leer cada atributo, están en `19_MODELOS_VISUALES`.
- **Páginas de los cinco puntos:** las lee del `.docx` la hoja «Aula Web 5 puntos» de la matriz K.

Se regenera con `pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\generar_informe_apa7.ps1`, a partir de la fuente `18_ENTREGA_Y_RESPALDOS/fuentes_documentos/fuente_informe_apa7.txt`.

## Documento técnico detallado

Es el informe ampliado que antes se llamaba `INFORME_FINAL_BASE_DE_DATOS_ECOACCESS`. Su contenido se conserva completo; solo cambiaron el nombre, la portada y el párrafo que explica su relación con el informe APA 7.

- **Parte I:** los cinco puntos del Aula Web, desarrollados por completo.
- **Parte II:** requisitos, roles, arquitectura, restricciones, PL/SQL, disparadores, seguridad, conexiones, datos sintéticos, pruebas, Power BI, Programación III, observaciones técnicas, revisión técnica, limitaciones, conclusiones y referencias.
- **Anexos:**
  - **A:** diccionario completo, con las 19 tablas y los 173 atributos.
  - **A.1:** las 38 relaciones.
  - **A.2:** los cuatro disparadores explicados.
  - **B:** el código de los disparadores.
  - **C:** la salida de las 163 pruebas.
  - **D:** las consultas de exposición.
  - **E:** la matriz de trazabilidad.

Se regenera con `pwsh -File 18_ENTREGA_Y_RESPALDOS\herramientas\generar_todos.ps1 -Solo INFORME`, a partir de la fuente `18_ENTREGA_Y_RESPALDOS/fuentes_documentos/fuente_informe.html`.

## Versiones anteriores

La versión anterior, con el nombre `INFORME_FINAL_BASE_DE_DATOS_ECOACCESS` y sin cambios, está fuera de la entrega, en `RESPALDOS_LOCALES/VERSIONES_ANTERIORES_20261115/` del computador.
