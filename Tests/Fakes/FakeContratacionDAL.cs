using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IContratacionDAL (sin base de datos). Configurable sobre
    /// los valores de retorno que BLL.Contratacion necesita para ejercitar sus distintas
    /// ramas; espía sobre las escrituras.</summary>
    public class FakeContratacionDAL : IContratacionDAL
    {
        // ── Configuración ─────────────────────────────────────────────────────
        public List<BE.Contratacion> PendientesDePago { get; set; } = new List<BE.Contratacion>();
        public BE.Contratacion ContratacionPorId { get; set; }
        public int AltaIdGenerado { get; set; }
        public int IntentosDespuesDeIncrementar { get; set; } = 1;

        // ── Espías ────────────────────────────────────────────────────────────
        public int AltaVeces { get; private set; }
        public BE.Contratacion UltimoAlta { get; private set; }
        public int IncrementarIntentoVeces { get; private set; }
        public int ConfirmarPagoVeces { get; private set; }
        public int UltimoIdContratacionConfirmado { get; private set; }
        public int UltimoIdCaja { get; private set; }
        public string UltimoMedioPago { get; private set; }
        public string UltimoNumeroComprobante { get; private set; }
        public int CancelarVeces { get; private set; }
        public int UltimoCancelarId { get; private set; }

        public List<BE.Contratacion> ObtenerPendientesDePago() => PendientesDePago;

        public BE.Contratacion ObtenerPorId(int idContratacion) => ContratacionPorId;

        public int Alta(BE.Contratacion contratacion)
        {
            AltaVeces++;
            UltimoAlta = contratacion;
            return AltaIdGenerado;
        }

        public int IncrementarIntento(int idContratacion)
        {
            IncrementarIntentoVeces++;
            return IntentosDespuesDeIncrementar;
        }

        public void ConfirmarPago(int idContratacion, int idCaja, string medioPago, string numeroComprobante)
        {
            ConfirmarPagoVeces++;
            UltimoIdContratacionConfirmado = idContratacion;
            UltimoIdCaja = idCaja;
            UltimoMedioPago = medioPago;
            UltimoNumeroComprobante = numeroComprobante;
        }

        public void Cancelar(int idContratacion)
        {
            CancelarVeces++;
            UltimoCancelarId = idContratacion;
        }
    }
}
