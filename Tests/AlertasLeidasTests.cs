using System;
using System.Collections.Generic;
using System.Linq;
using BLL;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tests
{
    /// <summary>
    /// 1.6.1: Alertas leídas por usuario (BLL.AlertasLeidas): las alertas se recalculan siempre, así que se
    /// guarda la firma de la situación que el usuario vio. Leída = no cuenta en «Alertas (N)» mientras la
    /// situación no cambie.
    /// </summary>
    [TestClass]
    public class AlertasLeidasTests
    {
        // ParametroSistema en memoria, con el mismo contrato que DAL.InteligenciaComercial (clave sin prefijo).
        private sealed class ParametrosEnMemoria : DAL.Interfaces.IParametrosSistemaDAL
        {
            public readonly Dictionary<string, string> Filas = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public bool Fallar;
            public int Escrituras;

            public Dictionary<string, string> ObtenerParametros(string prefijo)
            {
                if (Fallar) throw new Exception("base caída");
                return Filas.Where(f => f.Key.StartsWith(prefijo, StringComparison.OrdinalIgnoreCase))
                            .ToDictionary(f => f.Key.Substring(prefijo.Length), f => f.Value, StringComparer.OrdinalIgnoreCase);
            }

            public void GuardarParametros(string prefijo, Dictionary<string, string> valores)
            {
                if (Fallar) throw new Exception("base caída");
                Escrituras++;
                foreach (var kv in valores) Filas[prefijo + kv.Key] = kv.Value;
            }
        }

        private static BE.Alerta Vencidas(int n) =>
            new BE.Alerta(BE.NivelAlerta.Critica, "alert.subs.vencidas", "{0} suscripción(es) vencida(s).", n, n);
        private static BE.Alerta Backup(int dias) =>
            new BE.Alerta(BE.NivelAlerta.Advertencia, "alert.backup.dias", "Hace {0} día(s)…", dias, dias);

        [TestMethod]
        public void SinMarcas_TodasSonNoLeidas()
        {
            var r = new AlertasLeidas(new ParametrosEnMemoria()).Aplicar(5, new[] { Vencidas(3), Backup(8) });
            Assert.IsTrue(r.All(a => !a.Leida));
        }

        [TestMethod]
        public void MarcarLeida_MismaSituacion_QuedaLeida_YNoCuenta()
        {
            var dal = new ParametrosEnMemoria();
            var leidas = new AlertasLeidas(dal);
            leidas.MarcarLeidas(5, new[] { Vencidas(3) });

            var r = leidas.Aplicar(5, new[] { Vencidas(3), Backup(8) });
            Assert.IsTrue(r.Single(a => a.ClaveI18n == "alert.subs.vencidas").Leida);
            Assert.IsFalse(r.Single(a => a.ClaveI18n == "alert.backup.dias").Leida);
            Assert.AreEqual(1, r.Count(a => !a.Leida), "El aviso del menú cuenta solo la no leída.");
            Assert.AreEqual("3|3", dal.Filas["Alerta.Leida.5.alert.subs.vencidas"], "Se guarda la firma (cantidad|parámetros) por usuario.");
        }

        [TestMethod]
        public void SiCambiaLaSituacion_VuelveANotificar()
        {
            var leidas = new AlertasLeidas(new ParametrosEnMemoria());
            leidas.MarcarLeidas(5, new[] { Vencidas(3), Backup(8) });

            var r = leidas.Aplicar(5, new[] { Vencidas(4), Backup(8) });
            Assert.IsFalse(r.Single(a => a.ClaveI18n == "alert.subs.vencidas").Leida, "Otra cantidad: es una situación nueva.");
            Assert.IsTrue(r.Single(a => a.ClaveI18n == "alert.backup.dias").Leida);
        }

        [TestMethod]
        public void MarcarTodas_YLuegoNoLeida()
        {
            var leidas = new AlertasLeidas(new ParametrosEnMemoria());
            var alertas = new List<BE.Alerta> { Vencidas(3), Backup(8) };
            leidas.MarcarLeidas(5, alertas);
            Assert.IsTrue(alertas.All(a => a.Leida));
            Assert.IsTrue(leidas.Aplicar(5, new[] { Vencidas(3), Backup(8) }).All(a => a.Leida));

            leidas.MarcarNoLeida(5, alertas[0]);
            var r = leidas.Aplicar(5, new[] { Vencidas(3), Backup(8) });
            Assert.IsFalse(r[0].Leida);
            Assert.IsTrue(r[1].Leida);
        }

        [TestMethod]
        public void LaMarcaEsPorUsuario()
        {
            var leidas = new AlertasLeidas(new ParametrosEnMemoria());
            leidas.MarcarLeidas(5, new[] { Vencidas(3) });
            Assert.IsFalse(leidas.Aplicar(7, new[] { Vencidas(3) }).Single().Leida, "Otro usuario no la leyó.");
        }

        [TestMethod]
        public void SiLaAlertaDesaparece_SeBorraLaMarca_YSiVuelveSeNotificaDeNuevo()
        {
            var dal = new ParametrosEnMemoria();
            var leidas = new AlertasLeidas(dal);
            leidas.MarcarLeidas(5, new[] { Vencidas(3) });

            leidas.Aplicar(5, new[] { Backup(8) });   // ya no hay vencidas
            Assert.AreEqual("", dal.Filas["Alerta.Leida.5.alert.subs.vencidas"], "La marca de una alerta que ya no está se borra.");

            Assert.IsFalse(leidas.Aplicar(5, new[] { Vencidas(3) }).Single().Leida, "Volvió a pasar: se avisa otra vez.");
        }

        [TestMethod]
        public void SinCambios_NoEscribeEnLaBase()
        {
            var dal = new ParametrosEnMemoria();
            var leidas = new AlertasLeidas(dal);
            leidas.MarcarLeidas(5, new[] { Vencidas(3) });
            int antes = dal.Escrituras;
            leidas.Aplicar(5, new[] { Vencidas(3) });
            leidas.Aplicar(5, new[] { Vencidas(3) });
            Assert.AreEqual(antes, dal.Escrituras, "Contar el aviso del menú no debe escribir si nada cambió.");
        }

        [TestMethod]
        public void SiLaBaseFalla_NingunaAlertaSeEsconde()
        {
            var dal = new ParametrosEnMemoria();
            var leidas = new AlertasLeidas(dal);
            leidas.MarcarLeidas(5, new[] { Vencidas(3) });
            dal.Fallar = true;
            Assert.IsTrue(leidas.Aplicar(5, new[] { Vencidas(3) }).All(a => !a.Leida));
        }

        [TestMethod]
        public void Firma_DependeDeCantidadYParametros_YEntraEnLaColumna()
        {
            Assert.AreEqual(AlertasLeidas.Firma(Vencidas(3)), AlertasLeidas.Firma(Vencidas(3)));
            Assert.AreNotEqual(AlertasLeidas.Firma(Vencidas(3)), AlertasLeidas.Firma(Vencidas(2)));
            var larga = new BE.Alerta(BE.NivelAlerta.Info, "x", "x", 1, new string('a', 900));
            Assert.IsTrue(AlertasLeidas.Firma(larga).Length <= 400);
            Assert.AreEqual(string.Empty, AlertasLeidas.Firma(null));
        }
    }
}
