using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para PN03 — «Sugerencia de promoción» (ver el mapeo completo del
    /// diagrama en el encabezado de BLL.Promocion):
    ///
    ///   Gerencia        Registrar sugerencia (guarda OrigenMetrica y quién la creó) .. RegistrarSugerencia
    ///   Administración  ¿Acepta la sugerencia? (guarda BE.SugerenciaPromocion.PuedeEvaluarse)
    ///                     No → Descartar sugerencia (motivo obligatorio) ............ DescartarSugerencia
    ///                     Sí → BLL.Promocion.CrearDesdeSugerencia
    ///
    /// Actor de Gerencia: rol GerenteComercial. Actor de Administración: AdministracionComercial.
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
        public List<BE.SugerenciaPromocion> ObtenerTodas() => dalSugerencia.ObtenerTodas();
        public BE.SugerenciaPromocion ObtenerPorId(int idSugerencia) => dalSugerencia.ObtenerPorId(idSugerencia);

        // "¿Hay oportunidad? Sí → Registrar sugerencia" («Sugerencia de promoción»).
        public int RegistrarSugerencia(string modulo, BE.OrigenMetrica origen, int? idPlan, string categoriaPrenda,
                                       string motivo, BE.TipoDescuento tipoSugerido, decimal beneficioEstimado)
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
                OrigenMetrica = origen,
                IdUsuarioAlta = BLLHelper.ResolverUsuarioActivo(),
                FechaAlta = DateTime.Now
            };

            int idNuevo = dalSugerencia.Alta(sugerencia);

            string destino = aplicaPlan ? $"plan ID {idPlan}" : $"categoría '{sugerencia.CategoriaPrenda}'";
            bitacora.Registrar(modulo,
                $"Sugerencia de promoción #{idNuevo} ({origen}) — {destino} — Motivo: {sugerencia.Motivo}",
                BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Sugerencia de promoción #{idNuevo} para {destino} — beneficio estimado ${beneficioEstimado}");

            return idNuevo;
        }

        // "¿Acepta la sugerencia? No → Descartar sugerencia" (motivo obligatorio): Descartada, fin.
        public void DescartarSugerencia(string modulo, int idSugerencia, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            var sugerencia = dalSugerencia.ObtenerPorId(idSugerencia)
                ?? throw new BE.AppException("err.bll.promocion.sugerencia_inexistente",
                    "La sugerencia seleccionada no existe.");

            // Guarda de "¿Acepta la sugerencia?": solo se decide sobre una sugerencia Pendiente.
            if (!sugerencia.PuedeEvaluarse() || !sugerencia.TransicionValida(BE.EstadoSugerencia.Descartada))
                throw new BE.AppException("err.bll.promocion.sugerencia_evaluada",
                    "La sugerencia seleccionada ya fue evaluada. Actualizá la lista de sugerencias.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.sugerenciapromocion.motivodescarte_requerido",
                    "Debe indicar el motivo por el que se descarta la sugerencia.");

            // Claim atómico (UPDATE ... WHERE Estado = Pendiente).
            if (!dalSugerencia.Descartar(idSugerencia, motivo.Trim(), DateTime.Now))
                throw new BE.AppException("err.bll.promocion.sugerencia_evaluada",
                    "La sugerencia seleccionada ya fue evaluada. Actualizá la lista de sugerencias.");

            bitacora.Registrar(modulo, $"Descartar sugerencia de promoción #{idSugerencia} — Motivo: {motivo.Trim()}",
                BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                $"Administración descartó la sugerencia de promoción #{idSugerencia}: {motivo.Trim()}");
        }
    }
}
