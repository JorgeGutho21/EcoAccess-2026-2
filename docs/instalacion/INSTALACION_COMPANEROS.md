# Instalar EcoAccess P3 en otro computador del equipo

EcoAccess P3 · Universidad Popular del Cesar · Programación de Computadores III · Jorge Gutierrez Thomas y Heyder Canchila.

Guía corta para que otro integrante abra la aplicación de escritorio contra la misma base Oracle Cloud. El detalle de
cada componente está en `11_instalacion.md`; aquí solo el orden y lo que cambia por equipo.

## Qué pide permisos de administrador y qué no

| Paso | Administrador |
|---|---|
| .NET Framework 4.8 y Developer Pack 4.8 | Sí (una vez) |
| Visual Studio 2022 o posterior con «Desarrollo de escritorio de .NET» | Sí (una vez) |
| Clonar, compilar, ejecutar, pruebas | No |
| Wallet de Oracle Cloud, variable `TNS_ADMIN`, User Secrets | No (perfil del usuario) |

## Pasos

1. Clonar el repositorio autorizado y abrir una consola en su carpeta.
2. Pedir al propietario el **wallet** de Oracle Cloud por un canal privado (nunca por el repositorio ni el chat del
   grupo). Descomprimirlo en una carpeta propia, por ejemplo `%USERPROFILE%\.oracle\wallets\EcoAccess`.
3. En `sqlnet.ora` de esa carpeta cambiar `DIRECTORY="?/network/admin"` por la ruta real de la carpeta.
4. Crear la variable de usuario `TNS_ADMIN` con esa ruta (Configuración → Variables de entorno del usuario) y abrir
   una consola nueva.
5. Comprobar el equipo (solo lee, no instala nada):

   ```bash
   pwsh -File scripts/instalacion/Validar-Entorno.ps1
   ```

6. Compilar:

   ```bash
   dotnet build EcoAccess.P3.sln -c Release
   ```

7. Comprobar Oracle con una consulta real de solo lectura:

   ```bash
   pwsh -File scripts/instalacion/Validar-Entorno.ps1 -ProbarOracle
   ```

   Debe terminar en «Componentes obligatorios disponibles» y mostrar cuántos materiales devolvió Oracle.
8. Ejecutar `src\EcoAccess.P3.Presentacion\bin\Release\net48\EcoAccess.P3.Presentacion.exe` (o F5 en Visual Studio con
   `EcoAccess.P3.Presentacion` como proyecto de inicio). La primera vez pide la contraseña del esquema; solo si Oracle
   la acepta se guarda en los User Secrets **de ese perfil**. El destino sale de `App.config`: para Oracle Cloud cambiar
   `Data Source` por el alias del wallet (`ecoaccess_tp`) sin recompilar, en el `.exe.config` del equipo.

## Reglas para varios equipos a la vez

- Varios compañeros pueden usar Windows Forms y Oracle al mismo tiempo: cada uno abre su conexión del pool. La carga se
  midió con 10 y 20 personas simultáneas contra Oracle Cloud (`CargaUsuariosOracleTests`, 8/8).
- **Solo un equipo** ejecuta el bot de Telegram. Telegram entrega cada mensaje a quien lo pide primero: dos bots con el
  mismo token se reparten los mensajes. El validador avisa si ya hay un bot activo en el equipo.
- No copiar User Secrets, wallet ni `bridge.local.json` al repositorio.

## Estado de esta guía

Probada en el equipo de Jorge (Windows 11 Pro, Visual Studio 18 Community, .NET SDK 10.0.403) el 09/11/2026.
**Pendiente**: ejecutarla en un segundo computador; hasta entonces no se declara probada en otros equipos.
