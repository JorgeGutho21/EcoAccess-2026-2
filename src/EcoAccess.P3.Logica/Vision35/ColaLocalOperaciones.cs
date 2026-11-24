using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>Estado de una operación registrada localmente mientras Oracle Cloud no está disponible.</summary>
    public enum EstadoOperacionLocal
    {
        ClasificacionSoloOrientacion = 1,
        AperturaConfirmadaSimulada = 2,
        AperturaConfirmadaFisica = 3,
        CanceladaPorUsuario = 4,
        FalloHardware = 5,
        FalloVision = 6,
    }

    /// <summary>
    /// Registro operativo local sin datos personales innecesarios (Sección 14).
    /// Conserva una clave de idempotencia única para sincronizar con Oracle sin duplicados al recuperar conexión.
    /// </summary>
    public sealed record OperacionPendienteLocal(
        string ClaveIdempotencia,
        DateTime FechaUtc,
        string ModoOrigen,
        IReadOnlyList<string> CodigosMaterial35,
        IReadOnlyList<string> CanecasSolicitadas,
        IReadOnlyList<string> CanecasConfirmadas,
        EstadoOperacionLocal Estado,
        string? Detalle,
        bool SincronizadoOracle = false,
        int IntentosSincronizacion = 0,
        DateTime? FechaSincronizacionUtc = null);

    /// <summary>Resumen de una ronda de sincronización de operaciones pendientes hacia Oracle.</summary>
    public sealed record ResultadoSincronizacionLocal(
        int TotalPendientesInicial,
        int SincronizadasAhora,
        int FallidasAhora,
        int PendientesRestantes);

    /// <summary>
    /// Almacenamiento local seguro en JSON (escritura atómica) para tolerancia a desconexiones temporales de Oracle.
    /// No inventa depósitos exitosos cuando el hardware no confirmó la apertura y evita duplicados por clave de idempotencia.
    /// </summary>
    public sealed class ColaLocalOperaciones
    {
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() },
        };

        private readonly string _rutaArchivo;
        private readonly object _bloqueo = new();

        public string RutaArchivo => _rutaArchivo;

        public ColaLocalOperaciones(string? rutaArchivo = null)
        {
            _rutaArchivo = string.IsNullOrWhiteSpace(rutaArchivo)
                ? Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "EcoAccessP3", "cola_offline", "operaciones_pendientes.json")
                : rutaArchivo!;
        }

        /// <summary>Encola una operación de forma idempotente; si la clave ya existe, no crea duplicado y devuelve false.</summary>
        public bool Registrar(OperacionPendienteLocal operacion)
        {
            Compat.ThrowIfNull(operacion, nameof(operacion));
            if (string.IsNullOrWhiteSpace(operacion.ClaveIdempotencia))
                throw new ArgumentException("La clave de idempotencia es obligatoria.", nameof(operacion));

            // Regla de honestidad: nunca registrar apertura confirmada si no hay al menos una caneca confirmada.
            if (operacion.Estado is EstadoOperacionLocal.AperturaConfirmadaFisica or EstadoOperacionLocal.AperturaConfirmadaSimulada
                && operacion.CanecasConfirmadas.Count == 0)
            {
                throw new InvalidOperationException("No se puede registrar una apertura confirmada sin canecas confirmadas por el controlador.");
            }

            lock (_bloqueo)
            {
                List<OperacionPendienteLocal> lista = LeerInterno();
                if (lista.Any(o => string.Equals(o.ClaveIdempotencia, operacion.ClaveIdempotencia, StringComparison.Ordinal)))
                    return false;

                lista.Add(operacion);
                GuardarInterno(lista);
                return true;
            }
        }

        public IReadOnlyList<OperacionPendienteLocal> ListarTodas()
        {
            lock (_bloqueo) { return LeerInterno(); }
        }

        public IReadOnlyList<OperacionPendienteLocal> ListarPendientes()
        {
            lock (_bloqueo) { return LeerInterno().Where(o => !o.SincronizadoOracle).ToList(); }
        }

        /// <summary>
        /// Intenta sincronizar todas las operaciones pendientes hacia Oracle usando el delegado provisto.
        /// Cada operación exitosa se marca sincronizada y nunca vuelve a enviarse (0 duplicados).
        /// </summary>
        public async Task<ResultadoSincronizacionLocal> SincronizarPendientesAsync(
            Func<OperacionPendienteLocal, CancellationToken, Task<bool>> enviarAOracle,
            CancellationToken ct = default)
        {
            Compat.ThrowIfNull(enviarAOracle, nameof(enviarAOracle));
            List<OperacionPendienteLocal> pendientes;
            lock (_bloqueo) { pendientes = LeerInterno().Where(o => !o.SincronizadoOracle).ToList(); }

            int ok = 0, fail = 0;
            var actualizadas = new Dictionary<string, OperacionPendienteLocal>(StringComparer.Ordinal);

            foreach (OperacionPendienteLocal op in pendientes)
            {
                ct.ThrowIfCancellationRequested();
                bool exito;
                try
                {
                    exito = await enviarAOracle(op, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception)
                {
                    exito = false;
                }

                if (exito)
                {
                    ok++;
                    actualizadas[op.ClaveIdempotencia] = op with
                    {
                        SincronizadoOracle = true,
                        IntentosSincronizacion = op.IntentosSincronizacion + 1,
                        FechaSincronizacionUtc = DateTime.UtcNow,
                    };
                }
                else
                {
                    fail++;
                    actualizadas[op.ClaveIdempotencia] = op with
                    {
                        IntentosSincronizacion = op.IntentosSincronizacion + 1,
                    };
                }
            }

            int restantes;
            lock (_bloqueo)
            {
                List<OperacionPendienteLocal> lista = LeerInterno();
                for (int i = 0; i < lista.Count; i++)
                {
                    if (actualizadas.TryGetValue(lista[i].ClaveIdempotencia, out var nueva))
                        lista[i] = nueva;
                }
                GuardarInterno(lista);
                restantes = lista.Count(o => !o.SincronizadoOracle);
            }

            return new ResultadoSincronizacionLocal(pendientes.Count, ok, fail, restantes);
        }

        private List<OperacionPendienteLocal> LeerInterno()
        {
            if (!File.Exists(_rutaArchivo))
                return new List<OperacionPendienteLocal>();
            try
            {
                string json = File.ReadAllText(_rutaArchivo);
                return JsonSerializer.Deserialize<List<OperacionPendienteLocal>>(json, OpcionesJson) ?? new List<OperacionPendienteLocal>();
            }
            catch (Exception)
            {
                return new List<OperacionPendienteLocal>();
            }
        }

        private void GuardarInterno(List<OperacionPendienteLocal> lista)
        {
            string? dir = Path.GetDirectoryName(_rutaArchivo);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
            string temp = _rutaArchivo + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(lista, OpcionesJson));
            if (File.Exists(_rutaArchivo))
                File.Delete(_rutaArchivo);
            File.Move(temp, _rutaArchivo);
        }
    }
}
