using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>Versión inmutable de una tarifa por depósito verificado, sin inferir peso.</summary>
    public sealed class ReglaEcopuntos
    {
        public int Id { get; }
        public int IdMaterial { get; }
        public int Puntos { get; }
        public bool PorUnidad { get; }
        public string Descripcion { get; }
        public DateTime DesdeUtc { get; }
        public DateTime? HastaUtc { get; }
        public int IdCreador { get; }

        public ReglaEcopuntos(int id, int idMaterial, int puntos, string descripcion,
            DateTime desdeUtc, DateTime? hastaUtc, int idCreador, bool porUnidad = false)
        {
            if (id < 0) throw new ArgumentOutOfRangeException(nameof(id));
            Id = id;
            IdMaterial = Validacion.IdPositivo(idMaterial, nameof(idMaterial));
            if (puntos is < 1 or > 1_000_000) throw new ArgumentOutOfRangeException(nameof(puntos));
            Puntos = puntos;
            PorUnidad = porUnidad;
            Descripcion = Validacion.TextoRequerido(descripcion, nameof(descripcion), 200);
            DesdeUtc = Validacion.FechaUtc(desdeUtc, nameof(desdeUtc));
            HastaUtc = hastaUtc is { } fin ? Validacion.FechaUtc(fin, nameof(hastaUtc)) : null;
            if (HastaUtc <= DesdeUtc) throw new ArgumentException("El final debe ser posterior al inicio.", nameof(hastaUtc));
            IdCreador = Validacion.IdPositivo(idCreador, nameof(idCreador));
        }

        public bool EstaVigente(DateTime ahoraUtc) => ahoraUtc >= DesdeUtc && (HastaUtc is null || ahoraUtc < HastaUtc);

        /// <summary>Para acreditar, el llamador debe pasar unidades verificadas, nunca la declaración de la solicitud.</summary>
        public int CalcularPuntos(int? unidades)
        {
            if (!PorUnidad) return Puntos;
            if (unidades is null) throw new InvalidOperationException("Esta tarifa exige cantidad verificada independientemente.");
            if (unidades is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(unidades));
            long total = (long)Puntos * unidades.Value;
            if (total > 1_000_000) throw new ArgumentOutOfRangeException(nameof(unidades), "El total supera el máximo de un crédito.");
            return (int)total;
        }
    }

    public enum TipoMovimientoEcopuntos { Credito = 1, Reverso = 2 }

    /// <summary>Hecho del libro: nunca se modifica ni elimina para corregir un saldo.</summary>
    public sealed record MovimientoEcopuntos(int Id, int IdOperacion, int IdDeposito, int IdUsuario,
        int IdRegla, TipoMovimientoEcopuntos Tipo, int Puntos, int? IdCreditoRevertido,
        int? IdPredecesor, int IdActor, DateTime FechaUtc, string Motivo);
}
