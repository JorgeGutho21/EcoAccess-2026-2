---
archivo: 01_MANUAL_CSHARP_DESDE_CERO
titulo: Manual de C# desde cero con EcoAccess P3
subtitulo: Variables, tipos, clases, colecciones, excepciones, async/await e interfaces explicados con el código real del proyecto
codigo: 01
---

# Cómo usar este manual

Este manual enseña C# usando EcoAccess P3 como ejemplo. Cada concepto tiene tres partes:

1. **Definición sencilla**, como se le explicaría a alguien que está empezando.
2. **Ejemplo mínimo**, de pocas líneas.
3. **Dónde aparece en EcoAccess**, con la ruta exacta del archivo para abrirlo en Visual Studio.

Para abrir un archivo en Visual Studio: menú **Ver → Explorador de soluciones**, despliegue el proyecto (por ejemplo
`EcoAccess.P3.Entidades`) y haga doble clic en el archivo. Para ir a una línea: **Ctrl+G** y escriba el número.

EcoAccess P3 está escrito en **C# 9** sobre **.NET Framework 4.8** (archivo `Directory.Build.props` en la raíz). Esa
combinación funciona en Visual Studio 2019 16.8 o posterior y en Visual Studio 2022.

# Qué es un programa en C#

Un programa en C# es un conjunto de archivos `.cs` que el **compilador** traduce a un lenguaje intermedio (IL). Ese IL
lo ejecuta el **CLR** (el motor de .NET). En EcoAccess hay varios **proyectos**, y cada uno produce un `.dll` (biblioteca)
o un `.exe` (programa):

{{proyectos}}

Un `.exe` necesita un punto de entrada, el método `Main`. La aplicación de escritorio lo tiene en
`src/EcoAccess.P3.Presentacion/Program.cs`. El bot usa **instrucciones de nivel superior**: el archivo
`src/EcoAccess.P3.Bot/Program.cs` empieza a ejecutarse desde su primera línea, sin escribir `Main`.

# Variables y tipos

Una **variable** es un nombre que guarda un valor. En C# toda variable tiene un **tipo**, que dice qué valores admite.

| Tipo | Qué guarda | Ejemplo en EcoAccess |
|---|---|---|
| `int` | número entero de 32 bits | `Usuario.Id`, `ReglaEcopuntos.Puntos` |
| `long` | entero de 64 bits | `VinculoTelegram.TelegramUserId` (los id de Telegram no caben en `int`) |
| `decimal` | número con decimales exactos | `Deposito.PesoEstimadoKg` (pesos sin errores de redondeo binario) |
| `bool` | verdadero o falso | `Usuario.Activo`, `Material.Habilitado` |
| `string` | texto | `Material.Codigo` («PET», «CARTON») |
| `DateTime` | fecha y hora | `Interaccion.FechaInicioUtc` (siempre en UTC) |
| `byte[]` | arreglo de bytes | `CredencialAcceso.Hash` |

**`var`** deja que el compilador deduzca el tipo: `var reloj = Stopwatch.StartNew();` sigue siendo una variable de tipo
`Stopwatch`; `var` no la vuelve dinámica.

## Tipos que admiten null

`int?` significa «un entero o nada». Se usa cuando la ausencia del dato tiene significado. Ejemplo real:
`Deposito.PesoMedidoKg` es `decimal?` porque solo una báscula lo llena; si no hay báscula queda `null`, y no se inventa un
cero.

Con `<Nullable>enable</Nullable>` (activado en `Directory.Build.props`), también los textos distinguen `string` (nunca
null) de `string?` (puede ser null). El compilador avisa si se usa un `string?` sin comprobarlo.

## Constantes

`const` fija un valor en tiempo de compilación. En `CredencialAcceso`:

```csharp
public const int IteracionesMinimas = 100_000;   // el guion bajo solo separa miles para leerlo mejor
public const int LongitudMinimaContrasena = 12;
```

Usar constantes con nombre evita «números mágicos». Si la política cambia, se cambia en un solo lugar y las pantallas
(por ejemplo `FormConfiguracionInicial`, que muestra «12 caracteres como mínimo») se actualizan solas.

# Operadores y expresiones

- Aritméticos: `+ - * / %`. Ejemplo: `kg por unidad × unidades` en `CatalogoPesosEstimados`.
- Comparación: `== != < > <= >=`.
- Lógicos: `&&` (y), `||` (o), `!` (no).
- **Operador condicional** `condicion ? a : b`. En `FormConfiguracionInicial.MostrarModo` se elige el texto de la explicación según haya que crear la cuenta o no.
- **Coalescencia** `??`: «si es null, usa esto». En el bot: `configuracion["Vision:Ollama:MantenerCargado"] ?? "15m"`.
- **`is` con patrones**: `if (await _acceso.ExigirRolAsync(...) is { } rechazo) return rechazo;` se lee «si el resultado no es null, llámalo `rechazo` y devuélvelo». Es el patrón que usan todos los servicios para revalidar permisos.

# Métodos

Un **método** es un bloque de código con nombre que recibe **parámetros** y puede devolver un valor.

```csharp
public bool EstaVigente(DateTime ahoraUtc) => ahoraUtc >= DesdeUtc && (HastaUtc is null || ahoraUtc < HastaUtc);
```

Ese método real de `ReglaEcopuntos` recibe una fecha y devuelve `true` si la regla está vigente. La flecha `=>` es un
**cuerpo de expresión**: equivale a `{ return ...; }`.

**Parámetros opcionales:** en `Deposito(int idOrden, ..., bool sinSensor = false)` quien no pasa `sinSensor` obtiene `false`.

# Clases y objetos

Una **clase** es un molde y un **objeto** es una cosa hecha con ese molde. `Usuario` es la clase y «la cuenta de Ana» es un
objeto. Se crea con `new`:

```csharp
var ana = new Usuario("Ana Pérez", "ana@unicesar.edu.co", CodigoRol.Ciudadano, DateTime.UtcNow);
```

## Propiedades

Una propiedad parece un campo, pero controla cómo se lee o escribe. En EcoAccess casi todas son de solo lectura desde
fuera (`{ get; private set; }`). Así, nadie puede escribir `ana.Activo = false` sin pasar por `ana.Desactivar()`.

## Constructores

El **constructor** se ejecuta al hacer `new` y deja el objeto en un estado válido. Varias entidades tienen **dos
constructores**: uno para registrar un objeto nuevo y otro para reconstruir uno leído de Oracle. Ejemplo real:

{{codigo:src/EcoAccess.P3.Entidades/Usuario.cs:38:75}}

Fíjese en `: this(...)` y en las validaciones: aunque los datos vengan de la base, se comprueban igual.

# Colecciones

| Colección | Uso | Ejemplo |
|---|---|---|
| `List<T>` | lista que crece | los roles de `Usuario` (`private readonly List<CodigoRol> _roles`); `AsignarRol` evita repetirlos |
| `HashSet<T>` | conjunto sin repetidos | `EvaluacionVisionArchivos` (línea 90) guarda los hashes de las fotos para rechazar una foto repetida en el corpus |
| `Dictionary<K,V>` | buscar por clave | `CatalogoObjetos.PorClave` busca un objeto conocido por su clave («plastic_bottle») |
| `IReadOnlyList<T>` | lista de solo lectura | los repositorios devuelven `IReadOnlyList<Usuario>` para que nadie la modifique |

**LINQ** permite consultar colecciones: `usuarios.Any(u => u.TieneRol(CodigoRol.Administrador))` responde «¿hay algún
administrador?». Esa línea está en `ServicioAutenticacion.FaltaCuentaAdministradorAsync`.

# Enumeraciones

Un `enum` es una lista cerrada de opciones con nombre. En EcoAccess los valores numéricos se escriben explícitamente
porque Oracle guarda el número:

```csharp
public enum CodigoRol { Ciudadano = 1, Operario = 2, Administrador = 3 }
```

Si alguien reordena la lista, los números no cambian y los datos guardados siguen significando lo mismo.

# Records

Un `record` es una clase pensada para **datos inmutables**, con igualdad por valor. Ejemplo:
`public sealed record FiltroInforme(DateTime DesdeUtc, DateTime HastaUtc, int? IdContenedor = null, ...)`. La expresión
`filtro with { DesdeUtc = ... }` crea una copia con un dato cambiado. `ServicioInformeInstitucional` la usa para pedir el
período anterior.

# Excepciones

Una **excepción** interrumpe el flujo normal cuando algo no se puede hacer. Se lanza con `throw` y se atrapa con
`try`/`catch`.

EcoAccess separa dos tipos de error:

1. **Errores de programación o datos imposibles** → excepción. Ejemplo: `new Usuario("", ...)` lanza
   `ArgumentException`, porque ninguna parte del sistema debería crear un usuario sin nombre.
2. **Rechazos de negocio esperables** → `Resultado` con un `MotivoRechazo` (por ejemplo `CredencialesInvalidas`,
   `Duplicado`, `EstadoNoPermitido`). No son fallos del programa: son respuestas normales a la persona.

`Resultado` y `Resultado<T>` están en `src/EcoAccess.P3.Logica/Resultado.cs`. Gracias a ellos la interfaz no necesita
atrapar excepciones para mostrar «Correo o contraseña incorrectos».

Las excepciones propias del dominio también son clases: `ConflictoUnicidadException` (el repositorio la lanza cuando
Oracle rechaza un correo duplicado con ORA-00001) y `UltimoAdministradorException` (impide quitar el último administrador
activo).

# async y await

Consultar Oracle en la nube o pedirle a Ollama que analice una foto tarda. Si el programa esperara bloqueado, la ventana
se congelaría. **`async`/`await`** permite esperar sin bloquear:

```csharp
private async void BotonAceptar_Click(object? sender, EventArgs e)
{
    Resultado resultado = await _servicio.CrearAdministradorInicialAsync(nombre, correo, contrasena);
    ...
}
```

- Un método `async` devuelve `Task` o `Task<T>` (una «promesa» de un resultado futuro).
- `await` suspende el método hasta que la tarea termina, sin congelar la ventana.
- Por convención, los métodos asíncronos terminan en `Async`.
- `async void` solo se usa en manejadores de eventos de Windows Forms; en todo lo demás se devuelve `Task`.

Muchos métodos reciben un `CancellationToken`: permite cancelar una operación larga (por ejemplo, cuando se cierra la
ventana del dashboard mientras consulta Oracle).

# Interfaces

Una **interfaz** es un contrato: dice qué métodos debe tener una clase, sin decir cómo los hace. En EcoAccess la lógica
nunca habla directamente con Oracle; habla con interfaces como `IRepositorioUsuarios`:

{{codigo:src/EcoAccess.P3.Logica/Contratos/IRepositorioUsuarios.cs:10:35}}

Hay dos implementaciones de este contrato:

- `RepositorioUsuariosOracle` (proyecto Datos), que usa Oracle de verdad.
- `RepositorioUsuariosEnMemoria` (proyecto de pruebas), que guarda los datos en una lista para probar la lógica sin base de datos.

La lógica recibe el contrato por el constructor (**inyección de dependencias**) y no sabe cuál de las dos le dieron. Por
eso las 1 768 pruebas unitarias se ejecutan en segundos.

# Herencia y clases parciales

- `FormConfiguracionInicial : Form` hereda de `Form`: obtiene de Windows Forms todo lo que hace una ventana.
- `partial class` divide una clase en varios archivos. Los formularios separan el código escrito a mano
  (`FormX.cs`) del generado por el diseñador (`FormX.Designer.cs`). `ServicioConversacionTelegram` está repartido en cuatro
  archivos (`.cs`, `.Conversar.cs`, `.FlujoEco.cs`, `.Cantidades.cs`) para que cada uno trate un tema.

# Compilar y ejecutar

1. **Compilar** traduce los `.cs` a `.dll`/`.exe`. En Visual Studio: **Compilar → Compilar solución** (Ctrl+Mayús+B). En consola: `dotnet build EcoAccess.P3.sln -c Release`.
2. La salida queda en `src/<Proyecto>/bin/Release/net48/`.
3. **Errores** (rojo) impiden generar el programa; **advertencias** (amarillo) no. EcoAccess compila con **0 errores**; las advertencias que quedan son sugerencias de estilo de las pruebas (MSTEST0037).
4. **Ejecutar las pruebas**: **Prueba → Ejecutar todas las pruebas**, o `dotnet test tests/unitarias/EcoAccess.P3.Tests`.

# Ejercicios

1. Abra `src/EcoAccess.P3.Entidades/ReglaEcopuntos.cs`. Escriba en papel qué devuelve `CalcularPuntos(3)` para una regla de 4 puntos por unidad, y compruébelo con la prueba `ReglaEcopuntosTests`.
2. En `Usuario.cs`, busque qué excepción lanza `QuitarRol` cuando se intenta dejar la cuenta sin roles. ¿Por qué es una excepción y no un `Resultado`?
3. Cree en una prueba nueva un `Usuario` con correo en mayúsculas y compruebe que `CorreoElectronico` queda en minúsculas.
4. Cambie temporalmente `LongitudMinimaContrasena` a 8, ejecute `ServicioAutenticacionTests` y observe qué pruebas fallan. Deshaga el cambio.

# Errores frecuentes

| Error | Causa | Solución |
|---|---|---|
| CS8618 «propiedad no anulable sin inicializar» | `Nullable` activo y una propiedad `string` sin valor | inicialícela o declárela `string?` |
| La ventana se congela | se llamó `.Result` o `.Wait()` sobre una tarea en el hilo de la interfaz | use `await` |
| `ArgumentException` al crear una entidad | dato inválido (nombre vacío, fecha no UTC) | lea el mensaje: dice qué parámetro falló |
| CS0246 «no se encontró el tipo» | falta un `using` o una referencia de proyecto | agregue el `using` del espacio de nombres |

# Glosario

- **Clase:** molde de objetos. **Objeto:** instancia de una clase.
- **Propiedad:** dato de un objeto con acceso controlado.
- **Constructor:** método que crea el objeto en estado válido.
- **Interfaz:** contrato sin implementación.
- **Inyección de dependencias:** dar a una clase sus colaboradores por el constructor, en lugar de que los cree ella.
- **Tarea (`Task`):** operación que termina en el futuro.
- **UTC:** hora universal; EcoAccess guarda todas las fechas en UTC y las muestra en hora de Colombia (UTC−5) con `HoraColombia`.
