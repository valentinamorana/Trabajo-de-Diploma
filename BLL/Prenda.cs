using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>Lógica de negocio para gestión de prendas.</summary>
    public class Prenda : Interfaces.IPrendaService
    {
        private readonly DAL.Interfaces.IPrendaDAL              dalPrenda;
        private readonly DAL.Interfaces.IMantenimientoPrendaDAL dalMantenimiento;
        private readonly Servicios.IRegistroBitacora          bitacora         = Servicios.FabricaBitacora.CrearSistema();
        private readonly Servicios.IRegistroBitacoraNegocio   bitacoraNeg      = Servicios.FabricaBitacora.CrearNegocio();

        // Lista de Espera (mejora opcional) — composición lazy, mismo criterio que
        // BLL.Usuario.perfilesBLL => new BLL.Familia().
        private Interfaces.IListaEsperaService _listaEsperaLazy;
        private Interfaces.IListaEsperaService listaEsperaBLL => _listaEsperaLazy ?? (_listaEsperaLazy = new ListaEspera());

        // DI: el constructor por defecto usa los DAL reales; el otro permite inyectar dobles
        // de prueba (mismo criterio que BLL.Pedido/BLL.Cliente).
        public Prenda() : this(new DAL.Prenda(), new DAL.MantenimientoPrenda()) { }

        public Prenda(DAL.Interfaces.IPrendaDAL dalPrenda, DAL.Interfaces.IMantenimientoPrendaDAL dalMantenimiento)
        {
            this.dalPrenda        = dalPrenda ?? throw new ArgumentNullException(nameof(dalPrenda));
            this.dalMantenimiento = dalMantenimiento ?? throw new ArgumentNullException(nameof(dalMantenimiento));
        }

        // Overload para inyectar un doble de prueba de Lista de Espera (mismo criterio que BLL.Pedido):
        // sin esto, ObtenerDisponibles consulta la Lista de Espera real contra la base.
        public Prenda(DAL.Interfaces.IPrendaDAL dalPrenda, DAL.Interfaces.IMantenimientoPrendaDAL dalMantenimiento,
                      Interfaces.IListaEsperaService listaEsperaBLL)
            : this(dalPrenda, dalMantenimiento)
        {
            _listaEsperaLazy = listaEsperaBLL ?? throw new ArgumentNullException(nameof(listaEsperaBLL));
        }

        public List<BE.Prenda> ObtenerTodos()                   => dalPrenda.ObtenerTodos();

        // Categorías que hoy existen en el catálogo (sin las de prendas dadas de baja), para elegir
        // la de una promoción sin errores de tipeo (PN03). Sin distinguir mayúsculas.
        public List<string> ObtenerCategorias() =>
            dalPrenda.ObtenerTodos()
                .Where(p => p.Estado != BE.EstadoPrenda.Baja && !string.IsNullOrWhiteSpace(p.Categoria))
                .Select(p => p.Categoria.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(c => c, StringComparer.CurrentCultureIgnoreCase)
                .ToList();
        public List<BE.Prenda> ObtenerPorCliente(int id)       => dalPrenda.ObtenerPorCliente(id);

        // Prendas Disponible, excluyendo las reservadas por Lista de Espera para OTRO
        // cliente (mejora opcional). Filtrado en memoria para no acoplar la query ya
        // probada de DAL.Prenda a una tabla nueva y opcional — si ListaEspera todavía no
        // existe (BD sin migrar), ObtenerIdsReservadosParaOtro degrada a lista vacía.
        public List<BE.Prenda> ObtenerDisponibles(int? idClienteSolicitante = null)
        {
            var disponibles = dalPrenda.ObtenerDisponibles(idClienteSolicitante);
            var reservadasParaOtro = listaEsperaBLL.ObtenerIdsReservadosParaOtro(idClienteSolicitante);
            return reservadasParaOtro.Count == 0
                ? disponibles
                : disponibles.FindAll(p => !reservadasParaOtro.Contains(p.IdPrenda));
        }

        // Da de alta una nueva prenda. Estado inicial siempre Disponible.
        public void Alta(string modulo, BE.Prenda prenda)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            Validar(prenda);
            prenda.Estado    = BE.EstadoPrenda.Disponible;
            prenda.FechaAlta = DateTime.Now;

            int idNuevo = dalPrenda.Alta(prenda);
            prenda.IdPrenda = idNuevo;

            bitacora.Registrar(modulo,
                $"Alta Prenda: {prenda.Nombre} (Talle {prenda.Talle}, {prenda.Color})",
                BE.Criticidad.Baja);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.AltaPrenda,
                $"Nueva prenda: {prenda.Nombre} — Talle {prenda.Talle} — {prenda.Color} — {prenda.Categoria}",
                idPrenda: idNuevo);
        }

        // Modifica los datos descriptivos de una prenda.
        // No afecta estado ni cliente asignado.
        public void Modificar(string modulo, BE.Prenda prenda)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            Validar(prenda);
            dalPrenda.Modificar(prenda);

            bitacora.Registrar(modulo,
                $"Modificar Prenda ID {prenda.IdPrenda}: {prenda.Nombre}",
                BE.Criticidad.Baja);

            bitacoraNeg.Registrar(BE.TipoEventoNegocio.ModificacionPrenda,
                $"Modificación prenda: '{prenda.Nombre}' (ID {prenda.IdPrenda}) — Talle {prenda.Talle}, {prenda.Color}",
                idPrenda: prenda.IdPrenda);
        }

        // Cambia el estado de una prenda validando la transición.
        // Al entrar a EnLimpieza abre un registro de mantenimiento;
        // al volver a Disponible desde EnLimpieza lo cierra.
        // EnUso → Baja (Reportar Prenda Perdida) y EnLimpieza → Baja (Inspección de Devolución)
        // NUNCA pasan por acá: el patrón State (BE.Estados) las permite a nivel de datos, pero las
        // dos llevan un cargo al cliente en la misma transacción, así que solo las hace
        // BLL.InspeccionDevolucion. Acá se rechazan siempre, venga de la pantalla que venga.
        public void CambiarEstado(string modulo, BE.Prenda prenda, BE.EstadoPrenda nuevoEstado)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            string actor = Sesion.Actor;   // quién abre el mantenimiento: lo resuelve la BLL, no la GUI

            // Patrón State: el propio objeto Estado actual decide si la transición es
            // válida y, si lo es, muta prenda.Estado directamente (igual que el ejemplo
            // de cátedra: Estado.ControlarEstado(Switch) llama a sw.DefinirEstado(...)).
            // Por eso se guarda el estado anterior ANTES de llamar: después de un éxito,
            // prenda.Estado ya vale nuevoEstado.
            BE.EstadoPrenda estadoAnterior = prenda.Estado;

            if (RequiereFlujoPerdida(estadoAnterior, nuevoEstado))
                throw new BE.AppException("err.bll.prenda.baja_requiere_flujoperdida",
                    "Una prenda en uso solo puede darse de baja a través de 'Reportar Prenda Perdida' " +
                    "(con cargo de reposición al cliente), no directamente.");

            // PN04: una prenda que volvió del cliente (En Limpieza) solo se da de baja desde la
            // Inspección de Devolución, que registra ANTES el cargo por el daño irreparable. Sin
            // esta barrera en la BLL, cualquier otra pantalla podía retirarla del catálogo sin cargo.
            if (RequiereInspeccion(prenda, nuevoEstado))
                throw new BE.AppException("err.bll.prenda.baja_requiere_inspeccion",
                    "Una prenda En Limpieza solo puede darse de baja desde la Inspección de Devolución " +
                    "(que registra el cargo por el daño), no directamente.");

            if (!prenda.ControlarEstado(nuevoEstado))
            {
                // Se lanza una clave traducible por caso (antes el motivo era texto fijo en
                // español que el traductor no podía localizar).
                if (estadoAnterior == BE.EstadoPrenda.Baja)
                    throw new BE.AppException("err.bll.prenda.transicion_baja",
                        "Una prenda dada de baja no puede cambiar de estado.");
                if (estadoAnterior == BE.EstadoPrenda.EnUso)
                    throw new BE.AppException("err.bll.prenda.transicion_enuso",
                        "El estado de una prenda en uso se actualiza automáticamente al procesar pedidos.\n" +
                        "La única excepción manual es reportarla como perdida (PN04, módulo de Pedidos Realizados).");
                throw new BE.AppException("err.bll.prenda.transicion_generica",
                    "La transición de '{0}' a '{1}' no está permitida.",
                    estadoAnterior.ToString(), nuevoEstado.ToString());
            }

            int? idCliente = nuevoEstado == BE.EstadoPrenda.EnUso
                ? prenda.IdClienteActual
                : null;

            try
            {
                dalPrenda.CambiarEstado(prenda.IdPrenda, estadoAnterior, nuevoEstado, idCliente);
            }
            catch
            {
                // Anti-TOCTOU (DAL/Prenda.cs): si el UPDATE condicionado no afectó ninguna fila
                // porque el estado cambió entre la lectura y este punto, revertir acá la
                // mutación en memoria que ControlarEstado ya aplicó — si no, el objeto que
                // quedó cacheado en la GUI (_prendas/_prendasDetalleActual) sigue mostrando un
                // estado que en realidad nunca se persistió.
                prenda.Estado = estadoAnterior;
                throw;
            }

            if (nuevoEstado == BE.EstadoPrenda.EnLimpieza)
            {
                dalMantenimiento.IniciarMantenimiento(prenda.IdPrenda, actor);
            }
            else if (estadoAnterior == BE.EstadoPrenda.EnLimpieza && nuevoEstado == BE.EstadoPrenda.Baja)
            {
                // Baja de una prenda que entró a limpieza en el depósito (no por devolución): se
                // cierra el mantenimiento para que no quede abierto en el tablero.
                dalMantenimiento.CerrarMantenimiento(prenda.IdPrenda);
            }
            else if (estadoAnterior == BE.EstadoPrenda.EnLimpieza &&
                     nuevoEstado == BE.EstadoPrenda.Disponible)
            {
                dalMantenimiento.CerrarMantenimiento(prenda.IdPrenda);

                // Lista de Espera (mejora opcional): si alguien esperaba esta prenda,
                // se la reserva (ventana de HORAS_RESERVA). No hace nada si nadie espera,
                // ni si la tabla ListaEspera todavía no existe (BD sin migrar).
                try { listaEsperaBLL.NotificarSiCorresponde(prenda.IdPrenda); }
                catch (Exception ex) { System.Diagnostics.Trace.TraceError($"[BLL.Prenda] Lista de Espera: {ex.Message}"); }
            }

            bitacora.Registrar(modulo,
                $"Estado Prenda ID {prenda.IdPrenda} '{prenda.Nombre}': {estadoAnterior} → {nuevoEstado}",
                BE.Criticidad.Media);

            bitacoraNeg.Registrar(
                BE.TipoEventoNegocio.CambioEstadoPrenda,
                $"Prenda '{prenda.Nombre}' (ID {prenda.IdPrenda}): {estadoAnterior} → {nuevoEstado}",
                idPrenda: prenda.IdPrenda);
        }

        // PN04 — EnUso → Baja solo por CU-DEP-02 Reportar Prenda Perdida (con cargo de reposición).
        private static bool RequiereFlujoPerdida(BE.EstadoPrenda desde, BE.EstadoPrenda hacia) =>
            desde == BE.EstadoPrenda.EnUso && hacia == BE.EstadoPrenda.Baja;

        // PN04 — Una prenda que volvió de un cliente (mantenimiento abierto por la devolución) solo se
        // da de baja desde la Inspección de Devolución, que registra el cargo por daño. Si entró a
        // limpieza en el depósito, la baja es manual y sin cargo: el cliente no tuvo nada que ver.
        private bool RequiereInspeccion(BE.Prenda prenda, BE.EstadoPrenda hacia) =>
            prenda.Estado == BE.EstadoPrenda.EnLimpieza && hacia == BE.EstadoPrenda.Baja
            && VieneDeDevolucion(prenda.IdPrenda);

        private bool VieneDeDevolucion(int idPrenda) =>
            dalMantenimiento.ObtenerPorPrenda(idPrenda).Any(m => m.EstaAbierto && m.VieneDeDevolucion);

        // Destinos que el cambio de estado MANUAL (pantalla Prendas) puede ofrecer para la prenda,
        // en este orden: Disponible, EnLimpieza, Baja. Parte de las transiciones del patrón State
        // (BE.Prenda.TransicionPermitida) y saca las que solo existen por un flujo dedicado de PN04
        // (Reportar Prenda Perdida e Inspección de Devolución), que CambiarEstado igual rechaza.
        // EnUso nunca es destino manual: lo asigna el flujo de Pedido. Antes esta lista la armaba
        // GUI/Prendas.cs.
        public List<BE.EstadoPrenda> ObtenerTransicionesManuales(BE.Prenda prenda)
        {
            if (prenda == null) throw new ArgumentNullException(nameof(prenda));

            var candidatos = new[] { BE.EstadoPrenda.Disponible, BE.EstadoPrenda.EnLimpieza, BE.EstadoPrenda.Baja };
            return candidatos
                .Where(destino => destino != prenda.Estado
                               && !RequiereFlujoPerdida(prenda.Estado, destino)
                               && !RequiereInspeccion(prenda, destino)
                               && prenda.TransicionPermitida(destino))
                .ToList();
        }

        // CU01-CS-Verificar Disponibilidad (PN01): relee el estado real de toda la selección
        // desde la base en una sola consulta batch (no confía en el objeto en memoria que pasó
        // el caller, y no hace una query por prenda) y evalúa EstaDisponible() de cada una.
        // Operación de solo lectura: no reserva ni modifica nada.
        public (bool Disponible, List<BE.Prenda> NoDisponibles) VerificarDisponibilidad(List<BE.Prenda> seleccion)
        {
            var actuales = dalPrenda.ObtenerPorIds(seleccion.Select(p => p.IdPrenda).ToList())
                .ToDictionary(p => p.IdPrenda);

            var noDisponibles = new List<BE.Prenda>();
            foreach (var p in seleccion)
            {
                actuales.TryGetValue(p.IdPrenda, out var actual);
                if (actual == null || !actual.EstaDisponible())
                    noDisponibles.Add(actual ?? p);
            }
            return (noDisponibles.Count == 0, noDisponibles);
        }

        // PN04, CU-DEP-01 Inspeccionar Devolución: prendas EnLimpieza que volvieron de un cliente
        // (mantenimiento abierto por la devolución), pendientes de resolución: reingresan sin cargo o
        // se dan de baja con cargo al último cliente. Las que entraron a limpieza en el depósito no
        // están acá: no hay a quién cobrarle.
        public List<BE.Prenda> ObtenerEnLimpieza()
        {
            var devueltas = new HashSet<int>(dalMantenimiento.ObtenerTodos()
                .Where(m => m.EstaAbierto && m.VieneDeDevolucion).Select(m => m.IdPrenda));
            return dalPrenda.ObtenerTodos().FindAll(p => p.Estado == BE.EstadoPrenda.EnLimpieza && devueltas.Contains(p.IdPrenda));
        }

        public List<BE.MantenimientoPrenda> ObtenerHistorialMantenimiento(int idPrenda)
            => dalMantenimiento.ObtenerPorPrenda(idPrenda);

        public List<BE.MantenimientoPrenda> ObtenerEnMantenimiento()
        {
            var todos    = dalMantenimiento.ObtenerTodos();
            var abiertos = new List<BE.MantenimientoPrenda>();
            foreach (var m in todos)
                if (m.EstaAbierto) abiertos.Add(m);
            return abiertos;
        }

        // Devuelve el resumen de ocupación del stock para el Dashboard.
        public BE.OcupacionStock ObtenerOcupacion()
        {
            var todas = dalPrenda.ObtenerTodos();
            int enUso      = 0, enLimpieza = 0, disponibles = 0;
            foreach (var p in todas)
            {
                if      (p.Estado == BE.EstadoPrenda.EnUso)      enUso++;
                else if (p.Estado == BE.EstadoPrenda.EnLimpieza) enLimpieza++;
                else if (p.Estado == BE.EstadoPrenda.Disponible) disponibles++;
            }
            return new BE.OcupacionStock
            {
                Total       = todas.Count,
                EnUso       = enUso,
                EnLimpieza  = enLimpieza,
                Disponibles = disponibles
            };
        }

        private void Validar(BE.Prenda prenda)
        {
            if (prenda == null)
                throw new ArgumentNullException(nameof(prenda));

            if (string.IsNullOrWhiteSpace(prenda.Nombre))
                throw new BE.AppException("err.bll.prenda.nombre_requerido",
                    "El nombre de la prenda es obligatorio.");

            if (string.IsNullOrWhiteSpace(prenda.Talle))
                throw new BE.AppException("err.bll.prenda.talle_requerido",
                    "El talle es obligatorio.");

            if (string.IsNullOrWhiteSpace(prenda.Categoria))
                throw new BE.AppException("err.bll.prenda.categoria_requerida",
                    "La categoría es obligatoria.");
        }
    }
}
