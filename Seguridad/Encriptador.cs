using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Seguridad
{
    /// <summary>
    /// Centraliza la criptografia del sistema.
    /// PBKDF2-SHA256 para contrasenas (unidireccional).
    /// </summary>
    public static class Encriptador
    {
        // ── PBKDF2-SHA256 — hash unidireccional para contrasenas ──────────────

        private const int SaltSize   = 16;
        private const int HashSize   = 32;
        private const int Iterations = 100000;

        /// <summary>
        /// Genera un hash de la contrasena para guardar en BD.
        /// Formato: Base64( Salt[16] + Hash[32] ).
        /// </summary>
        public static string Hash(string contrasena)
        {
            byte[] salt = new byte[SaltSize];
            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                contrasena, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(HashSize);

                byte[] hashBytes = new byte[SaltSize + HashSize];
                Array.Copy(salt, 0, hashBytes, 0,        SaltSize);
                Array.Copy(hash, 0, hashBytes, SaltSize, HashSize);

                return ConvertirBase64(hashBytes);
            }
        }

        /// <summary>
        /// Verifica si la contrasena ingresada coincide con el hash almacenado en BD.
        /// Extrae el Salt, rehashea y compara byte a byte.
        /// </summary>
        public static bool VerificarContrasena(string contrasenaIngresada, string hashAlmacenado)
        {
            byte[] hashBytes = Convert.FromBase64String(hashAlmacenado);

            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            using (var pbkdf2 = new Rfc2898DeriveBytes(
                contrasenaIngresada, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] hashCalculado = pbkdf2.GetBytes(HashSize);

                // Comparación en TIEMPO CONSTANTE: recorre siempre los 32 bytes y acumula
                // las diferencias con XOR, en vez de cortar en el primer byte distinto. Así
                // el tiempo de respuesta no filtra cuántos bytes coincidieron (canal lateral).
                int diferencia = 0;
                for (int i = 0; i < HashSize; i++)
                    diferencia |= hashBytes[i + SaltSize] ^ hashCalculado[i];
                return diferencia == 0;
            }
        }

        // Hash "señuelo" precalculado una sola vez. Se usa para verificar contra un hash
        // VÁLIDO cuando el usuario no existe, de modo que el login consuma el mismo tiempo
        // de PBKDF2 que con un usuario real y no se pueda enumerar usuarios por temporización.
        private static readonly string _hashSenuelo = Hash("\0senuelo-sin-usuario\0");

        /// <summary>
        /// Ejecuta una verificación PBKDF2 contra el hash señuelo. Siempre devuelve false;
        /// su único objetivo es igualar el costo temporal del camino "usuario inexistente".
        /// </summary>
        public static bool VerificacionSenuelo(string contrasena)
        {
            return VerificarContrasena(contrasena, _hashSenuelo);
        }

        // ── Validacion de requisitos de contraseña ────────────────────────────

        // Requisitos: minimo 8 caracteres, al menos 1 numero y 1 caracter especial.
        // Devuelve también la CLAVE de traducción del motivo (una por requisito incumplido),
        // para que la GUI pueda mostrar el mensaje específico traducido al idioma activo
        // en vez de un texto genérico (ver BLL.Usuario.CambiarClavePropia).
        public static (bool valida, string clave, string mensaje) ValidarContrasena(string contrasena)
        {
            if (string.IsNullOrWhiteSpace(contrasena) || contrasena.Length < 8)
                return (false, "err.bll.usuario.clave_corta", "La contrasena debe tener al menos 8 caracteres.");

            bool tieneNumero   = false;
            bool tieneEspecial = false;
            const string especiales = "!@#$%^&*()_+-=[]{}|;:',.<>?/";

            foreach (char c in contrasena)
            {
                if (char.IsDigit(c))            tieneNumero   = true;
                if (especiales.IndexOf(c) >= 0) tieneEspecial = true;
            }

            if (!tieneNumero)
                return (false, "err.bll.usuario.clave_sinnumero", "La contrasena debe contener al menos un numero.");
            if (!tieneEspecial)
                return (false, "err.bll.usuario.clave_sinespecial", "La contrasena debe contener al menos un caracter especial (!@#$%...).");

            return (true, null, string.Empty);
        }

        // Wrapper trivial sobre Convert.ToBase64String, sin uso fuera de esta clase — bajado a
        // private (era public sin ningún consumidor externo).
        private static string ConvertirBase64(byte[] data)
        {
            return Convert.ToBase64String(data);
        }
    }
}
