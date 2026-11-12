using System.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Qué concluye el servicio sobre una foto, después de validar la propuesta del clasificador.</summary>
    public enum ResultadoClasificacion
    {
        /// <summary>Un material del catálogo con confianza declarada igual o superior al umbral.</summary>
        Identificado = 1,

        /// <summary>No hay una propuesta utilizable; la persona elige el material.</summary>
        Incierta = 2,

        ImagenBorrosa = 3,
        VariosObjetos = 4,
        SinResiduo = 5,

        /// <summary>El residuo no está en el catálogo, o el clasificador propuso un código que no existe.</summary>
        ObjetoDesconocido = 6,

        /// <summary>El clasificador falló o no respondió a tiempo.</summary>
        ProveedorNoDisponible = 7,
        CuotaGratuitaAgotada = 8,

        /// <summary>El modelo local falló y la foto solo podría seguir hacia un servicio externo que la persona no autorizó.</summary>
        AutorizacionExternaRequerida = 9,

        /// <summary>Se reconoce lo que aparece, pero esta caneca no lo recibe o no es un residuo. No es una abstención.</summary>
        ObjetoReconocido = 10
    }

    /// <summary>
    /// Conclusión sobre una foto. Es una sugerencia: no autoriza la apertura, no confirma el material y no otorga
    /// puntos. Solo <see cref="ResultadoClasificacion.Identificado"/> trae <see cref="Consulta"/>.
    /// </summary>
    /// <param name="Confianza">La confianza que declaró el proveedor, redondeada a tres decimales; no está calibrada.</param>
    /// <param name="Mensaje">Texto para la persona, armado por el servicio; nunca incluye texto del proveedor.</param>
    public sealed record ClasificacionImagen(
        ResultadoClasificacion Resultado,
        ConsultaMaterial? Consulta,
        decimal? Confianza,
        string Proveedor,
        string Mensaje,
        ObjetoVisible? Objeto = null,
        EstadoObservable Estado = EstadoObservable.NoDeterminado,
        ObjetoConocido? Reconocido = null,
        bool Corroborada = true)
    {
        /// <summary>Lo detectado en lenguaje corriente, redactado por EcoAccess: «una botella plástica». Null sin material.</summary>
        public string? FraseDeteccion => Reconocido is { } visto ? visto.Frase : Consulta is not { } consulta ? null
            : Objeto is { } objeto && objeto != ObjetoVisible.Desconocido ? ServicioClasificacionImagen.NombreObjeto(objeto)
            : ServicioClasificacionImagen.NombreMaterial(consulta.Material);
    }

    /// <summary>
    /// Clasificación de fotos de residuos. Valida la foto, pide una propuesta a
    /// <see cref="IClasificadorMaterial"/> y la contrasta con el catálogo habilitado: un código que no existe se trata
    /// como objeto desconocido, una confianza baja o ausente como incierta y un fallo del proveedor nunca se convierte
    /// en una sugerencia. No abre la caneca, no ejecuta SQL ni comandos, y no registra nada: la sugerencia la guarda
    /// quien llama con <see cref="ServicioInteracciones.RegistrarSugerenciaAsync"/>, y la persona siempre confirma.
    /// </summary>
    public sealed class ServicioClasificacionImagen
    {
        /// <summary>Tamaño máximo de la foto aceptada.</summary>
        public const int MaximoBytes = 5 * 1024 * 1024;

        /// <summary>
        /// Umbral provisional sobre la confianza declarada. No sale de una calibración: se revisa cuando haya
        /// una evaluación con un conjunto etiquetado real.
        /// </summary>
        public const decimal UmbralConfianza = 0.70m;

        /// <summary>Tiempo máximo de espera al proveedor si no se indica otro.</summary>
        public static readonly TimeSpan TiempoMaximoPredeterminado = TimeSpan.FromSeconds(20);

        private static readonly byte[] FirmaPng = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private readonly IClasificadorMaterial _clasificador;
        private readonly IRepositorioMateriales _materiales;
        private readonly ServicioCatalogoAmbiental _catalogo;
        private readonly TimeSpan _tiempoMaximo;

        public ServicioClasificacionImagen(
            IClasificadorMaterial clasificador,
            IRepositorioMateriales materiales,
            ServicioCatalogoAmbiental catalogo,
            TimeSpan? tiempoMaximo = null)
        {
            _clasificador = clasificador ?? throw new ArgumentNullException(nameof(clasificador));
            _materiales = materiales ?? throw new ArgumentNullException(nameof(materiales));
            _catalogo = catalogo ?? throw new ArgumentNullException(nameof(catalogo));
            _tiempoMaximo = tiempoMaximo ?? TiempoMaximoPredeterminado;
            if (_tiempoMaximo <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(tiempoMaximo), "El tiempo máximo debe ser positivo.");
            }
        }

        /// <summary>
        /// Clasifica la foto y, si se indica una caneca, dice además si esa caneca recibe el material. Una foto vacía,
        /// demasiado grande o que no sea JPEG, PNG o WebP se rechaza con <see cref="MotivoRechazo.DatosInvalidos"/> sin
        /// llegar al proveedor.
        /// </summary>
        /// <exception cref="OperationCanceledException">Si quien llama cancela; el vencimiento del tiempo no lanza.</exception>
        public async Task<Resultado<ClasificacionImagen>> ClasificarAsync(
            ImagenResiduo imagen,
            int? idContenedor = null,
            CancellationToken cancellationToken = default)
        {
            Compat.ThrowIfNull(imagen, nameof(imagen));
            // Se valida y se envía una copia: quien llama no puede cambiar los bytes entre la validación y el envío.
            byte[]? contenido = imagen.Contenido is { Length: <= MaximoBytes } original ? (byte[])original.Clone() : null;
            string? tipoMime = TipoMimeReal(contenido);
            if (tipoMime is null)
            {
                return Resultado<ClasificacionImagen>.Rechazo(MotivoRechazo.DatosInvalidos,
                    $"La foto debe ser JPEG, PNG o WebP y pesar como máximo {MaximoBytes / (1024 * 1024)} MB.");
            }

            IReadOnlyList<Material> habilitados = await _catalogo.ListarHabilitadosAsync(cancellationToken);
            if (habilitados.Count == 0)
            {
                return Resultado<ClasificacionImagen>.Rechazo(MotivoRechazo.EstadoNoPermitido, "No hay materiales habilitados en el catálogo.");
            }

            PropuestaVision? propuesta;
            try { propuesta = await PedirPropuestaAsync(new ImagenResiduo(contenido!, tipoMime, imagen.Envio), habilitados, cancellationToken); }
            catch (CuotaVisionAgotadaException) { return Concluir(ResultadoClasificacion.CuotaGratuitaAgotada); }
            catch (VisionExternaNoAutorizadaException) { return Concluir(ResultadoClasificacion.AutorizacionExternaRequerida); }
            if (propuesta is null)
            {
                return Concluir(ResultadoClasificacion.ProveedorNoDisponible);
            }

            Resultado<ClasificacionImagen> conclusion = await ConcluirPropuestaAsync(propuesta, habilitados, idContenedor, cancellationToken);
            return conclusion.Exitoso && propuesta.Proveedor is not null
                ? Resultado<ClasificacionImagen>.Exito(conclusion.Valor with { Proveedor = propuesta.Proveedor })
                : conclusion;
        }

        /// <summary>Contrasta la propuesta con el catálogo: el modelo propone y aquí se decide qué se le dice a la persona.</summary>
        private async Task<Resultado<ClasificacionImagen>> ConcluirPropuestaAsync(PropuestaVision propuesta, IReadOnlyList<Material> habilitados,
            int? idContenedor, CancellationToken cancellationToken)
        {
            if (propuesta.ClaveObjeto is not null)
                return await ConcluirObjetoAsync(propuesta, habilitados, idContenedor, cancellationToken);

            switch (propuesta.Tipo)
            {
                case TipoPropuestaVision.MaterialIdentificado:
                    break;
                case TipoPropuestaVision.ImagenBorrosa:
                    return Concluir(ResultadoClasificacion.ImagenBorrosa);
                case TipoPropuestaVision.VariosObjetos:
                    return Concluir(ResultadoClasificacion.VariosObjetos);
                case TipoPropuestaVision.SinResiduo:
                    return Concluir(ResultadoClasificacion.SinResiduo);
                case TipoPropuestaVision.ObjetoDesconocido:
                    return Concluir(ResultadoClasificacion.ObjetoDesconocido, objeto: propuesta.Objeto);
                default:
                    return Concluir(ResultadoClasificacion.Incierta);
            }

            // Solo vale un código que el servicio encuentre en el catálogo habilitado; cualquier otro es una alucinación.
            string codigo = propuesta.CodigoMaterial?.Trim().ToUpperInvariant() ?? "";
            Material? material = habilitados.FirstOrDefault(m => string.Equals(m.Codigo, codigo, StringComparison.Ordinal));
            if (material is null)
            {
                return Concluir(ResultadoClasificacion.ObjetoDesconocido, objeto: propuesta.Objeto);
            }

            if (propuesta.ConfianzaDeclarada is not { } declarada || declarada < 0m || declarada > 1m)
            {
                return Concluir(ResultadoClasificacion.Incierta);
            }

            // El umbral se compara con lo declarado; el redondeo es solo para guardar (CONFIANZA_SUGERIDA tiene tres decimales).
            decimal confianza = decimal.Round(declarada, 3, MidpointRounding.AwayFromZero);
            if (declarada < UmbralConfianza)
            {
                return Concluir(ResultadoClasificacion.Incierta, confianza: confianza);
            }

            Resultado<ConsultaMaterial> consulta = await _catalogo.ConsultarAsync(material.Id, idContenedor, cancellationToken);
            if (!consulta.Exitoso)
            {
                // El material se deshabilitó entre la lectura del catálogo y la consulta.
                return Concluir(ResultadoClasificacion.ObjetoDesconocido);
            }

            return Concluir(ResultadoClasificacion.Identificado, consulta.Valor, confianza, propuesta.Objeto, propuesta.Estado);
        }

        /// <summary>
        /// Primero qué se ve; después, si puede ser un material, qué dice Oracle. Lo que no se admite recibe una respuesta
        /// propia y nunca llega a la caneca. La confianza declarada no autoriza nada: solo una certeza baja lleva a abstenerse.
        /// </summary>
        private async Task<Resultado<ClasificacionImagen>> ConcluirObjetoAsync(PropuestaVision propuesta, IReadOnlyList<Material> habilitados,
            int? idContenedor, CancellationToken cancellationToken)
        {
            if (CatalogoObjetos.Buscar(propuesta.ClaveObjeto) is not { } visto) return Concluir(ResultadoClasificacion.Incierta);
            if (visto.Clave == CatalogoObjetos.NoDistinguible) return Concluir(ResultadoClasificacion.ImagenBorrosa);
            if (visto.Clave == CatalogoObjetos.VariosObjetos) return Concluir(ResultadoClasificacion.VariosObjetos);
            Resultado<ClasificacionImagen> Reconocido(ConsultaMaterial? consulta = null) => Resultado<ClasificacionImagen>.Exito(
                new ClasificacionImagen(ResultadoClasificacion.ObjetoReconocido, consulta, propuesta.ConfianzaDeclarada, _clasificador.Proveedor, visto.Mensaje, visto.Objeto, propuesta.Estado, visto, false));

            Material? material = visto.CodigoMaterial is { } codigo ? habilitados.FirstOrDefault(m => m.Codigo == codigo) : null;
            if (material is null) return Reconocido();
            // Una foto solo lleva a la caneca lo aprovechable, aunque la caneca admita más en Oracle: hoy no existe un contenedor
            // aparte para sanitarios, electrónicos u orgánicos. Al instalarse uno, esta es la regla que hay que revisar.
            if (visto.Contexto != ContextoObjeto.ResiduoAprovechable) return Reconocido();
            if (propuesta.ConfianzaDeclarada is < UmbralConfianza) return Concluir(ResultadoClasificacion.Incierta);
            Resultado<ConsultaMaterial> oracle = await _catalogo.ConsultarAsync(material.Id, idContenedor, cancellationToken);
            if (!oracle.Exitoso) return Reconocido();
            // Objetos que el modelo no distingue de un residuo NO aprovechable parecido: un «envase plástico» puede ser icopor
            // y una «bolsa plástica» puede ser un empaque metalizado. Se orienta, pero no se ofrece depositar ni abrir.
            if (MensajesObjetosConfundibles.TryGetValue(visto.Clave, out string? aviso))
                return Resultado<ClasificacionImagen>.Exito(new ClasificacionImagen(ResultadoClasificacion.ObjetoReconocido, oracle.Valor,
                    propuesta.ConfianzaDeclarada, _clasificador.Proveedor, aviso, visto.Objeto, propuesta.Estado, visto, false));
            if (oracle.Valor.Admision is not (AdmisionMaterial.Admitido or AdmisionMaterial.SinCaneca)
                || (oracle.Valor.Admision == AdmisionMaterial.SinCaneca && visto.Contexto != ContextoObjeto.ResiduoAprovechable))
                return Reconocido(oracle.Valor);
            // Una segunda lectura que ve algo sanitario o peligroso donde la primera vio un envase: ninguna de las dos es fiable.
            if (propuesta.CodigoMaterial is { } lectura && !VistasCompatibles(visto, lectura) && EsLecturaDeRiesgo(lectura, habilitados))
                return Concluir(ResultadoClasificacion.Incierta);
            bool corroborada = propuesta.CodigoMaterial is { } segunda && VistasCompatibles(visto, segunda);
            decimal confianza = propuesta.ConfianzaDeclarada ?? UmbralConfianza;
            return Resultado<ClasificacionImagen>.Exito(new ClasificacionImagen(ResultadoClasificacion.Identificado, oracle.Valor, decimal.Round(confianza, 3, MidpointRounding.AwayFromZero),
                _clasificador.Proveedor, Mensaje(ResultadoClasificacion.Identificado, oracle.Valor, visto.Objeto, propuesta.Estado), visto.Objeto, propuesta.Estado, visto, corroborada));
        }

        /// <summary>
        /// En la ronda visual 5 (docs/vision/EVALUACION_RONDA5_INDEPENDIENTE.md) las 5 ofertas por «plastic_container» fueron
        /// indebidas (4 icopor y 1 cartón) y la única por «plastic_bag» era un empaque metalizado: para estos objetos la foto
        /// no basta. La persona elige el material si de verdad es aprovechable; la caneca nunca se abre por la foto.
        /// </summary>
        internal static readonly IReadOnlyDictionary<string, string> MensajesObjetosConfundibles = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["plastic_container"] = "♻️ Parece un envase plástico. Si es icopor (espuma blanca), no es aprovechable: va a la caneca negra. " +
                                    "Si es plástico rígido limpio y seco, va a la blanca: elige el material en el menú para depositarlo.",
            ["plastic_bag"] = "♻️ Parece una bolsa o película plástica. Si es brillante o metalizada (paquete de mecato), no es aprovechable: " +
                              "va a la caneca negra. Si es plástico limpio y seco, elige el material en el menú para depositarlo.",
        };

        /// <summary>Si la lectura por material dice lo mismo que el objeto visto. Un envase genérico no contradice a una botella.</summary>
        private static bool VistasCompatibles(ObjetoConocido visto, string codigoLeido) =>
            codigoLeido == visto.CodigoMaterial || (codigoLeido == "PLASTICO_SIN_ID" && visto.Clave is "plastic_bottle" or "plastic_container");

        private static bool EsLecturaDeRiesgo(string codigo, IReadOnlyList<Material> habilitados) =>
            habilitados.FirstOrDefault(m => m.Codigo == codigo) is { } leido && codigo != "PLASTICO_SIN_ID" && codigo != "OBJETO_COMPUESTO"
            && leido.Categoria is CategoriaMaterial.NoAprovechable or CategoriaMaterial.Peligroso;

        /// <summary>
        /// «image/jpeg», «image/png» o «image/webp» según los primeros bytes, o null si el contenido está vacío, supera
        /// <see cref="MaximoBytes"/> o no tiene ninguna de esas firmas. El tipo declarado por quien envía no cuenta.
        /// </summary>
        public static string? TipoMimeReal(byte[]? contenido)
        {
            if (contenido is null || contenido.Length < 12 || contenido.Length > MaximoBytes)
            {
                return null;
            }

            ReadOnlySpan<byte> bytes = contenido;
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                return "image/jpeg";
            }

            if (bytes.StartsWith(FirmaPng))
            {
                return "image/png";
            }

            return bytes[..4].SequenceEqual(Encoding.UTF8.GetBytes("RIFF")) && bytes[8..12].SequenceEqual(Encoding.UTF8.GetBytes("WEBP")) ? "image/webp" : null;
        }

        /// <summary>
        /// La propuesta del proveedor, o null si lanzó una excepción, devolvió null o no respondió dentro del tiempo
        /// máximo. El tiempo se impone también a un proveedor que ignore el token. La cancelación pedida por quien llama
        /// se propaga como <see cref="OperationCanceledException"/>, aunque el proveedor reaccione con otra excepción.
        /// </summary>
        private async Task<PropuestaVision?> PedirPropuestaAsync(ImagenResiduo imagen, IReadOnlyList<Material> catalogo, CancellationToken cancellationToken)
        {
            using var limite = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            limite.CancelAfter(_tiempoMaximo);
            try
            {
                return await _clasificador.ClasificarAsync(imagen, catalogo, limite.Token).WaitAsync(_tiempoMaximo, cancellationToken);
            }
            catch (Exception ex) when (cancellationToken.IsCancellationRequested)
            {
                if (ex is OperationCanceledException)
                {
                    throw;
                }

                throw new OperationCanceledException("La clasificación se canceló.", ex, cancellationToken);
            }
            catch (CuotaVisionAgotadaException) { throw; }
            catch (VisionExternaNoAutorizadaException) { throw; }
            catch (Exception)
            {
                // Un fallo del proveedor (red, cuota, respuesta mal formada, tiempo vencido) no debe tumbar el flujo:
                // la persona sigue pudiendo elegir el material a mano.
                return null;
            }
        }

        private Resultado<ClasificacionImagen> Concluir(ResultadoClasificacion resultado, ConsultaMaterial? consulta = null, decimal? confianza = null,
            ObjetoVisible? objeto = null, EstadoObservable estado = EstadoObservable.NoDeterminado) =>
            Resultado<ClasificacionImagen>.Exito(new ClasificacionImagen(resultado, consulta, confianza, _clasificador.Proveedor, Mensaje(resultado, consulta, objeto, estado), objeto, estado));

        /// <summary>Texto para la persona: corto y sin nombres de proveedores, modelos ni detalles técnicos.</summary>
        private static string Mensaje(ResultadoClasificacion resultado, ConsultaMaterial? consulta, ObjetoVisible? objeto, EstadoObservable estado) => resultado switch
        {
            // Sucio ya no se recupera como material limpio: se avisa antes de confirmar, sin cambiar lo que Oracle admite.
            ResultadoClasificacion.Identificado when estado == EstadoObservable.Contaminado && consulta!.Admision == AdmisionMaterial.Admitido
                && consulta.Material.Categoria is CategoriaMaterial.Aprovechable =>
                $"♻️ Parece {consulta.Material.Nombre}, pero se ve sucio o con restos, así que podría no ser aprovechable. Si puedes, límpialo antes.\n¿Es correcto?",
            // Admitido en la caneca no significa reciclable: un residuo sanitario o común nunca se presenta como aprovechable.
            ResultadoClasificacion.Identificado when consulta!.Admision == AdmisionMaterial.Admitido && consulta.Material.Categoria == CategoriaMaterial.NoAprovechable =>
                $"🗑️ Parece {consulta.Material.Nombre}. El catálogo lo maneja como residuo no aprovechable: puedes depositarlo en esta caneca como residuo común.\n¿Es correcto?",
            ResultadoClasificacion.Identificado when consulta!.Guia.ColorCaneca is null && consulta.Admision == AdmisionMaterial.Admitido =>
                $"⚠️ Parece {consulta.Material.Nombre}, un residuo especial o peligroso. {GuiaSeparacion.Orientacion(consulta.Material)}\n¿Es correcto?",
            ResultadoClasificacion.Identificado when consulta!.Guia.ColorCaneca is null =>
                $"⚠️ Parece {consulta.Material.Nombre}, un residuo especial o peligroso. {GuiaSeparacion.Orientacion(consulta.Material)}",
            ResultadoClasificacion.Identificado when consulta!.Admision == AdmisionMaterial.NoAdmitido =>
                $"♻️ La propuesta es {consulta.Material.Nombre}, pero esta caneca no lo recibe. {GuiaSeparacion.Orientacion(consulta.Material)}",
            ResultadoClasificacion.Identificado when consulta!.Admision == AdmisionMaterial.CanecaNoDisponible =>
                $"♻️ Parece {consulta.Material.Nombre}, pero esta caneca no está disponible ahora.",
            ResultadoClasificacion.Identificado when objeto is { } identificado && identificado != ObjetoVisible.Desconocido =>
                $"♻️ Parece que es {NombreObjeto(identificado)}, de material {NombreMaterial(consulta!.Material)}. ¿Es correcto?",
            ResultadoClasificacion.Identificado => $"♻️ ¡Parece {consulta!.Material.Nombre}!\n¿Es correcto?",
            ResultadoClasificacion.ObjetoDesconocido when objeto is { } visto && visto != ObjetoVisible.Desconocido =>
                $"♻️ Parece {NombreObjeto(visto)}, pero no pude confirmar el material con el catálogo. Elígelo manualmente para comprobar si esta caneca lo admite.",
            ResultadoClasificacion.ImagenBorrosa => MensajesTelegram.NoIdentificado,
            ResultadoClasificacion.VariosObjetos => "🤔 Veo varios objetos y ninguno es el principal. Envíame una foto de uno solo, de cerca.",
            ResultadoClasificacion.SinResiduo => "🌱 No veo un residuo en la foto. ¿Intentamos otra?",
            ResultadoClasificacion.CuotaGratuitaAgotada => "🌱 Se agotó la cuota gratuita del reconocimiento de fotos. Puedes elegir el material manualmente.",
            ResultadoClasificacion.ProveedorNoDisponible =>
                "🌱 El reconocimiento de fotos no está disponible ahora. Puedes elegir el material manualmente.",
            ResultadoClasificacion.AutorizacionExternaRequerida => MensajesTelegram.AvisoRespaldoExterno,
            _ => "🌱 No logré identificarlo. ¿Intentamos otra foto o prefieres elegir el material?",
        };

        /// <summary>Material en lenguaje corriente; un código sin traducción usa el nombre del catálogo.</summary>
        internal static string NombreMaterial(Material material) => material.Codigo switch
        {
            "PET" => "plástico PET",
            "LATA_ALUMINIO" => "aluminio",
            "CARTON" => "cartón",
            "PAPEL" => "papel",
            "VIDRIO" => "vidrio",
            "MULTICAPA" => "cartón multicapa",
            "BOLSA_PLASTICA" or "PLASTICO_SIN_ID" => "plástico",
            "METAL" => "metal",
            "ORGANICO" => "orgánico",
            _ => material.Nombre.ToLowerInvariant(),
        };

        /// <summary>Nombre con artículo para «Parece …»; redactado por EcoAccess, no por el modelo.</summary>
        public static string NombreObjeto(ObjetoVisible objeto) => objeto switch
        {
            ObjetoVisible.BotellaPlastica => "una botella plástica",
            ObjetoVisible.BolsaPlastica => "una bolsa plástica",
            ObjetoVisible.PlasticoFlexible => "un plástico flexible",
            ObjetoVisible.EnvasePlasticoRigido => "un envase plástico rígido",
            ObjetoVisible.Caja => "una caja",
            ObjetoVisible.Carton => "cartón",
            ObjetoVisible.Papel => "papel",
            ObjetoVisible.PapelCometa => "papel cometa",
            ObjetoVisible.EnvaseVidrio => "un envase de vidrio",
            ObjetoVisible.Lata => "una lata",
            ObjetoVisible.ObjetoMetalico => "un objeto metálico",
            ObjetoVisible.Empaque => "un empaque",
            ObjetoVisible.EnvaseCompuesto => "un envase de varios materiales",
            ObjetoVisible.ResiduoOrganico => "un residuo orgánico",
            ObjetoVisible.Icopor => "icopor",
            ObjetoVisible.PilaOBateria => "una pila o batería",
            ObjetoVisible.Medicamento => "un medicamento",
            ObjetoVisible.Bombillo => "un bombillo",
            ObjetoVisible.Aparato => "un aparato electrónico",
            ObjetoVisible.ArticuloSanitario => "un residuo sanitario",
            ObjetoVisible.Jeringa => "una jeringa o aguja",
            ObjetoVisible.EmpaqueMetalizado => "un empaque metalizado",
            ObjetoVisible.EnvaseContaminado => "un envase sucio o contaminado",
            _ => "un objeto que no reconozco",
        };
    }
}
