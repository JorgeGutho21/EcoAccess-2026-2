# 02 · Conexión a un Oracle local

Para practicar o probar sin tocar la base compartida. Sirve el **Escenario B** de [14-guia-companeros.md](14-guia-companeros.md).

## Requisitos

| Herramienta | Versión probada | Para qué |
|---|---|---|
| Oracle Database Free | 26ai (23.26.3) en Windows; también vale 23ai Free | La base local |
| Oracle SQL Developer | 26.2 | Consultar y ver objetos |
| SQL*Plus o SQLcl | los que trae Oracle Database Free (`sqlplus.exe`, `sql.exe`) | Ejecutar los scripts por consola |

Antes de instalar, compruebe si ya están: `Get-Command sqlplus, sql` en PowerShell. Oracle Free escucha en el puerto **1521**, con el servicio **`FREEPDB1`**.

## Crear una conexión en SQL Developer

1. Abra SQL Developer y pulse **+** (*New Database Connection*).
2. Complete:

| Campo | Valor |
|---|---|
| Name | un nombre que diga **qué base es**, por ejemplo `EcoAccess LOCAL - ECOACCESS_P3_PRUEBAS` |
| Username | el esquema (por ejemplo `ECOACCESS_P3_PRUEBAS`). **No** use `SYSTEM` para trabajar |
| Password | la suya; no marque *Save Password* en equipos compartidos |
| Connection Type | **Basic** |
| Hostname | `localhost` |
| Port | `1521` |
| Service name | `FREEPDB1` (Service name, no SID) |

3. **Test** → *Status: Success*. Después, **Connect**.

> **Ojo con el nombre.** En el PC de Jorge, la conexión llamada `EcoAccess BD LOCAL` entra al esquema académico `ECOACCESS_BD`, que **no es** la base de la aplicación (ver [01-arquitectura.md](01-arquitectura.md)). Ponga nombres que digan qué base es.

## Comprobar a qué se conectó

En una hoja de trabajo (*SQL Worksheet*):
```sql
SELECT USER, SYS_CONTEXT('USERENV','CURRENT_SCHEMA') esquema, SYS_CONTEXT('USERENV','SERVICE_NAME') servicio FROM dual;
```
**Esperado:** su usuario, el mismo esquema y `freepdb1`.

## Crear un esquema de desarrollo vacío (una vez por PC)

Hace falta la contraseña de `SYSTEM` del Oracle local, y **la escribe la persona dueña del equipo**. El proyecto ya trae el script, que:
- solo acepta localhost y nunca toca Oracle Cloud;
- pide la contraseña sin mostrarla;
- guarda la conexión del esquema nuevo en los User Secrets de Windows.

```powershell
powershell -ExecutionPolicy Bypass -File scripts\pruebas\Crear-Esquema-Aislado-Local.ps1 -Esquema ECOACCESS_P3_PRUEBAS_DEV -IdSecretos ecoaccess-p3-dev
```

Después se instala la estructura en ese esquema siguiendo [04-instalacion.md](04-instalacion.md).

## Cadena de conexión para la aplicación o los scripts

La guardan los scripts en `%APPDATA%\Microsoft\UserSecrets\<id>\secrets.json`, fuera del repositorio, con la forma:
```
ConnectionStrings:OracleP3 = User Id=<esquema>;Password=<su contraseña>;Data Source=localhost:1521/FREEPDB1
```
**Nunca** la copie en el código, en `App.config`, en un `.env` versionado ni en un mensaje.
