# Estadísticas de cierre en SQL y cruce con Power BI (11/11/2026)

**Estado:** VERIFICADO REAL contra Oracle Cloud, en modo solo lectura. Evidencia: `docs/pruebas/resultados_cierre/estadisticas_sql_nube.txt`. Consultas: `database/04_consultas/09_estadisticas_cierre.sql`.

## La regla que usan todas las consultas

Un registro es **experimental** si su id aparece en `REGISTRO_SIMULACION` (V025, lote `SIM-2026-11-CIERRE`); si no, es **real**. Por eso cada consulta hace:

```sql
LEFT JOIN registro_simulacion s ON s.tabla = 'DEPOSITO' AND s.id_registro = d.id_deposito
```

y cuenta `s.id_registro IS NULL` como real. Es un `LEFT JOIN`, no un `JOIN`: un depósito real no tiene fila en `REGISTRO_SIMULACION`, y con un `JOIN` normal desaparecería del resultado.

Las fechas se guardan en UTC y se muestran en hora de Colombia con `FROM_TZ(fecha, 'UTC') AT TIME ZONE 'America/Bogota'`.

## Resultados y qué significan

| # | Pregunta | Cómo la responde | Resultado del 11/11/2026 |
|---|---|---|---|
| Q1 | Depósitos por caneca | `DEPOSITO` → `ORDEN_APERTURA` → `CONTENEDOR`, agrupado por caneca | 0 reales; 100 experimentales (85 blanca, 13 verde, 2 negra), todos en las canecas de simulación 1402–1404 |
| Q2 | Material más registrado | Agrupa por `id_material_declarado`; cuenta aparte las verificaciones que coinciden | PET 24, cartón 16, lata 16, restos de alimentos 13, papel 12… (todos experimentales) |
| Q3 | Actividad semanal | `TRUNC(fecha, 'IW')`: semana ISO que empieza el lunes | 5 semanas del 12/10 al 09/11, entre 4 y 28 depósitos por semana |
| Q4 | Actividad mensual | `TRUNC(fecha, 'MM')` | Octubre 64, noviembre 36 |
| Q5 | Participantes | `COUNT(DISTINCT id_usuario)` de las solicitudes con depósito | 50 personas ficticias, 0 reales |
| Q6 | Pesos | `peso_estimado_kg` y `peso_medido_kg` **en columnas separadas** | 14 476 g = 14,476 kg **estimados**; 0 kg **medidos** (no hay báscula) |
| Q7 | Ecopuntos generados y anulados | Libro `MOVIMIENTO_ECOPUNTOS`: tipo 1 crédito, tipo 2 reverso | 0 y 0: no se otorgaron puntos a datos sintéticos |
| Q8 | Verificación | `id_verificador` nulo = sin verificar; `sin_sensor = 1` = declarado sin sensor | 42 sin verificar, 58 verificados; 64 declarados sin sensor |
| Q9 | Fotos consultadas a Echo (V023) | `CONSULTA_VISION`, que no son depósitos | 0 (las consultas de prueba se borraron) |
| Q10 | Reglas vigentes (T001) | `desde_utc <= ahora` y sin fin, o fin posterior | 7 reglas, todas por unidad: PET 4, lata 5, cartón 3, metal 3, vidrio 3, papel 2, multicapa 2 |

## Cruce con Power BI

- La tabla `Depositos` del informe (repositorio V2, `powerbi/…/Depositos.tmdl`) lee la vista `EA_R_DEPOSITOS`. Esa vista es `EA_R_BASE_DEPOSITOS WHERE es_ensayo = 0`.
- Hoy `EA_R_BASE_DEPOSITOS` tiene `ES_ENSAYO = 1` para los 100 depósitos (58 verificados) y ninguna fila con `ES_ENSAYO = 0`. Por eso `EA_R_DEPOSITOS` devuelve **0 filas**, igual que la columna «reales» de Q1.
- Conclusión: con los datos de hoy, Power BI debe mostrar **0 depósitos operativos**. Es el estado real, no un error.
- **Pendiente:** abrir el informe en Power BI Desktop, actualizarlo con la credencial del propietario y comprobar visualmente las medidas. No se hizo, porque necesita la interfaz y la credencial del propietario.

## Por qué no hay ecopuntos aunque haya reglas y depósitos verificados

- Las reglas exigen unidades **verificadas** (`por_unidad = 1`).
- Acreditar es una operación explícita de un operario o administrador (`ServicioEcopuntos`), que deja un movimiento en el libro.
- Los depósitos del lote son sintéticos y nadie los acreditó, a propósito: un punto sobre un dato inventado no debe existir.
- Cuando haya depósitos reales verificados, el operario podrá acreditarlos y Q7 los contará.
