using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IClaveRecuperacionDAL (sin base de datos). Antes no existía —
    /// BLL.RecuperacionAdmin estaba diseñada explícitamente para testearse por interfaces
    /// (ver su propio doc-comment) pero no tenía ni el doble necesario para hacerlo.
    /// </summary>
    public class FakeClaveRecuperacionDAL : IClaveRecuperacionDAL
    {
        // Claves "disponibles" sembradas por el test: Id -> hash (Encriptador.Hash(claveEnClaro)).
        public List<KeyValuePair<int, string>> Disponibles { get; set; } = new List<KeyValuePair<int, string>>();

        // Si es true, MarcarUsada devuelve false (simula que otra ejecución la consumió antes).
        public bool SimularConsumidaPorOtro { get; set; }

        public int InsertarVeces { get; private set; }
        public int MarcarUsadaVeces { get; private set; }
        public int UltimoIdMarcado { get; private set; }
        public string UltimoUsernameMarcado { get; private set; }

        public void Insertar(string claveHash)
        {
            InsertarVeces++;
            Disponibles.Add(new KeyValuePair<int, string>(Disponibles.Count + 1, claveHash));
        }

        public int ContarTotal() => Disponibles.Count;

        public int ContarDisponibles() => Disponibles.Count;

        public List<KeyValuePair<int, string>> ObtenerDisponibles() => Disponibles;

        public bool MarcarUsada(int idClave, string username)
        {
            MarcarUsadaVeces++;
            UltimoIdMarcado = idClave;
            UltimoUsernameMarcado = username;
            if (SimularConsumidaPorOtro) return false;

            Disponibles.RemoveAll(kv => kv.Key == idClave);
            return true;
        }
    }
}
