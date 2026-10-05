using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace Tests
{
    /// <summary>T07 — Pruebas del algoritmo de Dígito Verificador (DVH / DVV).</summary>
    [TestClass]
    public class DigitoVerificadorTests
    {
        private readonly Seguridad.DigitoVerificador dv = new Seguridad.DigitoVerificador();

        [TestMethod]
        public void DVH_EsDeterminista()
        {
            int a = dv.CalcularDVH("1", "admin", "hash");
            int b = dv.CalcularDVH("1", "admin", "hash");
            Assert.AreEqual(a, b);
        }

        [TestMethod]
        public void DVH_DetectaCambioDeCampo()
        {
            int a = dv.CalcularDVH("1", "admin", "hash");
            int b = dv.CalcularDVH("1", "admin", "HASH");
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void DVH_DetectaIntercambioEntreCampos()
        {
            int a = dv.CalcularDVH("1", "AB", "CD");
            int b = dv.CalcularDVH("1", "CD", "AB");
            Assert.AreNotEqual(a, b, "El DVH debe detectar el intercambio de valores entre campos.");
        }

        [TestMethod]
        public void DVV_DetectaReordenamientoDeFilas()
        {
            int a = dv.CalcularDVV(new List<int> { 10, 20, 30 });
            int b = dv.CalcularDVV(new List<int> { 30, 20, 10 });
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void DVV_DetectaInsercionDeFila()
        {
            int a = dv.CalcularDVV(new List<int> { 10, 20 });
            int b = dv.CalcularDVV(new List<int> { 10, 20, 5 });
            Assert.AreNotEqual(a, b);
        }

        [TestMethod]
        public void DVH_IncluyeRol_DetectaManipulacionDeRol()
        {
            // A4: el Rol participa del DVH (formato v2) para que una manipulación del rol
            // —del que dependen los permisos efectivos— sea detectada por la verificación
            // de integridad. Antes solo se protegía el Perfil.
            var filaBase = new BE.FilaUsuarioDV
            {
                Id = 1, Username = "u", Clave = "hash",
                Rol = "Vendedor", Perfil = "Vendedor", Estado = "1", IntentosFallidos = "0"
            };
            var filaRolAlterado = new BE.FilaUsuarioDV
            {
                Id = 1, Username = "u", Clave = "hash",
                Rol = "Administrador", Perfil = "Vendedor", Estado = "1", IntentosFallidos = "0"
            };

            int dvhBase     = dv.CalcularDVH(filaBase.CamposParaDVH());
            int dvhAlterado = dv.CalcularDVH(filaRolAlterado.CamposParaDVH());

            Assert.AreNotEqual(dvhBase, dvhAlterado,
                "Cambiar solo el Rol debe cambiar el DVH; si no, una escalada por BD pasaría inadvertida.");
        }

        [TestMethod]
        public void CamposParaDVH_IncluyeRolYEstadoDeLaCuentaEnElOrdenEsperado()
        {
            var fila = new BE.FilaUsuarioDV
            {
                Id = 7, Username = "u", Clave = "h",
                Rol = "Supervisor", Perfil = "Supervisor", Estado = "1", IntentosFallidos = "2",
                Activo = "1", RequiereCambioClave = "0", CantidadBloqueos = "3",
                FechaBloqueo = "2026-10-05T10:20:30.000"
            };
            CollectionAssert.AreEqual(
                new[] { "7", "u", "h", "Supervisor", "Supervisor", "1", "2", "1", "0", "3", "2026-10-05T10:20:30.000" },
                fila.CamposParaDVH());
        }

        [TestMethod]
        public void CamposParaDVH_ReactivarUnUsuarioArchivado_CambiaElDVH()
        {
            // Formato 2: Activo entra al DVH. Antes, reactivar por SQL a un usuario archivado
            // (Activo 0 → 1) no alteraba el dígito y pasaba inadvertido.
            var dv = new Seguridad.DigitoVerificador();
            var fila = new BE.FilaUsuarioDV { Id = 3, Username = "x", Clave = "h", Rol = "Vendedor", Perfil = "Vendedor",
                                              Estado = "1", IntentosFallidos = "0", Activo = "0", RequiereCambioClave = "1",
                                              CantidadBloqueos = "0", FechaBloqueo = "" };
            int antes = dv.CalcularDVH(fila.CamposParaDVH());
            fila.Activo = "1";
            Assert.AreNotEqual(antes, dv.CalcularDVH(fila.CamposParaDVH()));
            fila.Activo = "0"; fila.RequiereCambioClave = "0";
            Assert.AreNotEqual(antes, dv.CalcularDVH(fila.CamposParaDVH()), "Quitar el cambio de clave obligatorio también se detecta.");
        }

        [TestMethod]
        public void Formatear_EsInvarianteALaCulturaRegional()
        {
            var cultura = System.Threading.Thread.CurrentThread.CurrentCulture;
            try
            {
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("es-AR");
                string fechaAr = DAL.DigitoVerificador.Formatear(new System.DateTime(2026, 3, 4, 5, 6, 7, 8));
                string montoAr = DAL.DigitoVerificador.Formatear(1234.50m);
                System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
                Assert.AreEqual(fechaAr, DAL.DigitoVerificador.Formatear(new System.DateTime(2026, 3, 4, 5, 6, 7, 8)));
                Assert.AreEqual(montoAr, DAL.DigitoVerificador.Formatear(1234.50m));
                Assert.AreEqual("2026-03-04T05:06:07.008", fechaAr);
                Assert.AreEqual("1234.50", montoAr);
                Assert.AreEqual("1", DAL.DigitoVerificador.Formatear(true));
                Assert.AreEqual("", DAL.DigitoVerificador.Formatear(System.DBNull.Value));
            }
            finally { System.Threading.Thread.CurrentThread.CurrentCulture = cultura; }
        }

        [TestMethod]
        public void Cliente_DVCubreSuscripcionDineroYBaja()
        {
            foreach (var c in new[] { "IdPlan", "FechaVencimiento", "FechaLimiteGracia", "FechaPausaHasta",
                                      "DescuentoProximoCobro", "IdClienteReferente", "Activo" })
                CollectionAssert.Contains(DAL.Cliente.DV_Columnas, c);
            foreach (var c in new[] { "Importe", "DescuentoAplicado", "IdVendedor", "IdCaja", "Estado" })
                CollectionAssert.Contains(DAL.Contratacion.DV_Columnas, c);
        }

        [TestMethod]
        public void Inicializar_SoloConMarcaDelInstaladorYTablaSinCalcular()
        {
            // Primera instalación / actualización de formato: marca + sin DVV + DVH en cero → inicializa.
            Assert.IsTrue(BLL.Configuracion.CorrespondeInicializar(true, null, new int?[] { 0, null, 0 }));
            // Sin la marca del instalador, una tabla "sin calcular" es una ANOMALÍA (no se sella sola).
            Assert.IsFalse(BLL.Configuracion.CorrespondeInicializar(false, null, new int?[] { 0, 0 }));
            // Con DVV presente (aunque en 0) no se reinicializa: antes, poner DVH=0 y DVV=0 por SQL
            // hacía que la app "lavara" la manipulación recalculando todo.
            Assert.IsFalse(BLL.Configuracion.CorrespondeInicializar(true, 0, new int?[] { 0, 0 }));
            // Alguna fila ya calculada → no es una tabla sin inicializar.
            Assert.IsFalse(BLL.Configuracion.CorrespondeInicializar(true, null, new int?[] { 0, 1234 }));
        }

        [TestMethod]
        public void Comparar_DetectaFilaAlteradaYDVVDesdeLosAlmacenados()
        {
            var svc = new Seguridad.DigitoVerificador();
            var campos = new System.Collections.Generic.List<string[]> { new[] { "1", "a" }, new[] { "2", "b" } };
            var dvhs = new System.Collections.Generic.List<int?> { svc.CalcularDVH(campos[0]), svc.CalcularDVH(campos[1]) };
            int dvv = svc.CalcularDVV(new[] { dvhs[0].Value, dvhs[1].Value });

            var ok = BLL.Configuracion.Comparar(campos, dvhs, dvv, svc);
            Assert.AreEqual(0, ok.Rotas.Count);
            Assert.IsTrue(ok.DvvOk);

            campos[1] = new[] { "2", "B" };   // fila alterada sin recalcular su DVH
            var mal = BLL.Configuracion.Comparar(campos, dvhs, dvv, svc);
            CollectionAssert.AreEqual(new[] { 1 }, mal.Rotas);

            var sinDvv = BLL.Configuracion.Comparar(campos, dvhs, null, svc);
            Assert.IsFalse(sinDvv.DvvOk, "Sin DVV almacenado la tabla no es íntegra.");
        }
    }
}
