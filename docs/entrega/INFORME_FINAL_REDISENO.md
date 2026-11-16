# Informe del rediseño y la estabilización

Fecha: 3 de noviembre de 2026. Rama: `desarrollo/telegram-doble-ia`. **El rediseño está incompleto y EcoAccess no se declara completamente funcional.**

## Qué se implementó y comprobó

| Área | Implementado | Comprobación |
|---|---|---|
| Desplazamiento | Caché de iconos, sombras e imagen de Eco; panel que no salta al recibir un clic | Medido: tabla siguiente |
| Registro manual | Búsqueda con Enter y botón, espera para no consultar en cada tecla, cancelación de la consulta anterior, flecha abajo, categoría y admisión por resultado | Arnés sobre clon: `botella`, `carton`, `cartón`, `pilas`, `plastico`, `vidrio`, `lata`, `papel`, y sin coincidencias |
| Invitado | Búsqueda arriba, acciones directas, caneca piloto separada de futuras instalaciones, crear cuenta | Arnés: Enter, vacío, catálogo, Anterior y Siguiente |
| Ciudadano | Actualizar mi estado, última interacción real, acceso a Mi actividad | Arnés: actualiza y muestra saldo y fecha |
| Ecopuntos del ciudadano | Resumen, Mi actividad, Esta semana, Historial, Cómo ganar, Próximamente; gráfico por día, semana o mes | Arnés: abre con datos de la clon. Agrupa los movimientos recibidos, no todo el libro si este supera lo que devuelve el servicio |
| Próximamente | Evolución, Separación inteligente y Expansión UPC, ilustrados y con estado | Arnés: abre; pruebas de contenido |
| Panel administrativo | 8 indicadores sin recorte, anillos visibles | Captura |
| Dashboard institucional | Tarjetas, listas reales de caneca, ubicación y material, categoría, agrupación, alcance, Limpiar, comparación con período anterior, estado de canecas | Arnés: los filtros cambian resultados con datos de la clon |
| Power BI en EcoAccess | Botón que abre el informe local si existe; si no, explica qué falta | Pruebas unitarias |
| Defectos corregidos | Estadísticas por canal omitían el registro asistido; cerrar opiniones o estadísticas personales podía lanzar una excepción | Prueba de integración y arnés |

### Desplazamiento, antes y después

Pasos con la ventana fuera de la pantalla y a 96 ppp. Miden el repintado: sirven para comparar, no sustituyen comprobarlo con la mano.

| Pantalla | Antes: media / P95 | Después: media / P95 |
|---|---|---|
| Explorar, invitado | 16,4 / 31,1 ms | 0,4–0,9 / 0,6–1,7 ms |
| Explorar, ciudadano | 19,6 / 41,4 ms | 1,7 / 4,1 ms |
| Panel del ciudadano en el marco principal | 14,9 / 24,5 ms | 0,8 / 1,7 ms |

La página de Explorar es además más alta que antes.

### Filtros del dashboard, sobre la clon en modo demostración

Formato: solicitudes, depósitos, verificados, acreditados y netos.

| Filtro | Resultado |
|---|---|
| Sin filtro | 6, 2, 2, 8, 8 |
| Material PET | 6, 2, 2, 8, 8 |
| Categoría Aprovechable | 6, 2, 2, 8, 8 |
| Categoría No aprovechable | 6, 0, 0, 8, 8 |
| Caneca DEMO | 0, 0, 0, 0, 0 |
| Operativo | 0, 0, 0, 0, 0 |

Solicitudes y puntos no se reparten por material en las vistas actuales: la tarjeta lo indica. Los filtros de estado y tipo de operación están en Estadísticas, no en este dashboard.

## Power BI

**No hay informe. Power BI Desktop no está instalado, así que no existe ningún `.pbix` ni `.pbip`.**

| Recurso | Estado |
|---|---|
| `V019`: materiales y ciudadanos únicos por mes | aplicada a la base del proyecto y en instalación limpia |
| Cuenta de solo lectura | en una clon: lee las vistas; no lee usuarios, credenciales ni movimientos; no inserta |
| Totales de `Validacion.sql` | coinciden con las tablas en la clon: 2 depósitos, 5 solicitudes y 8 puntos en ese momento |
| Consultas `.pq` y medidas `.dax` | **sin ejecutar** |
| Las 7 páginas | especificadas en `reportes/powerbi/LEEME.md`, **sin construir** |

Para seguir: instalar Power BI Desktop, con autorización del responsable, y seguir ese documento. Power BI Embedded no se implementó, porque no se han comprobado licencias, registro de aplicación ni capacidad.

## Bloques de la UPC

No se encontró una fuente oficial sobre los bloques A, B, C, H, I y P: solo se verificó el comedor de la sede Sabanas. Se muestran como nomenclatura aportada por el proyecto y por confirmar con la UPC. Las ilustraciones son conceptuales. Sin cantidades, fechas ni presupuestos.

## Pruebas finales

| Conjunto | Aprobadas | Fallidas | Omitidas |
|---|---|---|---|
| Unitarias | 1483 | 0 | 1 |
| Integración, Oracle temporal con piloto e instalación desde cero | 195 | 0 | 5 |
| Modelo real, Oracle temporal | 3 | 0 | 1 |

La integración se ejecutó con los binarios de la última compilación completa. Después solo cambió el ancho de la columna del botón Buscar en Explorar, compilado por separado porque el bot en marcha bloquea su ejecutable.

Dos pruebas antiguas cambiaron porque describían el comportamiento viejo: una exigía que «hola» respondiera «No encontré ese material» y otra, un botón retirado del menú. Una tercera pasó de 8 a 12 opciones. No se eliminó ninguna.

## Registros en la base del proyecto

- Siguen los **8 puntos ficticios**, sin modificar, a la espera de autorización.
- También hay cuentas de prueba, como «Ciudadano Web Ficticio», que aparecen en el registro manual.
- Al procesar mensajes pendientes el bot registró 6 updates y 1 interacción en la caneca piloto, marcada como `TelegramSimulado`.
- Al arrancar, el Bridge cerró como vencida la orden antigua `695`.

## Qué no se cumplió o sigue pendiente

| Requisito | Estado |
|---|---|
| Rediseño de todos los módulos | Parcial. Materiales, usuarios, canecas, depósitos y estadísticas conservan su diseño, con mejoras de scroll y estilo donde aplican |
| 125 % y 150 %, 1920×1080 | **Sin probar**: el arnés corre a la escala del equipo |
| Fluidez percibida del scroll | Medida, sin confirmar con la mano |
| Filtros del dashboard por día, estado y tipo | Agrupación por día, semana y mes sí; estado y tipo solo en Estadísticas |
| Gráfico de ecopuntos y participación en el dashboard | Indicadores y comparación sí; gráficos específicos no |
| Paginación en todos los módulos | Revisada en Explorar. En los demás no se ejecutó ninguna prueba nueva |
| Telegram desde el teléfono y segunda cuenta | **Pendiente de la persona** |
| Comparación LFM3-VL y Qwen | **Sin resultado**: límite de tokens |
| Inicio automático con Windows | No implementado; Defender bloqueó el script |
| ESP32 físico | Sin probar y sin activar |
| Inicio de sesión de punta a punta en la interfaz | No ejecutado; el arnés entra con una sesión construida |

Evidencia: `docs/telegram/DIAGNOSTICO_TELEGRAM.md`, `docs/pruebas/PRUEBAS_VISUALES.md`, `docs/pruebas/capturas/`.
