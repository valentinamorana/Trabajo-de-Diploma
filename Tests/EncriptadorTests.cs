using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>T03 — Pruebas de hash de contraseñas (PBKDF2).</summary>
    [TestClass]
    public class EncriptadorTests
    {
        [TestMethod]
        public void Hash_VerificaContrasenaCorrecta()
        {
            string h = Seguridad.Encriptador.Hash("Secreta1!");
            Assert.IsTrue(Seguridad.Encriptador.VerificarContrasena("Secreta1!", h));
        }

        [TestMethod]
        public void Hash_RechazaContrasenaIncorrecta()
        {
            string h = Seguridad.Encriptador.Hash("Secreta1!");
            Assert.IsFalse(Seguridad.Encriptador.VerificarContrasena("otra", h));
        }

        [TestMethod]
        public void Hash_UsaSaltDistintoPorLlamada()
        {
            // Mismo texto, hashes distintos → hay salt aleatorio.
            Assert.AreNotEqual(Seguridad.Encriptador.Hash("x"), Seguridad.Encriptador.Hash("x"));
        }
    }
}
