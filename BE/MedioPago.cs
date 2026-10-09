namespace BE
{
    /// <summary>
    /// PN02 — Catálogo de medios de pago con los que el cliente abona en Caja (Efectivo,
    /// Tarjeta de débito, Transferencia, Tarjeta de crédito). Tabla propia en vez de texto libre en la contratación (3FN): el
    /// nombre canónico vive una sola vez y la pantalla lo traduce con <see cref="ClaveTraduccion"/>.
    /// </summary>
    public class MedioPago
    {
        /// <summary>Id fijo de «Efectivo» en el catálogo (lo siembra la sección 20c del script).</summary>
        public const int IdEfectivo = 1;

        public int    IdMedioPago     { get; set; }
        public string Nombre          { get; set; }
        public string ClaveTraduccion { get; set; }

        /// <summary>PN02 — Pago en cuotas: solo la Tarjeta de crédito permite financiar el cobro en
        /// cuotas (catálogo PlanCuotas). El resto de los medios se cobra en un solo pago.</summary>
        public bool   PermiteCuotas   { get; set; }

        /// <summary>false = medio histórico: figura en cobros e intentos viejos pero ya no se ofrece ni se
        /// acepta en un cobro nuevo (p. ej. «Tarjeta», de antes de separar débito y crédito).</summary>
        public bool   Activo          { get; set; } = true;

        public override string ToString() => Nombre;
    }
}
