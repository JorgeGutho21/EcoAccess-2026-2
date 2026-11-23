# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-005400

- **Fecha UTC:** 2026-11-23 00:54:00
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 15/15 (Aprobados: 11, Fallidos: 4, Confirmación/Rechazo: 2)
- **Latencia:** Media 5998,9 ms · P95 7918 ms · Total 89999 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 15 evaluaciones (0/35 con ≥100) | 15 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 81,3 % | 15 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 79,0 % | 15 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 73,3 % (11/15) | 15 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 1 | 15 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=1, Hardware=0 | 15 | **NO APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 9 | 7 | 77,8 % |
| BLANCA | 9 | 8 | 88,9 % |
| NEGRA | 9 | 5 | 55,6 % |
