using System.Collections.Generic;
using System.Data.SqlClient;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de ICobroDAL (sin base de datos). En memoria, con espías.</summary>
    public class FakeCobroDAL : ICobroDAL
    {
        public readonly List<BE.Cobro> Registros = new List<BE.Cobro>();

        public int AltaVeces { get; private set; }

        public int Alta(BE.Cobro cobro)
        {
            AltaVeces++;
            cobro.IdCobro = Registros.Count + 1;
            Registros.Add(cobro);
            return cobro.IdCobro;
        }

        public int AltaEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cobro cobro) => Alta(cobro);

        public List<BE.Cobro> ObtenerPorCliente(int idCliente) => Registros.FindAll(c => c.IdCliente == idCliente);
        public List<BE.Cobro> ObtenerTodos() => Registros;
        public BE.Cobro ObtenerPorId(int id) => Registros.Find(c => c.IdCobro == id);

        public void AsignarComprobanteEnTx(SqlConnection conexion, SqlTransaction tx, int idCobro, string numeroComprobante)
        {
            var c = ObtenerPorId(idCobro);
            if (c != null) c.NumeroComprobante = numeroComprobante;
        }

        // Catálogo MedioPago (mismo contenido que el seed de la BD).
        public List<BE.MedioPago> MediosPago { get; set; } = new List<BE.MedioPago>
        {
            new BE.MedioPago { IdMedioPago = 1, Nombre = "Efectivo" },
            new BE.MedioPago { IdMedioPago = 2, Nombre = "Tarjeta de débito" },
            new BE.MedioPago { IdMedioPago = 3, Nombre = "Transferencia" },
            new BE.MedioPago { IdMedioPago = 4, Nombre = "Tarjeta de crédito", PermiteCuotas = true }
        };
        public List<BE.MedioPago> ObtenerMediosPago() => MediosPago;
    }
}
