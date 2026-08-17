using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Análisis de Tiempos de Mantenimiento (PdN11). Agrupa el
    /// historial de MantenimientoPrenda (T04-independiente, tabla propia desde la
    /// migración de PdN4) por prenda y marca las que exceden el umbral de cantidad o
    /// duración promedio de mantenimientos — señal de una prenda problemática (mala
    /// calidad, uso intensivo) a evaluar para baja o revisión de proveedor.
    /// </summary>
    public class AnalisisMantenimiento : Interfaces.IAnalisisMantenimientoService
    {
        private readonly DAL.MantenimientoPrenda dalMantenimiento;

        public const int CantidadMantenimientosExcesiva = 3;
        public const double DuracionPromedioExcesivaDias = 5.0;

        public AnalisisMantenimiento() : this(new DAL.MantenimientoPrenda()) { }

        public AnalisisMantenimiento(DAL.MantenimientoPrenda dalMantenimiento)
        {
            this.dalMantenimiento = dalMantenimiento ?? throw new ArgumentNullException(nameof(dalMantenimiento));
        }

        public List<BE.TiempoMantenimientoPrenda> Detectar()
        {
            var resultado = new List<BE.TiempoMantenimientoPrenda>();

            var porPrenda = dalMantenimiento.ObtenerTodos().GroupBy(m => new { m.IdPrenda, m.NombrePrenda });

            foreach (var grupo in porPrenda)
            {
                var cerrados = grupo.Where(m => m.FechaSalida.HasValue).ToList();
                double? promedio = cerrados.Count > 0 ? cerrados.Average(m => m.DuracionDias.Value) : (double?)null;
                int? maximo = cerrados.Count > 0 ? cerrados.Max(m => m.DuracionDias.Value) : (int?)null;
                int cantidad = grupo.Count();

                bool cantidadExcesiva = cantidad >= CantidadMantenimientosExcesiva;
                bool duracionExcesiva = promedio.HasValue && promedio.Value >= DuracionPromedioExcesivaDias;
                if (!cantidadExcesiva && !duracionExcesiva) continue;

                string motivo = cantidadExcesiva && duracionExcesiva
                    ? $"{grupo.Key.NombrePrenda} lleva {cantidad} mantenimientos, con un promedio de {promedio:F1} día(s) cada uno."
                    : cantidadExcesiva
                        ? $"{grupo.Key.NombrePrenda} lleva {cantidad} mantenimientos — cantidad excesiva."
                        : $"{grupo.Key.NombrePrenda} tiene un promedio de {promedio:F1} día(s) por mantenimiento — duración excesiva.";
                string clave = cantidadExcesiva && duracionExcesiva ? "mant.analisis.motivo.ambos"
                             : cantidadExcesiva ? "mant.analisis.motivo.cantidad"
                             : "mant.analisis.motivo.duracion";
                object[] args = cantidadExcesiva && duracionExcesiva
                    ? new object[] { grupo.Key.NombrePrenda, cantidad, promedio }
                    : cantidadExcesiva ? new object[] { grupo.Key.NombrePrenda, cantidad }
                                        : new object[] { grupo.Key.NombrePrenda, promedio };

                resultado.Add(new BE.TiempoMantenimientoPrenda
                {
                    IdPrenda = grupo.Key.IdPrenda,
                    NombrePrenda = grupo.Key.NombrePrenda,
                    CantidadMantenimientos = cantidad,
                    DuracionPromedioDias = promedio,
                    DuracionMaximaDias = maximo,
                    Motivo = motivo,
                    Clave = clave,
                    Args = args
                });
            }

            return resultado.OrderByDescending(r => r.CantidadMantenimientos).ToList();
        }
    }
}
