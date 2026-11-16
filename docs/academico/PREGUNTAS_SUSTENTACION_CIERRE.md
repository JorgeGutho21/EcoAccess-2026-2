# Banco de preguntas de sustentación — cierre definitivo (113 preguntas)

Cada respuesta remite a código, migraciones o mediciones del repositorio. Cuando algo **no** está hecho o validado, la
respuesta lo dice. Complementa `GUIA_SUSTENTACION.md` (tercer corte: ADO.NET, DAO/Repository y tres capas).

## A. Arquitectura (1–10)

1. **¿Qué arquitectura tiene EcoAccess P3?** Tres capas más entidades: Presentación (Windows Forms y bot de Telegram), Lógica (servicios y reglas) y Datos (repositorios ADO.NET con ODP.NET). Las entidades se comparten entre las tres capas. Todo está en .NET Framework 4.8 con C# 9.
2. **¿Por qué la lógica no conoce a Oracle?** Depende de contratos (`IRepositorio*`). Las pruebas unitarias usan dobles en memoria y la integración usa los repositorios Oracle reales.
3. **¿Qué patrón de acceso a datos se usa?** Repository/DAO. Cada repositorio abre su conexión con `FabricaConexionesOracle` y usa comandos con parámetros enlazados por nombre (`ComandosOracle.Crear`).
4. **¿Dónde se valida un permiso?** En cada servicio, antes de tocar datos, con `ControlAcceso`. Este vuelve a leer al actor de la base en cada operación.
5. **¿Por qué no confiar en la sesión de la GUI?** Si un administrador desactiva una cuenta, la siguiente operación de esa cuenta debe rechazarse. Revalidar lo garantiza (RN-15).
6. **¿Qué hace el bot?** Atiende Telegram por long polling, recibe fotos, consulta la visión, orienta el destino y, con dispositivo configurado, emite órdenes de apertura.
7. **¿Qué es «Eco»?** El recorrido conversacional del bot: saludo, QR, foto, destino o apertura y despedida (`ServicioConversacionTelegram.FlujoEco.cs`).
8. **¿Cómo se comunica el software con la caneca?** Mediante `ICanalDispositivo`. Hay tres implementaciones: simulador, HTTP local y serial USB. El canal solo entrega órdenes; lo que pasó lo prueban los eventos del dispositivo.
9. **¿Qué es `EstacionSimulada`?** Un adaptador ESP32 simulado con una tapa por contenedor y un ESP32 por tapa. Una orden para la blanca nunca abre la verde ni la negra.
10. **¿Dónde está la configuración secreta?** En User Secrets de Windows (`%APPDATA%\Microsoft\UserSecrets`), fuera del repositorio. Una prueba recorre el repositorio buscando claves y tokens (`SinSecretosEnRepositorioTests`).

## B. Base de datos y Oracle (11–25)

11. **¿Qué base usa el proyecto?** Oracle Autonomous Database 19c en Oracle Cloud, región Bogotá, esquema `ECOACCESS_P3_APP`, con conexión mTLS por wallet.
12. **¿Cuántas tablas tiene?** 27 antes de esta misión. V025 agregó `LOTE_SIMULACION` y `REGISTRO_SIMULACION`.
13. **¿Qué es una interacción?** Una solicitud de depósito de un ciudadano en una caneca. Tiene canal, cantidad declarada y estado.
14. **¿Qué diferencia hay entre orden de apertura y depósito?** La orden autoriza abrir una tapa durante una ventana. El depósito es la evidencia de que algo entró, o de que se declaró sin sensor. Hay como máximo un depósito por orden (RN-17).
15. **¿Qué guarda `evento_dispositivo`?** Acuse, apertura, detección, cierre, fallo y cierre sin sensor, con un identificador externo único que da idempotencia.
16. **¿Qué es `configuracion_caneca`?** La tabla de V021 que da a cada caneca su color (categoría), su ESP32 y su ubicación dentro de la estación.
17. **¿Qué es `vista_canecas_estacion`?** Una vista para SQL Developer con estación, caneca, color, estado, dispositivo y QR.
18. **¿Qué es `vista_depositos_estado`?** La vista de V025 que muestra cada depósito con su estado de registro, sus pesos medido y estimado por separado, su color de caneca y su marca de simulación o ensayo.
19. **¿Cómo se excluyen los datos de prueba de los informes?** Con la columna `es_ensayo` de las vistas `ea_r_*` (V015/V019): canal Simulador o Telegram simulado, canecas ficticias, estaciones «SIMULACIÓN» y códigos de prueba.
20. **¿Por qué no se borra nada en Oracle?** La misión lo prohíbe: crear registros experimentales no autoriza borrar historia. Todas las migraciones nuevas son aditivas.
21. **¿Qué son D001 y D002?** Datos de demostración. D001 es la caneca DEMO con su QR ficticio. D002 es la estación DEMO de tres canecas (canecas 1, 289 y 290).
22. **¿Cuántas canecas había antes de V024?** Cuatro: la estación DEMO (1, 289, 290) y la caneca piloto 1093 con el QR oficial `EcoAccessCanecaPiloto1`. No se borró ninguna.
23. **¿Por qué la 1093 es la «cuarta caneca»?** Es la caneca oficial del piloto, en otro punto de instalación (1015). No es un duplicado de la DEMO.
24. **¿Cómo se verifica que Oracle quedó sano?** Con los conteos de todas las tablas antes y después de cada cambio, y con `user_objects` sin objetos inválidos (0 tras V024 y V025).
25. **¿Hay PL/SQL en el proyecto?** Sí. V022 crea el paquete `PKG_ECOACCESS_STATS`, la función `FN_RESOLVER_CANECA_DISPONIBLE` y el procedimiento `PR_CONSULTAR_ESTADISTICAS`. V011 crea el trigger `tr_ecop_unidades`.

## C. Migraciones (26–33)

26. **¿Cómo se registran las migraciones en la nube?** En `ECO_MIGRACION_APLICADA`, con versión, archivo y huella SHA-256 del archivo exacto.
27. **¿Por qué importa el fin de línea?** La huella depende de los bytes. V023 tenía CRLF en una copia por `core.autocrlf` y no coincidía. Las migraciones aplicadas se marcaron `-text` en `.gitattributes`.
28. **¿Por qué faltaban V021 y V022?** Se escribieron en la línea V2 y se aplicaron en la nube, pero no estaban en este repositorio. Se copiaron byte a byte, con la misma huella.
29. **¿Cómo se prueba una migración antes de la nube?** En el esquema aislado `ECOACCESS_P3_PRUEBAS` del Oracle FREE local: se aplica, se ejecuta otra vez para comprobar que es repetible, se revierte y se reaplica.
30. **¿Qué hace V024?** Convierte la caneca piloto en la blanca de una estación de tres. Agrega una verde y una negra (fuera de servicio), sin cambiar el QR.
31. **¿Qué hace V025?** Crea el registro de lotes de simulación y las vistas de estado.
32. **¿Las migraciones tienen reversión?** Sí: R023, R024 y R025 en `reversion/`. Ningún instalador las ejecuta. R024 se detiene si las canecas nuevas ya tienen historia, y R025 si hay lotes.
33. **¿Qué es T001?** El script de tarifas propuestas. No es una migración: lo ejecuta un administrador real, que queda como creador de las reglas.

## D. Roles y seguridad (34–45)

34. **¿Qué roles hay?** Ciudadano (1), Operario (2) y Administrador (3). Una cuenta puede tener varios.
35. **¿Dónde está la matriz de permisos?** En `MatrizPermisos`: 12 operaciones con sus roles. Se documenta en `docs/seguridad/MATRIZ_ROLES.md`.
36. **¿Cómo se sabe que los servicios cumplen la matriz?** `MatrizPermisosTests` llama a los servicios reales con cada rol. Además, una prueba de mutación (abrir a propósito un permiso) hizo fallar la prueba.
37. **¿Qué puede hacer un ciudadano?** Consultar sus ecopuntos y sus estadísticas, siempre que haya aceptado el tratamiento de datos. No tiene operaciones de gestión.
38. **¿Qué puede hacer un operario?** Buscar ciudadanos, ver estadísticas generales y reglas de ecopuntos, consultar ecopuntos de otros y verificar depósitos.
39. **¿Qué es exclusivo del administrador?** Usuarios y roles, materiales, reglas de ecopuntos, informe institucional y diagnóstico de Oracle.
40. **¿Puede un administrador quitarse su propio rol?** No. Además, el repositorio impide dejar el sistema sin administradores activos (RN-23).
41. **¿Cómo se guardan las contraseñas?** Con PBKDF2 (hash con sal). Ningún panel ni consulta las muestra.
42. **¿Hay administradores reales en la nube?** No. Las cuentas existentes son ciudadanos ficticios de prueba. El propietario debe crear el operario y el administrador con su propia contraseña.
43. **¿Cómo se protege la orden hacia el ESP32?** Con el protocolo HMAC (`ProtocoloDispositivoHmac`), una clave de idempotencia y un vencimiento. El firmware solo obedece órdenes de su propio contenedor.
44. **¿Se envían fotos a terceros?** Solo con el consentimiento vigente de la persona (V018). Una foto con personas no se conserva ni se ofrece a un servicio externo.
45. **¿Cómo se evita la inyección SQL?** Los repositorios usan parámetros enlazados y nunca concatenan texto de la persona en SQL.

## E. Tres canecas (46–53)

46. **¿Qué dice la Resolución 2184 de 2019?** Define el código de colores: blanco para aprovechables, verde para orgánicos aprovechables y negro para no aprovechables.
47. **¿Y los residuos peligrosos?** No van en ninguna caneca de colores. `ResolutorDestino` los envía a «posconsumo».
48. **¿Cómo un solo QR sirve para tres canecas?** El QR identifica la caneca blanca. De ella se toma la estación y `ResolutorDestino` elige el color por categoría.
49. **¿Qué pasa si la estación no tiene caneca de ese color?** No se inventa una: la respuesta queda como antes.
50. **¿Una foto de comida abre la caneca verde?** No. La apertura por foto sigue reservada a los aprovechables. Lo orgánico recibe la orientación «caneca VERDE, al centro» hasta que exista la caneca física.
51. **¿Qué pasa si Oracle falla al leer la estación?** La persona recibe la respuesta original y el bot lo anota en su registro.
52. **¿Las canecas verde y negra del piloto existen físicamente?** No. Están en Oracle y fuera de servicio hasta que se instalen.
53. **¿Cómo se probó?** Con 18 pruebas unitarias y 2 de integración, en el esquema aislado y en Oracle Cloud.

## F. Flujo Eco y hardware (54–65)

54. **¿Cuánto dura la ventana de depósito?** 10 segundos (`VentanaDepositoSegundos`), igual que `ABIERTO_MS` del firmware.
55. **¿Qué es DECLARADA_SIN_SENSOR?** Un depósito de una caneca sin sensor: la tapa se abrió y cerró, la persona lo declaró y nadie lo comprobó.
56. **¿Qué otros estados hay?** DETECTADA_POR_SENSOR (el sensor registró un ingreso) y VERIFICADA (un operario distinto de quien depositó confirmó el material).
57. **¿Se guarda el estado aparte?** No. Se deduce de `sin_sensor` y de la verificación (`Deposito.EstadoRegistro`), así hay una sola fuente de verdad.
58. **¿Qué pasa si llega dos veces el mismo evento?** Se marca como repetido y no duplica el depósito. Hay pruebas unitarias y de integración.
59. **¿Y si se ejecuta dos veces la misma orden?** La clave de idempotencia evita una segunda apertura (`EstacionSimuladaTests`).
60. **¿Una orden vencida abre?** No. Los eventos que exigen ventana después del vencimiento quedan en revisión.
61. **¿Se probó con el ESP32 real?** No en esta misión: se prohibió cargar firmware y mover motores. Todo es simulación de software.
62. **¿Qué pasa si la caneca está ocupada?** El bot responde que está en uso y no crea otra orden.
63. **¿Qué demuestra que la persona está en la caneca?** Escanear el QR. La presencia dura 5 minutos; una foto sola no la demuestra.
64. **¿Qué es el modo software?** Sin dispositivo configurado, Eco informa el destino y no crea solicitud, orden ni apertura.
65. **¿Dónde está la guía de hardware?** En `docs/hardware/GUIA_FINAL_HARDWARE.md`.

## G. Visión e IA (66–77)

66. **¿Qué modelos se usan?** LFM3-VL local (Ollama, 1,7 B) y Groq `qwen/qwen3.8-27b` en la nube como respaldo, con consentimiento.
67. **¿Por qué no basta la confianza del modelo?** Porque no está calibrada: declara 0,95 también cuando se equivoca. La apertura exige que el nombre del objeto corrobore el material.
68. **¿Qué exactitud se midió?** En la cuarta ronda independiente: 119/141 (84 %) en la clave del objeto, con 0 falsas admisiones.
69. **¿Se alcanzó la meta del 90 %?** No. Se reporta tal cual y no se ajustó nada después de medir.
70. **¿Qué es una falsa admisión?** Un residuo que no debía entrar y que la política habría abierto. La meta es 0, y se cumple en la ronda 4.
71. **¿Cómo se evita hacer trampa?** Las fotos se etiquetan antes de ejecutar el modelo. Una ronda usada para ajustar deja de contar como independiente.
72. **¿Qué pasa con varios objetos?** El resultado `VariosObjetos` pide una foto de uno solo.
73. **¿Y con una persona en la foto?** Se reconoce como persona, no se trata como residuo y no se envía a terceros.
74. **¿Qué límite tiene Groq gratis?** Para visión, unas 60 fotos al día (≈3 200 tokens por foto, 8 000 tokens por minuto). Por eso se pausa entre fotos.
75. **¿Qué pasa si se agota el cupo?** Eco ofrece elegir el material manualmente. Nunca abre sin evidencia.
76. **¿El catálogo es colombiano?** Está adaptado. Los materiales siguen la Resolución 2184 e incluyen icopor contaminado y multicapa tipo Tetra Pak. El intérprete reconoce, por ejemplo, empaques de mecato (`snack_wrapper`). Se evaluó con 195 fotos de productos vendidos en Colombia (`docs/vision/EVALUACION_COLOMBIA.md`): 150/195 claves correctas tras la corrección.
77. **¿Se guardan las fotos?** No. Solo se guardan metadatos de la consulta (`CONSULTA_VISION`, V023), sin imagen ni identificador de Telegram.

## H. Pesos y ecopuntos (78–85)

78. **¿Qué diferencia hay entre `peso_medido_kg` y `peso_estimado_kg`?** El primero solo lo llena una báscula. El segundo es una estimación del catálogo y nunca se presenta como pesaje.
79. **¿Cómo se estima?** Con `CatalogoPesosEstimados` (versión 2026.11.10-1): kg por unidad × unidades declaradas, entre 1 y 50 unidades.
80. **¿Qué pasa con un material sin referencia?** El depósito queda «sin peso». No se inventa.
81. **¿Cuál es la matriz de ecopuntos?** Lata 5, PET 4, cartón 3, vidrio 3, acero 3, papel 2 y multicapa 2. Todo lo demás da 0.
82. **¿Qué estados tienen los ecopuntos?** Provisional (sin verificar), Confirmado (verificado con netos positivos) y Anulado (netos 0).
83. **¿Por qué la nube muestra 0 ecopuntos?** No hay reglas vigentes. T001 las crea, pero exige un administrador real, que todavía no existe.
84. **¿Un orgánico da puntos?** No: la categoría manda. Solo los aprovechables de la matriz dan puntos.
85. **¿Se puede cambiar la matriz?** Sí. En Oracle cada valor es una regla con vigencia y la nueva versión se crea sin borrar la anterior.

## I. Simulación (86–93)

86. **¿Qué es el lote `SIM-2026-11-CIERRE`?** 50 ciudadanos ficticios y 100 depósitos sintéticos en Oracle Cloud, para demostrar el panel y Power BI.
87. **¿Son depósitos reales?** No. Tienen canal Simulador, están en una estación «SIMULACIÓN» y su peso es solo estimado.
88. **¿Cómo se generaron?** Con `GeneradorLoteSimulacion`, que usa los mismos servicios que el bot, con un reloj propio repartido en 30 días y la semilla 20261110.
89. **¿Es idempotente?** Sí. Cada fila queda en `REGISTRO_SIMULACION` con una clave estable, y la segunda ejecución creó 0 filas.
90. **¿Cómo se excluye por defecto?** Las vistas de informes los marcan `es_ensayo = 1`. En la nube, los informes reales muestran 0 y el modo experimental muestra 100.
91. **¿Los ciudadanos ficticios pueden entrar?** No. Usan el dominio `.invalid` y no tienen contraseña ni Telegram.
92. **¿Cuál fue la distribución?** 27 declarados sin sensor, 15 detectados por sensor y 58 verificados. Por color: 85 blancos, 13 verdes y 2 negros.
93. **¿Se puede borrar el lote?** La misión no autoriza borrar. Para retirarlo de una demostración basta el filtro por defecto.

## J. Panel y Power BI (94–100)

94. **¿Qué muestra el dashboard institucional?** 8 indicadores, filtros de periodo, caneca, ubicación, material y categoría, agrupación por día, semana o mes, y gráficos.
95. **¿Qué es el modo experimental?** El selector «Datos → Demostración y simulaciones», con el aviso «no son actividad física».
96. **¿Qué mostró con datos reales?** Todo en 0, porque todavía no hay actividad física real.
97. **¿Y en modo experimental?** 96 depósitos en el periodo más 4 en el anterior, 55 verificados, 119 unidades y 50 ciudadanos.
98. **¿Cuánto tarda?** 4,2 s el operativo y 16,7 s el experimental contra Oracle Cloud.
99. **¿Power BI está validado?** Sesiones anteriores verificaron dos páginas con datos reales. Las páginas con el lote nuevo no se abrieron en Power BI Desktop en esta misión.
100. **¿Hay integración con Google Forms?** No. No existe en el repositorio y no se inventó. Queda pendiente.

## K. Pruebas (101–108)

101. **¿Cuántas pruebas unitarias hay?** 1 760 aprobadas y 1 omitida, la de enlaces simbólicos, que requiere un privilegio de Windows.
102. **¿Y de integración?** 211 aprobadas en el esquema aislado. Hay 7 inconclusas por diseño (visión real, PDB Docker, base sin administradores), que no se cuentan como aprobadas.
103. **¿Se probó la concurrencia?** Sí, con 1, 10, 20 y 30 personas simultáneas, cuentas existentes y 30 altas consecutivas: 0 errores.
104. **¿Por qué tarda ≈1 s cada alta?** Por PBKDF2, que es lento a propósito para proteger contraseñas.
105. **¿Qué es una prueba de mutación?** Cambiar a propósito el código para comprobar que la prueba falla. Se hizo con la matriz de permisos.
106. **¿Qué es el esquema aislado?** `ECOACCESS_P3_PRUEBAS` en el Oracle FREE local. Los scripts se niegan a ejecutarse fuera de él.
107. **¿Las pruebas tocan la nube?** Solo las de lectura (`EstacionTresCanecasOracleTests`). El generador de lotes en pruebas se niega fuera del esquema aislado.
108. **¿Dónde está el informe?** En `docs/pruebas/INFORME_PRUEBAS_CIERRE.md`.

## L. Despliegue y disponibilidad (109–113)

109. **¿El bot funciona sin el PC?** No todavía. Corre en el PC con una tarea que lo relanza cada 5 minutos.
110. **¿Por qué no está en una VM gratuita?** Oracle Bogotá solo ofrece A1 en el plan gratuito y responde «Out of host capacity». El reintento está documentado.
111. **¿Se gastó dinero?** No. El presupuesto es US$0; una VM Windows de pago requiere autorización.
112. **¿Qué depende ya de la nube?** La base de datos (Oracle Autonomous) y Groq. El bot y la visión local dependen del PC.
113. **¿Qué falta para producción?**
   - cuentas reales de operario y administrador;
   - ejecutar T001 (tarifas);
   - canecas verde y negra físicas con su ESP32;
   - alojamiento 24/7;
   - validar Power BI con el lote;
   - Google Forms, si se decide usarlo.
