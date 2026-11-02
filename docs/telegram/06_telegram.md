# 06. Telegram

Proyecto `EcoAccess.P3.Bot`, bot oficial `@EcoAccessP3_JG_bot`, por `getUpdates` con un solo consumidor.

## Recorrido

Foto → identificación → confirmación → apertura → depósito → agradecimiento.

1. **QR:** `https://t.me/EcoAccessP3_JG_bot?start=<identificador>`. El parámetro identifica la caneca; no contiene credenciales. Con una sola caneca en servicio, también se puede iniciar sin QR.
2. **Saludo:** «🌱 ¡Hola! Soy Eco. Voy a ayudarte a desechar correctamente tu residuo. ¡Envíame una foto!», con **📸 Enviar foto** y **☰ Más opciones**.
3. **Foto:** «🔍 Estoy identificando tu residuo...». Descarga por la API oficial, validación y análisis local.
4. **Identificación:** «♻️ Parece que es [objeto], de material [material]. ¿Es correcto?», con **✅ Sí, continuar** y **✏️ Corregir material**. Identificar **no** abre la caneca.
5. **Confirmación:** se valida el permiso, la admisión y la exclusión por contenedor, y se genera una orden única con una unidad declarada, corregible con `/cantidad`. La cantidad declarada no es la verificada.
6. **Apertura:** al registrarse la evidencia del dispositivo en Oracle, «🔓 La caneca está abierta. Tienes 20 segundos para depositar…».
7. **Final:** «🌱 ¡Gracias por utilizar EcoAccess! Tu operación quedó registrada.», el estado verdadero y «⭐ ¿Cómo fue tu experiencia?» con una a cinco estrellas, opcional.

Con la caneca simulada los mensajes lo dicen expresamente: no se presenta como apertura física ni como depósito real.

Si el modelo se abstiene, tiene certeza baja o el material no se admite, no se ofrece abrir: se orienta y se ofrece otra foto o la selección manual.

## Invitados y cuentas

Sin cuenta se puede hacer el recorrido como invitado, sin ecopuntos ni historial, y recibe los mismos avisos. **Mi cuenta** ofrece Crear cuenta, Ya tengo cuenta y Continuar como invitado. El vínculo está en Oracle y sobrevive a reinicios.

## Menú secundario

Enviar foto, Mis depósitos, Mis ecopuntos, Estadísticas, Educación ambiental, Próximamente, Mi cuenta y Acerca de EcoAccess. No se muestra antes de permitir el depósito.

## Seguridad

No se piden contraseñas en el chat. El token vive en los User Secrets. Una foto repetida no emite otra orden.

## Requisito pendiente

La página de registro necesita una dirección HTTPS pública para teléfonos externos. No se declara funcional desde teléfono mientras no se publique y pruebe.