---
archivo: 09_WINDOWS_FORMS_COMPLETO
titulo: Windows Forms en EcoAccess P3
subtitulo: Cada formulario, control y evento importante; cómo se conectan con la lógica y cómo modificarlos paso a paso en Visual Studio
codigo: 09
---

# Conceptos básicos

**Windows Forms** es la tecnología de .NET para hacer ventanas de escritorio. Una ventana es una clase que hereda de
`Form`. Dentro hay **controles** (botones, cajas de texto, etiquetas, tablas) y cada control dispara **eventos** (`Click`,
`TextChanged`, `Load`...). Un **manejador de evento** es el método que se ejecuta cuando ocurre el evento.

## El diseñador y `InitializeComponent`

Cuando se crea un formulario en Visual Studio aparecen dos archivos:

- `FormX.cs`: el código que escribe la persona (lógica de la ventana).
- `FormX.Designer.cs`: el código que genera el **diseñador visual**. Contiene `InitializeComponent()`, que crea cada control, le pone tamaño, posición y texto, y conecta los eventos.

Ejemplo real: así conecta el diseñador el botón Guardar de la configuración inicial.

{{codigo:src/EcoAccess.P3.Presentacion/FormConfiguracionInicial.Designer.cs:130:140}}

La línea `botonAceptar.Click += BotonAceptar_Click;` dice: «cuando se haga clic en `botonAceptar`, ejecute
`BotonAceptar_Click`».

## El constructor del formulario

El constructor llama primero a `InitializeComponent()` y después ajusta lo que depende de datos (por ejemplo, la longitud
máxima de los campos tomada de las constantes de las entidades):

{{codigo:src/EcoAccess.P3.Presentacion/FormConfiguracionInicial.cs:28:49}}

## Formularios construidos por código

Muchos formularios de EcoAccess no usan el diseñador: crean sus controles en código, con componentes propios
(`TarjetaDato`, `PanelGraficosResumen`) y la biblioteca **Krypton** (`KryptonButton`, `KryptonComboBox`). Ejemplo:
`FormRegistroCiudadano` declara `_crear = new KryptonButton { Text = "Crear mi cuenta", ... }` y en el constructor conecta
`_crear.Click += async (_, _) => await RegistrarAsync();`. Ambas formas son válidas; lo importante es que el evento
llame a un servicio y no contenga reglas de negocio.

# Del clic a Oracle y de vuelta

1. **Validación de la pantalla:** comprobaciones de comodidad (por ejemplo, que la contraseña y su confirmación coincidan). No son reglas de negocio.
2. **Bloqueo de la ventana:** `_ocupado = true`, cursor de espera y panel deshabilitado, para que la persona no pulse dos veces.
3. **Llamada asíncrona al servicio** con `await`.
4. **Resultado:** si `resultado.Exitoso`, se actualiza la ventana; si no, se muestra `resultado.Mensaje`, que es un texto pensado para la persona.
5. **Errores técnicos:** un `catch (Exception)` muestra un mensaje general («No se pudo conectar con la base de datos...»). Nunca se muestra el mensaje de Oracle ni la cadena de conexión.
6. **Desbloqueo** en `finally`.

# Inventario de formularios, controles y eventos

La tabla de cada formulario se generó leyendo el código con Roslyn: cada fila es una línea real donde se conecta un
evento (`control.Evento += manejador`). La columna «Servicios que llama» lista las llamadas a campos del formulario que
empiezan por `_` (servicios y repositorios) dentro del método manejador.

{{formularios}}

# Ejercicios guiados en Visual Studio

## Ejercicio 1: cambiar el texto de un botón del diseñador

1. Abra `EcoAccess.P3.sln` en Visual Studio.
2. En el **Explorador de soluciones**, despliegue `EcoAccess.P3.Presentacion` y haga doble clic en `FormConfiguracionInicial.cs`: se abre el **diseñador**.
3. Haga clic en el botón **Guardar**.
4. Pulse **F4** para abrir la ventana **Propiedades**. Busque **Text** y escriba `Crear administrador`.
5. Guarde (**Ctrl+S**). Visual Studio actualiza `FormConfiguracionInicial.Designer.cs` (`botonAceptar.Text = ...`).
6. Compile con **Ctrl+Mayús+B** y ejecute con **F5**.
7. Deshaga el cambio si no lo va a conservar.

## Ejercicio 2: cambiar el color de una tarjeta

Los colores no se escriben en cada formulario: están centralizados en `TemaEcoAccess` (`src/EcoAccess.P3.Presentacion`).
Cambie allí el valor de la propiedad de color que corresponda, compile y observe que todas las ventanas que la usan
cambian a la vez.

## Ejercicio 3: agregar una validación a un campo

En `FormConfiguracionInicial.BotonAceptar_Click`, después de comparar las contraseñas, agregue:

```csharp
if (textoCorreo.Text.Trim().Length == 0)
{
    MostrarError("Escriba el correo del administrador.");
    return;
}
```

Esta validación es solo de comodidad: la regla real ya la aplica la entidad `Usuario` al crear la cuenta. Ejecute
`ServicioAutenticacionTests` para confirmar que la lógica no cambió.

## Ejercicio 4: cambiar el evento de un botón

1. En el diseñador, seleccione el botón y pulse el ícono del **rayo** en la ventana Propiedades.
2. En el evento **Click** puede elegir otro método o escribir un nombre nuevo; Visual Studio crea el método en `FormX.cs`.
3. Escriba el cuerpo del método llamando a un servicio con `await`.

## Ejercicio 5: cambiar una consulta

Las consultas no están en los formularios. Para cambiar lo que muestra el tablero institucional, el cambio va en
`RepositorioInformeInstitucionalOracle` (capa Datos) y, si cambia el contrato, en `IRepositorioInformeInstitucional`
(capa Lógica). Después de cambiar el SQL, ejecute `InformeInstitucionalOracleTests` contra el esquema aislado.

## Ejercicio 6: navegación

`FormPrincipal` arma el menú lateral con `Agregar(menu, texto, icono, acción)`. Para mostrar una opción solo a
administradores se usa la condición `if (_admin)`. Recuerde: ocultar una opción no es seguridad; el servicio también
debe exigir el rol.

# Cambio del cierre: el operario ve el tablero institucional

Antes, `ServicioInformeInstitucional` exigía rol Administrador y `FormPrincipal` mostraba «Dashboard institucional» y
«Análisis en Power BI» solo a administradores. El operario necesita esos informes para su trabajo, y son de **solo lectura**.
Se cambiaron tres cosas, sin dar al operario ningún permiso de modificación:

1. `ServicioInformeInstitucional.ConsultarAsync` y `ConsultarConsultasVisionAsync`: `ExigirAlgunRolAsync(actor, ct, Operario, Administrador)`.
2. `MatrizPermisos`: `ConsultarInformeInstitucional = Gestion`.
3. `FormPrincipal`: las dos entradas del menú ya no dependen de `_admin`.

`MatrizPermisosTests` llama al servicio real con los tres roles y confirma el cambio: el ciudadano sigue rechazado.

# Preguntas probables

**¿Qué hace `InitializeComponent`?** Crea los controles del diseñador, les asigna propiedades y conecta sus eventos. Se
llama una vez, en el constructor.

**¿Por qué los manejadores son `async void`?** Porque Windows Forms exige que un manejador de evento devuelva `void`. Es la
única excepción a la regla de devolver `Task`.

**¿Dónde se valida realmente un dato?** En la entidad y en el servicio. La validación de la pantalla solo evita viajes
innecesarios a Oracle.
