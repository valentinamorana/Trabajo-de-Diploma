using System.Collections.Generic;

namespace DAL
{
    /// <summary>
    /// Clase base NO genérica: centraliza el acceso a BD para todos los DAL, tengan o no
    /// forma de "entidad con ObtenerTodos/ObtenerPorId" (para esa variante ver
    /// <see cref="BaseDAL{T}"/>). Reemplaza el "private readonly Acceso acceso =
    /// Acceso.GetInstance();" que se repetía copiado en cada DAL — varios (Bitacora,
    /// Traduccion, Permiso, historiales de auditoría, utilidades como DigitoVerificador)
    /// no calzan en el contrato ObtenerTodos/ObtenerPorId de un CRUD por ID, y forzarlos
    /// a implementarlo solo para "unificar" agregaría métodos sin uso real — heredar de
    /// esta base alcanza para ellos.
    /// </summary>
    public abstract class BaseDAL
    {
        protected readonly Acceso acceso = Acceso.GetInstance();
    }

    /// <summary>
    /// Variante para DAL de tipo "entidad con ID": además del acceso compartido, fuerza
    /// el contrato ObtenerTodos/ObtenerPorId.
    /// </summary>
    /// <typeparam name="T">Tipo de entidad que persiste este DAL.</typeparam>
    public abstract class BaseDAL<T> : BaseDAL where T : class
    {
        public abstract List<T> ObtenerTodos();
        public abstract T ObtenerPorId(int id);
    }
}
