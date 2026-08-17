using System;
using System.Collections.Generic;

namespace BLL
{
    /// <summary>
    /// Bloque 1 — Lógica de negocio de Cargo por daño/pérdida de prenda. Se registra desde
    /// el mismo flujo que da de baja una prenda (GUI.Prendas → BLL.Prenda.CambiarEstado),
    /// por eso comparte su permiso (StockEditar/Stock) — no hay una aprobación separada.
    /// </summary>
    public class CargoPrenda : Interfaces.ICargoPrendaService
    {
        private readonly DAL.Interfaces.ICargoPrendaDAL dalCargoPrenda;
        private readonly Servicios.Bitacora        bitacora    = new Servicios.Bitacora();
        private readonly Servicios.BitacoraNegocio bitacoraNeg = new Servicios.BitacoraNegocio();

        public CargoPrenda() : this(new DAL.CargoPrenda()) { }
        public CargoPrenda(DAL.Interfaces.ICargoPrendaDAL dalCargoPrenda)
        {
            this.dalCargoPrenda = dalCargoPrenda ?? throw new ArgumentNullException(nameof(dalCargoPrenda));
        }

        public void RegistrarCargo(string modulo, BE.Prenda prenda, string motivo, decimal monto, string actor = null)
        {
            PermisosAccion.Exigir(BE.Patentes.StockEditar, BE.Patentes.Stock);
            if (prenda == null) throw new ArgumentNullException(nameof(prenda));

            if (!prenda.IdUltimoCliente.HasValue)
                throw new BE.AppException("err.bll.cargoprenda.sin_cliente",
                    "La prenda '{0}' no tiene un último cliente registrado; no se le puede cargar el costo a nadie.",
                    prenda.Nombre);

            if (string.IsNullOrWhiteSpace(motivo))
                throw new BE.AppException("err.bll.cargoprenda.motivo_requerido",
                    "Debe indicar el motivo del cargo (daño o pérdida).");

            if (monto <= 0)
                throw new BE.AppException("err.bll.cargoprenda.monto_invalido",
                    "El monto del cargo debe ser mayor a cero.");

            var cargo = new BE.CargoPrenda
            {
                IdPrenda = prenda.IdPrenda,
                IdCliente = prenda.IdUltimoCliente.Value,
                Motivo = motivo,
                Monto = monto,
                FechaRegistro = DateTime.Now,
                Actor = actor
            };
            int idNuevo = dalCargoPrenda.Alta(cargo);
            cargo.IdCargo = idNuevo;

            bitacora.Registrar(modulo,
                $"Cargo por daño/pérdida — Prenda ID {prenda.IdPrenda} '{prenda.Nombre}': ${monto} ({motivo})",
                BE.Criticidad.Media);

            bitacoraNeg.Registrar(BE.TipoEventoNegocio.CambioEstadoPrenda,
                $"Cargo por daño/pérdida: prenda '{prenda.Nombre}' — ${monto} — {motivo} — se sumará al próximo cobro de {prenda.NombreUltimoCliente ?? "cliente ID " + prenda.IdUltimoCliente}",
                idPrenda: prenda.IdPrenda, idCliente: prenda.IdUltimoCliente);
        }

        public List<BE.CargoPrenda> ObtenerPendientesPorCliente(int idCliente) =>
            dalCargoPrenda.ObtenerPendientesPorCliente(idCliente);

        public List<BE.CargoPrenda> ObtenerTodos() => dalCargoPrenda.ObtenerTodos();
    }
}
