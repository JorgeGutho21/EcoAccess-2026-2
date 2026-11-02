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

    /// <summary>
    /// Canecas en <c>contenedor</c> y sus materiales admitidos en <c>contenedor_material</c>. Cada caneca tiene
    /// un solo compartimento; la lista de materiales se guarda en la misma transacción que la fila.
    /// Después del alta no se reescribe la caneca entera: cada cambio bloquea su fila y toca solo lo suyo.
    /// </summary>
    public sealed class RepositorioContenedoresOracle : IRepositorioContenedores
    {
        private readonly FabricaConexionesOracle _fabrica;

        public RepositorioContenedoresOracle(FabricaConexionesOracle fabrica)
        {
            _fabrica = fabrica;
        }

        private const string Columnas =
            "c.id_contenedor, c.id_punto_instalacion, c.nombre, c.identificador_qr, c.fecha_alta_utc, c.estado";

        private const string SqlInsertarMaterial =
            "INSERT INTO contenedor_material (id_contenedor, id_material) VALUES (:id, :material)";

        // Filtros fijos: el texto del SQL es siempre una de estas constantes y el valor va como parámetro :valor.
        private const string SinFiltro = "1 = 1";
        private const string FiltroId = "c.id_contenedor = :valor";
        private const string FiltroQr = "c.identificador_qr = :valor";
        private const string FiltroPunto = "c.id_punto_instalacion = :valor";
        private const string FiltroMaterial =
            "EXISTS (SELECT 1 FROM contenedor_material f WHERE f.id_contenedor = c.id_contenedor AND f.id_material = :valor)";

        public async Task<Contenedor?> ObtenerPorIdAsync(int idContenedor, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Contenedor> encontrados = await ListarConFiltroAsync(FiltroId, idContenedor, cancellationToken).ConfigureAwait(false);
            return encontrados.Count == 0 ? null : encontrados[0];
        }

        public async Task<Contenedor?> ObtenerPorIdentificadorQrAsync(string identificadorQr, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(identificadorQr, nameof(identificadorQr));
            IReadOnlyList<Contenedor> encontrados = await ListarConFiltroAsync(FiltroQr, identificadorQr, cancellationToken).ConfigureAwait(false);
            return encontrados.Count == 0 ? null : encontrados[0];
        }

        public Task<IReadOnlyList<Contenedor>> ListarAsync(CancellationToken cancellationToken = default) =>
            ListarConFiltroAsync(SinFiltro, null, cancellationToken);

        public Task<IReadOnlyList<Contenedor>> ListarPorPuntoAsync(int idPunto, CancellationToken cancellationToken = default) =>
            ListarConFiltroAsync(FiltroPunto, idPunto, cancellationToken);

        public Task<IReadOnlyList<Contenedor>> ListarPorMaterialAsync(int idMaterial, CancellationToken cancellationToken = default) =>
            ListarConFiltroAsync(FiltroMaterial, idMaterial, cancellationToken);

        public async Task<int> AgregarAsync(Contenedor contenedor, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(contenedor, nameof(contenedor));
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO contenedor (id_punto_instalacion, nombre, identificador_qr, fecha_alta_utc, estado) " +
                "VALUES (:id_punto, :nombre, :qr, :fecha_alta, :estado) RETURNING id_contenedor INTO :id", transaccion);
            comando.Agregar("id_punto", OracleDbType.Int32, contenedor.IdPuntoInstalacion);
            comando.Agregar("nombre", OracleDbType.Varchar2, contenedor.Nombre);
            comando.Agregar("qr", OracleDbType.Varchar2, contenedor.IdentificadorQr);
            comando.Agregar("fecha_alta", OracleDbType.TimeStamp, ConversionOracle.ParaColumnaUtc(contenedor.FechaAltaUtc));
            comando.Agregar("estado", OracleDbType.Int32, (int)contenedor.Estado);
            OracleParameter parametroId = comando.AgregarIdSalida();

            await TraductorErroresOracle.TraducirUnicidadAsync(
                () => comando.ExecuteNonQueryAsync(cancellationToken)).ConfigureAwait(false);
            int id = ComandosOracle.LeerId(parametroId);

            foreach (int idMaterial in contenedor.MaterialesAdmitidos)
            {
                await CambiarMaterialAsync(conexion, transaccion, SqlInsertarMaterial, id, idMaterial, cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
            return id;
        }

        /// <summary>Bloquea primero el material y después la caneca: el mismo orden que <c>DeshabilitarAsync</c> de materiales.</summary>
        public async Task AdmitirMaterialAsync(int idContenedor, int idMaterial, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            string nombreMaterial;
            bool habilitado;
            using (OracleCommand bloqueo = ComandosOracle.Crear(conexion,
                "SELECT nombre, habilitado FROM material WHERE id_material = :id FOR UPDATE WAIT 10", transaccion))
            {
                bloqueo.Agregar("id", OracleDbType.Int32, idMaterial);
                using DbDataReader lector = await TraductorErroresOracle.TraducirEsperaAsync(
                    () => bloqueo.ExecuteReaderAsync(cancellationToken)).ConfigureAwait(false);
                if (!await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    throw new InvalidOperationException($"No existe material con id {idMaterial}.");
                }

                nombreMaterial = lector.GetString(0);
                habilitado = ConversionOracle.Logico(lector.GetValue(1));
            }

            EstadoContenedor estado = await BloquearAsync(conexion, transaccion, idContenedor, cancellationToken).ConfigureAwait(false);
            if (estado == EstadoContenedor.DadoDeBaja)
            {
                await RechazarAsync(transaccion, MensajesCatalogo.CanecaDadaDeBaja).ConfigureAwait(false);
            }

            if (!habilitado)
            {
                await RechazarAsync(transaccion, MensajesCatalogo.MaterialDeshabilitado(nombreMaterial)).ConfigureAwait(false);
            }

            // Con la caneca bloqueada nadie más inserta este par; si ya estaba, no se repite.
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "INSERT INTO contenedor_material (id_contenedor, id_material) SELECT :id, :material FROM dual " +
                "WHERE NOT EXISTS (SELECT 1 FROM contenedor_material WHERE id_contenedor = :id_existe AND id_material = :material_existe)",
                transaccion))
            {
                comando.Agregar("id", OracleDbType.Int32, idContenedor);
                comando.Agregar("material", OracleDbType.Int32, idMaterial);
                comando.Agregar("id_existe", OracleDbType.Int32, idContenedor);
                comando.Agregar("material_existe", OracleDbType.Int32, idMaterial);
                await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task QuitarMaterialAsync(int idContenedor, int idMaterial, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            EstadoContenedor estado = await BloquearAsync(conexion, transaccion, idContenedor, cancellationToken).ConfigureAwait(false);
            if (estado == EstadoContenedor.DadoDeBaja)
            {
                await RechazarAsync(transaccion, MensajesCatalogo.CanecaDadaDeBaja).ConfigureAwait(false);
            }

            await CambiarMaterialAsync(conexion, transaccion, "DELETE FROM contenedor_material WHERE id_contenedor = :id AND id_material = :material",
                idContenedor, idMaterial, cancellationToken).ConfigureAwait(false);

            if (estado == EstadoContenedor.EnServicio
                && await ContarHabilitadosAsync(conexion, transaccion, idContenedor, cancellationToken).ConfigureAwait(false) == 0)
            {
                await RechazarAsync(transaccion, MensajesCatalogo.CanecaQuedariaSinMateriales).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task CambiarEstadoAsync(int idContenedor, EstadoContenedor estado, CancellationToken cancellationToken = default)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);
            using var transaccion = (OracleTransaction)await conexion.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);

            // Poner en servicio exige el punto activo: se bloquea antes que la caneca (orden punto → material → caneca),
            // para que CambiarActivoAsync de puntos no pueda desactivarlo entre esta comprobación y el commit.
            if (estado == EstadoContenedor.EnServicio)
            {
                int idPunto = await LeerPuntoAsync(conexion, transaccion, idContenedor, cancellationToken).ConfigureAwait(false);
                if (!await RepositorioPuntosInstalacionOracle.BloquearAsync(conexion, transaccion, idPunto, cancellationToken).ConfigureAwait(false))
                {
                    await RechazarAsync(transaccion, MensajesCatalogo.PuntoDeCanecaInactivo).ConfigureAwait(false);
                }
            }

            EstadoContenedor actual = await BloquearAsync(conexion, transaccion, idContenedor, cancellationToken).ConfigureAwait(false);
            if (actual == EstadoContenedor.DadoDeBaja && estado != EstadoContenedor.DadoDeBaja)
            {
                await RechazarAsync(transaccion, MensajesCatalogo.CanecaDadaDeBaja).ConfigureAwait(false);
            }

            if (estado == EstadoContenedor.EnServicio
                && await ContarHabilitadosAsync(conexion, transaccion, idContenedor, cancellationToken).ConfigureAwait(false) == 0)
            {
                await RechazarAsync(transaccion, MensajesCatalogo.CanecaSinMaterialParaServicio).ConfigureAwait(false);
            }

            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                "UPDATE contenedor SET estado = :estado WHERE id_contenedor = :id", transaccion))
            {
                comando.Agregar("estado", OracleDbType.Int32, (int)estado);
                comando.Agregar("id", OracleDbType.Int32, idContenedor);
                await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            }

            await transaccion.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Punto de la caneca; no cambia después del alta, así que no hace falta bloquear la caneca para leerlo.</summary>
        private static async Task<int> LeerPuntoAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idContenedor, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT id_punto_instalacion FROM contenedor WHERE id_contenedor = :id", transaccion);
            comando.Agregar("id", OracleDbType.Int32, idContenedor);
            object? valor = await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return ConversionOracle.Entero(valor ?? throw new InvalidOperationException($"No existe caneca con id {idContenedor}."));
        }

        /// <summary>Bloquea la fila de la caneca y devuelve su estado confirmado; lanza si no existe.</summary>
        private static async Task<EstadoContenedor> BloquearAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idContenedor, CancellationToken cancellationToken)
        {
            // WAIT 10: si otra transacción retiene el bloqueo más de 10 s, Oracle responde ORA-00054 (26ai) u ORA-30006 (versiones anteriores) en lugar de esperar sin límite;
            // el traductor lo convierte en CambioConcurrenteException.
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT estado FROM contenedor WHERE id_contenedor = :id FOR UPDATE WAIT 10", transaccion);
            comando.Agregar("id", OracleDbType.Int32, idContenedor);
            object? valor = await TraductorErroresOracle.TraducirEsperaAsync(
                () => comando.ExecuteScalarAsync(cancellationToken)).ConfigureAwait(false);
            return (EstadoContenedor)ConversionOracle.Entero(valor ?? throw new InvalidOperationException($"No existe caneca con id {idContenedor}."));
        }

        /// <summary>
        /// Materiales habilitados que admite la caneca, vistos dentro de la transacción. Un material no puede
        /// deshabilitarse a la vez, porque <c>DeshabilitarAsync</c> necesita el bloqueo de esta caneca.
        /// </summary>
        private static async Task<int> ContarHabilitadosAsync(OracleConnection conexion, OracleTransaction transaccion,
            int idContenedor, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion,
                "SELECT COUNT(*) FROM contenedor_material cm JOIN material m ON m.id_material = cm.id_material " +
                "WHERE cm.id_contenedor = :id AND m.habilitado = 1", transaccion);
            comando.Agregar("id", OracleDbType.Int32, idContenedor);
            return ConversionOracle.Entero((await comando.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false))!);
        }

        /// <summary>Deshace la transacción y lanza <see cref="CambioConcurrenteException"/>; nunca vuelve.</summary>
        private static async Task RechazarAsync(OracleTransaction transaccion, string mensaje)
        {
            // Sin token: el rollback debe ocurrir aunque la operación se haya cancelado.
            await transaccion.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            throw new CambioConcurrenteException(mensaje);
        }

        private static async Task CambiarMaterialAsync(OracleConnection conexion, OracleTransaction transaccion, string sql,
            int idContenedor, int idMaterial, CancellationToken cancellationToken)
        {
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql, transaccion);
            comando.Agregar("id", OracleDbType.Int32, idContenedor);
            comando.Agregar("material", OracleDbType.Int32, idMaterial);
            await comando.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Dos consultas con el mismo filtro: primero las canecas y luego sus pares caneca-material. Se agrupan
        /// en memoria para no repetir la fila de la caneca por cada material.
        /// </summary>
        private async Task<IReadOnlyList<Contenedor>> ListarConFiltroAsync(string filtro, object? valor, CancellationToken cancellationToken)
        {
            using OracleConnection conexion = await _fabrica.AbrirAsync(cancellationToken).ConfigureAwait(false);

            var materiales = new Dictionary<int, List<int>>();
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT cm.id_contenedor, cm.id_material FROM contenedor_material cm JOIN contenedor c ON c.id_contenedor = cm.id_contenedor WHERE {filtro}"))
            {
                AgregarValor(comando, valor);
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    int idContenedor = ConversionOracle.Entero(lector.GetValue(0));
                    if (!materiales.TryGetValue(idContenedor, out List<int>? lista))
                    {
                        materiales[idContenedor] = lista = new List<int>();
                    }

                    lista.Add(ConversionOracle.Entero(lector.GetValue(1)));
                }
            }

            var canecas = new List<Contenedor>();
            using (OracleCommand comando = ComandosOracle.Crear(conexion,
                $"SELECT {Columnas} FROM contenedor c WHERE {filtro} ORDER BY NLSSORT(c.nombre, 'NLS_SORT=BINARY'), c.id_contenedor"))
            {
                AgregarValor(comando, valor);
                using DbDataReader lector = await comando.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                while (await lector.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    int id = ConversionOracle.Entero(lector.GetValue(0));
                    canecas.Add(new Contenedor(
                        id,
                        ConversionOracle.Entero(lector.GetValue(1)),
                        lector.GetString(2),
                        lector.GetString(3),
                        ConversionOracle.FechaUtc(lector.GetValue(4)),
                        (EstadoContenedor)ConversionOracle.Entero(lector.GetValue(5)),
                        materiales.TryGetValue(id, out List<int>? suyos) ? suyos : new List<int>()));
                }
            }

            return canecas;
        }

        /// <summary>El valor del filtro es un id (entero) o el identificador del QR (texto).</summary>
        private static void AgregarValor(OracleCommand comando, object? valor)
        {
            switch (valor)
            {
                case int entero:
                    comando.Agregar("valor", OracleDbType.Int32, entero);
                    break;
                case string texto:
                    comando.Agregar("valor", OracleDbType.Varchar2, texto);
                    break;
            }
        }
    }
}
