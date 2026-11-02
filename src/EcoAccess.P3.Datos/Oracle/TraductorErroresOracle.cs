using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using EcoAccess.P3.Logica.Contratos;
using Oracle.ManagedDataAccess.Client;

namespace EcoAccess.P3.Datos.Oracle
{

    /// <summary>
    /// Traduce los errores de Oracle que Logica sabe manejar: ORA-00001 (UNIQUE), que se convierte en
    /// <see cref="ConflictoUnicidadException"/> con el nombre de la restricción en minúsculas, por ejemplo
    /// <c>uq_material_codigo</c>, y ORA-00054 u ORA-30006 (espera de bloqueo agotada) u ORA-00060 (interbloqueo), que se
    /// convierten en <see cref="CambioConcurrenteException"/>. Los demás errores se propagan sin tocar.
    /// </summary>
    internal static partial class TraductorErroresOracle
    {
        public const int ViolacionUnicidad = 1;
        public const int Interbloqueo = 60;
        public const int ReferenciaInexistente = 2291;

        // Oracle 23ai/26ai responde ORA-00054 cuando vence FOR UPDATE WAIT n; las versiones anteriores, ORA-30006.
        // Se aceptan ambos para que el mismo código funcione en Oracle Free 26ai y en una XE anterior.
        public const int RecursoOcupado = 54;
        public const int EsperaAgotada = 30006;

        /// <summary>Mensaje apto para la persona usuaria cuando otra sesión retiene las filas que se necesitan.</summary>
        public const string MensajeEsperaBloqueo = "Otra operación está modificando esos datos; inténtelo de nuevo.";

        /// <summary>Ejecuta la operación y convierte ORA-00001 en <see cref="ConflictoUnicidadException"/>.</summary>
        public static async Task<T> TraducirUnicidadAsync<T>(Func<Task<T>> operacion)
        {
            try
            {
                return await operacion().ConfigureAwait(false);
            }
            catch (OracleException ex) when (ex.Number == ViolacionUnicidad)
            {
                throw new ConflictoUnicidadException(ExtraerNombreRestriccion(ex.Message) ?? "restriccion_unica", ex);
            }
        }

        /// <summary>
        /// Ejecuta una sentencia <c>FOR UPDATE WAIT</c> y convierte ORA-00054, ORA-30006 y ORA-00060 en
        /// <see cref="CambioConcurrenteException"/>. La transacción abierta se deshace al liberarse.
        /// </summary>
        public static async Task<T> TraducirEsperaAsync<T>(Func<Task<T>> operacion)
        {
            try
            {
                return await operacion().ConfigureAwait(false);
            }
            catch (OracleException ex) when (EsErrorDeEspera(ex.Number))
            {
                throw new CambioConcurrenteException(MensajeEsperaBloqueo, ex);
            }
        }

        /// <summary>
        /// Ejecuta la operación y convierte ORA-02291 (la fila padre no existe) en <see cref="InvalidOperationException"/>,
        /// como hacen los repositorios cuando el id buscado no existe. ORA-00001 se sigue traduciendo como unicidad.
        /// </summary>
        public static Task<T> TraducirReferenciaAsync<T>(Func<Task<T>> operacion) => TraducirUnicidadAsync(async () =>
        {
            try
            {
                return await operacion().ConfigureAwait(false);
            }
            catch (OracleException ex) when (ex.Number == ReferenciaInexistente)
            {
                throw new InvalidOperationException("Alguna de las filas referenciadas no existe.", ex);
            }
        });

        /// <summary>Indica si el número de error de Oracle significa que la espera de un bloqueo terminó sin obtenerlo.</summary>
        public static bool EsErrorDeEspera(int numero) => numero is RecursoOcupado or EsperaAgotada or Interbloqueo;

        /// <summary>
        /// Saca el nombre de la restricción del mensaje
        /// «ORA-00001: unique constraint (ESQUEMA.UQ_MATERIAL_CODIGO) violated…». Devuelve null si no lo encuentra.
        /// </summary>
        public static string? ExtraerNombreRestriccion(string mensaje)
        {
            Match coincidencia = PatronRestriccion().Match(mensaje);
            return coincidencia.Success ? coincidencia.Groups["nombre"].Value.ToLowerInvariant() : null;
        }

        private static readonly Regex PatronPatronRestriccion = new Regex(@"ORA-00001:[^(]*\((?:[^.()\s]+\.)?(?<nombre>[^.()\s]+)\)", RegexOptions.CultureInvariant);
        private static Regex PatronRestriccion() => PatronPatronRestriccion;
    }
}
