# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-005202

- **Fecha UTC:** 2026-11-23 00:52:02
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 15/15 (Aprobados: 12, Fallidos: 3, Confirmación/Rechazo: 1)
- **Latencia:** Media 4268,5 ms · P95 15101 ms · Total 64055 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 15 evaluaciones (0/35 con ≥100) | 15 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 66,7 % | 15 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 65,0 % | 15 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 80,0 % (12/15) | 15 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 2 | 15 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 15 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 0 | 0 | 0,0 % |
| BLANCA | 15 | 12 | 80,0 % |
| NEGRA | 0 | 0 | 0,0 % |
