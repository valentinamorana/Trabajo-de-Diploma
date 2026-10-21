using System.Drawing;
using static GUI.Inteligencia.TextosComerciales;

namespace GUI.Inteligencia
{
    /// <summary>Etiquetas y colores de las metas comerciales (semáforo, valores con su unidad). Solo presentación.</summary>
    internal static class TextosMetas
    {
        public static string ValorMeta(BE.UnidadKpi unidad, decimal? valor) =>
            valor.HasValue ? ValorKpi(unidad, valor.Value) : "—";

        public static string EstadoMeta(BE.EstadoMeta e)
        {
            switch (e)
            {
                case BE.EstadoMeta.Cumplida: return Tr("cdc.meta.estado.cumplida", "Cumplida");
                case BE.EstadoMeta.EnCamino: return Tr("cdc.meta.estado.encamino", "En camino");
                case BE.EstadoMeta.EnRiesgo: return Tr("cdc.meta.estado.enriesgo", "En riesgo");
                case BE.EstadoMeta.NoCumplida: return Tr("cdc.meta.estado.nocumplida", "No cumplida");
                case BE.EstadoMeta.Pendiente: return Tr("cdc.meta.estado.pendiente", "Pendiente");
                default: return Tr("cdc.meta.estado.sindatos", "Sin datos");
            }
        }

        /// <summary>Colores del semáforo (fondo, texto): verde, azul, ámbar o rojo.</summary>
        public static (Color Fondo, Color Texto) ColorEstado(BE.EstadoMeta e)
        {
            switch (e)
            {
                case BE.EstadoMeta.Cumplida: return (Tema.FondoExito, Tema.Exito);
                case BE.EstadoMeta.EnCamino: return (Tema.FondoInfo, Tema.Info);
                case BE.EstadoMeta.EnRiesgo: return (Tema.FondoAlerta, Tema.Alerta);
                case BE.EstadoMeta.NoCumplida: return (Tema.FondoError, Tema.Error);
                default: return (Tema.PanelClaro, Tema.TextoMuted);
            }
        }

        public static string Indicador(BE.IndicadorMeta indicador) =>
            R(BLL.Inteligencia.CatalogoIndicadores.De(indicador).NombreTraducible);
    }
}
