using System;

namespace BE
{
    /// <summary>Entidad Prenda. Mapea la tabla [Prenda].</summary>
    public class Prenda
    {
        public int          IdPrenda        { get; set; }
        public DateTime     FechaAlta       { get; set; }
        public string       Nombre          { get; set; }
        public string       Descripcion     { get; set; }
        public string       Talle           { get; set; }
        public string       Color           { get; set; }
        public string       Categoria       { get; set; }
        public EstadoPrenda Estado          { get; set; } = EstadoPrenda.Disponible;

        /// <summary>FK → Cliente. Solo aplica cuando Estado = EnUso.</summary>
        public int?         IdClienteActual { get; set; }

        /// <summary>Nombre del cliente que la tiene (cargado por JOIN, no persiste).</summary>
        public string       NombreCliente   { get; set; }

        /// <summary>FK → Cliente. A diferencia de IdClienteActual, NUNCA se limpia al
        /// devolverse — sigue apuntando a quién la tuvo por última vez durante todo el
        /// ciclo de Mantenimiento (EnLimpieza), para poder registrar un cargo por
        /// daño/pérdida si corresponde aunque la prenda ya no esté EnUso.</summary>
        public int?         IdUltimoCliente { get; set; }

        /// <summary>Nombre del último cliente que la tuvo (cargado por JOIN, no persiste).</summary>
        public string       NombreUltimoCliente { get; set; }

        /// <summary>Descripción para mostrar en grillas: "Vestido azul – en uso"</summary>
        public string ResumenEstado =>
            Estado == EstadoPrenda.EnUso && !string.IsNullOrEmpty(NombreCliente)
                ? $"{Nombre} — en uso ({NombreCliente})"
                : $"{Nombre} — {Estado}";

        // ── Comportamiento ────────────────────────────────────────────────────

        /// <summary>Indica si la prenda puede ser asignada a un pedido.</summary>
        public bool EstaDisponible() => Estado == EstadoPrenda.Disponible;

        /// <summary>
        /// Indica si el cambio al nuevo estado está permitido, SIN modificar nada.
        /// Consulta de solo lectura para pantallas que arman opciones válidas antes de
        /// que el usuario confirme (ver GUI.Prendas). Delega en el mismo objeto Estado
        /// que <see cref="ControlarEstado"/> usa para aplicar la transición, así que las
        /// reglas de negocio (ver <see cref="Estados.Estado.EsTransicionValida"/> de cada
        /// estado concreto) viven en un solo lugar. Transiciones válidas (solo desde el
        /// módulo de Prendas):
        ///   Disponible  → EnLimpieza | Baja
        ///   EnLimpieza  → Disponible | Baja
        ///   EnUso       → (ninguna: solo via pedido)
        ///   Baja        → (ninguna: estado final)
        /// La transición Disponible/EnLimpieza → EnUso solo ocurre
        /// internamente al crear o des-cancelar un pedido (ver DAL.Pedido).
        /// </summary>
        public bool TransicionPermitida(EstadoPrenda nuevo) => ResolverEstado().EsTransicionValida(nuevo);

        /// <summary>
        /// Patrón State: delega en el objeto Estado que representa el estado actual de
        /// la prenda — equivalente a Switch.Presionar() → _estado.ControlarEstado(this)
        /// del ejemplo de cátedra. Si la transición es válida, el propio Estado modifica
        /// este.Estado y devuelve true; si no, devuelve false sin modificar nada. A
        /// diferencia del ejemplo, la Prenda no retiene un objeto Estado en vivo (persiste
        /// como enum en la BD), así que acá se resuelve el Estado concreto actual antes de
        /// delegarle el control — el objeto en sí no tiene estado propio, es descartable.
        /// </summary>
        public bool ControlarEstado(EstadoPrenda destino) => ResolverEstado().ControlarEstado(this, destino);

        // Resuelve el objeto Estado concreto correspondiente al valor actual de Estado.
        // Único lugar que traduce el enum persistido a su representación como patrón State.
        private Estados.Estado ResolverEstado()
        {
            switch (Estado)
            {
                case EstadoPrenda.Disponible: return new Estados.EstadoDisponible();
                case EstadoPrenda.EnLimpieza: return new Estados.EstadoEnLimpieza();
                case EstadoPrenda.EnUso:      return new Estados.EstadoEnUso();
                case EstadoPrenda.Baja:       return new Estados.EstadoBaja();
                default:
                    throw new InvalidOperationException($"Estado de prenda desconocido: {Estado}");
            }
        }
    }
}
