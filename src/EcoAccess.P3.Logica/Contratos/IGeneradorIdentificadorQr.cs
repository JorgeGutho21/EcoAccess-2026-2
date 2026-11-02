namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>
    /// Produce el texto opaco que va en el QR de una caneca. Es una interfaz para que las pruebas usen
    /// valores fijos; la implementación real es <see cref="Servicios.GeneradorIdentificadorQrAleatorio"/>.
    /// </summary>
    public interface IGeneradorIdentificadorQr
    {
        string Generar();
    }
}
