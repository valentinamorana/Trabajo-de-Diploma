using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// PN03 de punta a punta contra la base de pruebas WardrobeFlowDB_Tests, con los cuatro usuarios
    /// demo reales (login con su clave y permisos leídos de la base) y las BLL/DAL reales:
    /// Gerencia sugiere → Administración crea → Contabilidad aprueba → Vendedor pide la baja →
    /// Administración la atiende (primero no está de acuerdo, después sí) y, por otro camino, da de
    /// baja una promoción rechazada. Comprueba también que cada rol no pueda hacer lo de los otros.
    /// Si la base no existe queda Inconclusive. Deja la base como estaba.
    /// </summary>
    [TestClass]
    public class CircuitoPN03IntegracionTests
    {
        private const string Modulo = "Test circuito PN03";
        private static string _motivoSinBase;

        [ClassInitialize]
        public static void VerificarBase(TestContext _)
        {
            var cadena = System.Configuration.ConfigurationManager.ConnectionStrings["WardrobeFlowDB"]?.ConnectionString;
            if (cadena == null || !cadena.Contains("_Tests")) { _motivoSinBase = "La cadena de conexión no apunta a una base _Tests."; return; }
            try { using (var cn = new SqlConnection(cadena)) cn.Open(); }
            catch (Exception ex) { _motivoSinBase = "Sin base de pruebas WardrobeFlowDB_Tests: " + ex.Message; }
        }

        [TestCleanup] public void Cleanup() => Seguridad.SessionManager.Logout();

        private static void Entrar(string usuario, string clave)
        {
            Seguridad.SessionManager.Logout();
            BLL.PermisosAccion.LimpiarCacheVigencia();
            if (!new BLL.Usuario().Login(Modulo, usuario, clave))
                Assert.Inconclusive($"No se pudo ingresar con el usuario demo '{usuario}'.");
        }

        private static BE.Promocion Buscar(int id) => new BLL.Promocion().ObtenerTodas().Single(p => p.IdPromocion == id);

        private static void EsperarError(Action accion, string clave)
        {
            try { accion(); Assert.Fail("Debía lanzar " + clave); }
            catch (BE.AppException ex) { Assert.AreEqual(clave, ex.Clave); }
        }

        [TestMethod]
        public void Circuito_CuatroRoles_SugerirCrearAprobarSolicitarAtenderYDarDeBaja()
        {
            if (_motivoSinBase != null) Assert.Inconclusive(_motivoSinBase);
            var acceso = DAL.Acceso.GetInstance();
            var dt = acceso.Leer("SELECT TOP 1 IdPlan FROM PlanSuscripcion WHERE Estado = 1 ORDER BY Precio DESC", null);
            if (dt.Rows.Count == 0) Assert.Inconclusive("La base de pruebas no tiene planes activos.");
            int idPlan = Convert.ToInt32(dt.Rows[0]["IdPlan"]);

            int idSugerencia = 0, idPromo = 0, idRechazada = 0;
            try
            {
                // 1) Gerencia sugiere (siempre para un plan).
                Entrar("gcomercial", "gcomercial1!");
                var sug = new BLL.SugerenciaPromocion();
                EsperarError(() => sug.RegistrarSugerencia(Modulo, BE.OrigenMetrica.Manual, 0, "Sin plan", BE.TipoDescuento.Porcentaje, 100m),
                    "err.bll.sugerenciapromocion.plan_requerido");
                idSugerencia = sug.RegistrarSugerencia(Modulo, BE.OrigenMetrica.Manual, idPlan, "Circuito de 4 roles",
                    BE.TipoDescuento.Porcentaje, 5000m);

                // 2) Administración la acepta y crea la promoción.
                Entrar("admcomercial", "usuario1!");
                var promoAdm = new BLL.Promocion();
                idPromo = promoAdm.CrearDesdeSugerencia(Modulo, idSugerencia, "PROMO Circuito 4 roles", "prueba",
                    BE.TipoDescuento.Porcentaje, 5m, DateTime.Today, DateTime.Today.AddDays(10), 1000m, "impacto");
                Assert.AreEqual(BE.EstadoPromocion.EnRevisionContable, Buscar(idPromo).Estado);
                Assert.AreEqual(idPlan, Buscar(idPromo).IdPlan);

                // 3) Contabilidad la aprueba.
                Entrar("contable", "usuario1!");
                var analisis = new BLL.Promocion().AnalizarMargenEImpacto(idPromo);
                Assert.IsTrue(analisis.UsuarioPuedeDictaminar);
                Assert.IsTrue(analisis.MargenCalculable);
                new BLL.Promocion().AprobarContable(Modulo, Buscar(idPromo), "Margen suficiente");
                Assert.AreEqual(BE.EstadoPromocion.Vigente, Buscar(idPromo).Estado);
                EsperarError(() => new BLL.Promocion().DarDeBaja(Modulo, Buscar(idPromo), "no me corresponde"), "err.bll.sin_permiso");

                // 4) El Vendedor pide la baja: la promoción sigue Vigente y no puede pedir otra.
                Entrar("vendedor", "vendedor1!");
                var paraVentas = new BLL.Promocion().ObtenerParaVentas().Single(p => p.IdPromocion == idPromo);
                Assert.IsTrue(paraVentas.PuedeSolicitarseBaja());
                new BLL.Promocion().SolicitarBaja(Modulo, paraVentas, "No la usa nadie");
                var conPendiente = new BLL.Promocion().ObtenerParaVentas().Single(p => p.IdPromocion == idPromo);
                Assert.AreEqual(BE.EstadoPromocion.Vigente, conPendiente.Estado);
                Assert.IsTrue(conPendiente.TieneSolicitudBajaPendiente(), "La pantalla de Ventas muestra que ya hay una pendiente.");
                EsperarError(() => new BLL.Promocion().SolicitarBaja(Modulo, conPendiente, "otra"), "err.bll.promocion.solicitud_ya_pendiente");
                EsperarError(() => new BLL.Promocion().DarDeBaja(Modulo, conPendiente, "no me corresponde"), "err.bll.sin_permiso");

                // 5) Administración la atiende sin estar de acuerdo: sigue Vigente.
                Entrar("admcomercial", "usuario1!");
                var pendiente = new BLL.Promocion().ObtenerSolicitudesBajaPendientes().Single(s => s.IdPromocion == idPromo);
                Assert.AreEqual("PROMO Circuito 4 roles", pendiente.NombrePromocion);
                new BLL.Promocion().AtenderSolicitud(Modulo, pendiente, "Sigue rindiendo", darDeBaja: false);
                Assert.AreEqual(BE.EstadoPromocion.Vigente, Buscar(idPromo).Estado);
                Assert.IsFalse(Buscar(idPromo).TieneSolicitudBajaPendiente());

                // 6) El Vendedor la vuelve a pedir y Administración está de acuerdo: queda Dada de baja.
                Entrar("vendedor", "vendedor1!");
                new BLL.Promocion().SolicitarBaja(Modulo, new BLL.Promocion().ObtenerParaVentas().Single(p => p.IdPromocion == idPromo), "Sigue sin rendir");
                Entrar("admcomercial", "usuario1!");
                var segunda = new BLL.Promocion().ObtenerSolicitudesBajaPendientes().Single(s => s.IdPromocion == idPromo);
                new BLL.Promocion().AtenderSolicitud(Modulo, segunda, "De acuerdo, la damos de baja", darDeBaja: true);
                var final = Buscar(idPromo);
                Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, final.Estado);
                var solicitudes = new DAL.Promocion().ObtenerSolicitudesBaja(idPromo);
                Assert.AreEqual(2, solicitudes.Count);
                Assert.IsTrue(solicitudes.All(s => s.FueAtendida()));
                CollectionAssert.AreEqual(new[] { false, true }, solicitudes.Select(s => s.DioDeBaja).ToArray());
                Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, new BLL.Promocion().ObtenerBaja(idPromo).EstadoNuevo);
                Assert.IsFalse(new BLL.Promocion().ObtenerParaVentas().Any(p => p.IdPromocion == idPromo), "Ya no se ofrece a Ventas.");

                // 7) Otro camino: una rechazada que no se reformula se da de baja.
                idRechazada = new BLL.Promocion().CrearManual(Modulo, "PROMO Circuito rechazada", "prueba", BE.TipoDescuento.MontoFijo, 100m,
                    DateTime.Today, DateTime.Today.AddDays(10), idPlan, 500m, "impacto");
                Entrar("contable", "usuario1!");
                new BLL.Promocion().RechazarContable(Modulo, Buscar(idRechazada), "No cubre el costo");
                Entrar("admcomercial", "usuario1!");
                new BLL.Promocion().DarDeBaja(Modulo, Buscar(idRechazada), "No se reformula");
                Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, Buscar(idRechazada).Estado);
                CollectionAssert.AreEqual(
                    new[] { "-→EnRevisionContable", "EnRevisionContable→RechazadaContabilidad", "RechazadaContabilidad→DadaDeBaja" },
                    new BLL.Promocion().ObtenerHistorial(idRechazada)
                        .ConvertAll(h => $"{(h.EstadoAnterior.HasValue ? h.EstadoAnterior.ToString() : "-")}→{h.EstadoNuevo}"));
            }
            finally
            {
                foreach (int id in new[] { idPromo, idRechazada }.Where(x => x != 0))
                    acceso.Escribir(
                        "DELETE FROM SolicitudBajaPromocion WHERE IdPromocion = @Id; DELETE FROM DictamenContable WHERE IdPromocion = @Id; " +
                        "DELETE FROM PromocionHistorial WHERE IdPromocion = @Id; DELETE FROM Promocion WHERE IdPromocion = @Id;",
                        new[] { new SqlParameter("@Id", id) });
                if (idSugerencia != 0)
                    acceso.Escribir("DELETE FROM SugerenciaPromocion WHERE IdSugerencia = @Id", new[] { new SqlParameter("@Id", idSugerencia) });
            }
        }
    }
}
