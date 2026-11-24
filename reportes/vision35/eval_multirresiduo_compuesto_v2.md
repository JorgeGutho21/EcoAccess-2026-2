# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-002209

- **Fecha UTC:** 2026-11-23 00:22:09
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 18/18 (Aprobados: 12, Fallidos: 6, Confirmación/Rechazo: 5)
- **Latencia:** Media 6628,3 ms · P95 7848 ms · Total 119332 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 18 evaluaciones (0/35 con ≥100) | 18 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 66,8 % | 18 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 71,4 % | 18 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 66,7 % (12/18) | 18 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 1 | 18 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 18 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 13 | 12 | 92,3 % |
| BLANCA | 14 | 12 | 85,7 % |
| NEGRA | 13 | 10 | 76,9 % |
