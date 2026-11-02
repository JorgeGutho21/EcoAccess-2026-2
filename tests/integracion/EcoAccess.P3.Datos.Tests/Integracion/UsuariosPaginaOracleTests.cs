using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Datos.Repositorios;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica.Contratos;

namespace EcoAccess.P3.Datos.Tests.Integracion
{

    [TestClass]
    [TestCategory(Categoria)]
    [DoNotParallelize]
    public sealed class UsuariosPaginaOracleTests : BaseIntegracionOracle
    {
        [TestMethod]
        public async Task PaginaLimitaCuentasSinDividirRolesYOrdenaEmpatesPorId()
        {
            var repo = new RepositorioUsuariosOracle(Fabrica);
            string marca = Sufijo().ToLowerInvariant();
            int primero = RegistrarUsuario(await repo.AgregarAsync(new Usuario("Prueba " + marca, "pag1." + marca + "@ejemplo.com", CodigoRol.Ciudadano, DateTime.UtcNow)));
            await repo.AgregarRolAsync(primero, CodigoRol.Operario);
            await repo.AgregarRolAsync(primero, CodigoRol.Administrador);
            int segundo = RegistrarUsuario(await repo.AgregarAsync(new Usuario("Prueba " + marca, "pag2." + marca + "@ejemplo.com", CodigoRol.Ciudadano, DateTime.UtcNow)));
            var uno = await repo.ConsultarPaginaAsync(new FiltroUsuarios(marca, tamanoPagina: 1));
            Assert.HasCount(1, uno.Filas);
            Assert.AreEqual(primero, uno.Filas[0].Id);
            Assert.HasCount(3, uno.Filas[0].Roles);
            Assert.IsTrue(uno.HaySiguiente);
            var dos = await repo.ConsultarPaginaAsync(new FiltroUsuarios(marca, pagina: 2, tamanoPagina: 1));
            Assert.AreEqual(segundo, dos.Filas.Single().Id);
            Assert.IsFalse(dos.HaySiguiente);
            Assert.IsEmpty((await repo.ConsultarPaginaAsync(new FiltroUsuarios(marca, pagina: 3, tamanoPagina: 1))).Filas);
            var operarios = await repo.ConsultarPaginaAsync(new FiltroUsuarios(marca, rol: CodigoRol.Operario));
            Assert.AreEqual(primero, operarios.Filas.Single().Id);
            Assert.HasCount(3, operarios.Filas[0].Roles, "El filtro de rol no debe ocultar los otros roles de la cuenta.");
            await repo.CambiarActivoAsync(segundo, false);
            Assert.AreEqual(segundo, (await repo.ConsultarPaginaAsync(new FiltroUsuarios(marca, activo: false))).Filas.Single().Id);
            Assert.IsTrue((await repo.ListarAsync()).Any(u => u.Id == segundo), "El listado interno completo no debe cambiar.");
        }

        [TestMethod]
        public async Task BusquedaTrataComillasYComodinesComoTextoLiteral()
        {
            var repo = new RepositorioUsuariosOracle(Fabrica);
            string marca = Sufijo().ToLowerInvariant();
            int id = RegistrarUsuario(await repo.AgregarAsync(new Usuario("Prueba " + marca + " %_'",
                "literal." + marca + "@ejemplo.com", CodigoRol.Ciudadano, DateTime.UtcNow)));
            RegistrarUsuario(await repo.AgregarAsync(new Usuario("Prueba " + marca + " ABC",
                "normal." + marca + "@ejemplo.com", CodigoRol.Ciudadano, DateTime.UtcNow)));
            var literal = await repo.ConsultarPaginaAsync(new FiltroUsuarios(marca + " %_'"));
            Assert.AreEqual(id, literal.Filas.Single().Id);
            Assert.IsEmpty((await repo.ConsultarPaginaAsync(new FiltroUsuarios("' OR 1=1 --"))).Filas);
            Assert.HasCount(2, (await repo.ConsultarPaginaAsync(new FiltroUsuarios("  " + marca.ToUpperInvariant() + " "))).Filas);
        }
    }
}
