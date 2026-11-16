# Matriz de cumplimiento del tercer corte — Programación de Computadores III

Rama `desarrollo/auditoria-tercer-corte` (base `desarrollo/telegram-doble-ia`, 8c7ed50) · auditoría del 09/11/2026.

## 1. Fuentes oficiales y cómo se leyeron

| Fuente | Estado | Qué aporta |
|---|---|---|
| PDF «Teoria 3er corte Programacion3.pdf» (16 páginas, impresión del navegador del 09/11/2026) | **Parcial**: solo las páginas 1, 13, 14 y 15 tienen contenido; 2–12 y 16 salieron en blanco porque el sitio anima sus secciones al desplazarse y la impresión no las capturó | Portada (C#, .NET Framework 4.8, SQL Server, VS 2022; WinForms y WPF) y la sección App.config |
| Sitio del docente https://capadatos.vercel.app (pie del PDF) | **Leído completo** (HTTP 200). `aopadatos.vercel.app`, citado en la misión, responde 404 | Teoría, arquitectura, 4 diagramas de clases, 9 pasos, WPF, App.config y 6 errores comunes |
| Repositorio del docente https://github.com/john-patino/SistemaProductos (rama `master`) | Leído (estructura, interfaces, App.config, .csproj). `john-patino/demo-empleados-dao`, enlazado en el paso 09, responde 404 | Ejemplo CRUD de `Producto` con `IProductoDAO`, `ProductoDAO`, `IProductoRepository`, `ProductoRepository`, `ProductoService`, `Conexion` |
| Rúbrica con ponderaciones | **No disponible**. No se inventa calificación ni porcentaje | — |

El material del docente se usa como criterio, no como código a copiar: EcoAccess trabaja con Oracle (ODP.NET) en lugar
de SQL Server, y las equivalencias se justifican abajo.

## 2. Matriz

Estados: **Cumple**, **Cumple con adaptación** (mismo principio, implementación distinta y justificada), **Parcial**,
**No cumple**, **No aplica**, **No verificable**. «Obligatorio» = exigido por la guía del docente; «Mejora» = propio de
EcoAccess.

| ID | Criterio | Fuente oficial | Implementación en EcoAccess | Evidencia | Prueba | Estado | Brecha |
|---|---|---|---|---|---|---|---|
| C-01 | Solución en blanco con proyectos separados por capa (Obligatorio) | Sitio §04 paso 01; repo docente | `EcoAccess.P3.sln` con Entidades, Logica, Datos, Presentacion, Bot y Compatibilidad | `EcoAccess.P3.sln`, `src/*/*.csproj` | `PruebasArquitectura.ReferenciasDeProyecto_CoincidenConElDiagramaDeDependencias` | Cumple | — |
| C-02 | .NET Framework 4.8 (Obligatorio) | PDF p. 1; sitio §06 | `Directory.Build.props`: `TargetFramework net48`, C# 9 | `Directory.Build.props`; `App.config` `sku=.NETFramework,Version=v4.8` | compilación `dotnet build` (net48) | Cumple | Se compila con el SDK de .NET 10 apuntando a net48; también abre en VS 2022 |
| C-03 | Entidades POCO sin acceso a datos (Obligatorio) | Sitio §01 y paso 02 | `EcoAccess.P3.Entidades` sin referencias; las entidades validan sus propias invariantes (p. ej. `Material.Habilitar()`) | `src/EcoAccess.P3.Entidades/*.cs` | `PruebasArquitectura.Entidades_NoUsaOtrosProyectosOracleNiWindowsForms` | Cumple con adaptación | El curso pide POCO «sin lógica»; aquí guardan reglas de su propio estado (encapsulamiento POO), sin SQL ni UI |
| C-04 | Regla de oro: ninguna capa salta niveles; el formulario nunca toca la conexión (Obligatorio) | Sitio §01 | Formularios solo reciben servicios de Lógica; `Program.cs` es la única raíz que crea `Repositorio*Oracle` | `src/EcoAccess.P3.Presentacion/Program.cs` | `PruebasTercerCorte.Presentacion_NoEscribeSqlNiUsaTiposAdoNet`, `PruebasArquitectura.Bot_UsaDatosSoloEnProgram` | Cumple | — |
| C-05 | Patrón DAO: clase de acceso por entidad con CRUD y ADO.NET (Obligatorio) | Sitio §01, paso 05–06 | `Repositorio*Oracle` (23 clases) ejecutan el SQL con ODP.NET | `src/EcoAccess.P3.Datos/Repositorios/` | `tests/integracion/.../RepositoriosCatalogoOracleTests.cs` | Cumple con adaptación | No existe una clase `*DAO` separada: el repositorio Oracle cumple el papel de DAO. Ver §3 |
| C-06 | Patrón Repository con interfaz e inyección (Obligatorio) | Sitio §01, paso 07–08 | Interfaces `IRepositorio*` en `Logica/Contratos`; servicios las reciben por constructor | `IRepositorioMateriales.cs`, `ServicioMateriales(...)` | `tests/unitarias/.../Logica/*` usan dobles en memoria de las interfaces | Cumple | — |
| C-07 | Capa lógica con validaciones y reglas (Obligatorio) | Sitio paso 08 (`ProductoService.Validar`) | `Servicio*` validan y devuelven `Resultado`/`Resultado<T>` con `MotivoRechazo` | `src/EcoAccess.P3.Logica/Resultado.cs`, `ServicioMateriales.RegistrarAsync` | 1 627 pruebas unitarias | Cumple | — |
| C-08 | Cadena de conexión en App.config leída con `ConfigurationManager` (Obligatorio) | PDF p. 13–15; sitio §06 | **Corregido en esta auditoría**: `App.config` del proyecto de inicio con `OracleP3` (destino y usuario, sin contraseña) leído con `System.Configuration.ConfigurationManager`; la contraseña solo en User Secrets | `src/EcoAccess.P3.Presentacion/App.config`, `Program.cs`, `Datos/Oracle/CadenaConexionOracle.cs` | `PruebasTercerCorte.AppConfig_…`, `Program_LeeAppConfigConConfigurationManager`, `Resolver_…`; lectura real con `ConfigurationManager.OpenExeConfiguration` | Cumple con adaptación | La guía guarda la cadena completa en App.config; con Oracle Cloud eso publicaría una contraseña en GitHub |
| C-09 | Referencia a `System.Configuration` (Obligatorio) | Sitio paso 04 y error 3 | `<Reference Include="System.Configuration" />` en Presentacion | `EcoAccess.P3.Presentacion.csproj` | compilación | Cumple | La guía la pone en Datos; aquí la usa la raíz de composición y Datos recibe la cadena por constructor |
| C-10 | Conexión abierta y liberada con `using` (Obligatorio) | Sitio paso 06 | `FabricaConexionesOracle.AbrirAsync` + `using var conexion` en cada repositorio | `FabricaConexionesOracle.cs`, `Repositorio*Oracle.cs` | integración Oracle (§4) | Cumple | — |
| C-11 | Consultas parametrizadas contra SQL Injection (Obligatorio) | Sitio error 5 | `ComandosOracle.Crear` (BindByName) + `comando.Agregar(nombre, OracleDbType, valor)` | `Datos/Oracle/ComandosOracle.cs` | `PruebasTercerCorte.Datos_CreaComandosSoloConBindByNameYSinConcatenarValores` | Cumple | Hay SQL construido por concatenación solo con nombres fijos (tablas/columnas de listas internas), nunca con datos del usuario |
| C-12 | Manejo de excepciones de BD (Obligatorio) | Sitio paso 06 (`catch SqlException`) | `TraductorErroresOracle` convierte ORA-00001, ORA-02291, ORA-00054/30006/00060 en excepciones de dominio; la GUI muestra mensajes sin datos internos | `Datos/Oracle/TraductorErroresOracle.cs`, `Logica/Contratos/*Exception.cs` | pruebas de unicidad e interbloqueo en integración | Cumple | — |
| C-13 | CRUD completo (Obligatorio) | Sitio paso 06 | Alta, consulta, actualización y baja lógica (habilitar/deshabilitar) de materiales, puntos, canecas, usuarios | `ServicioMateriales`, `ServicioContenedores`, `ServicioUsuarios` | `RepositoriosCatalogoOracleTests` | Cumple | Baja lógica como en la guía (`Activo = 0`) |
| C-14 | Presentación WinForms que usa solo la Lógica (Obligatorio) | Sitio paso 09 | `FormMateriales(ServicioMateriales, idActor)` y demás formularios | `src/EcoAccess.P3.Presentacion/Form*.cs` | `PruebasTercerCorte.Presentacion_NoEscribeSqlNiUsaTiposAdoNet`; arnés visual `tests/visual` | Cumple | — |
| C-15 | Proyecto GUI-WPF (Opcional en la guía) | Sitio §05 | No implementado: se eligió WinForms | — | — | No aplica | La guía presenta WPF como alternativa que reutiliza Lógica y Datos; añadirlo no cambia la arquitectura |
| C-16 | Transacciones (Mejora: la guía no las cubre) | — | `OracleTransaction` en operaciones de varias filas (orden + interacción, canecas con materiales, ecopuntos) | `RepositorioInteraccionesOracle.EmitirOrdenAsync`, `RepositorioContenedoresOracle.AgregarAsync` | integración Oracle | Cumple | — |
| C-17 | Pool de conexiones (Mejora) | — | ODP.NET con pooling por defecto (no se desactiva en la cadena) | `FabricaConexionesOracle` | carga 10 y 20 usuarios simultáneos | Cumple | No se ajustan `Min/Max Pool Size`; valores por defecto |
| C-18 | Ventana de depósito de 10 s desde la apertura confirmada, fuente única (Requisito del proyecto) | Misión del tercer corte | `MensajesTelegram.VentanaDepositoSegundos = 10`; firmware `ABIERTO_MS = 10000`; **corregido**: el simulador sin configuración usaba 0 s y ahora usa la fuente única | `MensajesTelegram.cs`, `AplicacionBridge.VentanaSimulada`, `configuracion_ejemplo.h` | `PruebasTercerCorte.VentanaDelSimulador_UsaLaFuenteUnicaDeDiezSegundos`, `Documentacion_NoPrometeVeinteSegundosDeApertura`, `BridgeOracleTests` | Cumple (simulado) | Sin ESP32 físico conectado en esta auditoría |
| C-19 | Diagramas de clases, paquetes y secuencia (Obligatorio: la guía enseña diagramas de clases) | Sitio §03 | 10 diagramas Mermaid del código real | `docs/academico/DIAGRAMAS_UML.md` | revisión contra el código | Cumple | — |
| C-20 | Errores comunes documentados (Obligatorio) | Sitio §07 | Equivalentes Oracle en la guía paso a paso | `docs/academico/PASO_A_PASO_TERCER_CORTE.md` §8 | — | Cumple | — |

## 3. DAO frente a Repository en EcoAccess

La guía separa `ProductoDAO` (SQL) de `ProductoRepository` (delega cada método en el DAO). En el repositorio del docente
ese Repository es un pase directo: `GetAll → ObtenerTodos`, `Add → Insertar`. EcoAccess no reproduce esa capa
intermedia porque no añadiría comportamiento: `RepositorioMaterialesOracle` ya es la única clase que conoce el SQL de
`material` (responsabilidad del DAO) y ofrece a la Lógica una interfaz de colección de dominio (`IRepositorioMateriales`,
responsabilidad del Repository). El beneficio que la guía atribuye al Repository —probar la Lógica con un acceso falso—
se obtiene igual: las pruebas unitarias sustituyen `IRepositorio*` por implementaciones en memoria. Crear `*DAO` vacíos
solo para nombrarlos sería abstracción ceremonial.

## 4. Pruebas ejecutadas en esta auditoría

Unitarias 1 627/1 628 (1 omitida); arquitectura 12/12; integración contra Oracle Autonomous Database 203/208 (0 fallidas, 5
omitidas por depender de visión real), sin dejar datos. Comandos en `PASO_A_PASO_TERCER_CORTE.md` §7 y detalle en
`INFORME_AUDITORIA_TERCER_CORTE.md` §J.

## 5. Cobertura, brechas y riesgos (sin calificación inventada)

- Criterios obligatorios verificables identificados: 18 (C-01…C-14, C-19, C-20 y los dos de configuración C-08/C-09).
  Cumplen: 18, de ellos 4 con adaptación justificada (C-03, C-05, C-08, C-09). No aplica: C-15 (WPF, opcional).
- No verificable con las fuentes disponibles: los criterios de evaluación que estuvieran en las páginas 2–12 del PDF
  que no salieron impresas; se suplieron leyendo el mismo sitio en línea.
- Riesgo de evaluación: que se exija literalmente una clase `*DAO` o la cadena completa en App.config. Respuesta
  preparada en `GUIA_SUSTENTACION.md` (preguntas 3, 7–9).
- Pendientes externos: ESP32 físico (C-18 solo simulado); WPF no implementado.
