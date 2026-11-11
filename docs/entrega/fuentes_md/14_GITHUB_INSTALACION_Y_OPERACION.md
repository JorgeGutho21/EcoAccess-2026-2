---
archivo: 14_GITHUB_INSTALACION_Y_OPERACION
titulo: GitHub, instalación y operación de EcoAccess P3
subtitulo: Repositorio, ramas, commits y PR #11; instalación en otro equipo; arranque, vigilante, cierre y disponibilidad
codigo: 14
---

# Conceptos de Git y GitHub

- **Repositorio:** la carpeta del proyecto con todo su historial. EcoAccess P3 está en `JorgeGutho21/EcoAccess-P3`.
- **Commit:** una foto del proyecto con un mensaje que explica qué cambió y por qué.
- **Rama:** una línea de trabajo paralela. `main` es la principal; el trabajo de cierre está en `desarrollo/telegram-doble-ia`.
- **Pull request (PR):** la propuesta de integrar una rama en otra. El PR #11 reúne la entrega final.
- **Fusión con avance rápido (fast-forward):** si una rama solo agrega commits encima de la otra, Git la integra sin crear un commit de fusión ni tocar el historial.

# Ramas del cierre

| Rama | Papel | Estado (11/11/2026) |
|---|---|---|
| `main` | rama principal | sin la entrega final hasta que se fusione el PR #11 |
| `desarrollo/telegram-doble-ia` | rama del PR #11 | contiene todo el trabajo de cierre |
| `desarrollo/auditoria-tercer-corte` | auditoría paralela | sus 3 commits (`702def2`, `d37175d`, `382fce1`) se integraron en la rama del PR por avance rápido, sin conflictos |

Reglas que se siguieron:

- No se usó `force push`.
- No se reescribió ningún commit.
- No se creó un PR nuevo.

# Quién escribió el código

Según `git log`, los commits del repositorio están firmados por **Jorge Gutierrez** (111) y **JorgeGutho21** (12), que son
la misma persona con dos configuraciones de Git. Muchos commits recientes incluyen como coautor al asistente de
programación (Claude), y así lo indica el mensaje de cada uno. El historial de Git **no** registra commits de Heyder
Canchila ni de Saray Cadena; sus aportes al proyecto (canecas físicas, pruebas presenciales, sustentación) no se ven en
Git, y este documento no se los atribuye ni se los niega.

# Convención de los mensajes

Prefijo en minúscula (`feat`, `fix`, `docs`, `test`, `refactor`), un ámbito entre paréntesis y una frase en español que
explica el cambio. Ejemplo real:

`fix(operacion): el lanzador y el vigilante ya no toman como propio el bot de otra copia cuyo nombre de carpeta empieza igual`

# Historia del proyecto según Git

La tabla siguiente sale de `git log`. El primer commit del repositorio es del 01/11/2026.

{{commits}}

# Instalación en otro equipo

{{incluir:docs/instalacion/INSTALACION_COMPANEROS.md}}

# Operación diaria

{{incluir:docs/operacion/GUIA_INICIO_RAPIDO_ECOACCESS.md}}

# Disponibilidad sin el PC del propietario

Hoy EcoAccess **depende del PC del propietario**. El bot, Ollama y la página de registro corren allí. Oracle Cloud sí está
siempre disponible, pero sin el bot nadie atiende Telegram.

| Opción | Costo | Estado |
|---|---|---|
| PC encendido con la tarea vigilante | US$ 0 | **en uso** |
| Máquina virtual Always Free de Oracle Cloud | US$ 0 | **bloqueada**: no hay capacidad en la región, no hay claves de la CLI de OCI, y el bot es .NET Framework 4.8 (solo Windows) |
| Servidor de pago | > US$ 0 | descartado: la misión no autoriza gastos |

El análisis completo está en `docs/operacion/ALOJAMIENTO_PERMANENTE.md`.
