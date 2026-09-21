using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// T07 — Consola de recuperación: el diagnóstico de la tabla Usuario no alcanza. Si falla el dígito
    /// verificador de otra tabla protegida (Cliente, Empleado, Pedido) el sistema bloquea el arranque, así
    /// que el diagnóstico NO puede declararse íntegro ni deshabilitar "Asumir pérdida".
    /// </summary>
    [TestClass]
    public class DiagnosticoIntegridadTests
    {
        private static BLL.DiagnosticoEspejo UsuarioSano() => new BLL.DiagnosticoEspejo { DvvOk = true };

        [TestMethod]
        public void Integro_UsuarioSanoYOtrasTablasSanas_EsIntegro()
            => Assert.IsTrue(UsuarioSano().Integro);

        [TestMethod]
        public void Integro_UsuarioSanoPeroFallaOtraTabla_NoEsIntegro()
        {
            var d = UsuarioSano();
            d.OtrasTablasCorruptas.Add("Cliente: 6 fila(s) con DVH inválido");

            Assert.IsFalse(d.Integro, "Debe quedar COMPROMETIDO para habilitar las acciones correctivas.");
        }

        [TestMethod]
        public void Integro_UsuarioConDvvInvalido_NoEsIntegro()
            => Assert.IsFalse(new BLL.DiagnosticoEspejo { DvvOk = false }.Integro);
    }
}
