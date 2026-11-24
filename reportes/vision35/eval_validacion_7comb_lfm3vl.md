# Informe de Evaluación ECOACCESS 35 — EVAL35-20261123-023148

- **Fecha UTC:** 2026-11-23 02:31:48
- **Versión código:** `2026.11.22+vision35`
- **Modelo:** `ollama-multirresiduo:lfm3-vl:latest`
- **Parámetros:** `temperature=0;think=false;num_ctx=2048;num_predict=384;schema=strict-json`
- **Casos ejecutados:** 7/7 (Aprobados: 3, Fallidos: 4, Confirmación/Rechazo: 2)
- **Latencia:** Media 5865 ms · P95 11505 ms · Total 41064 ms

## Criterios de Aceptación (Sección 12)

| Criterio | Objetivo | Medición real | Casos | Estado |
|---|---|---|---:|---|
| Cobertura dataset (35 × 100) | 3.500 evaluaciones reales | 7 evaluaciones (0/35 con ≥100) | 7 | **NO APROBADO (EVALUACIÓN INCOMPLETA)** |
| Precisión promedio por material | ≥ 95 % | 55,0 % | 7 | **NO APROBADO** |
| Recall promedio por material | ≥ 95 % | 60,0 % | 7 | **NO APROBADO** |
| Coincidencia exacta de combinaciones de canecas | ≥ 99 % | 42,9 % (3/7) | 7 | **NO APROBADO** |
| Falsas aperturas en casos evaluados | 0 | 2 | 7 | **NO APROBADO** |
| Fallos de integración / hardware | 0 | Integración=0, Hardware=0 | 7 | **APROBADO** |

## Exactitud por Categoría

| Categoría | Casos | Exactos | Exactitud |
|---|---:|---:|---:|
| VERDE | 4 | 3 | 75,0 % |
| BLANCA | 4 | 2 | 50,0 % |
| NEGRA | 4 | 2 | 50,0 % |
