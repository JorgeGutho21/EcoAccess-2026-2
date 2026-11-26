# Procedimiento de reversión del lote SIM-FERIA-2026

**Solo con autorización expresa del responsable de la base de datos.** Borra datos en Oracle Cloud.

## Qué borra y qué no

T004 (`database/07_operacion/T004__reversion_lote_feria_2026.sql`) borra únicamente lo que `REGISTRO_SIMULACION` atribuye al
lote y lo que cuelga de ello. Cifras del 2026-11-26 07:55 UTC:

| Tabla | Filas |
|---|---|
| movimiento_ecopuntos / operacion_ecopuntos | 173 / 173 |
| auditoria_deposito | 552 |
| deposito | 650 |
| evento_dispositivo (solo `id_externo` `sim-…`) | 1950 |
| orden_apertura | 650 |
| interaccion | 690 |
| credencial_acceso / usuario_rol / usuario (`@feria.ecoaccess.test`) | 64 / 64 / 64 |
| registro_simulacion / lote_simulacion | 1404 / 1 |

No toca la Estación Oficial, las tres canecas, el catálogo, las tarifas ni las 55 cuentas reales y su actividad.

## Protecciones

1. Se detiene **sin cambiar nada** si el lote está `EN_CURSO` (salvo `-PermitirEnCurso`), si registra estaciones o canecas, si
   alguna cuenta no es `@feria.ecoaccess.test`, o si encuentra actividad real ligada al lote (interacciones, verificaciones,
   Ecopuntos, auditoría, vínculos de Telegram, opiniones, tarifas, operaciones de estación, análisis de imagen, eventos no
   sintéticos).
2. Copia todo en `ECO_RESP_T004_*` antes de borrar (incluye hashes con sal, nunca contraseñas en texto).
3. Los borrados van en una sola transacción. Al final compara los conteos de lo que no es del lote; si algo cambió, `ROLLBACK`.
4. Movimientos y operaciones de Ecopuntos se borran por rondas (lo que nadie referencia), sin desactivar restricciones.
5. Idempotente: si el lote ya no existe, solo lo informa.

## Pasos

1. Ver qué se borraría y comprobar que el bloque compila (solo lectura):
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\feria\Revertir-LoteFeria.ps1 -ValidarSintaxis
   ```
2. Guardar la verificación previa:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\feria\Verificar-LoteFeria.ps1
   ```
3. Con autorización, aplicar:
   ```powershell
   powershell -ExecutionPolicy Bypass -File scripts\feria\Revertir-LoteFeria.ps1 -Aplicar -Confirmar SIM-FERIA-2026
   ```
4. Verificar: `Verificar-LoteFeria.ps1` debe decir «SIM-FERIA-2026 no existe» y C44 debe seguir en OK (1 estación, 3 canecas).
5. Borrar el TXT de credenciales del escritorio (las cuentas ya no existen).

## Restaurar desde el respaldo (si hiciera falta)

Las tablas `ECO_RESP_T004_*` tienen las filas originales con sus identificadores. Se reinsertan a mano en orden inverso al borrado
(usuario → usuario_rol → credencial → interaccion → orden → evento → deposito → auditoria → operacion/movimiento → registro → lote),
dentro de una transacción. No hay script automático: hacerlo solo con el responsable de la base de datos.

## Estado de prueba

- Compilación del bloque en Oracle Cloud con `DBMS_SQL.PARSE` (compila sin ejecutar): **OK** (2026-11-26). Después se comprobó
  que no existe ninguna tabla `ECO_RESP_T004_*` y que el lote sigue intacto.
- **No ejecutado** en ningún esquema: falta ensayarlo en `ECOACCESS_P3_PRUEBAS` con una copia del lote (ver PENDIENTES P5).
