# Arquitectura de la estación de tres canecas

Fecha: 2026-11-09 · Migración `V021` · Clases `ConfiguracionCaneca` (Entidades), `ServicioTresCanecas` (Lógica), `RepositorioConfiguracionCanecasOracle` (Datos).

## 1. Qué recibe cada caneca (Resolución 2184 de 2019)

| Caneca | Categoría en EcoAccess (`CategoriaMaterial`) | Ejemplos | Condición |
|---|---|---|---|
| **Blanca** | `Aprovechable` (1) | papel, cartón, plástico PET y rígido, vidrio, latas | limpios y secos |
| **Verde** | `OrganicoAprovechable` (2) | cáscaras, restos de fruta y verdura, residuos de jardín | según admita la estación |
| **Negra** | `NoAprovechable` (3) | servilletas y papel higiénico usados, empaques contaminados | — |
| *Ninguna* | `Peligroso` (4) | pilas, medicamentos, aceites, electrónicos | **punto posconsumo**: nunca la negra por defecto |

Los materiales concretos los decide el catálogo de Oracle (`MATERIAL`, 40 habilitados en Oracle Cloud) y la admisión de cada caneca (`CONTENEDOR_MATERIAL`).

## 2. Modelo de datos: qué se reutilizó y qué se agregó

| Concepto | Tabla | Estado |
|---|---|---|
| Estación | `PUNTO_INSTALACION` | reutilizada: ya tenía 1 → N canecas |
| Caneca | `CONTENEDOR` (id estable, nombre, QR, estado) | reutilizada |
| Materiales admitidos | `CONTENEDOR_MATERIAL` | reutilizada |
| Orden de apertura | `ORDEN_APERTURA` (por `id_contenedor`, clave de idempotencia, vencimiento) | reutilizada |
| Historial de operaciones | `INTERACCION`, `EVENTO_DISPOSITIVO`, `DEPOSITO` | reutilizadas |
| **Categoría/color, dispositivo, ubicación** | **`CONFIGURACION_CANECA`** (1:1 con `CONTENEDOR`) | **nueva (V021)** |
| Vista para consultar la estación | `VISTA_CANECAS_ESTACION` | nueva (V021) |

```
PUNTO_INSTALACION (estación) 1 ──< CONTENEDOR (caneca) 1 ── 0..1 CONFIGURACION_CANECA
                                       │                          (categoria → color,
                                       ├──< CONTENEDOR_MATERIAL >── MATERIAL     identificador_dispositivo,
                                       └──< INTERACCION ──< ORDEN_APERTURA        ubicacion_en_estacion)
```

El color **no** se guarda: se deduce de la categoría (`ConfiguracionCaneca.ColorPara`), así no pueden contradecirse. Restricciones en Oracle: `CK_CONFIGURACION_CATEGORIA` (solo 1, 2, 3), `UQ_CONFIGURACION_DISPOSITIVO` (un ESP32 por caneca), formato del dispositivo y ubicación sin espacios sobrantes.

## 3. Cómo se elige la caneca (`ServicioTresCanecas.ResolverDestinoAsync`)

1. Se busca el material **confirmado** en el catálogo de Oracle (si está deshabilitado, se rechaza).
2. Su categoría da el color. Peligroso → mensaje de punto posconsumo; ninguna caneca.
3. Se buscan las canecas **de esa estación** con esa categoría. Ninguna → rechazo; **más de una** → rechazo por ambigüedad (no se abre «la primera»).
4. La caneca debe estar **en servicio** y **admitir ese material** explícitamente.
5. Se devuelve `DestinoCaneca(IdContenedor, Nombre, Color, IdentificadorDispositivo)`: la orden se crea para **ese identificador**, nunca «para la blanca».
6. Sin dispositivo vinculado, `PuedeAbrirFisicamente = false`: la apertura solo puede simularse.

Configurar (`ConfigurarAsync`) y renombrar (`RenombrarAsync`) exigen rol administrador. No se puede configurar como blanca una caneca que admite restos de comida, ni tener dos canecas del mismo color en una estación, ni repetir un dispositivo.

## 4. Identificación por QR

| Alternativa | Cómo funciona | Ventaja | Riesgo |
|---|---|---|---|
| **A. QR por caneca (principal)** | cada caneca conserva su `identificador_qr`; el enlace `https://t.me/EcoAccessP3_JG_bot?start=<id>` lleva al bot a esa caneca | ya implementado y probado (`CodigoQrCaneca`, `EnlaceTelegram`); la persona no elige | el ciudadano puede escanear la caneca equivocada: la admisión lo rechaza y `ResolverDestinoAsync` indica el color correcto |
| B. QR de estación + selección automática | un QR para la estación; el bot elige la caneca con `ResolverDestinoAsync` | un solo QR impreso | requiere un QR de punto (aún no existe en la tabla) y nueva rama en la conversación |

Se mantiene **A** como estrategia principal porque no cambia el flujo ya probado; **B** queda diseñada (el servicio de destino ya existe) para una siguiente iteración. Ningún QR contiene contraseñas ni tokens: solo el identificador público aleatorio de 128 bits.

## 5. Integración física: alternativas evaluadas

El firmware actual (`hardware/servosecoacces/`) mueve **una tapa con dos servomotores** (GPIO 18 y 19) y está **vinculado a una sola caneca** (`ID_CANECA`): rechaza órdenes con otro `idContenedor`.

| Criterio | **A. Una placa, tres tapas (6 servos)** | **B. Un ESP32 por caneca (elegida)** |
|---|---|---|
| Seguridad | un solo binding para tres tapas: hay que mapear `idContenedor` → par de servos en el firmware; un error de mapeo abre la tapa equivocada | cada ESP32 tiene **su clave HMAC** y su `ID_CANECA`: una orden para la blanca es rechazada criptográficamente por la verde y la negra |
| Firmware | cambio no trivial (tabla de canecas, reserva por tapa, recuperación por tapa) | **sin cambios**: el firmware ya probado se carga tres veces con distinta configuración privada |
| Alimentación | 6 servos en una fuente: picos de arranque simultáneos; requiere fuente de mayor capacidad y distribución | una fuente por caneca (2 servos), fallas aisladas |
| Fallas | una placa caída deja sin servicio las tres canecas | una caneca fuera de servicio no afecta a las demás |
| Costo | 1 ESP32 + fuente grande | 3 ESP32 (componentes económicos) + 3 fuentes pequeñas |
| Complejidad de cableado | alta (un solo tablero) | baja y repetible |

**Decisión: alternativa B.** Prioriza seguridad y reutiliza el firmware y el protocolo ya probados. Detalle eléctrico en [CONEXIONES_ESP32.md](CONEXIONES_ESP32.md), protocolo en [PROTOCOLO_APERTURA.md](PROTOCOLO_APERTURA.md) y pruebas en [PRUEBAS_TRES_CANECAS.md](PRUEBAS_TRES_CANECAS.md).

## 6. Límites honestos

* No hay sensores de ingreso ni de peso en el hardware documentado: sin medición física, el depósito queda **pendiente de verificación** por un operario y no hay kilogramos reales.
* La interfaz Windows Forms todavía no tiene una pantalla para configurar las tres canecas; hoy se configura con el servicio (pruebas) o con SQL Developer. Está registrada como pendiente.
* Ninguna apertura física se ha probado: no hay hardware conectado a este equipo.
