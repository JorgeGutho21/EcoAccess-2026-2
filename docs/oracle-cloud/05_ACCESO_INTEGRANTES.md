# Oracle Cloud · 05 · Acceso de los integrantes a la base compartida

Objetivo: que Jorge, Heyder y Saray trabajen desde sus computadores contra **una sola base** en Oracle Cloud. Cada uno
usa su propia cuenta en SQL Developer; nadie usa ADMIN ni descarga una copia de la base.

## 1. Diagnóstico de la instancia (auditoría de solo lectura, 21/11/2026)

Lo obtuvo `dotnet run --project src/EcoAccess.P3.Instalador -- auditar`. Salida sin datos sensibles en
[`docs/evidencias/oracle_cloud/auditoria_esquema_2026-11-21.txt`](../evidencias/oracle_cloud/auditoria_esquema_2026-11-21.txt).

| Aspecto | Valor verificado |
|---|---|
| Servicio | Autonomous Database 19c, tipo **OLTP** (Transaction Processing), región Bogotá |
| Alias del wallet | `ecoaccess_tp` → servicio `<base>_ecoaccess_tp.adb.oraclecloud.com`, puerto 1522 |
| Conexión | **mTLS** (`tcps`) con *Instance wallet*. La base se creó con «Secure access from everywhere»: no hay lista de IP. La conexión desde este equipo funcionó. |
| Esquema de la aplicación | `ECOACCESS_P3_APP`: 33 tablas, 21 vistas, 5 paquetes, 2 procedimientos, 3 funciones, 19 secuencias y 5 disparadores, **0 inválidos**. Última migración: V029. |
| Privilegios de `ECOACCESS_P3_APP` | `CREATE SESSION`, `TABLE`, `VIEW`, `SEQUENCE`, `PROCEDURE` y `TRIGGER`; ningún rol; cuota de 2 GB en `DATA` (usa 8 MB) |
| Roles para personas | `ECOACCESS_ROL_CONSULTA` y `ECOACCESS_ROL_EDICION` existen y tienen permisos |
| Cuentas de integrantes | **Ninguna todavía**: la única cuenta `ECO*` es `ECOACCESS_P3_APP` |
| `DBMS_CRYPTO` | **No hace falta**: ni el código C#, ni las migraciones, ni los scripts lo usan. Las contraseñas de la aplicación se calculan en C#. No se concede a nadie. |

Lo que solo puede ver ADMIN (estado de las cuentas, roles concedidos, privilegios peligrosos, perfil de contraseñas) lo
muestra `auditar-admin`. Ese comando pide la contraseña de ADMIN, que escribe el propietario.

### Tablas encontradas por tema

| Tema | Tablas |
|---|---|
| Usuarios y roles de la aplicación | `USUARIO`, `ROL`, `USUARIO_ROL`, `CREDENCIAL_ACCESO` |
| Catálogo de residuos | `MATERIAL`, `PESO_REFERENCIAL`, `PUNTO_DISPOSICION` |
| Canecas y estación | `CONTENEDOR`, `CONTENEDOR_MATERIAL`, `CONFIGURACION_CANECA`, `PUNTO_INSTALACION`, `HISTORIAL_ESTADO_CONTENEDOR` |
| Depósitos | `DEPOSITO`, `INTERACCION`, `AUDITORIA_DEPOSITO`, `ORDEN_APERTURA` |
| Ecopuntos | `REGLA_ECOPUNTOS`, `OPERACION_ECOPUNTOS`, `MOVIMIENTO_ECOPUNTOS` |
| Hardware | `EVENTO_DISPOSITIVO` |
| Telegram | `VINCULO_TELEGRAM`, `TOKEN_VINCULACION`, `VINCULACION_PENDIENTE`, `CONVERSACION_TELEGRAM`, `UPDATE_TELEGRAM` |
| Visión y opiniones | `ANALISIS_IMAGEN`, `CONSULTA_VISION`, `CONSENTIMIENTO_VISION_EXTERNA`, `OPINION`, `COMENTARIO_CIUDADANO` |
| Simulación y control | `LOTE_SIMULACION`, `REGISTRO_SIMULACION`, `ECO_MIGRACION_APLICADA` |

## 2. Cuentas y permisos mínimos

| Cuenta | Quién | Para qué | Privilegios |
|---|---|---|---|
| `ADMIN` | solo el propietario | crear cuentas y roles | administrador de la base; **no se comparte** |
| `ECOACCESS_P3_APP` | la aplicación C#, el bot y el instalador | dueña de las tablas | los de la tabla anterior |
| `ECO_JORGE`, `ECO_HEYDER`, `ECO_SARAY` | cada integrante en SQL Developer | consultar (y, si se autoriza, editar) | `CREATE SESSION` + **un** rol |

| Rol | Permisos (sobre `ECOACCESS_P3_APP`) |
|---|---|
| `ECOACCESS_ROL_CONSULTA` (**recomendado para los tres**) | `SELECT` en tablas y vistas, salvo las sensibles |
| `ECOACCESS_ROL_EDICION` (solo si alguien debe corregir datos a mano) | además `INSERT` y `UPDATE` en tablas de trabajo y `EXECUTE` en los paquetes de V027. **Nunca** `DELETE`. |

**Tablas sensibles:** ningún rol las ve. Son `CREDENCIAL_ACCESO` (hashes de contraseñas), `TOKEN_VINCULACION` y
`VINCULACION_PENDIENTE` (códigos de vinculación de Telegram) y `UPDATE_TELEGRAM` (mensajes crudos).

**Solo lectura también para EDICION:** el libro de ecopuntos, las reglas, la auditoría, los eventos del hardware, las
órdenes de apertura y el registro de migraciones. Los escribe únicamente la aplicación.

El script es `database/00_administracion/05_conceder_permisos_roles.sql` (revisado el 21/11/2026) y lo ejecuta el dueño
del esquema.

**Advertencia:** `EXECUTE` sobre `PKG_ECOPUNTOS` y `PKG_DEPOSITO` corre con los permisos del dueño. Por eso EDICION solo
se da a quien lo necesite y por el tiempo necesario.

Nadie recibe `DBA`, `ADMIN`, privilegios `ANY`, `DELETE` ni `DBMS_CRYPTO`. Los cambios de estructura se hacen **solo**
con las migraciones del instalador (`instalar`), con la cuenta dueña. No se hacen a mano.

## 3. Crear las cuentas (lo hace el propietario; las contraseñas las escribe él)

En una ventana propia de PowerShell, en la carpeta del repositorio, una vez por integrante:

```powershell
dotnet run --project src\EcoAccess.P3.Instalador -- crear-integrante
```

1. **Contraseña de ADMIN:** la escribe el propietario. No se muestra ni se guarda.
2. **Usuario:** `ECO_JORGE`, `ECO_HEYDER` o `ECO_SARAY`.
3. **Rol:** `CONSULTA`.
4. **Contraseña temporal:** la escribe el propietario.
   - Debe tener de 12 a 30 caracteres, con mayúscula, minúscula y número, sin comillas dobles y sin el nombre del usuario.
   - La cuenta queda con la contraseña **expirada**: el integrante elige la suya la primera vez que entra.
5. **Entrega:** la temporal se le da en persona o por un canal privado. Nunca por el grupo, GitHub ni correo con copia.

Antes de crear las cuentas, el dueño del esquema aplica los permisos revisados y crea la tabla de prueba. Usa la
conexión ya guardada en su equipo, sin escribir contraseñas:

```powershell
dotnet run --project src\EcoAccess.P3.Instalador -- preparar-prueba-colaborativa
```

Ese comando crea `PRUEBA_COLABORATIVA` y vuelve a ejecutar `05_conceder_permisos_roles.sql`.

Después, el propietario comprueba con `auditar-admin`. Debe ver las tres cuentas, cada una con `CREATE SESSION` y un solo
rol, y la sección «Privilegios peligrosos» vacía.

**Restablecer o bloquear una cuenta** (como ADMIN en SQL Developer; la contraseña la escribe él en la ventana *Enter Binds*):

```sql
ALTER USER ECO_HEYDER IDENTIFIED BY "<nueva temporal>" PASSWORD EXPIRE;   -- restablecer
ALTER USER ECO_HEYDER ACCOUNT UNLOCK;                                        -- desbloquear tras intentos fallidos
ALTER USER ECO_HEYDER ACCOUNT LOCK;                                          -- retirar el acceso
```

## 4. Paso a paso en un computador nuevo (cada integrante)

1. **Visual Studio 2022** con la carga «Desarrollo de escritorio de .NET» y el *.NET Framework 4.8 targeting pack*.
   También el SDK de .NET (`dotnet --version`).
2. **Clonar** el repositorio:

   ```powershell
   git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git
   ```

3. **Abrir y compilar:** abre `EcoAccess.P3.sln` y compila. Visual Studio restaura los paquetes NuGet solo, o con
   `dotnet restore EcoAccess.P3.sln`.
4. **SQL Developer:** instala la versión 23 o posterior desde oracle.com, o `winget install Oracle.SQLDeveloper`.
5. **Recibir el wallet:**
   - El propietario lo descarga en OCI (*Autonomous Database → EcoAccess → Database connection → Instance wallet →
     Download*).
   - Lo entrega en persona o por un enlace privado de OneDrive compartido solo con esa persona.
   - El integrante lo guarda **fuera del repositorio**, en `%USERPROFILE%\.oracle\wallets\EcoAccess`.
   - **No se sube a GitHub** (el `.gitignore` lo bloquea).
6. **Conexión en SQL Developer** (botón **+**):

   | Campo | Valor |
   |---|---|
   | Name | `EcoAccess Cloud - <tu nombre>` |
   | Username | `ECO_JORGE` / `ECO_HEYDER` / `ECO_SARAY` |
   | Password | la tuya; no marques «Save password» en un equipo compartido |
   | Connection Type | **Cloud Wallet** |
   | Configuration File | el `Wallet_EcoAccess.zip` o la carpeta descomprimida |
   | Service | `ecoaccess_tp` |

7. **Probar y conectar:** pulsa **Test**; la primera vez Oracle pide cambiar la contraseña temporal por la tuya. Debe
   decir *Success*. Después pulsa **Connect**.
8. **Consultar** en la hoja de trabajo:

   ```sql
   ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;
   SELECT SYS_CONTEXT('USERENV','SESSION_USER') yo, SYS_CONTEXT('USERENV','DB_NAME') base FROM dual;
   SELECT codigo, nombre, categoria FROM material ORDER BY codigo;
   SELECT COUNT(*) FROM deposito;
   ```

   Si la base no es `…_ECOACCESS` o no ves `MATERIAL`, revisa el wallet y el servicio.
9. **Comprobar tus permisos desde la consola.** La contraseña la escribes tú:

   ```powershell
   dotnet run --project src\EcoAccess.P3.Instalador -- comprobar-cuenta --usuario ECO_HEYDER
   ```

   Las lecturas deben salir **OK** y el borrado, la creación de tablas, `DBA_USERS` y `GRANT` deben salir rechazados.
10. **La aplicación C#:**
    - Se conecta como `ECOACCESS_P3_APP`, dueña de las tablas; no usa tu cuenta personal. Ese es su modelo actual y no
      se cambió.
    - Su contraseña la escribe **el propietario** en tu equipo:

      ```powershell
      powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Credenciales.ps1 -Oracle
      ```

      Queda solo en tu perfil de Windows (`%APPDATA%\Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json`).
    - Alternativa: `ECOACCESS_ConnectionStrings__OracleP3` como variable de entorno.
    - La carpeta del wallet se busca en `%USERPROFILE%\.oracle\wallets\EcoAccess` o donde diga `TNS_ADMIN`; no hay rutas
      de un equipo en el código.
11. **Ejecutar EcoAccess:** desde Visual Studio (proyecto `EcoAccess.P3.Presentacion`) o instalándolo:

    ```powershell
    powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"
    ```

    Si Oracle no responde, la aplicación lo dice («Sin conexión con Oracle») y reintenta sola.
12. **Prueba colaborativa** (sección 5).

## 5. Prueba colaborativa (misma base, sin tocar datos reales)

Cada integrante, desde su computador y con su contraseña:

```powershell
dotnet run --project src\EcoAccess.P3.Instalador -- prueba-colaborativa --usuario ECO_<TU NOMBRE>
```

1. Inserta una fila en `PRUEBA_COLABORATIVA`. Oracle pone el usuario, la base y la hora; nada del equipo.
2. Muestra el id nuevo y las últimas 10 filas de **todos**.
3. Jorge, Heyder y Saray lo ejecutan uno tras otro y comparan.

Resultado esperado:
- cada uno ve las filas de los otros dos con sus id;
- la columna `BASE` es la misma en las tres filas.

Con eso queda demostrado que no hay bases locales. Esta prueba requiere el rol EDICION. Con CONSULTA el
`INSERT` se rechaza, lo cual también es correcto. Si se usa CONSULTA, la inserción la hace el propietario y los demás
solo leen:

```sql
SELECT * FROM ecoaccess_p3_app.prueba_colaborativa ORDER BY id_prueba DESC;
```

La tabla no forma parte de la aplicación. Se borra al terminar con `DROP TABLE prueba_colaborativa PURGE;`, como dueño
del esquema.

## 6. Diagnóstico de errores frecuentes

| Error | Causa | Solución |
|---|---|---|
| `ORA-01017` | usuario o contraseña incorrectos | revisa mayúsculas; varios intentos bloquean la cuenta (`ACCOUNT UNLOCK` lo hace ADMIN) |
| `ORA-28001` | contraseña temporal expirada | SQL Developer pide la nueva; es lo esperado la primera vez |
| `ORA-28000` | cuenta bloqueada | el propietario la desbloquea |
| `ORA-12506` / `ORA-12541` / tiempo agotado | base detenida o red | en OCI la base debe estar *Available* (Always Free se detiene tras 7 días sin uso); prueba otra red si un firewall bloquea el puerto 1522 |
| `ORA-29024` / `ORA-28759` | wallet equivocado o incompleto | vuelve a descomprimir el wallet de la instancia EcoAccess |
| `ORA-00942` en una tabla | tabla sensible o sin permiso | es intencional para `CREDENCIAL_ACCESO`, tokens y mensajes de Telegram |
| No veo las tablas | falta el esquema | `ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;` o anteponer `ecoaccess_p3_app.` |

## 7. Lo que nunca va a GitHub

- Contraseñas.
- El wallet (`Wallet_*.zip`, `cwallet.sso`, `ewallet.p12`, `tnsnames.ora`, `sqlnet.ora`).
- `secrets.json` y cadenas de conexión con `Password=`.
- Tokens de Telegram.

Los comandos de este documento piden las contraseñas sin mostrarlas, no las reciben como argumentos y no las imprimen.
