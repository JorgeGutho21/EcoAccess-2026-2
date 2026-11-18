# Conexión de C# con Oracle Cloud — resultados verificados

Base `EcoAccess` · Autonomous Database **Oracle 19c EE**, Always Free, `sa-bogota-1` · PDB `G5AB3C4722E812D_ECOACCESS` · esquema `ECOACCESS_P3_APP` · servicio `ecoaccess_tp` · mTLS con wallet en `%USERPROFILE%\.oracle\wallets\EcoAccess` · sin Docker.

## 1. Cómo se conecta

**Presentación** (`Program.cs`, `FormActivacionOracle`) → **Lógica** (servicios) → **Datos** (`FabricaConexionesOracle`, repositorios, `WalletOracleNube`) → **Oracle Cloud**.

* Cadena: User Secrets `ecoaccess-p3-framework`, clave `ConnectionStrings:OracleP3` (o la variable `ECOACCESS_ConnectionStrings__OracleP3`). Nunca en el código ni en Git.
* `WalletOracleNube.CrearCadena` produce `User Id=…;Password=…;Data Source=ecoaccess_tp;Connection Timeout=30;Tns_Admin=<wallet>;Wallet_Location=<wallet>` y funciona con `Oracle.ManagedDataAccess` 23.26 en .NET Framework 4.8 (prueba `CrearCadena_UsaAliasYWalletYEsReconocidaPorOdpNet`).
* Variable de usuario `TNS_ADMIN` = carpeta del wallet (útil para SQL Developer y otras herramientas).
* La configuración inicial de Windows Forms comprueba **wallet → red → base → credenciales** y guarda la cadena solo cuando Oracle la acepta (máximo 3 intentos por ventana).

## 2. Validaciones obligatorias

| # | Validación | Evidencia | Resultado |
|---|---|---|---|
| 1 | Apertura de conexión | instalador (`plan`), bot `--comprobar-oracle` (40 materiales), aplicación | correcta |
| 2 | Registro de un usuario de prueba | `ServicioRegistroCiudadanos` + repositorio Oracle | `id_usuario = 244`, `prueba.nube.20261109035740@example.invalid` |
| 3 | Lectura | conexión nueva sin pool | nombre, correo, activo, rol ciudadano, credencial PBKDF2 |
| 4 | Actualización autorizada | `UPDATE` en transacción limitado a `@example.invalid`, leído con `RepositorioUsuariosOracle` | «Ciudadano Prueba Nube Editado» |
| 5 | Transacciones | `ROLLBACK` | el cambio no persistió |
| 6 | Manejo de errores | correo repetido, contraseña incorrecta | rechazados con mensaje claro |
| 7 | Restricciones | nombre de 1 letra; categoría 4 en `CONFIGURACION_CANECA` | `ORA-02290` en ambos |
| 8 | Concurrencia | 10 sesiones simultáneas | correctas, 1,2 s en total |
| 9 | Reconexión | `BridgeOracleTests.ConcurrenciaYRecuperacion` | superadas |
| 10 | Persistencia tras reiniciar | la aplicación se cerró y reabrió; el instalador releyó 25 versiones | correcta |

Las validaciones 2–6 y 8 se ejecutaron con el código de la línea EcoAccess_P3 sobre **la misma base y el mismo esquema**; las demás con EcoAccess-P3.

## 3. Pruebas automatizadas de EcoAccess-P3 contra Oracle Cloud

| Proyecto | Superadas | Con error | Omitidas |
|---|---|---|---|
| `EcoAccess.P3.Datos.Tests` (integración Oracle + instalador + wallet) | **225** + 1 de tres canecas | 0 | 5 (4 de visión real que requieren modelo y corpus; 1 marcada exclusiva del esquema aislado PRUEBAP3) |
| `EcoAccess.P3.Tests` (lógica, bot, arquitectura) | **1 638** | 0 | 1 (enlaces simbólicos de Windows) |

Con el modelo instalado se ejecutaron después 3 de las 4 de visión (2 superadas, 1 bloqueada por un dato de demostración que el proyecto restringe al esquema aislado; detalle en `docs/vision/LFM3VL_RECUPERACION_Y_OPTIMIZACION.md`).

## 4. Aplicación de escritorio y bot sin Docker

* `EcoAccess.P3.Presentacion.exe` (Debug y Release) abre **«EcoAccess · Bienvenida»** sin pasar por la configuración inicial: la comprobación real con Oracle Cloud fue correcta. No hay procesos de Docker.
* `EcoAccess.P3.Bot.exe --comprobar-telegram`: `@EcoAccessP3_JG_bot`, sin webhook. `--polling`: identidad comprobada, consumidor único.
