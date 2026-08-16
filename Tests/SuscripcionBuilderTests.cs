using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>PdN1 — Pruebas del patrón Builder para activar una suscripción.</summary>
    [TestClass]
    public class SuscripcionBuilderTests
    {
        private static BE.Cliente ClienteDePrueba() => new BE.Cliente
        {
            IdCliente = 1,
            Nombre = "Ana",
            Apellido = "Gómez"
        };

        private static BE.PlanSuscripcion PlanDePrueba() => new BE.PlanSuscripcion
        {
            IdPlan = 1,
            Nombre = "Básico",
            LimitePrendas = 3,
            Precio = 1000m
        };

        [TestMethod]
        public void Mensual_VenceEnUnMes()
        {
            var builder = new BE.Builders.SuscripcionMensualBuilder();
            var suscripcion = BE.Builders.DirectorSuscripcion.Construir(builder, ClienteDePrueba(), PlanDePrueba());

            Assert.AreEqual(BE.Builders.ModalidadCobro.Mensual, suscripcion.Modalidad);
            Assert.AreEqual(DateTime.Today.AddMonths(1), suscripcion.FechaVencimiento);
        }

        [TestMethod]
        public void Trimestral_VenceEnTresMeses()
        {
            var builder = new BE.Builders.SuscripcionTrimestralBuilder();
            var suscripcion = BE.Builders.DirectorSuscripcion.Construir(builder, ClienteDePrueba(), PlanDePrueba());

            Assert.AreEqual(BE.Builders.ModalidadCobro.Trimestral, suscripcion.Modalidad);
            Assert.AreEqual(DateTime.Today.AddMonths(3), suscripcion.FechaVencimiento);
        }

        [TestMethod]
        public void Anual_VenceEnDoceMeses()
        {
            var builder = new BE.Builders.SuscripcionAnualBuilder();
            var suscripcion = BE.Builders.DirectorSuscripcion.Construir(builder, ClienteDePrueba(), PlanDePrueba());

            Assert.AreEqual(BE.Builders.ModalidadCobro.Anual, suscripcion.Modalidad);
            Assert.AreEqual(DateTime.Today.AddMonths(12), suscripcion.FechaVencimiento);
        }

        [TestMethod]
        public void MismosPasosDistintoBuilder_ProducenResultadosDistintos()
        {
            var cliente = ClienteDePrueba();
            var plan = PlanDePrueba();

            var mensual = BE.Builders.DirectorSuscripcion.Construir(new BE.Builders.SuscripcionMensualBuilder(), cliente, plan);
            var anual   = BE.Builders.DirectorSuscripcion.Construir(new BE.Builders.SuscripcionAnualBuilder(), cliente, plan);

            Assert.AreNotEqual(mensual.FechaVencimiento, anual.FechaVencimiento);
            Assert.AreSame(cliente, mensual.Cliente);
            Assert.AreSame(plan, mensual.Plan);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Director_RechazaClienteNulo()
        {
            BE.Builders.DirectorSuscripcion.Construir(new BE.Builders.SuscripcionMensualBuilder(), null, PlanDePrueba());
        }
    }
}
