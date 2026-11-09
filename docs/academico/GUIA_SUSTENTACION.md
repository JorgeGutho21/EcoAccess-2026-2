# Guía de sustentación — tercer corte (ADO.NET, DAO/Repository, tres capas)

Cada respuesta remite al código de la rama `desarrollo/auditoria-tercer-corte`. Explica con tus palabras; abre el archivo
indicado si el docente pide verlo. Las comparaciones con el curso usan el ejemplo `Producto` de capadatos.vercel.app.

**1. ¿Qué es ADO.NET?** Es la biblioteca de .NET para trabajar con bases de datos relacionales escribiendo el SQL uno
mismo: conexión, comando, parámetros, lector y transacción. No es un ORM. EcoAccess usa el proveedor de Oracle
(ODP.NET, paquete `Oracle.ManagedDataAccess` en `src/EcoAccess.P3.Datos/EcoAccess.P3.Datos.csproj`); las clases son las
mismas que en la guía con prefijo Oracle en lugar de Sql.

**2. ¿Por qué Oracle?** El proyecto de aula se diseñó sobre Oracle Autonomous Database (gratuito, en la nube, con
conexión cifrada por wallet) para que la app de escritorio y el bot de Telegram compartan los mismos datos. Para
desarrollo local existe el destino `localhost:1522/PRUEBAP3`, que es el valor de `App.config`.

**3. ¿Cómo se establece una conexión?** `Program.cs` (Presentacion) lee la parte pública con
`System.Configuration.ConfigurationManager.ConnectionStrings["OracleP3"]` desde `App.config`, y la cadena completa
(con contraseña) de los User Secrets del perfil de Windows. `CadenaConexionOracle.Resolver` decide cuál usar y rechaza
una contraseña escrita en App.config. La cadena resultante se entrega a `new FabricaConexionesOracle(cadena)`. Es lo mismo
que la clase `Conexion` de la guía, salvo que la contraseña no se versiona.

**4. ¿Cómo funciona OracleConnection?** `FabricaConexionesOracle.AbrirAsync` crea `new OracleConnection(cadena)`, la abre
y la devuelve; si falla, la libera. Cada repositorio la usa con `using var conexion = await _fabrica.AbrirAsync(ct)`, así
se cierra al terminar el método y ODP.NET la devuelve al pool.

**5. ¿Cómo funciona OracleCommand?** `ComandosOracle.Crear(conexion, sql, transaccion)` crea el comando, pone
`BindByName = true` (Oracle enlaza por posición si no se indica) y lo asocia a la transacción. Se ejecuta con
`ExecuteReaderAsync` (consultas), `ExecuteNonQueryAsync` (INSERT/UPDATE) o `ExecuteScalarAsync` (un valor).

**6. ¿Qué es un DataReader?** Un cursor de solo avance que lee fila por fila. Ejemplo:
`RepositorioMaterialesOracle.ListarAsync` hace `using DbDataReader lector = await comando.ExecuteReaderAsync(...)` y
convierte cada fila en `Material` con el método privado `Leer(DbDataReader)` (el `Mapear` de la guía).

**7. ¿Qué es DAO?** Un objeto que encapsula el acceso a datos de una entidad, de modo que el resto del sistema no sabe
qué motor hay debajo. En EcoAccess ese papel lo cumplen las clases `Repositorio*Oracle` de `src/EcoAccess.P3.Datos/Repositorios`.

**8. ¿Qué es Repository?** Una abstracción tipo colección de objetos de dominio que la lógica usa sin conocer la
persistencia. En EcoAccess son las interfaces `IRepositorio*` de `src/EcoAccess.P3.Logica/Contratos`, por ejemplo
`IRepositorioMateriales` (`ListarAsync`, `AgregarAsync`, `HabilitarAsync`…).

**9. ¿Diferencia entre DAO y Repository?** DAO piensa en tablas y SQL; Repository piensa en el dominio. En la guía,
`ProductoRepository` solo delega en `ProductoDAO`. En EcoAccess la interfaz (Repository) está en Lógica y su única
implementación Oracle (que hace el trabajo del DAO) está en Datos. No se creó un `MaterialDAO` adicional porque sería
un pase directo sin comportamiento; la ventaja de probar con datos falsos se mantiene (ver
`docs/academico/MATRIZ_CUMPLIMIENTO_TERCER_CORTE.md` §3).

**10. ¿Cómo están distribuidas las capas?** Presentación (`EcoAccess.P3.Presentacion`, WinForms) → Lógica
(`EcoAccess.P3.Logica`) → Entidades. Datos (`EcoAccess.P3.Datos`) implementa los contratos de Lógica. `Program.cs` de
Presentación y de Bot son los únicos que conocen las cuatro (raíces de composición). Diagrama:
`DIAGRAMAS_UML.md` §2.

**11. ¿Qué es una interfaz?** Un contrato de métodos sin implementación. `IRepositorioMateriales` dice qué se puede hacer
con los materiales; `RepositorioMaterialesOracle` dice cómo con Oracle; las pruebas usan otra implementación en memoria.

**12. ¿Cómo se aplica la inyección de dependencias?** Por constructor y a mano, sin contenedor:
`new ServicioMateriales(usuarios, materiales, contenedores)` en `Presentacion/Program.cs`. El servicio recibe interfaces;
el formulario recibe el servicio: `new FormMateriales(servicio, idActor)`.

**13. ¿Dónde se aplican SOLID y GRASP?**
- S: `ComandosOracle` solo crea comandos; `TraductorErroresOracle` solo traduce errores; `CadenaConexionOracle` solo resuelve la cadena.
- O: un nuevo proveedor visual implementa `IClasificadorMaterial` sin modificar `ServicioClasificacionImagen`.
- L: cualquier `IRepositorioMateriales` (Oracle o en memoria) sirve al servicio.
- I: interfaces por agregado (`IRepositorioMateriales`, `IRepositorioUsuarios`…) en lugar de una sola gigante.
- D: Lógica define `IRepositorio*` y Datos depende de ellas (`EcoAccess.P3.Datos.csproj` referencia a Logica).
- GRASP: Experto en información (`Material.Habilitar()` cambia su propio estado), Creador
  (`ServicioInteracciones.IniciarAsync` crea la interacción y la orden), Controlador (`Servicio*` reciben los eventos de
  la GUI y del bot), Bajo acoplamiento y Alta cohesión (capas y repositorios por agregado), Indirección (`ICanalDispositivo`
  entre la lógica y el ESP32).

**14. ¿Cómo funciona un CRUD?** Con materiales: crear `ServicioMateriales.RegistrarAsync` → `RepositorioMaterialesOracle.AgregarAsync`
(INSERT … RETURNING id INTO :id); leer `ListarAsync`/`ObtenerPorIdAsync`; actualizar y eliminar lógicamente
`HabilitarAsync`/`DeshabilitarAsync` (UPDATE). Igual que el borrado lógico `Activo = 0` de la guía.

**15. ¿Cómo se evitan las inyecciones SQL?** Todo valor viaja como parámetro: `comando.Agregar("codigo", OracleDbType.Varchar2, codigo)`
con `:codigo` en el SQL. Lo único que se compone en el texto son nombres de columnas y filtros constantes del propio
repositorio. Lo comprueba `PruebasTercerCorte.Datos_CreaComandosSoloConBindByNameYSinConcatenarValores`.

**16. ¿Cómo se implementan las transacciones?** `using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(ct)`,
los comandos se crean con esa transacción, y al final `CommitAsync`; ante error se hace `RollbackAsync` o el `using` la
revierte. Ejemplo: `RepositorioInteraccionesOracle.EmitirOrdenAsync` comprueba la clave de idempotencia y registra la
orden en una sola transacción.

**17. ¿Cómo se maneja una excepción Oracle?** `TraductorErroresOracle` convierte números ORA en excepciones de dominio:
ORA-00001 → `ConflictoUnicidadException`; ORA-02291 → referencia inexistente; ORA-00054/30006/00060 → espera o
interbloqueo. El servicio responde un `Resultado.Rechazo` y la GUI muestra un mensaje sin la cadena ni el texto interno.

**18. ¿Cómo se conecta WinForms con la lógica?** El formulario recibe el servicio por constructor, llama al método y
muestra el `Resultado`. Ej.: `FormMateriales` → `ServicioMateriales.HabilitarAsync(idActor, idMaterial)`. No hay SQL en
formularios (`PruebasTercerCorte.Presentacion_NoEscribeSqlNiUsaTiposAdoNet`).

**19. ¿Cómo se integra Telegram sin duplicar reglas?** El bot es otra raíz de composición: `src/EcoAccess.P3.Bot/Program.cs`
construye los mismos servicios de Lógica que la GUI (`ServicioInteracciones`, `ServicioMateriales`…). Los adaptadores de
Telegram no tocan Oracle (`PruebasArquitectura.Bot_UsaDatosSoloEnProgram`).

**20. ¿Cómo se autoriza y registra la apertura del ESP32?** La Lógica emite una orden en Oracle; el bridge firma el
comando con HMAC (`ProtocoloDispositivoHmac`), consulta `ServicioInteracciones.SigueAutorizadoComandoAsync` justo antes de
enviarlo y registra cada recibo con `RegistrarEventoAsync`. El ESP32 abre, espera `ABIERTO_MS = 10000` y cierra solo;
el aviso de 10 s llega a Telegram después del evento de apertura (`MensajesTelegram.CanecaAbierta`).

**21. ¿Cómo se garantiza la integridad?** Claves primarias y foráneas, `UNIQUE` (correo, QR, clave de idempotencia,
`update_id`), `CHECK` de estados, transacciones y control de concurrencia optimista (`CambioConcurrenteException`). Ver
`database/02_tablas/01_crear_tablas_iniciales.sql` y `database/06_migraciones/`.

**22. ¿Qué pasa si se desconecta Oracle?** Al iniciar, `VerificadorConexionOracle.ComprobarAsync` devuelve
`ServicioNoDisponible` y se abre `FormActivacionOracle` en lugar de cerrar con error. Durante el uso, el servicio
responde un rechazo y el formulario muestra «No pudimos consultar Oracle…»; nada se registra a medias porque las
escrituras son transaccionales.

**23. ¿Qué pasa si falla el ESP32?** Sin recibo de apertura no se avisa de los 10 s; la orden vence
(`ServicioInteracciones.VencerOrdenesAsync`) o queda como fallida con evento de fallo, y el tablero lo muestra. El
cierre no depende de Telegram ni del PC: lo hace el firmware.

**24. ¿Cómo se comprueba que la arquitectura está separada?** Con pruebas automáticas: `PruebasArquitectura` revisa los
`ProjectReference` y las referencias compiladas de cada ensamblado; `PruebasTercerCorte` revisa que no haya SQL en
Presentación y que los comandos usen parámetros.

**25. Limitaciones actuales.** ESP32 sin prueba física en esta auditoría (simulador y pruebas de integración);
el depósito queda «pendiente de verificación» porque no hay sensor; no hay versión WPF; la exactitud visual depende del
modelo y se reporta aparte (`docs/vision/`); el PDF entregado solo conserva 4 de 16 páginas impresas.
