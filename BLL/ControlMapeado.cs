using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Capa de Lógica de Negocio — Etapa 4 (permisos a nivel de control).
    /// Expone los mapeos control↔patente a la GUI (respetando el orden de capas: GUI → BLL → DAL)
    /// y las reglas puras que decide qué controles de un formulario ve cada usuario. Las aplica
    /// GUI.ManejadorSeguridad (que solo se ocupa de encontrar y mostrar/ocultar los controles).
    /// </summary>
    public class ControlMapeado
    {
        // El Menú principal NO se gobierna por control: tiene su propia lógica por NombreMenu
        // (Menu.AplicarPermisos / BLL.MenuVisibilidad). Se excluye para no aplicar dos mecanismos
        // sobre los mismos ítems.
        public const string FormularioMenu = "Menu";

        private readonly DAL.Interfaces.IControlMapeadoDAL _dal;

        public ControlMapeado() : this(new DAL.ControlMapeado()) { }
        public ControlMapeado(DAL.Interfaces.IControlMapeadoDAL dal) { _dal = dal; }

        public List<BE.ControlMapeado> ObtenerTodos() => _dal.ObtenerTodos();

        /// <summary>
        /// Mapeos que se aplican a un formulario (comparación por nombre sin distinguir mayúsculas).
        /// El formulario "Menu" no recibe mapeos de control (tiene su propio gateo por NombreMenu).
        /// </summary>
        public static List<BE.ControlMapeado> AplicablesA(IEnumerable<BE.ControlMapeado> todos, string formulario)
        {
            if (todos == null || string.IsNullOrEmpty(formulario)
                || string.Equals(formulario, FormularioMenu, StringComparison.OrdinalIgnoreCase))
                return new List<BE.ControlMapeado>();
            return todos.Where(c => c != null
                                    && string.Equals(c.Formulario, formulario, StringComparison.OrdinalIgnoreCase))
                        .ToList();
        }

        /// <summary>Ids de las patentes efectivas del usuario (vacío si no tiene permisos cargados).</summary>
        public static HashSet<int> PatentesDe(BE.Usuario usuario)
        {
            var patentes = new HashSet<int>();
            if (usuario?.Permisos != null)
                foreach (var p in usuario.Permisos) patentes.Add(p.Id);
            return patentes;
        }

        /// <summary>
        /// Regla de acceso a un control mapeado: el Administrador ve todo (bypass B1); el resto, solo
        /// si tiene la patente exacta del mapeo.
        /// </summary>
        public static bool TieneAcceso(BE.ControlMapeado mapeo, bool esAdmin, ICollection<int> patentesUsuario)
            => esAdmin || (mapeo != null && patentesUsuario != null && patentesUsuario.Contains(mapeo.IdPermiso));
    }
}
