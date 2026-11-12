using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace EcoAccess.P3.Logica.Servicios
{

    public enum IntencionTexto
    {
        Ninguna, Saludo, Correccion, Cancelar, Ayuda, Menu, OtraFoto,
        Despedida, Agradecimiento, Casual, ConsultaAmbiental, ConsultaResiduo,
        Ecopuntos, CuantosTengo, Actividad, Cuenta, YaTengoCuenta, Repetir,
        Queja, QuejaExperiencia, ProblemaFoto, ProblemaCaneca, Demora, Sugerencia, Elogio, ElogioEcoAccess,
        TemaFutbol, TemaTecnologia, TemaEstudio, FueraDeTema,
        Sexual, Violencia, Insulto, Urgencia
    }

    /// <summary>
    /// Reconoce qué quiere decir la persona antes de buscar en el catálogo. Son reglas cerradas y predecibles: no usan ningún
    /// modelo, ningún mensaje sale del equipo, y solo deciden qué responder; las acciones siguen pasando por sus validaciones.
    /// </summary>
    public static class IntencionesTelegram
    {
        private static readonly HashSet<string> Saludos = new(StringComparer.Ordinal)
        {
            "hola", "hola eco", "ola", "holi", "hey", "buenas", "buen dia", "buenos dias", "buenas tardes", "buenas noches", "que tal", "saludos",
        };

        private static readonly HashSet<string> Cancelaciones = new(StringComparer.Ordinal) { "cancelar", "cancela", "cancelalo", "cancelar todo" };
        private static readonly HashSet<string> Ayudas = new(StringComparer.Ordinal) { "ayuda", "ayudame", "help", "necesito ayuda" };
        private static readonly HashSet<string> Menus = new(StringComparer.Ordinal) { "menu", "inicio", "opciones", "volver", "volver al menu" };
        private static readonly HashSet<string> OtrasFotos = new(StringComparer.Ordinal) { "otra foto", "otra fotografia", "enviar otra foto", "mandar otra foto" };

        private static readonly string[] FrasesCorreccion =
        {
            "te equivocaste", "te equivocas", "equivocado", "equivocada", "esta mal", "estas mal", "incorrecto", "incorrecta", "no es correcto",
            "no reconociste", "no lo reconociste", "reconociste mal", "identificaste mal", "mal identificado", "no acertaste", "no es asi",
            // Caso real del 12/11/2026: «era plástico y se abrió la caneca negra» quedó como comentario genérico.
            "es plastico", "era plastico", "es una bolsa", "era una bolsa", "es bolsa", "es de plastico", "era de plastico",
            "no es multicapa", "no era multicapa", "no es un empaque", "no era un empaque", "no es empaque", "se equivoco", "eco se equivoco",
            "clasifico mal", "lo clasifico mal", "identifico mal", "reconocio mal", "caneca equivocada", "caneca incorrecta", "otra caneca",
            "abrio la equivocada", "se abrio la equivocada", "no era esa caneca", "no era la caneca", "no iba en esa caneca",
        };

        /// <summary>Si una corrección habla de la caneca (accionamiento) y no solo del objeto (reconocimiento).</summary>
        public static bool MencionaCaneca(string? texto)
        {
            string c = " " + Normalizar(texto) + " ";
            return c.Contains(" caneca ") || c.Contains(" abrio ") || c.Contains(" se abrio ") || c.Contains(" tapa ")
                || c.Contains(" negra ") || c.Contains(" verde ") || c.Contains(" blanca ");
        }

        private static readonly string[] InicioCorreccion = { "no es", "eso no es", "no era", "eso no era" };

        private static readonly string[] Urgencias =
        {
            "me quiero matar", "quiero matarme", "me voy a matar", "me quiero morir", "quiero morirme", "quitarme la vida", "suicidarme", "suicidio", "hacerme dano",
            "estoy en peligro", "me estan atacando", "me quieren matar", "me van a matar", "necesito ayuda urgente", "auxilio",
        };

        private static readonly string[] Sexuales =
        {
            "sexo", "sexual", "porno", "pornografia", "desnuda", "desnudo", "desnudas", "follar", "follamos", "cogemos", "culiar", "culiemos", "tirar contigo", "nudes", "masturbar",
            "masturbarme", "orgasmo", "tetas", "verga", "excitado", "excitada", "calenton", "calenturiento",
        };

        /// <summary>Residuos cuya mención es legítima: nunca hacen que un mensaje se considere contenido sexual.</summary>
        private static readonly string[] ResiduosSensibles = { "condon", "condones", "preservativo", "preservativos", "toalla", "toallas", "tampon", "tampones", "panal", "panales", "higienica" };
        private static readonly string[] Disposicion = { "boto", "botar", "tiro", "tirar", "echo", "echar", "deposito", "depositar", "reciclo", "reciclar", "desecho", "desechar", "residuo", "basura", "caneca", "separo", "separar" };

        private static readonly string[] Violencias =
        {
            "como matar", "matar a", "voy a matar", "hacer una bomba", "fabricar una bomba", "como envenenar", "envenenar a", "golpear a", "degollar", "descuartizar", "torturar", "apunalar",
        };

        private static readonly string[] Insultos =
        {
            "idiota", "estupido", "estupida", "imbecil", "pendejo", "pendeja", "hijueputa", "hp", "gonorrea", "malparido", "malparida", "mierda", "tonto", "tonta", "inutil", "basura de bot", "bot basura", "bot de mierda", "marica",
        };

        private static readonly string[] NegativasExperiencia = { "no me gusto", "no me gusta", "no me gustaron", "no me sirvio" };
        private static readonly string[] Quejas =
        {
            "malo", "mala", "pesimo", "pesima", "horrible", "terrible", "no sirve", "no funciona", "funciona mal", "mal servicio", "queja", "quejarme", "reclamo", "decepcion", "decepcionante", "porqueria",
            "no es bueno", "no es buena", "problema", "falla", "fallo", "se dano", "esta danado", "esta danada",
        };
        private static readonly string[] Reconocimiento = { "no reconoce", "no reconocio", "no identifica", "no identifico", "no detecta", "no detecto", "no entiende mi foto", "no ve la" };
        private static readonly string[] Caneca = { "no abrio", "no abre", "no se abrio", "no se abre", "no cerro", "no cierra", "no se cerro", "no se cierra" };
        private static readonly string[] Demoras = { "demora", "demoro", "demorado", "tarda", "tardo", "tardando", "muy lento", "lento", "lenta", "se demora" };
        private static readonly string[] Sugerencias = { "sugerencia", "sugiero", "deberian", "podrian", "seria bueno", "recomiendo", "propongo", "me gustaria que", "quisiera que" };
        private static readonly string[] ElogiosEcoAccess = { "me encanto", "me encanta", "me gusto", "me gusta ecoaccess", "me gusta eco", "me gusta el bot" };
        private static readonly string[] Elogios = { "excelente", "genial", "buen servicio", "muy bueno", "muy buena", "muy bien", "maravilloso", "maravillosa", "increible", "que bueno", "que chevere", "buenisimo", "buenisima", "funciona bien" };
        private static readonly string[] Despedidas = { "adios", "chao", "hasta luego", "hasta pronto", "nos vemos", "bye", "me voy" };
        private static readonly string[] Agradecimientos = { "gracias", "te agradezco", "muy amable" };
        private static readonly string[] Casuales = { "como estas", "como vas", "como te va", "que haces", "quien eres", "como te llamas", "que eres", "estas bien", "que cuentas", "te gusta" };
        private static readonly string[] TemaFutbolPalabras = { "futbol", "gol", "goles", "partido", "seleccion", "messi", "james rodriguez", "baloncesto", "beisbol", "deporte", "deportes" };
        private static readonly string[] TemaTecnologiaPalabras = { "tecnologia", "computadores", "computador", "celulares", "inteligencia artificial" };
        private static readonly string[] TemaEstudioPalabras = { "programacion", "programando", "estudiando", "estudio", "universidad", "tarea", "examen", "parcial", "semestre" };
        private static readonly string[] Actualidad = { "noticias", "clima", "presidente", "politica", "elecciones", "dolar", "chiste", "receta", "pelicula", "musica", "horoscopo" };
        private static readonly string[] CuentasConsultas = { "crear cuenta", "crear una cuenta", "registrarme", "quiero registrarme", "vincular", "vincular mi cuenta", "vincular cuenta", "mi cuenta", "iniciar sesion" };
        private static readonly string[] YaTengo = { "ya tengo cuenta", "tengo cuenta", "ya tengo una cuenta", "ya me registre", "ya estoy registrado", "ya estoy registrada" };
        private static readonly string[] EcopuntosFrases = { "ecopunto", "ecopuntos", "mis puntos", "cuantos puntos", "mi saldo" };
        private static readonly string[] ActividadFrases = { "mi actividad", "mis depositos", "mi historial", "historial", "mis estadisticas", "estadisticas" };
        private static readonly string[] Repeticiones = { "intenta otra vez", "intentalo otra vez", "intenta de nuevo", "intentalo de nuevo", "otra vez", "de nuevo", "reintenta", "prueba otra vez", "vuelve a intentar" };
        private static readonly string[] CuantosFrases = { "cuantos tengo", "cuantos llevo", "cuantos gane", "cuanto tengo", "cuanto llevo" };
        private static readonly string[] ConsultasResiduo = { "donde boto", "donde tiro", "donde echo", "donde deposito", "donde desecho", "como boto", "como reciclo", "como separo", "como desecho", "puedo botar", "puedo reciclar", "puedo depositar", "se puede reciclar", "se recicla", "es reciclable", "en que caneca va", "en que caneca" };
        private static readonly string[] Ambientales = { "reciclaje", "reciclar", "separar residuos", "separacion de residuos", "medio ambiente", "contaminacion", "sostenibilidad", "codigo de colores", "cuidar el planeta", "calentamiento global", "cambio climatico", "aprovechable", "aprovechables" };

        public static IntencionTexto Clasificar(string? texto)
        {
            string normal = Normalizar(texto);
            if (normal.Length == 0) return IntencionTexto.Ninguna;
            string c = " " + normal + " ";
            bool Tiene(IEnumerable<string> frases) => frases.Any(f => c.Contains(" " + f + " "));

            // Lo delicado va primero: una situación de peligro nunca debe confundirse con otra cosa.
            if (Tiene(Urgencias)) return IntencionTexto.Urgencia;
            if (Tiene(Sexuales) && !(Tiene(ResiduosSensibles) && Tiene(Disposicion))) return IntencionTexto.Sexual;
            if (Tiene(Violencias)) return IntencionTexto.Violencia;

            if (Saludos.Contains(normal)) return IntencionTexto.Saludo;
            if (Cancelaciones.Contains(normal)) return IntencionTexto.Cancelar;
            if (Ayudas.Contains(normal)) return IntencionTexto.Ayuda;
            if (Menus.Contains(normal)) return IntencionTexto.Menu;
            if (OtrasFotos.Contains(normal)) return IntencionTexto.OtraFoto;

            if (Tiene(FrasesCorreccion) || InicioCorreccion.Any(i => normal == i || normal.StartsWith(i + " ", StringComparison.Ordinal)) && !Tiene(Quejas))
                return IntencionTexto.Correccion;
            if (Tiene(Repeticiones)) return IntencionTexto.Repetir;

            if (Tiene(Insultos)) return IntencionTexto.Insulto;
            if (Tiene(YaTengo)) return IntencionTexto.YaTengoCuenta;
            if (Tiene(CuantosFrases)) return Tiene(EcopuntosFrases) ? IntencionTexto.Ecopuntos : IntencionTexto.CuantosTengo;
            if (Tiene(EcopuntosFrases)) return IntencionTexto.Ecopuntos;
            if (Tiene(ActividadFrases)) return IntencionTexto.Actividad;
            if (Tiene(CuentasConsultas)) return IntencionTexto.Cuenta;

            if (Tiene(Caneca) || (c.Contains(" caneca ") && Tiene(Quejas))) return IntencionTexto.ProblemaCaneca;
            if (Tiene(Reconocimiento)) return IntencionTexto.ProblemaFoto;
            if (Tiene(NegativasExperiencia)) return IntencionTexto.QuejaExperiencia;
            if (Tiene(Sugerencias)) return IntencionTexto.Sugerencia;
            if (Tiene(Demoras)) return IntencionTexto.Demora;
            if (Tiene(Quejas)) return IntencionTexto.Queja;
            if (Tiene(ElogiosEcoAccess)) return IntencionTexto.ElogioEcoAccess;
            if (Tiene(Elogios)) return IntencionTexto.Elogio;

            if (Tiene(Agradecimientos)) return IntencionTexto.Agradecimiento;
            if (Tiene(Despedidas)) return IntencionTexto.Despedida;
            if (Tiene(ConsultasResiduo)) return IntencionTexto.ConsultaResiduo;
            if (Tiene(TemaFutbolPalabras)) return IntencionTexto.TemaFutbol;
            // Una sola palabra, como «computador», es una búsqueda de material; una frase sobre el tema, no.
            if (Tiene(TemaTecnologiaPalabras) && normal.Contains(' ')) return IntencionTexto.TemaTecnologia;
            if (Tiene(TemaEstudioPalabras)) return IntencionTexto.TemaEstudio;
            if (Tiene(Casuales)) return IntencionTexto.Casual;
            if (Tiene(Actualidad)) return IntencionTexto.FueraDeTema;
            if (Tiene(Ambientales)) return IntencionTexto.ConsultaAmbiental;
            return IntencionTexto.Ninguna;
        }

        /// <summary>Lo que queda tras quitar «dónde boto», para buscarlo en el catálogo: «dónde boto una botella» es «botella».</summary>
        public static string ResiduoConsultado(string? texto)
        {
            string normal = Normalizar(texto);
            foreach (string frase in ConsultasResiduo.OrderByDescending(f => f.Length))
            {
                int i = (" " + normal + " ").IndexOf(" " + frase + " ", StringComparison.Ordinal);
                if (i >= 0) return normal.Remove(i, Math.Min(frase.Length, normal.Length - i)).Trim();
            }
            return normal;
        }

        /// <summary>Si la frase parece una pregunta o un comentario y no el nombre de un residuo.</summary>
        public static bool ParecePregunta(string? texto) =>
            texto is not null && (texto.Contains('?') || texto.Contains('¿') || Normalizar(texto).Split(' ').Length >= 5);

        private static string Normalizar(string? texto)
        {
            if (Compat.IsNullOrWhiteSpace(texto) || texto!.Length > 200) return "";
            var salida = new StringBuilder(texto.Length);
            foreach (char c in texto.Normalize(NormalizationForm.FormD))
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    salida.Append(char.IsLetterOrDigit(c) ? char.ToLowerInvariant(c) : ' ');
            return string.Join(" ", salida.ToString().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
