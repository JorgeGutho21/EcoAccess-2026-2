namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Reserva irreversible antes de actuar. Un fallo parcial exige conciliación, nunca reapertura automática.</summary>
    public interface IRegistroEjecucionDispositivo
    {
        /// <summary>True solo para el primer intento de esta orden y clave. Ante incertidumbre o fallo de almacenamiento, false.</summary>
        bool IntentarReservar(ComandoApertura comando);
    }
}
