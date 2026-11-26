# Lote de demostración académica de la feria (`SIM-FERIA-2026`)

Datos **sintéticos** para mostrar en la feria todas las funciones de EcoAccess con cuentas que sí inician sesión:
ciudadanos con historial de 90 días, operarios que verifican y acreditan, supervisores que revisan y reversan, y
estadísticas con volumen suficiente para los gráficos. **No son operaciones físicas**: ningún depósito del lote movió
una tapa, y todos están marcados como demostración.

## Qué contiene

| Elemento | Cantidad | Cómo se creó (servicio oficial) |
|---|---:|---|
| Ciudadanos (8 nuevos, 12 ocasionales, 15 frecuentes, 10 destacados, 5 inactivos) | 50 | `ServicioRegistroCiudadanos` (hash PBKDF2, consentimiento) |
| Operarios | 8 | `ServicioUsuarios.RegistrarAsync` + `ServicioAutenticacion.EstablecerContrasenaInicialAsync` |
| Supervisores (rol 3) | 4 | ídem |
| Administradores de demostración (rol 3; el sistema no tiene un rol menor) | 2 | el primero con el repositorio y el mismo hash del servicio; crea al resto |
| Depósitos en las tres canecas de la Estación Oficial (90 días) | 650 | `ServicioInteracciones` (Telegram, Asistido, invitado) + `CanecaSimulada` |
| De ellos, de invitados (sin cuenta, canal Simulador, sin ecopuntos) | 30 | `IniciarVisitanteAsync` |
| Registros manuales del operario (canal Manual, sin depósito) | 40 | `RegistrarAperturaManualAsync` |
| Verificaciones / rechazos | ~510 / ~42 | `VerificarDepositoAsync` / `PKG_DEPOSITO.rechazar` |
| Créditos y reversos de ecopuntos | ~180 / 5 | `ServicioEcopuntos.AcreditarAsync` / `RevertirAsync` |

Reglas respetadas sin excepción: la tarifa vigente de `REGLA_ECOPUNTOS` (solo los 7 materiales aprovechables con
tarifa dan puntos; orgánico y no aprovechable dan 0), un solo crédito por depósito, nadie acredita lo suyo, ningún
saldo negativo, ninguna operación anterior al registro de la cuenta ni posterior al momento de la carga. Los
depósitos verificados antes de que existiera la tarifa (11/11/2026) se acreditan después de esa fecha, como lo haría
un operario con el rezago. No hay módulo de canjes ni recompensas (la app los muestra como «próximamente»), así que
el lote no los incluye.

## Separación de los datos reales

- Cada cuenta, visita y depósito está en `REGISTRO_SIMULACION` con el lote `SIM-FERIA-2026`.
- Correos `…@feria.ecoaccess.test` (dominio reservado, no pertenece a nadie).
- `VISTA_DEPOSITOS_ESTADO`: `es_simulacion = 1`, `es_ensayo = 1`.
- Dashboard institucional (`EA_R_*`): con **V036** el lote cuenta como ensayo; las cifras reales lo excluyen y el
  filtro **«Solo demostración»** lo muestra.
- Las pantallas de Estadísticas generales y el panel administrativo cuentan todos los depósitos de la base (así eran
  antes del lote): durante la feria muestran el volumen del lote.

## Herramienta

`tests\herramientas\EcoAccess.P3.DatosFeria` (no está en la solución). Las contraseñas generadas se guardan solo en
`%LOCALAPPDATA%\EcoAccessP3\privado\feria\` (fuera del repositorio) y en el TXT del escritorio.

```powershell
dotnet build tests\herramientas\EcoAccess.P3.DatosFeria -c Release -o $env:TEMP\feria
# Ensayo en el esquema aislado local (estación de pruebas 2):
& $env:TEMP\feria\EcoAccess.P3.DatosFeria.exe generar   --perfil pruebas --estacion 2
& $env:TEMP\feria\EcoAccess.P3.DatosFeria.exe verificar --perfil pruebas
# Oracle Cloud (exige confirmación explícita):
& $env:TEMP\feria\EcoAccess.P3.DatosFeria.exe generar   --perfil produccion --confirmar-produccion
& $env:TEMP\feria\EcoAccess.P3.DatosFeria.exe verificar --perfil produccion   # escribe los TXT del escritorio si todo pasa
```

Repetir `generar` no duplica nada (cada fila tiene una clave estable en el manifiesto). `verificar` inicia sesión
con cada cuenta por `ServicioAutenticacion`, comprueba áreas permitidas y bloqueadas por rol, compara el saldo de la
app con SQL y `PKG_ECOPUNTOS.saldo`, y ejecuta las comprobaciones de integridad.

## Después de la feria

```powershell
# Desactivar las cuentas (conserva el historial marcado como demostración):
& $env:TEMP\feria\EcoAccess.P3.DatosFeria.exe desactivar --perfil produccion --confirmar-produccion
```

o, en SQL Developer, `database/07_operacion/T004__desactivar_cuentas_feria.sql`. Para borrar todo el lote:
`database/07_operacion/reversion/R_T004__revertir_lote_feria.sql` (solo filas del manifiesto; una cuenta usada en
vivo con datos reales se desactiva en lugar de borrarse). Borrar también los TXT del escritorio.
