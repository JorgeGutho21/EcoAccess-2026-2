using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Bot.Dispositivos
{

    /// <summary>
    /// Disponibilidad antes de ordenar una apertura: el bridge y el dispositivo escuchan en este computador y Oracle no
    /// registra un fallo físico reciente sin una apertura posterior. Escuchar no demuestra que el ESP32 mueva la tapa.
    /// </summary>
    internal sealed class EstadoCanecaLocal : IEstadoCaneca
    {
        /// <summary>Tras un fallo físico no se ordena otra apertura durante este tiempo, salvo que otra apertura lo haya superado.</summary>
        private static readonly TimeSpan BloqueoPorFallo = TimeSpan.FromMinutes(15);

        private readonly Func<int, DateTime, CancellationToken, Task<bool>> _fallos;
        private readonly Uri[] _enlaces;
        private readonly System.Collections.Generic.IReadOnlyDictionary<int, Uri[]> _porCaneca;

        /// <param name="fallos">Si la caneca informó un fallo físico desde esa fecha, sin una apertura posterior.</param>
        public EstadoCanecaLocal(Func<int, DateTime, CancellationToken, Task<bool>> fallos, params Uri[] enlaces)
            : this(fallos, new System.Collections.Generic.Dictionary<int, Uri[]>(), enlaces)
        {
        }

        /// <summary>
        /// Estación de varias canecas (un bridge por caneca, ver docs/hardware): cada orden comprueba los enlaces del bridge
        /// de SU caneca. Una caneca sin enlaces propios usa los predeterminados.
        /// </summary>
        public EstadoCanecaLocal(Func<int, DateTime, CancellationToken, Task<bool>> fallos,
            System.Collections.Generic.IReadOnlyDictionary<int, Uri[]> porCaneca, params Uri[] predeterminados)
        {
            _fallos = fallos ?? throw new ArgumentNullException(nameof(fallos));
            _enlaces = predeterminados ?? throw new ArgumentNullException(nameof(predeterminados));
            _porCaneca = porCaneca ?? throw new ArgumentNullException(nameof(porCaneca));
        }

        public async Task<bool> DisponibleAsync(int idContenedor, CancellationToken cancellationToken = default)
        {
            Uri[] enlaces = _porCaneca.TryGetValue(idContenedor, out Uri[]? propios) ? propios : _enlaces;
            foreach (Uri enlace in enlaces)
                if (!await EscuchaAsync(enlace).ConfigureAwait(false)) return false;
            return !await _fallos(idContenedor, DateTime.UtcNow - BloqueoPorFallo, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>Solo direcciones locales: nunca se sondea un equipo externo.</summary>
        private static async Task<bool> EscuchaAsync(Uri enlace)
        {
            if (!enlace.IsLoopback) return false;
            using var cliente = new TcpClient();
            Task conectar = cliente.ConnectAsync(enlace.Host, enlace.Port);
            if (await Task.WhenAny(conectar, Task.Delay(500)).ConfigureAwait(false) != conectar)
            {
                _ = conectar.ContinueWith(t => t.Exception, TaskScheduler.Default);
                return false;
            }
            try { await conectar.ConfigureAwait(false); return true; }
            catch (Exception error) when (error is SocketException or ObjectDisposedException) { return false; }
        }
    }
}
