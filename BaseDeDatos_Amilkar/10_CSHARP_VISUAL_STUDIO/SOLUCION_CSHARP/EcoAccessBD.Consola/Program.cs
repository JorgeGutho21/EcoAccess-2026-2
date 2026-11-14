using System.Data;
using EcoAccessBD.Datos;
using Oracle.ManagedDataAccess.Client;

// Prueba de integración C# ↔ Oracle (sin contraseñas en el código).
// Variables de entorno necesarias:
//   LECTOR_USUARIO / LECTOR_CLAVE         cuenta ECOACCESS_BD_LECTOR (solo vistas)
//   PRACTICAS_USUARIO / PRACTICAS_CLAVE   cuenta ECOACCESS_PRACTICAS (CRUD empleado)
//   *_DATASOURCE (opcional)               por defecto localhost:1521/FREEPDB1
int fallas = 0;
void Ver(string nombre, bool ok, string detalle = "")
{
    Console.WriteLine($"{(ok ? "PASA " : "FALLA")} {nombre} {detalle}");
    if (!ok) fallas++;
}
void Mostrar(DataTable t)
{
    Console.WriteLine("    " + string.Join(" | ", t.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));
    foreach (DataRow r in t.Rows) Console.WriteLine("    " + string.Join(" | ", r.ItemArray));
}

// ---------- 1. Lector de informes (Power BI / dashboards) ----------
string lector = Conexion.Crear("LECTOR");
Console.WriteLine($"== Conectado como {Conexion.Describir(lector)} ==");
var informes = new RepositorioInformes(lector);
var ranking = informes.RankingVerificados(7);
Ver("Ranking de verificados (7 días) devuelve filas", ranking.Rows.Count > 0, $"({ranking.Rows.Count} filas)");
Mostrar(ranking);
var resumen = informes.ResumenPorEstado();
Ver("Resumen separa fotos y depósitos", resumen.Rows.Count >= 2);
Mostrar(resumen);
try
{
    using var con = new OracleConnection(lector);
    con.Open();
    using var cmd = new OracleCommand("SELECT COUNT(*) FROM ecoaccess_bd.usuario", con);
    cmd.ExecuteScalar();
    Ver("El lector NO puede leer la tabla USUARIO (mínimo privilegio)", false, "-> pudo leerla");
}
catch (OracleException ex) when (ex.Number == 942)
{
    Ver("El lector NO puede leer la tabla USUARIO (mínimo privilegio)", true, "-> ORA-00942 como se esperaba");
}

// ---------- 2. CRUD de empleado SIN DELETE ----------
string practicas = Conexion.Crear("PRACTICAS");
Console.WriteLine($"== Conectado como {Conexion.Describir(practicas)} ==");
var repo = new RepositorioEmpleados(practicas);
int antes = repo.Consultar(60).Rows.Count;
Ver("SELECT: departamento 60 tiene empleados", antes > 0, $"({antes})");

// Insertar y actualizar de verdad (COMMIT), luego comprobar.
// Para no acumular filas de prueba (la práctica NO permite DELETE), la fila de prueba de la consola
// (email PRUEBACS) se crea con COMMIT solo la primera vez; en las ejecuciones siguientes se reutiliza
// y el INSERT se vuelve a comprobar dentro de una transacción que termina en ROLLBACK (paso final).
var previa = antes > 0 ? repo.Consultar(60).Rows.Cast<DataRow>()
    .Where(r => string.Equals(Convert.ToString(r["EMAIL"]), "PRUEBACS", StringComparison.OrdinalIgnoreCase))
    .Select(r => Convert.ToInt32(r["ID_EMPLEADO"])).DefaultIfEmpty(0).Min() : 0;
int id;
if (previa == 0)
{
    id = repo.Insertar("Empleado prueba consola C#", "PRUEBACS", 4100m, 60);
    Ver("INSERT con COMMIT: id generado por la secuencia (>= 1000)", id >= 1000, $"(id {id}, primera ejecución)");
}
else
{
    id = previa;
    Ver("INSERT con COMMIT ya hecho en una ejecución anterior: se reutiliza la fila de prueba", id >= 1000, $"(id {id}, no se crea otra)");
}
repo.Actualizar(id, "Empleado prueba consola C# (actualizado)", 4100m, 60);
repo.Actualizar(id, "Empleado prueba consola C# (actualizado)", 4500m, 60);
var despues = repo.Consultar(60);
var fila = despues.Rows.Cast<DataRow>().FirstOrDefault(r => Convert.ToInt32(r["ID_EMPLEADO"]) == id);
Ver("UPDATE: el salario quedó en 4500", fila != null && Convert.ToDecimal(fila["SALARIO"]) == 4500m);
Ver("El repositorio no tiene método Eliminar (sin DELETE)",
    typeof(RepositorioEmpleados).GetMethods().All(m => !m.Name.StartsWith("Elimin", StringComparison.OrdinalIgnoreCase)));
try
{
    repo.Actualizar(99999, "Nadie", 1m, 60);
    Ver("UPDATE de un id inexistente lanza ORA-20002", false);
}
catch (OracleException ex) when (ex.Number == 20002)
{
    Ver("UPDATE de un id inexistente lanza ORA-20002", true);
}
var prueba = new RepositorioEmpleados(practicas) { SoloPrueba = true };
int idTemporal = prueba.Insertar("Fila que se deshace", "temp", 1000m, 60);
Ver("INSERT dentro de la transacción: id generado por la secuencia (>= 1000)", idTemporal >= 1000, $"(id {idTemporal})");
Ver("Transacción con ROLLBACK no deja la fila", repo.Consultar(60).Rows.Cast<DataRow>().All(r => Convert.ToInt32(r["ID_EMPLEADO"]) != idTemporal));

Console.WriteLine($"== Resultado: {(fallas == 0 ? "TODO PASA" : fallas + " FALLAS")} ==");
return fallas == 0 ? 0 : 1;
