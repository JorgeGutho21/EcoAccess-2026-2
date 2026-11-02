using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Foto de un residuo tal como llegó, sin decodificar. El servicio comprueba tamaño y formato antes de usarla.</summary>
    /// <param name="TipoMime">Tipo declarado por quien envía la foto; no se confía en él, se comparan los bytes.</param>
    public sealed record ImagenResiduo(byte[] Contenido, string? TipoMime);

    /// <summary>Qué cree ver el clasificador en la foto.</summary>
    public enum TipoPropuestaVision
    {
        /// <summary>Un solo residuo, que corresponde a un código del catálogo.</summary>
        MaterialIdentificado = 1,

        /// <summary>Ve un residuo pero no está seguro de cuál es.</summary>
        Incierta = 2,

        /// <summary>La foto está borrosa, oscura o demasiado lejos para decidir.</summary>
        ImagenBorrosa = 3,

        /// <summary>Hay varios residuos distintos en la foto.</summary>
        VariosObjetos = 4,

        /// <summary>En la foto no aparece ningún residuo.</summary>
        SinResiduo = 5,

        /// <summary>Hay un residuo, pero no es ninguno de los materiales del catálogo.</summary>
        ObjetoDesconocido = 6
    }

    /// <summary>
    /// Respuesta cruda del clasificador. Es una propuesta, no una decisión: <see cref="CodigoMaterial"/> puede no
    /// existir en el catálogo y <see cref="ConfianzaDeclarada"/> es lo que el proveedor dice, sin calibrar.
    /// </summary>
    /// <param name="CodigoMaterial">Solo tiene sentido con <see cref="TipoPropuestaVision.MaterialIdentificado"/>.</param>
    /// <param name="ConfianzaDeclarada">Entre 0 y 1 si el proveedor la informa.</param>
    /// <param name="Explicacion">Texto libre del proveedor; nunca se muestra ni se ejecuta, solo puede registrarse.</param>
    /// <param name="Objeto">Objeto visto, de una lista cerrada; sirve para explicar un residuo que no está en el catálogo.</param>
    public sealed record PropuestaVision(TipoPropuestaVision Tipo, string? CodigoMaterial, decimal? ConfianzaDeclarada, string? Explicacion = null,
        ObjetoVisible? Objeto = null);

    /// <summary>
    /// Qué objeto ve el modelo, aparte del material del catálogo («bolsa plástica» no es un material de Oracle). Es una
    /// lista cerrada: el mensaje a la persona lo redacta EcoAccess, nunca el texto libre del modelo.
    /// </summary>
    public enum ObjetoVisible
    {
        BotellaPlastica = 1,
        BolsaPlastica = 2,
        PlasticoFlexible = 3,
        EnvasePlasticoRigido = 4,
        Caja = 5,
        Carton = 6,
        Papel = 7,
        PapelCometa = 8,
        EnvaseVidrio = 9,
        Lata = 10,
        ObjetoMetalico = 11,
        Empaque = 12,
        EnvaseCompuesto = 13,
        ResiduoOrganico = 14,
        Icopor = 15,
        PilaOBateria = 16,
        Desconocido = 17,
        Medicamento = 18,
        Bombillo = 19,
        Aparato = 20
    }

    /// <summary>
    /// Clasificador de fotos de residuos, independiente del proveedor (simulador, modelo local o API externa).
    /// Recibe el catálogo habilitado para que solo proponga códigos de ahí; aun así, el servicio vuelve a
    /// comprobar el código. No tiene acceso a repositorios, a SQL ni a la caneca.
    /// </summary>
    public interface IClasificadorMaterial
    {
        /// <summary>Nombre del proveedor para trazabilidad (por ejemplo «simulador»); no es un secreto.</summary>
        string Proveedor { get; }

        /// <exception cref="OperationCanceledException">Si se cancela o vence el tiempo.</exception>
        Task<PropuestaVision> ClasificarAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken cancellationToken = default);
    }
}
