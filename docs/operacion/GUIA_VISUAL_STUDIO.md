# Abrir EcoAccess P3 en Visual Studio sin errores

## Síntoma (11/11/2026)

Visual Studio 2026 (versión 18.11.3) abre `EcoAccess.P3.sln`, pero los 9 proyectos aparecen como **«Descargado»**, el
botón de inicio dice «Adjuntar...» y la solución muestra «0 de 9 proyectos». La compilación por consola
(`dotnet build`) sí funciona.

## Diagnóstico

| Paso | Resultado |
|---|---|
| `devenv.com EcoAccess.P3.sln /build Debug` | los 9 proyectos fallan con `El tipo de resolución del SDK "DotNetMSBuildSdkResolver" no se pudo cargar. Se produjo una excepción en el inicializador de tipo de 'Microsoft.DotNet.DotNetSdkResolver.VSSettings'` |
| MSBuild de Visual Studio con un proyecto **vacío** fuera del repositorio | el mismo error (MSB4237): **no es un problema de EcoAccess** |
| Programa de diagnóstico que carga `Microsoft.DotNet.MSBuildSdkResolver.dll` | excepción interna: `UnauthorizedAccessException`: no se pudo obtener el componente COM `{177F0C4A-1CD3-4DE7-A32C-71DBBB9FA36D}` (Visual Studio Setup Configuration), **80070005 Acceso denegado** |
| Registro de ese componente | apunta a `C:\ProgramData\Microsoft\VisualStudio\Setup\x64\Microsoft.VisualStudio.Setup.Configuration.Native.dll` |
| Permisos | el usuario de Windows **no puede leer** `C:\ProgramData\Microsoft\VisualStudio` (ni siquiera su ACL), aunque el proceso no esté aislado |
| `vswhere.exe` | no devuelve ninguna instalación, por la misma causa |

**Causa:** los permisos NTFS de la carpeta de sistema `C:\ProgramData\Microsoft\VisualStudio` impiden al usuario leer el
registro de instalaciones de Visual Studio. Sin él, el resolutor de SDK de .NET no se inicializa y Visual Studio no puede
cargar ningún proyecto de estilo SDK (todos los de EcoAccess lo son).

**Lo que no es la causa:** el `.sln`, los `.csproj`, `Directory.Build.props`, `global.json` (SDK 10.0.100 con
`latestFeature`; está instalado el 10.0.403), NuGet ni el estado `.vs`.

## Corrección (requiere al propietario: permisos de administrador)

Cambiar permisos de una carpeta del sistema exige elevar privilegios. El agente no lo hace por usted. El 11/11/2026 dejó
abierto el **Instalador de Visual Studio**.

1. En el Instalador de Visual Studio, en *Visual Studio Community 2026*, pulse **Más > Reparar** y acepte el aviso de
   Control de cuentas de usuario. La reparación restablece los archivos y permisos de `C:\ProgramData\Microsoft\VisualStudio`.
2. Si la reparación no basta, abra **PowerShell como administrador** y restablezca los permisos heredados:
   ```powershell
   icacls "C:\ProgramData\Microsoft\VisualStudio" /reset /T /C
   ```
3. Compruebe que `vswhere` vuelve a ver la instalación (debe imprimir una ruta):
   ```powershell
   & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" -latest -property installationPath
   ```
4. Abra la solución con el acceso **Abrir EcoAccess P3 en Visual Studio**. Si algún proyecto sigue «Descargado», haga
   clic derecho en la solución > **Volver a cargar todos los proyectos**.

## Después de la corrección

- **Proyecto de inicio:** `EcoAccess.P3.Presentacion` ya es el primero de la solución, así que Visual Studio lo toma por
  defecto. Si no, haga clic derecho sobre él > *Establecer como proyecto de inicio*.
- **Compilar:** Ctrl+Mayús+B en Debug y en Release. Por consola, ambas compilaciones dan 0 errores.
- **Ejecutar:** F5 abre la ventana de bienvenida e inicio de sesión. Escriba su correo y contraseña.
- **Pruebas:** Prueba > Explorador de pruebas. Debe listar las pruebas unitarias (1 770) y las de integración.
- **Oracle:** la aplicación usa los User Secrets `ecoaccess-p3` y el wallet de `TNS_ADMIN`. Puede comprobarlo con
  `EcoAccess.P3.Bot.exe --comprobar-oracle`.

## Estado del estado local `.vs`

El 11/11/2026 se respaldó `.vs\EcoAccess.P3\v18\.suo` como `.suo.respaldo-20261111`. Eso no resolvió el problema, porque
la causa no estaba ahí. El respaldo puede borrarse cuando Visual Studio cargue bien.
