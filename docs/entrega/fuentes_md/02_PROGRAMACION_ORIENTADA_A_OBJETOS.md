---
archivo: 02_PROGRAMACION_ORIENTADA_A_OBJETOS
titulo: Programación orientada a objetos en EcoAccess P3
subtitulo: Encapsulamiento, herencia, polimorfismo, abstracción, composición, asociación y dependencias con ejemplos reales
codigo: 02
---

# Qué es la programación orientada a objetos

La **programación orientada a objetos (POO)** organiza un programa alrededor de **objetos** que tienen datos (estado) y
comportamiento (métodos). En lugar de tener datos sueltos y funciones que los modifican desde cualquier parte, cada
objeto cuida sus propios datos.

En EcoAccess los objetos representan cosas del mundo real (una caneca, un depósito, una persona) y piezas del software
(un repositorio que habla con Oracle, un clasificador de fotos). Este documento muestra cada principio con código que
existe en el repositorio. Ningún ejemplo es inventado: cada uno indica su archivo.

# Encapsulamiento

**Definición sencilla:** esconder los datos internos de un objeto y dejar que solo se cambien a través de sus métodos,
que comprueban las reglas.

**En EcoAccess:** `Usuario` guarda sus roles en una lista privada y solo la expone de solo lectura:

{{codigo:src/EcoAccess.P3.Entidades/Usuario.cs:27:30}}

Para cambiar los roles hay que llamar a `AsignarRol` o `QuitarRol`, y `QuitarRol` impide dejar la cuenta sin ningún rol:

{{codigo:src/EcoAccess.P3.Entidades/Usuario.cs:96:124}}

**Por qué importa:** si la lista fuera pública, cualquier pantalla podría vaciarla y crear una cuenta sin rol, que el
resto del sistema no sabría tratar.

Otros ejemplos reales:

- `Contenedor` no deja cambiar `Estado` directamente: hay `PonerEnServicio`, `PonerFueraDeServicio` y `DarDeBaja`.
- `OrdenApertura` solo avanza de estado con `MarcarEnviada`, `RegistrarAcuse`, `RegistrarApertura`, `RegistrarDeteccion`, `Cerrar`, `Vencer` o `Fallar`, y `PuedeAvanzarA` impide retroceder.
- `CredencialAcceso.Hash` devuelve una **copia** del arreglo: quien la recibe no puede alterar la credencial guardada.

# Abstracción

**Definición sencilla:** mostrar solo lo necesario para usar algo y ocultar cómo funciona por dentro. Al conducir, se usa
el volante sin conocer el motor.

**En EcoAccess:** la interfaz `IClasificadorMaterial` describe «algo que mira una foto y propone un material», sin decir
si usa un modelo local, uno en la nube o un simulador:

{{codigo:src/EcoAccess.P3.Logica/Contratos/IClasificadorMaterial.cs:119:133}}

`ServicioClasificacionImagen` trabaja solo con esa abstracción.

Hay también una **clase abstracta**: `TarjetaDibujada` (en `src/EcoAccess.P3.Presentacion/ComponentesExplorar.cs`). No
se puede crear directamente (`new TarjetaDibujada(...)` no compila); define lo común a todas las tarjetas (fondo
redondeado, foco, ratón, teclado) y deja a cada subclase dibujar su contenido.

# Herencia

**Definición sencilla:** una clase hija recibe lo que tiene su clase padre y agrega o cambia partes.

**En EcoAccess:**

| Clase hija | Clase padre | Qué hereda | Archivo |
|---|---|---|---|
| `FormInicioSesion`, `FormUsuarios` y los demás formularios | `Form` (Windows Forms) | ventana, título, botones de cerrar, eventos `Load` y `Shown` | `src/EcoAccess.P3.Presentacion/Form*.cs` |
| `TarjetaAccionRapida`, `TarjetaDato`, `TarjetaCategoria`, `TarjetaResultadoMaterial` | `TarjetaDibujada` | dibujo de la tarjeta, foco y activación con Enter o Espacio | `ComponentesExplorar.cs`, líneas 194, 242, 303 y 385 |
| `TarjetaDibujada` | `Control` | todo lo de un control de Windows Forms | `ComponentesExplorar.cs`, línea 74 |
| `RelojSimulacion` (anidada en `GeneradorLoteSimulacion`) | `TimeProvider` (.NET) | interfaz de reloj; redefine `GetUtcNow` | `src/EcoAccess.P3.Datos/Simulacion/GeneradorLoteSimulacion.cs` |

El proyecto usa la herencia **poco y a propósito**. Casi todas las clases son `sealed` (no se puede heredar de ellas): se
prefiere la composición, que se explica más adelante.

# Polimorfismo

**Definición sencilla:** el mismo mensaje produce comportamientos distintos según el objeto que lo recibe.

**En EcoAccess, polimorfismo por interfaz:** estas clases implementan `IClasificadorMaterial` y cada una responde
`ClasificarAsync` a su manera:

| Implementación | Qué hace | Archivo |
|---|---|---|
| `ClasificadorOllamaLocal` | envía la foto al modelo `lfm3-vl` en el PC (Ollama) | `src/EcoAccess.P3.Bot/Vision/ClasificadorOllamaLocal.cs` |
| `ClasificadorGroq` | usa el modelo remoto, solo con consentimiento vigente | `src/EcoAccess.P3.Bot/Vision/ClasificadorGroq.cs` |
| `SelectorVision` | elige entre local y remoto según la configuración y la disponibilidad | `src/EcoAccess.P3.Bot/Vision/SelectorVision.cs` |
| `SeleccionVisionLocal` | activa el reconocimiento local con Ollama cuando la configuración lo permite; sin él, el bot ofrece elegir el material a mano | `src/EcoAccess.P3.Bot/Vision/SeleccionVisionLocal.cs` |
| `ClasificadorMaterialSimulado` | respuestas fijas para pruebas y demostraciones | `src/EcoAccess.P3.Logica/Servicios/ClasificadorMaterialSimulado.cs` |

Lo mismo ocurre con `ICanalDispositivo` (cómo se envía una orden de apertura): `CanecaSimulada`, `EstacionSimulada` y
`CanalDispositivoHttp`. Y con `IClienteTelegram`: `ClienteTelegramHttp` (Telegram real), `ClienteTelegramSimulado` (pruebas)
y `ChatConsola` (conversar desde la consola).

**Polimorfismo por herencia:** cada subclase de `TarjetaDibujada` redefine (`override`) `PintarContenido` y `MedirAlto`.
El contenedor de tarjetas llama `MedirAlto` sin saber qué tarjeta es.

# Composición

**Definición sencilla:** construir un objeto a partir de otros objetos que hacen partes del trabajo, en lugar de heredar.
«Tiene un» en vez de «es un».

**En EcoAccess:** `ServicioAutenticacion` **tiene** un repositorio de credenciales, un repositorio de usuarios, un
calculador de hashes y un reloj:

{{codigo:src/EcoAccess.P3.Logica/Servicios/ServicioAutenticacion.cs:36:57}}

Cada pieza tiene una sola responsabilidad y puede reemplazarse. En las pruebas se le da un `HasherFalso` (rápido) y un
`RelojAjustable` (para simular el paso de 15 minutos de bloqueo sin esperar).

# Asociación

**Definición sencilla:** dos objetos se relacionan, pero cada uno existe por su cuenta.

En las entidades la asociación se guarda **por identificador**, no por referencia a otro objeto:

- `Interaccion.IdUsuario` asocia la interacción con una persona.
- `OrdenApertura.IdInteraccion` y `OrdenApertura.IdContenedor` asocian la orden con su interacción y su caneca.
- `Deposito.IdOrden` asocia el depósito con la orden que lo autorizó.

Así se corresponden exactamente con las **claves foráneas** de Oracle (`ORDEN_APERTURA.ID_INTERACCION → INTERACCION`). La
cardinalidad la imponen Oracle y la lógica: por ejemplo, como máximo un depósito por orden (RN-17), con una restricción
`UNIQUE` en `DEPOSITO.ID_ORDEN`.

# Dependencias

Una clase **depende** de otra cuando la necesita para trabajar. EcoAccess cuida la **dirección** de las dependencias:

```
Presentación (Windows Forms, bot) ──► Lógica ──► Entidades
             Datos (Oracle) ──────────► Lógica (implementa sus contratos)
```

- **Entidades** no depende de nadie.
- **Lógica** depende solo de Entidades y define los contratos (`IRepositorio...`).
- **Datos** implementa esos contratos con Oracle.
- **Presentación** une todo en el arranque (raíz de composición: `src/EcoAccess.P3.Bot/Program.cs` y `src/EcoAccess.P3.Presentacion/Program.cs`).

La prueba `PruebasArquitectura` comprueba esas direcciones: si alguien hiciera que Entidades usara Oracle, la prueba
fallaría.

# Inmutabilidad

Un objeto inmutable no cambia después de crearse. En EcoAccess son inmutables, entre otros:

- `CredencialAcceso`: un cambio de contraseña crea otra credencial.
- `ReglaEcopuntos`: una tarifa nueva es una versión nueva; la anterior respalda los movimientos ya hechos.
- `EventoDispositivo`: lo que reportó la caneca no se reescribe.
- Los `record` de contratos (`FiltroInforme`, `PropuestaVision`, `ComandoApertura`...).

La inmutabilidad facilita auditar: lo que ocurrió queda como ocurrió.

# Resumen de principios y dónde verlos

| Principio | Ejemplo principal | Archivo |
|---|---|---|
| Encapsulamiento | roles privados de `Usuario` | `src/EcoAccess.P3.Entidades/Usuario.cs` |
| Abstracción | `IClasificadorMaterial` | `src/EcoAccess.P3.Logica/Contratos/IClasificadorMaterial.cs` |
| Clase abstracta | `TarjetaDibujada` | `src/EcoAccess.P3.Presentacion/ComponentesExplorar.cs` |
| Herencia | formularios `: Form` | `src/EcoAccess.P3.Presentacion/Form*.cs` |
| Polimorfismo | cinco clasificadores intercambiables | `src/EcoAccess.P3.Bot/Vision/` |
| Composición | `ServicioAutenticacion` | `src/EcoAccess.P3.Logica/Servicios/ServicioAutenticacion.cs` |
| Asociación | `OrdenApertura.IdInteraccion` | `src/EcoAccess.P3.Entidades/OrdenApertura.cs` |
| Inmutabilidad | `ReglaEcopuntos` | `src/EcoAccess.P3.Entidades/ReglaEcopuntos.cs` |

# Ejercicios

1. Explique con sus palabras por qué `Usuario.Roles` es `IReadOnlyCollection<CodigoRol>` y no `List<CodigoRol>`.
2. Escriba una clase de prueba `ClasificadorSiemprePet : IClasificadorMaterial` que siempre proponga PET. ¿Qué tiene que cambiar en `ServicioClasificacionImagen` para usarla? (Respuesta: nada, se le pasa por el constructor.)
3. Busque otra clase `sealed` en Datos y explique por qué no tiene sentido heredar de un repositorio.
4. Dibuje la asociación Interaccion → OrdenApertura → Deposito con sus cardinalidades y compruébela con las claves foráneas de la tabla `DEPOSITO` en el documento 10.

# Preguntas probables y respuestas

**¿Por qué casi todas las clases son `sealed`?** Porque no se diseñaron para heredarse. Sellarlas evita que alguien
cambie su comportamiento con una subclase sin respetar sus reglas. Cuando se necesita variar comportamiento, se usa una
interfaz (polimorfismo por composición).

**¿Dónde hay polimorfismo de verdad y no solo «herencia de Form»?** En los cinco clasificadores de visión, los tres canales
de dispositivo y los tres clientes de Telegram, todos intercambiables detrás de una interfaz.

**¿Una entidad puede guardarse a sí misma en Oracle?** No. Las entidades no conocen la base de datos; de eso se encargan los
repositorios (capa Datos). Así la regla de negocio se prueba sin Oracle.

# Glosario

- **Invariante:** condición que un objeto garantiza siempre (por ejemplo, «un usuario tiene al menos un rol»).
- **`sealed`:** clase de la que no se puede heredar.
- **`override`:** redefinir en la hija un método virtual o abstracto del padre.
- **Interfaz:** contrato de métodos sin implementación.
- **Raíz de composición:** lugar del arranque donde se crean y conectan todos los objetos.
