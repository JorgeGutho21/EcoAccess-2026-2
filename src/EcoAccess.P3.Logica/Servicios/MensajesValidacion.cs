using System;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Convierte la excepción de validación de una entidad en un rechazo legible. Solo se usa
    /// alrededor de constructores que reciben datos escritos por una persona; un error con datos ya
    /// guardados sigue siendo una excepción, porque indica una falla y no una decisión de negocio.
    /// </summary>
    internal static class MensajesValidacion
    {
        public static Resultado<T> Rechazo<T>(ArgumentException error) =>
            Resultado<T>.Rechazo(MotivoRechazo.DatosInvalidos, Texto(error));

        public static Resultado Rechazo(ArgumentException error) =>
            Resultado.Rechazo(MotivoRechazo.DatosInvalidos, Texto(error));

        // ArgumentException agrega « (Parameter 'x')» al mensaje; se reemplaza por el nombre del campo al inicio.
        private static string Texto(ArgumentException error) =>
            error.ParamName is null
                ? error.Message
                : $"{error.ParamName}: {error.Message.Replace($" (Parameter '{error.ParamName}')", string.Empty)}";
    }
}
