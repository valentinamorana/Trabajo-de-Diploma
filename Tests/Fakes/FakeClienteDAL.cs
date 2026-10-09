using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IClienteDAL (sin base de datos). Espía sobre Modificar,
    /// configurable sobre ObtenerTodos/ObtenerPorId/ExisteDNI/ExisteDNIParaOtro/Alta para
    /// poder ejercitar las distintas ramas de BLL.Cliente sin tocar BD real.</summary>
    public class FakeClienteDAL : IClienteDAL
    {
        // ── Configuración (el test la fija antes de ejercitar el BLL) ──────────
        public List<BE.Cliente> ClientesDevueltos { get; set; } = new List<BE.Cliente>();
        public BE.Cliente ClientePorId { get; set; }
        public bool ExisteDNIRespuesta { get; set; }
        public bool ExisteDNIParaOtroRespuesta { get; set; }
        public int AltaIdGenerado { get; set; }

        // ── Espías (el test los lee después) ────────────────────────────────────
        public int ModificarVeces { get; private set; }
        public BE.Cliente UltimoModificado { get; private set; }
        public int RecalcularDVVeces { get; private set; }
        public int AltaVeces { get; private set; }
        public BE.Cliente UltimoAlta { get; private set; }
        public int BajaVeces { get; private set; }
        public int UltimoIdBaja { get; private set; }

        public List<BE.Cliente> ObtenerTodos() => ClientesDevueltos;
        // Clientes adicionales por ID (p. ej. el referente en PN02); si el ID no está, devuelve ClientePorId.
        public Dictionary<int, BE.Cliente> OtrosClientesPorId { get; } = new Dictionary<int, BE.Cliente>();
        public BE.Cliente ObtenerPorId(int idCliente)
            => OtrosClientesPorId.TryGetValue(idCliente, out var otro) ? otro : ClientePorId;

        public int Alta(BE.Cliente cliente)
        {
            AltaVeces++;
            UltimoAlta = cliente;
            return AltaIdGenerado;
        }

        public void Modificar(BE.Cliente cliente)
        {
            ModificarVeces++;
            UltimoModificado = cliente;
        }

        public void Baja(int idCliente)
        {
            BajaVeces++;
            UltimoIdBaja = idCliente;
        }

        public bool ExisteDNI(string dni) => ExisteDNIRespuesta;
        public bool ExisteDNIParaOtro(string dni, int idExcluir) => ExisteDNIParaOtroRespuesta;
        public bool TieneContratacionPendienteRespuesta { get; set; }
        // IDs puntuales con contratación pendiente (además de la respuesta global).
        public HashSet<int> IdsConContratacionPendiente { get; } = new HashSet<int>();
        public bool TieneContratacionPendiente(int idCliente)
            => TieneContratacionPendienteRespuesta || IdsConContratacionPendiente.Contains(idCliente);
        public int ConsultasIdsConContratacionPendiente { get; private set; }
        public HashSet<int> ObtenerIdsConContratacionPendiente()
        {
            ConsultasIdsConContratacionPendiente++;
            var ids = new HashSet<int>(IdsConContratacionPendiente);
            if (TieneContratacionPendienteRespuesta)
                foreach (var c in ClientesDevueltos) ids.Add(c.IdCliente);
            return ids;
        }

        // Catálogo de medios de pago como lo deja el script en una base nueva (sección 20c/20c2).
        public List<BE.MedioPago> MediosPago { get; } = new List<BE.MedioPago>
        {
            new BE.MedioPago { IdMedioPago = 1, Nombre = "Efectivo",           ClaveTraduccion = "medio.efectivo" },
            new BE.MedioPago { IdMedioPago = 2, Nombre = "Tarjeta de débito",  ClaveTraduccion = "medio.tarjeta_debito" },
            new BE.MedioPago { IdMedioPago = 3, Nombre = "Transferencia",      ClaveTraduccion = "medio.transferencia" },
            new BE.MedioPago { IdMedioPago = 4, Nombre = "Tarjeta de crédito", ClaveTraduccion = "medio.tarjeta_credito", PermiteCuotas = true },
        };
        public List<BE.MedioPago> ObtenerMediosPago() => MediosPago;

        // Sin BD real: no hay transacción que abrir, se ejecuta la acción directamente
        // (conexión/transacción null — los EnTx de estos fakes no las usan).
        public void EjecutarTransaccion(Action<SqlConnection, SqlTransaction> accion) => accion(null, null);

        public void ModificarEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cliente cliente) => Modificar(cliente);

        // Cobro recurrente con control optimista: el doble compara contra el vencimiento "en la
        // base" (VencimientoEnBase si se configura; si no, acepta) y registra la llamada.
        public int RenovarVencimientoVeces { get; private set; }
        public DateTime? VencimientoEnBase { get; set; }
        public bool VencimientoEnBaseConfigurado { get; set; }
        public bool RenovarVencimientoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente,
                                           DateTime? vencimientoLeido, DateTime nuevoVencimiento)
        {
            if (VencimientoEnBaseConfigurado && VencimientoEnBase != vencimientoLeido) return false;
            RenovarVencimientoVeces++;
            VencimientoEnBase = nuevoVencimiento;
            return true;
        }

        // Espías del crédito de referido (en la BD real son UPDATEs atómicos).
        public List<KeyValuePair<int, decimal>> CreditosSumados { get; } = new List<KeyValuePair<int, decimal>>();
        public List<KeyValuePair<int, decimal>> CreditosConsumidos { get; } = new List<KeyValuePair<int, decimal>>();

        public void SumarCreditoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente, decimal monto)
            => CreditosSumados.Add(new KeyValuePair<int, decimal>(idCliente, monto));

        public void ConsumirCreditoEnTx(SqlConnection conexion, SqlTransaction tx, int idCliente, decimal monto)
            => CreditosConsumidos.Add(new KeyValuePair<int, decimal>(idCliente, monto));

        public List<int> IdsDVRecalculados { get; } = new List<int>();
        public void RecalcularDV(int idCliente) { RecalcularDVVeces++; IdsDVRecalculados.Add(idCliente); }
    }
}
