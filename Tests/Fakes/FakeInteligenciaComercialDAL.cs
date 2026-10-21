using System;
using System.Collections.Generic;
using System.Linq;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de IInteligenciaComercialDAL (PN03, Centro de decisiones): filas en memoria filtradas por fecha
    /// como las consultas reales, y ParametroSistema simulado para los umbrales.
    /// </summary>
    public class FakeInteligenciaComercialDAL : IInteligenciaComercialDAL
    {
        public List<BE.ClienteComercial> Clientes { get; set; } = new List<BE.ClienteComercial>();
        public List<BE.MovimientoSuscripcion> Movimientos { get; set; } = new List<BE.MovimientoSuscripcion>();
        public List<BE.AltaSuscripcion> Altas { get; set; } = new List<BE.AltaSuscripcion>();
        public List<BE.CobroComercial> Cobros { get; set; } = new List<BE.CobroComercial>();
        public List<BE.PrendaComercial> Prendas { get; set; } = new List<BE.PrendaComercial>();
        public Dictionary<string, string> Parametros { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Si es true, leer las prendas falla (simula un error de BD en una fuente secundaria).</summary>
        public bool FallarPrendas { get; set; }
        public bool FallarParametros { get; set; }
        public int GuardarVeces { get; private set; }

        public List<BE.ClienteComercial> ObtenerClientes() => Clientes;

        public List<BE.MovimientoSuscripcion> ObtenerMovimientos(DateTime desde, DateTime hastaExcl) =>
            Movimientos.Where(m => m.Fecha >= desde && m.Fecha < hastaExcl).ToList();

        public List<BE.AltaSuscripcion> ObtenerAltas(DateTime desde, DateTime hastaExcl) =>
            Altas.Where(a => a.Fecha >= desde && a.Fecha < hastaExcl).ToList();

        public List<BE.CobroComercial> ObtenerCobros(DateTime desde, DateTime hastaExcl) =>
            Cobros.Where(c => c.Fecha >= desde && c.Fecha < hastaExcl).ToList();

        public List<BE.PrendaComercial> ObtenerPrendas(DateTime desde, DateTime hastaExcl) =>
            FallarPrendas ? throw new Exception("falla simulada") : Prendas;

        public Dictionary<string, string> ObtenerParametros(string prefijo) =>
            FallarParametros ? throw new Exception("falla simulada") : new Dictionary<string, string>(Parametros, StringComparer.OrdinalIgnoreCase);

        public void GuardarParametros(string prefijo, Dictionary<string, string> valores)
        {
            GuardarVeces++;
            foreach (var kv in valores) Parametros[kv.Key] = kv.Value;
        }
    }

    /// <summary>Doble de IDecisionComercialDAL: imita el alta con ID y el claim atómico por estado esperado.</summary>
    public class FakeDecisionComercialDAL : IDecisionComercialDAL
    {
        public List<BE.DecisionComercial> Filas { get; } = new List<BE.DecisionComercial>();
        private int siguienteId = 1;
        public bool FallarLectura { get; set; }

        private static BE.DecisionComercial Copia(BE.DecisionComercial d) => (BE.DecisionComercial)typeof(object)
            .GetMethod("MemberwiseClone", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
            .Invoke(d, null);

        public int Alta(BE.DecisionComercial decision)
        {
            var copia = Copia(decision);
            copia.IdDecision = siguienteId++;
            Filas.Add(copia);
            return copia.IdDecision;
        }

        public bool CambiarEstado(BE.DecisionComercial decision, BE.EstadoDecision estadoEsperado)
        {
            int i = Filas.FindIndex(f => f.IdDecision == decision.IdDecision);
            if (i < 0 || Filas[i].Estado != estadoEsperado) return false;
            Filas[i] = Copia(decision);
            return true;
        }

        public bool RegistrarResultado(int idDecision, decimal resultadoReal, DateTime fechaEvaluacion)
        {
            var f = Filas.Find(x => x.IdDecision == idDecision);
            if (f == null || !f.AdmiteResultado()) return false;
            f.ResultadoReal = resultadoReal;
            f.FechaEvaluacion = fechaEvaluacion;
            return true;
        }

        public BE.DecisionComercial ObtenerPorId(int idDecision)
        {
            var f = Filas.Find(x => x.IdDecision == idDecision);
            return f == null ? null : Copia(f);
        }

        public List<BE.DecisionComercial> Obtener(DateTime? desde, DateTime? hastaExcl)
        {
            if (FallarLectura) throw new Exception("falla simulada");
            return Filas.Where(f => (!desde.HasValue || f.Fecha >= desde) && (!hastaExcl.HasValue || f.Fecha < hastaExcl))
                        .OrderByDescending(f => f.Fecha).Select(Copia).ToList();
        }

        /// <summary>Detalle de cada decisión: (IdDecision, Tipo) → IDs.</summary>
        public Dictionary<(int, string), List<int>> Detalle { get; } = new Dictionary<(int, string), List<int>>();

        /// <summary>Si es true, EjecutarConDetalle simula que otra sesión cambió la decisión (no escribe nada).</summary>
        public bool FallarEjecucion { get; set; }

        public bool EjecutarConDetalle(BE.DecisionComercial decision, BE.EstadoDecision estadoEsperado, string tipo, IEnumerable<int> ids)
        {
            if (FallarEjecucion || !CambiarEstado(decision, estadoEsperado)) return false;   // todo o nada, como la transacción
            if (!Detalle.TryGetValue((decision.IdDecision, tipo), out var lista)) Detalle[(decision.IdDecision, tipo)] = lista = new List<int>();
            foreach (int id in ids ?? Enumerable.Empty<int>())
                if (!lista.Contains(id)) lista.Add(id);
            return true;
        }

        public Dictionary<int, List<int>> ObtenerDetalle(IEnumerable<int> idsDecision, string tipo)
        {
            var pedidos = new HashSet<int>(idsDecision ?? Enumerable.Empty<int>());
            return Detalle.Where(kv => kv.Key.Item2 == tipo && pedidos.Contains(kv.Key.Item1))
                          .ToDictionary(kv => kv.Key.Item1, kv => kv.Value.ToList());
        }
    }

    /// <summary>Doble de IMetaComercialDAL: metas en memoria, con alta, modificación y baja.</summary>
    public class FakeMetaComercialDAL : IMetaComercialDAL
    {
        public List<BE.MetaComercial> Filas { get; } = new List<BE.MetaComercial>();
        private int siguienteId = 1;

        private static BE.MetaComercial Copia(BE.MetaComercial m) => new BE.MetaComercial
        {
            IdMeta = m.IdMeta, Periodo = m.Periodo, Indicador = m.Indicador, ValorObjetivo = m.ValorObjetivo,
            Sentido = m.Sentido, IdUsuario = m.IdUsuario, NombreUsuario = m.NombreUsuario, Fecha = m.Fecha
        };

        public List<BE.MetaComercial> Obtener(DateTime desdePeriodo, DateTime hastaPeriodo) =>
            Filas.Where(m => m.Periodo >= desdePeriodo && m.Periodo <= hastaPeriodo)
                 .OrderBy(m => m.Periodo).ThenBy(m => m.Indicador).Select(Copia).ToList();

        public int Alta(BE.MetaComercial meta)
        {
            if (Filas.Any(f => f.Periodo == meta.Periodo && f.Indicador == meta.Indicador))
                throw new Exception("violación de UQ_MetaComercial (simulada)");
            var copia = Copia(meta);
            copia.IdMeta = siguienteId++;
            Filas.Add(copia);
            return copia.IdMeta;
        }

        public bool Modificar(BE.MetaComercial meta)
        {
            int i = Filas.FindIndex(f => f.IdMeta == meta.IdMeta);
            if (i < 0) return false;
            Filas[i] = Copia(meta);
            return true;
        }

        public bool Eliminar(int idMeta) => Filas.RemoveAll(f => f.IdMeta == idMeta) > 0;
    }
}
