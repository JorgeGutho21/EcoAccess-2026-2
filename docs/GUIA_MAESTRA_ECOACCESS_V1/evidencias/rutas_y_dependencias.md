# Rutas y dependencias

| Componente | Ruta de verificación |
| --- | --- |
| Solución y targets | `EcoAccess.P3.sln`, `Directory.Build.props`, proyectos en `src` |
| Clases de negocio | `src/EcoAccess.P3.Logica/Servicios`, `src/EcoAccess.P3.Logica/Contratos`, `src/EcoAccess.P3.Entidades` |
| Oracle | `src/EcoAccess.P3.Datos/Oracle`, `src/EcoAccess.P3.Datos/Repositorios`, `database/06_migraciones` |
| Bot y visión | `src/EcoAccess.P3.Bot/Program.cs`, `src/EcoAccess.P3.Bot/BuclePolling.cs`, `src/EcoAccess.P3.Bot/Telegram`, `src/EcoAccess.P3.Bot/Vision` |
| Registro | `scripts/instalacion/Publicar-Registro-HTTPS.ps1`, `src/EcoAccess.P3.Bot/ServidorRegistro.cs` |
| Arranque | `scripts/desarrollo`, `scripts/instalacion` |
| Hardware | `hardware/estacion_tres_canecas`, `hardware/calibracion_seis_servos`, `hardware/pruebas` |
| Informes | `reportes/powerbi/EcoAccess.pbix`, `reportes/powerbi/EcoAccessP3.pbip`, `reportes/powerbi/Validacion.sql` |
| Pruebas | `tests/unitarias/EcoAccess.P3.Tests`, `tests/integracion/EcoAccess.P3.Datos.Tests`, `.github/workflows/compilacion.yml` |
| Inventario de tablas | `docs/academico/inventario/oracle_tablas.txt`, `docs/academico/inventario/oracle_columnas.txt` y V026 |
| Material académico | `docs/academico`, `docs/entrega/INDICE_DOCUMENTAL_CIERRE.md` |

## Versiones observadas en el equipo

| Elemento | Revisión |
| --- | --- |
| Git for Windows | 2.56.0.windows.4 |
| PowerShell | 7.6.9 |
| Visual Studio Community | 18.11.12328.437 |
| SDK .NET | 10.0.403 |
| Framework | Runtime 4.8.09221 y Developer Pack 4.8 |
| NuGet | ODP.NET 23.26.400, QRCoder 1.8.1, SkiaSharp 4.153.1, MSTest 4.0.2. Los csproj son la autoridad |
| Ollama | 0.40.16, lfm3-vl:latest |
| Tailscale | 1.104.5 |
| Arduino IDE | 2.3.10, núcleo local 3.3.12 y núcleo CI 3.0.7 |
| Power BI Desktop | 2.159.1231.0 |

## Origen de capturas

- Telegram: `docs/evidencias/registro_movil/01_crear_cuenta_375px.jpg`.
- Windows Forms: `docs/evidencias/winforms/accesos/01_inicio_tres_accesos.png`.
- Consulta Oracle desde Windows Forms: `docs/evidencias/ejecucion_real/03_busqueda_botella_en_oracle.jpg`. No corresponde a la consola OCI.
- Power BI: `docs/evidencias/powerbi_p3/02_tres_canecas.jpg`.

Los diagramas nuevos representan dependencias, relaciones y flujo a partir del código. El cableado es un esquema funcional del ESP32 y tres señales, con especificaciones eléctricas pendientes de inspección física.
