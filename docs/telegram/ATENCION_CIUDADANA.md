# Atención ciudadana en el chat

Eco interpreta el texto libre con reglas cerradas (`IntencionesTelegram`). No se usa ningún modelo para conversar y ningún mensaje sale del equipo. Las reglas solo eligen qué responder: no abren canecas ni cambian puntos, permisos o depósitos.

## Orden de interpretación

1. Comandos y botones.
2. Un comentario que Eco esté esperando.
3. Situaciones delicadas: peligro, contenido sexual ajeno al servicio y violencia.
4. Saludos, cancelar, ayuda, menú y correcciones.
5. Insultos, cuenta, ecopuntos y actividad.
6. Quejas, problemas, sugerencias y elogios.
7. Agradecimientos, despedidas y temas casuales o ajenos.
8. Búsqueda en el catálogo, y si no aparece, una respuesta que orienta.

El contexto dura 10 minutos, vive en memoria y se pierde al reiniciar: un comentario pendiente y el último tema consultado. Lo permanente está en Oracle.

## Quejas, sugerencias y comentarios

Se guardan en `COMENTARIO_CIUDADANO` (migración V020) con tipo, categoría, estado, fecha, interacción relacionada y texto.

- Una queja **no crea una valoración** ni asigna estrellas.
- La clave única es la actualización de Telegram, así que un reintento no duplica la fila. Tampoco se repite el mismo comentario durante 10 minutos.
- Máximo 20 comentarios al día por participante; lo que excede no se guarda y la persona igual recibe respuesta.
- Los correos y los números largos se ocultan antes de guardar.
- Un invitado queda solo como un hash, sin datos personales.
- De un insulto se guarda la categoría, no el texto. Las situaciones delicadas no se guardan.
- Si Oracle falla al guardar, la persona igual recibe respuesta y queda constancia en el registro.

Dos estrellas o menos ofrecen `✍️ Escribir comentario` u `Omitir`, sin obligar.

## Moderación

| Caso | Comportamiento |
|---|---|
| Contenido sexual ajeno al servicio | Redirección breve, sin moralizar |
| Consulta o foto de condones, toallas o pañales | Se atiende como residuo |
| Violencia | Redirección breve |
| Peligro o deseo de hacerse daño | Respuesta prudente y línea de emergencias 123 |
| Insulto | Se trata como insatisfacción y ofrece contar qué falló |
| Spam | Límite existente de 20 mensajes por minuto, que se recupera solo |

Son listas de palabras, así que pueden equivocarse o dejar pasar frases nuevas. No son una moderación completa, y en una emergencia real la respuesta de un bot no reemplaza a una persona.

## Pruebas

Unitarias con repositorios en memoria: 1614 aprobadas, 0 fallidas, 1 omitida. Cubren intenciones, quejas, duplicados, invitados, ocultar datos, fallo de Oracle al guardar, vinculación sin página, cuenta ya vinculada y que el depósito sigue abriendo tras conversar. Se actualizaron pruebas existentes por los mensajes nuevos; ninguna se eliminó.

**Sin ejecutar con este código:** la integración en Oracle temporal, incluido `RepositorioComentariosOracle`. No se creó el Oracle temporal porque el disco tenía entre 4 y 10 GB libres y crearlo pone en riesgo la base real. Tampoco se ha comprobado desde un teléfono.
