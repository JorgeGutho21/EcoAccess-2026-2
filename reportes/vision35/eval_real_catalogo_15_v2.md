# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-001937

- **Fecha UTC:** 2026-11-23 00:19:37
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 15/15 (Aprobados: 9, Fallidos: 6, Confirmación/Rechazo: 1)
- **Latencia:** Media 3527,6 ms · P95 4576 ms · Total 52931 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 15 evaluaciones (0/35 con ≥100) | 15 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 35,4 % | 15 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 41,7 % | 15 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 60,0 % (9/15) | 15 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 2 | 15 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 15 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 0 | 0 | 0,0 % |
| BLANCA | 15 | 9 | 60,0 % |
| NEGRA | 0 | 0 | 0,0 % |
