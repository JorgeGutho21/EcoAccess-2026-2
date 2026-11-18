# Guía de instalación para los tres integrantes

Objetivo: que los tres trabajen con **la misma base Oracle Cloud**, sin Docker y sin Oracle local. Tiempo estimado: 45 minutos.

## 1. Programas (WinGet, en PowerShell)

```bash
winget install --id Git.Git -e
```

```bash
winget install --id Microsoft.VisualStudio.2022.Community -e
```

```bash
winget install --id Oracle.SQLDeveloper -e
```

En el instalador de Visual Studio marcar **«Desarrollo de escritorio de .NET»** (incluye .NET Framework 4.8). Opcionales: `Microsoft.PowerShell` (ventana del token de Telegram) y `Ollama.Ollama` (solo quien ejecute la visión local).

## 2. Acceso a GitHub

1. El propietario invita a cada integrante a `JorgeGutho21/EcoAccess-P3` (*Settings → Collaborators*).
2. Cada uno acepta la invitación con **su propia cuenta** y configura su identidad en el repositorio:

```bash
git config user.name "Nombre Apellido"
```

```bash
git config user.email "correo-de-su-cuenta@ejemplo.com"
```

## 3. Clonar y abrir

```bash
git clone https://github.com/JorgeGutho21/EcoAccess-P3.git
```

```bash
git checkout recovery/ecoaccess-oracle-cloud
```

Abrir `EcoAccess.P3.sln` en Visual Studio → *Compilar → Recompilar solución*.

## 4. Wallet y conexión Oracle

1. Recibir `Wallet_EcoAccess.zip` del propietario por un canal **privado** (nunca por el repositorio ni grupos públicos).
2. Descomprimir en `%USERPROFILE%\.oracle\wallets\EcoAccess`.
3. **SQL Developer** con la cuenta personal `ECO_DEV_<NOMBRE>` que crea el propietario (`dotnet run --project src/EcoAccess.P3.Instalador -- crear-integrante`); al primer ingreso se cambia la contraseña temporal. Pasos: [`docs/oracle-cloud/01_CONEXION_SQL_DEVELOPER.md`](../oracle-cloud/01_CONEXION_SQL_DEVELOPER.md).
4. **La aplicación** usa la cuenta de servicio `ECOACCESS_P3_APP` (su contraseña la entrega el propietario por canal privado): F5 → «Configuración inicial (Oracle Cloud)» → carpeta del wallet, servicio `ecoaccess_tp`, usuario y contraseña → *Comprobar y guardar*. Queda en los User Secrets de su perfil de Windows.

## 5. Probar

*Prueba → Ejecutar todas las pruebas*. Las de integración Oracle se ejecutan contra la base compartida y **borran sus datos al terminar**; evitar ejecutarlas a la vez desde dos equipos durante una demostración.

## 6. Consultar los datos compartidos

```sql
ALTER SESSION SET CURRENT_SCHEMA = ECOACCESS_P3_APP;
SELECT * FROM vista_canecas_estacion;
SELECT version, aplicada_utc FROM eco_migracion_aplicada ORDER BY aplicada_utc;
```

## 7. Trabajar con ramas y publicar

```bash
git checkout -b desarrollo/mi-cambio
```

```bash
git add ruta/del/archivo.cs
```

```bash
git commit -m "descripción concreta del cambio en minúsculas"
```

```bash
git push -u origin desarrollo/mi-cambio
```

Luego abrir un *pull request* hacia la rama de integración. No subir el wallet, `secrets.json`, fotos ni contraseñas (el `.gitignore` los bloquea).

## 8. Problemas frecuentes

| Síntoma | Solución |
|---|---|
| «Wallet: no existe la carpeta» | descomprimir el ZIP completo en la ruta indicada |
| «Red: sin respuesta del puerto 1522» | Internet o firewall de la red (algunas redes universitarias bloquean 1522; probar con datos móviles) |
| `ORA-12506`/base no responde | la base se detuvo por inactividad: el propietario la inicia en la consola (Always Free, sin costo) |
| `ORA-01017` | usuario o contraseña; varios intentos bloquean la cuenta (`ORA-28000`) |
| Error al compilar «archivo en uso» | cerrar EcoAccess o el bot, o compilar en Debug |
| El bot dice «otra instancia puede estar activa» | solo un integrante ejecuta el bot en polling |
