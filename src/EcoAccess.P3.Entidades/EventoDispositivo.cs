using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Mensaje recibido de una caneca o de su simulador. Se guarda siempre, también si no cambió nada, para auditar
    /// (RN-09). Es inmutable: el resultado se decide antes de crearlo. El identificador externo lo pone el dispositivo
    /// y es único por caneca, así una retransmisión se reconoce y no se procesa dos veces.
    /// La autenticidad del mensaje la comprueba el adaptador antes de llegar aquí (RN-20).
    /// </summary>
    public class EventoDispositivo
    {
        public const int LongitudMaximaIdExterno = 64;

        /// <summary>0 mientras el evento no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }
        public int IdContenedor { get; }
        public string IdExterno { get; }
        public TipoEventoDispositivo Tipo { get; }

        /// <summary>Orden a la que se refiere; puede faltar en un evento en revisión.</summary>
        public int? IdOrden { get; }
        public ResultadoEvento Resultado { get; }

        /// <summary>Hora que informa el dispositivo; solo informativa, porque su reloj puede estar desajustado.</summary>
        public DateTime? FechaDispositivoUtc { get; }

        /// <summary>Hora del servidor al recibirlo; con ella se mide la ventana de la orden.</summary>
        public DateTime FechaRecepcionUtc { get; }

        public bool EsNuevo => Id == 0;

        public EventoDispositivo(
            int idContenedor,
            string idExterno,
            TipoEventoDispositivo tipo,
            int? idOrden,
            ResultadoEvento resultado,
            DateTime? fechaDispositivoUtc,
            DateTime fechaRecepcionUtc)
        {
            IdContenedor = Validacion.IdPositivo(idContenedor, nameof(idContenedor));
            IdExterno = Validacion.TextoRequerido(idExterno, nameof(idExterno), LongitudMaximaIdExterno);
            Tipo = Validacion.EnumDefinido(tipo, nameof(tipo));
            IdOrden = idOrden is null ? null : Validacion.IdPositivo(idOrden.Value, nameof(idOrden));
            Resultado = Validacion.EnumDefinido(resultado, nameof(resultado));
            FechaDispositivoUtc = fechaDispositivoUtc is null ? null : Validacion.FechaUtc(fechaDispositivoUtc.Value, nameof(fechaDispositivoUtc));
            FechaRecepcionUtc = Validacion.FechaUtc(fechaRecepcionUtc, nameof(fechaRecepcionUtc));

            if (Resultado == ResultadoEvento.Aplicado && IdOrden is null)
            {
                throw new ArgumentException("Un evento aplicado debe referirse a una orden.", nameof(idOrden));
            }
        }

        public EventoDispositivo(
            int id,
            int idContenedor,
            string idExterno,
            TipoEventoDispositivo tipo,
            int? idOrden,
            ResultadoEvento resultado,
            DateTime? fechaDispositivoUtc,
            DateTime fechaRecepcionUtc)
            : this(idContenedor, idExterno, tipo, idOrden, resultado, fechaDispositivoUtc, fechaRecepcionUtc)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
        }
    }
}
