# Informe final de cierre — EcoAccess P3 (10/11/2026)

Universidad Popular del Cesar · Programación de Computadores III · Jorge Gutierrez Thomas y Heyder Canchila.

## Estados

- **REAL** = verificado en entorno real.
- **SIMULACIÓN** = verificado mediante simulación.
- **SIN VALIDAR** = implementado sin validación.
- **PENDIENTE** = pendiente o bloqueado.

Cada porcentaje indica su denominador. Ningún componente se declara «100 % funcional».

Commits de esta misión sobre la rama del PR #11 (`desarrollo/auditoria-tercer-corte`, publicada como `desarrollo/telegram-doble-ia`):

| Commit | Contenido |
|---|---|
| `d0742f3` | Registro de consultas de visión (migración V023) |
| `aa1cfa0` | Recorrido completo contra Oracle Cloud |
| `73d8b99` | Tapabocas y papel higiénico |
| `0d647d3` | Tercera ronda de visión |
| (este cierre) | Diseño de Windows Forms, arnés de capturas, guía de hardware y este informe |

## 1. ¿Qué problemas encontraste?

1. **Consultas de visión sin rastro en Oracle.** En modo software una foto atendida por Echo no dejaba ningún registro en Oracle. Las estadísticas personales no podían contarla sin confundirla con un depósito.
2. **Formularios cortados al 100 %.** Los formularios del Designer, diseñados al 125 %, cortaban texto al 100 % en 1366×768. Pasaba en «Poner en servicio», «Registrar», «Consultar» y en la fila de filtros de Depósitos.
3. **Tabla de cuentas casi invisible.** En 1366×768 la tabla de cuentas mostraba fila y media.
4. **Textos desactualizados.** La bienvenida y el modo invitado hablaban de la «caneca DEMO», pero el flujo invitado usa la caneca piloto real 1093.
5. **Clave faltante para tapabocas.** El catálogo no tenía clave para tapabocas, y «Tissue paper» no llegaba a pañuelo usado.
6. **Fallos de la tercera ronda de visión (independiente)**:
   - una foto de personas se ofreció como «metal»;
   - ventiladores y aires acondicionados casi nunca se reconocen;
   - una caja de condones se leyó como cartón.
7. **La migración V023 necesitaba autorización.** El primer intento de aplicarla a Oracle Cloud compartido lo bloqueó la política del entorno, y no se eludió.
   - Después se revisó, se ensayó en un esquema aislado y se aplicó el 10/11/2026 con la autorización expresa del propietario (ver §7).
8. **Prueba de integración sin su dato previo.** `FotoVisionReal_VinculaContratoSeguro_…` falla en la nube porque no existe la tarifa ficticia de PAPEL que espera.
   - En el esquema aislado, con su fixture, pasa.
   - Producción no tiene **ninguna** tarifa de ecopuntos: es una decisión de negocio pendiente (ver §18).
9. **Pruebas del Bridge intermitentes contra el Oracle local.** Fallaban de 0 a 4 de 18, siempre con «La solicitud venció».
   - Cada una pasaba sola, y el fallo se repetía sin V023.
   - **Causa hallada:** el reloj de .NET Framework avanza a saltos de hasta 15,6 ms (medido). Oracle redondea al microsegundo al guardar. Una solicitud recién creada podía volver un tick «en el futuro» respecto del mismo «ahora», y la regla de vigencia la rechazaba.
10. **Tarjetas del dashboard con el título cortado.** Pasaba con títulos de dos líneas («Depósitos registrados», «Ciudadanos únicos»).
11. **Una foto de un ciudadano fallaba al instante** si otra inferencia local (una evaluación o una prueba) tenía el modelo, aunque cada inferencia dura 1–4 s.
12. **Personas en uniforme, ventiladores y aires acondicionados se ofrecían como «metal».** El modelo los nombraba bien, pero el intérprete no conocía esas palabras.

## 2. ¿Cuáles corregiste?

| Problema | Corrección | Estado |
|---|---|---|
| 1 | Tabla `consulta_vision` y vista agregada sin datos personales (`database/06_migraciones/V023__consulta_vision.sql`). Repositorio, registro best-effort con clave idempotente `telegram:<update_id>` y línea aparte en «Mi actividad». El bot solo lo activa si la tabla existe. | REAL: V023 aplicada en la nube; el bot reiniciado la usa; el recorrido completo deja `ConsultaVisionPersistida: true` |
| 2 | `DisenoFormularios.AjustarDisenoFijo`: botones y etiquetas crecen a su texto, las filas siguientes bajan y las filas de filtros crecen. Se aplica al abrir cada módulo diseñado con escala por fuente. | SIMULACIÓN (arnés fuera de pantalla con formularios, servicios y Oracle reales) |
| 3 | El módulo de cuentas se desplaza en pantallas bajas y la tabla conserva un alto útil | SIMULACIÓN (mismo arnés) |
| 4 | Textos corregidos a «caneca piloto» | SIMULACIÓN (captura) |
| 5 | Clave `face_mask` (residuo sanitario) y «tissue paper» → pañuelo usado (`73d8b99`) | Pruebas unitarias |
| 6 | Ver el problema 12 | — |
| 7 | Revisión, ensayo aislado y aplicación con autorización (§7) | REAL |
| 9 | La vigencia tolera exactamente la precisión de almacenamiento, 1 µs (`9ef6159`). Un primer intento truncaba todas las fechas al guardar y rompió los límites de las consultas (`Resumen_HastaExacto_QuedaFueraDelPeriodo`); se revirtió (`ea1c460`). | REAL: 3 corridas seguidas del Bridge en el esquema aislado sin fallos (17/17), y suite completa (§13) |
| 10 | La cabecera de cada tarjeta crece con su título | SIMULACIÓN (captura `15_…`) |
| 11 | El bot espera su turno hasta 30 s, con prioridad; las evaluaciones le ceden el modelo (`aa2a654`) | Pruebas unitarias (3 nuevas); la evaluación ya no choca con otra |
| 12 | Palabras nuevas en el intérprete (`6e05b1a`), medidas en la **cuarta ronda independiente**: aparatos 43/47, personas 31/37 | REAL (fotos públicas): ver §10 |

## 3. ¿Qué partes de EcoAccess se ejecutaron realmente?

- **Oracle Cloud** (`ecoaccess_tp`, esquema `ECOACCESS_P3_APP`): todas las consultas de esta sesión. **REAL**.
- **Bot P3** (`--polling`): proceso activo desde las 10:29 del 10/11/2026. Sobrevivió a una suspensión del PC de 11:34 a 12:22. **REAL**.
- **Modelo visual local** `lfm3-vl` en Ollama: 131 fotos de la tercera ronda y la foto del recorrido completo. **REAL**, con fotos públicas, no del teléfono.
- **Windows Forms**: inicio, modo invitado y 12 módulos administrativos con servicios y Oracle reales, dibujados fuera de pantalla. **SIMULACIÓN** de la interacción (clic por código); los datos y los formularios son los reales.
- **Tarea programada «EcoAccess P3 - vigilante del bot»**: en estado *Ready*, último resultado 0. **REAL**.

## 4. ¿Cómo quedó Windows Forms?

- Se capturaron 14 pantallas por resolución (1366×768 y 1920×1080), al 100 % de escala, tras las correcciones.
  - Se publican 23 en `docs/evidencias/winforms/`, con su informe por resolución.
- No hubo errores de interfaz.
- El administrador ficticio de cada pasada se creó y se borró: 3159, 3160, 3163 y 3194. Antes se borraron 3157 y 3158, que quedaron de pasadas interrumpidas.
- **Lo que se corrigió y se confirmó en captura**:
  - En Depósitos, «Consultar» ya no queda escondido tras una barra de desplazamiento.
  - La paginación queda en una línea.
  - La tabla de cuentas muestra 6 filas en 1366×768 en lugar de fila y media.
- **Escala**
  - 125 %: es la del monitor de este PC, donde se usó la aplicación en sesiones anteriores.
  - **150 %: PENDIENTE**. Exige cambiar la escala de Windows del propietario. No se cambió una configuración del sistema.
- **Detalle visual sin confirmar en pantalla real**: en el modo invitado, el título «¿Sabías que puedes acumular ecopuntos?» aparece recortado en la captura. El arnés comprobó que la etiqueta tiene el tamaño que su texto necesita, así que puede ser un efecto de `DrawToBitmap`.
- **Tiempos**: el arnés espera un tiempo fijo por módulo. Sus cifras **no** miden la carga real y no se reportan como rendimiento.

## 5. ¿Cómo quedó Telegram?

- **Bot activo**, único consumidor (`--polling`). La tarea programada lo relanza si se detiene.
- **Saludo**: «🌱 ¡Hola, soy Eco! Envíame una foto de tu residuo. 📸».
  - La misión pide «Hola, soy Echo».
  - La mascota se llama «Eco» en toda la aplicación y en sus imágenes, así que no se renombró sin decisión del equipo.
- **Destino de una foto**: «Lata de aluminio.\nCaneca blanca.\nModo demostración: apertura física pendiente.» (recorrido completo). **SIMULACIÓN** (actualizaciones inyectadas, Oracle y modelo reales).
- **Conversación desde un teléfono real**: **PENDIENTE**.

## 6. ¿Cómo quedó la vinculación?

- La prueba `CiudadanoNuevo_QrOficial_Foto_Destino_Historial_Estadisticas_Y_Reinicio` recorre el flujo completo contra Oracle Cloud:
  - crea un ciudadano;
  - lee el QR oficial desde el PNG (ZXing), sin regenerarlo;
  - lo vincula, clasifica una foto real y consulta historial y estadísticas;
  - reinicia el servicio y comprueba que no hay duplicados;
  - deja los conteos iguales antes y después.
- Estado: **SIMULACIÓN** (sin teléfono).
- Tiempos: registro 6,7 s, QR 0,5 s, foto 4,2 s.
- Evidencia: `docs/pruebas/resultados_e2e/recorrido_completo.json`.

## 7. ¿Cómo quedó Oracle?

- Es la fuente única de información: escritorio, bot, registro y Power BI leen el mismo esquema. **REAL**.
- Los 19 usuarios ficticios y los 600 registros simulados se **conservaron**, como se pidió.
- **V023 aplicada (REAL, 10/11/2026)**:
  - Cada foto atendida por Echo queda en `CONSULTA_VISION`, separada de los depósitos.
  - **Huella antes y después:** las 13 tablas protegidas quedaron sin cambios en conteo ni en SCN. Solo se sumaron la tabla, la vista, 4 índices, 1 secuencia y 19 restricciones; 0 objetos inválidos.
  - Detalle y reversión: `docs/base_datos/V023_CONSULTAS_VISION.md`.

## 8. ¿Qué dashboards quedaron funcionando?

- **Windows Forms**: dashboard general, «Dashboard institucional» y «Estadísticas», contra Oracle real.
  - Muestran 0 depósitos, que es el estado real.
  - Estado: **REAL** en sesiones anteriores (`docs/evidencias/ejecucion_real/06…08`) y SIMULACIÓN de navegación en esta.
- **Fotos consultadas a Echo**: fila nueva del dashboard institucional, separada de los depósitos, con total, con destino, «pidió otra foto» y tiempo medio.
  - Estado: **REAL** contra Oracle, comprobado en el esquema aislado con 3 consultas de muestra (`docs/evidencias/winforms/15_…`).
  - En la nube hoy muestra 0, porque las consultas de prueba se borraron.
- **Power BI**: tabla `ConsultasVision` y cuatro medidas «Echo» en el repositorio V2. **SIN VALIDAR** en Power BI Desktop.

## 9. ¿Qué páginas de Power BI verificaste?

- Se verificaron dos páginas con datos reales de Oracle. El propietario escribió la credencial. **REAL**.
  - «Residuos y tendencias»
  - «Estaciones y canecas»
- Corrección del calendario cuando no hay depósitos: V2 `37f0719`. Capturas en V2 `docs/evidencias/powerbi/`.
- No se publicó ningún enlace público.

## 10. ¿Cuánto mejoró la precisión visual?

Todas son fotos públicas. No hay fotos de teléfono.

| Conjunto | Antes | Después | Estado |
|---|---|---|---|
| Colombia, 195 fotos: objeto correcto | 134/195 | 149/195 | REAL |
| Colombia, 195 fotos: ofertas indebidas | 4 | 2 | REAL |
| TrashNet final, contra la piloto 1093 | — | 72/100 correctas, 0 aperturas con material equivocado | REAL |
| Tercera ronda (131 fotos) | 93/131 (71 %), 6 ofertas indebidas | 106/131, 3 ofertas indebidas (ya no independiente: el ajuste miró sus fallos) | REAL |
| **Cuarta ronda independiente (141 fotos nuevas, etiquetadas antes de medir)** | — | **119/141 (84 %)**, 0 aperturas indebidas, 4 ofertas indebidas | REAL |
| Colombia 195, control de retrocesos tras la corrección | 149/195 | 150/195, contexto 156→161, mismas aperturas y ofertas | REAL |

El modelo no se reentrenó. Mejoraron el intérprete de nombres y el catálogo. Detalle: `docs/vision/EVALUACION_RONDA4_INDEPENDIENTE.md`.

## 11. ¿Qué categorías siguen fallando?

Datos de la cuarta ronda (independiente) y de la tercera:

| Categoría | Correctas |
|---|---|
| Metal real (bandejas de aluminio con comida, chatarra) | 3/8: se lee como comida o restos (rechazo de más, no apertura) |
| Cartón aplanado o cargado por una persona | 0/2 |
| Tubos de crema dental (ronda 3) | 2/7 |
| Condones (ronda 3) | 3/6 |
| Botellas plásticas de champú confundidas con vidrio (ronda 3) | 4/8 |

- Cuatro ofertas indebidas en la ronda 4: un dibujo de un soldado con armadura, una bobina de circuito, un portátil con un reloj y un niño entre latas. En los cuatro casos Echo ofrece el botón; nunca abre solo.
- Correctos o casi: perros, gatos, muebles, zapatos, audífonos, cargadores, celulares (7/7), aparatos (43/47) y personas (31/37).

## 12. ¿Qué rendimiento se midió?

| Medida | Valor | Estado |
|---|---|---|
| Visión local por foto (131 fotos, tercera ronda) | Mediana 1,0 s; p95 2,9 s | REAL |
| Recorrido completo: registro | 6,7 s | SIMULACIÓN, Oracle real |
| Recorrido completo: QR | 0,5 s | SIMULACIÓN, Oracle real |
| Recorrido completo: foto | 4,2 s | SIMULACIÓN, Oracle real |
| Recuperación del bot por la tarea programada | 11 s | REAL, sesión anterior |
| Carga de 30 usuarios | 270 recorridos sin errores | REAL, sesión anterior (`docs/pruebas/CARGA_30_USUARIOS.md`) |

## 13. ¿Qué pruebas reales ejecutaste?

Regresión final del 10/11/2026, con todo el código de este cierre:

- **Pruebas unitarias:** 1702/1703 correctas, 1 omitida a propósito, 0 fallidas.
- **Integración en Oracle Cloud** (visión integrada activada): **211 correctas, 1 fallida, 3 omitidas, de 215** (15 min).
  - El recorrido completo de un ciudadano nuevo y la prueba de V023 pasaron.
  - La única fallida es `FotoVisionReal_VinculaContratoSeguro_…`. Exige una tarifa ficticia de ecopuntos para PAPEL, que la nube no tiene.
  - No se crea una tarifa ficticia en producción. En el esquema aislado, con su fixture, la prueba pasa.
- **Integración en el esquema aislado** (Oracle FREE local): **210 correctas, 1 fallida, 4 omitidas, de 215**.
  - La fallida fue `RecorridoCanecaSinRespuesta_…`, que tardó 58 minutos porque el PC se suspendió en medio (Kernel-Power, reanudación a las 20:13).
  - Repetida sola, pasó 3 de 3 en 6 s cada vez.
  - Las pruebas del Bridge, que antes fallaban de forma intermitente, pasaron 3 corridas seguidas sin fallos.
- **Evaluación visual:** 131 fotos (ronda 3) y 141 fotos nuevas (ronda 4) con el modelo real, más la reevaluación de control de las 195 de Colombia.
- Revisión de la tarea programada y de los eventos de suspensión del sistema.

## 14. ¿Qué pruebas simulaste?

- Mensajes de Telegram: actualizaciones inyectadas en el servicio de conversación, con Oracle y modelo reales. No se enviaron mensajes desde una cuenta real.
- Navegación de Windows Forms: clic por código y dibujo fuera de pantalla.
- Hardware: ninguna orden. El modo software no emite órdenes y no se usó el simulador del dispositivo en esta misión.

## 15. ¿Qué capturas generaste?

- `docs/evidencias/winforms/`: pantallas en 1366×768 y 1920×1080, después de las correcciones.
  - Se excluyen las de «Ciudadanos y usuarios», porque muestran correos de cuentas de prueba.
  - Las capturas «antes» quedaron como referencia local y no se publican por el mismo motivo.
- Las capturas de Power BI de la sesión anterior están en el repositorio V2.

## 16. ¿Qué cambios hiciste al PR?

- Commits incrementales en español sobre la rama del PR #11. Se publican con avance rápido, sin force-push.
- No se abrió otro PR, no se fusionó y el PR sigue en borrador.
- Commits nuevos en el PR:
  - `d0742f3`, `aa1cfa0`, `73d8b99`, `0d647d3`;
  - `a4a10a4`: diseño de escritorio;
  - `a2abf3f`: arnés de capturas;
  - el commit de documentación de este cierre;
  - V023 y su ensayo: `aa1a039`, `6b9c415`, `36e5e2e`, `57bceb8`, `1042680`, `609f237`;
  - vigencia de solicitudes: `9ef6159` (tras revertir el intento global, `ea1c460`);
  - intérprete de personas y aparatos: `6e05b1a`;
  - turno del modelo con prioridad para el bot: `aa2a654`;
  - cuarta ronda: `db28bd6`.
- Regresión final: ver §13.
- **El bot en producción se reinició con el código final** (sesión `20261110-203302`): un solo proceso `--polling`, que registra las consultas en Oracle.

## 17. ¿Qué falta para la prueba física del ESP32?

- Ver `docs/hardware/GUIA_FINAL_HARDWARE.md`:
  - placa conectada con su puerto COM;
  - medición de la fuente;
  - calibración sin tapa;
  - primera orden firmada sin tapa;
  - ciclo con tapa de 10 s;
  - pruebas de orden repetida o vencida y de reinicio a mitad de ciclo;
  - eventos en Oracle;
  - foto real por Telegram.
- Ningún motor se movió. **PENDIENTE**.

## 18. ¿Qué intervención humana sigue siendo indispensable?

1. **Definir las tarifas reales de ecopuntos por material**, con el docente o el equipo.
   - Producción no tiene ninguna; sin ellas, un depósito verificado no acredita puntos.
   - No se crearon tarifas ficticias en la nube.
   - (V023 ya quedó aplicada con autorización; ver §7.)
2. **Abrir el informe en Power BI Desktop**, actualizarlo y revisar la tabla `ConsultasVision`.
3. **Prueba con el iPhone y datos móviles**: escanear el QR, vincular, enviar una foto y revisar «Mi actividad».
4. **Decidir el nombre de la mascota** en el saludo: «Eco», actual y en todas las imágenes, o «Echo», como en la misión.
5. **Comprobar la escala de 150 %** en un equipo con esa escala, o autorizar el cambio temporal en este PC.
6. **Hardware**: la secuencia de §17, con la placa en la mano.
7. **Disponibilidad**:
   - Evitar que el PC se suspenda.
   - Decidir el alojamiento permanente (`docs/operacion/ALOJAMIENTO_PERMANENTE.md`).
   - Hoy todo depende de que este computador esté encendido.
8. **Fotos propias para la siguiente medición visual:** las cuatro rondas son de Wikimedia Commons. La prueba con fotos del teléfono del equipo sigue pendiente.
