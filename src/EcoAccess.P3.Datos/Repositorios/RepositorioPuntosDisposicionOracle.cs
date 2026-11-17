using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Datos.Oracle;
using EcoAccess.P3.Logica.Servicios;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Repositorios
{
    /// <summary>
    /// Lee PUNTO_DISPOSICION (V026). Solo lectura desde el bot; la tabla se mantiene con la migración y su actualización
    /// periódica. Una ruta desconocida en la columna RUTAS se ignora en vez de inventar a qué corresponde.
    /// </summary>
    public sealed class RepositorioPuntosDisposicionOracle : IRepositorioPuntosDisposicion
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioPuntosDisposicionOracle(FabricaConexionesOracle fabrica) { _fabrica = fabrica ?? throw new ArgumentNullException(nameof(fabrica)); }

        public async Task<IReadOnlyList<PuntoDisposicion>> ListarAsync(string municipio, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT codigo, nombre, direccion, municipio, rutas, recibe, estado, fuente, fecha_verificacion, horario " +
                "FROM punto_disposicion WHERE municipio = :municipio AND activo = 1 ORDER BY codigo");
            comando.Agregar("municipio", OracleDbType.Varchar2, municipio);
            var puntos = new List<PuntoDisposicion>();
            using var lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                var rutas = ((string)lector["rutas"]).Split(',')
                    .Select(r => Enum.TryParse(r.Trim(), out RutaDisposicion ruta) ? ruta : (RutaDisposicion?)null)
                    .Where(r => r is not null).Select(r => r!.Value).ToArray();
                puntos.Add(new PuntoDisposicion((string)lector["codigo"], (string)lector["nombre"], ConversionOracle.TextoOpcional(lector["direccion"]),
                    (string)lector["municipio"], rutas, (string)lector["recibe"], Estado((string)lector["estado"]), (string)lector["fuente"],
                    DateTime.SpecifyKind((DateTime)lector["fecha_verificacion"], DateTimeKind.Utc), ConversionOracle.TextoOpcional(lector["horario"])));
            }
            return puntos;
        }

        private static EstadoVerificacionPunto Estado(string estado) => estado switch
        {
            "CONFIRMADO" => EstadoVerificacionPunto.Confirmado,
            "SOLO_PROGRAMA" => EstadoVerificacionPunto.SoloPrograma,
            _ => EstadoVerificacionPunto.ListadoSinConfirmar,
        };
    }
}
