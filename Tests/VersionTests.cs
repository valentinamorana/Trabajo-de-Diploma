using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// Versión del producto: un solo número en todos lados. Los ensamblados (AssemblyVersion y
    /// AssemblyFileVersion de cada Properties/AssemblyInfo.cs) tienen que decir lo mismo que
    /// MyAppVersion del instalador (Instalador/WardrobeFlow_Setup.iss) + ".0"; «Acerca de WardrobeFlow»
    /// muestra la del GUI.exe en ejecución. Así no vuelve a pasar que el instalador diga 1.6.0 y los
    /// ensamblados 1.0.0.0. No fija el número: al pasar a otra versión alcanza con cambiarla en los
    /// AssemblyInfo.cs y en el .iss.
    /// </summary>
    [TestClass]
    public class VersionTests
    {
        private static readonly Assembly[] Ensamblados =
        {
            typeof(BE.Usuario).Assembly,
            typeof(Seguridad.Encriptador).Assembly,
            typeof(DAL.Acceso).Assembly,
            typeof(Servicios.Multiidioma.Traductor).Assembly,
            typeof(BLL.MapaNavegacion).Assembly,
            typeof(VersionTests).Assembly,
        };

        [TestMethod]
        public void Ensamblados_TienenTodosLaMismaVersion_YNoLaDePlantilla()
        {
            Version referencia = typeof(BLL.MapaNavegacion).Assembly.GetName().Version;
            Assert.AreNotEqual(new Version(1, 0, 0, 0), referencia, "AssemblyVersion quedó en el valor de la plantilla (1.0.0.0).");
            foreach (var asm in Ensamblados)
                Assert.AreEqual(referencia, asm.GetName().Version, asm.GetName().Name);
        }

        // Raíz del repositorio (la carpeta de WardrobeFlow.slnx), subiendo desde la salida de los tests.
        private static string Raiz()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "WardrobeFlow.slnx"))) return dir.FullName;
            return null;
        }

        [TestMethod]
        public void AssemblyInfo_EInstalador_DicenLaMismaVersion()
        {
            string raiz = Raiz();
            if (raiz == null) Assert.Inconclusive("No se encontró WardrobeFlow.slnx junto a la salida de los tests.");

            string iss = File.ReadAllText(Path.Combine(raiz, "Instalador", "WardrobeFlow_Setup.iss"));
            Match m = Regex.Match(iss, "#define MyAppVersion \"([^\"]+)\"");
            Assert.IsTrue(m.Success, "No se encontró MyAppVersion en WardrobeFlow_Setup.iss.");
            string esperada = m.Groups[1].Value + ".0";
            Assert.AreEqual(Version.Parse(esperada), typeof(BLL.MapaNavegacion).Assembly.GetName().Version,
                "La versión de los ensamblados en ejecución no es la del instalador.");

            // Los 8 proyectos de WardrobeFlow.slnx.
            string[] proyectos = { "BE", "Seguridad", "DAL", "Servicios", "BLL", "GUI", "Tests", Path.Combine("Instalador", "DbInstaller") };

            var errores = new StringBuilder();
            foreach (var proyecto in proyectos)
            {
                string archivo = Path.Combine(raiz, proyecto, "Properties", "AssemblyInfo.cs");
                if (!File.Exists(archivo)) { errores.AppendLine(archivo + ": no existe"); continue; }
                string texto = File.ReadAllText(archivo);
                foreach (var atributo in new[] { "AssemblyVersion", "AssemblyFileVersion" })
                {
                    Match a = Regex.Match(texto, @"\[assembly:\s*" + atributo + "\\(\"([^\"]+)\"\\)\\]");
                    if (!a.Success) errores.AppendLine(archivo + ": falta " + atributo);
                    else if (a.Groups[1].Value != esperada)
                        errores.AppendLine(archivo + ": " + atributo + " = " + a.Groups[1].Value + " (se esperaba " + esperada + ")");
                }
            }
            Assert.AreEqual(0, errores.Length, errores.ToString());
        }
    }
}
