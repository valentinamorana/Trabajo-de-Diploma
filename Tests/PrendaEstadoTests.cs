using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>PdN2/PdN4 — Pruebas del patrón State para el ciclo de vida de Prenda.</summary>
    [TestClass]
    public class PrendaEstadoTests
    {
        [TestMethod]
        public void Disponible_PuedePasarAEnLimpieza()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.Disponible };
            Assert.IsTrue(p.TransicionPermitida(BE.EstadoPrenda.EnLimpieza));
        }

        [TestMethod]
        public void Disponible_PuedePasarABaja()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.Disponible };
            Assert.IsTrue(p.TransicionPermitida(BE.EstadoPrenda.Baja));
        }

        [TestMethod]
        public void Disponible_NoPuedePasarAEnUso()
        {
            // EnUso solo ocurre internamente vía Pedido, no como transición manual.
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.Disponible };
            Assert.IsFalse(p.TransicionPermitida(BE.EstadoPrenda.EnUso));
        }

        [TestMethod]
        public void EnLimpieza_PuedeVolverADisponible()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.EnLimpieza };
            Assert.IsTrue(p.TransicionPermitida(BE.EstadoPrenda.Disponible));
        }

        [TestMethod]
        public void EnLimpieza_PuedePasarABaja()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.EnLimpieza };
            Assert.IsTrue(p.TransicionPermitida(BE.EstadoPrenda.Baja));
        }

        [TestMethod]
        public void EnUso_NoAdmiteTransicionManual()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.EnUso };
            Assert.IsFalse(p.TransicionPermitida(BE.EstadoPrenda.Disponible));
            Assert.IsFalse(p.TransicionPermitida(BE.EstadoPrenda.EnLimpieza));
            Assert.IsFalse(p.TransicionPermitida(BE.EstadoPrenda.Baja));
        }

        [TestMethod]
        public void Baja_EsEstadoFinal()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.Baja };
            Assert.IsFalse(p.TransicionPermitida(BE.EstadoPrenda.Disponible));
            Assert.IsFalse(p.TransicionPermitida(BE.EstadoPrenda.EnLimpieza));
            Assert.IsFalse(p.TransicionPermitida(BE.EstadoPrenda.EnUso));
        }

        [TestMethod]
        public void TodosLosEstados_PermitenPermanecerEnSiMismos()
        {
            foreach (BE.EstadoPrenda estado in System.Enum.GetValues(typeof(BE.EstadoPrenda)))
            {
                var p = new BE.Prenda { Estado = estado };
                Assert.IsTrue(p.TransicionPermitida(estado), $"{estado} debería poder 'transicionar' a sí mismo.");
            }
        }

        // ── ControlarEstado: el propio Estado transiciona el Contexto (equivalente a
        // Switch.Presionar() → _estado.ControlarEstado(this) del ejemplo de cátedra) ──

        [TestMethod]
        public void ControlarEstado_TransicionValida_MutaElContexto()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.Disponible };

            bool aplicado = p.ControlarEstado(BE.EstadoPrenda.EnLimpieza);

            Assert.IsTrue(aplicado);
            Assert.AreEqual(BE.EstadoPrenda.EnLimpieza, p.Estado);
        }

        [TestMethod]
        public void ControlarEstado_TransicionInvalida_NoMutaElContexto()
        {
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.Baja };

            bool aplicado = p.ControlarEstado(BE.EstadoPrenda.Disponible);

            Assert.IsFalse(aplicado);
            Assert.AreEqual(BE.EstadoPrenda.Baja, p.Estado);
        }

        [TestMethod]
        public void EstadoDisponible_ControlarEstado_DelegaCorrectamente()
        {
            var estado = new BE.Estados.EstadoDisponible();
            var p = new BE.Prenda { Estado = BE.EstadoPrenda.Disponible };

            Assert.IsTrue(estado.ControlarEstado(p, BE.EstadoPrenda.Baja));
            Assert.AreEqual(BE.EstadoPrenda.Baja, p.Estado);

            var p2 = new BE.Prenda { Estado = BE.EstadoPrenda.Disponible };
            Assert.IsFalse(estado.ControlarEstado(p2, BE.EstadoPrenda.EnUso));
            Assert.AreEqual(BE.EstadoPrenda.Disponible, p2.Estado);
        }
    }
}
