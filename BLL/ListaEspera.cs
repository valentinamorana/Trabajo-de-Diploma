using System;
using System.Collections.Generic;
using System.Linq;

namespace BLL
{
    /// <summary>
    /// Lógica de negocio — Lista de Espera de prendas (mejora opcional, no requerida
    /// por la cátedra). Ver README, sección "Módulos", y BD/16_Lista_Espera.sql.
    /// </summary>
    public class ListaEspera : Interfaces.IListaEsperaService
    {
        /// <summary>Ventana de reserva exclusiva tras liberarse la prenda esperada.</summary>
        public const int HORAS_RESERVA = 48;

        private readonly DAL.Interfaces.IListaEsperaDAL dalListaEspera;
        private readonly DAL.Interfaces.IPrendaDAL dalPrenda;
        private readonly DAL.Interfaces.IClienteDAL dalCliente;
        private readonly Servicios.Bitacora bitacora = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();

        public ListaEspera() : this(new DAL.ListaEspera(), new DAL.Prenda(), new DAL.Cliente()) { }

        public ListaEspera(DAL.Interfaces.IListaEsperaDAL dalListaEspera, DAL.Interfaces.IPrendaDAL dalPrenda,
                            DAL.Interfaces.IClienteDAL dalCliente)
        {
            this.dalListaEspera = dalListaEspera ?? throw new ArgumentNullException(nameof(dalListaEspera));
            this.dalPrenda = dalPrenda ?? throw new ArgumentNullException(nameof(dalPrenda));
            this.dalCliente = dalCliente ?? throw new ArgumentNullException(nameof(dalCliente));
        }

        public void Anotar(string modulo, int idPrenda, int idCliente, string actor)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);

            var prenda = dalPrenda.ObtenerPorId(idPrenda);
            if (prenda == null)
                throw new BE.AppException("err.bll.listaespera.prenda_inexistente",
                    "La prenda no existe.");
            if (prenda.Estado != BE.EstadoPrenda.EnUso)
                throw new BE.AppException("err.bll.listaespera.prenda_no_enuso",
                    "'{0}' está {1} — no hace falta anotarse, ya se puede pedir directamente.",
                    prenda.Nombre, prenda.Estado);

            var cliente = dalCliente.ObtenerPorId(idCliente);
            if (cliente == null)
                throw new BE.AppException("err.bll.listaespera.cliente_inexistente",
                    "El cliente no existe.");
            if (!cliente.SuscripcionVigente())
                throw new BE.AppException("err.bll.listaespera.suscripcion_vencida",
                    "{0} no tiene una suscripción vigente.", cliente.NombreCompleto);

            bool yaAnotado = dalListaEspera.ObtenerPorPrenda(idPrenda).Any(le =>
                le.IdCliente == idCliente &&
                (le.Estado == BE.EstadoListaEspera.Pendiente || le.Estado == BE.EstadoListaEspera.Reservada));
            if (yaAnotado)
                throw new BE.AppException("err.bll.listaespera.ya_anotado",
                    "{0} ya está anotado en la lista de espera de '{1}'.",
                    cliente.NombreCompleto, prenda.Nombre);

            dalListaEspera.Alta(new BE.ListaEspera
            {
                IdPrenda  = idPrenda,
                IdCliente = idCliente,
                FechaAlta = DateTime.Now,
                Estado    = BE.EstadoListaEspera.Pendiente
            });

            bitacora.Registrar(modulo,
                $"Anotar en Lista de Espera — Prenda '{prenda.Nombre}' (ID {idPrenda}) — Cliente: {cliente.NombreCompleto}",
                BE.Criticidad.Baja);

            bitacoraNeg.Registrar(BE.TipoEventoNegocio.ListaEspera,
                $"{cliente.NombreCompleto} se anotó en la lista de espera de '{prenda.Nombre}'",
                idPrenda: idPrenda, idCliente: idCliente);
        }

        public void Cancelar(string modulo, int idListaEspera, string actor)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);

            var fila = dalListaEspera.ObtenerPorId(idListaEspera);
            if (fila == null)
                throw new BE.AppException("err.bll.listaespera.inexistente",
                    "La anotación no existe.");
            if (fila.Estado == BE.EstadoListaEspera.Convertida || fila.Estado == BE.EstadoListaEspera.Cancelada)
                throw new BE.AppException("err.bll.listaespera.ya_resuelta",
                    "Esta anotación ya está {0} — no se puede cancelar.", fila.Estado);

            dalListaEspera.CambiarEstado(idListaEspera, BE.EstadoListaEspera.Cancelada, fila.FechaLimiteReserva, actor);

            bitacora.Registrar(modulo,
                $"Cancelar Lista de Espera #{idListaEspera} — Prenda '{fila.NombrePrenda}' — Cliente: {fila.NombreCliente}",
                BE.Criticidad.Baja);
        }

        // Al liberarse una prenda (BLL.Prenda.CambiarEstado, EnLimpieza → Disponible), reserva
        // la fila Pendiente más antigua (FIFO) durante HORAS_RESERVA. No hace nada si nadie espera.
        public void NotificarSiCorresponde(int idPrenda, string actor)
        {
            var fila = dalListaEspera.ObtenerPendienteMasAntigua(idPrenda);
            if (fila == null) return;

            var limite = DateTime.Now.AddHours(HORAS_RESERVA);
            dalListaEspera.CambiarEstado(fila.IdListaEspera, BE.EstadoListaEspera.Reservada, limite, actor);

            bitacora.Registrar("Prendas",
                $"Lista de Espera #{fila.IdListaEspera}: '{fila.NombrePrenda}' reservada para " +
                $"{fila.NombreCliente} hasta {limite:dd/MM HH:mm}",
                BE.Criticidad.Media);

            bitacoraNeg.Registrar(BE.TipoEventoNegocio.ListaEspera,
                $"'{fila.NombrePrenda}' reservada para {fila.NombreCliente} (Lista de Espera) hasta {limite:dd/MM/yyyy HH:mm}",
                idPrenda: idPrenda, idCliente: fila.IdCliente);
        }

        public bool EstaReservadaParaOtro(int idPrenda, int idClienteSolicitante)
            => dalListaEspera.ObtenerReservaVigenteDeOtro(idPrenda, idClienteSolicitante) != null;

        // Tras crear el pedido (BLL.Pedido.CrearPedido), cierra la reserva si esta prenda
        // estaba retenida para este mismo cliente. No hace nada si no había reserva.
        public void CerrarSiReservada(string modulo, int idPrenda, int idCliente, string actor)
        {
            var fila = dalListaEspera.ObtenerReservaVigenteDeCliente(idPrenda, idCliente);
            if (fila == null) return;

            dalListaEspera.CambiarEstado(fila.IdListaEspera, BE.EstadoListaEspera.Convertida, fila.FechaLimiteReserva, actor);

            bitacora.Registrar(modulo,
                $"Lista de Espera #{fila.IdListaEspera} cerrada: {fila.NombreCliente} retiró '{fila.NombrePrenda}'.",
                BE.Criticidad.Baja);
        }

        // Best-effort: si la tabla ListaEspera todavía no existe (BD sin migrar,
        // BD/16_Lista_Espera.sql no corrido), no filtra nada — degrada al comportamiento
        // anterior sin romper Nuevo Pedido, mismo criterio que BLL.PanelAlertas.
        public List<int> ObtenerIdsReservadosParaOtro(int? idClienteSolicitante)
        {
            try
            {
                return dalListaEspera.ObtenerActivas()
                    .Where(le => le.ReservaVigente &&
                                 (!idClienteSolicitante.HasValue || le.IdCliente != idClienteSolicitante.Value))
                    .Select(le => le.IdPrenda)
                    .Distinct()
                    .ToList();
            }
            catch { return new List<int>(); }
        }

        public List<BE.ListaEspera> ObtenerActivas() => dalListaEspera.ObtenerActivas();
        public List<BE.ListaEspera> ObtenerPorPrenda(int idPrenda) => dalListaEspera.ObtenerPorPrenda(idPrenda);

        public int ContarReservadasVigentes()
        {
            try { return dalListaEspera.ContarReservadasVigentes(); }
            catch { return 0; }
        }
    }
}
