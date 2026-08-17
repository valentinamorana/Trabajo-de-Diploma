using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BLL.Manejadores;
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
    }
}
