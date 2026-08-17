using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Patrón Chain of Responsibility (PdN6 — Cobro y pago de suscripción). Misma
    /// estructura que ManejadorRenovacion / Aprobador del ejemplo de cátedra: clase
    /// abstracta con un campo protegido al sucesor, un método para encadenarlo (sin
    /// retorno) y un ÚNICO método abstracto donde cada eslabón concreto decide, dentro
    /// del mismo método, si atiende la petición o la delega a "DelegarASucesor(...)".
    /// </summary>
    public abstract class ManejadorCobro
    {
        private ManejadorCobro _sucesor;

        public void AgregarSiguiente(ManejadorCobro siguiente)
        {
            _sucesor = siguiente;
        }

        /// <summary>
        /// Delega al siguiente eslabón de la cadena. Si no hay uno configurado, falla con
        /// un mensaje claro de "cadena mal armada" en vez de una NullReferenceException
        /// genérica — hoy BLL.Cobro arma la cadena completa así que esto nunca dispara,
        /// pero protege contra el día en que se agregue/reordene un eslabón y se olvide
        /// un AgregarSiguiente.
        /// </summary>
        protected ResultadoCobro DelegarASucesor(ContextoCobro contexto)
        {
            if (_sucesor == null)
                throw new InvalidOperationException(
                    "Cadena de Cobro mal configurada: " + GetType().Name +
                    " no tiene un sucesor asignado (falta AgregarSiguiente).");
            return _sucesor.Procesar(contexto);
        }

        public abstract ResultadoCobro Procesar(ContextoCobro contexto);
    }
}
