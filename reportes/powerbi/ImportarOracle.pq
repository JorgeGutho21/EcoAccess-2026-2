// Crear cuatro consultas con esta función. Credenciales: diálogo del conector Oracle, nunca este archivo.
// Ejemplo: ImportarOracle("localhost:1522/PRUEBAP3", "ECOACCESS_P3_APP", "EA_R_DEPOSITOS")
(Servidor as text, Esquema as text, Vista as text, optional SoloDemostracion as nullable logical) as table =>
let
    Permitidas = {"EA_R_DEPOSITOS", "EA_R_PUNTOS", "EA_R_OPINIONES", "EA_R_BASE_DEPOSITOS", "EA_R_BASE_PUNTOS", "EA_R_SOLICITUDES"},
    Nombre = Text.Upper(Vista),
    Origen = Oracle.Database(Servidor, [HierarchicalNavigation=true]),
    Tablas = Origen{[Schema=Text.Upper(Esquema)]}[Data],
    Datos = if List.Contains(Permitidas, Nombre)
        then Tablas{[Name=Nombre]}[Data]
        else error "Vista de informe no permitida",
    Ensayo = if SoloDemostracion = true then 1 else 0,
    Filtrados = if SoloDemostracion = true and List.Contains({"EA_R_DEPOSITOS", "EA_R_PUNTOS"}, Nombre)
        then error "Para demostración utilice EA_R_BASE_DEPOSITOS o EA_R_BASE_PUNTOS"
        else if Table.HasColumns(Datos, "ES_ENSAYO")
        then Table.SelectRows(Datos, each [ES_ENSAYO] = Ensayo) else Datos
in
    Filtrados
