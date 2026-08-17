using System;
using System.Collections.Generic;
using System.Data;

namespace DAL
{
    /// <summary>
    /// T05 — Acceso a datos de la entidad CONTROL (textos traducibles del sistema).
    /// </summary>
    public class Control
    {
        private readonly Acceso acceso = Acceso.GetInstance();

        // Todos los controles traducibles, para el grid de Controles del FormIdiomas.
        public List<BE.Control> ObtenerTodos()
        {
            var lista = new List<BE.Control>();
            DataTable dt = acceso.Leer(
                "SELECT IdControl, Clave, Formulario FROM Control ORDER BY Formulario, Clave", null);
            if (dt == null) return lista;
            foreach (DataRow row in dt.Rows)
                lista.Add(new BE.Control
                {
                    IdControl  = Convert.ToInt32(row["IdControl"]),
                    Clave      = row["Clave"].ToString(),
                    Formulario = row["Formulario"].ToString()
                });
            return lista;
        }

        // Alta/lookup de un Control por clave vive en DAL.Traduccion.ObtenerOCrearControl
        // (único lugar que lo necesita — evita mantener la misma consulta en dos DAO).
    }
}
