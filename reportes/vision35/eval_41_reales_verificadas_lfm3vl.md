# Informe de Evaluación ECOACCESS 35 — EVAL35-20261124-011603

- **Fecha UTC:** 2026-11-24 01:16:03
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 41/41 (Aprobados: 30, Fallidos: 11, Confirmación/Rechazo: 2)
- **Latencia:** Media 5988,3 ms · P95 7165 ms · Total 245548 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 41 evaluaciones (0/35 con ≥100) | 41 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 66,0 % | 41 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 49,5 % | 41 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 73,2 % (30/41) | 41 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 10 | 41 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 41 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 25 | 20 | 80,0 % |
| BLANCA | 14 | 6 | 42,9 % |
| NEGRA | 5 | 5 | 100,0 % |
