using System;
using System.Collections.Generic;
using BLL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IListaEsperaService (sin base de datos), pensado para ejercitar
    /// los catches fail-open de BLL.Pedido sobre Lista de Espera (mejora opcional: una falla
    /// acá no debe abortar la creación/consulta de disponibilidad de un pedido). Cada método
    /// puede configurarse para lanzar, y cuenta invocaciones.
    /// </summary>
    public class FakeListaEsperaService : IListaEsperaService
    {
        public bool CerrarSiReservadaLanza { get; set; }
        public bool EstaReservadaParaOtroLanza { get; set; }
        public bool EstaReservadaParaOtroRespuesta { get; set; }

        public int CerrarSiReservadaVeces { get; private set; }
        public int EstaReservadaParaOtroVeces { get; private set; }

        public void CerrarSiReservada(string modulo, int idPrenda, int idCliente, string actor)
        {
            CerrarSiReservadaVeces++;
            if (CerrarSiReservadaLanza) throw new InvalidOperationException("Fallo simulado de Lista de Espera.");
        }

        public bool EstaReservadaParaOtro(int idPrenda, int idClienteSolicitante)
        {
            EstaReservadaParaOtroVeces++;
            if (EstaReservadaParaOtroLanza) throw new InvalidOperationException("Fallo simulado de Lista de Espera.");
            return EstaReservadaParaOtroRespuesta;
        }

        // Resto del contrato: no ejercitado por estos tests, cuerpos mínimos.
        public void Anotar(string modulo, int idPrenda, int idCliente, string actor) { }
        public void Cancelar(string modulo, int idListaEspera, string actor) { }
        public void NotificarSiCorresponde(int idPrenda, string actor) { }
        public List<int> ObtenerIdsReservadosParaOtro(int? idClienteSolicitante) => new List<int>();
        public List<BE.ListaEspera> ObtenerActivas() => new List<BE.ListaEspera>();
        public List<BE.ListaEspera> ObtenerPorPrenda(int idPrenda) => new List<BE.ListaEspera>();
        public int ContarReservadasVigentes() => 0;
    }
}
