# Evidencia · Prueba H, integración de la capa de datos C# con Oracle

- **Fecha:** 18/11/2026, alrededor de las 19:33 (hora del equipo).
- **Base:** Oracle local, esquema **aislado** `ECOACCESS_P3_PRUEBAS` (`localhost:1521/FREEPDB1`). **No** se usó Oracle Cloud.
- **Cómo se apuntó al esquema aislado:** variable de entorno `ECOACCESS_ConnectionStrings__OracleP3`, tomada de los User Secrets `ecoaccess-p3-pruebas`. Tiene prioridad sobre el secreto por defecto (`BaseIntegracionOracle.LeerCadena`). Antes de ejecutar, el comando comprobó que la cadena fuera de `localhost`.
- **Proyecto:** `tests/integracion/EcoAccess.P3.Datos.Tests` (.NET Framework 4.8, MSTest 4.0.2), rama `docs/database-oracle-auditoria`.

**Comando** (PowerShell; la cadena no se muestra):
```powershell
$env:ECOACCESS_ConnectionStrings__OracleP3 = (Get-Content "$env:APPDATA\Microsoft\UserSecrets\ecoaccess-p3-pruebas\secrets.json" -Raw | ConvertFrom-Json).'ConnectionStrings:OracleP3'
dotnet test tests\integracion\EcoAccess.P3.Datos.Tests\EcoAccess.P3.Datos.Tests.csproj --filter "FullyQualifiedName~VerificadorConexionOracleTests|FullyQualifiedName~EstacionTresCanecasOracleTests|FullyQualifiedName~VerificacionDepositosOracleTests|FullyQualifiedName~EcopuntosOracleTests|FullyQualifiedName~RepositorioEstadisticasOracleTests"
```

**Resultado** (línea final de `dotnet test`):
```text
Correctas! - Con error:     0, Superado:    56, Omitido:     1, Total:    57, Duración: 33 s - EcoAccess.P3.Datos.Tests.dll (net48)
```

**Prueba omitida:** `InformeInstitucional_CanecasHomonimasConservanSuIdentidadYConteosPositivos`. `dotnet test` la informó como omitida y no se investigó el motivo; es del informe institucional, no de la capa de base de datos auditada.

**Qué cubren las clases ejecutadas:**

| Clase | Qué comprueba |
|---|---|
| `VerificadorConexionOracleTests` | Conexión, tablas y errores de credencial o servicio |
| `EstacionTresCanecasOracleTests` | El QR lleva a una estación con caneca blanca, verde y negra |
| `VerificacionDepositosOracleTests` | Verificación de depósitos desde C#, que pasa por los disparadores V027 |
| `EcopuntosOracleTests` | Créditos, reversos, idempotencia, tarifa por unidad y concurrencia en el mismo libro que `PKG_ECOPUNTOS` |
| `RepositorioEstadisticasOracleTests` | Estadísticas |

**Qué no se ejecutó:** el resto del proyecto de integración, que incluye bridge del ESP32, carga de usuarios, visión y Telegram. Está fuera del alcance de base de datos, y algunas de esas pruebas dependen de `scripts/pruebas/Ejecutar-Integracion-Esquema.ps1`, que usa un contenedor Docker que ya no existe (hallazgo H-14).
