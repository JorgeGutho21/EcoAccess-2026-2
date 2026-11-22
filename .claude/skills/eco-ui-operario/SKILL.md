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

## 2. Pantallas de solo lectura (Consultar)

- Aviso superior en tarjeta `InfoSuave` con icono `CircleInfo` en `Info`: «Consulta de solo lectura…».
  Nada de botones de crear/editar/deshabilitar.
- Pestañas o selector de botones secundarios (Ciudadanos / Canecas / Materiales); el activo con borde 2 px
  `VerdeOscuro`.
- `CajaBusqueda` grande arriba + tabla `EstilizarTabla` debajo.
- Estado de cada caneca como `PastillaEstado`: en servicio (`VerdeOscuro`/`VerdeSuave`), llena o con aviso
  (`Advertencia`/`AdvertenciaSuave`), fuera de servicio (`Error`/`ErrorSuave`).

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
