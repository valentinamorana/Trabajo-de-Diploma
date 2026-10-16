using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Verificación de integridad con relectura: una escritura legítima que confirma entre la
    /// lectura de las filas y la del DVV no debe marcar la base como comprometida; una
    /// manipulación real sí, aunque se relea. Sin SQL Server (las lecturas se simulan).
    /// </summary>
    [TestClass]
    public class IntegridadRelecturaTests
    {
        private static readonly (List<int>, int, bool) Sana     = (new List<int>(), 10, true);
        private static readonly (List<int>, int, bool) NoCierra = (new List<int>(), 10, false);
        private static readonly (List<int>, int, bool) FilaRota = (new List<int> { 0 }, 10, true);

        private int _pausa;

        [TestInitialize]
        public void SinPausa()
        {
            _pausa = BLL.Configuracion.PausaEntreLecturasMs;
            BLL.Configuracion.PausaEntreLecturasMs = 0;
        }

        [TestCleanup]
        public void RestaurarPausa() => BLL.Configuracion.PausaEntreLecturasMs = _pausa;

        private static (List<int> Rotas, int DvvCalculado, bool DvvOk) Verificar(
            Queue<(List<int>, int, bool)> lecturas, out int relecturas)
        {
            int n = 0;
            var actual = lecturas.Dequeue();
            var r = BLL.Configuracion.CompararReleyendo(
                () => { n++; actual = lecturas.Dequeue(); },
                () => actual);
            relecturas = n;
            return r;
        }

        [TestMethod]
        public void BaseSana_NoRelee()
        {
            var r = Verificar(new Queue<(List<int>, int, bool)>(new[] { Sana }), out int relecturas);
            Assert.IsTrue(r.DvvOk && r.Rotas.Count == 0);
            Assert.AreEqual(0, relecturas);
        }

        [TestMethod]
        public void EscrituraEntreLasDosLecturas_AlReleerCierra()
        {
            var r = Verificar(new Queue<(List<int>, int, bool)>(new[] { NoCierra, Sana }), out int relecturas);
            Assert.IsTrue(r.DvvOk && r.Rotas.Count == 0, "La carrera desaparece al releer.");
            Assert.AreEqual(1, relecturas);
        }

        [TestMethod]
        public void ManipulacionReal_SeInformaAunqueSeRelea()
        {
            var r = Verificar(new Queue<(List<int>, int, bool)>(new[] { FilaRota, FilaRota, FilaRota }), out int relecturas);
            Assert.AreEqual(1, r.Rotas.Count, "Una fila adulterada sigue rota en todas las lecturas.");
            Assert.AreEqual(BLL.Configuracion.LecturasVerificacion - 1, relecturas, "No relee más de lo previsto.");
        }
    }
}
