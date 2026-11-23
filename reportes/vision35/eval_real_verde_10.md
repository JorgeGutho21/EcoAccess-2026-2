# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-002323

- **Fecha UTC:** 2026-11-23 00:23:23
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 10/10 (Aprobados: 10, Fallidos: 0, Confirmación/Rechazo: 0)
- **Latencia:** Media 4013,7 ms · P95 5025 ms · Total 40151 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 10 evaluaciones (0/35 con ≥100) | 10 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 95,0 % | 10 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 100,0 % | 10 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 100,0 % (10/10) | 10 | **APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 0 | 10 | **APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 10 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 10 | 10 | 100,0 % |
| BLANCA | 0 | 0 | 0,0 % |
| NEGRA | 0 | 0 | 0,0 % |
