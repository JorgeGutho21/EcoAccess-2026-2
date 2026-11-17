using System.Data;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccessBD.Datos;

/// <summary>
/// CRUD de empleado SIN DELETE (tarea de clase) sobre ECOACCESS_PRACTICAS.EMPLEADO_PRACTICA,
/// usando el paquete PKG_EMPLEADO_CRUD (INSERT con secuencia, SELECT con SYS_REFCURSOR, UPDATE).
/// Cada operación de escritura va en una transacción: COMMIT si sale bien, ROLLBACK si falla.
/// </summary>
public sealed class RepositorioEmpleados(string cadena)
{
    /// <summary>Si es true, la operación se deshace al final (para pruebas sin dejar datos).</summary>
    public bool SoloPrueba { get; init; }

    public int Insertar(string nombre, string email, decimal salario, int departamento)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();
        using var cmd = new OracleCommand("pkg_empleado_crud.insertar", con) { CommandType = CommandType.StoredProcedure, BindByName = true };
        cmd.Parameters.Add("p_nombre", OracleDbType.Varchar2).Value = nombre;
        cmd.Parameters.Add("p_email", OracleDbType.Varchar2).Value = email;
        cmd.Parameters.Add("p_salario", OracleDbType.Decimal).Value = salario;
        cmd.Parameters.Add("p_depto", OracleDbType.Int32).Value = departamento;
        var id = cmd.Parameters.Add("p_id", OracleDbType.Int32, ParameterDirection.Output);
        cmd.ExecuteNonQuery();
        Terminar(tx);
        return Convert.ToInt32(id.Value.ToString());
    }

    public DataTable Consultar(int? departamento)
    {
        using var con = Abrir();
        using var cmd = new OracleCommand("pkg_empleado_crud.consultar", con) { CommandType = CommandType.StoredProcedure, BindByName = true };
        cmd.Parameters.Add("resultado", OracleDbType.RefCursor, ParameterDirection.ReturnValue);
        cmd.Parameters.Add("p_depto", OracleDbType.Int32).Value = departamento.HasValue ? departamento.Value : DBNull.Value;
        var tabla = new DataTable();
        using var adaptador = new OracleDataAdapter(cmd);
        adaptador.Fill(tabla);
        return tabla;
    }

    public void Actualizar(int id, string nombre, decimal salario, int departamento)
    {
        using var con = Abrir();
        using var tx = con.BeginTransaction();
        using var cmd = new OracleCommand("pkg_empleado_crud.actualizar", con) { CommandType = CommandType.StoredProcedure, BindByName = true };
        cmd.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
        cmd.Parameters.Add("p_nombre", OracleDbType.Varchar2).Value = nombre;
        cmd.Parameters.Add("p_salario", OracleDbType.Decimal).Value = salario;
        cmd.Parameters.Add("p_depto", OracleDbType.Int32).Value = departamento;
        cmd.ExecuteNonQuery();
        Terminar(tx);
    }

    // No existe Eliminar(): la tarea pide CRUD SIN DELETE.

    private OracleConnection Abrir()
    {
        var con = new OracleConnection(cadena);
        con.Open();
        return con;
    }

    private void Terminar(OracleTransaction tx)
    {
        if (SoloPrueba) tx.Rollback(); else tx.Commit();
    }
}
