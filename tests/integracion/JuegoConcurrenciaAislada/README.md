# Concurrencia y seguridad del juego en el esquema aislado

Arnés .NET 10 que llama directamente a `PKG_ECO_JUEGO` en el esquema **aislado** `ECOACCESS_P3_PRUEBAS` del Oracle
Database Free local (User Secrets `ecoaccess-p3-pruebas`). Rechaza cualquier otro destino: nunca toca producción.

- 20 comprobaciones de seguridad y reglas (ronda de 7): registro idempotente, alias duplicado, aviso obligatorio, entradas
  con inyección/HTML, JSON inválido, sesión inválida o ajena, orden de preguntas, opción de otra pregunta, respuesta
  inmutable, revisión bloqueada, solo SHA-256 de la sesión, panel sin clave, concurso finalizado.
- **La ronda de 7 no cambió con V035**: compila en el esquema aislado una copia del paquete de V032 (`PKG_ECO_JUEGO_V032`,
  tomada de `reversion/R035…`), juega intentos alternando ambos paquetes y compara sus respuestas JSON/CSV (registro,
  inicio, 7 respuestas, reintentos cruzados, errores, cierre por fin de concurso, clasificación, revisión, panel y CSV).
  La copia se borra al terminar.
- **Ronda de 10** (banco `EXPO2026-10`): 10 preguntas y 1000 puntos, orden y temas, errores 400/409, inmutabilidad,
  cierre parcial (10 − aciertos), revisión con 10 explicaciones, clasificación, disparadores que impiden cambiar el banco de
  una competencia abierta o un banco en uso, CHECK de 1000 puntos y «crear» del panel con banco.
- Partidas completas de 10, 25, 50 y 100 jugadores simultáneos (barrera común) **en cada ronda**, con reintentos duplicados
  de red: latencias por operación (media, mediana, P95, P99), duplicados, coherencia del puntaje y orden del top 5.
- En el esquema aislado marca como correcta la opción de menor id de cada pregunta (la clave real es privada), reinicia los
  contadores de límite por origen entre fases y, si no hay clave de administración, usa una temporal que borra al final.

Requiere V031, el paquete de V032 (sin el bloque ORDS: no hay ORDS local), V034 y V035 aplicados en el esquema aislado.

```powershell
dotnet run --project tests/integracion/JuegoConcurrenciaAislada -- 10,25,50,100
```
Si la ruta del repositorio es muy larga (más de 260 caracteres en `obj\…`), copia `Program.cs` y el `.csproj` a una carpeta
corta, compílalos allí y ejecuta el `.dll` desde la raíz del repositorio (el arnés busca `EcoAccess.P3.sln` hacia arriba).
