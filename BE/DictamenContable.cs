using System;

namespace BE
{
    /// <summary>
    /// PN03 — «Dictamen contable»: resultado de "Analizar margen e impacto" → ¿Aprueba?
    /// Una fila por cada decisión de Contabilidad (una promoción reformulada puede tener varios).
    /// </summary>
    public class DictamenContable
    {
        public int IdDictamen { get; set; }
        public int IdPromocion { get; set; }
        public int IdUsuario { get; set; }
        public bool Aprobada { get; set; }
        public string Observacion { get; set; }
        public DateTime Fecha { get; set; }

        /// <summary>Cargado por JOIN con Usuario, no persiste.</summary>
        public string NombreUsuario { get; set; }
    }
}
