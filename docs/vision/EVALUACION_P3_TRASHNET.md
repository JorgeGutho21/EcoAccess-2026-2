# Evaluación del motor visual de P3 con TrashNet (09/11/2026)

Motor: `ollama-local:lfm3-vl:latest` (las fotos no salen del equipo). Fuente: TrashNet (MIT), zip fijado al commit
`6fa2b87` y verificado por SHA-256 (`0BF47279…07C2`).

> **Corrección del 10/11/2026.** Este documento decía que la admisión se midió contra la caneca piloto 1093. No es
> exacto: la herramienta tomaba primero la caneca de `Telegram:QrDemo`, que era «DEMO - Caneca ficticia» (id 1). En ese
> momento esa caneca admitía vidrio, porque se admitieron 18 botellas. Hoy está fuera de servicio y solo admite 4
> materiales. «Antes» y «después» usaron la misma caneca, así que la comparación vale. La cifra de admisión no es la
> de la piloto. La herramienta ahora usa la piloto por su nombre y escribe cuál usa; la nueva medición contra 1093
> está en `EVALUACION_COLOMBIA.md`.

## Conjuntos (sin fuga entre ellos)

| Conjunto | Fotos | Uso |
|---|---|---|
| Validación anterior | 50 (10 por clase) | La misma de V2; comparación entre motores. No se usó para ajustar |
| Ajuste | 45 (25 metal + 5 por clase) | Diagnóstico de las latas: se miraron fotos y nombres del modelo |
| Final | 100 (20 por clase) | Solo para medir antes y después; ninguna foto se miró |

Los tres conjuntos excluyen por SHA-256 las fotos de los demás y las de ajuste de V2 (0 coincidencias entre ajuste y final).
Archivos: `C:\ProgramData\EcoAccessP3\Vision\p3-ajuste` y `p3-final` (fuera del repositorio), manifiestos con hash.

## Diagnóstico de las latas

1. **Etiqueta mal puesta en la evaluación.** La clase `metal` de TrashNet incluye latas de bebida, latas de comida, papel
   aluminio, tapas y bandejas. Se había etiquetado todo como «lata de bebida»; un papel aluminio reconocido como metal se
   contaba como error. Corrección: la clase `metal` acepta `can` o `metal_item` (ambos metal, caneca blanca).
2. **Error real del intérprete, no del modelo.** Con la misma pregunta del bot, LFM3-VL nombró bien las latas del conjunto de
   ajuste («Canned fruit syrup», «Canned food», «Foil wrapper», «Aluminum foil tray»). El intérprete de P3 se quedaba con la
   última palabra conocida: «fruit» o «food» → comida servida; «wrapper» → empaque multicapa; «tray» → recipiente plástico.
   Corrección en `InterpretadorNombreObjeto`: «canned/tinned» es lata y el envase gana sobre su contenido; «foil/aluminum»
   gana sobre «wrapper» y «tray». Pruebas: `InterpretadorLatasTests` (14 casos, incluidos los que no deben cambiar).

No se reentrenó ningún modelo ni se cambió el prompt.

## Resultados en el conjunto final (100 fotos nuevas)

| Medida | Antes | Después |
|---|---|---|
| Objeto correcto | 69/100 | **72/100** |
| Destino ambiental correcto (blanca) | 84/100 | **87/100** |
| Enviado a otra caneca | 0 | 0 |
| Abriría con material equivocado | 0 | 0 |
| Abstenciones | 2 | 5 |
| Fallos técnicos | 0 | 0 |
| Latencia p50 / p95 | 3,33 s / 3,40 s | 3,33 s / 3,40 s |

Por clase después: vidrio 17/20, papel 16/20, cartón 15/20, metal 15/20 (antes 12/20), PET 9/20. Cambios foto a foto: tres
latas pasaron de «comida» a lata (acierto); dos papeles aluminio y un plástico que antes se tomaban como envoltorio ahora se
abstienen (más seguro, pero no se cuentan como acierto). Archivos: `resultados/objetos_p3_final100_antes.json` y
`objetos_p3_final100_despues.json`.

Punto débil siguiente: botellas PET (9/20), que el modelo describe a veces como recipiente o envoltorio.

## Error de método detectado y corregido

La primera medición «después» salió idéntica a «antes» en 17 s: la herramienta reanudaba desde un archivo de progreso y
reutilizó las respuestas de la versión anterior. Se descartó, se repitió desde cero (5,4 min) y ahora el archivo de progreso
lleva la huella de compilación de Lógica y del Bot, así que solo se reanuda con el mismo código.

## Límites

TrashNet son fotos de un objeto sobre fondo blanco, no fotos de teléfono en la calle. Estas cifras no son una exactitud de
campo; el objetivo de 90 % de destino ambiental aún no se alcanza en este conjunto (87 %). Las evaluaciones de V2 (19/50) y
la de Wikimedia de P3 (192/217) miden otras cosas y no se mezclan con estas.
