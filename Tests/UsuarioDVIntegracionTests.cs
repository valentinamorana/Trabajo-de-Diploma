using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// DAL.Usuario contra la base de pruebas WardrobeFlowDB_Tests: cada escritura deja el DVH de la
    /// fila y el espejo de integridad (Usuario_Seguridad) al día en la MISMA transacción, y si la
    /// escritura falla no queda nada a medias. También el reset de la escala del bloqueo progresivo
    /// tras un login exitoso. Si no hay base de pruebas, quedan Inconclusive (mismo criterio que
    /// IntegracionBDTests). Crean y purgan su propio usuario temporal.
    /// </summary>
    [TestClass]
    public class UsuarioDVIntegracionTests
    {
        private static string _motivoSinBase;

        [ClassInitialize]
        public static void VerificarBase(TestContext _)
        {
            var cadena = System.Configuration.ConfigurationManager.ConnectionStrings["WardrobeFlowDB"]?.ConnectionString;
            if (cadena == null || !cadena.Contains("_Tests"))
            {
                _motivoSinBase = "La cadena de conexión de las pruebas no apunta a una base _Tests.";
                return;
            }
            try
            {
                using (var cn = new SqlConnection(cadena)) cn.Open();
            }
            catch (Exception ex) { _motivoSinBase = "Sin base de pruebas WardrobeFlowDB_Tests: " + ex.Message; }
        }

        private static void ExigirBase()
        {
            if (_motivoSinBase != null) Assert.Inconclusive(_motivoSinBase);
        }

        private static string NombreTemporal() => "tdv" + Guid.NewGuid().ToString("N").Substring(0, 12);

        private static BE.FilaUsuarioDV Fila(int id) =>
            new DAL.DigitoVerificador().ObtenerFilasUsuario().SingleOrDefault(f => f.Id == id);

        private static BE.FilaUsuarioDV FilaEspejo(int id) =>
            new DAL.EspejoUsuario().ObtenerFilas().SingleOrDefault(f => f.Id == id);

        // DVH almacenado = recalculado, y el espejo tiene exactamente los mismos campos y DVH.
        private static void AssertDvYEspejoAlDia(int id, string momento)
        {
            var fila = Fila(id);
            Assert.IsNotNull(fila, momento + ": la fila existe.");
            Assert.AreEqual(Seguridad.CalculadorDV.Crear().CalcularDVH(fila.CamposParaDVH()), fila.DVHAlmacenado,
                momento + ": el DVH se recalculó en la misma transacción.");
            var espejo = FilaEspejo(id);
            Assert.IsNotNull(espejo, momento + ": el espejo tiene la fila.");
            CollectionAssert.AreEqual(fila.CamposParaDVH(), espejo.CamposParaDVH(), momento + ": espejo sincronizado.");
            Assert.AreEqual(fila.DVHAlmacenado, espejo.DVHAlmacenado, momento + ": DVH del espejo.");
        }

        [TestMethod]
        public void Usuario_CadaEscrituraDejaDvhYEspejoAlDia_YLaBajaFisicaLosQuita()
        {
            ExigirBase();
            var dal = new DAL.Usuario();
            string nombre = NombreTemporal();
            dal.Alta(nombre, "hash-de-prueba", "Vendedor");
            int id = dal.ObtenerPorUsername(nombre).Id;
            try
            {
                AssertDvYEspejoAlDia(id, "Alta");
                Assert.AreEqual("1", Fila(id).RequiereCambioClave, "El alta exige cambiar la clave en el primer login.");

                Assert.AreEqual(1, dal.IncrementarIntentosFallidos(nombre));
                AssertDvYEspejoAlDia(id, "IncrementarIntentosFallidos");

                dal.BloquearConTiempo(id);
                AssertDvYEspejoAlDia(id, "BloquearConTiempo");

                dal.AutoDesbloquear(id);
                AssertDvYEspejoAlDia(id, "AutoDesbloquear");

                dal.Modificar(id, "Nom", "Ape", nombre, null, null);
                AssertDvYEspejoAlDia(id, "Modificar");

                dal.CambiarRol(id, "Cajero");
                AssertDvYEspejoAlDia(id, "CambiarRol");

                dal.CambiarClave(id, "otro-hash");
                AssertDvYEspejoAlDia(id, "CambiarClave");
                Assert.AreEqual("0", Fila(id).RequiereCambioClave);

                dal.BajaLogica(id);
                AssertDvYEspejoAlDia(id, "BajaLogica");
            }
            finally
            {
                dal.EliminarFisico(id);
            }
            Assert.IsNull(Fila(id), "EliminarFisico borra la fila.");
            Assert.IsNull(FilaEspejo(id), "…y la quita del espejo en la misma transacción.");
        }

        [TestMethod]
        public void Usuario_SiLaEscrituraFalla_SePropagaYNoQuedaNadaAMedias()
        {
            ExigirBase();
            var dal = new DAL.Usuario();
            string a = NombreTemporal(), b = NombreTemporal();
            dal.Alta(a, "h", "Vendedor");
            dal.Alta(b, "h", "Vendedor");
            int idA = dal.ObtenerPorUsername(a).Id, idB = dal.ObtenerPorUsername(b).Id;
            try
            {
                var antes = Fila(idB);
                // Username repetido → choca con UQ_Usuario_Username: antes el error del recálculo se
                // tragaba; ahora la transacción entera se revierte y el error llega al llamador.
                // (Assert.ThrowsException no está disponible en esta versión de MSTest.)
                bool lanzo = false;
                try { dal.Modificar(idB, "X", "Y", a, null, null); }
                catch (Exception) { lanzo = true; }
                Assert.IsTrue(lanzo, "El error de la escritura se propaga.");
                var despues = Fila(idB);
                Assert.AreEqual(b, despues.Username);
                Assert.AreEqual(antes.DVHAlmacenado, despues.DVHAlmacenado);
                AssertDvYEspejoAlDia(idB, "Tras el rollback");
            }
            finally
            {
                dal.EliminarFisico(idA);
                dal.EliminarFisico(idB);
            }
        }

        [TestMethod]
        public void Usuario_LoginExitoso_ReseteaIntentosYLaEscalaDeBloqueos()
        {
            ExigirBase();
            var dal = new DAL.Usuario();
            string nombre = NombreTemporal();
            dal.Alta(nombre, "h", "Vendedor");
            int id = dal.ObtenerPorUsername(nombre).Id;
            try
            {
                dal.BloquearConTiempo(id);
                dal.AutoDesbloquear(id);
                dal.IncrementarIntentosFallidos(nombre);
                Assert.AreEqual("1", Fila(id).CantidadBloqueos);
                Assert.AreEqual("1", Fila(id).IntentosFallidos);

                dal.ResetearIntentosFallidos(nombre);

                var fila = Fila(id);
                Assert.AreEqual("0", fila.IntentosFallidos);
                Assert.AreEqual("0", fila.CantidadBloqueos,
                    "La escala cuenta bloqueos consecutivos: un login exitoso la reinicia.");
                AssertDvYEspejoAlDia(id, "ResetearIntentosFallidos");
            }
            finally
            {
                dal.EliminarFisico(id);
            }
        }
    }
}
