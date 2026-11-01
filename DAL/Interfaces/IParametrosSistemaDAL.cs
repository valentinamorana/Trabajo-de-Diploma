using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>
    /// Clave/valor de la tabla ParametroSistema por prefijo: los parámetros del análisis del Centro de
    /// decisiones («Comercial.*»), el «No mostrar hoy» de los avisos comerciales y las alertas leídas por
    /// usuario («Alerta.Leida.&lt;IdUsuario&gt;.*»). Lo implementa DAL.InteligenciaComercial.
    /// </summary>
    public interface IParametrosSistemaDAL
    {
        /// <summary>Parámetros de ParametroSistema cuya clave empieza con el prefijo (clave sin prefijo → valor).</summary>
        Dictionary<string, string> ObtenerParametros(string prefijo);

        /// <summary>Guarda (alta o modificación) los parámetros en una sola transacción.</summary>
        void GuardarParametros(string prefijo, Dictionary<string, string> valores);
    }
}
