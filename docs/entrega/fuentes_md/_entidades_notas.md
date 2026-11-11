Notas didácticas por entidad. El generador las inserta en la ficha de cada entidad, después de las tablas que salen
del código. Cada sección empieza con «## NombreExacto» de la clase.

## Usuario
### 4. Problema que representa
EcoAccess necesita saber quién es cada persona para atribuirle depósitos y ecopuntos, para darle permisos según su rol y
para respetar su consentimiento de datos. `Usuario` es la cuenta de esa persona.

### 10. Relaciones
- Un usuario tiene uno o varios roles (`CodigoRol`): Ciudadano, Operario o Administrador.
- Su contraseña no vive aquí: vive en `CredencialAcceso` (tabla `CREDENCIAL_ACCESO`). Así, leer la tabla `USUARIO` nunca expone un hash.
- Su cuenta de Telegram se une con `VinculoTelegram`.
- Inicia interacciones (`Interaccion.IdUsuario`) y recibe movimientos de ecopuntos (`MovimientoEcopuntos.IdUsuario`).

### 11. Principios de POO aplicados
- **Encapsulamiento:** la lista de roles es privada (`_roles`) y solo se expone como `IReadOnlyCollection<CodigoRol>`. Nadie puede agregar un rol sin pasar por `AsignarRol`.
- **Invariantes en el constructor:** nombre válido, correo normalizado en minúsculas y al menos un rol. Un objeto `Usuario` inválido no puede existir.
- **Dos constructores:** uno para registrar una cuenta nueva (`Id` = 0) y otro para reconstruir una guardada, que aplica las mismas validaciones.

### 14. Ejemplo de creación
```csharp
var ana = new Usuario("Ana Pérez", "Ana.Perez@Unicesar.edu.co", CodigoRol.Ciudadano, DateTime.UtcNow);
// ana.CorreoElectronico es "ana.perez@unicesar.edu.co" (el constructor lo normaliza)
// ana.Id es 0 hasta que RepositorioUsuariosOracle.AgregarAsync lo guarda
```

### 15. Ejemplo de consulta
`ServicioUsuarios.ObtenerAsync(idActor, idUsuario)` revisa primero que el actor sea Administrador (RN-25, datos personales) y luego llama a `IRepositorioUsuarios.ObtenerPorIdAsync`.

### 16. Ejemplo de modificación
Agregar el rol Operario a una cuenta: el administrador usa `ServicioUsuarios.AsignarRolAsync(idActor, idUsuario, CodigoRol.Operario)`. El servicio revalida al actor, carga la cuenta, llama a `usuario.AsignarRol(...)` y el repositorio inserta la fila en `USUARIO_ROL`.

### 18. Pregunta probable del profesor
¿Por qué la contraseña no es una propiedad de `Usuario`?

### 19. Respuesta técnica
Por separación de responsabilidades y menor exposición: casi todas las pantallas leen usuarios, pero solo la autenticación necesita el hash. Al guardarlo en `CREDENCIAL_ACCESO`, una consulta de usuarios, un informe o Power BI nunca reciben el hash ni la sal.

### 20. Explicación sencilla
`Usuario` es como el carné de una persona: nombre, correo y qué puede hacer. La clave para entrar se guarda en otro sobre cerrado (`CredencialAcceso`), que solo abre la puerta de inicio de sesión.

## CredencialAcceso
### 4. Problema que representa
El personal (operarios y administradores) entra a la aplicación de escritorio con correo y contraseña. Guardar la contraseña tal cual sería peligroso: cualquiera que leyera la tabla la conocería.

### 9. Validaciones
- El hash y la sal deben tener la longitud exacta (`LongitudHash`, `LongitudSal`).
- Las iteraciones no pueden bajar de `IteracionesMinimas` (100 000).
- La contraseña nueva debe tener entre 12 y `LongitudMaximaContrasena` caracteres (la valida `ServicioAutenticacion`).
- `Hash` y `Sal` devuelven **copias**: quien recibe el arreglo no puede cambiar la credencial.

### 11. Principios de POO aplicados
Inmutabilidad: una credencial no se modifica; cuando cambia la contraseña se crea otra y el repositorio la reemplaza (`ReemplazarSecretoAsync`). Esto evita estados a medias.

### 14. Ejemplo de creación
```csharp
HashContrasena h = new HasherPbkdf2().Calcular("una-clave-larga-y-propia");
var credencial = new CredencialAcceso(idUsuario, h.Hash, h.Sal, h.Iteraciones, DateTime.UtcNow);
```

### 18. Pregunta probable del profesor
¿Qué es PBKDF2 y por qué se usa?

### 19. Respuesta técnica
PBKDF2 es una función de derivación de claves: aplica un hash (SHA-256) miles de veces con una sal aleatoria. La sal hace que dos personas con la misma contraseña tengan hashes distintos, y las 100 000 iteraciones hacen costoso probar contraseñas a fuerza bruta. Al iniciar sesión, `HasherPbkdf2.Verificar` recalcula el hash con la misma sal y compara en tiempo constante.

### 20. Explicación sencilla
No se guarda la llave, se guarda una «huella» de la llave. Para comprobar una llave se le saca la huella otra vez y se comparan las huellas.

## Material
### 4. Problema que representa
La IA puede decir cualquier palabra («botella», «bottle», «envase»). EcoAccess necesita un vocabulario cerrado: un catálogo de materiales con código estable (`PET`, `CARTON`, `LATA_ALUMINIO`…) y una categoría según la Resolución 2184 de 2019.

### 10. Relaciones
- `CONTENEDOR_MATERIAL` dice qué caneca admite cada material.
- `REGLA_ECOPUNTOS` da puntos por material.
- `Interaccion` guarda el material sugerido por la IA y el confirmado; `Deposito`, el declarado y el verificado.

### 14. Ejemplo de creación
```csharp
var pet = new Material("PET", "Botella PET", CategoriaMaterial.Aprovechable, "Vacía y aplastada");
```

### 16. Ejemplo de modificación
Deshabilitar un material sin borrarlo (para conservar la historia): `material.Deshabilitar()` desde `ServicioMateriales`, que exige Administrador.

### 18. Pregunta probable del profesor
¿Por qué no se borran los materiales que ya no se usan?

### 19. Respuesta técnica
Porque hay depósitos y reglas antiguas que los referencian con claves foráneas. Borrarlos rompería la historia o exigiría borrar datos reales. Se deshabilitan (`Habilitado = false`) y dejan de ofrecerse.

### 20. Explicación sencilla
El catálogo es la lista oficial de «qué cosas se reciclan y cómo se llaman». La IA solo puede elegir de esa lista.

## Contenedor
### 4. Problema que representa
Cada caneca física tiene un QR pegado. El QR solo dice «soy esta caneca»: no autentica a nadie ni da permiso para abrir (RN-04).

### 9. Validaciones
- Una caneca nueva empieza **fuera de servicio**: primero se configuran sus materiales y después se pone en servicio (seguridad por defecto).
- `IdentificadorQr` debe tener entre 16 caracteres y el máximo que permite el parámetro `start` de Telegram.
- `DarDeBaja` es definitivo, y la historia se conserva.

### 15. Ejemplo de consulta
El bot recibe `/start EcoAccessCanecaPiloto1`, y `IRepositorioContenedores.ObtenerPorIdentificadorQrAsync` devuelve la caneca piloto (1093).

### 18. Pregunta probable del profesor
¿Qué pasa si alguien fotografía el QR y lo usa desde su casa?

### 19. Respuesta técnica
Puede iniciar la conversación, pero no abrir la caneca a distancia sin cumplir las demás reglas: cuenta habilitada, caneca en servicio, material admitido, una sola orden vigente por caneca y vencimiento corto. Además, abrir no es reciclar: el depósito solo se registra con el evento del sensor (o como declarado sin sensor, claramente marcado).

## Interaccion
### 4. Problema que representa
Es el «expediente» de una visita a una caneca: quién la pidió, por qué canal, qué sugirió la IA, qué material se confirmó y cómo terminó.

### 9. Validaciones
- Estados: `Iniciada` → `Autorizada` → `ConDeposito` o `SinDeposito`, o `Iniciada` → `Rechazada`. Los tres últimos son definitivos.
- La sugerencia de la IA (`IdMaterialSugerido`, `ConfianzaSugerida`) y el material confirmado (`IdMaterialConfirmado`) se guardan por separado: la sugerencia orienta, no decide.
- `CantidadDeclarada` es lo que la persona dice; nunca un conteo físico.

### 16. Ejemplo de modificación
`interaccion.Autorizar(idMaterial)` fija el material confirmado; quien comprueba que la caneca lo admite es `ServicioInteracciones.AutorizarAperturaAsync`.

### 18. Pregunta probable del profesor
¿Por qué hay material sugerido y material confirmado?

### 19. Respuesta técnica
Porque la confianza del modelo no está calibrada (puede decir 0,95 y equivocarse). Separar los dos datos permite medir cuánto acierta la IA y evita que una sugerencia errónea se convierta en un registro oficial.

## OrdenApertura
### 4. Problema que representa
Para abrir una tapa hace falta una orden única, con vencimiento y clave de idempotencia. Así un mensaje repetido no abre dos veces y un evento del sensor se atribuye a la orden correcta (RN-07, RN-16).

### 9. Validaciones
Solo avanza, nunca retrocede: `Emitida` → `Enviada` → `Acusada` → `Abierta` → `Detectada` → `Cerrada`, o termina en `Vencida` o `Fallida`. `PuedeAvanzarA` concentra esa regla.

### 18. Pregunta probable del profesor
¿Qué es la idempotencia?

### 19. Respuesta técnica
Que repetir una operación tenga el mismo efecto que hacerla una vez. La `ClaveIdempotencia` es única en Oracle: si el emisor reintenta con la misma clave, no se crea otra orden.

### 20. Explicación sencilla
Es como un tiquete numerado: si alguien lo presenta dos veces, la puerta solo se abre la primera.

## Deposito
### 4. Problema que representa
Registra que algo entró a la caneca dentro de la ventana de 10 segundos de una orden (RN-08). Hay como máximo un depósito por orden (RN-17).

### 9. Validaciones
- `PesoMedidoKg` solo lo llena una báscula; `PesoEstimadoKg` viene del catálogo versionado. Una foto nunca produce un peso medido.
- `Verificar` exige un verificador distinto de quien depositó; el rol lo comprueba la lógica.
- `EstadoRegistro` se **calcula**: `DeclaradaSinSensor`, `DetectadaPorSensor` o `Verificada`.

### 18. Pregunta probable del profesor
¿Por qué `EstadoRegistro` no es una columna de Oracle?

### 19. Respuesta técnica
Para tener una sola fuente de verdad: el estado se deduce de `SinSensor` y de si hay verificación. Si fuera una columna aparte, podría contradecir esos datos.

### 20. Explicación sencilla
El depósito tiene tres niveles de certeza: «lo dijo la persona», «lo vio el sensor» y «lo revisó un operario».

## EventoDispositivo
### 4. Problema que representa
Todo lo que reporta la caneca (acuse, apertura, detección, cierre, fallo) se guarda siempre, también si llega tarde o repetido, para auditar (RN-09).

### 9. Validaciones
`FechaRecepcionUtc` (hora del servidor) es la que mide la ventana; `FechaDispositivoUtc` es informativa porque el reloj del ESP32 puede estar desajustado. `Resultado` dice si el evento hizo avanzar la orden (`Aplicado`) o quedó en `Revision`.

## ReglaEcopuntos
### 4. Problema que representa
Los ecopuntos son un incentivo educativo, no dinero. Cada regla es una versión de tarifa por material, con vigencia (`DesdeUtc`, `HastaUtc`) y creador (un administrador).

### 9. Validaciones
- `CalcularPuntos(unidades)` usa **unidades verificadas**, nunca la cantidad declarada.
- Una regla no se modifica: para cambiar la tarifa se crea una versión nueva, y los movimientos antiguos siguen apuntando a la que los respaldó.

### 18. Pregunta probable del profesor
¿Por qué la nube mostraba 0 ecopuntos?

### 19. Respuesta técnica
Porque no había reglas vigentes: `T001__tarifas_ecopuntos_propuestas.sql` exige un administrador real como creador, y la base no tenía ninguno. Sin regla vigente, el sistema no acredita nada (es el comportamiento seguro).

## MovimientoEcopuntos
### 4. Problema que representa
Es el libro contable de los ecopuntos: créditos y reversos. El saldo es la suma de los movimientos; nunca se edita un movimiento para corregir un saldo, se agrega un reverso.

## VinculoTelegram
### 4. Problema que representa
Une una cuenta de EcoAccess con una cuenta de Telegram. El identificador numérico lo entrega Telegram en cada mensaje y no se puede escribir a mano: es el «contacto verificado» de RN-02.

### 18. Pregunta probable del profesor
¿Por qué no se borra el vínculo cuando alguien se desvincula?

### 19. Respuesta técnica
Se registra `FechaBajaUtc` y el vínculo deja de estar activo. Se conserva para auditar qué cuenta de Telegram hizo cada acción en su momento (RN-24).

## TokenVinculacion
### 4. Problema que representa
El bot entrega un enlace de un solo uso para registrarse en la página web. En Oracle solo se guarda el hash SHA-256 del token: leer la tabla no permite usar un enlace.

## ConversacionTelegram
### 4. Problema que representa
El bot atiende a muchas personas a la vez y cada una va en un paso distinto (sin caneca, caneca identificada, esperando confirmación, orden emitida). Esta entidad guarda ese paso con una versión optimista para que dos mensajes simultáneos no se pisen.

## PuntoInstalacion
### 4. Problema que representa
El lugar físico (por ejemplo, un bloque del campus). Está separado del contenedor para que, si una caneca se reubica, sus depósitos antiguos sigan asociados al lugar donde ocurrieron.

## AnalisisImagen
### 4. Problema que representa
Traza de cada análisis de foto: proveedor, modelo, versión, estado y material sugerido. **No guarda la foto** ni la respuesta libre del modelo.

## Opinion
### 4. Problema que representa
Valoración voluntaria (1 a 5 estrellas) con un identificador seudónimo del participante, no su correo ni su identificador de Telegram.

## Rol
### 4. Problema que representa
Fila del catálogo `ROL`. El usuario guarda el `CodigoRol`, y esta clase sirve para listar los roles con su descripción y comprobar que Oracle coincide con la enumeración.

## Validacion
### 4. Problema que representa
Comprobaciones que comparten varias entidades: texto requerido, texto opcional (vacío se vuelve null), fecha UTC obligatoria, identificador positivo y enumeración definida. Es `internal`: las demás capas solo ven el efecto, una `ArgumentException` clara.

### 18. Pregunta probable del profesor
¿Por qué `EnumDefinido`?

### 19. Respuesta técnica
En C# se puede convertir cualquier número a un enum, por ejemplo `(CodigoRol)99`. Esa conversión compila, pero el valor no existe. `EnumDefinido` lo rechaza antes de que llegue a Oracle.
