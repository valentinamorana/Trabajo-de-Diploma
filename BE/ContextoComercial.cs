using System;
using System.Collections.Generic;

namespace BE
{
    // ══════════════════════════════════════════════════════════════════════════
    // Filas crudas que lee DAL.InteligenciaComercial (solo lectura, sin lógica)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Cliente activo con su suscripción (para MRR, vencimientos y uso del plan).</summary>
    public class ClienteComercial
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }
        public int? IdPlan { get; set; }
        public DateTime? FechaVencimiento { get; set; }
        public DateTime? FechaPausaHasta { get; set; }
        /// <summary>Fin del período de gracia tras un cobro fallido (null = al día). Ver BE.Cliente.</summary>
        public DateTime? FechaLimiteGracia { get; set; }
        /// <summary>Prendas que tiene en uso hoy (uso del cupo del plan).</summary>
        public int PrendasEnUso { get; set; }
        public string NombreCompleto => $"{Apellido}, {Nombre}".Trim(' ', ',');

        /// <summary>
        /// ¿Paga hoy su plan? Es la base del MRR: tiene plan y NO está pausado, ni con la suscripción
        /// vencida, ni suspendido por falta de pago (gracia vencida). Mismos criterios que BE.Cliente
        /// (EstaPausada, VencimientoExpirado, EstaSuspendidoPorPago), con el día como parámetro.
        /// </summary>
        public bool PagaAl(DateTime dia) =>
            IdPlan.HasValue
            && !Cliente.PausaVigente(FechaPausaHasta, dia)
            && !Cliente.VencimientoExpiradoAl(FechaVencimiento, dia)
            && !Cliente.SuspendidoPorPagoAl(FechaLimiteGracia, dia);
    }

    /// <summary>Decisión de suscripción resuelta (HistorialRenovacion): renovación, cambio de plan, baja o pausa.</summary>
    public class MovimientoSuscripcion
    {
        public int IdCliente { get; set; }
        public int? IdPlanAnterior { get; set; }
        public int? IdPlanNuevo { get; set; }
        public DateTime Fecha { get; set; }
        public EstadoRenovacion Resultado { get; set; }
    }

    /// <summary>Alta de un suscriptor: primer pago de una contratación (o fecha de alta del cliente).</summary>
    public class AltaSuscripcion
    {
        public int IdCliente { get; set; }
        public int? IdPlan { get; set; }
        public DateTime Fecha { get; set; }
    }

    /// <summary>Un cobro concretado (contratación de PN02 o cobro recurrente de N01).</summary>
    public class CobroComercial
    {
        public DateTime Fecha { get; set; }
        public decimal Importe { get; set; }
        public decimal Descuento { get; set; }
        public int? IdPromocion { get; set; }
        public int? IdPlan { get; set; }
    }

    /// <summary>Prenda del catálogo con su demanda en el período (pedidos y lista de espera).</summary>
    public class PrendaComercial
    {
        public int IdPrenda { get; set; }
        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public string Talle { get; set; }
        public EstadoPrenda Estado { get; set; }
        public DateTime FechaAlta { get; set; }
        public decimal? PrecioReposicion { get; set; }
        public int PedidosPeriodo { get; set; }
        public int EnListaEspera { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════
    // Contexto que consume el motor de recomendaciones (ya calculado por la BLL)
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>Métricas de un plan en el período y en el anterior de igual duración.</summary>
    public class MetricaPlanComercial
    {
        public int IdPlan { get; set; }
        public string Nombre { get; set; }
        public decimal Precio { get; set; }
        public int LimitePrendas { get; set; }
        /// <summary>Clientes activos hoy en el plan (reciben el descuento si se lanza una promoción).</summary>
        public int ClientesActivos { get; set; }
        /// <summary>Clientes al FIN del período, reconstruidos desde hoy: activos hoy − altas posteriores
        /// al período + bajas posteriores al período (igual a ClientesActivos si el período termina hoy).</summary>
        public int ClientesFinPeriodo { get; set; }
        /// <summary>Clientes al inicio del período = clientes al fin − altas + bajas (reconstrucción).</summary>
        public int ClientesInicio { get; set; }
        public int Bajas { get; set; }
        public int ClientesInicioAnterior { get; set; }
        public int BajasAnterior { get; set; }
        /// <summary>% de bajas del período (bajas / clientes al inicio); null si no había clientes.</summary>
        public decimal? Churn { get; set; }
        public decimal? ChurnAnterior { get; set; }
        public int ClientesEnRiesgo { get; set; }
        public decimal IngresoEnRiesgo => ClientesEnRiesgo * Precio;
        /// <summary>Mejora de retención promedio (pp) que lograron las promociones ya medidas del plan.</summary>
        public decimal? RetencionHistorica { get; set; }
        public int PromocionesMedidas { get; set; }
        public bool TienePromocionVigente { get; set; }
    }

    /// <summary>Cliente con la suscripción vencida o por vencer (renovación en juego).</summary>
    public class ClienteVencimiento
    {
        public int IdCliente { get; set; }
        public string Nombre { get; set; }
        public string Email { get; set; }
        public int? IdPlan { get; set; }
        public string NombrePlan { get; set; }
        public decimal Precio { get; set; }
        public DateTime FechaVencimiento { get; set; }
        /// <summary>Días hasta el vencimiento (negativo: vencida hace N días).</summary>
        public int DiasRestantes { get; set; }
        public bool Vencida => DiasRestantes < 0;
    }

    /// <summary>
    /// Todo lo que el motor de recomendaciones necesita para decidir, ya calculado. Las reglas son
    /// puras sobre este objeto: se prueban armándolo a mano, sin base de datos.
    /// </summary>
    public class ContextoComercial
    {
        public DateTime Hoy { get; set; }
        public DateTime Desde { get; set; }
        public DateTime Hasta { get; set; }
        public int DiasPeriodo => Math.Max(1, (int)(Hasta.Date - Desde.Date).TotalDays + 1);

        /// <summary>Valores de los umbrales configurables (clave → valor). Ver BLL.Inteligencia.CatalogoUmbrales.</summary>
        public Dictionary<string, decimal> Umbrales { get; set; } = new Dictionary<string, decimal>();

        public List<MetricaPlanComercial> Planes { get; set; } = new List<MetricaPlanComercial>();
        public List<ClienteComercial> Clientes { get; set; } = new List<ClienteComercial>();
        public List<ClienteVencimiento> Vencimientos { get; set; } = new List<ClienteVencimiento>();
        public List<PrendaComercial> Prendas { get; set; } = new List<PrendaComercial>();
        public List<TiempoMantenimientoPrenda> Mantenimientos { get; set; } = new List<TiempoMantenimientoPrenda>();
        public List<MedicionPromocion> Promociones { get; set; } = new List<MedicionPromocion>();

        public decimal TicketPromedio { get; set; }
        public decimal OcupacionPorcentaje { get; set; }

        /// <summary>Valor de un umbral; si falta, el valor por defecto indicado.</summary>
        public decimal Umbral(string clave, decimal porDefecto)
            => Umbrales != null && Umbrales.TryGetValue(clave, out var v) ? v : porDefecto;
    }
}
