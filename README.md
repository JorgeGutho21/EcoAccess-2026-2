# EcoAccess P3

Sistema inteligente de orientación y registro de residuos de la Universidad Popular del Cesar, asignatura Programación de Computadores III. El prototipo usa una estación de tres canecas (BLANCA aprovechables 1093, VERDE orgánicos 1400, NEGRA no aprovechables 1401, un ESP32 por tapa), Telegram, Windows Forms, Oracle y Power BI.

**Plataforma:** .NET Framework 4.8 · C# 9 · Oracle Database · Telegram Bot API · Ollama con LFM3-VL · ESP32 (Arduino/C++).

## Modalidades

- **Telegram por QR:** el ciudadano escanea el código, envía una foto y, si el residuo y su estado lo permiten, Eco autoriza solo la caneca que corresponde durante 10 segundos, sin confirmación, según `docs/telegram/FLUJO_ECO_DEFINITIVO.md`. Lo dudoso o peligroso solo orienta y no abre nada. Sin dispositivo configurado (modo software) Eco orienta y no emite órdenes de apertura.
- **Windows Forms:** un operador registra a un ciudadano, busca el residuo por nombre o sinónimo, solicita la apertura y confirma lo recibido.

Ambas comparten una sola identidad en Oracle. Reconocer un material no autoriza depositarlo: decide la lógica de negocio.

## Inicio rápido

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Validar-Entorno.ps1
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Preparar-BaseDatos.ps1
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Preparar-Demostracion.ps1
dotnet build EcoAccess.P3.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-Demostracion.ps1
```

Guía completa: [docs/instalacion/11_instalacion.md](docs/instalacion/11_instalacion.md). Para compañeros (sin rutas privadas, con sus propias credenciales): [docs/instalacion/INSTALACION_COMPANEROS.md](docs/instalacion/INSTALACION_COMPANEROS.md). Estado real: [docs/entrega/12_entrega.md](docs/entrega/12_entrega.md).

## Estructura

| Carpeta | Contenido |
|---|---|
| `src/` | Compatibilidad, Entidades, Logica, Datos, Presentacion y Bot |
| `tests/` | pruebas unitarias e integración contra Oracle |
| `database/` | modelo, tablas, migraciones, catálogo y consultas |
| `hardware/` | firmware del ESP32 |
| `scripts/` | instalación, demostración y pruebas |
| `docs/` | documentación cronológica del desarrollo |
| `reportes/` | informe Power BI de cinco páginas (PBIP y `EcoAccess.pbix`) |

## Equipo

Jorge Gutierrez Thomas y Heyder Canchila.