---
archivo: 13_PRUEBAS_DE_SOFTWARE
titulo: Pruebas de software en EcoAccess P3
subtitulo: Pruebas unitarias y de integración, dobles en memoria, esquema aislado, concurrencia, regresión de cierre y el fallo intermitente investigado
codigo: 13
---

# Conceptos básicos

- **Prueba automatizada:** un método que ejecuta una parte del sistema y comprueba el resultado con `Assert`. Si la comprobación falla, la prueba falla.
- **Prueba unitaria:** prueba una clase sin base de datos ni red. Usa **dobles** en memoria (`RepositorioUsuariosEnMemoria`...) que implementan las mismas interfaces que los repositorios Oracle.
- **Prueba de integración:** usa Oracle real. En EcoAccess se ejecuta contra el esquema aislado `ECOACCESS_P3_PRUEBAS` (Oracle FREE local) o contra Oracle Cloud. Cada prueba borra lo que crea.
- **Prueba omitida (inconclusa):** no se pudo ejecutar porque falta algo, por ejemplo hardware, el modelo local o una credencial. **No cuenta como aprobada.**
- **Regresión:** volver a ejecutar todo después de un cambio, para comprobar que no se rompió nada.

EcoAccess usa **MSTest 4**. Una prueba se ve así:

{{codigo:tests/unitarias/EcoAccess.P3.Tests/Bot/ServidorRegistroTests.cs:284:310}}

# Estructura

| Proyecto | Qué prueba | Necesita |
|---|---|---|
| `tests/unitarias/EcoAccess.P3.Tests` | entidades, lógica, bot, presentación y arquitectura | nada |
| `tests/integracion/EcoAccess.P3.Datos.Tests` | repositorios, migraciones, flujos completos con Oracle, carga y concurrencia | la variable `ECOACCESS_ConnectionStrings__OracleP3` |

Las pruebas de **arquitectura** (`tests/unitarias/.../Arquitectura`) comprueban, por ejemplo, que la lógica no referencie
a Oracle y que no haya secretos en el repositorio.

# Cómo se ejecutan

```powershell
dotnet build EcoAccess.P3.sln -c Debug
dotnet test tests\unitarias\EcoAccess.P3.Tests -c Debug --no-build
# integración en el esquema aislado: la cadena se lee de los User Secrets ecoaccess-p3-pruebas
$env:ECOACCESS_ConnectionStrings__OracleP3 = (Get-Content "$env:APPDATA\Microsoft\UserSecrets\ecoaccess-p3-pruebas\secrets.json" -Raw | ConvertFrom-Json).'ConnectionStrings:OracleP3'
dotnet test tests\integracion\EcoAccess.P3.Datos.Tests -c Debug --no-build
```

En Visual Studio: **Prueba > Explorador de pruebas > Ejecutar todas**.

# Regresión de cierre

{{incluir:docs/pruebas/resultados_cierre/REGRESION_ENTREGA_FINAL.md}}

# Concurrencia: 1, 10, 20 y 30 personas

`CargaUsuariosOracleTests` simula personas que, a la vez:

1. piden registrarse por Telegram;
2. abren la página;
3. envían el formulario;
4. confirman el vínculo;
5. consultan su saldo.

Después comprueba en Oracle que no hubo cuentas duplicadas y que todo sobrevive a un reinicio:

- `UsuariosNuevosSimultaneosQuedanAisladosYSobrevivenAlReinicio` con 1, 10, 20 y 30 personas;
- `CuentasExistentesSeVinculanSimultaneamenteSinDuplicarse` con 10, 20 y 30;
- `ElMismoCorreoAlMismoTiempoCreaUnaSolaCuenta`.

# Catálogo de clases de prueba

Generado desde el código: cada clase de prueba con sus métodos.

{{pruebas}}
