# 00. Presentación del proyecto

**Universidad Popular del Cesar · Programación de Computadores III · EcoAccess P3**

## Problema y justificación

Muchas personas no saben en qué caneca va cada residuo: los materiales aprovechables se mezclan con los que no lo son y los peligrosos terminan en recipientes comunes. EcoAccess orienta en el momento de desechar y deja un registro verificable.

## Objetivo general

Construir un prototipo inteligente de orientación y registro de residuos con una caneca física, Telegram, Windows Forms, Oracle y ESP32.

## Objetivos específicos

1. Identificar el residuo por fotografía con un modelo local y orientar su disposición.
2. Autorizar la apertura solo mediante reglas de negocio, nunca por decisión directa del modelo.
3. Registrar cada operación en Oracle sin depósitos ni puntos ficticios.
4. Ofrecer al operador un registro asistido en Windows Forms.
5. Diseñar para varias canecas sin reconstruir la solución.

## Alcance

Versión 1: una caneca con un compartimiento, dos servomotores en una tapa y comunicación USB. Son proyecciones, no funciones instaladas, las indicadas en el módulo Próximamente.

## Tecnologías

C# 9 sobre .NET Framework 4.8, Windows Forms, Oracle Database 26ai Free (`Oracle.ManagedDataAccess`), Telegram Bot API por HTTP, Ollama con `lfm3-vl`, ESP32 con Arduino/C++, MSTest, Git y GitHub.

## Beneficiarios

Comunidad universitaria, operadores del punto ecológico y el equipo de gestión ambiental de la UPC.

## Limitaciones

Sin sensor de ingreso no se puede afirmar que entró un residuo: el depósito queda pendiente de verificación. EcoAccess no afirma que un residuo se reciclara por haber ingresado al sistema. El reconocimiento visual es una sugerencia con errores medidos (documento 07).