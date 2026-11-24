using System;
using System.Collections.Generic;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>Condición física observable reportada por el modelo visual (nunca decide la apertura por sí sola).</summary>
    public enum EstadoObservableObjeto
    {
        Desconocido = 0,
        LimpioYVacio = 1,
        Contaminado = 2,
        ConLiquido = 3,
        DanadoORoto = 4,
        Reutilizable = 5,
        NuevoSinUso = 6,
    }

    /// <summary>Calidad general de la imagen validada antes o durante el análisis.</summary>
    public enum CalidadImagenVision
    {
        Aceptable = 1,
        Borrosa = 2,
        Oscura = 3,
        Vacia = 4,
        CorruptaOInvalida = 5,
    }

    /// <summary>Decisión emitida exclusivamente por el motor de reglas en C# para cada objeto observado.</summary>
    public enum EstadoClasificacionObjeto
    {
        Clasificado = 1,
        RequiereConfirmacion = 2,
        NoIdentificado = 3,
        NoEsResiduo = 4,
        FueraDeCatalogo = 5,
    }

    /// <summary>Estado global del análisis de una fotografía multirresiduo.</summary>
    public enum EstadoGlobalAnalisis
    {
        Clasificado = 1,
        SinResiduos = 2,
        RequiereConfirmacion = 3,
        RequiereRepeticion = 4,
        NoIdentificado = 5,
        ModoSeguroError = 6,
        Cancelado = 7,
    }

    /// <summary>Las 7 combinaciones no vacías posibles de las tres canecas (C01–C07), más ninguna.</summary>
    public enum CombinacionCanecas
    {
        Ninguna = 0,
        C01_Verde = 1,
        C02_Blanca = 2,
        C03_Negra = 3,
        C04_VerdeBlanca = 4,
        C05_VerdeNegra = 5,
        C06_BlancaNegra = 6,
        C07_VerdeBlancaNegra = 7,
    }

    /// <summary>
    /// Cuadro delimitador normalizado (0..1). Solo debe poblarse cuando un detector verificado entrega coordenadas reales;
    /// jamás se inventan coordenadas a partir de texto libre.
    /// </summary>
    public sealed record CajaDelimitadoraNormalizada(double X, double Y, double Ancho, double Alto, bool Verificada = false);

    /// <summary>Observación cruda y validada de un objeto dentro de la imagen, producida por el modelo visual.</summary>
    public sealed record ObservacionObjetoVisual(
        string NombreDetectado,
        string MaterialObservable,
        EstadoObservableObjeto EstadoObservable,
        int Cantidad,
        decimal Confianza,
        bool EsEtiquetaImpresa = false,
        bool EsPantalla = false,
        bool EsReutilizable = false,
        bool RequiereAclaracion = false,
        CajaDelimitadoraNormalizada? Caja = null);

    /// <summary>Respuesta JSON estructurada y validada recibida del servicio visual.</summary>
    public sealed record RespuestaVisionEstructurada(
        bool JsonValido,
        bool HayResiduos,
        CalidadImagenVision CalidadImagen,
        IReadOnlyList<ObservacionObjetoVisual> Objetos,
        string ModeloUtilizado,
        long LatenciaMs,
        string? ErrorValidacion = null,
        string? JsonOriginalSanitizado = null);

    /// <summary>Objeto individual ya evaluado por el motor de reglas C# contra el catálogo prioritario de 35 materiales.</summary>
    public sealed record ObjetoClasificado35(
        int Ordinal,
        ObservacionObjetoVisual Observacion,
        MaterialPrioritario35? Material35,
        EstadoClasificacionObjeto Estado,
        CategoriaMaterial? CanecaAsignada,
        string Motivo,
        bool RequiereConfirmacion,
        bool ElegibleApertura,
        int Cantidad);

    /// <summary>Resultado consolidado del análisis multirresiduo listo para la interfaz, el gestor de sesión y el registro.</summary>
    public sealed record ResultadoAnalisisMultirresiduo(
        EstadoGlobalAnalisis EstadoGlobal,
        IReadOnlyList<ObjetoClasificado35> Objetos,
        IReadOnlyList<CategoriaMaterial> CanecasRequeridas,
        CombinacionCanecas Combinacion,
        int TotalObjetosDetectados,
        bool RequiereConfirmacion,
        bool PuedeAbrirCanecas,
        string MensajeUsuario,
        string ModeloUtilizado,
        long LatenciaMs,
        string? ErrorTecnico = null,
        string? HashImagenSha256 = null);

    /// <summary>Opciones deterministas del motor de reglas (sin afectar el estado global estático).</summary>
    public sealed record OpcionesMotorReglas(
        decimal UmbralConfianzaMinima = 0.65m,
        bool RutaPeliculaPlasticaDisponible = true,
        bool PermitirAperturaConConfirmacionParcial = false);

    /// <summary>Contrato de validación de calidad visual (nitidez, brillo, tamaño) desacoplado de bibliotecas gráficas.</summary>
    public interface IValidadorCalidadImagen
    {
        CalidadImagenVision Evaluar(byte[] bytesImagen, out string? detalle);
    }

    /// <summary>Contrato del proveedor de observaciones visuales (Ollama/LFM3-VL/Qwen-VL o simulador en pruebas).</summary>
    public interface IProveedorVisionMultirresiduo
    {
        string NombreProveedor { get; }
        System.Threading.Tasks.Task<RespuestaVisionEstructurada> ObservarAsync(byte[] bytesImagen, System.Threading.CancellationToken ct = default);
    }
}
