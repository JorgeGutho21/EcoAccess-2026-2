# Cómo abrir EcoAccess P3 desde cero

Guía corta para el propietario y para el equipo (Jorge Gutiérrez Thomas, Heyder Canchila y Saray Cadena). Ninguno de los
pasos guarda contraseñas: cada persona escribe la suya cuando la aplicación se la pide.

## 1. Qué aplicación abrir

En el Escritorio de Windows hay **un solo acceso, «EcoAccess P3»**, con el logo. Lo crea
`scripts/instalacion/Instalar-EcoAccess.ps1` y abre la copia instalada en `%LOCALAPPDATA%\Programs\EcoAccess P3`.
El código está en **una sola carpeta**: `Programación de Computadores III-SS462-\EcoAccess-P3` (desde el 17/11/2026 ya no
está dentro de `ProyectoDeAula_EcoAccess`). Para abrir el código en Visual Studio: `scripts/desarrollo/Abrir-EcoAccess-VisualStudio.ps1`
o doble clic en `EcoAccess.P3.sln`.

Al abrir, la aplicación comprueba el wallet de Oracle y la credencial guardada en el perfil de Windows; si falta algo,
muestra la pantalla de configuración y dice qué hacer.

## 2. Cómo abrir Visual Studio y qué solución elegir

1. Haga doble clic en `EcoAccess.P3.sln` de la carpeta `EcoAccess-P3`, o en Visual Studio elija **Archivo > Abrir > Proyecto o solución**.
2. La solución correcta es:
   `Documents\01_UNIVERSIDAD\2026-2\Programación de Computadores III-SS462-\EcoAccess-P3\EcoAccess.P3.sln`
3. **No** abra la de `EcoAccess-P3-auditoria`, `EcoAccess-P3-Nube` ni `EcoAccess_V2`.

## 3. Qué proyecto ejecutar

- Aplicación de escritorio: **`EcoAccess.P3.Presentacion`**. En el Explorador de soluciones, clic derecho > **Establecer como proyecto de inicio**. Basta con hacerlo una vez.
- Bot de Telegram: `EcoAccess.P3.Bot`. Normalmente **no** se ejecuta desde Visual Studio, porque lo mantiene vivo la tarea vigilante (punto 14).

## 4. Cómo iniciar Windows Forms

Pulse **F5** (con depuración) o **Ctrl+F5** (sin depuración). Para compilar sin ejecutar, use **Ctrl+Mayús+B**. La
configuración **Release** es la que usan los accesos directos.

## 5. Cómo iniciar sesión

Escriba su correo y su contraseña en `FormInicioSesion`.

- **La primera vez en una base nueva** aparece la **configuración inicial**, que crea el primer administrador. Solo funciona una vez.
- Después de cinco intentos fallidos, la cuenta se bloquea 15 minutos.

## 6. Cómo abrir el panel administrativo

Con una cuenta Administrador, el menú lateral de `FormPrincipal` muestra:

- **Ciudadanos y usuarios**, con tres pestañas: «Crear cuenta», «Cuenta y roles» y «Acceso inicial».
- Materiales, canecas, reglas de ecopuntos y diagnóstico de Oracle.

## 7. Cómo acceder al dashboard

Menú **Dashboard institucional**. Lo ven el operario y el administrador.

- El **modo operativo** muestra solo datos reales: hoy son 0.
- El **modo experimental** muestra el lote `SIM-2026-11-CIERRE`.

## 8. Cómo iniciar Telegram

El bot `@EcoAccessP3_JG_bot` se inicia solo al iniciar sesión en Windows (tarea vigilante). Para iniciarlo a mano:

```powershell
pwsh -File scripts\desarrollo\Iniciar-EcoAccess-Nube.ps1
```

Para comprobar la identidad del bot sin enviar mensajes:

```powershell
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-telegram
```

## 9. Cómo comprobar Ollama

```powershell
ollama list                      # debe aparecer lfm3-vl:latest
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --estado-vision
```

## 10. Cómo verificar Oracle Cloud

```powershell
src\EcoAccess.P3.Bot\bin\Release\net48\EcoAccess.P3.Bot.exe --comprobar-oracle
```

La comprobación es de solo lectura. Si falla, revise `TNS_ADMIN` (wallet) y los secretos (`Configurar-Credenciales.ps1`).

## 11. Cómo abrir Power BI

Desde la aplicación: menú **Análisis en Power BI**. Abre `reportes\powerbi\EcoAccess.pbix` si existe. Ese archivo aún no
está construido; los pasos están en `reportes/powerbi/LEEME.md`. **No** confunda este informe con `EcoAccess_BD`, que es de
otra asignatura.

## 12. Cómo ejecutar las pruebas

En Visual Studio: **Prueba > Explorador de pruebas > Ejecutar todas**. Por consola:

```powershell
dotnet build EcoAccess.P3.sln -c Debug
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug --no-build
```

Las pruebas de integración necesitan la variable `ECOACCESS_ConnectionStrings__OracleP3`. Use el esquema aislado; el
procedimiento está en el documento 13.

## 13. Cómo comprobar el vigilante

```powershell
Get-ScheduledTask -TaskName 'EcoAccess P3 - vigilante del bot' | Get-ScheduledTaskInfo
Get-Content C:\ProgramData\EcoAccessP3Framework\Desarrollo\vigilante.log -Tail 20
```

Si el bot se cierra, el vigilante lo relanza en un máximo de 5 minutos. **El vigilante no hace que EcoAccess esté
disponible 24/7:** si el PC se apaga o se cierra la sesión de Windows, el bot se detiene.

## 14. Errores comunes

| Síntoma | Causa | Solución |
|---|---|---|
| «No se pudo conectar con la base de datos» | sin internet, `TNS_ADMIN` mal o wallet vencido | `--comprobar-oracle`; revise el wallet |
| «Correo o contraseña incorrectos» | dato errado o cuenta bloqueada 15 min | espere o pida a un administrador revisar la cuenta |
| El bot no responde | proceso detenido o dos bots con el mismo token | revise el vigilante; debe haber **un solo** `EcoAccess.P3.Bot.exe` |
| La compilación Release falla al copiar archivos | el bot en ejecución bloquea su `.exe` | detenga el bot, compile y deje que el vigilante lo relance |
| El bot dice «no estoy seguro» con una foto | foto borrosa o material difícil | tome la foto con buena luz y el objeto solo |

## 15. Cómo cerrar correctamente el sistema

1. Cierre la aplicación de escritorio con **Cerrar sesión** y luego la X.
2. Deje el bot corriendo si quiere que Telegram responda. Para detenerlo por completo, primero deshabilite la tarea (si no, el vigilante lo relanza):
   ```powershell
   Disable-ScheduledTask -TaskName 'EcoAccess P3 - vigilante del bot'
   Get-Process EcoAccess.P3.Bot | Stop-Process
   ```
   Para reactivarlo: `Enable-ScheduledTask -TaskName 'EcoAccess P3 - vigilante del bot'`.
3. Cierre Visual Studio después de guardar.

## Cuentas del equipo

Las cuentas y sus contraseñas iniciales están en un archivo **local y privado**, fuera del repositorio:
`%LOCALAPPDATA%\EcoAccessP3\privado\CUENTAS_EQUIPO_LOCAL.txt`. Solo puede leerlo el usuario de Windows del propietario.
Nunca lo suba a GitHub ni lo envíe por Telegram.
