# Oracle Cloud · 01 · Conexión con SQL Developer (mTLS con wallet)

Base: **EcoAccess** · Autonomous Database 19c · Transaction Processing · Always Free · `sa-bogota-1` (Bogotá) · acceso «Secure access from everywhere» con **mTLS obligatorio**.

> No usar la base `SQLSelectLab`. No cambiar la base a un tipo de pago. No compartir la contraseña de ADMIN.

## 1. Comprobar que la base está disponible

1. Entrar en <https://cloud.oracle.com> con la cuenta del propietario y elegir la región **Colombia Central (Bogotá)**.
2. *Oracle Database → Autonomous Database* → **EcoAccess**.
3. El estado debe ser **Available** (verde). Si dice *Stopped*, pulsar **Start** (las bases Always Free se detienen solas tras 7 días sin uso; arrancarlas no tiene costo).
4. Confirmar en *Details* que aparece **Always Free: Yes**.

## 2. Descargar el wallet

1. En la página de la base: **Database connection**.
2. *Wallet type*: **Instance wallet** → **Download wallet**.
3. Oracle pide una contraseña para el wallet (protege `ewallet.p12`; ODP.NET y SQL Developer usan `cwallet.sso` y no la necesitan). Elegirla y guardarla en un gestor de contraseñas.
4. Se descarga `Wallet_EcoAccess.zip`.
5. Descomprimirlo **fuera del repositorio** en:

```bash
powershell -Command "Expand-Archive -Path $env:USERPROFILE\Downloads\Wallet_EcoAccess.zip -DestinationPath $env:USERPROFILE\.oracle\wallets\EcoAccess -Force"
```

La carpeta debe contener al menos `tnsnames.ora`, `sqlnet.ora`, `cwallet.sso` y `ewallet.p12`. El `.gitignore` del proyecto bloquea estos archivos por si alguien los copia por error dentro del repositorio.

En `tnsnames.ora` aparecen cinco alias: `ecoaccess_high`, `_medium`, `_low`, `_tp` y `_tpurgent`. **EcoAccess usa `ecoaccess_tp`** (transacciones cortas y concurrentes, el perfil adecuado para Windows Forms y el bot).

## 3. Conexión administrativa inicial en SQL Developer

1. Abrir SQL Developer → **+** (nueva conexión).
2. Datos:

| Campo | Valor |
|---|---|
| Name | `EcoAccess Cloud - ADMIN` |
| Username | `ADMIN` |
| Password | la de ADMIN (no marcar «Save password» en equipos compartidos) |
| Connection Type | **Cloud Wallet** |
| Configuration File | `%USERPROFILE%\.oracle\wallets\EcoAccess\Wallet_EcoAccess.zip` o la carpeta descomprimida |
| Service | `ecoaccess_tp` |

3. **Test** → debe decir *Status: Success*. Después **Connect**.

## 4. Consulta de validación (no destructiva)

En la hoja de trabajo de ADMIN:

```sql
SELECT SYS_CONTEXT('USERENV','SESSION_USER') AS usuario,
       SYS_CONTEXT('USERENV','CON_NAME')     AS pdb,
       (SELECT banner FROM v$version WHERE ROWNUM = 1) AS version,
       SYSTIMESTAMP AS fecha_servidor
FROM dual;

SELECT username, account_status, created
FROM dba_users
WHERE oracle_maintained = 'N'
ORDER BY created;
```

Se espera `ADMIN`, un nombre de PDB asignado por Oracle, `Oracle Database 19c Enterprise Edition ...` y la lista de esquemas creados por personas (en una base nueva: solo `ADMIN`, y `ECOACCESS_P3_APP` después de prepararla).

## 5. Conexión de la aplicación (`ECOACCESS_P3_APP`)

Igual que la anterior, con *Name* `EcoAccess Cloud - APP` y *Username* `ECOACCESS_P3_APP`. Es la cuenta dueña de las tablas; con ella se ejecutan las consultas de `database/04_consultas/`.

## 6. Conexión desde otro computador (integrantes)

Cada integrante usa **su propia cuenta** `ECO_DEV_<NOMBRE>` (ver [04_SEGURIDAD_Y_USUARIOS.md](04_SEGURIDAD_Y_USUARIOS.md)):

1. Instalar SQL Developer (`winget install --id Oracle.SQLDeveloper -e`).
2. Recibir el `Wallet_EcoAccess.zip` del propietario por un canal privado (no por el repositorio ni por grupos públicos). Para revocar el acceso basta con **Rotate wallet** en la consola.
3. Crear la conexión *Cloud Wallet* con su usuario `ECO_DEV_...` y servicio `ecoaccess_tp`.
4. En el primer ingreso Oracle obliga a cambiar la contraseña temporal.
5. Para ver las tablas del proyecto:

```sql
ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;
SELECT COUNT(*) FROM usuario;
```

## 7. Problemas frecuentes

| Síntoma | Causa probable | Solución |
|---|---|---|
| `ORA-12506` / `ORA-12541` / *IO Error* | base detenida o sin red | ver §1 (Start); revisar Internet/firewall (puerto **1522** saliente) |
| `ORA-28759` / errores TLS | wallet antiguo (tras *Rotate wallet*) | descargar de nuevo el wallet |
| `ORA-01017` | usuario o contraseña incorrectos | revisar; varios fallos bloquean la cuenta (`ORA-28000`) |
| `ORA-28001` | contraseña expirada | SQL Developer pide la nueva al conectar |
| `ORA-01031` en una tabla | la cuenta personal no tiene ese permiso | pedir al propietario el rol adecuado |
