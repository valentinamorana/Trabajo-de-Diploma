using System.Collections.Generic;
using DAL.Interfaces;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble de prueba de IPermisoDAL (sin base de datos). Por defecto devuelve un árbol
    /// Composite fijo en memoria para testear la resolución recursiva de permisos:
    ///
    ///   Rol "Gerente"
    ///     └─ Rol "Admin"
    ///          ├─ Familia "Ventas"
    ///          │     ├─ Patente "Clientes" (Id 1)
    ///          │     └─ Patente "Stock"    (Id 2)
    ///          └─ Patente "Clientes" (Id 1)   ← mismo permiso por un segundo camino
    ///
    /// Configurable además para ejercitar los métodos de ESCRITURA de BLL.Familia
    /// (ArbolPersonalizado, IdsPorRol, ContarUsuariosPorRolRespuesta) con espías sobre
    /// cada operación de mutación.
    /// </summary>
    public class FakePermisoDAL : IPermisoDAL
    {
        // Si se setea, ObtenerArbol() devuelve este árbol en vez del fijo de arriba —
        // usado por los tests de escritura, que necesitan controlar la forma del árbol.
        public List<BE.Componente> ArbolPersonalizado { get; set; }

        public Dictionary<string, int> IdsPorRol { get; set; } = new Dictionary<string, int>();
        // Hijos DIRECTOS configurables por id de padre — usado por ObtenerIdsDirectosDelRol
        // (BLL.Familia) para saber qué tiene HOY un rol antes de comparar contra una selección
        // propuesta (guard anti-autoescalación). Sin configurar, se comporta como antes (vacío).
        public Dictionary<int, List<int>> HijosPorIdPadre { get; set; } = new Dictionary<int, List<int>>();
        public int ContarUsuariosPorRolRespuesta { get; set; }
        public List<string> ObtenerUsuariosPorRolRespuesta { get; set; } = new List<string>();
        public int AltaComponenteIdGenerado { get; set; } = 1;

        public int AgregarRelacionVeces { get; private set; }
        public (int Padre, int Hijo) UltimaRelacionAgregada { get; private set; }
        public int QuitarRelacionVeces { get; private set; }
        public (int Padre, int Hijo) UltimaRelacionQuitada { get; private set; }
        public int AltaComponenteVeces { get; private set; }
        public int BajaComponenteVeces { get; private set; }
        public int UltimoBajaComponente { get; private set; }

        public List<BE.Componente> ObtenerArbol()
        {
            if (ArbolPersonalizado != null) return ArbolPersonalizado;

            var p1 = new BE.Patente { Id = 1, Nombre = "Clientes", NombreMenu = "mnuClientes" };
            var p2 = new BE.Patente { Id = 2, Nombre = "Stock",    NombreMenu = "mnuStock" };

            var ventas = new BE.Familia { Id = 10, Nombre = "Ventas" };
            ventas.AgregarHijo(p1);
            ventas.AgregarHijo(p2);

            var admin = new BE.Rol { Id = 100, Nombre = "Admin" };
            admin.AgregarHijo(ventas);
            admin.AgregarHijo(p1);     // segundo camino al mismo permiso → debe deduplicarse

            var gerente = new BE.Rol { Id = 200, Nombre = "Gerente" };
            gerente.AgregarHijo(admin); // rol dentro de rol

            return new List<BE.Componente> { gerente };
        }

        public List<string>     ObtenerRoles()                  => new List<string> { "Admin", "Gerente" };
        public List<BE.Permiso> ObtenerPorRol(string rol)       => new List<BE.Permiso>();   // fallback vacío
        public int              ObtenerIdRol(string rolNombre)  => IdsPorRol.TryGetValue(rolNombre, out int id) ? id : 0;
        public List<int>        ObtenerIdsHijos(int idPadre)
            => HijosPorIdPadre.TryGetValue(idPadre, out var hijos) ? hijos : new List<int>();

        public void AgregarRelacion(int p, int h)
        {
            AgregarRelacionVeces++;
            UltimaRelacionAgregada = (p, h);
        }

        public void QuitarRelacion(int p, int h)
        {
            QuitarRelacionVeces++;
            UltimaRelacionQuitada = (p, h);
        }

        public int AltaComponente(string n, string m, bool f, bool r, string t)
        {
            AltaComponenteVeces++;
            return AltaComponenteIdGenerado;
        }

        public void ModificarComponente(int id, string n, string m) { }

        public void BajaComponente(int id)
        {
            BajaComponenteVeces++;
            UltimoBajaComponente = id;
        }

        public int              ContarUsuariosPorRol(string r)  => ContarUsuariosPorRolRespuesta;
        public List<string>     ObtenerUsuariosPorRol(string r) => ObtenerUsuariosPorRolRespuesta;
        public List<string>     ObtenerNombresMenuPatentes()    => new List<string>();
    }
}
