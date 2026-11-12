---
archivo: 11_CIUDADANO_OPERARIO_ADMINISTRADOR
titulo: Ciudadano, Operario y Supervisor
subtitulo: Matriz de permisos, autenticación, cuentas, restricciones, control de acceso, seguridad y pruebas
codigo: 11
---

# Los tres roles

| Rol | Código | Quién es | Dónde trabaja |
|---|---|---|---|
| Ciudadano | 1 | persona que separa y deposita sus residuos | bot de Telegram y bienvenida del escritorio |
| Operario | 2 | personal que atiende la caneca y verifica el material | aplicación de escritorio, área Operario |
| Supervisor (código técnico Administrador) | 3 | personal que configura catálogos, usuarios, canecas, tarifas y auditoría | aplicación de escritorio, área Supervisor |

Los códigos son explícitos en `CodigoRol` porque coinciden con la columna `ROL.CODIGO` de Oracle. Una cuenta puede tener
varios roles a la vez (`USUARIO_ROL`).

En la interfaz el rol 3 se llama **Supervisor (Administrador del sistema)**. El código técnico `Administrador = 3` y las
relaciones de Oracle no cambiaron; la traducción está en `AreasAcceso.NombreRol`.

# Los tres accesos de la pantalla de inicio (11/11/2026)

{{incluir:docs/operacion/ACCESOS_CIUDADANO_OPERARIO_SUPERVISOR.md}}

# Matriz de permisos

La matriz vive en un solo lugar del código, `MatrizPermisos`:

{{codigo:src/EcoAccess.P3.Logica/Servicios/MatrizPermisos.cs:30:50}}

| Operación | Ciudadano | Operario | Administrador |
|---|---|---|---|
| Consultar mis ecopuntos y mis estadísticas | Sí | No | No |
| Consultar ecopuntos de otro, listar reglas, buscar ciudadanos, estadísticas generales | No | Sí | Sí |
| **Consultar el informe institucional (tablero y modo experimental)** | No | **Sí (cambio del cierre)** | Sí |
| Listar usuarios, asignar roles, registrar reglas, gestionar materiales, diagnóstico de Oracle | No | No | Sí |

**La matriz no reemplaza la comprobación de cada servicio: la documenta.** `MatrizPermisosTests` llama a los servicios
reales con una cuenta de cada rol y comprueba que permitan exactamente lo que dice la matriz. Si alguien cambia un permiso
en un servicio sin cambiar la matriz (o al revés), la prueba falla.

# Cómo se comprueba un permiso

{{codigo:src/EcoAccess.P3.Logica/Servicios/ControlAcceso.cs:23:45}}

1. Se carga la cuenta desde Oracle **en cada operación**.
2. Se comprueba que exista, esté activa y tenga el rol.
3. Si falla, se devuelve un `Resultado` con `MotivoRechazo` (`ActorNoEncontrado`, `ActorInactivo` o `PermisoDenegado`).

Por eso desactivar una cuenta tiene efecto inmediato: su siguiente operación se rechaza aunque su ventana siga abierta.

# Autenticación

- **Ciudadanos:** se registran desde la página enlazada en Telegram o con `FormRegistroCiudadano`. Su contraseña se guarda como hash PBKDF2.
- **Personal:** inicia sesión en `FormInicioSesion`. Cinco fallos seguidos bloquean la credencial 15 minutos.
- **Respuesta idéntica ante cualquier fallo:** «Correo o contraseña incorrectos.», sin revelar si el correo existe. Si no existe, se calcula igual un hash de relleno para que el tiempo de respuesta tampoco lo revele.

# Creación del primer administrador (cierre del 11/11/2026)

**Problema encontrado:** Oracle Cloud no tenía ninguna cuenta Administrador. La configuración inicial existente solo podía
**dar contraseña** a un administrador que ya existiera, y no había forma segura de crear el primero. Habría hecho falta
escribir un `INSERT` a mano en la tabla de usuarios, que es justo lo que se quiere evitar.

**Solución:** `ServicioAutenticacion.CrearAdministradorInicialAsync(nombre, correo, contraseña)`.

{{codigo:src/EcoAccess.P3.Logica/Servicios/ServicioAutenticacion.cs:275:345}}

Garantías:

| Garantía | Cómo se cumple | Prueba |
|---|---|---|
| Un solo uso | se rechaza si existe **alguna** cuenta con rol Administrador, activa o no | `CrearAdministradorInicialAsync_SegundoIntento_RechazaYNoCreaOtraCuenta` |
| No crea un segundo administrador si ya hay uno sin contraseña | ese caso va por `ConfigurarAdministradorInicialAsync` | `CrearAdministradorInicialAsync_HayAdministradorSinContrasena_RechazaParaNoCrearUnSegundo` |
| Un ciudadano no puede elevarse | si el correo ya existe, se rechaza sin tocar la cuenta | `CrearAdministradorInicialAsync_CorreoDeCuentaExistente_RechazaSinElevarla` |
| Datos válidos | la entidad `Usuario` valida nombre y correo; la contraseña exige 12 caracteres | `CrearAdministradorInicialAsync_DatosInvalidos_RechazaSinCrearNada` |
| Sin contraseña en texto plano | solo se guarda el hash PBKDF2; no hay SQL, registros ni argumentos con la contraseña | revisión de código |
| Trazabilidad | `USUARIO.FECHA_REGISTRO_UTC`, `USUARIO_ROL.FECHA_ASIGNACION_UTC`, `CREDENCIAL_ACCESO.FECHA_CAMBIO_UTC` | consulta en Oracle |

`FormConfiguracionInicial` pregunta al abrirse (`FaltaCuentaAdministradorAsync`) si falta la cuenta: en ese caso muestra el
campo **Nombre**; si ya existe un administrador sin contraseña, lo oculta.

**Resultado real:** el 11/11/2026 el propietario abrió la aplicación contra Oracle Cloud y creó su cuenta. La base registró
la cuenta **4521** con rol 3 (Administrador), activa y con credencial (consulta en el documento 10). Con ella se ejecutó T001.

# Cuentas del equipo con dos roles

El equipo decidió que sus tres integrantes tengan **Administrador y Operario** en una sola cuenta cada uno, en lugar de
seis cuentas. El modelo lo permite sin cambios, porque `USUARIO_ROL` es una relación muchos a muchos.

| Integrante | Código visual | Cuenta Oracle | Roles en Oracle (11/11/2026) | Estado |
|---|---|---|---|---|
| Jorge Gutiérrez Thomas | ADM-0001 / OP-0001 | 4521 | 2 y 3 (Operario y Administrador) | VERIFICADO REAL |
| Heyder Canchila | ADM-0002 / OP-0002 | no existe | — | PENDIENTE: falta su correo real |
| Saray Cadena | ADM-0003 / OP-0003 | no existe | — | PENDIENTE: falta su correo real |

- El rol Operario de la cuenta 4521 se asignó con el servicio real, `ServicioUsuarios.AsignarRolAsync`, con la cuenta 4521 como actor. No se escribió SQL a mano. La comprobación se hizo con una consulta a `USUARIO_ROL` (`docs/pruebas/resultados_cierre/roles_equipo_oracle_cloud.txt`).
- La contraseña de la cuenta 4521 **no se cambió**: ya tenía una, elegida por el propietario.
- Los códigos ADM/OP son visuales. No son claves primarias.
- Las contraseñas iniciales propuestas están solo en un archivo local y privado del propietario, fuera del repositorio.

Para crear las cuentas de Heyder y Saray, el administrador usa `FormUsuarios`:

1. Pestaña **Crear cuenta**: nombre y correo real.
2. Pestaña **Cuenta y roles**: marcar Administrador y Operario.
3. Pestaña **Acceso inicial**: escribir la contraseña inicial. La persona la cambia en su primer inicio de sesión.

# El último administrador

`UltimoAdministradorException` impide que el sistema quede sin administradores activos (RN-23). El servicio ya lo comprueba,
pero si dos administradores se quitaran el rol uno al otro a la vez, el repositorio lo detecta dentro de su transacción,
la deshace y lanza la excepción. También `Usuario.QuitarRol` impide dejar una cuenta sin ningún rol (RN-01).

# Cuenta Operario

En Oracle Cloud existe la cuenta 4468 con rol Operario y **sin contraseña**. Para habilitar un operario real:

1. Inicie sesión con la cuenta de administrador.
2. Abra **Ciudadanos y usuarios**.
3. Registre la cuenta del operario (o elija una existente) y asígnele el rol **Operario**.
4. Use la opción de **contraseña inicial**: el administrador escribe una contraseña temporal (`EstablecerContrasenaInicialAsync`) y el operario la cambia en su primer inicio de sesión (`CambiarContrasenaAsync`).

Solo un administrador puede dar la **primera** contraseña, y solo a cuentas de personal que todavía no tienen una. Cambiar
una contraseña existente es exclusivo de su titular.

# Seguridad aplicada

| Medida | Dónde |
|---|---|
| Revalidación del actor en cada operación | `ControlAcceso` |
| Contraseñas con PBKDF2-SHA256, sal y 100 000 iteraciones como mínimo | `HasherPbkdf2`, `CredencialAcceso` |
| Bloqueo temporal tras 5 fallos | `ServicioAutenticacion` |
| Parámetros enlazados contra inyección SQL | todos los repositorios (`ComandosOracle`) |
| Órdenes al ESP32 firmadas con HMAC, con idempotencia y vencimiento | `ProtocoloDispositivoHmac` |
| Secretos fuera del repositorio (User Secrets) | `SinSecretosEnRepositorioTests` |
| Power BI con una cuenta de solo lectura que no ve credenciales | `database/00_administracion/cuenta_informes_powerbi.sql` |
| Datos personales solo para el administrador | `ServicioUsuarios.ObtenerAsync` (RN-25) |

# Pruebas de roles

{{pruebas:MatrizPermisos}}

{{pruebas:ServicioAutenticacion}}

# Preguntas probables

**¿Qué pasa si un operario intenta registrar una regla de ecopuntos?** `ServicioEcopuntos.RegistrarReglaAsync` exige
Administrador y devuelve `PermisoDenegado`. El menú ni siquiera muestra la opción, pero aunque se llamara directamente,
el servicio la rechaza.

**¿Por qué el operario puede ver el informe institucional?** Porque es de solo lectura y lo necesita para supervisar la
operación. No puede cambiar usuarios, materiales ni reglas.

**¿Cómo se crea el primer administrador en una base nueva?** Con la configuración inicial de la aplicación, una sola vez.
Después, las demás cuentas las crea ese administrador.
