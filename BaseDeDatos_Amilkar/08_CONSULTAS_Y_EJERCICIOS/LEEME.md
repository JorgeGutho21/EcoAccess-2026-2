# Consultas y ejercicios

## El banco de 309 ejercicios verificados

| Archivo | Qué es |
|---|---|
| `GUIA_DE_ESTUDIO_309_EJERCICIOS.pdf` / `.docx` | Guía de estudio: teoría y los 309 ejercicios con su salida real. |
| `banco_ejercicios_BD.sql` | Ejercicios sobre `ECOACCESS_BD`. |
| `banco_ejercicios_PRACTICAS.sql` | Ejercicios sobre `ECOACCESS_PRACTICAS` (HR). |
| `resultados_ejercicios.md` | El estado de cada ejercicio en la última ejecución real: 299 correctos y 10 con error intencional, 0 fallan. |
| `ejercicios_fuente.txt`, `ejercicios_fuente_2.txt` | La fuente de los ejercicios (tema, título, objetivo, explicación y SQL). |
| `generar_banco.ps1` | Genera los `.sql`, los ejecuta en Oracle, guarda la salida real y marca cada ejercicio. Lo llama el instalador. |

Las salidas completas están en `16_EVIDENCIAS_Y_PRUEBAS/salidas/17_banco_ejercicios.txt`. Los mismos ejercicios, repartidos por tema, están en `05_SQL_Y_PLSQL`.

## Por nivel

| Nivel | Temas del banco | Ejercicios |
|---|---|---:|
| Básico | 1. Conexión y diccionario · 2. SELECT · 3. WHERE · 4. Ordenar y limitar · 5. Funciones de una fila · 6. GROUP BY y HAVING | 109 |
| Intermedio | 7. JOIN · 8. Subconsultas · 9. Conjuntos · 10. Analíticas · 11. Fechas · 12. DML y transacciones · 13. Vistas y seguridad | 106 |
| Avanzado | 14. PL/SQL básico · 15. Cursores · 16. Excepciones · 17. Procedimientos, funciones y paquetes · 18. Disparadores · 19. HR | 94 |

## Errores intencionales

Los 10 ejercicios marcados «ERROR ESPERADO» deben fallar: muestran una restricción en acción (ORA-00001, ORA-01400, ORA-02290, ORA-02291, una vista READ ONLY o una excepción propia). En `resultados_ejercicios.md` aparecen como «OK (error esperado)».

## Consultas que el profesor puede pedir durante la exposición

| Archivo | Contenido |
|---|---|
| `04_SCRIPTS_ORACLE/13_consultas_exposicion.sql` | Las 22 consultas de exposición (Q01–Q22): ranking de ciudadanos con depósitos verificados, últimos 7 días, materiales, canecas, pesos estimados y medidos, historial y auditoría. Salida real en `16_EVIDENCIAS_Y_PRUEBAS/salidas/13_consultas_exposicion.txt`. |
| `03_DICCIONARIO_DE_DATOS/GUIA_COMPLETA_173_ATRIBUTOS_Y_38_RELACIONES.pdf`, Parte IX | 12 consultas para explicar el modelo: FK con JOIN, estados con DECODE, tabla puente, FK compuesta, titular de un depósito, reglas y movimientos, relación recursiva, saldo, auditoría, historial, fotos frente a depósitos y la vista de recorrido. |
| `00_EMPEZAR_AQUI/ORDEN_PARA_EXPONER.md` | Los comandos de demostración en el orden del guion. |
