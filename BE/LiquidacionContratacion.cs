using System;
using System.Collections.Generic;
using System.Linq;

namespace BE
{
    /// <summary>Resultado de resolver qué descuento corresponde a un cobro.</summary>
    public class ResultadoDescuento
    {
        /// <summary>Importe bruto sobre el que se calculó (precio del plan por cobro).</summary>
        public decimal Bruto { get; set; }

        /// <summary>Descuento aplicado (0 si no corresponde ninguno).</summary>
        public decimal Descuento { get; set; }

        /// <summary>Upgrade: crédito por los días no usados del plan actual (BLL.Politicas.PoliticaCambioPlan); 0 si no corresponde.</summary>
        public decimal CreditoCambioPlan { get; set; }

        /// <summary>Upgrade (plan más caro con período vigente): el período del plan nuevo arranca hoy, haya o no crédito.</summary>
        public bool EsUpgrade { get; set; }

        /// <summary>Plan igual o más barato con el período vigente: fecha desde la que rige (null si rige ya).</summary>
        public DateTime? CambioProgramadoDesde { get; set; }

        /// <summary>Importe a cobrar sin cargos adicionales: Bruto - Descuento - crédito por cambio de plan (nunca negativo).</summary>
        public decimal Total => Math.Max(0, Bruto - Descuento - CreditoCambioPlan);

        /// <summary>Promoción vigente aplicada, o null si se aplicó el crédito por referido (o ninguno).</summary>
        public Promocion Promocion { get; set; }

        /// <summary>true si el descuento aplicado es el crédito acumulado por referidos.</summary>
        public bool UsaCreditoReferido { get; set; }
    }

    /// <summary>Liquidación de una contratación: qué se cobra, con qué descuento y qué comprobante se emitió.</summary>
    public class LiquidacionContratacion
    {
        public decimal Bruto { get; set; }
        public decimal Descuento { get; set; }
        /// <summary>Upgrade: crédito por los días no usados del plan actual; el plan nuevo rige desde hoy.</summary>
        public decimal CreditoCambioPlan { get; set; }
        /// <summary>Plan igual o más barato con el período vigente: rige desde esta fecha (nodo a11 de PN02).</summary>
        public System.DateTime? CambioProgramadoDesde { get; set; }
        /// <summary>PN04: cargos por daño o pérdida pendientes del cliente que se suman a este cobro.</summary>
        public decimal Cargos { get; set; }
        public int CantidadCargos { get; set; }
        /// <summary>Período (Bruto - Descuento - crédito, nunca negativo) más los cargos pendientes.</summary>
        public decimal Total => System.Math.Max(0, Bruto - Descuento - CreditoCambioPlan) + Cargos;
        public string NombrePromocion { get; set; }
        public bool UsaCreditoReferido { get; set; }
        /// <summary>Número de comprobante emitido (null mientras solo se está calculando el importe).</summary>
        public string NumeroComprobante { get; set; }

        /// <summary>Período activado por el cobro (Constancia de suscripción).</summary>
        public System.DateTime? VigenciaDesde { get; set; }
        public System.DateTime? VigenciaHasta { get; set; }

        /// <summary>"¿Referido? Sí → Acreditar crédito": el referente al que se le acreditó el
        /// beneficio con este cobro, o null si no correspondía.</summary>
        public string ReferenteAcreditado { get; set; }

        /// <summary>Pago en cuotas con Tarjeta de crédito (1 = un solo pago). El recargo se suma al Total.</summary>
        public int CantidadCuotas { get; set; } = 1;
        public decimal RecargoPorcentaje { get; set; }
        public decimal RecargoCuotas { get; set; }
        public decimal ValorCuota { get; set; }

        /// <summary>Lo que abona el cliente: Total (con el descuento) + recargo por cuotas.</summary>
        public decimal TotalConRecargo => Total + RecargoCuotas;

        /// <summary>Copia el «Detalle de financiación» en la liquidación.</summary>
        public void AplicarFinanciacion(FinanciacionCuotas f)
        {
            if (f == null) return;
            CantidadCuotas = f.CantidadCuotas;
            RecargoPorcentaje = f.RecargoPorcentaje;
            RecargoCuotas = f.Recargo;
            ValorCuota = f.ValorCuota;
        }
    }

}
