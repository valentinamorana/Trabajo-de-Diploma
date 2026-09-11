using System;

namespace DAL
{
    /// <summary>
    /// Whitelist de identificadores SQL (nombre de tabla/columna) armados por interpolación de
    /// string en vez de parámetro — único punto de defensa contra inyección por nombre de
    /// tabla/columna en los pocos lugares del DAL que necesitan SQL dinámico (DigitoVerificador,
    /// Backup). Antes esta misma validación vivía duplicada byte a byte en las dos clases.
    /// </summary>
    internal static class SqlIdentificador
    {
        public static string Validar(string identificador)
        {
            if (string.IsNullOrEmpty(identificador))
                throw new ArgumentException("Identificador SQL vacío.");
            foreach (char c in identificador)
                if (!(char.IsLetterOrDigit(c) || c == '_'))
                    throw new ArgumentException($"Identificador SQL inválido: '{identificador}'.");
            if (char.IsDigit(identificador[0]))
                throw new ArgumentException($"Identificador SQL inválido: '{identificador}'.");
            return "[" + identificador + "]";
        }
    }
}
