using System;
using System.Collections.Generic;

namespace DAL.Interfaces
{
    /// <summary>
    /// PN03 «Inteligencia comercial» — lecturas crudas para el Centro de decisiones comerciales
    /// (KPI, tendencia, reglas). Solo lectura salvo los umbrales: todos los cálculos viven en la BLL
    /// (BLL.Inteligencia), así se prueban con un doble sin base de datos. Los rangos de fechas son
    /// [desde, hastaExcl).
    /// </summary>
    public interface IInteligenciaComercialDAL
    {
        /// <summary>Clientes activos con su plan, vencimiento y prendas en uso.</summary>
        List<BE.ClienteComercial> ObtenerClientes();

        /// <summary>Renovaciones resueltas (renovada, cambio de plan, baja, pausa) en el rango.</summary>
        List<BE.MovimientoSuscripcion> ObtenerMovimientos(DateTime desde, DateTime hastaExcl);

        /// <summary>Altas de suscriptores (primer pago o alta del cliente) en el rango.</summary>
        List<BE.AltaSuscripcion> ObtenerAltas(DateTime desde, DateTime hastaExcl);

        /// <summary>Cobros concretados (PN02 + N01) en el rango.</summary>
        List<BE.CobroComercial> ObtenerCobros(DateTime desde, DateTime hastaExcl);

        /// <summary>Catálogo de prendas con sus pedidos en el rango y su lista de espera activa.</summary>
        List<BE.PrendaComercial> ObtenerPrendas(DateTime desde, DateTime hastaExcl);

        /// <summary>Parámetros de ParametroSistema cuya clave empieza con el prefijo (clave sin prefijo → valor).</summary>
        Dictionary<string, string> ObtenerParametros(string prefijo);

        /// <summary>Guarda (alta o modificación) los parámetros en una sola transacción.</summary>
        void GuardarParametros(string prefijo, Dictionary<string, string> valores);
    }
}
