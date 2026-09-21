using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// PN03 — BE.PoliticaDescuento: regla de NUULY "un solo descuento por ciclo de facturación;
    /// los no usados quedan acumulados". Compiten la promoción vigente del plan y el crédito por referidos.
    /// </summary>
    [TestClass]
    public class PoliticaDescuentoTests
    {
        private static BE.Promocion Promo(BE.TipoDescuento tipo, decimal valor, int? idPlan = 1,
                                          BE.EstadoPromocion estado = BE.EstadoPromocion.Vigente,
                                          int diasInicio = -5, int diasFin = 30) => new BE.Promocion
        {
            IdPromocion = 1,
            Nombre = "Promo",
            TipoDescuento = tipo,
            Valor = valor,
            IdPlan = idPlan,
            Estado = estado,
            FechaInicio = DateTime.Today.AddDays(diasInicio),
            FechaFin = DateTime.Today.AddDays(diasFin)
        };

        [TestMethod]
        public void DescuentoDe_Porcentaje_CalculaSobreElBruto()
            => Assert.AreEqual(1500m, BE.PoliticaDescuento.DescuentoDe(Promo(BE.TipoDescuento.Porcentaje, 10), 15000m));

        [TestMethod]
        public void DescuentoDe_MontoFijo_NoSuperaElBruto()
        {
            Assert.AreEqual(3000m, BE.PoliticaDescuento.DescuentoDe(Promo(BE.TipoDescuento.MontoFijo, 3000), 15000m));
            Assert.AreEqual(2000m, BE.PoliticaDescuento.DescuentoDe(Promo(BE.TipoDescuento.MontoFijo, 9000), 2000m));
        }

        [TestMethod]
        public void DescuentoDe_PrecioPromocional_EsPrecioMensual_EscalaPorMeses()
        {
            // Plan a 1000/mes, cobro anual (bruto 12000), promo a 800/mes: paga 9600, no 800.
            Assert.AreEqual(2400m, BE.PoliticaDescuento.DescuentoDe(Promo(BE.TipoDescuento.PrecioPromocional, 800), 12000m, 12));
            Assert.AreEqual(600m, BE.PoliticaDescuento.DescuentoDe(Promo(BE.TipoDescuento.PrecioPromocional, 800), 3000m, 3));
        }
        [TestMethod]
        public void DescuentoDe_PrecioPromocional_EsLaDiferenciaConElBruto()
        {
            Assert.AreEqual(5000m, BE.PoliticaDescuento.DescuentoDe(Promo(BE.TipoDescuento.PrecioPromocional, 10000), 15000m));
            Assert.AreEqual(0m, BE.PoliticaDescuento.DescuentoDe(Promo(BE.TipoDescuento.PrecioPromocional, 20000), 15000m),
                "Un precio promocional mayor al bruto no genera descuento.");
        }

        [TestMethod]
        public void Resolver_SoloPromocion_LaAplica()
        {
            var r = BE.PoliticaDescuento.Resolver(15000m, 1, new[] { Promo(BE.TipoDescuento.Porcentaje, 10) }, 0m);

            Assert.AreEqual(1500m, r.Descuento);
            Assert.IsNotNull(r.Promocion);
            Assert.IsFalse(r.UsaCreditoReferido);
            Assert.AreEqual(13500m, r.Total);
        }

        [TestMethod]
        public void Resolver_SoloCreditoReferido_LoAplica()
        {
            var r = BE.PoliticaDescuento.Resolver(15000m, 1, new List<BE.Promocion>(), 1000m);

            Assert.AreEqual(1000m, r.Descuento);
            Assert.IsNull(r.Promocion);
            Assert.IsTrue(r.UsaCreditoReferido);
        }

        [TestMethod]
        public void Resolver_GanaLaPromocion_ElCreditoNoSeConsume()
        {
            // Un solo descuento por ciclo: la promoción (1500) supera al crédito (1000) → el crédito se conserva.
            var r = BE.PoliticaDescuento.Resolver(15000m, 1, new[] { Promo(BE.TipoDescuento.Porcentaje, 10) }, 1000m);

            Assert.AreEqual(1500m, r.Descuento);
            Assert.IsFalse(r.UsaCreditoReferido, "El crédito por referido queda acumulado para el próximo ciclo.");
        }

        [TestMethod]
        public void Resolver_GanaElCredito_SeConsumeElCreditoYNoSeSuman()
        {
            var r = BE.PoliticaDescuento.Resolver(15000m, 1, new[] { Promo(BE.TipoDescuento.MontoFijo, 500) }, 2000m);

            Assert.AreEqual(2000m, r.Descuento, "No se acumulan los dos descuentos.");
            Assert.IsTrue(r.UsaCreditoReferido);
            Assert.IsNull(r.Promocion);
        }

        [TestMethod]
        public void Resolver_Empate_PrefierePromocionParaNoPerderElCredito()
        {
            var r = BE.PoliticaDescuento.Resolver(15000m, 1, new[] { Promo(BE.TipoDescuento.MontoFijo, 1000) }, 1000m);

            Assert.IsNotNull(r.Promocion);
            Assert.IsFalse(r.UsaCreditoReferido);
        }

        [TestMethod]
        public void Resolver_VariasPromociones_ElijeLaDeMayorDescuento()
        {
            var chica = Promo(BE.TipoDescuento.MontoFijo, 500);
            var grande = Promo(BE.TipoDescuento.Porcentaje, 20);
            grande.IdPromocion = 2;

            var r = BE.PoliticaDescuento.Resolver(10000m, 1, new[] { chica, grande }, 0m);

            Assert.AreEqual(2000m, r.Descuento);
            Assert.AreEqual(2, r.Promocion.IdPromocion);
        }

        [TestMethod]
        public void Resolver_IgnoraPromocionesDeOtroPlanFueraDeVigenciaOEnOtroEstado()
        {
            var promos = new[]
            {
                Promo(BE.TipoDescuento.Porcentaje, 50, idPlan: 2),                                          // otro plan
                Promo(BE.TipoDescuento.Porcentaje, 50, diasInicio: 5, diasFin: 20),                          // aún no empieza
                Promo(BE.TipoDescuento.Porcentaje, 50, diasInicio: -30, diasFin: -1),                        // ya terminó
                Promo(BE.TipoDescuento.Porcentaje, 50, estado: BE.EstadoPromocion.EnRevisionContable),       // sin aprobar
                Promo(BE.TipoDescuento.Porcentaje, 50, estado: BE.EstadoPromocion.BajaSolicitada),
                Promo(BE.TipoDescuento.Porcentaje, 50, idPlan: null)                                         // de categoría (informativa)
            };
            promos[5].CategoriaPrenda = "Abrigo";

            var r = BE.PoliticaDescuento.Resolver(10000m, 1, promos, 0m);

            Assert.AreEqual(0m, r.Descuento);
            Assert.IsNull(r.Promocion);
            Assert.AreEqual(10000m, r.Total);
        }

        [TestMethod]
        public void Resolver_CreditoMayorAlBruto_SeTopeaYElTotalNoEsNegativo()
        {
            var r = BE.PoliticaDescuento.Resolver(800m, 1, new List<BE.Promocion>(), 5000m);

            Assert.AreEqual(800m, r.Descuento);
            Assert.AreEqual(0m, r.Total);
        }
    }
}
