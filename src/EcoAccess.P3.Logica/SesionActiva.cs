using System;
using System.Collections.Generic;
using System.Linq;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica
{

    /// <summary>
    /// Quién inició sesión en la GUI y cuándo. Vive solo en memoria y no se persiste. No da permisos
    /// por sí misma: la GUI pasa <see cref="IdUsuario"/> como <c>idActor</c> y cada servicio vuelve a comprobar
    /// en la base de datos que la cuenta siga activa y con el rol necesario.
    /// </summary>
    public sealed class SesionActiva
    {
        public int IdUsuario { get; }
        public string Nombre { get; }
        public IReadOnlyList<CodigoRol> Roles { get; }
        public DateTime InicioUtc { get; }

        public SesionActiva(int idUsuario, string nombre, IEnumerable<CodigoRol> roles, DateTime inicioUtc)
        {
            IdUsuario = idUsuario;
            Nombre = nombre;
            Roles = roles.ToArray();
            InicioUtc = inicioUtc;
        }

        /// <summary>Sirve para mostrar u ocultar opciones del menú; la autorización real la hacen los servicios.</summary>
        public bool TieneRol(CodigoRol rol) => Roles.Contains(rol);
    }
}
