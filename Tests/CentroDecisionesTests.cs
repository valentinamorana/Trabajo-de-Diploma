using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// PN03 «Inteligencia comercial y promociones» — servicios del Centro de decisiones comerciales con
    /// dobles de prueba (sin SQL Server): el análisis (KPI, tendencia, avisos), el registro de decisiones
    /// con su máquina de estados, el cierre del ciclo (resultado real medido) y los umbrales.
    /// </summary>
    [TestClass]
    public class CentroDecisionesTests
    {
        private static readonly DateTime Hoy = new DateTime(2026, 10, 20);
        private static readonly DateTime Desde = Hoy.AddDays(-89);

        [TestInitialize] public void Setup() { SessionManager.Logout(); ConfiguracionTests.BitacoraNegocio.Eventos.Clear(); }
        [TestCleanup] public void Cleanup() => SessionManager.Logout();

        private static void LoginAdmin() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        private class AbandonoFake : BLL.Interfaces.IAnalisisAbandonoService
        {
            public List<BE.ClienteEnRiesgo> Resultado { get; set; } = new List<BE.ClienteEnRiesgo>();
            public void CambiarEstrategia(BLL.Estrategias.EstrategiaRiesgo estrategia) { }
            public List<BE.ClienteEnRiesgo> Detectar() => Resultado;
        }

        private class MantenimientoFake : BLL.Interfaces.IAnalisisMantenimientoService
        {
            public List<BE.TiempoMantenimientoPrenda> Detectar() => new List<BE.TiempoMantenimientoPrenda>();
        }

        private class Contexto
        {
            public FakeInteligenciaComercialDAL Dal = new FakeInteligenciaComercialDAL();
            public FakePlanSuscripcionDAL Planes = new FakePlanSuscripcionDAL
            {
                Planes = new List<BE.PlanSuscripcion>
                {
                    new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", LimitePrendas = 3, Precio = 1000m, Estado = true },
                    new BE.PlanSuscripcion { IdPlan = 2, Nombre = "Premium", LimitePrendas = 5, Precio = 2000m, Estado = true }
                }
            };
            public AbandonoFake Abandono = new AbandonoFake();
            public FakePromocionDAL Promociones = new FakePromocionDAL();
            public FakeSugerenciaPromocionDAL Sugerencias = new FakeSugerenciaPromocionDAL();
            public FakeDecisionComercialDAL Decisiones = new FakeDecisionComercialDAL();

            public InteligenciaComercial Crear() => new InteligenciaComercial(Dal, Planes, Abandono, new MantenimientoFake(),
                Promociones, Sugerencias, Decisiones, () => Hoy);

            // 10 clientes en Básico; en el período: 2 altas y 3 bajas; en el anterior: 1 alta y 1 baja.
            public Contexto ConDatos()
            {
                for (int i = 1; i <= 10; i++)
                    Dal.Clientes.Add(new BE.ClienteComercial { IdCliente = i, Nombre = "N" + i, Apellido = "A" + i, IdPlan = 1,
                                                               FechaVencimiento = Hoy.AddDays(i * 3 - 6), PrendasEnUso = 1 });
                Dal.Altas.Add(new BE.AltaSuscripcion { IdCliente = 1, IdPlan = 1, Fecha = Hoy.AddDays(-10) });
                Dal.Altas.Add(new BE.AltaSuscripcion { IdCliente = 2, IdPlan = 1, Fecha = Hoy.AddDays(-20) });
                Dal.Altas.Add(new BE.AltaSuscripcion { IdCliente = 3, IdPlan = 1, Fecha = Desde.AddDays(-10) });
                foreach (var (id, dias) in new[] { (20, -5), (21, -15), (22, -30), (23, -100) })
                    Dal.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = id, IdPlanAnterior = 1, Fecha = Hoy.AddDays(dias), Resultado = BE.EstadoRenovacion.Baja });
                for (int i = 0; i < 4; i++)
                    Dal.Cobros.Add(new BE.CobroComercial { Fecha = Hoy.AddDays(-i * 7), Importe = 1000m, Descuento = i == 0 ? 100m : 0m, IdPlan = 1 });
                Dal.Cobros.Add(new BE.CobroComercial { Fecha = Desde.AddDays(-5), Importe = 900m, IdPlan = 1 });
                Dal.Cobros.Add(new BE.CobroComercial { Fecha = Desde.AddDays(-30), Importe = 900m, IdPlan = 1 });
                Abandono.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 4, IdPlan = 1 });
                Abandono.Resultado.Add(new BE.ClienteEnRiesgo { IdCliente = 5, IdPlan = 1 });
                Dal.Prendas.Add(new BE.PrendaComercial { IdPrenda = 1, Nombre = "Vestido", Categoria = "Vestido", Estado = BE.EstadoPrenda.EnUso, FechaAlta = Hoy.AddDays(-200), PedidosPeriodo = 4 });
                Dal.Prendas.Add(new BE.PrendaComercial { IdPrenda = 2, Nombre = "Saco", Categoria = "Saco", Estado = BE.EstadoPrenda.Disponible, FechaAlta = Hoy.AddDays(-200) });
                Dal.Prendas.Add(new BE.PrendaComercial { IdPrenda = 3, Nombre = "Viejo", Categoria = "Saco", Estado = BE.EstadoPrenda.Baja, FechaAlta = Hoy.AddDays(-400) });
                return this;
            }
        }

        // ══ Analizar ═════════════════════════════════════════════════════════════

        [TestMethod]
        public void Analizar_CalculaKpiConVariacionContraElPeriodoAnterior()
        {
            LoginAdmin();
            var panel = new Contexto().ConDatos().Crear().Analizar("Test", Desde, Hoy);

            var mrr = panel.Kpi("mrr");
            // El cliente 1 tiene la suscripción vencida (hace 3 días): no paga hoy, no entra en el MRR.
            Assert.AreEqual(9000m, mrr.Valor);
            Assert.AreEqual(10000m, mrr.ValorAnterior);          // 9000 − 2 altas × 1000 + 3 bajas × 1000
            Assert.AreEqual(false, mrr.EsFavorable);

            var churn = panel.Kpi("churn");
            Assert.AreEqual(27.27m, churn.Valor);                // 3 bajas / 11 clientes al inicio
            Assert.AreEqual(9.09m, churn.ValorAnterior);         // 1 / 11
            Assert.AreEqual(BE.TendenciaKpi.Sube, churn.Tendencia);

            Assert.AreEqual(2m, panel.Kpi("riesgo").Valor);
            Assert.AreEqual(2000m, panel.Kpi("ingresoriesgo").Valor);
            Assert.AreEqual(100m, panel.Kpi("descontado").Valor);
            Assert.AreEqual(4000m, panel.Kpi("ingresos").Valor);
            Assert.AreEqual(1800m, panel.Kpi("ingresos").ValorAnterior);
            Assert.AreEqual(1000m, panel.Kpi("ticket").Valor);
            Assert.AreEqual(50m, panel.Kpi("ocupacion").Valor, "La prenda de baja no cuenta.");
            Assert.AreEqual(8, panel.Kpis.Count);
        }

        [TestMethod]
        public void Analizar_TendenciaDeSeisMesesMasTresProyectados_YGraficos()
        {
            LoginAdmin();
            var panel = new Contexto().ConDatos().Crear().Analizar("Test", Desde, Hoy);
            Assert.AreEqual(9, panel.Tendencia.Count);
            Assert.AreEqual(3, panel.Tendencia.Count(p => p.EsProyeccion));
            Assert.AreEqual(new DateTime(2026, 10, 1), panel.Tendencia[5].Mes);
            Assert.IsTrue(panel.Tendencia.All(p => p.Ingresos >= 0 && p.Altas >= 0 && p.Bajas >= 0));
            Assert.AreEqual(2000m, panel.RiesgoPorPlan.Single().IngresoMensualEnRiesgo);
            Assert.AreEqual(2, panel.StockPorCategoria.Count);
            Assert.AreEqual(1, panel.MasPedidas.Single().IdPrenda);
            Assert.IsFalse(panel.MenosPedidas.Any(p => p.IdPrenda == 1 || p.IdPrenda == 3));
        }

        [TestMethod]
        public void Analizar_SinDatos_NoDivideporCeroYDevuelvePanelVacio()
        {
            LoginAdmin();
            var panel = new Contexto().Crear().Analizar("Test", Desde, Hoy);
            Assert.AreEqual(0m, panel.Kpi("churn").Valor);
            Assert.AreEqual(0m, panel.Kpi("ticket").Valor);
            Assert.AreEqual(0m, panel.Kpi("ocupacion").Valor);
            Assert.AreEqual(9, panel.Tendencia.Count);
            Assert.AreEqual(0, panel.RiesgoPorPlan.Count);
            Assert.AreEqual(1500m, panel.Contexto.TicketPromedio, "Sin cobros, el ticket se estima con el precio promedio de los planes.");
        }

        [TestMethod]
        public void Analizar_FuenteSecundariaQueFalla_AvisaYSigue()
        {
            LoginAdmin();
            var ctx = new Contexto().ConDatos();
            ctx.Dal.FallarPrendas = true;
            var panel = ctx.Crear().Analizar("Test", Desde, Hoy);
            Assert.AreEqual("aviso.prendas", panel.Avisos.Single().Clave);
            Assert.AreEqual(9000m, panel.Kpi("mrr").Valor, "El resto del panel se calcula igual.");
        }

        [TestMethod]
        public void Analizar_FiltroDePlan_SoloCuentaEsePlan()
        {
            LoginAdmin();
            var panel = new Contexto().ConDatos().Crear().Analizar("Test", Desde, Hoy, idPlan: 2);
            Assert.AreEqual(0m, panel.Kpi("mrr").Valor);
            Assert.AreEqual(0m, panel.Kpi("ingresos").Valor);
            Assert.AreEqual("Premium", panel.NombrePlan);
            Assert.IsTrue(panel.Contexto.Planes.All(p => p.IdPlan == 2));
        }

        [TestMethod]
        public void Analizar_FechasInvertidasOSinSesion_Rechaza()
        {
            var bll = new Contexto().Crear();
            try { bll.Analizar("Test", Desde, Hoy); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sesion_expirada", ex.Clave); }
            LoginAdmin();
            try { bll.Analizar("Test", Hoy, Desde); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.promocion.rango_fechas_invalido", ex.Clave); }
        }

        [TestMethod]
        public void Analizar_SinLaPatenteDeGerencia_Rechaza()
        {
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor",
                Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = BE.Patentes.Clientes } } });
            try { new Contexto().Crear().Analizar("Test", Desde, Hoy); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
        }

        [TestMethod]
        public void Recomendar_SobreElAnalisis_DetectaBajasYRenovaciones()
        {
            LoginAdmin();
            var ctx = new Contexto().ConDatos();
            var bll = ctx.Crear();
            var resultado = bll.Recomendar(bll.Analizar("Test", Desde, Hoy));
            Assert.IsTrue(resultado.Recomendaciones.Any(r => r.Clave == "ChurnPlan:1"));
            Assert.IsTrue(resultado.Recomendaciones.Any(r => r.TipoRegla == "Renovaciones"));
            Assert.AreEqual(0, resultado.ReglasConError.Count);
        }

        [TestMethod]
        public void EscenarioBase_TomaLosDatosDelPlanYLosUmbrales()
        {
            LoginAdmin();
            var panel = new Contexto().ConDatos().Crear().Analizar("Test", Desde, Hoy);
            var e = InteligenciaComercial.EscenarioBase(panel, 1);
            Assert.AreEqual(10, e.ClientesPlan);
            Assert.AreEqual(3, e.ClientesObjetivo);              // max(2 en riesgo, 3 bajas)
            Assert.AreEqual(10m, e.DescuentoPorcentaje);
            Assert.AreEqual(30m, e.TasaRetencion);
            Assert.IsNull(InteligenciaComercial.EscenarioBase(panel, 99));
        }

        // ══ Decisiones ═══════════════════════════════════════════════════════════

        private static BE.Recomendacion Rec(string clave = "Renovaciones") => new BE.Recomendacion
        {
            TipoRegla = "Renovaciones", Clave = clave, Prioridad = BE.PrioridadRecomendacion.Alta,
            Titulo = new BE.TextoTraducible("t", "Gestionar renovaciones"),
            Diagnostico = new BE.TextoTraducible("d", "2 vencidas"),
            Principal = new BE.OpcionRecomendacion { Descripcion = new BE.TextoTraducible("o1", "Cobrar"), ImpactoEstimado = 1800m,
                                                     Accion = BE.AccionRecomendacion.ExportarContactos },
            Alternativas = { new BE.OpcionRecomendacion { Descripcion = new BE.TextoTraducible("o2", "Campaña"), ImpactoEstimado = 900m,
                                                          Accion = BE.AccionRecomendacion.OrdenCompra } }
        };

        private static DecisionesComerciales Servicio(FakeDecisionComercialDAL dal) => new DecisionesComerciales(dal, () => Hoy);

        [TestMethod]
        public void Aceptar_RegistraLaOpcionElegidaYLaBitacoraDeNegocio()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var d = Servicio(dal).Aceptar("Test", Rec(), 1);
            Assert.AreEqual(BE.EstadoDecision.Aceptada, dal.Filas.Single().Estado);
            Assert.IsTrue(d.EsAlternativa);
            Assert.AreEqual(900m, d.ImpactoEstimado);
            Assert.AreEqual("Campaña", d.AccionElegida);
            Assert.AreEqual(1, d.IdUsuario);
            Assert.IsTrue(ConfiguracionTests.BitacoraNegocio.Eventos.Any(e => e.Tipo == BE.TipoEventoNegocio.DecisionComercial));
        }

        [TestMethod]
        public void Aceptar_OpcionInexistente_Rechaza()
        {
            LoginAdmin();
            try { Servicio(new FakeDecisionComercialDAL()).Aceptar("Test", Rec(), 5); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.decision.opcion", ex.Clave); }
        }

        [TestMethod]
        public void Aceptar_DosVecesLaMismaOpcion_NoDuplica_YOtraOpcionPideDescartar()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = Servicio(dal);
            var rec = Rec();
            svc.Aceptar("Test", rec, 0);
            svc.AsociarDecisiones(new[] { rec });
            svc.Aceptar("Test", rec, 0);
            Assert.AreEqual(1, dal.Filas.Count);
            try { svc.Aceptar("Test", rec, 1); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.decision.yaaceptada", ex.Clave); }
        }

        [TestMethod]
        public void Descartar_ExigeMotivo()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            try { Servicio(dal).Descartar("Test", Rec(), "  "); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.decision.motivo_requerido", ex.Clave); }
            Assert.AreEqual(0, dal.Filas.Count);
            Servicio(dal).Descartar("Test", Rec(), "Ya se gestionó por teléfono");
            Assert.AreEqual(BE.EstadoDecision.Descartada, dal.Filas.Single().Estado);
        }

        [TestMethod]
        public void Posponer_OcultaHastaLaFecha_YLuegoSeAceptaSobreLaMismaDecision()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = Servicio(dal);
            var rec = Rec();
            try { svc.Posponer("Test", rec, 0, null); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.decision.dias_posponer", ex.Clave); }

            var d = svc.Posponer("Test", rec, 7, "Esperar el cierre de mes");
            Assert.AreEqual(Hoy.AddDays(7), d.PospuestaHasta);
            svc.AsociarDecisiones(new[] { rec });
            Assert.IsFalse(DecisionesComerciales.DebeMostrarse(rec, Hoy, 30));
            Assert.IsTrue(DecisionesComerciales.DebeMostrarse(rec, Hoy.AddDays(8), 30));

            svc.Aceptar("Test", rec, 0);
            Assert.AreEqual(1, dal.Filas.Count, "Se continúa la decisión pospuesta, no se crea otra.");
            Assert.AreEqual(BE.EstadoDecision.Aceptada, dal.Filas.Single().Estado);
            Assert.IsNull(dal.Filas.Single().PospuestaHasta);
        }

        [TestMethod]
        public void Descartada_SeOcultaDuranteLosDiasDeSilencio()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = Servicio(dal);
            var rec = Rec();
            svc.Descartar("Test", rec, "No aplica");
            svc.AsociarDecisiones(new[] { rec });
            Assert.IsFalse(DecisionesComerciales.DebeMostrarse(rec, Hoy.AddDays(10), 30));
            Assert.IsTrue(DecisionesComerciales.DebeMostrarse(rec, Hoy.AddDays(31), 30));
        }

        [TestMethod]
        public void MarcarEjecutada_GuardaLaReferencia_YEsFinal()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = Servicio(dal);
            var d = svc.Aceptar("Test", Rec(), 0);
            svc.MarcarEjecutada("Test", d.IdDecision, BE.DecisionComercial.RefSugerencia, 12);
            var fila = dal.Filas.Single();
            Assert.AreEqual(BE.EstadoDecision.Ejecutada, fila.Estado);
            Assert.AreEqual(12, fila.Referencia);
            try { svc.DescartarDecision("Test", d.IdDecision, "arrepentido"); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.decision.transicion", ex.Clave); }
        }

        [TestMethod]
        public void RegistrarResultado_SoloAceptadasOEjecutadas_YCalculaElDesvio()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var svc = Servicio(dal);
            var descartada = svc.Descartar("Test", Rec("X"), "no");
            try { svc.RegistrarResultado("Test", descartada.IdDecision, 100m); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.decision.sinresultado", ex.Clave); }

            var aceptada = svc.Aceptar("Test", Rec("Y"), 0);
            svc.RegistrarResultado("Test", aceptada.IdDecision, 1500m);
            var fila = dal.ObtenerPorId(aceptada.IdDecision);
            Assert.AreEqual(1500m, fila.ResultadoReal);
            Assert.AreEqual(-300m, fila.Desvio);                 // 1500 real − 1800 estimado
            Assert.AreEqual(Hoy, fila.FechaEvaluacion);
        }

        [TestMethod]
        public void DecisionComercial_MaquinaDeEstados()
        {
            var d = new BE.DecisionComercial { Estado = BE.EstadoDecision.Pendiente };
            Assert.IsTrue(d.TransicionValida(BE.EstadoDecision.Aceptada));
            Assert.IsTrue(d.TransicionValida(BE.EstadoDecision.Pospuesta));
            Assert.IsFalse(d.TransicionValida(BE.EstadoDecision.Ejecutada), "No se ejecuta sin aceptar.");
            d.Estado = BE.EstadoDecision.Aceptada;
            Assert.IsTrue(d.TransicionValida(BE.EstadoDecision.Ejecutada));
            Assert.IsFalse(d.TransicionValida(BE.EstadoDecision.Pospuesta));
            d.Estado = BE.EstadoDecision.Ejecutada;
            Assert.IsTrue(d.EsFinal());
            Assert.IsFalse(Enum.GetValues(typeof(BE.EstadoDecision)).Cast<BE.EstadoDecision>().Any(d.TransicionValida));
            d.Estado = BE.EstadoDecision.Descartada;
            Assert.IsFalse(Enum.GetValues(typeof(BE.EstadoDecision)).Cast<BE.EstadoDecision>().Any(d.TransicionValida));
        }

        [TestMethod]
        public void AsociarDecisiones_TablaInaccesible_NoRompeLasRecomendaciones()
        {
            LoginAdmin();   // la consulta exige la patente de Gerencia
            var rec = Rec();
            Assert.IsFalse(Servicio(new FakeDecisionComercialDAL { FallarLectura = true }).AsociarDecisiones(new[] { rec }));
            Assert.IsNull(rec.UltimaDecision);
        }

        [TestMethod]
        public void Decidir_SinLaPatenteDeGerencia_Rechaza()
        {
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor", Permisos = new List<BE.Permiso>() });
            var dal = new FakeDecisionComercialDAL();
            try { Servicio(dal).Aceptar("Test", Rec(), 0); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
            Assert.AreEqual(0, dal.Filas.Count);
        }

        // ══ Cierre del ciclo ═════════════════════════════════════════════════════

        [TestMethod]
        public void MedirDecisiones_SugerenciaQueTerminoEnPromocion_DevuelveElBeneficioReal()
        {
            LoginAdmin();
            var ctx = new Contexto();
            ctx.Promociones.Todas.Add(new BE.Promocion
            {
                IdPromocion = 4, Nombre = "Retención", Estado = BE.EstadoPromocion.Vigente, IdPlan = 1, NombrePlan = "Básico",
                TipoDescuento = BE.TipoDescuento.Porcentaje, Valor = 10m, FechaInicio = Hoy.AddDays(-40), FechaFin = Hoy.AddDays(30),
                IdSugerenciaOrigen = 5
            });
            for (int i = 1; i <= 10; i++)
            {
                ctx.Promociones.Decisiones.Add((1, new BE.DecisionRenovacion { IdCliente = i, Fecha = Hoy.AddDays(-10),
                    Resultado = i <= 8 ? BE.EstadoRenovacion.Renovada : BE.EstadoRenovacion.Baja }));
                ctx.Promociones.Decisiones.Add((1, new BE.DecisionRenovacion { IdCliente = 100 + i, Fecha = Hoy.AddDays(-60),
                    Resultado = i <= 5 ? BE.EstadoRenovacion.Renovada : BE.EstadoRenovacion.Baja }));
            }
            ctx.Promociones.Impacto.Add(new BE.MetricaImpactoPromocion { IdPromocion = 4, Cobros = 8, TotalDescontado = 2000m });
            var decision = new BE.DecisionComercial
            {
                IdDecision = 1, Fecha = Hoy.AddDays(-45), Estado = BE.EstadoDecision.Ejecutada,
                TipoReferencia = BE.DecisionComercial.RefSugerencia, Referencia = 5, ImpactoEstimado = 10000m
            };
            var medido = ctx.Crear().MedirDecisiones(new[] { decision });
            // Mejora 80 % − 50 % = 30 pp → 3 retenidos × 1000 × 6 meses − 2000 descontados.
            Assert.AreEqual(BE.EstadoMedicion.Medido, medido[1].Estado);
            Assert.AreEqual(16000m, medido[1].Valor);
        }

        [TestMethod]
        public void MedirDecisiones_SinReferenciaMedible_NoDevuelveNada()
        {
            LoginAdmin();
            var d =new BE.DecisionComercial { IdDecision = 1, Estado = BE.EstadoDecision.Aceptada, TipoReferencia = null };
            Assert.AreEqual(0, new Contexto().Crear().MedirDecisiones(new[] { d }).Count);
        }

        // ══ Auditoría 1.6.0: período que no termina hoy, MRR de los que pagan, mes parcial ══

        // Plan Básico (1000) con 10 clientes hoy. Período: agosto 2026 (termina antes de hoy).
        //   En el período: 1 alta (cliente 3) y 2 bajas (40, 41).
        //   Después del período (septiembre-octubre): 2 altas (1, 2) y 1 baja (30).
        private static Contexto ConPeriodoPasado()
        {
            var ctx = new Contexto();
            for (int i = 1; i <= 10; i++)
                ctx.Dal.Clientes.Add(new BE.ClienteComercial { IdCliente = i, Nombre = "N" + i, Apellido = "A" + i, IdPlan = 1, FechaVencimiento = Hoy.AddDays(20) });
            ctx.Dal.Altas.Add(new BE.AltaSuscripcion { IdCliente = 3, IdPlan = 1, Fecha = new DateTime(2026, 8, 10) });
            ctx.Dal.Altas.Add(new BE.AltaSuscripcion { IdCliente = 1, IdPlan = 1, Fecha = new DateTime(2026, 9, 5) });
            ctx.Dal.Altas.Add(new BE.AltaSuscripcion { IdCliente = 2, IdPlan = 1, Fecha = new DateTime(2026, 10, 15) });
            foreach (var (id, fecha) in new[] { (40, new DateTime(2026, 8, 12)), (41, new DateTime(2026, 8, 25)), (30, new DateTime(2026, 9, 20)) })
                ctx.Dal.Movimientos.Add(new BE.MovimientoSuscripcion { IdCliente = id, IdPlanAnterior = 1, Fecha = fecha, Resultado = BE.EstadoRenovacion.Baja });
            return ctx;
        }

        [TestMethod]
        public void Analizar_PeriodoQueNoTerminaHoy_ReconstruyeClientesAlFinDelPeriodo()
        {
            LoginAdmin();
            var panel = ConPeriodoPasado().Crear().Analizar("Test", new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));
            var plan = panel.Contexto.Planes.Single(p => p.IdPlan == 1);
            Assert.AreEqual(10, plan.ClientesActivos, "Hoy (los que recibirían una promoción lanzada ahora).");
            Assert.AreEqual(9, plan.ClientesFinPeriodo, "10 hoy − 2 altas posteriores + 1 baja posterior.");
            Assert.AreEqual(10, plan.ClientesInicio, "9 al fin − 1 alta + 2 bajas del período.");
            Assert.AreEqual(2, plan.Bajas);
            Assert.AreEqual(20m, plan.Churn, "2 bajas / 10 al inicio (antes daba 2/11 al usar los activos de hoy).");
            Assert.AreEqual(20m, panel.Kpi("churn").Valor);
        }

        [TestMethod]
        public void Analizar_PeriodoQueNoTerminaHoy_MrrAlFinDelPeriodoYAlInicio()
        {
            LoginAdmin();
            var panel = ConPeriodoPasado().Crear().Analizar("Test", new DateTime(2026, 8, 1), new DateTime(2026, 8, 31));
            var mrr = panel.Kpi("mrr");
            Assert.AreEqual(9000m, mrr.Valor, "MRR de hoy 10000 − 2000 de altas posteriores + 1000 de la baja posterior.");
            Assert.AreEqual(10000m, mrr.ValorAnterior, "9000 − 1000 de la alta del período + 2000 de las bajas del período.");
        }

        [TestMethod]
        public void Analizar_PeriodoQueTerminaHoy_ElFinDelPeriodoEsElDeHoy()
        {
            LoginAdmin();
            var panel = ConPeriodoPasado().Crear().Analizar("Test", new DateTime(2026, 9, 1), Hoy);
            var plan = panel.Contexto.Planes.Single(p => p.IdPlan == 1);
            Assert.AreEqual(10, plan.ClientesFinPeriodo);
            Assert.AreEqual(10000m, panel.Kpi("mrr").Valor);
        }

        [TestMethod]
        public void Analizar_Mrr_SoloCuentaALosClientesQuePaganHoy()
        {
            LoginAdmin();
            var ctx = new Contexto();
            ctx.Dal.Clientes.Add(new BE.ClienteComercial { IdCliente = 1, IdPlan = 1, FechaVencimiento = Hoy.AddDays(10) });                            // paga
            ctx.Dal.Clientes.Add(new BE.ClienteComercial { IdCliente = 2, IdPlan = 1, FechaVencimiento = Hoy.AddDays(10), FechaPausaHasta = Hoy.AddDays(5) }); // pausado
            ctx.Dal.Clientes.Add(new BE.ClienteComercial { IdCliente = 3, IdPlan = 1, FechaVencimiento = Hoy.AddDays(-1) });                            // vencido
            ctx.Dal.Clientes.Add(new BE.ClienteComercial { IdCliente = 4, IdPlan = 2, FechaVencimiento = Hoy.AddDays(10), FechaLimiteGracia = Hoy.AddDays(-1) }); // suspendido
            ctx.Dal.Clientes.Add(new BE.ClienteComercial { IdCliente = 5, IdPlan = 2, FechaVencimiento = Hoy.AddDays(10), FechaLimiteGracia = Hoy.AddDays(2) });  // en gracia: paga
            var panel = ctx.Crear().Analizar("Test", Desde, Hoy);
            Assert.AreEqual(3000m, panel.Kpi("mrr").Valor, "1000 (cliente 1) + 2000 (cliente 5 en gracia).");
            Assert.AreEqual(3, panel.Contexto.Planes.Single(p => p.IdPlan == 1).ClientesActivos,
                "El pausado y el vencido siguen siendo clientes del plan (no son bajas): solo salen del MRR.");
        }

        [TestMethod]
        public void ClienteComercial_PagaAl_MismosCriteriosQueBeCliente()
        {
            var dia = new DateTime(2026, 10, 20);
            Assert.IsFalse(new BE.ClienteComercial().PagaAl(dia), "Sin plan no paga.");
            Assert.IsTrue(new BE.ClienteComercial { IdPlan = 1 }.PagaAl(dia), "Plan sin vencimiento ni pausa.");
            Assert.IsFalse(new BE.ClienteComercial { IdPlan = 1, FechaPausaHasta = dia }.PagaAl(dia), "La pausa rige hasta su último día inclusive.");
            Assert.IsTrue(new BE.ClienteComercial { IdPlan = 1, FechaPausaHasta = dia.AddDays(-1) }.PagaAl(dia));
            Assert.IsTrue(new BE.ClienteComercial { IdPlan = 1, FechaVencimiento = dia }.PagaAl(dia), "Vence hoy: todavía vigente.");
            Assert.IsFalse(new BE.ClienteComercial { IdPlan = 1, FechaVencimiento = dia.AddDays(-1) }.PagaAl(dia));
            Assert.IsTrue(new BE.ClienteComercial { IdPlan = 1, FechaLimiteGracia = dia }.PagaAl(dia), "Último día de gracia: no suspendido.");
            Assert.IsFalse(new BE.ClienteComercial { IdPlan = 1, FechaLimiteGracia = dia.AddDays(-1) }.PagaAl(dia));
            // Coherente con BE.Cliente (que usa HOY).
            var c = new BE.Cliente { FechaPausaHasta = DateTime.Today, FechaVencimiento = DateTime.Today.AddDays(-1), FechaLimiteGracia = DateTime.Today.AddDays(-1) };
            Assert.IsTrue(c.EstaPausada && c.VencimientoExpirado && c.EstaSuspendidoPorPago);
        }

        [TestMethod]
        public void Analizar_MesEnCurso_EsParcialYNoEntraEnLaRegresion()
        {
            LoginAdmin();
            var ctx = new Contexto();
            // Ingresos completos de mayo a septiembre: 1000, 2000, 3000, 4000, 5000. Octubre (al 20): 500.
            for (int m = 0; m < 5; m++)
                ctx.Dal.Cobros.Add(new BE.CobroComercial { Fecha = new DateTime(2026, 5 + m, 10), Importe = 1000m * (m + 1), IdPlan = 1 });
            ctx.Dal.Cobros.Add(new BE.CobroComercial { Fecha = new DateTime(2026, 10, 5), Importe = 500m, IdPlan = 1 });
            var panel = ctx.Crear().Analizar("Test", Desde, Hoy);
            var reales = panel.Tendencia.Where(p => !p.EsProyeccion).ToList();
            Assert.AreEqual(6, reales.Count);
            Assert.IsTrue(reales[5].EsParcial, "Octubre termina el 20: mes parcial.");
            Assert.IsFalse(reales.Take(5).Any(p => p.EsParcial));
            Assert.AreEqual(500m, reales[5].Ingresos, "Se muestra con lo que lleva.");
            var proy = panel.Tendencia.Where(p => p.EsProyeccion).ToList();
            Assert.AreEqual(new DateTime(2026, 11, 1), proy[0].Mes);
            // Recta de los 5 meses completos (y = 1000 + 1000·x): noviembre es x = 6 → 7000 (sin el parcial).
            Assert.AreEqual(7000m, proy[0].Ingresos);
            Assert.AreEqual(9000m, proy[2].Ingresos);
            Assert.AreEqual(1m, panel.R2Ingresos);
        }

        [TestMethod]
        public void Analizar_PeriodoQueTerminaAFinDeMes_NoHayMesParcial()
        {
            LoginAdmin();
            var panel = new Contexto().ConDatos().Crear().Analizar("Test", new DateTime(2026, 9, 1), new DateTime(2026, 9, 30));
            Assert.IsFalse(panel.Tendencia.Any(p => p.EsParcial));
            Assert.AreEqual(new DateTime(2026, 9, 1), panel.Tendencia[5].Mes);
        }

        [TestMethod]
        public void AgregarProyeccion_SinMesParcial_UsaTodosLosMeses()
        {
            var t = Enumerable.Range(0, 6).Select(i => new BE.PuntoMensual { Mes = new DateTime(2026, 5 + i, 1), Ingresos = 10m * (i + 1), Altas = i, Bajas = 1 }).ToList();
            var r2 = InteligenciaComercial.AgregarProyeccion(t, new DateTime(2026, 10, 1));
            var proy = t.Where(p => p.EsProyeccion).ToList();
            Assert.AreEqual(3, proy.Count);
            Assert.AreEqual(70m, proy[0].Ingresos);
            Assert.AreEqual(6m, proy[0].Altas);
            Assert.AreEqual(1m, proy[0].Bajas);
            Assert.AreEqual(new DateTime(2026, 11, 1), proy[0].Mes);
            Assert.AreEqual(1m, r2);
        }

        // ══ Auditoría 1.6.0: una decisión final deja de bloquear cuando ya no rige (A1) ══

        private static BE.DecisionComercial Decision(BE.EstadoDecision estado, DateTime fecha) =>
            new BE.DecisionComercial { IdDecision = 7, Estado = estado, Fecha = fecha, Motivo = "m",
                                       PospuestaHasta = estado == BE.EstadoDecision.Pospuesta ? fecha.AddDays(7) : (DateTime?)null };

        [TestMethod]
        public void DecisionVigente_Descartada_RigeSoloDuranteElSilencio()
        {
            var d = Decision(BE.EstadoDecision.Descartada, Hoy.AddDays(-10));
            Assert.IsTrue(DecisionesComerciales.DecisionVigente(d, Hoy, 30, Desde));
            Assert.IsFalse(DecisionesComerciales.DecisionVigente(d, Hoy.AddDays(20), 30, Desde), "Pasados 30 días ya no rige.");
            // Mismo corte que DebeMostrarse: vuelve a mostrarse justo cuando deja de regir.
            var rec = Rec(); rec.UltimaDecision = d;
            foreach (var dia in new[] { Hoy, Hoy.AddDays(19), Hoy.AddDays(20), Hoy.AddDays(40) })
                Assert.AreEqual(DecisionesComerciales.DebeMostrarse(rec, dia, 30), !DecisionesComerciales.DecisionVigente(d, dia, 30, Desde));
        }

        [TestMethod]
        public void DecisionVigente_Ejecutada_RigeSoloSiEsDelPeriodoAnalizado()
        {
            Assert.IsTrue(DecisionesComerciales.DecisionVigente(Decision(BE.EstadoDecision.Ejecutada, Desde), Hoy, 30, Desde));
            Assert.IsTrue(DecisionesComerciales.DecisionVigente(Decision(BE.EstadoDecision.Ejecutada, Hoy.AddDays(-3)), Hoy, 30, Desde));
            Assert.IsFalse(DecisionesComerciales.DecisionVigente(Decision(BE.EstadoDecision.Ejecutada, Desde.AddDays(-1)), Hoy, 30, Desde),
                "Ejecutada antes del período analizado: la recomendación de hoy es una situación nueva.");
            Assert.IsFalse(DecisionesComerciales.DecisionVigente(null, Hoy, 30, Desde));
        }

        [TestMethod]
        public void PuedeDecidirse_SoloLaBloqueaUnaDecisionFinalQueSigueRigiendo()
        {
            var rec = Rec();
            Assert.IsFalse(DecisionesComerciales.PuedeDecidirse(null, Hoy, 30, Desde));
            Assert.IsTrue(DecisionesComerciales.PuedeDecidirse(rec, Hoy, 30, Desde), "Sin decisión.");
            rec.UltimaDecision = Decision(BE.EstadoDecision.Aceptada, Hoy.AddDays(-200));
            Assert.IsTrue(DecisionesComerciales.PuedeDecidirse(rec, Hoy, 30, Desde), "Abierta (aceptada): se puede continuar.");
            rec.UltimaDecision = Decision(BE.EstadoDecision.Pospuesta, Hoy.AddDays(-1));
            Assert.IsTrue(DecisionesComerciales.PuedeDecidirse(rec, Hoy, 30, Desde), "Abierta (pospuesta).");
            rec.UltimaDecision = Decision(BE.EstadoDecision.Descartada, Hoy.AddDays(-5));
            Assert.IsFalse(DecisionesComerciales.PuedeDecidirse(rec, Hoy, 30, Desde));
            rec.UltimaDecision = Decision(BE.EstadoDecision.Descartada, Hoy.AddDays(-45));
            Assert.IsTrue(DecisionesComerciales.PuedeDecidirse(rec, Hoy, 30, Desde));
            rec.UltimaDecision = Decision(BE.EstadoDecision.Ejecutada, Hoy.AddDays(-5));
            Assert.IsFalse(DecisionesComerciales.PuedeDecidirse(rec, Hoy, 30, Desde));
            rec.UltimaDecision = Decision(BE.EstadoDecision.Ejecutada, Desde.AddDays(-20));
            Assert.IsTrue(DecisionesComerciales.PuedeDecidirse(rec, Hoy, 30, Desde));
        }

        [TestMethod]
        public void DescartadaQueYaNoRige_SeVuelveADecidirConUnaDecisionNueva()
        {
            LoginAdmin();
            var dal = new FakeDecisionComercialDAL();
            var rec = Rec();
            Servicio(dal).Descartar("Test", rec, "No aplica");
            var luego = new DecisionesComerciales(dal, () => Hoy.AddDays(45));
            luego.AsociarDecisiones(new[] { rec });
            Assert.IsTrue(DecisionesComerciales.PuedeDecidirse(rec, Hoy.AddDays(45), 30, Hoy.AddDays(-44)));
            var d = luego.Aceptar("Test", rec, 0);
            Assert.AreEqual(2, dal.Filas.Count, "La descartada queda como historia; la aceptación es otra decisión.");
            Assert.AreEqual(BE.EstadoDecision.Descartada, dal.Filas.First(f => f.IdDecision != d.IdDecision).Estado);
            Assert.AreEqual(BE.EstadoDecision.Aceptada, d.Estado);
        }

        // ══ Auditoría 1.6.0: "Simular esta opción" ══

        private static BE.PanelComercial PanelConPlan() => new BE.PanelComercial
        {
            Contexto = new BE.ContextoComercial
            {
                Hoy = Hoy, Desde = Desde, Hasta = Hoy, Umbrales = CatalogoUmbrales.ValoresPorDefecto(),
                Planes = { new BE.MetricaPlanComercial { IdPlan = 1, Nombre = "Básico", Precio = 1000m, ClientesActivos = 40, ClientesEnRiesgo = 3, Bajas = 2 } }
            }
        };

        private static BE.OpcionRecomendacion OpcionPromo(BE.AccionRecomendacion accion = BE.AccionRecomendacion.SugerirPromocion, int? idPlan = 1) =>
            new BE.OpcionRecomendacion
            {
                Accion = accion,
                Parametros = new BE.ParametrosAccion { IdPlan = idPlan, DescuentoPorcentaje = 15m, DuracionDias = 60, TasaRetencion = 42m, ClientesObjetivo = 7 }
            };

        [TestMethod]
        public void EsSimulable_SoloOpcionesDePromocionConPlanPorcentajeYDias()
        {
            Assert.IsTrue(InteligenciaComercial.EsSimulable(OpcionPromo()));
            Assert.IsTrue(InteligenciaComercial.EsSimulable(OpcionPromo(BE.AccionRecomendacion.ExtenderPromocion)));
            Assert.IsFalse(InteligenciaComercial.EsSimulable(OpcionPromo(BE.AccionRecomendacion.ExportarContactos)));
            Assert.IsFalse(InteligenciaComercial.EsSimulable(OpcionPromo(idPlan: null)));
            var sinPorcentaje = OpcionPromo(); sinPorcentaje.Parametros.DescuentoPorcentaje = 0m;
            Assert.IsFalse(InteligenciaComercial.EsSimulable(sinPorcentaje), "Una promoción de monto fijo no entra en el simulador (que trabaja en %).");
            Assert.IsFalse(InteligenciaComercial.EsSimulable(null));
            Assert.IsFalse(InteligenciaComercial.EsSimulable(new BE.OpcionRecomendacion { Accion = BE.AccionRecomendacion.SugerirPromocion }));
        }

        [TestMethod]
        public void EscenarioDesdeOpcion_TomaPlanPorcentajeDiasRetencionYObjetivoDeLaOpcion()
        {
            var e = InteligenciaComercial.EscenarioDesdeOpcion(PanelConPlan(), OpcionPromo());
            Assert.AreEqual(1, e.IdPlan);
            Assert.AreEqual(40, e.ClientesPlan, "Del análisis.");
            Assert.AreEqual(15m, e.DescuentoPorcentaje);
            Assert.AreEqual(60, e.DuracionDias);
            Assert.AreEqual(42m, e.TasaRetencion);
            Assert.AreEqual(7, e.ClientesObjetivo);
            Assert.AreEqual(CatalogoUmbrales.ValoresPorDefecto()[CatalogoUmbrales.HorizonteMeses], e.HorizonteMeses);

            var sinObjetivo = OpcionPromo(); sinObjetivo.Parametros.ClientesObjetivo = 0; sinObjetivo.Parametros.TasaRetencion = 0m;
            var e2 = InteligenciaComercial.EscenarioDesdeOpcion(PanelConPlan(), sinObjetivo);
            Assert.AreEqual(3, e2.ClientesObjetivo, "Sin objetivo en la opción, el del escenario base (máx. en riesgo, bajas).");
            Assert.AreEqual(CatalogoUmbrales.ValoresPorDefecto()[CatalogoUmbrales.TasaRetencionDefecto], e2.TasaRetencion);

            Assert.IsNull(InteligenciaComercial.EscenarioDesdeOpcion(PanelConPlan(), OpcionPromo(idPlan: 9)), "Plan fuera del análisis.");
            Assert.IsNull(InteligenciaComercial.EscenarioDesdeOpcion(PanelConPlan(), OpcionPromo(BE.AccionRecomendacion.ExportarContactos)), "Solo una opción de promoción se simula.");
        }

        [TestMethod]
        public void OpcionDeLaReglaDeBajas_AlSimularla_ReproduceSuImpacto()
        {
            LoginAdmin();
            var ctx = new Contexto().ConDatos();
            var bll = ctx.Crear();
            var panel = bll.Analizar("Test", Desde, Hoy);
            var rec = bll.Recomendar(panel).Recomendaciones.Single(r => r.Clave == "ChurnPlan:1");
            var opcion = rec.Opciones().First(InteligenciaComercial.EsSimulable);
            var sim = bll.Simular(InteligenciaComercial.EscenarioDesdeOpcion(panel, opcion));
            Assert.AreEqual(opcion.ImpactoEstimado, sim.BeneficioNeto, "El simulador precargado da el mismo beneficio que estimó la regla.");
        }

        // ══ Auditoría 1.6.0: lecturas con permiso ══

        [TestMethod]
        public void Lecturas_SinSesion_Rechazan()
        {
            var bll = new Contexto().Crear();
            var panel = PanelConPlan();
            void Rechaza(Action a)
            {
                try { a(); Assert.Fail(); }
                catch (BE.AppException ex) { Assert.AreEqual("err.bll.sesion_expirada", ex.Clave); }
            }
            Rechaza(() => bll.Recomendar(panel));
            Rechaza(() => bll.Simular(InteligenciaComercial.EscenarioBase(panel, 1)));
            Rechaza(() => bll.MedirDecisiones(new BE.DecisionComercial[0]));
            Rechaza(() => bll.Umbrales.ObtenerValores());
            Rechaza(() => bll.Umbrales.Obtener());
        }

        [TestMethod]
        public void Lecturas_SinLaPatenteDeGerencia_Rechazan()
        {
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor",
                Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = BE.Patentes.Clientes } } });
            var bll = new Contexto().Crear();
            try { bll.Recomendar(PanelConPlan()); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
            try { bll.Umbrales.ObtenerValores(); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
        }

        // ══ Umbrales ═════════════════════════════════════════════════════════════

        [TestMethod]
        public void Umbrales_Guardar_ValidaCoherenciaYSoloGuardaLosCambios()
        {
            LoginAdmin();
            var dal = new FakeInteligenciaComercialDAL();
            var u = new UmbralesComerciales(dal);
            try { u.Guardar("Test", new Dictionary<string, decimal> { [CatalogoUmbrales.DescuentoMaximoPct] = 5m }); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.umbral.descuentos", ex.Clave); }

            u.Guardar("Test", new Dictionary<string, decimal> { [CatalogoUmbrales.ChurnAltoPct] = 8m, [CatalogoUmbrales.DiasPorVencer] = 7m });
            Assert.AreEqual("8", dal.Parametros[CatalogoUmbrales.ChurnAltoPct]);
            Assert.IsFalse(dal.Parametros.ContainsKey(CatalogoUmbrales.DiasPorVencer), "Sin cambios contra el valor vigente, no se escribe.");
            Assert.AreEqual(8m, u.ObtenerValores()[CatalogoUmbrales.ChurnAltoPct]);
        }

        [TestMethod]
        public void Umbrales_LecturaQueFalla_UsaLosValoresDeFabrica()
        {
            LoginAdmin();
            var u =new UmbralesComerciales(new FakeInteligenciaComercialDAL { FallarParametros = true });
            Assert.AreEqual(5m, u.ObtenerValores()[CatalogoUmbrales.ChurnAltoPct]);
        }
    }
}
