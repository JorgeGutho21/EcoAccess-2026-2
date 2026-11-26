# Verificación del lote SIM-FERIA-2026

- **Fecha:** 2026-11-26 07:55 UTC · **Commit:** 8f00efb
- **Base:** ECOACCESS_P3_APP · Oracle Cloud (producción) · **Modo:** solo lectura (SET TRANSACTION READ ONLY)
- **Resultado:** 0 fallas · 2 avisos · 46 comprobaciones

| Id | Estado | Comprobación | Detalle |
|---|---|---|---|
| C01 | INFO | Conexión (solo lectura) | ECOACCESS_P3_APP · Oracle Cloud (producción) |
| C02 | OK | Lote | SIM-FERIA-2026 · estado COMPLETO · creado 2026-11-26 07:20 UTC · semilla 20261126 |
| C03 | OK | Escritura en curso | sin cambios en 5 s (1404 filas del lote) |
| C04 | OK | Cuentas del lote por tipo | ciu 50/50 · op 8/8 · sup 4/4 · adm 2/2 |
| C05 | OK | Cuentas del lote fuera del dominio de demostración | 0 filas |
| C06 | OK | Ciudadanos del lote con un rol distinto de 1 | 0 filas |
| C07 | OK | Operarios del lote sin rol 2 exclusivo | 0 filas |
| C08 | OK | Supervisores/administradores del lote sin rol 3 | 0 filas |
| C09 | OK | Cuentas del lote sin contraseña (no podrían iniciar sesión) | 0 filas |
| C10 | OK | Cuentas del lote bloqueadas o con intentos fallidos | 0 filas |
| C11 | INFO | Cuentas reales (fuera del lote) | 55 usuarios |
| C12 | OK | Depósitos del lote | 650 de 650 planeados |
| C13 | INFO | Claves dep-NNN sin depósito | 0 (los registros que el generador decidió no crear o no pudo crear) |
| C14 | INFO | Reparto por caneca (plan 45 % verde · 35 % blanca · 20 % negra) | VERDE 298 (45.8 %) · BLANCA 246 (37.8 %) · NEGRA 106 (16.3 %) |
| C15 | OK | Material declarado que esa caneca no admite | 0 filas |
| C16 | OK | Material verificado que esa caneca no admite | 0 filas |
| C17 | OK | Materiales deshabilitados en el lote | 0 filas |
| C18 | INFO | Reparto por material (17 materiales) | ORGANICO=158, RESTOS_ALIMENTOS=140, PET=59, SERVILLETA_USADA=53, PAPEL=45, LATA_ALUMINIO=40, CARTON=35, MULTICAPA=21, VIDRIO=20, ICOPOR_CONTAMINADO=17, COLILLA=14, PLASTICO_RIGIDO=11, PLASTICO_SIN_ID=11, OBJETO_COMPUESTO=9, METAL=8, BOLSA_PLASTICA=7, MADERA=2 |
| C19 | INFO | Antigüedad en días (plan 100·110·95·95·150·100) | 01-07=100, 08-14=110, 15-21=95, 22-30=95, 31-60=150, 61-90=100 |
| C20 | OK | Reparto por hora de Colombia | 14 horas distintas; la más cargada tiene 88 |
| C21 | OK | Fechas futuras (usuario, interacción, depósito) | 0 filas |
| C22 | OK | Actividad anterior a la creación de la cuenta | 0 filas |
| C23 | OK | Verificación anterior a la creación del verificador | 0 filas |
| C24 | OK | Depósito anterior al inicio de su interacción | 0 filas |
| C25 | OK | Depósitos del lote cuya interacción no está en el lote | 0 filas |
| C26 | INFO | Estado de los depósitos (PKG_DEPOSITO.estado) | pendiente=98, rechazado=42, verificado=510 |
| C27 | OK | Depósitos verificados por su propio titular | 0 filas |
| C28 | OK | Depósitos del lote verificados por cuentas reales | 0 filas |
| C29 | OK | Depósitos reales verificados por cuentas del lote | 0 filas |
| C30 | INFO | Ecopuntos del lote | movimientos 173 · acreditados 1323 · revertidos -49 · neto 1274 |
| C31 | OK | Ecopuntos del lote para cuentas reales | 0 filas |
| C32 | OK | Cuentas del lote con Ecopuntos de depósitos reales | 0 filas |
| C33 | OK | Crédito vigente en depósitos no verificados o rechazados | 0 filas |
| C34 | OK | Depósitos con más de un crédito vigente | 0 filas |
| C35 | OK | Crédito con tarifa de otro material | 0 filas |
| C36 | OK | Crédito que no coincide con tarifa × cantidad verificada | 0 filas |
| C37 | OK | Crédito para un material sin tarifa vigente al acreditar | 0 filas |
| C38 | INFO | Saldo de los ciudadanos del lote (suma de movimientos = PKG_ECOPUNTOS.saldo) | 0=14, >100=2, 1-20=15, 21-100=19 |
| C39 | OK | Eventos de órdenes del lote que no son sintéticos (id_externo sin «sim-») | 0 filas |
| C40 | AVISO | Aperturas sintéticas registradas en canecas reales | 650 del lote frente a 40 aperturas aceptadas fuera del lote (las consultas que no cruzan REGISTRO_SIMULACION las suman) |
| C41 | AVISO | Canales del lote (1 Telegram · 2 Manual · 3 Simulador · 4 Telegram simulado · 5 Asistido) | 1=466, 2=40, 3=30, 5=154 · 660 con canal operativo (1, 2 o 5): los informes que solo excluyen 3 y 4 los mezclan con la actividad real |
| C46 | INFO | Interacciones del lote sin depósito (invitados, aperturas manuales) | canal 2 sin cuenta=40 |
| C42 | OK | Interacciones reales atendidas por cuentas del lote | 0 filas |
| C43 | OK | Tarifas creadas por cuentas del lote | 0 filas |
| C44 | OK | Solo la Estación Oficial con tres canecas | estaciones 1 · canecas 3 · creadas por el lote 0 |
| C45 | INFO | Operaciones de la estación (V030/V033) de cuentas del lote | 0 (no se trazan en REGISTRO_SIMULACION) |
