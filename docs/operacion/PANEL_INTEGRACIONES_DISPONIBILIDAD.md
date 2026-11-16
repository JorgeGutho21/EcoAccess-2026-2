# Panel del operario, Power BI, Google Forms y disponibilidad (fases 9, 10 y 11)

## Fase 9 — Panel del operario (VERIFICADO REAL contra Oracle Cloud, 11/11/2026)

El «Dashboard institucional» de Windows Forms tiene:
- **periodo** (desde/hasta) y **agrupación** por día, semana o mes;
- **filtros**: caneca, ubicación, material y categoría;
- **8 indicadores**: solicitudes, depósitos registrados y verificados, unidades verificadas, ciudadanos únicos, y ecopuntos acreditados, reversados y netos, comparados con el periodo anterior;
- **gráficos**: actividad por día o semana, depósitos por material y estado de verificación.

El selector **Datos** hace de modo experimental:
- **«Datos operativos»** es el valor por defecto y excluye todo lo marcado como ensayo o SIMULACIÓN;
- **«Demostración y simulaciones»** muestra solo eso, con el aviso «no son actividad física».

Capturas reales (`docs/evidencias/winforms_cierre/`, arnés `tests/herramientas/EcoAccess.P3.Capturas`):

| Captura | Resultado |
|---|---|
| `20_dashboard_institucional_operativo_1366x768.png` | Todo en 0: no hay actividad física real todavía |
| `20_dashboard_institucional_experimental_1366x768.png` | 96 solicitudes y 96 depósitos en el periodo, más 4 en el anterior (los 100 del lote); 55 verificados; 119 unidades verificadas; 50 ciudadanos únicos; 0 ecopuntos, porque la nube no tiene tarifas |

**Limitación:**
- la consulta experimental tardó 16,7 s contra Oracle Cloud y la operativa 4,2 s;
- el arnés crea un administrador ficticio temporal y lo borra al terminar (ids 4519 y 4520, ambos borrados).

## Fase 10 — Power BI y Google Forms

- **Power BI**:
  - Las vistas `ea_r_*` (V015/V019) marcan `es_ensayo`, así que el lote de SIMULACIÓN queda fuera de las páginas operativas.
  - `vista_depositos_estado` (V025) añade el estado de registro y el lote.
  - Sesiones anteriores verificaron dos páginas con datos reales, con la credencial escrita por el propietario. En esta misión **no se abrió Power BI Desktop**: las páginas nuevas con el lote quedan **sin validar en Power BI**.
- **Google Forms**:
  - **No existe** formulario ni integración en el repositorio (búsqueda en `src`, `docs`, `scripts`: 0 resultados).
  - No se inventa ni se simula una integración.
  - Si se quiere, el camino sin costo es un formulario de opinión que exporta a Google Sheets y se lee con Power BI. Las opiniones dentro del sistema ya existen (`/valorar` en Telegram, tabla de opiniones).
  - Estado: **PENDIENTE**, requiere crear el formulario con la cuenta del propietario.

## Fase 11 — Disponibilidad sin el PC

| Pieza | Estado |
|---|---|
| Oracle Autonomous Database | En la nube: no depende del PC |
| Bot de Telegram | En el PC con la tarea «EcoAccess P3 - vigilante del bot», que lo relanza cada 5 minutos. **No es 24/7**: depende del PC encendido |
| Página de registro | Tailscale Funnel desde el PC |
| VM Always Free de Oracle (Bogotá) | **BLOQUEADO**: solo hay forma A1 y la consola devuelve «Out of host capacity». La red quedó creada y el reintento está documentado |

No se contrató nada: el presupuesto es US$0. La alternativa de pago, una VM Windows, exige la autorización del
propietario. Ver `docs/operacion/ALOJAMIENTO_PERMANENTE.md`.
