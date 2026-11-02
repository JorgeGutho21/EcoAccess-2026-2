using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Residuo detectado por el sensor dentro de la ventana de una orden vigente (RN-08). Hay como máximo uno por
    /// orden y por interacción (RN-17). El material declarado es el confirmado al autorizar; el verificado lo fija
    /// después un operario que no sea quien depositó (RN-11, RN-21). Un peso no medido queda nulo y la estimación se
    /// guarda aparte, nunca como kilogramos reales (RN-12).
    /// </summary>
    public class Deposito
    {
        /// <summary>0 mientras el depósito no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }
        public int IdOrden { get; }

        /// <summary>Evento de detección que lo originó; 0 hasta que el repositorio guarda ambos en la misma transacción.</summary>
        public int IdEvento { get; }
        public int IdMaterialDeclarado { get; }
        public int? IdMaterialVerificado { get; private set; }
        public int? IdVerificador { get; private set; }
        public DateTime? FechaVerificacionUtc { get; private set; }
        /// <summary>Conteo del verificador independiente, distinto de la cantidad declarada en la solicitud.</summary>
        public int? CantidadVerificada { get; private set; }
        public decimal? PesoMedidoKg { get; }
        public decimal? PesoEstimadoKg { get; }
        public DateTime FechaDeteccionUtc { get; }

        public bool EsNuevo => Id == 0;
        public bool EstaVerificado => IdMaterialVerificado is not null;

        public Deposito(int idOrden, int idMaterialDeclarado, decimal? pesoMedidoKg, decimal? pesoEstimadoKg, DateTime fechaDeteccionUtc)
        {
            IdOrden = Validacion.IdPositivo(idOrden, nameof(idOrden));
            IdMaterialDeclarado = Validacion.IdPositivo(idMaterialDeclarado, nameof(idMaterialDeclarado));
            PesoMedidoKg = pesoMedidoKg is null ? null : ValidarPeso(pesoMedidoKg.Value, nameof(pesoMedidoKg));
            PesoEstimadoKg = pesoEstimadoKg is null ? null : ValidarPeso(pesoEstimadoKg.Value, nameof(pesoEstimadoKg));
            FechaDeteccionUtc = Validacion.FechaUtc(fechaDeteccionUtc, nameof(fechaDeteccionUtc));
        }

        public Deposito(
            int id,
            int idOrden,
            int idEvento,
            int idMaterialDeclarado,
            int? idMaterialVerificado,
            int? idVerificador,
            DateTime? fechaVerificacionUtc,
            decimal? pesoMedidoKg,
            decimal? pesoEstimadoKg,
            DateTime fechaDeteccionUtc,
            int? cantidadVerificada = null)
            : this(idOrden, idMaterialDeclarado, pesoMedidoKg, pesoEstimadoKg, fechaDeteccionUtc)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
            IdEvento = Validacion.IdPositivo(idEvento, nameof(idEvento));

            bool sinVerificar = idMaterialVerificado is null && idVerificador is null && fechaVerificacionUtc is null;
            bool verificado = idMaterialVerificado is not null && idVerificador is not null && fechaVerificacionUtc is not null;
            if (!sinVerificar && !verificado)
            {
                throw new ArgumentException("La verificación guarda material, verificador y fecha juntos.", nameof(idMaterialVerificado));
            }

            if (verificado)
            {
                Verificar(idMaterialVerificado!.Value, idVerificador!.Value, fechaVerificacionUtc!.Value, cantidadVerificada);
            }
            else if (cantidadVerificada is not null) throw new ArgumentException("La cantidad verificada exige material, responsable y fecha de verificación.", nameof(cantidadVerificada));
        }

        /// <summary>
        /// Fija el material verificado. Que el verificador no sea quien depositó y tenga el rol adecuado (RN-21)
        /// lo comprueba la lógica, porque la entidad no conoce al ciudadano de la interacción.
        /// </summary>
        public void Verificar(int idMaterialVerificado, int idVerificador, DateTime fechaUtc, int? cantidadVerificada = null)
        {
            Validacion.IdPositivo(idMaterialVerificado, nameof(idMaterialVerificado));
            Validacion.IdPositivo(idVerificador, nameof(idVerificador));
            Validacion.FechaUtc(fechaUtc, nameof(fechaUtc));
            if (cantidadVerificada is < 1 or > 1000) throw new ArgumentOutOfRangeException(nameof(cantidadVerificada));
            if (fechaUtc < FechaDeteccionUtc)
            {
                throw new ArgumentException("La verificación no puede ser anterior a la detección.", nameof(fechaUtc));
            }

            if (EstaVerificado)
            {
                throw new InvalidOperationException("El depósito ya fue verificado.");
            }

            IdMaterialVerificado = idMaterialVerificado;
            IdVerificador = idVerificador;
            FechaVerificacionUtc = fechaUtc;
            CantidadVerificada = cantidadVerificada;
        }

        // NUMBER(6,3): hasta 999,999 kg con tres decimales.
        private static decimal ValidarPeso(decimal peso, string nombreParametro)
        {
            if (peso <= 0m || peso >= 1000m || decimal.Round(peso, 3) != peso)
            {
                throw new ArgumentOutOfRangeException(nombreParametro, "El peso va de 0,001 a 999,999 kg con tres decimales como máximo.");
            }

            return peso;
        }
    }
}
