# Pruebas de las tres canecas

Ejecutadas el 2026-11-09. **Ninguna es una prueba de hardware físico**: no hay ESP32 ni servomotores conectados. «Simulada» significa `CanecaSimulada` (Lógica), que aplica las mismas reglas que el firmware.

## 1. Pruebas automatizadas

| Prueba | Tipo | Resultado |
|---|---|---|
| PET y cartón → blanca; cáscaras → verde; servilleta → negra, cada uno con su `id_contenedor` y dispositivo (`ResolverDestino_CadaMaterialVaASuCanecaYSuDispositivo`, 4 casos) | lógica | superada |
| Pila (peligroso) → ninguna caneca, mensaje de punto posconsumo | lógica | superada |
| Material de la categoría correcta pero no admitido por esa caneca → rechazo | lógica | superada |
| Caneca fuera de servicio → rechazo | lógica | superada |
| Estación sin caneca de ese color → rechazo | lógica | superada |
| Dos canecas del mismo color → rechazo por ambigüedad | lógica | superada |
| Sin dispositivo → destino solo simulable (`PuedeAbrirFisicamente = false`) | lógica | superada |
| Configurar como verde una caneca que admite PET → rechazo | lógica | superada |
| Segunda caneca del mismo color en la estación → rechazo | lógica | superada |
| Dispositivo ya vinculado a otra caneca → rechazo | lógica | superada |
| Categoría peligrosa o dispositivo con espacios → rechazo | lógica | superada |
| Operario no puede configurar ni renombrar | lógica | superada |
| Renombrar valida longitud y guarda el nombre limpio | lógica | superada |
| **Orden para la blanca: la verde y la negra simuladas la rechazan; la blanca la ejecuta; todos los eventos son de la blanca** | **simulada** | superada |
| **Orden vencida no se acepta; la misma clave de idempotencia no abre dos veces** | **simulada** | superada |
| **Estación real en Oracle Cloud**: configura las tres, persiste, resuelve cada material, rechaza peligroso, dispositivo duplicado (`UQ_CONFIGURACION_DISPOSITIVO`) y categoría 4 (`ORA-02290`), renombra (`TresCanecasOracleTests`) | **integración con servicio real** | superada |

Totales tras la fase: `EcoAccess.P3.Tests` 1 638 superadas (19 nuevas), 1 omitida; `TresCanecasOracleTests` 1/1 contra Oracle Cloud.

## 2. Registro en Oracle, asociación con usuario y caneca, desconexión

Las pruebas ya existentes del proyecto (ejecutadas contra Oracle Cloud en esta recuperación, 225 superadas) cubren el resto de la tabla de requisitos con una caneca: `BridgeOracleTests` (orden → evento → depósito asociados a usuario y caneca, desconexión y recuperación, órdenes duplicadas), `RepositorioInteraccionesOracleTests` y `VerificacionDepositosOracleTests`. Como cada caneca de la estación es un `CONTENEDOR` independiente con su propio dispositivo, el mismo camino aplica a cada una.

## 3. Pendiente (requiere hardware)

| Prueba física | Estado |
|---|---|
| Apertura real de la blanca, verde y negra | pendiente: sin hardware |
| Una orden firmada para la blanca enviada al ESP32 negro es rechazada | pendiente: sin hardware (verificado en firmware por lectura de código y en simulación) |
| Consumo de corriente con dos servos bajo carga | pendiente |
| Calibración y ventana de 10 s medida | pendiente |
