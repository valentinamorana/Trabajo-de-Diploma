namespace BE
{
    /// <summary>
    /// PN03 — Umbral configurable del motor de recomendaciones (se guarda en ParametroSistema con la
    /// clave "Comercial.&lt;Clave&gt;"). Lleva su rango válido y la explicación que muestra el diálogo
    /// "Umbrales" (clave de traducción "umbral.&lt;clave&gt;.ayuda").
    /// </summary>
    public class UmbralComercial
    {
        public string Clave { get; set; }
        public decimal Valor { get; set; }
        public decimal PorDefecto { get; set; }
        public decimal Minimo { get; set; }
        public decimal Maximo { get; set; }
        /// <summary>Cantidad de decimales que admite (0 = entero).</summary>
        public int Decimales { get; set; }
        public TextoTraducible Nombre { get; set; }
        public TextoTraducible Explicacion { get; set; }
    }
}
