using System;
using System.Collections.Generic;
using System.Linq;
using Servicios.Serializacion;

namespace BLL
{
    /// <summary>
    /// A02 Serialización — consulta, importación y exportación de los errores serializados en XML
    /// (Servicios.Serializacion.RegistroErrores). Es información de auditoría: solo con la patente
    /// de Auditoría (la misma de la Bitácora). La importación valida el archivo antes de mostrarlo.
    /// </summary>
    public class ErroresSerializados
    {
        private readonly Servicios.IRegistroBitacora _bitacora;

        public ErroresSerializados() : this(Servicios.FabricaBitacora.CrearSistema()) { }
        internal ErroresSerializados(Servicios.IRegistroBitacora bitacora) { _bitacora = bitacora; }

        /// <summary>Errores registrados en esta PC, del más nuevo al más viejo.</summary>
        public List<BE.RegistroError> ObtenerRegistrados()
        {
            PermisosAccion.Exigir(BE.Patentes.Auditoria, BE.Patentes.Auditoria);
            return RegistroErrores.ObtenerTodos();
        }

        /// <summary>Deserializa un archivo XML de errores (por ejemplo, traído de otra PC).</summary>
        public List<BE.RegistroError> Importar(string modulo, string ruta)
        {
            PermisosAccion.Exigir(BE.Patentes.Auditoria, BE.Patentes.Auditoria);
            var libro = SerializadorXml.LeerArchivo<BE.LibroErrores>(ruta);
            var errores = (libro?.Errores ?? new List<BE.RegistroError>()).OrderByDescending(e => e.Fecha).ToList();
            Registrar(modulo, $"Importación de errores serializados: {errores.Count} registro(s) desde '{System.IO.Path.GetFileName(ruta)}'");
            return errores;
        }

        /// <summary>Serializa a XML la lista que se está mostrando.</summary>
        public void Exportar(string modulo, List<BE.RegistroError> errores, string ruta)
        {
            PermisosAccion.Exigir(BE.Patentes.Auditoria, BE.Patentes.Auditoria);
            if (errores == null || errores.Count == 0)
                throw new BE.AppException("err.serial.sin_datos", "No hay errores para exportar.");
            SerializadorXml.GuardarArchivo(new BE.LibroErrores { Errores = errores }, ruta);
            Registrar(modulo, $"Exportación de errores serializados: {errores.Count} registro(s) a '{System.IO.Path.GetFileName(ruta)}'");
        }

        private void Registrar(string modulo, string actividad)
        {
            try { _bitacora.Registrar(modulo, actividad, BE.Criticidad.Baja); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.ErroresSerializados] " + ex.Message); }
        }
    }
}
