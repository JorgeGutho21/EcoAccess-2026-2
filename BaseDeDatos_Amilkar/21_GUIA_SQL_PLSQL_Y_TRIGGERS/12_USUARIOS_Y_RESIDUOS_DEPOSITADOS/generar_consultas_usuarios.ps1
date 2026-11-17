<#
  Genera las consultas de «qué residuos deposita cada usuario» (solo SELECT):
    * un archivo .sql por pregunta (Q01…Q15),
    * 00_EJECUTAR_TODAS_SOLO_SELECT.sql (script maestro para F5 en SQL Developer),
    * GUIA_USUARIOS_Y_RESIDUOS.md (explicación de cada JOIN, FK, WHERE, agregación e interpretación).
  Si existe RESULTADOS_ESPERADOS.txt (salida real de Oracle), la guía incluye el resultado de cada consulta.
  Uso: pwsh -File generar_consultas_usuarios.ps1
#>
$ErrorActionPreference = 'Stop'
$aqui = Split-Path -Parent $MyInvocation.MyCommand.Path
$utf8 = New-Object Text.UTF8Encoding $false

# Cadena REAL de FK para saber de quién es un depósito (DEPOSITO no tiene id_usuario):
#   DEPOSITO.id_orden ─FK_DEPOSITO_EVENTO(id_evento,id_orden)→ EVENTO_DISPOSITIVO ─FK_EVENTO_ORDEN→ ORDEN_APERTURA
#   ORDEN_APERTURA.id_interaccion ─FK_ORDEN_INTERACCION→ INTERACCION.id_usuario ─FK_INTERACCION_USUARIO→ USUARIO
# (DEPOSITO.id_orden ya es la misma orden del evento, por eso se une directo con ORDEN_APERTURA.)
$CONSULTAS = @(
  @{ n = 'Q01'; a = 'Q01_usuarios_registrados_y_roles.sql'; p = '¿Qué usuarios están registrados y qué roles tienen?'
     sql = @"
SELECT u.id_usuario,
       'C-' || LPAD(u.id_usuario, 4, '0')                       AS seudonimo,
       u.activo,
       u.es_sintetico,
       TO_CHAR(u.fecha_registro_utc, 'YYYY-MM-DD')               AS fecha_registro,
       LISTAGG(r.descripcion, ', ') WITHIN GROUP (ORDER BY r.codigo) AS roles
FROM   usuario u
LEFT   JOIN usuario_rol ur ON ur.id_usuario = u.id_usuario
LEFT   JOIN rol r          ON r.id_rol      = ur.id_rol
GROUP  BY u.id_usuario, u.activo, u.es_sintetico, u.fecha_registro_utc
ORDER  BY u.id_usuario;
"@
     j = 'USUARIO → USUARIO_ROL → ROL (LEFT JOIN: también aparece quien no tenga rol).'; f = 'FK_USUARIO_ROL_USUARIO, FK_USUARIO_ROL_ROL'
     w = 'Ninguno: se listan todas las personas.'; g = 'GROUP BY por persona y LISTAGG para unir sus roles en un solo texto.'
     i = 'Una fila por persona. No se muestran nombre ni correo: se usa el seudónimo C-0001, igual que en Power BI. es_sintetico = 1 indica dato de demostración.' }
  @{ n = 'Q02'; a = 'Q02_cuantos_usuarios_con_interacciones.sql'; p = '¿Cuántos usuarios han realizado interacciones (sesiones QR)?'
     sql = @"
SELECT COUNT(*)                        AS interacciones,
       COUNT(i.id_usuario)             AS interacciones_con_usuario,
       COUNT(*) - COUNT(i.id_usuario)  AS interacciones_de_visitante,
       COUNT(DISTINCT i.id_usuario)    AS usuarios_distintos
FROM   interaccion i;
"@
     j = 'Ninguno: se cuenta sobre INTERACCION.'; f = 'INTERACCION.id_usuario es FK a USUARIO (FK_INTERACCION_USUARIO) y admite NULL.'
     w = 'Ninguno.'; g = 'COUNT(*) cuenta filas; COUNT(columna) no cuenta los NULL; COUNT(DISTINCT) cuenta personas distintas.'
     i = 'La diferencia entre COUNT(*) y COUNT(id_usuario) son las sesiones de visitantes sin cuenta.' }
  @{ n = 'Q03'; a = 'Q03_residuos_depositados_por_cada_usuario.sql'; p = '¿Qué residuos ha depositado cada usuario?'
     sql = @"
SELECT 'C-' || LPAD(i.id_usuario, 4, '0')   AS usuario,
       d.id_deposito,
       md.codigo                            AS material_declarado,
       mv.codigo                            AS material_verificado,
       CASE d.estado_verificacion WHEN 0 THEN 'PENDIENTE' WHEN 1 THEN 'VERIFICADO' ELSE 'RECHAZADO' END AS estado,
       c.nombre                             AS caneca,
       TO_CHAR(d.fecha_deteccion_utc, 'YYYY-MM-DD HH24:MI') AS fecha_utc
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   contenedor c     ON c.id_contenedor  = o.id_contenedor
JOIN   material md      ON md.id_material   = d.id_material_declarado
LEFT   JOIN material mv ON mv.id_material   = d.id_material_verificado
WHERE  i.id_usuario IS NOT NULL
ORDER  BY i.id_usuario, d.fecha_deteccion_utc;
"@
     j = 'DEPOSITO → ORDEN_APERTURA → INTERACCION (titular) y → CONTENEDOR; MATERIAL dos veces (declarado y verificado, con alias md y mv).'
     f = 'FK_DEPOSITO_EVENTO (id_evento, id_orden), FK_EVENTO_ORDEN, FK_ORDEN_INTERACCION, FK_ORDEN_CONTENEDOR, FK_DEPOSITO_MAT_DECL, FK_DEPOSITO_MAT_VERIF'
     w = 'i.id_usuario IS NOT NULL: solo depósitos de personas identificadas.'; g = 'Ninguna: es un listado.'
     i = 'DEPOSITO no tiene id_usuario: el dueño se obtiene de la sesión (INTERACCION) de la orden que abrió la caneca. El material verificado sale vacío mientras el depósito está pendiente o si fue rechazado.' }
  @{ n = 'Q04'; a = 'Q04_material_mas_frecuente_por_usuario.sql'; p = '¿Qué material deposita con mayor frecuencia cada usuario?'
     sql = @"
SELECT usuario, material, depositos
FROM  (SELECT 'C-' || LPAD(i.id_usuario, 4, '0') AS usuario,
              m.codigo                           AS material,
              COUNT(*)                           AS depositos,
              ROW_NUMBER() OVER (PARTITION BY i.id_usuario ORDER BY COUNT(*) DESC, m.codigo) AS puesto
       FROM   deposito d
       JOIN   orden_apertura o ON o.id_orden       = d.id_orden
       JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
       JOIN   material m       ON m.id_material    = d.id_material_declarado
       WHERE  i.id_usuario IS NOT NULL
       GROUP  BY i.id_usuario, m.codigo)
WHERE  puesto = 1
ORDER  BY depositos DESC, usuario;
"@
     j = 'Igual que Q03, con MATERIAL por el material declarado.'; f = 'FK_DEPOSITO_MAT_DECL, FK_ORDEN_INTERACCION'
     w = 'Afuera: puesto = 1 (el más frecuente). Adentro: solo usuarios identificados.'
     g = 'GROUP BY usuario y material; ROW_NUMBER() OVER (PARTITION BY usuario ...) numera los materiales de cada persona.'
     i = 'Se usa el material DECLARADO porque existe en todos los depósitos; el verificado solo existe en los verificados.' }
  @{ n = 'Q05'; a = 'Q05_depositos_por_usuario_y_estado.sql'; p = '¿Cuántos depósitos tiene cada usuario y en qué estado?'
     sql = @"
SELECT 'C-' || LPAD(i.id_usuario, 4, '0')                       AS usuario,
       COUNT(*)                                                 AS depositos,
       SUM(CASE WHEN d.estado_verificacion = 0 THEN 1 ELSE 0 END) AS pendientes,
       SUM(CASE WHEN d.estado_verificacion = 1 THEN 1 ELSE 0 END) AS verificados,
       SUM(CASE WHEN d.estado_verificacion = 2 THEN 1 ELSE 0 END) AS rechazados
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY depositos DESC, usuario;
"@
     j = 'DEPOSITO → ORDEN_APERTURA → INTERACCION.'; f = 'FK_DEPOSITO_EVENTO, FK_EVENTO_ORDEN, FK_ORDEN_INTERACCION'
     w = 'Solo usuarios identificados.'; g = 'GROUP BY usuario; SUM(CASE ...) cuenta por estado en una sola consulta (tabla dinámica).'
     i = 'pendientes + verificados + rechazados = depositos en cada fila.' }
  @{ n = 'Q06'; a = 'Q06_depositos_pendientes.sql'; p = '¿Cuáles depósitos están pendientes de verificación?'
     sql = @"
SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0') AS usuario,
       m.codigo                            AS material_declarado,
       d.cantidad_declarada,
       ROUND((CAST(SYS_EXTRACT_UTC(SYSTIMESTAMP) AS DATE) - CAST(d.fecha_deteccion_utc AS DATE)), 1) AS dias_esperando
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material m       ON m.id_material    = d.id_material_declarado
WHERE  d.estado_verificacion = 0
ORDER  BY d.fecha_deteccion_utc;
"@
     j = 'DEPOSITO → ORDEN_APERTURA → INTERACCION; DEPOSITO → MATERIAL.'; f = 'FK_ORDEN_INTERACCION, FK_DEPOSITO_MAT_DECL'
     w = 'estado_verificacion = 0 (PENDIENTE).'; g = 'Ninguna; se calcula cuántos días lleva esperando.'
     i = 'Un depósito pendiente todavía NO tiene EcoPuntos: los puntos se acreditan solo al verificar.' }
  @{ n = 'Q07'; a = 'Q07_depositos_verificados.sql'; p = '¿Cuáles depósitos fueron verificados y por quién?'
     sql = @"
SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')    AS titular,
       'C-' || LPAD(d.id_verificador, 4, '0') AS verificador,
       mv.codigo                             AS material_verificado,
       d.cantidad_verificada,
       TO_CHAR(d.fecha_verificacion_utc, 'YYYY-MM-DD HH24:MI') AS verificado_utc
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material mv      ON mv.id_material   = d.id_material_verificado
WHERE  d.estado_verificacion = 1
ORDER  BY d.fecha_verificacion_utc;
"@
     j = 'DEPOSITO → ORDEN_APERTURA → INTERACCION (titular); DEPOSITO → MATERIAL (verificado).'; f = 'FK_DEPOSITO_VERIFICADOR (DEPOSITO.id_verificador → USUARIO), FK_DEPOSITO_MAT_VERIF'
     w = 'estado_verificacion = 1 (VERIFICADO).'; g = 'Ninguna.'
     i = 'El titular y el verificador siempre son personas distintas: el disparador 2 lo garantiza (ORA-20101).' }
  @{ n = 'Q08'; a = 'Q08_depositos_rechazados.sql'; p = '¿Cuáles depósitos fueron rechazados y por qué?'
     sql = @"
SELECT d.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')    AS titular,
       md.codigo                             AS material_declarado,
       'C-' || LPAD(d.id_verificador, 4, '0') AS verificador,
       d.motivo_verificacion
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
JOIN   material md      ON md.id_material   = d.id_material_declarado
WHERE  d.estado_verificacion = 2
ORDER  BY d.id_deposito;
"@
     j = 'Igual que Q07, con el material declarado.'; f = 'FK_ORDEN_INTERACCION, FK_DEPOSITO_MAT_DECL'
     w = 'estado_verificacion = 2 (RECHAZADO).'; g = 'Ninguna.'
     i = 'Un rechazo no da EcoPuntos y exige motivo (CHECK de DEPOSITO).' }
  @{ n = 'Q09'; a = 'Q09_unidades_declaradas_vs_verificadas.sql'; p = '¿Cuántas unidades se declararon y cuántas se verificaron por usuario?'
     sql = @"
SELECT 'C-' || LPAD(i.id_usuario, 4, '0')                             AS usuario,
       SUM(NVL(d.cantidad_declarada, 0))                              AS unidades_declaradas,
       SUM(CASE WHEN d.estado_verificacion = 1 THEN NVL(d.cantidad_verificada, 0) ELSE 0 END) AS unidades_verificadas
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY unidades_verificadas DESC, usuario;
"@
     j = 'DEPOSITO → ORDEN_APERTURA → INTERACCION.'; f = 'FK_ORDEN_INTERACCION'
     w = 'Solo usuarios identificados.'; g = 'SUM con NVL (un NULL no suma) y SUM(CASE) para contar solo lo verificado.'
     i = 'Declarado = lo que dijo la persona; verificado = lo que comprobó el operario. No son lo mismo y no se mezclan.' }
  @{ n = 'Q10'; a = 'Q10_peso_estimado_vs_medido.sql'; p = '¿Qué peso se estimó y qué peso se midió por usuario?'
     sql = @"
SELECT 'C-' || LPAD(i.id_usuario, 4, '0') AS usuario,
       ROUND(SUM(d.peso_estimado_kg), 3)  AS kg_estimados,
       COUNT(d.peso_medido_kg)            AS depositos_con_bascula,
       ROUND(SUM(d.peso_medido_kg), 3)    AS kg_medidos
FROM   deposito d
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
WHERE  i.id_usuario IS NOT NULL
GROUP  BY i.id_usuario
ORDER  BY kg_estimados DESC NULLS LAST, usuario;
"@
     j = 'DEPOSITO → ORDEN_APERTURA → INTERACCION.'; f = 'FK_ORDEN_INTERACCION (y FK_DEPOSITO_PESO explica de dónde sale el estimado)'
     w = 'Solo usuarios identificados.'; g = 'SUM por columna separada: nunca se suma estimado + medido.'
     i = 'kg_medidos queda vacío si nunca hubo báscula: el sistema no inventa pesos.' }
  @{ n = 'Q11'; a = 'Q11_ecopuntos_acreditados_por_usuario.sql'; p = '¿Cuántos EcoPuntos se acreditaron a cada usuario?'
     sql = @"
SELECT 'C-' || LPAD(mv.id_usuario, 4, '0')                     AS usuario,
       SUM(CASE WHEN mv.tipo = 1 THEN mv.puntos ELSE 0 END)    AS creditos,
       SUM(CASE WHEN mv.tipo = 2 THEN mv.puntos ELSE 0 END)    AS reversos,
       SUM(mv.puntos)                                          AS saldo
FROM   movimiento_ecopuntos mv
GROUP  BY mv.id_usuario
ORDER  BY saldo DESC, usuario;
"@
     j = 'Ninguno: MOVIMIENTO_ECOPUNTOS ya tiene id_usuario.'; f = 'FK_MOV_USUARIO (MOVIMIENTO_ECOPUNTOS.id_usuario → USUARIO)'
     w = 'Ninguno.'; g = 'SUM(CASE) por tipo (1 crédito, 2 reverso) y SUM total = saldo.'
     i = 'El saldo no se guarda en ninguna columna: es la suma de los movimientos (igual que VW_SALDO_ECOPUNTOS y PKG_ECOPUNTOS.saldo).' }
  @{ n = 'Q12'; a = 'Q12_movimientos_reversados.sql'; p = '¿Qué movimientos de EcoPuntos fueron reversados?'
     sql = @"
SELECT rv.id_movimiento                     AS id_reverso,
       rv.id_movimiento_revertido           AS id_credito_anulado,
       'C-' || LPAD(rv.id_usuario, 4, '0')  AS usuario,
       cr.puntos                            AS puntos_del_credito,
       rv.puntos                            AS puntos_del_reverso,
       rv.motivo
FROM   movimiento_ecopuntos rv
JOIN   movimiento_ecopuntos cr ON cr.id_movimiento = rv.id_movimiento_revertido
WHERE  rv.tipo = 2
ORDER  BY rv.id_movimiento;
"@
     j = 'MOVIMIENTO_ECOPUNTOS consigo misma (auto-JOIN): rv = reverso, cr = crédito original.'; f = 'FK_MOV_REVERTIDO (id_movimiento_revertido → id_movimiento de la misma tabla)'
     w = 'tipo = 2 (reverso).'; g = 'Ninguna.'
     i = 'Un reverso nunca borra: agrega un movimiento negativo que apunta al crédito anulado. Si no hay filas, no se ha revertido ningún crédito.' }
  @{ n = 'Q13'; a = 'Q13_que_usuario_uso_una_caneca.sql'; p = '¿Qué usuarios utilizaron una caneca determinada?'
     sql = @"
DEFINE id_caneca = 12
SELECT c.id_contenedor,
       c.nombre                             AS caneca,
       'C-' || LPAD(i.id_usuario, 4, '0')   AS usuario,
       COUNT(DISTINCT o.id_orden)           AS aperturas,
       COUNT(d.id_deposito)                 AS depositos
FROM   contenedor c
JOIN   orden_apertura o ON o.id_contenedor  = c.id_contenedor
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
LEFT   JOIN deposito d  ON d.id_orden       = o.id_orden
WHERE  c.id_contenedor = &id_caneca
AND    i.id_usuario IS NOT NULL
GROUP  BY c.id_contenedor, c.nombre, i.id_usuario
ORDER  BY depositos DESC, usuario;
"@
     j = 'CONTENEDOR → ORDEN_APERTURA → INTERACCION; LEFT JOIN DEPOSITO (una orden puede no terminar en depósito).'; f = 'FK_ORDEN_CONTENEDOR, FK_ORDEN_INTERACCION, FK_DEPOSITO_EVENTO'
     w = 'c.id_contenedor = &id_caneca (variable de sustitución: cambia el 12 en DEFINE por otra caneca).'; g = 'GROUP BY caneca y usuario; COUNT(DISTINCT orden) = aperturas; COUNT(deposito) = depósitos.'
     i = 'Abrir la caneca (orden) no es lo mismo que depositar: por eso se cuentan por separado.' }
  @{ n = 'Q14'; a = 'Q14_cambios_registrados_en_auditoria.sql'; p = '¿Qué cambios fueron registrados en la auditoría de depósitos?'
     sql = @"
SELECT a.id_auditoria,
       a.id_deposito,
       'C-' || LPAD(i.id_usuario, 4, '0')   AS titular,
       a.operacion,
       a.estado_anterior || ' -> ' || a.estado_nuevo AS estado,
       'C-' || LPAD(a.id_actor, 4, '0')     AS actor,
       a.usuario_bd,
       TO_CHAR(a.fecha_utc, 'YYYY-MM-DD HH24:MI') AS fecha_utc
FROM   auditoria_deposito a
JOIN   deposito d       ON d.id_deposito    = a.id_deposito
JOIN   orden_apertura o ON o.id_orden       = d.id_orden
JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
ORDER  BY a.fecha_utc, a.id_auditoria;
"@
     j = 'AUDITORIA_DEPOSITO → DEPOSITO → ORDEN_APERTURA → INTERACCION.'; f = 'FK_AUD_DEP_DEPOSITO, FK_ORDEN_INTERACCION'
     w = 'Ninguno.'; g = 'Ninguna.'
     i = 'Cada fila la escribió el disparador 3 (TRG_DEPOSITO_AUD_VERIFICACION) con los valores :OLD y :NEW. El actor es quien verificó o corrigió; el titular es el dueño del depósito.' }
  @{ n = 'Q15'; a = 'Q15_fotos_consultadas_vs_depositos.sql'; p = '¿Cuántas fotos consultó cada usuario frente a cuántos depósitos hizo?'
     sql = @"
SELECT u.id_usuario,
       'C-' || LPAD(u.id_usuario, 4, '0') AS usuario,
       (SELECT COUNT(*) FROM consulta_vision cv WHERE cv.id_usuario = u.id_usuario) AS fotos_consultadas,
       (SELECT COUNT(*)
        FROM   deposito d
        JOIN   orden_apertura o ON o.id_orden       = d.id_orden
        JOIN   interaccion i    ON i.id_interaccion = o.id_interaccion
        WHERE  i.id_usuario = u.id_usuario)                                       AS depositos
FROM   usuario u
ORDER  BY fotos_consultadas DESC, u.id_usuario
FETCH  FIRST 15 ROWS ONLY;
"@
     j = 'Subconsultas escalares correlacionadas: una sobre CONSULTA_VISION y otra sobre la cadena del depósito.'; f = 'FK_CONSULTA_USUARIO, FK_ORDEN_INTERACCION'
     w = 'Dentro de cada subconsulta: el mismo usuario de la fila externa.'; g = 'COUNT(*) en cada subconsulta.'
     i = 'Una foto consultada a la IA (CONSULTA_VISION) NO es un depósito. Una persona puede consultar muchas fotos y depositar pocas veces.' }
)

# --- archivos .sql individuales ---
$cab = @"
-- EcoAccess · Base de Datos (profesor Amílkar) · Universidad Popular del Cesar
-- Carpeta 21 · 12_USUARIOS_Y_RESIDUOS_DEPOSITADOS · SOLO LECTURA (SELECT). No modifica datos.
-- Conexión: EcoAccess BD LOCAL (usuario ECOACCESS_BD). Ejecuta con Ctrl+Enter (una consulta) o F5 (script).
-- Los datos son SINTÉTICOS (es_sintetico = 1). Las personas se muestran con seudónimo C-0001.
"@
$maestro = New-Object Text.StringBuilder
[void]$maestro.AppendLine($cab); [void]$maestro.AppendLine('SET PAGESIZE 200'); [void]$maestro.AppendLine('SET LINESIZE 220'); [void]$maestro.AppendLine('SET VERIFY OFF'); [void]$maestro.AppendLine('')
foreach ($q in $CONSULTAS) {
  $txt = "$cab`r`n-- $($q.n) · $($q.p)`r`n-- JOIN: $($q.j)`r`n-- FK: $($q.f)`r`n`r`n$($q.sql.Trim())`r`n"
  [IO.File]::WriteAllText((Join-Path $aqui $q.a), $txt, $utf8)
  [void]$maestro.AppendLine("PROMPT ===== $($q.n) · $($q.p) =====")
  [void]$maestro.AppendLine($q.sql.Trim()); [void]$maestro.AppendLine('')
}
[IO.File]::WriteAllText((Join-Path $aqui '00_EJECUTAR_TODAS_SOLO_SELECT.sql'), $maestro.ToString(), $utf8)
$todo = $maestro.ToString()
if ([regex]::IsMatch($todo, '(?im)^\s*(INSERT|UPDATE|DELETE|MERGE|DROP|ALTER|CREATE|TRUNCATE|GRANT|COMMIT)\b')) { throw 'El script maestro contiene algo distinto de SELECT' }

# --- resultados reales (si existen) ---
$res = @{}
$rf = Join-Path $aqui 'RESULTADOS_ESPERADOS.txt'
if (Test-Path $rf) {
  $r = Get-Content $rf -Raw -Encoding UTF8
  foreach ($m in [regex]::Matches($r, '(?s)===== (Q\d\d) [^\n]*=====\s*(.*?)(?====== Q\d\d|\z)')) { $res[$m.Groups[1].Value] = $m.Groups[2].Value.TrimEnd() }
}

# --- guía ---
$md = New-Object Text.StringBuilder
$L = { param($s) [void]$md.AppendLine($s) }
& $L '# ¿Qué residuos deposita cada usuario? — consultas SQL de EcoAccess'
& $L ''
& $L '**EcoAccess · Base de Datos · Profesor Amílkar · Universidad Popular del Cesar**  '
& $L 'Integrantes: Jorge Alfonso Gutiérrez Thomas · Heyder Hedrey Canchila de Ángel · Saray Marcela Cadena Palomino'
& $L ''
& $L '## Antes de empezar: cómo se sabe de quién es un depósito'
& $L ''
& $L 'La tabla `DEPOSITO` **no tiene una columna `id_usuario`**. El dueño (titular) de un depósito se encuentra siguiendo las claves foráneas reales:'
& $L ''
& $L '```text'
& $L 'DEPOSITO.id_orden ──► ORDEN_APERTURA.id_interaccion ──► INTERACCION.id_usuario ──► USUARIO'
& $L '        (FK_DEPOSITO_EVENTO + FK_EVENTO_ORDEN)   (FK_ORDEN_INTERACCION)   (FK_INTERACCION_USUARIO)'
& $L '```'
& $L ''
& $L '| No confundir | con | Por qué |'
& $L '|---|---|---|'
& $L '| Una foto consultada (`CONSULTA_VISION`) | un depósito (`DEPOSITO`) | La foto solo orienta; el depósito es lo que entró a la caneca. |'
& $L '| `cantidad_declarada` | `cantidad_verificada` | La primera la dice la persona; la segunda la comprueba el operario. |'
& $L '| `peso_estimado_kg` | `peso_medido_kg` | El estimado sale del catálogo; el medido, de una báscula (puede quedar vacío). |'
& $L '| Depósito pendiente (estado 0) | depósito con EcoPuntos | Los puntos se acreditan solo al verificar (estado 1). |'
& $L ''
& $L '**Cómo ejecutar:** en SQL Developer abre la conexión **EcoAccess BD LOCAL**, abre un archivo `Qnn_*.sql` (Archivo → Abrir) y pulsa **Ctrl+Enter** sobre la consulta. Para ejecutar las 15 de una vez, abre `00_EJECUTAR_TODAS_SOLO_SELECT.sql` y pulsa **F5**. Todas son SELECT: no cambian datos.'
& $L ''
foreach ($q in $CONSULTAS) {
  & $L "## $($q.n) · $($q.p)"
  & $L ''
  & $L "Archivo: [``$($q.a)``]($($q.a))"
  & $L ''
  & $L '```sql'; & $L $q.sql.Trim(); & $L '```'
  & $L ''
  & $L '| Parte | Explicación |'
  & $L '|---|---|'
  & $L "| JOIN | $($q.j) |"
  & $L "| Claves foráneas usadas | $($q.f) |"
  & $L "| WHERE | $($q.w) |"
  & $L "| Agregación | $($q.g) |"
  & $L "| Cómo interpretar el resultado | $($q.i) |"
  & $L ''
  if ($res.ContainsKey($q.n)) {
    $lin = @($res[$q.n] -split "`r?`n" | ForEach-Object { ($_ -replace ' {4,}', '  ') -replace '-{20,}', ('-' * 20) }); $mostrar = if ($lin.Count -gt 28) { @($lin[0..27]) + "… ($($lin.Count - 28) líneas más en RESULTADOS_ESPERADOS.txt)" } else { $lin }
    & $L '**Resultado real en Oracle (datos sintéticos):**'; & $L ''; & $L '```text'; foreach ($x in $mostrar) { & $L $x }; & $L '```'; & $L ''
  } else { & $L '_Resultado: pendiente de ejecutar en Oracle._'; & $L '' }
  $cap = Get-ChildItem (Join-Path $aqui 'CAPTURAS') -Filter "*_$($q.n)_*.jpg" -ErrorAction SilentlyContinue | Select-Object -First 1
  if ($cap) { & $L "**Captura real en SQL Developer:**"; & $L ''; & $L "![$($q.n) en SQL Developer](CAPTURAS/$($cap.Name))"; & $L '' }
}
[IO.File]::WriteAllText((Join-Path $aqui 'GUIA_USUARIOS_Y_RESIDUOS.md'), $md.ToString(), $utf8)
"Generados: $($CONSULTAS.Count) consultas, script maestro y guía. Resultados incluidos: $($res.Count)."
