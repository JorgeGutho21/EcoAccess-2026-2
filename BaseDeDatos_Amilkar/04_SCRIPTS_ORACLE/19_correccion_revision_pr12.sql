-- =====================================================================
-- EcoAccess BD — 19 · Corrección de las dos observaciones P2 de la revisión del PR #12
-- Ejecutar como ECOACCESS_BD en un esquema YA instalado (14/11/2026). Se puede repetir.
-- Una instalación nueva no lo necesita: 03 y 07 ya traen las dos correcciones.
--
-- 1. CK_DEPOSITO_MEDIDO: Oracle acepta una fila cuando el CHECK da UNKNOWN. La versión
--    anterior daba UNKNOWN con un peso e instrumento sin precisión, o con un instrumento y
--    precisión sin peso, y aceptaba esas mediciones incompletas. La nueva exige IS NOT NULL
--    en los tres datos de la medición.
-- 2. PKG_ECOPUNTOS.REGISTRAR_REGLA: bloquea la fila padre de MATERIAL antes de revisar las
--    tarifas. Así dos sesiones no pueden crear a la vez la primera tarifa abierta de un material.
--
-- No borra datos ni cambia tablas, columnas o relaciones. Respaldo previo:
-- 18_ENTREGA_Y_RESPALDOS/registro_respaldo_data_pump/ecoaccess_respaldo_20261114_antes_pr12.log
-- =====================================================================
SET SERVEROUTPUT ON SIZE UNLIMITED
WHENEVER SQLERROR EXIT FAILURE ROLLBACK

PROMPT ----- 1. Ningún depósito guardado viola la regla nueva (si alguno la viola, se detiene sin cambiar nada)
DECLARE
    n NUMBER;
BEGIN
    SELECT COUNT(*) INTO n FROM deposito
    WHERE NOT ((peso_medido_kg IS NULL AND instrumento_medicion IS NULL AND precision_medicion_g IS NULL)
            OR (peso_medido_kg IS NOT NULL AND instrumento_medicion IS NOT NULL AND precision_medicion_g IS NOT NULL
                AND peso_medido_kg > 0 AND TRIM(instrumento_medicion) IS NOT NULL AND precision_medicion_g > 0));
    IF n > 0 THEN
        RAISE_APPLICATION_ERROR(-20900, n || ' depósitos tienen una medición incompleta: revísalos antes de corregir el CHECK.');
    END IF;
    DBMS_OUTPUT.PUT_LINE('Depósitos con medición incompleta: 0');
END;
/

PROMPT ----- 2. CK_DEPOSITO_MEDIDO con los tres IS NOT NULL
ALTER TABLE deposito DROP CONSTRAINT ck_deposito_medido;
ALTER TABLE deposito ADD CONSTRAINT ck_deposito_medido CHECK (
    (peso_medido_kg IS NULL AND instrumento_medicion IS NULL AND precision_medicion_g IS NULL) OR
    (peso_medido_kg IS NOT NULL AND instrumento_medicion IS NOT NULL AND precision_medicion_g IS NOT NULL
         AND peso_medido_kg > 0 AND TRIM(instrumento_medicion) IS NOT NULL AND precision_medicion_g > 0));

WHENEVER SQLERROR CONTINUE
PROMPT ----- 3. Paquetes con registrar_regla corregido (07 solo usa CREATE OR REPLACE: no toca datos)
@@07_procedimientos_funciones_paquetes.sql

PROMPT ----- 4. Comprobación
SELECT constraint_name, status, validated FROM user_constraints WHERE constraint_name = 'CK_DEPOSITO_MEDIDO';
SELECT 'OBJETOS_INVALIDOS=' || COUNT(*) AS resultado FROM user_objects WHERE status <> 'VALID';
