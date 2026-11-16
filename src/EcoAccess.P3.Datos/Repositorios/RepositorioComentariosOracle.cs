using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed class RepositorioComentariosOracle : IRepositorioComentarios
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioComentariosOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        public async Task<bool> AgregarAsync(ComentarioCiudadano comentario, CancellationToken ct = default)
        {
            using var conexion = await _fabrica.AbrirAsync(ct);
            using var comando = ComandosOracle.Crear(conexion,
                "INSERT INTO comentario_ciudadano(clave,id_usuario,participante,tipo,categoria,comentario,id_interaccion,fecha_utc) " +
                "VALUES(:clave,:usuario,:participante,:tipo,:categoria,:comentario,:interaccion,:fecha)");
            comando.Agregar("clave", OracleDbType.Varchar2, comentario.Clave);
            comando.Agregar("usuario", OracleDbType.Int32, comentario.IdUsuario);
            comando.Agregar("participante", OracleDbType.Varchar2, comentario.Participante);
            comando.Agregar("tipo", OracleDbType.Int32, (int)comentario.Tipo);
            comando.Agregar("categoria", OracleDbType.Int32, (int)comentario.Categoria);
            comando.Agregar("comentario", OracleDbType.Varchar2, comentario.Comentario);
            comando.Agregar("interaccion", OracleDbType.Int32, comentario.IdInteraccion);
            comando.Agregar("fecha", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(comentario.FechaUtc));
            try { await comando.ExecuteNonQueryAsync(ct); return true; }
            catch (OracleException error) when (error.Number == 1) { return false; }
        }
    }
}
