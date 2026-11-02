# 02. Entidades

Proyecto `EcoAccess.P3.Entidades`: recuperado y adaptado del proyecto anterior. No depende de datos ni de interfaz. Cada entidad protege su estado y valida en el constructor.

| Grupo | Clases | Responsabilidad |
|---|---|---|
| Usuarios | `Usuario`, `CredencialAcceso`, `CodigoRol` | identidad, consentimiento y roles |
| Materiales | `Material`, `CategoriaMaterial` | nombre, sinónimos, categoría, precauciones y ruta de separación |
| Contenedores | `Contenedor`, `PuntoInstalacion`, `EstadoContenedor` | caneca, ubicación, QR y materiales admitidos |
| Operación | `Interaccion`, `OrdenApertura`, `EventoDispositivo`, `Deposito`, `CanalInteraccion` | visita, orden, hechos del dispositivo y depósito |
| Telegram | `VinculoTelegram`, `ConversacionTelegram`, `TokenVinculacion` | sesión persistente y diálogo |
| Ecopuntos | `MovimientoEcopuntos`, `ReglaEcopuntos` | créditos y reversos |

## Relaciones

Una `Interaccion` pertenece a un usuario y una caneca; emite como máximo una `OrdenApertura` y termina con un `Deposito` o sin él. Los `EventoDispositivo` hacen avanzar la orden.

## Adiciones de esta versión

- `CanalInteraccion.Asistido`: el operador atiende al ciudadano.
- `TipoEventoDispositivo.CierreSinSensor` y `Deposito.SinSensor`: distinguen una ventana terminada de una detección real.