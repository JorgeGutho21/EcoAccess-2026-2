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

## 8. Estado verificado el 21/11/2026 (después de crear las cuentas)

Auditoría de solo lectura con `auditar`. Salida en
[`docs/evidencias/oracle_cloud/auditoria_esquema_2026-11-21.txt`](../evidencias/oracle_cloud/auditoria_esquema_2026-11-21.txt).

| Comprobación | Resultado |
|---|---|
| Conexión `ecoaccess_tp` (mTLS) | OK, como `ECOACCESS_P3_APP` |
| Objetos | 34 tablas (con `PRUEBA_COLABORATIVA`), 21 vistas, 5 paquetes y 20 secuencias; **0 inválidos** |
| Migraciones | última V029 |
| Cuentas | `ECO_JORGE`, `ECO_HEYDER` y `ECO_SARAY` creadas el 21/11/2026 con contraseña temporal expirada y rol **EDICION** (elegido por el propietario) |
| Permisos de los roles | 39 objetos con `SELECT` (sin las 4 tablas sensibles); EDICION escribe en 22 tablas; `EXECUTE` en 7 objetos PL/SQL |
| Datos de la aplicación | 41 materiales habilitados, 3 canecas por color, 79 usuarios activos, 113 depósitos (la vista del dashboard `VW_BI_DEPOSITOS` también tiene 113), 376 eventos del hardware, 0 movimientos de ecopuntos y 7 reglas vigentes (matriz anterior; la 2026.11.21-1 del PR #20 no se ha aplicado) |
| `PRUEBA_COLABORATIVA` | creada, sin filas todavía |

El estado de cada cuenta (OPEN, EXPIRED o LOCKED) y el perfil de contraseñas solo los ve ADMIN. El propietario los
consulta con `auditar-admin`, escribiendo su contraseña.

## 9. Riesgo de seguridad pendiente y su corrección (requiere autorización)

**Hallazgo.** EDICION tiene `EXECUTE` sobre paquetes que se ejecutan con los derechos del dueño:

| Objeto | Qué permite |
|---|---|
| `PKG_ECOPUNTOS` | `ACREDITAR`, `REVERTIR` y `RECALCULAR` |
| `PKG_DEPOSITO` | `VERIFICAR`, `RECHAZAR` y `CORREGIR_VERIFICACION` |
| `PKG_CONTEXTO_APP` | `FIJAR_ACTOR` |
| `PR_CAMBIAR_ESTADO_CONTENEDOR` | cambiar el estado de una caneca |

`ACREDITAR` recibe el id del actor como parámetro y solo comprueba que ese actor sea Operario o Administrador. Como
EDICION puede leer `USUARIO_ROL`, una persona en SQL Developer puede acreditar ecopuntos usando el id de un supervisor.
Además, EDICION puede hacer `INSERT` en `USUARIO_ROL`, es decir, hacer Administrador a cualquier usuario de la aplicación.

**Tres tipos de acceso que no se deben confundir:**
1. **Humano en SQL Developer:** `ECO_JORGE`, `ECO_HEYDER` y `ECO_SARAY`, con un rol de Oracle.
2. **Técnico de la aplicación C# y del bot:** `ECOACCESS_P3_APP`, dueña de las tablas. No usa los roles, así que la
   corrección no la afecta.
3. **Roles internos de EcoAccess:** Ciudadano, Operario y Administrador (supervisor). Están en las tablas `ROL` y
   `USUARIO_ROL` y se comprueban dentro de la aplicación y de los paquetes.

**Corrección preparada** en `database/00_administracion/05_conceder_permisos_roles.sql`:
- Se revoca `EXECUTE` de `PKG_ECOPUNTOS`, `PKG_DEPOSITO`, `PKG_CONTEXTO_APP` y `PR_CAMBIAR_ESTADO_CONTENEDOR`.
- Se conservan las funciones de consulta: `PKG_ESTADISTICAS`, `FN_PESO_ESTIMADO_KG` y `FN_DEPOSITOS_CIUDADANO`.
- Pasan a solo lectura para EDICION: `USUARIO`, `USUARIO_ROL`, `ROL`, `DEPOSITO`, `INTERACCION`, `VINCULO_TELEGRAM`,
  `CONVERSACION_TELEGRAM`, `CONSENTIMIENTO_VISION_EXTERNA`, `ANALISIS_IMAGEN` y `CONSULTA_VISION`.
- EDICION seguirá escribiendo en 12 tablas de catálogo y configuración: `MATERIAL`, `CONTENEDOR`, `CONTENEDOR_MATERIAL`,
  `CONFIGURACION_CANECA`, `PESO_REFERENCIAL`, `PUNTO_DISPOSICION`, `PUNTO_INSTALACION`, `LOTE_SIMULACION`,
  `REGISTRO_SIMULACION`, `OPINION`, `COMENTARIO_CIUDADANO` y `PRUEBA_COLABORATIVA`.

**Impacto exacto, sin aplicarlo.** El comando de abajo compila el mismo script contra la base real y solo imprime las
sentencias. Resultado del 21/11/2026 en
[`docs/evidencias/oracle_cloud/simulacion_permisos_edicion_2026-11-21.txt`](../evidencias/oracle_cloud/simulacion_permisos_edicion_2026-11-21.txt).

```powershell
dotnet run --project src\EcoAccess.P3.Instalador -- simular-permisos
```

**Aplicarlo**, solo con autorización del propietario. Lo ejecuta el dueño del esquema y no necesita ADMIN:

```powershell
dotnet run --project src\EcoAccess.P3.Instalador -- aplicar-permisos
dotnet run --project src\EcoAccess.P3.Instalador -- auditar
```

Después de aplicarlo, `auditar` debe mostrar EDICION con `EXECUTE` en 3 objetos e `INSERT`/`UPDATE` en 12 tablas.

**Ojo:** el instalador vuelve a ejecutar `05_conceder_permisos_roles.sql` al final de cada `instalar`. Si este cambio se
fusiona en `main` sin aplicarlo antes, la próxima instalación lo aplicará.

## 10. Instalación automática en un computador nuevo (script)

`scripts/instalacion/Configurar-Integrante.ps1` hace todo en orden y se puede repetir:

1. Comprueba los requisitos.
2. Compila la solución.
3. Revisa el `Wallet_*.zip` sin mostrar su contenido.
4. Instala el wallet, la aplicación y el acceso directo.
5. Te guía en SQL Developer.
6. Ejecuta `comprobar-cuenta`.
7. Ejecuta `prueba-colaborativa`.
8. Configura la conexión de la aplicación C#.
9. Ejecuta las pruebas unitarias.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1 -Usuario ECO_HEYDER -Wallet "$HOME\Downloads\Wallet_EcoAccess.zip"
```

| Requisito | Versión |
|---|---|
| Windows | 10 u 11 |
| .NET Framework | **4.8** (ejecución) y **4.8 Developer Pack** (compilación) |
| SDK de .NET | **10.0.100 o posterior de la versión 10** (lo fija `global.json`; compruébalo con `dotnet --version`) |
| Visual Studio | 2022 o posterior, carga «Desarrollo de escritorio de .NET» |
| SQL Developer | 23 o posterior |

**Resultado:**
- Aplicación instalada en `%LOCALAPPDATA%\Programs\EcoAccess P3\EcoAccess.P3.Presentacion.exe`, con el acceso directo
  «EcoAccess P3» en el escritorio.
- Wallet en `%LOCALAPPDATA%\EcoAccessP3\wallet`.
- Conexión de la aplicación en `%APPDATA%\Microsoft\UserSecrets\ecoaccess-p3-framework\secrets.json`.

Nada de esto queda en el repositorio.

**Actualizar EcoAccess más adelante:**

```powershell
git pull
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1
```

## 11. Instrucciones exactas para Heyder

1. Clona el repositorio, o actualízalo si ya lo tienes, y cámbiate a la rama del PR mientras no se fusione:

   ```powershell
   git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git
   cd EcoAccess-2026-2
   git switch feature/oracle-cloud-integrantes
   ```

   Cuando el PR #21 esté fusionado, usa `git switch main` y `git pull`.
2. Ten a mano el `Wallet_EcoAccess.zip` y la contraseña temporal de `ECO_HEYDER`, que te entrega Jorge en persona o por
   un canal privado.
3. Ejecuta:

   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1 -Usuario ECO_HEYDER -Wallet "<ruta de tu Wallet_EcoAccess.zip>"
   ```

4. Cuando el script lo pida, crea en SQL Developer la conexión `EcoAccess Cloud - ECO_HEYDER` (Cloud Wallet, servicio
   `ecoaccess_tp`) y pulsa **Test**. Oracle te pide cambiar la contraseña temporal por una tuya.
5. En `comprobar-cuenta` y `prueba-colaborativa` escribe **tu** contraseña nueva.
6. Para la conexión de la aplicación, Jorge escribe la contraseña de `ECOACCESS_P3_APP`, en persona o por control
   remoto. Si no está, responde «n» y repite después:

   ```powershell
   dotnet run --project src\EcoAccess.P3.Instalador -- guardar-conexion --wallet "$env:LOCALAPPDATA\EcoAccessP3\wallet"
   ```

7. Abre EcoAccess desde el acceso directo «EcoAccess P3».

## 12. Instrucciones exactas para Saray

Los mismos pasos que Heyder, cambiando `ECO_HEYDER` por **`ECO_SARAY`**:

```powershell
git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git
cd EcoAccess-2026-2
git switch feature/oracle-cloud-integrantes
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1 -Usuario ECO_SARAY -Wallet "<ruta de tu Wallet_EcoAccess.zip>"
```

En SQL Developer la conexión se llama `EcoAccess Cloud - ECO_SARAY`.

## 13. INSTALACIÓN CON IA LOCAL — CLAUDE CODE, CODEX O ANTIGRAVITY

Cada integrante puede pedirle a su asistente de IA, abierto en la carpeta del repositorio, que configure EcoAccess en
**su propio** computador.

**Reglas que la IA debe respetar:**
- Las contraseñas las escribe la persona en la consola; la IA no las pide por chat ni las guarda.
- El wallet no se copia dentro del repositorio.
- Nada de esto se sube a GitHub.

Pega este mensaje (cambia `ECO_HEYDER` por tu cuenta y la ruta del wallet):

```text
Estoy en el repositorio EcoAccess-2026-2 (rama feature/oracle-cloud-integrantes o main si ya se fusionó el PR #21).
Configura EcoAccess en este computador siguiendo docs/oracle-cloud/05_ACCESO_INTEGRANTES.md:
1. Verifica requisitos con: powershell -ExecutionPolicy Bypass -File scripts\instalacion\Validar-Entorno.ps1
2. Abre una terminal interactiva para mí y ejecuta:
   powershell -ExecutionPolicy Bypass -File scripts\instalacion\Configurar-Integrante.ps1 -Usuario ECO_HEYDER -Wallet "C:\Users\<yo>\Downloads\Wallet_EcoAccess.zip"
   Yo escribiré las contraseñas en esa terminal; no me las pidas por chat, no las guardes y no las imprimas.
3. No copies el wallet dentro del repositorio ni hagas commit de secrets.json, tnsnames.ora, sqlnet.ora o cwallet.sso.
4. Si falla, léeme el error y consulta la tabla «Diagnóstico de errores frecuentes» de la guía antes de cambiar nada.
5. Al terminar, muéstrame el resumen del script y confirma que el acceso directo «EcoAccess P3» abre la aplicación.
```

**Comandos reales que la IA usará** (todos existen en el repositorio):

| Comando | Para qué |
|---|---|
| `scripts\instalacion\Validar-Entorno.ps1` | revisa .NET 4.8, Visual Studio, el SDK y el wallet |
| `scripts\instalacion\Configurar-Integrante.ps1` | instalación completa del integrante |
| `scripts\instalacion\Instalar-EcoAccess.ps1 -Wallet <zip>` | wallet, aplicación y acceso directo |
| `dotnet run --project src\EcoAccess.P3.Instalador -- comprobar-cuenta --usuario ECO_X --wallet <carpeta>` | permisos de tu cuenta |
| `dotnet run --project src\EcoAccess.P3.Instalador -- prueba-colaborativa --usuario ECO_X --wallet <carpeta>` | prueba con la base compartida |
| `dotnet run --project src\EcoAccess.P3.Instalador -- guardar-conexion --wallet <carpeta>` | conexión de la aplicación (la contraseña la escribe Jorge) |
| `dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug` | pruebas unitarias |

Si la terminal de la IA no permite escribir contraseñas ocultas, la IA debe pedirte que ejecutes el comando en tu propia
ventana de PowerShell.
