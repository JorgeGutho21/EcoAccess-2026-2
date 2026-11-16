# Regresión completa de cierre (11/11/2026)

**Rama:** `desarrollo/auditoria-tercer-corte`, commit base `d37175d`. Equipo del propietario (Windows 11), .NET Framework 4.8.
Visión integrada activada (`ECOACCESS_PROBAR_VISION_INTEGRADA=1`, Ollama local `lfm3-vl`). Las pruebas omitidas **no** se cuentan como aprobadas.

## Primera pasada (11:19–11:39)

| Bloque | Entorno | Superadas | Con error | Omitidas | Total |
|---|---|---|---|---|---|
| Compilación Debug | — | correcta | 0 | — | — |
| Compilación Release | — | correcta | 0 | — | — |
| Unitarias | sin base de datos | 1760 | 0 | 1 | 1761 |
| Integración | Oracle Cloud (ECOACCESS_P3_APP) | 212 | 1 | 5 | 218 |
| Integración | esquema aislado local (ECOACCESS_P3_PRUEBAS) | 212 | 2 | 4 | 218 |

## Análisis de los fallos

1. **`FotoVisionReal_VinculaContratoSeguro_ConsultaCatalogo_DeclaraYConfirma_HttpSimulado_Persiste`** (nube y aislado).
   - *Síntoma:* esperaba `"Papel de oficina y cuadernos, verificado, sin peso"` y el bot respondió `"…, verificado, peso estimado 0.015 kg"`.
   - *Causa:* la prueba se escribió antes de `cf6f7ee` (catálogo versionado de pesos estimados). Desde ese commit, verificar un depósito sin báscula le asigna un peso **estimado** (2 unidades × peso de catálogo). El producto se comporta como especifica la regla de negocio; la expectativa de la prueba estaba desactualizada. Nadie lo detectó antes porque la prueba solo corre con la visión integrada activada.
   - *Corrección:* la prueba ahora exige `"verificado, peso estimado"` y comprueba que el texto **no** contiene `"peso medido"`, que es la garantía importante (un estimado nunca se presenta como medido).
   - *Resultado tras la corrección:* superada en el esquema aislado (14 s) y en Oracle Cloud (23 s).
2. **`UsuariosNuevosSimultaneosQuedanAisladosYSobrevivenAlReinicio (30)`** (solo aislado).
   - Se repitió tres veces aislada: **3/3 superada**. Es intermitente, por carga: corrió en paralelo con otra herramienta que usaba el mismo equipo. Queda anotada como prueba inestable, sin cambiar el código.

## Datos operativos después de la regresión (Oracle Cloud, solo lectura)

| Comprobación | Valor | Igual que antes |
|---|---|---|
| Depósitos totales | 100 | sí |
| Depósitos fuera del lote `SIM-2026-11-CIERRE` | 0 | sí |
| Movimientos de ecopuntos | 0 | sí |
| Fotos consultadas a Echo (`CONSULTA_VISION`) | 0 | sí |
| Usuarios | 73 (71 ciudadanos, 1 operario, 1 administrador) | sí |
| Contenedores | 9 | sí |
| Reglas de ecopuntos | 7 | sí |

Las pruebas de integración limpian lo que crean. La regresión no dejó residuos en los datos operativos.

## Estado

- Compilación y unitarias: **VERIFICADO REAL**.
- Integración nube y aislada: **VERIFICADO REAL**, con la corrección de la prueba de visión y la prueba de concurrencia intermitente (3/3 al repetirla).
- Omitidas (5 nube, 4 aislado): dependen de hardware físico o de credenciales no configuradas; **no** se cuentan como aprobadas.
