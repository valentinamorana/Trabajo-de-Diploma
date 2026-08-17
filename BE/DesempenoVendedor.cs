namespace BE
{
    /// <summary>PdN8 — Desempeño de un vendedor: pedidos generados, entregados y cancelados.</summary>
    public class DesempenoVendedor
    {
        public int IdEmpleado { get; set; }
        public string NombreEmpleado { get; set; }
        public int TotalPedidos { get; set; }
        public int Entregados { get; set; }
        public int Cancelados { get; set; }

        public double TasaCancelacion => TotalPedidos > 0 ? (double)Cancelados / TotalPedidos * 100.0 : 0.0;
    }
}
