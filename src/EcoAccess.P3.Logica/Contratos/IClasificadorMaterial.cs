using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Contratos
{

    /// <summary>Foto de un residuo tal como llegó, sin decodificar. El servicio comprueba tamaño y formato antes de usarla.</summary>
    /// <param name="TipoMime">Tipo declarado por quien envía la foto; no se confía en él, se comparan los bytes.</param>
    /// <param name="Envio">Si la foto puede salir del equipo. Por omisión no puede: autorizarlo es una decisión explícita.</param>
    public sealed record ImagenResiduo(byte[] Contenido, string? TipoMime, EnvioExterno Envio = EnvioExterno.Prohibido);

    /// <summary>Qué permite la persona respecto a enviar esta foto a un servicio externo.</summary>
    public enum EnvioExterno
    {
        Prohibido = 0,

        /// <summary>Autorizada: puede enviarse si el motor elegido es externo o si el local no está disponible.</summary>
        Autorizado = 1,

        /// <summary>La persona pidió expresamente la opinión del servicio externo para esta foto.</summary>
        SegundaOpinion = 2
    }

    /// <summary>Estado del residuo a la vista; lo admite o no el catálogo, nunca el modelo.</summary>
    public enum EstadoObservable
    {
        NoDeterminado = 0,
        Limpio = 1,
        Contaminado = 2
    }

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
    /// <param name="Estado">Limpio o contaminado si el proveedor lo informa; solo orienta el mensaje.</param>
    /// <param name="Proveedor">Motor que respondió de verdad cuando hay varios; null si es el único.</param>
    /// <param name="Descripcion">Descripción concreta del objeto; no cambia destino ni admisión.</param>
    /// <param name="ClaveObjeto">Qué se ve, sea o no un residuo (clave de CatalogoObjetos); null si el motor solo clasifica materiales.</param>
    public sealed record PropuestaVision(TipoPropuestaVision Tipo, string? CodigoMaterial, decimal? ConfianzaDeclarada, string? Explicacion = null,
        ObjetoVisible? Objeto = null, EstadoObservable Estado = EstadoObservable.NoDeterminado, string? Proveedor = null, string? ClaveObjeto = null,
        DescripcionObjeto? Descripcion = null);

    /// <summary>Lo concreto que se ve («una manzana verde»), redactado por EcoAccess con palabras de una lista cerrada. Solo es lenguaje.</summary>
    public sealed record DescripcionObjeto(string Frase, string Emoji);

    /// <summary>Qué clase de cosa aparece en la foto. Es independiente de si una caneca la admite: eso lo decide Oracle.</summary>
    public enum ContextoObjeto
    {
        Desconocido = 0,
        ResiduoAprovechable = 1,
        ResiduoNoAprovechable = 2,
        ResiduoOrganico = 3,
        ResiduoSanitario = 4,
        ResiduoElectronico = 5,
        ResiduoEspecial = 6,

        /// <summary>Objeto de uso diario que no se presenta como residuo.</summary>
        ObjetoCotidiano = 7,
        AnimalVivo = 8,
        Persona = 9,
        PlantaViva = 10
    }

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
        Aparato = 20,
        ArticuloSanitario = 21,
        Jeringa = 22,
        EmpaqueMetalizado = 23,
        EnvaseContaminado = 24
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
