# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-000830

- **Fecha UTC:** 2026-11-23 00:08:30
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 18/18 (Aprobados: 6, Fallidos: 12, Confirmación/Rechazo: 5)
- **Latencia:** Media 11886,6 ms · P95 24877 ms · Total 213997 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 18 evaluaciones (0/35 con ≥100) | 18 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 41,1 % | 18 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 38,7 % | 18 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 33,3 % (6/18) | 18 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 1 | 18 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 18 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 13 | 9 | 69,2 % |
| BLANCA | 14 | 6 | 42,9 % |
| NEGRA | 13 | 4 | 30,8 % |
