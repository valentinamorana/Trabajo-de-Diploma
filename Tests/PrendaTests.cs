using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// BLL.Prenda — cobertura puntual de VerificarDisponibilidad (PN01, split lógico del
    /// Depósito: antes era una relectura inline dentro de BLL.Pedido.ValidarDisponibilidadPrendas,
    /// ahora es un método público propio de BLL.Prenda). Se agregó DI de constructor (mismo
    /// criterio que BLL.Pedido/BLL.Cliente) específicamente para poder escribir estos tests.
    /// </summary>
    [TestClass]
    public class PrendaTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static void LoginComoAdministrador()
        {
            SessionManager.Login(new BE.Usuario
            {
                Id = 1,
                Username = "admin",
                Perfil = "Administrador",
                Contraseña = Encriptador.Hash("Admin1!")
            });
        }

        private class Contexto
        {
            public FakePrendaDAL DalPrenda = new FakePrendaDAL();
            public FakeMantenimientoPrendaDAL DalMantenimiento = new FakeMantenimientoPrendaDAL();

            public BLL.Prenda Crear() => new BLL.Prenda(DalPrenda, DalMantenimiento);
        }

        [TestMethod]
        public void VerificarDisponibilidad_TodasDisponibles_DevuelveDisponibleTrue()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.Disponible });
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1 } });

            Assert.IsTrue(disponible);
            Assert.AreEqual(0, noDisponibles.Count);
        }

        [TestMethod]
        public void VerificarDisponibilidad_PrendaEnUso_DevuelveDisponibleFalseConDetalle()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.EnUso });
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1 } });

            Assert.IsFalse(disponible);
            Assert.AreEqual(1, noDisponibles.Count);
            Assert.AreEqual("Remera", noDisponibles[0].Nombre);
        }

        [TestMethod]
        public void VerificarDisponibilidad_PrendaInexistente_DevuelveDisponibleFalse()
        {
            var ctx = new Contexto(); // Todas queda vacío: ObtenerPorId no encuentra nada
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 99 } });

            Assert.IsFalse(disponible);
            Assert.AreEqual(1, noDisponibles.Count);
        }

        [TestMethod]
        public void VerificarDisponibilidad_SeleccionMixta_DevuelveSoloLasNoDisponibles()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.Disponible });
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 2, Nombre = "Pantalón", Estado = BE.EstadoPrenda.EnUso });
            var bll = ctx.Crear();

            var (disponible, noDisponibles) = bll.VerificarDisponibilidad(
                new List<BE.Prenda> { new BE.Prenda { IdPrenda = 1 }, new BE.Prenda { IdPrenda = 2 } });

            Assert.IsFalse(disponible);
            Assert.AreEqual(1, noDisponibles.Count);
            Assert.AreEqual(2, noDisponibles[0].IdPrenda);
        }

        // ── ObtenerEnLimpieza (PN04, CU-DEP-01 Inspeccionar Devolución) ──────────

        [TestMethod]
        public void ObtenerEnLimpieza_FiltraSoloLasEnLimpieza()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Estado = BE.EstadoPrenda.EnLimpieza });
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 2, Estado = BE.EstadoPrenda.Disponible });
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 3, Estado = BE.EstadoPrenda.EnLimpieza });
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 4, Estado = BE.EstadoPrenda.EnUso });
            var bll = ctx.Crear();

            var enLimpieza = bll.ObtenerEnLimpieza();

            Assert.AreEqual(2, enLimpieza.Count);
            CollectionAssert.AreEquivalent(new[] { 1, 3 }, enLimpieza.ConvertAll(p => p.IdPrenda));
        }

        [TestMethod]
        public void ObtenerEnLimpieza_NingunaEnLimpieza_DevuelveVacio()
        {
            var ctx = new Contexto();
            ctx.DalPrenda.Todas.Add(new BE.Prenda { IdPrenda = 1, Estado = BE.EstadoPrenda.Disponible });
            var bll = ctx.Crear();

            Assert.AreEqual(0, bll.ObtenerEnLimpieza().Count);
        }

        // ── CambiarEstado — guard EnUso→Baja (CU-DEP-02, Reportar Prenda Perdida) ───────────
        // Antes esta restricción vivía únicamente en GUI/Prendas.cs (un `continue` en una lista
        // de opciones del diálogo genérico de cambio de estado) — cualquier código que llamara
        // CambiarEstado directo podía saltearla. Ahora la guarda vive en el propio BLL.

        [TestMethod]
        public void CambiarEstado_EnUsoABajaSinFlujoPerdida_LanzaBajaRequiereFlujoPerdida()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var prenda = new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.EnUso };

            try
            {
                bll.CambiarEstado("Test", prenda, BE.EstadoPrenda.Baja);
                Assert.Fail("Debía rechazar dar de baja directamente una prenda EnUso.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.prenda.baja_requiere_flujoperdida", ex.Clave);
            }
            Assert.AreEqual(0, ctx.DalPrenda.CambiarEstadoVeces);
            Assert.AreEqual(BE.EstadoPrenda.EnUso, prenda.Estado, "No debe mutar el estado en memoria si rechaza la transición.");
        }

        [TestMethod]
        public void CambiarEstado_EnUsoABajaViaFlujoPerdida_Permite()
        {
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var prenda = new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.EnUso };

            bll.CambiarEstado("Test", prenda, BE.EstadoPrenda.Baja, actor: "vendedor1", viaFlujoPerdida: true);

            Assert.AreEqual(1, ctx.DalPrenda.CambiarEstadoVeces);
            Assert.AreEqual(BE.EstadoPrenda.Baja, prenda.Estado);
        }

        [TestMethod]
        public void CambiarEstado_EnLimpiezaADisponible_NoRequiereFlujoPerdida()
        {
            // La guarda es específica de EnUso→Baja; otras transiciones válidas del patrón State
            // no deben verse afectadas por el nuevo parámetro (default false).
            LoginComoAdministrador();
            var ctx = new Contexto();
            var bll = ctx.Crear();
            var prenda = new BE.Prenda { IdPrenda = 1, Nombre = "Remera", Estado = BE.EstadoPrenda.EnLimpieza };

            bll.CambiarEstado("Test", prenda, BE.EstadoPrenda.Disponible);

            Assert.AreEqual(1, ctx.DalPrenda.CambiarEstadoVeces);
            Assert.AreEqual(BE.EstadoPrenda.Disponible, prenda.Estado);
        }
    }
}
