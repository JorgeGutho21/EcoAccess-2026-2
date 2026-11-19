# 04 · Instalar la base en un esquema de desarrollo

## Dos cosas distintas: instalación inicial y migración

| | **Instalación inicial** | **Migración** |
|---|---|---|
| Para qué | Construir el esquema completo en una base **vacía** | Aplicar **un cambio** a un esquema que ya existe y tiene datos |
| Archivo | [`database/schema/00_instalar_esquema_completo.sql`](../../database/schema/00_instalar_esquema_completo.sql), generado desde la base real | [`database/06_migraciones/V0NN__*.sql`](../../database/06_migraciones/), uno por cambio, en orden |
| Dónde | Solo un esquema nuevo de desarrollo o de pruebas | La base oficial y los esquemas de prueba |
| Se repite | No: se niega si el esquema no está vacío (error −20991) | Sí: cada migración comprueba si ya se aplicó y queda registrada en `ECO_MIGRACION_APLICADA` |

**Regla:** una migración aplicada **no se edita**. Su huella SHA-256 queda registrada, y `.gitattributes` protege los bytes de las migraciones aplicadas. Para corregirla se escribe la siguiente (`V029`, …).

> **DDL y ROLLBACK.** En Oracle, cada `CREATE`, `ALTER` o `DROP` hace un `COMMIT` implícito. Un `ROLLBACK` **no** deshace un cambio de estructura. Por eso cada migración trae su script de reversión en [`06_migraciones/reversion/`](../../database/06_migraciones/reversion/), y la instalación inicial se prueba en un esquema que se puede tirar.

## Orden de instalación (validado contra las dependencias reales)

`00_instalar_esquema_completo.sql` ejecuta, en este orden:

| Paso | Qué | Por qué en ese orden |
|---|---|---|
| 1 | Comprobación | Rechaza SYS, SYSTEM o ADMIN, y un esquema con objetos |
| 2 | `tables/` (33) | Con PK, UNIQUE, CHECK y columnas IDENTITY. Las secuencias las crea Oracle |
| 3 | `constraints/` (24 archivos) | Claves foráneas, cuando ya existen todas las tablas. Incluyen las autorreferencias de `MOVIMIENTO_ECOPUNTOS` |
| 4 | `indexes/` (41) | Los que no respaldan una restricción, entre ellos el único de crédito inicial |
| 5 | `comments/` | Comentarios del diccionario |
| 6 | Especificaciones de `packages/` | Las vistas usan `PKG_DEPOSITO.ESTADO` |
| 7 | `views/` (21) | Con `FORCE`: se crean aunque falte algo que viene después |
| 8 | `functions/`, `procedures/` | `FN_DEPOSITOS_CIUDADANO` usa una vista |
| 9 | Cuerpos de `packages/` | Ya existe todo lo que usan |
| 10 | `triggers/` (5) | `TRG_DEPOSITO_COHERENCIA` va con `FOLLOWS` después de `TRG_DEPOSITO_BLOQUEO_CERRADO` |
| 11 | `DBMS_UTILITY.COMPILE_SCHEMA` y control | Error −20992 si queda algún objeto inválido |

## Cómo ejecutarlo

1. Cree el esquema vacío (ver [02-conexion-local.md](02-conexion-local.md)).
2. Desde la carpeta del repositorio:
   ```powershell
   sql ECOACCESS_P3_PRUEBAS_DEV@//localhost:1521/FREEPDB1 "@database/schema/00_instalar_esquema_completo.sql"
   ```
   SQLcl pide la contraseña sin mostrarla. Con SQL*Plus es igual: `sqlplus <usuario>@//localhost:1521/FREEPDB1 @database/schema/00_instalar_esquema_completo.sql`.
3. **Resultado correcto:** la última línea dice `Esquema instalado sin objetos inválidos.`
4. **Datos de demostración** (opcional, solo ficticios): los catálogos están en [`database/03_datos_prueba/`](../../database/03_datos_prueba/), por ejemplo `01_catalogos.sql` para roles y materiales y `03_catalogo_ambiental_piloto.sql`. No cargue datos reales ni copias de la base oficial con datos personales.
5. Ejecute las pruebas (ver [13-pruebas.md](13-pruebas.md)):
   ```powershell
   pwsh -File database\scripts\Ejecutar-PruebasBD.ps1 -Secreto ecoaccess-p3-dev -IncluirEscritura
   ```

## Volver a generar `database/schema` desde la base

Cuando una migración nueva cambie la estructura oficial, el dueño del esquema regenera los archivos:
```powershell
pwsh -File database\scripts\Exportar-EsquemaOracle.ps1
git diff database/schema     # revise que solo cambió lo que la migración debía cambiar
```
El script es de solo lectura (`DBMS_METADATA` y `USER_*`), no muestra la contraseña y reescribe también `INVENTARIO.md` y el orden de instalación.

## Estado de la prueba de instalación desde cero

**BLOQUEADA, pendiente de una acción del dueño del PC.** Para probarla hace falta un esquema **vacío**. Crearlo requiere la contraseña de `SYSTEM` del Oracle local, que solo debe escribir el dueño del equipo:
- el esquema aislado que existe (`ECOACCESS_P3_PRUEBAS`) ya tiene la estructura completa;
- borrarlo para reinstalar sería destructivo, y no se hizo.

Lo que **sí** está comprobado:
- los archivos de `database/schema` son el DDL real de la base;
- la estructura del esquema aislado coincide objeto por objeto con la oficial (154 objetos iguales).

Pasos para completar la prueba:
1. `Crear-Esquema-Aislado-Local.ps1 -Esquema ECOACCESS_P3_PRUEBAS_DEV -IdSecretos ecoaccess-p3-dev` (lo ejecuta el dueño del PC).
2. El paso 2 de arriba.
3. `Ejecutar-PruebasBD.ps1 -Secreto ecoaccess-p3-dev -IncluirEscritura`.
