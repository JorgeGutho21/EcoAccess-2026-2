using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;
using EcoAccess.P3.Logica;
using EcoAccess.P3.Logica.Contratos;
using EcoAccess.P3.Logica.Servicios;
using EcoAccess.P3.Tests.Logica.Dobles;

namespace EcoAccess.P3.Tests.Logica
{

    /// <summary>Usa los mismos códigos y nombres del catálogo de datos de prueba (01_catalogos.sql).</summary>
    [TestClass]
    public sealed class ServicioCatalogoAmbientalTests
    {
        private readonly RepositorioMaterialesEnMemoria _materiales = new();
        private readonly RepositorioContenedoresEnMemoria _contenedores = new();
        private readonly ServicioCatalogoAmbiental _servicio;
        private readonly Material _pet;
        private readonly Material _papel;
        private readonly Material _lata;
        private readonly Material _servilleta;
        private readonly Material _pila;

        public ServicioCatalogoAmbientalTests()
        {
            _materiales.Contenedores = _contenedores;
            _contenedores.Materiales = _materiales;
            _pet = _materiales.Sembrar("PET", "Botella plástica PET", CategoriaMaterial.Aprovechable);
            _materiales.Sembrar("PLASTICO_RIGIDO", "Envase plástico rígido", CategoriaMaterial.Aprovechable);
            _papel = _materiales.Sembrar("PAPEL", "Papel de oficina y cuadernos", CategoriaMaterial.Aprovechable);
            _materiales.Sembrar("VIDRIO", "Botella o frasco de vidrio", CategoriaMaterial.Aprovechable);
            _lata = _materiales.Sembrar("LATA_ALUMINIO", "Lata de aluminio", CategoriaMaterial.Aprovechable);
            _materiales.Sembrar("ORGANICO", "Restos de comida", CategoriaMaterial.OrganicoAprovechable);
            _servilleta = _materiales.Sembrar("SERVILLETA_USADA", "Servilleta o papel higiénico usado", CategoriaMaterial.NoAprovechable);
            _pila = _materiales.Sembrar("PILA", "Pila o batería", CategoriaMaterial.Peligroso);
            _servicio = new ServicioCatalogoAmbiental(_materiales, _contenedores);
        }

        private async Task<string[]> CodigosAsync(string? texto) =>
            (await _servicio.BuscarAsync(texto)).Select(m => m.Codigo).ToArray();

        [TestMethod]
        public async Task SinonimosYPrecaucionesSeCompartenSinClasificarCualquierPlasticoComoPet()
        {
            int id = await _materiales.AgregarAsync(new Material("JERINGA", "Jeringa usada", CategoriaMaterial.Peligroso,
                "Consulta a tu EPS o IPS.", "aguja lanceta cortopunzante", "No mezclar con medicamentos.", "Ruta de cortopunzantes."));
            Assert.AreEqual(id, (await _servicio.BuscarAsync("lanceta")).Single().Id);
            Assert.AreEqual(id, (await _servicio.ConsultarPaginaPublicaAsync("aguja")).Valor.Filas.Single().Id);
            var consulta = (await _servicio.ConsultarAsync(id, null)).Valor;
            string informacion = MensajesTelegram.InformacionMaterial(consulta, null);
            StringAssert.Contains(informacion, "EPS o IPS");
            StringAssert.Contains(informacion, "No mezclar con medicamentos");
            StringAssert.Contains(informacion, "Ruta de cortopunzantes");
            Assert.DoesNotContain("PET", informacion);
        }

        [TestMethod]
        public async Task PaginaPublicaAcotaDiezYOcultaFixturesSinBorrarlos()
        {
            for (int i = 0; i < 25; i++) _materiales.Sembrar("NORMAL_" + i.ToString("D2"));
            var privado = _materiales.Sembrar("BR_NO_PUBLICO", "Material ficticio bridge", CategoriaMaterial.Aprovechable);
            var primera = await _servicio.ConsultarPaginaPublicaAsync("", 1);
            var segunda = await _servicio.ConsultarPaginaPublicaAsync("", 2);
            Assert.IsTrue(primera.Exitoso); Assert.HasCount(10, primera.Valor.Filas);
            Assert.IsTrue(primera.Valor.HaySiguiente);
            Assert.IsFalse(primera.Valor.Filas.Select(m => m.Id).Intersect(segunda.Valor.Filas.Select(m => m.Id)).Any());
            Assert.IsTrue(primera.Valor.Filas.Concat(segunda.Valor.Filas).All(m => !FiltroMateriales.EsDatoDePrueba(m)));
            Assert.IsTrue(_materiales.Leer(privado.Id)!.Habilitado);
            Assert.IsTrue((await _materiales.ListarAsync(true)).Any(m => m.Id == privado.Id));
            Assert.AreEqual(2, _materiales.ConsultasPagina);
            Assert.AreEqual(MotivoRechazo.NoEncontrado, (await _servicio.ConsultarAsync(privado.Id, null)).Motivo);
            Assert.IsEmpty(await _servicio.BuscarAsync("bridge"));
        }

        [TestMethod]
        public async Task PaginaPublicaValidaLimitesYCategoriaAntesDeConsultar()
        {
            Assert.IsFalse((await _servicio.ConsultarPaginaPublicaAsync("", 0)).Exitoso);
            Assert.IsFalse((await _servicio.ConsultarPaginaPublicaAsync(new string('a', 101))).Exitoso);
            Assert.IsFalse((await _servicio.ConsultarPaginaPublicaAsync("", categoria: (CategoriaMaterial)999)).Exitoso);
            Assert.AreEqual(0, _materiales.ConsultasPagina);
            var r = await _servicio.ConsultarPaginaPublicaAsync("", categoria: CategoriaMaterial.Peligroso);
            Assert.IsTrue(r.Valor.Filas.All(m => m.Categoria == CategoriaMaterial.Peligroso));
        }

        [TestMethod]
        public async Task ConsultarCaneca_PorQr_DevuelveSoloMaterialesQueRecibe()
        {
            Contenedor caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id, _papel.Id);

            var r = await _servicio.ConsultarCanecaAsync(caneca.IdentificadorQr);
            var inexistente = await _servicio.ConsultarCanecaAsync("QR-QUE-NO-EXISTE");

            Assert.IsTrue(r.Exitoso, r.ToString());
            CollectionAssert.AreEquivalent(new[] { "PET", "PAPEL" }, r.Valor.Admitidos.Select(m => m.Codigo).ToArray());
            Assert.AreEqual(MotivoRechazo.NoEncontrado, inexistente.Motivo);
        }

        [TestMethod]
        [DataRow("BATERÍA", "PILA")]
        [DataRow("bateria", "PILA")]
        [DataRow("pilas", "PILA")]
        [DataRow("¿dónde boto una pila?", "PILA")]
        [DataRow("lata", "LATA_ALUMINIO")]
        [DataRow("lata aluminio", "LATA_ALUMINIO")]
        [DataRow("pet", "PET")]
        [DataRow("comida", "ORGANICO")]
        public async Task BuscarAsync_IgnoraMayusculasTildesPluralesYPalabrasVacias(string texto, string codigo)
        {
            CollectionAssert.AreEqual(new[] { codigo }, await CodigosAsync(texto));
        }

        [TestMethod]
        public async Task BuscarAsync_ElCodigoExactoVaPrimeroYLaMayorCoincidenciaDespues()
        {
            Assert.AreEqual("PET", (await CodigosAsync("PET"))[0]);
            CollectionAssert.AreEqual(new[] { "SERVILLETA_USADA", "PAPEL" }, await CodigosAsync("papel higiénico"));
        }

        [TestMethod]
        public async Task BuscarAsync_VariasCoincidencias_OrdenaPorNombre()
        {
            CollectionAssert.AreEqual(new[] { "VIDRIO", "PET" }, await CodigosAsync("botellas"));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("   ")]
        [DataRow("¿?!")]
        [DataRow("de la")]
        [DataRow("celular")]
        [DataRow("'; DROP TABLE materiales; --")]
        public async Task BuscarAsync_SinPalabrasUtilesOSinCoincidencias_DevuelveVacio(string? texto)
        {
            Assert.IsEmpty(await CodigosAsync(texto));
        }

        [TestMethod]
        public async Task BuscarAsync_NoMuestraMaterialesDeshabilitados()
        {
            _materiales.Sembrar("MEDICAMENTO", "Medicamento vencido", CategoriaMaterial.Peligroso, habilitado: false);

            Assert.IsEmpty(await CodigosAsync("medicamento"));
        }

        [TestMethod]
        public async Task BuscarAsync_DevuelveComoMaximoCincoResultados()
        {
            for (int i = 0; i < 8; i++)
            {
                _materiales.Sembrar($"CAJA_{i}", $"Caja {i}", CategoriaMaterial.Aprovechable);
            }

            Assert.HasCount(ServicioCatalogoAmbiental.MaximoResultados, await CodigosAsync("caja"));
        }

        [TestMethod]
        public async Task BuscarAsync_SoloAnalizaLosPrimerosCienCaracteres()
        {
            string relleno = new('x', ServicioCatalogoAmbiental.LargoMaximoBusqueda);

            Assert.IsEmpty(await CodigosAsync(relleno + " pila"));
            CollectionAssert.AreEqual(new[] { "PILA" }, await CodigosAsync("pila " + relleno));
        }

        [TestMethod]
        public async Task ConsultarAsync_SinCaneca_DaLaGuiaSinAdmision()
        {
            Resultado<ConsultaMaterial> r = await _servicio.ConsultarAsync(_pila.Id, null);

            Assert.IsTrue(r.Exitoso);
            Assert.AreEqual(AdmisionMaterial.SinCaneca, r.Valor.Admision);
            Assert.IsNull(r.Valor.Guia.ColorCaneca, "Un peligroso no va en ninguna caneca de colores.");
        }

        [TestMethod]
        public async Task ConsultarAsync_SeparaClasificacionDeAdmision()
        {
            Contenedor caneca = _contenedores.Sembrar(1, EstadoContenedor.EnServicio, _pet.Id);

            Resultado<ConsultaMaterial> admitido = await _servicio.ConsultarAsync(_pet.Id, caneca.Id);
            Resultado<ConsultaMaterial> noAdmitido = await _servicio.ConsultarAsync(_lata.Id, caneca.Id);

            Assert.AreEqual(AdmisionMaterial.Admitido, admitido.Valor.Admision);
            Assert.AreEqual(AdmisionMaterial.NoAdmitido, noAdmitido.Valor.Admision);
            Assert.AreEqual("blanca", noAdmitido.Valor.Guia.ColorCaneca, "Es aprovechable aunque esta caneca no lo reciba.");
        }

        [TestMethod]
        [DataRow(EstadoContenedor.FueraDeServicio)]
        [DataRow(EstadoContenedor.DadoDeBaja)]
        public async Task ConsultarAsync_CanecaNoEnServicio_EsCanecaNoDisponible(EstadoContenedor estado)
        {
            Contenedor caneca = _contenedores.Sembrar(1, estado, _pet.Id);

            Assert.AreEqual(AdmisionMaterial.CanecaNoDisponible, (await _servicio.ConsultarAsync(_pet.Id, caneca.Id)).Valor.Admision);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(999)]
        public async Task ConsultarAsync_CanecaInexistente_EsCanecaNoDisponible(int idContenedor)
        {
            Assert.AreEqual(AdmisionMaterial.CanecaNoDisponible, (await _servicio.ConsultarAsync(_servilleta.Id, idContenedor)).Valor.Admision);
        }

        [TestMethod]
        public async Task ConsultarAsync_MaterialInexistenteODeshabilitado_EsNoEncontrado()
        {
            Material retirado = _materiales.Sembrar("MEDICAMENTO", "Medicamento vencido", CategoriaMaterial.Peligroso, habilitado: false);

            foreach (int id in new[] { 0, -1, 999, retirado.Id })
            {
                Resultado<ConsultaMaterial> r = await _servicio.ConsultarAsync(id, null);
                Assert.AreEqual(MotivoRechazo.NoEncontrado, r.Motivo, $"id {id}");
            }
        }

        [TestMethod]
        public async Task ConsultarAsync_LaGuiaCorrespondeALaCategoria()
        {
            Assert.AreEqual("negra", (await _servicio.ConsultarAsync(_servilleta.Id, null)).Valor.Guia.ColorCaneca);
            Assert.AreEqual("blanca", (await _servicio.ConsultarAsync(_papel.Id, null)).Valor.Guia.ColorCaneca);
        }

        [TestMethod]
        public void ElConstructorRechazaRepositoriosNulos()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioCatalogoAmbiental(null!, _contenedores));
            Assert.ThrowsExactly<ArgumentNullException>(() => new ServicioCatalogoAmbiental(_materiales, null!));
        }
    }
}
