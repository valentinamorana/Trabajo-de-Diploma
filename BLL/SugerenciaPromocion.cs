using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para PN03, CU-GE-01-Sugerir Promoción a la Administración.
    /// Actor: GerenteComercial (Gerencia reusa este rol ya existente — ver roadmap).
    /// </summary>
    public class SugerenciaPromocion : Interfaces.ISugerenciaPromocionService
    {
        private readonly DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL     dalPlan;
        private readonly Servicios.Bitacora        bitacora    = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();

        public SugerenciaPromocion() : this(new DAL.SugerenciaPromocion(), new DAL.PlanSuscripcion()) { }

        public SugerenciaPromocion(DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia,
                                    DAL.Interfaces.IPlanSuscripcionDAL dalPlan)
        {
            this.dalSugerencia = dalSugerencia ?? throw new ArgumentNullException(nameof(dalSugerencia));
            this.dalPlan       = dalPlan       ?? throw new ArgumentNullException(nameof(dalPlan));
        }

        public List<BE.SugerenciaPromocion> ObtenerPendientes() => dalSugerencia.ObtenerPendientes();
        public BE.SugerenciaPromocion ObtenerPorId(int idSugerencia) => dalSugerencia.ObtenerPorId(idSugerencia);

        public int Crear(string modulo, int? idPlan, string categoriaPrenda, string motivo,
                          BE.TipoDescuento tipoSugerido, decimal beneficioEstimado)
        {
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

            bool aplicaPlan = idPlan.HasValue;
            bool aplicaCategoria = !string.IsNullOrWhiteSpace(categoriaPrenda);

            if (aplicaPlan == aplicaCategoria)
                throw new BE.AppException("err.bll.sugerenciapromocion.destino_invalido",
                    "La sugerencia debe aplicar a un plan o a una categoría de prenda, nunca a ambos ni a ninguno.");

            if (aplicaPlan && dalPlan.ObtenerPorId(idPlan.Value) == null)
                throw new BE.AppException("err.bll.sugerenciapromocion.plan_inexistente",
                    "El plan seleccionado no existe.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.sugerenciapromocion.motivo_requerido",
                    "Debe indicar el motivo de la sugerencia.");

            if (beneficioEstimado <= 0)
                throw new BE.AppException("err.bll.sugerenciapromocion.beneficio_invalido",
                    "El beneficio estimado debe ser mayor a cero.");

            var sugerencia = new BE.SugerenciaPromocion
            {
                IdPlan = idPlan,
                CategoriaPrenda = aplicaCategoria ? categoriaPrenda.Trim() : null,
                Motivo = motivo.Trim(),
                TipoDescuentoSugerido = tipoSugerido,
                BeneficioEstimado = beneficioEstimado,
                Estado = BE.EstadoSugerencia.Pendiente,
                FechaAlta = DateTime.Now
            };

            int idNuevo = dalSugerencia.Alta(sugerencia);

            string destino = aplicaPlan ? $"plan ID {idPlan}" : $"categoría '{categoriaPrenda}'";
            bitacora.Registrar(modulo,
                $"Sugerencia de promoción #{idNuevo} — {destino} — Motivo: {motivo}",
                BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Sugerencia de promoción #{idNuevo} para {destino} — beneficio estimado ${beneficioEstimado}");

            return idNuevo;
        }
    }
}
