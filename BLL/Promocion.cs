using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para PN03 — Métricas, promociones y toma de decisiones.
    /// Administración crea/gestiona, Contabilidad aprueba o rechaza, Vendedor puede sugerir la
    /// baja de una promoción vigente y Administración resuelve esa solicitud.
    /// </summary>
    public class Promocion : Interfaces.IPromocionService
    {
        private readonly DAL.Interfaces.IPromocionDAL           dalPromocion;
        private readonly DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL     dalPlan;
        private readonly Servicios.Bitacora        bitacora    = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();

        public Promocion() : this(new DAL.Promocion(), new DAL.SugerenciaPromocion(), new DAL.PlanSuscripcion()) { }

        public Promocion(DAL.Interfaces.IPromocionDAL dalPromocion, DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia,
                          DAL.Interfaces.IPlanSuscripcionDAL dalPlan)
        {
            this.dalPromocion  = dalPromocion  ?? throw new ArgumentNullException(nameof(dalPromocion));
            this.dalSugerencia = dalSugerencia ?? throw new ArgumentNullException(nameof(dalSugerencia));
            this.dalPlan       = dalPlan       ?? throw new ArgumentNullException(nameof(dalPlan));
        }

        public List<BE.Promocion> ObtenerTodas() => dalPromocion.ObtenerTodas();
        public List<BE.Promocion> ObtenerVigentes() => dalPromocion.ObtenerVigentes();
        public List<BE.Promocion> ObtenerPendientesRevisionContable() => dalPromocion.ObtenerPendientesRevisionContable();
        public BE.Promocion ObtenerPorId(int idPromocion) => dalPromocion.ObtenerPorId(idPromocion);

        // CU-ADM-Gestionar Promociones (a partir de una sugerencia de Gerencia).
        public int CrearDesdeSugerencia(string modulo, int idSugerencia, string nombre, string descripcion,
                                         BE.TipoDescuento tipo, decimal valor, DateTime fechaInicio, DateTime fechaFin,
                                         decimal margenEstimado, string impactoEconomico)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            var sugerencia = dalSugerencia.ObtenerPorId(idSugerencia);
            if (sugerencia == null)
                throw new BE.AppException("err.bll.promocion.sugerencia_inexistente",
                    "La sugerencia seleccionada no existe.");

            // CU01-ADM flujo 2.1: la sugerencia ya fue evaluada (otra sesión, o esta misma antes): no
            // se puede reutilizar para crear otra promoción.
            if (sugerencia.Estado != BE.EstadoSugerencia.Pendiente)
                throw new BE.AppException("err.bll.promocion.sugerencia_evaluada",
                    "La sugerencia seleccionada ya fue evaluada. Actualizá la lista de sugerencias.");

            // Reclamo atómico (UPDATE ... WHERE Estado = Pendiente): dos administradores que abren la misma
            // sugerencia no pueden crear dos promociones. Si la creación falla (validación, BD), se
            // compensa devolviendo la sugerencia a Pendiente para que pueda reintentarse.
            if (!dalSugerencia.MarcarEvaluada(idSugerencia))
                throw new BE.AppException("err.bll.promocion.sugerencia_evaluada",
                    "La sugerencia seleccionada ya fue evaluada. Actualizá la lista de sugerencias.");

            try
            {
                return CrearInterna(modulo, nombre, descripcion, tipo, valor, fechaInicio, fechaFin,
                    sugerencia.IdPlan, sugerencia.CategoriaPrenda, margenEstimado, impactoEconomico, idSugerencia);
            }
            catch
            {
                try { dalSugerencia.ReabrirEvaluacion(idSugerencia); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[BLL.Promocion] No se pudo reabrir la sugerencia #{idSugerencia}: {ex.Message}"); }
                throw;
            }
        }

        // CU-ADM-Gestionar Promociones (manual, sin sugerencia previa).
        public int CrearManual(string modulo, string nombre, string descripcion, BE.TipoDescuento tipo, decimal valor,
                                DateTime fechaInicio, DateTime fechaFin, int? idPlan, string categoriaPrenda,
                                decimal margenEstimado, string impactoEconomico)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            return CrearInterna(modulo, nombre, descripcion, tipo, valor, fechaInicio, fechaFin,
                idPlan, categoriaPrenda, margenEstimado, impactoEconomico, idSugerenciaOrigen: null);
        }

        // Validación común a CrearInterna y Modificar (antes duplicada entre las dos, con riesgo de
        // que una cambiara un umbral/regla y la otra quedara desincronizada). No muta nada — solo
        // valida y lanza; el caller decide qué hacer con los valores ya validados.
        private void ValidarCamposComunes(string nombre, int? idPlan, string categoriaPrenda,
                                           decimal valor, BE.TipoDescuento tipo, DateTime fechaInicio, DateTime fechaFin)
        {
            bool aplicaPlan = idPlan.HasValue;
            bool aplicaCategoria = !string.IsNullOrWhiteSpace(categoriaPrenda);

            if (string.IsNullOrWhiteSpace(nombre))
                throw new BE.AppException("err.bll.promocion.nombre_requerido",
                    "El nombre de la promoción es obligatorio.");

            if (aplicaPlan == aplicaCategoria)
                throw new BE.AppException("err.bll.promocion.destino_invalido",
                    "La promoción debe aplicar a un plan o a una categoría de prenda, nunca a ambos ni a ninguno.");

            if (aplicaPlan && dalPlan.ObtenerPorId(idPlan.Value) == null)
                throw new BE.AppException("err.bll.promocion.plan_inexistente",
                    "El plan seleccionado no existe.");

            if (valor <= 0)
                throw new BE.AppException("err.bll.promocion.valor_invalido",
                    "El beneficio de la promoción debe ser mayor a cero.");

            if (tipo == BE.TipoDescuento.Porcentaje && valor > 100)
                throw new BE.AppException("err.bll.promocion.porcentaje_invalido",
                    "Un descuento por porcentaje no puede superar el 100%.");

            if (fechaFin.Date < fechaInicio.Date)
                throw new BE.AppException("err.bll.promocion.rango_fechas_invalido",
                    "La fecha de fin no puede ser anterior a la fecha de inicio.");
        }

        private int CrearInterna(string modulo, string nombre, string descripcion, BE.TipoDescuento tipo, decimal valor,
                                  DateTime fechaInicio, DateTime fechaFin, int? idPlan, string categoriaPrenda,
                                  decimal margenEstimado, string impactoEconomico, int? idSugerenciaOrigen)
        {
            bool aplicaCategoria = !string.IsNullOrWhiteSpace(categoriaPrenda);
            ValidarCamposComunes(nombre, idPlan, categoriaPrenda, valor, tipo, fechaInicio, fechaFin);

            var promocion = new BE.Promocion
            {
                Nombre = nombre.Trim(),
                Descripcion = descripcion?.Trim(),
                TipoDescuento = tipo,
                Valor = valor,
                FechaInicio = fechaInicio.Date,
                FechaFin = fechaFin.Date,
                Estado = BE.EstadoPromocion.EnRevisionContable,
                IdPlan = idPlan,
                CategoriaPrenda = aplicaCategoria ? categoriaPrenda.Trim() : null,
                MargenEstimado = margenEstimado,
                ImpactoEconomico = impactoEconomico?.Trim(),
                IdSugerenciaOrigen = idSugerenciaOrigen,
                FechaAlta = DateTime.Now
            };

            int idNuevo = dalPromocion.Alta(promocion);

            bitacora.Registrar(modulo, $"Alta Promoción #{idNuevo}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Promoción #{idNuevo} '{promocion.Nombre}' registrada, pendiente de revisión contable");

            return idNuevo;
        }

        public void Modificar(string modulo, BE.Promocion promocion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            // Guarda de estado que faltaba (a diferencia de Desactivar/AprobarContable/etc., todos
            // con su Puede...() antes de mutar): sin esto se podía modificar precio/fechas/tipo de
            // descuento de una promoción ya Vigente (aprobada por Contabilidad) o Desactivada sin
            // pasar de nuevo por revisión contable. Mismo estado que exige AprobarContable/
            // RechazarContable — modificar solo tiene sentido mientras todavía está en revisión.
            if (!promocion.PuedeAprobarseORechazarseContable())
                throw new BE.AppException("err.bll.promocion.modificar_estado",
                    "Solo se puede modificar una promoción mientras está en revisión contable. Estado actual: '{0}'.",
                    promocion.Estado);

            bool aplicaCategoria = !string.IsNullOrWhiteSpace(promocion.CategoriaPrenda);
            ValidarCamposComunes(promocion.Nombre, promocion.IdPlan, promocion.CategoriaPrenda,
                promocion.Valor, promocion.TipoDescuento, promocion.FechaInicio, promocion.FechaFin);

            promocion.CategoriaPrenda = aplicaCategoria ? promocion.CategoriaPrenda.Trim() : null;
            promocion.Nombre = promocion.Nombre.Trim();

            dalPromocion.Modificar(promocion);

            bitacora.Registrar(modulo, $"Modificar Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
        }

        // CU01-ADM (PN03): una promoción Rechazada por Contabilidad vuelve a Administración para
        // REFORMULARSE: se corrigen sus condiciones y vuelve a la cola de revisión contable. Conserva la
        // observación de Contabilidad (queda como referencia de qué había que corregir).
        public void Reformular(string modulo, BE.Promocion promocion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            if (!promocion.PuedeReformularse())
                throw new BE.AppException("err.bll.promocion.reformular_estado",
                    "Solo se puede reformular una promoción Rechazada por Contabilidad. Esta promoción está '{0}'.",
                    promocion.Estado);

            bool aplicaCategoria = !string.IsNullOrWhiteSpace(promocion.CategoriaPrenda);
            ValidarCamposComunes(promocion.Nombre, promocion.IdPlan, promocion.CategoriaPrenda,
                promocion.Valor, promocion.TipoDescuento, promocion.FechaInicio, promocion.FechaFin);

            promocion.CategoriaPrenda = aplicaCategoria ? promocion.CategoriaPrenda.Trim() : null;
            promocion.Nombre = promocion.Nombre.Trim();

            // Primero se reclama el cambio de estado (UPDATE condicionado a Rechazada) y recién después
            // se guardan las condiciones nuevas, así dos sesiones no pisan la reformulación de la otra.
            CambiarEstadoOFalla(promocion, BE.EstadoPromocion.EnRevisionContable, null);
            dalPromocion.Modificar(promocion);

            bitacora.Registrar(modulo, $"Reformular Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' reformulada por Administración: vuelve a revisión contable");
        }

        // Cambia el estado con UPDATE condicionado al estado que tenía la promoción al leerla.
        private void CambiarEstadoOFalla(BE.Promocion promocion, BE.EstadoPromocion nuevo, string observacionOMotivo)
        {
            if (!dalPromocion.CambiarEstado(promocion.IdPromocion, promocion.Estado, nuevo, observacionOMotivo))
                throw EstadoConcurrente();
        }

        private static BE.AppException EstadoConcurrente()
            => new BE.AppException("err.bll.promocion.estado_concurrente",
                "La promoción cambió de estado desde otra sesión. Actualizá la lista e intentá de nuevo.");

        // A8 del documento fuente: Administración desactiva una promoción Vigente directamente.
        public void Desactivar(string modulo, BE.Promocion promocion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            if (!promocion.PuedeDesactivarseDirecto())
                throw new BE.AppException("err.bll.promocion.desactivar_estado",
                    "Solo se pueden desactivar promociones Vigentes. Esta promoción está '{0}'.", promocion.Estado);

            CambiarEstadoOFalla(promocion, BE.EstadoPromocion.Desactivada, null);

            bitacora.Registrar(modulo, $"Desactivar Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' desactivada por Administración");
        }

        // CU-CONT-03-Analizar Promoción.
        public void AprobarContable(string modulo, BE.Promocion promocion, string observacion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesContableEditar, BE.Patentes.PromocionesContable);
            ExigirEnRevisionContable(promocion);
            ExigirObservacion(observacion);

            CambiarEstadoOFalla(promocion, BE.EstadoPromocion.Vigente, observacion.Trim());

            bitacora.Registrar(modulo, $"Aprobar Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' aprobada por Contabilidad y ya está Vigente");
        }

        public void RechazarContable(string modulo, BE.Promocion promocion, string observacion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesContableEditar, BE.Patentes.PromocionesContable);
            ExigirEnRevisionContable(promocion);
            ExigirObservacion(observacion);

            CambiarEstadoOFalla(promocion, BE.EstadoPromocion.RechazadaContabilidad, observacion.Trim());

            bitacora.Registrar(modulo, $"Rechazar Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' rechazada por Contabilidad: {observacion}");
        }

        // CU-VEND-04-Sugerir Baja de Promoción.
        public void SugerirBaja(string modulo, BE.Promocion promocion, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesVigentesEditar, BE.Patentes.PromocionesVigentes);

            if (!promocion.PuedeSugerirseBaja())
                throw new BE.AppException("err.bll.promocion.sugerirbaja_estado",
                    "Solo se puede sugerir la baja de promociones Vigentes. Esta promoción está '{0}'.", promocion.Estado);

            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.promocion.motivobaja_requerido",
                    "Debe indicar el motivo de la baja sugerida.");

            if (!dalPromocion.SolicitarBaja(promocion.IdPromocion, motivo.Trim()))
                throw EstadoConcurrente();

            bitacora.Registrar(modulo, $"Sugerir baja Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Vendedor sugiere dar de baja la Promoción #{promocion.IdPromocion} '{promocion.Nombre}': {motivo}");
        }

        public void AprobarBaja(string modulo, BE.Promocion promocion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            if (!promocion.PuedeResolverseBaja())
                throw new BE.AppException("err.bll.promocion.resolverbaja_estado",
                    "Solo se puede resolver la baja de promociones con baja Solicitada. Esta promoción está '{0}'.", promocion.Estado);

            CambiarEstadoOFalla(promocion, BE.EstadoPromocion.Desactivada, null);

            bitacora.Registrar(modulo, $"Aprobar baja Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' dada de baja (Administración aprobó la solicitud de Ventas)");
        }

        public void RechazarBaja(string modulo, BE.Promocion promocion, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            if (!promocion.PuedeResolverseBaja())
                throw new BE.AppException("err.bll.promocion.resolverbaja_estado",
                    "Solo se puede resolver la baja de promociones con baja Solicitada. Esta promoción está '{0}'.", promocion.Estado);

            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.promocion.motivorechazobaja_requerido",
                    "Debe indicar el motivo por el cual la promoción sigue vigente.");

            // No se pasa "motivo" a CambiarEstado: Observacion ya guarda la evaluación de
            // Contabilidad (AprobarContable/RechazarContable) y no hay un campo propio para el
            // motivo de rechazo de una baja — pisarla acá perdería esa evaluación. El motivo
            // queda igual registrado en bitácora y bitácora de negocio, abajo.
            CambiarEstadoOFalla(promocion, BE.EstadoPromocion.Vigente, null);

            bitacora.Registrar(modulo, $"Rechazar baja Promoción #{promocion.IdPromocion}: {promocion.Nombre} — Motivo: {motivo}", BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Administración rechaza la baja de la Promoción #{promocion.IdPromocion} '{promocion.Nombre}', sigue Vigente: {motivo}");
        }

        private static void ExigirEnRevisionContable(BE.Promocion promocion)
        {
            if (!promocion.PuedeAprobarseORechazarseContable())
                throw new BE.AppException("err.bll.promocion.revisioncontable_estado",
                    "Solo se pueden aprobar o rechazar promociones En Revisión Contable. Esta promoción está '{0}'.",
                    promocion.Estado);
        }

        private static void ExigirObservacion(string observacion)
        {
            if (string.IsNullOrWhiteSpace(observacion))
                throw new BE.AppException("err.bll.promocion.observacion_requerida",
                    "Debe ingresar una observación para esta decisión.");
        }
    }
}
