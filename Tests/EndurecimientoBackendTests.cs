using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Seguridad;
using Tests.Fakes;

namespace Tests
{
    /// <summary>
    /// Hallazgos BAJA/MEDIA de la auditoría de backend y seguridad que no tienen un archivo de
    /// pruebas propio: recorte de la bitácora, LIKE literal, borrado de backups solo en su
    /// carpeta, preferencias del propio usuario, días de pausa al activar, bitácora de planes,
    /// permisos evaluados sobre la propia sesión y guards de dígitos verificadores.
    /// </summary>
    [TestClass]
    public class EndurecimientoBackendTests
    {
        [TestInitialize] public void Setup()   => SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => SessionManager.Logout();

        private static void LoginAdmin() =>
            SessionManager.Login(new BE.Usuario { Id = 1, Username = "admin", Perfil = "Administrador", Rol = "Administrador" });

        private static BE.AppException Capturar(Action a)
        {
            try { a(); return null; }
            catch (BE.AppException ex) { return ex; }
        }

        // ── Bitácora: textos largos se recortan en vez de perder la entrada ──
        [TestMethod]
        public void TextoSeguro_RecortaAlLargoDeLaColumna()
        {
            string largo = new string('a', 600);
            string r = Servicios.TextoSeguro.Recortar(largo, Servicios.BitacoraNegocio.LargoDescripcion);
            Assert.AreEqual(Servicios.BitacoraNegocio.LargoDescripcion, r.Length);
            Assert.IsTrue(r.EndsWith("…"));
            Assert.AreEqual("corto", Servicios.TextoSeguro.Recortar("corto", 200));
            Assert.IsNull(Servicios.TextoSeguro.Recortar(null, 200));
        }

        // ── Bitácora: los comodines de LIKE se buscan literalmente ──
        [TestMethod]
        public void EscaparLike_TrataComodinesComoTexto()
        {
            Assert.AreEqual("100\\%", DAL.Bitacora.EscaparLike("100%"));
            Assert.AreEqual("a\\_b", DAL.Bitacora.EscaparLike("a_b"));
            Assert.AreEqual("\\[x]", DAL.Bitacora.EscaparLike("[x]"));
            Assert.AreEqual("c:\\\\x", DAL.Bitacora.EscaparLike("c:\\x"));
        }

        // ── Backups: solo se borra dentro de la carpeta de backups ──
        [TestMethod]
        public void EliminarBackup_FueraDeLaCarpetaDeBackups_SeRechaza()
        {
            LoginAdmin();
            string ajeno = Path.Combine(Path.GetTempPath(), "WF_no_es_backup_" + Guid.NewGuid().ToString("N") + ".bak");
            File.WriteAllText(ajeno, "x");
            try
            {
                var ex = Capturar(() => new BLL.Backup(new FakeBackupDAL()).EliminarBackup("Test", ajeno));
                Assert.AreEqual("err.bll.backup.fuera_de_carpeta", ex?.Clave);
                Assert.IsTrue(File.Exists(ajeno), "No debe borrar archivos fuera de la carpeta de backups.");

                string traversal = Path.Combine(BLL.Backup.CarpetaBackups, "..", Path.GetFileName(ajeno));
                Assert.IsFalse(BLL.Backup.EstaDentroDeCarpetaBackups(traversal));
                Assert.IsTrue(BLL.Backup.EstaDentroDeCarpetaBackups(Path.Combine(BLL.Backup.CarpetaBackups, "x.wfbak")));
            }
            finally { File.Delete(ajeno); }
        }

        // ── Preferencias: cada usuario solo guarda las suyas ──
        private sealed class PrefDalFalso : DAL.Interfaces.IPreferenciaDAL
        {
            public int GuardarVeces;
            public BE.Preferencia Obtener(int idUsuario) => null;
            public void Guardar(BE.Preferencia pref) => GuardarVeces++;
        }

        [TestMethod]
        public void Preferencia_Guardar_DeOtroUsuario_SeRechaza()
        {
            LoginAdmin();
            var dal = new PrefDalFalso();
            var bll = new BLL.Preferencia(dal);
            Assert.AreEqual("err.bll.usuario.preferencia_ajena",
                Capturar(() => bll.Guardar(new BE.Preferencia { IdUsuario = 99 }))?.Clave);
            bll.Guardar(new BE.Preferencia { IdUsuario = 1 });
            Assert.AreEqual(1, dal.GuardarVeces);
        }

        [TestMethod]
        public void GuardarPreferenciaIdioma_DeOtroUsuario_SeRechaza()
        {
            LoginAdmin();
            var ex = Capturar(() => new BLL.Usuario(new FakeUsuarioDAL()).GuardarPreferenciaIdioma(99, "EN"));
            Assert.AreEqual("err.bll.usuario.preferencia_ajena", ex?.Clave);
        }

        // ── Activar una suscripción pausada no regala los días de pausa no usados ──
        [TestMethod]
        public void ActivarDesdeContratacion_ClientePausado_DescuentaLosDiasDePausaNoUsados()
        {
            LoginAdmin();
            var plan = new BE.PlanSuscripcion { IdPlan = 1, Nombre = "Básico", LimitePrendas = 3, Precio = 1000m, Estado = true };
            var dalCliente = new FakeClienteDAL();
            // Pagó hasta hoy+30; pausó 10 días → el vencimiento quedó corrido a hoy+40.
            var cliente = new BE.Cliente
            {
                IdCliente = 1, Nombre = "Ana", Apellido = "Gómez", IdPlan = 1,
                FechaVencimiento = DateTime.Today.AddDays(40), FechaPausaHasta = DateTime.Today.AddDays(10)
            };
            dalCliente.ClientePorId = cliente;
            var bll = new BLL.Cliente(dalCliente, new FakePlanSuscripcionDAL { PlanPorId = plan });

            var s = bll.ActivarSuscripcionDesdeContratacion("Test", cliente, 1, BE.Builders.ModalidadCobro.Mensual);

            Assert.AreEqual(DateTime.Today.AddDays(30).AddMonths(1), s.FechaVencimiento,
                "El período nuevo se suma al tiempo realmente pagado, no al corrido por la pausa.");
            Assert.IsNull(cliente.FechaPausaHasta);
        }

        // ── Planes de suscripción: alta/modificación/baja/reactivación quedan en la bitácora ──
        [TestMethod]
        public void PlanSuscripcion_EscriturasQuedanEnLaBitacora()
        {
            LoginAdmin();
            var bitacora = new FakeRegistroBitacora();
            var dal = new FakePlanSuscripcionDAL();
            var bll = new BLL.PlanSuscripcion(dal, bitacora);

            bll.Alta(new BE.PlanSuscripcion { Nombre = "Plus", LimitePrendas = 4, Precio = 2000m });
            bll.Activar(5);

            Assert.AreEqual(2, bitacora.Registros.Count);
            StringAssert.Contains(bitacora.Registros[0].Actividad, "Plus");
            StringAssert.Contains(bitacora.Registros[1].Actividad, "#5");
        }

        // ── SessionManager evalúa los permisos de SU sesión ──
        [TestMethod]
        public void TienePermiso_UnaSesionVieja_NoUsaLosPermisosDeLaSesionNueva()
        {
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor", Permisos = new List<BE.Permiso>() });
            var vieja = SessionManager.GetInstance();
            SessionManager.Logout();
            LoginAdmin();
            Assert.IsFalse(vieja.TienePermiso(BE.Patentes.Usuarios),
                "Una referencia a la sesión anterior no hereda el bypass del Administrador actual.");
        }

        // ── Dígitos verificadores: recalcular exige una sesión de Administrador ──
        [TestMethod]
        public void RecalcularIntegridadDV_SinSesion_Rechaza()
        {
            Assert.AreEqual("err.bll.sesion_expirada", Capturar(() => BLL.Configuracion.RecalcularIntegridadDV())?.Clave);
            Assert.AreEqual("err.bll.sesion_expirada", Capturar(() => BLL.Configuracion.RecalcularUsuario())?.Clave);
        }

        [TestMethod]
        public void GuardarDiasRecordatorio_SinAdministrador_Rechaza()
        {
            SessionManager.Login(new BE.Usuario { Id = 2, Username = "vend", Perfil = "Vendedor" });
            Assert.AreEqual("err.bll.backup.sin_permiso", Capturar(() => BLL.Configuracion.GuardarDiasRecordatorio(3))?.Clave);
        }
    }
}
