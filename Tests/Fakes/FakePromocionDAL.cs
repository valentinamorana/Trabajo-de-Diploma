using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IPromocionDAL (sin base de datos). Imita el claim atómico del DAL real:
    /// si la promoción está en <see cref="Todas"/> y no tiene el estado esperado (o
    /// <see cref="ClaimResultado"/> es false, "otra sesión ganó"), no escribe nada. Cada transición
    /// exitosa guarda su fila en <see cref="Historial"/> y el objeto del flujo que genera.
    /// </summary>
    public class FakePromocionDAL : IPromocionDAL
    {
        // ── Configuración ─────────────────────────────────────────────────────
        public List<BE.Promocion> Todas { get; set; } = new List<BE.Promocion>();
        public int AltaIdGenerado { get; set; }

        /// <summary>false simula que otra sesión ya cambió el estado (el UPDATE condicionado no afectó filas).</summary>
        public bool ClaimResultado { get; set; } = true;

        // ── Espías / "tablas" ─────────────────────────────────────────────────
        public List<BE.PromocionHistorial> Historial { get; } = new List<BE.PromocionHistorial>();
        public List<BE.DictamenContable> Dictamenes { get; } = new List<BE.DictamenContable>();
        public List<BE.SolicitudBajaPromocion> Solicitudes { get; } = new List<BE.SolicitudBajaPromocion>();

        public int AltaVeces { get; private set; }
        public BE.Promocion UltimoAlta { get; private set; }
        public int ReformularVeces { get; private set; }
        public BE.Promocion UltimoReformular { get; private set; }
        public int CambiarEstadoVeces { get; private set; }
        public BE.EstadoPromocion UltimoEstadoEsperado { get; private set; }
        public BE.SolicitudBajaPromocion UltimaResolucion { get; private set; }

        /// <summary>Cantidad de escrituras que cambiaron el estado (cualquier transición).</summary>
        public int Transiciones => Historial.Count;

        public List<BE.Promocion> ObtenerTodas() => Todas;
        public List<BE.Promocion> ObtenerVigentes() => Todas.FindAll(p => p.EstaVigente());
        public List<BE.MetricaImpactoPromocion> Impacto { get; set; } = new List<BE.MetricaImpactoPromocion>();
        public System.DateTime? UltimoDesdeImpacto { get; private set; }
        public List<BE.MetricaImpactoPromocion> ObtenerImpacto(System.DateTime desde, System.DateTime hasta)
        {
            UltimoDesdeImpacto = desde;
            return Impacto;
        }
        // Decisiones de suscripción por plan (filtradas por fecha como la consulta real).
        public List<(int IdPlan, BE.DecisionRenovacion Decision)> Decisiones { get; set; } = new List<(int, BE.DecisionRenovacion)>();
        public List<BE.DecisionRenovacion> ObtenerDecisionesDelPlan(int idPlan, System.DateTime desde, System.DateTime hastaExcl) =>
            Decisiones.FindAll(d => d.IdPlan == idPlan && d.Decision.Fecha >= desde && d.Decision.Fecha < hastaExcl)
                      .ConvertAll(d => d.Decision);
        public List<BE.Promocion> ObtenerPendientesRevisionContable() => Todas.FindAll(p => p.Estado == BE.EstadoPromocion.EnRevisionContable);
        public BE.Promocion ObtenerPorId(int idPromocion) => Todas.Find(p => p.IdPromocion == idPromocion);

        /// <summary>Si no es null, el alta falla con esta excepción (simula un error de BD).</summary>
        public System.Exception AltaExcepcion { get; set; }

        public int Alta(BE.Promocion promocion, BE.PromocionHistorial historial)
        {
            if (AltaExcepcion != null) throw AltaExcepcion;
            AltaVeces++;
            UltimoAlta = promocion;
            historial.IdPromocion = AltaIdGenerado;
            Historial.Add(historial);
            return AltaIdGenerado;
        }

        public bool Reformular(BE.Promocion promocion, BE.PromocionHistorial historial)
        {
            if (!Reclamar(promocion.IdPromocion, historial.EstadoAnterior.GetValueOrDefault(), historial)) return false;
            ReformularVeces++;
            UltimoReformular = promocion;
            return true;
        }

        public bool CambiarEstado(int idPromocion, BE.EstadoPromocion estadoEsperado, BE.PromocionHistorial historial)
        {
            UltimoEstadoEsperado = estadoEsperado;
            if (!Reclamar(idPromocion, estadoEsperado, historial)) return false;
            CambiarEstadoVeces++;
            return true;
        }

        public int Dictaminar(BE.DictamenContable dictamen, BE.PromocionHistorial historial)
        {
            if (!Reclamar(dictamen.IdPromocion, BE.EstadoPromocion.EnRevisionContable, historial)) return 0;
            dictamen.IdDictamen = Dictamenes.Count + 1;
            Dictamenes.Add(dictamen);
            return dictamen.IdDictamen;
        }

        public int DarDeBajaVeces { get; private set; }

        public bool DarDeBaja(int idPromocion, BE.EstadoPromocion estadoEsperado, BE.PromocionHistorial historial, string respuestaSolicitudes)
        {
            UltimoEstadoEsperado = estadoEsperado;
            if (!Reclamar(idPromocion, estadoEsperado, historial)) return false;
            DarDeBajaVeces++;
            // Como el DAL real: las solicitudes pendientes quedan atendidas con la baja.
            foreach (var s in Solicitudes.FindAll(x => x.IdPromocion == idPromocion && x.EstaPendiente()))
            {
                s.Estado = BE.EstadoSolicitudBaja.Atendida;
                s.DioDeBaja = true;
                s.Respuesta = respuestaSolicitudes;
                s.IdUsuarioResuelve = historial.IdUsuario;
                s.FechaResolucion = historial.Fecha;
            }
            return true;
        }

        // INSERT ... WHERE la promoción sigue Vigente y no tiene otra solicitud pendiente. No cambia el estado.
        public int SolicitarBaja(BE.SolicitudBajaPromocion solicitud)
        {
            if (!ClaimResultado) return 0;
            var guardada = Todas.Find(p => p.IdPromocion == solicitud.IdPromocion);
            if (guardada != null && guardada.Estado != BE.EstadoPromocion.Vigente) return 0;
            if (Solicitudes.Exists(s => s.IdPromocion == solicitud.IdPromocion && s.EstaPendiente())) return 0;
            solicitud.IdSolicitud = Solicitudes.Count + 1;
            Solicitudes.Add(solicitud);
            return solicitud.IdSolicitud;
        }

        public bool AtenderSolicitud(BE.SolicitudBajaPromocion atendida)
        {
            if (!ClaimResultado) return false;
            var guardada = Solicitudes.Find(s => s.IdSolicitud == atendida.IdSolicitud);
            if (guardada != null && !guardada.EstaPendiente()) return false;
            if (guardada != null)
            {
                guardada.Estado = BE.EstadoSolicitudBaja.Atendida;
                guardada.Respuesta = atendida.Respuesta;
                guardada.DioDeBaja = atendida.DioDeBaja;
                guardada.IdUsuarioResuelve = atendida.IdUsuarioResuelve;
                guardada.FechaResolucion = atendida.FechaResolucion;
            }
            UltimaResolucion = atendida;
            return true;
        }

        public List<BE.SolicitudBajaPromocion> ObtenerSolicitudesBajaPendientes() => Solicitudes.FindAll(s => s.EstaPendiente());

        public List<BE.PromocionHistorial> ObtenerHistorial(int idPromocion) => Historial.FindAll(h => h.IdPromocion == idPromocion);
        public List<BE.DictamenContable> ObtenerDictamenes(int idPromocion) => Dictamenes.FindAll(d => d.IdPromocion == idPromocion);
        public List<BE.SolicitudBajaPromocion> ObtenerSolicitudesBaja(int idPromocion) => Solicitudes.FindAll(s => s.IdPromocion == idPromocion);

        // UPDATE ... WHERE Estado = @esperado: solo cambia si la promoción (si está cargada) sigue en ese estado.
        private bool Reclamar(int idPromocion, BE.EstadoPromocion esperado, BE.PromocionHistorial historial)
        {
            if (!ClaimResultado) return false;
            var guardada = Todas.Find(p => p.IdPromocion == idPromocion);
            if (guardada != null)
            {
                if (guardada.Estado != esperado) return false;
                guardada.Estado = historial.EstadoNuevo;
            }
            Historial.Add(historial);
            return true;
        }
    }
}
