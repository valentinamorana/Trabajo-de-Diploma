namespace BLL.Manejadores
{
    /// <summary>
    /// Patrón Chain of Responsibility (PdN6 — Cobro y pago de suscripción). Misma
    /// estructura que ManejadorRenovacion / Aprobador del ejemplo de cátedra: clase
    /// abstracta con un campo protegido al sucesor, un método para encadenarlo (sin
    /// retorno) y un ÚNICO método abstracto donde cada eslabón concreto decide, dentro
    /// del mismo método, si atiende la petición o la delega a "_sucesor.Procesar(...)".
    /// </summary>
    public abstract class ManejadorCobro
    {
        protected ManejadorCobro _sucesor;

        public void AgregarSiguiente(ManejadorCobro siguiente)
        {
            _sucesor = siguiente;
        }

        public abstract ResultadoCobro Procesar(ContextoCobro contexto);
    }
}
