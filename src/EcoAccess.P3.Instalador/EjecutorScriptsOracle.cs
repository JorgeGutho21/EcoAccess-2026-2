using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Instalador
{
    /// <summary>
    /// Ejecuta en Oracle las sentencias que produce <see cref="LectorScriptSql"/> y lleva el registro de versiones en
    /// ECO_MIGRACION_APLICADA. Usa una sola conexión para todo el plan.
    /// </summary>
    /// <remarks>
    /// Oracle confirma implícitamente cada DDL (CREATE/ALTER), así que un script que falla a mitad no se puede deshacer
    /// con ROLLBACK: por eso cada migración trae su propia guarda inicial y el instalador se detiene en el primer error,
    /// sin registrar la versión, para que una persona revise el estado antes de reintentar.
    /// </remarks>
    public sealed class EjecutorScriptsOracle
    {
        private readonly OracleConnection _conexion;
        private readonly TextWriter _salida;
        private IReadOnlyDictionary<string, string> _variables = new Dictionary<string, string>();

        public EjecutorScriptsOracle(OracleConnection conexion, TextWriter salida)
        {
            _conexion = conexion;
            _salida = salida;
        }

        /// <summary>Versiones registradas como aplicadas.</summary>
        public async Task<HashSet<string>> LeerAplicadasAsync(CancellationToken ct) =>
            new HashSet<string>((await LeerHuellasAsync(ct)).Keys, StringComparer.OrdinalIgnoreCase);

        /// <summary>Huella registrada de cada versión, para avisar si un script ya aplicado se modificó.</summary>
        public async Task<Dictionary<string, string>> LeerHuellasAsync(CancellationToken ct)
        {
            var huellas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using OracleCommand comando = _conexion.CreateCommand();
            comando.CommandText = "SELECT version, huella_sha256 FROM eco_migracion_aplicada";
            using DbDataReader lector = await comando.ExecuteReaderAsync(ct);
            while (await lector.ReadAsync(ct)) huellas[lector.GetString(0)] = lector.GetString(1);
            return huellas;
        }

        /// <summary>Ejecuta un script completo. Devuelve true si terminó sin errores que lo detuvieran.</summary>
        /// <param name="variables">
        /// Valores para variables de enlace (<c>:clave_esquema</c>…) de los scripts administrativos. Se envían como
        /// parámetros, nunca se concatenan al texto ni se imprimen.
        /// </param>
        public async Task<bool> EjecutarAsync(string rutaScript, CancellationToken ct, IReadOnlyDictionary<string, string>? variables = null)
        {
            _variables = variables ?? new Dictionary<string, string>();
            IReadOnlyList<SentenciaScript> sentencias = LectorScriptSql.Leer(File.ReadAllText(rutaScript));
            bool continuarAnteErrores = false;
            await HabilitarSalidaAsync(ct);
            foreach (SentenciaScript sentencia in sentencias)
            {
                switch (sentencia.Tipo)
                {
                    case TipoSentencia.Mensaje:
                        _salida.WriteLine("   » " + sentencia.Texto);
                        continue;
                    case TipoSentencia.ContinuarAnteErrores:
                        continuarAnteErrores = true;
                        continue;
                    case TipoSentencia.DetenerseAnteErrores:
                        continuarAnteErrores = false;
                        continue;
                }

                try
                {
                    await EjecutarSentenciaAsync(sentencia, ct);
                }
                catch (OracleException error)
                {
                    _salida.WriteLine($"   ✗ Línea {sentencia.Linea}: ORA-{error.Number:00000} {PrimeraLinea(error.Message)}");
                    await MostrarSalidaAsync(ct);
                    if (!continuarAnteErrores)
                    {
                        await EjecutarTextoAsync("ROLLBACK", ct);
                        return false;
                    }
                }
            }

            await EjecutarTextoAsync("COMMIT", ct);
            return true;
        }

        /// <summary>Guarda la versión como aplicada. «MARCADA» indica que una persona verificó que ya existía.</summary>
        public async Task RegistrarAsync(PasoInstalacion paso, string huella, string modo, long duracionMs, CancellationToken ct)
        {
            using (OracleCommand comando = _conexion.CreateCommand())
            {
                comando.BindByName = true;
                comando.CommandText =
                    "INSERT INTO eco_migracion_aplicada (version, descripcion, archivo, huella_sha256, modo, duracion_ms) " +
                    "VALUES (:version, :descripcion, :archivo, :huella, :modo, :duracion)";
                comando.Parameters.Add("version", OracleDbType.Varchar2).Value = paso.Version;
                comando.Parameters.Add("descripcion", OracleDbType.Varchar2).Value = paso.Descripcion;
                comando.Parameters.Add("archivo", OracleDbType.Varchar2).Value = paso.Archivo;
                comando.Parameters.Add("huella", OracleDbType.Varchar2).Value = huella;
                comando.Parameters.Add("modo", OracleDbType.Varchar2).Value = modo;
                comando.Parameters.Add("duracion", OracleDbType.Int64).Value = duracionMs;
                await comando.ExecuteNonQueryAsync(ct);
            }

            await EjecutarTextoAsync("COMMIT", ct);
        }

        private async Task EjecutarSentenciaAsync(SentenciaScript sentencia, CancellationToken ct)
        {
            using OracleCommand comando = _conexion.CreateCommand();
            comando.CommandText = sentencia.Texto;
            comando.CommandTimeout = 300;
            comando.BindByName = true;
            foreach (string nombre in LectorScriptSql.VariablesDeEnlace(sentencia.Texto))
            {
                if (!_variables.TryGetValue(nombre, out string? valor))
                    throw new InvalidOperationException($"Línea {sentencia.Linea}: falta el valor de la variable :{nombre}.");
                comando.Parameters.Add(nombre, OracleDbType.Varchar2).Value = valor;
            }

            if (sentencia.Tipo == TipoSentencia.Consulta)
            {
                using DbDataReader lector = await comando.ExecuteReaderAsync(ct);
                ImprimirFilas(lector);
                return;
            }

            var reloj = Stopwatch.StartNew();
            int filas = await comando.ExecuteNonQueryAsync(ct);
            _salida.WriteLine(sentencia.Tipo == TipoSentencia.PlSql
                ? $"   ✓ bloque PL/SQL (línea {sentencia.Linea}, {reloj.ElapsedMilliseconds} ms)"
                : $"   ✓ {Recortar(PrimeraLinea(sentencia.Texto), 70)}{(filas > 0 ? $" — {filas} fila(s)" : "")}");
            await MostrarSalidaAsync(ct);
        }

        private void ImprimirFilas(IDataReader lector)
        {
            _salida.WriteLine("   " + string.Join(" | ", Enumerable.Range(0, lector.FieldCount).Select(lector.GetName)));
            int total = 0;
            while (lector.Read())
            {
                total++;
                var valores = Enumerable.Range(0, lector.FieldCount).Select(i => lector.IsDBNull(i) ? "(nulo)" : Convert.ToString(lector.GetValue(i)) ?? "");
                _salida.WriteLine("   " + string.Join(" | ", valores));
            }

            _salida.WriteLine($"   ({total} fila(s))");
        }

        private Task HabilitarSalidaAsync(CancellationToken ct) => EjecutarTextoAsync("BEGIN DBMS_OUTPUT.ENABLE(NULL); END;", ct);

        /// <summary>Equivale a SET SERVEROUTPUT ON: muestra lo que los bloques escribieron con DBMS_OUTPUT.PUT_LINE.</summary>
        private async Task MostrarSalidaAsync(CancellationToken ct)
        {
            using OracleCommand comando = _conexion.CreateCommand();
            comando.BindByName = true;
            comando.CommandText = "BEGIN DBMS_OUTPUT.GET_LINE(:linea, :estado); END;";
            OracleParameter linea = comando.Parameters.Add("linea", OracleDbType.Varchar2, 32767);
            linea.Direction = ParameterDirection.Output;
            OracleParameter estado = comando.Parameters.Add("estado", OracleDbType.Int32);
            estado.Direction = ParameterDirection.Output;
            for (int i = 0; i < 1000; i++)
            {
                await comando.ExecuteNonQueryAsync(ct);
                if (Convert.ToInt32(estado.Value.ToString()) != 0) break;
                _salida.WriteLine("     " + linea.Value);
            }
        }

        private async Task EjecutarTextoAsync(string texto, CancellationToken ct)
        {
            using OracleCommand comando = _conexion.CreateCommand();
            comando.CommandText = texto;
            await comando.ExecuteNonQueryAsync(ct);
        }

        private static string PrimeraLinea(string texto) => texto.Split(new[] { '\n' }, 2)[0].Trim();

        private static string Recortar(string texto, int maximo) => texto.Length <= maximo ? texto : texto.Substring(0, maximo) + "…";
    }
}
