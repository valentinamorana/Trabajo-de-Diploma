using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Backups cifrados (.wfbak) autenticados: formato v2 con HMAC-SHA256 (encrypt-then-MAC).
    /// Ida y vuelta, rechazo de archivos alterados/truncados, contraseña incorrecta, y lectura
    /// del formato v1 anterior (sin HMAC) con constancia en la bitácora al restaurarlo.
    /// Sin SQL Server: el DAL de backup es <see cref="FakeBackupDAL"/>.
    /// </summary>
    [TestClass]
    public class BackupCifradoTests
    {
        private const string Clave = "Clave-de-backup-1!";
        private string _dir;

        [TestInitialize]
        public void Preparar()
        {
            _dir = Path.Combine(Path.GetTempPath(), "WF_BackupCifradoTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SessionManager.Logout();
        }

        [TestCleanup]
        public void Limpiar()
        {
            SessionManager.Logout();
            BLL.PermisosAccion.LimpiarCacheVigencia();
            try { Directory.Delete(_dir, true); } catch { }
        }

        private string Ruta(string nombre) => Path.Combine(_dir, nombre);

        // MSTest v1 no trae Assert.ThrowsException: acepta T o un derivado y lo devuelve.
        private static T Lanza<T>(Action accion) where T : Exception
        {
            try { accion(); }
            catch (T ex) { return ex; }
            Assert.Fail("Se esperaba una excepción " + typeof(T).Name + ".");
            return null;
        }

        // Contenido "plano" de prueba: más de un bloque de buffer para ejercitar el streaming.
        private string CrearPlano(int largo = 200_000)
        {
            var datos = new byte[largo];
            new Random(42).NextBytes(datos);
            string ruta = Ruta("plano.bak");
            File.WriteAllBytes(ruta, datos);
            return ruta;
        }

        // Reproduce el formato v1 (anterior) a mano: [salt(16)][IV(16)][AES-128-CBC], clave
        // PBKDF2-SHA256 100.000 iteraciones. Es independiente del código de producción a
        // propósito: si alguien cambia la lectura legacy, este archivo "viejo" deja de abrir.
        private static void CifrarFormatoV1(string origen, string destino, string password)
        {
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
            byte[] clave;
            using (var kdf = new Rfc2898DeriveBytes(password, salt, 100000, HashAlgorithmName.SHA256))
                clave = kdf.GetBytes(16);
            using (var aes = Aes.Create())
            {
                aes.KeySize = 128; aes.Mode = CipherMode.CBC; aes.Padding = PaddingMode.PKCS7;
                aes.Key = clave; aes.GenerateIV();
                using (var fsOut = new FileStream(destino, FileMode.Create, FileAccess.Write))
                {
                    fsOut.Write(salt, 0, 16);
                    fsOut.Write(aes.IV, 0, 16);
                    using (var cs = new CryptoStream(fsOut, aes.CreateEncryptor(), CryptoStreamMode.Write))
                    using (var fsIn = File.OpenRead(origen))
                        fsIn.CopyTo(cs);
                }
            }
        }

        private static void InvertirByte(string ruta, long posicion)
        {
            using (var fs = new FileStream(ruta, FileMode.Open, FileAccess.ReadWrite))
            {
                fs.Position = posicion;
                int b = fs.ReadByte();
                fs.Position = posicion;
                fs.WriteByte((byte)(b ^ 0x01));
            }
        }

        private static void IniciarSesionAdmin()
        {
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Rol = "Administrador" });
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
        }

        // ── Seguridad.CifradorArchivos ──────────────────────────────────────────────────

        [TestMethod]
        public void Cifrar_GeneraFormatoV2_ConCabeceraYIdaYVuelta()
        {
            string plano = CrearPlano();
            string cifrado = Ruta("b.wfbak");
            string salida  = Ruta("salida.bak");

            CifradorArchivos.Cifrar(plano, cifrado, Clave);

            byte[] cab = File.ReadAllBytes(cifrado).Take(5).ToArray();
            CollectionAssert.AreEqual(new byte[] { (byte)'W', (byte)'F', (byte)'B', (byte)'K', 2 }, cab,
                "El archivo nuevo debe llevar la cabecera versionada WFBK v2.");

            var formato = CifradorArchivos.Descifrar(cifrado, salida, Clave);

            Assert.AreEqual(FormatoArchivoCifrado.V2Autenticado, formato);
            CollectionAssert.AreEqual(File.ReadAllBytes(plano), File.ReadAllBytes(salida));
        }

        [TestMethod]
        public void Descifrar_V2_ByteDelCiphertextAlterado_SeRechazaSinDejarSalida()
        {
            string plano = CrearPlano();
            string cifrado = Ruta("b.wfbak");
            string salida  = Ruta("salida.bak");
            CifradorArchivos.Cifrar(plano, cifrado, Clave);

            // Un byte en el medio del ciphertext (después de la cabecera de 53 bytes).
            InvertirByte(cifrado, 53 + 1000);

            Lanza<ArchivoCifradoAlteradoException>(
                () => CifradorArchivos.Descifrar(cifrado, salida, Clave));
            Assert.IsFalse(File.Exists(salida), "No debe quedar un archivo descifrado a medias.");
        }

        [TestMethod]
        public void Descifrar_V2_IvOHmacAlterados_SeRechazan()
        {
            string plano = CrearPlano(5_000);
            string cifrado = Ruta("b.wfbak");
            CifradorArchivos.Cifrar(plano, cifrado, Clave);
            byte[] original = File.ReadAllBytes(cifrado);

            // IV (offset 37..52): alterar el IV cambiaría el primer bloque sin romper el padding.
            InvertirByte(cifrado, 40);
            Lanza<ArchivoCifradoAlteradoException>(
                () => CifradorArchivos.Descifrar(cifrado, Ruta("s1.bak"), Clave));

            // Último byte (parte del HMAC).
            File.WriteAllBytes(cifrado, original);
            InvertirByte(cifrado, original.Length - 1);
            Lanza<ArchivoCifradoAlteradoException>(
                () => CifradorArchivos.Descifrar(cifrado, Ruta("s2.bak"), Clave));
        }

        [TestMethod]
        public void Descifrar_V2_Truncado_SeRechaza()
        {
            string plano = CrearPlano(5_000);
            string cifrado = Ruta("b.wfbak");
            CifradorArchivos.Cifrar(plano, cifrado, Clave);

            byte[] bytes = File.ReadAllBytes(cifrado);
            // Se le quitan 16 bytes (un bloque): el largo sigue siendo coherente, pero el MAC no.
            File.WriteAllBytes(cifrado, bytes.Take(bytes.Length - 16).ToArray());

            Lanza<ArchivoCifradoAlteradoException>(
                () => CifradorArchivos.Descifrar(cifrado, Ruta("s.bak"), Clave));
        }

        [TestMethod]
        public void Descifrar_V2_ContrasenaIncorrecta_NoSeInformaComoAlteracion()
        {
            string plano = CrearPlano(5_000);
            string cifrado = Ruta("b.wfbak");
            string salida  = Ruta("salida.bak");
            CifradorArchivos.Cifrar(plano, cifrado, Clave);

            var ex = Lanza<CryptographicException>(
                () => CifradorArchivos.Descifrar(cifrado, salida, "otra-clave"));
            Assert.IsNotInstanceOfType(ex, typeof(ArchivoCifradoAlteradoException));
            Assert.IsFalse(File.Exists(salida));
        }

        [TestMethod]
        public void Descifrar_FormatoV1Viejo_SigueSiendoLegible()
        {
            string plano = CrearPlano();
            string viejo  = Ruta("viejo.wfbak");
            string salida = Ruta("salida.bak");
            CifrarFormatoV1(plano, viejo, Clave);

            var formato = CifradorArchivos.Descifrar(viejo, salida, Clave);

            Assert.AreEqual(FormatoArchivoCifrado.V1SinAutenticacion, formato);
            CollectionAssert.AreEqual(File.ReadAllBytes(plano), File.ReadAllBytes(salida));
        }

        [TestMethod]
        public void Descifrar_FormatoV1Viejo_ContrasenaIncorrecta_Falla()
        {
            string plano = CrearPlano(5_000);
            string viejo = Ruta("viejo.wfbak");
            CifrarFormatoV1(plano, viejo, Clave);

            Lanza<CryptographicException>(
                () => CifradorArchivos.Descifrar(viejo, Ruta("s.bak"), "otra-clave"));
        }

        // ── BLL.Backup.RestaurarBackup ──────────────────────────────────────────────────

        [TestMethod]
        public void Restaurar_V2Valido_RestauraElContenidoSinAvisoDeVerificacion()
        {
            IniciarSesionAdmin();
            string plano = CrearPlano(5_000);
            string cifrado = Ruta("WardrobeFlow_Backup_20261010120000_admin.wfbak");
            CifradorArchivos.Cifrar(plano, cifrado, Clave);
            var fake = new FakeBackupDAL();
            int antes = ConfiguracionTests.Bitacora.Registros.Count;

            new BLL.Backup(fake).RestaurarBackup("Test", cifrado, Clave);

            Assert.AreEqual(1, fake.VecesRestaurado);
            CollectionAssert.AreEqual(File.ReadAllBytes(plano), fake.ContenidoRestaurado);
            var nuevos = ConfiguracionTests.Bitacora.Registros.Skip(antes).ToList();
            Assert.IsFalse(nuevos.Any(r => r.Actividad.StartsWith(BE.ActividadesBitacora.BackupSinVerificacionPrefijo)),
                "Un backup v2 verificado no debe asentar la advertencia de 'sin verificación'.");
        }

        [TestMethod]
        public void Restaurar_V2Alterado_SeRechazaConAppExceptionYNoRestaura()
        {
            IniciarSesionAdmin();
            string plano = CrearPlano(5_000);
            string cifrado = Ruta("alterado.wfbak");
            CifradorArchivos.Cifrar(plano, cifrado, Clave);
            InvertirByte(cifrado, 53 + 100);
            var fake = new FakeBackupDAL();

            var ex = Lanza<BE.AppException>(
                () => new BLL.Backup(fake).RestaurarBackup("Test", cifrado, Clave));

            Assert.AreEqual("err.bll.backup.alterado", ex.Clave);
            Assert.AreEqual(0, fake.VecesRestaurado, "Un backup alterado no debe llegar al DAL.");
        }

        [TestMethod]
        public void Restaurar_V2ContrasenaIncorrecta_DaClaveInvalida()
        {
            IniciarSesionAdmin();
            string plano = CrearPlano(5_000);
            string cifrado = Ruta("b.wfbak");
            CifradorArchivos.Cifrar(plano, cifrado, Clave);
            var fake = new FakeBackupDAL();

            var ex = Lanza<BE.AppException>(
                () => new BLL.Backup(fake).RestaurarBackup("Test", cifrado, "otra-clave"));

            Assert.AreEqual("err.bll.backup.clave_invalida", ex.Clave);
            Assert.AreEqual(0, fake.VecesRestaurado);
        }

        [TestMethod]
        public void Restaurar_FormatoV1Viejo_RestauraYAsientaQueNoTeniaVerificacion()
        {
            IniciarSesionAdmin();
            string plano = CrearPlano(5_000);
            string viejo = Ruta("WardrobeFlow_Backup_20250101120000_admin.wfbak");
            CifrarFormatoV1(plano, viejo, Clave);
            var fake = new FakeBackupDAL();
            int antes = ConfiguracionTests.Bitacora.Registros.Count;

            new BLL.Backup(fake).RestaurarBackup("Test", viejo, Clave);

            Assert.AreEqual(1, fake.VecesRestaurado);
            CollectionAssert.AreEqual(File.ReadAllBytes(plano), fake.ContenidoRestaurado);
            var nuevos = ConfiguracionTests.Bitacora.Registros.Skip(antes).ToList();
            Assert.IsTrue(nuevos.Any(r =>
                    r.Actividad.StartsWith(BE.ActividadesBitacora.BackupSinVerificacionPrefijo) &&
                    r.Actividad.Contains(Path.GetFileName(viejo))),
                "Restaurar un .wfbak sin HMAC debe dejar constancia en la bitácora.");
        }
    }
}
