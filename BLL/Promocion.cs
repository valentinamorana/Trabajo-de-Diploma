using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio para PN03 — Evaluar y aplicar promociones comerciales.
    ///
    /// Cada método público corresponde a una actividad del diagrama de actividad aprobado de PN03
    /// (carriles Gerencia, Administración, Contabilidad y Vendedor) y cada decisión tiene su guarda:
    ///
    ///   Gerencia        Analizar métricas («Reporte de métricas») ........ AnalisisPromociones.AnalizarMetricas
    ///                   ¿Hay oportunidad? (No → fin "Sin promoción") ..... AnalisisPromociones.HayOportunidad
    ///                   Registrar sugerencia («Sugerencia de promoción») . SugerenciaPromocion.RegistrarSugerencia
    ///   Administración  ¿Acepta la sugerencia? (guarda: Pendiente) ....... BE.SugerenciaPromocion.PuedeEvaluarse
    ///                     No → Descartar sugerencia ...................... SugerenciaPromocion.DescartarSugerencia
    ///                     Sí → Crear promoción desde la sugerencia ....... CrearDesdeSugerencia
    ///                   Crear promoción manual ........................... CrearManual
    ///   Sistema         Validar → EnRevisionContable («Ficha») ........... ValidarPromocion
    ///   Contabilidad    Analizar margen e impacto (margen proyectado) .... AnalizarMargenEImpacto
    ///                   ¿Aprueba? (guarda: el creador no dictamina) ...... PuedeDictaminar
    ///                     Sí → Vigente («Dictamen contable») ............. AprobarContable
    ///                     No → RechazadaContabilidad («Dictamen») ........ RechazarContable
    ///   Administración  ¿Reformular? (guarda: RechazadaContabilidad) ..... BE.Promocion.PuedeReformularse
    ///                     Sí → Reformular (vuelve a Validar) ............. Reformular
    ///                     No → Dar de baja («Constancia de baja») ........ DarDeBaja
    ///   Vigencia        (a) Vendedor solicita la baja («Solicitud») ...... SolicitarBaja (no cambia el estado)
    ///                       Atender la solicitud («Respuesta») ........... AtenderSolicitud
    ///                         de acuerdo → Dar de baja ................... DarDeBaja
    ///                   (b) Administración da de baja directamente ....... DarDeBaja
    ///                   (c) Llega la FechaFin → Vencida .................. CerrarVencidas
    ///
    /// Cada transición es un claim atómico en el DAL (UPDATE ... WHERE Estado = esperado) que inserta,
    /// en la misma transacción, su fila de PromocionHistorial. Quien crea la promoción no puede
    /// dictaminarla (separación de funciones), salvo el Administrador, que puede hacer todo.
    /// </summary>
    public class Promocion : Interfaces.IPromocionService
    {
        private readonly DAL.Interfaces.IPromocionDAL           dalPromocion;
        private readonly DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia;
        private readonly DAL.Interfaces.IPlanSuscripcionDAL     dalPlan;
        private readonly Servicios.IRegistroBitacora        bitacora    = Servicios.FabricaBitacora.CrearSistema();
        private readonly Servicios.IRegistroBitacoraNegocio bitacoraNeg = Servicios.FabricaBitacora.CrearNegocio();

        // "Analizar margen e impacto": clientes activos del plan destino. ContarClientesActivosPorPlan
        // no forma parte de IClienteDAL (mismo criterio que BLL.PlanSuscripcion): por defecto se usa
        // el DAL concreto, creado recién al analizar; las pruebas inyectan el conteo.
        private readonly Func<int, int> contarClientesActivosPorPlan;

        private const string ModuloPromociones = "Promociones";

        public Promocion() : this(new DAL.Promocion(), new DAL.SugerenciaPromocion(), new DAL.PlanSuscripcion()) { }

        public Promocion(DAL.Interfaces.IPromocionDAL dalPromocion, DAL.Interfaces.ISugerenciaPromocionDAL dalSugerencia,
                          DAL.Interfaces.IPlanSuscripcionDAL dalPlan, Func<int, int> contarClientesActivosPorPlan = null)
        {
            this.dalPromocion  = dalPromocion  ?? throw new ArgumentNullException(nameof(dalPromocion));
            this.dalSugerencia = dalSugerencia ?? throw new ArgumentNullException(nameof(dalSugerencia));
            this.dalPlan       = dalPlan       ?? throw new ArgumentNullException(nameof(dalPlan));
            this.contarClientesActivosPorPlan = contarClientesActivosPorPlan
                ?? (idPlan => new DAL.Cliente().ContarClientesActivosPorPlan(idPlan));
        }

        // ══════════════════════════════════════════════════════════════════════
        // Consultas (antes de listar se cierran las vencidas: evento (c))
        // ══════════════════════════════════════════════════════════════════════

        public List<BE.Promocion> ObtenerTodas()
        {
            CerrarVencidasSinFallar();
            return dalPromocion.ObtenerTodas();
        }

        // Pantalla de Ventas: las Vigentes (con la marca de si ya tienen una solicitud de baja pendiente).
        public List<BE.Promocion> ObtenerParaVentas()
        {
            CerrarVencidasSinFallar();
            return dalPromocion.ObtenerTodas()
                .Where(p => p.Estado == BE.EstadoPromocion.Vigente)
                .OrderBy(p => p.FechaFin).ToList();
        }

        public List<BE.Promocion> ObtenerPendientesRevisionContable() => dalPromocion.ObtenerPendientesRevisionContable();
        public List<BE.PromocionHistorial> ObtenerHistorial(int idPromocion) => dalPromocion.ObtenerHistorial(idPromocion);

        // Bandeja de Administración: «Solicitudes de baja» que todavía no atendió.
        public List<BE.SolicitudBajaPromocion> ObtenerSolicitudesBajaPendientes() => dalPromocion.ObtenerSolicitudesBajaPendientes();

        public BE.DictamenContable ObtenerUltimoDictamen(int idPromocion) =>
            dalPromocion.ObtenerDictamenes(idPromocion).OrderBy(d => d.Fecha).ThenBy(d => d.IdDictamen).LastOrDefault();

        public BE.SolicitudBajaPromocion ObtenerUltimaSolicitudBaja(int idPromocion) =>
            dalPromocion.ObtenerSolicitudesBaja(idPromocion).OrderBy(s => s.FechaSolicitud).ThenBy(s => s.IdSolicitud).LastOrDefault();

        // Transición a DadaDeBaja (motivo y quién): base de la «Constancia de baja».
        public BE.PromocionHistorial ObtenerBaja(int idPromocion) =>
            dalPromocion.ObtenerHistorial(idPromocion).LastOrDefault(h => h.EstadoNuevo == BE.EstadoPromocion.DadaDeBaja);

        // ══════════════════════════════════════════════════════════════════════
        // Administración: Crear promoción (desde sugerencia / manual) → Validar
        // ══════════════════════════════════════════════════════════════════════

        // "¿Acepta la sugerencia? Sí → Crear promoción" desde la sugerencia de Gerencia.
        public int CrearDesdeSugerencia(string modulo, int idSugerencia, string nombre, string descripcion,
                                         BE.TipoDescuento tipo, decimal valor, DateTime fechaInicio, DateTime fechaFin,
                                         decimal margenEstimado, string impactoEconomico)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            var sugerencia = dalSugerencia.ObtenerPorId(idSugerencia)
                ?? throw new BE.AppException("err.bll.promocion.sugerencia_inexistente",
                    "La sugerencia seleccionada no existe.");

            // Guarda de "¿Acepta la sugerencia?": solo una sugerencia Pendiente se acepta o descarta.
            if (!sugerencia.PuedeEvaluarse() || !sugerencia.TransicionValida(BE.EstadoSugerencia.Evaluada))
                throw new BE.AppException("err.bll.promocion.sugerencia_evaluada",
                    "La sugerencia seleccionada ya fue evaluada. Actualizá la lista de sugerencias.");

            // Las sugerencias por categoría (versión anterior) se cerraron al discontinuarlas.
            if (!sugerencia.AplicaAPlan())
                throw new BE.AppException("err.bll.promocion.sugerencia_sin_plan",
                    "La sugerencia no indica un plan: las promociones se crean siempre para un plan.");

            var promocion = Armar(nombre, descripcion, tipo, valor, fechaInicio, fechaFin,
                sugerencia.IdPlan.Value, margenEstimado, impactoEconomico);
            promocion.IdSugerenciaOrigen = idSugerencia;
            ValidarPromocion(promocion);

            // Claim atómico de la sugerencia: dos administradores no crean dos promociones con ella.
            // Si el alta falla, se compensa devolviéndola a Pendiente para que pueda reintentarse.
            if (!dalSugerencia.MarcarEvaluada(idSugerencia, DateTime.Now))
                throw new BE.AppException("err.bll.promocion.sugerencia_evaluada",
                    "La sugerencia seleccionada ya fue evaluada. Actualizá la lista de sugerencias.");
            try
            {
                return Registrar(modulo, promocion, $"Alta desde la sugerencia #{idSugerencia}");
            }
            catch
            {
                try { dalSugerencia.ReabrirEvaluacion(idSugerencia); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[BLL.Promocion] No se pudo reabrir la sugerencia #{idSugerencia}: {ex.Message}"); }
                throw;
            }
        }

        // "Crear promoción" manual, sin sugerencia previa.
        public int CrearManual(string modulo, string nombre, string descripcion, BE.TipoDescuento tipo, decimal valor,
                                DateTime fechaInicio, DateTime fechaFin, int idPlan,
                                decimal margenEstimado, string impactoEconomico)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);
            var promocion = Armar(nombre, descripcion, tipo, valor, fechaInicio, fechaFin,
                idPlan, margenEstimado, impactoEconomico);
            ValidarPromocion(promocion);
            return Registrar(modulo, promocion, "Alta manual");
        }

        // "Validar": plan obligatorio y existente, valor y fechas. No muta nada.
        public void ValidarPromocion(BE.Promocion promocion)
        {
            if (promocion == null) throw new ArgumentNullException(nameof(promocion));

            if (string.IsNullOrWhiteSpace(promocion.Nombre))
                throw new BE.AppException("err.bll.promocion.nombre_requerido",
                    "El nombre de la promoción es obligatorio.");

            if (promocion.IdPlan <= 0)
                throw new BE.AppException("err.bll.promocion.plan_requerido",
                    "Elegí el plan al que aplica la promoción.");

            if (dalPlan.ObtenerPorId(promocion.IdPlan) == null)
                throw new BE.AppException("err.bll.promocion.plan_inexistente",
                    "El plan seleccionado no existe.");

            if (promocion.Valor <= 0)
                throw new BE.AppException("err.bll.promocion.valor_invalido",
                    "El beneficio de la promoción debe ser mayor a cero.");

            if (promocion.TipoDescuento == BE.TipoDescuento.Porcentaje && promocion.Valor > 100)
                throw new BE.AppException("err.bll.promocion.porcentaje_invalido",
                    "Un descuento por porcentaje no puede superar el 100%.");

            if (promocion.FechaFin.Date < promocion.FechaInicio.Date)
                throw new BE.AppException("err.bll.promocion.rango_fechas_invalido",
                    "La fecha de fin no puede ser anterior a la fecha de inicio.");

            // Una promoción que ya terminó nunca llegaría a aplicarse: se vencería al aprobarla.
            if (promocion.FechaFin.Date < DateTime.Today)
                throw new BE.AppException("err.bll.promocion.fecha_fin_pasada",
                    "La fecha de fin ({0:dd/MM/yyyy}) ya pasó. Elegí una fecha de hoy en adelante.",
                    promocion.FechaFin);
        }

        private static BE.Promocion Armar(string nombre, string descripcion, BE.TipoDescuento tipo, decimal valor,
                                          DateTime fechaInicio, DateTime fechaFin, int idPlan,
                                          decimal margenEstimado, string impactoEconomico)
        {
            return new BE.Promocion
            {
                Nombre = nombre?.Trim(),
                Descripcion = descripcion?.Trim(),
                TipoDescuento = tipo,
                Valor = valor,
                FechaInicio = fechaInicio.Date,
                FechaFin = fechaFin.Date,
                Estado = BE.EstadoPromocion.EnRevisionContable,
                IdPlan = idPlan,
                MargenEstimado = margenEstimado,
                ImpactoEconomico = impactoEconomico?.Trim()
            };
        }

        // Alta validada → EnRevisionContable («Ficha de promoción») + historial (sin estado anterior).
        private int Registrar(string modulo, BE.Promocion promocion, string observacion)
        {
            int idUsuario = BLLHelper.ResolverUsuarioActivo();
            promocion.IdUsuarioAlta = idUsuario;
            promocion.FechaAlta = DateTime.Now;
            int idNuevo = dalPromocion.Alta(promocion,
                Historial(0, null, BE.EstadoPromocion.EnRevisionContable, idUsuario, observacion));
            promocion.IdPromocion = idNuevo;

            bitacora.Registrar(modulo, $"Alta Promoción #{idNuevo}: {promocion.Nombre} — {observacion}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Promoción #{idNuevo} '{promocion.Nombre}' registrada, pendiente de revisión contable");
            return idNuevo;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Contabilidad: Analizar margen e impacto → ¿Aprueba?
        // ══════════════════════════════════════════════════════════════════════

        // "Analizar margen e impacto": beneficio estimado de la sugerencia (si la hay), las otras
        // promociones Vigentes del mismo plan que se superponen en fechas (advertencia) y el margen
        // proyectado = beneficio estimado − costo del descuento (ver BE.AnalisisImpactoPromocion).
        public BE.AnalisisImpactoPromocion AnalizarMargenEImpacto(int idPromocion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesContable, BE.Patentes.PromocionesContable);

            var promocion = dalPromocion.ObtenerPorId(idPromocion)
                ?? throw new BE.AppException("err.bll.promocion.inexistente", "La promoción ya no existe.");

            var analisis = new BE.AnalisisImpactoPromocion
            {
                Promocion = promocion,
                UsuarioPuedeDictaminar = PuedeDictaminar(promocion),
                Superpuestas = dalPromocion.ObtenerTodas().Where(promocion.SeSuperponeCon).ToList()
            };
            if (promocion.IdSugerenciaOrigen.HasValue)
            {
                var sugerencia = dalSugerencia.ObtenerPorId(promocion.IdSugerenciaOrigen.Value);
                analisis.BeneficioEstimadoSugerencia = sugerencia?.BeneficioEstimado;
                analisis.OrigenSugerencia = sugerencia?.OrigenMetrica;
                analisis.TasaRetencionSugerencia = sugerencia?.TasaRetencion;
            }
            ProyectarCostoDelDescuento(analisis);
            return analisis;
        }

        // Costo mensual proyectado del descuento: lo que la promoción descuenta sobre el precio
        // mensual del plan (la misma regla que el cobro, PoliticaDescuento.DescuentoDe) por la
        // cantidad de clientes activos del plan.
        private void ProyectarCostoDelDescuento(BE.AnalisisImpactoPromocion analisis)
        {
            var promocion = analisis.Promocion;
            var plan = dalPlan.ObtenerPorId(promocion.IdPlan);
            if (plan == null) return;   // el plan ya no existe: sin datos para calcular el margen

            analisis.PrecioPlan = plan.Precio;
            analisis.DescuentoPorCliente = Politicas.PoliticaDescuento.DescuentoDe(promocion, plan.Precio);
            analisis.ClientesActivosPlan = Math.Max(0, contarClientesActivosPorPlan(plan.IdPlan));
        }

        // Guarda de "¿Aprueba?": quien creó la promoción no puede dictaminarla (separación de
        // funciones). Excepción: el Administrador sí puede (decisión de la alumna, 05/10).
        public bool PuedeDictaminar(BE.Promocion promocion)
        {
            if (promocion == null || !Seguridad.SessionManager.IsLoggedIn) return false;
            var usuario = Seguridad.SessionManager.GetInstance().Usuario;
            // El Administrador puede dictaminar aunque la haya creado él (decisión de la alumna).
            return usuario.EsAdministrador || promocion.PuedeDictaminarla(usuario.Id);
        }

        // "¿Aprueba? Sí" → Vigente + «Dictamen contable».
        public int AprobarContable(string modulo, BE.Promocion promocion, string observacion)
        {
            int id = Dictaminar(promocion, observacion, aprobada: true);
            bitacora.Registrar(modulo, $"Aprobar Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' aprobada por Contabilidad y ya está Vigente");
            return id;
        }

        // "¿Aprueba? No" → RechazadaContabilidad + «Dictamen contable» (vuelve a Administración).
        public int RechazarContable(string modulo, BE.Promocion promocion, string observacion)
        {
            int id = Dictaminar(promocion, observacion, aprobada: false);
            bitacora.Registrar(modulo, $"Rechazar Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' rechazada por Contabilidad: {observacion}");
            return id;
        }

        private int Dictaminar(BE.Promocion promocion, string observacion, bool aprobada)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesContableEditar, BE.Patentes.PromocionesContable);
            var destino = aprobada ? BE.EstadoPromocion.Vigente : BE.EstadoPromocion.RechazadaContabilidad;

            if (!promocion.PuedeAprobarseORechazarseContable() || !promocion.TransicionValida(destino))
                throw new BE.AppException("err.bll.promocion.revisioncontable_estado",
                    "Solo se pueden aprobar o rechazar promociones En Revisión Contable. Esta promoción está '{0}'.",
                    promocion.Estado);
            // Mientras esperaba la revisión contable llegó su fecha de fin: aprobarla la dejaría
            // Vigente un instante y se vencería sola. Se rechaza para que Administración la reformule.
            if (aprobada && promocion.FechaFin.Date < DateTime.Today)
                throw new BE.AppException("err.bll.promocion.aprobar_vencida",
                    "La promoción terminó el {0:dd/MM/yyyy}: no se puede aprobar. Rechazala para que Administración la reformule con fechas nuevas.",
                    promocion.FechaFin);
            if (!PuedeDictaminar(promocion))
                throw new BE.AppException("err.bll.promocion.creador_no_dictamina",
                    "Quien creó la promoción no puede dictaminarla: la tiene que analizar otro usuario de Contabilidad.");
            ExigirTexto(observacion, "err.bll.promocion.observacion_requerida",
                "Debe ingresar una observación para esta decisión.");

            int idUsuario = BLLHelper.ResolverUsuarioActivo();
            var dictamen = new BE.DictamenContable
            {
                IdPromocion = promocion.IdPromocion,
                IdUsuario = idUsuario,
                Aprobada = aprobada,
                Observacion = observacion.Trim(),
                Fecha = DateTime.Now
            };
            int idDictamen = dalPromocion.Dictaminar(dictamen,
                Historial(promocion.IdPromocion, BE.EstadoPromocion.EnRevisionContable, destino, idUsuario, dictamen.Observacion));
            if (idDictamen <= 0) throw EstadoConcurrente();
            promocion.Estado = destino;
            return idDictamen;
        }

        // ══════════════════════════════════════════════════════════════════════
        // Administración: ¿Reformular?
        // ══════════════════════════════════════════════════════════════════════

        // "¿Reformular? Sí": se corrigen las condiciones y vuelve a Validar → EnRevisionContable.
        public void Reformular(string modulo, BE.Promocion promocion)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);

            if (!promocion.PuedeReformularse() || !promocion.TransicionValida(BE.EstadoPromocion.EnRevisionContable))
                throw new BE.AppException("err.bll.promocion.reformular_estado",
                    "Solo se puede reformular una promoción Rechazada por Contabilidad. Esta promoción está '{0}'.",
                    promocion.Estado);

            promocion.Nombre = promocion.Nombre?.Trim();
            promocion.FechaInicio = promocion.FechaInicio.Date;
            promocion.FechaFin = promocion.FechaFin.Date;
            ValidarPromocion(promocion);

            int idUsuario = BLLHelper.ResolverUsuarioActivo();
            if (!dalPromocion.Reformular(promocion, Historial(promocion.IdPromocion, BE.EstadoPromocion.RechazadaContabilidad,
                    BE.EstadoPromocion.EnRevisionContable, idUsuario, "Reformulada tras el rechazo contable")))
                throw EstadoConcurrente();
            promocion.Estado = BE.EstadoPromocion.EnRevisionContable;

            bitacora.Registrar(modulo, $"Reformular Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' reformulada por Administración: vuelve a revisión contable");
        }

        // "Dar de baja" (motivo obligatorio) → DadaDeBaja (fin) + «Constancia de baja». Vale para
        // una Vigente (Administración la da de baja directamente o porque atendió una solicitud de
        // Ventas) y para una Rechazada por Contabilidad que no se reformula. En la misma transacción
        // se cierran las solicitudes de baja pendientes de la promoción.
        public void DarDeBaja(string modulo, BE.Promocion promocion, string motivo)
        {
            ExigirTexto(motivo, "err.bll.promocion.motivobaja_admin_requerido",
                "Debe indicar el motivo por el que se da de baja la promoción.");
            DarDeBaja(modulo, promocion, motivo.Trim(), motivo.Trim());
        }

        private void DarDeBaja(string modulo, BE.Promocion promocion, string motivo, string respuestaSolicitudes)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);
            if (promocion == null) throw new ArgumentNullException(nameof(promocion));

            if (!promocion.PuedeDarseDeBaja() || !promocion.TransicionValida(BE.EstadoPromocion.DadaDeBaja))
                throw new BE.AppException("err.bll.promocion.darbaja_estado",
                    "Solo se pueden dar de baja promociones Vigentes o Rechazadas por Contabilidad. Esta promoción está '{0}'.",
                    promocion.Estado);

            var anterior = promocion.Estado;
            int idUsuario = BLLHelper.ResolverUsuarioActivo();
            if (!dalPromocion.DarDeBaja(promocion.IdPromocion, anterior,
                    Historial(promocion.IdPromocion, anterior, BE.EstadoPromocion.DadaDeBaja, idUsuario, motivo),
                    respuestaSolicitudes))
                throw EstadoConcurrente();
            promocion.Estado = BE.EstadoPromocion.DadaDeBaja;
            promocion.MotivoBaja = null;   // las solicitudes pendientes quedaron atendidas

            bitacora.Registrar(modulo, $"Dar de baja Promoción #{promocion.IdPromocion}: {promocion.Nombre} — Motivo: {motivo}",
                BE.Criticidad.Media);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                $"Promoción #{promocion.IdPromocion} '{promocion.Nombre}' dada de baja por Administración: {motivo}");
        }

        // ══════════════════════════════════════════════════════════════════════
        // Vigencia: región interrumpible
        // ══════════════════════════════════════════════════════════════════════

        // (a) "Vendedor solicita la baja" (motivo obligatorio) → «Solicitud de baja» Pendiente.
        // No cambia el estado: la promoción sigue Vigente (y aplicándose) hasta que Administración
        // atiende la solicitud. Una sola solicitud pendiente por promoción.
        public int SolicitarBaja(string modulo, BE.Promocion promocion, string motivo)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesVigentesEditar, BE.Patentes.PromocionesVigentes);

            if (promocion.Estado != BE.EstadoPromocion.Vigente)
                throw new BE.AppException("err.bll.promocion.sugerirbaja_estado",
                    "Solo se puede sugerir la baja de promociones Vigentes. Esta promoción está '{0}'.", promocion.Estado);
            if (promocion.TieneSolicitudBajaPendiente())
                throw new BE.AppException("err.bll.promocion.solicitud_ya_pendiente",
                    "La promoción ya tiene una solicitud de baja pendiente: Administración todavía no la atendió.");
            ExigirTexto(motivo, "err.bll.promocion.motivobaja_requerido", "Debe indicar el motivo de la baja sugerida.");

            var solicitud = new BE.SolicitudBajaPromocion
            {
                IdPromocion = promocion.IdPromocion,
                IdUsuarioSolicita = BLLHelper.ResolverUsuarioActivo(),
                Motivo = motivo.Trim(),
                FechaSolicitud = DateTime.Now,
                Estado = BE.EstadoSolicitudBaja.Pendiente
            };
            // 0: la promoción dejó de estar Vigente o ya tiene otra solicitud pendiente (otra sesión).
            int idSolicitud = dalPromocion.SolicitarBaja(solicitud);
            if (idSolicitud <= 0)
                throw new BE.AppException("err.bll.promocion.solicitud_concurrente",
                    "La promoción cambió desde otra sesión (ya no está Vigente o ya tiene una solicitud de baja pendiente). Actualizá la lista.");
            promocion.MotivoBaja = solicitud.Motivo;

            bitacora.Registrar(modulo, $"Solicitar baja Promoción #{promocion.IdPromocion}: {promocion.Nombre}", BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Vendedor solicita dar de baja la Promoción #{promocion.IdPromocion} '{promocion.Nombre}': {solicitud.Motivo}");
            return idSolicitud;
        }

        // "Atender la solicitud" (respuesta obligatoria) → «Respuesta a la solicitud». Si
        // Administración está de acuerdo, da de baja la promoción (DarDeBaja cierra la solicitud);
        // si no, la solicitud queda Atendida y la promoción sigue Vigente.
        public void AtenderSolicitud(string modulo, BE.SolicitudBajaPromocion solicitud, string respuesta, bool darDeBaja)
        {
            PermisosAccion.Exigir(BE.Patentes.PromocionesAdminEditar, BE.Patentes.PromocionesAdmin);
            if (solicitud == null) throw new ArgumentNullException(nameof(solicitud));

            if (!solicitud.EstaPendiente())
                throw new BE.AppException("err.bll.promocion.solicitud_atendida",
                    "La solicitud de baja ya fue atendida. Actualizá la lista de solicitudes.");
            ExigirTexto(respuesta, "err.bll.promocion.respuesta_requerida",
                "Debe escribir la respuesta a la solicitud de baja.");
            string texto = respuesta.Trim();

            if (darDeBaja)
            {
                var promocion = dalPromocion.ObtenerPorId(solicitud.IdPromocion)
                    ?? throw new BE.AppException("err.bll.promocion.inexistente", "La promoción ya no existe.");
                DarDeBaja(modulo, promocion, $"Solicitud de baja #{solicitud.IdSolicitud} atendida: {texto}", texto);
                solicitud.Estado = BE.EstadoSolicitudBaja.Atendida;
                solicitud.DioDeBaja = true;
                solicitud.Respuesta = texto;
                return;
            }

            var atendida = new BE.SolicitudBajaPromocion
            {
                IdSolicitud = solicitud.IdSolicitud,
                IdPromocion = solicitud.IdPromocion,
                Estado = BE.EstadoSolicitudBaja.Atendida,
                DioDeBaja = false,
                Respuesta = texto,
                IdUsuarioResuelve = BLLHelper.ResolverUsuarioActivo(),
                FechaResolucion = DateTime.Now
            };
            // Claim atómico (UPDATE ... WHERE Estado = Pendiente): otra sesión pudo atenderla antes.
            if (!dalPromocion.AtenderSolicitud(atendida))
                throw new BE.AppException("err.bll.promocion.solicitud_atendida",
                    "La solicitud de baja ya fue atendida. Actualizá la lista de solicitudes.");
            solicitud.Estado = atendida.Estado;
            solicitud.DioDeBaja = false;
            solicitud.Respuesta = texto;
            solicitud.IdUsuarioResuelve = atendida.IdUsuarioResuelve;
            solicitud.FechaResolucion = atendida.FechaResolucion;

            bitacora.Registrar(modulo, $"Atender solicitud de baja #{solicitud.IdSolicitud} (Promoción #{solicitud.IdPromocion}): sigue vigente — {texto}",
                BE.Criticidad.Baja);
            bitacoraNeg.Registrar(BE.TipoEventoNegocio.Venta,
                $"Administración atendió la solicitud de baja #{solicitud.IdSolicitud}: la Promoción #{solicitud.IdPromocion} sigue Vigente. {texto}");
        }

        // (c) "Llega la FechaFin" → Vencida. Se ejecuta al consultar las promociones; cada una se
        // cierra con su propio claim, así dos sesiones que consultan a la vez no duplican el historial.
        public int CerrarVencidas()
        {
            // El vencimiento lo hace el sistema (llegó la fecha), no quien abrió la pantalla: el
            // historial queda sin usuario.
            int? idUsuario = null;
            DateTime hoy = DateTime.Today;
            int cerradas = 0;
            foreach (var p in dalPromocion.ObtenerTodas().Where(x => x.DebeVencer(hoy)))
            {
                if (!dalPromocion.CambiarEstado(p.IdPromocion, BE.EstadoPromocion.Vigente,
                        Historial(p.IdPromocion, BE.EstadoPromocion.Vigente, BE.EstadoPromocion.Vencida, idUsuario,
                                  $"Llegó la fecha de fin ({p.FechaFin:dd/MM/yyyy})")))
                    continue;   // otra sesión la cerró (o cambió de estado) primero
                cerradas++;
                bitacoraNeg.Registrar(BE.TipoEventoNegocio.Cancelacion,
                    $"Promoción #{p.IdPromocion} '{p.Nombre}' vencida: terminó el {p.FechaFin:dd/MM/yyyy}");
            }
            if (cerradas > 0)
                bitacora.Registrar(ModuloPromociones, $"{cerradas} promoción(es) pasaron a Vencida por fecha de fin", BE.Criticidad.Baja);
            return cerradas;
        }

        // Las consultas no deben fallar porque no se pudo cerrar una vencida (se reintenta en la próxima).
        private void CerrarVencidasSinFallar()
        {
            try { CerrarVencidas(); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[BLL.Promocion] CerrarVencidas: {ex.Message}"); }
        }

        // ── Auxiliares ─────────────────────────────────────────────────────────

        private static BE.PromocionHistorial Historial(int idPromocion, BE.EstadoPromocion? anterior, BE.EstadoPromocion nuevo,
                                                       int? idUsuario, string observacion) =>
            new BE.PromocionHistorial
            {
                IdPromocion = idPromocion,
                EstadoAnterior = anterior,
                EstadoNuevo = nuevo,
                IdUsuario = idUsuario,
                Fecha = DateTime.Now,
                Observacion = observacion
            };

        private static BE.AppException EstadoConcurrente()
            => new BE.AppException("err.bll.promocion.estado_concurrente",
                "La promoción cambió de estado desde otra sesión. Actualizá la lista e intentá de nuevo.");

        private static void ExigirTexto(string texto, string clave, string mensaje)
        {
            if (string.IsNullOrWhiteSpace(texto)) throw new BE.AppException(clave, mensaje);
        }
    }
}
