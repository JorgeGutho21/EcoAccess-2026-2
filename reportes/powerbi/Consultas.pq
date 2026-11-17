// Consultas de Power Query del informe. Cree una consulta en blanco por bloque, con el nombre indicado.
// Credenciales: diálogo del conector Oracle, con la cuenta ECOACCESS_P3_INFORMES. Nunca en este archivo.

// ===== Parámetros (Administrar parámetros) =====
// Servidor          texto    localhost:1522/PRUEBAP3
// Esquema           texto    ECOACCESS_P3_APP
// SoloDemostracion  lógico   false = datos operativos; true = demostración y simulaciones. No mezcle ambos.

// ===== Depositos =====
let
    Datos = ImportarOracle(Servidor, Esquema, "EA_R_BASE_DEPOSITOS", SoloDemostracion),
    Tipos = Table.TransformColumnTypes(Datos, {{"DIA", type date}, {"ID_CONTENEDOR", Int64.Type}, {"ID_PUNTO_INSTALACION", Int64.Type}, {"DEPOSITOS", Int64.Type}, {"VERIFICADOS", Int64.Type}, {"CON_PESO_MEDIDO", Int64.Type}, {"UNIDADES_VERIFICADAS", Int64.Type}, {"VERIFICADOS_SIN_CONTEO", Int64.Type}, {"KG_MEDIDOS", type number}}),
    // CIUDADANOS_DEL_DIA no es aditivo y se retira: los únicos por mes vienen de Participacion.
    SinNoAditivo = Table.RemoveColumns(Tipos, {"CIUDADANOS_DEL_DIA", "CANECA", "UBICACION", "ES_ENSAYO"})
in
    SinNoAditivo

// ===== Puntos =====
let
    Datos = ImportarOracle(Servidor, Esquema, "EA_R_BASE_PUNTOS", SoloDemostracion),
    Tipos = Table.TransformColumnTypes(Datos, {{"DIA", type date}, {"ID_CONTENEDOR", Int64.Type}, {"ID_PUNTO_INSTALACION", Int64.Type}, {"ACREDITADOS", Int64.Type}, {"REVERSOS", Int64.Type}, {"NETOS", Int64.Type}}),
    Columnas = Table.RemoveColumns(Tipos, {"CANECA", "UBICACION", "ES_ENSAYO"})
in
    Columnas

// ===== Solicitudes =====
let
    Datos = ImportarOracle(Servidor, Esquema, "EA_R_SOLICITUDES", SoloDemostracion),
    Tipos = Table.TransformColumnTypes(Datos, {{"DIA", type date}, {"ID_CONTENEDOR", Int64.Type}, {"ID_PUNTO_INSTALACION", Int64.Type}, {"SOLICITUDES", Int64.Type}}),
    Columnas = Table.RemoveColumns(Tipos, {"CANECA", "UBICACION", "ES_ENSAYO"})
in
    Columnas

// ===== Opiniones (globales: sin relación con caneca ni ubicación) =====
let
    Datos = ImportarOracle(Servidor, Esquema, "EA_R_OPINIONES", SoloDemostracion),
    Tipos = Table.TransformColumnTypes(Datos, {{"DIA", type date}, {"OPINIONES", Int64.Type}, {"SUMA_ESTRELLAS", Int64.Type}, {"INVITADAS", Int64.Type}})
in
    Tipos

// ===== Participacion =====
let
    Datos = ImportarOracle(Servidor, Esquema, "EA_R_PARTICIPACION_MENSUAL", SoloDemostracion),
    Tipos = Table.TransformColumnTypes(Datos, {{"MES", type date}, {"CIUDADANOS_UNICOS", Int64.Type}, {"DEPOSITOS", Int64.Type}}),
    Columnas = Table.RemoveColumns(Tipos, {"ES_ENSAYO"})
in
    Columnas

// ===== DimMaterial =====
let
    Datos = ImportarOracle(Servidor, Esquema, "EA_R_MATERIALES", null),
    Tipos = Table.TransformColumnTypes(Datos, {{"MATERIAL", type text}, {"NOMBRE", type text}, {"CATEGORIA", type text}, {"HABILITADO", Int64.Type}})
in
    Tipos

// ===== DimCaneca (solo canecas con actividad o registradas en las vistas) =====
let
    Unidas = Table.Combine({
        Table.SelectColumns(ImportarOracle(Servidor, Esquema, "EA_R_BASE_DEPOSITOS", SoloDemostracion), {"ID_CONTENEDOR", "CANECA"}),
        Table.SelectColumns(ImportarOracle(Servidor, Esquema, "EA_R_BASE_PUNTOS", SoloDemostracion), {"ID_CONTENEDOR", "CANECA"}),
        Table.SelectColumns(ImportarOracle(Servidor, Esquema, "EA_R_SOLICITUDES", SoloDemostracion), {"ID_CONTENEDOR", "CANECA"})}),
    Distintas = Table.Distinct(Unidas, {"ID_CONTENEDOR"}),
    Tipos = Table.TransformColumnTypes(Distintas, {{"ID_CONTENEDOR", Int64.Type}, {"CANECA", type text}})
in
    Tipos

// ===== DimUbicacion =====
let
    Unidas = Table.Combine({
        Table.SelectColumns(ImportarOracle(Servidor, Esquema, "EA_R_BASE_DEPOSITOS", SoloDemostracion), {"ID_PUNTO_INSTALACION", "UBICACION"}),
        Table.SelectColumns(ImportarOracle(Servidor, Esquema, "EA_R_BASE_PUNTOS", SoloDemostracion), {"ID_PUNTO_INSTALACION", "UBICACION"}),
        Table.SelectColumns(ImportarOracle(Servidor, Esquema, "EA_R_SOLICITUDES", SoloDemostracion), {"ID_PUNTO_INSTALACION", "UBICACION"})}),
    Distintas = Table.Distinct(Unidas, {"ID_PUNTO_INSTALACION"}),
    Tipos = Table.TransformColumnTypes(Distintas, {{"ID_PUNTO_INSTALACION", Int64.Type}, {"UBICACION", type text}})
in
    Tipos

// ===== ExpansionUPC (PROYECCIÓN FUTURA: texto fijo, sin conexión a hechos ni cifras) =====
let
    Filas = #table(type table [Ubicacion = text, Estado = text, Canecas = text, Integracion = text],
        List.Transform({"Bloque A", "Bloque B", "Bloque C", "Bloque H", "Bloque I", "Bloque P", "Comedor universitario"},
            each {_, "Proyección futura: sin canecas instaladas", "Pendiente de planificación", "QR, Telegram, reconocimiento visual, Oracle y estadísticas"}))
in
    Filas
