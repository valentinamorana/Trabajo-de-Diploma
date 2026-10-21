using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Servicios.Serializacion;

namespace BLL
{
    /// <summary>
    /// A02 Serialización — consulta, importación y exportación de los errores serializados
    /// (Servicios.Serializacion.RegistroErrores). El registro de esta PC es XML; la importación y la
    /// exportación aceptan XML o JSON (el formato sale de la extensión del archivo). Es información de
    /// auditoría: solo con la patente de Auditoría (la misma de la Bitácora). La importación valida el
    /// archivo antes de mostrarlo (extensión, tamaño, formato, versión de formato y registros), igual
    /// en los dos formatos.
    /// </summary>
    public class ErroresSerializados
    {
        /// <summary>
        /// Tamaño máximo de un archivo de errores que se acepta importar: el mismo que el de los
        /// historiales de versiones (5 MB; un archivo mensual tiene como máximo 500 errores).
        /// </summary>
        public const long TamanoMaximoArchivo = VersionUsuario.TamanoMaximoArchivo;

        private readonly Servicios.IRegistroBitacora _bitacora;

        public ErroresSerializados() : this(Servicios.FabricaBitacora.CrearSistema()) { }
        internal ErroresSerializados(Servicios.IRegistroBitacora bitacora) { _bitacora = bitacora; }

        /// <summary>Errores registrados en esta PC, del más nuevo al más viejo.</summary>
        public List<BE.RegistroError> ObtenerRegistrados()
        {
            PermisosAccion.Exigir(BE.Patentes.Auditoria, BE.Patentes.Auditoria);
            return RegistroErrores.ObtenerTodos();
        }

        /// <summary>Deserializa un archivo XML o JSON de errores (por ejemplo, traído de otra PC).</summary>
        public List<BE.RegistroError> Importar(string modulo, string ruta)
        {
            PermisosAccion.Exigir(BE.Patentes.Auditoria, BE.Patentes.Auditoria);
            string formato = FabricaSerializadores.Formato(ruta);   // rechaza una extensión no admitida
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                throw new BE.AppException("err.serial.no_existe", "No se encontró el archivo '{0}'.", ruta ?? "");
            if (new FileInfo(ruta).Length > TamanoMaximoArchivo)
                throw new BE.AppException("err.bll.errores.muy_grande",
                    "El archivo supera el tamaño máximo admitido ({0} MB); no parece un archivo de errores de WardrobeFlow.",
                    TamanoMaximoArchivo / (1024 * 1024));

            BE.LibroErrores libro;
            try { libro = RegistroErrores.LeerArchivo(ruta); }   // valida también la versión de formato
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new BE.AppException("err.bll.version.xml.no_lee",
                    "No se pudo leer el archivo '{0}': {1}", Path.GetFileName(ruta), ex.Message);
            }
            // Mismo criterio que el historial de versiones: un archivo con registros vacíos no es un
            // archivo exportado por WardrobeFlow y se rechaza entero.
            if (libro.Errores.Any(e => e == null))
                throw new BE.AppException("err.bll.errores.inconsistente",
                    "El archivo es inconsistente: contiene registros de error vacíos.");

            var errores = libro.Errores.OrderByDescending(e => e.Fecha).ToList();
            Registrar(modulo, $"Importación de errores serializados desde {formato}: {errores.Count} registro(s) desde '{Path.GetFileName(ruta)}'");
            return errores;
        }

        /// <summary>
        /// Serializa a XML o JSON (según la extensión de la ruta) la lista que se está mostrando.
        /// Devuelve el nombre del formato usado ("XML" o "JSON").
        /// </summary>
        public string Exportar(string modulo, List<BE.RegistroError> errores, string ruta)
        {
            PermisosAccion.Exigir(BE.Patentes.Auditoria, BE.Patentes.Auditoria);
            if (errores == null || errores.Count == 0)
                throw new BE.AppException("err.serial.sin_datos", "No hay errores para exportar.");
            if (string.IsNullOrWhiteSpace(ruta))
                throw new BE.AppException("err.bll.version.xml.sin_ruta", "Indique el archivo de destino.");

            string formato;
            try { formato = RegistroErrores.Exportar(errores, ruta); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new BE.AppException("err.bll.version.xml.no_escribe",
                    "No se pudo guardar el archivo '{0}': {1}", Path.GetFileName(ruta), ex.Message);
            }
            Registrar(modulo, $"Exportación de errores serializados a {formato}: {errores.Count} registro(s) a '{Path.GetFileName(ruta)}'");
            return formato;
        }

        private void Registrar(string modulo, string actividad)
        {
            try { _bitacora.Registrar(modulo, actividad, BE.Criticidad.Baja); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.ErroresSerializados] " + ex.Message); }
        }
    }
}
