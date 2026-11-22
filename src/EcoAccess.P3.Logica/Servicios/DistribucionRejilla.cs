using System;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>
    /// Cálculo puro de la cuadrícula del catálogo visual: cuántas columnas caben en un ancho y cuántas filas resultan
    /// para N tarjetas. Nunca reserva casillas vacías: con 4 residuos hay como mucho 4 columnas y 1 fila.
    /// </summary>
    public static class DistribucionRejilla
    {
        /// <summary>Columnas para <paramref name="tarjetas"/> en <paramref name="ancho"/> px (al menos 1, a lo sumo el máximo y el número de tarjetas).</summary>
        public static int Columnas(int tarjetas, int ancho, int anchoMinimo, int separacion, int maximo)
        {
            if (tarjetas <= 0) return 1;
            int caben = Math.Max(1, (ancho + separacion) / Math.Max(1, anchoMinimo + separacion));
            return Math.Max(1, Math.Min(Math.Min(caben, Math.Max(1, maximo)), tarjetas));
        }

        /// <summary>Filas necesarias para <paramref name="tarjetas"/> en <paramref name="columnas"/> columnas (0 si no hay tarjetas).</summary>
        public static int Filas(int tarjetas, int columnas) => tarjetas <= 0 ? 0 : (tarjetas + Math.Max(1, columnas) - 1) / Math.Max(1, columnas);

        /// <summary>Alto que ocupan las filas (sin separación después de la última).</summary>
        public static int Alto(int tarjetas, int columnas, int altoTarjeta, int separacion)
        {
            int filas = Filas(tarjetas, columnas);
            return filas == 0 ? 0 : filas * altoTarjeta + (filas - 1) * separacion;
        }
    }
}
