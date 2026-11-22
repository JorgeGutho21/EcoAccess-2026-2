# Oracle Cloud · 04 · Seguridad y usuarios

> Paso a paso para los integrantes, auditoría de la instancia y prueba colaborativa: [05_ACCESO_INTEGRANTES.md](05_ACCESO_INTEGRANTES.md).

## Principios

* **mTLS obligatorio**: sin el wallet no hay conexión, aunque se conozca una contraseña.
* **Menor privilegio**: ADMIN solo administra; la aplicación usa una cuenta sin privilegios de administración.
* **Una cuenta por persona** para trabajar en SQL Developer (trazabilidad de quién consultó o modificó).
* Ninguna contraseña, cadena de conexión ni wallet entra en Git (`.gitignore` bloquea `Wallet_*.zip`, `*.sso`, `*.p12`, `tnsnames.ora`, `sqlnet.ora`, `secrets.json`).

## Cuentas

| Cuenta | Quién la usa | Privilegios | Creada por |
|---|---|---|---|
| `ADMIN` | solo el propietario, para tareas administrativas | administrador de Autonomous Database | Oracle |
| `ECOACCESS_P3_APP` | Windows Forms, el bot y el instalador | `CREATE SESSION`, `TABLE`, `SEQUENCE`, `VIEW`, `PROCEDURE`, `TRIGGER`; cuota 2 GB en `DATA`; dueña de las tablas | `04_crear_esquema_oracle_cloud.sql` |
| `ECO_JORGE`, `ECO_HEYDER`, `ECO_SARAY` (o `ECO_DEV_<NOMBRE>`) | cada integrante en SQL Developer | `CREATE SESSION` + `ECOACCESS_ROL_CONSULTA` (recomendado) o `_EDICION` | `06_crear_cuenta_integrante.sql` |

| Rol | Permisos sobre las tablas de `ECOACCESS_P3_APP` |
|---|---|
| `ECOACCESS_ROL_CONSULTA` | `SELECT`, salvo tablas sensibles (credenciales, tokens y mensajes crudos de Telegram) |
| `ECOACCESS_ROL_EDICION` | `SELECT`; `INSERT` y `UPDATE` solo en tablas de trabajo (el libro de ecopuntos, la auditoría y los eventos quedan en solo lectura); sin `DELETE` |

Los permisos de los roles se conceden con `05_conceder_permisos_roles.sql`, que el instalador ejecuta al final de cada instalación (así una tabla nueva queda cubierta).

## Crear la cuenta de un integrante

```bash
dotnet run --project src/EcoAccess.P3.Instalador -- crear-integrante
```

Pide la contraseña de ADMIN, el usuario (`ECO_JORGE`, `ECO_HEYDER`, `ECO_SARAY`), el rol y una contraseña **temporal**. La cuenta nace con la contraseña expirada: el integrante elige la suya al primer ingreso. La contraseña temporal se entrega por un canal privado.

## Dónde guarda cada equipo su conexión

* Windows Forms, el bot, las pruebas y el instalador leen `ConnectionStrings:OracleP3` de los **User Secrets `ecoaccess-p3-framework`** (`%APPDATA%\Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json`, dentro del perfil de Windows), o de la variable de entorno `ECOACCESS_ConnectionStrings__OracleP3`.
* La ventana «Configuración inicial (Oracle Cloud)» solo guarda la cadena **después** de que Oracle la acepta, junto con la carpeta del wallet y el alias (no secretos).
* Máximo tres intentos por ventana para no bloquear la cuenta (Autonomous bloquea tras 10 fallos).

## Revocación

| Situación | Acción |
|---|---|
| Un integrante deja el equipo | `ALTER USER ECO_DEV_X ACCOUNT LOCK;` como ADMIN |
| Se filtró el wallet | consola → *Database connection* → **Rotate wallet**; repartir el nuevo |
| Se filtró la contraseña de la aplicación | `ALTER USER ECOACCESS_P3_APP IDENTIFIED BY "<nueva>";` como ADMIN y volver a pasar por «Configuración inicial» en cada equipo |

## Decisión pendiente

Windows Forms y el bot se conectan como `ECOACCESS_P3_APP` porque el código usa nombres de tabla sin esquema. Si se quiere que cada integrante ejecute la **aplicación** con su cuenta personal, hará falta que la fábrica de conexiones fije `CURRENT_SCHEMA` y conceder también `DELETE` donde la lógica lo use. Se dejó documentado y sin implementar hasta decidirlo.
