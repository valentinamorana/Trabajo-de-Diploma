using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BLL.Manejadores;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PdN5 — Pruebas del patrón Chain of Responsibility para renovación de suscripción.
    /// Igual que el ejemplo de cátedra, la cadena no valida que el sucesor exista: si un
    /// eslabón delega y no tiene sucesor asignado, es responsabilidad de quien arma la
    /// cadena (BLL.Renovacion) haberla dejado bien formada. Para probar la delegación en
    /// aislamiento se usa un manejador espía como sucesor.
    /// </summary>
    [TestClass]
    public class RenovacionTests
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
            FechaVencimiento = DateTime.Today.AddDays(60)
        };

        /// <summary>Manejador espía: siempre "atiende" y registra que fue invocado.</summary>
        private sealed class ManejadorEspia : ManejadorRenovacion
        {
            public bool Invocado { get; private set; }

            public override ResultadoRenovacion Procesar(ContextoRenovacion contexto)
            {
                Invocado = true;
                return new ResultadoRenovacion { Resuelto = true, Estado = BE.EstadoRenovacion.Pendiente, Mensaje = "espía" };
            }
        }

        [TestMethod]
        public void VerificarVencimiento_ClienteVigente_QuedaPendiente()
        {
            var handler = new VerificarVencimientoHandler();
            var resultado = handler.Procesar(new ContextoRenovacion
            {
                Cliente = ClienteVigente(),
                Decision = DecisionRenovacion.Renovar
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoRenovacion.Pendiente, resultado.Estado);
        }

        [TestMethod]
        public void VerificarVencimiento_ClienteVencido_DelegaAlSucesor()
        {
            var handler = new VerificarVencimientoHandler();
            var espia = new ManejadorEspia();
            handler.AgregarSiguiente(espia);

            handler.Procesar(new ContextoRenovacion { Cliente = ClienteVencido(), Decision = DecisionRenovacion.Renovar });

            Assert.IsTrue(espia.Invocado);
        }

        [TestMethod]
        public void IntentarRenovar_ClienteVencido_RenuevaYActualizaVencimiento()
        {
            var dalCliente = new FakeClienteDAL();
            var dalRenovacion = new FakeRenovacionDAL();
            var handler = new IntentarRenovarHandler(dalCliente, dalRenovacion);
            var cliente = ClienteVencido();

            var resultado = handler.Procesar(new ContextoRenovacion
            {
                Cliente = cliente,
                Decision = DecisionRenovacion.Renovar,
                Modalidad = BE.Builders.ModalidadCobro.Mensual,
                Actor = "vendedor1"
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoRenovacion.Renovada, resultado.Estado);
            Assert.AreEqual(DateTime.Today.AddMonths(1), cliente.FechaVencimiento);
            Assert.AreEqual(1, dalCliente.ModificarVeces);
            Assert.AreEqual(1, dalRenovacion.AltaVeces);

            // Alta() ya persiste el resultado final y FechaResolucion en el mismo INSERT.
            var registro = dalRenovacion.Registros[0];
            Assert.AreEqual(BE.EstadoRenovacion.Renovada, registro.Resultado);
            Assert.IsTrue(registro.FechaResolucion.HasValue);
        }

        [TestMethod]
        public void IntentarRenovar_DecisionDistinta_DelegaAlSucesor()
        {
            var handler = new IntentarRenovarHandler(new FakeClienteDAL(), new FakeRenovacionDAL());
            var espia = new ManejadorEspia();
            handler.AgregarSiguiente(espia);

            handler.Procesar(new ContextoRenovacion { Cliente = ClienteVencido(), Decision = DecisionRenovacion.Baja });

            Assert.IsTrue(espia.Invocado);
        }

        [TestMethod]
        public void Cadena_VerificarMasRenovar_ClienteVencidoConDecisionRenovar_Resuelve()
        {
            var dalCliente = new FakeClienteDAL();
            var dalRenovacion = new FakeRenovacionDAL();
            var verificar = new VerificarVencimientoHandler();
            var renovar = new IntentarRenovarHandler(dalCliente, dalRenovacion);
            verificar.AgregarSiguiente(renovar);

            var resultado = verificar.Procesar(new ContextoRenovacion
            {
                Cliente = ClienteVencido(),
                Decision = DecisionRenovacion.Renovar,
                Modalidad = BE.Builders.ModalidadCobro.Anual
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoRenovacion.Renovada, resultado.Estado);
        }

        [TestMethod]
        public void Cadena_VerificarMasRenovar_ClienteVigente_NoLlegaARenovar()
        {
            // El cliente vigente lo atiende Verificar (Pendiente); Renovar ni se ejercita.
            var dalCliente = new FakeClienteDAL();
            var verificar = new VerificarVencimientoHandler();
            var renovar = new IntentarRenovarHandler(dalCliente, new FakeRenovacionDAL());
            verificar.AgregarSiguiente(renovar);

            var resultado = verificar.Procesar(new ContextoRenovacion
            {
                Cliente = ClienteVigente(),
                Decision = DecisionRenovacion.Renovar
            });

            Assert.AreEqual(BE.EstadoRenovacion.Pendiente, resultado.Estado);
            Assert.AreEqual(0, dalCliente.ModificarVeces);
        }

        // ── PausarSuscripcionHandler (Bloque 1) ──────────────────────────────────

        [TestMethod]
        public void Pausar_MasDeTresMeses_LanzaTopeDeNuuly()
        {
            var dalCliente = new FakeClienteDAL();
            var handler = new PausarSuscripcionHandler(dalCliente, new FakeRenovacionDAL());
            try
            {
                handler.Procesar(new ContextoRenovacion
                {
                    Cliente = ClienteVigente(),
                    Decision = DecisionRenovacion.Pausar,
                    FechaPausaHasta = DateTime.Today.AddMonths(PausarSuscripcionHandler.MaxMesesPausa).AddDays(1)
                });
                Assert.Fail("La pausa no puede superar los 3 meses.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.pausa_excede_tope", ex.Clave);
            }
            Assert.AreEqual(0, dalCliente.ModificarVeces);
        }

        [TestMethod]
        public void Pausar_ExactamenteTresMeses_EsValida()
        {
            var handler = new PausarSuscripcionHandler(new FakeClienteDAL(), new FakeRenovacionDAL());
            var resultado = handler.Procesar(new ContextoRenovacion
            {
                Cliente = ClienteVigente(),
                Decision = DecisionRenovacion.Pausar,
                FechaPausaHasta = DateTime.Today.AddMonths(PausarSuscripcionHandler.MaxMesesPausa)
            });
            Assert.IsTrue(resultado.Resuelto);
        }

        [TestMethod]
        public void Pausar_ConPrendasEnUso_LanzaYNoPersiste()
        {
            var dalCliente = new FakeClienteDAL();
            var dalPrenda = new FakePrendaDAL { PorCliente = new System.Collections.Generic.List<BE.Prenda>
            {
                new BE.Prenda { IdPrenda = 1, Nombre = "Remera" }
            }};
            var handler = new PausarSuscripcionHandler(dalCliente, new FakeRenovacionDAL(), dalPrenda);
            try
            {
                handler.Procesar(new ContextoRenovacion
                {
                    Cliente = ClienteVigente(),
                    Decision = DecisionRenovacion.Pausar,
                    FechaPausaHasta = DateTime.Today.AddDays(10)
                });
                Assert.Fail("No se puede pausar con prendas pendientes de devolución.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.pausa_con_prendas", ex.Clave);
            }
            Assert.AreEqual(0, dalCliente.ModificarVeces);
        }
        [TestMethod]
        public void Pausar_ConFechaValida_PausaYPersisteHistorial_SinTocarVencimiento()
        {
            var dalCliente = new FakeClienteDAL();
            var dalRenovacion = new FakeRenovacionDAL();
            var handler = new PausarSuscripcionHandler(dalCliente, dalRenovacion);
            var cliente = ClienteVigente();
            var vencimientoOriginal = cliente.FechaVencimiento;
            var fechaHasta = DateTime.Today.AddDays(10);

            var resultado = handler.Procesar(new ContextoRenovacion
            {
                Cliente = cliente,
                Decision = DecisionRenovacion.Pausar,
                FechaPausaHasta = fechaHasta,
                Actor = "vendedor1"
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoRenovacion.Pausada, resultado.Estado);
            Assert.AreEqual(fechaHasta, cliente.FechaPausaHasta);
            Assert.AreEqual(vencimientoOriginal, cliente.FechaVencimiento, "Pausar no debe tocar el vencimiento.");
            Assert.AreEqual(1, dalCliente.ModificarVeces);
            Assert.AreEqual(1, dalRenovacion.AltaVeces);

            var registro = dalRenovacion.Registros[0];
            Assert.AreEqual(BE.EstadoRenovacion.Pausada, registro.Resultado);
        }

        [TestMethod]
        public void Pausar_SinFecha_LanzaPausaSinFecha_SinTocarElDAL()
        {
            var dalCliente = new FakeClienteDAL();
            var handler = new PausarSuscripcionHandler(dalCliente, new FakeRenovacionDAL());
            var cliente = ClienteVigente();

            try
            {
                handler.Procesar(new ContextoRenovacion { Cliente = cliente, Decision = DecisionRenovacion.Pausar });
                Assert.Fail("Debía exigir la fecha de reanudación.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.pausa_sin_fecha", ex.Clave);
            }
            Assert.AreEqual(0, dalCliente.ModificarVeces);
        }

        [TestMethod]
        public void Pausar_FechaPasada_LanzaPausaFechaPasada()
        {
            var handler = new PausarSuscripcionHandler(new FakeClienteDAL(), new FakeRenovacionDAL());
            var cliente = ClienteVigente();

            try
            {
                handler.Procesar(new ContextoRenovacion
                {
                    Cliente = cliente,
                    Decision = DecisionRenovacion.Pausar,
                    FechaPausaHasta = DateTime.Today.AddDays(-1)
                });
                Assert.Fail("Debía rechazar una fecha de reanudación pasada.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.pausa_fecha_pasada", ex.Clave);
            }
        }

        [TestMethod]
        public void Pausar_DecisionDistinta_DelegaAlSucesor()
        {
            var handler = new PausarSuscripcionHandler(new FakeClienteDAL(), new FakeRenovacionDAL());
            var espia = new ManejadorEspia();
            handler.AgregarSiguiente(espia);

            handler.Procesar(new ContextoRenovacion { Cliente = ClienteVencido(), Decision = DecisionRenovacion.Baja });

            Assert.IsTrue(espia.Invocado);
        }

        // ── CambioPlanHandler ─────────────────────────────────────────────────────
        // Antes tomaba DAL.PlanSuscripcion concreto en vez de IPlanSuscripcionDAL: no se podía
        // instanciar con un doble de prueba y quedaba sin ningún test, incluida la validación de
        // "plan insuficiente para el stock en uso" al cambiar de plan.

        private static BE.PlanSuscripcion PlanPremium(int limitePrendas = 10) => new BE.PlanSuscripcion
        {
            IdPlan = 2,
            Nombre = "Premium",
            LimitePrendas = limitePrendas,
            Precio = 2000,
            Estado = true
        };

        [TestMethod]
        public void CambioPlan_PlanSuficiente_CambiaYActualizaVencimiento()
        {
            var dalCliente = new FakeClienteDAL();
            var dalRenovacion = new FakeRenovacionDAL();
            var dalPlan = new FakePlanSuscripcionDAL { PlanPorId = PlanPremium() };
            var handler = new CambioPlanHandler(dalCliente, dalPlan, dalRenovacion);
            var cliente = ClienteVigente();
            cliente.StockUtilizado = 2;

            var resultado = handler.Procesar(new ContextoRenovacion
            {
                Cliente = cliente,
                Decision = DecisionRenovacion.CambiarPlan,
                IdPlanNuevo = 2,
                Modalidad = BE.Builders.ModalidadCobro.Mensual,
                Actor = "vendedor1"
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoRenovacion.CambioPlan, resultado.Estado);
            Assert.AreEqual(2, cliente.IdPlan);
            Assert.AreEqual("Premium", cliente.NombrePlan);
            Assert.AreEqual(DateTime.Today.AddMonths(1), cliente.FechaVencimiento);
            Assert.AreEqual(1, dalCliente.ModificarVeces);
            Assert.AreEqual(1, dalRenovacion.AltaVeces);
        }

        [TestMethod]
        public void CambioPlan_SinPlanNuevo_LanzaPlanRequerido()
        {
            var handler = new CambioPlanHandler(new FakeClienteDAL(), new FakePlanSuscripcionDAL(), new FakeRenovacionDAL());

            try
            {
                handler.Procesar(new ContextoRenovacion { Cliente = ClienteVigente(), Decision = DecisionRenovacion.CambiarPlan });
                Assert.Fail("Debía exigir el plan nuevo.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.plan_requerido", ex.Clave);
            }
        }

        [TestMethod]
        public void CambioPlan_PlanInexistente_LanzaPlanInexistente()
        {
            var handler = new CambioPlanHandler(new FakeClienteDAL(), new FakePlanSuscripcionDAL { PlanPorId = null }, new FakeRenovacionDAL());

            try
            {
                handler.Procesar(new ContextoRenovacion { Cliente = ClienteVigente(), Decision = DecisionRenovacion.CambiarPlan, IdPlanNuevo = 99 });
                Assert.Fail("Debía rechazar un plan inexistente.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.plan_inexistente", ex.Clave);
            }
        }

        [TestMethod]
        public void CambioPlan_PlanInsuficienteParaStockEnUso_LanzaPlanInsuficiente()
        {
            var dalCliente = new FakeClienteDAL();
            var dalPlan = new FakePlanSuscripcionDAL { PlanPorId = PlanPremium(limitePrendas: 1) };
            var handler = new CambioPlanHandler(dalCliente, dalPlan, new FakeRenovacionDAL());
            var cliente = ClienteVigente();
            cliente.StockUtilizado = 3; // más prendas en uso que el nuevo plan permite

            try
            {
                handler.Procesar(new ContextoRenovacion { Cliente = cliente, Decision = DecisionRenovacion.CambiarPlan, IdPlanNuevo = 2 });
                Assert.Fail("Debía rechazar un plan que no alcanza para el stock en uso.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.plan_insuficiente", ex.Clave);
            }
            Assert.AreEqual(0, dalCliente.ModificarVeces);
        }

        [TestMethod]
        public void CambioPlan_DecisionDistinta_DelegaAlSucesor()
        {
            var handler = new CambioPlanHandler(new FakeClienteDAL(), new FakePlanSuscripcionDAL(), new FakeRenovacionDAL());
            var espia = new ManejadorEspia();
            handler.AgregarSiguiente(espia);

            handler.Procesar(new ContextoRenovacion { Cliente = ClienteVencido(), Decision = DecisionRenovacion.Renovar });

            Assert.IsTrue(espia.Invocado);
        }

        // ── BajaSuscripcionHandler ────────────────────────────────────────────────
        // Antes tomaba DAL.Prenda concreto en vez de IPrendaDAL: no se podía instanciar con un
        // doble de prueba y quedaba sin ningún test (eslabón terminal, siempre resuelve).

        [TestMethod]
        public void Baja_SinPrendasEnUso_DaDeBajaSinAvisoDeDevolucion()
        {
            var dalCliente = new FakeClienteDAL();
            var dalRenovacion = new FakeRenovacionDAL();
            var dalPrenda = new FakePrendaDAL();
            var handler = new BajaSuscripcionHandler(dalCliente, dalRenovacion, dalPrenda);
            var cliente = ClienteVigente();

            var resultado = handler.Procesar(new ContextoRenovacion
            {
                Cliente = cliente,
                Decision = DecisionRenovacion.Baja,
                Actor = "vendedor1"
            });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoRenovacion.Baja, resultado.Estado);
            Assert.IsNull(cliente.IdPlan);
            Assert.IsNull(cliente.FechaVencimiento);
            Assert.AreEqual("renov.msg.baja", resultado.Clave);
            Assert.AreEqual(1, dalCliente.ModificarVeces);
            Assert.AreEqual(1, dalRenovacion.AltaVeces);
        }

        [TestMethod]
        public void Baja_ConPrendasEnUso_AvisaSolicitarDevolucion()
        {
            var dalPrenda = new FakePrendaDAL { PorCliente = new System.Collections.Generic.List<BE.Prenda>
            {
                new BE.Prenda { IdPrenda = 1, Nombre = "Remera" },
                new BE.Prenda { IdPrenda = 2, Nombre = "Pantalón" }
            }};
            var handler = new BajaSuscripcionHandler(new FakeClienteDAL(), new FakeRenovacionDAL(), dalPrenda);

            var resultado = handler.Procesar(new ContextoRenovacion { Cliente = ClienteVigente(), Decision = DecisionRenovacion.Baja });

            Assert.AreEqual("renov.msg.baja_conprendas", resultado.Clave);
            Assert.AreEqual(2, resultado.Args[0]);
        }

        [TestMethod]
        public void Baja_SiempreResuelve_SinDelegar()
        {
            // Eslabón terminal: nunca delega, sin importar la Decision (igual que
            // DirectorGeneral del ejemplo de cátedra).
            var handler = new BajaSuscripcionHandler(new FakeClienteDAL(), new FakeRenovacionDAL(), new FakePrendaDAL());

            var resultado = handler.Procesar(new ContextoRenovacion { Cliente = ClienteVigente(), Decision = DecisionRenovacion.Renovar });

            Assert.IsTrue(resultado.Resuelto);
            Assert.AreEqual(BE.EstadoRenovacion.Baja, resultado.Estado);
        }

        // ── BLL.Renovacion (fachada real) ─────────────────────────────────────────
        // Hasta acá todos los tests de este archivo arman su PROPIA copia de la cadena a mano
        // (mismo orden que el constructor de BLL.Renovacion, pero reconstruido en el test) — si
        // alguien invierte el orden real en el constructor de producción, ningún test de los de
        // arriba lo detecta. Estos instancian la clase fachada REAL y ejercitan Procesar(...) de
        // punta a punta, más el guard de entrada (permisos + sin_plan) que tampoco tenía cobertura.

        [TestMethod]
        public void Real_SinSesion_LanzaSesionExpirada()
        {
            // Setup() ya hizo Logout.
            var bll = new BLL.Renovacion(new FakeClienteDAL(), new FakeRenovacionDAL(), new FakePlanSuscripcionDAL(), new FakePrendaDAL());

            try
            {
                bll.Procesar("Test", ClienteVencido(), DecisionRenovacion.Renovar, null, BE.Builders.ModalidadCobro.Mensual, "vendedor1");
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
            var bll = new BLL.Renovacion(new FakeClienteDAL(), new FakeRenovacionDAL(), new FakePlanSuscripcionDAL(), new FakePrendaDAL());
            var cliente = ClienteVencido();
            cliente.IdPlan = null;
            cliente.NombrePlan = null;

            try
            {
                bll.Procesar("Test", cliente, DecisionRenovacion.Renovar, null, BE.Builders.ModalidadCobro.Mensual, "vendedor1");
                Assert.Fail("Debía rechazar un cliente sin plan asignado.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.renovacion.sin_plan", ex.Clave);
            }
        }

        [TestMethod]
        public void Real_ClienteVencidoConRenovacion_ProcesaDePuntaAPuntaConLaCadenaReal()
        {
            LoginComoAdministrador();
            var dalRenovacion = new FakeRenovacionDAL();
            var bll = new BLL.Renovacion(new FakeClienteDAL(), dalRenovacion, new FakePlanSuscripcionDAL(), new FakePrendaDAL());

            var resultado = bll.Procesar("Test", ClienteVencido(), DecisionRenovacion.Renovar, null, BE.Builders.ModalidadCobro.Anual, "vendedor1");

            Assert.AreEqual(BE.EstadoRenovacion.Renovada, resultado.Estado);
            Assert.AreEqual(1, dalRenovacion.AltaVeces);
        }

        [TestMethod]
        public void Real_ClienteVencidoConCambioDePlan_ProcesaDePuntaAPuntaConLaCadenaReal()
        {
            // VerificarVencimientoHandler (primer eslabón real) solo delega si la suscripción
            // está vencida o próxima a vencer, sea cual sea la Decision pedida — con un cliente
            // vigente, CambiarPlan/Baja quedan en Pendiente sin llegar a los eslabones que las
            // resuelven (comportamiento documentado, no un bug: PdN5 solo cubre la decisión
            // tomada AL VENCER, no una baja/cambio anticipado). Por eso estos tests de punta a
            // punta usan un cliente vencido, igual que el de Renovar más arriba.
            LoginComoAdministrador();
            var dalRenovacion = new FakeRenovacionDAL();
            var dalPlan = new FakePlanSuscripcionDAL { PlanPorId = PlanPremium() };
            var bll = new BLL.Renovacion(new FakeClienteDAL(), dalRenovacion, dalPlan, new FakePrendaDAL());

            var resultado = bll.Procesar("Test", ClienteVencido(), DecisionRenovacion.CambiarPlan, 2, BE.Builders.ModalidadCobro.Mensual, "vendedor1");

            Assert.AreEqual(BE.EstadoRenovacion.CambioPlan, resultado.Estado);
            Assert.AreEqual(1, dalRenovacion.AltaVeces);
        }

        [TestMethod]
        public void Real_ClienteVencidoConBaja_ProcesaDePuntaAPuntaConLaCadenaReal()
        {
            LoginComoAdministrador();
            var dalRenovacion = new FakeRenovacionDAL();
            var bll = new BLL.Renovacion(new FakeClienteDAL(), dalRenovacion, new FakePlanSuscripcionDAL(), new FakePrendaDAL());

            var resultado = bll.Procesar("Test", ClienteVencido(), DecisionRenovacion.Baja, null, BE.Builders.ModalidadCobro.Mensual, "vendedor1");

            Assert.AreEqual(BE.EstadoRenovacion.Baja, resultado.Estado);
            Assert.AreEqual(1, dalRenovacion.AltaVeces);
        }
    }
}
