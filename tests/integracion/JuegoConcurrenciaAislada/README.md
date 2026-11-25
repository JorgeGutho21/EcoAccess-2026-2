# Concurrencia y seguridad del juego en el esquema aislado

Arnés .NET 10 que llama directamente a `PKG_ECO_JUEGO` en el esquema **aislado** `ECOACCESS_P3_PRUEBAS` del Oracle
Database Free local (User Secrets `ecoaccess-p3-pruebas`). Rechaza cualquier otro destino: nunca toca producción.

- 20 comprobaciones de seguridad y reglas: registro idempotente, alias duplicado, aviso obligatorio, entradas con
  inyección/HTML, JSON inválido, sesión inválida o ajena, orden de preguntas, opción de otra pregunta, respuesta
  inmutable, revisión bloqueada, solo SHA-256 de la sesión, panel sin clave, concurso finalizado.
- Partidas completas de 10, 25, 50 y 100 jugadores simultáneos (barrera común), con reintentos duplicados de red:
  latencias por operación (media, mediana, P95, P99), duplicados, coherencia del puntaje y orden del top 5.
- En el esquema aislado marca como correcta la opción de menor id de cada pregunta (la clave real es privada).

Requiere V031 y el paquete de V032 aplicados en el esquema aislado (el bloque ORDS no existe en Oracle Free local).

```powershell
dotnet run --project tests/integracion/JuegoConcurrenciaAislada -- 10,25,50,100
```
