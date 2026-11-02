namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Calcula y verifica hashes de contraseñas. Es un contrato para que las pruebas del servicio
    /// usen un doble rápido y para poder cambiar de algoritmo sin tocar <c>ServicioAutenticacion</c> (DIP).
    /// </summary>
    public interface IHasherContrasenas
    {
        /// <summary>Iteraciones que usa para las contraseñas nuevas.</summary>
        int Iteraciones { get; }

        /// <summary>Genera una sal aleatoria y devuelve el hash de la contraseña con esa sal.</summary>
        HashContrasena Calcular(string contrasena);

        /// <summary>Recalcula el hash con la sal y las iteraciones guardadas y lo compara en tiempo constante.</summary>
        bool Verificar(string contrasena, byte[] sal, int iteraciones, byte[] hashEsperado);
    }

    /// <summary>Lo que se guarda de una contraseña: nunca el texto, solo el hash, la sal y las iteraciones.</summary>
    public sealed record HashContrasena(byte[] Hash, byte[] Sal, int Iteraciones);
}
