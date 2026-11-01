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
    /// PN03 + A02 (1.6.1): «Constancia de la decisión»: Aceptar, Descartar y Posponer guardan la foto de lo
    /// que se vio (con fakes de DAL, sin base); una falla al guardarla no frena la decisión; una decisión
    /// anterior queda sin constancia; la comparación campo por campo detecta un cambio.
    /// </summary>
    [TestClass]
    public class ConstanciaDecisionTests
    {
        private static readonly DateTime Hoy = new DateTime(2026, 11, 1, 10, 15, 42);

        [TestInitialize]
        public void Login() => SessionManager.Login(new BE.Usuario
        {
            Id = 3, Username = "gcomercial", Nombre = "Gabriela", Apellido = "Núñez", Perfil = "GerenteComercial",
            Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = BE.Patentes.SugerenciaPromocion } }
        });

        [TestCleanup]
        public void Logout() => SessionManager.Logout();

        internal static BE.Recomendacion Rec(string clave = "ChurnPlan:2") => new BE.Recomendacion
        {
            TipoRegla = "ChurnPlan", Clave = clave, Prioridad = BE.PrioridadRecomendacion.Media, Confianza = BE.NivelConfianza.Alta,
            Titulo = new BE.TextoTraducible("t", "Bajas altas en el plan Básico"),
            Diagnostico = new BE.TextoTraducible("d", "El % de bajas llegó a 7,5 % (Удержание клиентов)"),
            Motivo = new BE.TextoTraducible("m", "Supera el límite de los parámetros del análisis"),
            Principal = new BE.OpcionRecomendacion
            {
                Descripcion = new BE.TextoTraducible("o1", "Promoción de retención 20 % por 30 días"), ImpactoEstimado = 12345.67m,
                Formula = new BE.TextoTraducible("f1", "Ingreso en riesgo × retención − costo"), Accion = BE.AccionRecomendacion.SugerirPromocion
            },
            Alternativas =
            {
                new BE.OpcionRecomendacion { Descripcion = new BE.TextoTraducible("o2", "Exportar contactos"), ImpactoEstimado = 900m,
                                             Formula = new BE.TextoTraducible("f2", "Contactos × tasa"), Accion = BE.AccionRecomendacion.ExportarContactos }
            }
        };

        internal static BE.PanelComercial Panel() => new BE.PanelComercial
        {
            Desde = new DateTime(2026, 8, 4), Hasta = new DateTime(2026, 11, 1), IdPlan = 2, NombrePlan = "Básico",
            Kpis =
            {
                new BE.KpiComercial { Clave = "mrr", Nombre = new BE.TextoTraducible("k1", "Ingreso mensual"), Unidad = BE.UnidadKpi.Moneda,
                                      Valor = 659000m, ValorAnterior = 640000.5m, Variacion = 18999.5m },
                new BE.KpiComercial { Clave = "churn", Nombre = new BE.TextoTraducible("k2", "% de bajas"), Unidad = BE.UnidadKpi.Porcentaje,
                                      Valor = 7.5m, ValorAnterior = null, Variacion = null }
            },
            Contexto = new BE.ContextoComercial { Umbrales = CatalogoUmbrales.ValoresPorDefecto() },
            Avisos = { new BE.TextoTraducible("a", "Hay clientes sin plan: los indicadores pueden quedar cortos.") }
        };

        private static DecisionesComerciales Servicio(FakeDecisionComercialDAL dal) => new DecisionesComerciales(dal, () => Hoy);

        private static BE.ConstanciaDecision Guardada(FakeDecisionComercialDAL dal, int id) =>
            ConstanciasDecision.Deserializar(dal.Constancias[id]);

        [TestMethod]
        public void Aceptar_GuardaLaConstanciaConLoQueSeVio()
        {
            var dal = new FakeDecisionComercialDAL();
            var d = Servicio(dal).Aceptar("Test", Rec(), 1, Panel());

            Assert.IsTrue(d.TieneConstancia);
            Assert.IsNull(d.AvisoConstancia);
            var c = Guardada(dal, d.IdDecision);
            Assert.AreEqual(BE.ConstanciaDecision.FormatoActual, c.Version);
            Assert.AreEqual(d.IdDecision, c.IdDecision);
            Assert.AreEqual("Aceptada", c.Accion);
            Assert.AreEqual(new DateTime(2026, 11, 1, 10, 15, 42), c.Fecha, "Fecha y hora al segundo.");
            Assert.AreEqual("gcomercial", c.Username);
            Assert.AreEqual("Gabriela Núñez", c.NombreUsuario);
            Assert.AreEqual("Bajas altas en el plan Básico", c.Recomendacion.Titulo);
            Assert.AreEqual("ChurnPlan", c.Recomendacion.Regla);
            Assert.AreEqual("Media", c.Recomendacion.Prioridad);
            Assert.AreEqual("Alta", c.Recomendacion.Confianza);
            Assert.AreEqual("Exportar contactos", c.OpcionElegida.Descripcion, "La alternativa elegida (índice 1).");
            Assert.AreEqual(900m, c.OpcionElegida.ImpactoEstimado);
            Assert.AreEqual(1, c.Alternativas.Count);
            Assert.AreEqual("Promoción de retención 20 % por 30 días", c.Alternativas[0].Descripcion);
            Assert.AreEqual("Básico", c.Periodo.Plan);
            Assert.AreEqual(new DateTime(2026, 8, 4), c.Periodo.Desde);
            Assert.AreEqual(2, c.Indicadores.Count);
            Assert.AreEqual(640000.5m, c.Indicadores[0].Anterior);
            Assert.IsNull(c.Indicadores[1].Anterior);
            Assert.AreEqual(CatalogoUmbrales.Claves.Count(), c.ParametrosDelAnalisis.Count, "Todos los parámetros del análisis.");
            Assert.IsTrue(c.Supuestos.Any(s => s.Contains("Ingreso en riesgo × retención − costo")), "Las fórmulas van en los supuestos.");
            Assert.IsTrue(c.Supuestos.Any(s => s.StartsWith("Parámetro del análisis")), "Y los parámetros también.");
            Assert.AreEqual("Hay clientes sin plan: los indicadores pueden quedar cortos.", c.Advertencias.Single());
        }

        [TestMethod]
        public void Descartar_GuardaLaConstanciaConElMotivo()
        {
            var dal = new FakeDecisionComercialDAL();
            var d = Servicio(dal).Descartar("Test", Rec(), "No hay presupuesto", Panel());
            var c = Guardada(dal, d.IdDecision);
            Assert.AreEqual("Descartada", c.Accion);
            Assert.AreEqual("No hay presupuesto", c.Motivo);
            Assert.AreEqual("Promoción de retención 20 % por 30 días", c.OpcionElegida.Descripcion, "Al descartar, la principal.");
        }

        [TestMethod]
        public void Posponer_GuardaLaConstanciaConLaFecha()
        {
            var dal = new FakeDecisionComercialDAL();
            var d = Servicio(dal).Posponer("Test", Rec(), 7, null, Panel());
            var c = Guardada(dal, d.IdDecision);
            Assert.AreEqual("Pospuesta", c.Accion);
            Assert.AreEqual(new DateTime(2026, 11, 8), c.PospuestaHasta);
            Assert.IsTrue(dal.ObtenerPorId(d.IdDecision).TieneConstancia || dal.Constancias.ContainsKey(d.IdDecision));
        }

        [TestMethod]
        public void SinPanel_IgualGuardaLaConstancia_SinPeriodoNiIndicadores()
        {
            var dal = new FakeDecisionComercialDAL();
            var d = Servicio(dal).Aceptar("Test", Rec(), 0);
            var c = Guardada(dal, d.IdDecision);
            Assert.AreEqual(0, c.Indicadores.Count);
            Assert.AreEqual(0, c.ParametrosDelAnalisis.Count);
        }

        [TestMethod]
        public void SiFallaGuardarLaConstancia_LaDecisionQuedaRegistrada_YSeAvisa()
        {
            var dal = new FakeDecisionComercialDAL { FallarConstancia = true };
            var d = Servicio(dal).Aceptar("Test", Rec(), 0, Panel());

            Assert.AreEqual(BE.EstadoDecision.Aceptada, dal.Filas.Single().Estado, "La decisión no se revierte.");
            Assert.IsFalse(d.TieneConstancia);
            Assert.IsNotNull(d.AvisoConstancia, "Se informa (mensaje no bloqueante en la pantalla).");
            Assert.IsFalse(dal.Constancias.ContainsKey(d.IdDecision));
            Assert.IsTrue(ConfiguracionTests.Bitacora.Registros.Any(r => r.Actividad.StartsWith("No se pudo guardar la constancia de la decisión comercial #" + d.IdDecision)));
        }

        [TestMethod]
        public void DecisionAnterior_SinConstancia_ObtenerDevuelveNull_YExportarLoExplica()
        {
            var dal = new FakeDecisionComercialDAL();
            int id = dal.Alta(new BE.DecisionComercial { Fecha = Hoy, TipoRegla = "x", ClaveRecomendacion = "x", Titulo = "Vieja", Estado = BE.EstadoDecision.Descartada, Motivo = "m" });
            var bll = new ConstanciasDecision(dal);
            Assert.IsNull(bll.Obtener(id));
            Assert.IsFalse(dal.ObtenerPorId(id).TieneConstancia);
            try { bll.Exportar("Test", id, System.IO.Path.Combine(System.IO.Path.GetTempPath(), "no_se_escribe_" + Guid.NewGuid().ToString("N") + ".xml")); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.constancia.sinconstancia", ex.Clave); }
        }

        [TestMethod]
        public void SinLaPatenteDeGerencia_NoSeLeeNiSeExporta()
        {
            var dal = new FakeDecisionComercialDAL();
            var d = Servicio(dal).Aceptar("Test", Rec(), 0, Panel());
            SessionManager.Logout();
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor", Permisos = new List<BE.Permiso>() });
            var bll = new ConstanciasDecision(dal);
            try { bll.Obtener(d.IdDecision); Assert.Fail(); }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
        }

        [TestMethod]
        public void Diferencias_DetectaUnValorCambiado_YNadaSiSonIguales()
        {
            var dal = new FakeDecisionComercialDAL();
            var d = Servicio(dal).Aceptar("Test", Rec(), 0, Panel());
            var a = Guardada(dal, d.IdDecision);
            var b = Guardada(dal, d.IdDecision);
            Assert.AreEqual(0, ConstanciasDecision.Diferencias(a, b).Count);

            b.Indicadores[0].Valor = 1m;
            b.Motivo = "otro";
            var dif = ConstanciasDecision.Diferencias(b, a);
            Assert.AreEqual(2, dif.Count, string.Join(" | ", dif));
            Assert.IsTrue(dif.Any(x => x.StartsWith("Indicadores[mrr].Valor")));
            Assert.IsTrue(dif.Any(x => x.StartsWith("Motivo")));
        }
    }
}
