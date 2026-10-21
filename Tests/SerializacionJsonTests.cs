using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Servicios.Serializacion;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// A02 Serialización — estructura de serializadores (SerializadorAbstracto → SerializadorXml /
    /// SerializadorJson, FabricaSerializadores, GestorArchivos) y exportación/importación en JSON de
    /// las versiones de usuario y de los errores, con las mismas validaciones que en XML. También fija
    /// que el XML siga saliendo igual que antes (los archivos ya exportados se siguen importando).
    /// Sin base de datos: FakeUsuarioDAL + Caretaker e historial en memoria, raíz de archivos temporal.
    /// </summary>
    [TestClass]
    public class SerializacionJsonTests
    {
        private static readonly SerializadorJson<BE.HistorialUsuarioXml> JsonHistorial = new SerializadorJson<BE.HistorialUsuarioXml>();
        private static readonly SerializadorXml<BE.HistorialUsuarioXml>  XmlHistorial  = new SerializadorXml<BE.HistorialUsuarioXml>();
        private static readonly SerializadorJson<BE.LibroErrores>        JsonErrores   = new SerializadorJson<BE.LibroErrores>();
        private static readonly SerializadorXml<BE.LibroErrores>         XmlErrores    = new SerializadorXml<BE.LibroErrores>();

        private FakeUsuarioDAL _dal;
        private Dictionary<int, List<BE.VersionUsuario>> _historial;
        private List<(int IdUsuario, BE.VersionUsuario Version)> _guardadas;
        private string _carpeta;

        [TestInitialize]
        public void Setup()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
            _dal       = new FakeUsuarioDAL();
            _historial = new Dictionary<int, List<BE.VersionUsuario>>();
            _guardadas = new List<(int, BE.VersionUsuario)>();
            _carpeta   = Path.Combine(Path.GetTempPath(), "wf_json_" + Guid.NewGuid().ToString("N"));
            GestorArchivos.Instancia.CarpetaRaiz = Path.Combine(_carpeta, "raiz");
        }

        [TestCleanup]
        public void Cleanup()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
            GestorArchivos.Instancia.CarpetaRaiz = null;
            try { Directory.Delete(_carpeta, true); } catch { }
        }

        private BLL.VersionUsuario Crear() => new BLL.VersionUsuario(_dal,
            id => _historial.Values.SelectMany(l => l).FirstOrDefault(v => v.Id == id),
            (id, m) => _guardadas.Add((id, (BE.VersionUsuario)m)),
            id => _historial.TryGetValue(id, out var l) ? new List<BE.VersionUsuario>(l) : new List<BE.VersionUsuario>());

        private static void LoginComo(string perfil)
            => SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = perfil, Rol = perfil });

        private static BE.AppException Capturar(Action accion)
        {
            try { accion(); return null; }
            catch (BE.AppException ex) { return ex; }
        }

        private string Ruta(string nombre)
        {
            string ruta = Path.Combine(_carpeta, nombre);
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            return ruta;
        }

        private static BE.VersionUsuario Version(int id, string username, string email, DateTime fecha) => new BE.VersionUsuario
        {
            Id = id, IdUsuario = 5, Fecha = fecha, Actor = "admin", Detalle = "Email: 'a' → 'b'",
            UsernameSnapshot = username, NombreSnapshot = "Ana", ApellidoSnapshot = "Pérez",
            FechaNacSnapshot = new DateTime(1990, 3, 15), EmailSnapshot = email,
            // Estado de seguridad que NUNCA debe salir en el archivo.
            ClaveSnapshot = "HASH-SECRETO-PBKDF2", EstadoSnapshot = false, IntentosSnapshot = 3
        };

        // Usuario 5 hoy es "ana.nueva"; su historial tiene la versión 10 ("ana") y la 11 (actual).
        private void Sembrar()
        {
            _dal.Usuarios.Add(new BE.Usuario { Id = 5, Username = "ana.nueva", Nombre = "Ana", Apellido = "Pérez", Email = "nueva@x.com" });
            _historial[5] = new List<BE.VersionUsuario>
            {
                Version(11, "ana.nueva", "nueva@x.com", new DateTime(2026, 10, 2, 9, 0, 0)),
                Version(10, "ana",       "vieja@x.com", new DateTime(2026, 10, 1, 8, 30, 15))
            };
        }

        private static BE.HistorialUsuarioXml ArchivoValido() => new BE.HistorialUsuarioXml
        {
            Version = BE.HistorialUsuarioXml.FormatoActual, Equipo = "PC-UAI", ExportadoPor = "admin",
            FechaExportacion = new DateTime(2026, 10, 3, 10, 0, 0), IdUsuario = 5, Username = "ana.nueva",
            Versiones = new List<BE.VersionUsuario> { Version(10, "ana", "vieja@x.com", new DateTime(2026, 10, 1, 8, 30, 15)) }
        };

        private static BE.LibroErrores LibroDePrueba() => new BE.LibroErrores
        {
            Errores = new List<BE.RegistroError>
            {
                new BE.RegistroError
                {
                    Fecha = new DateTime(2026, 10, 9, 14, 30, 5), Usuario = "caja", Modulo = "ContratacionesPendientesForm",
                    Tipo = "System.InvalidOperationException", Mensaje = "Algo <falló> & \"sigue\" en C:\\x/y", Causa = null, Equipo = "PC1"
                },
                new BE.RegistroError
                {
                    Fecha = new DateTime(2026, 10, 10, 9, 0, 0), Usuario = "admin", Modulo = "Aplicación",
                    Tipo = "System.TimeoutException", Mensaje = "lento", Causa = "SqlException: timeout", Equipo = "PC2"
                }
            }
        };

        // ── JSON: ida y vuelta ──────────────────────────────────────────────────

        [TestMethod]
        public void Json_HistorialUsuario_IdaYVuelta_ConservaTodosLosCampos_YExcluyeLosSecretos()
        {
            var original = ArchivoValido();
            original.FormatoArchivo = "JSON";   // dato de sesión: no se serializa
            original.Versiones[0].FechaNacSnapshot = null;   // también un nulo
            var conFraccion = new DateTime(2026, 10, 2, 11, 5, 59, DateTimeKind.Local).AddTicks(1234567);
            original.Versiones.Add(Version(12, "ana.x", "x@x.com", conFraccion));

            string json = JsonHistorial.Serializar(original);

            foreach (var secreto in new[] { "ClaveSnapshot", "HASH-SECRETO", "EstadoSnapshot", "IntentosSnapshot", "k__BackingField", "FormatoArchivo" })
                Assert.IsFalse(json.Contains(secreto), $"El JSON no debe contener '{secreto}'.");
            StringAssert.Contains(json, "\"Version\": 1");
            StringAssert.Contains(json, "\"UsernameSnapshot\": \"ana\"");
            StringAssert.Contains(json, "\"Fecha\": \"2026-10-01T08:30:15\"", "Fechas ISO 8601 legibles.");
            StringAssert.Contains(json, "\"FechaNacSnapshot\": null");
            StringAssert.Contains(json, "\"FechaNacSnapshot\": \"1990-03-15T00:00:00\"");
            StringAssert.Contains(json, "\n  ", "Indentado.");

            var leido = JsonHistorial.Deserializar(json);

            Assert.AreEqual(1, leido.Version);
            Assert.AreEqual("PC-UAI", leido.Equipo);
            Assert.AreEqual("admin", leido.ExportadoPor);
            Assert.AreEqual(new DateTime(2026, 10, 3, 10, 0, 0), leido.FechaExportacion);
            Assert.AreEqual(5, leido.IdUsuario);
            Assert.AreEqual("ana.nueva", leido.Username);
            Assert.IsNull(leido.FormatoArchivo);
            Assert.AreEqual(2, leido.Versiones.Count);

            var v = leido.Versiones[0];
            Assert.AreEqual(10, v.Id);
            Assert.AreEqual(5, v.IdUsuario);
            Assert.AreEqual(new DateTime(2026, 10, 1, 8, 30, 15), v.Fecha);
            Assert.AreEqual("admin", v.Actor);
            Assert.AreEqual("Email: 'a' → 'b'", v.Detalle);
            Assert.AreEqual("ana", v.UsernameSnapshot);
            Assert.AreEqual("Ana", v.NombreSnapshot);
            Assert.AreEqual("Pérez", v.ApellidoSnapshot);
            Assert.IsNull(v.FechaNacSnapshot);
            Assert.AreEqual("vieja@x.com", v.EmailSnapshot);
            Assert.IsNull(v.ClaveSnapshot, "La clave no viaja.");
            Assert.IsFalse(v.EstadoSnapshot);
            Assert.AreEqual(0, v.IntentosSnapshot);

            var w = leido.Versiones[1];
            Assert.AreEqual(conFraccion, w.Fecha, "La fecha con fracción y zona hace el viaje exacta.");
            Assert.AreEqual(new DateTime(1990, 3, 15), w.FechaNacSnapshot);
        }

        [TestMethod]
        public void Json_SecretosAgregadosAMano_NoSeLeen()
        {
            string json = JsonHistorial.Serializar(ArchivoValido()).Replace(
                "\"UsernameSnapshot\"", "\"ClaveSnapshot\": \"X\", \"EstadoSnapshot\": true, \"IntentosSnapshot\": 9, \"UsernameSnapshot\"");

            var v = JsonHistorial.Deserializar(json).Versiones[0];

            Assert.IsNull(v.ClaveSnapshot);
            Assert.IsFalse(v.EstadoSnapshot);
            Assert.AreEqual(0, v.IntentosSnapshot);
        }

        [TestMethod]
        public void Json_LibroErrores_IdaYVuelta_ConservaTodosLosCampos()
        {
            string json = JsonErrores.Serializar(LibroDePrueba());

            Assert.IsFalse(json.Contains("k__BackingField"));
            StringAssert.Contains(json, "\"Errores\"");
            StringAssert.Contains(json, "\"Fecha\": \"2026-10-09T14:30:05\"");

            var leido = JsonErrores.Deserializar(json);

            Assert.AreEqual(1, leido.Version);
            Assert.AreEqual(2, leido.Errores.Count);
            var e = leido.Errores[0];
            Assert.AreEqual(new DateTime(2026, 10, 9, 14, 30, 5), e.Fecha);
            Assert.AreEqual("caja", e.Usuario);
            Assert.AreEqual("ContratacionesPendientesForm", e.Modulo);
            Assert.AreEqual("System.InvalidOperationException", e.Tipo);
            Assert.AreEqual("Algo <falló> & \"sigue\" en C:\\x/y", e.Mensaje);
            Assert.IsNull(e.Causa);
            Assert.AreEqual("PC1", e.Equipo);
            Assert.AreEqual("SqlException: timeout", leido.Errores[1].Causa);
        }

        [TestMethod]
        public void GuardarArchivo_AmbosFormatos_Utf8SinBom_SinTemporal_YReemplaza()
        {
            string json = Ruta(Path.Combine("sub", "v.json"));   // la carpeta "sub" se crea al guardar
            string xml  = Ruta("v.xml");
            JsonHistorial.GuardarArchivo(ArchivoValido(), json);
            XmlHistorial.GuardarArchivo(ArchivoValido(), xml);

            var bytesJson = File.ReadAllBytes(json);
            var bytesXml  = File.ReadAllBytes(xml);
            Assert.AreEqual((byte)'{', bytesJson[0], "JSON sin BOM.");
            Assert.AreEqual((byte)'<', bytesXml[0], "XML sin BOM.");
            StringAssert.StartsWith(File.ReadAllText(xml), "<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            StringAssert.Contains(Encoding.UTF8.GetString(bytesJson), "Pérez");

            var otro = ArchivoValido();
            otro.Equipo = "PC-NUEVA";
            JsonHistorial.GuardarArchivo(otro, json);   // ya existe: se reemplaza
            Assert.AreEqual("PC-NUEVA", JsonHistorial.LeerArchivo(json).Equipo);
            Assert.AreEqual(0, Directory.GetFiles(_carpeta, "*.tmp", SearchOption.AllDirectories).Length);
        }

        [TestMethod]
        public void LeerArchivo_VacioONull_EnAmbosFormatos_ErrSerialVacio()
        {
            foreach (var nombre in new[] { "vacio.xml", "vacio.json" })
            {
                string ruta = Ruta(nombre);
                File.WriteAllText(ruta, "  \r\n ");
                Assert.AreEqual("err.serial.vacio",
                    Capturar(() => FabricaSerializadores.Crear<BE.LibroErrores>(ruta).LeerArchivo(ruta))?.Clave, nombre);
            }
            Assert.AreEqual("err.serial.vacio", Capturar(() => JsonErrores.Deserializar("null"))?.Clave);
            Assert.AreEqual("err.serial.no_existe", Capturar(() => JsonErrores.LeerArchivo(Ruta("no.json")))?.Clave);
        }

        [TestMethod]
        public void Json_TextoQueSuperaLaCuota_SeRechazaComoFormatoInvalido()
        {
            var archivo = ArchivoValido();
            archivo.Versiones[0].Detalle = new string('x', 1024 * 1024 + 1);
            string json = JsonHistorial.Serializar(archivo);

            var ex = Capturar(() => JsonHistorial.Deserializar(json));

            Assert.AreEqual("err.serial.formato", ex?.Clave);
            Assert.AreEqual("JSON", ex.Args[1]);
        }

        // ── XML: el formato no cambió ───────────────────────────────────────────

        // Cómo serializaba la versión anterior (SerializadorXml estático): XmlSerializer sobre un
        // StringWriter que declara UTF-8, indentado.
        private static string SerializarComoAntes<T>(T objeto)
        {
            var opciones = new XmlWriterSettings { Indent = true, Encoding = new UTF8Encoding(false) };
            using (var texto = new StringWriterUtf8())
            using (var escritor = XmlWriter.Create(texto, opciones))
            {
                new XmlSerializer(typeof(T)).Serialize(escritor, objeto);
                escritor.Flush();
                return texto.ToString();
            }
        }

        private sealed class StringWriterUtf8 : StringWriter
        {
            public override Encoding Encoding => new UTF8Encoding(false);
        }

        [TestMethod]
        public void Xml_SalidaIdenticaALaDeLaVersionAnterior()
        {
            var historial = ArchivoValido();
            historial.Versiones.Add(Version(12, "ana.x", "x@x.com", new DateTime(2026, 10, 2, 11, 5, 59)));
            historial.Versiones[1].FechaNacSnapshot = null;

            Assert.AreEqual(SerializarComoAntes(historial), XmlHistorial.Serializar(historial));
            Assert.AreEqual(SerializarComoAntes(LibroDePrueba()), XmlErrores.Serializar(LibroDePrueba()));
        }

        [TestMethod]
        public void Xml_ArchivoExportadoConElFormatoAnterior_SeSigueImportando()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string ruta = Ruta("versiones_ana.nueva_20261003_100000.xml");
            File.WriteAllText(ruta,
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                "<HistorialUsuarioWardrobeFlow xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" version=\"1\">\r\n" +
                "  <Equipo>PC-UAI</Equipo>\r\n" +
                "  <FechaExportacion>2026-10-03T10:00:00</FechaExportacion>\r\n" +
                "  <ExportadoPor>admin</ExportadoPor>\r\n" +
                "  <IdUsuario>5</IdUsuario>\r\n" +
                "  <Username>ana.nueva</Username>\r\n" +
                "  <Versiones>\r\n" +
                "    <Version id=\"10\">\r\n" +
                "      <IdUsuario>5</IdUsuario>\r\n" +
                "      <Fecha>2026-10-01T08:30:15</Fecha>\r\n" +
                "      <Actor>admin</Actor>\r\n" +
                "      <Detalle>Email: 'a' → 'b'</Detalle>\r\n" +
                "      <UsernameSnapshot>ana</UsernameSnapshot>\r\n" +
                "      <NombreSnapshot>Ana</NombreSnapshot>\r\n" +
                "      <ApellidoSnapshot>Pérez</ApellidoSnapshot>\r\n" +
                "      <FechaNacSnapshot>1990-03-15T00:00:00</FechaNacSnapshot>\r\n" +
                "      <EmailSnapshot>vieja@x.com</EmailSnapshot>\r\n" +
                "    </Version>\r\n" +
                "    <Version id=\"11\">\r\n" +
                "      <IdUsuario>5</IdUsuario>\r\n" +
                "      <Fecha>2026-10-02T09:00:00</Fecha>\r\n" +
                "      <Actor>admin</Actor>\r\n" +
                "      <UsernameSnapshot>ana.nueva</UsernameSnapshot>\r\n" +
                "      <FechaNacSnapshot xsi:nil=\"true\" />\r\n" +
                "      <EmailSnapshot>nueva@x.com</EmailSnapshot>\r\n" +
                "    </Version>\r\n" +
                "  </Versiones>\r\n" +
                "</HistorialUsuarioWardrobeFlow>", new UTF8Encoding(false));

            var importado = Crear().Importar("Test", ruta);

            Assert.AreEqual("XML", importado.FormatoArchivo);
            Assert.AreEqual(2, importado.Versiones.Count);
            var v = importado.Versiones.Single(x => x.Id == 10);
            Assert.AreEqual("ana", v.UsernameSnapshot);
            Assert.AreEqual("Pérez", v.ApellidoSnapshot);
            Assert.AreEqual(new DateTime(1990, 3, 15), v.FechaNacSnapshot);
            Assert.IsNull(importado.Versiones.Single(x => x.Id == 11).FechaNacSnapshot);
        }

        // ── BLL: versiones en JSON ──────────────────────────────────────────────

        [TestMethod]
        public void Json_ExportarEImportarVersiones_PorBLL_ViajeCompleto_YRestaura()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string nombre = "versiones_" + Guid.NewGuid().ToString("N") + ".json";
            string ruta = Path.Combine(_carpeta, "sub", nombre);   // carpeta que no existe

            var exportado = Crear().Exportar("Test", 5, ruta);
            var importado = Crear().Importar("Test", ruta);

            Assert.AreEqual("JSON", exportado.FormatoArchivo);
            Assert.AreEqual("JSON", importado.FormatoArchivo);
            string contenido = File.ReadAllText(ruta);
            StringAssert.StartsWith(contenido, "{");
            Assert.IsFalse(contenido.Contains("HASH-SECRETO"));
            Assert.AreEqual(Environment.MachineName, importado.Equipo);
            CollectionAssert.AreEquivalent(new[] { 10, 11 }, importado.Versiones.Select(v => v.Id).ToArray());
            Assert.AreEqual("vieja@x.com", importado.Versiones.Single(v => v.Id == 10).EmailSnapshot);
            Assert.AreEqual(0, _guardadas.Count, "Importar no escribe en HistorialUsuario.");

            var bitacora = ConfiguracionTests.Bitacora.Registros.Where(r => r.Actividad.Contains(nombre)).Select(r => r.Actividad).ToList();
            Assert.IsTrue(bitacora.Any(a => a.StartsWith("Exportación de versiones a JSON")), "La bitácora indica el formato.");
            Assert.IsTrue(bitacora.Any(a => a.StartsWith("Importación de versiones desde JSON")));

            Crear().RestaurarVersionImportada("Test", importado, 10);

            Assert.AreEqual("ana", _dal.ObtenerPorId(5).Username);
            Assert.AreEqual(1, _guardadas.Count);
            StringAssert.StartsWith(_guardadas[0].Version.Detalle, "Restauración a versión importada ID 10 (archivo JSON del");
        }

        [TestMethod]
        public void Json_ImportarVersiones_VersionDeFormato_Nueva_O_Faltante_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string json = JsonHistorial.Serializar(ArchivoValido());
            StringAssert.Contains(json, "\"Version\": 1,");

            string nuevo = Ruta("nuevo.json");
            File.WriteAllText(nuevo, json.Replace("\"Version\": 1,", "\"Version\": 2,"));
            var ex = Capturar(() => Crear().Importar("Test", nuevo));
            Assert.AreEqual("err.serial.version_nueva", ex?.Clave);
            CollectionAssert.AreEqual(new object[] { 2, 1 }, ex.Args);

            string sinVersion = Ruta("sinversion.json");
            File.WriteAllText(sinVersion, json.Replace("\"Version\": 1,", ""));
            Assert.AreEqual("err.serial.sin_version", Capturar(() => Crear().Importar("Test", sinVersion))?.Clave);
        }

        [TestMethod]
        public void Json_ImportarVersiones_Malformado_Ajeno_Vacio_Gigante_O_Inconsistente_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();

            string corrupto = Ruta("corrupto.json");
            File.WriteAllText(corrupto, "{ \"Version\": 1, \"Versiones\": [ {");
            var ex = Capturar(() => Crear().Importar("Test", corrupto));
            Assert.AreEqual("err.serial.formato", ex?.Clave);
            Assert.AreEqual("JSON", ex.Args[1]);

            string xmlDisfrazado = Ruta("xml.json");
            File.WriteAllText(xmlDisfrazado, XmlHistorial.Serializar(ArchivoValido()));
            Assert.AreEqual("err.serial.formato", Capturar(() => Crear().Importar("Test", xmlDisfrazado))?.Clave);

            string fechaInvalida = Ruta("fecha.json");
            File.WriteAllText(fechaInvalida, JsonHistorial.Serializar(ArchivoValido()).Replace("2026-10-03T10:00:00", "ayer"));
            Assert.AreEqual("err.serial.formato", Capturar(() => Crear().Importar("Test", fechaInvalida))?.Clave);

            string errores = Ruta("errores.json");   // un JSON válido, pero de errores
            JsonErrores.GuardarArchivo(LibroDePrueba(), errores);
            Assert.AreEqual("err.serial.formato", Capturar(() => Crear().Importar("Test", errores))?.Clave);

            string vacio = Ruta("vacio.json");
            File.WriteAllText(vacio, "");
            Assert.AreEqual("err.serial.vacio", Capturar(() => Crear().Importar("Test", vacio))?.Clave);

            string gigante = Ruta("gigante.json");
            using (var fs = new FileStream(gigante, FileMode.Create)) fs.SetLength(BLL.VersionUsuario.TamanoMaximoArchivo + 1);
            Assert.AreEqual("err.bll.version.xml.muy_grande", Capturar(() => Crear().Importar("Test", gigante))?.Clave);

            var deOtro = ArchivoValido();
            deOtro.Versiones[0].IdUsuario = 6;
            string inconsistente = Ruta("inconsistente.json");
            JsonHistorial.GuardarArchivo(deOtro, inconsistente);
            Assert.AreEqual("err.bll.version.xml.inconsistente", Capturar(() => Crear().Importar("Test", inconsistente))?.Clave);
        }

        [TestMethod]
        public void Json_ImportarVersiones_UsuarioInexistente_U_OtraBase_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            string ruta = Ruta("v.json");
            JsonHistorial.GuardarArchivo(ArchivoValido(), ruta);

            Assert.AreEqual("err.bll.version.xml.usuario_inexistente", Capturar(() => Crear().Importar("Test", ruta))?.Clave);

            _dal.Usuarios.Add(new BE.Usuario { Id = 5, Username = "carlos" });   // otra base: el ID 5 es otro
            var ex = Capturar(() => Crear().Importar("Test", ruta));
            Assert.AreEqual("err.bll.version.xml.otro_usuario", ex?.Clave);
            CollectionAssert.AreEqual(new object[] { "ana.nueva", 5, "carlos" }, ex.Args);
        }

        [TestMethod]
        public void Versiones_ExtensionNoAdmitida_O_SinPermiso_Rechaza_SinEscribir()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string txt = Ruta("v.txt");
            var ex = Capturar(() => Crear().Exportar("Test", 5, txt));
            Assert.AreEqual("err.serial.extension", ex?.Clave);
            Assert.AreEqual("v.txt", ex.Args[0]);
            Assert.IsFalse(File.Exists(txt));

            File.WriteAllText(txt, JsonHistorial.Serializar(ArchivoValido()));
            Assert.AreEqual("err.serial.extension", Capturar(() => Crear().Importar("Test", txt))?.Clave);

            SessionManager.Logout();
            LoginComo("Vendedor");
            string json = Ruta("x.json");
            Assert.AreEqual("err.bll.version.xml.sin_permiso", Capturar(() => Crear().Exportar("Test", 5, json))?.Clave);
            Assert.IsFalse(File.Exists(json));
        }

        // ── BLL: errores en JSON ────────────────────────────────────────────────

        private static void LoginComoAdministrador() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        [TestMethod]
        public void Json_ErroresExportarEImportar_PorBLL_ViajeCompleto_YBitacoraIndicaElFormato()
        {
            LoginComoAdministrador();
            var bitacora = new FakeRegistroBitacora();
            var bll = new BLL.ErroresSerializados(bitacora);
            string ruta = Ruta("errores_exportados.json");

            string formato = bll.Exportar("Test", LibroDePrueba().Errores, ruta);
            var importados = bll.Importar("Test", ruta);

            Assert.AreEqual("JSON", formato);
            StringAssert.StartsWith(File.ReadAllText(ruta), "{");
            Assert.AreEqual(2, importados.Count);
            Assert.AreEqual("lento", importados[0].Mensaje, "Del más nuevo al más viejo.");
            Assert.AreEqual("Algo <falló> & \"sigue\" en C:\\x/y", importados[1].Mensaje);
            Assert.IsTrue(bitacora.Registros.Any(r => r.Actividad.StartsWith("Exportación de errores serializados a JSON")));
            Assert.IsTrue(bitacora.Registros.Any(r => r.Actividad.StartsWith("Importación de errores serializados desde JSON")));
        }

        [TestMethod]
        public void Json_ErroresImportar_Version_Formato_Extension_Rechaza()
        {
            LoginComoAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            string json = JsonErrores.Serializar(LibroDePrueba());
            StringAssert.Contains(json, "\"Version\": 1,");

            string nuevo = Ruta("nuevo.json");
            File.WriteAllText(nuevo, json.Replace("\"Version\": 1,", "\"Version\": 3,"));
            Assert.AreEqual("err.serial.version_nueva", Capturar(() => bll.Importar("Test", nuevo))?.Clave);

            string sinVersion = Ruta("sinversion.json");
            File.WriteAllText(sinVersion, json.Replace("\"Version\": 1,", ""));
            Assert.AreEqual("err.serial.sin_version", Capturar(() => bll.Importar("Test", sinVersion))?.Clave);

            string corrupto = Ruta("corrupto.json");
            File.WriteAllText(corrupto, "{\"Version\": 1, \"Errores\": [");
            Assert.AreEqual("err.serial.formato", Capturar(() => bll.Importar("Test", corrupto))?.Clave);

            string versiones = Ruta("versiones.json");   // JSON válido, pero de versiones
            JsonHistorial.GuardarArchivo(ArchivoValido(), versiones);
            Assert.AreEqual("err.serial.formato", Capturar(() => bll.Importar("Test", versiones))?.Clave);

            string txt = Ruta("errores.txt");
            File.WriteAllText(txt, json);
            Assert.AreEqual("err.serial.extension", Capturar(() => bll.Importar("Test", txt))?.Clave);
            Assert.AreEqual("err.serial.extension", Capturar(() => bll.Exportar("Test", LibroDePrueba().Errores, Ruta("x.csv")))?.Clave);

            Assert.AreEqual("err.serial.no_existe", Capturar(() => bll.Importar("Test", Ruta("no_existe.json")))?.Clave);
        }

        [TestMethod]
        public void Errores_RegistroMensual_SigueSiendoXml()
        {
            var ahora = DateTime.Now;   // una sola fecha: el mes no cambia entre las dos llamadas
            Assert.IsTrue(RegistroErrores.Registrar(new InvalidOperationException("mensual"), "FormA", "admin"));

            string ruta = RegistroErrores.RutaDelMes(ahora);
            StringAssert.StartsWith(ruta, GestorArchivos.Instancia.CarpetaRaiz);
            StringAssert.EndsWith(ruta, ".xml");
            StringAssert.StartsWith(File.ReadAllText(ruta), "<?xml");
            Assert.AreEqual("mensual", RegistroErrores.ObtenerTodos().Single().Mensaje);
        }

        // ── Errores: carpeta de exportación aparte del registro mensual ─────────

        [TestMethod]
        public void Errores_ObtenerTodos_SoloLeeLosArchivosMensuales_IgnoraExportacionesEnLaMismaCarpeta()
        {
            Assert.IsTrue(RegistroErrores.Registrar(new InvalidOperationException("propio"), "FormA", "admin"));
            string carpeta = RegistroErrores.Carpeta;
            // Lo que antes se duplicaba: una exportación guardada en la carpeta del registro.
            XmlErrores.GuardarArchivo(LibroDePrueba(), Path.Combine(carpeta, "errores_wardrobeflow_20261020_101010.xml"));
            XmlErrores.GuardarArchivo(LibroDePrueba(), Path.Combine(carpeta, "errores_2026101.xml"));    // 7 dígitos
            XmlErrores.GuardarArchivo(LibroDePrueba(), Path.Combine(carpeta, "errores_202601x.xml"));
            File.WriteAllText(Path.Combine(carpeta, "errores_202609.xml.danado_20261001000000"), "x");

            var todos = RegistroErrores.ObtenerTodos();

            Assert.AreEqual(1, todos.Count, "Solo errores_AAAAMM.xml.");
            Assert.AreEqual("propio", todos[0].Mensaje);
            Assert.AreEqual(1, RegistroErrores.ObtenerTodos().Count, "Leer de nuevo no duplica.");
            Assert.IsTrue(RegistroErrores.EsArchivoMensual(@"C:\x\ERRORES_202610.XML"));
            Assert.IsFalse(RegistroErrores.EsArchivoMensual("errores_wardrobeflow_20261020_101010.xml"));
        }

        [TestMethod]
        public void Errores_CarpetaDeExportados_EstaAparteDelRegistro_YSeCrea()
        {
            string exportados = RegistroErrores.PrepararCarpetaExportados();

            Assert.AreEqual(Path.Combine(RegistroErrores.Carpeta, "Exportados"), exportados);
            Assert.IsTrue(Directory.Exists(exportados));

            // Una exportación ahí no aparece entre los errores de esta PC.
            LoginComoAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            Assert.IsTrue(RegistroErrores.Registrar(new Exception("uno"), "FormA", "admin"));
            bll.Exportar("Test", bll.ObtenerRegistrados(), Path.Combine(exportados, "errores_202610.xml"));
            Assert.AreEqual(1, bll.ObtenerRegistrados().Count);
        }

        // ── Errores: archivos vacíos, registros nulos y tamaño ──────────────────

        [TestMethod]
        public void Errores_LeerArchivo_RaizXsiNil_SeTomaComoLibroVacio()
        {
            string ruta = Ruta("nil.xml");
            File.WriteAllText(ruta, "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ErroresWardrobeFlow xsi:nil=\"true\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" />");

            var libro = RegistroErrores.LeerArchivo(ruta);

            Assert.IsNotNull(libro);
            Assert.AreEqual(0, libro.Errores.Count);
        }

        [TestMethod]
        public void Errores_Importar_JsonConRegistrosNulos_Rechaza_YEnXmlNoHayNulos()
        {
            LoginComoAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());

            string json = Ruta("nulos.json");
            File.WriteAllText(json, "{\"Version\":1,\"Errores\":[null]}");
            Assert.AreEqual("err.bll.errores.inconsistente", Capturar(() => bll.Importar("Test", json))?.Clave);

            const string conNulo =
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
                "<ErroresWardrobeFlow xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" version=\"1\">" +
                "<Error xsi:nil=\"true\" /><Error fecha=\"2026-10-09T00:00:00\"><Mensaje>real</Mensaje></Error></ErroresWardrobeFlow>";
            // En XML, XmlSerializer no devuelve nulos en la lista (un <Error xsi:nil> queda como un
            // registro sin datos): no hay NullReference ni al importar ni en el registro de esta PC.
            string xml = Ruta("nulos.xml");
            File.WriteAllText(xml, conNulo);
            var importados = bll.Importar("Test", xml);
            Assert.AreEqual(2, importados.Count);
            Assert.IsTrue(importados.All(e => e != null));

            Directory.CreateDirectory(RegistroErrores.Carpeta);
            File.WriteAllText(RegistroErrores.RutaDelMes(new DateTime(2026, 9, 1)), conNulo);
            var todos = bll.ObtenerRegistrados();
            Assert.IsTrue(todos.All(e => e != null));
            Assert.IsTrue(todos.Any(e => e.Mensaje == "real"));
        }

        [TestMethod]
        public void Errores_Importar_ArchivoGigante_Rechaza_EnAmbosFormatos()
        {
            LoginComoAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            foreach (var nombre in new[] { "gigante.xml", "gigante.json" })
            {
                string ruta = Ruta(nombre);
                using (var fs = new FileStream(ruta, FileMode.Create)) fs.SetLength(BLL.ErroresSerializados.TamanoMaximoArchivo + 1);
                var ex = Capturar(() => bll.Importar("Test", ruta));
                Assert.AreEqual("err.bll.errores.muy_grande", ex?.Clave, nombre);
                Assert.AreEqual(5L, ex.Args[0]);
            }
        }

        // ── Lectura tolerante a la codificación y fallas de disco ───────────────

        [TestMethod]
        public void LeerArchivo_XmlUtf16ConBomYDeclaracion_Y_JsonConBom_SeLeen()
        {
            string xml = Ruta("utf16.xml");
            string contenido = XmlErrores.Serializar(LibroDePrueba()).Replace("encoding=\"utf-8\"", "encoding=\"utf-16\"");
            StringAssert.Contains(contenido, "encoding=\"utf-16\"");
            File.WriteAllText(xml, contenido, Encoding.Unicode);   // UTF-16 LE con BOM
            Assert.AreEqual(0xFF, File.ReadAllBytes(xml)[0]);

            Assert.AreEqual(2, RegistroErrores.LeerArchivo(xml).Errores.Count);
            Assert.AreEqual("Pérez", XmlHistorialUtf16(Ruta("v16.xml")).Versiones[0].ApellidoSnapshot);

            string json = Ruta("bom.json");
            File.WriteAllText(json, JsonErrores.Serializar(LibroDePrueba()), new UTF8Encoding(true));
            Assert.AreEqual(0xEF, File.ReadAllBytes(json)[0]);
            Assert.AreEqual(2, JsonErrores.LeerArchivo(json).Errores.Count);

            // XXE sigue prohibido leyendo desde el archivo.
            string xxe = Ruta("xxe.xml");
            File.WriteAllText(xxe, "<?xml version=\"1.0\" encoding=\"utf-16\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]>" +
                                   "<ErroresWardrobeFlow><Error fecha=\"2026-10-09T00:00:00\"><Mensaje>&e;</Mensaje></Error></ErroresWardrobeFlow>", Encoding.Unicode);
            Assert.AreEqual("err.serial.formato", Capturar(() => RegistroErrores.LeerArchivo(xxe))?.Clave);
        }

        private static BE.HistorialUsuarioXml XmlHistorialUtf16(string ruta)
        {
            File.WriteAllText(ruta, XmlHistorial.Serializar(ArchivoValido()).Replace("encoding=\"utf-8\"", "encoding=\"utf-16\""), Encoding.Unicode);
            return XmlHistorial.LeerArchivo(ruta);
        }

        [TestMethod]
        public void GuardarArchivo_SiFallaElReemplazo_NoDejaTemporal_YLaBLLDaUnMensajeClaro()
        {
            LoginComoAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            string ruta = Ruta("solo_lectura.json");
            File.WriteAllText(ruta, "{}");
            File.SetAttributes(ruta, FileAttributes.ReadOnly);   // File.Replace no puede pisarlo
            try
            {
                Assert.AreEqual("err.bll.version.xml.no_escribe", Capturar(() => bll.Exportar("Test", LibroDePrueba().Errores, ruta))?.Clave);
                Assert.IsFalse(File.Exists(ruta + ".tmp"), "No queda el .tmp.");
                Assert.AreEqual("{}", File.ReadAllText(ruta), "El archivo anterior sigue intacto.");
            }
            finally { File.SetAttributes(ruta, FileAttributes.Normal); }
        }

        [TestMethod]
        public void Errores_Importar_ArchivoBloqueado_DaUnMensajeClaro()
        {
            LoginComoAdministrador();
            var bll = new BLL.ErroresSerializados(new FakeRegistroBitacora());
            string ruta = Ruta("bloqueado.xml");
            XmlErrores.GuardarArchivo(LibroDePrueba(), ruta);

            using (new FileStream(ruta, FileMode.Open, FileAccess.Read, FileShare.None))
                Assert.AreEqual("err.bll.version.xml.no_lee", Capturar(() => bll.Importar("Test", ruta))?.Clave);
        }

        // ── Fábrica por extensión ───────────────────────────────────────────────

        [TestMethod]
        public void Fabrica_EligeElSerializadorPorExtension_YRechazaOtras()
        {
            Assert.IsInstanceOfType(FabricaSerializadores.Crear<BE.LibroErrores>("a.xml"), typeof(SerializadorXml<BE.LibroErrores>));
            Assert.IsInstanceOfType(FabricaSerializadores.Crear<BE.LibroErrores>(@"C:\x\A.XML"), typeof(SerializadorXml<BE.LibroErrores>));
            Assert.IsInstanceOfType(FabricaSerializadores.Crear<BE.LibroErrores>(@"C:\x\b.Json"), typeof(SerializadorJson<BE.LibroErrores>));
            Assert.IsInstanceOfType(FabricaSerializadores.Crear<BE.LibroErrores>(".json"), typeof(SerializadorJson<BE.LibroErrores>));

            var xml  = FabricaSerializadores.Crear<BE.HistorialUsuarioXml>("v.xml");
            var json = FabricaSerializadores.Crear<BE.HistorialUsuarioXml>("v.json");
            Assert.AreEqual(".xml", xml.Extension);
            Assert.AreEqual("XML", xml.Formato);
            Assert.AreEqual(".json", json.Extension);
            Assert.AreEqual("JSON", json.Formato);
            Assert.AreEqual("JSON", FabricaSerializadores.Formato("errores.json"));

            foreach (var ruta in new[] { "a.txt", "sin_extension", "a.xml.bak", "", null })
            {
                var ex = Capturar(() => FabricaSerializadores.Crear<BE.LibroErrores>(ruta));
                Assert.AreEqual("err.serial.extension", ex?.Clave, ruta ?? "(null)");
                Assert.AreEqual("err.serial.extension", Capturar(() => FabricaSerializadores.Formato(ruta))?.Clave);
            }
            Assert.AreEqual("a.txt", Capturar(() => FabricaSerializadores.Crear<BE.LibroErrores>(@"C:\x\a.txt")).Args[0]);
        }

        // ── GestorArchivos (Singleton) ──────────────────────────────────────────

        [TestMethod]
        public void GestorArchivos_EsUnico_RutasBajoLocalAppData_YCreaLaCarpeta()
        {
            Assert.AreSame(GestorArchivos.Instancia, GestorArchivos.Instancia);

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            Assert.AreEqual(Path.Combine(localAppData, "WardrobeFlow"), GestorArchivos.CarpetaRaizPorDefecto);

            GestorArchivos.Instancia.CarpetaRaiz = null;   // raíz real (sin crear nada)
            Assert.AreEqual(Path.Combine(localAppData, "WardrobeFlow", "Versiones"), BLL.VersionUsuario.CarpetaVersiones);
            Assert.AreEqual(Path.Combine(localAppData, "WardrobeFlow", "Errores"), RegistroErrores.Carpeta);

            string raiz = Path.Combine(_carpeta, "otra", "raiz");
            GestorArchivos.Instancia.CarpetaRaiz = raiz;
            Assert.IsFalse(Directory.Exists(raiz));
            string errores = RegistroErrores.PrepararCarpeta();
            Assert.AreEqual(Path.Combine(raiz, "Errores"), errores);
            Assert.IsTrue(Directory.Exists(errores));
            Assert.IsTrue(Directory.Exists(GestorArchivos.Instancia.PrepararCarpeta(GestorArchivos.SubcarpetaVersiones)));

            var fecha = new DateTime(2026, 10, 20, 14, 5, 9);
            Assert.AreEqual("errores_wardrobeflow_20261020_140509.json",
                GestorArchivos.Instancia.NombreArchivoSugerido("errores_wardrobeflow", null, fecha, "json"));
            Assert.AreEqual("versiones_a_b_20261020_140509.xml",
                GestorArchivos.Instancia.NombreArchivoSugerido("versiones", "a<b", fecha, ".xml"));
        }
    }
}
