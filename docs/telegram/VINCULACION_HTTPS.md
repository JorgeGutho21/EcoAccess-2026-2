# Vinculación y página HTTPS

**Estado: preparada, sin publicar.** La página no está en internet y ningún teléfono la ha probado. Tailscale está instalado en el equipo, pero sin sesión iniciada; iniciarla corresponde a la persona responsable.

## Causa del error de `/vincular`

`Telegram:PaginaRegistro` nunca se configuró, porque no existe una dirección HTTPS pública. El bot no emite enlaces que no se puedan abrir desde un teléfono, y mostraba ese detalle técnico al ciudadano. Ahora dice que el registro no está disponible y que puede seguir como invitado. La causa real se resuelve publicando la página con los pasos siguientes.

## Recorrido

1. `/vincular` ofrece crear cuenta, usar una existente o seguir como invitado.
2. El botón entrega un enlace de un solo uso, que vence en 10 minutos.
3. En la página la persona crea su cuenta o inicia sesión. Nunca se piden contraseñas en el chat.
4. La página muestra un código, y la persona escribe `/confirmar CODIGO` en el bot: es el segundo canal.
5. El vínculo se guarda en Oracle con el identificador numérico de Telegram, no con el nombre de usuario.

Si ya está vinculada, `/vincular` lo dice y ofrece ecopuntos, actividad y menú. `/start` saluda con «¡Hola de nuevo!». El vínculo vive en Oracle, no en memoria, así que sobrevive a reinicios.

## Publicar la página

En una consola de **administrador**:

1. Abre Tailscale e inicia sesión con tu cuenta.
2. Ejecuta el script. Si muestra un enlace para permitir Funnel, ábrelo, acéptalo y repítelo.

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\instalacion\Publicar-Registro-HTTPS.ps1
```

3. Reinicia el bot y prueba `/vincular` desde el teléfono, **con datos móviles**.

Para retirar la publicación:

```powershell
& "$env:ProgramFiles\Tailscale\tailscale.exe" funnel reset
```

## Qué queda expuesto

Solo `https://<equipo>.ts.net/registro/`. Oracle, Ollama, el bridge y el resto del equipo no se publican.

- `ServidorRegistro` responde 403 a toda conexión que no llegue desde este mismo equipo. El túnel de Tailscale entra por ahí; otro equipo de la red local, no.
- Con el prefijo público `http://+:8081/registro/`, además exige `X-Forwarded-Proto: https`, para no recibir contraseñas por HTTP.
- Tailscale termina el TLS con un certificado válido para su dominio.

La página solo está disponible mientras este computador esté encendido y conectado. Eso no es disponibilidad permanente.

## Controles ya existentes en el código

Token de un solo uso con vencimiento, guardado como hash; CSRF por formulario con cookie `__Host-`; límite de solicitudes global y por token; código de confirmación por el segundo canal; un solo vínculo activo por cuenta y sin duplicar cuentas.

## Comprobaciones pendientes

No marcar como hechas sin evidencia real:

| Comprobación | Estado |
|---|---|
| Certificado válido desde internet | Pendiente |
| Abrir el enlace desde un teléfono externo | Pendiente |
| Crear una cuenta y confirmar el código | Pendiente |
| Vincular una cuenta existente sin duplicarla | Pendiente |
| El vínculo sigue tras reiniciar bot, Oracle y Windows | Pendiente |
| Comportamiento del encabezado `X-Forwarded-Proto` con Funnel | Pendiente |
