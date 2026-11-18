using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace EcoAccess.P3.Instalador
{
    /// <summary>Un paso del plan: una versión del esquema y el script que la produce.</summary>
    /// <param name="Version">Identificador estable que se guarda en ECO_MIGRACION_APLICADA (V001, C001, V002…).</param>
    /// <param name="Archivo">Ruta relativa a la raíz del repositorio, con «/».</param>
    /// <param name="Opcional">Los datos de demostración solo se instalan si se piden con --con-demo.</param>
    public sealed record PasoInstalacion(string Version, string Descripcion, string Archivo, bool Opcional = false)
    {
        /// <summary>SHA-256 del archivo: permite detectar si un script ya aplicado se modificó después.</summary>
        public string Huella(string raiz)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(File.ReadAllBytes(Path.Combine(raiz, Archivo)));
            return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
        }
    }

    /// <summary>
    /// Orden único de instalación del esquema de EcoAccess. Es la única lista que hay que tocar al crear una migración
    /// nueva (V021, V022…): se agrega al final y se ejecuta solo donde falte. Sigue el mismo orden que usaba
    /// scripts\instalacion\Preparar-BaseDatos.ps1 con Docker.
    /// </summary>
    public static class PlanInstalacion
    {
        public static readonly IReadOnlyList<PasoInstalacion> Pasos = new[]
        {
            new PasoInstalacion("V001", "Tablas iniciales: rol, usuario, material, punto de instalación y contenedor", "database/02_tablas/01_crear_tablas_iniciales.sql"),
            new PasoInstalacion("C001", "Catálogos de roles y materiales (MERGE, sin duplicar)", "database/03_datos_prueba/01_catalogos.sql"),
            new PasoInstalacion("V002", "Credenciales de acceso (PBKDF2)", "database/06_migraciones/V002__credencial_acceso.sql"),
            new PasoInstalacion("V003", "Interacciones, órdenes de apertura, eventos y depósitos", "database/06_migraciones/V003__interacciones_depositos.sql"),
            new PasoInstalacion("V004", "Telegram: vínculos, conversaciones y notificaciones", "database/06_migraciones/V004__telegram.sql"),
            new PasoInstalacion("V005", "Ecopuntos auditables", "database/06_migraciones/V005__ecopuntos.sql"),
            new PasoInstalacion("V006", "Refuerzo de restricciones CHECK iniciales", "database/06_migraciones/V006__reforzar_restricciones_iniciales.sql"),
            new PasoInstalacion("V007", "Vinculación de cuentas existentes con Telegram", "database/06_migraciones/V007__vinculacion_cuenta_existente.sql"),
            new PasoInstalacion("V008", "Trazabilidad del análisis visual", "database/06_migraciones/V008__trazabilidad_vision.sql"),
            new PasoInstalacion("V009", "Cantidad declarada en el depósito", "database/06_migraciones/V009__cantidad_declarada.sql"),
            new PasoInstalacion("V010", "Opiniones de los ciudadanos", "database/06_migraciones/V010__opiniones.sql"),
            new PasoInstalacion("V011", "Tarifas de ecopuntos por unidad", "database/06_migraciones/V011__tarifas_por_unidad.sql"),
            new PasoInstalacion("V012", "Orientación del catálogo ambiental", "database/06_migraciones/V012__orientacion_catalogo.sql"),
            new PasoInstalacion("V013", "Vistas para informes", "database/06_migraciones/V013__vistas_informes.sql"),
            new PasoInstalacion("V014", "Canal Telegram simulado", "database/06_migraciones/V014__canal_telegram_simulado.sql"),
            new PasoInstalacion("V015", "Informes de demostración con cantidades", "database/06_migraciones/V015__informes_demostracion_cantidades.sql"),
            new PasoInstalacion("V016", "Depósito sin sensor", "database/06_migraciones/V016__deposito_sin_sensor.sql"),
            new PasoInstalacion("V017", "Canal asistido", "database/06_migraciones/V017__canal_asistido.sql"),
            new PasoInstalacion("V018", "Consentimiento para visión externa", "database/06_migraciones/V018__consentimiento_vision_externa.sql"),
            new PasoInstalacion("V019", "Vistas para Power BI", "database/06_migraciones/V019__vistas_power_bi.sql"),
            new PasoInstalacion("V020", "Comentarios ciudadanos", "database/06_migraciones/V020__comentarios_ciudadanos.sql"),
            new PasoInstalacion("C002", "Catálogo ambiental del piloto", "database/03_datos_prueba/03_catalogo_ambiental_piloto.sql"),
            new PasoInstalacion("C003", "Catálogo ampliado de materiales", "database/03_datos_prueba/06_catalogo_ampliado.sql"),
            // Versiones nuevas: siempre al final de las obligatorias, para no dejar huecos donde ya hay versiones aplicadas.
            new PasoInstalacion("V021", "Estación de tres canecas: categoría, dispositivo y ubicación de cada caneca", "database/06_migraciones/V021__configuracion_tres_canecas.sql"),
            new PasoInstalacion("V022", "Paquete, función y procedimiento de estadísticas Oracle", "database/06_migraciones/V022__objetos_plsql_estadisticas.sql"),
            new PasoInstalacion("V023", "Consultas de visión atendidas por Echo (tabla y vista agregada)", "database/06_migraciones/V023__consulta_vision.sql"),
            new PasoInstalacion("V024", "V024__estacion_piloto_tres_canecas", "database/06_migraciones/V024__estacion_piloto_tres_canecas.sql"),
            new PasoInstalacion("V025", "V025__lote_simulacion", "database/06_migraciones/V025__lote_simulacion.sql"),
            new PasoInstalacion("V026", "Puntos de disposición especial de Valledupar (programas y lugares verificados)", "database/06_migraciones/V026__puntos_disposicion_valledupar.sql"),
            new PasoInstalacion("V027", "Capa académica de Base de Datos: paquetes, disparadores, auditoría y vistas VW_BI", "database/06_migraciones/V027__capa_academica_bd.sql"),
            new PasoInstalacion("V028", "Códigos reales de resultado en VW_BI_CONSULTAS_VISION", "database/06_migraciones/V028__vista_bi_consultas_vision.sql"),
            new PasoInstalacion("D001","Datos de DEMOSTRACIÓN ficticios (punto y caneca DEMO)", "database/03_datos_prueba/02_datos_demo.sql", Opcional: true),
        };

        /// <summary>Pasos que faltan, en orden, sin los opcionales salvo que se pidan.</summary>
        public static IReadOnlyList<PasoInstalacion> Pendientes(ISet<string> aplicadas, bool conDemo) =>
            Pasos.Where(p => !aplicadas.Contains(p.Version) && (conDemo || !p.Opcional)).ToList();

        /// <summary>
        /// Una versión obligatoria solo puede aplicarse si todas las anteriores ya lo están: evita «huecos» si alguien
        /// marcó a mano una versión posterior.
        /// </summary>
        public static string? ValidarOrden(ISet<string> aplicadas)
        {
            bool falta = false;
            foreach (PasoInstalacion paso in Pasos.Where(p => !p.Opcional))
            {
                if (!aplicadas.Contains(paso.Version)) falta = true;
                else if (falta) return "La versión " + paso.Version + " figura aplicada, pero falta una anterior. Revisa ECO_MIGRACION_APLICADA.";
            }

            return null;
        }
    }
}
