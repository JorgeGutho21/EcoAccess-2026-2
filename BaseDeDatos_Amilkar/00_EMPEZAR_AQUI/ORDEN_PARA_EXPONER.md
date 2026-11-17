# Orden para exponer

Guion de unos 20 minutos.

**Antes de empezar, deja abiertos:**
- el informe final (PDF);
- SQL Developer con la conexión `ECOACCESS_BD`;
- el `.dmd` en Data Modeler;
- el `.pbix` en Power BI.

La lista de comprobación del día está en `13_SUSTENTACION/CHECKLIST_SUSTENTACION.md`. El manual completo, con más detalle, está en `13_SUSTENTACION/MANUAL_DE_SUSTENTACION.pdf`.

| # | Tema | Qué decir | Qué mostrar |
|---|---|---|---|
| 1 | Qué es EcoAccess | Sistema para clasificar residuos en estaciones con tres canecas (blanca, verde y negra). El ciudadano escanea un QR con Telegram, la IA orienta con una foto, la caneca se abre y un operario verifica el depósito para dar EcoPuntos. | Informe final, capítulo 1 |
| 2 | Problema que resuelve | La separación en la fuente no se mide ni se premia; sin datos confiables no se sabe qué se recicla ni quién lo hace. | Informe final, 1.1 Problemática |
| 3 | Objetivo | Diseñar e implementar en Oracle una base de datos que registre todo el ciclo con integridad, trazabilidad y auditoría. | Informe final, capítulo 2 |
| 4 | Cómo funciona la base | 19 tablas en cinco módulos; las cinco ideas: una foto no es un depósito; lo declarado, lo verificado y lo medido se separan; nada se borra; los puntos se calculan; los datos sintéticos van marcados. | `02_MODELOS_DE_DATOS/03_MODELO_RELACIONAL_GENERAL/MR_00_general_por_modulos.png` |
| 5 | Cómo se relacionan las tablas | MER (15 entidades, 19 relaciones) frente a modelo relacional (38 FK); las dos tablas puente; las FK compuestas que impiden mezclar colores; la relación recursiva de los reversos. | `.dmd` abierto, subvista M4 Operación |
| 6 | Cómo se conecta Oracle | Oracle 26ai Free, `localhost:1521/FREEPDB1`; tres esquemas: proyecto, prácticas y lector. | SQL Developer: `SELECT USER FROM dual;` |
| 7 | Cómo consultar | Una consulta de cada nivel: JOIN, GROUP BY y el recorrido de un depósito. | `04_SCRIPTS_ORACLE/13_consultas_exposicion.sql` (Q01–Q22) o los ejemplos de la Parte IX de la guía de los 173 atributos |
| 8 | Cómo funcionan los triggers | BEFORE para validar (coherencia), AFTER para auditar (`:OLD` y `:NEW`), el bloqueo de un depósito cerrado y el historial de canecas. | `07_DISPARADORES_TRIGGERS/demo_cuatro_triggers.sql` (F5) |
| 9 | Cómo se conecta con C# | ODP.NET, procedimientos almacenados y CRUD sin DELETE; la contraseña va en variables de entorno. | `10_CSHARP_VISUAL_STUDIO/SOLUCION_CSHARP/EcoAccessBD.sln` |
| 10 | Cómo funciona Power BI | Cuenta lectora con acceso solo a las vistas `VW_BI_*`, cinco páginas, glosario, filtros y datos sintéticos rotulados. | `09_POWER_BI/ARCHIVO_PBIX/EcoAccess_BD.pbix` |
| 11 | Qué pruebas realizamos | Instalador con 28 comprobaciones, 163 pruebas del esquema, 19 de prácticas, 309 ejercicios, C# 10/10 y 0 objetos inválidos. | `16_EVIDENCIAS_Y_PRUEBAS/RESUMEN_ULTIMA_EJECUCION.md` |
| 12 | Conclusiones | Separar hechos parecidos es diseño de datos; muchas reglas se cumplen solas con FK y CHECK; los disparadores se reservan para lo que mira otras tablas; lo pendiente se declara. | Informe final, Conclusiones |

## Comandos de demostración

```sql
-- 6. ¿Dónde estoy?
SELECT USER, SYS_CONTEXT('USERENV','CON_NAME') FROM dual;

-- 7. Recorrido completo de un residuo (vista)
SELECT * FROM vw_recorrido WHERE id_deposito = 5;

-- 7. Saldo de EcoPuntos: es una suma, no una columna
SELECT SUM(puntos) AS puntos_netos FROM movimiento_ecopuntos;

-- 5. Relación recursiva: cada reverso junto a su crédito
SELECT r.id_movimiento AS reverso, r.puntos, c.id_movimiento AS credito, c.puntos
FROM   movimiento_ecopuntos r
JOIN   movimiento_ecopuntos c ON c.id_movimiento = r.id_movimiento_revertido;

-- 8. Un trigger en acción: intentar editar un depósito ya verificado
UPDATE deposito SET cantidad_verificada = 5 WHERE id_deposito = 1;   -- ORA-20103
ROLLBACK;
```

Para comprobar la instalación completa, desde PowerShell en la carpeta raíz:

```
pwsh -File .\INSTALAR_Y_PROBAR.ps1
```

## Preguntas probables del profesor

Las respuestas completas están en `13_SUSTENTACION/PREGUNTAS_SUSTENTACION_MODELO_RELACIONAL.pdf`.

- ¿Por qué 19 tablas y 173 atributos?
- ¿Qué diferencia hay entre PK y FK? ¿Qué es una PK compuesta?
- ¿Por qué `USUARIO_ROL` es una tabla puente? ¿Por qué `CONTENEDOR_MATERIAL` tiene `CATEGORIA`?
- ¿Qué diferencia hay entre `CONSULTA_VISION` y `DEPOSITO`?
- ¿Qué significa la tabla que se referencia a sí misma?
- ¿Dónde está el saldo de puntos?
- ¿Cuándo se usa BEFORE y cuándo AFTER? ¿Qué son `:OLD` y `:NEW`?
- ¿Por qué una auditoría guarda el valor anterior y el nuevo?
- ¿Qué significa que el modelo esté en tercera forma normal?
