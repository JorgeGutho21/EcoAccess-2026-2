---
archivo: 05_DICCIONARIO_ENTIDADES_Y_CLASES
titulo: Diccionario completo de entidades y clases de EcoAccess P3
subtitulo: Cada entidad con propósito, propiedades, constructores, métodos, validaciones, tablas de Oracle, pruebas y preguntas de sustentación
codigo: 05
---

# Cómo leer este diccionario

Este documento tiene dos partes:

1. **Fichas de entidades** (capa Entidades): la explicación más profunda, con los 20 aspectos que pide la guía de estudio. La numeración de cada ficha sigue esa lista: 1. nombre, 2. ruta, 3. propósito, 4. problema, 5–9. propiedades, constructores, métodos y validaciones, 10. relaciones, 11. principios de POO, 13. tabla de Oracle, 14–16. ejemplos, 17. pruebas, 18–20. pregunta, respuesta y explicación sencilla.
2. **Catálogo de clases** de las demás capas, con cada miembro público.

Las tablas de propiedades, métodos, columnas de Oracle y pruebas **se generaron a partir del código y de Oracle Cloud**
(herramienta `tests/herramientas/EcoAccess.P3.Inventario`), en el commit de la portada. La columna «Qué hace» repite el
comentario XML del propio código; cuando aparece vacía es porque ese miembro no tiene comentario. Las secciones
explicativas (problema, relaciones, ejemplos, preguntas) están escritas a mano y se comprobaron contra el código.

**Resumen del inventario**

{{conteos}}

# Fichas de entidades

{{entidades}}

# Catálogo de clases de la lógica

## Servicios y reglas

{{detalle:Lógica}}

# Catálogo de clases de datos

{{detalle:Datos}}

# Catálogo de clases de compatibilidad

{{detalle:Compatibilidad}}
