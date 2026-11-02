# 04. Persistencia Oracle

Proyecto `EcoAccess.P3.Datos`: ADO.NET con `Oracle.ManagedDataAccess`, SQL parametrizado y transacciones explícitas. Modelo y diccionario en `database/01_modelo/`.

## Instalación reproducible

`scripts/instalacion/Preparar-BaseDatos.ps1` crea el esquema con los privilegios mínimos, guarda la conexión en los User Secrets y aplica en orden los pasos pendientes, registrándolos en `registro_migracion`.

| Comprobación sobre base limpia (2026-11-01, Oracle 26ai Free 23.26.3) | Resultado |
|---|---|
| Creación desde cero | correcta |
| Objetos inválidos | 0 |
| Segunda ejecución | 0 migraciones aplicadas |

## Migraciones añadidas

- `V016`: marca `deposito.sin_sensor` y evento de tipo 6.
- `V017`: canal asistido.

## Datos

Los datos de `03_datos_prueba` son ficticios y sus preparadores solo admiten la base `PRUEBAP3`. Las vistas de informes separan ensayos de operaciones reales. Los pesos solo se guardan si los midió una báscula.

## Catálogo

38 materiales con sinónimos, precauciones y ruta de separación en Oracle. La búsqueda es por texto normalizado, sin condicionales por palabra. Admitir un material es una configuración de cada contenedor.