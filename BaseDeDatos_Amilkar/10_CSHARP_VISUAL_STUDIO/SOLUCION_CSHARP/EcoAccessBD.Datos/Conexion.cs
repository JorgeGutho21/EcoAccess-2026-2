using Oracle.ManagedDataAccess.Client;

namespace EcoAccessBD.Datos;

/// <summary>
/// Arma la cadena de conexión SIN contraseñas en el código ni en archivos del repositorio.
/// Lee tres variables de entorno (o las de un perfil distinto con un prefijo):
///   ECOACCESS_DATASOURCE  por defecto localhost:1521/FREEPDB1 (Oracle local)
///   ECOACCESS_USUARIO     por ejemplo ECOACCESS_BD_LECTOR o ECOACCESS_PRACTICAS
///   ECOACCESS_CLAVE       la contraseña (solo en tu PC: variable de entorno o User Secrets)
/// Para Oracle Cloud: DataSource = alias del wallet (por ejemplo ecoaccess_tp) y la
/// variable TNS_ADMIN apuntando a la carpeta del wallet descomprimido.
/// </summary>
public static class Conexion
{
    public static string Crear(string prefijo = "ECOACCESS")
    {
        string fuente = Environment.GetEnvironmentVariable($"{prefijo}_DATASOURCE") ?? "localhost:1521/FREEPDB1";
        string usuario = Environment.GetEnvironmentVariable($"{prefijo}_USUARIO")
            ?? throw new InvalidOperationException($"Falta la variable de entorno {prefijo}_USUARIO.");
        string clave = Environment.GetEnvironmentVariable($"{prefijo}_CLAVE")
            ?? throw new InvalidOperationException($"Falta la variable de entorno {prefijo}_CLAVE.");
        var b = new OracleConnectionStringBuilder
        {
            DataSource = fuente,
            UserID = usuario,
            Password = clave,
            ConnectionTimeout = 15,
        };
        return b.ConnectionString;
    }

    /// <summary>Texto seguro para mostrar: nunca incluye la contraseña.</summary>
    public static string Describir(string cadena)
    {
        var b = new OracleConnectionStringBuilder(cadena);
        return $"{b.UserID}@{b.DataSource}";
    }
}
