using System;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 «Simular» — "¿Qué pasa si…?": costo, beneficio, ROI y retención de equilibrio de una
    /// promoción de retención. Matemática pura (probada en Tests/SimuladorPromocionTests), con la misma
    /// base mensual que usa Contabilidad en "Analizar margen e impacto":
    ///
    ///   meses               = duración (días) / 30
    ///   costo del descuento = clientes del plan × precio × descuento % × meses
    ///                         (el descuento lo reciben TODOS los clientes activos del plan)
    ///   clientes retenidos  = clientes objetivo × retención %
    ///   ingreso retenido    = clientes retenidos × precio × horizonte (meses que sigue pagando)
    ///   beneficio neto      = ingreso retenido − costo del descuento
    ///   ROI                 = beneficio neto / costo × 100
    ///   retención de equilibrio = costo / (clientes objetivo × precio × horizonte) × 100
    ///                         → "la promoción conviene si retiene al menos X %"
    ///
    /// Veredicto: No conviene si pierde plata; Riesgoso si gana pero con ROI menor al mínimo o con un
    /// margen de seguridad (retención supuesta − equilibrio) menor al configurado; Conviene en otro caso.
    /// </summary>
    public static class SimuladorPromocion
    {
        public const int PasoCurva = 5;

        public static void Validar(BE.EscenarioPromocion e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            if (e.PrecioPlan <= 0)
                throw new BE.AppException("err.bll.simulador.precio", "El plan elegido no tiene precio: no se puede simular.");
            if (e.DescuentoPorcentaje <= 0 || e.DescuentoPorcentaje > 100)
                throw new BE.AppException("err.bll.simulador.descuento", "El descuento debe ser mayor a 0 y hasta 100 %.");
            if (e.DuracionDias < 1 || e.DuracionDias > 365)
                throw new BE.AppException("err.bll.simulador.duracion", "La duración debe estar entre 1 y 365 días.");
            if (e.TasaRetencion < 0 || e.TasaRetencion > 100)
                throw new BE.AppException("err.bll.simulador.tasa", "La tasa de retención debe estar entre 0 y 100 %.");
            if (e.ClientesObjetivo < 0 || e.ClientesPlan < 0)
                throw new BE.AppException("err.bll.simulador.clientes", "La cantidad de clientes no puede ser negativa.");
            if (e.HorizonteMeses < 1)
                throw new BE.AppException("err.bll.simulador.horizonte", "El horizonte de retención debe ser de al menos 1 mes.");
        }

        /// <summary>Costo del descuento durante la promoción.</summary>
        public static decimal CostoDescuento(BE.EscenarioPromocion e) =>
            Math.Round(Math.Max(e.ClientesPlan, e.ClientesObjetivo) * e.PrecioPlan * e.DescuentoPorcentaje / 100m
                       * (e.DuracionDias / 30m), 2);

        /// <summary>Beneficio neto con una tasa de retención dada (para la curva de sensibilidad).</summary>
        public static decimal BeneficioNeto(BE.EscenarioPromocion e, decimal tasaRetencion) =>
            Math.Round(e.ClientesObjetivo * tasaRetencion / 100m * e.PrecioPlan * e.HorizonteMeses, 2) - CostoDescuento(e);

        public static BE.ResultadoSimulacion Simular(BE.EscenarioPromocion e, decimal roiMinimoPct, decimal margenSeguridadPp)
        {
            Validar(e);
            var r = new BE.ResultadoSimulacion { Escenario = e };
            r.Meses = Math.Round(e.DuracionDias / 30m, 2);
            r.CostoDescuento = CostoDescuento(e);
            r.ClientesRetenidos = Math.Round(e.ClientesObjetivo * e.TasaRetencion / 100m, 2);
            r.IngresoRetenido = Math.Round(r.ClientesRetenidos * e.PrecioPlan * e.HorizonteMeses, 2);
            r.BeneficioNeto = r.IngresoRetenido - r.CostoDescuento;
            r.Roi = r.CostoDescuento > 0 ? Math.Round(r.BeneficioNeto * 100m / r.CostoDescuento, 2) : (decimal?)null;

            decimal potencial = e.ClientesObjetivo * e.PrecioPlan * e.HorizonteMeses;
            r.TasaEquilibrio = potencial > 0 ? Math.Round(r.CostoDescuento * 100m / potencial, 2) : (decimal?)null;
            r.MargenSeguridad = r.TasaEquilibrio.HasValue ? e.TasaRetencion - r.TasaEquilibrio.Value : (decimal?)null;

            for (int tasa = 0; tasa <= 100; tasa += PasoCurva)
                r.Curva.Add(new BE.PuntoSensibilidad { TasaRetencion = tasa, BeneficioNeto = BeneficioNeto(e, tasa) });

            Dictaminar(r, roiMinimoPct, margenSeguridadPp);
            return r;
        }

        private static void Dictaminar(BE.ResultadoSimulacion r, decimal roiMinimoPct, decimal margenSeguridadPp)
        {
            var e = r.Escenario;
            if (e.ClientesObjetivo == 0 && r.CostoDescuento == 0)
            {
                r.Veredicto = BE.VeredictoSimulacion.SinDatos;
                r.Explicacion = new BE.TextoTraducible("sim.ver.sindatos",
                    "No hay clientes objetivo ni clientes en el plan: cargue al menos un cliente objetivo.");
                return;
            }
            if (e.ClientesObjetivo == 0)
            {
                r.Veredicto = BE.VeredictoSimulacion.NoConviene;
                r.Explicacion = new BE.TextoTraducible("sim.ver.sinobjetivo",
                    "Sin clientes objetivo la promoción solo cuesta ({0:C2}): no retiene a nadie.", r.CostoDescuento);
                return;
            }
            if (r.BeneficioNeto < 0)
            {
                r.Veredicto = BE.VeredictoSimulacion.NoConviene;
                r.Explicacion = r.TasaEquilibrio > 100
                    ? new BE.TextoTraducible("sim.ver.nunca",
                        "Ni reteniendo al 100 % de los clientes objetivo se recupera el costo del descuento ({0:C2}).", r.CostoDescuento)
                    : new BE.TextoTraducible("sim.ver.noconviene",
                        "Con {0:0.#} % de retención se pierden {1:C2}: la promoción conviene si retiene al menos {2:0.#} %.",
                        e.TasaRetencion, -r.BeneficioNeto, r.TasaEquilibrio);
                return;
            }
            bool roiBajo = r.Roi.HasValue && r.Roi.Value < roiMinimoPct;
            bool margenChico = r.MargenSeguridad.HasValue && r.MargenSeguridad.Value < margenSeguridadPp;
            string roiTexto = r.Roi.HasValue ? r.Roi.Value.ToString("0.#") + " %" : "—";
            if (roiBajo || margenChico)
            {
                // La explicación dice la causa real: ROI bajo el mínimo, margen de seguridad chico o ambas.
                r.Veredicto = BE.VeredictoSimulacion.Riesgoso;
                if (roiBajo && margenChico)
                    r.Explicacion = new BE.TextoTraducible("sim.ver.riesgoso.ambos",
                        "Gana {0:C2}, pero el ROI ({1}) está por debajo del mínimo de {2:0.#} % y el margen es chico: si la retención real queda por debajo de {3:0.#} %, deja de convenir.",
                        r.BeneficioNeto, roiTexto, roiMinimoPct, r.TasaEquilibrio ?? 0m);
                else if (roiBajo)
                    r.Explicacion = new BE.TextoTraducible("sim.ver.riesgoso.roi",
                        "Gana {0:C2}, pero el ROI ({1}) está por debajo del mínimo de {2:0.#} %: recupera poco por cada peso de descuento. Conviene mientras retenga al menos {3:0.#} %.",
                        r.BeneficioNeto, roiTexto, roiMinimoPct, r.TasaEquilibrio ?? 0m);
                else
                    r.Explicacion = new BE.TextoTraducible("sim.ver.riesgoso",
                        "Gana {0:C2} (ROI {1}), pero el margen es chico: si la retención real queda por debajo de {2:0.#} %, deja de convenir.",
                        r.BeneficioNeto, roiTexto, r.TasaEquilibrio ?? 0m);
                return;
            }
            r.Veredicto = BE.VeredictoSimulacion.Conviene;
            r.Explicacion = new BE.TextoTraducible("sim.ver.conviene",
                "Gana {0:C2} (ROI {1}). Conviene mientras retenga al menos {2:0.#} % (supuesto: {3:0.#} %).",
                r.BeneficioNeto, roiTexto, r.TasaEquilibrio ?? 0m, e.TasaRetencion);
        }
    }
}
