using System.IO;
using System.Security.Cryptography;

namespace Seguridad
{
    /// <summary>
    /// Cifrado de ARCHIVOS con contraseña, para backups portables (patrón tomado del BackupService
    /// de Stach). Deriva una clave AES-128 de la contraseña con PBKDF2-SHA256 y cifra en AES-CBC.
    ///
    /// Formato del archivo de salida:  [ salt(16) ][ IV(16) ][ ciphertext ]
    ///
    /// A diferencia de <see cref="Encriptador"/> (que ya no maneja claves AES; no
    /// portable), acá la clave depende SOLO de la contraseña, de modo que el backup se puede
    /// restaurar en otra máquina conociendo la contraseña. Una contraseña incorrecta produce
    /// una CryptographicException al descifrar (padding inválido), que la capa superior traduce
    /// a un mensaje claro para el usuario.
    /// </summary>
    public static class CifradorArchivos
    {
        private const int SaltSize   = 16;
        private const int IvSize     = 16;
        private const int KeySize    = 16;      // AES-128
        private const int Iterations = 100000;

        private static byte[] DerivarClave(string password, byte[] salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(KeySize);
        }

        /// <summary>Cifra rutaOrigen → rutaDestino con la contraseña dada (streaming, soporta archivos grandes).</summary>
        public static void Cifrar(string rutaOrigen, string rutaDestino, string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            try
            {
                using (var aes = Aes.Create())
                {
                    aes.KeySize = 128;
                    aes.Mode    = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;
                    aes.Key     = DerivarClave(password, salt);
                    aes.GenerateIV();

                    using (var fsOut = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write))
                    {
                        fsOut.Write(salt,   0, salt.Length);
                        fsOut.Write(aes.IV, 0, aes.IV.Length);
                        using (var encryptor = aes.CreateEncryptor())
                        using (var cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write))
                        using (var fsIn = new FileStream(rutaOrigen, FileMode.Open, FileAccess.Read))
                            fsIn.CopyTo(cs);
                    }
                }
            }
            catch
            {
                // Si la operación falla a mitad de camino (disco lleno, etc.), no debe quedar un
                // archivo con el nombre "definitivo" pero corrupto/parcial — podía confundir a un
                // admin más adelante haciéndole creer que es un backup válido.
                BorrarSiExiste(rutaDestino);
                throw;
            }
        }

        /// <summary>
        /// Descifra rutaOrigen → rutaDestino. Lanza CryptographicException si la contraseña es
        /// incorrecta o el archivo está dañado.
        /// </summary>
        public static void Descifrar(string rutaOrigen, string rutaDestino, string password)
        {
            try
            {
                using (var fsIn = new FileStream(rutaOrigen, FileMode.Open, FileAccess.Read))
                {
                    byte[] salt = LeerExacto(fsIn, SaltSize);
                    byte[] iv   = LeerExacto(fsIn, IvSize);

                    using (var aes = Aes.Create())
                    {
                        aes.KeySize = 128;
                        aes.Mode    = CipherMode.CBC;
                        aes.Padding = PaddingMode.PKCS7;
                        aes.Key     = DerivarClave(password, salt);
                        aes.IV      = iv;

                        using (var decryptor = aes.CreateDecryptor())
                        using (var cs = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read))
                        using (var fsOut = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write))
                            cs.CopyTo(fsOut);   // contraseña incorrecta → CryptographicException acá
                    }
                }
            }
            catch
            {
                // Contraseña incorrecta (padding inválido) u otro fallo a mitad de la escritura:
                // no debe quedar un archivo parcial/corrupto con el nombre "definitivo".
                BorrarSiExiste(rutaDestino);
                throw;
            }
        }

        private static void BorrarSiExiste(string ruta)
        {
            try { if (File.Exists(ruta)) File.Delete(ruta); }
            catch { /* best-effort: no tapar la excepción original por no poder limpiar */ }
        }

        private static byte[] LeerExacto(Stream s, int n)
        {
            byte[] buf = new byte[n];
            int off = 0;
            while (off < n)
            {
                int r = s.Read(buf, off, n - off);
                if (r == 0) throw new CryptographicException("El archivo de backup es inválido o está dañado.");
                off += r;
            }
            return buf;
        }
    }
}
