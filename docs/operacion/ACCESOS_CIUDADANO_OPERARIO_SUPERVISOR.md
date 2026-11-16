# Accesos Ciudadano, Operario y Supervisor (11/11/2026)

## Nombres y códigos

| Nombre visible | Código técnico (`CodigoRol`, tabla `ROL`) | Área en Windows Forms |
|---|---|---|
| Ciudadano | `Ciudadano = 1` | Explorar EcoAccess (`FormExplorarEcoAccess`) y Telegram |
| Operario | `Operario = 2` | `FormPrincipal` en área Operario |
| **Supervisor** (Administrador del sistema) | `Administrador = 3` | `FormPrincipal` en área Supervisor |

No se creó un cuarto rol ni se tocó Oracle. «Supervisor» es solo el nombre visible del rol 3.
La traducción está en un único lugar: `src/EcoAccess.P3.Logica/AreasAcceso.cs` (`NombreRol`, `NombreRolCompleto`, `RolDe`).

## Cómo se decide el acceso (de la pantalla a Oracle)

1. **Elección.** `FormInicioSesion` muestra tres tarjetas (`SelectorAreaAcceso`). Elegir una **no concede nada**.
2. **Credenciales.** `ServicioAutenticacion.IniciarSesionPublicaAsync` comprueba el correo y la contraseña (PBKDF2, bloqueo por intentos) y lee de Oracle los roles reales de la cuenta (`USUARIO_ROL`).
3. **Comparación.** `AreasAcceso.Elegir(sesion, area)` admite el área solo si la cuenta tiene ese rol. Si no lo tiene, la sesión se descarta y aparece, por ejemplo: «Tu cuenta no tiene acceso de Supervisor. Puedes entrar como Ciudadano.».
4. **Apertura.** `Program.EjecutarSesiones` abre `FormExplorarEcoAccess` (Ciudadano) o `FormPrincipal(servicios, sesion, area)`. `FormPrincipal` vuelve a comprobar el área en su constructor.
5. **Cambio de área.** Solo aparece el botón «Cambiar al área …» si la cuenta tiene **ambos** roles de gestión. `CambiarArea` vuelve a llamar a `Elegir` antes de reabrir el menú.
6. **Autorización real.** Cada servicio revalida al actor en Oracle (`ControlAcceso.ExigirRolAsync` y `ExigirAlgunRolAsync`, documentados en `MatrizPermisos`). Ocultar un botón es una comodidad, no una protección.

## Qué ve cada área

| Área | Menú lateral | Permisos en servicios |
|---|---|---|
| Operario | Dashboard · **Consultar** (ciudadanos, canecas, materiales; solo lectura, `FormConsultaOperario`) · Depósitos (revisar y verificar) · Registro manual · Ecopuntos (consulta) · Estadísticas (con exportación CSV) · Dashboard institucional · Power BI | `BuscarCiudadanos`, `ConsultarEstadisticasGenerales`, `ConsultarEcopuntosDeOtro`, `ListarReglasEcopuntos`, `ConsultarInformeInstitucional`; acreditar ecopuntos de depósitos verificados |
| Supervisor | Dashboard · Usuarios y roles · Materiales · Canecas y QR · Puntos de instalación · Tarifas y ecopuntos · Auditar depósitos · Estadísticas · Valoraciones · Dashboard institucional · Power BI · Sistema y Telegram | Todo lo anterior, más `ListarUsuarios`, `AsignarRol`, `GestionarMateriales`, `RegistrarReglaEcopuntos` y `DiagnosticarOracle` |

Se quitó del menú «Próximamente»: era una pantalla sin acción real.

## Pruebas

- `AreasAccesoTests` (7 casos):
  - un Ciudadano no entra como Operario ni como Supervisor;
  - un Operario no entra como Supervisor;
  - una cuenta solo Supervisor no entra como Operario;
  - una cuenta con ambos roles puede cambiar de área;
  - un área inválida se rechaza;
  - una cuenta sin roles se rechaza;
  - Administrador sigue siendo el código 3.
- `MatrizPermisosTests` (existente): los servicios reales cumplen la matriz para los tres roles.
- Unitarias completas: 1792 superadas, 0 fallidas, 1 omitida.
- Capturas automáticas (`tests/herramientas/EcoAccess.P3.Capturas`, 1366×768): todas las secciones de las dos áreas se abren con datos de Oracle Cloud. Usan una cuenta temporal que se borra al terminar. Evidencias en `docs/evidencias/winforms/accesos/`.

## Cuentas del equipo (consulta de solo lectura en Oracle Cloud, 11/11/2026)

| Integrante | Cuenta | Activa | Roles | Credencial | Estado |
|---|---|---|---|---|---|
| Jorge Gutiérrez Thomas | 4521 | sí | Operario (2) y Supervisor (3) | sí | VERIFICADO REAL (consulta); inicio de sesión: lo hace el propietario |
| Heyder Canchila | no existe | — | — | — | BLOQUEADO: falta su correo real |
| Saray Cadena | no existe | — | — | — | BLOQUEADO: falta su correo real |

- No hay correos duplicados en `USUARIO`.
- El único operario existente es 4468, de la simulación, sin credencial; no es una persona real.
- Los códigos visuales ADM-000n y OP-000n quedan pendientes. Mostrarlos exige guardar la correspondencia con cada integrante, y hoy solo existe la cuenta de Jorge.

**Para crear a Heyder y Saray** (cuando den su correo real):

1. Entra como Supervisor y ve a «Usuarios y roles».
2. Crea la cuenta con el rol Operario y asigna también «Supervisor (Administrador del sistema)».
3. En «Contraseña inicial (operario o supervisor)», la persona escribe su contraseña.

Las contraseñas nunca se suben a Git.

## Iniciar sesión

| Área | Pasos |
|---|---|
| Ciudadano | Tarjeta **Ciudadano** → correo y contraseña → Iniciar sesión como Ciudadano. Sin cuenta: «Entrar como invitado» o «Registrarme». |
| Operario | Tarjeta **Operario** → correo y contraseña de una cuenta con rol Operario → Iniciar sesión como Operario. |
| Supervisor | Tarjeta **Supervisor** → correo y contraseña de una cuenta con rol 3 → Iniciar sesión como Supervisor. Con ambos roles, el botón ⟳ de la cabecera cambia de área. |
