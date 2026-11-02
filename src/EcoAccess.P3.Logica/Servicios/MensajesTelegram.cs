using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>
    /// Textos del bot y de la página de registro. Están juntos para revisarlos de una vez: ninguno pide contraseñas
    /// ni datos personales por el chat.
    /// </summary>
    public static class MensajesTelegram
    {
        public const string SoloChatPrivado = "El bot de EcoAccess solo atiende chats privados.";

        public const string YaVinculada = "Esta cuenta de Telegram ya está vinculada a EcoAccess.";

        public const string SinVinculo = "Esta cuenta de Telegram no está vinculada a EcoAccess.";
        public const string VinculacionNoDisponible = "La vinculación todavía no está disponible: falta configurar su página HTTPS. Puedes consultar la información ambiental pública. No envíes contraseñas por Telegram.";
        /// <summary>Versión del aviso de envío externo: cambiarla obliga a pedir la autorización otra vez.</summary>
        public const string VersionAvisoExterno = "groq-2026-11";
        public const string AvisoFotoExterna = "🌐 Para analizar tu foto necesito enviarla a un servicio externo (Groq), fuera de este computador. Solo se envía la imagen, sin tu nombre ni tu cuenta. Comprueba que muestre únicamente el residuo, sin personas ni datos personales. Puedes retirar tu autorización con /revocarfotos o elegir el material sin enviar nada.";
        public const string AvisoRespaldoExterno = "🌱 El reconocimiento en este computador no está disponible ahora. Puedo enviar tu foto a un servicio externo (Groq) si lo autorizas: solo va la imagen, sin tu nombre ni tu cuenta. Puedes retirar tu autorización con /revocarfotos o elegir el material sin enviar nada.";
        public const string AvisoSegundaOpinion = "🔎 Una segunda opinión significa enviar esta foto a un servicio externo (Groq): solo va la imagen, sin tu nombre ni tu cuenta. Comprueba que no muestre personas ni datos personales. Puedes retirar tu autorización con /revocarfotos.";
        public const string AutorizacionRevocada = "🔒 Listo: retiré tu autorización. Tus próximas fotos no saldrán de este computador.";
        public const string SinAutorizacionExterna = "🔒 No tienes una autorización vigente: tus fotos no se envían a servicios externos.";
        public const string ArchivoNoCompatible = "📎 Ese archivo no lo puedo analizar. Envíame una foto JPEG, PNG o WebP de hasta 5 MB, o elige el material con /manual.";
        public const string AvisoConsentimientoFoto ="📷 Antes de analizar tu foto: comprueba que solo muestra el residuo, sin personas ni datos personales. La foto sugiere el material; no abre la caneca ni mide peso.";

        /// <summary>Recordatorio corto al escanear el QR; las condiciones completas se muestran al enviar cada foto.</summary>
        public const string SugerenciaFoto = "📷 También puedes enviarme una foto del residuo y te sugiero el material.";

        public const string Menu = "¿Qué quieres hacer?";
        /// <summary>Tiempo que la tapa permanece abierta; lo controla el firmware de la caneca.</summary>
        public const int VentanaDepositoSegundos = 20;
        public const string SaludoEco = "🌱 ¡Hola! Soy Eco. Voy a ayudarte a desechar correctamente tu residuo. ¡Envíame una foto!";
        public const string ComoEnviarFoto = "📸 Toca la cámara o el clip 📎 de Telegram y envíame una foto del residuo, de cerca y con buena luz.";
        public const string Agradecimiento = "🌱 ¡Gracias por utilizar EcoAccess! Tu operación quedó registrada.";
        public const string PreguntaExperiencia = "⭐ ¿Cómo fue tu experiencia?";
        public const string AcercaDe = "EcoAccess es un prototipo de orientación y registro de residuos de la Universidad Popular del Cesar. Hoy comenzamos con una caneca. Con tu participación, construiremos una UPC más inteligente y sostenible.";

        public const string ComoDepositar = "Para depositar, escanea con la cámara del celular el código QR pegado en la caneca. Después eliges el material o me envías una foto.";

        public const string FotoGuardadaSinCaneca = "Recibí tu foto. Para depositar, primero escanea el QR de la caneca: la guardaré 10 minutos y podrás analizarla con un botón.";

        public const string OtraFoto = "Envíame otra foto del residuo, de cerca y con buena luz, sin personas ni datos personales.";

        public static string MiCuenta(bool vinculada) => vinculada
            ? "👤 Tu Telegram está vinculado a tu cuenta de EcoAccess. Para desvincularlo escribe /desvincular (tu historial se conserva)."
            : "👤 Tu Telegram aún no está vinculado. Pulsa /vincular para crear o conectar tu cuenta en una página segura.";

        public const string SinConsentimiento = "Para crear la cuenta hay que aceptar el tratamiento de datos personales (Ley 1581 de 2012).";

        public const string CorreoExistente = "Ya existe una cuenta con ese correo. Inicia sesión en la página HTTPS del enlace para vincularla.";

        public const string CorreoOCuentaExistente = "El correo ya está registrado o esta cuenta de Telegram ya quedó vinculada.";

        public const string EnlaceInvalido = "El enlace de registro no es válido. Pide otro al bot con /vincular.";

        public const string EnlaceVencido = "El enlace de registro venció o ya se usó. Pide otro al bot con /vincular.";

        public const string Bienvenida =
            "¡Hola! Soy Eco 🌱, tu compañero ambiental.\nEstoy aquí para ayudarte a reciclar de manera sencilla.\n" +
            "Puedes enviarme una foto de tu residuo o escanear el QR de una caneca.\n¡Cada residuo cuenta!";

        public const string FotoRecibida = "🔍 Estoy identificando tu residuo...";

        public const string FotoNoDisponible = "🌱 El reconocimiento de fotos no está disponible ahora. Puedes elegir el material manualmente.";

        public const string ConfirmarFoto = "Al confirmar, solicitaré la apertura. EcoAccess comprobará la autorización y te informará del estado de la orden.";

        public const string EscanearQr = "📱 Abre la cámara de tu celular, apunta al código QR pegado en la caneca y toca el enlace: te traerá de vuelta conmigo con la caneca identificada.";

        public const string Demo = "🧪 Esta es nuestra caneca de demostración. Puedes probar cómo EcoAccess identifica materiales y registra depósitos mediante una caneca simulada (no es una caneca física).";

        public const string DemoNoDisponible = "La caneca de demostración no está configurada en este momento.";

        public const string Ayuda =
            "Comandos:\n/start: empezar\n/vincular: crear o conectar tu cuenta de EcoAccess\n/cancelar: cancelar la solicitud actual\n" +
            "/operaciones: ver tus últimas operaciones\n/estado: consultar la orden de este diálogo\n/depositos: ver tus últimos depósitos\n/ecopuntos: consultar tus ecopuntos\n" +
            "/estadisticas, /mismateriales o /evolucion: tu actividad de 30 días y comparación con el período anterior\n" +
            "/cantidad N: declarar unidades de tu solicitud vinculada\n/potenciales: consultar estimación sin acreditar puntos\n" +
            "/materiales: catálogo de materiales y cómo separarlos\n/categorias o /separar: guía de separación por colores\n" +
            "/foto: cómo enviar una fotografía\n/manual: elegir un material sin reconocimiento visual\n" +
            "/confirmar <código>: terminar la vinculación desde el bot oficial\n" +
            "/desvincular: desvincular esta cuenta de Telegram\n\n" +
            "Escribe el nombre de un residuo para saber cómo separarlo. Para abrir una caneca, escanea su código QR. " +
            "EcoAccess nunca te pedirá tu contraseña por este chat.";

        public const string QrDesconocido = "No reconozco ese código. Escanea el QR pegado en la caneca.";

        public const string CanecaNoDisponible = "Esta caneca no está disponible en este momento. Busca otra cercana.";

        public const string InvitaRegistro = "Para pedir que se abra, conecta tu cuenta con /vincular.";

        public const string ElegirMaterial = "¿Qué deseas depositar? Elige el material o envíame una foto:";

        public const string SeleccionManual = "Selección manual: tú eliges el material; esto no es reconocimiento visual mediante IA. Elige un material admitido por esta caneca. Después debes confirmar la solicitud.";

        public const string GuiaFoto = "📸 Envíame una foto de tu residuo. Si prefieres, elige el material con /manual.";

        public const string SinMateriales = "Esta caneca no tiene materiales habilitados ahora.";

        public const string MaterialNoValido = "Ese material no corresponde a esta caneca. Elige uno de la lista.";

        public const string EscaneaPrimero = "Primero escanea el código QR de la caneca.";

        public const string NadaQueConfirmar = "No hay nada pendiente de confirmar. Escanea el QR de la caneca.";

        public const string OrdenEmitida = "✅ Material confirmado. Continuemos.\nLa caneca está recibiendo la orden. Consulta /estado: te diré cuándo se abra y se registre tu depósito.";

        public const string OrdenYaEmitida = "Ya existe una orden para esta solicitud. Consulta /estado para conocer los hechos registrados.";

        public const string DialogoCerrado = "Diálogo cerrado. La orden emitida no se revoca desde el chat; esto no detiene una apertura. Consulta /operaciones o /depositos.";
        public const string CancelacionNoConfirmada = "Diálogo cerrado. No se confirmó la cancelación de la operación; consulta /operaciones o /depositos. Esto no revoca un comando ya emitido.";

        public static string Despacho(ResultadoDespacho resultado) => resultado switch
        {
            ResultadoDespacho.Enviada => "✅ Listo. Estoy abriendo la caneca; te aviso en cuanto se abra.",
            ResultadoDespacho.YaEnviada => "La caneca ya recibió esta orden. Consulta /estado.",
            ResultadoDespacho.NoEntregada => "🌱 La caneca no respondió todavía. Puedes reintentar; no se creará otra orden.",
            ResultadoDespacho.Vencida => "La orden venció; no se reenvió. Consulta /estado y escanea el QR para una nueva solicitud.",
            _ => "La orden terminó o no permite entrega; no se reenvió. Consulta /estado.",
        };

        public static string EstadoOperacion(OperacionApertura operacion, DateTime ahoraUtc)
        {
            OrdenApertura orden = operacion.Orden;
            string estado = orden.EsVigente && orden.EstaVencida(ahoraUtc) ? "vencida por tiempo" : orden.Estado.ToString();
            string deposito = operacion.Deposito is not { } d ? "Sin depósito registrado."
                : d.EstaVerificado ? "Depósito registrado, material verificado."
                : "Depósito registrado por detección, material pendiente de verificación.";
            return $"Estado registrado de la orden {orden.Id}: {estado}.\n{deposito}";
        }

        public const string Cancelado = "Solicitud cancelada.";

        public const string NadaQueCancelar = "No tienes una solicitud en curso.";

        public const string Desvinculado = "Listo, desvinculamos esta cuenta de Telegram. Tu historial en EcoAccess se conserva.";

        public const string ConversacionVencida = "La solicitud anterior venció. Escanea el QR de la caneca otra vez.";

        public const string DemasiadosMensajes = "Recibimos demasiados mensajes seguidos. Espera un momento e intenta de nuevo.";

        public const string MensajeSimultaneo = "Recibimos otro mensaje tuyo al mismo tiempo. Intenta de nuevo.";

        public const string SinVinculoConsulta = "Esta cuenta de Telegram no está vinculada a EcoAccess. Crea tu cuenta con /vincular.";

        public const string SinOperaciones = "Todavía no tienes operaciones registradas.";

        public const string SinDepositos = "Todavía no tienes depósitos registrados.";

        public const string EcopuntosPendiente =
            "La consulta de ecopuntos aún no está disponible. Los ecopuntos se otorgan solo por depósitos verificados, " +
            "nunca por escanear, enviar fotos o abrir la caneca.";

        public const string MaterialNoEncontrado =
            "No encontré ese material en el catálogo. Escribe el nombre de un residuo (por ejemplo «botella» o «pila») " +
            "o usa /separar para ver la guía de colores.";

        public const string VariosMateriales = "Encontré varios materiales. ¿Cuál es el tuyo?";

        public const string CatalogoVacio = "El catálogo de materiales no tiene materiales habilitados en este momento.";

        /// <summary>Encabezado de /materiales; avisa si la lista se recortó para no ocultarlo.</summary>
        public static string Catalogo(IReadOnlyList<EcoAccess.P3.Entidades.Material> visibles, int total) =>
            "Catálogo de materiales de EcoAccess. Pulsa uno para saber cómo separarlo." +
            (total > visibles.Count ? $" Se muestran {visibles.Count} de {total}; escribe el nombre para buscar otro." : "") +
            " Para depositar, escanea el QR de una caneca.";

        public const string NoEntendido = "No entendí el mensaje. Elige una opción del menú.";

        public static string EnlaceRegistro(Uri enlace) =>
            $"Abre este enlace para crear o conectar tu cuenta (vence en 10 minutos y sirve una sola vez):\n{enlace}\n\n" +
            "Escribe tus datos solo en esa página. EcoAccess nunca te pedirá tu contraseña por este chat.";

        public static string CanecaIdentificada(string nombreCaneca, IEnumerable<string> materiales) =>
            $"Caneca «{nombreCaneca}». Recibe: {string.Join(", ", materiales)}.";

        /// <summary>Guía por colores de la Resolución 2184 de 2019, más la ruta especial de los peligrosos.</summary>
        public static string Separacion(IEnumerable<IndicacionSeparacion> indicaciones) =>
            "Separación en la fuente (Resolución 2184 de 2019, código de colores):\n" +
            string.Join("\n", indicaciones.Select(i => $"• {Destino(i)}: {i.Indicacion}")) +
            "\n\nCada caneca de EcoAccess recibe solo algunos materiales: escanea su QR para ver cuáles.";

        /// <summary>
        /// Cómo se separa el material y, aparte, si la caneca identificada lo recibe. Son dos respuestas distintas:
        /// que un material sea aprovechable no significa que esta caneca lo acepte.
        /// </summary>
        public static string InformacionMaterial(ConsultaMaterial consulta, string? nombreCaneca)
        {
            IndicacionSeparacion guia = consulta.Guia;
            string clasificacion = $"{consulta.Material.Nombre}: {guia.Nombre.ToLowerInvariant()}, {Destino(guia).ToLowerInvariant()}.\n{GuiaSeparacion.Orientacion(consulta.Material)}";
            string caneca = nombreCaneca is null ? "esta caneca" : $"la caneca «{nombreCaneca}»";
            string admision = consulta.Admision switch
            {
                AdmisionMaterial.Admitido => $"Sí lo recibe {caneca}.",
                AdmisionMaterial.NoAdmitido => $"No lo recibe {caneca}.",
                AdmisionMaterial.CanecaNoDisponible => "La caneca que escaneaste no está disponible ahora.",
                _ => "Para saber si una caneca lo recibe, escanea su código QR.",
            };
            return clasificacion + "\n\n" + admision;
        }

        private static string Destino(IndicacionSeparacion indicacion) =>
            indicacion.ColorCaneca is { } color ? $"Caneca {color} ({indicacion.Nombre.ToLowerInvariant()})" : $"Ruta especial ({indicacion.Nombre.ToLowerInvariant()})";

                public static string Confirmar(string nombreMaterial) => $"Vas a depositar {nombreMaterial}. ¿Confirmas la solicitud de apertura?";

        public static string Operaciones(IEnumerable<(DateTime FechaUtc, EstadoInteraccion Estado, string? Material)> operaciones) =>
            "Tus últimas operaciones (hora UTC):\n" + string.Join("\n", operaciones.Select(o =>
                $"• {Fecha(o.FechaUtc)}: {NombreEstado(o.Estado)}{(o.Material is null ? "" : $" ({o.Material})")}"));

        public static string Depositos(
            IEnumerable<(DateTime FechaUtc, string Material, bool Verificado, decimal? PesoMedidoKg, decimal? PesoEstimadoKg)> depositos) =>
            "Tus últimos depósitos (hora UTC):\n" + string.Join("\n", depositos.Select(d =>
                $"• {Fecha(d.FechaUtc)}: {d.Material}, {(d.Verificado ? "verificado" : "pendiente de verificación")}" +
                (d.PesoMedidoKg is { } medido ? $", peso medido {Kilos(medido)}" : "") +
                (d.PesoEstimadoKg is { } estimado ? $", peso estimado {Kilos(estimado)}" : "") +
                (d.PesoMedidoKg is null && d.PesoEstimadoKg is null ? ", sin peso" : "")));

        private static string NombreEstado(EstadoInteraccion estado) => estado switch
        {
            EstadoInteraccion.Iniciada => "iniciada",
            EstadoInteraccion.Autorizada => "apertura autorizada",
            EstadoInteraccion.ConDeposito => "con depósito",
            EstadoInteraccion.SinDeposito => "sin depósito",
            EstadoInteraccion.Rechazada => "cancelada o rechazada",
            _ => "desconocido",
        };

        private static string Fecha(DateTime utc) => utc.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

        private static string Kilos(decimal kg) => kg.ToString("0.###", CultureInfo.InvariantCulture) + " kg";

        /// <summary>Aviso automático de un hecho registrado; usa solo datos de Oracle y no revela a otros ciudadanos.</summary>
        public static string Notificacion(EcoAccess.P3.Logica.Contratos.HechoNotificable hecho)
        {
            string material = Compat.IsNullOrWhiteSpace(hecho.Material) ? "tu residuo" : hecho.Material;
            return hecho.Tipo switch
            {
                Contratos.TipoHechoNotificable.Apertura => hecho.Simulacion
                    ? $"🧪 Simulación: la caneca virtual está abierta {VentanaDepositoSegundos} segundos para {material}. No hay movimiento físico."
                    : $"🔓 La caneca está abierta. Tienes {VentanaDepositoSegundos} segundos para depositar {material}.",
                Contratos.TipoHechoNotificable.FalloCaneca => $"⚠️ La caneca informó un fallo en la orden {hecho.IdOrden}. No se registró ningún depósito por ese fallo; puedes intentarlo de nuevo escaneando el QR.",
                Contratos.TipoHechoNotificable.CierreSinDeposito => $"La caneca se cerró sin detectar un residuo (orden {hecho.IdOrden}). No se registró depósito ni se otorgan ecopuntos.",
                Contratos.TipoHechoNotificable.DepositoRegistrado => $"{Agradecimiento}\nEstado: {material}, {(hecho.Simulacion ? "simulación de software, sin depósito físico" : "pendiente de verificación")}.\n\n{PreguntaExperiencia}",
                Contratos.TipoHechoNotificable.DepositoVerificado => $"🔎 Tu depósito {hecho.IdDeposito} fue verificado. Material verificado: {material}.",
                Contratos.TipoHechoNotificable.EcopuntosAcreditados => $"🌱 Recibiste {hecho.Puntos} ecopuntos por tu depósito de {material}.\nSaldo actual: {hecho.Saldo} ecopuntos.",
                Contratos.TipoHechoNotificable.EcopuntosRevertidos => $"Se revirtieron {Math.Abs(hecho.Puntos ?? 0)} ecopuntos del depósito {hecho.IdDeposito}.\nSaldo actual: {hecho.Saldo} ecopuntos.",
                _ => "Tu operación cambió de estado. Consulta Mis depósitos.",
            };
        }
    }
}
