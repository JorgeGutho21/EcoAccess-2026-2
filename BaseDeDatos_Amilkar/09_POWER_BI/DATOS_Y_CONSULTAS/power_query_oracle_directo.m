// Power Query (M) · VARIANTE CONEXIÓN DIRECTA A ORACLE (modo en línea).
// Derivada automáticamente de power_query.m: solo cambia el paso Origen; tipos y pasos siguientes son idénticos.
// Parámetro ServidorOracle = "localhost:1521/FREEPDB1" (texto). Autenticación: Base de datos,
// usuario ECOACCESS_BD_LECTOR (solo vistas VW_BI_*). La contraseña se escribe en el cuadro de Power BI, nunca aquí.
// ESTADO: expresión generada, NO verificada todavía dentro de Power BI Desktop (ver GUIA_POWER_BI_ORACLE).
// Power Query (M) del modelo EcoAccess_BD. GENERADO por generar_pbip.ps1.
// Parámetro CarpetaDatos = carpeta con los CSV exportados por exportar_csv.sql.

// ===== Depositos  (origen: ecoaccess_bd.vw_bi_depositos, cuenta de solo lectura)
let
    Origen = Oracle.Database(ServidorOracle, [Query = "SELECT * FROM ECOACCESS_BD.VW_BI_DEPOSITOS"]),
    Encabezados = Origen,
    Tipos = Table.TransformColumnTypes(Encabezados, {{"ID_DEPOSITO", Int64.Type}, {"CIUDADANO", type text}, {"DIA", type datetime}, {"FECHA_HORA", type datetime}, {"ESTACION", type text}, {"CANECA", type text}, {"MATERIAL", type text}, {"MATERIAL_NOMBRE", type text}, {"ESTADO", type text}, {"EVIDENCIA", type text}, {"UNIDADES", Int64.Type}, {"PESO_ESTIMADO_KG", type number}, {"PESO_MEDIDO_KG", type number}, {"CANAL", type text}, {"ORIGEN_DATO", type text}}, "en-US"),
    SoloFecha = Table.TransformColumnTypes(Tipos, {{"DIA", type date}})
in
    SoloFecha

// ===== ConsultasVision  (origen: ecoaccess_bd.vw_bi_consultas_vision, cuenta de solo lectura)
let
    Origen = Oracle.Database(ServidorOracle, [Query = "SELECT * FROM ECOACCESS_BD.VW_BI_CONSULTAS_VISION"]),
    Encabezados = Origen,
    Tipos = Table.TransformColumnTypes(Encabezados, {{"ID_CONSULTA", Int64.Type}, {"CIUDADANO", type text}, {"DIA", type datetime}, {"CANAL", type text}, {"MODO", type text}, {"RESULTADO", type text}, {"MATERIAL", type text}, {"CANECA_INDICADA", type text}, {"UNIDADES_ESTIMADAS", Int64.Type}, {"PESO_ESTIMADO_KG", type number}, {"CONTEXTO_QR", type text}, {"LATENCIA_MS", Int64.Type}, {"ORIGEN_DATO", type text}}, "en-US"),
    SoloFecha = Table.TransformColumnTypes(Tipos, {{"DIA", type date}})
in
    SoloFecha

// ===== Ecopuntos  (origen: ecoaccess_bd.vw_bi_ecopuntos, cuenta de solo lectura)
let
    Origen = Oracle.Database(ServidorOracle, [Query = "SELECT * FROM ECOACCESS_BD.VW_BI_ECOPUNTOS"]),
    Encabezados = Origen,
    Tipos = Table.TransformColumnTypes(Encabezados, {{"ID_MOVIMIENTO", Int64.Type}, {"CIUDADANO", type text}, {"DIA", type datetime}, {"TIPO", type text}, {"PUNTOS", Int64.Type}, {"MATERIAL", type text}, {"TARIFA", type text}}, "en-US"),
    SoloFecha = Table.TransformColumnTypes(Tipos, {{"DIA", type date}})
in
    SoloFecha

// ===== Auditoria  (origen: ecoaccess_bd.vw_bi_auditoria, cuenta de solo lectura)
let
    Origen = Oracle.Database(ServidorOracle, [Query = "SELECT * FROM ECOACCESS_BD.VW_BI_AUDITORIA"]),
    Encabezados = Origen,
    Tipos = Table.TransformColumnTypes(Encabezados, {{"ID_AUDITORIA", Int64.Type}, {"ID_DEPOSITO", Int64.Type}, {"OPERACION", type text}, {"ESTADO_ANTERIOR", type text}, {"ESTADO_NUEVO", type text}, {"CANTIDAD_ANTERIOR", Int64.Type}, {"CANTIDAD_NUEVA", Int64.Type}, {"PESO_MEDIDO_ANTERIOR_KG", type number}, {"PESO_MEDIDO_NUEVO_KG", type number}, {"ACTOR", type text}, {"MOTIVO", type text}, {"FECHA_COLOMBIA", type datetime}}, "en-US")
in
    Tipos

// ===== Ciudadanos  (origen: ecoaccess_bd.vw_bi_ciudadanos, cuenta de solo lectura)
let
    Origen = Oracle.Database(ServidorOracle, [Query = "SELECT * FROM ECOACCESS_BD.VW_BI_CIUDADANOS"]),
    Encabezados = Origen,
    Tipos = Table.TransformColumnTypes(Encabezados, {{"CIUDADANO", type text}, {"FECHA_REGISTRO", type datetime}, {"ACTIVO", Int64.Type}, {"ES_SINTETICO", Int64.Type}, {"ORIGEN_DATO", type text}}, "en-US"),
    SoloFecha = Table.TransformColumnTypes(Tipos, {{"FECHA_REGISTRO", type date}})
in
    SoloFecha

// ===== Calendario (generado; no viene de Oracle)
let
    Ultimo = List.Max(List.Combine({Depositos[DIA], ConsultasVision[DIA], Ecopuntos[DIA]})),
    Fechas = List.Dates(#date(2026, 9, 1), Duration.Days(#date(2026, 12, 31) - #date(2026, 9, 1)) + 1, #duration(1, 0, 0, 0)),
    Tabla = Table.FromList(Fechas, Splitter.SplitByNothing(), {"Fecha"}),
    ConTipo = Table.TransformColumnTypes(Tabla, {{"Fecha", type date}}),
    Semana = Table.AddColumn(ConTipo, "Semana", each Text.From(Date.Year([Fecha])) & "-S" & Text.PadStart(Text.From(Date.WeekOfYear([Fecha], Day.Monday)), 2, "0"), type text),
    Mes = Table.AddColumn(Semana, "Mes", each Date.ToText([Fecha], "yyyy-MM"), type text),
    Ventana = Table.AddColumn(Mes, "Ventana", each let d = Duration.Days(Ultimo - [Fecha]) in
        if d < 0 then "5. Posterior al último dato" else if d = 0 then "1. Último día con datos" else if d <= 6 then "2. Días 2 a 7"
        else if d <= 29 then "3. Días 8 a 30" else "4. Más de 30 días", type text)
in
    Ventana
