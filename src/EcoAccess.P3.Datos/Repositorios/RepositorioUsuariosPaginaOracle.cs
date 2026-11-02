using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Data.Common;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{

    public sealed partial class RepositorioUsuariosOracle
    {
        public async Task<PaginaUsuarios> ConsultarPaginaAsync(FiltroUsuarios filtro, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(filtro, nameof(filtro));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            // Limitar cuentas ANTES de unir sus roles: una cuenta multirrol nunca se divide entre páginas.
            using OracleCommand comando = ComandosOracle.Crear(conexion, $@"WITH pagina AS (
    SELECT {Columnas} FROM usuario u
    WHERE (:texto IS NULL OR INSTR(LOWER(u.nombre), :texto) > 0 OR INSTR(LOWER(u.correo_electronico), :texto) > 0)
      AND (:activo IS NULL OR u.activo = :activo)
      AND (:rol IS NULL OR EXISTS (
        SELECT 1 FROM usuario_rol ur JOIN rol r ON r.id_rol=ur.id_rol
        WHERE ur.id_usuario=u.id_usuario AND r.codigo=:rol))
    ORDER BY NLSSORT(u.nombre, 'NLS_SORT=BINARY'), u.id_usuario
    OFFSET :desplazamiento ROWS FETCH NEXT :limite ROWS ONLY)
SELECT {Columnas}, r.codigo FROM pagina u
LEFT JOIN usuario_rol ur ON ur.id_usuario=u.id_usuario
LEFT JOIN rol r ON r.id_rol=ur.id_rol
ORDER BY NLSSORT(u.nombre, 'NLS_SORT=BINARY'), u.id_usuario, r.codigo");
            comando.Agregar("texto", OracleDbType.Varchar2, filtro.Texto.Length == 0 ? DBNull.Value : filtro.Texto);
            comando.Agregar("activo", OracleDbType.Int32, filtro.Activo is bool activo ? (activo ? 1 : 0) : DBNull.Value);
            comando.Agregar("rol", OracleDbType.Int32, filtro.Rol is CodigoRol rol ? (int)rol : DBNull.Value);
            comando.Agregar("desplazamiento", OracleDbType.Int64, filtro.Desplazamiento);
            comando.Agregar("limite", OracleDbType.Int32, filtro.TamanoPagina + 1);
            var cuentas = new List<Usuario>();
            Usuario? actual = null;
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                int id = ConversionOracle.Entero(lector.GetValue(0));
                CodigoRol codigo = (CodigoRol)ConversionOracle.Entero(lector.GetValue(7));
                if (actual?.Id == id)
                {
                    actual.AsignarRol(codigo);
                    continue;
                }
                actual = new Usuario(id, lector.GetString(1), lector.GetString(2),
                    ConversionOracle.FechaUtc(lector.GetValue(3)), ConversionOracle.Logico(lector.GetValue(4)),
                    new CodigoRol[] { codigo }, ConversionOracle.TextoOpcional(lector.GetValue(5)),
                    ConversionOracle.FechaUtcOpcional(lector.GetValue(6)));
                cuentas.Add(actual);
            }
            bool siguiente = cuentas.Count > filtro.TamanoPagina;
            if (siguiente) cuentas.RemoveAt(cuentas.Count - 1);
            return new PaginaUsuarios(cuentas, filtro.Pagina, filtro.TamanoPagina, siguiente);
        }
    }
}
