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
        private readonly Servicios.Bitacora          bitacora         = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio   bitacoraNeg      = new Servicios.BitacoraNegocio();

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

        public List<BE.Prenda> ObtenerTodos()                   => dalPrenda.ObtenerTodos();
        public List<BE.Prenda> ObtenerPorCliente(int id)       => dalPrenda.ObtenerPorCliente(id);
        public BE.Prenda       ObtenerPorId(int idPrenda)      => dalPrenda.ObtenerPorId(idPrenda);

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
        public void CambiarEstado(string modulo, BE.Prenda prenda, BE.EstadoPrenda nuevoEstado, string actor = null)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);

            // Patrón State: el propio objeto Estado actual decide si la transición es
            // válida y, si lo es, muta prenda.Estado directamente (igual que el ejemplo
            // de cátedra: Estado.ControlarEstado(Switch) llama a sw.DefinirEstado(...)).
            // Por eso se guarda el estado anterior ANTES de llamar: después de un éxito,
            // prenda.Estado ya vale nuevoEstado.
            BE.EstadoPrenda estadoAnterior = prenda.Estado;

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
            else if (estadoAnterior == BE.EstadoPrenda.EnLimpieza &&
                     nuevoEstado == BE.EstadoPrenda.Disponible)
            {
                dalMantenimiento.CerrarMantenimiento(prenda.IdPrenda);

                // Lista de Espera (mejora opcional): si alguien esperaba esta prenda,
                // se la reserva (ventana de HORAS_RESERVA). No hace nada si nadie espera,
                // ni si la tabla ListaEspera todavía no existe (BD sin migrar).
                try { listaEsperaBLL.NotificarSiCorresponde(prenda.IdPrenda, actor); }
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

        // PN04, CU-DEP-01 Inspeccionar Devolución: prendas EnLimpieza pendientes de
        // resolución (reingresan sin cargo o se dan de baja con cargo). Filtrado en memoria,
        // mismo criterio que ObtenerOcupacion() — no hace falta una query SQL dedicada.
        public List<BE.Prenda> ObtenerEnLimpieza() =>
            dalPrenda.ObtenerTodos().FindAll(p => p.Estado == BE.EstadoPrenda.EnLimpieza);

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
