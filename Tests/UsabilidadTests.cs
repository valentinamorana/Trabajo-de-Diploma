using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Servicios.Multiidioma;

namespace Tests
{
    /// <summary>
    /// USABILIDAD — "dónde estoy, qué hace el sistema, cómo avanzar":
    ///   • BLL.MapaNavegacion: toda pantalla que se abre desde el menú tiene su ítem y su pista
    ///     (ux.hint.*) en los 4 idiomas, y el catálogo coincide con los manejadores reales de
    ///     GUI/Menu.cs (se leen los fuentes: el proyecto de tests no referencia la GUI).
    ///   • Tooltips tip.&lt;Formulario&gt;.&lt;control&gt;: en los 4 idiomas y apuntando a controles que existen.
    ///   • BLL.GuiaPrimerUso: pasos por rol y la preferencia "No volver a mostrar" por usuario.
    /// </summary>
    [TestClass]
    public class UsabilidadTests
    {
        private static readonly string[] Idiomas = { "ES", "EN", "RU", "PT" };

        private static IDictionary<string, Traduccion> Corpus(string idioma)
            => Traductor.ObtenerTraduccionesHardcode(new Idioma { Id = idioma });

        private static void AssertClaveEnLosCuatroIdiomas(string clave, StringBuilder errores)
        {
            foreach (var idm in Idiomas)
            {
                var c = Corpus(idm);
                if (!c.ContainsKey(clave) || string.IsNullOrWhiteSpace(c[clave].Texto))
                    errores.AppendLine(idm + " sin texto para " + clave);
            }
        }

        // ── Fuentes de la GUI (para cotejar sin instanciar formularios) ─────────

        private static string CarpetaGui()
        {
            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            for (int i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
            {
                string gui = Path.Combine(dir.FullName, "GUI");
                if (File.Exists(Path.Combine(gui, "Menu.Designer.cs"))) return gui;
            }
            return null;
        }

        private static string GuiOInconcluso()
        {
            string gui = CarpetaGui();
            if (gui == null) Assert.Inconclusive("No se encontraron los fuentes de GUI junto a la salida de los tests.");
            return gui;
        }

        // Todos los .cs de GUI (y subcarpetas, sin bin/obj), leídos una sola vez.
        private static Dictionary<string, string> _fuentesGui;
        private static Dictionary<string, string> FuentesGui(string gui)
        {
            if (_fuentesGui != null) return _fuentesGui;
            var d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var f in Directory.GetFiles(gui, "*.cs", SearchOption.AllDirectories))
            {
                string rel = f.Substring(gui.Length);
                if (rel.IndexOf(@"\bin\", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    rel.IndexOf(@"\obj\", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                d[f] = File.ReadAllText(f);
            }
            return _fuentesGui = d;
        }

        /// <summary>Texto de TODAS las partes (partial) de la clase, estén en el archivo que estén.</summary>
        private static List<string> PartesDeLaClase(string gui, string clase)
        {
            var decl = new Regex(@"\bclass\s+" + Regex.Escape(clase) + @"\b");
            return FuentesGui(gui).Values.Where(t => decl.IsMatch(t)).ToList();
        }

        private static readonly Dictionary<string, bool> _esFormulario = new Dictionary<string, bool>(StringComparer.Ordinal);
        private static bool EsFormularioDeGui(string gui, string tipo)
        {
            if (_esFormulario.TryGetValue(tipo, out bool es)) return es;
            var baseForm = new Regex(@"\bclass\s+" + Regex.Escape(tipo) + @"\s*:\s*(FormBase|Form)\b");
            return _esFormulario[tipo] = FuentesGui(gui).Values.Any(t => baseForm.IsMatch(t));
        }

        // ── Lector mínimo de C# para los manejadores del Menú ──────────────────

        /// <summary>Saltea un literal de texto o carácter que empieza en i; devuelve el índice siguiente.</summary>
        private static int SaltearLiteral(string s, int i)
        {
            bool verbatim = false;
            int j = i;
            while (j < s.Length && (s[j] == '$' || s[j] == '@')) { if (s[j] == '@') verbatim = true; j++; }
            char cierre = s[j];
            j++;
            while (j < s.Length)
            {
                if (!verbatim && s[j] == '\\') { j += 2; continue; }
                if (s[j] == cierre)
                {
                    if (verbatim && cierre == '"' && j + 1 < s.Length && s[j + 1] == '"') { j += 2; continue; }
                    return j + 1;
                }
                j++;
            }
            throw new FormatException("Literal sin cerrar.");
        }

        private static bool EmpiezaLiteral(string s, int i)
            => s[i] == '"' || s[i] == '\'' ||
               ((s[i] == '$' || s[i] == '@') && i + 1 < s.Length &&
                (s[i + 1] == '"' || ((s[i + 1] == '$' || s[i + 1] == '@') && i + 2 < s.Length && s[i + 2] == '"')));

        /// <summary>
        /// Cuerpo que empieza en <paramref name="inicio"/>: un bloque "{…}" (llaves balanceadas) o una
        /// expresión "=> …;" (hasta el ';' de nivel 0). Saltea textos, caracteres y comentarios.
        /// Lanza FormatException si no se puede delimitar (el test falla, no saltea).
        /// </summary>
        private static string ExtraerCuerpo(string s, int inicio)
        {
            int i = inicio;
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
            bool esBloque;
            if (i < s.Length && s[i] == '{') esBloque = true;
            else if (i + 1 < s.Length && s[i] == '=' && s[i + 1] == '>') { esBloque = false; i += 2; }
            else throw new FormatException("Se esperaba '{' o '=>'.");

            int desde = i, nivel = 0;
            for (; i < s.Length; i++)
            {
                if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/') { int f = s.IndexOf('\n', i); i = f < 0 ? s.Length : f; continue; }
                if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '*') { int f = s.IndexOf("*/", i + 2, StringComparison.Ordinal); if (f < 0) break; i = f + 1; continue; }
                if (EmpiezaLiteral(s, i)) { i = SaltearLiteral(s, i) - 1; continue; }
                char c = s[i];
                if (c == '{' || c == '(' || c == '[') nivel++;
                else if (c == '}' || c == ')' || c == ']')
                {
                    nivel--;
                    if (nivel < 0) throw new FormatException("Cierre sin apertura.");
                    if (esBloque && nivel == 0) return s.Substring(desde, i - desde + 1);
                }
                else if (!esBloque && c == ';' && nivel == 0) return s.Substring(desde, i - desde);
            }
            throw new FormatException("Cuerpo sin cerrar.");
        }

        private static readonly HashSet<string> NoSonTipos = new HashSet<string>(StringComparer.Ordinal)
            { "return", "else", "new", "await", "throw", "using", "case", "if", "while", "for", "foreach", "switch", "lock", "catch" };

        /// <summary>Métodos declarados en todas las partes de la clase: nombre → cuerpo.</summary>
        private static Dictionary<string, string> MetodosDe(List<string> partes)
        {
            var metodos = new Dictionary<string, string>(StringComparer.Ordinal);
            var decl = new Regex(@"(?m)^[ \t]*(?:(?:private|protected|internal|public|static|override|async|virtual|new|unsafe|extern)[ \t]+)*" +
                                 @"(?<tipo>[\w.<>\[\],?]+)[ \t]+(?<nombre>\w+)[ \t]*(?:<[^>\r\n]*>)?[ \t]*\(");
            foreach (var texto in partes)
                foreach (Match m in decl.Matches(texto))
                {
                    if (NoSonTipos.Contains(m.Groups["tipo"].Value) || NoSonTipos.Contains(m.Groups["nombre"].Value)) continue;
                    // Saltear la lista de parámetros (paréntesis balanceados) y exigir '{' o '=>'.
                    int i = m.Index + m.Length, nivel = 1;
                    while (i < texto.Length && nivel > 0)
                    {
                        if (EmpiezaLiteral(texto, i)) { i = SaltearLiteral(texto, i); continue; }
                        if (texto[i] == '(') nivel++; else if (texto[i] == ')') nivel--;
                        i++;
                    }
                    int k = i;
                    while (k < texto.Length && char.IsWhiteSpace(texto[k])) k++;
                    bool cuerpo = k < texto.Length && (texto[k] == '{' || (texto[k] == '=' && k + 1 < texto.Length && texto[k + 1] == '>'));
                    if (!cuerpo) continue;   // llamada o declaración sin cuerpo: no es un método con código
                    metodos[m.Groups["nombre"].Value] = ExtraerCuerpo(texto, k);
                }
            return metodos;
        }

        /// <summary>Formularios que abre un cuerpo (new X(…)/new X{…}, is X), siguiendo hasta 3 niveles de llamadas a métodos del Menú.</summary>
        private static void FormulariosDelCuerpo(string gui, string cuerpo, Dictionary<string, string> metodos,
                                                 HashSet<string> visitados, HashSet<string> formularios, int profundidad)
        {
            foreach (Match t in Regex.Matches(cuerpo, @"\bnew\s+(\w+)\s*[\(\{]|\bis\s+(\w+)\b"))
            {
                string tipo = t.Groups[1].Success ? t.Groups[1].Value : t.Groups[2].Value;
                if (EsFormularioDeGui(gui, tipo)) formularios.Add(tipo);
            }
            if (profundidad >= 3) return;
            foreach (Match llamada in Regex.Matches(cuerpo, @"(?<![\w.])(\w+)\s*\("))
            {
                string nombre = llamada.Groups[1].Value;
                if (metodos.TryGetValue(nombre, out string sub) && visitados.Add(nombre))
                    FormulariosDelCuerpo(gui, sub, metodos, visitados, formularios, profundidad + 1);
            }
        }

        /// <summary>
        /// Ítem del menú → formularios que abre su manejador de Click, leyendo TODAS las partes de la
        /// clase Menu: suscripciones del Designer (new EventHandler(this.X)), grupos de métodos (+= X;)
        /// y lambdas ((s, e) =&gt; …), con manejadores de bloque o de expresión (=&gt;). Un manejador
        /// que no se puede leer hace fallar el test (no se saltea).
        /// </summary>
        private static Dictionary<string, HashSet<string>> FormulariosPorItemSegunElCodigo(string gui)
        {
            var partes = PartesDeLaClase(gui, "Menu");
            Assert.IsTrue(partes.Count >= 3, "Se esperaban al menos Menu.cs, Menu.Designer.cs y Menu.Ubicacion.cs.");
            var metodos = MetodosDe(partes);
            var resultado = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
            var errores = new StringBuilder();

            var suscripcion = new Regex(@"(?:this\.)?(?<item>\w+)\.Click\s*\+=\s*" +
                @"(?:new\s+System\.EventHandler\(\s*(?:this\.)?(?<metodo>\w+)\s*\)|(?<metodo>\w+)\s*;|(?<lambda>\(\s*\w+\s*,\s*\w+\s*\)\s*(?==>)))");
            foreach (var texto in partes)
                foreach (Match m in suscripcion.Matches(texto))
                {
                    string item = m.Groups["item"].Value;
                    string cuerpo;
                    try
                    {
                        if (m.Groups["lambda"].Success)
                            cuerpo = ExtraerCuerpo(texto, m.Index + m.Length);
                        else if (!metodos.TryGetValue(m.Groups["metodo"].Value, out cuerpo))
                        {
                            errores.AppendLine($"{item}: no se encontró el manejador {m.Groups["metodo"].Value} en las partes de Menu.");
                            continue;
                        }
                    }
                    catch (FormatException ex) { errores.AppendLine($"{item}: no se pudo leer el manejador ({ex.Message})."); continue; }

                    var formularios = resultado.TryGetValue(item, out var previos) ? previos : new HashSet<string>(StringComparer.Ordinal);
                    FormulariosDelCuerpo(gui, cuerpo, metodos, new HashSet<string>(StringComparer.Ordinal), formularios, 0);
                    resultado[item] = formularios;
                }

            if (errores.Length > 0) Assert.Fail("Manejadores del Menú ilegibles:\n" + errores);
            return resultado;
        }

        // ── Mapa de navegación ──────────────────────────────────────────────────

        [TestMethod]
        public void Mapa_CadaItemQueAbrePantalla_TieneSuEntrada()
        {
            var items = new HashSet<string>(BLL.MapaNavegacion.Pantallas.Select(p => p.ItemMenu));
            var faltan = BLL.MapaNavegacion.ItemsQueAbrenPantalla().Where(i => !items.Contains(i)).ToList();
            Assert.AreEqual(0, faltan.Count, "Ítems del menú sin pantalla en el catálogo: " + string.Join(", ", faltan));
        }

        [TestMethod]
        public void Mapa_CadaEntrada_ApuntaAUnItemDelMenu()
        {
            var conocidos = new HashSet<string>(BLL.MenuVisibilidad.Resolver(new string[0], esAdmin: true).Keys,
                StringComparer.Ordinal);
            foreach (var i in BLL.MapaNavegacion.ItemsSiempreVisibles) conocidos.Add(i);

            var huerfanas = BLL.MapaNavegacion.Pantallas.Where(p => !conocidos.Contains(p.ItemMenu))
                .Select(p => p.Formulario + "→" + p.ItemMenu).ToList();
            Assert.AreEqual(0, huerfanas.Count, "Entradas con un ítem que el menú no conoce: " + string.Join(", ", huerfanas));
        }

        [TestMethod]
        public void Mapa_CoincideConLosManejadoresDeMenuCs()
        {
            string gui = GuiOInconcluso();
            var segunCodigo = FormulariosPorItemSegunElCodigo(gui);
            int conFormulario = segunCodigo.Count(kv => kv.Value.Count > 0);
            Assert.IsTrue(conFormulario >= 30, "Se esperaban ≥ 30 ítems del menú que abren formularios; se leyeron " + conFormulario);

            var errores = new StringBuilder();
            foreach (var kv in segunCodigo)
                foreach (var formulario in kv.Value)
                    if (!BLL.MapaNavegacion.Pantallas.Any(p => p.Formulario == formulario && p.ItemMenu == kv.Key))
                        errores.AppendLine($"{kv.Key} abre {formulario} pero el catálogo no lo tiene.");

            // Cada entrada del catálogo tiene que estar respaldada por un manejador real que abra ese formulario.
            foreach (var p in BLL.MapaNavegacion.Pantallas)
            {
                if (!segunCodigo.TryGetValue(p.ItemMenu, out var formularios))
                    errores.AppendLine($"El catálogo usa {p.ItemMenu}, pero ninguna parte de Menu le suscribe un Click.");
                else if (!formularios.Contains(p.Formulario))
                    errores.AppendLine($"El catálogo dice que {p.ItemMenu} abre {p.Formulario}, pero su manejador abre {string.Join("/", formularios)}.");
            }

            Assert.AreEqual(0, errores.Length, errores.ToString());
        }

        [TestMethod]
        public void Mapa_PanelDeControl_CubreLosCuatroTablerosPorRol()
        {
            var tableros = BLL.MapaNavegacion.Pantallas.Where(p => p.ItemMenu == "panelControlToolStripMenuItem")
                .Select(p => p.Formulario).ToList();
            CollectionAssert.AreEquivalent(
                new[] { "DashboardForm", "DashboardVendedor", "DashboardDeposito", "DashboardLogistica" }, tableros);
        }

        [TestMethod]
        public void Mapa_CadaPantallaTienePista_EnLosCuatroIdiomas_YElFallbackEsElTextoES()
        {
            var errores = new StringBuilder();
            var es = Corpus("ES");
            var formularios = BLL.MapaNavegacion.Formularios().ToList();
            Assert.IsTrue(formularios.Count >= 33, "Se esperaban ≥ 33 pantallas en el catálogo; hay " + formularios.Count);

            foreach (var f in formularios.Concat(new[] { BLL.MapaNavegacion.Inicio }))
            {
                string clave = BLL.MapaNavegacion.ClavePista(f);
                Assert.AreEqual("ux.hint." + f, clave);
                AssertClaveEnLosCuatroIdiomas(clave, errores);
                if (es.ContainsKey(clave) && es[clave].Texto != BLL.MapaNavegacion.PistaEs(f))
                    errores.AppendLine("El fallback de " + clave + " no coincide con el texto ES del .tsv.");
            }

            string generica = BLL.MapaNavegacion.ClavePista("FormularioQueNoExiste");
            Assert.AreEqual("ux.hint.generica", generica);
            AssertClaveEnLosCuatroIdiomas(generica, errores);
            Assert.AreEqual(es[generica].Texto, BLL.MapaNavegacion.PistaEs("FormularioQueNoExiste"));
            Assert.AreEqual("ux.hint.Inicio", BLL.MapaNavegacion.ClavePista(null));

            Assert.AreEqual(0, errores.Length, errores.ToString());
        }

        [TestMethod]
        public void Mapa_ArmarRuta_UnRecorrido_SeUneConFlechas()
        {
            var r = BLL.MapaNavegacion.ArmarRuta(
                new List<IList<string>> { new List<string> { "Promociones", "Centro de decisiones comerciales" } }, "Otro título");
            Assert.AreEqual("Promociones → Centro de decisiones comerciales", r);
        }

        [TestMethod]
        public void Mapa_ArmarRuta_VariosItems_UsaLosPadresComunesYElTituloDeLaVentana()
        {
            var r = BLL.MapaNavegacion.ArmarRuta(new List<IList<string>>
            {
                new List<string> { "Auditoría", "Bitácora del Sistema" },
                new List<string> { "Auditoría", "Bitácora de Negocio" },
            }, "Bitácora");
            Assert.AreEqual("Auditoría → Bitácora", r);
        }

        [TestMethod]
        public void Mapa_ArmarRuta_SinRecorrido_UsaElTituloOInicio()
        {
            Assert.AreEqual("Mi ventana", BLL.MapaNavegacion.ArmarRuta(new List<IList<string>>(), "  Mi ventana "));
            Assert.AreEqual(BLL.MapaNavegacion.Inicio, BLL.MapaNavegacion.ArmarRuta(null, null));
        }

        // ── Tooltips ────────────────────────────────────────────────────────────

        [TestMethod]
        public void Tooltip_FormatoDeLaClave()
        {
            Assert.AreEqual("tip.PedidosVenta.btnFormalizar", BLL.MapaNavegacion.ClaveTooltip("PedidosVenta", "btnFormalizar"));
        }

        // tip.<Formulario>.<control>: el formulario empieza con mayúscula (deja afuera tip.config.backup,
        // que es un tooltip anterior asignado a mano por DashboardForm).
        private static List<string> ClavesDeTooltipPorFormulario()
            => Corpus("ES").Keys.Where(k =>
            {
                string[] p = k.Split('.');
                return p.Length == 3 && p[0] == "tip" && p[1].Length > 0 && char.IsUpper(p[1][0]);
            }).ToList();

        [TestMethod]
        public void Tooltips_EstanEnLosCuatroIdiomas()
        {
            var claves = ClavesDeTooltipPorFormulario();
            Assert.IsTrue(claves.Count >= 100, "Se esperaban ≥ 100 tooltips tip.<Formulario>.<control>; hay " + claves.Count);

            var errores = new StringBuilder();
            foreach (var c in claves) AssertClaveEnLosCuatroIdiomas(c, errores);
            Assert.AreEqual(0, errores.Length, errores.ToString());
        }

        [TestMethod]
        public void Tooltips_ApuntanAControlesQueExistenEnElFormulario()
        {
            string gui = GuiOInconcluso();
            var errores = new StringBuilder();
            var fuentes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var clave in ClavesDeTooltipPorFormulario())
            {
                string[] partes = clave.Split('.');
                string formulario = partes[1], control = partes[2];
                if (!EsFormularioDeGui(gui, formulario)) { errores.AppendLine(clave + ": no existe el formulario " + formulario); continue; }

                // Todas las partes (partial) del formulario, en GUI o en sus subcarpetas.
                if (!fuentes.TryGetValue(formulario, out string fuente))
                    fuentes[formulario] = fuente = string.Join("\n", PartesDeLaClase(gui, formulario));

                // Campo (no variable local ni retorno): con modificador de acceso y de un tipo de control.
                var m = Regex.Match(fuente,
                    @"(?m)^[ \t]*(?:readonly[ \t]+)?(?:private|protected|internal|public)(?:[ \t]+(?:readonly|internal))*[ \t]+(?<tipo>[\w.]+)[ \t]+"
                    + Regex.Escape(control) + @"[ \t]*[;=]");
                if (!m.Success)
                    errores.AppendLine(clave + ": " + formulario + " no declara el campo " + control);
                else if (!EsTipoDeControl(m.Groups["tipo"].Value))
                    errores.AppendLine(clave + ": el campo " + control + " es " + m.Groups["tipo"].Value + ", no un control ni un ítem de ToolStrip");
            }
            Assert.AreEqual(0, errores.Length, errores.ToString());
        }

        // Tipos de WinForms que admiten tooltip (Control) o ToolTipText (ToolStripItem).
        private static bool EsTipoDeControl(string tipo)
        {
            string corto = tipo.Substring(tipo.LastIndexOf('.') + 1);
            var conocidos = new HashSet<string>(StringComparer.Ordinal)
            {
                "Button", "CheckBox", "RadioButton", "LinkLabel", "Label", "TextBox", "ComboBox", "ListBox",
                "NumericUpDown", "DateTimePicker", "DataGridView", "Panel", "PictureBox", "TabPage", "TreeView",
                "ToolStripButton", "ToolStripMenuItem", "ToolStripDropDownButton", "ToolStripSplitButton",
                "ToolStripLabel", "ToolStripStatusLabel", "ToolStripComboBox"
            };
            return conocidos.Contains(corto);
        }

        [TestMethod]
        public void Tooltips_NoSeSuperponenConLosQueYaAsignaElFormulario()
        {
            // ContratacionesPendientes y las 3 de Promociones ya ponen "Actualizar" en btnRefrescar con su
            // propio ToolTip: un segundo tooltip central mostraría dos globos.
            var es = Corpus("ES");
            foreach (var f in new[] { "ContratacionesPendientesForm", "PromocionesAdministracionForm",
                                      "PromocionesContabilidadForm", "PromocionesVigentesForm" })
                Assert.IsFalse(es.ContainsKey("tip." + f + ".btnRefrescar"), f);
        }

        // ── Textos en usted (sin voseo) ────────────────────────────────────────

        private static readonly Regex Voseo = new Regex(
            @"\b(podés|tenés|querés|sabés|elegí|hacé|presioná|usá|tocá|abrí|buscá|completá|ingresá|seleccioná|revisá|mirá|andá|vos|tu|tus|te)\b",
            RegexOptions.IgnoreCase);

        [TestMethod]
        public void TextosNuevos_EnEspanol_SonEnUsted()
        {
            var errores = new StringBuilder();
            foreach (var kv in Corpus("ES").Where(k => k.Key.StartsWith("ux.") || (k.Key.StartsWith("tip.") && k.Key.Split('.').Length == 3)))
                if (Voseo.IsMatch(kv.Value.Texto))
                    errores.AppendLine(kv.Key + ": " + Voseo.Match(kv.Value.Texto).Value);
            Assert.AreEqual(0, errores.Length, "Textos con voseo o tuteo:\n" + errores);
        }

        [TestMethod]
        public void AyudaGeneral_TextoDeLaVentanaPrincipal_FallbackIgualAlES()
        {
            var es = Corpus("ES");
            Assert.IsTrue(es.ContainsKey("ux.ayuda.ventana"));
            var errores = new StringBuilder();
            AssertClaveEnLosCuatroIdiomas("ux.ayuda.ventana", errores);
            Assert.AreEqual(0, errores.Length, errores.ToString());

            string gui = GuiOInconcluso();
            string fuente = File.ReadAllText(Path.Combine(gui, "Ayuda", "AyudaEnLinea.cs"));
            var lineas = es["ux.ayuda.ventana"].Texto.Split('\n');
            for (int i = 0; i < lineas.Length; i++)
            {
                string literal = "\"" + lineas[i] + (i < lineas.Length - 1 ? "\\n\"" : "\";");
                StringAssert.Contains(fuente, literal, "El fallback de AyudaEnLinea difiere del .tsv en: " + lineas[i]);
            }
        }

        // ── Guía de primer uso: pasos por rol ──────────────────────────────────

        [TestMethod]
        public void Guia_CadaRol_TieneEntreTresYCincoPasos_EnLosCuatroIdiomas()
        {
            var es = Corpus("ES");
            var errores = new StringBuilder();
            var roles = BLL.GuiaPrimerUso.RolesConGuia.ToList();
            CollectionAssert.IsSubsetOf(new[] { "administrador", "vendedor", "caja", "deposito", "operadorlogistico",
                "gerentecomercial", "gerenteinventario", "administracioncomercial", "contabilidad", "auditor", "generico" }, roles);

            foreach (var rol in roles)
            {
                var pasos = BLL.GuiaPrimerUso.PasosPara(rol, esAdministrador: false);
                Assert.IsTrue(pasos.Count >= 3 && pasos.Count <= 5, rol + ": " + pasos.Count + " pasos");
                Assert.IsTrue(pasos.All(p => !string.IsNullOrWhiteSpace(p.TextoEs)), rol);
                foreach (var p in pasos)
                {
                    AssertClaveEnLosCuatroIdiomas(p.Clave, errores);
                    if (es.ContainsKey(p.Clave) && es[p.Clave].Texto != p.TextoEs)
                        errores.AppendLine("El fallback de " + p.Clave + " no coincide con el texto ES del .tsv.");
                }
            }
            foreach (var clave in new[] { "ux.guia.titulo", "ux.guia.intro", "ux.guia.nomostrar", "ux.guia.comenzar", "ux.mnu.guia" })
                AssertClaveEnLosCuatroIdiomas(clave, errores);
            Assert.AreEqual(0, errores.Length, errores.ToString());
        }

        [TestMethod]
        public void Guia_PasosDelRol_IncluyenDondeEmpezar_LaBarraYF1()
        {
            var pasos = BLL.GuiaPrimerUso.PasosPara("Vendedor", esAdministrador: false);
            Assert.AreEqual("ux.guia.vendedor.1", pasos[0].Clave);
            StringAssert.Contains(pasos[1].TextoEs, "Pedidos de Venta");
            Assert.AreEqual("ux.guia.comun.barra", pasos[pasos.Count - 2].Clave);
            Assert.AreEqual("ux.guia.comun.f1", pasos[pasos.Count - 1].Clave);
            StringAssert.Contains(pasos[pasos.Count - 1].TextoEs, "F1");
        }

        [TestMethod]
        public void Guia_ClaveRol_NormalizaElPerfil()
        {
            Assert.AreEqual("gerentecomercial", BLL.GuiaPrimerUso.ClaveRol("Gerente Comercial", false));
            Assert.AreEqual("deposito", BLL.GuiaPrimerUso.ClaveRol("Depósito", false));
            Assert.AreEqual("operadorlogistico", BLL.GuiaPrimerUso.ClaveRol("OperadorLogistico", false));
            Assert.AreEqual("administrador", BLL.GuiaPrimerUso.ClaveRol("Vendedor", esAdministrador: true),
                "Un Administrador siempre ve la guía de administrador.");
            Assert.AreEqual("generico", BLL.GuiaPrimerUso.ClaveRol("Rol creado a mano", false));
            Assert.AreEqual("generico", BLL.GuiaPrimerUso.ClaveRol(null, false));
        }

        [TestMethod]
        public void Guia_SeSuprimeConSilentSinGuiaOLaVariableDeEntorno()
        {
            Assert.IsTrue(BLL.GuiaPrimerUso.SuprimidaPorEntorno(new[] { "/silent" }, null));
            Assert.IsTrue(BLL.GuiaPrimerUso.SuprimidaPorEntorno(new[] { "-SinGuia" }, null));
            Assert.IsTrue(BLL.GuiaPrimerUso.SuprimidaPorEntorno(new string[0], "1"));
            Assert.IsFalse(BLL.GuiaPrimerUso.SuprimidaPorEntorno(new[] { "/otro" }, ""));
            Assert.IsFalse(BLL.GuiaPrimerUso.SuprimidaPorEntorno(null, null));
        }

        // ── Guía de primer uso: preferencia "No volver a mostrar" ──────────────

        private string _carpeta;

        [TestInitialize]
        public void CrearCarpeta()
        {
            _carpeta = Path.Combine(Path.GetTempPath(), "wf_guia_" + Guid.NewGuid().ToString("N"));
        }

        [TestCleanup]
        public void BorrarCarpeta()
        {
            try { if (Directory.Exists(_carpeta)) Directory.Delete(_carpeta, true); } catch { }
        }

        [TestMethod]
        public void Preferencia_PorDefecto_LaGuiaSeMuestra_SinCrearArchivo()
        {
            var pref = new BLL.GuiaPrimerUso(_carpeta);
            Assert.IsTrue(pref.DebeMostrarse("vendedor1"));
            Assert.IsFalse(File.Exists(pref.RutaArchivo), "Leer no debe crear el archivo.");
            Assert.IsFalse(pref.DebeMostrarse(""), "Sin usuario no hay guía.");
            Assert.IsFalse(pref.DebeMostrarse(null));
        }

        [TestMethod]
        public void Preferencia_NoVolverAMostrar_SePersiste_YEsPorUsuario()
        {
            new BLL.GuiaPrimerUso(_carpeta).NoVolverAMostrar("vendedor1");

            var otraInstancia = new BLL.GuiaPrimerUso(_carpeta);
            Assert.IsFalse(otraInstancia.DebeMostrarse("vendedor1"), "Se guarda en el archivo, no en memoria.");
            Assert.IsFalse(otraInstancia.DebeMostrarse("  VENDEDOR1 "), "El usuario se normaliza (espacios y mayúsculas).");
            Assert.IsTrue(otraInstancia.DebeMostrarse("caja1"), "Otro usuario todavía no la vio.");
        }

        [TestMethod]
        public void Preferencia_VolverAMostrar_LaReactiva_SinAfectarAOtros()
        {
            var pref = new BLL.GuiaPrimerUso(_carpeta);
            pref.NoVolverAMostrar("vendedor1");
            pref.NoVolverAMostrar("caja1");
            pref.NoVolverAMostrar("caja1"); // idempotente

            pref.VolverAMostrar("vendedor1");

            Assert.IsTrue(pref.DebeMostrarse("vendedor1"));
            Assert.IsFalse(pref.DebeMostrarse("caja1"));
            Assert.AreEqual(1, File.ReadAllLines(pref.RutaArchivo).Count(l => l.Trim().Length > 0));
        }

        [TestMethod]
        public void Preferencia_ElArchivoNoGuardaElNombreDelUsuario()
        {
            var pref = new BLL.GuiaPrimerUso(_carpeta);
            pref.NoVolverAMostrar("gerente.comercial");
            string contenido = File.ReadAllText(pref.RutaArchivo);
            Assert.IsFalse(contenido.IndexOf("gerente", StringComparison.OrdinalIgnoreCase) >= 0);
            StringAssert.Matches(contenido.Trim(), new Regex("^[0-9a-f]{64}$"));
        }

        [TestMethod]
        public void Preferencia_ArchivoIlegible_NoRompe_YOfreceLaGuia()
        {
            Directory.CreateDirectory(_carpeta);
            var pref = new BLL.GuiaPrimerUso(_carpeta);
            // Una carpeta con el nombre del archivo: leer y escribir fallan, pero nunca lanzan.
            Directory.CreateDirectory(pref.RutaArchivo);
            Assert.IsTrue(pref.DebeMostrarse("vendedor1"));
            pref.NoVolverAMostrar("vendedor1");
            pref.VolverAMostrar("vendedor1");
        }

        [TestMethod]
        public void Preferencia_ArchivoEnUso_NoSeReescribe_YNoBorraLasMarcasDeOtros()
        {
            var pref = new BLL.GuiaPrimerUso(_carpeta);
            pref.NoVolverAMostrar("caja1");
            string antes = File.ReadAllText(pref.RutaArchivo);

            // Otro proceso tiene el archivo abierto en exclusiva: no se puede leer.
            using (new FileStream(pref.RutaArchivo, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.IsTrue(pref.DebeMostrarse("caja1"), "Ilegible: se ofrece la guía (valor por defecto).");
                pref.NoVolverAMostrar("vendedor1");   // no debe reescribir el archivo con solo "vendedor1"
                pref.VolverAMostrar("caja1");
            }

            Assert.AreEqual(antes, File.ReadAllText(pref.RutaArchivo), "Las marcas existentes no se tocan.");
            Assert.IsFalse(pref.DebeMostrarse("caja1"));
            Assert.IsTrue(pref.DebeMostrarse("vendedor1"));
        }

        [TestMethod]
        public void Preferencia_EscrituraAtomica_NoDejaTemporales()
        {
            var pref = new BLL.GuiaPrimerUso(_carpeta);
            pref.NoVolverAMostrar("a");
            pref.NoVolverAMostrar("b");
            pref.VolverAMostrar("a");
            Assert.AreEqual(0, Directory.GetFiles(_carpeta, "*.tmp").Length);
            Assert.IsTrue(pref.DebeMostrarse("a"));
            Assert.IsFalse(pref.DebeMostrarse("b"));
        }

        // ── Barra: formato tolerante y diálogos del menú ───────────────────────

        [TestMethod]
        public void FormatoSeguro_PlantillaRota_UsaLaDeFabrica()
        {
            Assert.AreEqual("Usted está en: Ventas",
                BLL.MapaNavegacion.FormatoSeguro("Usted está en: {0", "Usted está en: {0}", "Ventas"));
            Assert.AreEqual("You are in: Ventas",
                BLL.MapaNavegacion.FormatoSeguro("You are in: {0}", "Usted está en: {0}", "Ventas"));
        }

        [TestMethod]
        public void FormatoSeguro_AmbasRotasOFaltanArgumentos_NoLanza()
        {
            Assert.AreEqual("ana · Caja", BLL.MapaNavegacion.FormatoSeguro("{0} · {2}", "{x}", "ana", "Caja"));
            Assert.AreEqual("ana", BLL.MapaNavegacion.FormatoSeguro(null, null, "ana"));
            Assert.AreEqual("Inicio", BLL.MapaNavegacion.FormatoSeguro("Inicio", null));
        }

        [TestMethod]
        public void DialogosDelMenu_SoloLosModalesDelCatalogo()
        {
            foreach (var f in new[] { "NuevaContratacionForm", "SugerirPromocionForm", "BackupForm", "MiPerfilForm", "GuiaPrimerUsoForm" })
                Assert.IsTrue(BLL.MapaNavegacion.EsDialogoDelMenu(f), f);
            // Ventanas MDI del menú y diálogos que abren las ventanas hijas: no cuentan.
            foreach (var f in new[] { "Clientes", "PedidosVenta", "ClienteForm", "CambioEstadoDialog", "ConfirmarAdminForm", "InputDialog" })
                Assert.IsFalse(BLL.MapaNavegacion.EsDialogoDelMenu(f), f);
        }

        // ── Secuencia: lo que va después de la guía ────────────────────────────

        [TestMethod]
        public void Secuencia_EncolaHastaResolver_YDespachaEnOrden()
        {
            var log = new List<string>();
            var s = new BLL.SecuenciaDespuesDeLaGuia(a => a());
            s.Agregar(() => log.Add("aviso"));
            s.Agregar(() => log.Add("otro"));
            Assert.AreEqual(0, log.Count, "Mientras la guía no se resolvió, nada se ejecuta.");

            s.Resolver();
            CollectionAssert.AreEqual(new[] { "aviso", "otro" }, log);

            s.Resolver();   // idempotente
            Assert.AreEqual(2, log.Count);
        }

        [TestMethod]
        public void Secuencia_DespuesDeResolver_DespachaEnElMomento_PorElDespachador()
        {
            var despachadas = new List<Action>();
            var s = new BLL.SecuenciaDespuesDeLaGuia(a => despachadas.Add(a));   // como BeginInvoke: diferido
            s.Resolver();
            bool corrio = false;
            s.Agregar(() => corrio = true);
            Assert.AreEqual(1, despachadas.Count, "Se entrega al despachador (BeginInvoke en el Menú).");
            Assert.IsFalse(corrio);
            despachadas[0]();
            Assert.IsTrue(corrio);
        }

        [TestMethod]
        public void Secuencia_UnaAccionQueFalla_NoCortaLasSiguientes()
        {
            var log = new List<string>();
            var s = new BLL.SecuenciaDespuesDeLaGuia(a => a());
            s.Agregar(() => throw new InvalidOperationException("aviso roto"));
            s.Agregar(() => log.Add("siguiente"));
            s.Resolver();
            CollectionAssert.AreEqual(new[] { "siguiente" }, log);
            Assert.IsTrue(s.Resuelta);
        }

        // ── Cola: lo encolado espera a que no haya un diálogo modal abierto ──────

        [TestMethod]
        public void ColaVentanaLibre_ConUnModalAbierto_EsperaYDespuesEjecutaEnOrden()
        {
            // Caso del login: la guía no se muestra (la secuencia se resuelve en el acto) mientras el
            // aviso modal del backup sigue abierto; el aviso comercial tiene que esperar a que se cierre.
            bool modalAbierto = true;
            var log = new List<string>();
            var cola = new BLL.ColaHastaVentanaLibre(() => !modalAbierto);
            var s = new BLL.SecuenciaDespuesDeLaGuia(a => { cola.Agregar(a); cola.Procesar(); });
            s.Agregar(() => log.Add("aviso"));
            s.Agregar(() => log.Add("otro"));
            s.Resolver();

            Assert.AreEqual(0, log.Count, "Con un modal abierto no se ejecuta nada.");
            Assert.AreEqual(2, cola.Pendientes);
            Assert.IsFalse(cola.Procesar(), "Sigue esperando (el Menú vuelve a mirar cada 400 ms).");

            modalAbierto = false;
            Assert.IsTrue(cola.Procesar());
            CollectionAssert.AreEqual(new[] { "aviso", "otro" }, log);
            Assert.AreEqual(0, cola.Pendientes);
        }

        [TestMethod]
        public void ColaVentanaLibre_SiUnaAccionAbreUnModal_LasSiguientesEsperan()
        {
            bool modalAbierto = false;
            var log = new List<string>();
            var cola = new BLL.ColaHastaVentanaLibre(() => !modalAbierto);
            cola.Agregar(() => { log.Add("abre modal"); modalAbierto = true; });
            cola.Agregar(() => log.Add("siguiente"));

            Assert.IsFalse(cola.Procesar());
            CollectionAssert.AreEqual(new[] { "abre modal" }, log);

            modalAbierto = false;
            Assert.IsTrue(cola.Procesar());
            CollectionAssert.AreEqual(new[] { "abre modal", "siguiente" }, log);
        }

        [TestMethod]
        public void ColaVentanaLibre_NoLanza_SiFallaUnaAccionOLaCondicion()
        {
            var log = new List<string>();
            var cola = new BLL.ColaHastaVentanaLibre(() => throw new InvalidOperationException("handle destruido"));
            cola.Agregar(() => throw new InvalidOperationException("aviso roto"));
            cola.Agregar(() => log.Add("siguiente"));
            cola.Agregar(null);

            Assert.IsTrue(cola.Procesar(), "Si no se puede mirar la ventana, se toma como libre.");
            CollectionAssert.AreEqual(new[] { "siguiente" }, log);
        }
    }
}
