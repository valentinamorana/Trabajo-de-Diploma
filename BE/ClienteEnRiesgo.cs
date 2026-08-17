using System;

namespace BE
{
    /// <summary>PdN10 — Fila del reporte de Análisis de Abandono: un cliente que la
    /// estrategia activa marcó en riesgo, con el motivo ya resuelto.</summary>
    public class ClienteEnRiesgo
    {
        public int IdCliente { get; set; }
        public string NombreCliente { get; set; }
        public string NombrePlan { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaUltimoPedido { get; set; }

        /// <summary>Nombre de la estrategia que lo marcó (BLL.Estrategias.EstrategiaRiesgo.ToString()).</summary>
        public string Estrategia { get; set; }

        /// <summary>Motivo ya formateado en español — fallback si Clave no está en el corpus.</summary>
        public string Motivo { get; set; }

        /// <summary>Clave de traducción (Servicios.Multiidioma) para mostrar el motivo en el idioma activo.</summary>
        public string Clave { get; set; }

        /// <summary>Argumentos para string.Format sobre el texto traducido de Clave.</summary>
        public object[] Args { get; set; }
    }
}
