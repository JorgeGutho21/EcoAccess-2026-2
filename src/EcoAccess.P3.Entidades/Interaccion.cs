using System;

namespace EcoAccess.P3.Entidades
{

    /// <summary>
    /// Una solicitud de apertura de una caneca, desde que alguien la pide hasta que se cierra con o sin depósito.
    /// Guarda la sugerencia del clasificador y el material confirmado por separado: la sugerencia orienta, pero solo
    /// el material confirmado autoriza la apertura (RN-06). Tiene como máximo un depósito (RN-17).
    /// La apertura manual de un operario (CU-17) nace cerrada y nunca genera depósito automático.
    /// </summary>
    public class Interaccion
    {
        public const int LongitudMaximaMotivo = 300;

        /// <summary>0 mientras la interacción no se ha guardado; la base de datos asigna el valor real.</summary>
        public int Id { get; }
        public int IdContenedor { get; }

        /// <summary>Ciudadano que la inició; null en la apertura manual, donde el responsable es el operario.</summary>
        public int? IdUsuario { get; }
        public CanalInteraccion Canal { get; }
        /// <summary>Declaración ciudadana, nunca conteo físico. Null conserva la ausencia de ese dato histórico.</summary>
        public int? CantidadDeclarada { get; }
        public EstadoInteraccion Estado { get; private set; }
        public int? IdMaterialSugerido { get; private set; }

        /// <summary>Confianza del clasificador entre 0 y 1, con tres decimales como la columna NUMBER(4,3).</summary>
        public decimal? ConfianzaSugerida { get; private set; }
        public int? IdMaterialConfirmado { get; private set; }
        public int? IdOperario { get; }
        public string? Motivo { get; private set; }
        public DateTime FechaInicioUtc { get; }
        public DateTime? FechaCierreUtc { get; private set; }

        public bool EsNuevo => Id == 0;
        public bool EstaCerrada => Estado is EstadoInteraccion.ConDeposito or EstadoInteraccion.SinDeposito or EstadoInteraccion.Rechazada;

        /// <summary>Inicia una solicitud automática (Telegram o simulador); la manual se crea con <see cref="AperturaManual"/>.</summary>
        public Interaccion(int idContenedor, int? idUsuario, CanalInteraccion canal, DateTime fechaInicioUtc, int? cantidadDeclarada = null)
        {
            CantidadDeclarada = ValidarCantidad(cantidadDeclarada);
            IdContenedor = Validacion.IdPositivo(idContenedor, nameof(idContenedor));
            IdUsuario = idUsuario is null ? null : Validacion.IdPositivo(idUsuario.Value, nameof(idUsuario));
            Canal = Validacion.EnumDefinido(canal, nameof(canal));
            if (Canal == CanalInteraccion.Manual)
            {
                throw new ArgumentException("La apertura manual se registra con Interaccion.AperturaManual.", nameof(canal));
            }

            FechaInicioUtc = Validacion.FechaUtc(fechaInicioUtc, nameof(fechaInicioUtc));
            Estado = EstadoInteraccion.Iniciada;
        }

        private Interaccion(int idContenedor, int idOperario, string motivo, DateTime fechaUtc)
        {
            IdContenedor = Validacion.IdPositivo(idContenedor, nameof(idContenedor));
            IdOperario = Validacion.IdPositivo(idOperario, nameof(idOperario));
            Motivo = Validacion.TextoRequerido(motivo, nameof(motivo), LongitudMaximaMotivo);
            FechaInicioUtc = Validacion.FechaUtc(fechaUtc, nameof(fechaUtc));
            FechaCierreUtc = FechaInicioUtc;
            Canal = CanalInteraccion.Manual;
            Estado = EstadoInteraccion.SinDeposito;
        }

        /// <summary>
        /// Reconstruye una interacción guardada. Rechaza las combinaciones que prohíben los CHECK de
        /// <c>interaccion</c> en V003, para que un dato corrupto no pase desapercibido.
        /// </summary>
        public Interaccion(
            int id,
            int idContenedor,
            int? idUsuario,
            CanalInteraccion canal,
            EstadoInteraccion estado,
            int? idMaterialSugerido,
            decimal? confianzaSugerida,
            int? idMaterialConfirmado,
            int? idOperario,
            string? motivo,
            DateTime fechaInicioUtc,
            DateTime? fechaCierreUtc,
            int? cantidadDeclarada = null)
        {
            CantidadDeclarada = ValidarCantidad(cantidadDeclarada);
            Id = Validacion.IdPositivo(id, nameof(id));
            IdContenedor = Validacion.IdPositivo(idContenedor, nameof(idContenedor));
            IdUsuario = idUsuario is null ? null : Validacion.IdPositivo(idUsuario.Value, nameof(idUsuario));
            Canal = Validacion.EnumDefinido(canal, nameof(canal));
            Estado = Validacion.EnumDefinido(estado, nameof(estado));
            IdMaterialSugerido = idMaterialSugerido is null ? null : Validacion.IdPositivo(idMaterialSugerido.Value, nameof(idMaterialSugerido));
            ConfianzaSugerida = confianzaSugerida is null ? null : ValidarConfianza(confianzaSugerida.Value, nameof(confianzaSugerida));
            IdMaterialConfirmado = idMaterialConfirmado is null ? null : Validacion.IdPositivo(idMaterialConfirmado.Value, nameof(idMaterialConfirmado));
            IdOperario = idOperario is null ? null : Validacion.IdPositivo(idOperario.Value, nameof(idOperario));
            Motivo = Validacion.TextoOpcional(motivo, nameof(motivo), LongitudMaximaMotivo);
            FechaInicioUtc = Validacion.FechaUtc(fechaInicioUtc, nameof(fechaInicioUtc));
            FechaCierreUtc = fechaCierreUtc is null ? null : Validacion.FechaUtc(fechaCierreUtc.Value, nameof(fechaCierreUtc));

            if ((IdMaterialSugerido is null) != (ConfianzaSugerida is null))
            {
                throw new ArgumentException("La sugerencia guarda material y confianza juntos.", nameof(confianzaSugerida));
            }

            if (EstaCerrada != (FechaCierreUtc is not null) || FechaCierreUtc < FechaInicioUtc)
            {
                throw new ArgumentException("Solo una interacción cerrada tiene fecha de cierre, y no anterior al inicio.", nameof(fechaCierreUtc));
            }

            if (Estado == EstadoInteraccion.Rechazada && Motivo is null)
            {
                throw new ArgumentException("Una interacción rechazada debe tener motivo.", nameof(motivo));
            }

            if (Canal == CanalInteraccion.Manual)
            {
                bool manualValida = Estado == EstadoInteraccion.SinDeposito && IdOperario is not null && Motivo is not null
                    && IdMaterialSugerido is null && IdMaterialConfirmado is null;
                if (!manualValida)
                {
                    throw new ArgumentException("Una apertura manual queda sin depósito, con operario y motivo, y sin materiales.", nameof(canal));
                }
            }
            else
            {
                if (IdOperario is not null)
                {
                    throw new ArgumentException("Solo la apertura manual tiene operario.", nameof(idOperario));
                }

                bool requiereConfirmado = Estado is EstadoInteraccion.Autorizada or EstadoInteraccion.ConDeposito or EstadoInteraccion.SinDeposito;
                if (requiereConfirmado && IdMaterialConfirmado is null)
                {
                    throw new ArgumentException("Una interacción autorizada debe tener material confirmado.", nameof(idMaterialConfirmado));
                }
            }
        }

        /// <summary>
        /// Registra una apertura manual supervisada (CU-17): queda cerrada sin depósito, con el operario y el motivo.
        /// Si el operario registra después el depósito de un ciudadano, ese depósito sigue la verificación normal.
        /// </summary>
        public static Interaccion AperturaManual(int idContenedor, int idOperario, string motivo, DateTime fechaUtc) =>
            new(idContenedor, idOperario, motivo, fechaUtc);

        /// <summary>Guarda lo que propuso el clasificador; una sugerencia nueva reemplaza la anterior.</summary>
        public void RegistrarSugerencia(int idMaterial, decimal confianza)
        {
            Validacion.IdPositivo(idMaterial, nameof(idMaterial));
            ValidarConfianza(confianza, nameof(confianza));
            ExigirEstado(EstadoInteraccion.Iniciada);
            IdMaterialSugerido = idMaterial;
            ConfianzaSugerida = confianza;
        }

        /// <summary>
        /// Fija el material confirmado. La entidad no sabe si la caneca lo admite ni si está habilitado: eso lo
        /// comprueba el repositorio con los bloqueos de la emisión de la orden (RN-05, RN-06).
        /// </summary>
        public void Autorizar(int idMaterialConfirmado)
        {
            Validacion.IdPositivo(idMaterialConfirmado, nameof(idMaterialConfirmado));
            ExigirEstado(EstadoInteraccion.Iniciada);
            IdMaterialConfirmado = idMaterialConfirmado;
            Estado = EstadoInteraccion.Autorizada;
        }

        public void Rechazar(string motivo, DateTime fechaUtc)
        {
            string limpio = Validacion.TextoRequerido(motivo, nameof(motivo), LongitudMaximaMotivo);
            DateTime fecha = ValidarCierre(fechaUtc, nameof(fechaUtc));
            ExigirEstado(EstadoInteraccion.Iniciada);
            Motivo = limpio;
            Cerrar(EstadoInteraccion.Rechazada, fecha);
        }

        public void CerrarConDeposito(DateTime fechaUtc)
        {
            DateTime fecha = ValidarCierre(fechaUtc, nameof(fechaUtc));
            ExigirEstado(EstadoInteraccion.Autorizada);
            Cerrar(EstadoInteraccion.ConDeposito, fecha);
        }

        public void CerrarSinDeposito(DateTime fechaUtc)
        {
            DateTime fecha = ValidarCierre(fechaUtc, nameof(fechaUtc));
            ExigirEstado(EstadoInteraccion.Autorizada);
            Cerrar(EstadoInteraccion.SinDeposito, fecha);
        }

        private void Cerrar(EstadoInteraccion estado, DateTime fechaUtc)
        {
            Estado = estado;
            FechaCierreUtc = fechaUtc;
        }

        private DateTime ValidarCierre(DateTime fechaUtc, string nombreParametro)
        {
            Validacion.FechaUtc(fechaUtc, nombreParametro);
            if (fechaUtc < FechaInicioUtc)
            {
                throw new ArgumentException("La fecha de cierre no puede ser anterior al inicio.", nombreParametro);
            }

            return fechaUtc;
        }

        private void ExigirEstado(EstadoInteraccion esperado)
        {
            if (Estado != esperado)
            {
                throw new InvalidOperationException($"La interacción está en estado {Estado}; la operación exige {esperado}.");
            }
        }

        private static decimal ValidarConfianza(decimal confianza, string nombreParametro)
        {
            if (confianza < 0m || confianza > 1m || decimal.Round(confianza, 3) != confianza)
            {
                throw new ArgumentOutOfRangeException(nombreParametro, "La confianza va de 0 a 1 con tres decimales como máximo.");
            }

            return confianza;
        }

        public static int? ValidarCantidad(int? cantidad)
        {
            if (cantidad is < 1 or > 1000)
                throw new ArgumentOutOfRangeException(nameof(cantidad), "La cantidad declarada debe estar entre 1 y 1000 unidades.");
            return cantidad;
        }
    }
}
