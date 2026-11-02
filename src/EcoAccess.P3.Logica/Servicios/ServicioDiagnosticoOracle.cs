using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Autoriza al administrador y contrasta el inventario con las migraciones del proyecto.</summary>
    public sealed class ServicioDiagnosticoOracle
    {
        private readonly ControlAcceso _acceso;
        private readonly IRepositorioDiagnosticoOracle _repositorio;
        private readonly TimeProvider _reloj;

        private static readonly (string Version, string[] Tablas)[] Migraciones =
        new (string Version, string[] Tablas)[]
    {
        ("Inicial", new string[] { "ROL", "USUARIO", "USUARIO_ROL", "MATERIAL", "PUNTO_INSTALACION", "CONTENEDOR", "CONTENEDOR_MATERIAL" }),
        ("V002", new string[] { "CREDENCIAL_ACCESO" }),
        ("V003", new string[] { "INTERACCION", "ORDEN_APERTURA", "EVENTO_DISPOSITIVO", "DEPOSITO" }),
        ("V004", new string[] { "VINCULO_TELEGRAM", "TOKEN_VINCULACION", "CONVERSACION_TELEGRAM", "UPDATE_TELEGRAM" }),
        ("V005", new string[] { "REGLA_ECOPUNTOS", "OPERACION_ECOPUNTOS", "MOVIMIENTO_ECOPUNTOS" })
    };
        private static readonly string[] RestriccionesV006 =
        new string[]
    {
        "ROL:CK_V006_ROL_DESCRIPCION",
        "USUARIO:CK_V006_USUARIO_NOMBRE",
        "MATERIAL:CK_V006_MATERIAL_NOMBRE",
        "PUNTO_INSTALACION:CK_V006_PUNTO_NOMBRE",
        "CONTENEDOR:CK_V006_CONTENEDOR_NOMBRE",
        "MATERIAL:CK_V006_MATERIAL_CODIGO"
    };

        public ServicioDiagnosticoOracle(IRepositorioUsuarios usuarios, IRepositorioDiagnosticoOracle repositorio, TimeProvider reloj)
        {
            _acceso = new ControlAcceso(usuarios);
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
            _reloj = reloj ?? throw new ArgumentNullException(nameof(reloj));
        }

        public async Task<Resultado<DiagnosticoOracle>> ConsultarAsync(int idActor, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (await _acceso.ExigirRolAsync(idActor, CodigoRol.Administrador, cancellationToken) is { } rechazo)
                return Resultado<DiagnosticoOracle>.DesdeRechazo(rechazo);

            EstadoEsquemaOracle inventario = await _repositorio.ConsultarAsync(cancellationToken);
            var tablas = inventario.Tablas.ToHashSet(StringComparer.Ordinal);
            var estados = Migraciones.Select(m =>
            {
                string[] faltantes = m.Tablas.Where(t => !tablas.Contains(t)).ToArray();
                string estado = faltantes.Length == 0 ? "Estructura presente"
                    : faltantes.Length == m.Tablas.Length ? "Ausente" : "Parcial";
                return new EstadoMigracionOracle(m.Version, estado, string.Join(", ", faltantes));
            }).ToList();
            var restricciones = inventario.RestriccionesValidadas.ToHashSet(StringComparer.Ordinal);
            string[] faltantesV006 = RestriccionesV006.Where(r => !restricciones.Contains(r)).ToArray();
            estados.Add(new("V006", faltantesV006.Length == 0 ? "Estructura presente"
                : faltantesV006.Length == RestriccionesV006.Length ? "Ausente" : "Parcial", string.Join(", ", faltantesV006)));
            return Resultado<DiagnosticoOracle>.Exito(new(_reloj.GetUtcNow(), inventario, estados.AsReadOnly()));
        }
    }
}
