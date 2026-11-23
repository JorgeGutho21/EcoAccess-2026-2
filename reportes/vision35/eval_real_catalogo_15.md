# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-001414

- **Fecha UTC:** 2026-11-23 00:14:14
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 15/15 (Aprobados: 1, Fallidos: 14, Confirmación/Rechazo: 0)
- **Latencia:** Media 8792,2 ms · P95 54773 ms · Total 131926 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 15 evaluaciones (0/35 con ≥100) | 15 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 0,0 % | 15 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 0,0 % | 15 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 6,7 % (1/15) | 15 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 0 | 15 | **APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 15 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 0 | 0 | 0,0 % |
| BLANCA | 15 | 1 | 6,7 % |
| NEGRA | 0 | 0 | 0,0 % |
