using System;
using System.Collections.Generic;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{
    /// <summary>Adónde debe ir un objeto reconocido. Solo las tres primeras son canecas de EcoAccess.</summary>
    public enum RutaDisposicion
    {
        Blanca = 1,
        Negra = 2,
        Verde = 3,
        /// <summary>Residuos de aparatos eléctricos y electrónicos.</summary>
        Raee = 10,
        PilasBaterias = 11,
        /// <summary>Medicamentos vencidos o sin uso (posconsumo farmacéutico).</summary>
        Medicamentos = 12,
        Bombillos = 13,
        /// <summary>Químicos, aerosoles, envases con sustancias peligrosas.</summary>
        Peligroso = 14,
        /// <summary>Agujas, lancetas, cuchillas, vidrio roto: recipiente rígido y gestión especial.</summary>
        Cortopunzante = 15,
        AceiteUsado = 16,
        /// <summary>Objeto que puede seguir usándose: donar, reparar o reutilizar. EcoAccess no lo recibe.</summary>
        ReutilizacionDonacion = 20,
        NoEsResiduo = 30,
        /// <summary>Depende de algo que la foto no muestra (contenido, uso, estado). No se abre nada.</summary>
        Evaluar = 40,
    }

    /// <summary>
    /// Matriz explícita y versionada (fase 6 de la misión final): ruta por clave de <see cref="CatalogoObjetos"/>. No sustituye la
    /// normativa (Resolución 2184 de 2019 y programas posconsumo); se revisa cuando cambian las condiciones de recepción. Solo
    /// <see cref="RutaDisposicion.Blanca"/>, <see cref="RutaDisposicion.Negra"/> y <see cref="RutaDisposicion.Verde"/> pueden llegar a
    /// una caneca, y además Oracle debe admitir el material.
    /// </summary>
    public static class MatrizDecisionResiduos
    {
        public const string Version = "2026.11.12-1";

        public static bool EsCaneca(RutaDisposicion ruta) => ruta is RutaDisposicion.Blanca or RutaDisposicion.Negra or RutaDisposicion.Verde;

        /// <summary>Ruta de una clave. Las claves ampliadas traen la suya; las anteriores se deducen de su contexto.</summary>
        public static RutaDisposicion Ruta(ObjetoConocido objeto)
        {
            Compat.ThrowIfNull(objeto, nameof(objeto));
            if (CatalogoAmpliado.Rutas.TryGetValue(objeto.Clave, out RutaDisposicion propia)) return propia;
            if (Especificas.TryGetValue(objeto.Clave, out RutaDisposicion especifica)) return especifica;
            return objeto.Contexto switch
            {
                ContextoObjeto.ResiduoAprovechable => RutaDisposicion.Blanca,
                ContextoObjeto.ResiduoOrganico => RutaDisposicion.Verde,
                ContextoObjeto.ResiduoNoAprovechable or ContextoObjeto.ResiduoSanitario => RutaDisposicion.Negra,
                ContextoObjeto.ResiduoElectronico => RutaDisposicion.Raee,
                ContextoObjeto.ResiduoEspecial => RutaDisposicion.Peligroso,
                ContextoObjeto.AnimalVivo or ContextoObjeto.Persona or ContextoObjeto.PlantaViva => RutaDisposicion.NoEsResiduo,
                ContextoObjeto.ObjetoCotidiano => RutaDisposicion.ReutilizacionDonacion,
                _ => RutaDisposicion.Evaluar,
            };
        }

        /// <summary>Claves anteriores a la ampliación cuya ruta no se deduce bien del contexto.</summary>
        private static readonly Dictionary<string, RutaDisposicion> Especificas = new(StringComparer.Ordinal)
        {
            ["battery"] = RutaDisposicion.PilasBaterias,
            ["medicine"] = RutaDisposicion.Medicamentos,
            ["bulb"] = RutaDisposicion.Bombillos,
            ["syringe"] = RutaDisposicion.Cortopunzante,
            ["broken_glass"] = RutaDisposicion.Cortopunzante,
            ["cooking_oil"] = RutaDisposicion.AceiteUsado,
            ["aerosol"] = RutaDisposicion.Peligroso,
            ["chemical_container"] = RutaDisposicion.Peligroso,
            ["served_food"] = RutaDisposicion.Evaluar,
            ["served_fish"] = RutaDisposicion.Evaluar,
            ["raw_food"] = RutaDisposicion.Evaluar,
            ["eggs"] = RutaDisposicion.Evaluar,
            ["whole_produce"] = RutaDisposicion.Evaluar,
            ["toilet_paper_roll"] = RutaDisposicion.Evaluar,
            ["plant_waste"] = RutaDisposicion.Verde,
            ["appliance"] = RutaDisposicion.Raee,
            ["door"] = RutaDisposicion.ReutilizacionDonacion,
            ["window"] = RutaDisposicion.ReutilizacionDonacion,
            [CatalogoObjetos.VariosObjetos] = RutaDisposicion.Evaluar,
            [CatalogoObjetos.NoDistinguible] = RutaDisposicion.Evaluar,
        };

        /// <summary>Destino del flujo de canecas para una ruta (lo especial y lo reutilizable no tienen caneca).</summary>
        public static DestinoResiduo Destino(RutaDisposicion ruta) => ruta switch
        {
            RutaDisposicion.Blanca => DestinoResiduo.Blanca,
            RutaDisposicion.Negra => DestinoResiduo.Negra,
            RutaDisposicion.Verde => DestinoResiduo.Verde,
            RutaDisposicion.NoEsResiduo or RutaDisposicion.ReutilizacionDonacion => DestinoResiduo.NoEsResiduo,
            RutaDisposicion.Evaluar => DestinoResiduo.Indeterminado,
            _ => DestinoResiduo.Posconsumo,
        };

        /// <summary>Texto corto de la ruta para la persona.</summary>
        public static string Texto(RutaDisposicion ruta) => ruta switch
        {
            RutaDisposicion.Blanca => "⚪ Si está limpio y seco, va a la caneca BLANCA.",
            RutaDisposicion.Negra => "⚫ Va a la caneca NEGRA (no aprovechable).",
            RutaDisposicion.Verde => "🟢 Si lo vas a desechar, va a la caneca VERDE (orgánicos).",
            RutaDisposicion.Raee => "🔌 Es un aparato eléctrico o electrónico: no va en las canecas de EcoAccess. Si ya no sirve, entrégalo en un punto de residuos electrónicos (RAEE); si aún funciona, considera repararlo o donarlo.",
            RutaDisposicion.PilasBaterias => "🔋 No va en las canecas de EcoAccess. Entrégala en un punto de recolección de pilas y baterías; si está hinchada o dañada, no la manipules sin protección.",
            RutaDisposicion.Medicamentos => "💊 No va en las canecas de EcoAccess. Llévalo, con su empaque, a un contenedor de medicamentos posconsumo en una droguería.",
            RutaDisposicion.Bombillos => "💡 Puede romperse y contener mercurio: entrégalo entero en un punto de recolección de bombillos.",
            RutaDisposicion.Peligroso => "⚠️ Residuo peligroso: no lo deposites en las canecas ni lo mezcles. Entrégalo en un punto de residuos peligrosos o de posconsumo.",
            RutaDisposicion.Cortopunzante => "⚠️ Puede cortar o pinchar: guárdalo en un recipiente rígido cerrado y entrégalo en un punto de gestión de residuos peligrosos. No va en las canecas.",
            RutaDisposicion.AceiteUsado => "🛢️ Nunca lo viertas al desagüe: guárdalo en una botella cerrada y llévalo a un punto de recolección de aceite usado.",
            RutaDisposicion.ReutilizacionDonacion => "💚 Si todavía sirve, considera donarlo, repararlo o reutilizarlo. EcoAccess no lo recibe en sus contenedores.",
            RutaDisposicion.NoEsResiduo => "🌱 No parece un residuo.",
            _ => "🤔 Depende de algo que la foto no muestra. Cuéntame más o envía otra foto; por ahora no abro ninguna caneca.",
        };

        /// <summary>Nombre de la ruta en mayúsculas para la línea «→ …».</summary>
        public static string Etiqueta(RutaDisposicion ruta) => ruta switch
        {
            RutaDisposicion.Blanca => "⚪ BLANCA",
            RutaDisposicion.Negra => "⚫ NEGRA",
            RutaDisposicion.Verde => "🟢 VERDE",
            RutaDisposicion.Raee => "🔌 RESIDUOS ELECTRÓNICOS (RAEE)",
            RutaDisposicion.PilasBaterias => "🔋 RECOLECCIÓN DE PILAS",
            RutaDisposicion.Medicamentos => "💊 MEDICAMENTOS POSCONSUMO",
            RutaDisposicion.Bombillos => "💡 RECOLECCIÓN DE BOMBILLOS",
            RutaDisposicion.Peligroso => "⚠️ RESIDUOS PELIGROSOS",
            RutaDisposicion.Cortopunzante => "⚠️ CORTOPUNZANTES",
            RutaDisposicion.AceiteUsado => "🛢️ ACEITE USADO",
            RutaDisposicion.ReutilizacionDonacion => "💚 REUTILIZAR O DONAR",
            RutaDisposicion.NoEsResiduo => "no es un residuo",
            _ => "por confirmar",
        };
    }
}
