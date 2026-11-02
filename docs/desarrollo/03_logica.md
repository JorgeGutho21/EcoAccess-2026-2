# 03. Lógica de negocio

Proyecto `EcoAccess.P3.Logica`. Define contratos (`Contratos/`) que implementan Datos y Bot, de modo que las reglas no dependen de Oracle ni de Telegram.

| Servicio | Función |
|---|---|
| `ServicioInteracciones` | iniciar, autorizar, registrar eventos, verificar y registro asistido |
| `ServicioConversacionTelegram` | diálogo del bot |
| `ServicioClasificacionImagen` | valida la foto y convierte la propuesta en una consulta del catálogo |
| `ServicioCatalogoAmbiental` | búsqueda por nombre y sinónimos, y admisión por caneca |
| `ServicioEcopuntos` | tarifas, acreditación idempotente y reversos |
| `ServicioDespachoOrdenes` | entrega de órdenes por un canal abstracto |
| `ServicioNotificacionesTelegram` | avisa hechos ya persistidos; nunca los anticipa |

## Reglas esenciales

- **La visión solo propone.** La persona confirma y la autorización exige material admitido, caneca en servicio y ninguna orden vigente en ese contenedor.
- **Concurrencia por contenedor:** una segunda solicitud se rechaza si hay una orden vigente, y Oracle lo refuerza con una restricción.
- **Idempotencia:** una clave por operación evita órdenes, depósitos y créditos duplicados.
- **Separación de funciones:** nadie verifica su propio depósito.
- **Sin sensor no hay detección:** `CierreSinSensor` deja el depósito pendiente, sin puntos.