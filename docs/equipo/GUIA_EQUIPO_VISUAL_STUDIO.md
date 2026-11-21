# EcoAccess P3: obtener, compilar y ejecutar la versión del equipo

Guía para cualquier integrante. No usa rutas de ningún computador en particular: donde dice `<carpeta>` elige la tuya.
Repositorio (privado, necesitas ser colaborador): <https://github.com/JorgeGutho21/EcoAccess-2026-2>

## 0. Qué necesitas

| Necesitas | Para qué | Cómo |
|---|---|---|
| Git para Windows | clonar y actualizar | <https://git-scm.com/download/win> |
| Visual Studio 2022 o 2026 con la carga **Desarrollo de escritorio de .NET** | abrir y depurar la solución | Visual Studio Installer |
| **.NET SDK 10** | `global.json` fija el SDK 10.0.100 (o un parche más nuevo) | <https://dotnet.microsoft.com/download/dotnet/10.0> (Visual Studio 2026 ya lo trae) |
| Acceso de colaborador al repositorio | clonar | pídelo al responsable del proyecto |
| Wallet de Oracle Cloud y un usuario autorizado | conectar con la base | lo entrega el responsable por un canal privado, **nunca por GitHub** |
| ESP32 y canecas físicas | **opcional**: sin hardware funciona el modo demo | ver `hardware/README.md` |

No hace falta Docker ni instalar el «targeting pack» de .NET Framework 4.8: los ensamblados de referencia llegan por NuGet.

## 1. Clonar por primera vez

```powershell
cd <carpeta>
git clone https://github.com/JorgeGutho21/EcoAccess-2026-2.git EcoAccess-P3
cd EcoAccess-P3
git config core.longpaths true
```

`core.longpaths` es obligatorio: algunos archivos de `BaseDeDatos_Amilkar` superan 260 caracteres de ruta.

## 2. Actualizar un clon que ya tienes

```powershell
cd <carpeta>\EcoAccess-P3
git status                 # debe decir «working tree clean»; si no, guarda o descarta tus cambios primero
git switch main
git pull --ff-only
```

Si `git pull --ff-only` se niega, tienes commits locales en `main` que no están en GitHub: no los borres; crea una rama
con ellos (`git switch -c mi-trabajo`) y vuelve a `main` con `git switch main` y `git reset --hard origin/main` **solo si
ya pusiste tu trabajo a salvo en esa rama**.

Antes de compilar, cierra EcoAccess y el bot si los tienes abiertos desde esta carpeta: Windows bloquea sus DLL y la
compilación falla con «El archivo se ha bloqueado por EcoAccess.P3.…».

## 3. `main` o una rama de características

- **`main`** es la versión estable e integrada. Es la que se usa para estudiar, presentar y probar.
- Una **rama de características** (`feature/...`, `docs/...`) es trabajo en curso. Se publica en GitHub, se revisa en un
  *pull request* y solo entra a `main` cuando compila y pasan las pruebas (lo comprueba GitHub Actions).
- Para trabajar en algo nuevo: `git switch main`, `git pull --ff-only`, `git switch -c feature/mi-cambio`, haz commits y
  `git push -u origin feature/mi-cambio`; luego abre el *pull request* en GitHub. Nunca uses `git push --force` en ramas
  compartidas.

## 4. Abrir en Visual Studio

1. Visual Studio → **Abrir un proyecto o una solución** → `EcoAccess.P3.sln` (en la raíz del clon). Es la única solución.
2. Visual Studio restaura los paquetes NuGet solo. Si no: clic derecho en la solución → **Restaurar paquetes NuGet**.
3. Proyecto de inicio: **EcoAccess.P3.Presentacion** (clic derecho → *Establecer como proyecto de inicio*).
4. **Compilar → Compilar solución** (Debug o Release).

Desde la consola es lo mismo:

```powershell
dotnet restore EcoAccess.P3.sln
dotnet build EcoAccess.P3.sln -c Debug
dotnet build EcoAccess.P3.sln -c Release
```

Las imágenes (`assets/imagenes/`, `assets/identidad/`) se incrustan en el ejecutable al compilar: no hay que copiarlas.

## 5. Configurar Oracle con tus propios permisos

Cada integrante usa **su** usuario autorizado. Las contraseñas no van en el repositorio ni en `App.config`.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"
```

El instalador compila, instala el wallet en tu perfil y crea el acceso directo. La primera vez que abras la aplicación,
escribe la contraseña de tu usuario: se comprueba con Oracle y queda solo en tu perfil de Windows
(`%APPDATA%\Microsoft\UserSecrets\ecoaccess-p3-framework`). Las claves que existen están documentadas, sin valores
reales, en [secrets.ejemplo.json](secrets.ejemplo.json). Detalle: [INSTALACION_COMPANEROS.md](../instalacion/INSTALACION_COMPANEROS.md).

## 6. Ejecutar y probar

1. **F5** en Visual Studio (o el acceso directo «EcoAccess P3»).
2. **Login:** elige *Ciudadano*, *Operario* o *Supervisor* y entra con tu cuenta; o pulsa **Continuar como invitado**.
3. **Inicio del ciudadano:** debe verse completo, sin barras de desplazamiento, con ECO, el campus y las tres canecas.
   Abajo a la derecha de la fila de estado debe decir **Oracle conectado**.
4. **Búsqueda:** escribe `botellita` (blanca), `cáscara de banano` (verde), `servileta` (negra, con «¿Quisiste decir…?»),
   `vaso` (te pregunta si está limpio o sucio) y `pila` (manejo especial, sin botón de depósito).
5. **Canecas:** toca cada una; la tarjeta muestra qué recibe y «Ver los N residuos que recibe».
6. **Modo demo** (sin hardware): `scripts\desarrollo\Iniciar-Demostracion.ps1`. El inicio muestra
   **Estación simulada (demo)** y el asistente de depósito sigue la apertura simulada. Para detener:
   `scripts\desarrollo\Detener-Demostracion.ps1`.
7. **Estación física** (opcional): `scripts\desarrollo\Iniciar-Estacion-Fisica.ps1`, solo con el ESP32 conectado,
   calibrado y una persona responsable presente (ver [guía de integración](../hardware/GUIA_INTEGRACION_ESP32_TRES_CANECAS.md)).

Más detalle de estas pantallas: [INICIO_CIUDADANO_CATALOGO_DEPOSITOS.md](../desarrollo/INICIO_CIUDADANO_CATALOGO_DEPOSITOS.md).

## 7. Versión compilada (sin Visual Studio)

Quien solo usa la aplicación descarga `EcoAccess-P3-<commit>.zip` de las *Releases* privadas del repositorio, lo
descomprime y ejecuta `Instalar-EcoAccess.ps1 -Wallet "<ruta del Wallet_*.zip>"` (instrucciones en su `LEEME.txt`).
El paquete contiene solo el ejecutable y sus bibliotecas: **ni wallet, ni contraseñas, ni tokens**.

| Pieza | Dónde | ¿En GitHub? |
|---|---|---|
| Código fuente | este repositorio | sí |
| Aplicación compilada | Release `EcoAccess-P3-<commit>.zip` | sí (privada) |
| Configuración local (contraseña, wallet, token) | tu perfil de Windows | **no, nunca** |
| Servicios externos | Oracle Cloud; Telegram (solo quien ejecuta el bot) | no |
| Hardware | ESP32 y tres canecas: **opcional**, el modo demo lo simula | no |

## 8. Pruebas

```powershell
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug        # no necesitan Oracle ni hardware
```

Las pruebas de **integración con Oracle** solo corren contra un **esquema aislado** (usuario `*_PRUEBAS` o PDB
`PRUEBAP3`). Desde el 21/11/2026 `BaseIntegracionOracle` se niega a ejecutarlas contra la base de la aplicación aunque tu
secreto apunte allí (quedan «omitidas» con un mensaje). Para ejecutarlas, con tu propio esquema de pruebas:

```powershell
$env:ECOACCESS_ConnectionStrings__OracleP3 = "<cadena de TU esquema *_PRUEBAS>"
dotnet test tests\integracion\EcoAccess.P3.Datos.Tests -c Debug
Remove-Item Env:ECOACCESS_ConnectionStrings__OracleP3
```

GitHub Actions compila Debug y Release y ejecuta las pruebas sin Oracle en cada *pull request* y en cada cambio de `main`.

## 9. Errores frecuentes

| Mensaje | Causa | Solución |
|---|---|---|
| `A compatible .NET SDK was not found` / `global.json` | Falta el SDK 10 | Instala .NET SDK 10 o Visual Studio 2026. |
| `MSB3027 ... bloqueado por EcoAccess.P3.Bot` (o `.Presentacion`) | La app o el bot siguen abiertos desde esa carpeta | Ciérralos (`scripts\desarrollo\Detener-Demostracion.ps1` para los servicios) y vuelve a compilar. |
| `Filename too long` al clonar | Rutas de más de 260 caracteres | `git config --global core.longpaths true` y vuelve a clonar. |
| Pastilla roja **Sin conexión con Oracle** | Sin internet, wallet ausente o contraseña no configurada | Repite el paso 5; comprueba que el wallet esté en `%LOCALAPPDATA%\EcoAccessP3\wallet`. |
| `ORA-01017` | Usuario o contraseña incorrectos | Vuelve a escribir la contraseña en la pantalla de configuración. |
| `ORA-12506`/`ORA-12541` o tiempo de espera | La IP no está autorizada en Oracle Cloud o no hay red | Pide al responsable que revise la lista de acceso de Autonomous Database. |
| Las imágenes no aparecen | Compilaste una copia vieja o fuera del repositorio | `git pull --ff-only` y recompila; las imágenes van incrustadas. |
| Integración «omitida: el destino no es un esquema de pruebas» | Es la protección de producción | Usa tu esquema aislado (sección 8). |
