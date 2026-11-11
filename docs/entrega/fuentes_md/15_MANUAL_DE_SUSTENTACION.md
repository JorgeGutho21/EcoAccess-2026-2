---
archivo: 15_MANUAL_DE_SUSTENTACION
titulo: Manual de sustentación de EcoAccess P3
subtitulo: Guion de la demostración, reparto sugerido, 158 preguntas con respuesta y lo que no se debe afirmar
codigo: 15
---

# Cómo usar este manual

1. Lea primero el **guion de la demostración**.
2. Repase las **158 preguntas**: las 113 del banco de cierre y 45 nuevas (114 a 158) sobre el cierre del 11/11/2026.
3. Memorice la lista de **lo que no se debe afirmar**. Un jurado valora más una respuesta honesta que una cifra inflada.

**Actualizaciones del 11/11/2026 que corrigen el banco anterior.** Donde el banco de las secciones A a L diga otra cosa,
vale lo siguiente:

- T001 **ya se ejecutó**: hay 7 tarifas vigentes, creadas por el administrador 4521.
- Ya existe un administrador real (4521), con roles Administrador y Operario.
- El operario puede consultar el dashboard institucional.
- Google Forms **no** se implementó.

# Guion de la demostración (20 minutos)

| Min. | Paso | Qué se muestra | Quién (sugerido) |
|---|---|---|---|
| 0–2 | Problema y objetivo | la separación en la fuente en Colombia y el código de colores blanco, verde y negro | Jorge Gutiérrez Thomas |
| 2–5 | Arquitectura | diagrama de capas (documento 04) y la solución en Visual Studio | Jorge Gutiérrez Thomas |
| 5–9 | Escritorio | inicio de sesión, menú, usuarios con dos roles, dashboard en modo operativo y en modo experimental | Heyder Canchila |
| 9–13 | Telegram | QR oficial, foto de una botella PET, respuesta «caneca blanca», ventana de 10 s en el simulador | Saray Cadena |
| 13–16 | Oracle | consultas de `09_estadisticas_cierre.sql`: 0 reales, 100 experimentales, 7 tarifas | Heyder Canchila |
| 16–18 | Canecas | estación de tres canecas en Oracle (1093, 1400, 1401) y estado del hardware | Saray Cadena |
| 18–20 | Pruebas y límites | regresión (unitarias e integración) y lo que falta validar | Jorge Gutiérrez Thomas |

El reparto es una **sugerencia** para el ensayo; el equipo decide quién presenta cada parte.

# Lo que NO se debe afirmar

- Que las tapas se abren físicamente desde el software. **No se ha probado**.
- Que el modelo de visión fue entrenado por el equipo. Es un modelo preentrenado; se ajustaron el vocabulario y las reglas.
- Que hay depósitos o kilos reciclados reales. Hay **0 depósitos reales**; los 100 son sintéticos.
- Que el sistema funciona 24/7. Depende del PC del propietario.
- Que Power BI ya tiene un informe P3 terminado, o que hay integración con Google Forms.
- Que la visión acierta siempre. En la ronda independiente 5 acertó el **75,5 %**.

{{incluir:docs/academico/PREGUNTAS_SUSTENTACION_CIERRE.md}}

# M. Cierre del 11/11/2026 (114–158)

114. **¿Cómo se creó el primer administrador en Oracle Cloud?** Con `FormConfiguracionInicial`, que llama a `ServicioAutenticacion.CrearAdministradorInicialAsync`. Solo funciona si no existe ninguna cuenta con rol Administrador. La cuenta creada es la 4521.
115. **¿Qué impide crear un segundo administrador por esa vía?** El método rechaza la operación si existe cualquier administrador, activo o no. Lo comprueba la prueba `CrearAdministradorInicialAsync_SegundoIntento_RechazaYNoCreaOtraCuenta`.
116. **¿Y si un ciudadano escribe su correo en la configuración inicial?** Se rechaza sin elevar la cuenta (`..._CorreoDeCuentaExistente_RechazaSinElevarla`).
117. **¿Por qué no se insertó el administrador con SQL?** Porque escribir la contraseña en un script la dejaría en texto plano. El servicio guarda solo el hash PBKDF2.
118. **¿Puede una cuenta tener dos roles?** Sí. `USUARIO_ROL` es una relación muchos a muchos. La cuenta 4521 tiene Operario (2) y Administrador (3).
119. **¿Cómo se asignó el rol Operario a la cuenta 4521?** Con `ServicioUsuarios.AsignarRolAsync`, con la cuenta 4521 como actor administrador. El servicio valida el permiso y Oracle guarda la fila en `USUARIO_ROL`.
120. **¿Hay cuentas para Heyder y Saray?** No en Oracle Cloud, el 11/11/2026. Faltan sus correos reales, y no se inventaron direcciones. El administrador las crea en la pestaña «Crear cuenta».
121. **¿Qué son ADM-0001 y OP-0001?** Códigos visuales del equipo. No son claves primarias ni contraseñas.
122. **¿Dónde están las contraseñas iniciales del equipo?** En un TXT local, fuera del repositorio y de OneDrive, con permisos NTFS solo para el propietario. Nunca en Git ni en Telegram.
123. **¿Qué cambió en los permisos del operario?** Ahora puede consultar el informe institucional (tablero y modo experimental), que es de solo lectura. Sigue sin poder modificar usuarios, materiales ni reglas.
124. **¿Cómo se demuestra ese cambio?** `MatrizPermisosTests` llama al servicio real con los tres roles: el ciudadano es rechazado y el operario y el administrador son aceptados.
125. **¿Ocultar un botón es seguridad?** No. El servicio también exige el rol. El menú solo evita confusiones.
126. **¿Qué es T001?** Un script operativo, no una migración, que crea las tarifas de ecopuntos con un administrador real como creador.
127. **¿Cómo se probó T001 sin riesgo?** Primero en el esquema aislado, dentro de una transacción que terminó con `ROLLBACK`: 11 reglas antes y 11 después. Luego se ejecutó en la nube.
128. **¿Qué tarifas quedaron?** Lata 5, PET 4, cartón 3, metal 3, vidrio 3, multicapa 2 y papel 2 puntos por unidad verificada.
129. **¿Por qué hay reglas y depósitos verificados pero 0 ecopuntos?** Acreditar es una acción explícita de un operario sobre depósitos reales. Los 100 depósitos son sintéticos, y nadie debe ganar puntos por datos inventados.
130. **¿Qué impide acreditar puntos por una cantidad solo declarada?** El disparador `tr_ecop_unidades` (ORA-20113). Exige `cantidad_verificada` y que los puntos sean exactamente tarifa × cantidad.
131. **¿Qué es el lote `SIM-2026-11-CIERRE`?** 50 ciudadanos ficticios y 100 depósitos sintéticos, registrados en `REGISTRO_SIMULACION` (V025) para poder separarlos de los datos reales.
132. **¿Cómo se separan datos reales y experimentales en SQL?** Con `LEFT JOIN registro_simulacion`. Si no hay fila, el dato es real. Un `JOIN` normal haría desaparecer los reales.
133. **¿Cuántos kilos hay?** 14,476 kg **estimados** (catálogo × unidades) y 0 kg **medidos**, porque no hay báscula. Nunca se presenta un estimado como medido.
134. **¿Por qué el tablero tardaba 16,7 s?** Por la primera conexión mTLS a Oracle Cloud (unos 5 s) y otras cargas del equipo. Las consultas del tablero tardan entre 0,23 y 0,96 s, así que no hacían falta índices.
135. **¿Qué mide la ronda visual 5?** 102 fotos públicas de Wikimedia Commons, etiquetadas antes de ejecutar el modelo, con clases difíciles de Colombia (icopor, multicapa, metalizados).
136. **¿Cuál fue el resultado?** 77 de 102 correctas (75,5 %), 2 falsas admisiones, 10 ofertas indebidas, 0 fallos técnicos y ninguna oferta para personas o animales.
137. **¿Por qué no se corrigieron los errores de la ronda 5?** Para no contaminar la evaluación. Corregir mirando esas fotos haría que dejaran de ser una prueba independiente.
138. **¿Cuál es el punto débil de la visión?** El icopor, que se lee como plástico rígido, y la multicapa, que se describe por su uso y no por su material.
139. **¿Qué pasa si el bot se cae?** La tarea programada «EcoAccess P3 - vigilante del bot» lo relanza en máximo 5 minutos. Se corrigió que tomara como propio el bot de otra copia del repositorio.
140. **¿Eso es disponibilidad 24/7?** No. Si el PC se apaga, el bot se detiene. La VM gratuita de Oracle está bloqueada por capacidad y por el requisito de Windows.
141. **¿Qué identificadores tienen las canecas?** 1093 blanca (en servicio), 1400 verde y 1401 negra (fuera de servicio hasta validarlas). Las tres comparten el QR oficial.
142. **¿Por qué la verde y la negra están fuera de servicio si ya están construidas?** Porque ningún software las ha probado físicamente. Mientras tanto, Eco orienta hacia ellas pero no emite órdenes de apertura.
143. **¿Qué protege una orden de apertura?** La firma HMAC con una clave propia de cada ESP32, la idempotencia y el vencimiento. Un ESP32 rechaza las órdenes de otra caneca.
144. **¿Qué es la ventana de 10 segundos?** El tiempo que la tapa permanece habilitada tras una orden. Si no se detecta el depósito, la orden vence y no se registra nada como detectado.
145. **¿Cuántas pruebas hay?** Unas 1 400 funciones de prueba en 166 clases, que con los casos parametrizados son 1 769 ejecuciones unitarias más la integración contra Oracle (documento 13).
146. **¿Una prueba omitida cuenta como aprobada?** No. Se informa aparte, con su motivo (hardware, credenciales o permisos de Windows).
147. **¿Qué es el esquema aislado?** `ECOACCESS_P3_PRUEBAS`, en un Oracle FREE local. Allí corren las pruebas destructivas sin tocar la nube.
148. **¿Qué es una migración idempotente?** Una que se puede ejecutar dos veces sin error ni cambios adicionales. Cada una se prueba así antes de llevarla a la nube.
149. **¿Por qué las migraciones aplicadas no se editan?** Porque su huella SHA-256 queda en `ECO_MIGRACION_APLICADA`. Un cambio se hace con una migración nueva.
150. **¿Dónde está la documentación completa?** En `docs/entrega/academica`: el documento maestro y 16 documentos especializados en Word y PDF, generados desde el código y Oracle.
151. **¿Cómo se generaron las tablas de clases y formularios?** Con Roslyn, que lee el código C#, y con el diccionario de Oracle. Nada de eso se escribió a mano (`tests/herramientas/EcoAccess.P3.Inventario`).
152. **¿Cómo se abre el proyecto el día de la sustentación?** Con los accesos del Escritorio «Iniciar EcoAccess P3» y «Abrir EcoAccess P3 en Visual Studio». No guardan contraseñas.
153. **¿Qué proyecto es el de inicio en Visual Studio?** `EcoAccess.P3.Presentacion`.
154. **¿Qué aporta la rama de auditoría?** La guía ESP32 de tres canecas, las diez consultas de estadísticas cruzadas con Power BI y la corrección de una prueba de visión. Se integró por avance rápido.
155. **¿Qué hace `EcoAccess.P3.Bot.exe --evaluar-objetos`?** Mide el reconocimiento con fotos etiquetadas, sin crear solicitudes, depósitos ni consultas en Oracle.
156. **¿Qué datos personales ve Power BI?** Ninguno. La cuenta de informes lee solo vistas agregadas, sin credenciales ni correos.
157. **¿Qué falta para declarar el sistema completo?**
    - una foto real desde un teléfono por Telegram;
    - la apertura física de las tres tapas;
    - el informe `.pbix` de P3;
    - las cuentas de Heyder y Saray con sus correos;
    - un alojamiento sin el PC.
158. **¿Por qué el informe final no dice «100 % funcional»?** Porque no todos los criterios tienen evidencia de aprobación. Se usan los estados VERIFICADO REAL, VERIFICADO EN SIMULACIÓN, IMPLEMENTADO SIN VALIDACIÓN EXTERNA, BLOQUEADO y PENDIENTE.
