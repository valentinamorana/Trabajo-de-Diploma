using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Servicios.Serializacion;

namespace BLL.Inteligencia
{
    /// <summary>
    /// PN03 + A02: «Constancia de la decisión» (1.6.1). Al Aceptar, Descartar o Posponer una recomendación
    /// (DecisionesComerciales) se guarda una foto de lo que el usuario vio: la recomendación, la opción
    /// elegida y las alternativas, el período y el plan, los indicadores, los Parámetros del análisis, los
    /// supuestos (parámetros y fórmulas) y las advertencias del panel. Se guarda en XML en
    /// DecisionComercial.Constancia (base compartida entre PCs) y se exporta o importa en XML o JSON con la
    /// fábrica de serializadores existente (Servicios.Serializacion: sin DTD ni entidades externas, versión
    /// de formato) más un tamaño máximo.
    ///
    /// Al abrir un archivo se verifica contra la base: si la decisión existe y tiene constancia, se compara
    /// campo por campo (los valores normalizados de la entidad, no el texto del archivo) y se informa
    /// «coincide» o «fue modificado» con los campos que difieren; si no existe, «sin registro».
    /// Todo con la patente de Gerencia del Centro (mnuSugerenciaPromocion).
    /// </summary>
    public class ConstanciasDecision
    {
        /// <summary>Tamaño máximo de un archivo de constancia (una constancia real pesa unos pocos KB).</summary>
        public const long TamanoMaximoArchivo = 1L * 1024 * 1024;
        public const string PrefijoArchivo = "constancia_decision";

        private readonly DAL.Interfaces.IDecisionComercialDAL dal;
        private readonly Servicios.IRegistroBitacora bitacora = Servicios.FabricaBitacora.CrearSistema();

        public ConstanciasDecision() : this(new DAL.DecisionComercial()) { }

        public ConstanciasDecision(DAL.Interfaces.IDecisionComercialDAL dal)
        {
            this.dal = dal ?? throw new ArgumentNullException(nameof(dal));
        }

        // ── Armado (puro) ─────────────────────────────────────────────────────────

        /// <summary>
        /// Arma la constancia de una decisión ya registrada (con IdDecision). <paramref name="indiceOpcion"/>
        /// es la opción elegida al aceptar (0 = principal); al descartar o posponer, la principal.
        /// </summary>
        public static BE.ConstanciaDecision Armar(BE.DecisionComercial decision, BE.Recomendacion rec, int indiceOpcion,
                                                  BE.PanelComercial panel, BE.Usuario usuario)
        {
            if (decision == null) throw new ArgumentNullException(nameof(decision));
            if (rec == null) throw new ArgumentNullException(nameof(rec));
            var opciones = rec.Opciones();
            var elegida = indiceOpcion >= 0 && indiceOpcion < opciones.Count ? opciones[indiceOpcion] : rec.Principal;

            var c = new BE.ConstanciaDecision
            {
                Version = BE.ConstanciaDecision.FormatoActual,
                IdDecision = decision.IdDecision,
                Fecha = Segundos(decision.Fecha),
                Username = usuario?.Username,
                NombreUsuario = NombreCompleto(usuario),
                Accion = decision.Estado.ToString(),
                Motivo = decision.Motivo,
                PospuestaHasta = decision.PospuestaHasta.HasValue ? Segundos(decision.PospuestaHasta.Value.Date) : (DateTime?)null,
                Recomendacion = new BE.ConstanciaRecomendacion
                {
                    Clave = rec.Clave,
                    Titulo = rec.Titulo?.Espanol,
                    Regla = rec.TipoRegla,
                    Prioridad = rec.Prioridad.ToString(),
                    Confianza = rec.Confianza.ToString(),
                    Diagnostico = rec.Diagnostico?.Espanol,
                    Motivo = rec.Motivo?.Espanol
                },
                OpcionElegida = Opcion(elegida),
                Alternativas = opciones.Where(o => o != null && !ReferenceEquals(o, elegida)).Select(Opcion).ToList()
            };

            if (panel != null)
            {
                c.Periodo = new BE.ConstanciaPeriodo
                {
                    Desde = Segundos(panel.Desde.Date), Hasta = Segundos(panel.Hasta.Date),
                    IdPlan = panel.IdPlan, Plan = panel.NombrePlan ?? "Todos los planes"
                };
                c.Indicadores = (panel.Kpis ?? new List<BE.KpiComercial>()).Where(k => k != null).Select(k => new BE.ConstanciaIndicador
                {
                    Clave = k.Clave, Nombre = k.Nombre?.Espanol, Unidad = k.Unidad.ToString(),
                    Valor = k.Valor, Anterior = k.ValorAnterior, Variacion = k.Variacion
                }).ToList();
                var valores = panel.Contexto?.Umbrales ?? new Dictionary<string, decimal>();
                c.ParametrosDelAnalisis = CatalogoUmbrales.Describir(valores).Select(u => new BE.ConstanciaParametro
                {
                    Clave = u.Clave, Nombre = u.Nombre?.Espanol, Valor = u.Valor
                }).ToList();
                c.Advertencias = (panel.Avisos ?? new List<BE.TextoTraducible>()).Where(a => a != null).Select(a => a.Espanol).ToList();
            }

            // Supuestos: sobre qué base se decidió (fórmulas de las opciones y valores de los parámetros).
            foreach (var o in new[] { c.OpcionElegida }.Concat(c.Alternativas).Where(o => !string.IsNullOrWhiteSpace(o?.Formula)))
                c.Supuestos.Add($"Fórmula de «{o.Descripcion}»: {o.Formula}");
            foreach (var p in c.ParametrosDelAnalisis)
                c.Supuestos.Add($"Parámetro del análisis «{p.Nombre}» = {Numero(p.Valor)}");
            return c;
        }

        private static BE.ConstanciaOpcion Opcion(BE.OpcionRecomendacion o) => o == null ? new BE.ConstanciaOpcion() : new BE.ConstanciaOpcion
        {
            Descripcion = o.Descripcion?.Espanol,
            Accion = o.Accion.ToString(),
            ImpactoEstimado = o.ImpactoEstimado,
            Formula = o.Formula?.Espanol
        };

        private static string NombreCompleto(BE.Usuario u)
        {
            if (u == null) return null;
            string nombre = $"{u.Nombre} {u.Apellido}".Trim();
            return nombre.Length > 0 ? nombre : u.Username;
        }

        // Fechas legibles a mano: sin fracción de segundo ni zona horaria (yyyy-MM-ddTHH:mm:ss).
        private static DateTime Segundos(DateTime f) =>
            new DateTime(f.Year, f.Month, f.Day, f.Hour, f.Minute, f.Second, DateTimeKind.Unspecified);

        private static string Numero(decimal v) => v.ToString("0.##########", CultureInfo.InvariantCulture);

        // ── Serialización (fábrica existente) ────────────────────────────────────

        /// <summary>XML de la constancia (lo que se guarda en la base).</summary>
        public static string Serializar(BE.ConstanciaDecision c) =>
            FabricaSerializadores.Crear<BE.ConstanciaDecision>(FabricaSerializadores.ExtensionXml).Serializar(c);

        /// <summary>Lee y valida el XML guardado en la base.</summary>
        public static BE.ConstanciaDecision Deserializar(string xml)
        {
            var c = FabricaSerializadores.Crear<BE.ConstanciaDecision>(FabricaSerializadores.ExtensionXml).Deserializar(xml);
            Validar(c);
            return c;
        }

        /// <summary>Validación pura: versión de formato, decisión, acción y recomendación presentes.</summary>
        public static void Validar(BE.ConstanciaDecision c)
        {
            if (c == null) throw new BE.AppException("err.serial.vacio", "El archivo está vacío.");
            SerializadorAbstracto.ExigirVersionFormato(c.Version, BE.ConstanciaDecision.FormatoActual);
            if (c.IdDecision <= 0 || c.Recomendacion == null || string.IsNullOrWhiteSpace(c.Recomendacion.Titulo)
                || !Enum.TryParse(c.Accion ?? "", out BE.EstadoDecision _))
                throw new BE.AppException("err.bll.constancia.invalida",
                    "El archivo no es una constancia de decisión válida: le faltan la decisión, la acción o la recomendación.");
        }

        // ── Comparación campo por campo (pura) ───────────────────────────────────

        /// <summary>Campos normalizados de la constancia (ruta → valor invariante), en orden fijo.</summary>
        public static List<KeyValuePair<string, string>> Campos(BE.ConstanciaDecision c)
        {
            var campos = new List<KeyValuePair<string, string>>();
            void Agregar(string ruta, object valor) => campos.Add(new KeyValuePair<string, string>(ruta, Normalizar(valor)));
            if (c == null) return campos;
            Agregar("Version", c.Version);
            Agregar("IdDecision", c.IdDecision);
            Agregar("Fecha", c.Fecha);
            Agregar("Username", c.Username);
            Agregar("NombreUsuario", c.NombreUsuario);
            Agregar("Accion", c.Accion);
            Agregar("Motivo", c.Motivo);
            Agregar("PospuestaHasta", c.PospuestaHasta);
            var r = c.Recomendacion ?? new BE.ConstanciaRecomendacion();
            Agregar("Recomendacion.Clave", r.Clave);
            Agregar("Recomendacion.Titulo", r.Titulo);
            Agregar("Recomendacion.Regla", r.Regla);
            Agregar("Recomendacion.Prioridad", r.Prioridad);
            Agregar("Recomendacion.Confianza", r.Confianza);
            Agregar("Recomendacion.Diagnostico", r.Diagnostico);
            Agregar("Recomendacion.Motivo", r.Motivo);
            void AgregarOpcion(string ruta, BE.ConstanciaOpcion o)
            {
                o = o ?? new BE.ConstanciaOpcion();
                Agregar(ruta + ".Descripcion", o.Descripcion);
                Agregar(ruta + ".Accion", o.Accion);
                Agregar(ruta + ".ImpactoEstimado", o.ImpactoEstimado);
                Agregar(ruta + ".Formula", o.Formula);
            }
            AgregarOpcion("OpcionElegida", c.OpcionElegida);
            var alternativas = c.Alternativas ?? new List<BE.ConstanciaOpcion>();
            Agregar("Alternativas.Cantidad", alternativas.Count);
            for (int i = 0; i < alternativas.Count; i++) AgregarOpcion($"Alternativas[{i + 1}]", alternativas[i]);
            var p = c.Periodo ?? new BE.ConstanciaPeriodo();
            Agregar("Periodo.Desde", p.Desde);
            Agregar("Periodo.Hasta", p.Hasta);
            Agregar("Periodo.IdPlan", p.IdPlan);
            Agregar("Periodo.Plan", p.Plan);
            var kpis = c.Indicadores ?? new List<BE.ConstanciaIndicador>();
            Agregar("Indicadores.Cantidad", kpis.Count);
            foreach (var k in kpis.Where(k => k != null))
            {
                string ruta = $"Indicadores[{k.Clave ?? k.Nombre}]";
                Agregar(ruta + ".Nombre", k.Nombre);
                Agregar(ruta + ".Unidad", k.Unidad);
                Agregar(ruta + ".Valor", k.Valor);
                Agregar(ruta + ".Anterior", k.Anterior);
                Agregar(ruta + ".Variacion", k.Variacion);
            }
            var parametros = c.ParametrosDelAnalisis ?? new List<BE.ConstanciaParametro>();
            Agregar("ParametrosDelAnalisis.Cantidad", parametros.Count);
            foreach (var q in parametros.Where(q => q != null))
            {
                Agregar($"ParametrosDelAnalisis[{q.Clave}].Nombre", q.Nombre);
                Agregar($"ParametrosDelAnalisis[{q.Clave}].Valor", q.Valor);
            }
            var supuestos = c.Supuestos ?? new List<string>();
            Agregar("Supuestos.Cantidad", supuestos.Count);
            for (int i = 0; i < supuestos.Count; i++) Agregar($"Supuestos[{i + 1}]", supuestos[i]);
            var advertencias = c.Advertencias ?? new List<string>();
            Agregar("Advertencias.Cantidad", advertencias.Count);
            for (int i = 0; i < advertencias.Count; i++) Agregar($"Advertencias[{i + 1}]", advertencias[i]);
            return campos;
        }

        private static string Normalizar(object v)
        {
            switch (v)
            {
                case null: return "";
                case DateTime f: return f.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
                case decimal d: return Numero(d);
                case IFormattable n: return n.ToString(null, CultureInfo.InvariantCulture);
                default: return (v.ToString() ?? "").Replace("\r\n", "\n").Trim();
            }
        }

        /// <summary>Campos que difieren entre el archivo y lo registrado: «campo: «archivo» ≠ «registrado»».</summary>
        public static List<string> Diferencias(BE.ConstanciaDecision archivo, BE.ConstanciaDecision registrada)
        {
            var a = Campos(archivo);
            var r = Campos(registrada).GroupBy(kv => kv.Key).ToDictionary(g => g.Key, g => g.First().Value);
            var vistos = new HashSet<string>();
            var dif = new List<string>();
            foreach (var kv in a)
            {
                vistos.Add(kv.Key);
                if (!r.TryGetValue(kv.Key, out var valor)) dif.Add($"{kv.Key}: «{Corto(kv.Value)}» no figura en la constancia registrada");
                else if (valor != kv.Value) dif.Add($"{kv.Key}: «{Corto(kv.Value)}» ≠ «{Corto(valor)}» registrado");
            }
            foreach (var kv in r.Where(kv => !vistos.Contains(kv.Key)))
                dif.Add($"{kv.Key}: falta en el archivo (registrado «{Corto(kv.Value)}»)");
            return dif;
        }

        private static string Corto(string s) => s == null ? "" : (s.Length <= 80 ? s : s.Substring(0, 79) + "…");

        // ── Base de datos ─────────────────────────────────────────────────────────

        /// <summary>Constancia registrada de la decisión (null si no tiene: decisión anterior a la 1.6.1).</summary>
        public BE.ConstanciaDecision Obtener(int idDecision)
        {
            Exigir();
            string xml = dal.ObtenerConstancia(idDecision);
            return string.IsNullOrWhiteSpace(xml) ? null : Deserializar(xml);
        }

        /// <summary>Guarda la constancia (la llama DecisionesComerciales al registrar la decisión).</summary>
        internal void Guardar(BE.ConstanciaDecision c)
        {
            Validar(c);
            if (!dal.GuardarConstancia(c.IdDecision, Serializar(c)))
                throw new BE.AppException("err.bll.decision.inexistente", "La decisión ya no existe.");
        }

        /// <summary>Verifica una constancia leída de un archivo contra la registrada en esta base.</summary>
        public BE.ConstanciaVerificada Verificar(BE.ConstanciaDecision archivo)
        {
            Validar(archivo);
            var v = new BE.ConstanciaVerificada { Constancia = archivo };
            if (dal.ObtenerPorId(archivo.IdDecision) == null) { v.Estado = BE.EstadoVerificacionConstancia.SinRegistro; return v; }
            string xml = dal.ObtenerConstancia(archivo.IdDecision);
            if (string.IsNullOrWhiteSpace(xml)) { v.Estado = BE.EstadoVerificacionConstancia.SinConstanciaEnSistema; return v; }
            BE.ConstanciaDecision registrada;
            try { registrada = Deserializar(xml); }
            catch (BE.AppException)
            {
                v.Estado = BE.EstadoVerificacionConstancia.Modificada;
                v.Diferencias.Add("La constancia registrada en el sistema no se pudo leer.");
                return v;
            }
            v.Diferencias = Diferencias(archivo, registrada);
            v.Estado = v.Diferencias.Count == 0 ? BE.EstadoVerificacionConstancia.Coincide : BE.EstadoVerificacionConstancia.Modificada;
            return v;
        }

        // ── Exportar / abrir desde archivo ────────────────────────────────────────

        /// <summary>constancia_decision_{IdDecision}_{yyyyMMdd_HHmmss}.xml (o .json).</summary>
        public static string NombreArchivoSugerido(BE.ConstanciaDecision c, DateTime fecha, string extension = FabricaSerializadores.ExtensionXml) =>
            GestorArchivos.Instancia.NombreArchivoSugerido(PrefijoArchivo, c?.IdDecision.ToString(CultureInfo.InvariantCulture), fecha, extension);

        /// <summary>Exporta la constancia registrada de la decisión a XML o JSON (según la extensión).</summary>
        public BE.ConstanciaDecision Exportar(string modulo, int idDecision, string ruta)
        {
            Exigir();
            if (string.IsNullOrWhiteSpace(ruta))
                throw new BE.AppException("err.bll.version.xml.sin_ruta", "Indique el archivo de destino.");
            var serializador = FabricaSerializadores.Crear<BE.ConstanciaDecision>(ruta);   // rechaza otra extensión
            var c = Obtener(idDecision)
                ?? throw new BE.AppException("err.bll.constancia.sinconstancia",
                       "La decisión #{0} no tiene constancia: es anterior a la versión 1.6.1.", idDecision);
            try { serializador.GuardarArchivo(c, ruta); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new BE.AppException("err.bll.version.xml.no_escribe",
                    "No se pudo guardar el archivo '{0}': {1}", Path.GetFileName(ruta), ex.Message);
            }
            RegistrarBitacora(modulo, $"Exportación de la constancia de la decisión comercial #{idDecision} a {serializador.Formato}: '{Path.GetFileName(ruta)}'");
            return c;
        }

        /// <summary>
        /// Abre una constancia exportada (XML o JSON): tamaño, formato, versión y contenido validados, y la
        /// verifica contra la base. Un archivo corrupto, adulterado en su estructura o que no es una
        /// constancia se rechaza con un mensaje claro; si solo cambió un valor, se informa «fue modificado».
        /// </summary>
        public BE.ConstanciaVerificada Abrir(string modulo, string ruta)
        {
            Exigir();
            if (string.IsNullOrWhiteSpace(ruta) || !File.Exists(ruta))
                throw new BE.AppException("err.serial.no_existe", "No se encontró el archivo '{0}'.", ruta ?? "");
            if (new FileInfo(ruta).Length > TamanoMaximoArchivo)
                throw new BE.AppException("err.bll.constancia.muy_grande",
                    "El archivo supera el tamaño máximo admitido ({0} MB); no parece una constancia de decisión.",
                    TamanoMaximoArchivo / (1024 * 1024));
            var serializador = FabricaSerializadores.Crear<BE.ConstanciaDecision>(ruta);
            BE.ConstanciaDecision c;
            try { c = serializador.LeerArchivo(ruta); }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                throw new BE.AppException("err.bll.version.xml.no_lee",
                    "No se pudo leer el archivo '{0}': {1}", Path.GetFileName(ruta), ex.Message);
            }
            var v = Verificar(c);
            v.FormatoArchivo = serializador.Formato;
            RegistrarBitacora(modulo, $"Apertura de la constancia de la decisión comercial #{c.IdDecision} desde {serializador.Formato} " +
                                      $"('{Path.GetFileName(ruta)}'): {v.Estado}" + (v.Diferencias.Count > 0 ? $", {v.Diferencias.Count} campo(s) distinto(s)" : ""));
            return v;
        }

        private static void Exigir() =>
            PermisosAccion.Exigir(BE.Patentes.SugerenciaPromocion, BE.Patentes.SugerenciaPromocion);

        private void RegistrarBitacora(string modulo, string actividad)
        {
            try { bitacora.Registrar(modulo, actividad, BE.Criticidad.Media); }
            catch (Exception ex) { System.Diagnostics.Trace.TraceError("[BLL.ConstanciasDecision] " + ex.Message); }
        }
    }
}
