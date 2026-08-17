namespace BE
{
    /// <summary>PdN12 — Stock Disponible agrupado por combinación Talle+Categoría (dato crudo,
    /// sin evaluar contra el umbral). Insumo de BLL.AnalisisEscasez.</summary>
    public class StockPorTalleCategoria
    {
        public string Talle { get; set; }
        public string Categoria { get; set; }
        public int CantidadDisponible { get; set; }
    }
}
