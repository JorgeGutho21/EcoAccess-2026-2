using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Text;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Tests.Logica.Dobles
{

    // Dobles de autenticación. El repositorio reproduce las reglas que el de Oracle aplica en un único UPDATE
    // (contar el fallo, bloquear al llegar al máximo y dejar el contador en cero) para probar el servicio sin base.

    internal sealed class RepositorioCredencialesEnMemoria : IRepositorioCredenciales
    {
        private readonly RepositorioUsuariosEnMemoria _usuarios;

        public RepositorioCredencialesEnMemoria(RepositorioUsuariosEnMemoria usuarios)
        {
            _usuarios = usuarios;
        }

        private readonly Dictionary<int, CredencialAcceso> _credenciales = new Dictionary<int, CredencialAcceso>();

        public int Reinicios { get; private set; }
        public bool SimularConflictoAlAgregar { get; set; }

        public CredencialAcceso? Leer(int idUsuario) => _credenciales.GetValueOrDefault(idUsuario);

        public void Guardar(CredencialAcceso credencial) => _credenciales[credencial.IdUsuario] = credencial;

        public void Quitar(int idUsuario) => _credenciales.Remove(idUsuario);

        public async Task<CredencialAcceso?> ObtenerPorCorreoAsync(string correoNormalizado, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Usuario> todos = await _usuarios.ListarAsync(cancellationToken);
            Usuario? usuario = todos.FirstOrDefault(u => u.CorreoElectronico == correoNormalizado);
            return usuario is null ? null : Leer(usuario.Id);
        }

        public Task<CredencialAcceso?> ObtenerPorUsuarioAsync(int idUsuario, CancellationToken cancellationToken = default) =>
            Task.FromResult(Leer(idUsuario));

        public Task AgregarAsync(CredencialAcceso credencial, CancellationToken cancellationToken = default)
        {
            if (SimularConflictoAlAgregar || _credenciales.ContainsKey(credencial.IdUsuario))
            {
                throw new ConflictoUnicidadException("pk_credencial");
            }

            Guardar(credencial);
            return Task.CompletedTask;
        }

        public Task ReemplazarSecretoAsync(CredencialAcceso credencial, CancellationToken cancellationToken = default)
        {
            if (!_credenciales.ContainsKey(credencial.IdUsuario))
            {
                throw new InvalidOperationException("La cuenta no tiene credencial.");
            }

            Guardar(new CredencialAcceso(credencial.IdUsuario, credencial.Hash, credencial.Sal, credencial.Iteraciones, credencial.FechaCambioUtc));
            return Task.CompletedTask;
        }

        public Task RegistrarFalloAsync(int idUsuario, int maximoIntentos, DateTime bloquearHastaUtc, CancellationToken cancellationToken = default)
        {
            // Igual que Oracle: sin credencial, el UPDATE no toca filas y no es un error.
            if (!_credenciales.TryGetValue(idUsuario, out CredencialAcceso? c))
            {
                return Task.CompletedTask;
            }

            int intentos = c.IntentosFallidos + 1;
            Guardar(intentos >= maximoIntentos
                ? new CredencialAcceso(c.IdUsuario, c.Hash, c.Sal, c.Iteraciones, c.FechaCambioUtc, 0, bloquearHastaUtc)
                : new CredencialAcceso(c.IdUsuario, c.Hash, c.Sal, c.Iteraciones, c.FechaCambioUtc, intentos, c.BloqueadaHastaUtc));
            return Task.CompletedTask;
        }

        public Task ReiniciarIntentosAsync(int idUsuario, CancellationToken cancellationToken = default)
        {
            if (!_credenciales.TryGetValue(idUsuario, out CredencialAcceso? c))
            {
                return Task.CompletedTask;
            }

            Guardar(new CredencialAcceso(c.IdUsuario, c.Hash, c.Sal, c.Iteraciones, c.FechaCambioUtc));
            Reinicios++;
            return Task.CompletedTask;
        }
    }

    /// <summary>Hasher rápido y determinista para las pruebas del servicio; cuenta las verificaciones.</summary>
    internal sealed class HasherFalso : IHasherContrasenas
    {
        private static readonly byte[] SalFija = Enumerable.Repeat((byte)7, CredencialAcceso.LongitudSal).ToArray();

        public int Iteraciones => CredencialAcceso.IteracionesMinimas;
        public int Verificaciones { get; private set; }

        public HashContrasena Calcular(string contrasena) => new(Derivar(contrasena), SalFija, Iteraciones);

        public bool Verificar(string contrasena, byte[] sal, int iteraciones, byte[] hashEsperado)
        {
            Verificaciones++;
            return Derivar(contrasena).AsSpan().SequenceEqual(hashEsperado);
        }

        private static byte[] Derivar(string contrasena) => Compat.Sha256(Encoding.UTF8.GetBytes(contrasena));
    }

    /// <summary>Reloj que la prueba puede adelantar, para comprobar que el bloqueo vence.</summary>
    internal sealed class RelojAjustable : TimeProvider
    {
        public RelojAjustable(DateTimeOffset inicio)
        {
            Ahora = inicio;
        }

        public DateTimeOffset Ahora { get; private set; }

        public void Avanzar(TimeSpan intervalo) => Ahora += intervalo;

        public override DateTimeOffset GetUtcNow() => Ahora;
    }
}
