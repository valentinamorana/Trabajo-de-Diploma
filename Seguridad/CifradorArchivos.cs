using System;
using System.IO;
using System.Security.Cryptography;

namespace Seguridad
{
    /// <summary>Formato con el que estaba guardado un archivo cifrado que se descifró.</summary>
    public enum FormatoArchivoCifrado
    {
        /// <summary>Formato v1 (anterior): solo AES-CBC, SIN código de autenticación. Se sigue
        /// pudiendo leer para no perder backups viejos, pero no detecta alteraciones.</summary>
        V1SinAutenticacion = 1,

        /// <summary>Formato v2: AES-CBC + HMAC-SHA256 (encrypt-then-MAC). Verificado.</summary>
        V2Autenticado = 2
    }

    /// <summary>
    /// El archivo cifrado (formato v2) no pasó la verificación HMAC: fue alterado o truncado
    /// después de generarse. Deriva de CryptographicException para que un llamador que solo
    /// conoce esa excepción igual lo rechace.
    /// </summary>
    [Serializable]
    public class ArchivoCifradoAlteradoException : CryptographicException
    {
        public ArchivoCifradoAlteradoException(string mensaje) : base(mensaje) { }
        protected ArchivoCifradoAlteradoException(
            System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    /// <summary>
    /// Cifrado de ARCHIVOS con contraseña, para backups portables (patrón tomado del BackupService
    /// de Stach). Deriva las claves de la contraseña con PBKDF2-SHA256 y cifra en AES-CBC.
    ///
    /// Formato v2 (el que se ESCRIBE hoy), encrypt-then-MAC:
    ///   [ "WFBK"(4) ][ versión=2 (1) ][ salt(16) ][ verificador(16) ][ IV(16) ][ ciphertext ][ HMAC(32) ]
    ///   • PBKDF2(contraseña, salt) da 64 bytes que se parten en tres claves independientes:
    ///     AES-128 (16) | HMAC-SHA256 (32) | verificador de contraseña (16).
    ///   • El HMAC-SHA256 cubre TODO lo anterior (cabecera + IV + ciphertext) y se verifica
    ///     ANTES de descifrar: un archivo alterado o truncado se rechaza sin tocar el descifrado.
    ///   • El verificador solo sirve para distinguir "contraseña incorrecta" de "archivo
    ///     alterado" en el mensaje al usuario; la garantía de integridad la da el HMAC (si alguien
    ///     altera el verificador, el archivo igual se rechaza).
    ///
    /// Formato v1 (legacy, solo LECTURA): [ salt(16) ][ IV(16) ][ ciphertext ] — sin autenticación.
    /// Se detecta por la ausencia de la cabecera "WFBK"+2; <see cref="Descifrar"/> informa el
    /// formato leído para que la capa superior deje constancia de que no se pudo verificar.
    ///
    /// A diferencia de <see cref="Encriptador"/>, acá la clave depende SOLO de la contraseña,
    /// de modo que el backup se puede restaurar en otra máquina conociendo la contraseña.
    /// </summary>
    public static class CifradorArchivos
    {
        private static readonly byte[] Magia = { (byte)'W', (byte)'F', (byte)'B', (byte)'K' };
        private const byte VersionAutenticada = 2;

        private const int SaltSize        = 16;
        private const int IvSize          = 16;
        private const int KeySize         = 16;      // AES-128
        private const int MacKeySize      = 32;      // HMAC-SHA256
        private const int VerificadorSize = 16;
        private const int MacSize         = 32;
        private const int Iterations      = 100000;

        // Largo de la cabecera v2 hasta el comienzo del ciphertext.
        private const int CabeceraV2 = 4 + 1 + SaltSize + VerificadorSize + IvSize;   // 53

        // ── Derivación de claves ────────────────────────────────────────────────────────

        // v1: solo la clave AES (comportamiento histórico, sin cambios para poder leer los viejos).
        private static byte[] DerivarClaveV1(string password, byte[] salt)
        {
            using (var kdf = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256))
                return kdf.GetBytes(KeySize);
        }

        // v2: una sola derivación PBKDF2 de 64 bytes partida en tres claves que no se solapan.
        private static void DerivarClavesV2(string password, byte[] salt,
                                            out byte[] claveAes, out byte[] claveMac, out byte[] verificador)
        {
            byte[] material;
            using (var kdf = new Rfc2898DeriveBytes(password ?? string.Empty, salt, Iterations, HashAlgorithmName.SHA256))
                material = kdf.GetBytes(KeySize + MacKeySize + VerificadorSize);

            claveAes    = new byte[KeySize];
            claveMac    = new byte[MacKeySize];
            verificador = new byte[VerificadorSize];
            Buffer.BlockCopy(material, 0,                    claveAes,    0, KeySize);
            Buffer.BlockCopy(material, KeySize,              claveMac,    0, MacKeySize);
            Buffer.BlockCopy(material, KeySize + MacKeySize, verificador, 0, VerificadorSize);
            Array.Clear(material, 0, material.Length);
        }

        private static Aes CrearAes(byte[] clave)
        {
            var aes = Aes.Create();
            aes.KeySize = 128;
            aes.Mode    = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key     = clave;
            return aes;
        }

        // ── Cifrado (siempre formato v2) ────────────────────────────────────────────────

        /// <summary>
        /// Cifra rutaOrigen → rutaDestino con la contraseña dada, en formato v2 autenticado
        /// (streaming, soporta archivos grandes).
        /// </summary>
        public static void Cifrar(string rutaOrigen, string rutaDestino, string password)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            DerivarClavesV2(password, salt, out byte[] claveAes, out byte[] claveMac, out byte[] verificador);
            try
            {
                // 1) Cabecera + ciphertext.
                using (var aes = CrearAes(claveAes))
                {
                    aes.GenerateIV();
                    using (var fsOut = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write))
                    {
                        fsOut.Write(Magia, 0, Magia.Length);
                        fsOut.WriteByte(VersionAutenticada);
                        fsOut.Write(salt,        0, salt.Length);
                        fsOut.Write(verificador, 0, verificador.Length);
                        fsOut.Write(aes.IV,      0, aes.IV.Length);
                        using (var encryptor = aes.CreateEncryptor())
                        using (var cs = new CryptoStream(fsOut, encryptor, CryptoStreamMode.Write))
                        using (var fsIn = new FileStream(rutaOrigen, FileMode.Open, FileAccess.Read))
                            fsIn.CopyTo(cs);
                    }
                }

                // 2) Encrypt-then-MAC: HMAC de todo lo escrito, agregado al final.
                using (var fs = new FileStream(rutaDestino, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                using (var hmac = new HMACSHA256(claveMac))
                {
                    byte[] tag = hmac.ComputeHash(fs);   // lee hasta el final: queda posicionado ahí
                    fs.Write(tag, 0, tag.Length);
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
            finally
            {
                Array.Clear(claveAes, 0, claveAes.Length);
                Array.Clear(claveMac, 0, claveMac.Length);
            }
        }

        // ── Descifrado (v2 verificado, v1 legacy) ───────────────────────────────────────

        /// <summary>
        /// Descifra rutaOrigen → rutaDestino y devuelve el formato que tenía el archivo.
        /// • v2: verifica el HMAC ANTES de descifrar. Lanza <see cref="ArchivoCifradoAlteradoException"/>
        ///   si el archivo fue alterado o truncado, y CryptographicException si la contraseña es
        ///   incorrecta.
        /// • v1 (legacy, sin HMAC): lo descifra como antes; una contraseña incorrecta o un archivo
        ///   dañado dan CryptographicException (padding inválido), pero una alteración que no rompa
        ///   el padding NO se detecta — por eso se devuelve <see cref="FormatoArchivoCifrado.V1SinAutenticacion"/>.
        /// </summary>
        public static FormatoArchivoCifrado Descifrar(string rutaOrigen, string rutaDestino, string password)
        {
            try
            {
                using (var fsIn = new FileStream(rutaOrigen, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    if (TieneCabeceraV2(fsIn))
                    {
                        DescifrarV2(fsIn, rutaDestino, password);
                        return FormatoArchivoCifrado.V2Autenticado;
                    }

                    fsIn.Position = 0;
                    DescifrarV1(fsIn, rutaDestino, password);
                    return FormatoArchivoCifrado.V1SinAutenticacion;
                }
            }
            catch
            {
                // Contraseña incorrecta, archivo alterado u otro fallo a mitad de la escritura:
                // no debe quedar un archivo parcial/corrupto con el nombre "definitivo".
                BorrarSiExiste(rutaDestino);
                throw;
            }
        }

        // ¿Empieza con "WFBK" + versión 2? Un v1 empieza con un salt aleatorio: la probabilidad de
        // que coincida por azar con estos 5 bytes es 2^-40 (despreciable).
        private static bool TieneCabeceraV2(Stream s)
        {
            if (s.Length < Magia.Length + 1) return false;
            byte[] cab = new byte[Magia.Length + 1];
            s.Position = 0;
            if (s.Read(cab, 0, cab.Length) != cab.Length) return false;
            for (int i = 0; i < Magia.Length; i++)
                if (cab[i] != Magia[i]) return false;
            return cab[Magia.Length] == VersionAutenticada;
        }

        private static void DescifrarV2(FileStream fsIn, string rutaDestino, string password)
        {
            // Mínimo: cabecera + un bloque AES + HMAC.
            long largoCifrado = fsIn.Length - CabeceraV2 - MacSize;
            if (largoCifrado < 16 || largoCifrado % 16 != 0)
                throw new ArchivoCifradoAlteradoException("El archivo de backup está truncado o fue alterado.");

            fsIn.Position = Magia.Length + 1;
            byte[] salt        = LeerExacto(fsIn, SaltSize);
            byte[] verifLeido  = LeerExacto(fsIn, VerificadorSize);
            byte[] iv          = LeerExacto(fsIn, IvSize);

            DerivarClavesV2(password, salt, out byte[] claveAes, out byte[] claveMac, out byte[] verifEsperado);
            try
            {
                if (!IgualesTiempoConstante(verifLeido, verifEsperado))
                    throw new CryptographicException("La contraseña del backup es incorrecta.");

                // 1) Verificar el HMAC sobre [0, largo - 32) ANTES de descifrar nada.
                byte[] tagCalculado;
                fsIn.Position = 0;
                using (var hmac = new HMACSHA256(claveMac))
                {
                    long restante = fsIn.Length - MacSize;
                    byte[] buf = new byte[81920];
                    while (restante > 0)
                    {
                        int n = fsIn.Read(buf, 0, (int)Math.Min(buf.Length, restante));
                        if (n <= 0) throw new ArchivoCifradoAlteradoException("El archivo de backup está truncado.");
                        hmac.TransformBlock(buf, 0, n, null, 0);
                        restante -= n;
                    }
                    hmac.TransformFinalBlock(new byte[0], 0, 0);
                    tagCalculado = hmac.Hash;
                }
                byte[] tagLeido = LeerExacto(fsIn, MacSize);
                if (!IgualesTiempoConstante(tagLeido, tagCalculado))
                    throw new ArchivoCifradoAlteradoException(
                        "El archivo de backup fue alterado: la verificación de integridad (HMAC) no coincide.");

                // 2) Descifrar solo el tramo del ciphertext (el archivo está abierto sin permitir
                //    escrituras de terceros, así que es el mismo contenido que se verificó).
                using (var aes = CrearAes(claveAes))
                {
                    aes.IV = iv;
                    fsIn.Position = CabeceraV2;
                    using (var fsOut = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write))
                    using (var decryptor = aes.CreateDecryptor())
                    using (var cs = new CryptoStream(fsOut, decryptor, CryptoStreamMode.Write))
                    {
                        long restante = largoCifrado;
                        byte[] buf = new byte[81920];
                        while (restante > 0)
                        {
                            int n = fsIn.Read(buf, 0, (int)Math.Min(buf.Length, restante));
                            if (n <= 0) throw new ArchivoCifradoAlteradoException("El archivo de backup está truncado.");
                            cs.Write(buf, 0, n);
                            restante -= n;
                        }
                        cs.FlushFinalBlock();
                    }
                }
            }
            finally
            {
                Array.Clear(claveAes, 0, claveAes.Length);
                Array.Clear(claveMac, 0, claveMac.Length);
            }
        }

        private static void DescifrarV1(Stream fsIn, string rutaDestino, string password)
        {
            byte[] salt = LeerExacto(fsIn, SaltSize);
            byte[] iv   = LeerExacto(fsIn, IvSize);

            using (var aes = CrearAes(DerivarClaveV1(password, salt)))
            {
                aes.IV = iv;
                using (var decryptor = aes.CreateDecryptor())
                using (var cs = new CryptoStream(fsIn, decryptor, CryptoStreamMode.Read))
                using (var fsOut = new FileStream(rutaDestino, FileMode.Create, FileAccess.Write))
                    cs.CopyTo(fsOut);   // contraseña incorrecta → CryptographicException acá
            }
        }

        // Comparación que no corta en el primer byte distinto (no filtra por tiempo cuántos
        // bytes del MAC coinciden). .NET Framework 4.7.2 no trae CryptographicOperations.FixedTimeEquals.
        private static bool IgualesTiempoConstante(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int dif = 0;
            for (int i = 0; i < a.Length; i++) dif |= a[i] ^ b[i];
            return dif == 0;
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
