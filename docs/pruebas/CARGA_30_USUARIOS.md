# Registro y vinculación con 30 usuarios — Oracle Cloud

Fecha: 09/11/2026, 20:36–20:41 (COT). Entorno: Windows 11 Pro, .NET Framework 4.8, Oracle Autonomous Database (alias
`ecoaccess_tp`). Datos sintéticos `it.carga.*@example.invalid`, cuentas de Telegram ficticias (900 000 000 000+), borrados
por la limpieza de la prueba; el conteo de las 20 tablas principales fue idéntico antes y después.

Cada recorrido es real de extremo a extremo en el mismo proceso: chat de Eco (`/vincular` → «Crear cuenta»), formulario
HTTP con cookie y CSRF del servidor de registro, alta en Oracle (PBKDF2), confirmación con el código en el bot, consulta
de ecopuntos y `/start`. Después, un servicio nuevo (equivalente a reiniciar) lee el vínculo de Oracle, y una consulta
directa comprueba por cada correo: 1 usuario, 1 rol, 1 credencial, 1 vínculo activo y que el vínculo apunta a esa cuenta.

| Escenario | Personas | Errores | Tiempo total | Crear cuenta / iniciar sesión p95 | Chat p95 (n) |
|---|---|---|---|---|---|
| Nuevas simultáneas | 1 | 0 | 5,5 s | 1,9 s | 2,1 s (5) |
| Nuevas simultáneas | 10 | 0 | 6,1 s | 3,8 s | 0,6 s (50) |
| Nuevas simultáneas | 20 | 0 | 9,2 s | 7,4 s | 0,5 s (100) |
| Nuevas simultáneas | 30 | 0 | 11,3 s | 8,2 s | 1,2 s (150) |
| Nuevas consecutivas | 30 | 0 | 79,1 s | 1,5 s | 0,4 s (150) |
| Existentes simultáneas (iniciar sesión) | 10 | 0 | 2,5 s | 2,0 s | 0,4 s (20) |
| Existentes simultáneas | 20 | 0 | 4,0 s | 3,2 s | 0,6 s (40) |
| Existentes simultáneas | 30 | 0 | 5,3 s | 4,4 s | 0,5 s (60) |

Otras comprobaciones de la misma batería (todas aprobadas): el mismo correo enviado a la vez crea una sola cuenta; un
enlace usado no sirve otra vez y el código solo vale para su cuenta; una cuenta vinculada no pasa a otro Telegram
conociendo el correo; repetir la misma actualización de Telegram no duplica respuestas; QR oficial en modo software.

Lectura: 270 recorridos sin errores (denominador: 1+10+20+30+30 nuevas y 10+20+30 existentes). El tiempo de crear cuenta
crece con la concurrencia porque el hash PBKDF2 es costoso a propósito; con 30 altas a la vez, el p95 fue 8,2 s. No se
probó desde fuera del computador: el servidor de registro corre en el mismo proceso de prueba, no a través de Funnel.
Métricas completas: `resultados_carga_30/*.json`.
