using System;
using System.Globalization;
using System.Linq;
using System.Text;
using BLL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Servicios.Multiidioma;

namespace Tests
{
    /// <summary>
    /// 1.6.1: El error de conexión del arranque explica la causa concreta según el número de SqlException
    /// (BLL.Configuracion.ClasificarErrorConexion, pura) y lo dice en el idioma de Windows con los textos del
    /// corpus embebido (todavía no hay tabla Traduccion). Además, ningún texto del corpus supera los 1000
    /// caracteres de Traduccion.Texto.
    /// </summary>
    [TestClass]
    public class ConexionErrorTests
    {
        [TestMethod]
        public void Clasificar_4060_BaseInexistente()
        {
            Assert.AreEqual(Configuracion.CausaConexion.BaseInexistente, Configuracion.ClasificarErrorConexion(4060));
        }

        [TestMethod]
        public void Clasificar_18456_y_18452_SinPermiso()
        {
            Assert.AreEqual(Configuracion.CausaConexion.SinPermiso, Configuracion.ClasificarErrorConexion(18456));
            Assert.AreEqual(Configuracion.CausaConexion.SinPermiso, Configuracion.ClasificarErrorConexion(18452));
        }

        [TestMethod]
        public void Clasificar_ServidorNoResponde_OTiempoAgotado()
        {
            foreach (int n in new[] { -2, -1, 2, 53, 258, 26 })
                Assert.AreEqual(Configuracion.CausaConexion.ServidorNoResponde, Configuracion.ClasificarErrorConexion(n), n.ToString());
        }

        [TestMethod]
        public void Clasificar_OtroNumero_OSinNumero_Otra()
        {
            Assert.AreEqual(Configuracion.CausaConexion.Otra, Configuracion.ClasificarErrorConexion(547));
            Assert.AreEqual(Configuracion.CausaConexion.Otra, Configuracion.ClasificarErrorConexion(null));
            Assert.AreEqual(Configuracion.CausaConexion.Otra, Configuracion.ClasificarErrorConexion(null, new InvalidOperationException("falta la cadena")));
            Assert.IsNull(Configuracion.NumeroErrorSql(new InvalidOperationException("x", new ArgumentException())));
        }

        private static string T(string clave, string fallback) => fallback;

        [TestMethod]
        public void Mensaje_CadaCausa_NombraElServidor_LaSolucion_YLaCadenaDeConexion()
        {
            const string ruta = "GUI.exe.config";
            string Msj(Configuracion.CausaConexion c, string srv = @".\SQLEXPRESS", int? n = null) =>
                Configuracion.MensajeErrorConexion(c, n, "detalle técnico", srv, "WardrobeFlowDB", ruta, T);

            string base_ = Msj(Configuracion.CausaConexion.BaseInexistente);
            StringAssert.Contains(base_, "'WardrobeFlowDB' no existe en el servidor '.\\SQLEXPRESS'");
            StringAssert.Contains(base_, "reinstálelo");

            StringAssert.Contains(Msj(Configuracion.CausaConexion.SinPermiso), "Pida acceso al administrador del servidor");

            string servidor = Msj(Configuracion.CausaConexion.ServidorNoResponde);
            StringAssert.Contains(servidor, "services.msc");
            StringAssert.Contains(servidor, "SQL Server (SQLEXPRESS)");

            string localdb = Msj(Configuracion.CausaConexion.ServidorNoResponde, @"(localdb)\MSSQLLocalDB");
            StringAssert.Contains(localdb, "sqllocaldb start");
            Assert.IsFalse(localdb.Contains("services.msc"), "LocalDB no es un servicio de Windows.");

            string otra = Msj(Configuracion.CausaConexion.Otra, n: 547);
            StringAssert.Contains(otra, "Error 547");
            StringAssert.Contains(otra, "detalle técnico");

            foreach (var c in Enum.GetValues(typeof(Configuracion.CausaConexion)).Cast<Configuracion.CausaConexion>())
                StringAssert.Contains(Msj(c), ruta, c + ": siempre dice dónde está la cadena de conexión.");
        }

        [TestMethod]
        public void Mensaje_TextosDelCorpus_EnLosCuatroIdiomas_YConLosMismosMarcadores()
        {
            var claves = new[] { "conexion.error.titulo", "conexion.error.base", "conexion.error.permiso", "conexion.error.servidor",
                                 "conexion.error.localdb", "conexion.error.otro", "conexion.ayuda.cadena" };
            var es = Traductor.ObtenerTraduccionesHardcode(new Idioma { Id = "ES" });
            foreach (var idioma in new[] { "ES", "EN", "RU", "PT" })
            {
                var t = Traductor.ObtenerTraduccionesHardcode(new Idioma { Id = idioma });
                foreach (var k in claves)
                {
                    Assert.IsTrue(t.ContainsKey(k) && !string.IsNullOrWhiteSpace(t[k].Texto), $"Falta {k} en {idioma}");
                    for (int i = 0; i <= 3; i++)
                        Assert.AreEqual(es[k].Texto.Contains("{" + i + "}"), t[k].Texto.Contains("{" + i + "}"), $"{k} {idioma}: marcador {{{i}}}");
                }
                string msj = Configuracion.MensajeErrorConexion(Configuracion.CausaConexion.ServidorNoResponde, -2, null, "srv", "WardrobeFlowDB", "x.config",
                    (k, fb) => t.ContainsKey(k) ? t[k].Texto : fb);
                StringAssert.Contains(msj, "srv", idioma);
                StringAssert.Contains(msj, "x.config", idioma);
            }
        }

        [TestMethod]
        public void IdiomaPrevioALaConexion_EsElDeWindows_SiEstaEntreLosCuatro_SiNoElPorDefecto()
        {
            Assert.AreEqual("EN", Configuracion.IdiomaPrevioALaConexion(new CultureInfo("en-US")).Id.ToUpperInvariant());
            Assert.AreEqual("RU", Configuracion.IdiomaPrevioALaConexion(new CultureInfo("ru-RU")).Id.ToUpperInvariant());
            Assert.AreEqual("PT", Configuracion.IdiomaPrevioALaConexion(new CultureInfo("pt-BR")).Id.ToUpperInvariant());
            Assert.AreEqual("ES", Configuracion.IdiomaPrevioALaConexion(new CultureInfo("es-AR")).Id.ToUpperInvariant());
            Assert.AreEqual(Traductor.ObtenerIdiomaDefault().Id, Configuracion.IdiomaPrevioALaConexion(new CultureInfo("de-DE")).Id);
        }

        [TestMethod]
        public void Corpus_NingunTextoSuperaLos1000CaracteresDeTraduccionTexto()
        {
            var errores = new StringBuilder();
            foreach (var idioma in new[] { "ES", "EN", "RU", "PT" })
                foreach (var kv in Traductor.ObtenerTraduccionesHardcode(new Idioma { Id = idioma }))
                    if ((kv.Value.Texto ?? "").Length > 1000)
                        errores.AppendLine($"{idioma} {kv.Key}: {kv.Value.Texto.Length}");
            Assert.AreEqual(0, errores.Length, "Traduccion.Texto es NVARCHAR(1000):\n" + errores);
        }
    }
}
