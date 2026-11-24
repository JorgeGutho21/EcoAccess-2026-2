using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Servicios;

namespace EcoAccess.P3.Logica.Vision35
{
    /// <summary>
    /// Motor determinista de reglas en C# para clasificación multirresiduo contra el catálogo de los 35 materiales.
    /// El modelo visual produce observaciones; este motor decide la clasificación autorizada, filtra pantallas/etiquetas,
    /// protege objetos reutilizables y peligrosos, solicita confirmación ante incertidumbre y deduplica las canecas
    /// requeridas (p. ej., 5 botellas limpias = 1 sola caneca BLANCA).
    /// </summary>
    public sealed class MotorReglasMultirresiduo
    {
        private static readonly CategoriaMaterial[] OrdenCanecas =
        {
            CategoriaMaterial.OrganicoAprovechable, // VERDE
            CategoriaMaterial.Aprovechable,         // BLANCA
            CategoriaMaterial.NoAprovechable,       // NEGRA
        };

        private static readonly string[] PalabrasEtiquetaImpresa =
        {
            "recycling label", "recycling symbol", "recycling logo", "printed label", "sticker", "sign",
            "etiqueta de reciclaje", "simbolo de reciclaje", "letrero", "afiche", "infografia", "cartel",
        };

        private static readonly string[] PalabrasPantalla =
        {
            "screen", "monitor", "phone screen", "tablet screen", "bin on screen", "trash can on screen", "photo of bin",
            "pantalla", "caneca en pantalla", "imagen de caneca", "caneca en celular", "foto de caneca",
        };

        private static readonly string[] PalabrasReutilizables =
        {
            "ice cube tray", "ice tray", "cubeta de hielo", "cubeta plastica para hielo", "bandeja de hielo", "molde de hielo",
            "reusable bottle", "thermos", "termo", "reusable container", "lunchbox", "lonchera", "tupperware",
        };

        private static readonly string[] PalabrasPeligrosasOEspeciales =
        {
            "battery", "pila", "bateria", "syringe", "jeringa", "needle", "aguja", "medicine", "medicamento", "blister",
            "light bulb", "bombillo", "aerosol", "spray can", "bleach", "cloro", "chemical", "quimico", "broken glass",
            "vidrio roto", "phone", "celular", "charger", "cargador", "usb", "cable usb", "usb cable", "charging cable", "cable", "power cord", "electronic", "raee", "cooking oil", "aceite usado", "expired medicine", "medicamento vencido", "pharmaceutical", "farmaceutico", "pastilla", "pills",
        };

        private static readonly string[] PalabrasNoResiduo =
        {
            "person", "human face", "rostro", "human", "persona", "dog", "cat", "animal", "perro", "gato", "living plant", "planta viva",
            "trash bin", "garbage bin", "recycling bin", "caneca", "contenedor",
        };

        private static readonly string[] PalabrasContaminadoEnNombre =
        {
            "dirty", "greasy", "oily", "soiled", "stained", "contaminated", "used", "sucio", "sucia", "contaminado",
            "contaminada", "grasa", "engrasado", "engrasada", "usado", "usada", "manchado", "manchada", "con comida",
        };

        private static readonly string[] PalabrasConLiquidoEnNombre =
        {
            "full", "liquid", "half full", "con liquido", "con agua", "llena", "lleno", "con bebida", "con contenido",
        };

        private readonly OpcionesMotorReglas _opciones;

        public MotorReglasMultirresiduo(OpcionesMotorReglas? opciones = null)
        {
            _opciones = opciones ?? new OpcionesMotorReglas();
        }

        /// <summary>Evalúa una respuesta JSON validada y produce el dictamen autorizado para el gestor de sesión.</summary>
        public ResultadoAnalisisMultirresiduo Evaluar(RespuestaVisionEstructurada respuesta, string? hashImagenSha256 = null)
        {
            Compat.ThrowIfNull(respuesta, nameof(respuesta));

            if (!respuesta.JsonValido)
            {
                return new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.ModoSeguroError,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0,
                    RequiereConfirmacion: false,
                    PuedeAbrirCanecas: false,
                    MensajeUsuario: "La respuesta del modelo visual no pasó la validación segura (JSON inválido). No se abrirá ninguna caneca. Intenta de nuevo o elige manualmente.",
                    ModeloUtilizado: respuesta.ModeloUtilizado,
                    LatenciaMs: respuesta.LatenciaMs,
                    ErrorTecnico: respuesta.ErrorValidacion,
                    HashImagenSha256: hashImagenSha256);
            }

            if (respuesta.CalidadImagen is CalidadImagenVision.Borrosa or CalidadImagenVision.Oscura or CalidadImagenVision.CorruptaOInvalida)
            {
                string motivoCalidad = respuesta.CalidadImagen switch
                {
                    CalidadImagenVision.Borrosa => "La fotografía está borrosa. Por favor repite la toma enfocando claramente los residuos.",
                    CalidadImagenVision.Oscura => "La fotografía tiene muy poca iluminación. Acerca el residuo a la luz y repite la toma.",
                    _ => "La fotografía es inválida o está dañada. Por favor toma una nueva foto.",
                };
                return new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.RequiereRepeticion,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0,
                    RequiereConfirmacion: false,
                    PuedeAbrirCanecas: false,
                    MensajeUsuario: motivoCalidad,
                    ModeloUtilizado: respuesta.ModeloUtilizado,
                    LatenciaMs: respuesta.LatenciaMs,
                    ErrorTecnico: null,
                    HashImagenSha256: hashImagenSha256);
            }

            if (respuesta.CalidadImagen == CalidadImagenVision.Vacia || !respuesta.HayResiduos || respuesta.Objetos.Count == 0)
            {
                return new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.SinResiduos,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0,
                    RequiereConfirmacion: false,
                    PuedeAbrirCanecas: false,
                    MensajeUsuario: "No se detectaron residuos en la fotografía. Muestra el residuo frente a la cámara o usa la búsqueda manual.",
                    ModeloUtilizado: respuesta.ModeloUtilizado,
                    LatenciaMs: respuesta.LatenciaMs,
                    ErrorTecnico: null,
                    HashImagenSha256: hashImagenSha256);
            }

            var clasificados = new List<ObjetoClasificado35>();
            int ordinal = 0;

            foreach (ObservacionObjetoVisual obs in respuesta.Objetos)
            {
                // T023: etiquetas de reciclaje impresas NO cuentan como objeto adicional.
                if (EsEtiquetaImpresa(obs))
                    continue;

                ordinal++;
                clasificados.Add(ClasificarObjeto(ordinal, obs));
            }

            if (clasificados.Count == 0)
            {
                return new ResultadoAnalisisMultirresiduo(
                    EstadoGlobalAnalisis.SinResiduos,
                    Array.Empty<ObjetoClasificado35>(),
                    Array.Empty<CategoriaMaterial>(),
                    CombinacionCanecas.Ninguna,
                    0,
                    RequiereConfirmacion: false,
                    PuedeAbrirCanecas: false,
                    MensajeUsuario: "Solo se detectaron etiquetas o elementos informativos, sin residuos para depositar.",
                    ModeloUtilizado: respuesta.ModeloUtilizado,
                    LatenciaMs: respuesta.LatenciaMs,
                    HashImagenSha256: hashImagenSha256);
            }

            int totalObjetos = clasificados
                .Where(o => o.Estado != EstadoClasificacionObjeto.NoEsResiduo)
                .Sum(o => o.Cantidad);

            bool hayConfirmacionPendiente = clasificados.Any(o => o.Estado == EstadoClasificacionObjeto.RequiereConfirmacion);
            bool hayNoIdentificados = clasificados.Any(o => o.Estado == EstadoClasificacionObjeto.NoIdentificado);
            var elegibles = clasificados.Where(o => o.ElegibleApertura && o.CanecaAsignada.HasValue).ToList();

            // Deduplicación estricta de canecas: 5 botellas limpias = 1 sola caneca BLANCA.
            var canecasUnicas = OrdenCanecas
                .Where(c => elegibles.Any(o => o.CanecaAsignada == c))
                .ToList();

            CombinacionCanecas combinacion = CalcularCombinacion(canecasUnicas);

            EstadoGlobalAnalisis estadoGlobal;
            bool puedeAbrir;

            if (hayConfirmacionPendiente)
            {
                estadoGlobal = EstadoGlobalAnalisis.RequiereConfirmacion;
                puedeAbrir = _opciones.PermitirAperturaConConfirmacionParcial && canecasUnicas.Count > 0;
            }
            else if (elegibles.Count > 0 && !hayNoIdentificados)
            {
                estadoGlobal = EstadoGlobalAnalisis.Clasificado;
                puedeAbrir = true;
            }
            else if (elegibles.Count > 0 && hayNoIdentificados)
            {
                // Mezcla con un objeto no identificado: exigir confirmación humana antes de abrir.
                estadoGlobal = EstadoGlobalAnalisis.RequiereConfirmacion;
                puedeAbrir = false;
            }
            else if (clasificados.All(o => o.Estado == EstadoClasificacionObjeto.NoEsResiduo))
            {
                estadoGlobal = EstadoGlobalAnalisis.SinResiduos;
                puedeAbrir = false;
            }
            else
            {
                estadoGlobal = EstadoGlobalAnalisis.NoIdentificado;
                puedeAbrir = false;
            }

            // Nunca convertir una respuesta incierta en apertura automática.
            if (!puedeAbrir)
            {
                // Se conservan las canecas recomendadas como orientación si ya hubo objetos clasificados,
                // pero PuedeAbrirCanecas queda en false hasta que el ciudadano confirme.
            }

            string mensaje = ConstruirMensajeUsuario(estadoGlobal, clasificados, canecasUnicas, puedeAbrir);

            return new ResultadoAnalisisMultirresiduo(
                estadoGlobal,
                clasificados,
                canecasUnicas,
                combinacion,
                totalObjetos,
                RequiereConfirmacion: estadoGlobal == EstadoGlobalAnalisis.RequiereConfirmacion,
                PuedeAbrirCanecas: puedeAbrir,
                MensajeUsuario: mensaje,
                ModeloUtilizado: respuesta.ModeloUtilizado,
                LatenciaMs: respuesta.LatenciaMs,
                ErrorTecnico: null,
                HashImagenSha256: hashImagenSha256);
        }

        /// <summary>Calcula el código oficial de combinación (C01..C07 o Ninguna) a partir de las categorías únicas.</summary>
        public static CombinacionCanecas CalcularCombinacion(IReadOnlyCollection<CategoriaMaterial> canecas)
        {
            bool v = canecas.Contains(CategoriaMaterial.OrganicoAprovechable);
            bool b = canecas.Contains(CategoriaMaterial.Aprovechable);
            bool n = canecas.Contains(CategoriaMaterial.NoAprovechable);

            return (v, b, n) switch
            {
                (true, false, false) => CombinacionCanecas.C01_Verde,
                (false, true, false) => CombinacionCanecas.C02_Blanca,
                (false, false, true) => CombinacionCanecas.C03_Negra,
                (true, true, false) => CombinacionCanecas.C04_VerdeBlanca,
                (true, false, true) => CombinacionCanecas.C05_VerdeNegra,
                (false, true, true) => CombinacionCanecas.C06_BlancaNegra,
                (true, true, true) => CombinacionCanecas.C07_VerdeBlancaNegra,
                _ => CombinacionCanecas.Ninguna,
            };
        }

        private ObjetoClasificado35 ClasificarObjeto(int ordinal, ObservacionObjetoVisual obs)
        {
            string nombreNorm = CatalogoPrototipo.Normalizar(obs.NombreDetectado);
            string matNorm = CatalogoPrototipo.Normalizar(obs.MaterialObservable);
            string combinado = (nombreNorm + " " + matNorm).Trim();

            // T024: imagen de caneca en pantalla o elemento no residuo.
            if (obs.EsPantalla || ContienePatron(combinado, PalabrasPantalla))
            {
                return new ObjetoClasificado35(ordinal, obs, null, EstadoClasificacionObjeto.NoEsResiduo, null,
                    "Objeto mostrado en una pantalla o fotografía de caneca: no se clasifica como residuo físico.", false, false, obs.Cantidad);
            }

            // Residuos peligrosos o especiales (RAEE, cable USB, medicamentos/blíster, vidrio roto): prioridad máxima; jamás abrir caneca ordinaria.
            bool esVidrioRoto = obs.EstadoObservable == EstadoObservableObjeto.DanadoORoto &&
                                (combinado.Contains("glass") || combinado.Contains("vidrio"));
            bool esCajaCartonVacia = (nombreNorm.Contains("box") || nombreNorm.Contains("caja"))
                && !nombreNorm.Contains("blister") && !nombreNorm.Contains("pill") && !nombreNorm.Contains("tablet")
                && !nombreNorm.Contains("pastilla") && !nombreNorm.Contains("battery") && !nombreNorm.Contains("pila")
                && !nombreNorm.Contains("syringe") && !nombreNorm.Contains("jeringa")
                && !nombreNorm.Contains("vencid") && !nombreNorm.Contains("expired");
            if ((!esCajaCartonVacia && ContienePatron(combinado, PalabrasPeligrosasOEspeciales)) || esVidrioRoto)
            {
                return new ObjetoClasificado35(ordinal, obs, null, EstadoClasificacionObjeto.FueraDeCatalogo, null,
                    "GESTION_ESPECIAL: residuo especial, RAEE, farmacéutico o peligroso. No abrir ninguna de las tres canecas ordinarias; entregar en punto de recolección posconsumo/especializado.", false, false, obs.Cantidad);
            }

            // Sección 4: cubeta plástica para hielo u otro artículo duradero reutilizable.
            // Si el modelo nombró explícitamente un empaque desechable del catálogo 35 (p. ej. caja de cartón o botella PET)
            // y marcó es_reutilizable=true por ser reciclable, no se descarta como no-residuo.
            bool coincidePatronReutilizable = ContienePatron(combinado, PalabrasReutilizables);
            bool marcadoReutilizable = obs.EsReutilizable || obs.EstadoObservable == EstadoObservableObjeto.Reutilizable;
            if (coincidePatronReutilizable || (marcadoReutilizable && ResolverMaterial35(nombreNorm, matNorm, EstadoObservableObjeto.LimpioYVacio) is null))
            {
                return new ObjetoClasificado35(ordinal, obs, null, EstadoClasificacionObjeto.NoEsResiduo, null,
                    "Objeto reutilizable (por ejemplo, cubeta para hielo aún útil): no debe clasificarse automáticamente como basura.", false, false, obs.Cantidad);
            }

            if (ContienePatron(combinado, PalabrasNoResiduo))
            {
                return new ObjetoClasificado35(ordinal, obs, null, EstadoClasificacionObjeto.NoEsResiduo, null,
                    "No corresponde a un residuo depositable.", false, false, obs.Cantidad);
            }

            // Resolver estado efectivo combinando campo estructurado + pistas léxicas del nombre.
            EstadoObservableObjeto estadoEfectivo = obs.EstadoObservable;
            if (estadoEfectivo == EstadoObservableObjeto.Desconocido)
            {
                if (ContienePatron(nombreNorm, PalabrasConLiquidoEnNombre))
                    estadoEfectivo = EstadoObservableObjeto.ConLiquido;
                else if (ContienePatron(nombreNorm, PalabrasContaminadoEnNombre))
                    estadoEfectivo = EstadoObservableObjeto.Contaminado;
            }

            // Coincidencia contra el catálogo prioritario de 35 materiales.
            MaterialPrioritario35? material = ResolverMaterial35(nombreNorm, matNorm, estadoEfectivo);
            if (material is null)
            {
                // Clasificación segura de orgánicos reales del campus (manzana, cebolla) e higiene no aprovechable
                // (hilo dental usado, máquina de afeitar desechable, esponja usada) sin alterar los 35 códigos oficiales.
                bool esOrganicoExtra = nombreNorm.Contains("manzana") || nombreNorm.Contains("apple")
                    || nombreNorm.Contains("cebolla") || nombreNorm.Contains("onion")
                    || nombreNorm.Contains("mango") || nombreNorm.Contains("guayaba") || nombreNorm.Contains("citrus") || nombreNorm.Contains("citrico") || nombreNorm.Contains("fruta");
                bool esHigieneNegraExtra = nombreNorm.Contains("hilo dental") || nombreNorm.Contains("dental floss") || nombreNorm.Contains("floss")
                    || nombreNorm.Contains("maquina de afeitar") || nombreNorm.Contains("rastrillo") || nombreNorm.Contains("razor")
                    || nombreNorm.Contains("esponja") || nombreNorm.Contains("sponge");

                if ((esOrganicoExtra || esHigieneNegraExtra) && obs.Confianza >= _opciones.UmbralConfianzaMinima && !obs.RequiereAclaracion)
                {
                    CategoriaMaterial catExtra = esOrganicoExtra ? CategoriaMaterial.OrganicoAprovechable : CategoriaMaterial.NoAprovechable;
                    string nomCaneca = NombreCortoCaneca(catExtra);
                    return new ObjetoClasificado35(ordinal, obs, null, EstadoClasificacionObjeto.Clasificado, catExtra,
                        $"Clasificado como '{obs.NombreDetectado}' ({(esOrganicoExtra ? "orgánico real" : "higiene no aprovechable")}) → Caneca {nomCaneca}.",
                        RequiereConfirmacion: false, ElegibleApertura: true, Cantidad: obs.Cantidad);
                }

                return new ObjetoClasificado35(ordinal, obs, null, EstadoClasificacionObjeto.NoIdentificado, null,
                    $"Objeto '{obs.NombreDetectado}' no coincide con los 35 residuos prioritarios (MATERIAL NO IDENTIFICADO).", false, false, obs.Cantidad);
            }

            // Regla: baja confianza o aclaración explícita solicitada por el modelo -> REQUIERE_CONFIRMACION.
            if (obs.Confianza < _opciones.UmbralConfianzaMinima || obs.RequiereAclaracion)
            {
                return new ObjetoClasificado35(ordinal, obs, material, EstadoClasificacionObjeto.RequiereConfirmacion, material.Caneca,
                    $"Confianza insuficiente ({obs.Confianza:P0}) o imagen ambigua para '{material.Nombre}': requiere confirmación manual.", true, false, obs.Cantidad);
            }

            // T022: envase o botella con líquido desconocido -> REQUIERE_CONFIRMACION.
            if (estadoEfectivo == EstadoObservableObjeto.ConLiquido)
            {
                return new ObjetoClasificado35(ordinal, obs, material, EstadoClasificacionObjeto.RequiereConfirmacion, null,
                    $"'{material.Nombre}' parece contener líquido o contenido desconocido: vacíalo y confirma antes de depositar.", true, false, obs.Cantidad);
            }

            // Producto sanitario nuevo sin usar -> no clasificar como basura automáticamente.
            if (estadoEfectivo == EstadoObservableObjeto.NuevoSinUso && material.Caneca == CategoriaMaterial.NoAprovechable)
            {
                return new ObjetoClasificado35(ordinal, obs, material, EstadoClasificacionObjeto.RequiereConfirmacion, null,
                    $"'{material.Nombre}' parece nuevo sin usar: confirma si realmente deseas desecharlo.", true, false, obs.Cantidad);
            }

            // Regla de estado sobre aprovechables (BLANCA): si está contaminado -> reclasificar a NEGRA; si el estado es desconocido -> REQUIERE_CONFIRMACION.
            if (material.Caneca == CategoriaMaterial.Aprovechable)
            {
                if (estadoEfectivo == EstadoObservableObjeto.Contaminado)
                {
                    MaterialPrioritario35 equivalenteNegra = MapearAprovechableContaminadoANegra(material);
                    return new ObjetoClasificado35(ordinal, obs, equivalenteNegra, EstadoClasificacionObjeto.Clasificado, CategoriaMaterial.NoAprovechable,
                        $"Detectado contaminado/sucio: reclasificado por reglas a {equivalenteNegra.Codigo} ({equivalenteNegra.Nombre}) → NEGRA.", false, true, obs.Cantidad);
                }

                if (material.RequiereRutaAprovechamiento)
                {
                    string motivoPelicula = _opciones.RutaPeliculaPlasticaDisponible
                        ? "Película plástica flexible (B13): puede confundirse visualmente con envolturas no aprovechables; confirma que esté limpia y seca antes de abrir BLANCA."
                        : "La ruta de aprovechamiento para película plástica no está habilitada en esta estación: confirmar disposición en NEGRA.";
                    CategoriaMaterial sugerida = _opciones.RutaPeliculaPlasticaDisponible ? CategoriaMaterial.Aprovechable : CategoriaMaterial.NoAprovechable;
                    return new ObjetoClasificado35(ordinal, obs, material, EstadoClasificacionObjeto.RequiereConfirmacion, sugerida,
                        motivoPelicula, true, false, obs.Cantidad);
                }

                if (material.RequiereValidacionEstado && estadoEfectivo == EstadoObservableObjeto.Desconocido)
                {
                    return new ObjetoClasificado35(ordinal, obs, material, EstadoClasificacionObjeto.RequiereConfirmacion, CategoriaMaterial.Aprovechable,
                        $"No se pudo confirmar visualmente si '{material.Nombre}' está limpio y vacío. Confirma su estado para habilitar la caneca BLANCA.", true, false, obs.Cantidad);
                }
            }

            string avisoSanitario = material.EsSanitarioSimuladoEnStand
                ? " (En pruebas físicas de stand utilizar representación limpia artificial)."
                : "";

            return new ObjetoClasificado35(
                ordinal,
                obs,
                material,
                EstadoClasificacionObjeto.Clasificado,
                material.Caneca,
                $"Clasificado como {material.Codigo} ({material.Nombre}) → Caneca {material.NombreCaneca}.{avisoSanitario}",
                RequiereConfirmacion: false,
                ElegibleApertura: true,
                Cantidad: obs.Cantidad);
        }

        private static MaterialPrioritario35? ResolverMaterial35(string nombreNorm, string matNorm, EstadoObservableObjeto estado)
        {
            if (string.IsNullOrWhiteSpace(nombreNorm))
                return null;

            // Reglas de desambiguación explícitas antes de puntuación general (evitan confundir limpio vs contaminado).
            bool contaminado = estado == EstadoObservableObjeto.Contaminado || ContienePatron(nombreNorm, PalabrasContaminadoEnNombre);

            // Pañuelos / tissue paper son sanitarios (NEGRA N01), nunca papel de cuaderno limpio (B08).
            if (nombreNorm.Contains("tissue") || nombreNorm.Contains("panuelo") || nombreNorm.Contains("napkin") || nombreNorm.Contains("servilleta"))
                return CatalogoPrioritario35.BuscarPorCodigo("N01");

            // Orgánicos reales del campus fuera de los 10 códigos base (cebolla, manzana, cítrico genérico) no deben confundirse por puntuación parcial.
            if (nombreNorm.Contains("onion") || nombreNorm.Contains("cebolla") || nombreNorm.Contains("apple") || nombreNorm.Contains("manzana") || nombreNorm.Contains("citrus") || nombreNorm.Contains("citrico"))
                return null;

            // Envases multicapa (Tetra Pak de jugo/leche) deben evaluarse ANTES de "carton" genérico (B09).
            if (nombreNorm.Contains("jugo") || nombreNorm.Contains("juice") || nombreNorm.Contains("hit"))
            {
                if (nombreNorm.Contains("caja") || nombreNorm.Contains("carton") || nombreNorm.Contains("box") || nombreNorm.Contains("tetra"))
                    return CatalogoPrioritario35.BuscarPorCodigo("B10");
            }

            if (nombreNorm.Contains("leche") || nombreNorm.Contains("milk"))
            {
                if (nombreNorm.Contains("caja") || nombreNorm.Contains("carton") || nombreNorm.Contains("box") || nombreNorm.Contains("tetra"))
                    return CatalogoPrioritario35.BuscarPorCodigo("B11");
            }

            if (nombreNorm.Contains("carton") || nombreNorm.Contains("cardboard") || nombreNorm.Contains("pizza box"))
            {
                return contaminado || nombreNorm.Contains("pizza")
                    ? CatalogoPrioritario35.BuscarPorCodigo("N03")
                    : CatalogoPrioritario35.BuscarPorCodigo("B09");
            }

            if (nombreNorm.Contains("vaso") || nombreNorm.Contains("cup"))
            {
                if (nombreNorm.Contains("icopor") || nombreNorm.Contains("foam") || nombreNorm.Contains("styrofoam"))
                    return CatalogoPrioritario35.BuscarPorCodigo("N07");
                return contaminado
                    ? CatalogoPrioritario35.BuscarPorCodigo("N09")
                    : CatalogoPrioritario35.BuscarPorCodigo("B04");
            }

            if (nombreNorm.Contains("limon") || nombreNorm.Contains("lemon") || nombreNorm.Contains("lime"))
            {
                if (nombreNorm.Contains("cascara") || nombreNorm.Contains("peel") || nombreNorm.Contains("rind"))
                    return CatalogoPrioritario35.BuscarPorCodigo("V02");
                return CatalogoPrioritario35.BuscarPorCodigo("V05");
            }

            if (nombreNorm.Contains("vidrio") || nombreNorm.Contains("glass"))
            {
                if (nombreNorm.Contains("frasco") || nombreNorm.Contains("jar") || nombreNorm.Contains("tarro"))
                    return CatalogoPrioritario35.BuscarPorCodigo("B06");
                if (nombreNorm.Contains("botella") || nombreNorm.Contains("bottle"))
                    return CatalogoPrioritario35.BuscarPorCodigo("B07");
            }

            if (nombreNorm.Contains("botella") || nombreNorm.Contains("bottle"))
            {
                if (nombreNorm.Contains("gaseosa") || nombreNorm.Contains("soda") || nombreNorm.Contains("cola") || nombreNorm.Contains("refresco"))
                    return CatalogoPrioritario35.BuscarPorCodigo("B02");
                if (!nombreNorm.Contains("vidrio") && !nombreNorm.Contains("glass"))
                    return CatalogoPrioritario35.BuscarPorCodigo("B01");
            }

            if (nombreNorm.Contains("pitillo") || nombreNorm.Contains("straw") || nombreNorm.Contains("pajilla"))
                return CatalogoPrioritario35.BuscarPorCodigo("N06");

            if (nombreNorm.Contains("tapa") || nombreNorm.Contains("bottle cap") || nombreNorm.Contains("plastic cap") || nombreNorm.Contains("plastic lid"))
                return CatalogoPrioritario35.BuscarPorCodigo("B03");

            if (nombreNorm.Contains("pelicula") || nombreNorm.Contains("plastic film") || nombreNorm.Contains("plastic wrap") || nombreNorm.Contains("vinipel"))
                return contaminado
                    ? CatalogoPrioritario35.BuscarPorCodigo("N08")
                    : CatalogoPrioritario35.BuscarPorCodigo("B13");

            // Búsqueda general por coincidencia exacta y por puntaje sobre claves visuales y sinónimos de los 35 materiales.
            MaterialPrioritario35? mejor = null;
            int mejorPuntaje = 0;
            string[] tokens = nombreNorm.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (MaterialPrioritario35 candidato in CatalogoPrioritario35.Todos)
            {
                IEnumerable<string> claves = new[] { CatalogoPrototipo.Normalizar(candidato.Nombre) }
                    .Concat(candidato.ClavesVision.Select(CatalogoPrototipo.Normalizar))
                    .Concat(candidato.Sinonimos.Select(CatalogoPrototipo.Normalizar));

                foreach (string clave in claves)
                {
                    if (string.IsNullOrEmpty(clave)) continue;
                    if (clave == nombreNorm)
                        return candidato;

                    string[] partes = clave.Split(' ');
                    int hits = partes.Count(p => tokens.Contains(p));
                    if (hits == partes.Length && partes.Length > 0)
                    {
                        int puntaje = 100 + partes.Length * 10;
                        if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = candidato; }
                    }
                    else if (hits > 0 && hits * 2 >= Math.Max(partes.Length, tokens.Length))
                    {
                        int puntaje = hits * 20;
                        if (puntaje > mejorPuntaje) { mejorPuntaje = puntaje; mejor = candidato; }
                    }
                }
            }

            if (mejor is not null)
                return mejor;

            // Respaldo controlado a través del interpretador oficial de EcoAccess si el modelo usó una variante en inglés.
            string? claveEco = InterpretadorNombreObjeto.Clave(nombreNorm + " " + matNorm, out _);
            return claveEco switch
            {
                "plastic_bottle" => CatalogoPrioritario35.BuscarPorCodigo("B01"),
                "glass_bottle" => CatalogoPrioritario35.BuscarPorCodigo("B07"),
                "can" => CatalogoPrioritario35.BuscarPorCodigo("B12"),
                "drink_carton" => CatalogoPrioritario35.BuscarPorCodigo("B10"),
                "cardboard" => contaminado ? CatalogoPrioritario35.BuscarPorCodigo("N03") : CatalogoPrioritario35.BuscarPorCodigo("B09"),
                "paper" => contaminado ? CatalogoPrioritario35.BuscarPorCodigo("N01") : CatalogoPrioritario35.BuscarPorCodigo("B08"),
                "plastic_container" => contaminado ? CatalogoPrioritario35.BuscarPorCodigo("N09") : CatalogoPrioritario35.BuscarPorCodigo("B05"),
                "plastic_bag" => contaminado ? CatalogoPrioritario35.BuscarPorCodigo("N08") : CatalogoPrioritario35.BuscarPorCodigo("B13"),
                "styrofoam" => CatalogoPrioritario35.BuscarPorCodigo("N07"),
                "snack_wrapper" => CatalogoPrioritario35.BuscarPorCodigo("N08"),
                "used_tissue" => CatalogoPrioritario35.BuscarPorCodigo("N01"),
                "cigarette_butt" => CatalogoPrioritario35.BuscarPorCodigo("N04"),
                "condom" => CatalogoPrioritario35.BuscarPorCodigo("N05"),
                "face_mask" => CatalogoPrioritario35.BuscarPorCodigo("N12"),
                "greasy_paper" => CatalogoPrioritario35.BuscarPorCodigo("N03"),
                "disposable_cutlery" => CatalogoPrioritario35.BuscarPorCodigo("N06"),
                "plant_waste" => CatalogoPrioritario35.BuscarPorCodigo("V08"),
                _ => null,
            };
        }

        private static MaterialPrioritario35 MapearAprovechableContaminadoANegra(MaterialPrioritario35 aprovechable) => aprovechable.Codigo switch
        {
            "B04" or "B05" => CatalogoPrioritario35.BuscarPorCodigo("N09")!,
            "B08" => CatalogoPrioritario35.BuscarPorCodigo("N01")!,
            "B09" => CatalogoPrioritario35.BuscarPorCodigo("N03")!,
            "B13" => CatalogoPrioritario35.BuscarPorCodigo("N08")!,
            _ => CatalogoPrioritario35.BuscarPorCodigo("N08")!,
        };

        private static bool EsEtiquetaImpresa(ObservacionObjetoVisual obs)
        {
            string norm = CatalogoPrototipo.Normalizar(obs.NombreDetectado);
            if (ContienePatron(norm, PalabrasEtiquetaImpresa))
                return true;
            if (!obs.EsEtiquetaImpresa)
                return false;
            // Si el modelo nombró un envase físico real (botella, lata, caja) y marcó es_etiqueta_impresa=true
            // por la etiqueta impresa del envase, NO descartarlo como si fuera un simple adhesivo.
            string matNorm = CatalogoPrototipo.Normalizar(obs.MaterialObservable);
            return ResolverMaterial35(norm, matNorm, obs.EstadoObservable) is null;
        }

        private static bool ContienePatron(string textoNormalizado, string[] patrones) =>
            patrones.Any(p => textoNormalizado.Contains(CatalogoPrototipo.Normalizar(p)));

        private static string ConstruirMensajeUsuario(
            EstadoGlobalAnalisis estado,
            IReadOnlyList<ObjetoClasificado35> objetos,
            IReadOnlyList<CategoriaMaterial> canecas,
            bool puedeAbrir)
        {
            var sb = new StringBuilder();
            if (estado == EstadoGlobalAnalisis.RequiereConfirmacion)
                sb.AppendLine("⚠️ Se requiere confirmación humana antes de abrir las canecas:");
            else if (estado == EstadoGlobalAnalisis.Clasificado)
                sb.AppendLine($"♻️ Objetos identificados ({objetos.Sum(o => o.Cantidad)}):");
            else
                sb.AppendLine("ℹ️ Resultado de la revisión:");

            foreach (ObjetoClasificado35 o in objetos)
            {
                string qty = o.Cantidad > 1 ? $"{o.Cantidad} × " : "";
                string destino = o.CanecaAsignada switch
                {
                    CategoriaMaterial.OrganicoAprovechable => "VERDE",
                    CategoriaMaterial.Aprovechable => "BLANCA",
                    CategoriaMaterial.NoAprovechable => "NEGRA",
                    _ when o.Estado == EstadoClasificacionObjeto.FueraDeCatalogo => "GESTION_ESPECIAL (no abrir caneca ordinaria)",
                    _ => o.Estado.ToString().ToUpperInvariant(),
                };
                string nombre = o.Material35 is not null ? $"{o.Material35.Codigo} {o.Material35.Nombre}" : o.Observacion.NombreDetectado;
                sb.AppendLine($"- {qty}{nombre} → {destino}");
            }

            if (canecas.Count > 0)
            {
                string listaCanecas = string.Join(" + ", canecas.Select(NombreCortoCaneca));
                sb.Append(puedeAbrir
                    ? $"Canecas requeridas (apertura secuencial guiada): {listaCanecas}."
                    : $"Canecas sugeridas (pendientes de confirmación): {listaCanecas}.");
            }
            else
            {
                sb.Append("Ninguna caneca habilitada para apertura automática.");
            }
            return sb.ToString();
        }

        public static string NombreCortoCaneca(CategoriaMaterial c) => c switch
        {
            CategoriaMaterial.OrganicoAprovechable => "VERDE",
            CategoriaMaterial.Aprovechable => "BLANCA",
            CategoriaMaterial.NoAprovechable => "NEGRA",
            _ => "OTRA",
        };
    }
}
