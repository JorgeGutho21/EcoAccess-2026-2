# Oracle Cloud · 02 · Instalación reproducible de la base de datos

Herramienta: **`src/EcoAccess.P3.Instalador`** (consola .NET Framework 4.8, ODP.NET administrado). Reemplaza a `scripts/instalacion/Preparar-BaseDatos.ps1`, que copiaba los scripts a un contenedor Docker y los ejecutaba con SQL*Plus. Solo necesita Visual Studio/.NET y el wallet.

## 1. Qué hace

1. Abre la conexión mTLS con el wallet (`Tns_Admin` y `Wallet_Location`) y muestra usuario, PDB, versión y hora del servidor.
2. Crea, si no existe, **`ECO_MIGRACION_APLICADA`** (`database/00_administracion/03_registro_migraciones.sql`).
3. Compara `PlanInstalacion.Pasos` con lo registrado y ejecuta **solo lo pendiente**, en orden.
4. Registra cada versión con huella **SHA-256**, duración y modo (`EJECUTADA` / `MARCADA`).
5. Se **detiene en el primer error** sin registrar esa versión; reejecutar continúa desde ahí. Si encuentra tablas sin versiones registradas, no ejecuta nada.
6. Concede los permisos de los roles de consulta y edición.

| Versión | Script |
|---|---|
| V001 | `02_tablas/01_crear_tablas_iniciales.sql` |
| C001 | `03_datos_prueba/01_catalogos.sql` |
| V002–V020 | `06_migraciones/V0xx__*.sql` |
| C002 | `03_datos_prueba/03_catalogo_ambiental_piloto.sql` |
| C003 | `03_datos_prueba/06_catalogo_ampliado.sql` |
| **V021** | `06_migraciones/V021__configuracion_tres_canecas.sql` |
| D001 (opcional, `--con-demo`) | `03_datos_prueba/02_datos_demo.sql` |

Las versiones nuevas se agregan **al final** de las obligatorias (V022…). La prueba `TodasLasMigracionesDelRepositorioEstanEnElPlan` falla si alguien crea una migración y olvida agregarla.

## 2. Comandos

```bash
dotnet run --project src/EcoAccess.P3.Instalador -- preparar-esquema
```

```bash
dotnet run --project src/EcoAccess.P3.Instalador -- guardar-conexion
```

```bash
dotnet run --project src/EcoAccess.P3.Instalador -- plan
```

```bash
dotnet run --project src/EcoAccess.P3.Instalador -- instalar --con-demo
```

```bash
dotnet run --project src/EcoAccess.P3.Instalador -- crear-integrante
```

Las contraseñas se escriben en la consola (asteriscos), nunca como argumentos ni en el chat; una contraseña vacía no se envía a Oracle.

## 3. Evidencia de la instalación real (2026-11-09 UTC)

| Paso | Resultado |
|---|---|
| `preparar-esquema` (ADMIN) | PDB `G5AB3C4722E812D_ECOACCESS`, **Oracle Database 19c EE 19.0.0.0.0**; creados `ECOACCESS_P3_APP` (OPEN, DATA), 6 privilegios y los roles `ECOACCESS_ROL_CONSULTA`/`_EDICION`. Un primer intento llegó con contraseña vacía (ORA-01005) y no creó nada |
| `instalar` V001–V008, C001, D001 | ejecutadas desde la línea EcoAccess_P3 (misma estructura: solo cambian comentarios, por eso `plan` avisa que V001–V007 «cambiaron después de aplicarse») |
| `instalar` V009–V020, C002, C003 | **14 versiones sin errores** (V014 la más lenta: 5,8 s) |
| `instalar` V021 | sin errores, 563 ms |
| Segunda ejecución | «No hay versiones pendientes» |
| D002 (tarifas ficticias) | **rechazada por el propio script** (ORA-20150: solo en el esquema aislado); el instalador se detuvo sin registrar: comportamiento correcto; se retiró del plan |

Inventario después de V021 (solo lectura del diccionario): **26 tablas**, 26 PK, 44 FK, 21 UNIQUE, 87 CHECK con nombre, 82 índices, 9 vistas, 1 trigger, **0 objetos inválidos**; las 8 restricciones «no validadas» son las `WITH READ ONLY` de las vistas de informes (tipo `O`), no de tablas. Modelo: [03_MODELO_RELACIONAL.md](03_MODELO_RELACIONAL.md).
