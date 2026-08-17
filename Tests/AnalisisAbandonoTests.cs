using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BLL.Estrategias;

namespace Tests
{
    /// <summary>
    /// PdN10 — Pruebas del patrón Strategy para detección de riesgo de abandono. Cada
    /// estrategia es pura (sin BD): recibe BE.DatosClienteRiesgo y devuelve
    /// BE.ResultadoRiesgo, igual que Arma.Disparar() en el ejemplo de cátedra.
    /// </summary>
    [TestClass]
    public class AnalisisAbandonoTests
    {
        private static BE.Cliente Cliente(DateTime fechaAlta, DateTime? fechaVencimiento) => new BE.Cliente
        {
            IdCliente = 1,
            Nombre = "Ana",
            Apellido = "Gómez",
            IdPlan = 1,
            NombrePlan = "Básico",
            FechaAlta = fechaAlta,
            FechaVencimiento = fechaVencimiento
        };

        // ── EstrategiaVencimientoInactividad ────────────────────────────────────

        [TestMethod]
        public void VencimientoInactividad_VenceProntoYSinPedidos_EnRiesgo()
        {
            var estrategia = new EstrategiaVencimientoInactividad();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddYears(-1), DateTime.Today.AddDays(5)),
                FechaUltimoPedido = null
            };

            var resultado = estrategia.Evaluar(datos);

            Assert.IsTrue(resultado.EnRiesgo);
            Assert.AreEqual("abandono.motivo.vencinactividad_nunca", resultado.Clave);
        }

        [TestMethod]
        public void VencimientoInactividad_VenceProntoConInactividadLarga_EnRiesgo()
        {
            var estrategia = new EstrategiaVencimientoInactividad();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddYears(-1), DateTime.Today.AddDays(3)),
                FechaUltimoPedido = DateTime.Today.AddDays(-EstrategiaVencimientoInactividad.DiasSinActividadParaRiesgo - 5)
            };

            var resultado = estrategia.Evaluar(datos);

            Assert.IsTrue(resultado.EnRiesgo);
            Assert.AreEqual("abandono.motivo.vencinactividad", resultado.Clave);
        }

        [TestMethod]
        public void VencimientoInactividad_VenceProntoPeroConActividadReciente_NoEnRiesgo()
        {
            var estrategia = new EstrategiaVencimientoInactividad();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddYears(-1), DateTime.Today.AddDays(3)),
                FechaUltimoPedido = DateTime.Today.AddDays(-2) // actividad reciente
            };

            Assert.IsFalse(estrategia.Evaluar(datos).EnRiesgo);
        }

        [TestMethod]
        public void VencimientoInactividad_VigenteYLejosDeVencer_NoEnRiesgo()
        {
            var estrategia = new EstrategiaVencimientoInactividad();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddYears(-1), DateTime.Today.AddDays(90)),
                FechaUltimoPedido = null // ni actividad, pero falta mucho para vencer
            };

            Assert.IsFalse(estrategia.Evaluar(datos).EnRiesgo);
        }

        // ── EstrategiaInactividadPura ────────────────────────────────────────────

        [TestMethod]
        public void InactividadPura_SinActividadLarga_EnRiesgo_AunqueSuscripcionVigente()
        {
            var estrategia = new EstrategiaInactividadPura();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddYears(-1), DateTime.Today.AddMonths(6)), // vigente, lejos de vencer
                FechaUltimoPedido = DateTime.Today.AddDays(-EstrategiaInactividadPura.DiasSinActividadParaRiesgo - 1)
            };

            var resultado = estrategia.Evaluar(datos);

            Assert.IsTrue(resultado.EnRiesgo);
            Assert.AreEqual("abandono.motivo.inactividad", resultado.Clave);
        }

        [TestMethod]
        public void InactividadPura_NuncaPidio_NoEnRiesgo()
        {
            // Esta estrategia NO cubre "nunca pidió" — ese es el criterio de
            // EstrategiaClienteNuevoInactivo (evita que las tres estrategias se solapen).
            var estrategia = new EstrategiaInactividadPura();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddYears(-1), DateTime.Today.AddMonths(6)),
                FechaUltimoPedido = null
            };

            Assert.IsFalse(estrategia.Evaluar(datos).EnRiesgo);
        }

        [TestMethod]
        public void InactividadPura_ActividadReciente_NoEnRiesgo()
        {
            var estrategia = new EstrategiaInactividadPura();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddYears(-1), DateTime.Today.AddMonths(6)),
                FechaUltimoPedido = DateTime.Today.AddDays(-5)
            };

            Assert.IsFalse(estrategia.Evaluar(datos).EnRiesgo);
        }

        // ── EstrategiaClienteNuevoInactivo ───────────────────────────────────────

        [TestMethod]
        public void ClienteNuevoInactivo_AltaViejaSinPedidos_EnRiesgo()
        {
            var estrategia = new EstrategiaClienteNuevoInactivo();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddDays(-EstrategiaClienteNuevoInactivo.DiasDeGraciaInicial - 5), null),
                FechaUltimoPedido = null
            };

            var resultado = estrategia.Evaluar(datos);

            Assert.IsTrue(resultado.EnRiesgo);
            Assert.AreEqual("abandono.motivo.nuncaactivo", resultado.Clave);
        }

        [TestMethod]
        public void ClienteNuevoInactivo_AltaReciente_NoEnRiesgo()
        {
            // Todavía dentro del período de gracia inicial: no es "abandono", es recién llegado.
            var estrategia = new EstrategiaClienteNuevoInactivo();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddDays(-3), null),
                FechaUltimoPedido = null
            };

            Assert.IsFalse(estrategia.Evaluar(datos).EnRiesgo);
        }

        [TestMethod]
        public void ClienteNuevoInactivo_AltaViejaConAlgunPedido_NoEnRiesgo()
        {
            var estrategia = new EstrategiaClienteNuevoInactivo();
            var datos = new BE.DatosClienteRiesgo
            {
                Cliente = Cliente(DateTime.Today.AddDays(-EstrategiaClienteNuevoInactivo.DiasDeGraciaInicial - 5), null),
                FechaUltimoPedido = DateTime.Today.AddDays(-100) // tuvo actividad alguna vez
            };

            Assert.IsFalse(estrategia.Evaluar(datos).EnRiesgo);
        }

        // ── DatosClienteRiesgo (cálculos derivados) ──────────────────────────────

        [TestMethod]
        public void DatosClienteRiesgo_DiasSinActividad_NullSiNuncaPidio()
        {
            var datos = new BE.DatosClienteRiesgo { Cliente = Cliente(DateTime.Today, null), FechaUltimoPedido = null };
            Assert.IsNull(datos.DiasSinActividad);
        }

        [TestMethod]
        public void DatosClienteRiesgo_DiasSinActividad_CalculaCorrectamente()
        {
            var datos = new BE.DatosClienteRiesgo { Cliente = Cliente(DateTime.Today, null), FechaUltimoPedido = DateTime.Today.AddDays(-10) };
            Assert.AreEqual(10, datos.DiasSinActividad);
        }
    }
}
