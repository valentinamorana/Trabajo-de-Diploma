namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Preferencias de UI por usuario. Fachada simple sobre el DAL.
    /// </summary>
    public class Preferencia
    {
        private readonly DAL.Interfaces.IPreferenciaDAL _dal;

        // DI: el constructor por defecto usa el DAL real; el otro permite inyectar un doble de prueba.
        public Preferencia() : this(new DAL.Preferencia()) { }
        public Preferencia(DAL.Interfaces.IPreferenciaDAL dal) { _dal = dal; }

        public BE.Preferencia Obtener(int idUsuario)
        {
            return _dal.Obtener(idUsuario) ?? new BE.Preferencia { IdUsuario = idUsuario };
        }

        public void Guardar(BE.Preferencia pref)
        {
            if (pref == null)
                throw new BE.AppException("err.bll.pref.nula", "No hay preferencias para guardar.");
            // Solo el propio usuario de la sesión guarda sus preferencias.
            if (!Seguridad.SessionManager.IsLoggedIn)
                throw new BE.AppException("err.bll.sesion_expirada", "La sesión expiró. Volvé a iniciar sesión.");
            if (Seguridad.SessionManager.GetInstance().Usuario.Id != pref.IdUsuario)
                throw new BE.AppException("err.bll.usuario.preferencia_ajena",
                    "Solo podés cambiar tu propia preferencia de idioma.");
            _dal.Guardar(pref);
        }
    }
}
