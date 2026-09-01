using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Capa de Lógica de Negocio — Etapa 4 (permisos a nivel de control).
    /// Expone los mapeos control↔patente a la GUI (respetando el orden de capas: GUI → BLL → DAL).
    /// </summary>
    public class ControlMapeado
    {
        private readonly DAL.Interfaces.IControlMapeadoDAL _dal;

        public ControlMapeado() : this(new DAL.ControlMapeado()) { }
        public ControlMapeado(DAL.Interfaces.IControlMapeadoDAL dal) { _dal = dal; }

        public List<BE.ControlMapeado> ObtenerTodos() => _dal.ObtenerTodos();

        public List<BE.ControlMapeado> ObtenerPorPermiso(int idPermiso) => _dal.ObtenerPorPermiso(idPermiso);
    }
}
