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

## Instalación recomendada (un comando)

Desde 17/11/2026 la base oficial es Oracle Cloud y **Docker no es necesario**. El instalador hace los pasos manuales
de abajo (NuGet, compilación, wallet con `sqlnet.ora` corregido, acceso directo «EcoAccess P3» con logo y pruebas de humo):

- **Si solo vas a usar la aplicación:** pide al responsable el paquete `EcoAccess-P3-<commit>.zip` (lo genera
  `scripts\instalacion\Publicar-Paquete.ps1`) y el `Wallet_*.zip`, descomprime el paquete y ejecuta
  `powershell -ExecutionPolicy Bypass -File .\Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"`. No necesitas Visual Studio.
- **Si programas:** `git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git EcoAccess-P3`, `cd EcoAccess-P3`,
  `git config core.longpaths true` y `powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Wallet "<ruta>"`.
  Para actualizar: `git switch main`, `git pull` y vuelve a ejecutar el instalador.

La primera vez que abras «EcoAccess P3», la pantalla «Configuración inicial» muestra *Wallet de Oracle Cloud* y pide la
contraseña del esquema que te entregó el responsable por un canal privado. Se comprueba con Oracle y se guarda solo en
tu perfil de Windows; desde entonces la aplicación abre directamente.

## Pasos manuales (equivalentes)

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
   la acepta se guarda en los User Secrets **de ese perfil**. El destino sale de `App.config`, que desde el 17/11/2026
   ya trae el alias de Oracle Cloud (`Data Source=ecoaccess_tp`); solo para desarrollo local se cambia por
   `localhost:1522/PRUEBAP3` o `localhost:1521/FREEPDB1` en el `.exe.config` del equipo, sin recompilar.

## Reglas para varios equipos a la vez

- Varios compañeros pueden usar Windows Forms y Oracle al mismo tiempo: cada uno abre su conexión del pool. La carga se
  midió con 10 y 20 personas simultáneas contra Oracle Cloud (`CargaUsuariosOracleTests`, 8/8).
- **Solo un equipo** ejecuta el bot de Telegram. Telegram entrega cada mensaje a quien lo pide primero: dos bots con el
  mismo token se reparten los mensajes. El validador avisa si ya hay un bot activo en el equipo.
- No copiar User Secrets, wallet ni `bridge.local.json` al repositorio.

## Reproducir las pruebas

Ninguna prueba necesita las canecas, COM3 ni los servos.

| Qué | Comando | Necesita |
|---|---|---|
| Unitarias (2 150+) | `dotnet test tests/unitarias/EcoAccess.P3.Tests -c Release` | nada más que compilar |
| Integración con Oracle aislado | `pwsh -File scripts/pruebas/Ejecutar-Integracion-Esquema.ps1` | un esquema de pruebas propio (`Crear-Esquema-Aislado-Local.ps1` o Docker); nunca el de producción |
| Visión con el modelo real | variables `ECOACCESS_PROBAR_VISION_INTEGRADA=1` y `ECOACCESS_PROBAR_VISION_REAL=1` | Ollama con `lfm3-vl:latest` en 127.0.0.1 |
| Banco de 915 fotos | `EcoAccess.P3.Bot.exe --evaluar-objetos <manifiesto> local` con `ECOACCESS_SIN_HARDWARE=1` | las fotos de Commons (no están en Git; ver `docs/vision/resultados/banco_ampliado_v1/banco.csv`) |
| Ventanas y escalado | `tests/visual/ArnesVentanas` (ver `docs/evidencias/winforms_ventanas/LEEME.md`) | Windows con escritorio |

Cada compañero usa **sus propias credenciales**: User Secrets de su perfil (`Configurar-Credenciales.ps1`), su wallet y,
si corre el bot, un token de prueba distinto del oficial. Nada de eso va al repositorio.

## Estado de esta guía

- 09/11/2026: probada en el equipo de Jorge (Windows 11 Pro, Visual Studio 18 Community, .NET SDK 10.0.403).
- 15/11/2026: verificada desde una **carpeta limpia** en el mismo equipo, sin rutas privadas: `git clone` de la rama
  `desarrollo/telegram-doble-ia` desde GitHub, `dotnet build EcoAccess.P3.sln -c Release` correcto y pruebas
  unitarias 2 156 correctas, 0 fallidas, 1 omitida (enlace simbólico, requiere privilegio).
- 17/11/2026: `Instalar-EcoAccess.ps1` probado en el equipo de Jorge: 5/5 comprobaciones OK, el acceso directo abre
  «EcoAccess · Bienvenida» desde `%LOCALAPPDATA%\Programs\EcoAccess P3` sin consola; la instalación del wallet se probó
  con un zip de prueba sin claves (corrige `sqlnet.ora`).
- **Pendiente**: ejecutarla en un segundo computador de otro integrante; hasta entonces no se declara probada en otros equipos.

## Rediseño de la interfaz (ExpoSoftware)

Tras `git pull` no hay pasos extra: las imágenes están en `assets/imagenes/` y se incrustan al compilar, y no hay migraciones
nuevas. Qué cambió, cómo probarlo y errores frecuentes: [../desarrollo/REDISENO_EXPO_CANECAS.md](../desarrollo/REDISENO_EXPO_CANECAS.md).
