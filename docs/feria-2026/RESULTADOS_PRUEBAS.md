# Resultados de pruebas · datos de feria

Ejecutadas el 2026-11-26 en el PC de Heyder (Windows 11, SDK .NET 10.0.401, rama `feature/ecoaccess-datos-feria-2026`).

## Compilación y pruebas automatizadas

| Prueba | Resultado |
|---|---|
| `dotnet restore EcoAccess.P3.sln` | OK |
| Compilación Debug | **OK** · 0 errores · 171 advertencias (las mismas de `main`) |
| Compilación Release | **OK** · 0 errores · 171 advertencias |
| `EcoAccess.P3.Tests` (unitarias, Release) | **2649 superadas** · 0 con error · 0 omitidas |
| `EcoAccess.P3.Datos.Tests` sin categoría `IntegracionOracle` | **65 superadas** · 0 con error · 8 omitidas |
| Integración Oracle (`IntegracionOracle`) | **No ejecutada**: solo corre en `ECOACCESS_P3_PRUEBAS*`, no disponible en este PC |
| Sintaxis de los 5 scripts `scripts/feria/*.ps1` (parser de PowerShell) | OK, 0 errores |
| Compilación de T004 en Oracle (`DBMS_SQL.PARSE`, sin ejecutar) | OK |

Comando reproducible: `powershell -ExecutionPolicy Bypass -File scripts\feria\Compilar-Y-Probar.ps1`.

## Verificación del lote en Oracle Cloud (solo lectura)

`scripts/feria/Verificar-LoteFeria.ps1` → `verificaciones/verificacion-SIM-FERIA-2026-20261126-0755.md`: **46 comprobaciones,
0 fallas, 2 avisos.**

| Área | Comprobaciones | Resultado |
|---|---|---|
| Cuentas y roles | C04–C10 | OK: 50/8/4/2, dominio de demostración, roles correctos, todas con contraseña, ninguna bloqueada |
| Integridad referencial y de lote | C25, C29, C42, C43 | OK |
| Materiales y canecas | C15–C17 | OK: todo material admitido por su caneca y habilitado |
| Fechas | C21–C24 | OK: sin fechas futuras ni actividad anterior a la cuenta |
| Verificación independiente | C27–C29 | OK: nadie verifica lo propio; ninguna cuenta real interviene |
| Ecopuntos | C31–C37 | OK: sin créditos a cuentas reales, sin créditos en pendientes/rechazados, sin duplicados, tarifa × cantidad exacta |
| Estación | C44 | OK: 1 estación, 3 canecas, ninguna creada por el lote |
| Hardware | C39 | OK: ningún evento real en órdenes del lote (todos `sim-…`) |
| Separación de la actividad real | C40, C41 | **AVISO**: 650 aperturas sintéticas en canecas reales; 660 interacciones con canal operativo (ver PENDIENTES P1, P2) |

## No ejecutado (con motivo)

| Prueba pedida | Estado | Motivo |
|---|---|---|
| Autenticación de cada cuenta sintética | **Pendiente** | Las contraseñas solo están en el PC de Jorge (PENDIENTES P4) |
| Ensayo aislado, idempotencia y reversión en `ECOACCESS_P3_PRUEBAS` | **Pendiente** | Esquema aislado no disponible aquí; el lote se generó directamente en la nube (P5) |
| Concurrencia del generador | **No verificable aquí** | El generador no está en GitHub (P3) |
| Comparación servicios estadísticos vs SQL | **Parcial** | Se midió con SQL el efecto en `EA_R_BASE_DEPOSITOS`; falta abrir los paneles en la app |
| Gráficos en la aplicación | **Pendiente** | Requiere abrir la app con una cuenta de demostración |
