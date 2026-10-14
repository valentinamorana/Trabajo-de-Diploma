using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// T07 — Recuperación de integridad asistida por el espejo (BLL.RecuperacionIntegridad).
    /// El núcleo del diagnóstico (RecuperacionIntegridad.Evaluar) se ejercita con filas armadas a mano
    /// y el calculador de DV real: clasificación del daño (modificación, DVH corrupto, inserción externa,
    /// eliminación física, vaciado, reordenamiento) y qué opciones de recuperación quedan habilitadas.
    /// Además, que Reparar / Asumir pérdida exijan un Administrador con sesión antes de tocar la base.
    /// </summary>
    [TestClass]
    public class RecuperacionIntegridadTests
    {
        private static readonly Seguridad.ICalculadorDV Svc = Seguridad.CalculadorDV.Crear();

        [TestInitialize] public void Setup()   => Seguridad.SessionManager.Logout();
        [TestCleanup]    public void Cleanup() => Seguridad.SessionManager.Logout();

        // Fila íntegra: DVH almacenado = recalculado.
        private static BE.FilaUsuarioDV Fila(int id, string username, string rol = "Vendedor")
        {
            var f = new BE.FilaUsuarioDV
            {
                Id = id, Username = username, Clave = "hash-" + username, Rol = rol, Perfil = rol,
                Estado = "Activo", IntentosFallidos = "0", Activo = "1", RequiereCambioClave = "0",
                CantidadBloqueos = "0", FechaBloqueo = ""
            };
            f.DVHAlmacenado = Svc.CalcularDVH(f.CamposParaDVH());
            return f;
        }

        private static BE.FilaUsuarioDV Copia(BE.FilaUsuarioDV f) => new BE.FilaUsuarioDV
        {
            Id = f.Id, Username = f.Username, Clave = f.Clave, Rol = f.Rol, Perfil = f.Perfil, Estado = f.Estado,
            IntentosFallidos = f.IntentosFallidos, Activo = f.Activo, RequiereCambioClave = f.RequiereCambioClave,
            CantidadBloqueos = f.CantidadBloqueos, FechaBloqueo = f.FechaBloqueo, DVHAlmacenado = f.DVHAlmacenado
        };

        // Simula una edición por SQL que además "arregla" el DVH de la fila (el atacante conoce la fórmula):
        // solo el espejo permite saber qué cambió.
        private static void ResellarDvh(BE.FilaUsuarioDV f) => f.DVHAlmacenado = Svc.CalcularDVH(f.CamposParaDVH());

        private static int Dvv(IEnumerable<BE.FilaUsuarioDV> filas)
            => Svc.CalcularDVV(filas.Select(f => Svc.CalcularDVH(f.CamposParaDVH())).ToList());

        private static List<BE.FilaUsuarioDV> Base()
            => new List<BE.FilaUsuarioDV> { Fila(1, "admin", "Administrador"), Fila(2, "vendedor1"), Fila(3, "deposito1", "Deposito") };

        // Diagnóstico con el DVV almacenado "legítimo" (el del espejo) salvo que se indique otro.
        private static BLL.DiagnosticoEspejo Evaluar(List<BE.FilaUsuarioDV> actuales, List<BE.FilaUsuarioDV> espejo,
                                                     int? dvvAlmacenado = null, bool usarDvvDeActuales = false)
        {
            int? dvv = dvvAlmacenado ?? (usarDvvDeActuales ? Dvv(actuales) : Dvv(espejo));
            return BLL.RecuperacionIntegridad.Evaluar(new BLL.DiagnosticoEspejo(), actuales, espejo, dvv, Svc);
        }

        // ── Caso sano ───────────────────────────────────────────────────────

        [TestMethod]
        public void Evaluar_DatosIgualesAlEspejo_Integro()
        {
            var espejo = Base();
            var d = Evaluar(espejo.Select(Copia).ToList(), espejo);

            Assert.IsTrue(d.EspejoDisponible);
            Assert.IsTrue(d.DvvOk);
            Assert.IsTrue(d.Integro);
            Assert.AreEqual(0, d.Alteradas.Count);
            Assert.AreEqual(0, d.Faltantes.Count);
            Assert.IsFalse(d.PuedeReparar, "Nada que reparar.");
            Assert.IsTrue(d.PuedeAsumirPerdida);
            Assert.IsTrue(d.PuedeRestaurarBackup);
            Assert.AreEqual(1, d.Resumen.Count);
            StringAssert.Contains(d.Resumen[0], "Integridad verificada");
        }

        [TestMethod]
        public void Evaluar_NullYVacioSonEquivalentes_NoMarcaAlteracion()
        {
            // FechaBloqueo NULL en un lado y "" en el otro no es una alteración (CamposParaDVH usa ?? "").
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales[1].FechaBloqueo = null;

            var d = Evaluar(actuales, espejo);

            Assert.AreEqual(0, d.Alteradas.Count);
            Assert.IsTrue(d.Integro);
        }

        // ── Sin espejo / tabla vaciada ──────────────────────────────────────

        [TestMethod]
        public void Evaluar_SinEspejo_SoloPermiteRecalcularOBackup()
        {
            var actuales = Base();
            var d = Evaluar(actuales, new List<BE.FilaUsuarioDV>(), usarDvvDeActuales: true);

            Assert.IsFalse(d.EspejoDisponible);
            Assert.IsFalse(d.PuedeReparar, "Sin espejo no hay valores legítimos a los que volver.");
            Assert.IsTrue(d.PuedeAsumirPerdida);
            Assert.IsTrue(d.PuedeRestaurarBackup);
            Assert.AreEqual(0, d.Alteradas.Count, "Sin espejo no se diagnostica por campo.");
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("No hay un espejo")));
        }

        [TestMethod]
        public void Evaluar_SinEspejo_IgualCalculaElDvv()
        {
            var actuales = Base();
            var d = Evaluar(actuales, new List<BE.FilaUsuarioDV>(), dvvAlmacenado: Dvv(actuales) + 1);

            Assert.AreEqual(Dvv(actuales), d.DvvCalculado);
            Assert.IsFalse(d.DvvOk);
            Assert.IsFalse(d.Integro);
        }

        [TestMethod]
        public void Evaluar_TablaVaciada_SoloBackup()
        {
            var espejo = Base();
            var d = Evaluar(new List<BE.FilaUsuarioDV>(), espejo);

            Assert.IsTrue(d.TablaVaciada);
            Assert.IsFalse(d.Integro);
            Assert.IsFalse(d.PuedeReparar, "No se reinsertan filas desde el espejo.");
            Assert.IsFalse(d.PuedeAsumirPerdida, "Recalcular sobre 0 filas sellaría la pérdida total.");
            Assert.IsTrue(d.PuedeRestaurarBackup);
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("vaciada")));
        }

        // ── Clasificación del daño ──────────────────────────────────────────

        [TestMethod]
        public void Evaluar_RolModificadoYDvhResellado_DetectaCampoYPermiteReparar()
        {
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales[1].Rol = "Administrador";   // escalada de privilegios por SQL
            ResellarDvh(actuales[1]);

            var d = Evaluar(actuales, espejo);

            Assert.AreEqual(1, d.Alteradas.Count);
            var a = d.Alteradas[0];
            Assert.AreEqual(2, a.Id);
            Assert.AreEqual("vendedor1", a.Username);
            Assert.AreEqual(BLL.TipoAlteracion.Modificada, a.Tipo);
            Assert.AreEqual(1, a.Campos.Count);
            Assert.AreEqual("Rol", a.Campos[0].Campo);
            Assert.AreEqual("Administrador", a.Campos[0].ValorActual);
            Assert.AreEqual("Vendedor", a.Campos[0].ValorEsperado);
            Assert.IsFalse(d.DvvOk, "El DVV almacenado (legítimo) ya no coincide con los datos.");
            Assert.IsTrue(d.PuedeReparar);
            Assert.IsTrue(d.PuedeAsumirPerdida);
            Assert.IsFalse(d.Integro);
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("Modificación externa") && r.Contains("1 campo(s)")));
        }

        [TestMethod]
        public void Evaluar_VariosCamposModificados_LosListaTodos()
        {
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales[2].Activo = "0";
            actuales[2].RequiereCambioClave = "1";
            actuales[2].CantidadBloqueos = "3";
            actuales[2].FechaBloqueo = "2026-10-13 10:00:00";

            var d = Evaluar(actuales, espejo);

            var campos = d.Alteradas.Single().Campos.Select(c => c.Campo).ToList();
            CollectionAssert.AreEquivalent(
                new[] { "Activo", "RequiereCambioClave", "CantidadBloqueos", "FechaBloqueo" }, campos);
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("4 campo(s)")));
        }

        [TestMethod]
        public void Evaluar_ClaveModificada_SeInformaEnmascaradaSinFiltrarElHash()
        {
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales[0].Clave = "hash-del-atacante";

            var d = Evaluar(actuales, espejo);

            var c = d.Alteradas.Single().Campos.Single();
            Assert.AreEqual("Clave (hash)", c.Campo);
            Assert.IsFalse(c.ValorActual.Contains("hash-del-atacante"), "No debe mostrarse el hash actual.");
            Assert.IsFalse(c.ValorEsperado.Contains("hash-admin"), "No debe mostrarse el hash legítimo.");
        }

        [TestMethod]
        public void Evaluar_SoloDvhAlteradoAMano_ClasificaDvhCorrupto()
        {
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales[1].DVHAlmacenado = actuales[1].DVHAlmacenado + 7;

            var d = Evaluar(actuales, espejo);

            var a = d.Alteradas.Single();
            Assert.AreEqual(BLL.TipoAlteracion.DVHCorrupto, a.Tipo);
            Assert.AreEqual(0, a.Campos.Count, "Los datos coinciden con el espejo.");
            Assert.IsTrue(d.DvvOk, "El DVV se recalcula sobre los datos, que no cambiaron.");
            Assert.IsTrue(d.PuedeReparar);
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("DVH corrompido")));
        }

        [TestMethod]
        public void Evaluar_DvhNuloConDatosIntactos_ClasificaDvhCorrupto()
        {
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales[0].DVHAlmacenado = null;

            var d = Evaluar(actuales, espejo);

            Assert.AreEqual(BLL.TipoAlteracion.DVHCorrupto, d.Alteradas.Single().Tipo);
        }

        [TestMethod]
        public void Evaluar_FilaQueNoEstaEnElEspejo_InsercionExterna()
        {
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales.Add(Fila(99, "intruso", "Administrador"));   // con DVH "correcto"

            var d = Evaluar(actuales, espejo, usarDvvDeActuales: true);

            var a = d.Alteradas.Single();
            Assert.AreEqual(99, a.Id);
            Assert.AreEqual(BLL.TipoAlteracion.InsertadaExterna, a.Tipo);
            Assert.IsTrue(d.DvvOk, "Aunque el atacante resella el DVV, el espejo delata la inserción.");
            Assert.IsFalse(d.Integro);
            Assert.IsTrue(d.PuedeReparar, "Reparar borra la fila insertada.");
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("Inserción externa") && r.Contains("intruso")));
        }

        [TestMethod]
        public void Evaluar_FilaEliminada_InformaFaltanteYNoPermiteReparar()
        {
            var espejo = Base();
            var actuales = espejo.Where(f => f.Id != 3).Select(Copia).ToList();

            var d = Evaluar(actuales, espejo);

            Assert.AreEqual(3, d.Faltantes.Single().Id);
            Assert.AreEqual(0, d.Alteradas.Count);
            Assert.IsFalse(d.TablaVaciada);
            Assert.IsFalse(d.Integro);
            Assert.IsFalse(d.PuedeReparar, "Reparar no reinserta filas (IDENTITY/FKs): hace falta un backup.");
            Assert.IsTrue(d.PuedeAsumirPerdida);
            Assert.IsTrue(d.PuedeRestaurarBackup);
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("ELIMINACIÓN") && r.Contains("1 registro")));
        }

        [TestMethod]
        public void Evaluar_EliminacionMasModificacion_SigueSinPermitirReparar()
        {
            var espejo = Base();
            var actuales = espejo.Where(f => f.Id != 3).Select(Copia).ToList();
            actuales[0].Estado = "Bloqueado";

            var d = Evaluar(actuales, espejo);

            Assert.AreEqual(1, d.Faltantes.Count);
            Assert.AreEqual(1, d.Alteradas.Count);
            Assert.IsFalse(d.PuedeReparar, "Con eliminación física, la reparación parcial dejaría la tabla incompleta.");
        }

        [TestMethod]
        public void Evaluar_SoloDvvDistinto_ReordenamientoReparable()
        {
            var espejo = Base();
            var d = Evaluar(espejo.Select(Copia).ToList(), espejo, dvvAlmacenado: Dvv(espejo) + 1);

            Assert.AreEqual(0, d.Alteradas.Count);
            Assert.AreEqual(0, d.Faltantes.Count);
            Assert.IsFalse(d.DvvOk);
            Assert.IsFalse(d.Integro);
            Assert.IsTrue(d.PuedeReparar);
            Assert.IsTrue(d.Resumen.Any(r => r.Contains("DVV")));
        }

        [TestMethod]
        public void Evaluar_DvvNoAlmacenado_NoEsIntegro()
        {
            var espejo = Base();
            var d = BLL.RecuperacionIntegridad.Evaluar(new BLL.DiagnosticoEspejo(),
                        espejo.Select(Copia).ToList(), espejo, null, Svc);

            Assert.IsNull(d.DvvAlmacenado);
            Assert.IsFalse(d.DvvOk);
            Assert.IsFalse(d.Integro);
        }

        [TestMethod]
        public void Evaluar_OtraTablaCorrupta_UsuarioSano_NoDiceIntegroYPermiteAsumirPerdida()
        {
            // Diagnosticar carga primero las otras tablas protegidas en el mismo DiagnosticoEspejo.
            var d0 = new BLL.DiagnosticoEspejo();
            d0.OtrasTablasCorruptas.Add("Cliente: 1 fila(s) con DVH inválido");
            d0.Resumen.Add("Los dígitos verificadores de otras tablas protegidas no coinciden (Cliente).");
            var espejo = Base();

            var d = BLL.RecuperacionIntegridad.Evaluar(d0, espejo.Select(Copia).ToList(), espejo, Dvv(espejo), Svc);

            Assert.IsFalse(d.Integro);
            Assert.IsFalse(d.Resumen.Any(r => r.Contains("Integridad verificada")),
                           "No puede afirmar integridad cuando otra tabla falla.");
            Assert.IsTrue(d.PuedeAsumirPerdida);
            Assert.IsFalse(d.PuedeReparar, "El espejo es solo de Usuario: no repara otras tablas.");
        }

        [TestMethod]
        public void Evaluar_ResumenUnaLineaPorAlteracion()
        {
            var espejo = Base();
            var actuales = espejo.Select(Copia).ToList();
            actuales[0].Perfil = "Otro";
            actuales[1].DVHAlmacenado = -1;
            actuales.Add(Fila(50, "nuevo"));

            var d = Evaluar(actuales, espejo, usarDvvDeActuales: true);

            Assert.AreEqual(3, d.Alteradas.Count);
            CollectionAssert.AreEquivalent(
                new[] { BLL.TipoAlteracion.Modificada, BLL.TipoAlteracion.DVHCorrupto, BLL.TipoAlteracion.InsertadaExterna },
                d.Alteradas.Select(a => a.Tipo).ToList());
            Assert.AreEqual(3, d.Resumen.Count);
        }

        // ── Guardas de las acciones (antes de tocar la base) ────────────────

        // MSTest v1 no trae Assert.ThrowsException.
        private static T Lanza<T>(Action accion) where T : Exception
        {
            try { accion(); }
            catch (T ex) { return ex; }
            Assert.Fail("Se esperaba una excepción " + typeof(T).Name + ".");
            return null;
        }

        [TestMethod]
        public void RepararDesdeEspejo_SinSesion_Rechaza()
        {
            var ex = Lanza<BE.AppException>(() => BLL.RecuperacionIntegridad.RepararDesdeEspejo());
            Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
        }

        [TestMethod]
        public void RepararDesdeEspejo_NoAdministrador_RechazaYRegistraEnBitacora()
        {
            Seguridad.SessionManager.Login(new BE.Usuario { Id = 7, Username = "vendedor1", Perfil = "Vendedor" });
            int antes;
            lock (ConfiguracionTests.Bitacora.Registros) antes = ConfiguracionTests.Bitacora.Registros.Count;

            var ex = Lanza<BE.AppException>(() => BLL.RecuperacionIntegridad.RepararDesdeEspejo());

            Assert.AreEqual("err.bll.dv.sin_permiso", ex.Clave);
            lock (ConfiguracionTests.Bitacora.Registros)
                Assert.IsTrue(ConfiguracionTests.Bitacora.Registros.Skip(antes)
                    .Any(r => r.Actividad.Contains("DENEGADO") && r.Criticidad == BE.Criticidad.Alta));
        }

        [TestMethod]
        public void AsumirPerdida_SinSesion_Rechaza()
        {
            var ex = Lanza<BE.AppException>(() => BLL.RecuperacionIntegridad.AsumirPerdida());
            Assert.AreEqual("err.bll.sesion_expirada", ex.Clave);
        }

        [TestMethod]
        public void AsumirPerdida_NoAdministrador_Rechaza()
        {
            Seguridad.SessionManager.Login(new BE.Usuario { Id = 7, Username = "vendedor1", Perfil = "Vendedor" });
            var ex = Lanza<BE.AppException>(() => BLL.RecuperacionIntegridad.AsumirPerdida());
            Assert.AreEqual("err.bll.dv.sin_permiso", ex.Clave);
        }
    }
}
