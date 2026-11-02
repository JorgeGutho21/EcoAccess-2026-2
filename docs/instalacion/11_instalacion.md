# 11. Instalación

## Requisitos

Windows 10/11, Visual Studio 2019 16.8 o posterior con desarrollo de escritorio .NET, Developer Pack 4.8, .NET SDK, Docker Desktop y Git. Opcional: Ollama con el modelo LFM3-VL, Arduino IDE y un ESP32.

```powershell
git clone https://github.com/JorgeGutho21/EcoAccess-P3.git
cd EcoAccess-P3
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Validar-Entorno.ps1
```

El validador solo consulta e indica qué falta.

## Oracle

Crea un contenedor Oracle Free, publica el puerto 1521 como 1522 y crea el PDB de pruebas `PRUEBAP3`. Luego:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Preparar-BaseDatos.ps1
```

La contraseña se genera y queda en los User Secrets de tu perfil de Windows.

## Modo demostración

Usa datos ficticios y una caneca simulada. **Un simulador no equivale al dispositivo físico.**

```powershell
powershell -ExecutionPolicy Bypass -File scripts\instalacion\Preparar-Demostracion.ps1
dotnet build EcoAccess.P3.sln -c Release
powershell -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-Demostracion.ps1
```

Para detenerlo: `scripts\desarrollo\Detener-Demostracion.ps1`. La cuenta `admin.demo@ejemplo.com` define su contraseña en el enlace de configuración inicial de la bienvenida.

## Pruebas

```powershell
powershell -ExecutionPolicy Bypass -File scripts\pruebas\Probar-Solucion.ps1
```

## Reconocimiento visual

```powershell
ollama pull lfm3-vl
```

## Modo prototipo físico

Lo atiende **un solo computador**: Telegram admite un consumidor y un solo equipo controla el ESP32.

1. `scripts\instalacion\Configurar-Telegram.ps1` guarda el token tras comprobarlo con Telegram.
2. Prepara el firmware según `hardware/README.md`.
3. `scripts\desarrollo\Iniciar-Demostracion.ps1 -ConBot` inicia el bot; sustituye el simulador por `--dispositivo-serial`.

## Estado de esta guía

Probada en el equipo de desarrollo. **La instalación en un segundo computador está pendiente.**