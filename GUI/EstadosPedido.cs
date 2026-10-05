using System.Drawing;
using Servicios.Multiidioma;

namespace GUI
{
    /// <summary>
    /// Etiqueta traducida y color de cada estado de pedido, compartidos por las pantallas que
    /// listan pedidos (Pedidos de Venta, Pedidos Realizados, Control de Stock, Nuevo Pedido).
    /// </summary>
    public static class EstadosPedido
    {
        public static string Etiqueta(BE.EstadoPedido estado)
        {
            var t = Traductor.ObtenerTraducciones(GestorIdioma.IdiomaActual);
            string T(string clave, string fallback) => t.ContainsKey(clave) ? t[clave].Texto : fallback;

            switch (estado)
            {
                case BE.EstadoPedido.Pendiente:      return T("est.formalizado",   "Formalizado (pendiente de despacho)");
                case BE.EstadoPedido.Despachado:     return T("est.despachado",    "Despachado");
                case BE.EstadoPedido.Entregado:      return T("est.entregado",     "Entregado");
                case BE.EstadoPedido.Cancelado:      return T("est.cancelado",     "Cancelado");
                case BE.EstadoPedido.EnControlStock: return T("est.encontrol",     "En control de stock");
                case BE.EstadoPedido.ConFaltantes:   return T("est.confaltantes",  "Con faltantes");
                case BE.EstadoPedido.Separado:       return T("est.separado",      "Separado");
                case BE.EstadoPedido.Desistido:      return T("est.desistido",     "Desistido");
                default:                             return estado.ToString();
            }
        }

        public static Color Color(BE.EstadoPedido estado)
        {
            switch (estado)
            {
                case BE.EstadoPedido.Pendiente:      return Tema.Alerta;
                case BE.EstadoPedido.Despachado:     return Tema.Info;
                case BE.EstadoPedido.Entregado:      return Tema.Exito;
                case BE.EstadoPedido.Cancelado:      return Tema.Error;
                case BE.EstadoPedido.EnControlStock: return System.Drawing.Color.FromArgb(110, 60, 150);
                case BE.EstadoPedido.ConFaltantes:   return Tema.Alerta;
                case BE.EstadoPedido.Separado:       return System.Drawing.Color.FromArgb(0, 120, 120);
                case BE.EstadoPedido.Desistido:      return Tema.TextoSecundario;
                default:                             return System.Drawing.Color.Black;
            }
        }
    }
}
