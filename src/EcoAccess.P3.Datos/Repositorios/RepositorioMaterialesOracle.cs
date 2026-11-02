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

    /// <summary>Catálogo de materiales en la tabla <c>material</c> (SQL a mano y parametrizado).</summary>
    public sealed partial class RepositorioMaterialesOracle : IRepositorioMateriales
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioMaterialesOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string Columnas = "id_material, codigo, nombre, categoria, descripcion, habilitado, sinonimos, precauciones, ruta_separacion";

        public async Task<Material?> ObtenerPorIdAsync(int idMaterial, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM material WHERE id_material = :id");
            comando.Agregar("id", OracleDbType.Int32, idMaterial);
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
            return await lector.ReadAsync(cancellationToken).ConfigureAwait(false) ? Leer(lector) : null;
        }

        public async Task<bool> ExisteCodigoAsync(string codigo, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM material WHERE codigo = :codigo");
            comando.Agregar("codigo", OracleDbType.Varchar2, codigo);
            object? cantidad = await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return ConversionOracle.Entero(cantidad!) > 0;
        }

        public async Task<IReadOnlyList<Material>> ListarAsync(bool soloHabilitados, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM material WHERE (:solo_habilitados = 0 OR habilitado = 1) ORDER BY NLSSORT(codigo, 'NLS_SORT=BINARY')");
            comando.Agregar("solo_habilitados", OracleDbType.Int32, ConversionOracle.DesdeLogico(soloHabilitados));
            using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            var materiales = new List<Material>();
            while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                materiales.Add(Leer(lector));
            }

            return materiales;
        }

        public async Task<int> AgregarAsync(Material material, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(material, nameof(material));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO material (codigo, nombre, categoria, descripcion, habilitado, sinonimos, precauciones, ruta_separacion) " +
                "VALUES (:codigo, :nombre, :categoria, :descripcion, :habilitado, :sinonimos, :precauciones, :ruta) RETURNING id_material INTO :id");
            comando.Agregar("codigo", OracleDbType.Varchar2, material.Codigo);
            comando.Agregar("nombre", OracleDbType.Varchar2, material.Nombre);
            comando.Agregar("categoria", OracleDbType.Int32, (int)material.Categoria);
            comando.Agregar("descripcion", OracleDbType.Varchar2, ConversionOracle.ParaColumnaOpcional(material.Descripcion));
            comando.Agregar("habilitado", OracleDbType.Int32, ConversionOracle.DesdeLogico(material.Habilitado));
            comando.Agregar("sinonimos", OracleDbType.Varchar2, material.Sinonimos);
            comando.Agregar("precauciones", OracleDbType.Varchar2, material.Precauciones);
            comando.Agregar("ruta", OracleDbType.Varchar2, material.RutaSeparacion);
            OracleParameter id = comando.AgregarIdSalida();

            await TraductorErroresOracle.TraducirUnicidadAsync(
                () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            return ComandosOracle.LeerId(id);
        }

        /// <summary>
        /// Habilitar no puede dejar una caneca sin materiales, así que no revalida nada; aun así bloquea la fila con
        /// <c>WAIT 10</c> para que la espera tras un <see cref="DeshabilitarAsync"/> en curso tenga límite y termine en
        /// <see cref="CambioConcurrenteException"/> en lugar de colgar la GUI.
        /// </summary>
        public async Task HabilitarAsync(int idMaterial, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await BloquearMaterialAsync(conexion, transaccion, idMaterial, cancellationToken).ConfigureAwait(false);
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE material SET habilitado = 1 WHERE id_material = :id", transaccion))
            {
                comando.Agregar("id", OracleDbType.Int32, idMaterial);
                int filas = await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                ComandosOracle.ExigirUnaFila(filas, "material", idMaterial);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        private static async Task BloquearMaterialAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idMaterial, CancellationToken cancellationToken)
        {
            using OracleCommand bloqueo = ComandosOracle.Crear(conexion,
                "SELECT id_material FROM material WHERE id_material = :id FOR UPDATE WAIT 10", transaccion);
            bloqueo.Agregar("id", OracleDbType.Int32, idMaterial);
            if (await TraductorErroresOracle.TraducirEsperaAsync(
                    () => bloqueo.ExecuteScalarAsync(cancellationToken)).ConfigureAwait(false) is null)
            {
                throw new InvalidOperationException($"No existe material con id {idMaterial}.");
            }
        }

        /// <summary>
        /// Orden de bloqueo: el material y después sus canecas, por id (el mismo que usa el repositorio de canecas,
        /// para no provocar interbloqueos). Con esas filas bloqueadas, ninguna otra sesión puede deshabilitar otro
        /// material de las mismas canecas, quitarles materiales ni ponerlas en servicio hasta que esta confirme.
        /// </summary>
        public async Task DeshabilitarAsync(int idMaterial, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            await BloquearMaterialAsync(conexion, transaccion, idMaterial, cancellationToken).ConfigureAwait(false);

            using (OracleCommand bloqueo = ComandosOracle.Crear(conexion,
                "SELECT c.id_contenedor FROM contenedor c " +
                "JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor WHERE cm.id_material = :id " +
                "ORDER BY c.id_contenedor FOR UPDATE OF c.estado WAIT 10", transaccion))
            {
                // JOIN y no IN (subconsulta): FOR UPDATE OF c.estado bloquea solo las filas de contenedor.
                bloqueo.Agregar("id", OracleDbType.Int32, idMaterial);
                // El cursor se recorre dentro del traductor: Oracle puede devolver el error de espera al leer las filas, no solo al abrirlo.
                // Recorrerlo completo garantiza que Oracle bloqueó todas las filas.
                await TraductorErroresOracle.TraducirEsperaAsync(async () =>
                {
                    using DbDataReader lector = await bloqueo.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                    while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                    }

                    return true;
                }).ConfigureAwait(false);
            }

            string? bloqueada;
            using (OracleCommand consulta = ComandosOracle.Crear(conexion,
                "SELECT c.nombre FROM contenedor c " +
                "JOIN contenedor_material cm ON cm.id_contenedor = c.id_contenedor AND cm.id_material = :id " +
                "WHERE c.estado = :en_servicio AND NOT EXISTS (" +
                "SELECT 1 FROM contenedor_material o JOIN material m ON m.id_material = o.id_material " +
                "WHERE o.id_contenedor = c.id_contenedor AND o.id_material <> :id_otro AND m.habilitado = 1) " +
                "ORDER BY c.id_contenedor FETCH FIRST 1 ROWS ONLY", transaccion))
            {
                consulta.Agregar("id", OracleDbType.Int32, idMaterial);
                consulta.Agregar("en_servicio", OracleDbType.Int32, (int)EstadoContenedor.EnServicio);
                consulta.Agregar("id_otro", OracleDbType.Int32, idMaterial);
                bloqueada = await consulta.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false) as string;
            }

            if (bloqueada is not null)
            {
                // Sin token: el rollback debe ocurrir aunque la operación se haya cancelado.
                await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
                throw new CambioConcurrenteException(MensajesCatalogo.UnicoMaterialDeCanecaEnServicio(bloqueada));
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE material SET habilitado = 0 WHERE id_material = :id", transaccion))
            {
                comando.Agregar("id", OracleDbType.Int32, idMaterial);
                await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        private static Material Leer(DbDataReader lector) => new(
            ConversionOracle.Entero(lector.GetValue(0)),
            lector.GetString(1),
            lector.GetString(2),
            (CategoriaMaterial)ConversionOracle.Entero(lector.GetValue(3)),
            ConversionOracle.TextoOpcional(lector.GetValue(4)),
            ConversionOracle.Logico(lector.GetValue(5)),
            ConversionOracle.TextoOpcional(lector.GetValue(6)),
            ConversionOracle.TextoOpcional(lector.GetValue(7)),
            ConversionOracle.TextoOpcional(lector.GetValue(8)));
    }
}
