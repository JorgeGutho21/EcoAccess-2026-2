using System;
using System.Linq;

namespace EcoAccess.P3.Entidades
{

    /// <summary>Traza estructurada sin foto, respuesta libre, identidad externa ni credenciales.</summary>
    public sealed class AnalisisImagen
    {
        public string Clave { get; }
        public int IdInteraccion { get; }
        public string Proveedor { get; }
        public string Modelo { get; }
        public string Version { get; }
        public int Estado { get; }
        public int? IdMaterialSugerido { get; }
        public DateTime FechaUtc { get; }
        public int? IdMaterialConfirmado { get; }
        public DateTime? FechaConfirmacionUtc { get; }

        public AnalisisImagen(string clave, int idInteraccion, string proveedor, string modelo, string version, int estado,
            int? idMaterialSugerido, DateTime fechaUtc, int? idMaterialConfirmado = null, DateTime? fechaConfirmacionUtc = null)
        {
            Clave = Identificador(clave, nameof(clave), 100);
            IdInteraccion = Validacion.IdPositivo(idInteraccion, nameof(idInteraccion));
            Proveedor = Identificador(proveedor, nameof(proveedor), 80);
            Modelo = Identificador(modelo, nameof(modelo), 120);
            Version = Identificador(version, nameof(version), 40);
            if (estado is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(estado));
            Estado = estado;
            IdMaterialSugerido = idMaterialSugerido is int sugerido ? Validacion.IdPositivo(sugerido, nameof(idMaterialSugerido)) : null;
            FechaUtc = Validacion.FechaUtc(fechaUtc, nameof(fechaUtc));
            IdMaterialConfirmado = idMaterialConfirmado is int confirmado ? Validacion.IdPositivo(confirmado, nameof(idMaterialConfirmado)) : null;
            FechaConfirmacionUtc = fechaConfirmacionUtc is DateTime fecha ? Validacion.FechaUtc(fecha, nameof(fechaConfirmacionUtc)) : null;
            if ((IdMaterialConfirmado is null) != (FechaConfirmacionUtc is null) || FechaConfirmacionUtc < FechaUtc)
                throw new ArgumentException("La confirmación requiere material y fecha posterior al análisis.");
        }

        private static string Identificador(string valor, string parametro, int maximo)
        {
            string limpio = Validacion.TextoRequerido(valor, parametro, maximo);
            if (limpio.Any(c => !Compat.IsAsciiLetterOrDigit(c) && c is not ('-' or '_' or '.' or ':')))
                throw new ArgumentException("Solo se permiten identificadores técnicos acotados.", parametro);
            return limpio;
        }
    }
}
