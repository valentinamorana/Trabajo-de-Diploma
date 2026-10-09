using System;
using System.Data.SqlClient;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Pruebas de integración contra una base REAL instalada con BD/00_Instalacion_Completa.sql:
    /// la base de pruebas WardrobeFlowDB_Tests (cadena de conexión de Tests/App.config). Todo el resto
    /// de la suite usa dobles; estas verifican que el SQL de los DAL coincide con el esquema y que el
    /// dígito verificador se recalcula dentro de la misma transacción del negocio.
    ///
    /// Si la base no existe, las pruebas quedan Inconclusive (no fallan): para correrlas, instalar el
    /// script en WardrobeFlowDB_Tests (por ejemplo con sqlcmd y el nombre de base reemplazado).
    /// Escriben datos: nunca apuntar esta cadena a la base real.
    /// </summary>
    [TestClass]
    public class IntegracionBDTests
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

        private static bool DvhCoincide(string tabla, string pk, string[] columnas, int id)
        {
            var fila = new DAL.DigitoVerificador().ObtenerFilas(tabla, pk, columnas).Single(f => f.Id == id);
            return fila.DVHAlmacenado == Seguridad.CalculadorDV.Crear().CalcularDVH(fila.Campos);
        }

        [TestMethod]
        public void EsquemaYDal_LasConsultasPrincipalesCorren()
        {
            ExigirBase();
            Assert.IsTrue(new DAL.Cliente().ObtenerTodos().Count > 0, "La instalación siembra clientes de demo.");
            new DAL.Prenda().ObtenerTodos();
            new DAL.Pedido().ObtenerTodos();
            new DAL.Contratacion().ObtenerPendientesDePago();
            new DAL.Promocion().ObtenerTodas();
            new DAL.Usuario().ObtenerTodos();
            new DAL.CargoPrenda().ObtenerPendientesPorCliente(1);
            new DAL.InspeccionDevolucion().ObtenerPedidoEnCurso(1);
            // Compensación de un cobro con cargos (los parámetros con estado 0 tienen que llegar como valor).
            new DAL.Contratacion().ReabrirPago(-1, new System.Collections.Generic.List<int> { -1 });
        }

        [TestMethod]
        public void Cliente_EscrituraEnTransaccion_DejaElDvhAlDiaAntesDelCommit()
        {
            ExigirBase();
            var dal = new DAL.Cliente();
            var cliente = dal.ObtenerTodos().First();

            dal.EjecutarTransaccion((cn, tx) => dal.SumarCreditoEnTx(cn, tx, cliente.IdCliente, 1m));

            Assert.IsTrue(DvhCoincide(DAL.Cliente.DV_Tabla, DAL.Cliente.DV_Pk, DAL.Cliente.DV_Columnas, cliente.IdCliente),
                "El DVH de la fila escrita se recalcula en la misma transacción.");
            var dvv = new DAL.DigitoVerificador().ObtenerDVV(DAL.Cliente.DV_Tabla);
            Assert.IsTrue(dvv.HasValue, "El DVV de la tabla también se guarda.");

            dal.EjecutarTransaccion((cn, tx) => dal.ConsumirCreditoEnTx(cn, tx, cliente.IdCliente, 1m));   // deja el saldo como estaba
        }

        [TestMethod]
        public void Cliente_SiLaTransaccionFalla_NoQuedaNada()
        {
            ExigirBase();
            var dal = new DAL.Cliente();
            var antes = dal.ObtenerTodos().First();

            try
            {
                dal.EjecutarTransaccion((cn, tx) =>
                {
                    dal.SumarCreditoEnTx(cn, tx, antes.IdCliente, 500m);
                    throw new InvalidOperationException("falla a propósito");
                });
                Assert.Fail("Debía propagar la falla.");
            }
            catch (InvalidOperationException) { }

            var despues = dal.ObtenerPorId(antes.IdCliente);
            Assert.AreEqual(antes.DescuentoProximoCobro, despues.DescuentoProximoCobro, "Rollback del crédito.");
        }
    }
}
