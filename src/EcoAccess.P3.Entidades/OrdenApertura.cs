using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Orden para que una caneca se abra una vez (RN-07). Tiene clave de idempotencia y vencimiento; mientras está
    /// vigente (Emitida a Detectada) es la única de su caneca (RN-16), para que un evento del sensor se atribuya
    /// a una sola interacción. Los estados solo avanzan; una orden terminada no cambia ni se reenvía.
    /// </summary>
    public class OrdenApertura
    {
        public const int LongitudMinimaClave = 8;
        public const int LongitudMaximaClave = 64;

        /// <summary>0 mientras la orden no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }
        public int IdInteraccion { get; }
        public int IdContenedor { get; }

        /// <summary>Clave que el emisor repite al reintentar; la misma clave nunca crea dos órdenes.</summary>
        public string ClaveIdempotencia { get; }
        public EstadoOrdenApertura Estado { get; private set; }
        public DateTime FechaEmisionUtc { get; }
        public DateTime FechaVencimientoUtc { get; }
        public DateTime? FechaCierreUtc { get; private set; }

        public bool EsNuevo => Id == 0;
        public bool EsVigente => Estado <= EstadoOrdenApertura.Detectada;

        public OrdenApertura(int idInteraccion, int idContenedor, string claveIdempotencia, DateTime fechaEmisionUtc, DateTime fechaVencimientoUtc)
        {
            IdInteraccion = Validacion.IdPositivo(idInteraccion, nameof(idInteraccion));
            IdContenedor = Validacion.IdPositivo(idContenedor, nameof(idContenedor));
            ClaveIdempotencia = Validacion.TextoRequerido(claveIdempotencia, nameof(claveIdempotencia), LongitudMaximaClave, LongitudMinimaClave);
            FechaEmisionUtc = Validacion.FechaUtc(fechaEmisionUtc, nameof(fechaEmisionUtc));
            FechaVencimientoUtc = Validacion.FechaUtc(fechaVencimientoUtc, nameof(fechaVencimientoUtc));
            if (FechaVencimientoUtc <= FechaEmisionUtc)
            {
                throw new ArgumentException("El vencimiento debe ser posterior a la emisión.", nameof(fechaVencimientoUtc));
            }

            Estado = EstadoOrdenApertura.Emitida;
        }

        /// <summary>Reconstruye una orden guardada; solo una orden terminada tiene fecha de cierre.</summary>
        public OrdenApertura(
            int id,
            int idInteraccion,
            int idContenedor,
            string claveIdempotencia,
            EstadoOrdenApertura estado,
            DateTime fechaEmisionUtc,
            DateTime fechaVencimientoUtc,
            DateTime? fechaCierreUtc)
            : this(idInteraccion, idContenedor, claveIdempotencia, fechaEmisionUtc, fechaVencimientoUtc)
        {
            Id = Validacion.IdPositivo(id, nameof(id));
            Estado = Validacion.EnumDefinido(estado, nameof(estado));
            FechaCierreUtc = fechaCierreUtc is null ? null : Validacion.FechaUtc(fechaCierreUtc.Value, nameof(fechaCierreUtc));

            if (EsVigente == (FechaCierreUtc is not null) || FechaCierreUtc < FechaEmisionUtc)
            {
                throw new ArgumentException("Solo una orden terminada tiene fecha de cierre, y no anterior a la emisión.", nameof(fechaCierreUtc));
            }
        }

        /// <summary>Vigente y con el vencimiento alcanzado: ya no debe abrir la caneca ni aceptar eventos.</summary>
        public bool EstaVencida(DateTime ahoraUtc) => EsVigente && Validacion.FechaUtc(ahoraUtc, nameof(ahoraUtc)) >= FechaVencimientoUtc;

        /// <summary>Indica si llevar la orden a <paramref name="estado"/> sería un avance permitido.</summary>
        public bool PuedeAvanzarA(EstadoOrdenApertura estado) =>
            EsVigente && estado > Estado && estado <= EstadoOrdenApertura.Detectada;

        public void MarcarEnviada() => Avanzar(EstadoOrdenApertura.Enviada);

        public void RegistrarAcuse() => Avanzar(EstadoOrdenApertura.Acusada);

        public void RegistrarApertura() => Avanzar(EstadoOrdenApertura.Abierta);

        public void RegistrarDeteccion() => Avanzar(EstadoOrdenApertura.Detectada);

        public void Cerrar(DateTime fechaUtc) => Terminar(EstadoOrdenApertura.Cerrada, fechaUtc);

        public void Vencer(DateTime fechaUtc) => Terminar(EstadoOrdenApertura.Vencida, fechaUtc);

        public void Fallar(DateTime fechaUtc) => Terminar(EstadoOrdenApertura.Fallida, fechaUtc);

        private void Avanzar(EstadoOrdenApertura estado)
        {
            if (!PuedeAvanzarA(estado))
            {
                throw new InvalidOperationException($"La orden está en estado {Estado}; no puede pasar a {estado}.");
            }

            Estado = estado;
        }

        private void Terminar(EstadoOrdenApertura estado, DateTime fechaUtc)
        {
            Validacion.FechaUtc(fechaUtc, nameof(fechaUtc));
            if (fechaUtc < FechaEmisionUtc)
            {
                throw new ArgumentException("La fecha de cierre no puede ser anterior a la emisión.", nameof(fechaUtc));
            }

            if (!EsVigente)
            {
                throw new InvalidOperationException($"La orden ya terminó en estado {Estado}.");
            }

            Estado = estado;
            FechaCierreUtc = fechaUtc;
        }
    }
}
