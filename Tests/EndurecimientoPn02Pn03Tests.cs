using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Endurecimiento de PN02/PN03: claim atómico y compensación del cobro, promoción y crédito por
    /// referido aplicados al importe, reclamo atómico de la sugerencia, reformular una promoción
    /// rechazada y acreditación del beneficio por referido.
    /// </summary>
    [TestClass]
    public class EndurecimientoPn02Pn03Tests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static void LoginComoAdministrador()
        {
            SessionManager.Login(new BE.Usuario
            {
                Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!")
            });
        }

        // ── PN02: cobro con promoción / crédito por referido ─────────────────────────────

        private class CtxCobro
        {
            public FakeContratacionDAL DalContratacion = new FakeContratacionDAL();
            public BE.Cliente Cliente = new BE.Cliente { IdCliente = 10, Nombre = "Ana", Apellido = "Gómez" };
            public FakeClienteDAL DalCliente;
            public FakeEmpleadoDAL DalEmpleado = new FakeEmpleadoDAL { EmpleadoPorUsuario = new BE.Empleado { IdEmpleado = 5 } };
            public FakePlanSuscripcionDAL DalPlan = new FakePlanSuscripcionDAL
            {
                PlanPorId = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", LimitePrendas = 3, Precio = 10000m, Estado = true }
            };
            public FakePromocionDAL DalPromocion = new FakePromocionDAL();
            public FakeClienteService ClienteBLL = new FakeClienteService();

            public CtxCobro() { DalCliente = new FakeClienteDAL { ClientePorId = Cliente }; }

            public BLL.Contratacion Crear() => new BLL.Contratacion(
                DalContratacion, DalCliente, DalEmpleado, DalPlan, ClienteBLL, DalPromocion);

            public BE.Contratacion Pendiente()
            {
                var c = new BE.Contratacion
                {
                    IdContratacion = 7, IdCliente = 10, IdPlan = 1, IdVendedor = 5,
                    Modalidad = BE.Builders.ModalidadCobro.Mensual, Estado = BE.EstadoContratacion.PendientePago,
                    NombreCliente = "Ana Gómez", NombrePlan = "Básico"
                };
                DalContratacion.ContratacionPorId = c;
                return c;
            }

            public void AgregarPromocion(BE.TipoDescuento tipo, decimal valor) => DalPromocion.Todas.Add(new BE.Promocion
            {
                IdPromocion = 3, Nombre = "Verano", TipoDescuento = tipo, Valor = valor, IdPlan = 1,
                Estado = BE.EstadoPromocion.Vigente,
                FechaInicio = DateTime.Today.AddDays(-5), FechaFin = DateTime.Today.AddDays(30)
            });
        }

        [TestMethod]
        public void ConfirmarPago_ConPromocionVigenteDelPlan_CobraElImporteConDescuentoYGuardaLaPromocion()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.AgregarPromocion(BE.TipoDescuento.Porcentaje, 10);
            var contratacion = ctx.Pendiente();

            var liq = ctx.Crear().ConfirmarPago("Test", contratacion, "Efectivo");

            Assert.AreEqual(9000m, ctx.DalContratacion.UltimoImporte);
            Assert.AreEqual(1000m, ctx.DalContratacion.UltimoDescuento);
            Assert.AreEqual(3, ctx.DalContratacion.UltimaPromocion);
            Assert.AreEqual(9000m, liq.Total);
            Assert.AreEqual("Verano", liq.NombrePromocion);
            Assert.IsFalse(string.IsNullOrWhiteSpace(liq.NumeroComprobante));
        }

        [TestMethod]
        public void ConfirmarPago_SinPromocionNiCredito_CobraElPrecioDelPlanCompleto()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            var contratacion = ctx.Pendiente();

            ctx.Crear().ConfirmarPago("Test", contratacion, "Efectivo");

            Assert.AreEqual(10000m, ctx.DalContratacion.UltimoImporte);
            Assert.AreEqual(0m, ctx.DalContratacion.UltimoDescuento);
            Assert.IsNull(ctx.DalContratacion.UltimaPromocion);
        }

        [TestMethod]
        public void ConfirmarPago_CreditoPorReferidoMayorQueLaPromocion_ConsumeSoloLoAplicado()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.Cliente.DescuentoProximoCobro = 3000m;
            ctx.AgregarPromocion(BE.TipoDescuento.MontoFijo, 500);
            var contratacion = ctx.Pendiente();

            ctx.Crear().ConfirmarPago("Test", contratacion, "Efectivo");

            Assert.AreEqual(7000m, ctx.DalContratacion.UltimoImporte, "Un solo descuento: el crédito (3000), no la suma con la promo.");
            Assert.IsNull(ctx.DalContratacion.UltimaPromocion);
            Assert.AreEqual(0m, ctx.Cliente.DescuentoProximoCobro);
        }

        [TestMethod]
        public void ConfirmarPago_GanaLaPromocion_ElCreditoPorReferidoQuedaAcumulado()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.Cliente.DescuentoProximoCobro = 500m;
            ctx.AgregarPromocion(BE.TipoDescuento.Porcentaje, 10);
            var contratacion = ctx.Pendiente();

            ctx.Crear().ConfirmarPago("Test", contratacion, "Efectivo");

            Assert.AreEqual(9000m, ctx.DalContratacion.UltimoImporte);
            Assert.AreEqual(500m, ctx.Cliente.DescuentoProximoCobro, "No se consume el crédito si se aplicó la promoción.");
        }

        [TestMethod]
        public void ConfirmarPago_CreditoMayorAlPrecio_ElExcedenteQuedaAcumulado()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.Cliente.DescuentoProximoCobro = 12500m;   // el plan cuesta 10000
            var contratacion = ctx.Pendiente();

            ctx.Crear().ConfirmarPago("Test", contratacion, "Efectivo");

            Assert.AreEqual(0m, ctx.DalContratacion.UltimoImporte);
            Assert.AreEqual(2500m, ctx.Cliente.DescuentoProximoCobro, "Los descuentos no usados quedan acumulados.");
        }

        [TestMethod]
        public void CalcularImporte_MuestraElMismoDescuentoQueSeCobra_SinCobrar()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.AgregarPromocion(BE.TipoDescuento.MontoFijo, 2500);
            var contratacion = ctx.Pendiente();

            var liq = ctx.Crear().CalcularImporte(contratacion);

            Assert.AreEqual(10000m, liq.Bruto);
            Assert.AreEqual(2500m, liq.Descuento);
            Assert.AreEqual(7500m, liq.Total);
            Assert.AreEqual(0, ctx.DalContratacion.ConfirmarPagoVeces);
        }

        [TestMethod]
        public void ModalidadCobro_Meses_EsUnoTresYDoce()
        {
            Assert.AreEqual(1, BE.Builders.ModalidadCobroExtensiones.Meses(BE.Builders.ModalidadCobro.Mensual));
            Assert.AreEqual(3, BE.Builders.ModalidadCobroExtensiones.Meses(BE.Builders.ModalidadCobro.Trimestral));
            Assert.AreEqual(12, BE.Builders.ModalidadCobroExtensiones.Meses(BE.Builders.ModalidadCobro.Anual));
        }

        [TestMethod]
        public void CalcularImporte_Trimestral_CobraTresMesesDelPrecioMensual()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            var contratacion = ctx.Pendiente();
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Trimestral;

            var liq = ctx.Crear().CalcularImporte(contratacion);

            Assert.AreEqual(30000m, liq.Bruto);
            Assert.AreEqual(30000m, liq.Total);
        }

        [TestMethod]
        public void CalcularImporte_Anual_CobraDoceMesesYElDescuentoSeRestaUnaVez()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.AgregarPromocion(BE.TipoDescuento.MontoFijo, 2500);
            var contratacion = ctx.Pendiente();
            contratacion.Modalidad = BE.Builders.ModalidadCobro.Anual;

            var liq = ctx.Crear().CalcularImporte(contratacion);

            Assert.AreEqual(120000m, liq.Bruto);
            Assert.AreEqual(2500m, liq.Descuento);
            Assert.AreEqual(117500m, liq.Total);
        }
        [TestMethod]
        public void CalcularImportes_DevuelveUnaLiquidacionPorContratacion()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.AgregarPromocion(BE.TipoDescuento.Porcentaje, 20);
            var a = ctx.Pendiente();
            var b = new BE.Contratacion { IdContratacion = 8, IdCliente = 10, IdPlan = 1 };

            var r = ctx.Crear().CalcularImportes(new List<BE.Contratacion> { a, b });

            Assert.AreEqual(2, r.Count);
            Assert.AreEqual(8000m, r[7].Total);
            Assert.AreEqual(8000m, r[8].Total);
        }

        [TestMethod]
        public void ConfirmarPago_FallaLaActivacionYTampocoSePuedeReabrir_AvisaQueElCobroQuedoSinActivar()
        {
            LoginComoAdministrador();
            var ctx = new CtxCobro();
            ctx.ClienteBLL.ActivarSuscripcionLanza = new InvalidOperationException("boom");
            ctx.DalContratacion.ReabrirPagoLanza = new InvalidOperationException("indice unico");
            var contratacion = ctx.Pendiente();

            try
            {
                ctx.Crear().ConfirmarPago("Test", contratacion, "Efectivo");
                Assert.Fail("Debía avisar que el cobro quedó sin activar.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.contratacion.cobro_sin_activar", ex.Clave);
            }
            Assert.AreEqual(1, ctx.DalContratacion.ReabrirPagoVeces);
        }

        // ── PN03: sugerencias y promociones ──────────────────────────────────────────────

        private class CtxPromo
        {
            public FakePromocionDAL DalPromocion = new FakePromocionDAL();
            public FakeSugerenciaPromocionDAL DalSugerencia = new FakeSugerenciaPromocionDAL();
            public FakePlanSuscripcionDAL DalPlan = new FakePlanSuscripcionDAL
            {
                PlanPorId = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", Estado = true }
            };
            public BLL.Promocion Crear() => new BLL.Promocion(DalPromocion, DalSugerencia, DalPlan);
        }

        private static BE.SugerenciaPromocion Sugerencia(BE.EstadoSugerencia estado) => new BE.SugerenciaPromocion
        {
            IdSugerencia = 5, IdPlan = 1, Motivo = "m", TipoDescuentoSugerido = BE.TipoDescuento.Porcentaje,
            BeneficioEstimado = 100m, Estado = estado
        };

        private static BE.Promocion Promo(BE.EstadoPromocion estado) => new BE.Promocion
        {
            IdPromocion = 42, Nombre = "Promo", TipoDescuento = BE.TipoDescuento.Porcentaje, Valor = 10,
            FechaInicio = DateTime.Today, FechaFin = DateTime.Today.AddDays(30), Estado = estado, IdPlan = 1
        };

        private static void EsperarError(Action accion, string claveEsperada)
        {
            try { accion(); Assert.Fail("Debía lanzar " + claveEsperada); }
            catch (BE.AppException ex) { Assert.AreEqual(claveEsperada, ex.Clave); }
        }

        [TestMethod]
        public void CrearDesdeSugerencia_SugerenciaYaEvaluada_LanzaSugerenciaEvaluada_SinCrearNada()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Evaluada);

            EsperarError(() => ctx.Crear().CrearDesdeSugerencia("Test", 5, "P", "d", BE.TipoDescuento.Porcentaje, 10m,
                DateTime.Today, DateTime.Today.AddDays(5), 1m, "i"), "err.bll.promocion.sugerencia_evaluada");

            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void CrearDesdeSugerencia_OtraSesionGanoElReclamo_NoCreaLaPromocionDuplicada()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Pendiente);
            ctx.DalSugerencia.MarcarEvaluadaResultado = false;

            EsperarError(() => ctx.Crear().CrearDesdeSugerencia("Test", 5, "P", "d", BE.TipoDescuento.Porcentaje, 10m,
                DateTime.Today, DateTime.Today.AddDays(5), 1m, "i"), "err.bll.promocion.sugerencia_evaluada");

            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
            Assert.AreEqual(0, ctx.DalSugerencia.ReabrirEvaluacionVeces);
        }

        [TestMethod]
        public void CrearDesdeSugerencia_FallaLaValidacion_DevuelveLaSugerenciaAPendiente()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();
            ctx.DalSugerencia.SugerenciaPorId = Sugerencia(BE.EstadoSugerencia.Pendiente);

            // porcentaje mayor a 100: la creación falla DESPUÉS de reclamar la sugerencia
            EsperarError(() => ctx.Crear().CrearDesdeSugerencia("Test", 5, "P", "d", BE.TipoDescuento.Porcentaje, 150m,
                DateTime.Today, DateTime.Today.AddDays(5), 1m, "i"), "err.bll.promocion.porcentaje_invalido");

            Assert.AreEqual(1, ctx.DalSugerencia.MarcarEvaluadaVeces);
            Assert.AreEqual(1, ctx.DalSugerencia.ReabrirEvaluacionVeces, "La sugerencia debe poder reintentarse.");
            Assert.AreEqual(0, ctx.DalPromocion.AltaVeces);
        }

        [TestMethod]
        public void AprobarContable_OtraSesionYaCambioElEstado_LanzaEstadoConcurrente()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();
            ctx.DalPromocion.CambiarEstadoResultado = false;

            EsperarError(() => ctx.Crear().AprobarContable("Test", Promo(BE.EstadoPromocion.EnRevisionContable), "ok"),
                "err.bll.promocion.estado_concurrente");
        }

        [TestMethod]
        public void SugerirBaja_OtraSesionYaCambioElEstado_LanzaEstadoConcurrente()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();
            ctx.DalPromocion.CambiarEstadoResultado = false;

            EsperarError(() => ctx.Crear().SugerirBaja("Test", Promo(BE.EstadoPromocion.Vigente), "motivo"),
                "err.bll.promocion.estado_concurrente");
        }

        [TestMethod]
        public void Reformular_PromocionRechazadaPorContabilidad_VuelveARevisionContableConLasCondicionesNuevas()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();
            var promo = Promo(BE.EstadoPromocion.RechazadaContabilidad);
            promo.Valor = 15;

            ctx.Crear().Reformular("Test", promo);

            Assert.AreEqual(1, ctx.DalPromocion.CambiarEstadoVeces);
            Assert.AreEqual(BE.EstadoPromocion.RechazadaContabilidad, ctx.DalPromocion.UltimoEstadoEsperado);
            Assert.AreEqual(BE.EstadoPromocion.EnRevisionContable, ctx.DalPromocion.UltimoNuevoEstado);
            Assert.AreEqual(1, ctx.DalPromocion.ModificarVeces);
            Assert.AreEqual(15m, ctx.DalPromocion.UltimoModificar.Valor);
        }

        [TestMethod]
        public void Reformular_PromocionQueNoFueRechazada_LanzaReformularEstado()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();

            EsperarError(() => ctx.Crear().Reformular("Test", Promo(BE.EstadoPromocion.Vigente)),
                "err.bll.promocion.reformular_estado");
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
        }

        [TestMethod]
        public void Reformular_ConDatosInvalidos_NoCambiaElEstado()
        {
            LoginComoAdministrador();
            var ctx = new CtxPromo();
            var promo = Promo(BE.EstadoPromocion.RechazadaContabilidad);
            promo.Valor = 150;   // porcentaje > 100

            EsperarError(() => ctx.Crear().Reformular("Test", promo), "err.bll.promocion.porcentaje_invalido");
            Assert.AreEqual(0, ctx.DalPromocion.CambiarEstadoVeces);
            Assert.AreEqual(0, ctx.DalPromocion.ModificarVeces);
        }

        // ── N01/PN02: acreditación del beneficio por referido al activar la suscripción ────

        private static BE.Cliente ClienteReferido(int? idReferente, bool otorgado) => new BE.Cliente
        {
            IdCliente = 20, Nombre = "Nuevo", Apellido = "Cliente", DNI = "30111222",
            FechaNacimiento = new DateTime(1990, 1, 1), IdClienteReferente = idReferente, BeneficioReferidoOtorgado = otorgado
        };

        private static BLL.Cliente BllCliente(FakeClienteDAL dal)
            => new BLL.Cliente(dal, new FakePlanSuscripcionDAL
            { PlanPorId = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", LimitePrendas = 3, Precio = 1000m, Estado = true } });

        [TestMethod]
        public void ActivarSuscripcion_ClienteReferido_AcreditaElBeneficioAlReferenteYLoMarcaOtorgado()
        {
            LoginComoAdministrador();
            var referente = new BE.Cliente { IdCliente = 5, Nombre = "Ref", Apellido = "Erente", DescuentoProximoCobro = 0m };
            var dal = new FakeClienteDAL { ClientePorId = referente };
            var nuevo = ClienteReferido(5, otorgado: false);

            BllCliente(dal).ActivarSuscripcion("Test", nuevo, 1, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(1000m, referente.DescuentoProximoCobro);
            Assert.IsTrue(nuevo.BeneficioReferidoOtorgado);
        }

        [TestMethod]
        public void ActivarSuscripcion_BeneficioYaOtorgado_NoLoDuplica()
        {
            LoginComoAdministrador();
            var referente = new BE.Cliente { IdCliente = 5, Nombre = "Ref", Apellido = "Erente", DescuentoProximoCobro = 1000m };
            var dal = new FakeClienteDAL { ClientePorId = referente };

            BllCliente(dal).ActivarSuscripcion("Test", ClienteReferido(5, otorgado: true), 1, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(1000m, referente.DescuentoProximoCobro, "No se acredita dos veces.");
        }

        [TestMethod]
        public void ActivarSuscripcion_SinReferente_NoAcreditaNada()
        {
            LoginComoAdministrador();
            var otro = new BE.Cliente { IdCliente = 5, Nombre = "Otro", Apellido = "Cliente", DescuentoProximoCobro = 0m };
            var dal = new FakeClienteDAL { ClientePorId = otro };
            var nuevo = ClienteReferido(null, otorgado: false);

            BllCliente(dal).ActivarSuscripcion("Test", nuevo, 1, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(0m, otro.DescuentoProximoCobro);
            Assert.IsFalse(nuevo.BeneficioReferidoOtorgado);
        }
    }
}
