# Registro, vinculación y página HTTPS

**Estado: la página responde por HTTPS público y el recorrido completo aprobó en pruebas automáticas con Oracle. Falta hacerlo una vez desde un teléfono real.**

## Diagnóstico del error

Eco respondía «El registro en línea no está disponible». La evidencia del 3 de noviembre de 2026:

| Comprobación | Resultado |
|---|---|
| `tailscale funnel status` | Funnel activo: `/registro` hacia `http://127.0.0.1:8081/registro` |
| Puerto 8081 | Sin servidor escuchando; conexión rechazada |
| HTTPS público | HTTP 502 |
| Configuración guardada | Correcta, escrita a las 18:19:42 UTC |
| Arranque del bot | 17:54:49 UTC, 25 minutos **antes** |

**Causa:** el bot lee `Telegram:PaginaRegistro` al arrancar. Quedó corriendo con la configuración anterior y por eso no abrió la página. `Iniciar-Bot-Eco.ps1` tampoco podía aplicarla: solo decía que ya había servicios en marcha. No había un fallo en Oracle ni en Tailscale.

Al probar con varias personas apareció un **segundo defecto**: con 10 registros a la vez, la página respondía 503. Cada alta calcula PBKDF2 dos veces con 600 000 iteraciones, y en .NET Framework esa función es código administrado. Las peticiones agotaban el plazo de 30 s.

## Correcciones

- **Reinicio controlado:** `Iniciar-Bot-Eco.ps1 -Reiniciar` detiene solo los procesos de la sesión registrada, espera a que terminen y arranca de nuevo, sin tocar Docker ni Oracle. Al terminar comprueba el puerto 8081 y la URL pública. Si el bot usa una configuración más vieja que la guardada, el mensaje lo dice.
- **PBKDF2 nativo de Windows:** mismo algoritmo, iteraciones y resultado, verificado con un vector publicado y contra la versión anterior. Las contraseñas ya guardadas siguen valiendo. No se debilitó nada.
- **Capacidad:** 64 peticiones simultáneas en la página (antes 16), plazo total de 60 s, 32 conversaciones en paralelo en el bot (antes 8) y 256 carriles por persona (antes 64).
- **Experiencia:** cada botón abre su formulario y, al terminar, un enlace `Volver a Eco y confirmar` completa la vinculación sin copiar el código a mano. `/confirmar CODIGO` sigue disponible.

## Recorrido

1. `/vincular` ofrece crear cuenta, usar una existente o seguir como invitado.
2. El botón entrega un enlace de un solo uso, que vence en 10 minutos.
3. En la página la persona crea su cuenta o inicia sesión. Nunca se piden contraseñas en el chat.
4. El enlace de vuelta abre el bot con el código. Solo surte efecto en la cuenta de Telegram que pidió el enlace.
5. Oracle guarda el vínculo con el identificador numérico de Telegram, no con el nombre visible.

Si la cuenta ya está vinculada, `/vincular` lo dice y `/start` saluda con «¡Hola de nuevo!».

**Cuenta vinculada a otro Telegram:** se rechaza. No existe aún un flujo de transferencia: para cambiar de Telegram, el dueño usa `/desvincular` desde el anterior. Si lo perdió, hace falta recuperación con ayuda del personal, y ese proceso no está implementado.

## Publicación

En PowerShell como administrador, con Tailscale iniciado:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\instalacion\Publicar-Registro-HTTPS.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\desarrollo\Iniciar-Bot-Eco.ps1 -Reiniciar
```

Para retirarla: `tailscale funnel reset`.

**Inicia el bot desde tu propia ventana de PowerShell.** Lo iniciado desde el panel de terminal de otra aplicación puede depender de esa aplicación, y si Docker se detiene el bot no puede continuar. El equipo debe seguir encendido: esto no es disponibilidad permanente.

## Qué queda expuesto

Solo `https://desktop-c99tip3.tailec56db.ts.net/registro/`. Oracle, Ollama y el bridge no se publican.

| Comprobación real | Resultado |
|---|---|
| HTTPS público sin token | HTTP 400 con certificado válido; la página está viva y exige un enlace |
| Petición directa por la IP de la red local | HTTP 403 |
| Encabezados | CSP, `X-Frame-Options: DENY` |

Además, ya existían: token de un solo uso guardado como hash, CSRF por formulario con cookie `__Host-`, límites por token y global, bloqueo tras intentos fallidos, y restricciones de unicidad en Oracle.

## Pruebas de concurrencia

`CargaUsuariosOracleTests`: chat, página HTTP local y Oracle reales, con los límites de producción, en el esquema `ECOACCESS_P3_PRUEBAS` de la instancia existente. **Telegram se reemplaza por llamadas directas y no pasa por el túnel público.** Las fotos no forman parte de esta carga.

| Escenario | Total | Crear o iniciar sesión: media / P95 / máx. | Chat: media / P95 / máx. | CPU del proceso | RAM pico |
|---|---|---|---|---|---|
| 10 cuentas nuevas | 12,3 s | 5,7 / 6,5 / 6,5 s | 1,0 / 3,9 / 3,9 s | 43,8 s | 114 MB |
| 20 cuentas nuevas | 14,6 s | 9,3 / 10,3 / 10,4 s | 0,4 / 2,1 / 9,8 s | 51,7 s | 124 MB |
| 10 cuentas existentes | 3,0 s | 2,6 / 2,8 / 2,8 s | 0,1 / 0,4 / 0,5 s | 13,2 s | 125 MB |
| 20 cuentas existentes | 6,9 s | 4,9 / 5,5 / 5,5 s | 0,3 / 0,4 / 3,5 s | 26,0 s | 129 MB |

Máximo observado: 24 sesiones Oracle del esquema de pruebas, en un muestreo cada 3 s. Equipo de 8 núcleos, con otros Oracle ajenos en marcha. Los archivos están en `docs/telegram/resultados_carga/`.

Cada escenario verifica que cada respuesta llega a su chat y que cada persona queda en su propia cuenta. También se prueba que:

- el mismo correo enviado por 5 personas a la vez crea una sola cuenta;
- un formulario o enlace usado no vale otra vez;
- el código de otra persona no vincula;
- sin contraseña no hay código;
- una actualización repetida se atiende una sola vez;
- otra instancia nueva de los servicios lee los mismos vínculos desde Oracle.

Eso último simula el reinicio del proceso; **no se reinició Windows**.

```powershell
dotnet build EcoAccess.P3.sln -c Release
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\pruebas\Ejecutar-Integracion-Esquema.ps1 -Filtro 'TestCategory=CargaUsuarios'
```

## Pendiente de comprobación real

| Comprobación | Estado |
|---|---|
| Crear cuenta desde un teléfono con datos móviles | Pendiente |
| Ya tengo cuenta desde un teléfono | Pendiente |
| Vínculo tras reiniciar Windows | Pendiente |
| Varias personas reales en Telegram al mismo tiempo | Pendiente |
| Fotos simultáneas con el modelo real | Sin medir |
