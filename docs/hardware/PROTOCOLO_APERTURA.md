# Protocolo de apertura (tres canecas)

El protocolo ya existente (`ProtocoloDispositivoHmac`, firmware `servosecoacces.ino`) cumple los requisitos de la estación de tres canecas sin cambios de formato: cada orden identifica caneca, dispositivo y vigencia, y está firmada con la clave del dispositivo.

## 1. Datos de una orden

| Requisito | Campo | Dónde se valida |
|---|---|---|
| Estación | implícita: `CONTENEDOR.id_punto_instalacion` del contenedor | `ServicioTresCanecas.ResolverDestinoAsync` |
| Caneca | `idContenedor` | firmware: `caneca == ID_CANECA`; `CanecaSimulada`: `comando.IdContenedor != _idContenedor` |
| Dispositivo | `dispositivo` en el sobre firmado | firmware: `strcmp(dispositivo, DISPOSITIVO)`; binding `DISPOSITIVO|ID_CANECA|VERSION_CLAVE` |
| Operación | `tipo` del sobre (`actuacion-serial`) | firmware |
| Identificador único | `idOrden` + `claveIdempotencia` | `ORDEN_APERTURA` y reserva persistente en el ESP32 |
| Vigencia | `venceUtc` | firmware y simulador rechazan órdenes vencidas |
| Estado | `ORDEN_APERTURA.estado` (`EstadoOrdenApertura`) | Lógica/Oracle |
| Confirmación | recibos autenticados (apertura y cierre) → `EVENTO_DISPOSITIVO` | `ServicioInteracciones.RegistrarEventoAsync` |

Sobre firmado (campos exactos que exige el firmware): `version`, `dispositivo`, `versionClave`, `tipo`, `datos` (JSON en base64 con `idOrden`, `idContenedor`, `claveIdempotencia`, `venceUtc`) y `firma` HMAC-SHA256 sobre `ecoaccess-p3/v1\n<dispositivo>\n<versionClave>\n<tipo>\n<datos>`.

## 2. Secuencia

1. El ciudadano confirma el material (foto + confirmación, o selección manual).
2. `ServicioTresCanecas.ResolverDestinoAsync(estación, material)` devuelve el `id_contenedor` correcto o un rechazo.
3. La lógica crea la `ORDEN_APERTURA` para ese contenedor, con clave de idempotencia y vencimiento.
4. El adaptador (bridge) pide un desafío, comprueba con Oracle que la orden sigue autorizada y la firma con la clave **del dispositivo de esa caneca**.
5. El ESP32 valida firma, desafío, dispositivo y caneca; **reserva** la orden en memoria persistente antes de mover nada.
6. Abre, envía recibo; espera 10 s; cierra, envía recibo. EcoAccess registra los eventos y avisa al ciudadano.
7. Sin sensor, el depósito queda pendiente de verificación (no genera ecopuntos hasta que un operario lo confirme).

## 3. Garantías

| Riesgo | Por qué no ocurre |
|---|---|
| Una orden para la blanca abre la verde o la negra | la orden lleva `idContenedor` de la blanca y la firma de su clave: el ESP32 verde/negro tiene otro `ID_CANECA` y otra clave → rechazo |
| Abrir «por color» sin identificador | la lógica nunca emite órdenes por color: `DestinoCaneca` solo existe con `IdContenedor` y configuración verificada |
| Orden vencida | `venceUtc` comprobado en Oracle, bridge, firmware y simulador |
| Orden repetida | `claveIdempotencia` y reserva persistente: la misma clave no vuelve a abrir |
| Caneca sin configurar o ambigua | `ResolverDestinoAsync` rechaza (sin color, dos del mismo color, fuera de servicio, no admite) |
| Desconexión durante el ciclo | el firmware no reintenta una ejecución incierta y queda bloqueado hasta revisión |

Las pruebas que respaldan cada fila están en [PRUEBAS_TRES_CANECAS.md](PRUEBAS_TRES_CANECAS.md).
