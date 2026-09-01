using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IPromocionDAL (sin base de datos). Configurable sobre los
    /// valores de retorno que BLL.Promocion necesita para ejercitar sus distintas ramas; espía
    /// sobre las escrituras.</summary>
    public class FakePromocionDAL : IPromocionDAL
    {
        // ── Configuración ─────────────────────────────────────────────────────
        public List<BE.Promocion> Todas { get; set; } = new List<BE.Promocion>();
        public int AltaIdGenerado { get; set; }

        // ── Espías ────────────────────────────────────────────────────────────
        public int AltaVeces { get; private set; }
        public BE.Promocion UltimoAlta { get; private set; }
        public int ModificarVeces { get; private set; }
        public BE.Promocion UltimoModificar { get; private set; }
        public int CambiarEstadoVeces { get; private set; }
        public int UltimoIdPromocionCambiarEstado { get; private set; }
        public BE.EstadoPromocion UltimoNuevoEstado { get; private set; }
        public string UltimaObservacionOMotivo { get; private set; }
        public int SolicitarBajaVeces { get; private set; }
        public int UltimoIdSolicitarBaja { get; private set; }
        public string UltimoMotivoSolicitarBaja { get; private set; }

        public List<BE.Promocion> ObtenerTodas() => Todas;
        public List<BE.Promocion> ObtenerVigentes() => Todas.FindAll(p => p.Estado == BE.EstadoPromocion.Vigente);
        public List<BE.Promocion> ObtenerPendientesRevisionContable() => Todas.FindAll(p => p.Estado == BE.EstadoPromocion.EnRevisionContable);
        public BE.Promocion ObtenerPorId(int idPromocion) => Todas.Find(p => p.IdPromocion == idPromocion);

        public int Alta(BE.Promocion promocion)
        {
            AltaVeces++;
            UltimoAlta = promocion;
            return AltaIdGenerado;
        }

        public void Modificar(BE.Promocion promocion)
        {
            ModificarVeces++;
            UltimoModificar = promocion;
        }

        public void CambiarEstado(int idPromocion, BE.EstadoPromocion nuevoEstado, string observacionOMotivo)
        {
            CambiarEstadoVeces++;
            UltimoIdPromocionCambiarEstado = idPromocion;
            UltimoNuevoEstado = nuevoEstado;
            UltimaObservacionOMotivo = observacionOMotivo;
        }

        public void SolicitarBaja(int idPromocion, string motivo)
        {
            SolicitarBajaVeces++;
            UltimoIdSolicitarBaja = idPromocion;
            UltimoMotivoSolicitarBaja = motivo;
        }
    }
}
