using System;
using System.Collections.Generic;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Consulta acotada para administración; texto literal, sin comodines SQL.</summary>
    public sealed record FiltroUsuarios
    {
        public string Texto { get; }
        public bool? Activo { get; }
        public CodigoRol? Rol { get; }
        public int Pagina { get; }
        public int TamanoPagina { get; }
        public long Desplazamiento => ((long)Pagina - 1) * TamanoPagina;

        public FiltroUsuarios(string? texto = null, bool? activo = null, CodigoRol? rol = null,
            int pagina = 1, int tamanoPagina = 25)
        {
            Texto = (texto ?? "").Trim().ToLowerInvariant();
            if (Texto.Length > Usuario.LongitudMaximaCorreo)
                throw new ArgumentException("La búsqueda no puede superar 254 caracteres.", nameof(texto));
            if (rol is not null && !Compat.IsDefined(rol.Value))
                throw new ArgumentException("Seleccione un rol válido.", nameof(rol));
            if (pagina < 1) throw new ArgumentOutOfRangeException(nameof(pagina), "La página debe ser positiva.");
            if (tamanoPagina is < 1 or > 100)
                throw new ArgumentOutOfRangeException(nameof(tamanoPagina), "La página debe contener entre 1 y 100 cuentas.");
            Activo = activo;
            Rol = rol;
            Pagina = pagina;
            TamanoPagina = tamanoPagina;
        }
    }

    /// <summary>No calcula un total global: una cuenta adicional determina si hay siguiente página.</summary>
    public sealed record PaginaUsuarios(IReadOnlyList<Usuario> Filas, int Pagina, int TamanoPagina, bool HaySiguiente);
}
