using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Servicios.Serializacion;

namespace Tests
{
    /// <summary>
    /// A02 Serialización: los errores inesperados se serializan a XML (Servicios.Serializacion) y se
    /// pueden volver a leer, importar desde otro archivo y exportar.
    /// </summary>
    [TestClass]
    public class SerializacionTests
    {
        private string _carpetaOriginal;
        private string _carpeta;

        [TestInitialize]
        public void Setup()
        {
            SessionManager.Logout();
            _carpetaOriginal = RegistroErrores.Carpeta;
            _carpeta = Path.Combine(Path.GetTempPath(), "wf_errores_" + Guid.NewGuid().ToString("N"));
            RegistroErrores.Carpeta = _carpeta;
        }

        [TestCleanup]
        public void Cleanup()
        {
            SessionManager.Logout();
            RegistroErrores.Carpeta = _carpetaOriginal;
            try { Directory.Delete(_carpeta, true); } catch { }
        }

        private static void LoginComoAdministrador() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Contraseña = Encriptador.Hash("Admin1!") });

        [TestMethod]
        public void SerializarYDeserializar_ConservaTodosLosCampos()
        {
            var libro = new BE.LibroErrores();
            libro.Errores.Add(new BE.RegistroError
            {
                Fecha = new DateTime(2026, 10, 9, 14, 30, 5), Usuario = "caja", Modulo = "ContratacionesPendientesForm",
                Tipo = "System.InvalidOperationException", Mensaje = "Algo <falló> & \"sigue\"", Causa = "SqlException: timeout", Equipo = "PC1"
            });

            string xml = SerializadorXml.Serializar(libro);
            var leido = SerializadorXml.Deserializar<BE.LibroErrores>(xml);

            StringAssert.Contains(xml, "<ErroresWardrobeFlow");
            Assert.AreEqual(1, leido.Errores.Count);
            var e = leido.Errores[0];
            Assert.AreEqual(new DateTime(2026, 10, 9, 14, 30, 5), e.Fecha);
            Assert.AreEqual("caja", e.Usuario);
            Assert.AreEqual("Algo <falló> & \"sigue\"", e.Mensaje);
            Assert.AreEqual("SqlException: timeout", e.Causa);
            Assert.AreEqual("PC1", e.Equipo);
        }

        [TestMethod]
        public void Registrar_GuardaElErrorEnElArchivoDelMes_YSeAcumulan()
        {
            Assert.IsTrue(RegistroErrores.Registrar(new InvalidOperationException("uno", new TimeoutException("lento")), "FormA", "admin"));
            Assert.IsTrue(RegistroErrores.Registrar(new ArgumentException("dos"), "FormB", null));

            Assert.IsTrue(File.Exists(RegistroErrores.RutaDelMes(DateTime.Now)));
            var todos = RegistroErrores.ObtenerTodos();
            Assert.AreEqual(2, todos.Count);
            Assert.AreEqual("dos", todos[0].Mensaje, "Del más nuevo al más viejo.");
            Assert.AreEqual("(sin sesión)", todos[0].Usuario);
            Assert.AreEqual("TimeoutException: lento", todos[1].Causa);
        }

        [TestMethod]
        public void Registrar_ArchivoDanado_LoApartaYSigueRegistrando()
        {
            Directory.CreateDirectory(_carpeta);
            File.WriteAllText(RegistroErrores.RutaDelMes(DateTime.Now), "<esto no es xml");

            Assert.IsTrue(RegistroErrores.Registrar(new Exception("nuevo"), "FormA", "admin"));

            Assert.AreEqual(1, RegistroErrores.ObtenerTodos().Count);
            Assert.AreEqual(1, Directory.GetFiles(_carpeta, "*.danado_*").Length);
        }

        [TestMethod]
        public void Deserializar_ConDTD_SeRechaza()
        {
            string xml = "<?xml version=\"1.0\"?><!DOCTYPE x [<!ENTITY e SYSTEM \"file:///c:/windows/win.ini\">]>" +
                         "<ErroresWardrobeFlow><Error fecha=\"2026-10-09T00:00:00\"><Mensaje>&e;</Mensaje></Error></ErroresWardrobeFlow>";
            try
            {
                SerializadorXml.Deserializar<BE.LibroErrores>(xml);
                Assert.Fail("Un XML con DTD no se debe procesar.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.serial.formato", ex.Clave); }
        }

        [TestMethod]
        public void ImportarYExportar_ConAuditoria_HaceElViajeCompleto()
        {
            LoginComoAdministrador();
            var bll = new BLL.ErroresSerializados(new Tests.Fakes.FakeRegistroBitacora());
            RegistroErrores.Registrar(new Exception("para exportar"), "FormA", "admin");
            string destino = Path.Combine(_carpeta, "exportado.xml");

            bll.Exportar("Test", bll.ObtenerRegistrados(), destino);
            var importados = bll.Importar("Test", destino);

            Assert.AreEqual(1, importados.Count);
            Assert.AreEqual("para exportar", importados.Single().Mensaje);
        }

        [TestMethod]
        public void ObtenerRegistrados_SinPatenteDeAuditoria_Rechaza()
        {
            var u = new BE.Usuario { Id = 7, Username = "vend", Perfil = "Vendedor", Contraseña = Encriptador.Hash("Clave1!") };
            u.Permisos.Add(new BE.Permiso { NombreMenu = BE.Patentes.PedidosVenta });
            SessionManager.Login(u);
            try
            {
                new BLL.ErroresSerializados(new Tests.Fakes.FakeRegistroBitacora()).ObtenerRegistrados();
                Assert.Fail("Solo con la patente de Auditoría.");
            }
            catch (BE.AppException ex) { Assert.AreEqual("err.bll.sin_permiso", ex.Clave); }
        }
    }
}
