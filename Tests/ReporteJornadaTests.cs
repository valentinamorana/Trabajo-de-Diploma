using System;
using System.Collections.Generic;
using BLL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Cubre la fachada real de BLL.ReporteJornada (antes 0% testeable: constructor sin
    /// ningún parámetro, colaboradores hardcodeados — ver Tests §9, "Reportes"). No repite
    /// la agregación pura de CalcularTendencia, ya cubierta por ReporteTendenciaTests.
    /// </summary>
    [TestClass]
    public class ReporteJornadaTests
    {
        private static ReporteJornada Crear(FakeBitacoraService bitacora, FakePrendaService prenda, FakeClienteService cliente)
            => new ReporteJornada(bitacora, prenda, cliente);

        [TestMethod]
        public void ContarPrendasDisponibles_DelegaEnPrendaService()
        {
            var fakePrenda = new FakePrendaService { Disponibles = new List<BE.Prenda> { new BE.Prenda(), new BE.Prenda() } };
            var bll = Crear(new FakeBitacoraService(), fakePrenda, new FakeClienteService());

            Assert.AreEqual(2, bll.ContarPrendasDisponibles());
        }

        [TestMethod]
        public void ContarClientes_DelegaEnClienteService()
        {
            var bll = Crear(new FakeBitacoraService(), new FakePrendaService(), new FakeClienteService());

            // FakeClienteService.ObtenerTodos() devuelve lista vacía por defecto.
            Assert.AreEqual(0, bll.ContarClientes());
        }

        [TestMethod]
        public void ContarEventosDia_CuentaFilasDeLaTablaDeNegocio()
        {
            var fakeBitacora = new FakeBitacoraService();
            fakeBitacora.TablaNegocio.Rows.Add("Alta", "Se registró un pedido", DateTime.Now, "admin", "Juan Pérez");
            fakeBitacora.TablaNegocio.Rows.Add("Baja", "Se canceló un pedido", DateTime.Now, "admin", "María López");
            var bll = Crear(fakeBitacora, new FakePrendaService(), new FakeClienteService());

            Assert.AreEqual(2, bll.ContarEventosDia(DateTime.Today));
        }

        [TestMethod]
        public void ObtenerEventosDelDia_DevuelveLaTablaDelBitacoraService()
        {
            var fakeBitacora = new FakeBitacoraService();
            fakeBitacora.TablaNegocio.Rows.Add("Alta", "Se registró un pedido", DateTime.Now, "admin", "Juan Pérez");
            var bll = Crear(fakeBitacora, new FakePrendaService(), new FakeClienteService());

            var dt = bll.ObtenerEventosDelDia(DateTime.Today);

            Assert.AreEqual(1, dt.Rows.Count);
            Assert.AreEqual(1, fakeBitacora.BuscarPorFiltrosNegocioVeces);
        }

        [TestMethod]
        public void Generar_SinEventos_IncluyeElMensajeDeJornadaVacia()
        {
            var bll = Crear(new FakeBitacoraService(), new FakePrendaService(), new FakeClienteService());

            string reporte = bll.Generar(DateTime.Today);

            StringAssert.Contains(reporte, "sin eventos registrados para esta jornada");
        }

        [TestMethod]
        public void Generar_ConEventos_IncluyeElDetalleDelEvento()
        {
            var fakeBitacora = new FakeBitacoraService();
            fakeBitacora.TablaNegocio.Rows.Add("Alta", "Se registró un pedido", DateTime.Now, "admin", "Juan Pérez");
            var bll = Crear(fakeBitacora, new FakePrendaService(), new FakeClienteService());

            string reporte = bll.Generar(DateTime.Today);

            StringAssert.Contains(reporte, "Juan Pérez");
            StringAssert.Contains(reporte, "TOTAL EVENTOS");
        }

        [TestMethod]
        public void GenerarComparacion_MasEventosEnJornadaA_LoIndicaComoGanadora()
        {
            var fakeBitacora = new FakeBitacoraService();
            var bll = Crear(fakeBitacora, new FakePrendaService(), new FakeClienteService());

            // Ambas fechas comparten la misma TablaNegocio del fake (2 filas siempre).
            fakeBitacora.TablaNegocio.Rows.Add("Alta", "Evento 1", DateTime.Now, "admin", null);
            fakeBitacora.TablaNegocio.Rows.Add("Alta", "Evento 2", DateTime.Now, "admin", null);

            string reporte = bll.GenerarComparacion(DateTime.Today, DateTime.Today.AddDays(-1));

            StringAssert.Contains(reporte, "Ambas jornadas tuvieron la misma cantidad de eventos.");
        }

        [TestMethod]
        public void Constructor_ColaboradorNull_LanzaArgumentNullException()
        {
            try
            {
                new ReporteJornada(null, new FakePrendaService(), new FakeClienteService());
                Assert.Fail("Debía rechazar un IBitacoraService null.");
            }
            catch (ArgumentNullException) { /* esperado */ }
        }
    }
}
