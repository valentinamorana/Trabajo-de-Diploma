using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IClienteDAL (sin base de datos). Espía sobre Modificar.</summary>
    public class FakeClienteDAL : IClienteDAL
    {
        public int ModificarVeces { get; private set; }
        public BE.Cliente UltimoModificado { get; private set; }
        public int RecalcularDVVeces { get; private set; }

        public List<BE.Cliente> ObtenerTodos() => new List<BE.Cliente>();
        public BE.Cliente ObtenerPorId(int idCliente) => null;
        public int Alta(BE.Cliente cliente) => 0;

        public void Modificar(BE.Cliente cliente)
        {
            ModificarVeces++;
            UltimoModificado = cliente;
        }

        public void Baja(int idCliente) { }
        public bool ExisteDNI(string dni) => false;
        public bool ExisteDNIParaOtro(string dni, int idExcluir) => false;

        // Sin BD real: no hay transacción que abrir, se ejecuta la acción directamente
        // (conexión/transacción null — los EnTx de estos fakes no las usan).
        public void EjecutarTransaccion(Action<SqlConnection, SqlTransaction> accion) => accion(null, null);

        public void ModificarEnTx(SqlConnection conexion, SqlTransaction tx, BE.Cliente cliente) => Modificar(cliente);

        public void RecalcularDV() => RecalcularDVVeces++;
    }
}
