using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Reúne en un solo lugar las alertas operativas del sistema: suscripciones por
    /// vencer/vencidas, antigüedad del último backup, prendas trabadas en limpieza e
    /// integridad de datos (DV). Toda la lógica de detección vive acá (capa BLL); la
    /// GUI solo lista las <see cref="BE.Alerta"/> resultantes y las traduce.
    ///
    /// Cada chequeo está aislado en su try: si una fuente falla, no tumba al resto.
    ///
    /// FILTRO POR PATENTES: cada alerta tiene un "dueño" funcional (ver
    /// <see cref="PatentesPorAlerta"/>). Antes todas se mostraban a cualquier rol — un
    /// Vendedor veía "Integridad comprometida" o "No hay backups registrados", que solo el
    /// Administrador puede resolver. Ahora <see cref="ObtenerAlertas(BE.Usuario)"/> solo
    /// recolecta y devuelve las que el usuario puede atender. Administrador: bypass total
    /// (mismo criterio que BLL.MenuVisibilidad y BLL.PermisosAccion).
    /// </summary>
    public class PanelAlertas
    {
        private readonly Cliente        _cliente     = new Cliente();
        private readonly Prenda         _prenda      = new Prenda();
        private readonly ReporteJornada _reporte     = new ReporteJornada();
        private readonly Interfaces.IListaEsperaService _listaEspera = new ListaEspera();
        private readonly Pedido         _pedido      = new Pedido();

        /// <summary>
        /// Clave de alerta → patentes que la habilitan (basta con una). Una clave que no esté
        /// acá NO se muestra a nadie salvo al Administrador (fail-closed: una alerta nueva sin
        /// dueño definido se nota enseguida en vez de filtrarse a todos los roles).
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string[]> PatentesPorAlerta =
            new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
            {
                // Suscripciones: quien gestiona clientes, renovaciones o cobros.
                ["alert.subs.vencidas"]  = new[] { BE.Patentes.Clientes, BE.Patentes.RenovacionSuscripcion, BE.Patentes.CobroSuscripcion },
                ["alert.subs.porvencer"] = new[] { BE.Patentes.Clientes, BE.Patentes.RenovacionSuscripcion, BE.Patentes.CobroSuscripcion },
                // Backup e integridad (DV): solo la administración del sistema.
                ["alert.backup.nunca"]   = new[] { BE.Patentes.Usuarios },
                ["alert.backup.dias"]    = new[] { BE.Patentes.Usuarios },
                ["alert.dv.corruptos"]   = new[] { BE.Patentes.Usuarios },
                // Inventario: Depósito (prendas / stock / inspección de devolución).
                ["alert.prendas.limpieza"]       = new[] { BE.Patentes.Prendas, BE.Patentes.Stock, BE.Patentes.InspeccionDevolucion },
                ["alert.listaespera.reservadas"] = new[] { BE.Patentes.ListaEspera },
                // PN01 — el control de stock lo hace Depósito (el Vendedor sigue su pedido);
                // comunicar faltantes y formalizar le toca al Vendedor.
                ["alert.pedidos.control"]    = new[] { BE.Patentes.ControlStock, BE.Patentes.PedidosVenta },
                ["alert.pedidos.faltantes"]  = new[] { BE.Patentes.PedidosVenta },
                ["alert.pedidos.formalizar"] = new[] { BE.Patentes.PedidosVenta },
                // PN02 — cola de cobro: rol Caja.
                ["alert.contr.pendientes"]   = new[] { BE.Patentes.Caja },
            };

        /// <summary>
        /// Decisión PURA: ¿un usuario con estas patentes (NombreMenu, case-insensitive) puede
        /// ver la alerta? Administrador: siempre.
        /// </summary>
        public static bool PuedeVer(string claveAlerta, IEnumerable<string> patentes, bool esAdmin)
        {
            if (esAdmin) return true;
            if (string.IsNullOrEmpty(claveAlerta)) return false;
            if (!PatentesPorAlerta.TryGetValue(claveAlerta, out var requeridas)) return false;
            var set = new HashSet<string>(patentes ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
            return requeridas.Any(set.Contains);
        }

        /// <summary>Filtro PURO: deja solo las alertas que el usuario puede atender.</summary>
        public static List<BE.Alerta> FiltrarPorPatentes(IEnumerable<BE.Alerta> alertas,
            IEnumerable<string> patentes, bool esAdmin)
        {
            var lista = patentes?.ToList() ?? new List<string>();
            return (alertas ?? Enumerable.Empty<BE.Alerta>())
                .Where(a => a != null && PuedeVer(a.ClaveI18n, lista, esAdmin))
                .ToList();
        }

        /// <summary>
        /// ¿El usuario puede ver las alertas de integridad (DV)? Lo usa el Menú para decidir
        /// si el chequeo periódico de integridad corre y muestra su aviso emergente: para
        /// cualquier otro rol el timer no hace nada.
        /// </summary>
        public static bool PuedeVerIntegridad(BE.Usuario usuario)
        {
            if (usuario == null) return false;
            return PuedeVer("alert.dv.corruptos", PatentesDe(usuario), usuario.EsAdministrador);
        }

        private static List<string> PatentesDe(BE.Usuario usuario) =>
            (usuario?.Permisos ?? new List<BE.Permiso>())
                .Where(p => p != null && !string.IsNullOrEmpty(p.NombreMenu))
                .Select(p => p.NombreMenu)
                .ToList();

        /// <summary>
        /// Alertas visibles para el usuario. Solo consulta las fuentes de las alertas que
        /// puede ver (ej. el diagnóstico de DV, costoso, no corre para un Vendedor).
        /// Sin usuario (sin sesión) → lista vacía.
        /// </summary>
        public List<BE.Alerta> ObtenerAlertas(BE.Usuario usuario)
        {
            if (usuario == null) return new List<BE.Alerta>();
            var patentes = PatentesDe(usuario);
            bool esAdmin = usuario.EsAdministrador;
            bool Ve(string clave) => PuedeVer(clave, patentes, esAdmin);

            // Recolección de métricas desde las fuentes (cada una aislada en su try para
            // que una caída no tumbe al resto). Desconocido = métrica no disponible (se ignora).
            int vencidas = Desconocido, porVencer = Desconocido, diasSinBackup = Desconocido,
                enLimpieza = Desconocido, dvRotas = Desconocido, reservadasEspera = Desconocido,
                enControl = Desconocido, conFaltantes = Desconocido, separados = Desconocido,
                contratacionesPendientes = Desconocido;

            if (Ve("alert.subs.vencidas") || Ve("alert.subs.porvencer"))
            {
                try
                {
                    var clientes = _cliente.ObtenerTodos();
                    porVencer = clientes.Count(c => c.SuscripcionProximaAVencer(7));
                    vencidas  = clientes.Count(c => c.VencimientoExpirado);
                }
                catch { }
            }

            if (Ve("alert.backup.dias"))
                try { diasSinBackup = _reporte.ObtenerDiasSinBackup(); } catch { }

            if (Ve("alert.prendas.limpieza"))
            {
                try
                {
                    enLimpieza = _prenda.ObtenerTodos()
                        .Count(p => p.Estado == BE.EstadoPrenda.EnLimpieza);
                }
                catch { }
            }

            if (Ve("alert.dv.corruptos"))
            {
                try
                {
                    var diag = Configuracion.ObtenerDiagnostico();
                    dvRotas = diag.Integro ? 0 : diag.FilasRotas.Count;
                }
                catch { }
            }

            // Lista de Espera (mejora opcional): prendas reservadas esperando que el
            // cliente pase a retirarlas. Desconocido si la tabla todavía no existe (BD sin migrar).
            if (Ve("alert.listaespera.reservadas"))
                try { reservadasEspera = _listaEspera.ContarReservadasVigentes(); } catch { }

            // PN01 — pedidos esperando una acción en el armado: Depósito (control de stock) o
            // Vendedor (comunicar faltantes / formalizar).
            if (Ve("alert.pedidos.control") || Ve("alert.pedidos.faltantes") || Ve("alert.pedidos.formalizar"))
            {
                try
                {
                    var pedidos = _pedido.ObtenerTodos();
                    enControl    = pedidos.Count(p => p.Estado == BE.EstadoPedido.EnControlStock);
                    conFaltantes = pedidos.Count(p => p.Estado == BE.EstadoPedido.ConFaltantes);
                    separados    = pedidos.Count(p => p.Estado == BE.EstadoPedido.Separado);
                }
                catch { }
            }

            // PN02 — contrataciones registradas por el Vendedor que esperan el cobro de Caja.
            if (Ve("alert.contr.pendientes"))
                try { contratacionesPendientes = new Contratacion().ContarPendientesDePago(); } catch { }

            var todas = EvaluarAlertas(vencidas, porVencer, diasSinBackup, enLimpieza, dvRotas, reservadasEspera,
                                       enControl, conFaltantes, separados, contratacionesPendientes);
            return FiltrarPorPatentes(todas, patentes, esAdmin);
        }

        /// <summary>Centinela: la métrica no pudo obtenerse (fuente caída) → se ignora.</summary>
        public const int Desconocido = int.MinValue;

        /// <summary>
        /// NÚCLEO PURO de las reglas de alerta: dadas las métricas ya recolectadas, decide
        /// qué alertas emitir y con qué severidad. Sin acceso a datos → 100% testeable.
        /// Una métrica en <see cref="Desconocido"/> se ignora. Para el backup, un valor
        /// negativo (pero conocido) significa "no hay backups registrados".
        /// </summary>
        public static List<BE.Alerta> EvaluarAlertas(int vencidas, int porVencer,
            int diasSinBackup, int enLimpieza, int dvRotas, int reservadasEspera = 0,
            int enControl = 0, int conFaltantes = 0, int separados = 0, int contratacionesPendientes = 0)
        {
            var alertas = new List<BE.Alerta>();

            // 1) Suscripciones vencidas / por vencer
            if (vencidas > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Critica, "alert.subs.vencidas",
                    "{0} suscripción(es) vencida(s).", vencidas, vencidas));
            if (porVencer > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Advertencia, "alert.subs.porvencer",
                    "{0} suscripción(es) vence(n) en los próximos 7 días.", porVencer, porVencer));

            // 2) Backup: Desconocido = se ignora; negativo conocido = no hay backups; >=7 = aviso
            if (diasSinBackup != Desconocido)
            {
                if (diasSinBackup < 0)
                    alertas.Add(new BE.Alerta(BE.NivelAlerta.Critica, "alert.backup.nunca",
                        "No hay backups registrados.", 0));
                else if (diasSinBackup >= 7)
                    alertas.Add(new BE.Alerta(BE.NivelAlerta.Advertencia, "alert.backup.dias",
                        "Hace {0} día(s) que no se realiza un backup.", diasSinBackup, diasSinBackup));
            }

            // 3) Prendas trabadas en limpieza
            if (enLimpieza > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Info, "alert.prendas.limpieza",
                    "{0} prenda(s) en limpieza.", enLimpieza, enLimpieza));

            // 4) Integridad de datos
            if (dvRotas > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Critica, "alert.dv.corruptos",
                    "Integridad comprometida: {0} fila(s) con DV inválido.", dvRotas, dvRotas));

            // 5) Lista de Espera (mejora opcional) — prendas reservadas esperando retiro
            if (reservadasEspera > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Info, "alert.listaespera.reservadas",
                    "{0} prenda(s) reservada(s) por Lista de Espera esperando que el cliente pase a retirarlas.",
                    reservadasEspera, reservadasEspera));

            // 6) PN01 — armado de pedidos pendiente de acción
            if (enControl > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Advertencia, "alert.pedidos.control",
                    "{0} pedido(s) esperando el control de stock de Depósito.", enControl, enControl));
            if (conFaltantes > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Advertencia, "alert.pedidos.faltantes",
                    "{0} pedido(s) con faltantes para comunicar al cliente.", conFaltantes, conFaltantes));
            if (separados > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Info, "alert.pedidos.formalizar",
                    "{0} pedido(s) con las prendas separadas, listos para formalizar.", separados, separados));

            // 7) PN02 — cola de Caja
            if (contratacionesPendientes > 0)
                alertas.Add(new BE.Alerta(BE.NivelAlerta.Info, "alert.contr.pendientes",
                    "{0} contratación(es) esperando el cobro de Caja.", contratacionesPendientes, contratacionesPendientes));

            return alertas;
        }

        /// <summary>Cantidad de alertas visibles para el usuario (para el badge del menú).</summary>
        public int Contar(BE.Usuario usuario)
        {
            return ObtenerAlertas(usuario).Count;
        }
    }
}
