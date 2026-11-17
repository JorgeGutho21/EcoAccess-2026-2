---
archivo: 03_SOLID_Y_GRASP
titulo: Principios SOLID y patrones GRASP en EcoAccess P3
subtitulo: Cada principio con definición sencilla, explicación técnica, clase, método, ruta, evidencia y ejercicio
codigo: 03
---

# Para qué sirven SOLID y GRASP

**SOLID** son cinco principios para que el código orientado a objetos sea fácil de cambiar sin romper otras partes.
**GRASP** son nueve patrones que ayudan a decidir **a qué clase le toca cada responsabilidad**.

Este documento no repite definiciones de libro: muestra dónde se cumple cada principio **en EcoAccess**, con la clase,
el método y la ruta. También dice con honestidad dónde el proyecto no lo aplica del todo. Un principio no se cumple
«en todo el proyecto» por decirlo: se cumple en clases concretas que se pueden abrir y revisar.

# S — Principio de responsabilidad única (SRP)

**Definición sencilla:** una clase debe tener **una sola razón para cambiar**.

**Explicación técnica:** si una clase mezcla, por ejemplo, reglas de negocio, acceso a datos y presentación, cualquier
cambio en una de esas áreas obliga a tocarla y puede romper las otras.

**Evidencia en EcoAccess**

| Clase | Única responsabilidad | Ruta |
|---|---|---|
| `HasherPbkdf2` | calcular y verificar hashes PBKDF2 de contraseñas | `src/EcoAccess.P3.Logica/Servicios/HasherPbkdf2.cs` |
| `ControlAcceso` | comprobar que el actor existe, está activo y tiene el rol exigido | `src/EcoAccess.P3.Logica/Servicios/ControlAcceso.cs` |
| `FabricaConexionesOracle` | abrir conexiones a Oracle | `src/EcoAccess.P3.Datos/Oracle/FabricaConexionesOracle.cs` |
| `TraductorErroresOracle` | convertir ORA-00001 en `ConflictoUnicidadException` | `src/EcoAccess.P3.Datos/Oracle/TraductorErroresOracle.cs` |
| `ResolutorDestino` | decidir la caneca (blanca, verde, negra, posconsumo) según la categoría | `src/EcoAccess.P3.Logica/Servicios/DestinoEstacion.cs` |
| `CatalogoPesosEstimados` | estimar el peso de un material declarado | `src/EcoAccess.P3.Logica/Servicios/CatalogoPesosEstimados.cs` |

{{codigo:src/EcoAccess.P3.Logica/Servicios/ControlAcceso.cs:12:45}}

**Dónde se tensiona:** `ServicioInteracciones` y `ServicioConversacionTelegram` son grandes. Para mantener la cohesión se
dividieron en archivos parciales por tema (`ServicioInteracciones.Cantidades.cs`, `ServicioInteracciones.InvitadosTelegram.cs`,
`ServicioConversacionTelegram.FlujoEco.cs`...). Siguen siendo una sola clase, así que son el primer candidato si hubiera
que dividir responsabilidades.

**Ejercicio:** busque en `ServicioAutenticacion` qué haría falta cambiar si se pasara de PBKDF2 a Argon2. Respuesta: nada
en el servicio; solo se cambiaría la implementación de `IHasherContrasenas`.

# O — Principio abierto/cerrado (OCP)

**Definición sencilla:** se debe poder **agregar** comportamiento nuevo **sin modificar** el código que ya funciona.

**Evidencia:** para agregar un proveedor de visión nuevo basta con escribir una clase que implemente
`IClasificadorMaterial`. Así se agregaron `ClasificadorGroq` y luego `SelectorVision`, sin cambiar
`ServicioClasificacionImagen`, que solo conoce la interfaz.

Lo mismo con los canales de dispositivo: `CanecaSimulada`, `EstacionSimulada` (agregada en el cierre para tres tapas) y
`CanalDispositivoHttp` implementan `ICanalDispositivo` y `ServicioDespachoOrdenes` no cambió.

**Ejercicio:** escriba el esqueleto de un `ClasificadorPorNombreArchivo : IClasificadorMaterial` que proponga «PET» si el
nombre del archivo contiene «botella». ¿Qué clase existente tuvo que modificar? (Ninguna.)

# L — Principio de sustitución de Liskov (LSP)

**Definición sencilla:** si una clase cumple un contrato, cualquier otra que lo cumpla debe poder **reemplazarla** sin que el
programa se comporte mal.

**Evidencia:** las pruebas unitarias reemplazan los repositorios de Oracle por repositorios en memoria
(`tests/unitarias/EcoAccess.P3.Tests/Logica/Dobles/RepositoriosEnMemoria.cs`). Los servicios funcionan igual con ambos.
Las pruebas de integración (`tests/integracion/...`) ejecutan las mismas operaciones contra Oracle real. Si una
implementación violara el contrato (por ejemplo, el repositorio en memoria no lanzara `ConflictoUnicidadException` ante un
correo repetido), las pruebas de los servicios darían resultados distintos con uno y con otro.

Por eso los dobles en memoria reproducen también los errores de negocio de Oracle: `RepositorioCredencialesEnMemoria`
tiene `SimularConflictoAlAgregar` para probar la carrera de dos configuraciones iniciales.

# I — Principio de segregación de interfaces (ISP)

**Definición sencilla:** es mejor **varias interfaces pequeñas** que una grande que obligue a implementar métodos que no se
usan.

**Evidencia:** no hay un «IRepositorio» gigante. Hay un contrato por tema, cada uno con lo que su servicio necesita:

{{tipos:Lógica (contratos)}}

`ServicioInformeInstitucional` recibe solo `IRepositorioUsuarios`, `IRepositorioInformeInstitucional` y, opcionalmente, `IRepositorioConsultasVision`; no puede tocar
ecopuntos ni credenciales aunque quisiera.

# D — Principio de inversión de dependencias (DIP)

**Definición sencilla:** las piezas importantes (las reglas de negocio) no deben depender de detalles técnicos (Oracle,
Telegram, Ollama). Ambos deben depender de **abstracciones**.

**Evidencia:** las interfaces `IRepositorio...`, `IClasificadorMaterial`, `ICanalDispositivo` e `IHasherContrasenas` están
en el proyecto **Lógica** (`src/EcoAccess.P3.Logica/Contratos/`). El proyecto **Datos** las implementa. Por eso la
flecha de dependencia va de Datos hacia Lógica, y no al revés:

| Proyecto | Puede referenciar | Comprobado por |
|---|---|---|
| Entidades | nada | `PruebasArquitectura` |
| Lógica | Entidades | `PruebasArquitectura` |
| Datos | Lógica, Entidades | `PruebasArquitectura` |
| Presentación y Bot | todos | raíz de composición |

Los objetos concretos se crean en un solo lugar, la **raíz de composición**:

- `src/EcoAccess.P3.Bot/Program.cs`, por ejemplo `new ServicioInteracciones(usuarios, repositorioInteracciones, contenedores, materiales, TimeProvider.System)`.
- `src/EcoAccess.P3.Presentacion/Program.cs` para la aplicación de escritorio.

Hasta el reloj es una abstracción (`TimeProvider`): en producción es `TimeProvider.System` y en las pruebas un
`RelojAjustable`, que permite probar vencimientos y bloqueos sin esperar.

# Los nueve patrones GRASP

## 1. Experto en información

**Idea:** la responsabilidad se asigna a la clase que **tiene la información** necesaria.

| Pregunta | Experto | Método |
|---|---|---|
| ¿Esta cuenta tiene el rol Operario? | `Usuario` (tiene la lista de roles) | `TieneRol` |
| ¿Cuántos ecopuntos da este depósito? | `ReglaEcopuntos` (tiene puntos y si son por unidad) | `CalcularPuntos` |
| ¿La orden puede pasar a «Detectada»? | `OrdenApertura` (conoce su estado) | `PuedeAvanzarA` |
| ¿La credencial está bloqueada ahora? | `CredencialAcceso` | `EstaBloqueada` |
| ¿Qué tan comprobado está el depósito? | `Deposito` | propiedad `EstadoRegistro` |

{{codigo:src/EcoAccess.P3.Entidades/ReglaEcopuntos.cs:34:46}}

## 2. Creador

**Idea:** crea un objeto la clase que lo **contiene, registra o usa de cerca**, o que tiene los datos para iniciarlo.

`ServicioInteracciones` crea la `Interaccion` (línea 158), la `OrdenApertura` (línea 265) y el `Deposito` (líneas 912
y 928), porque es quien registra el ciclo completo de una solicitud y tiene los datos para cada uno.
`Interaccion.AperturaManual(...)` es un **método de fábrica** de la propia entidad: crea una interacción manual ya cerrada.

## 3. Controlador

**Idea:** un objeto que **no es de la interfaz** recibe los eventos del sistema y coordina el trabajo.

- En el escritorio, los manejadores de botones (por ejemplo `FormConfiguracionInicial.BotonAceptar_Click`) no contienen reglas: llaman a un servicio de la lógica (`ServicioAutenticacion`).
- En el bot, `ProcesadorUpdates.ProcesarAsync` recibe cada mensaje de Telegram y lo entrega a `ServicioConversacionTelegram.ProcesarAsync`, que coordina el flujo.
- Para la caneca, `ServicioInteracciones.RegistrarEventoAsync` es el controlador de los eventos del dispositivo.

## 4. Bajo acoplamiento

**Idea:** que las clases dependan de pocas otras, y de forma que un cambio no se propague.

Evidencia: los servicios dependen de interfaces. El formulario del dashboard no conoce SQL: pide un `InformeInstitucional`
al servicio. Las consultas a las vistas `ea_r_*` viven solo en `RepositorioInformeInstitucionalOracle`: cambiar una vista no exige tocar el formulario.

## 5. Alta cohesión

**Idea:** que cada clase haga cosas relacionadas entre sí.

Evidencia: `ServicioConversacionTelegram` se reparte en cuatro archivos parciales, cada uno con un tema (conversación,
flujo Eco, cantidades). `CatalogoObjetos` solo contiene la lista cerrada de objetos que la IA puede nombrar.

## 6. Polimorfismo

**Idea:** cuando el comportamiento varía según el tipo, se usan tipos distintos con la misma interfaz, no un `if` por tipo.

Evidencia: cinco implementaciones de `IClasificadorMaterial` y tres de `ICanalDispositivo` (ver documento 02).

## 7. Fabricación pura

**Idea:** una clase que no representa nada del dominio, pero que se inventa para mantener la cohesión y el bajo acoplamiento.

Evidencia: los repositorios (`RepositorioUsuariosOracle`...), `FabricaConexionesOracle`, `ComandosOracle`,
`HasherPbkdf2`, `GeneradorTokensAleatorio` y `PreparadorImagen`. En un reciclaje real no existe un «repositorio»; existe
porque separa la persistencia de las reglas.

## 8. Indirección

**Idea:** poner un intermediario para que dos piezas no se conozcan directamente.

Evidencia: `ICanalDispositivo` está entre la lógica y la caneca. La lógica emite un `ComandoApertura`; el canal
(simulado, HTTP local o serial USB) lo entrega. Además, `ProtocoloDispositivoHmac` firma cada mensaje, de modo que
el ESP32 no confía en la red.

## 9. Variaciones protegidas

**Idea:** identificar lo que puede cambiar y protegerlo detrás de una interfaz estable.

Evidencia:

- El proveedor de visión puede cambiar (local, Groq, otro): está detrás de `IClasificadorMaterial`, y el vocabulario de la IA está protegido por `CatalogoObjetos` (lista cerrada) e `InterpretadorNombreObjeto`. Una palabra inventada por el modelo no llega a la base.
- Oracle puede responder con errores técnicos: `TraductorErroresOracle` los convierte en excepciones del dominio.
- La matriz de permisos puede cambiar: `MatrizPermisos` la centraliza y `MatrizPermisosTests` comprueba que los servicios reales la cumplan.

# Tabla resumen

| Principio | Clase de ejemplo | Método o elemento | Ruta |
|---|---|---|---|
| SRP | `HasherPbkdf2` | `Calcular`, `Verificar` | `Logica/Servicios/HasherPbkdf2.cs` |
| OCP | `SelectorVision` | nueva implementación de `IClasificadorMaterial` | `Bot/Vision/SelectorVision.cs` |
| LSP | `RepositorioUsuariosEnMemoria` | sustituye a `RepositorioUsuariosOracle` | `tests/unitarias/.../Dobles/RepositoriosEnMemoria.cs` |
| ISP | `IRepositorioInformeInstitucional` | contrato pequeño | `Logica/Contratos/InformeInstitucional.cs` |
| DIP | `ServicioAutenticacion` | constructor con interfaces | `Logica/Servicios/ServicioAutenticacion.cs` |
| Experto | `ReglaEcopuntos` | `CalcularPuntos` | `Entidades/ReglaEcopuntos.cs` |
| Creador | `ServicioInteracciones` | crea `OrdenApertura` | `Logica/Servicios/ServicioInteracciones.cs:265` |
| Controlador | `ProcesadorUpdates` | `ProcesarAsync` | `Bot/ProcesadorUpdates.cs` |
| Bajo acoplamiento | `FormDashboardInstitucional` | solo usa el servicio | `Presentacion/FormDashboardInstitucional.cs` |
| Alta cohesión | `CatalogoObjetos` | lista cerrada | `Logica/Servicios/CatalogoObjetos.cs` |
| Polimorfismo | `IClasificadorMaterial` | `ClasificarAsync` | `Logica/Contratos/IClasificadorMaterial.cs` |
| Fabricación pura | `FabricaConexionesOracle` | `AbrirAsync` | `Datos/Oracle/FabricaConexionesOracle.cs` |
| Indirección | `ICanalDispositivo` | `EnviarAperturaAsync` | `Logica/Contratos/ICanalDispositivo.cs` |
| Variaciones protegidas | `TraductorErroresOracle` | ORA-00001 → excepción del dominio | `Datos/Oracle/TraductorErroresOracle.cs` |

# Preguntas probables del profesor

**¿Dónde se ve la inversión de dependencias?** En que `IRepositorioUsuarios` está en Lógica y `RepositorioUsuariosOracle`
en Datos. La regla de negocio define qué necesita y la base de datos se adapta.

**¿Qué patrón GRASP justifica que exista un repositorio?** Fabricación pura: no es un concepto del reciclaje, se inventa
para separar la persistencia.

**¿Hay algo que viole SRP?** Los servicios grandes (`ServicioInteracciones`, `ServicioConversacionTelegram`). Se mitigó con
archivos parciales por tema, pero dividirlos en clases sería la mejora siguiente.
