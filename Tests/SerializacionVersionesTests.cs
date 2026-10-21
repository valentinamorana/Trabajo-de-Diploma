using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Servicios.Serializacion;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// A02 Serialización de ESTADOS DE OBJETOS: las versiones (Mementos) de un usuario se exportan a
    /// XML (BLL.VersionUsuario.Exportar), se importan validadas (Importar) y desde el archivo se
    /// restaura al usuario con el mismo flujo del historial (RestaurarVersionImportada).
    /// Sin base de datos: FakeUsuarioDAL + Caretaker e historial en memoria, carpeta temporal.
    /// </summary>
    [TestClass]
    public class SerializacionVersionesTests
    {
        private FakeUsuarioDAL _dal;
        private Dictionary<int, List<BE.VersionUsuario>> _historial;
        private List<(int IdUsuario, BE.VersionUsuario Version)> _guardadas;
        private string _carpeta;
        private string _carpetaOriginal;
        private static readonly SerializadorXml<BE.HistorialUsuarioXml> Xml = new SerializadorXml<BE.HistorialUsuarioXml>();

        [TestInitialize]
        public void Setup()
        {
            SessionManager.Logout();
            ConfiguracionTests.InstalarUsuarioVigentePorDefecto();
            _dal       = new FakeUsuarioDAL();
            _historial = new Dictionary<int, List<BE.VersionUsuario>>();
            _guardadas = new List<(int, BE.VersionUsuario)>();
            _carpeta   = Path.Combine(Path.GetTempPath(), "wf_versiones_" + Guid.NewGuid().ToString("N"));
            GestorArchivos.Instancia.CarpetaRaiz = null;   // raíz real: %LOCALAPPDATA%\WardrobeFlow
            _carpetaOriginal = BLL.VersionUsuario.CarpetaVersiones;
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
            Directory.CreateDirectory(_carpeta);
            return Path.Combine(_carpeta, nombre);
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

        private BE.HistorialUsuarioXml ArchivoValido() => new BE.HistorialUsuarioXml
        {
            Version = BE.HistorialUsuarioXml.FormatoActual, Equipo = "PC-UAI", ExportadoPor = "admin",
            FechaExportacion = new DateTime(2026, 10, 3, 10, 0, 0), IdUsuario = 5, Username = "ana.nueva",
            Versiones = new List<BE.VersionUsuario> { Version(10, "ana", "vieja@x.com", new DateTime(2026, 10, 1, 8, 30, 15)) }
        };

        // ── Ida y vuelta ────────────────────────────────────────────────────────

        [TestMethod]
        public void SerializarYDeserializar_ConservaElEstadoDelMemento_YExcluyeLosSecretos()
        {
            var original = ArchivoValido();
            original.Versiones[0].FechaNacSnapshot = null;   // también un nulo
            original.Versiones.Add(Version(12, "ana.x", "x@x.com", new DateTime(2026, 10, 2, 11, 5, 59)));

            string xml = Xml.Serializar(original);
            var leido = Xml.Deserializar(xml);

            StringAssert.Contains(xml, "<HistorialUsuarioWardrobeFlow");
            StringAssert.Contains(xml, "version=\"1\"");
            foreach (var secreto in new[] { "ClaveSnapshot", "HASH-SECRETO", "EstadoSnapshot", "IntentosSnapshot" })
                Assert.IsFalse(xml.Contains(secreto), $"El archivo no debe contener '{secreto}'.");

            Assert.AreEqual(1, leido.Version);
            Assert.AreEqual("PC-UAI", leido.Equipo);
            Assert.AreEqual("admin", leido.ExportadoPor);
            Assert.AreEqual(new DateTime(2026, 10, 3, 10, 0, 0), leido.FechaExportacion);
            Assert.AreEqual(5, leido.IdUsuario);
            Assert.AreEqual("ana.nueva", leido.Username);
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
            Assert.AreEqual(0, v.IntentosSnapshot);
            Assert.AreEqual(new DateTime(1990, 3, 15), leido.Versiones[1].FechaNacSnapshot);
        }

        [TestMethod]
        public void ExportarEImportar_ConAdministrador_HaceElViajeCompletoPorArchivo()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string ruta = Path.Combine(_carpeta, "sub", "versiones_ana.xml");   // carpeta que no existe

            var exportado = Crear().Exportar("Test", 5, ruta);
            var importado = Crear().Importar("Test", ruta);

            Assert.IsTrue(File.Exists(ruta));
            Assert.AreEqual(2, exportado.Versiones.Count);
            Assert.AreEqual(Environment.MachineName, importado.Equipo);
            Assert.AreEqual("admin", importado.ExportadoPor);
            Assert.AreEqual("ana.nueva", importado.Username);
            CollectionAssert.AreEquivalent(new[] { 10, 11 }, importado.Versiones.Select(v => v.Id).ToArray());
            Assert.AreEqual("vieja@x.com", importado.Versiones.Single(v => v.Id == 10).EmailSnapshot);
            Assert.IsFalse(File.ReadAllText(ruta).Contains("HASH-SECRETO"));
            Assert.AreEqual(0, _guardadas.Count, "Importar no escribe en HistorialUsuario.");
        }

        [TestMethod]
        public void Exportar_NoAdministrador_Rechaza()
        {
            LoginComo("Vendedor");
            Sembrar();
            var ex = Capturar(() => Crear().Exportar("Test", 5, Ruta("x.xml")));
            Assert.AreEqual("err.bll.version.xml.sin_permiso", ex?.Clave);
            Assert.IsFalse(File.Exists(Ruta("x.xml")));
        }

        [TestMethod]
        public void Exportar_SinVersiones_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            _dal.Usuarios.Add(new BE.Usuario { Id = 5, Username = "ana.nueva" });
            var ex = Capturar(() => Crear().Exportar("Test", 5, Ruta("x.xml")));
            Assert.AreEqual("err.bll.version.xml.sin_versiones", ex?.Clave);
        }

        // ── Versión de formato ──────────────────────────────────────────────────

        [TestMethod]
        public void Importar_VersionDeFormatoMasNueva_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string ruta = Ruta("nuevo.xml");
            File.WriteAllText(ruta, Xml.Serializar(ArchivoValido()).Replace("version=\"1\"", "version=\"2\""));

            var ex = Capturar(() => Crear().Importar("Test", ruta));

            Assert.AreEqual("err.serial.version_nueva", ex?.Clave);
            CollectionAssert.AreEqual(new object[] { 2, 1 }, ex.Args);
        }

        [TestMethod]
        public void Importar_SinVersionDeFormato_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string ruta = Ruta("sinversion.xml");
            File.WriteAllText(ruta, Xml.Serializar(ArchivoValido()).Replace(" version=\"1\"", ""));

            Assert.AreEqual("err.serial.sin_version", Capturar(() => Crear().Importar("Test", ruta))?.Clave);
        }

        // ── Archivos dañados o ajenos ───────────────────────────────────────────

        [TestMethod]
        public void Importar_ArchivoCorrupto_Inexistente_OtraRaiz_O_Gigante_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();

            string corrupto = Ruta("corrupto.xml");
            File.WriteAllText(corrupto, "<HistorialUsuarioWardrobeFlow version=\"1\"><Versiones><Version");
            Assert.AreEqual("err.serial.formato", Capturar(() => Crear().Importar("Test", corrupto))?.Clave);

            Assert.AreEqual("err.serial.no_existe", Capturar(() => Crear().Importar("Test", Ruta("no_existe.xml")))?.Clave);

            string errores = Ruta("errores.xml");   // un XML válido, pero del log de errores
            new SerializadorXml<BE.LibroErrores>().GuardarArchivo(new BE.LibroErrores(), errores);
            Assert.AreEqual("err.serial.formato", Capturar(() => Crear().Importar("Test", errores))?.Clave);

            string gigante = Ruta("gigante.xml");
            using (var fs = new FileStream(gigante, FileMode.Create)) fs.SetLength(BLL.VersionUsuario.TamanoMaximoArchivo + 1);
            Assert.AreEqual("err.bll.version.xml.muy_grande", Capturar(() => Crear().Importar("Test", gigante))?.Clave);
        }

        [TestMethod]
        public void Importar_ConDTD_SeRechaza()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string ruta = Ruta("xxe.xml");
            File.WriteAllText(ruta, "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]>" +
                                    "<HistorialUsuarioWardrobeFlow version=\"1\"><Username>&e;</Username></HistorialUsuarioWardrobeFlow>");
            Assert.AreEqual("err.serial.formato", Capturar(() => Crear().Importar("Test", ruta))?.Clave);
        }

        [TestMethod]
        public void ValidarArchivo_VersionesDeOtroUsuarioORepetidas_Inconsistente()
        {
            var deOtro = ArchivoValido();
            deOtro.Versiones[0].IdUsuario = 6;
            Assert.AreEqual("err.bll.version.xml.inconsistente", Capturar(() => BLL.VersionUsuario.ValidarArchivo(deOtro))?.Clave);

            var repetidas = ArchivoValido();
            repetidas.Versiones.Add(repetidas.Versiones[0]);
            Assert.AreEqual("err.bll.version.xml.inconsistente", Capturar(() => BLL.VersionUsuario.ValidarArchivo(repetidas))?.Clave);

            var largo = ArchivoValido();
            largo.Versiones[0].EmailSnapshot = new string('x', 201);
            Assert.AreEqual("err.bll.version.xml.dato_invalido", Capturar(() => BLL.VersionUsuario.ValidarArchivo(largo))?.Clave);

            var vacio = ArchivoValido();
            vacio.Versiones.Clear();
            Assert.AreEqual("err.bll.version.xml.vacio", Capturar(() => BLL.VersionUsuario.ValidarArchivo(vacio))?.Clave);
        }

        // ── Pertenencia al usuario ──────────────────────────────────────────────

        [TestMethod]
        public void Importar_UsuarioInexistente_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            string ruta = Ruta("v.xml");
            Xml.GuardarArchivo(ArchivoValido(), ruta);

            var ex = Capturar(() => Crear().Importar("Test", ruta));

            Assert.AreEqual("err.bll.version.xml.usuario_inexistente", ex?.Clave);
            StringAssert.Contains(ex.Message, "'ana.nueva' (ID 5)");
        }

        [TestMethod]
        public void Importar_MismoIdPeroOtraPersona_Rechaza()
        {
            LoginComo(BE.Roles.Administrador);
            _dal.Usuarios.Add(new BE.Usuario { Id = 5, Username = "carlos" });   // otra base: el ID 5 es otro
            string ruta = Ruta("v.xml");
            Xml.GuardarArchivo(ArchivoValido(), ruta);

            var ex = Capturar(() => Crear().Importar("Test", ruta));

            Assert.AreEqual("err.bll.version.xml.otro_usuario", ex?.Clave);
            CollectionAssert.AreEqual(new object[] { "ana.nueva", 5, "carlos" }, ex.Args);
        }

        [TestMethod]
        public void Importar_UsuarioRenombradoDespuesDeExportar_SeAceptaPorSuHistorial()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            _dal.Usuarios[0].Username = "ana.otra";   // se renombró después de exportar
            string ruta = Ruta("v.xml");
            Xml.GuardarArchivo(ArchivoValido(), ruta);   // exportado como "ana.nueva"

            var importado = Crear().Importar("Test", ruta);

            Assert.AreEqual(1, importado.Versiones.Count);
        }

        // ── Restaurar desde el archivo importado ────────────────────────────────

        [TestMethod]
        public void RestaurarVersionImportada_UsaElMismoFlujo_RestauraYGrabaSnapshot()
        {
            LoginComo(BE.Roles.Administrador);
            Sembrar();
            string ruta = Ruta("v.xml");
            Crear().Exportar("Test", 5, ruta);
            var importado = Crear().Importar("Test", ruta);

            Crear().RestaurarVersionImportada("Test", importado, 10);

            Assert.AreEqual(1, _dal.RestaurarVersionVeces, "Mismo UPDATE (con DV) que la restauración normal.");
            var u = _dal.ObtenerPorId(5);
            Assert.AreEqual("ana", u.Username);
            Assert.AreEqual("vieja@x.com", u.Email);
            Assert.AreEqual(1, _guardadas.Count, "Snapshot del nuevo estado en el historial.");
            StringAssert.StartsWith(_guardadas[0].Version.Detalle, "Restauración a versión importada ID 10");
            StringAssert.Contains(_guardadas[0].Version.Detalle, "'ana.nueva' → 'ana'");
        }

        [TestMethod]
        public void RestaurarVersionImportada_NoAdministrador_UsernameEnUso_OVersionInexistente_Rechaza()
        {
            Sembrar();
            var archivo = ArchivoValido();

            LoginComo("Vendedor");
            Assert.AreEqual("err.bll.version.sin_permiso", Capturar(() => Crear().RestaurarVersionImportada("Test", archivo, 10))?.Clave);

            SessionManager.Logout();
            LoginComo(BE.Roles.Administrador);
            Assert.AreEqual("err.bll.version.no_existe", Capturar(() => Crear().RestaurarVersionImportada("Test", archivo, 999))?.Clave);

            _dal.Usuarios.Add(new BE.Usuario { Id = 6, Username = "ANA" });
            Assert.AreEqual("err.bll.version.username_en_uso", Capturar(() => Crear().RestaurarVersionImportada("Test", archivo, 10))?.Clave);
            Assert.AreEqual(0, _dal.RestaurarVersionVeces);
            Assert.AreEqual(0, _guardadas.Count);
        }

        [TestMethod]
        public void RestaurarDesde_MementoImportado_DejaElEstadoEsperado_SinTocarClaveNiBloqueo()
        {
            var xml = Xml.Serializar(ArchivoValido());
            var memento = Xml.Deserializar(xml).Versiones[0];
            var usuario = new BE.Usuario
            {
                Id = 5, Username = "ana.nueva", Nombre = "Otra", Apellido = "Persona", Email = "nueva@x.com",
                FechaNacimiento = null, Contraseña = "HASH-ACTUAL", Bloqueado = true, IntentosFallidos = 2
            };

            usuario.RestaurarDesde(memento);

            Assert.AreEqual("ana", usuario.Username);
            Assert.AreEqual("Ana", usuario.Nombre);
            Assert.AreEqual("Pérez", usuario.Apellido);
            Assert.AreEqual("vieja@x.com", usuario.Email);
            Assert.AreEqual(new DateTime(1990, 3, 15), usuario.FechaNacimiento);
            Assert.AreEqual("HASH-ACTUAL", usuario.Contraseña);
            Assert.IsTrue(usuario.Bloqueado);
            Assert.AreEqual(2, usuario.IntentosFallidos);
        }

        // ── Ruta genérica ───────────────────────────────────────────────────────

        [TestMethod]
        public void PrepararCarpetaVersiones_CreaLaCarpeta_YElNombreEsValido()
        {
            StringAssert.StartsWith(_carpetaOriginal,
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));
            StringAssert.EndsWith(_carpetaOriginal, Path.Combine("WardrobeFlow", "Versiones"));

            GestorArchivos.Instancia.CarpetaRaiz = Path.Combine(_carpeta, "a", "b");   // raíz de prueba (GestorArchivos)
            Assert.AreEqual(Path.Combine(_carpeta, "a", "b", "Versiones"), BLL.VersionUsuario.CarpetaVersiones);
            Assert.IsFalse(Directory.Exists(BLL.VersionUsuario.CarpetaVersiones));

            string carpeta = BLL.VersionUsuario.PrepararCarpetaVersiones();

            Assert.IsTrue(Directory.Exists(carpeta));
            string nombre = BLL.VersionUsuario.NombreArchivoSugerido("ana:x/y", new DateTime(2026, 10, 20, 14, 5, 9));
            Assert.AreEqual("versiones_ana_x_y_20261020_140509.xml", nombre);
            Assert.AreEqual(-1, nombre.IndexOfAny(Path.GetInvalidFileNameChars()));
            Assert.AreEqual("versiones_ana_20261020_140509.json",
                BLL.VersionUsuario.NombreArchivoSugerido("ana", new DateTime(2026, 10, 20, 14, 5, 9), ".json"));
        }

        // ── Log de errores: la versión de formato también se valida al leer ─────

        [TestMethod]
        public void ErroresSerializados_Importar_VersionDeFormatoDesconocida_Rechaza_YSinAtributoSeAcepta()
        {
            string nuevo = Ruta("errores_nuevo.xml");
            File.WriteAllText(nuevo, "<?xml version=\"1.0\" encoding=\"utf-8\"?><ErroresWardrobeFlow version=\"3\" />");
            Assert.AreEqual("err.serial.version_nueva", Capturar(() => RegistroErrores.LeerArchivo(nuevo))?.Clave);

            string viejo = Ruta("errores_viejo.xml");   // archivos previos a la validación: sin atributo
            File.WriteAllText(viejo, "<?xml version=\"1.0\" encoding=\"utf-8\"?><ErroresWardrobeFlow><Error fecha=\"2026-10-09T00:00:00\"><Mensaje>x</Mensaje></Error></ErroresWardrobeFlow>");
            Assert.AreEqual(1, RegistroErrores.LeerArchivo(viejo).Errores.Count);
        }
    }
}
