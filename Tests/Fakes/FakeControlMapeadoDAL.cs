using System.Collections.Generic;
using System.Linq;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>Doble en memoria de IControlMapeadoDAL (mapeos control↔patente, Etapa 4).</summary>
    public class FakeControlMapeadoDAL : IControlMapeadoDAL
    {
        public List<BE.ControlMapeado> Mapeos { get; } = new List<BE.ControlMapeado>();
        public int LlamadasObtenerTodos { get; private set; }

        public List<BE.ControlMapeado> ObtenerTodos()
        {
            LlamadasObtenerTodos++;
            return Mapeos.ToList();
        }

        public List<BE.ControlMapeado> ObtenerPorPermiso(int idPermiso)
            => Mapeos.Where(m => m.IdPermiso == idPermiso).ToList();
    }
}
