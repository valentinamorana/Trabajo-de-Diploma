using System;
using System.Collections.Generic;
using System.Linq;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble de prueba de IListaEsperaDAL (sin base de datos). En memoria, con espías.</summary>
    public class FakeListaEsperaDAL : IListaEsperaDAL
    {
        public readonly List<BE.ListaEspera> Registros = new List<BE.ListaEspera>();
        public int AltaVeces { get; private set; }
        public int CambiarEstadoVeces { get; private set; }

        public int Alta(BE.ListaEspera fila)
        {
            AltaVeces++;
            fila.IdListaEspera = Registros.Count + 1;
            Registros.Add(fila);
            return fila.IdListaEspera;
        }

        public BE.ListaEspera ObtenerPorId(int id) => Registros.FirstOrDefault(f => f.IdListaEspera == id);

        public BE.ListaEspera ObtenerPendienteMasAntigua(int idPrenda) =>
            Registros.Where(f => f.IdPrenda == idPrenda && f.Estado == BE.EstadoListaEspera.Pendiente)
                     .OrderBy(f => f.FechaAlta).FirstOrDefault();

        public BE.ListaEspera ObtenerReservaVigenteDeOtro(int idPrenda, int idClienteSolicitante) =>
            Registros.FirstOrDefault(f => f.IdPrenda == idPrenda && f.IdCliente != idClienteSolicitante
                                        && f.Estado == BE.EstadoListaEspera.Reservada && f.ReservaVigente);

        public BE.ListaEspera ObtenerReservaVigenteDeCliente(int idPrenda, int idCliente) =>
            Registros.FirstOrDefault(f => f.IdPrenda == idPrenda && f.IdCliente == idCliente
                                        && f.Estado == BE.EstadoListaEspera.Reservada && f.ReservaVigente);

        public List<BE.ListaEspera> ObtenerActivas() =>
            Registros.Where(f => f.Estado == BE.EstadoListaEspera.Pendiente || f.Estado == BE.EstadoListaEspera.Reservada).ToList();

        public List<BE.ListaEspera> ObtenerPorPrenda(int idPrenda) =>
            Registros.Where(f => f.IdPrenda == idPrenda).ToList();

        public int ContarReservadasVigentes() =>
            Registros.Count(f => f.Estado == BE.EstadoListaEspera.Reservada && f.ReservaVigente);

        public void CambiarEstado(int idListaEspera, BE.EstadoListaEspera nuevoEstado,
                                   DateTime? fechaLimiteReserva, string actor)
        {
            CambiarEstadoVeces++;
            var fila = Registros.FirstOrDefault(f => f.IdListaEspera == idListaEspera);
            if (fila == null) return;
            fila.Estado = nuevoEstado;
            fila.FechaLimiteReserva = fechaLimiteReserva;
            fila.Actor = actor;
        }
    }
}
