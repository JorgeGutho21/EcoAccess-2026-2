# Matriz de roles y permisos

EcoAccess P3 tiene tres roles (`CodigoRol`): **Ciudadano** (1), **Operario** (2) y **Administrador** (3). Una cuenta
puede tener varios.

Cada servicio vuelve a leer al actor desde la base en cada operación (`ControlAcceso`), así que la interfaz no puede
saltarse el permiso. Si un administrador desactiva una cuenta, la siguiente operación de esa cuenta ya se rechaza.

La matriz está en código (`src/EcoAccess.P3.Logica/Servicios/MatrizPermisos.cs`). La prueba
`MatrizPermisosTests` llama a los servicios reales con los tres roles y falla si alguno no coincide con la matriz.

| Operación | Ciudadano | Operario | Administrador |
|---|:-:|:-:|:-:|
| Consultar mis ecopuntos | ✔ (con consentimiento) | ✘ | ✘ |
| Consultar mis estadísticas | ✔ (con consentimiento) | ✘ | ✘ |
| Consultar ecopuntos de otro ciudadano | ✘ | ✔ | ✔ |
| Listar reglas de ecopuntos | ✘ | ✔ | ✔ |
| Registrar regla de ecopuntos | ✘ | ✘ | ✔ |
| Buscar ciudadanos | ✘ | ✔ | ✔ |
| Estadísticas generales y panel | ✘ | ✔ | ✔ |
| Listar usuarios | ✘ | ✘ | ✔ |
| Asignar o quitar roles | ✘ | ✘ | ✔ |
| Gestionar materiales | ✘ | ✘ | ✔ |
| Informe institucional | ✘ | ✘ | ✔ |
| Diagnóstico de Oracle | ✘ | ✘ | ✔ |

Las operaciones «mis…» exigen el rol Ciudadano. Un operario que también deposita necesita además ese rol.

## Reglas comprobadas

- Una cuenta desactivada o inexistente no pasa ningún control, aunque sea administradora.
- Un administrador no puede quitarse a sí mismo el rol de administrador. El repositorio Oracle impide además dejar
  el sistema sin administradores activos (RN-23).
- Solo las cuentas con rol Operario o Administrador entran a la aplicación de escritorio. El ciudadano usa Telegram.
- Ningún rol lee contraseñas, hashes, tokens ni secretos. Las credenciales se guardan con PBKDF2 y no se muestran en
  ninguna pantalla ni consulta del panel.

## Estado en Oracle Cloud (10/11/2026)

Hay 21 cuentas activas, todas Ciudadano, y ninguna Operario ni Administrador real. El propietario crea esas cuentas
desde el instalador o la aplicación y fija su contraseña. Nadie más la escribe ni la guarda en el repositorio.
