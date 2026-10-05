using System;
using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IContratacionDAL (sin base de datos). Configurable sobre
    /// los valores de retorno que BLL.Contratacion necesita para ejercitar sus distintas
    /// ramas de PN02; espía sobre las escrituras.</summary>
    public class FakeContratacionDAL : IContratacionDAL
    {
        // ── Configuración ─────────────────────────────────────────────────────
        public List<BE.Contratacion> PendientesDePago { get; set; } = new List<BE.Contratacion>();
        public List<BE.Contratacion> Resueltas { get; set; } = new List<BE.Contratacion>();
        public BE.Contratacion ContratacionPorId { get; set; }
        public int AltaIdGenerado { get; set; }
        // false simula que otra sesión de Caja ya reclamó el cobro (el UPDATE condicional no afectó filas).
        public bool ConfirmarCobroResultado { get; set; } = true;
        public int AltaDesistimientoIdGenerado { get; set; } = 1;
        public BE.DesistimientoContratacion DesistimientoPorId { get; set; }

        // Catálogo MedioPago (mismo contenido que el seed de la BD).
        public List<BE.MedioPago> MediosPago { get; set; } = new List<BE.MedioPago>
        {
            new BE.MedioPago { IdMedioPago = 1, Nombre = "Efectivo",      ClaveTraduccion = "medio.efectivo" },
            new BE.MedioPago { IdMedioPago = 2, Nombre = "Tarjeta",       ClaveTraduccion = "medio.tarjeta" },
            new BE.MedioPago { IdMedioPago = 3, Nombre = "Transferencia", ClaveTraduccion = "medio.transferencia" }
        };

        // "Registrar intento": si se asigna, RegistrarIntentoFallido devuelve este resultado tal
        // cual. Si queda null (default) se simula el DAL real: cuenta los intentos por
        // contratación y, al llegar a 'maximo', la cancela (ContratacionPorId pasa a Cancelada).
        public BE.ResultadoIntentoPago ResultadoIntento { get; set; }
        // true simula que la contratación ya no estaba pendiente (otra sesión la resolvió): devuelve null.
        public bool RegistrarIntentoDevuelveNull { get; set; }

        // ── Espías ────────────────────────────────────────────────────────────
        public int AltaVeces { get; private set; }
        public BE.Contratacion UltimoAlta { get; private set; }

        public int ConfirmarCobroVeces { get; private set; }
        public int UltimoIdContratacionConfirmado { get; private set; }
        public int UltimoIdCaja { get; private set; }
        public int? UltimoIdMedioPago { get; private set; }
        public string UltimoNumeroComprobante { get; private set; }
        public decimal UltimoImporte { get; private set; }
        public decimal UltimoDescuento { get; private set; }
        public int? UltimaPromocion { get; private set; }

        public int RegistrarVigenciaVeces { get; private set; }
        public DateTime? UltimaVigenciaDesde { get; private set; }
        public DateTime? UltimaVigenciaHasta { get; private set; }

        public int ReabrirPagoVeces { get; private set; }

        public int RegistrarIntentoVeces { get; private set; }
        public string UltimoMotivoIntento { get; private set; }
        public int UltimoMaximo { get; private set; }
        public List<BE.IntentoPago> Intentos { get; } = new List<BE.IntentoPago>();

        public int AltaDesistimientoVeces { get; private set; }
        public BE.DesistimientoContratacion UltimoDesistimiento { get; private set; }

        private readonly Dictionary<int, int> intentosPorContratacion = new Dictionary<int, int>();
        // Intentos ya registrados antes de la prueba (clave = IdContratacion).
        public Dictionary<int, int> IntentosPrevios => intentosPorContratacion;

        // ── IContratacionDAL ──────────────────────────────────────────────────
        public List<BE.Contratacion> ObtenerPendientesDePago() => PendientesDePago;

        public List<BE.Contratacion> ObtenerResueltas() => Resueltas;

        public BE.Contratacion ObtenerPorId(int idContratacion) => ContratacionPorId;

        public int Alta(BE.Contratacion contratacion)
        {
            AltaVeces++;
            UltimoAlta = contratacion;
            return AltaIdGenerado;
        }

        public bool ConfirmarCobro(int idContratacion, int idCaja, int idMedioPago, string numeroComprobante,
                                   decimal importe, decimal descuento, int? idPromocion)
        {
            ConfirmarCobroVeces++;
            UltimoIdContratacionConfirmado = idContratacion;
            UltimoIdCaja = idCaja;
            UltimoIdMedioPago = idMedioPago;
            UltimoNumeroComprobante = numeroComprobante;
            UltimoImporte = importe;
            UltimoDescuento = descuento;
            UltimaPromocion = idPromocion;
            return ConfirmarCobroResultado;
        }

        public int? UltimoReferenteAcreditado { get; private set; }
        public void RegistrarVigencia(int idContratacion, DateTime desde, DateTime hasta, int? idReferenteAcreditado = null)
        {
            UltimoReferenteAcreditado = idReferenteAcreditado;
            RegistrarVigenciaVeces++;
            UltimaVigenciaDesde = desde;
            UltimaVigenciaHasta = hasta;
        }

        // Si se asigna, ReabrirPago lanza (simula que la compensación también falla).
        public Exception ReabrirPagoLanza { get; set; }
        public void ReabrirPago(int idContratacion)
        {
            ReabrirPagoVeces++;
            if (ReabrirPagoLanza != null) throw ReabrirPagoLanza;
        }

        public BE.ResultadoIntentoPago RegistrarIntentoFallido(int idContratacion, int? idMedioPago, string motivo,
                                                               int idCaja, int maximo)
        {
            RegistrarIntentoVeces++;
            UltimoIdMedioPago = idMedioPago;
            UltimoMotivoIntento = motivo;
            UltimoIdCaja = idCaja;
            UltimoMaximo = maximo;

            if (RegistrarIntentoDevuelveNull) return null;
            if (ResultadoIntento != null) return ResultadoIntento;

            // Simulación del DAL real: exige que siga PendientePago.
            if (ContratacionPorId != null && ContratacionPorId.IdContratacion == idContratacion
                && ContratacionPorId.Estado != BE.EstadoContratacion.PendientePago)
                return null;

            intentosPorContratacion.TryGetValue(idContratacion, out int previos);
            int nro = previos + 1;
            bool enTope = nro > maximo;   // igual que el DAL real: ya estaba en el tope, se cancela sin insertar
            if (enTope) nro = maximo;
            intentosPorContratacion[idContratacion] = nro;
            if (!enTope) Intentos.Add(new BE.IntentoPago
            {
                IdIntento = Intentos.Count + 1, IdContratacion = idContratacion, NroIntento = nro,
                Fecha = DateTime.Now, IdMedioPago = idMedioPago, Motivo = motivo, IdCaja = idCaja
            });

            bool cancelada = nro >= maximo;
            if (cancelada && ContratacionPorId != null && ContratacionPorId.IdContratacion == idContratacion)
                ContratacionPorId.Estado = BE.EstadoContratacion.Cancelada;

            return new BE.ResultadoIntentoPago { NroIntento = nro, Maximo = maximo, Cancelada = cancelada };
        }

        public List<BE.IntentoPago> ObtenerIntentos(int idContratacion)
            => Intentos.FindAll(i => i.IdContratacion == idContratacion);

        public List<BE.MedioPago> ObtenerMediosPago() => MediosPago;

        public int AltaDesistimiento(BE.DesistimientoContratacion desistimiento)
        {
            AltaDesistimientoVeces++;
            UltimoDesistimiento = desistimiento;
            return AltaDesistimientoIdGenerado;
        }

        public BE.DesistimientoContratacion ObtenerDesistimiento(int idDesistimiento) => DesistimientoPorId;
    }
}
