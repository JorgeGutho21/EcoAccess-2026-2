# 06. Telegram

Proyecto `EcoAccess.P3.Bot`, bot oficial `@EcoAccessP3_JG_bot`, por `getUpdates` con un solo consumidor.

## Recorrido

Foto → identificación → confirmación → apertura → depósito → agradecimiento.

1. **QR:** `https://t.me/EcoAccessP3_JG_bot?start=<identificador>`. El parámetro identifica la caneca; no contiene credenciales. Con una sola caneca en servicio, también se puede iniciar sin QR.
2. **Saludo:** «🌱 ¡Hola! Soy Eco. Voy a ayudarte a desechar correctamente tu residuo. ¡Envíame una foto!», con **📸 Enviar foto** y **☰ Más opciones**.
3. **Foto:** «🔍 Estoy identificando tu residuo...». Descarga por la API oficial, validación y análisis con el motor configurado. Una imagen enviada como archivo también vale; un PDF, video o audio recibe una explicación.
4. **Identificación:** «♻️ Parece que es [objeto], de material [material]. ¿Es correcto?», con **✅ Sí, continuar** y **✏️ Corregir material**. Identificar **no** abre la caneca.
5. **Confirmación:** se valida el permiso, la admisión y la exclusión por contenedor, y se genera una orden única con una unidad declarada, corregible con `/cantidad`. La cantidad declarada no es la verificada.
6. **Apertura:** al registrarse la evidencia del dispositivo en Oracle, «🔓 ¡Caneca abierta! Tienes 10 segundos para depositarla.».
7. **Final:** «🌱 ¡Gracias por utilizar EcoAccess! Tu operación quedó registrada.», el estado verdadero y «⭐ ¿Cómo fue tu experiencia?» con una a cinco estrellas, opcional.

Con la caneca simulada los mensajes lo dicen expresamente: no se presenta como apertura física ni como depósito real.

Si el modelo se abstiene o tiene certeza baja, no se ofrece abrir: se ofrece otra foto o la selección manual.

## Caneca única

El catálogo conserva todos los materiales para orientar, pero una foto solo lleva a la caneca lo aprovechable (`docs/vision/RECONOCIMIENTO_GENERAL.md`). Lo demás recibe una respuesta propia y no abre.

El QR es permanente e igual en todos los equipos: identificador `EcoAccessCanecaPiloto1`, enlace `https://t.me/EcoAccessP3_JG_bot?start=EcoAccessCanecaPiloto1`, imagen en `assets/identidad/QR_Caneca_piloto.png`.


## Motores de reconocimiento

Un único bot con dos motores intercambiables, elegidos con `Vision:Modo`. Telegram, Oracle y el ESP32 no cambian según el motor. Guía completa: `GUIA_TELEGRAM_DOBLE_IA.md`.

| Modo | Motor | Si falla |
|---|---|---|
| `Local` (predeterminado) | LFM3-VL con Ollama | selección manual |
| `Groq` | Qwen 3.8 en Groq | selección manual |
| `Automatico` | LFM3-VL; Groq solo si el local falla y la persona autorizó | autorización o selección manual |

**Privacidad:** ninguna foto sale del equipo sin autorización. Se guarda por cuenta de Telegram y versión del aviso en `consentimiento_vision_externa`, se consulta en cada foto y se retira con `/revocarfotos`. Una foto ambigua no se envía sola: la persona ve **🔎 Segunda opinión externa**. A Groq solo viaja la imagen, sin EXIF, sin nombre ni cuenta.

## Invitados y cuentas

Sin cuenta se puede hacer el recorrido como invitado, sin ecopuntos ni historial, y recibe los mismos avisos. **Mi cuenta** ofrece Crear cuenta, Ya tengo cuenta y Continuar como invitado. El vínculo está en Oracle y sobrevive a reinicios.

## Menú secundario

Enviar foto, Mis depósitos, Mis ecopuntos, Estadísticas, Educación ambiental, Próximamente, Mi cuenta y Acerca de EcoAccess. No se muestra antes de permitir el depósito.

## Seguridad

No se piden contraseñas en el chat. El token vive en los User Secrets. Una foto repetida no emite otra orden.

## Requisito pendiente

La página de registro necesita una dirección HTTPS pública para teléfonos externos. No se declara funcional desde teléfono mientras no se publique y pruebe.