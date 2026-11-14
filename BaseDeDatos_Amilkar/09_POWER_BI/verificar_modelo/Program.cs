using Microsoft.AnalysisServices.AdomdClient;
using Tom = Microsoft.AnalysisServices.Tabular;

// Verificador del modelo Power BI abierto en Power BI Desktop (motor local msmdsrv).
// Uso: PbiCheck <puerto> [refrescar]
var puerto = args[0];
var cad = $"Data Source=localhost:{puerto}";
var srv = new Tom.Server();
srv.Connect(cad);
var db = srv.Databases[0];
Console.WriteLine($"Base: {db.Name} · tablas: {db.Model.Tables.Count} · relaciones: {db.Model.Relationships.Count} · medidas: {db.Model.Tables.Sum(t => t.Measures.Count)}");
if (args.Length > 1)
{
    try
    {
        db.Model.RequestRefresh(Tom.RefreshType.Full);
        db.Model.SaveChanges();
        Console.WriteLine("REFRESCO: correcto");
    }
    catch (Exception ex) { Console.WriteLine("REFRESCO FALLÓ: " + ex.Message); }
}
foreach (var t in db.Model.Tables)
    foreach (var p in t.Partitions)
        Console.WriteLine($"  Partición {t.Name}: estado {p.State} {(string.IsNullOrEmpty(p.ErrorMessage) ? "" : "ERROR " + p.ErrorMessage)}");
foreach (var t in db.Model.Tables)
    foreach (var m in t.Measures)
        if (!string.IsNullOrEmpty(m.ErrorMessage)) Console.WriteLine($"  MEDIDA CON ERROR {t.Name}[{m.Name}]: {m.ErrorMessage}");
foreach (var r in db.Model.Relationships.OfType<Tom.SingleColumnRelationship>())
    Console.WriteLine($"  Relación {r.FromTable.Name}[{r.FromColumn.Name}] -> {r.ToTable.Name}[{r.ToColumn.Name}] activa={r.IsActive}");
srv.Disconnect();

using var cn = new AdomdConnection(cad);
cn.Open();
void Consulta(string titulo, string dax)
{
    Console.WriteLine($"== {titulo}");
    try
    {
        using var cmd = new AdomdCommand(dax, cn);
        using var rd = cmd.ExecuteReader();
        var cols = Enumerable.Range(0, rd.FieldCount).Select(rd.GetName).ToArray();
        Console.WriteLine("   " + string.Join(" | ", cols.Select(c => c.Replace("[", "").Replace("]", ""))));
        int n = 0;
        while (rd.Read() && n++ < 12)
            Console.WriteLine("   " + string.Join(" | ", Enumerable.Range(0, rd.FieldCount).Select(i => rd.IsDBNull(i) ? "(vacío)" : Convert.ToString(rd.GetValue(i), System.Globalization.CultureInfo.InvariantCulture))));
    }
    catch (Exception ex) { Console.WriteLine("   ERROR: " + ex.Message); }
}
Consulta("Filas por tabla", "EVALUATE { (\"Depositos\", COUNTROWS(Depositos)), (\"ConsultasVision\", COUNTROWS(ConsultasVision)), (\"Ecopuntos\", COUNTROWS(Ecopuntos)), (\"Auditoria\", COUNTROWS(Auditoria)), (\"Ciudadanos\", COUNTROWS(Ciudadanos)), (\"Calendario\", COUNTROWS(Calendario)) }");
Consulta("Tarjetas de Inicio", "EVALUATE ROW(\"Fotos\", [Fotos consultadas], \"Registrados\", [Depósitos registrados], \"Verificados\", [Depósitos verificados], \"Pendientes\", [Depósitos pendientes], \"Rechazados\", [Depósitos rechazados], \"EcoPuntosNetos\", [EcoPuntos netos], \"Acreditados\", [EcoPuntos acreditados], \"Revertidos\", [EcoPuntos revertidos])");
Consulta("Pesos (estimado y medido por separado)", "EVALUATE ROW(\"Unidades\", [Unidades verificadas], \"KgEstimados\", [Kg estimados (verificados)], \"KgMedidos\", [Kg medidos (verificados)], \"ConPesoMedido\", [Verificados con peso medido], \"CiudadanosConVerif\", [Ciudadanos con depósito verificado], \"CiudadanosSinVerif\", [Ciudadanos sin depósito verificado])");
Consulta("Verificados por caneca", "EVALUATE SUMMARIZECOLUMNS(Depositos[CANECA], \"Verificados\", [Depósitos verificados]) ORDER BY [Verificados] DESC");
Consulta("Ventana de tiempo", "EVALUATE SUMMARIZECOLUMNS(Calendario[Ventana], \"Fotos\", [Fotos consultadas], \"Depositos\", [Depósitos registrados], \"Verificados\", [Depósitos verificados]) ORDER BY Calendario[Ventana]");
Consulta("Últimos 7 días (tramos 1 y 2): TOP ciudadanos verificados", "EVALUATE TOPN(5, CALCULATETABLE(SUMMARIZECOLUMNS(Depositos[CIUDADANO], \"Verificados\", [Depósitos verificados], \"Unidades\", [Unidades verificadas]), Calendario[Ventana] IN {\"1. Último día con datos\", \"2. Días 2 a 7\"}), [Verificados], DESC)");
Consulta("Por semana", "EVALUATE SUMMARIZECOLUMNS(Calendario[Semana], \"Fotos\", [Fotos consultadas], \"Depositos\", [Depósitos registrados]) ORDER BY Calendario[Semana]");
Consulta("EcoPuntos por material", "EVALUATE TOPN(5, SUMMARIZECOLUMNS(Ecopuntos[MATERIAL], \"Puntos\", [EcoPuntos netos]), [Puntos], DESC)");
Consulta("Rótulo", "EVALUATE ROW(\"Rotulo\", [Rótulo datos])");
