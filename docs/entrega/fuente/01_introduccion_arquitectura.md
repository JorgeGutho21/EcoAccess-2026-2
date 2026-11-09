# Introducción

## Descripción del proyecto

EcoAccess es un sistema para separar correctamente los residuos en la fuente en un campus de Valledupar. Un ciudadano escanea el código QR de una caneca, conversa con **Eco** (un bot de Telegram), envía una foto del residuo o lo elige de una lista, confirma el material y, si la caneca lo admite, el sistema autoriza una orden de apertura firmada que ejecuta un ESP32 con servomotores. Cada paso queda registrado en Oracle y los depósitos verificados generan **ecopuntos** auditables. Una aplicación de escritorio en Windows Forms permite administrar usuarios, materiales, canecas, puntos de instalación, depósitos, ecopuntos y estadísticas.

## Problema que resuelve

Los residuos aprovechables se contaminan al mezclarse con los no aprovechables, y la persona que deposita no siempre sabe en qué caneca va cada cosa. EcoAccess orienta en el momento del depósito, verifica que la caneca admita el material y deja evidencia para medir la participación sin inventar pesos ni datos.

## Objetivo general

Construir y validar un sistema de información, por capas, que integre una aplicación de escritorio en C#, una base de datos Oracle, un bot de Telegram con reconocimiento visual y un dispositivo ESP32 para gestionar el depósito correcto de residuos en una estación de tres canecas.

## Objetivos específicos

1. Modelar en Oracle 19c, con integridad referencial y restricciones reales, usuarios, materiales, estaciones, canecas, interacciones, órdenes, eventos, depósitos y ecopuntos.
2. Implementar en C# (.NET Framework 4.8) una arquitectura por capas con entidades, lógica, datos y presentación separadas y probadas.
3. Ofrecer a los ciudadanos un canal de Telegram que reconozca el residuo con un modelo visual local y que funcione aunque el modelo falle (selección manual).
4. Dirigir cada material a la caneca correcta (blanca, verde o negra) y orientar los residuos peligrosos a un punto posconsumo.
5. Proteger la apertura con órdenes firmadas, vigentes e idempotentes, y distinguir siempre simulación de prueba física.
6. Trabajar con una base compartida en Oracle Cloud que puedan usar tres integrantes sin Docker.

## Alcance

Incluye la aplicación de escritorio, el bot, el reconocimiento visual local, la base en Oracle Cloud, el instalador de migraciones, el protocolo y firmware del ESP32, y las pruebas automatizadas. El piloto es una estación de un campus.

## Limitaciones (comprobadas)

* No hay sensores de ingreso ni de peso: sin medición física no se registran kilogramos y el depósito queda pendiente de verificación por un operario.
* No hay hardware conectado: las aperturas de las tres canecas están probadas en simulación, no físicamente.
* El reconocimiento visual alcanza 47 % de categorías correctas en una muestra de 60 fotos públicas con el catálogo real: por eso la persona siempre confirma y existe la selección manual.
* El registro y la vinculación de cuentas por Telegram requieren una página HTTPS pública que aún no está publicada.
* El bot se ejecuta en el computador del propietario; el despliegue centralizado está diseñado pero no ejecutado.

# Arquitectura

## Arquitectura por capas

| Capa | Proyecto | Responsabilidad | Depende de |
|---|---|---|---|
| Entidades | `EcoAccess.P3.Entidades` | clases del dominio con sus validaciones (`Usuario`, `Material`, `Contenedor`…) | nada |
| Lógica | `EcoAccess.P3.Logica` | casos de uso, permisos, reglas (`ServicioContenedores`, `ServicioTresCanecas`…) y contratos `IRepositorio*` | Entidades |
| Datos | `EcoAccess.P3.Datos` | repositorios Oracle con ADO.NET y SQL parametrizado; `FabricaConexionesOracle`, `WalletOracleNube` | Lógica, Entidades |
| Presentación | `EcoAccess.P3.Presentacion` | Windows Forms; compone los servicios en `Program.cs` | Lógica, Entidades (Datos solo en la composición) |
| Bot | `EcoAccess.P3.Bot` | Telegram, visión, bridge del ESP32; segunda raíz de composición | Lógica, Entidades, Datos |
| Instalador | `EcoAccess.P3.Instalador` | aplica migraciones en Oracle Cloud | Datos |
| Compatibilidad | `EcoAccess.P3.Compatibilidad` | API modernas reimplementadas para .NET Framework 4.8 | nada |

La prueba `PruebasArquitectura.ReferenciasDeProyecto_CoincidenConElDiagramaDeDependencias` falla si alguien agrega una referencia que rompe este diagrama.

## Diagrama de componentes

```
 Ciudadano ──Telegram──► EcoAccess.P3.Bot ──► Lógica ──► Datos ──mTLS──► Oracle Cloud (ECOACCESS_P3_APP)
                              │  ▲                                        ▲
              Ollama + LFM3-VL┘  │ órdenes firmadas (HMAC)                 │
                                 ▼                                         │
                      Bridge ──USB──► ESP32 (una por caneca)               │
 Administrador ──► Windows Forms ──► Lógica ──► Datos ─────────────────────┘
 Integrantes ──► SQL Developer (cuentas ECO_DEV_*) ────────────────────────┘
```

## Comunicación entre servicios

* Windows Forms y el bot no se comunican entre sí: comparten la base de datos.
* El bot y el ESP32 se comunican por un bridge local con mensajes firmados HMAC-SHA256; el ESP32 nunca recibe credenciales de Oracle ni de Telegram.
* La visión corre en Ollama local (`127.0.0.1:11434`) sin enviar fotos a la nube.

## Principios SOLID y DRY aplicados (con su clase)

| Principio | Dónde | Beneficio |
|---|---|---|
| Responsabilidad única | `WalletOracleNube` solo entiende el wallet; `FabricaConexionesOracle` solo abre conexiones; `ServicioTresCanecas` solo decide destino y configuración de canecas | cada clase cambia por una sola razón |
| Abierto/cerrado | nuevas migraciones se agregan a `PlanInstalacion.Pasos` sin tocar el ejecutor | ampliar sin modificar |
| Sustitución de Liskov | cualquier `IClasificadorMaterial` (`ClasificadorOllamaLocal`, `ClasificadorGroq`, `ClasificadorMaterialSimulado`) sirve al servicio de clasificación | pruebas sin modelo real |
| Segregación de interfaces | `IRepositorioConfiguracionCanecas` es un contrato aparte de `IRepositorioContenedores` | no obliga a cambiar sus implementaciones |
| Inversión de dependencias | la lógica depende de `IRepositorio*`; Oracle se inyecta en `Program.cs` | la lógica se prueba con repositorios en memoria |
| DRY | colores en `TemaEcoAccess` (`ColorEstado`, `CanecaBlanca`…); el color de una caneca se deduce de la categoría y no se guarda dos veces; mensajes de validación en `MensajesValidacion` | un cambio, un lugar |
