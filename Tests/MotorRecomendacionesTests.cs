using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Inteligencia;
using BLL.Inteligencia.Reglas;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// PN03 «Recomendar / decidir» — cada regla del motor (Strategy) por separado: cuándo se dispara,
    /// cuándo no, la fórmula del impacto y la elección entre la opción principal y las alternativas.
    /// El contexto se arma a mano: las reglas son puras, no tocan la base.
    /// </summary>
    [TestClass]
    public class MotorRecomendacionesTests
    {
        private static readonly DateTime Hoy = new DateTime(2026, 10, 20);

        private static BE.ContextoComercial Ctx() => new BE.ContextoComercial
        {
            Hoy = Hoy, Desde = Hoy.AddDays(-89), Hasta = Hoy,
            Umbrales = CatalogoUmbrales.ValoresPorDefecto(),
            TicketPromedio = 1000m
        };

        private static BE.MetricaPlanComercial Plan(int id, decimal precio, int activos, int bajas, decimal? churnAnterior = null,
                                                    int enRiesgo = 0, decimal? retencion = null, int medidas = 0, bool vigente = false, int limite = 3)
            => new BE.MetricaPlanComercial
            {
                IdPlan = id, Nombre = "Plan" + id, Precio = precio, LimitePrendas = limite,
                ClientesActivos = activos, ClientesInicio = activos, Bajas = bajas,
                Churn = CalculoKpi.TasaBajas(bajas, activos), ChurnAnterior = churnAnterior,
                ClientesEnRiesgo = enRiesgo, RetencionHistorica = retencion, PromocionesMedidas = medidas, TienePromocionVigente = vigente
            };

        private static List<BE.Recomendacion> Evaluar(IReglaRecomendacion regla, BE.ContextoComercial ctx) => regla.Evaluar(ctx).ToList();

        // ══ ChurnPlan ════════════════════════════════════════════════════════════

        [TestMethod]
        public void Churn_ConRetencionHistoricaAlta_PropuestaDePromocionConDescuentoYDuracionCalculados()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 20, 2, churnAnterior: 4m, enRiesgo: 10, retencion: 60m, medidas: 2));
            ctx.Clientes.Add(new BE.ClienteComercial { IdCliente = 7, Nombre = "Ana", Apellido = "Gómez", IdPlan = 1 });   // a quién contactar
            var rec = Evaluar(new ReglaChurnPlan(), ctx).Single();

            // 10 % de bajas con umbral 5 % → gravedad 2 → descuento 10 × 2 = 20 %, 60 días.
            Assert.AreEqual(BE.AccionRecomendacion.SugerirPromocion, rec.Principal.Accion);
            Assert.AreEqual(20m, rec.Principal.Parametros.DescuentoPorcentaje);
            Assert.AreEqual(60, rec.Principal.Parametros.DuracionDias);
            Assert.AreEqual(60m, rec.Principal.Parametros.TasaRetencion);
            // 10 × 1000 × 60 % × 6 − 20 × 1000 × 20 % × 2 = 36000 − 8000.
            Assert.AreEqual(28000m, rec.ImpactoEstimado);
            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, rec.Prioridad);
            Assert.AreEqual(BE.NivelConfianza.Alta, rec.Confianza);
            Assert.AreEqual(2, rec.Alternativas.Count, "Promoción más suave y contacto sin descuento.");
            Assert.IsTrue(rec.Alternativas.All(a => a.ImpactoEstimado <= rec.ImpactoEstimado));
            Assert.AreEqual("ChurnPlan:1", rec.Clave);
        }

        [TestMethod]
        public void Churn_PromocionNoRentableYSinContactos_NoOfreceUnaOpcionQueNoHagaNada()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 100, 10, enRiesgo: 12));
            var rec = Evaluar(new ReglaChurnPlan(), ctx).Single();
            // Sin clientes del plan en el contexto no hay a quién contactar: solo quedan las dos promociones
            // (con 100 clientes recibiendo el descuento las dos pierden) y el motor lo dice.
            Assert.IsTrue(rec.Opciones().All(o => o.Accion == BE.AccionRecomendacion.SugerirPromocion));
            Assert.AreEqual(1, rec.Alternativas.Count);
            Assert.IsTrue(rec.ImpactoEstimado < 0);
            Assert.AreEqual(BE.PrioridadRecomendacion.Baja, rec.Prioridad);
            Assert.AreEqual("rec.churn.motivo.norentable", rec.Motivo.Clave);
        }

        [TestMethod]
        public void Churn_PromocionNoRentableConContactos_GanaLaCampaniaQueExportaLosContactos()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 100, 10, enRiesgo: 12));
            ctx.Clientes.Add(new BE.ClienteComercial { IdCliente = 7, Nombre = "Ana", Apellido = "Gómez", IdPlan = 1 });
            var rec = Evaluar(new ReglaChurnPlan(), ctx).Single();
            // El contacto (10 % de retención, sin costo) gana: 12 × 10 % × 1000 × 6 = 7200.
            Assert.AreEqual(BE.AccionRecomendacion.ExportarContactos, rec.Principal.Accion);
            Assert.AreEqual(7200m, rec.ImpactoEstimado);
            Assert.IsTrue(rec.Alternativas.Any(a => a.Accion == BE.AccionRecomendacion.SugerirPromocion && a.ImpactoEstimado < 0));
        }

        [TestMethod]
        public void Churn_ContactoConClientesDelPlan_SeEjecutaExportandoLosContactos()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 100, 10, enRiesgo: 12));
            ctx.Clientes.Add(new BE.ClienteComercial { IdCliente = 7, Nombre = "Ana", Apellido = "Gómez", Email = "ana@x.com", IdPlan = 1, FechaVencimiento = Hoy.AddDays(20) });
            ctx.Clientes.Add(new BE.ClienteComercial { IdCliente = 8, Nombre = "Otro", Apellido = "Plan", IdPlan = 2 });
            var rec = Evaluar(new ReglaChurnPlan(), ctx).Single();
            Assert.AreEqual(BE.AccionRecomendacion.ExportarContactos, rec.Principal.Accion, "La campaña de contacto tiene un botón que la ejecuta.");
            Assert.AreEqual(7, rec.Principal.Parametros.Contactos.Single().IdCliente, "Solo los clientes del plan.");
            Assert.AreEqual(20, rec.Principal.Parametros.Contactos.Single().DiasRestantes);
        }

        [TestMethod]
        public void Churn_BajoElUmbralYSinSuba_NoRecomienda()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 100, 3, churnAnterior: 2m, enRiesgo: 5));
            Assert.AreEqual(0, Evaluar(new ReglaChurnPlan(), ctx).Count);
        }

        [TestMethod]
        public void Churn_SubaContraElPeriodoAnterior_RecomiendaConMotivoDeSuba()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 100, 4, churnAnterior: 1m, enRiesgo: 5));
            var rec = Evaluar(new ReglaChurnPlan(), ctx).Single();
            Assert.IsTrue(rec.Motivo.Clave == "rec.churn.motivo.suba" || rec.Motivo.Clave == "rec.churn.motivo.norentable");
            Assert.AreEqual(5m, rec.Opciones().First(o => o.Accion == BE.AccionRecomendacion.SugerirPromocion
                                                         && o.Descripcion.Clave == "rec.churn.opcion.suave").Parametros.DescuentoPorcentaje);
        }

        [TestMethod]
        public void Churn_PlanConPromocionVigenteOSinClientes_NoRecomienda()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 20, 5, enRiesgo: 5, vigente: true));
            ctx.Planes.Add(Plan(2, 1000m, 0, 0));      // sin clientes al inicio: % de bajas null
            Assert.AreEqual(0, Evaluar(new ReglaChurnPlan(), ctx).Count);
        }

        [TestMethod]
        public void Churn_UmbralConfigurable_CambiaLaDecision()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 100, 6, enRiesgo: 5));
            Assert.AreEqual(1, Evaluar(new ReglaChurnPlan(), ctx).Count);
            ctx.Umbrales[CatalogoUmbrales.ChurnAltoPct] = 10m;
            Assert.AreEqual(0, Evaluar(new ReglaChurnPlan(), ctx).Count, "Con umbral 10 %, un 6 % de bajas no alcanza.");
        }

        // ══ Renovaciones ═════════════════════════════════════════════════════════

        private static BE.ClienteVencimiento Vence(int id, int dias, decimal precio) => new BE.ClienteVencimiento
        {
            IdCliente = id, Nombre = "Cliente" + id, NombrePlan = "Básico", Precio = precio,
            FechaVencimiento = Hoy.AddDays(dias), DiasRestantes = dias
        };

        [TestMethod]
        public void Renovaciones_VencidasYPorVencer_CampaniaQueExportaLosContactosConRecupero()
        {
            var ctx = Ctx();
            ctx.Vencimientos.AddRange(new[] { Vence(1, -3, 1000m), Vence(2, 5, 2000m), Vence(3, 20, 5000m), Vence(4, -40, 5000m) });
            var rec = Evaluar(new ReglaRenovaciones(), ctx).Single();
            Assert.AreEqual(BE.AccionRecomendacion.ExportarContactos, rec.Principal.Accion, "Aceptarla exporta los contactos (y después se mide).");
            Assert.AreEqual(1800m, rec.ImpactoEstimado);        // (1000 + 2000) × 60 %
            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, rec.Prioridad, "Hay una suscripción ya vencida.");
            CollectionAssert.AreEquivalent(new[] { 1, 2 }, rec.Principal.Parametros.Contactos.Select(c => c.IdCliente).ToList());
            Assert.AreEqual(0, rec.Alternativas.Count, "La campaña por email era la misma acción: ya no se duplica.");
        }

        [TestMethod]
        public void Renovaciones_SoloLejanas_NoRecomienda()
        {
            var ctx = Ctx();
            ctx.Vencimientos.Add(Vence(1, 20, 1000m));
            Assert.AreEqual(0, Evaluar(new ReglaRenovaciones(), ctx).Count);
        }

        // ══ Escasez ══════════════════════════════════════════════════════════════

        private static BE.PrendaComercial Prenda(int id, BE.EstadoPrenda estado, string categoria = "Vestido", string talle = "M",
                                                 int pedidos = 0, int espera = 0, decimal? precio = 5000m, int diasEnCatalogo = 100)
            => new BE.PrendaComercial
            {
                IdPrenda = id, Nombre = "Prenda" + id, Categoria = categoria, Talle = talle, Estado = estado,
                PedidosPeriodo = pedidos, EnListaEspera = espera, PrecioReposicion = precio, FechaAlta = Hoy.AddDays(-diasEnCatalogo)
            };

        [TestMethod]
        public void Escasez_PocoStockConListaDeEspera_OrdenDeCompraConLineas()
        {
            var ctx = Ctx();
            ctx.Prendas.AddRange(new[]
            {
                Prenda(1, BE.EstadoPrenda.Disponible), Prenda(2, BE.EstadoPrenda.EnUso, espera: 2), Prenda(3, BE.EstadoPrenda.EnUso)
            });
            var rec = Evaluar(new ReglaEscasez(), ctx).Single();
            decimal horizonte = ctx.Umbral(CatalogoUmbrales.HorizonteMeses, 6m);
            // Orden completa: (mínimo 3 − 1 disponible) + 2 en espera = 4 prendas → 2 × 1000 × horizonte − 4 × 5000.
            // Solo la lista de espera: 2 prendas → 2 × 1000 × horizonte − 2 × 5000.
            decimal completa = 2m * 1000m * horizonte - 20000m, acotada = 2m * 1000m * horizonte - 10000m;
            var todas = rec.Opciones();
            Assert.AreEqual(BE.AccionRecomendacion.OrdenCompra, rec.Principal.Accion);
            Assert.AreEqual(completa, todas.Single(o => o.Parametros.Lineas.Single().Cantidad == 4).ImpactoEstimado);
            Assert.AreEqual(acotada, todas.Single(o => o.Parametros.Lineas.Single().Cantidad == 2).ImpactoEstimado);
            // La principal es la completa si se recupera; si no, la acotada (que sí se recupera).
            Assert.AreEqual(completa > 0 ? completa : acotada, rec.ImpactoEstimado);
            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, rec.Prioridad);
            Assert.AreEqual(2, todas.Count);
        }

        [TestMethod]
        public void Escasez_OrdenCompletaNoSeRecupera_ProponeReponerSoloLaListaDeEspera()
        {
            var ctx = Ctx();
            ctx.Umbrales[CatalogoUmbrales.HorizonteMeses] = 6m;
            ctx.Prendas.AddRange(new[]
            {
                Prenda(1, BE.EstadoPrenda.Disponible, precio: 8000m), Prenda(2, BE.EstadoPrenda.EnUso, espera: 2, precio: 8000m)
            });
            var rec = Evaluar(new ReglaEscasez(), ctx).Single();
            // completa: 2 × 1000 × 6 − 4 × 8000 = −20000 (no se recupera); acotada: 2 × 1000 × 6 − 2 × 8000 = −4000.
            // Ninguna se recupera → se mantiene la completa como propuesta y baja la prioridad.
            Assert.AreEqual(4, rec.Principal.Parametros.Lineas.Single().Cantidad);
            Assert.AreEqual(BE.PrioridadRecomendacion.Baja, rec.Prioridad);

            // Con reposición más barata la acotada sí se recupera (2 × 1000 × 6 − 2 × 5500 = 1000) y pasa a ser la propuesta.
            foreach (var p in ctx.Prendas) p.PrecioReposicion = 5500m;
            rec = Evaluar(new ReglaEscasez(), ctx).Single();
            Assert.AreEqual(2, rec.Principal.Parametros.Lineas.Single().Cantidad, "Se propone la reposición acotada, que sí se recupera.");
            Assert.AreEqual(1000m, rec.ImpactoEstimado);
            Assert.AreEqual(4, rec.Alternativas.Single().Parametros.Lineas.Single().Cantidad, "La orden completa queda como alternativa.");
            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, rec.Prioridad);
        }

        [TestMethod]
        public void Escasez_NingunaOpcionRecuperaLaInversion_PrioridadBajaYLoExplica()
        {
            var ctx = Ctx();
            // Reposición muy cara frente al ticket: ninguna opción se recupera en el horizonte.
            ctx.Prendas.AddRange(new[]
            {
                Prenda(1, BE.EstadoPrenda.Disponible, precio: 1000000m), Prenda(2, BE.EstadoPrenda.EnUso, espera: 2, precio: 1000000m)
            });
            var rec = Evaluar(new ReglaEscasez(), ctx).Single();
            Assert.IsTrue(rec.Opciones().All(o => o.ImpactoEstimado <= 0));
            Assert.AreEqual(BE.PrioridadRecomendacion.Baja, rec.Prioridad);
            Assert.AreEqual("rec.escasez.motivo.norentable", rec.Motivo.Clave);
        }

        [TestMethod]
        public void Escasez_PocoStockSinDemanda_NoRecomiendaComprar()
        {
            var ctx = Ctx();
            ctx.Prendas.Add(Prenda(1, BE.EstadoPrenda.Disponible));
            Assert.AreEqual(0, Evaluar(new ReglaEscasez(), ctx).Count);
            ctx.Prendas[0].PedidosPeriodo = 2;   // con pedidos en el período sí hay demanda
            Assert.AreEqual(1, Evaluar(new ReglaEscasez(), ctx).Count);
        }

        // ══ Baja rotación ════════════════════════════════════════════════════════

        [TestMethod]
        public void BajaRotacion_TresPrendasQuietas_LiquidarConCapitalInmovilizado()
        {
            var ctx = Ctx();
            ctx.Prendas.AddRange(new[]
            {
                Prenda(1, BE.EstadoPrenda.Disponible, precio: 1000m), Prenda(2, BE.EstadoPrenda.Disponible, precio: 1000m),
                Prenda(3, BE.EstadoPrenda.Disponible, precio: 1000m),
                Prenda(4, BE.EstadoPrenda.Disponible, espera: 1), Prenda(5, BE.EstadoPrenda.Disponible, diasEnCatalogo: 10),
                Prenda(6, BE.EstadoPrenda.Disponible, pedidos: 1)
            });
            var rec = Evaluar(new ReglaBajaRotacion(), ctx).Single();
            CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, rec.Principal.Parametros.Ids);
            Assert.AreEqual(1200m, rec.ImpactoEstimado);        // 3000 × 40 %
            Assert.AreEqual(BE.AccionRecomendacion.DarDeBajaPrendas, rec.Principal.Accion);
            Assert.AreEqual(0, rec.Alternativas.Count, "Destacarlas no era una acción ejecutable.");
        }

        [TestMethod]
        public void BajaRotacion_MenosDelMinimo_NoRecomienda()
        {
            var ctx = Ctx();
            ctx.Prendas.AddRange(new[] { Prenda(1, BE.EstadoPrenda.Disponible), Prenda(2, BE.EstadoPrenda.Disponible) });
            Assert.AreEqual(0, Evaluar(new ReglaBajaRotacion(), ctx).Count);
        }

        // ══ Mantenimiento ════════════════════════════════════════════════════════

        [TestMethod]
        public void Mantenimiento_ReponerCuestaMasQueLoQueSePierde_ElMotorPrefiereRevisar()
        {
            var ctx = Ctx();
            ctx.TicketPromedio = 900m;
            ctx.Planes.Add(Plan(1, 900m, 10, 0, limite: 3));
            ctx.Prendas.Add(Prenda(5, BE.EstadoPrenda.Disponible, precio: 3000m));
            ctx.Mantenimientos.Add(new BE.TiempoMantenimientoPrenda { IdPrenda = 5, NombrePrenda = "Prenda5", CantidadMantenimientos = 4, DuracionPromedioDias = 6 });
            var rec = Evaluar(new ReglaMantenimiento(), ctx).Single();
            Assert.AreEqual(BE.AccionRecomendacion.EnviarAMantenimiento, rec.Principal.Accion, "Revisar = mandarlas a limpieza (abre el mantenimiento).");
            // 24 días × (900 / 3 / 30 = 10) − 3000 = −2760.
            Assert.AreEqual(-2760m, rec.Alternativas.Single().ImpactoEstimado);
            Assert.AreEqual(BE.AccionRecomendacion.DarDeBajaPrendas, rec.Alternativas.Single().Accion);
            CollectionAssert.AreEqual(new[] { 5 }, rec.Principal.Parametros.Ids);
        }

        [TestMethod]
        public void Mantenimiento_PrendaYaDadaDeBaja_NoRecomienda()
        {
            var ctx = Ctx();
            ctx.Prendas.Add(Prenda(5, BE.EstadoPrenda.Baja));
            ctx.Mantenimientos.Add(new BE.TiempoMantenimientoPrenda { IdPrenda = 5, CantidadMantenimientos = 4, DuracionPromedioDias = 6 });
            Assert.AreEqual(0, Evaluar(new ReglaMantenimiento(), ctx).Count);
        }

        // ══ Rendimiento de promociones ═══════════════════════════════════════════

        private static BE.MedicionPromocion Medicion(BE.VeredictoPromocion veredicto, BE.EstadoPromocion estado = BE.EstadoPromocion.Vigente)
            => new BE.MedicionPromocion
            {
                Promocion = new BE.Promocion { IdPromocion = 9, Nombre = "Promo", Estado = estado, IdPlan = 1,
                                               TipoDescuento = BE.TipoDescuento.Porcentaje, Valor = 15m,
                                               FechaInicio = Hoy.AddDays(-29), FechaFin = Hoy.AddDays(10) },
                Resultado = new BE.ResultadoPromocion { IdPromocion = 9, Nombre = "Promo", IdPlan = 1, NombrePlan = "Básico",
                                                        Desde = Hoy.AddDays(-29), Hasta = Hoy.AddDays(1), ClientesEnJuego = 12,
                                                        ClientesRetenidos = 9, ClientesEnJuegoAntes = 10, ClientesRetenidosAntes = 5 },
                TotalDescontado = 3000m, Cobros = 6, PrecioPlan = 1000m, BeneficioReal = 6000m, Veredicto = veredicto,
                Explicacion = new BE.TextoTraducible("med.ver.x", "x")
            };

        [TestMethod]
        public void Promocion_RindeMal_SolicitarBajaConAhorroMensual()
        {
            var ctx = Ctx();
            ctx.Promociones.Add(Medicion(BE.VeredictoPromocion.DarDeBaja));
            var rec = Evaluar(new ReglaRendimientoPromocion(), ctx).Single();
            Assert.AreEqual(BE.AccionRecomendacion.SolicitarBajaPromocion, rec.Principal.Accion);
            Assert.AreEqual(9, rec.Principal.Parametros.IdPromocion);
            Assert.AreEqual(3000m, rec.ImpactoEstimado);        // 3000 / 30 días × 30
            Assert.AreEqual(0, rec.Alternativas.Count, "Esperar el vencimiento no es una acción que ejecutar.");
            Assert.AreEqual(BE.PrioridadRecomendacion.Alta, rec.Prioridad);
        }

        [TestMethod]
        public void Promocion_Cumple_ExtenderConNuevaSugerencia()
        {
            var ctx = Ctx();
            ctx.Promociones.Add(Medicion(BE.VeredictoPromocion.Extender));
            var rec = Evaluar(new ReglaRendimientoPromocion(), ctx).Single();
            Assert.AreEqual(BE.AccionRecomendacion.ExtenderPromocion, rec.Principal.Accion);
            Assert.AreEqual(6000m, rec.ImpactoEstimado);
            Assert.AreEqual(15m, rec.Principal.Parametros.DescuentoPorcentaje);
        }

        [TestMethod]
        public void Promocion_MantenerOYaVencida_NoRecomienda()
        {
            var ctx = Ctx();
            ctx.Promociones.Add(Medicion(BE.VeredictoPromocion.Mantener));
            ctx.Promociones.Add(Medicion(BE.VeredictoPromocion.DarDeBaja, BE.EstadoPromocion.Vencida));
            Assert.AreEqual(0, Evaluar(new ReglaRendimientoPromocion(), ctx).Count);
        }

        [TestMethod]
        public void DesdeMedicion_SoloDarDeBajaOExtender_MantenerYSinDatosNull()
        {
            Assert.IsNull(ReglaRendimientoPromocion.DesdeMedicion(Medicion(BE.VeredictoPromocion.Mantener), Hoy, null),
                "Mantener no tiene nada que ejecutar: no se registra una decisión que solo diga «mantener».");
            Assert.AreEqual(BE.AccionRecomendacion.ExtenderPromocion,
                ReglaRendimientoPromocion.DesdeMedicion(Medicion(BE.VeredictoPromocion.Extender), Hoy, null).Principal.Accion);
            Assert.AreEqual(BE.AccionRecomendacion.SolicitarBajaPromocion,
                ReglaRendimientoPromocion.DesdeMedicion(Medicion(BE.VeredictoPromocion.DarDeBaja), Hoy, null).Principal.Accion);
            Assert.IsNull(ReglaRendimientoPromocion.DesdeMedicion(Medicion(BE.VeredictoPromocion.SinDatos), Hoy, null));
            Assert.IsNull(ReglaRendimientoPromocion.DesdeMedicion(Medicion(BE.VeredictoPromocion.Mantener, BE.EstadoPromocion.Vencida), Hoy, null));
        }

        // ══ Upgrade de plan ══════════════════════════════════════════════════════

        [TestMethod]
        public void Upgrade_ClientesQueUsanTodoElCupo_OfrecerPlanSuperior()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 3, 0, limite: 3));
            ctx.Planes.Add(Plan(2, 1500m, 1, 0, limite: 5));
            ctx.Clientes.AddRange(new[]
            {
                new BE.ClienteComercial { IdCliente = 1, Nombre = "A", Apellido = "Uno", IdPlan = 1, PrendasEnUso = 3 },
                new BE.ClienteComercial { IdCliente = 2, Nombre = "B", Apellido = "Dos", IdPlan = 1, PrendasEnUso = 3 },
                new BE.ClienteComercial { IdCliente = 3, Nombre = "C", Apellido = "Tres", IdPlan = 1, PrendasEnUso = 1 }
            });
            var rec = Evaluar(new ReglaUpgradePlan(), ctx).Single();
            Assert.AreEqual(2, rec.Principal.Parametros.Contactos.Count);
            // Opciones: 2 × 20 % × 500 × 6 = 1200 y, bonificando el primer mes, 2 × 30 % × 500 × 5 = 1500 (gana).
            Assert.AreEqual(1500m, rec.ImpactoEstimado);
            Assert.AreEqual(1200m, rec.Alternativas.Single().ImpactoEstimado);
        }

        [TestMethod]
        public void Upgrade_SinPlanSuperior_NoRecomienda()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 3, 0, limite: 3));
            ctx.Clientes.Add(new BE.ClienteComercial { IdCliente = 1, IdPlan = 1, PrendasEnUso = 3 });
            Assert.AreEqual(0, Evaluar(new ReglaUpgradePlan(), ctx).Count);
        }

        // ══ Motor ════════════════════════════════════════════════════════════════

        private sealed class ReglaQueFalla : IReglaRecomendacion
        {
            public string Tipo => "Falla";
            public IEnumerable<BE.Recomendacion> Evaluar(BE.ContextoComercial ctx) => throw new InvalidOperationException("dato inesperado");
        }

        [TestMethod]
        public void Motor_UnaReglaQueFalla_NoCortaLasDemasYQuedaInformada()
        {
            var ctx = Ctx();
            ctx.Vencimientos.Add(Vence(1, 3, 1000m));
            var resultado = new MotorRecomendaciones(new IReglaRecomendacion[] { new ReglaQueFalla(), new ReglaRenovaciones() }).Evaluar(ctx);
            Assert.AreEqual(1, resultado.Recomendaciones.Count);
            CollectionAssert.AreEqual(new[] { "Falla" }, resultado.ReglasConError);
        }

        [TestMethod]
        public void Motor_OrdenaPorPrioridadYDespuesPorImpacto()
        {
            var ctx = Ctx();
            ctx.Vencimientos.Add(Vence(1, 3, 1000m));                                 // Media
            ctx.Vencimientos.Add(Vence(2, 2, 1000m));
            ctx.Promociones.Add(Medicion(BE.VeredictoPromocion.DarDeBaja));          // Alta
            ctx.Prendas.Add(Prenda(5, BE.EstadoPrenda.EnUso));                        // Baja: menos de 3 excesivas
            ctx.Mantenimientos.Add(new BE.TiempoMantenimientoPrenda { IdPrenda = 5, NombrePrenda = "Prenda5", CantidadMantenimientos = 4, DuracionPromedioDias = 6 });
            var recs = new MotorRecomendaciones().Evaluar(ctx).Recomendaciones;
            Assert.AreEqual(3, recs.Count);
            Assert.AreEqual(ReglaRendimientoPromocion.Codigo, recs[0].TipoRegla);
            Assert.AreEqual(ReglaRenovaciones.Codigo, recs[1].TipoRegla);
            Assert.AreEqual(ReglaMantenimiento.Codigo, recs[2].TipoRegla);
        }

        [TestMethod]
        public void Motor_ContextoVacio_SinRecomendacionesNiErrores()
        {
            var resultado = new MotorRecomendaciones().Evaluar(Ctx());
            Assert.AreEqual(0, resultado.Recomendaciones.Count);
            Assert.AreEqual(0, resultado.ReglasConError.Count);
            Assert.AreEqual(7, new MotorRecomendaciones().Reglas.Count);
            CollectionAssert.AreEquivalent(
                new[] { ReglaChurnPlan.Codigo, ReglaRenovaciones.Codigo, ReglaEscasez.Codigo, ReglaBajaRotacion.Codigo,
                        ReglaMantenimiento.Codigo, ReglaRendimientoPromocion.Codigo, ReglaUpgradePlan.Codigo },
                new MotorRecomendaciones().Reglas.Select(r => r.Tipo).ToList(),
                "Vendedores y OcupacionStock se retiraron: solo registraban la decisión.");
        }

        [TestMethod]
        public void Acciones_SoloLasEjecutables_ConValoresNumericosEstables()
        {
            // Las acciones que no hacían nada (o solo abrían una pantalla) ya no existen.
            var nombres = Enum.GetNames(typeof(BE.AccionRecomendacion));
            foreach (var retirada in new[] { "Informativa", "GestionarCobro", "VerPrendas", "VerMantenimiento" })
                CollectionAssert.DoesNotContain(nombres, retirada);
            // Los valores que siguen no cambian de número.
            Assert.AreEqual(1, (int)BE.AccionRecomendacion.SugerirPromocion);
            Assert.AreEqual(3, (int)BE.AccionRecomendacion.ExportarContactos);
            Assert.AreEqual(4, (int)BE.AccionRecomendacion.OrdenCompra);
            Assert.AreEqual(7, (int)BE.AccionRecomendacion.SolicitarBajaPromocion);
            Assert.AreEqual(8, (int)BE.AccionRecomendacion.ExtenderPromocion);
            Assert.AreEqual(9, (int)BE.AccionRecomendacion.DarDeBajaPrendas);
            Assert.AreEqual(10, (int)BE.AccionRecomendacion.EnviarAMantenimiento);
        }

        [TestMethod]
        public void Motor_TodaOpcionDeTodaRecomendacionEjecutaUnaAccionDefinida()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 3, 3, enRiesgo: 2, limite: 3));
            ctx.Planes.Add(Plan(2, 1500m, 1, 0, limite: 5));
            ctx.Clientes.AddRange(new[]
            {
                new BE.ClienteComercial { IdCliente = 1, Nombre = "A", Apellido = "Uno", IdPlan = 1, PrendasEnUso = 3 },
                new BE.ClienteComercial { IdCliente = 2, Nombre = "B", Apellido = "Dos", IdPlan = 1, PrendasEnUso = 3 }
            });
            ctx.Vencimientos.Add(Vence(1, -1, 1000m));
            ctx.Promociones.Add(Medicion(BE.VeredictoPromocion.DarDeBaja));
            ctx.Promociones.Add(Medicion(BE.VeredictoPromocion.Extender));
            for (int i = 10; i < 16; i++) ctx.Prendas.Add(Prenda(i, BE.EstadoPrenda.Disponible));
            ctx.Prendas.Add(Prenda(20, BE.EstadoPrenda.EnUso, espera: 2));
            ctx.Mantenimientos.Add(new BE.TiempoMantenimientoPrenda { IdPrenda = 10, NombrePrenda = "Prenda10", CantidadMantenimientos = 4, DuracionPromedioDias = 6 });
            var recs = new MotorRecomendaciones().Evaluar(ctx).Recomendaciones;
            Assert.IsTrue(recs.Select(r => r.TipoRegla).Distinct().Count() >= 6, string.Join(", ", recs.Select(r => r.TipoRegla).Distinct()));
            foreach (var o in recs.SelectMany(r => r.Opciones()))
                Assert.IsTrue(Enum.IsDefined(typeof(BE.AccionRecomendacion), o.Accion), o.Descripcion?.Clave);
        }

        [TestMethod]
        public void Motor_TodaRecomendacionTraeDiagnosticoMotivoFormulaYClave()
        {
            var ctx = Ctx();
            ctx.Planes.Add(Plan(1, 1000m, 20, 2, enRiesgo: 10, retencion: 60m, medidas: 2));
            ctx.Vencimientos.Add(Vence(1, -1, 1000m));
            ctx.Prendas.AddRange(new[] { Prenda(1, BE.EstadoPrenda.Disponible), Prenda(2, BE.EstadoPrenda.EnUso, espera: 1) });
            foreach (var r in new MotorRecomendaciones().Evaluar(ctx).Recomendaciones)
            {
                Assert.IsFalse(string.IsNullOrEmpty(r.Clave));
                Assert.IsNotNull(r.Titulo?.Clave, r.TipoRegla);
                Assert.IsNotNull(r.Diagnostico?.Clave, r.TipoRegla);
                Assert.IsNotNull(r.Motivo?.Clave, r.TipoRegla);
                Assert.IsTrue(r.Opciones().All(o => o.Formula?.Clave != null && o.Descripcion?.Clave != null), r.TipoRegla);
            }
        }
    }
}
