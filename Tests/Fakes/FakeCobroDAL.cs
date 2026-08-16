using System.Collections.Generic;
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

        public List<BE.Cobro> ObtenerPorCliente(int idCliente) => Registros.FindAll(c => c.IdCliente == idCliente);
        public List<BE.Cobro> ObtenerTodos() => Registros;
    }
}
