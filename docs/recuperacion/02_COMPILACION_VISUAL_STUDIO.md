# 02 · Compilación en Visual Studio

Visual Studio 2026 Community · .NET Framework 4.8 (C# 9, `Directory.Build.props`) · SDK 10.0.403 para compilar · rama `recovery/ecoaccess-oracle-cloud`.

| Comprobación | Resultado |
|---|---|
| Restauración NuGet | correcta |
| `dotnet build EcoAccess.P3.sln -c Release --no-incremental` (inicio) | correcta, **0 errores**, 6 advertencias del analizador de MSTest en pruebas existentes |
| Ídem al final de la recuperación | correcta, 0 errores |
| `EcoAccess.P3.Tests` | 1 638 superadas, 0 con error, 1 omitida |
| `EcoAccess.P3.Datos.Tests` contra Oracle Cloud | 225 + 1 superadas, 0 con error, 5 omitidas (justificadas) |

No hubo errores de compilación que corregir: todas las dependencias son paquetes NuGet. Proyecto nuevo: `EcoAccess.P3.Instalador` (consola, net48), registrado en la prueba de arquitectura.

## Abrir y ejecutar

1. Abrir `EcoAccess.P3.sln`.
2. Proyecto de inicio: **EcoAccess.P3.Presentacion** (escritorio) o **EcoAccess.P3.Bot** (Telegram).
3. *Compilar → Recompilar solución*; *Prueba → Ejecutar todas las pruebas*.
4. **F5**. Si el equipo no tiene la conexión guardada, aparece «Configuración inicial (Oracle Cloud)».

Si el bot está en ejecución bloquea los binarios de `Release`: compilar en `Debug` o detenerlo antes.
