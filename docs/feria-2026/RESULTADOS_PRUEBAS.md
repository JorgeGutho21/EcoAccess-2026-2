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

## 2026-11-26 · PC de Jorge (integración, rama `integracion/datos-feria-2026`)

Compilado en una copia aparte (worktree) para no bloquear los servicios que corren desde `bin\Release` del repositorio.

| Prueba | Resultado |
|---|---|
| Compilación Debug y Release de `EcoAccess.P3.sln` | **OK** · 0 errores · 171 advertencias |
| `EcoAccess.P3.Tests` (Release, sin `IntegracionOracle`) | **2648 superadas** · 0 con error · 1 omitida |
| `EcoAccess.P3.Datos.Tests` (sin `IntegracionOracle`) | **65 superadas** · 0 con error · 8 omitidas |
| Generador `EcoAccess.P3.DatosFeria` | Compila: 0 errores, 0 advertencias |
| `Verificar-LoteFeria.ps1` (solo lectura, Oracle Cloud) | **0 fallas · 2 avisos (P1, P2) · 46 comprobaciones**; 650/650 depósitos, 64 cuentas · `verificaciones/verificacion-SIM-FERIA-2026-20261126-0836.md` |
| `DatosFeria verificar --perfil produccion` | **95/95**: las 64 cuentas inician sesión con su rol, áreas permitidas y bloqueadas correctas; integridad y saldos correctos. Contraseñas sin regenerar. |
| `Verificar-Equipo-EcoAccess.ps1 -Perfil EstacionPrincipal` | Instalación OK, 4 accesos directos OK, Wallet configurado, Ollama conectado (qwen3-vl:4b, lfm3-vl), **sin puerto serial: ESP32 no conectado** |
| Web, Dos Clics, Challenge (GitHub Pages), panel 8090 y pasarela 8093 | HTTP 200 (solo lectura) |
| Bot de Telegram | Proceso `--polling` activo. **No probado con un mensaje real.** |
| Integración Oracle (`IntegracionOracle`) y canecas físicas | **No ejecutadas** (sin esquema aislado y sin ESP32 conectado; no se envían órdenes físicas) |
