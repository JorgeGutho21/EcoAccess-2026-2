# database/schema · DDL real del esquema oficial

Definiciones **extraídas** de Oracle Cloud (`ECOACCESS_P3_APP`) con `DBMS_METADATA`, un archivo por objeto. No se reconstruyeron a mano.

| Carpeta | Contenido |
|---|---|
| `tables/` | 33 tablas con PK, UNIQUE, CHECK e IDENTITY |
| `constraints/` | Claves foráneas, un archivo por tabla |
| `indexes/` | Índices que no respaldan una restricción |
| `comments/` | `COMMENT ON TABLE/COLUMN` |
| `views/` | 21 vistas |
| `packages/` | `*.pks.sql` (especificación) y `*.pkb.sql` (cuerpo) de los 5 paquetes |
| `procedures/`, `functions/`, `triggers/` | 2 procedimientos, 3 funciones y 5 disparadores |
| [`INVENTARIO.md`](INVENTARIO.md) | Objeto, tipo, estado, dependencias, archivo y documentación |
| [`00_instalar_esquema_completo.sql`](00_instalar_esquema_completo.sql) | Instalación en un esquema **vacío**, en orden de dependencias |

## Reglas

- **No se edita a mano.** Un cambio de estructura va en una migración nueva en [`../06_migraciones/`](../06_migraciones/). Después de aplicarla, se regenera esta carpeta:
  ```powershell
  pwsh -File database\scripts\Exportar-EsquemaOracle.ps1
  ```
- **No se ejecuta en Oracle Cloud:** allí los objetos ya existen. El instalador se niega a correr si el esquema tiene objetos.
- No contiene datos, contraseñas ni nombres de esquema: se exporta con `EMIT_SCHEMA = FALSE`, sin almacenamiento ni tablespace.
- Los archivos reflejan Oracle **tal como está**. Se volvieron a exportar después de V029, que corrigió las tildes dañadas por V026–V028 ([hallazgo H-18](../../docs/database/16-hallazgos.md)).

Documentación de cada objeto: [`docs/database/`](../../docs/database/README.md).
