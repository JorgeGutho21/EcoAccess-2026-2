# Informe de Evaluación ECOACCESS 35 — EVAL35-20261124-191925

- **Fecha UTC:** 2026-11-24 19:19:25
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 41/41 (Aprobados: 26, Fallidos: 15, Confirmación/Rechazo: 8)
- **Latencia:** Media 7301,7 ms · P95 9963 ms · Total 299393 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 41 evaluaciones (0/35 con ≥100) | 41 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 59,3 % | 41 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 46,2 % | 41 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 63,4 % (26/41) | 41 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 6 | 41 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 41 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 25 | 21 | 84,0 % |
| BLANCA | 14 | 5 | 35,7 % |
| NEGRA | 5 | 2 | 40,0 % |
