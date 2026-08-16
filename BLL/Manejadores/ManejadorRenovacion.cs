namespace BLL.Manejadores
{
    /// <summary>
    /// Patrón Chain of Responsibility (PdN5 — Renovación de suscripción). Equivalente a
    /// Aprobador del ejemplo de cátedra: clase abstracta con un campo protegido al
    /// sucesor, un método para encadenarlo (sin retorno) y un ÚNICO método abstracto
    /// donde cada eslabón concreto decide, dentro del mismo método, si atiende la
    /// petición o la delega a "_sucesor.Procesar(...)" — igual que
    /// Comprador/GerenteZonal/Director deciden inline según el importe de la Compra.
    /// </summary>
    public abstract class ManejadorRenovacion
    {
        protected ManejadorRenovacion _sucesor;

        public void AgregarSiguiente(ManejadorRenovacion siguiente)
        {
            _sucesor = siguiente;
        }

        public abstract ResultadoRenovacion Procesar(ContextoRenovacion contexto);
    }
}
