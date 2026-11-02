using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Clasificador determinista para pruebas y demostraciones: no mira la foto, busca su huella SHA-256 en
    /// una tabla de respuestas preparadas y, si no la encuentra, responde <see cref="TipoPropuestaVision.Incierta"/>.
    /// Sus aciertos no dicen nada del rendimiento de un modelo real: solo permiten probar el flujo.
    /// </summary>
    public sealed class ClasificadorMaterialSimulado : IClasificadorMaterial
    {
        private readonly Dictionary<string, PropuestaVision> _respuestas = new(StringComparer.Ordinal);
        private readonly object _cerrojo = new object();

        public string Proveedor => "simulador";

        /// <summary>Veces que se llamó a <see cref="ClasificarAsync"/>; sirve para comprobar que una foto inválida no llega aquí.</summary>
        public int Llamadas => Volatile.Read(ref _llamadas);

        private int _llamadas;

        /// <summary>Prepara la respuesta para una foto; la última preparación de la misma foto reemplaza a la anterior.</summary>
        public void Preparar(byte[] contenido, PropuestaVision propuesta)
        {
            Compat.ThrowIfNull(contenido, nameof(contenido));
            Compat.ThrowIfNull(propuesta, nameof(propuesta));
            string huella = Huella(contenido);
            lock (_cerrojo)
            {
                _respuestas[huella] = propuesta;
            }
        }

        public Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(imagen, nameof(imagen));
            Compat.ThrowIfNull(catalogo, nameof(catalogo));
            cancellationToken.ThrowIfCancellationRequested();
            Interlocked.Increment(ref _llamadas);
            string huella = Huella(imagen.Contenido);
            lock (_cerrojo)
            {
                return Task.FromResult(_respuestas.TryGetValue(huella, out PropuestaVision? propuesta)
                    ? propuesta
                    : new PropuestaVision(TipoPropuestaVision.Incierta, null, null));
            }
        }

        /// <summary>SHA-256 del contenido en hexadecimal.</summary>
        public static string Huella(byte[] contenido) => Compat.ToHexString(Compat.Sha256(contenido));
    }
}
