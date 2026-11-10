using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using BLL.Inteligencia;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Servicios.Serializacion;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// A02 Serialización (1.6.1): exportar e importar con ARCHIVO REAL, en XML y en JSON (el formato lo decide
    /// la extensión, FabricaSerializadores), para los tres usos:
    ///   A) Versiones de usuario (CU09/CU10-SIS, BLL.VersionUsuario).
    ///   B) Errores serializados (CU11-SIS, BLL.ErroresSerializados).
    ///   C) Constancia de la decisión (PN03, BLL.Inteligencia.ConstanciasDecision).
    /// Complementa SerializacionTests, SerializacionJsonTests y SerializacionVersionesTests (que ya cubren DTD,
    /// tamaño, versión de formato y permisos de A/B) con: ida y vuelta por archivo con acentos y ruso en una
    /// carpeta temporal con espacios y acentos, archivo legible (raíz XML, versión JSON, fechas ISO), archivo
    /// truncado / etiqueta rota / vacío / .txt renombrado, archivo adulterado y, para C, todo lo demás
    /// (XXE, tamaño, permisos y verificación contra la base) y las constancias sembradas en el script.
    /// Sin SQL Server: fakes de DAL. Carpeta: Path.GetTempPath() + «Prueba ñ á …», borrada al terminar.
    /// </summary>
    [TestClass]
    public class SerializacionArchivosTests
    {
        private static readonly string[] Extensiones = { ".xml", ".json" };
        private string _carpeta;
        private FakeUsuarioDAL _usuarios;
        private Dictionary<int, List<BE.VersionUsuario>> _historial;

        [TestInitialize]
        public void Crear()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
            _carpeta = Path.Combine(Path.GetTempPath(), "Prueba ñ á " + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_carpeta);
            GestorArchivos.Instancia.CarpetaRaiz = Path.Combine(_carpeta, "raiz");
            _usuarios = new FakeUsuarioDAL();
            _historial = new Dictionary<int, List<BE.VersionUsuario>>();
        }

        [TestCleanup]
        public void Borrar()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
            GestorArchivos.Instancia.CarpetaRaiz = null;
            try { Directory.Delete(_carpeta, true); } catch (IOException) { }
        }

        private string Ruta(string nombre) => Path.Combine(_carpeta, nombre);

        private static BE.AppException Capturar(Action accion)
        {
            try { accion(); return null; }
            catch (BE.AppException ex) { return ex; }
        }

        private static void LoginAdministrador() { SessionManager.Logout(); LoginAdministrador0(); }
        private static void LoginAdministrador0() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = BE.Roles.Administrador, Rol = BE.Roles.Administrador });

        private static void LoginGerencia() { SessionManager.Logout(); LoginGerencia0(); }
        private static void LoginGerencia0() => SessionManager.Login(new BE.Usuario
        {
            Id = 3, Username = "gcomercial", Nombre = "Gabriela", Apellido = "Núñez", Perfil = "GerenteComercial",
            Permisos = new List<BE.Permiso> { new BE.Permiso { NombreMenu = BE.Patentes.SugerenciaPromocion } }
        });

        private static void LoginSinPatentes() { SessionManager.Logout(); LoginSinPatentes0(); }
        private static void LoginSinPatentes0() =>
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor", Permisos = new List<BE.Permiso>() });

        // ── Formato legible ──────────────────────────────────────────────────

        private static readonly Regex FechaIso = new Regex(@"\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}");

        private static void AssertLegible(string ruta, string raizXml)
        {
            string texto = File.ReadAllText(ruta, Encoding.UTF8);
            Assert.IsTrue(FechaIso.IsMatch(texto), "Las fechas tienen que estar en ISO (yyyy-MM-ddTHH:mm:ss).");
            Assert.IsFalse(texto.Contains("/Date("), "Nunca /Date(...)/.");
            if (ruta.EndsWith(".xml"))
            {
                var doc = XDocument.Load(ruta);
                Assert.AreEqual(raizXml, doc.Root.Name.LocalName);
                Assert.IsTrue(doc.Root.Attribute("version") != null || doc.Root.Element("Version") != null, "El XML lleva su versión de formato.");
            }
            else
            {
                using (var lector = JsonReaderWriterFactory.CreateJsonReader(File.ReadAllBytes(ruta), XmlDictionaryReaderQuotas.Max))
                {
                    var json = XElement.Load(lector);
                    Assert.IsNotNull(json.Element("Version"), "El JSON lleva la propiedad Version.");
                    Assert.AreEqual("1", json.Element("Version").Value);
                }
            }
        }

        // Variantes corruptas de un archivo válido: truncado, etiqueta rota, vacío y un .txt renombrado.
        private IEnumerable<(string Caso, string Ruta)> Corruptos(string valido)
        {
            string ext = Path.GetExtension(valido);
            string texto = File.ReadAllText(valido, Encoding.UTF8);
            string truncado = Ruta("truncado" + ext), roto = Ruta("roto" + ext), vacio = Ruta("vacio" + ext), txt = Ruta("renombrado" + ext);
            File.WriteAllText(truncado, texto.Substring(0, texto.Length / 2), Encoding.UTF8);
            File.WriteAllText(roto, ext == ".xml" ? texto.Replace("</", "<") : texto.Replace("\":", "\"="), Encoding.UTF8);
            File.WriteAllText(vacio, "", Encoding.UTF8);
            File.WriteAllText(txt, "Esto es un archivo de texto cualquiera, renombrado.\r\nLínea 2: ñ.", Encoding.UTF8);
            yield return ("truncado", truncado);
            yield return ("etiqueta rota", roto);
            yield return ("vacío", vacio);
            yield return (".txt renombrado", txt);
        }

        // ══ A) Versiones de usuario ══════════════════════════════════════════

        private BLL.VersionUsuario Versiones() => new BLL.VersionUsuario(_usuarios,
            id => _historial.Values.SelectMany(l => l).FirstOrDefault(v => v.Id == id),
            (id, m) => { },
            id => _historial.TryGetValue(id, out var l) ? new List<BE.VersionUsuario>(l) : new List<BE.VersionUsuario>());

        private void SembrarUsuario()
        {
            _usuarios.Usuarios.Add(new BE.Usuario { Id = 7, Username = "vendedor", Nombre = "José", Apellido = "Иванов", Email = "jose@wardrobeflow.com" });
            _historial[7] = new List<BE.VersionUsuario>
            {
                new BE.VersionUsuario { Id = 21, IdUsuario = 7, Fecha = new DateTime(2026, 9, 3, 11, 20, 5), Actor = "admin",
                    Detalle = "Email: 'jose@x.com' → 'jose@wardrobeflow.com'", UsernameSnapshot = "vendedor", NombreSnapshot = "José Ñandú",
                    ApellidoSnapshot = "Иванов", FechaNacSnapshot = new DateTime(1991, 2, 28), EmailSnapshot = "jose@wardrobeflow.com" },
                new BE.VersionUsuario { Id = 20, IdUsuario = 7, Fecha = new DateTime(2026, 8, 1, 9, 0, 0), Actor = "admin",
                    Detalle = "Alta del usuario", UsernameSnapshot = "vendedor", NombreSnapshot = "José", ApellidoSnapshot = "Пéрез",
                    FechaNacSnapshot = null, EmailSnapshot = "jose@x.com" }
            };
        }

        [TestMethod]
        public void A_Versiones_IdaYVueltaPorArchivo_ConservaTodosLosCampos_AcentosYRuso_EnXmlYJson()
        {
            LoginAdministrador();
            SembrarUsuario();
            foreach (var ext in Extensiones)
            {
                string ruta = Ruta("versiones ñ" + ext);
                Versiones().Exportar("Test", 7, ruta);
                var leido = Versiones().Importar("Test", ruta);
                var original = _historial[7];
                Assert.AreEqual(original.Count, leido.Versiones.Count, ext);
                foreach (var o in original)
                {
                    var l = leido.Versiones.Single(v => v.Id == o.Id);
                    Assert.AreEqual(o.IdUsuario, l.IdUsuario, ext);
                    Assert.AreEqual(o.Fecha, l.Fecha, ext);
                    Assert.AreEqual(o.Actor, l.Actor, ext);
                    Assert.AreEqual(o.Detalle, l.Detalle, ext);
                    Assert.AreEqual(o.UsernameSnapshot, l.UsernameSnapshot, ext);
                    Assert.AreEqual(o.NombreSnapshot, l.NombreSnapshot, ext + ": acentos y ñ");
                    Assert.AreEqual(o.ApellidoSnapshot, l.ApellidoSnapshot, ext + ": cirílico");
                    Assert.AreEqual(o.FechaNacSnapshot, l.FechaNacSnapshot, ext);
                    Assert.AreEqual(o.EmailSnapshot, l.EmailSnapshot, ext);
                }
                AssertLegible(ruta, "HistorialUsuarioWardrobeFlow");
            }
        }

        [TestMethod]
        public void A_Versiones_ArchivoCorrupto_SeRechazaConAppException_EnXmlYJson()
        {
            LoginAdministrador();
            SembrarUsuario();
            foreach (var ext in Extensiones)
            {
                string valido = Ruta("valido" + ext);
                Versiones().Exportar("Test", 7, valido);
                foreach (var (caso, ruta) in Corruptos(valido))
                    Assert.IsNotNull(Capturar(() => Versiones().Importar("Test", ruta)), $"{ext} {caso}: tenía que rechazarse con AppException.");
            }
            string texto = Ruta("versiones.txt");
            File.WriteAllText(texto, "hola");
            Assert.AreEqual("err.serial.extension", Capturar(() => Versiones().Importar("Test", texto))?.Clave);
        }

        [TestMethod]
        public void A_Versiones_ArchivoAdulterado_OtroUsuario_SeRechazaPorPertenencia()
        {
            LoginAdministrador();
            SembrarUsuario();
            foreach (var ext in Extensiones)
            {
                string ruta = Ruta("adulterado" + ext);
                Versiones().Exportar("Test", 7, ruta);
                // Se cambia el usuario dueño del historial a otro que no existe en esta base.
                string texto = File.ReadAllText(ruta, Encoding.UTF8);
                texto = ext == ".xml" ? texto.Replace("<Username>vendedor</Username>", "<Username>intruso</Username>")
                                      : Regex.Replace(texto, "\"Username\": ?\"vendedor\"", "\"Username\":\"intruso\"");
                File.WriteAllText(ruta, texto, Encoding.UTF8);
                Assert.IsNotNull(Capturar(() => Versiones().Importar("Test", ruta)), ext + ": un historial de otro usuario no se importa.");
            }
        }

        // ══ B) Errores serializados ══════════════════════════════════════════

        private static List<BE.RegistroError> Errores() => new List<BE.RegistroError>
        {
            new BE.RegistroError { Fecha = new DateTime(2026, 10, 30, 18, 4, 9), Usuario = "caja", Modulo = "Cobros: Caja",
                Tipo = "System.InvalidOperationException", Mensaje = "No se pudo cobrar: «Ошибка» ñandú", Causa = "SqlException: tiempo agotado", Equipo = "PC-Ñ" },
            new BE.RegistroError { Fecha = new DateTime(2026, 10, 29, 8, 0, 0), Usuario = "admin", Modulo = "Aplicación",
                Tipo = "System.TimeoutException", Mensaje = "Lento", Causa = null, Equipo = "PC1" }
        };

        [TestMethod]
        public void B_Errores_IdaYVueltaPorArchivo_ConservaTodosLosCampos_AcentosYRuso_EnXmlYJson()
        {
            LoginAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            foreach (var ext in Extensiones)
            {
                string ruta = Ruta("errores ñ" + ext);
                bll.Exportar("Test", Errores(), ruta);
                var leidos = bll.Importar("Test", ruta);
                foreach (var o in Errores())
                {
                    var l = leidos.Single(e => e.Fecha == o.Fecha);
                    Assert.AreEqual(o.Usuario, l.Usuario, ext);
                    Assert.AreEqual(o.Modulo, l.Modulo, ext);
                    Assert.AreEqual(o.Tipo, l.Tipo, ext);
                    Assert.AreEqual(o.Mensaje, l.Mensaje, ext + ": acentos, ñ y cirílico");
                    Assert.AreEqual(o.Causa, l.Causa, ext);
                    Assert.AreEqual(o.Equipo, l.Equipo, ext);
                }
                AssertLegible(ruta, "ErroresWardrobeFlow");
            }
        }

        [TestMethod]
        public void B_Errores_ArchivoCorrupto_SeRechazaConAppException_EnXmlYJson()
        {
            LoginAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            foreach (var ext in Extensiones)
            {
                string valido = Ruta("valido_err" + ext);
                bll.Exportar("Test", Errores(), valido);
                foreach (var (caso, ruta) in Corruptos(valido))
                    Assert.IsNotNull(Capturar(() => bll.Importar("Test", ruta)), $"{ext} {caso}: tenía que rechazarse con AppException.");
            }
        }

        [TestMethod]
        public void B_Errores_ArchivoAdulterado_VersionDeFormatoCambiada_SeRechaza()
        {
            LoginAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            foreach (var ext in Extensiones)
            {
                string ruta = Ruta("err_adulterado" + ext);
                bll.Exportar("Test", Errores(), ruta);
                string texto = File.ReadAllText(ruta, Encoding.UTF8);
                texto = ext == ".xml" ? Regex.Replace(texto, "version=\"1\"", "version=\"9\"") : Regex.Replace(texto, "\"Version\": ?1", "\"Version\":9");
                File.WriteAllText(ruta, texto, Encoding.UTF8);
                Assert.AreEqual("err.serial.version_nueva", Capturar(() => bll.Importar("Test", ruta))?.Clave, ext);
            }
        }

        [TestMethod]
        public void B_Errores_SinLaPatenteDeAuditoria_NoSeExportaNiSeImporta()
        {
            LoginAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            string ruta = Ruta("err_permiso.xml");
            bll.Exportar("Test", Errores(), ruta);
            LoginSinPatentes();
            Assert.AreEqual("err.bll.sin_permiso", Capturar(() => bll.Exportar("Test", Errores(), Ruta("otro.xml")))?.Clave);
            Assert.AreEqual("err.bll.sin_permiso", Capturar(() => bll.Importar("Test", ruta))?.Clave);
            Assert.IsFalse(File.Exists(Ruta("otro.xml")));
        }

        // ══ C) Constancia de la decisión ═════════════════════════════════════

        private FakeDecisionComercialDAL _decisiones;

        private int DecidirConConstancia()
        {
            LoginGerencia();
            _decisiones = new FakeDecisionComercialDAL();
            var d = new DecisionesComerciales(_decisiones, () => new DateTime(2026, 11, 1, 10, 15, 42))
                .Aceptar("Test", ConstanciaDecisionTests.Rec(), 0, ConstanciaDecisionTests.Panel());
            Assert.IsTrue(d.TieneConstancia);
            return d.IdDecision;
        }

        [TestMethod]
        public void C_Constancia_IdaYVueltaPorArchivo_IgualCampoPorCampo_YCoincideConLaBase_EnXmlYJson()
        {
            int id = DecidirConConstancia();
            var bll = new ConstanciasDecision(_decisiones);
            var registrada = bll.Obtener(id);
            foreach (var ext in Extensiones)
            {
                string ruta = Ruta(ConstanciasDecision.NombreArchivoSugerido(registrada, new DateTime(2026, 11, 1, 10, 20, 0), ext));
                StringAssert.StartsWith(Path.GetFileName(ruta), "constancia_decision_" + id + "_20261101_102000");
                bll.Exportar("Test", id, ruta);
                var abierta = bll.Abrir("Test", ruta);
                Assert.AreEqual(BE.EstadoVerificacionConstancia.Coincide, abierta.Estado, ext + ": " + string.Join(" | ", abierta.Diferencias));
                Assert.AreEqual(0, ConstanciasDecision.Diferencias(abierta.Constancia, registrada).Count, ext);
                Assert.AreEqual("Gabriela Núñez", abierta.Constancia.NombreUsuario, ext + ": acentos");
                StringAssert.Contains(abierta.Constancia.Recomendacion.Diagnostico, "Удержание клиентов", ext + ": cirílico");
                Assert.AreEqual(ext == ".xml" ? "XML" : "JSON", abierta.FormatoArchivo);
                AssertLegible(ruta, "ConstanciaDecisionWardrobeFlow");
                string texto = File.ReadAllText(ruta, Encoding.UTF8);
                StringAssert.Contains(texto, "2026-11-01T10:15:42", ext + ": fecha legible, sin fracción ni zona.");
                Assert.IsTrue(texto.IndexOf("Version", StringComparison.OrdinalIgnoreCase) < texto.IndexOf("IdDecision", StringComparison.Ordinal), "Version va primero.");
            }
            Assert.IsTrue(ConfiguracionTests.Bitacora.Registros.Any(r => r.Actividad.StartsWith("Exportación de la constancia de la decisión comercial #" + id + " a XML")));
            Assert.IsTrue(ConfiguracionTests.Bitacora.Registros.Any(r => r.Actividad.StartsWith("Exportación de la constancia de la decisión comercial #" + id + " a JSON")));
        }

        [TestMethod]
        public void C_Constancia_Adulterada_NoCoincide_YDiceQueCampos_EnXmlYJson()
        {
            int id = DecidirConConstancia();
            var bll = new ConstanciasDecision(_decisiones);
            foreach (var ext in Extensiones)
            {
                string ruta = Ruta("constancia_editada" + ext);
                bll.Exportar("Test", id, ruta);
                // Demo: se edita un número en el archivo (el impacto de la opción elegida).
                string texto = File.ReadAllText(ruta, Encoding.UTF8);
                string editado = texto.Replace("12345.67", "99999.99");
                Assert.AreNotEqual(texto, editado, ext + ": el número tenía que estar en el archivo.");
                File.WriteAllText(ruta, editado, Encoding.UTF8);

                var v = bll.Abrir("Test", ruta);
                Assert.AreEqual(BE.EstadoVerificacionConstancia.Modificada, v.Estado, ext);
                Assert.AreEqual(1, v.Diferencias.Count, ext + ": " + string.Join(" | ", v.Diferencias));
                StringAssert.StartsWith(v.Diferencias[0], "OpcionElegida.ImpactoEstimado");
                StringAssert.Contains(v.Diferencias[0], "99999.99");
            }
        }

        [TestMethod]
        public void C_Constancia_SinRegistroEnEsteSistema_O_SinConstanciaRegistrada()
        {
            int id = DecidirConConstancia();
            var bll = new ConstanciasDecision(_decisiones);
            string ruta = Ruta("constancia_otra_base.xml");
            bll.Exportar("Test", id, ruta);

            // Otra base (otra PC): la decisión no existe.
            var otraBase = new ConstanciasDecision(new FakeDecisionComercialDAL());
            Assert.AreEqual(BE.EstadoVerificacionConstancia.SinRegistro, otraBase.Abrir("Test", ruta).Estado);

            // La decisión existe pero no tiene constancia (anterior a la 1.6.1).
            _decisiones.Constancias.Remove(id);
            Assert.AreEqual(BE.EstadoVerificacionConstancia.SinConstanciaEnSistema, bll.Abrir("Test", ruta).Estado);
        }

        [TestMethod]
        public void C_Constancia_ArchivoCorrupto_NoEsConstancia_OSinVersion_SeRechaza()
        {
            int id = DecidirConConstancia();
            var bll = new ConstanciasDecision(_decisiones);
            foreach (var ext in Extensiones)
            {
                string valido = Ruta("c_valido" + ext);
                bll.Exportar("Test", id, valido);
                foreach (var (caso, ruta) in Corruptos(valido))
                    Assert.IsNotNull(Capturar(() => bll.Abrir("Test", ruta)), $"{ext} {caso}: tenía que rechazarse con AppException.");
            }
            // Otro archivo de WardrobeFlow (errores) no es una constancia.
            LoginAdministrador();
            string errores = Ruta("errores_no_constancia.xml");
            new BLL.ErroresSerializados(new FakeRegistroBitacora()).Exportar("Test", Errores(), errores);
            LoginGerencia();
            Assert.IsNotNull(Capturar(() => bll.Abrir("Test", errores)), "Un archivo de errores no es una constancia.");
            // Sin versión de formato.
            string sinVersion = Ruta("sin_version.xml");
            File.WriteAllText(sinVersion, File.ReadAllText(Ruta("c_valido.xml")).Replace(" version=\"1\"", ""), Encoding.UTF8);
            Assert.AreEqual("err.serial.sin_version", Capturar(() => bll.Abrir("Test", sinVersion))?.Clave);
            // Extensión no admitida.
            string txt = Ruta("constancia.txt");
            File.WriteAllText(txt, "x");
            Assert.AreEqual("err.serial.extension", Capturar(() => bll.Abrir("Test", txt))?.Clave);
        }

        [TestMethod]
        public void C_Constancia_XmlConDtdOEntidadExterna_SeRechaza_YUnArchivoGigante_Tambien()
        {
            int id = DecidirConConstancia();
            var bll = new ConstanciasDecision(_decisiones);
            string valido = Ruta("xxe_base.xml");
            bll.Exportar("Test", id, valido);
            string xml = File.ReadAllText(valido, Encoding.UTF8);
            string secreto = Ruta("secreto.txt");
            File.WriteAllText(secreto, "clave");
            string xxe = Ruta("xxe.xml");
            File.WriteAllText(xxe, xml.Replace("<ConstanciaDecisionWardrobeFlow",
                "<!DOCTYPE ConstanciaDecisionWardrobeFlow [ <!ENTITY x SYSTEM \"" + new Uri(secreto).AbsoluteUri + "\"> ]>\n<ConstanciaDecisionWardrobeFlow")
                .Replace("<Motivo />", "<Motivo>&x;</Motivo>"), Encoding.UTF8);
            var ex = Capturar(() => bll.Abrir("Test", xxe));
            Assert.AreEqual("err.serial.formato", ex?.Clave, "DTD prohibida (XXE).");

            string gigante = Ruta("gigante.json");
            using (var f = new FileStream(gigante, FileMode.Create)) f.SetLength(ConstanciasDecision.TamanoMaximoArchivo + 1);
            Assert.AreEqual("err.bll.constancia.muy_grande", Capturar(() => bll.Abrir("Test", gigante))?.Clave);
        }

        [TestMethod]
        public void C_Constancia_SinLaPatenteDeGerencia_NoSeExportaNiSeAbre()
        {
            int id = DecidirConConstancia();
            var bll = new ConstanciasDecision(_decisiones);
            string ruta = Ruta("permiso.xml");
            bll.Exportar("Test", id, ruta);
            LoginSinPatentes();
            Assert.AreEqual("err.bll.sin_permiso", Capturar(() => bll.Exportar("Test", id, Ruta("otra.xml")))?.Clave);
            Assert.AreEqual("err.bll.sin_permiso", Capturar(() => bll.Abrir("Test", ruta))?.Clave);
            Assert.IsFalse(File.Exists(Ruta("otra.xml")));
        }

        // ── Datos de demostración (tarea 10): las constancias sembradas en el script son válidas ──

        private static string ScriptInstalacion()
        {
            for (var d = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory); d != null; d = d.Parent)
            {
                string ruta = Path.Combine(d.FullName, "BD", "00_Instalacion_Completa.sql");
                if (File.Exists(ruta)) return ruta;
            }
            return null;
        }

        [TestMethod]
        public void Demo_LasConstanciasSembradasEnElScript_SeLeen_SeExportanYSeAbren()
        {
            string script = ScriptInstalacion();
            if (script == null) Assert.Inconclusive("No se encontró BD/00_Instalacion_Completa.sql junto a las pruebas.");
            string sql = File.ReadAllText(script, Encoding.UTF8);
            int ini = sql.IndexOf("-- WardrobeFlow: 21z17.", StringComparison.Ordinal);
            Assert.IsTrue(ini >= 0, "Falta la sección 21z17 (datos de demostración de la serialización).");
            int fin = sql.IndexOf("-- WardrobeFlow: 22.", ini, StringComparison.Ordinal);
            string seccion = sql.Substring(ini, fin - ini);
            var literales = Regex.Matches(seccion, @"N'(<\?xml.*?</ConstanciaDecisionWardrobeFlow>)'", RegexOptions.Singleline)
                                 .Cast<Match>().Select(m => m.Groups[1].Value.Replace("''", "'")).ToList();
            Assert.IsTrue(literales.Count >= 2, "Se esperaban constancias sembradas en la sección 21z17; hay " + literales.Count);

            LoginGerencia();
            foreach (var xml in literales)
            {
                var c = ConstanciasDecision.Deserializar(xml);   // valida versión y contenido
                var dal = new FakeDecisionComercialDAL();
                int id = c.IdDecision;   // 999999: el script le pone el Id real de la decisión
                dal.Filas.Add(new BE.DecisionComercial { IdDecision = id, Fecha = c.Fecha, Titulo = c.Recomendacion.Titulo, Estado = BE.EstadoDecision.Pendiente });
                dal.GuardarConstancia(id, xml);
                var bll = new ConstanciasDecision(dal);
                foreach (var ext in Extensiones)
                {
                    string ruta = Ruta("demo_" + id + ext);
                    bll.Exportar("Test", id, ruta);
                    Assert.AreEqual(BE.EstadoVerificacionConstancia.Coincide, bll.Abrir("Test", ruta).Estado, $"Constancia sembrada #{id} {ext}");
                }
            }
        }
    }
}
