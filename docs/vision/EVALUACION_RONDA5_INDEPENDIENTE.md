# Quinta ronda independiente de reconocimiento visual (11/11/2026)

**Estado:** VERIFICADO MEDIANTE SIMULACIÓN. Son fotos públicas que se pasaron al modelo local real
(`ollama-local:lfm3-vl:latest`) con el mismo recorrido que usa el bot. No son fotos tomadas con un teléfono, y la
prueba no abrió ninguna tapa.

**No hubo entrenamiento.** El modelo no se reentrenó ni se ajustó. En esta ronda tampoco se cambiaron las instrucciones
ni las reglas: es una medición del sistema tal como está en la rama.

## Cómo se armó el conjunto

| Dato | Valor | Archivo |
|---|---|---|
| Candidatas descargadas | 156 fotos de Wikimedia Commons, con licencias CC0, CC BY, CC BY-SA o de dominio público | `resultados/ronda5_fuentes.json` (título, autor, licencia y URL de cada una) |
| Evaluadas | **102** | `resultados/ronda5_etiquetas.txt` |
| Excluidas | **54**, con el motivo escrito en el encabezado del archivo de etiquetas | `resultados/ronda5_etiquetas.txt` |
| Etiquetado | a ojo, **antes** de ejecutar el modelo, sin ver ninguna respuesta suya sobre estas fotos | idem |
| Independencia | se excluyeron todos los títulos usados en las rondas 1 a 4 | `scripts/pruebas/vision/Buscar-FotosCommons.ps1` |
| Manifiesto con SHA-256 | sí | `resultados/ronda5_manifiesto.json` |
| Resultado completo | sí, foto por foto | `resultados/ronda5_independiente.json` |

Un informe anterior hablaba de 104 evaluables y 52 excluidas. El archivo de etiquetas final tiene 102 filas, y esa
es la cifra que se usó.

Clases buscadas pensando en Colombia: PET, latas, cartón, papel, multicapa (tipo Tetra Pak), metalizados (paquetes de
mecato), icopor, orgánicos, envases de aseo, vidrio, residuos de rechazo, personas, animales, aparatos electrónicos y
escenas mixtas.

Comando reproducible (no crea solicitudes ni depósitos):

```
EcoAccess.P3.Bot.exe --evaluar-objetos <ronda5>\fotos\manifiesto.json local
```

## Resultados globales

| Métrica | Ronda 3 | Ronda 4 | **Ronda 5** |
|---|---|---|---|
| Fotos evaluadas | 131 | 141 | **102** |
| Respuestas correctas | 93 (71,0 %) | 119 (84,4 %) | **77 (75,5 %)** |
| Contexto correcto | n/d | n/d | 74 (72,5 %) |
| Falsas admisiones (abriría la tapa sin deber) | 7 | 0 | **2** |
| Ofertas indebidas (botón «depositar» sin deber) | 6 | 4 | **10** |
| Falsos rechazos | 9 | 9 | **6** |
| Abstenciones («no estoy seguro») | n/d | n/d | 9 |
| Fallos técnicos | 0 | 0 | **0** |
| Latencia mediana | 1 028 ms | 1 028 ms | **2 472 ms** |
| Latencia p95 | 2 921 ms | 3 270 ms | **3 849 ms** |

La ronda 3 dejó de ser independiente desde que se usó para corregir el intérprete, y por eso aparece solo como
referencia. Las rondas 4 y 5 son independientes entre sí.

**Lectura honesta:** la exactitud bajó de 84,4 % a 75,5 %. Esta ronda trae a propósito clases más difíciles para el
contexto colombiano (icopor, metalizados, multicapa, escenas mixtas) que no estaban en la ronda 4. No se ajustó nada
para mejorar la cifra.

## Por clase

| Clase | Fotos | Correctas | Observación |
|---|---|---|---|
| animal | 12 | 12 | todas reconocidas como animal vivo; ninguna oferta |
| aseo | 8 | 7 | un envase de aseo de vidrio se leyó como botella de vidrio |
| cartón | 6 | 5 | una caja se leyó como envase plástico |
| electrónico | 8 | 8 | 2 con contexto equivocado (una como persona y otra como «varios objetos»), pero sin ofertas |
| icopor | 7 | 4 | 4 leídos como plástico rígido, con oferta; 3 abstenciones |
| lata | 1 | 1 | |
| metalizado | 2 | 0 | uno leído como bolsa plástica (con oferta) y una abstención |
| mixto | 12 | 10 | dos escenas se leyeron como lata, con oferta |
| multicapa | 5 | **0** | el catálogo existe, pero el modelo los llama botella PET, empaque de mecato o medicamento |
| orgánico | 8 | 4 | uno se leyó como metal, con oferta |
| papel | 5 | 4 | |
| persona | 8 | 4 | 3 personas con comida se leyeron como «comida servida»; ninguna oferta |
| PET | 6 | 6 | |
| rechazo | 5 | 5 | |
| vidrio | 9 | 7 | dos abstenciones |

## Seguridad: lo que importa para la caneca

1. **Ninguna persona y ningún animal recibió una oferta de apertura** (20 fotos de personas y animales; 0 ofertas).
2. **Falsas admisiones: 2.**
   - `aseo-07`: envase de aseo leído como botella de vidrio.
   - `multicapa-03`: caja multicapa leída como botella PET.

   En ambas, la tapa que se habría ofrecido es la **blanca** (aprovechables), que es la caneca a la que también van esos
   materiales en Colombia. El error real es el **material declarado**, y eso afecta los ecopuntos y el peso estimado. Por
   eso los ecopuntos solo se acreditan cuando un operario verifica el depósito (RN de verificación y disparador
   `tr_ecop_unidades`).
3. **Ofertas indebidas: 10.** Una oferta es un botón que el ciudadano debe pulsar; no es una apertura automática. Se
   concentran en icopor (4), escenas mixtas (2), metalizado (1), multicapa (1), orgánico (1) y cartón (1).

## Errores y causa probable

| Patrón | Fotos | Causa probable | Acción |
|---|---|---|---|
| Icopor leído como plástico rígido | icopor-01, 02, 04 y 08 | el modelo no distingue poliestireno expandido de un envase plástico | candidato a regla: «icopor/espuma» → no admisible |
| Multicapa sin reconocer | multicapa-03, 04, 07, 08 y 09 | el modelo describe el uso (bebida, mecato) y no el material | candidato a vocabulario: «juice box», «tetra» |
| Metalizado como bolsa plástica | metalizado-02 | aspecto similar | revisar con fotos propias |
| Comida con personas | persona-01, 03 y 04 | la comida domina la escena | seguro (sin oferta); no se corrige |
| Latencia mayor | toda la ronda | primera foto con el modelo frío (16,6 s); no se controló la carga del equipo durante la medición | medir de nuevo con el equipo libre |

## Qué se hizo con estos errores

**Nada en esta ronda, a propósito.** Si se cambiaran el vocabulario o las reglas mirando estas 102 fotos, la ronda 5
dejaría de ser una prueba independiente (eso ocurrió con la ronda 3). Los candidatos de la tabla anterior quedan para una
corrección que tendrá que medirse con una **sexta ronda nueva**.

## Conclusión

- El reconocimiento local funciona sin fallos técnicos y **no ofreció abrir la caneca a personas ni a animales**.
- La exactitud real con fotos difíciles de Colombia fue **75,5 %**. No alcanza el 85 % que se buscaba, y se informa la cifra
  tal como salió.
- El punto débil está en **icopor y multicapa**. Por eso la apertura sigue pidiendo confirmación del ciudadano y los
  ecopuntos esperan la verificación del operario.
