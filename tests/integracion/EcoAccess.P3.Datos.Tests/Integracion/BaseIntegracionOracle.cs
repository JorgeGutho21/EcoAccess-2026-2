using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Oracle;
using Microsoft.Extensions.Configuration;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    // Base de las pruebas contra Oracle real. Se ejecutan donde haya un Oracle Free con el esquema
    // ECOACCESS_P3 y el secreto ConnectionStrings:OracleP3. Sin secreto
    // quedan Inconclusive: no cuentan como superadas. Cada prueba crea datos con sufijo único
    // y los borra al final, en orden de claves foráneas.
    public abstract class BaseIntegracionOracle
    {
        public const string Categoria = "IntegracionOracle";

        private readonly List<int> _contenedores = new List<int>();
        private readonly List<int> _puntos = new List<int>();
        private readonly List<int> _materiales = new List<int>();
        private readonly List<int> _usuarios = new List<int>();

        protected FabricaConexionesOracle Fabrica { get; private set; } = null!;

        /// <summary>PDB al que apunta la cadena en uso: FREEPDB1 en la base real, PRUEBAP3 en el entorno aislado.</summary>
        protected string PdbEsperado { get; private set; } = "FREEPDB1";
        protected string EsquemaEsperado { get; private set; } = "ECOACCESS_P3";

        /// <summary>
        /// False si la cadena usa un alias TNS (wallet de Autonomous Database): el PDB real es el nombre interno del
        /// tenant y no se deduce del alias, así que solo se puede exigir que exista.
        /// </summary>
        protected bool PdbDeducible { get; private set; } = true;

        /// <summary>
        /// Solo en PRUEBAP3 y con ECOACCESS_DEMO_CONSERVAR=1 (scripts/pruebas/Probar-Oracle.ps1 -ConservarDemo): las
        /// pruebas no borran sus datos ficticios, para consultarlos en SQL Developer. Nunca aplica a la base real.
        /// </summary>
        protected bool ConservarDemo => PdbEsperado == "PRUEBAP3"
            && Environment.GetEnvironmentVariable("ECOACCESS_DEMO_CONSERVAR") == "1";

        protected static string Sufijo() => Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();

        /// <summary>Variable que autoriza, a conciencia, ejecutar la integración contra un esquema que no es de pruebas.</summary>
        public const string PermitirOtroEsquema = "ECOACCESS_INTEGRACION_PERMITIR_OTRO_ESQUEMA";

        /// <summary>
        /// User Secrets del proyecto (ecoaccess-p3-framework, LOS MISMOS de la aplicación y el bot) y, encima, la variable
        /// ECOACCESS_ConnectionStrings__OracleP3 si existe. scripts/pruebas/Ejecutar-Integracion-Esquema.ps1 la fija con el
        /// secreto del esquema aislado ECOACCESS_P3_PRUEBAS sin tocar el secreto ni los datos de la base real.
        /// </summary>
        protected static string? LeerCadena() => new ConfigurationBuilder()
            .AddUserSecrets<BaseIntegracionOracle>(optional: true)
            .AddEnvironmentVariables("ECOACCESS_")
            .Build()
            .GetConnectionString("OracleP3");

        /// <summary>
        /// Un destino es de pruebas si su usuario termina en _PRUEBAS o si apunta al PDB aislado PRUEBAP3. El 20/11/2026 la
        /// batería corrió contra Oracle Cloud (ECOACCESS_P3_APP) porque el secreto del proyecto es el de la aplicación: desde
        /// entonces cualquier otro destino se rechaza salvo que <see cref="PermitirOtroEsquema"/> valga 1.
        /// </summary>
        public static bool EsDestinoDePruebas(string cadena)
        {
            var partes = new OracleConnectionStringBuilder(cadena);
            return partes.UserID.Trim().ToUpperInvariant().EndsWith("_PRUEBAS", StringComparison.Ordinal)
                || partes.DataSource.ToUpperInvariant().TrimEnd('/').EndsWith("/PRUEBAP3", StringComparison.Ordinal);
        }

        [TestInitialize]
        public void PrepararConexion()
        {
            string? cadena = LeerCadena();
            if (Compat.IsNullOrWhiteSpace(cadena))
            {
                Assert.Inconclusive("Falta ConnectionStrings:OracleP3 en los User Secrets ecoaccess-p3: prueba de integración no ejecutada.");
            }
            if (!EsDestinoDePruebas(cadena!) && Environment.GetEnvironmentVariable(PermitirOtroEsquema) != "1")
            {
                Assert.Inconclusive("El destino configurado no es un esquema de pruebas (usuario *_PRUEBAS o PDB PRUEBAP3): la integración " +
                    "no se ejecuta contra la base de la aplicación. Usa scripts\\pruebas\\Ejecutar-Integracion-Esquema.ps1 o fija " +
                    "ECOACCESS_ConnectionStrings__OracleP3 con el esquema aislado.");
            }

            var configuracion = new OracleConnectionStringBuilder(cadena);
            PdbDeducible = configuracion.DataSource.Contains('/');
            PdbEsperado = configuracion.DataSource.Split('/').Last().ToUpperInvariant();
            EsquemaEsperado = configuracion.UserID.ToUpperInvariant();
            Fabrica = new FabricaConexionesOracle(cadena);
        }

        [TestCleanup]
        public async Task LimpiarDatosAsync()
        {
            if (Fabrica is null || ConservarDemo)
            {
                return;
            }

            // Orden de las FK: hijos antes que padres. Un fallo en un paso no impide los siguientes;
            // al final se informan todos juntos para que la basura no pase inadvertida.
            var errores = new List<Exception>();
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            await BorrarAsync(conexion, "DELETE FROM deposito WHERE id_orden IN (SELECT id_orden FROM orden_apertura WHERE id_contenedor = :id)",
                _contenedores, errores);
            await BorrarAsync(conexion, "DELETE FROM evento_dispositivo WHERE id_contenedor = :id", _contenedores, errores);
            await BorrarAsync(conexion, "DELETE FROM orden_apertura WHERE id_contenedor = :id", _contenedores, errores);
            await BorrarAsync(conexion, "DELETE FROM interaccion WHERE id_contenedor = :id", _contenedores, errores);
            await BorrarAsync(conexion, "DELETE FROM contenedor_material WHERE id_contenedor = :id", _contenedores, errores);
            await BorrarAsync(conexion, "DELETE FROM contenedor WHERE id_contenedor = :id", _contenedores, errores);
            await BorrarAsync(conexion, "DELETE FROM punto_instalacion WHERE id_punto_instalacion = :id", _puntos, errores);
            await BorrarAsync(conexion, "DELETE FROM material WHERE id_material = :id", _materiales, errores);
            await BorrarAsync(conexion, "DELETE FROM opinion WHERE id_usuario = :id", _usuarios, errores);
            await BorrarAsync(conexion, "DELETE FROM credencial_acceso WHERE id_usuario = :id", _usuarios, errores);
            await BorrarAsync(conexion, "DELETE FROM usuario_rol WHERE id_usuario = :id", _usuarios, errores);
            await BorrarAsync(conexion, "DELETE FROM usuario WHERE id_usuario = :id", _usuarios, errores);
            if (errores.Count > 0)
            {
                throw new AggregateException("La limpieza de datos de prueba falló en algún paso.", errores);
            }
        }

        protected int RegistrarContenedor(int id) => Registrar(_contenedores, id);
        protected int RegistrarPunto(int id) => Registrar(_puntos, id);
        protected int RegistrarMaterial(int id) => Registrar(_materiales, id);
        protected int RegistrarUsuario(int id) => Registrar(_usuarios, id);

        protected async Task<int> ContarAsync(string sql)
        {
            using OracleConnection conexion = await Fabrica.AbrirAsync(CancellationToken.None);
            using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
            return ConversionOracle.Entero(await comando.ExecuteScalarAsync() ?? throw new InvalidOperationException("La consulta no devolvió valor."));
        }

        private static int Registrar(List<int> lista, int id)
        {
            lista.Add(id);
            return id;
        }

        private static async Task BorrarAsync(OracleConnection conexion, string sql, List<int> ids, List<Exception> errores)
        {
            foreach (int id in ids)
            {
                try
                {
                    using OracleCommand comando = ComandosOracle.Crear(conexion, sql);
                    comando.Agregar("id", OracleDbType.Int32, id);
                    await comando.ExecuteNonQueryAsync();
                }
                catch (OracleException error)
                {
                    errores.Add(error);
                }
            }
        }
    }
}
