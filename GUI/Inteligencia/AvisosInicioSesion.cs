using System;
using System.Threading.Tasks;

namespace GUI.Inteligencia
{
    /// <summary>
    /// PN03 «Avisos al iniciar sesión» — Después del login, para quien tiene el Centro de decisiones, calcula
    /// en segundo plano qué hay para decidir hoy (BLL.Inteligencia.InteligenciaComercial.ResumirAvisos) y, si
    /// hay algo, muestra un aviso NO modal en la esquina del menú (AvisoComercialForm) con un botón que abre
    /// el Centro con el mecanismo del menú (Menu.AbrirUnico). El cálculo arranca enseguida (fuera del hilo de
    /// la interfaz); mostrarlo lo encola el Menú con DespuesDeLaGuia, así en el primer ingreso aparece después
    /// de la guía de primer uso, y nunca encima de un diálogo modal abierto (Menu.CuandoLaVentanaEsteLibre:
    /// si el cálculo termina con un modal abierto, el aviso espera a que se cierre). Si el cálculo falla o no
    /// hay nada, no se muestra nada.
    /// </summary>
    internal static class AvisosInicioSesion
    {
        private const string Modulo = "Avisos del Centro de decisiones";

        /// <summary>
        /// Arranca el cálculo y devuelve la acción que muestra el aviso cuando el resultado está listo
        /// (para Menu.DespuesDeLaGuia). Si al usuario no le corresponde, devuelve una acción vacía.
        /// </summary>
        public static Action Preparar(Menu menu)
        {
            if (menu == null || !BLL.Sesion.Activa) return () => { };
            if (!BLL.Inteligencia.AvisosComerciales.Corresponde(BLL.PermisosAccion.Puede)) return () => { };
            int? idUsuario = BLL.Sesion.IdUsuario;
            if (!idUsuario.HasValue) return () => { };

            var calculo = Task.Run(() =>
            {
                try
                {
                    if (new BLL.Inteligencia.AvisosComerciales().OcultoHoy(idUsuario.Value)) return null;
                    return new BLL.Inteligencia.InteligenciaComercial().ResumirAvisos(Modulo);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.TraceError("[AvisosInicioSesion] " + ex.Message);
                    return null;
                }
            });

            return () => calculo.ContinueWith(t =>
            {
                var resumen = t.Status == TaskStatus.RanToCompletion ? t.Result : null;
                if (resumen == null || !resumen.HayAlgo) return;
                if (menu.IsDisposed || !menu.IsHandleCreated) return;
                // Nunca lanza; si el menú se cerró mientras se calculaba, no hace nada.
                menu.CuandoLaVentanaEsteLibre(() =>
                {
                    if (menu.IsDisposed || !BLL.Sesion.Activa) return;
                    var aviso = new AvisoComercialForm(resumen, idUsuario.Value, Modulo,
                        () => menu.AbrirUnico(() => new CentroDecisionesForm()));
                    aviso.MostrarEnEsquina(menu);
                });
            });
        }
    }
}
