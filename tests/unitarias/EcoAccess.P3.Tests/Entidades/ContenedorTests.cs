using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EcoAccess.P3.Entidades;

namespace EcoAccess.P3.Tests.Entidades
{

    [TestClass]
    public class ContenedorTests
    {
        private static readonly DateTime FechaUtc = new(2026, 10, 24, 15, 0, 0, DateTimeKind.Utc);

        // Identificador ficticio de 22 caracteres, como el que genera un valor aleatorio de 128 bits en base64url.
        private const string QrValido = "Kp3_x9ZqT-4mWbN7cRsLvA";

        private static Contenedor CrearCaneca() => new(1, "Caneca piloto bloque A", QrValido, FechaUtc);

        private static Contenedor CrearCanecaEnServicio(params int[] materiales)
        {
            Contenedor caneca = CrearCaneca();
            foreach (int idMaterial in materiales.Length == 0 ? new[] { 10 } : materiales)
            {
                caneca.AdmitirMaterial(idMaterial);
            }

            caneca.PonerEnServicio();
            return caneca;
        }

        // ---------- registro ----------

        [TestMethod]
        public void Constructor_DatosValidos_CreaCanecaNuevaFueraDeServicioYSinMateriales()
        {
            Contenedor caneca = CrearCaneca();

            Assert.AreEqual(0, caneca.Id);
            Assert.IsTrue(caneca.EsNuevo);
            Assert.AreEqual(1, caneca.IdPuntoInstalacion);
            Assert.AreEqual("Caneca piloto bloque A", caneca.Nombre);
            Assert.AreEqual(QrValido, caneca.IdentificadorQr);
            Assert.AreEqual(FechaUtc, caneca.FechaAltaUtc);
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, caneca.Estado);
            Assert.IsFalse(caneca.EstaEnServicio);
            Assert.IsEmpty(caneca.MaterialesAdmitidos);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-1)]
        public void Constructor_PuntoInstalacionNoPositivo_LanzaArgumentOutOfRangeException(int idPunto)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Contenedor(idPunto, "Caneca", QrValido, FechaUtc));
        }

        [TestMethod]
        [DataRow(Contenedor.LongitudMinimaNombre)]
        [DataRow(Contenedor.LongitudMaximaNombre)]
        public void Constructor_NombreEnLosLimitesDeLongitud_LoAcepta(int longitud)
        {
            var caneca = new Contenedor(1, new string('c', longitud), QrValido, FechaUtc);

            Assert.AreEqual(longitud, caneca.Nombre.Length);
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("C")]
        public void Constructor_NombreVacioOMuyCorto_LanzaArgumentException(string? nombre)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Contenedor(1, nombre!, QrValido, FechaUtc));
        }

        [TestMethod]
        public void Constructor_NombreMasLargoQueElMaximo_LanzaArgumentException()
        {
            string largo = new('c', Contenedor.LongitudMaximaNombre + 1);

            Assert.ThrowsExactly<ArgumentException>(() => new Contenedor(1, largo, QrValido, FechaUtc));
        }

        [TestMethod]
        [DataRow(Contenedor.LongitudMinimaIdentificadorQr)]
        [DataRow(Contenedor.LongitudMaximaIdentificadorQr)]
        public void Constructor_IdentificadorQrEnLosLimitesDeLongitud_LoAcepta(int longitud)
        {
            string identificador = new('a', longitud);

            var caneca = new Contenedor(1, "Caneca", identificador, FechaUtc);

            Assert.AreEqual(identificador, caneca.IdentificadorQr);
        }

        [TestMethod]
        [DataRow(Contenedor.LongitudMinimaIdentificadorQr - 1)]
        [DataRow(Contenedor.LongitudMaximaIdentificadorQr + 1)]
        public void Constructor_IdentificadorQrFueraDeLosLimites_LanzaArgumentException(int longitud)
        {
            string identificador = new('a', longitud);

            Assert.ThrowsExactly<ArgumentException>(() => new Contenedor(1, "Caneca", identificador, FechaUtc));
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow("corto123")]
        [DataRow("Kp3_x9ZqT-4mWbN7cRsLvA!")]             // carácter no permitido por Telegram
        [DataRow("Kp3_x9ZqT 4mWbN7cRsLvA")]              // espacio interno
        [DataRow(" Kp3_x9ZqT-4mWbN7cRsLvA")]             // espacio inicial: no se recorta, es un dato técnico
        [DataRow("Kp3_x9ZqT-4mWbN7cRsLvA\n")]            // salto de línea final
        [DataRow("Kp3_x9ZqT-4mWbN7cRsLñvA")]             // letra con tilde
        [DataRow("Kp3+x9ZqT/4mWbN7cRsLvA==")]            // base64 estándar en vez de base64url: + / =
        [DataRow("Kp3_x9ZqT.4mWbN7cRsLvA")]              // punto
        [DataRow("Kp3_x9ZqT-4mWbN7cRsLv٣")]              // dígito árabe: char.IsDigit lo aceptaría
        public void Constructor_IdentificadorQrInvalido_LanzaArgumentException(string? identificador)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new Contenedor(1, "Caneca", identificador!, FechaUtc));
        }

        [TestMethod]
        public void Constructor_FechaNoUtc_LanzaArgumentException()
        {
            var fechaLocal = DateTime.SpecifyKind(FechaUtc, DateTimeKind.Local);

            Assert.ThrowsExactly<ArgumentException>(() => new Contenedor(1, "Caneca", QrValido, fechaLocal));
        }

        // ---------- reconstrucción ----------

        [TestMethod]
        public void ConstructorReconstruccion_DatosValidos_ConservaTodosLosValores()
        {
            var caneca = new Contenedor(5, 2, "Caneca bloque B", QrValido, FechaUtc, EstadoContenedor.EnServicio, new[] { 10, 11 });

            Assert.AreEqual(5, caneca.Id);
            Assert.AreEqual(2, caneca.IdPuntoInstalacion);
            Assert.AreEqual("Caneca bloque B", caneca.Nombre);
            Assert.IsTrue(caneca.EstaEnServicio);
            Assert.IsTrue(caneca.AdmiteMaterial(10));
            Assert.IsTrue(caneca.AdmiteMaterial(11));
        }

        [TestMethod]
        public void ConstructorReconstruccion_ListaOriginalModificadaDespues_NoAlteraLaCaneca()
        {
            var materiales = new List<int> { 10 };
            var caneca = new Contenedor(5, 1, "Caneca", QrValido, FechaUtc, EstadoContenedor.EnServicio, materiales);

            materiales.Add(99);

            Assert.IsFalse(caneca.AdmiteMaterial(99));
            Assert.HasCount(1, caneca.MaterialesAdmitidos);
        }

        [TestMethod]
        public void ConstructorReconstruccion_EnServicioSinMateriales_LanzaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Contenedor(5, 1, "Caneca", QrValido, FechaUtc, EstadoContenedor.EnServicio, Array.Empty<int>()));
        }

        [TestMethod]
        public void ConstructorReconstruccion_FueraDeServicioSinMateriales_LoAcepta()
        {
            var caneca = new Contenedor(5, 1, "Caneca", QrValido, FechaUtc, EstadoContenedor.FueraDeServicio, Array.Empty<int>());

            Assert.IsEmpty(caneca.MaterialesAdmitidos);
        }

        [TestMethod]
        public void ConstructorReconstruccion_MaterialesRepetidos_LanzaArgumentException()
        {
            Assert.ThrowsExactly<ArgumentException>(
                () => new Contenedor(5, 1, "Caneca", QrValido, FechaUtc, EstadoContenedor.FueraDeServicio, new[] { 10, 10 }));
        }

        [TestMethod]
        public void ConstructorReconstruccion_MaterialConIdNoPositivo_LanzaArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Contenedor(5, 1, "Caneca", QrValido, FechaUtc, EstadoContenedor.FueraDeServicio, new[] { 10, 0 }));
        }

        [TestMethod]
        public void ConstructorReconstruccion_MaterialesNulos_LanzaArgumentNullException()
        {
            Assert.ThrowsExactly<ArgumentNullException>(
                () => new Contenedor(5, 1, "Caneca", QrValido, FechaUtc, EstadoContenedor.FueraDeServicio, null!));
        }

        [TestMethod]
        public void ConstructorReconstruccion_EstadoNoDefinido_LanzaArgumentOutOfRangeException()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Contenedor(5, 1, "Caneca", QrValido, FechaUtc, (EstadoContenedor)9, new[] { 10 }));
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-2)]
        public void ConstructorReconstruccion_IdNoPositivo_LanzaArgumentOutOfRangeException(int id)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(
                () => new Contenedor(id, 1, "Caneca", QrValido, FechaUtc, EstadoContenedor.FueraDeServicio, new[] { 10 }));
        }

        // ---------- materiales admitidos ----------

        [TestMethod]
        public void AdmitirMaterial_IdValido_LoAgrega()
        {
            Contenedor caneca = CrearCaneca();

            caneca.AdmitirMaterial(10);

            Assert.IsTrue(caneca.AdmiteMaterial(10));
            Assert.IsFalse(caneca.AdmiteMaterial(11));
        }

        [TestMethod]
        public void AdmitirMaterial_CanecaEnServicio_LoAgregaSinCambiarElEstado()
        {
            Contenedor caneca = CrearCanecaEnServicio(10);

            caneca.AdmitirMaterial(11);

            Assert.IsTrue(caneca.AdmiteMaterial(11));
            Assert.IsTrue(caneca.EstaEnServicio);
        }

        [TestMethod]
        [DataRow(0)]
        [DataRow(-4)]
        public void AdmitirMaterial_IdNoPositivo_LanzaArgumentOutOfRangeException(int idMaterial)
        {
            Contenedor caneca = CrearCaneca();

            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => caneca.AdmitirMaterial(idMaterial));
            Assert.IsEmpty(caneca.MaterialesAdmitidos);
        }

        [TestMethod]
        public void AdmitirMaterial_MaterialYaAdmitido_LanzaInvalidOperationException()
        {
            Contenedor caneca = CrearCaneca();
            caneca.AdmitirMaterial(10);

            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.AdmitirMaterial(10));
            Assert.HasCount(1, caneca.MaterialesAdmitidos);
        }

        [TestMethod]
        public void QuitarMaterialAdmitido_FueraDeServicio_PuedeDejarlaSinMateriales()
        {
            Contenedor caneca = CrearCaneca();
            caneca.AdmitirMaterial(10);

            caneca.QuitarMaterialAdmitido(10);

            Assert.IsEmpty(caneca.MaterialesAdmitidos);
        }

        [TestMethod]
        public void QuitarMaterialAdmitido_EnServicioConDosMateriales_LoQuitaYSigueEnServicio()
        {
            Contenedor caneca = CrearCanecaEnServicio(10, 11);

            caneca.QuitarMaterialAdmitido(10);

            Assert.IsFalse(caneca.AdmiteMaterial(10));
            Assert.IsTrue(caneca.EstaEnServicio);
        }

        [TestMethod]
        public void QuitarMaterialAdmitido_UltimoMaterialEnServicio_LanzaInvalidOperationException()
        {
            Contenedor caneca = CrearCanecaEnServicio(10);

            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.QuitarMaterialAdmitido(10));
            Assert.IsTrue(caneca.AdmiteMaterial(10));
        }

        [TestMethod]
        public void QuitarMaterialAdmitido_MaterialNoAdmitido_LanzaInvalidOperationException()
        {
            Assert.ThrowsExactly<InvalidOperationException>(() => CrearCaneca().QuitarMaterialAdmitido(99));
        }

        [TestMethod]
        public void MaterialesAdmitidos_ColeccionExpuesta_NoPermiteModificarDesdeAfuera()
        {
            var lista = (ICollection<int>)CrearCaneca().MaterialesAdmitidos;

            Assert.ThrowsExactly<NotSupportedException>(() => lista.Add(10));
        }

        // ---------- estados (RN-05) ----------

        [TestMethod]
        public void PonerEnServicio_ConMaterialAdmitido_QuedaEnServicio()
        {
            Contenedor caneca = CrearCanecaEnServicio();

            Assert.AreEqual(EstadoContenedor.EnServicio, caneca.Estado);
            Assert.IsTrue(caneca.EstaEnServicio);
        }

        [TestMethod]
        public void PonerEnServicio_SinMateriales_LanzaInvalidOperationExceptionYSigueFuera()
        {
            Contenedor caneca = CrearCaneca();

            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.PonerEnServicio());
            Assert.AreEqual(EstadoContenedor.FueraDeServicio, caneca.Estado);
        }

        [TestMethod]
        public void PonerEnServicio_DespuesDeVaciarlaFueraDeServicio_LanzaInvalidOperationException()
        {
            // Recorrido completo del único camino que podría dejar una caneca en servicio sin materiales.
            Contenedor caneca = CrearCanecaEnServicio(10);
            caneca.PonerFueraDeServicio();
            caneca.QuitarMaterialAdmitido(10);

            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.PonerEnServicio());
            Assert.IsFalse(caneca.EstaEnServicio);
        }

        [TestMethod]
        public void PonerFueraDeServicio_EnServicio_DejaDeRecibirAperturas()
        {
            Contenedor caneca = CrearCanecaEnServicio();

            caneca.PonerFueraDeServicio();

            Assert.IsFalse(caneca.EstaEnServicio);
            Assert.IsTrue(caneca.AdmiteMaterial(10), "Ponerla fuera de servicio no borra su configuración.");
        }

        [TestMethod]
        public void DarDeBaja_EsDefinitivoYBloqueaCualquierCambio()
        {
            Contenedor caneca = CrearCanecaEnServicio();

            caneca.DarDeBaja();
            caneca.DarDeBaja();

            Assert.AreEqual(EstadoContenedor.DadoDeBaja, caneca.Estado);
            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.PonerEnServicio());
            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.PonerFueraDeServicio());
            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.AdmitirMaterial(11));
            Assert.ThrowsExactly<InvalidOperationException>(() => caneca.QuitarMaterialAdmitido(10));
            Assert.AreEqual(EstadoContenedor.DadoDeBaja, caneca.Estado);
        }

        [TestMethod]
        public void EstadoContenedor_ValoresNumericos_SonLosAprobados()
        {
            Assert.AreEqual(1, (int)Compat.ParseEnum<EstadoContenedor>("EnServicio"));
            Assert.AreEqual(2, (int)Compat.ParseEnum<EstadoContenedor>("FueraDeServicio"));
            Assert.AreEqual(3, (int)Compat.ParseEnum<EstadoContenedor>("DadoDeBaja"));
            Assert.HasCount(3, Compat.GetNames<EstadoContenedor>());
        }
    }
}
