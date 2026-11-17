# Informe final — misión maestra de cierre definitivo (11/11/2026)

Rama `desarrollo/telegram-doble-ia`, PR #11 (único, sin force push). Base: Oracle Autonomous Database, esquema
`ECOACCESS_P3_APP`.

**Estados posibles:**
- **VERIFICADO REAL:** probado contra Oracle Cloud o la aplicación real.
- **VERIFICADO EN SIMULACIÓN:** software y simuladores, sin hardware ni fotos de teléfono.
- **IMPLEMENTADO SIN VALIDACIÓN EXTERNA.**
- **BLOQUEADO:** depende de un tercero o de una decisión.
- **PENDIENTE.**

## Matriz de requisitos

| REQUISITO | IMPLEMENTADO | PROBADO | EVIDENCIA | ESTADO | LIMITACIÓN |
|---|---|---|---|---|---|
| F0 Auditoría, línea base y riesgos | Sí | Sí | `PROGRESO.md` Fase 0; compilación con 0 errores; 1702 pruebas | VERIFICADO REAL | — |
| F0 V021/V022 en el repositorio con la misma huella | Sí | Sí | SHA-256 iguales a `ECO_MIGRACION_APLICADA` | VERIFICADO REAL | — |
| F1 Tres roles con permisos | Sí | Sí | `MatrizPermisos`, `MatrizPermisosTests` (8), prueba de mutación | VERIFICADO EN SIMULACIÓN | No hay cuentas Operario ni Administrador reales en la nube |
| F2 Tres canecas (Res. 2184) con el QR oficial | Sí | Sí | V024 en la nube; `ResolutorDestinoTests` (18); `EstacionTresCanecasOracleTests` (2, nube) | VERIFICADO REAL (datos), EN SIMULACIÓN (flujo) | Verde y negra sin caneca física, fuera de servicio |
| F2 Cuarta caneca identificada y conservada | Sí | Sí | 1093 = piloto oficial; 1/289/290 = DEMO; ninguna borrada | VERIFICADO REAL | — |
| F3 Ventana de 10 s, DECLARADA_SIN_SENSOR e idempotencia | Sí | Sí | `EstadoRegistroDeposito`, `EstacionSimuladaTests` (6), 64 sin sensor en el lote (27 siguen declarados y 37 se verificaron) | VERIFICADO EN SIMULACIÓN | Sin firmware ni motores (prohibido en esta misión) |
| F3 Adaptador ESP32 simulado (un ESP32 por tapa) | Sí | Sí | `EstacionSimulada` | VERIFICADO EN SIMULACIÓN | — |
| F4 Catálogo colombiano y evaluación independiente ≥ 90 % | Parcial | Sí (ronda 4) | 119/141 (84 %), 0 falsas admisiones | PENDIENTE (meta no alcanzada) | Una ronda nueva exige un corpus nuevo; no se descargó sin autorización |
| F4 Varios objetos | Sí | Sí | `VariosObjetos` pide una foto de uno solo | VERIFICADO EN SIMULACIÓN | — |
| F5 Pesos estimados versionados | Sí | Sí | `CatalogoPesosEstimados` 2026.11.10-1; 0 pesos medidos en 100 sintéticos | VERIFICADO REAL (nube, lote) | Valores de referencia, no medidos |
| F6 Ecopuntos configurables con estados | Sí | Sí | `MatrizEcopuntosPropuesta`; T001 rechaza sin administrador y no duplica | IMPLEMENTADO SIN VALIDACIÓN EXTERNA | La nube tiene 0 reglas: T001 exige un administrador real. La rama de T001 que inserta reglas no se ejercitó, porque las 7 ya existían en el esquema aislado |
| F7 Auditoría Oracle, migraciones nuevas y consultas | Sí | Sí | V024 y V025 aisladas y en la nube; `11_consultas_cierre.sql` (8 consultas sin errores) | VERIFICADO REAL | — |
| F8 50 ciudadanos y 100 registros SIMULACION | Sí | Sí | `SIM-2026-11-CIERRE`; segunda ejecución con 0 nuevos; informes reales en 0 | VERIFICADO REAL | Datos sintéticos: no son depósitos físicos ni pesajes |
| F8 Modo experimental | Sí | Sí | Selector «Demostración y simulaciones»: 96 + 4 depósitos y 50 ciudadanos | VERIFICADO REAL | — |
| F9 Panel del operario (periodos, filtros, KPI, gráficos) | Sí | Sí | `docs/evidencias/winforms_cierre/20_*.png` | VERIFICADO REAL | La consulta experimental tarda 16,7 s |
| F10 Power BI | Sí (vistas) | Parcial | V015/V019 con `es_ensayo`; dos páginas verificadas antes | IMPLEMENTADO SIN VALIDACIÓN EXTERNA | No se abrió Power BI Desktop con el lote |
| F10 Google Forms | No | — | 0 referencias | PENDIENTE | No se inventó una integración |
| F11 Disponibilidad sin el PC | Parcial | Sí (PC) | Vigilante cada 5 min; OCI «Out of host capacity» | BLOQUEADO | Sin capacidad gratuita A1 en Bogotá; sin gasto autorizado |
| F12 Pruebas unitarias | Sí | Sí | 1760 aprobadas, 1 omitida | VERIFICADO REAL | La omitida requiere privilegio de enlaces simbólicos |
| F12 Integración | Sí | Sí | 211 aprobadas, 7 inconclusas por diseño | VERIFICADO REAL (esquema aislado) | Visión real y PDB Docker no ejecutados |
| F12 Concurrencia 1/10/20/30 | Sí | Sí | `resultados_cierre/*.json`: 0 errores | VERIFICADO REAL (esquema aislado) | — |
| F12 Seguridad | Sí | Sí | Permisos, secretos, PBKDF2 y HMAC | VERIFICADO EN SIMULACIÓN | Sin análisis automático de inyección SQL aparte |
| F13 Documento maestro Word/PDF y 15 documentos | Sí | Revisión | `docs/entrega/INDICE_DOCUMENTAL_CIERRE.md` | VERIFICADO REAL | — |
| F13 100+ preguntas de sustentación | Sí | Revisión | 113 preguntas | VERIFICADO REAL | — |
| F15 Commits en el PR #11 sin force push | Sí | Sí | Historial de la rama | VERIFICADO REAL | — |

## Cambios en Oracle Cloud durante la misión (todos aditivos)

| Cambio | Antes → después |
|---|---|
| V024 | CONTENEDOR 4→6, CONFIGURACION_CANECA 3→6, CONTENEDOR_MATERIAL 30→46 |
| V025 | 2 tablas y 2 vistas nuevas |
| Lote `SIM-2026-11-CIERRE` | USUARIO 21→72, PUNTO_INSTALACION 2→3, CONTENEDOR 6→9, INTERACCION, ORDEN y DEPOSITO 0→100, EVENTO_DISPOSITIVO 0→336 |
| Administradores temporales del arnés de capturas (4519, 4520) | Creados y borrados por el propio arnés |

No se borró ni modificó ninguna fila previa. Al cierre hay 0 objetos inválidos.

## Lo que necesita el propietario

1. Crear en la aplicación una cuenta **Administrador** y una **Operario** reales, con su propia contraseña.
2. Con el id de ese administrador, ejecutar `database/07_operacion/T001__tarifas_ecopuntos_propuestas.sql`, reemplazando `<<ID_ADMINISTRADOR>>`.
3. Ejecutar una vez `pwsh -File scripts\desarrollo\Instalar-Arranque-Automatico.ps1` desde esta carpeta (`EcoAccess-P3`). El bot en vivo ya corre con esta versión desde el 11/11/2026, pero la tarea vigilante todavía apunta a `EcoAccess-P3-auditoria`: si el bot se cae, relanzaría la versión anterior. Cambiar la tarea programada queda en manos del propietario.
4. Decidir el alojamiento 24/7 (reintento de OCI A1 o VM de pago) y si se usará Google Forms.
5. Si se quiere la meta de visión del 90 %, autorizar una quinta ronda con un corpus nuevo.
6. Abrir Power BI Desktop para validar las páginas con el lote.
