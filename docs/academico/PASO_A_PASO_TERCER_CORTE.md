# EcoAccess P3 paso a paso — tercer corte

Documento de entrada para estudiar, instalar, ejecutar y defender el proyecto con los temas del tercer corte (ADO.NET,
DAO/Repository, tres capas, App.config). Resume y enlaza; el detalle de cada tema ya existe en `docs/` y no se repite.

## 1. Problema, objetivos y alcance

- **Problema.** En Valledupar los residuos se mezclan en la fuente; quien quiere separar no sabe en qué caneca va cada
  residuo y nadie registra lo que se separa.
- **Objetivo general.** Un sistema en C# (.NET Framework 4.8) con arquitectura por capas y persistencia en Oracle que
  orienta la separación, autoriza la apertura de la caneca correcta y registra cada depósito de forma trazable.
- **Objetivos específicos.** (1) Modelar entidades y reglas en capas independientes; (2) persistir con ADO.NET y
  consultas parametrizadas; (3) administrar catálogo, puntos y canecas desde WinForms; (4) atender ciudadanos por
  Telegram con las mismas reglas; (5) controlar la apertura física con órdenes autorizadas y una ventana de 10 s.
- **Alcance.** Prototipo académico con una estación de demostración. Fuera de alcance: báscula o sensor de depósito,
  despliegue en servidor de producción, versión WPF.
- Contexto completo: `docs/introduccion/00_presentacion.md`.

## 2. Tecnologías y requisitos

| Elemento | Versión | Nota |
|---|---|---|
| .NET Framework | 4.8 | `Directory.Build.props` (`net48`, C# 9) |
| SDK para compilar | .NET SDK 10.0.100 (`global.json`) o Visual Studio 2022 | compila el destino net48 |
| Proveedor ADO.NET | `Oracle.ManagedDataAccess` (ODP.NET administrado) | equivalente Oracle de `System.Data.SqlClient` |
| Base de datos | Oracle (local `localhost:1522/PRUEBAP3` o Autonomous Database con wallet) | esquema `ECOACCESS_P3_APP` |
| Interfaz | Windows Forms | WPF no implementado (opcional en la guía) |
| Pruebas | MSTest 4 | unitarias e integración con Oracle |

## 3. Arquitectura en una página

Presentación (WinForms) y Bot (Telegram + bridge del ESP32) son **raíces de composición**: crean los repositorios Oracle
y se los entregan a los servicios. Lógica define las interfaces `IRepositorio*` y las reglas; Datos las implementa con
ADO.NET; Entidades no depende de nadie. Diagramas en `DIAGRAMAS_UML.md`; detalle por capa en
`docs/desarrollo/02_entidades.md`, `03_logica.md`, `05_presentacion.md` y `docs/base_datos/04_persistencia_oracle.md`.

Equivalencias con la guía del docente (capadatos.vercel.app):

| Guía (SQL Server) | EcoAccess (Oracle) |
|---|---|
| `Conexion.ObtenerConexion()` | `FabricaConexionesOracle.AbrirAsync()` |
| `SqlCommand` + `AddWithValue("@Id", id)` | `ComandosOracle.Crear(...)` + `comando.Agregar("id", OracleDbType.Int32, id)` con `:id` |
| `IProductoDAO` / `ProductoDAO` | rol cumplido por `Repositorio*Oracle` |
| `IProductoRepository` | `IRepositorio*` en `Logica/Contratos` |
| `ProductoService.Validar` | `Servicio*` que devuelven `Resultado` con motivo |
| `FrmProductos` usa `ProductoService` | `FormMateriales` usa `ServicioMateriales` |
| `App.config` con `TiendaDB` | `App.config` con `OracleP3` (sin contraseña) + User Secrets |

## 4. Instalación

1. Clonar el repositorio y abrir `EcoAccess.P3.sln` (Visual Studio 2022) o usar la línea de comandos.
2. Preparar Oracle: crear el usuario con `database/00_administracion/01_crear_usuario_p3.sql`, las tablas con
   `database/02_tablas/01_crear_tablas_iniciales.sql` y aplicar `database/06_migraciones/V002…V020` en orden. Guía
   completa: `docs/instalacion/11_instalacion.md` y `database/README.md`.
3. Para Oracle Cloud: descomprimir el wallet fuera del repositorio y definir `TNS_ADMIN` con esa carpeta.

## 5. Configuración (App.config + secreto local)

`src/EcoAccess.P3.Presentacion/App.config` trae la parte pública:

```xml
<connectionStrings>
  <add name="OracleP3"
       connectionString="Data Source=localhost:1522/PRUEBAP3;User Id=ECOACCESS_P3_APP"
       providerName="Oracle.ManagedDataAccess.Client" />
</connectionStrings>
```

- Para otra base se edita `Data Source` (por ejemplo el alias del wallet) **sin recompilar**: al compilar se copia como
  `EcoAccess.P3.Presentacion.exe.config` junto al ejecutable.
- La contraseña **no** va en App.config (se publica en GitHub). La primera vez la app pide la contraseña en
  `FormActivacionOracle`, la prueba contra Oracle y solo si Oracle la acepta la guarda en los User Secrets
  `ecoaccess-p3-framework` del perfil de Windows. También se puede definir antes:

```bash
dotnet user-secrets set "ConnectionStrings:OracleP3" "Data Source=...;User Id=ECOACCESS_P3_APP;Password=..." --project src/EcoAccess.P3.Presentacion
```

- Prioridad: variable `ECOACCESS_ConnectionStrings__OracleP3` > User Secrets > App.config. Si alguien escribe una
  contraseña en App.config, `CadenaConexionOracle.Resolver` la ignora y la prueba `AppConfig_DelProyectoDeInicio_TieneLaCadenaSinContrasena` falla.

## 6. Compilación y ejecución

```bash
dotnet build EcoAccess.P3.sln -c Debug
```

```bash
src/EcoAccess.P3.Presentacion/bin/Debug/net48/EcoAccess.P3.Presentacion.exe
```

El bot y el bridge del ESP32 se inician según `docs/telegram/GUIA_TELEGRAM_DOBLE_IA.md` y `docs/hardware/GUIA_VALIDACION_ESP32.md`.

## 7. Pruebas

```bash
dotnet test tests/unitarias/EcoAccess.P3.Tests/EcoAccess.P3.Tests.csproj -c Debug
```

```bash
dotnet test tests/integracion/EcoAccess.P3.Datos.Tests/EcoAccess.P3.Datos.Tests.csproj -c Debug
```

Las de integración necesitan `ConnectionStrings:OracleP3` en los User Secrets; sin él quedan **inconclusas** (no se
dan por aprobadas). Cada prueba crea sus propios datos ficticios y los borra al terminar. La de carga
(`CargaUsuariosOracleTests`) registra 10 y 20 personas simultáneas. Resultados de esta auditoría:
`INFORME_AUDITORIA_TERCER_CORTE.md` §J.

## 8. Errores comunes y soluciones (equivalentes Oracle de la sección «Errores» del curso)

| Síntoma | Causa | Solución |
|---|---|---|
| La app abre «Configuración inicial» y pide contraseña | No hay cadena privada todavía (solo la plantilla de App.config) | Escribir la contraseña del esquema; se guarda en el perfil, no en el repositorio |
| «Oracle rechazó el usuario o la contraseña» (ORA-01017) | Contraseña errada | Revisarla con calma: varios intentos bloquean la cuenta (ORA-28000) |
| «Oracle no respondió en …» | Contenedor local apagado, sin red o `TNS_ADMIN` sin el wallet | Encender Oracle o corregir `Data Source`/`TNS_ADMIN` en App.config o en el entorno |
| «Esquema sin las tablas de EcoAccess» | Usuario correcto sin migraciones | Ejecutar los scripts de `database/02_tablas` y `06_migraciones` |
| `ConfigurationManager` devuelve null | App.config fuera del proyecto de inicio o sin `System.Configuration` | App.config está en Presentacion y la referencia ya está en su `.csproj` |
| Inyección SQL | Concatenar datos del usuario | Usar siempre `comando.Agregar(...)` con `:parametro`; lo vigila `PruebasTercerCorte` |
| Referencias entre proyectos rotas | Se agregó una referencia que salta capas | `PruebasArquitectura` indica cuál; revisar el `.csproj` |

## 9. Resultados parciales, limitaciones y conclusiones

Los resultados medidos están en `INFORME_AUDITORIA_TERCER_CORTE.md`. Limitaciones: ESP32 sin prueba física en esta
auditoría; depósito «pendiente de verificación» por falta de sensor; sin WPF. Conclusión técnica: el proyecto aplica los
principios del curso (capas, acceso a datos encapsulado, interfaces, parámetros, App.config) con dos adaptaciones
justificadas —Repository sin DAO intermedio y contraseña fuera de App.config— que se pueden defender con el código y las
pruebas citadas.
