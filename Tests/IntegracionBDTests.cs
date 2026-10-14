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
            new DAL.Promocion().ObtenerDecisionesDelPlan(1, DateTime.Today.AddYears(-1), DateTime.Today.AddDays(1));   // PN03: Evaluar resultado
            new DAL.Usuario().ObtenerTodos();
            new DAL.CargoPrenda().ObtenerPendientesPorCliente(1);
            new DAL.InspeccionDevolucion().ObtenerPedidoEnCurso(1);
            // Compensación de un cobro con cargos (los parámetros con estado 0 tienen que llegar como valor).
            Assert.IsFalse(new DAL.Contratacion().ReabrirPago(-1, new System.Collections.Generic.List<int> { -1 }),
                "Una contratación que no está Pagada no se reabre.");
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

        // PN03: el beneficio que calcula BE (redondeo desde el cero) pasa el CHECK de la base, que lo
        // compara con ROUND(ingreso × tasa / 100, 2) — incluso en un punto medio (1,15 × 50 % = 0,575).
        [TestMethod]
        public void PN03_SugerenciaConIngresoYTasa_PasaElCheckDelBeneficio()
        {
            ExigirBase();
            var plan = new DAL.PlanSuscripcion().ObtenerTodos().First();
            var dal = new DAL.SugerenciaPromocion();
            var s = new BE.SugerenciaPromocion
            {
                IdPlan = plan.IdPlan, Motivo = "Prueba de integración: punto medio del redondeo",
                TipoDescuentoSugerido = BE.TipoDescuento.Porcentaje, OrigenMetrica = BE.OrigenMetrica.Manual,
                IngresoMensualEnRiesgo = 1.15m, TasaRetencion = 50m,
                BeneficioEstimado = BE.SugerenciaPromocion.CalcularBeneficio(1.15m, 50m), FechaAlta = DateTime.Now
            };
            Assert.AreEqual(0.58m, s.BeneficioEstimado);
            int id = dal.Alta(s);
            var acceso = DAL.Acceso.GetInstance();
            try
            {
                var leida = dal.ObtenerPorId(id);
                Assert.AreEqual(1.15m, leida.IngresoMensualEnRiesgo);
                Assert.AreEqual(1.15m, leida.IngresoEnRiesgo());
                Assert.AreEqual(50m, leida.TasaRetencion);

                // Un beneficio que no coincide con ingreso × tasa no entra.
                s.BeneficioEstimado = 0.57m;
                int idDesfasado = 0;
                try { idDesfasado = dal.Alta(s); }
                catch (Exception ex)
                {
                    StringAssert.Contains(ex.InnerException?.Message ?? ex.Message, "CHK_SugerenciaPromocion_Beneficio");
                }
                if (idDesfasado != 0)
                {
                    acceso.Escribir("DELETE FROM SugerenciaPromocion WHERE IdSugerencia = @Id",
                        new[] { new SqlParameter("@Id", idDesfasado) });
                    Assert.Fail("El CHECK CHK_SugerenciaPromocion_Beneficio debía rechazar el desfasaje.");
                }
            }
            finally
            {
                acceso.Escribir("DELETE FROM SugerenciaPromocion WHERE IdSugerencia = @Id",
                    new[] { new SqlParameter("@Id", id) });
            }
        }

        // PN04: con los datos reales, una prenda que entró a limpieza en el depósito se libera desde
        // Prendas; una que volvió de un cliente no ofrece ningún destino manual (sale por la Inspección).
        [TestMethod]
        public void PN04_TransicionesManuales_DistinguenLimpiezaDelDepositoDeDevolucion()
        {
            ExigirBase();
            var bll = new BLL.Prenda();
            var abiertos = new DAL.MantenimientoPrenda().ObtenerTodos().Where(m => m.EstaAbierto).ToList();
            var enLimpieza = new DAL.Prenda().ObtenerTodos().Where(p => p.Estado == BE.EstadoPrenda.EnLimpieza).ToList();

            var delDeposito = enLimpieza.FirstOrDefault(p => abiertos.Any(m => m.IdPrenda == p.IdPrenda && !m.VieneDeDevolucion));
            if (delDeposito == null) Assert.Inconclusive("La base de pruebas no tiene una prenda en limpieza del depósito.");
            CollectionAssert.Contains(bll.ObtenerTransicionesManuales(delDeposito), BE.EstadoPrenda.Disponible);

            foreach (var devuelta in enLimpieza.Where(p => abiertos.Any(m => m.IdPrenda == p.IdPrenda && m.VieneDeDevolucion)))
                Assert.AreEqual(0, bll.ObtenerTransicionesManuales(devuelta).Count,
                    $"La prenda devuelta {devuelta.IdPrenda} solo sale de limpieza por la Inspección de Devolución.");
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

        // C5: dos cobros que leyeron el mismo crédito no lo aplican los dos. Si el saldo real ya no lo
        // cubre, ConsumirCreditoEnTx lanza y se revierte toda la transacción (también lo que se escribió antes).
        [TestMethod]
        public void Cliente_ConsumirCreditoSinSaldoSuficiente_LanzaYRevierteTodo()
        {
            ExigirBase();
            var dal = new DAL.Cliente();
            var antes = dal.ObtenerTodos().First();

            try
            {
                dal.EjecutarTransaccion((cn, tx) =>
                {
                    dal.SumarCreditoEnTx(cn, tx, antes.IdCliente, 1m);
                    dal.ConsumirCreditoEnTx(cn, tx, antes.IdCliente, antes.DescuentoProximoCobro + 2m);
                });
                Assert.Fail("Debía rechazar el consumo de un crédito que ya no está.");
            }
            catch (BE.AppException ex)
            {
                Assert.AreEqual("err.bll.cobro.credito_concurrente", ex.Clave);
            }

            Assert.AreEqual(antes.DescuentoProximoCobro, dal.ObtenerPorId(antes.IdCliente).DescuentoProximoCobro,
                "Rollback: ni la suma ni el consumo quedaron aplicados.");
            Assert.IsTrue(DvhCoincide(DAL.Cliente.DV_Tabla, DAL.Cliente.DV_Pk, DAL.Cliente.DV_Columnas, antes.IdCliente));
        }

        // C1 + C2: alta, cobro y reapertura de una contratación dejan el DVH al día (recalculado en la
        // misma transacción); la reapertura exige que siga Pagada y la segunda vez no toca nada.
        [TestMethod]
        public void Contratacion_ReabrirPago_SoloSiEstaPagadaYConElDvAlDia()
        {
            ExigirBase();
            var acceso = DAL.Acceso.GetInstance();
            var dt = acceso.Leer(
                "SELECT (SELECT TOP 1 IdCliente FROM Cliente c WHERE Activo = 1 AND NOT EXISTS " +
                "          (SELECT 1 FROM Contratacion x WHERE x.IdCliente = c.IdCliente AND x.Estado = 0) ORDER BY IdCliente DESC) AS IdCliente, " +
                "       (SELECT TOP 1 IdPlan FROM PlanSuscripcion ORDER BY IdPlan) AS IdPlan, " +
                "       (SELECT TOP 1 IdEmpleado FROM Empleado ORDER BY IdEmpleado) AS IdEmpleado, " +
                "       (SELECT TOP 1 IdMedioPago FROM MedioPago ORDER BY IdMedioPago) AS IdMedioPago", null);
            var r = dt.Rows[0];
            if (r["IdCliente"] == DBNull.Value || r["IdPlan"] == DBNull.Value || r["IdEmpleado"] == DBNull.Value || r["IdMedioPago"] == DBNull.Value)
                Assert.Inconclusive("La base de pruebas no tiene los datos de demo necesarios.");
            int idEmpleado = Convert.ToInt32(r["IdEmpleado"]);

            var dal = new DAL.Contratacion();
            int id = 0;
            try
            {
                id = dal.Alta(new BE.Contratacion
                {
                    IdCliente = Convert.ToInt32(r["IdCliente"]), IdPlan = Convert.ToInt32(r["IdPlan"]), IdVendedor = idEmpleado,
                    Modalidad = BE.Builders.ModalidadCobro.Mensual, FechaAlta = DateTime.Now
                });
                Assert.IsTrue(DvhCoincide(DAL.Contratacion.DV_Tabla, DAL.Contratacion.DV_Pk, DAL.Contratacion.DV_Columnas, id));
                Assert.IsFalse(dal.ReabrirPago(id), "Pendiente de pago: no hay cobro que reabrir.");

                Assert.IsTrue(dal.ConfirmarCobro(id, idEmpleado, Convert.ToInt32(r["IdMedioPago"]),
                    "TEST-REABRIR-" + Guid.NewGuid().ToString("N").Substring(0, 12), 100m, 0m, null));
                Assert.IsTrue(dal.ReabrirPago(id));
                var reabierta = dal.ObtenerPorId(id);
                Assert.AreEqual(BE.EstadoContratacion.PendientePago, reabierta.Estado);
                Assert.IsNull(reabierta.NumeroComprobante);
                Assert.IsTrue(DvhCoincide(DAL.Contratacion.DV_Tabla, DAL.Contratacion.DV_Pk, DAL.Contratacion.DV_Columnas, id),
                    "El DVH de la reapertura se recalcula en la misma transacción.");
                Assert.IsFalse(dal.ReabrirPago(id), "Ya no está Pagada: la segunda vez no reabre nada.");
            }
            finally
            {
                if (id != 0)
                {
                    acceso.Escribir("DELETE FROM Contratacion WHERE IdContratacion = @Id", new[] { new SqlParameter("@Id", id) });
                    new DAL.DigitoVerificador().ActualizarFila(DAL.Contratacion.DV_Tabla, DAL.Contratacion.DV_Pk,
                                                               DAL.Contratacion.DV_Columnas, id);   // solo cambia el DVV
                }
            }
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
                dal.RecalcularDV();   // la fila se insertó a mano: se acepta como legítima

                var antes = dal.ObtenerPorId(idPedido);
                Assert.IsNull(antes.FechaDevolucion);
                Assert.IsTrue(BLL.Politicas.PoliticaCompraTacita.EstaAtrasado(antes, DateTime.Today));
                Assert.AreEqual(idPedido, new DAL.InspeccionDevolucion().ObtenerPedidoEnCurso(idPrenda)?.IdPedido);

                Assert.AreEqual(1, dal.RegistrarDevolucion(idPedido, idCliente, "deposito.test",
                    n => new System.Collections.Generic.List<BE.PedidoHistorial>
                    {
                        new BE.PedidoHistorial { Fecha = DateTime.Now, Accion = "DEVOLUCION", Campo = "Prendas",
                                                 ValorAnterior = "EnUso", ValorNuevo = "EnLimpieza (" + n + " prenda(s))" }
                    }));
                var historial = new DAL.PedidoHistorial().ObtenerPorOperacion(idPedido, 1);
                Assert.AreEqual(1, historial.Count, "El historial se asienta en la misma transacción de la devolución.");
                Assert.AreEqual("EnLimpieza (1 prenda(s))", historial[0].ValorNuevo);
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
                        "DELETE FROM PedidoHistorial WHERE IdPedido = @Id; " +
                        "DELETE FROM PedidoPrenda WHERE IdPedido = @Id; DELETE FROM Pedido WHERE IdPedido = @Id;",
                        new[] { new SqlParameter("@Prenda", idPrenda), new SqlParameter("@Id", idPedido) });
                    dal.RecalcularDV();
                }
            }
        }

        // C1 + C3: el cambio de estado, su historial y el DV del pedido van en UNA transacción. Si el
        // historial no se puede asentar, el cambio se revierte (antes quedaba hecho, sin historial, y el
        // DV se recalculaba aparte tragándose el error). El IdOperacion se numera dentro de la
        // transacción y UQ_PedidoHistorial_Operacion impide repetirlo.
        [TestMethod]
        public void Pedido_CambioDeEstado_HistorialYDVEnLaMismaTransaccion()
        {
            ExigirBase();
            var acceso = DAL.Acceso.GetInstance();
            // Un cliente sin pedido en curso (UX_Pedido_ClienteActivo) y un empleado cualquiera.
            var dt = acceso.Leer(
                "SELECT TOP 1 c.IdCliente, (SELECT TOP 1 IdEmpleado FROM Empleado ORDER BY IdEmpleado) AS IdEmpleado " +
                "FROM Cliente c WHERE NOT EXISTS (SELECT 1 FROM Pedido p WHERE p.IdCliente = c.IdCliente " +
                "AND p.Estado IN (0, 1, 4, 5, 6)) ORDER BY c.IdCliente DESC", null);
            if (dt.Rows.Count == 0) Assert.Inconclusive("La base de pruebas no tiene un cliente sin pedido en curso.");

            var dal = new DAL.Pedido();
            int idPedido = 0;
            System.Collections.Generic.List<BE.PedidoHistorial> Evento(string accion, string campo, string anterior, string nuevo) =>
                new System.Collections.Generic.List<BE.PedidoHistorial>
                {
                    new BE.PedidoHistorial { Fecha = DateTime.Now, Accion = accion, Campo = campo, ValorAnterior = anterior, ValorNuevo = nuevo }
                };
            try
            {
                idPedido = dal.AltaSinReserva(new BE.Pedido
                {
                    IdCliente   = Convert.ToInt32(dt.Rows[0]["IdCliente"]),
                    IdEmpleado  = Convert.ToInt32(dt.Rows[0]["IdEmpleado"]),
                    Estado      = BE.EstadoPedido.Despachado,
                    FechaPedido = DateTime.Now
                }, Evento("ENVIAR_CONTROL", "Estado", null, "Despachado"));
                Assert.AreEqual(1, new DAL.PedidoHistorial().ObtenerUltimaIdOperacion(idPedido), "El alta asienta la operación #1.");
                AssertDvhPedidoAlDia(dal, idPedido);

                // Historial inválido (Campo NOT NULL): falla el INSERT del historial → se revierte el cambio de estado.
                try
                {
                    dal.MarcarEntregado(idPedido, Evento("ENTREGAR", null, "Despachado", "Entregado"));
                    Assert.Fail("El historial inválido debía hacer fallar la operación.");
                }
                catch (SqlException) { }
                Assert.AreEqual(BE.EstadoPedido.Despachado, dal.ObtenerPorId(idPedido).Estado,
                    "Sin historial no queda el cambio de estado: rollback de toda la transacción.");
                Assert.AreEqual(1, new DAL.PedidoHistorial().ObtenerUltimaIdOperacion(idPedido));
                AssertDvhPedidoAlDia(dal, idPedido);

                dal.MarcarEntregado(idPedido, Evento("ENTREGAR", "Estado", "Despachado", "Entregado"));
                Assert.AreEqual(BE.EstadoPedido.Entregado, dal.ObtenerPorId(idPedido).Estado);
                var op2 = new DAL.PedidoHistorial().ObtenerPorOperacion(idPedido, 2);
                Assert.AreEqual(1, op2.Count, "La entrega es la operación #2 (numerada dentro de la transacción).");
                Assert.AreEqual("ENTREGAR", op2[0].Accion);
                AssertDvhPedidoAlDia(dal, idPedido);

                // UQ_PedidoHistorial_Operacion (sección 20k): no puede haber dos filas del mismo campo en una operación.
                try
                {
                    acceso.Escribir(
                        "INSERT INTO PedidoHistorial (IdPedido, IdOperacion, Fecha, Accion, Campo) " +
                        "VALUES (@Id, 2, GETDATE(), 'ENTREGAR', 'Estado')",
                        new[] { new SqlParameter("@Id", idPedido) });
                    Assert.Fail("La restricción única debía rechazar el IdOperacion repetido.");
                }
                catch (Exception ex) when (ex is SqlException || ex.InnerException is SqlException) { }
            }
            finally
            {
                if (idPedido != 0)
                {
                    acceso.Escribir(
                        "DELETE FROM PedidoHistorial WHERE IdPedido = @Id; " +
                        "DELETE FROM PedidoPrenda WHERE IdPedido = @Id; DELETE FROM Pedido WHERE IdPedido = @Id;",
                        new[] { new SqlParameter("@Id", idPedido) });
                    dal.RecalcularDV();
                }
            }
        }

        private static void AssertDvhPedidoAlDia(DAL.Pedido dal, int idPedido)
        {
            var fila = dal.ObtenerFilasDV().Single(f => f.Id == idPedido);
            Assert.AreEqual(Seguridad.CalculadorDV.Crear().CalcularDVH(fila.Campos), fila.DVHAlmacenado,
                "El DVH del pedido quedó al día dentro de la misma transacción.");
        }

        // PN03 — la solicitud de baja no cambia el estado; atenderla sin baja la cierra; dar de baja la
        // promoción cierra en la misma transacción las solicitudes pendientes (DioDeBaja = 1).
        [TestMethod]
        public void Promocion_SolicitarAtenderYDarDeBaja_RespetaElEsquemaNuevo()
        {
            ExigirBase();
            var acceso = DAL.Acceso.GetInstance();
            var dt = acceso.Leer(
                "SELECT (SELECT TOP 1 IdPlan FROM PlanSuscripcion ORDER BY IdPlan) AS IdPlan, " +
                "       (SELECT TOP 1 IdUsuario FROM Usuario ORDER BY IdUsuario) AS IdUsuario", null);
            if (dt.Rows[0]["IdPlan"] == DBNull.Value || dt.Rows[0]["IdUsuario"] == DBNull.Value)
                Assert.Inconclusive("La base de pruebas no tiene planes o usuarios.");
            int idPlan = Convert.ToInt32(dt.Rows[0]["IdPlan"]);
            int idUsuario = Convert.ToInt32(dt.Rows[0]["IdUsuario"]);

            var dal = new DAL.Promocion();
            int idPromo = 0;
            try
            {
                idPromo = dal.Alta(new BE.Promocion
                {
                    Nombre = "PROMO Integracion Baja", TipoDescuento = BE.TipoDescuento.Porcentaje, Valor = 5,
                    FechaInicio = DateTime.Today, FechaFin = DateTime.Today.AddDays(10), IdPlan = idPlan,
                    IdUsuarioAlta = idUsuario, FechaAlta = DateTime.Now
                }, new BE.PromocionHistorial { EstadoNuevo = BE.EstadoPromocion.EnRevisionContable, IdUsuario = idUsuario, Fecha = DateTime.Now });
                acceso.Escribir("UPDATE Promocion SET Estado = 1 WHERE IdPromocion = @Id", new[] { new SqlParameter("@Id", idPromo) });

                // Solicitar: queda Pendiente y la promoción sigue Vigente; no se admite una segunda pendiente.
                var solicitud = new BE.SolicitudBajaPromocion
                {
                    IdPromocion = idPromo, IdUsuarioSolicita = idUsuario, Motivo = "No rinde", FechaSolicitud = DateTime.Now
                };
                int idSol = dal.SolicitarBaja(solicitud);
                Assert.IsTrue(idSol > 0);
                Assert.AreEqual(0, dal.SolicitarBaja(solicitud), "Una sola solicitud pendiente por promoción.");
                var promo = dal.ObtenerPorId(idPromo);
                Assert.AreEqual(BE.EstadoPromocion.Vigente, promo.Estado);
                Assert.AreEqual("No rinde", promo.MotivoBaja);
                Assert.IsTrue(dal.ObtenerSolicitudesBajaPendientes().Exists(s => s.IdSolicitud == idSol && s.NombrePromocion == "PROMO Integracion Baja"));

                // Atender sin baja: Atendida con respuesta; la promoción sigue Vigente; no se atiende dos veces.
                var atendida = new BE.SolicitudBajaPromocion
                {
                    IdSolicitud = idSol, IdPromocion = idPromo, Estado = BE.EstadoSolicitudBaja.Atendida, Respuesta = "Sigue",
                    IdUsuarioResuelve = idUsuario, FechaResolucion = DateTime.Now
                };
                Assert.IsTrue(dal.AtenderSolicitud(atendida));
                Assert.IsFalse(dal.AtenderSolicitud(atendida), "Ya no está pendiente.");
                var s1 = dal.ObtenerSolicitudesBaja(idPromo).Single();
                Assert.AreEqual(BE.EstadoSolicitudBaja.Atendida, s1.Estado);
                Assert.IsFalse(s1.DioDeBaja);
                Assert.AreEqual("Sigue", s1.Respuesta);
                Assert.IsNull(dal.ObtenerPorId(idPromo).MotivoBaja);

                // Nueva solicitud y Dar de baja: la cierra con DioDeBaja = 1 y escribe el historial.
                int idSol2 = dal.SolicitarBaja(solicitud);
                Assert.IsTrue(idSol2 > 0);
                Assert.IsTrue(dal.DarDeBaja(idPromo, BE.EstadoPromocion.Vigente, new BE.PromocionHistorial
                {
                    IdPromocion = idPromo, EstadoAnterior = BE.EstadoPromocion.Vigente, EstadoNuevo = BE.EstadoPromocion.DadaDeBaja,
                    IdUsuario = idUsuario, Fecha = DateTime.Now, Observacion = "Fin de campaña"
                }, "De acuerdo"));
                Assert.IsFalse(dal.DarDeBaja(idPromo, BE.EstadoPromocion.Vigente, new BE.PromocionHistorial
                {
                    IdPromocion = idPromo, EstadoAnterior = BE.EstadoPromocion.Vigente, EstadoNuevo = BE.EstadoPromocion.DadaDeBaja,
                    IdUsuario = idUsuario, Fecha = DateTime.Now
                }, "x"), "Claim atómico: ya no está Vigente.");
                Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, dal.ObtenerPorId(idPromo).Estado);
                var s2 = dal.ObtenerSolicitudesBaja(idPromo).Single(s => s.IdSolicitud == idSol2);
                Assert.IsTrue(s2.DioDeBaja);
                Assert.AreEqual("De acuerdo", s2.Respuesta);
                Assert.AreEqual(BE.EstadoSolicitudBaja.Atendida, s2.Estado);
                Assert.AreEqual(BE.EstadoPromocion.DadaDeBaja, dal.ObtenerHistorial(idPromo).Last().EstadoNuevo);
                Assert.AreEqual(0, dal.SolicitarBaja(solicitud), "Una promoción dada de baja no admite solicitudes.");
            }
            finally
            {
                if (idPromo != 0)
                    acceso.Escribir(
                        "DELETE FROM SolicitudBajaPromocion WHERE IdPromocion = @Id; DELETE FROM PromocionHistorial WHERE IdPromocion = @Id; " +
                        "DELETE FROM Promocion WHERE IdPromocion = @Id;", new[] { new SqlParameter("@Id", idPromo) });
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
