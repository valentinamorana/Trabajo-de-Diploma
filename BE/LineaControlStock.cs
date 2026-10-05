namespace BE
{
    /// <summary>
    /// PN01 — una línea de la "Planilla de control de existencias" que revisa Depósito
    /// ("Revisar stock de las prendas"): la prenda del pedido con su estado real, releído
    /// de la base en el momento de la revisión.
    /// </summary>
    public class LineaControlStock
    {
        public Prenda Prenda { get; set; }

        // Estado real de la prenda al revisar el stock.
        public EstadoPrenda EstadoActual { get; set; }

        // Reservada por Lista de Espera para otro cliente (aunque su estado sea Disponible).
        public bool ReservadaParaOtro { get; set; }

        // Depósito ya la confirmó como disponible (PedidoPrenda.Confirmada).
        public bool Confirmada { get; set; }

        // ¿Selección disponible? (para esta prenda).
        public bool Disponible => EstadoActual == EstadoPrenda.Disponible && !ReservadaParaOtro;
    }
}
