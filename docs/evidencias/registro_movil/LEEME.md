# Evidencia del formulario móvil de registro (11/11/2026)

Capturas de la página real (`PaginaVinculacion` + `PaginaRegistro.Documento`). El HTML lo generó la prueba
`Plantilla_UnaColumnaCamposDelMismoAnchoYLetraQueNoAmpliaEnIphone` con `ECOACCESS_VISTA_PREVIA_REGISTRO`. Se abrió en el
navegador integrado emulando un teléfono de 375 × 812 px (agente Android Chrome).

| Captura | Qué muestra |
|---|---|
| `01_crear_cuenta_375px.jpg` | una columna; etiquetas encima; nombre, correo, contraseña y confirmación del mismo ancho; botón «Mostrar» dentro del campo; consentimiento plegado |
| `02_error_junto_al_campo_375px.jpg` | error de longitud junto a la contraseña, con nombre y correo conservados (la contraseña nunca vuelve) |
| `03_iniciar_sesion_375px.jpg` | «Ya tengo cuenta»: solo correo y contraseña, con enlace para crear cuenta |

Medición en el navegador: ancho del documento 375 px = ancho de la vista (sin desplazamiento horizontal); los cuatro
campos miden 303 px. El botón «Mostrar» cambia el tipo del campo a texto y vuelve a ocultarlo.

**Límite:** es una emulación de escritorio, no un iPhone real. La verificación en Safari del iPhone queda PENDIENTE hasta que el
propietario abra el enlace de registro desde su teléfono con la versión publicada. Los datos de las capturas son ficticios
(`movil@ejemplo.com`).
