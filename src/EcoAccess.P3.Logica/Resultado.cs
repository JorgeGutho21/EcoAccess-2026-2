using System;

namespace EcoAccess.P3.Logica
{

    /// <summary>
    /// Respuesta de un servicio a una operación sin valor de retorno: éxito, o rechazo
    /// con un motivo y un mensaje que la GUI o el bot pueden mostrar. Los errores de programación y las
    /// fallas de infraestructura (Oracle caído) siguen siendo excepciones; esto es solo para decisiones de negocio.
    /// </summary>
    public class Resultado
    {
        private static readonly Resultado ExitoUnico = new(null, null);

        public bool Exitoso => Motivo is null;
        public MotivoRechazo? Motivo { get; }
        public string? Mensaje { get; }

        protected Resultado(MotivoRechazo? motivo, string? mensaje)
        {
            Motivo = motivo;
            Mensaje = mensaje;
        }

        public static Resultado Exito() => ExitoUnico;

        public static Resultado Rechazo(MotivoRechazo motivo, string mensaje)
        {
            Compat.ThrowIfNullOrWhiteSpace(mensaje, nameof(mensaje));
            return new Resultado(motivo, mensaje);
        }

        public override string ToString() => Exitoso ? "Éxito" : $"Rechazo {Motivo}: {Mensaje}";
    }

    /// <summary>Igual que <see cref="Resultado"/>, pero con el valor que produce la operación cuando tiene éxito.</summary>
    public sealed class Resultado<T> : Resultado
    {
        private readonly T? _valor;

        private Resultado(T valor) : base(null, null) => _valor = valor;

        private Resultado(MotivoRechazo motivo, string mensaje) : base(motivo, mensaje) { }

        /// <summary>El valor producido. Leerlo en un rechazo es un error de programación.</summary>
        public T Valor => Exitoso
            ? _valor!
            : throw new InvalidOperationException("Un resultado rechazado no tiene valor; consulte Motivo y Mensaje.");

        public static Resultado<T> Exito(T valor) => new(valor);

        public static new Resultado<T> Rechazo(MotivoRechazo motivo, string mensaje)
        {
            Compat.ThrowIfNullOrWhiteSpace(mensaje, nameof(mensaje));
            return new Resultado<T>(motivo, mensaje);
        }

        /// <summary>Convierte un rechazo sin valor (por ejemplo, del control de acceso) en uno de este tipo.</summary>
        public static Resultado<T> DesdeRechazo(Resultado rechazo)
        {
            if (rechazo.Exitoso)
            {
                throw new ArgumentException("Solo se puede convertir un resultado rechazado.", nameof(rechazo));
            }

            return new Resultado<T>(rechazo.Motivo!.Value, rechazo.Mensaje!);
        }
    }
}
