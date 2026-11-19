# Base de datos EcoAccess · documentación

Auditoría, documentación y pruebas de la base Oracle de EcoAccess, hechas sobre la base **real** el 18/11/2026. Todo lo que dice esta carpeta se comprobó con consultas, salvo lo que está marcado como pendiente.

**En una frase:**
- La base oficial es **Oracle Cloud** (Autonomous Database 19c, esquema `ECOACCESS_P3_APP`), con 33 tablas, 21 vistas, 5 paquetes, 2 procedimientos, 3 funciones y 5 disparadores, **todos válidos**.
- La conexión «EcoAccess BD LOCAL» de SQL Developer apunta a **otra** base, el Oracle local del PC.

| # | Documento | Para quién |
|---|---|---|
| 01 | [Arquitectura y diagnóstico de conexiones](01-arquitectura.md) | Todos. **Empiece aquí** |
| 02 | [Conexión a un Oracle local](02-conexion-local.md) | Escenario B |
| 03 | [Oracle Cloud](03-oracle-cloud.md) | Escenario A |
| 04 | [Instalación en un esquema de desarrollo y migraciones](04-instalacion.md) | Quien instale o cambie la estructura |
| 05 | [Tablas, relaciones y diccionario de datos (252 columnas)](05-tablas-relaciones.md) | Todos |
| 06 | [Vistas (las nueve `VW_*` y las demás)](06-vistas.md) | Todos, Power BI |
| 07 | [Paquetes PL/SQL](07-paquetes.md) | Todos |
| 08 | [Procedimientos](08-procedimientos.md) | Todos |
| 09 | [Funciones](09-funciones.md) | Todos |
| 10 | [Disparadores y secuencias](10-triggers-secuencias.md) | Todos |
| 11 | [Flujo completo de un depósito](11-flujo-deposito.md) | Sustentación |
| 12 | [Ecopuntos: fórmula y reglas](12-ecopuntos.md) | Sustentación |
| 13 | [Pruebas y resultados](13-pruebas.md) | Todos |
| 14 | [**Guía para compañeros**, de cero a funcionando](14-guia-companeros.md) | Integrantes nuevos |
| 15 | [Explicación académica con ejercicios](15-explicacion-academica.md) | Profesor de Base de Datos |
| 16 | [Hallazgos y propuestas](16-hallazgos.md) | Equipo: decisiones pendientes |

**Código SQL:**
- [`database/schema/`](../../database/schema/): DDL real, un archivo por objeto, más el [inventario](../../database/schema/INVENTARIO.md).
- [`database/06_migraciones/`](../../database/06_migraciones/): cambios aplicados, en orden.
- [`database/tests/`](../../database/tests/): pruebas.
- [`database/scripts/`](../../database/scripts/): exportar el esquema y ejecutar las pruebas.

**Evidencias:** [`evidencias/`](evidencias/).
