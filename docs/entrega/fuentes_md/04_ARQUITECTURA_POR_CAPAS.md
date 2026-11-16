---
archivo: 04_ARQUITECTURA_POR_CAPAS
titulo: Arquitectura por capas de EcoAccess P3
subtitulo: Capas, dependencias, componentes y los ocho recorridos completos del sistema con clases y métodos reales
codigo: 04
---

# Qué es una arquitectura por capas

Una **arquitectura por capas** divide el programa en niveles, y cada nivel tiene un trabajo. Funciona como un restaurante:
el mesero (presentación) atiende a la persona, la cocina (lógica) decide cómo se prepara el plato y la despensa (datos)
guarda los ingredientes. El mesero no cocina y la cocina no atiende mesas.

EcoAccess P3 tiene **cuatro capas** más una base de datos externa:

![Diagrama de capas de EcoAccess P3](../../fuentes_md/img/arquitectura_capas.png)

*Figura 1. Capas y dependencias. Las flechas indican «depende de». Datos depende de Lógica porque implementa sus contratos.*

{{proyectos}}

# Cada capa en detalle

## Entidades (`src/EcoAccess.P3.Entidades`)

**Qué contiene:** las clases del dominio, es decir, las cosas de las que trata el proyecto: `Usuario`, `Material`,
`Contenedor`, `Interaccion`, `OrdenApertura`, `Deposito`, `ReglaEcopuntos`, `VinculoTelegram`, etc.

**Por qué está separada:** las reglas que siempre deben cumplirse (un usuario tiene al menos un rol, una orden solo
avanza de estado) viven aquí y no dependen de ninguna tecnología. Se pueden probar sin base de datos, sin Telegram y sin
ventanas.

**De qué depende:** de nada (lo comprueba `PruebasArquitectura`).

## Lógica (`src/EcoAccess.P3.Logica`)

**Qué contiene:**

- **Servicios** (`Servicios/`): coordinan cada caso de uso, por ejemplo `ServicioAutenticacion`, `ServicioInteracciones`, `ServicioEcopuntos`, `ServicioConversacionTelegram`.
- **Contratos** (`Contratos/`): las interfaces que la lógica necesita del exterior, como `IRepositorioUsuarios`, `IClasificadorMaterial` e `ICanalDispositivo`.
- **Reglas puras**: `ResolutorDestino`, `PoliticaAperturaAutomatica`, `CatalogoObjetos`, `CatalogoPesosEstimados`, `MatrizPermisos`.

**Regla de oro:** cada servicio **revalida al actor** en cada operación con `ControlAcceso`. No confía en lo que recuerde
la ventana: si un administrador desactiva una cuenta, la siguiente operación de esa cuenta ya se rechaza (RN-15).

**De qué depende:** solo de Entidades.

## Datos (`src/EcoAccess.P3.Datos`)

**Qué contiene:** los repositorios que hablan con Oracle usando ODP.NET (`Oracle.ManagedDataAccess`), siempre con
**parámetros enlazados** (nunca concatenando texto de la persona en el SQL).

- `Oracle/FabricaConexionesOracle`: abre conexiones con la cadena guardada en los secretos de usuario.
- `Oracle/ComandosOracle`: crea comandos con `BindByName = true`.
- `Oracle/TraductorErroresOracle`: convierte ORA-00001 en `ConflictoUnicidadException`.
- `Repositorios/`: una clase por contrato (`RepositorioUsuariosOracle`, `RepositorioInteraccionesOracle`...).
- `Simulacion/GeneradorLoteSimulacion`: genera el lote experimental con los mismos servicios del bot.

**De qué depende:** de Lógica (implementa sus contratos) y de Entidades.

## Presentación

Hay **dos** programas de presentación que comparten Lógica, Datos y Entidades:

1. **Aplicación de escritorio** (`src/EcoAccess.P3.Presentacion`, Windows Forms): para operarios y administradores, y una bienvenida para ciudadanos e invitados.
2. **Bot de Telegram** (`src/EcoAccess.P3.Bot`, consola): atiende a los ciudadanos, analiza fotos con la IA local y, si se configura, se comunica con el dispositivo de la caneca.

Cada uno tiene su **raíz de composición** (`Program.cs`), donde se crean los repositorios y servicios y se conectan.

## Compatibilidad (`src/EcoAccess.P3.Compatibilidad`)

Proyecto pequeño que da a .NET Framework 4.8 algunas funciones de .NET moderno (por ejemplo `Compat.IsNullOrWhiteSpace`,
`Compat.ToHexString`, `Compat.Sha256`). Lo referencian todos los proyectos desde `Directory.Build.props`.

# Cómo se comunican las capas

1. La **presentación** recibe una acción (clic en un botón, mensaje de Telegram).
2. Llama a un **servicio** de la lógica con datos simples (identificador del actor, textos, números).
3. El servicio comprueba permisos y reglas, y usa **entidades** para validar.
4. Para leer o guardar, el servicio llama a un **contrato** (`IRepositorio...`), que en ejecución es un repositorio Oracle.
5. El servicio devuelve un `Resultado` o `Resultado<T>`: éxito con el valor, o rechazo con `MotivoRechazo` y un mensaje para la persona.
6. La presentación muestra el resultado; nunca muestra el texto técnico de Oracle.

# Recorridos completos del sistema

Los siguientes recorridos muestran, paso a paso, qué clases y métodos intervienen en las operaciones principales. Los
nombres son exactos y se comprobaron en el código del commit indicado en la portada.

## Recorrido A: registro de un ciudadano

![Recorrido A](../../fuentes_md/img/recorrido_a_registro.png)

1. En `FormRegistroCiudadano`, el botón **Crear mi cuenta** (`_crear.Click`) llama a `RegistrarAsync()` del formulario.
2. El formulario llama a `ServicioRegistroCiudadanos.RegistrarAsync(nombre, correo, clave, confirmación, consentimiento)`.
3. El servicio valida los datos creando un `Usuario` con rol Ciudadano (si algo es inválido, la entidad lanza `ArgumentException` y el servicio lo convierte en un rechazo con mensaje).
4. `HasherPbkdf2.Calcular` produce hash, sal e iteraciones.
5. `RepositorioRegistroCiudadanosOracle.RegistrarAsync` inserta, en **una sola transacción**, las filas de `USUARIO`, `USUARIO_ROL` (rol 1) y `CREDENCIAL_ACCESO`.
6. Si el correo ya existe, Oracle responde ORA-00001, el repositorio lanza `ConflictoUnicidadException` y el servicio responde «ya existe una cuenta con ese correo».

## Recorrido B: inicio de sesión del administrador

![Recorrido B](../../fuentes_md/img/recorrido_b_inicio_sesion.png)

1. `FormInicioSesion` llama a `ServicioAutenticacion.IniciarSesionPublicaAsync(correo, contraseña)`.
2. `IRepositorioCredenciales.ObtenerPorCorreoAsync` trae la credencial. Si no existe, se calcula igual un hash «de relleno» para que el tiempo de respuesta no revele si el correo existe.
3. Si la credencial está bloqueada (`EstaBloqueada`), se rechaza. Si la contraseña falla, `RegistrarFalloAsync` suma un intento; al quinto (`MaximoIntentos = 5`) bloquea 15 minutos.
4. Se carga el `Usuario` y se comprueba que esté activo y tenga un rol con acceso a la aplicación.
5. Se devuelve una `SesionActiva` (id, nombre, roles). `Program.cs` crea `FormPrincipal`, que muestra el menú según los roles.

**Primer administrador.** Si la base no tiene ninguna cuenta de administrador, el enlace «Configuración inicial» abre
`FormConfiguracionInicial`, que llama a `ServicioAutenticacion.CrearAdministradorInicialAsync(nombre, correo, contraseña)`.
Ese método solo funciona mientras no exista ningún administrador (es de un solo uso), no promueve cuentas existentes y
guarda solo el hash de la contraseña.

## Recorrido C: el operario consulta las estadísticas institucionales

![Recorrido C](../../fuentes_md/img/recorrido_c_dashboard.png)

1. `FormDashboardInstitucional.ConsultarAsync()` arma un `FiltroInforme` con el período, la agrupación, la caneca, la ubicación, el material, la categoría y el modo de datos (operativos o demostración).
2. `ServicioInformeInstitucional.ConsultarAsync` exige rol Operario o Administrador (`ExigirAlgunRolAsync`) y valida el período (máximo 366 días, en hora de Colombia).
3. `RepositorioInformeInstitucionalOracle.ConsultarAsync` consulta las vistas `ea_r_base_depositos`, `ea_r_base_puntos`, `ea_r_solicitudes` y `ea_r_opiniones` filtrando por `es_ensayo`.
4. Se consulta también el período anterior para la comparación y `AgruparAsync` por día, material, caneca y punto.
5. El formulario dibuja ocho tarjetas, gráficos y la tabla comparativa.

Medición real en Oracle Cloud (11/11/2026, prueba `ConsultasDelTableroInstitucional_RespondenEnMenosDeCincoSegundos`):
el tablero completo tarda **0,96 s** en modo operativo y **0,23 s** en modo experimental.

## Recorrido D: Telegram recibe una foto

![Recorrido D](../../fuentes_md/img/recorrido_d_foto.png)

1. `BuclePolling.EjecutarAsync` pide mensajes a Telegram (long polling) y entrega cada uno a `ProcesadorUpdates.ProcesarAsync`.
2. `ServicioConversacionTelegram.ProcesarAsync` reconoce la foto y llama a `ServicioClasificacionImagen.ClasificarAsync`.
3. El clasificador (normalmente `ClasificadorOllamaLocal`, modelo `lfm3-vl` en Ollama) devuelve el nombre del objeto. `InterpretadorNombreObjeto.Clave` lo traduce a una clave cerrada de `CatalogoObjetos` («plastic_bottle», «can»...).
4. El servicio de clasificación consulta en Oracle si ese material existe, está habilitado y si la caneca lo admite.
5. `ServicioConversacionTelegram.ConDestinoEstacionAsync` usa `ResolutorDestino` para decidir el color: blanca (aprovechables), verde (orgánicos), negra (no aprovechables), posconsumo (peligrosos) o «no es residuo».
6. Si `PoliticaAperturaAutomatica.Permite` (identificación clara, material admitido y nombre del objeto que corrobora el material), se inicia la interacción y se autoriza la apertura. Si no, Eco solo orienta.
7. La respuesta (`RespuestaBot`) vuelve como texto y botones.

## Recorrido E: el ciudadano consulta sus ecopuntos

![Recorrido E](../../fuentes_md/img/recorrido_e_ecopuntos.png)

1. `IntencionesTelegram` reconoce frases como «mis ecopuntos» o «mi saldo».
2. `ServicioEcopuntos.ConsultarMiosAsync(actor)` exige un ciudadano habilitado (activo y con consentimiento vigente, RN-02).
3. `RepositorioEcopuntosOracle.ConsultarAsync` suma los movimientos de `MOVIMIENTO_ECOPUNTOS`. El saldo **es la suma**: nunca se guarda un saldo editable.
4. Las reglas (`REGLA_ECOPUNTOS`) solo se aplican a **depósitos verificados**, con unidades verificadas.

## Recorrido F: modificar un botón

![Recorrido F](../../fuentes_md/img/recorrido_f_boton.png)

Este recorrido muestra cómo un botón llega a Oracle, usando el botón **Guardar** de la configuración inicial:

1. En `FormConfiguracionInicial.Designer.cs`, el diseñador conecta el evento: `botonAceptar.Click += BotonAceptar_Click;`.
2. `BotonAceptar_Click` compara contraseña y confirmación (una ayuda de la pantalla), bloquea el panel y llama al servicio.
3. El servicio aplica las reglas y guarda.
4. El formulario cierra con `DialogResult.OK` o muestra el mensaje en `etiquetaEstado`.

Para cambiar el texto del botón: abra `FormConfiguracionInicial.cs` en el diseñador, seleccione el botón, cambie la
propiedad **Text** en la ventana **Propiedades** (F4) y compile. El documento 09 trae ejercicios paso a paso.

## Recorrido G: Power BI

![Recorrido G](../../fuentes_md/img/recorrido_g_powerbi.png)

Power BI se conecta a Oracle con una cuenta de **solo lectura** para informes (`database/00_administracion/cuenta_informes_powerbi.sql`)
y lee vistas, no tablas. Las vistas (V013, V015 y V019) traen la columna `es_ensayo`, y con ella cada página puede excluir
los datos de simulación. El documento 12 detalla el modelo y sus limitaciones.

## Recorrido H: la ventana de diez segundos simulada

![Recorrido H](../../fuentes_md/img/recorrido_h_ventana.png)

1. `ServicioDespachoOrdenes` envía el `ComandoApertura` por `ICanalDispositivo`. En simulación es `EstacionSimulada`, que tiene **un ESP32 simulado por tapa**: la orden para la blanca nunca llega a la verde ni a la negra.
2. La `CanecaSimulada` de esa tapa responde con eventos: acuse, apertura, detección (o cierre sin sensor) y cierre.
3. Cada evento entra por `ServicioInteracciones.RegistrarEventoAsync`. La ventana (`VentanaDepositoSegundos = 10`) se mide con la hora del **servidor**.
4. Se crea **un solo depósito por orden** (restricción `UNIQUE` en `DEPOSITO.ID_ORDEN`). Un evento repetido o tardío queda en `Revision`, sin duplicar nada.
5. Sin sensor, el depósito queda `DECLARADA_SIN_SENSOR`; nunca se presenta como ingreso detectado ni como pesaje.

**Importante:** este recorrido está **verificado en simulación** (`EstacionSimuladaTests`). No se cargó firmware ni se movió
ningún motor en la misión de cierre.

# Componentes del bot

El proyecto Bot agrupa varias responsabilidades en carpetas:

| Carpeta | Qué hace | Clases principales |
|---|---|---|
| raíz | arranque, polling, webhook, página de registro | `Program`, `BuclePolling`, `ProcesadorUpdates`, `PaginaRegistro`, `ServidorRegistro` |
| `Telegram/` | cliente de la API de Telegram y asistente | `ClienteTelegramHttp`, `AsistenteEco`, `TraductorUpdates` |
| `Vision/` | motores de visión y evaluación | `ClasificadorOllamaLocal`, `ClasificadorGroq`, `SelectorVision`, `EvaluacionObjetos` |
| `Dispositivos/` | comunicación firmada con la caneca | `ProtocoloDispositivoHmac`, `BridgeLocal`, `EnlacePuertoSerial` |
| `Transportes/` | utilidades de lectura y registro seguro | `LecturaAcotada`, `CopiadorRegistroSeguro` |

# Preguntas probables

**¿Por qué Datos depende de Lógica y no al revés?** Porque los contratos están en Lógica: la regla de negocio decide qué
necesita, y la persistencia se adapta. Así se puede cambiar Oracle sin tocar las reglas (inversión de dependencias).

**¿Dónde se valida un permiso?** En el servicio, con `ControlAcceso`, en cada operación. Los menús de Windows Forms solo
ocultan opciones por comodidad; aunque alguien llamara al servicio directamente, el servicio lo rechazaría.

**¿Qué pasa si Oracle no responde?** El repositorio lanza la excepción de ODP.NET. La presentación la atrapa y muestra un
mensaje general («No se pudo conectar con la base de datos...»), sin detalles técnicos ni la cadena de conexión.
