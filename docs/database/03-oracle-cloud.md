# 03 · Oracle Cloud (la base oficial)

**Base:** Autonomous Database 19c, Transaction Processing, *Always Free*, en Bogotá. **Esquema:** `ECOACCESS_P3_APP`.

Los pasos de la consola de Oracle (arrancar la base, descargar el wallet, conexión ADMIN) están en [`docs/oracle-cloud/01_CONEXION_SQL_DEVELOPER.md`](../oracle-cloud/01_CONEXION_SQL_DEVELOPER.md). Aquí está lo que necesita un integrante.

## Método de conexión

Lo verificado en la configuración que usan la aplicación y SQL*Plus en este PC:
- **mTLS con wallet**, usando la carpeta `%USERPROFILE%\.oracle\wallets\EcoAccess` (`cwallet.sso`, `tnsnames.ora` y `sqlnet.ora`);
- el alias **`ecoaccess_tp`**.

La documentación del proyecto indica que la base exige mTLS. No se comprobó desde la consola si además está habilitado TLS sin wallet: esa opción está en la consola, en *Network → Mutual TLS (mTLS) authentication*.

**Alias:** el wallet trae cinco (`_high`, `_medium`, `_low`, `_tp`, `_tpurgent`). EcoAccess usa **`ecoaccess_tp`**, el perfil para transacciones cortas y concurrentes (la aplicación y el bot).

**Red:**
- salida a Internet por el puerto TCP **1522** (el de Autonomous Database con mTLS);
- *Secure access from everywhere*, sin lista de IP según la documentación del proyecto;
- la base Always Free se **detiene sola tras 7 días sin uso**: si no conecta, revise en la consola que esté *Available*.

## Wallet: dónde va y dónde NO

- **Va** en `%USERPROFILE%\.oracle\wallets\EcoAccess`, fuera del repositorio.
- **No va** en el repositorio, en un chat ni en un correo. El `.gitignore` bloquea `cwallet.sso`, `ewallet.p12`, `ewallet.pem`, `*.jks`, `Wallet_*.zip` y cualquier carpeta `wallets/`. Lo comprueba la Prueba I.
- Solo el dueño de la cuenta de Oracle Cloud descarga el wallet. A un integrante se le entrega **en persona o por un canal privado**, nunca por GitHub.

## Conexión en SQL Developer (integrante)

Cada integrante usa **su propia cuenta** `ECO_DEV_<NOMBRE>`, no la de la aplicación ni ADMIN. La crea el administrador con [`database/00_administracion/06_crear_cuenta_integrante.sql`](../../database/00_administracion/06_crear_cuenta_integrante.sql):
- con el rol `ECOACCESS_ROL_CONSULTA` (solo lectura) o `ECOACCESS_ROL_EDICION`;
- con una contraseña temporal **expirada**, que el integrante cambia la primera vez.

1. SQL Developer → **+**.
2. Complete:

| Campo | Valor |
|---|---|
| Name | `EcoAccess CLOUD - ECO_DEV_<NOMBRE>` |
| Username | `ECO_DEV_<NOMBRE>` |
| Password | la suya |
| Connection Type | **Cloud Wallet** |
| Configuration File | el `Wallet_EcoAccess.zip` (o la carpeta) que le entregaron |
| Service | **`ecoaccess_tp`** |

3. **Test** → *Success* → **Connect**.
4. En la hoja de trabajo, para ver las tablas del proyecto con nombres cortos:
   ```sql
   ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;
   ```
5. En el árbol, los objetos están en *Otros usuarios → ECOACCESS_P3_APP*.

**Comprobar la conexión:** abra y ejecute con **F5** [`database/tests/A2_verificacion_integrante.sql`](../../database/tests/A2_verificacion_integrante.sql). Debe terminar en `RESULTADO INTEGRANTE: 7 correctas, 0 fallidas.`

## Permisos de cada rol

Leídos de `USER_TAB_PRIVS_MADE` el 18/11/2026:

| Rol | Permisos |
|---|---|
| `ECOACCESS_ROL_CONSULTA` | `SELECT` sobre 38 objetos (tablas y vistas) |
| `ECOACCESS_ROL_EDICION` | `SELECT` sobre 38, `INSERT` y `UPDATE` sobre 25 y `EXECUTE` sobre 7 objetos PL/SQL. **Sin `DELETE`** |
| `ECOACCESS_P3_APP` (dueño) | `CREATE SESSION/TABLE/VIEW/PROCEDURE/SEQUENCE/TRIGGER`; ningún privilegio `ANY` |

El 18/11/2026 **no existía ninguna cuenta `ECO_DEV_*`** en la base: hay que crearlas antes de que los integrantes se conecten.

## Reglas en la base compartida

- **No** ejecute scripts de creación (`database/schema/00_instalar_esquema_completo.sql`, `database/02_tablas`): los objetos ya existen, y ese instalador se niega a correr en un esquema con objetos.
- **No** ejecute pruebas que escriben (Prueba F). Las de solo lectura (A–E) se pueden ejecutar con la cuenta dueña, y la verificación A2 con una cuenta de integrante.
- Un cambio de estructura se hace con una **migración nueva** (`V029__…`) revisada en un pull request, **nunca** editando un objeto a mano en SQL Developer.
