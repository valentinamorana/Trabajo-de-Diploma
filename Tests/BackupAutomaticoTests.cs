using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Backup automático (al iniciar sesión y reloj de 24 h): rotación semanal sobre
    /// auto_&lt;día&gt;.bak, autor en la lista y resultado de la falla. Sin SQL Server.
    /// </summary>
    [TestClass]
    public class BackupAutomaticoTests
    {
        [TestMethod]
        public void NombreArchivoAutomatico_RotaSobreSieteArchivosUnoPorDia()
        {
            // 11/10/2026 es domingo.
            var domingo = new DateTime(2026, 10, 11, 23, 59, 0);
            string[] esperados =
                { "auto_domingo.bak", "auto_lunes.bak", "auto_martes.bak", "auto_miercoles.bak",
                  "auto_jueves.bak", "auto_viernes.bak", "auto_sabado.bak" };

            for (int i = 0; i < 7; i++)
                Assert.AreEqual(esperados[i], BLL.Backup.NombreArchivoAutomatico(domingo.AddDays(i)));

            // Una semana después vuelve al mismo archivo (lo pisa).
            Assert.AreEqual("auto_domingo.bak", BLL.Backup.NombreArchivoAutomatico(domingo.AddDays(7)));
        }

        [TestMethod]
        public void NombreArchivoAutomatico_MismoDiaMismoArchivo()
        {
            Assert.AreEqual(
                BLL.Backup.NombreArchivoAutomatico(new DateTime(2026, 10, 16, 0, 1, 0)),
                BLL.Backup.NombreArchivoAutomatico(new DateTime(2026, 10, 16, 23, 59, 0)));
        }

        [TestMethod]
        public void ExtraerAutor_BackupAutomatico_EsElSistema()
        {
            var bll = new BLL.Backup(new Fakes.FakeBackupDAL());
            Assert.IsTrue(BLL.Backup.EsAutomatico("auto_lunes.bak"));
            Assert.AreEqual("sistema (automático)", bll.ExtraerAutorDeNombre("auto_lunes.bak"));
            Assert.IsFalse(BLL.Backup.EsAutomatico("WardrobeFlow_Backup_20261016120000_admin.wfbak"));
        }

        [TestMethod]
        public void AvisoLogin_SoloSeGuardaSiFallo_YSeMuestraUnaVez()
        {
            BLL.Backup.GuardarAvisoLogin(BLL.ResultadoBackupAutomatico.Realizado(@"C:\x\auto_lunes.bak"));
            Assert.IsNull(BLL.Backup.TomarAvisoLogin(), "Un backup exitoso no deja aviso.");

            BLL.Backup.GuardarAvisoLogin(BLL.ResultadoBackupAutomatico.Salteado("intervalo"));
            Assert.IsNull(BLL.Backup.TomarAvisoLogin(), "Un backup salteado no deja aviso.");

            BLL.Backup.GuardarAvisoLogin(BLL.ResultadoBackupAutomatico.Fallido(@"C:\x\auto_lunes.bak", "disco lleno"));
            var aviso = BLL.Backup.TomarAvisoLogin();
            Assert.IsNotNull(aviso);
            Assert.AreEqual("disco lleno", aviso.Error);
            Assert.IsNull(BLL.Backup.TomarAvisoLogin(), "El aviso se muestra una sola vez.");
        }

        [TestMethod]
        public void BackupAutomatico_ConIntegridadComprometida_SeSaltea_SinTocarElDAL()
        {
            var fake = new Fakes.FakeBackupDAL();
            bool antes = BLL.Configuracion.IntegridadComprometida;
            try
            {
                BLL.Configuracion.IntegridadComprometida = true;
                var r = new BLL.Backup(fake).HacerBackupAutomatico(BLL.OrigenBackup.Login);
                Assert.AreEqual(BLL.EstadoBackupAutomatico.Salteado, r.Estado);
                Assert.AreEqual(0, fake.VecesBackup, "Con la integridad comprometida no se respalda.");

                Assert.AreEqual(BLL.EstadoBackupAutomatico.Salteado, BLL.BackupProgramado.Revisar().Estado);
            }
            finally
            {
                BLL.Configuracion.IntegridadComprometida = antes;
            }
        }
    }
}
