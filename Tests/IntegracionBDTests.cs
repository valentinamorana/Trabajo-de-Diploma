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

        // Normalización 3FN: catálogo de categorías, medio de pago preferido por FK y origen del
        // mantenimiento, leídos por los DAL contra la base instalada por el script.
        [TestMethod]
        public void Normalizacion3FN_LosDalLeenLosCatalogos()
        {
            ExigirBase();
            CollectionAssert.Contains(new DAL.Prenda().ObtenerCategorias(), "Vestido");

            var dalCliente = new DAL.Cliente();
            Assert.IsTrue(dalCliente.ObtenerMediosPago().Exists(m => m.IdMedioPago == BE.MedioPago.IdEfectivo && m.Activo));
            Assert.IsTrue(dalCliente.ObtenerTodos().All(c => c.IdMedioPagoPreferido.HasValue && c.MetodoPago != null
                                                            && c.ClaveTraduccionMedioPago != null),
                "Los clientes de demo tienen su medio de pago preferido del catálogo (con nombre y clave por JOIN).");

            // La prenda de demo en limpieza la mandó el depósito a mano: no viene de una devolución.
            var mantenimientos = new DAL.MantenimientoPrenda().ObtenerTodos();
            Assert.IsTrue(mantenimientos.Exists(m => m.Actor == "deposito" && m.Origen == BE.OrigenMantenimiento.Manual));
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

        // PN04: la devolución completa Pedido.FechaDevolucion en la misma transacción que las prendas,
        // el DV del pedido queda al día y el pedido devuelto deja de ser el "pedido en curso" de la prenda.
        [TestMethod]
        public void Pedido_RegistrarDevolucion_CompletaLaFechaYDejaDeEstarEnCurso()
        {
            ExigirBase();
            var acceso = DAL.Acceso.GetInstance();
            var dt = acceso.Leer(
                "SELECT TOP 1 (SELECT TOP 1 IdCliente FROM Cliente ORDER BY IdCliente) AS IdCliente, " +
                "       (SELECT TOP 1 IdEmpleado FROM Empleado ORDER BY IdEmpleado) AS IdEmpleado, " +
                "       IdPrenda FROM Prenda WHERE Estado = 0 ORDER BY IdPrenda", null);
            if (dt.Rows.Count == 0) Assert.Inconclusive("La base de pruebas no tiene prendas disponibles.");
            int idCliente  = Convert.ToInt32(dt.Rows[0]["IdCliente"]);
            int idEmpleado = Convert.ToInt32(dt.Rows[0]["IdEmpleado"]);
            int idPrenda   = Convert.ToInt32(dt.Rows[0]["IdPrenda"]);

            var dal = new DAL.Pedido();
            int idPedido = 0;
            try
            {
                // Pedido Entregado hace 40 días con la prenda En uso a nombre del cliente (atrasado).
                var r = acceso.Leer(
                    "INSERT INTO Pedido (IdCliente, IdEmpleado, Estado, FechaPedido, FechaDespacho, FechaEntrega, DVH) " +
                    "VALUES (@Cli, @Emp, 2, DATEADD(DAY,-42,GETDATE()), DATEADD(DAY,-41,GETDATE()), DATEADD(DAY,-40,GETDATE()), 0); " +
                    "DECLARE @Id INT = SCOPE_IDENTITY(); " +
                    "INSERT INTO PedidoPrenda (IdPedido, IdPrenda, Confirmada) VALUES (@Id, @Prenda, 1); " +
                    "UPDATE Prenda SET Estado = 1, IdClienteActual = @Cli WHERE IdPrenda = @Prenda; " +
                    "SELECT @Id AS IdPedido;",
                    new[]
                    {
                        new SqlParameter("@Cli", idCliente), new SqlParameter("@Emp", idEmpleado),
                        new SqlParameter("@Prenda", idPrenda)
                    });
                idPedido = Convert.ToInt32(r.Rows[0]["IdPedido"]);
                dal.ActualizarDV(idPedido);

                var antes = dal.ObtenerPorId(idPedido);
                Assert.IsNull(antes.FechaDevolucion);
                Assert.IsTrue(BLL.Politicas.PoliticaCompraTacita.EstaAtrasado(antes, DateTime.Today));
                Assert.AreEqual(idPedido, new DAL.InspeccionDevolucion().ObtenerPedidoEnCurso(idPrenda)?.IdPedido);

                Assert.AreEqual(1, dal.RegistrarDevolucion(idPedido, idCliente, "deposito.test"));
                var abierto = new DAL.MantenimientoPrenda().ObtenerPorPrenda(idPrenda).Single(m => m.EstaAbierto);
                Assert.AreEqual(BE.OrigenMantenimiento.Devolucion, abierto.Origen,
                    "La devolución abre el mantenimiento con Origen = Devolución (lo que la lleva a la Inspección).");
                Assert.AreEqual("deposito.test", abierto.Actor, "El mantenimiento queda a nombre de quien registró la devolución.");

                var despues = dal.ObtenerPorId(idPedido);
                Assert.AreEqual(BE.EstadoPedido.Entregado, despues.Estado, "El estado no cambia.");
                Assert.IsTrue(despues.FechaDevolucion.HasValue, "La devolución completa la fecha.");
                Assert.IsFalse(BLL.Politicas.PoliticaCompraTacita.EstaAtrasado(despues, DateTime.Today));
                var fila = dal.ObtenerFilasDV().Single(f => f.Id == idPedido);
                Assert.AreEqual(Seguridad.CalculadorDV.Crear().CalcularDVH(fila.Campos), fila.DVHAlmacenado,
                    "El DVH del pedido incluye la fecha de devolución y quedó al día.");
                Assert.AreNotEqual(idPedido, new DAL.InspeccionDevolucion().ObtenerPedidoEnCurso(idPrenda)?.IdPedido,
                    "Un pedido ya devuelto no es el pedido en curso de la prenda.");
            }
            finally
            {
                // Deja la base de pruebas como estaba (la prenda vuelve a Disponible).
                if (idPedido != 0)
                {
                    acceso.Escribir(
                        "DELETE FROM MantenimientoPrenda WHERE IdPrenda = @Prenda AND FechaSalida IS NULL; " +
                        "UPDATE Prenda SET Estado = 0, IdClienteActual = NULL WHERE IdPrenda = @Prenda; " +
                        "DELETE FROM PedidoPrenda WHERE IdPedido = @Id; DELETE FROM Pedido WHERE IdPedido = @Id;",
                        new[] { new SqlParameter("@Prenda", idPrenda), new SqlParameter("@Id", idPedido) });
                    dal.ActualizarDV(idPedido);
                }
            }
        }

        // Empleado ↔ Usuario: al modificar el usuario, su Empleado toma Nombre/Apellido/Email; un dato
        // vacío no pisa el del empleado y el DVH queda al día.
        [TestMethod]
        public void Empleado_SincronizarDatosPersonales_CopiaLosDatosSinPisarVaciosYActualizaElDV()
        {
            ExigirBase();
            var acceso = DAL.Acceso.GetInstance();
            var dt = acceso.Leer(
                "SELECT TOP 1 IdEmpleado, IdUsuario, Nombre, Apellido, Email FROM Empleado " +
                "WHERE IdUsuario IS NOT NULL ORDER BY IdEmpleado", null);
            if (dt.Rows.Count == 0) Assert.Inconclusive("La base de pruebas no tiene empleados vinculados a un usuario.");
            int idEmpleado = Convert.ToInt32(dt.Rows[0]["IdEmpleado"]);
            int idUsuario  = Convert.ToInt32(dt.Rows[0]["IdUsuario"]);
            object nombre   = dt.Rows[0]["Nombre"];
            object apellido = dt.Rows[0]["Apellido"];
            object email    = dt.Rows[0]["Email"];

            var dal = new DAL.Empleado();
            try
            {
                Assert.AreEqual(0, dal.SincronizarDatosPersonales(-1, "X", "Y", "z@z.com"), "Sin empleado vinculado no hace nada.");
                Assert.AreEqual(0, dal.SincronizarDatosPersonales(idUsuario, null, " ", null), "Sin datos no hace nada.");

                Assert.AreEqual(idEmpleado, dal.SincronizarDatosPersonales(idUsuario, " NombreSync ", null, "sync@test.com"));
                var e = dal.ObtenerPorId(idEmpleado);
                Assert.AreEqual("NombreSync", e.Nombre);
                Assert.AreEqual(apellido.ToString(), e.Apellido, "El apellido vacío del usuario no pisa el del empleado.");
                Assert.AreEqual("sync@test.com", e.Email);
                Assert.IsTrue(DvhCoincide(DAL.Empleado.DV_Tabla, DAL.Empleado.DV_Pk, DAL.Empleado.DV_Columnas, idEmpleado),
                    "El DVH del empleado se recalculó tras la sincronización.");
            }
            finally
            {
                acceso.Escribir(
                    "UPDATE Empleado SET Nombre = @N, Apellido = @A, Email = @E WHERE IdEmpleado = @Id",
                    new[]
                    {
                        new SqlParameter("@N", nombre), new SqlParameter("@A", apellido),
                        new SqlParameter("@E", email),  new SqlParameter("@Id", idEmpleado)
                    });
                dal.ActualizarDV(idEmpleado);
            }
        }
    }
}
