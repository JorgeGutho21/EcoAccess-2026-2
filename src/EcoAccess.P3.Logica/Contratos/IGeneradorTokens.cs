namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Produce los tokens de registro desde Telegram. Es una interfaz para que las pruebas usen valores fijos; la
    /// implementación real es <see cref="Servicios.GeneradorTokensAleatorio"/>.
    /// </summary>
    public interface IGeneradorTokens
    {
        string Generar();
    }
}
