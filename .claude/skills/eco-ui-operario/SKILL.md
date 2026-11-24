---
name: eco-ui-operario
description: Estilo de las pantallas del área Operario de EcoAccess — misma estética que la app ciudadana, sin imágenes, enfocada en consultar, verificar depósitos y registrar manualmente. Úsala al crear o modificar FormPrincipal en modo operario, FormConsultaOperario, FormInteraccionesDepositos (verificación), FormRegistroManual, FormEcopuntos (consulta), FormEstadisticas o cualquier módulo nuevo del operario.
---

# EcoAccess · Área Operario

Lee primero `eco-estilo-base`. El operario comparte la estructura y las reglas de `eco-ui-supervisor`
(barra lateral, cabecera, tarjetas, **sin imágenes**), porque varios módulos son los mismos para ambos roles.
Esta skill añade lo propio del trabajo del operario.

## 1. Identidad del área

- Rótulo lateral `ÁREA OPERARIO` (`Pequena`, `VerdeOscuro`).
- Menú: Dashboard · Consultar · Depósitos · Registro manual · Ecopuntos · Estadísticas · (Dashboard institucional)
  · Análisis en Power BI. Iconos FontAwesome: `House`, `MagnifyingGlass`, `BoxOpen`, `PenToSquare`, `Coins`,
  `ChartColumn`, `ChartPie`, `ChartLine`.
- Acciones rápidas del dashboard: «Consultar ciudadanos y canecas», «Verificar depósitos», «Registro manual».

### 1.1 Banner de módulo (decisión del usuario, 23/11/2026)

- Todo módulo del Operario y del Supervisor abre con **el mismo** `BannerModulo` (ComponentesDashboard.cs): un único
  componente que cada módulo invoca con su título, descripción, icono del estado y la línea de información actual
  (`Estado`), enlazada al mensaje que ya escribe el formulario (`_estado.TextChanged += … banner.Estado = …`). No se
  copia ni se redibuja un banner propio por módulo (responsabilidad única y reutilización, principios SOLID).
- Es la excepción aprobada a «sin imágenes»: el banner lleva a Eco y el campus. El resto del módulo sigue sin imágenes.
- Pendiente (23/11/2026): Análisis en Power BI. Registro manual ya lo tiene (FormRegistroManual.Diseno.cs): paso 1 con
  el selector «Caneca» (canecas en servicio registradas por el Supervisor, ninguna elegida al abrir) y debajo UNA sola
  caneca, la elegida, con la imagen de su tipo (`registro/caneca_verde|blanca|negra.png`, las del usuario; excepción aprobada como en
  ciudadano e invitado), nombre, pastilla del tipo y los residuos que recibe; el tipo sale de la categoría más frecuente
  de sus materiales. La lista de residuos no ofrece peligrosos ni lo que la caneca no admite. Ecopuntos ya lo tiene (FormEcopuntos.Diseno.cs, el
  mismo formulario para Operario y Supervisor: fichas de saldo, historial con pastillas Crédito/Reverso y puntos ±;
  el Operario no ve crédito ni motivo porque anular y corregir son del Supervisor).
- La disposición va en `FormX.Diseno.cs` (partial) y el módulo termina con `ModuloEco.TerminarDesplazable`; la lógica
  (`FormX.cs`) no se toca. Nunca editar fuentes con PowerShell 5.1 `Set-Content`: daña las tildes.

## 2. Pantallas de solo lectura (Consultar) — implementado en FormConsultaOperario.Diseno.cs

- Bajo el banner, `AvisoEco` «Consulta de solo lectura» con icono `Lock`. Nada de botones de crear/editar/deshabilitar.
- `PaginasSinPestanas` + `SelectorPestanas` (píldoras Ciudadanos `Users` / Canecas `TrashCan` / Materiales `Leaf`)
  dentro de la sección «Resultados de la consulta».
- Búsqueda en **cada** página con la misma pieza `BarraBusqueda` (campo con `MagnifyingGlass`, texto guía y botón
  principal «Buscar»; Enter también busca). Ciudadanos consulta Oracle (lógica del formulario); Canecas y Materiales
  usan `FiltroTabla<T>`: filtran la lista ya cargada sin otra consulta, sin distinguir mayúsculas ni tildes, conservan
  los encabezados y muestran «Mostrando N de M…» debajo de la tabla.
- `DisenoGestion.Pastilla` escribe solo el texto si la celda es más angosta que la pastilla (un arco con ancho ≤ 0
  hacía fallar a GDI+ y la tabla dejaba de pintarse).
- Tablas con `DisenoGestion.PulirTabla` (filas de 46 px, estado vacío con icono) y pesos de columna (`FillWeight`)
  para que el código no ocupe lo mismo que el nombre.
- Pastillas: cuenta Activa/Inactiva con `PastillaEstado`; caneca en servicio (verde), fuera de servicio
  (`Advertencia`/`AdvertenciaSuave`, `CirclePause`), dada de baja (`Error`/`ErrorSuave`, `Ban`); materiales admitidos
  como pastilla verde con `Leaf`; categoría con `EstiloCategoria.Para`. Ciudadano con iniciales en círculo verde suave.

## 3. Verificación de depósitos

- Tabla de pendientes; la fila seleccionada en `VerdeSuave`.
- Botón principal «Verificar» (`VerdeOscuro`), secundario «Rechazar» con texto `Error`; ambos con icono
  (`CircleCheck`, `CircleXmark`) de 16 px.
- Tras verificar, mensaje en línea con `CampoEco.MostrarValido` o `PastillaEstado` verde — no `MessageBox`
  para confirmaciones de éxito.

## 4. Registro manual

- Formulario en una sola columna (`DisenoFormularios.Columna()`), máx. ~560 px de ancho dentro de una `TarjetaEco`.
- Cada dato con `CampoEco` y su icono: ciudadano `User`, correo `Envelope`, caneca `Trash`, material `Leaf`,
  peso/cantidad `Scale`/`Hashtag`.
- Errores bajo el campo (`MostrarError`), nunca solo en un `MessageBox`.
- Botonera al final: «Registrar» principal + «Limpiar» secundario, alineados a la derecha.
- La categoría del material se muestra con pastilla de color de caneca + icono, **sin** ilustración del residuo.

## 5. Lista corta antes de entregar

- [ ] Rótulo y menú del área Operario correctos; ninguna acción de Supervisor visible.
- [ ] Sin imágenes (solo iconos FontAwesome y logo lateral).
- [ ] Mismos tokens, controles y medidas que `eco-estilo-base`.
- [ ] Ejecutado el chequeo de `eco-chequeo-estetica` sin errores.
