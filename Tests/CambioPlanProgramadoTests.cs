using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PN02, nodo a11 del diagrama de actividad: "Activar suscripción (al vencer la vigente, o desde hoy
    /// si es un plan más caro)". Pasar a un plan igual o más barato con el período vigente deja el plan
    /// nuevo PROGRAMADO: hasta el vencimiento sigue el plan actual (y su límite de prendas), y ese día lo
    /// aplica BLL.Cliente.AplicarCambiosDePlanProgramados. Antes el plan y el límite cambiaban el día del cobro.
    /// </summary>
    [TestClass]
    public class CambioPlanProgramadoTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static void LoginAdmin() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        private static BE.PlanSuscripcion Premium() => new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Premium", LimitePrendas = 6, Precio = 3000m, Estado = true };
        private static BE.PlanSuscripcion Basico()  => new BE.PlanSuscripcion { IdPlan = 2, Nombre = "Básico",  LimitePrendas = 3, Precio = 1000m, Estado = true };

        private static (BLL.Cliente Bll, FakeClienteDAL Dal, BE.Cliente Cliente) Contexto(int idPlanActual, DateTime vence)
        {
            var dal = new FakeClienteDAL();
            var cliente = new BE.Cliente
            {
                IdCliente = 1, Nombre = "Ana", Apellido = "Gómez", IdPlan = idPlanActual,
                NombrePlan = idPlanActual == 1 ? "Premium" : "Básico", LimitePrendas = idPlanActual == 1 ? 6 : 3,
                FechaVencimiento = vence
            };
            dal.ClientePorId = cliente;
            var planes = new FakePlanSuscripcionDAL { Planes = new List<BE.PlanSuscripcion> { Premium(), Basico() } };
            return (new BLL.Cliente(dal, planes), dal, cliente);
        }

        [TestMethod]
        public void PlanMasBaratoConPeriodoVigente_QuedaProgramadoParaElVencimiento()
        {
            LoginAdmin();
            var vence = DateTime.Today.AddDays(15);
            var (bll, _, cliente) = Contexto(1, vence);

            var s = bll.ActivarSuscripcionDesdeContratacion("Test", cliente, 2, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(1, cliente.IdPlan, "Hasta el vencimiento sigue el plan actual.");
            Assert.AreEqual(6, cliente.LimitePrendas);
            Assert.AreEqual(2, cliente.IdPlanSiguiente);
            Assert.AreEqual(vence, cliente.FechaCambioPlan);
            Assert.AreEqual(vence.AddMonths(1), s.FechaVencimiento, "El período del plan nuevo va a continuación.");
        }

        [TestMethod]
        public void PlanMasBaratoConPeriodoVencido_RigeDesdeYa()
        {
            LoginAdmin();
            var (bll, _, cliente) = Contexto(1, DateTime.Today.AddDays(-3));

            bll.ActivarSuscripcionDesdeContratacion("Test", cliente, 2, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(2, cliente.IdPlan);
            Assert.AreEqual(3, cliente.LimitePrendas);
            Assert.IsNull(cliente.IdPlanSiguiente);
        }

        [TestMethod]
        public void Upgrade_RigeDesdeHoy_YDescartaUnCambioProgramado()
        {
            LoginAdmin();
            var (bll, _, cliente) = Contexto(2, DateTime.Today.AddDays(15));
            cliente.IdPlanSiguiente = 2; cliente.FechaCambioPlan = DateTime.Today.AddDays(15);

            bll.ActivarSuscripcionDesdeContratacion("Test", cliente, 1, BE.Builders.ModalidadCobro.Mensual, iniciarHoy: true);

            Assert.AreEqual(1, cliente.IdPlan);
            Assert.IsNull(cliente.IdPlanSiguiente);
            Assert.IsNull(cliente.FechaCambioPlan);
        }

        [TestMethod]
        public void AplicarCambiosDePlanProgramados_SoloLosQueLlegaronASuFecha()
        {
            var (bll, dal, _) = Contexto(1, DateTime.Today.AddDays(30));
            var llego   = new BE.Cliente { IdCliente = 5, Nombre = "A", Apellido = "B", IdPlan = 1, IdPlanSiguiente = 2, FechaCambioPlan = DateTime.Today };
            var noLlego = new BE.Cliente { IdCliente = 6, Nombre = "C", Apellido = "D", IdPlan = 1, IdPlanSiguiente = 2, FechaCambioPlan = DateTime.Today.AddDays(1) };
            var sinCambio = new BE.Cliente { IdCliente = 7, Nombre = "E", Apellido = "F", IdPlan = 1 };
            dal.ClientesDevueltos = new List<BE.Cliente> { llego, noLlego, sinCambio };

            int aplicados = bll.AplicarCambiosDePlanProgramados(DateTime.Today);

            Assert.AreEqual(1, aplicados);
            Assert.AreEqual(2, llego.IdPlan);
            Assert.IsNull(llego.IdPlanSiguiente);
            Assert.IsNull(llego.FechaCambioPlan);
            Assert.AreEqual(1, noLlego.IdPlan, "Todavía no llegó la fecha.");
            Assert.AreEqual(1, dal.ModificarVeces);
        }
    }
}
