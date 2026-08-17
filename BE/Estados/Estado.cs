namespace BE.Estados
{
    /// <summary>
    /// Patrón State (PdN2/PdN4). Equivalente al "Estado" del ejemplo de cátedra
    /// (Estado.ControlarEstado(Switch) → Apagado/Encendido/StandBy): cada estado
    /// concreto decide si la transición pedida es válida y, si lo es, la aplica
    /// directamente sobre el Contexto (Prenda) — igual que el ejemplo hace
    /// "sw.DefinirEstado(new StandBy())" dentro de ControlarEstado.
    /// A diferencia del ejemplo (un ciclo fijo con un único sucesor por estado), el
    /// ciclo de vida de una Prenda admite más de un destino válido por estado
    /// (Disponible puede ir a EnLimpieza O a Baja), así que ControlarEstado recibe el
    /// destino deseado en vez de tener un único "siguiente" hardcodeado.
    /// </summary>
    public abstract class Estado
    {
        /// <summary>
        /// Indica, SIN modificar nada, si la transición a <paramref name="destino"/> es
        /// válida para este estado. Única fuente de verdad de las reglas de transición
        /// (también la consulta <see cref="Prenda.TransicionPermitida"/>, de solo lectura).
        /// </summary>
        public abstract bool EsTransicionValida(EstadoPrenda destino);

        /// <summary>
        /// Si la transición a <paramref name="destino"/> es válida para este estado,
        /// la aplica sobre <paramref name="prenda"/> (Estado = destino) y devuelve true.
        /// Si no es válida, no modifica nada y devuelve false.
        /// </summary>
        public bool ControlarEstado(Prenda prenda, EstadoPrenda destino)
        {
            if (!EsTransicionValida(destino)) return false;
            prenda.Estado = destino;
            return true;
        }
    }
}
