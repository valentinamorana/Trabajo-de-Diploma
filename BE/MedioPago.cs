namespace BE
{
    /// <summary>
    /// PN02 — Catálogo de medios de pago con los que el cliente abona en Caja (Efectivo,
    /// Tarjeta, Transferencia). Tabla propia en vez de texto libre en la contratación (3FN): el
    /// nombre canónico vive una sola vez y la pantalla lo traduce con <see cref="ClaveTraduccion"/>.
    /// </summary>
    public class MedioPago
    {
        public int    IdMedioPago     { get; set; }
        public string Nombre          { get; set; }
        public string ClaveTraduccion { get; set; }

        public override string ToString() => Nombre;
    }
}
