using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BLL.Manejadores;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PdN6 — Pruebas del patrón Chain of Responsibility para cobro de suscripción.
    /// Misma estructura que RenovacionTests (PdN5): la cadena no valida que el sucesor
    /// exista — si un eslabón delega y no tiene sucesor asignado, es responsabilidad de
    /// quien arma la cadena (BLL.Cobro) haberla dejado bien formada. Para probar la
    /// delegación en aislamiento se usa un manejador espía como sucesor.
    /// </summary>
    [TestClass]
    public class CobroTests
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

        private static BE.Cliente ClienteVencido() => new BE.Cliente
        {
            IdCliente = 1,
            Nombre = "Ana",
            Apellido = "Gómez",
            IdPlan = 1,
            NombrePlan = "Básico",
            LimitePrendas = 3,
            PrecioPlan = 5000m,
            FechaVencimiento = DateTime.Today.AddDays(-1)
        };

        private static BE.Cliente ClienteVigente() => new BE.Cliente
        {
            IdCliente = 2,
            Nombre = "Luis",
            Apellido = "Pérez",
            IdPlan = 1,
            NombrePlan = "Básico",
            LimitePrendas = 3,
            PrecioPlan = 5000m,
            FechaVencimiento = DateTime.Today.AddDays(60)
        };

        private static BE.Cliente ClienteEnGracia(int diasRestantes) => new BE.Cliente
        {
            IdCliente = 3,
            Nombre = "Marta",
            Apellido = "Ruiz",
            IdPlan = 1,
            NombrePlan = "Básico",
            LimitePrendas = 3,
            PrecioPlan = 5000m,
            FechaVencimiento = DateTime.Today.AddDays(-3),
            FechaLimiteGracia = DateTime.Today.AddDays(diasRestantes)
        };

        /// <summary>Manejador espía: siempre "atiende" y registra que fue invocado.</summary>
        private sealed class ManejadorEspia : ManejadorCobro
        {
            public bool Invocado { get; private set; }

            public override ResultadoCobro Procesar(ContextoCobro contexto)
            {
                Invocado = true;
                return new ResultadoCobro { Resuelto = true, Estado = BE.EstadoCobro.Pendiente, Mensaje = "espía" };
            }
        }

        [TestMethod]
        public void DetectarCobro_ClienteVigente_QuedaPendiente()
        {
            var handler = new DetectarCobroHandler();
            var resultado = handler.Procesar(new ContextoCobro
            {
                Cliente = ClienteVigente(),
                Decision = DecisionCobro.Cobrado
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoCobro.Pendiente, resultado.Estado);
        }

        [TestMethod]
        public void DetectarCobro_ClienteVencido_DelegaAlSucesor()
        {
            var handler = new DetectarCobroHandler();
            var espia = new ManejadorEspia();
            handler.AgregarSiguiente(espia);

            handler.Procesar(new ContextoCobro { Cliente = ClienteVencido(), Decision = DecisionCobro.Cobrado });

            Assert.IsTrue(espia.Invocado);
        }

        [TestMethod]
        public void ProcesarPago_Cobrado_ExtiendeVencimientoYLimpiaGracia()
        {
            var dalCliente = new FakeClienteDAL();
            var dalCobro = new FakeCobroDAL();
            var handler = new ProcesarPagoHandler(dalCliente, dalCobro, new FakeCargoPrendaDAL());
            var cliente = ClienteEnGracia(2); // veníamos de un ciclo con gracia activa

            var resultado = handler.Procesar(new ContextoCobro
            {
                Cliente = cliente,
                Decision = DecisionCobro.Cobrado,
                Modalidad = BE.Builders.ModalidadCobro.Mensual,
                Actor = "vendedor1"
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoCobro.Cobrado, resultado.Estado);
            Assert.AreEqual(DateTime.Today.AddMonths(1), cliente.FechaVencimiento);
            Assert.IsNull(cliente.FechaLimiteGracia);
            Assert.AreEqual(1, dalCliente.ModificarVeces);
            Assert.AreEqual(1, dalCobro.AltaVeces);

            var registro = dalCobro.Registros[0];
            Assert.AreEqual(BE.EstadoCobro.Cobrado, registro.Resultado);
            Assert.AreEqual(5000m, registro.Importe);
            Assert.IsTrue(registro.FechaResolucion.HasValue);
        }

        [TestMethod]
        public void ProcesarPago_ConDescuentoPendiente_LoRestaDelImporteYLoConsume()
        {
            var dalCliente = new FakeClienteDAL();
            var dalCobro = new FakeCobroDAL();
            var dalCargo = new FakeCargoPrendaDAL();
            var handler = new ProcesarPagoHandler(dalCliente, dalCobro, dalCargo);
            var cliente = ClienteVigente();
            cliente.DescuentoProximoCobro = 1000m;

            var resultado = handler.Procesar(new ContextoCobro
            {
                Cliente = cliente,
                Decision = DecisionCobro.Cobrado,
                Modalidad = BE.Builders.ModalidadCobro.Mensual,
                Actor = "vendedor1"
            });

            Assert.AreEqual(BE.EstadoCobro.Cobrado, resultado.Estado);
            Assert.AreEqual(0m, cliente.DescuentoProximoCobro, "El descuento se consume en este cobro.");
            Assert.AreEqual(4000m, dalCobro.Registros[0].Importe); // 5000 (PrecioPlan) - 1000
        }

        // ── PN03: las promociones vigentes se aplican al cobro (un solo descuento por ciclo) ─────

        private static BE.Promocion PromoVigenteDelPlan(BE.TipoDescuento tipo, decimal valor, int idPlan) => new BE.Promocion
        {
            IdPromocion = 1, Nombre = "Verano", TipoDescuento = tipo, Valor = valor, IdPlan = idPlan,
            Estado = BE.EstadoPromocion.Vigente,
            FechaInicio = DateTime.Today.AddDays(-5), FechaFin = DateTime.Today.AddDays(30)
        };

        [TestMethod]
        public void ProcesarPago_ConPromocionVigenteDelPlan_LaAplicaYNoConsumeElCreditoPorReferido()
        {
            var dalCobro = new FakeCobroDAL();
            var dalPromo = new FakePromocionDAL();
            var cliente = ClienteVigente();
            cliente.DescuentoProximoCobro = 500m;
            dalPromo.Todas.Add(PromoVigenteDelPlan(BE.TipoDescuento.Porcentaje, 10, cliente.IdPlan.Value));
            var handler = new ProcesarPagoHandler(new FakeClienteDAL(), dalCobro, new FakeCargoPrendaDAL(), dalPromo);

            var resultado = handler.Procesar(new ContextoCobro
            {
                Cliente = cliente, Decision = DecisionCobro.Cobrado,
                Modalidad = BE.Builders.ModalidadCobro.Mensual, Actor = "vendedor1"
            });

            Assert.AreEqual(4500m, dalCobro.Registros[0].Importe); // 5000 - 10% (500): un solo descuento
            Assert.AreEqual(500m, cliente.DescuentoProximoCobro, "El crédito por referido queda acumulado para el próximo ciclo.");
            Assert.AreEqual("cobro.msg.cobrado.promo", resultado.Clave);
        }

        [TestMethod]
        public void ProcesarPago_CreditoPorReferidoMayorQueLaPromocion_ConsumeElCreditoYNoAplicaLaPromocion()
        {
            var dalCobro = new FakeCobroDAL();
            var dalPromo = new FakePromocionDAL();
            var cliente = ClienteVigente();
            cliente.DescuentoProximoCobro = 2000m;
            dalPromo.Todas.Add(PromoVigenteDelPlan(BE.TipoDescuento.MontoFijo, 300, cliente.IdPlan.Value));
            var handler = new ProcesarPagoHandler(new FakeClienteDAL(), dalCobro, new FakeCargoPrendaDAL(), dalPromo);

            var resultado = handler.Procesar(new ContextoCobro
            {
                Cliente = cliente, Decision = DecisionCobro.Cobrado,
                Modalidad = BE.Builders.ModalidadCobro.Mensual, Actor = "vendedor1"
            });

            Assert.AreEqual(3000m, dalCobro.Registros[0].Importe); // 5000 - 2000 (no se suman los dos)
            Assert.AreEqual(0m, cliente.DescuentoProximoCobro);
            Assert.AreEqual("cobro.msg.cobrado.descuento", resultado.Clave);
        }

        [TestMethod]
        public void ProcesarPago_PromocionDeOtroPlan_NoSeAplica()
        {
            var dalCobro = new FakeCobroDAL();
            var dalPromo = new FakePromocionDAL();
            var cliente = ClienteVigente();
            dalPromo.Todas.Add(PromoVigenteDelPlan(BE.TipoDescuento.Porcentaje, 50, cliente.IdPlan.Value + 99));
            var handler = new ProcesarPagoHandler(new FakeClienteDAL(), dalCobro, new FakeCargoPrendaDAL(), dalPromo);

            handler.Procesar(new ContextoCobro
            {
                Cliente = cliente, Decision = DecisionCobro.Cobrado,
                Modalidad = BE.Builders.ModalidadCobro.Mensual, Actor = "vendedor1"
            });

            Assert.AreEqual(5000m, dalCobro.Registros[0].Importe);
        }

        [TestMethod]
        public void ProcesarPago_ConCargosPendientes_LosSumaAlImporteYLosMarcaCobrados()
        {
            var dalCliente = new FakeClienteDAL();
            var dalCobro = new FakeCobroDAL();
            var dalCargo = new FakeCargoPrendaDAL();
            dalCargo.Alta(new BE.CargoPrenda { IdPrenda = 1, IdCliente = 2, Motivo = "Rotura", Monto = 500m });
            dalCargo.Alta(new BE.CargoPrenda { IdPrenda = 2, IdCliente = 2, Motivo = "Pérdida", Monto = 300m });
            var handler = new ProcesarPagoHandler(dalCliente, dalCobro, dalCargo);
            var cliente = ClienteVigente(); // IdCliente = 2

            var resultado = handler.Procesar(new ContextoCobro
            {
                Cliente = cliente,
                Decision = DecisionCobro.Cobrado,
                Modalidad = BE.Builders.ModalidadCobro.Mensual,
                Actor = "vendedor1"
            });

            Assert.AreEqual(BE.EstadoCobro.Cobrado, resultado.Estado);
            Assert.AreEqual(5800m, dalCobro.Registros[0].Importe); // 5000 (PrecioPlan) + 500 + 300
            Assert.AreEqual(1, dalCargo.MarcarCobradosVeces);
            Assert.AreEqual(0, dalCargo.ObtenerPendientesPorCliente(2).Count, "Los cargos deben quedar marcados como cobrados.");
        }

        [TestMethod]
        public void ProcesarPago_DecisionDistinta_DelegaAlSucesor()
        {
            var handler = new ProcesarPagoHandler(new FakeClienteDAL(), new FakeCobroDAL(), new FakeCargoPrendaDAL());
            var espia = new ManejadorEspia();
            handler.AgregarSiguiente(espia);

            handler.Procesar(new ContextoCobro { Cliente = ClienteVencido(), Decision = DecisionCobro.PagoFallido });

            Assert.IsTrue(espia.Invocado);
        }

        [TestMethod]
        public void AplicarGracia_PrimerPagoFallido_AbreElPeriodoDeGracia()
        {
            var dalCliente = new FakeClienteDAL();
            var dalCobro = new FakeCobroDAL();
            var handler = new AplicarGraciaHandler(dalCliente, dalCobro);
            var cliente = ClienteVencido(); // FechaLimiteGracia null: todavía al día

            var resultado = handler.Procesar(new ContextoCobro { Cliente = cliente, Decision = DecisionCobro.PagoFallido });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoCobro.Gracia, resultado.Estado);
            Assert.AreEqual(DateTime.Today.AddDays(AplicarGraciaHandler.DiasDeGracia), cliente.FechaLimiteGracia);
            Assert.AreEqual(1, dalCliente.ModificarVeces);
            Assert.AreEqual(1, dalCobro.AltaVeces);
        }

        [TestMethod]
        public void AplicarGracia_YaEnGracia_NoReiniciaElPlazo()
        {
            var dalCliente = new FakeClienteDAL();
            var dalCobro = new FakeCobroDAL();
            var handler = new AplicarGraciaHandler(dalCliente, dalCobro);
            var cliente = ClienteEnGracia(2); // ya tenía un plazo vigente

            var fechaOriginal = cliente.FechaLimiteGracia;
            var resultado = handler.Procesar(new ContextoCobro { Cliente = cliente, Decision = DecisionCobro.PagoFallido });

            Assert.AreEqual(BE.EstadoCobro.Gracia, resultado.Estado);
            Assert.AreEqual(fechaOriginal, cliente.FechaLimiteGracia); // no se movió el plazo
            Assert.AreEqual(0, dalCliente.ModificarVeces); // no hizo falta persistir de nuevo
        }

        [TestMethod]
        public void AplicarGracia_PlazoVencido_DelegaASuspender()
        {
            var handler = new AplicarGraciaHandler(new FakeClienteDAL(), new FakeCobroDAL());
            var espia = new ManejadorEspia();
            handler.AgregarSiguiente(espia);
            var cliente = ClienteEnGracia(-1); // el plazo ya venció

            handler.Procesar(new ContextoCobro { Cliente = cliente, Decision = DecisionCobro.PagoFallido });

            Assert.IsTrue(espia.Invocado);
        }

        [TestMethod]
        public void Suspender_SiempreResuelve_SinDelegar()
        {
            var dalCobro = new FakeCobroDAL();
            var handler = new SuspenderHandler(dalCobro);
            var cliente = ClienteEnGracia(-1);

            var resultado = handler.Procesar(new ContextoCobro { Cliente = cliente, Decision = DecisionCobro.PagoFallido, Actor = "vendedor1" });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoCobro.Suspendido, resultado.Estado);
            Assert.AreEqual(1, dalCobro.AltaVeces);
        }

        [TestMethod]
        public void Cadena_Completa_ClienteVencidoConPagoFallidoYaSuspendido_TerminaEnSuspendido()
        {
            var dalCliente = new FakeClienteDAL();
            var dalCobro = new FakeCobroDAL();
            var detectar = new DetectarCobroHandler();
            var procesar = new ProcesarPagoHandler(dalCliente, dalCobro, new FakeCargoPrendaDAL());
            var gracia = new AplicarGraciaHandler(dalCliente, dalCobro);
            var suspender = new SuspenderHandler(dalCobro);

            gracia.AgregarSiguiente(suspender);
            procesar.AgregarSiguiente(gracia);
            detectar.AgregarSiguiente(procesar);

            var cliente = ClienteEnGracia(-1); // vencido, en gracia, pero el plazo ya pasó

            var resultado = detectar.Procesar(new ContextoCobro { Cliente = cliente, Decision = DecisionCobro.PagoFallido });

            Assert.AreEqual(BE.EstadoCobro.Suspendido, resultado.Estado);
        }

        [TestMethod]
        public void Cadena_Completa_ClienteVencidoConCobroExitoso_ConfirmaRenovacion()
        {
            var dalCliente = new FakeClienteDAL();
            var dalCobro = new FakeCobroDAL();
            var detectar = new DetectarCobroHandler();
            var procesar = new ProcesarPagoHandler(dalCliente, dalCobro, new FakeCargoPrendaDAL());
            var gracia = new AplicarGraciaHandler(dalCliente, dalCobro);
            var suspender = new SuspenderHandler(dalCobro);

            gracia.AgregarSiguiente(suspender);
            procesar.AgregarSiguiente(gracia);
            detectar.AgregarSiguiente(procesar);

            var resultado = detectar.Procesar(new ContextoCobro
            {
                Cliente = ClienteVencido(),
                Decision = DecisionCobro.Cobrado,
                Modalidad = BE.Builders.ModalidadCobro.Anual
            });

            Assert.AreEqual(BE.EstadoCobro.Cobrado, resultado.Estado);
        }

        // ── BLL.Cobro (fachada real) ──────────────────────────────────────────────
        // Hasta acá todos los tests de este archivo arman su PROPIA copia de la cadena a mano
        // (mismo orden que el constructor de BLL.Cobro, pero reconstruido en el test) — si
        // alguien invierte el orden real en el constructor de producción, ningún test de los de
        // arriba lo detecta. Estos instancian la clase fachada REAL y ejercitan Procesar(...) de
        // punta a punta, más el guard de entrada (permisos + sin_plan) que tampoco tenía cobertura.

        [TestMethod]
        public void Real_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var bll = new BLL.Cobro(new FakeClienteDAL(), new FakeCobroDAL(), new FakeCargoPrendaDAL());

            try
            {
                bll.Procesar("Test", ClienteVencido(), DecisionCobro.Cobrado, BE.Builders.ModalidadCobro.Mensual, "vendedor1");
                Assert.Fail("Debía exigir sesión iniciada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
            }
        }

        [TestMethod]
        public void Real_ClienteSinPlan_LanzaSinPlan()
        {
            LoginComoAdministrador();
            var bll = new BLL.Cobro(new FakeClienteDAL(), new FakeCobroDAL(), new FakeCargoPrendaDAL());
            var cliente = ClienteVencido();
            cliente.IdPlan = null;
            cliente.NombrePlan = null;

            try
            {
                bll.Procesar("Test", cliente, DecisionCobro.Cobrado, BE.Builders.ModalidadCobro.Mensual, "vendedor1");
                Assert.Fail("Debía rechazar un cliente sin plan asignado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cobro.sin_plan", ex.Clave);
            }
        }

        [TestMethod]
        public void Real_ClienteVencidoConCobroExitoso_ProcesaDePuntaAPuntaConLaCadenaReal()
        {
            LoginComoAdministrador();
            var dalCobro = new FakeCobroDAL();
            var bll = new BLL.Cobro(new FakeClienteDAL(), dalCobro, new FakeCargoPrendaDAL());

            var resultado = bll.Procesar("Test", ClienteVencido(), DecisionCobro.Cobrado, BE.Builders.ModalidadCobro.Anual, "vendedor1");

            Assert.AreEqual(BE.EstadoCobro.Cobrado, resultado.Estado);
            Assert.AreEqual(1, dalCobro.AltaVeces);
        }

        [TestMethod]
        public void Real_ClienteVencidoConPagoFallidoYaSuspendido_ProcesaDePuntaAPuntaConLaCadenaReal()
        {
            LoginComoAdministrador();
            var dalCobro = new FakeCobroDAL();
            var bll = new BLL.Cobro(new FakeClienteDAL(), dalCobro, new FakeCargoPrendaDAL());

            var resultado = bll.Procesar("Test", ClienteEnGracia(-1), DecisionCobro.PagoFallido, BE.Builders.ModalidadCobro.Mensual, "vendedor1");

            Assert.AreEqual(BE.EstadoCobro.Suspendido, resultado.Estado);
        }
    }
}
