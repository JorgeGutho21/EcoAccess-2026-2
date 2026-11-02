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

Con Docker Desktop abierto, crea el contenedor. La primera vez descarga la imagen; sustituye la contraseña por una propia:

```powershell
docker run -d --name oracle-free-p3 --restart unless-stopped -p 1522:1521 -e ORACLE_PWD=CambiaEstaClave1 -v ecoaccess-p3-oradata:/opt/oracle/oradata container-registry.oracle.com/database/free:latest
```

Espera a que `docker ps` lo muestre como `healthy` y crea el PDB de pruebas:

```powershell
@"
CREATE PLUGGABLE DATABASE PRUEBAP3 ADMIN USER pdbadmin IDENTIFIED BY "CambiaEstaClave1" FILE_NAME_CONVERT=('/opt/oracle/oradata/FREE/pdbseed/','/opt/oracle/oradata/FREE/PRUEBAP3/');
ALTER PLUGGABLE DATABASE PRUEBAP3 OPEN;
ALTER PLUGGABLE DATABASE PRUEBAP3 SAVE STATE;
ALTER SESSION SET CONTAINER=PRUEBAP3;
CREATE TABLESPACE users DATAFILE '/opt/oracle/oradata/FREE/PRUEBAP3/users01.dbf' SIZE 100M AUTOEXTEND ON;
EXIT
"@ | docker exec -i oracle-free-p3 bash -c "sed '1s/^\xEF\xBB\xBF//' | sqlplus -s / as sysdba"
```

Luego:

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

Con credenciales configuradas (`scripts\instalacion\Configurar-Credenciales.ps1`), `scripts\desarrollo\Iniciar-EcoAccess-Telegram.ps1` comprueba todo e inicia el bot, y `scripts\desarrollo\Verificar-EcoAccess-Telegram.ps1` informa el estado de cada componente. Pasos detallados: `docs\telegram\GUIA_TELEGRAM_DOBLE_IA.md`.

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