using System;
using System.Threading;
using System.Threading.Tasks;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Logica.Servicios
{

    /// <summary>Una sesión recordada no autoriza consultar indicadores: recarga al actor en cada petición.</summary>
    public sealed class ServicioPanelAdministrativo
    {
        private readonly ControlAcceso _acceso;
        private readonly IRepositorioPanelAdministrativo _repositorio;

        public ServicioPanelAdministrativo(IRepositorioUsuarios usuarios, IRepositorioPanelAdministrativo repositorio)
        {
            _acceso = new ControlAcceso(usuarios);
            _repositorio = repositorio ?? throw new ArgumentNullException(nameof(repositorio));
        }

        public async Task<Resultado<ResumenPanelAdministrativo>> ConsultarAsync(int actor, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            if (await _acceso.ExigirAlgunRolAsync(actor, ct, CodigoRol.Administrador, CodigoRol.Operario) is { } rechazo)
                return Resultado<ResumenPanelAdministrativo>.DesdeRechazo(rechazo);
            return Resultado<ResumenPanelAdministrativo>.Exito(await _repositorio.ConsultarAsync(ct));
        }
    }
}
