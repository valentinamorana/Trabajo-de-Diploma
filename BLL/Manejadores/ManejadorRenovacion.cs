using System;

namespace BLL.Manejadores
{
    /// <summary>
    /// Patrón Chain of Responsibility (PdN5 — Renovación de suscripción). Equivalente a
    /// Aprobador del ejemplo de cátedra: clase abstracta con un campo protegido al
    /// sucesor, un método para encadenarlo (sin retorno) y un ÚNICO método abstracto
    /// donde cada eslabón concreto decide, dentro del mismo método, si atiende la
    /// petición o la delega a "DelegarASucesor(...)" — igual que
    /// Comprador/GerenteZonal/Director deciden inline según el importe de la Compra.
    /// </summary>
    public abstract class ManejadorRenovacion
    {
        private ManejadorRenovacion _sucesor;

        public void AgregarSiguiente(ManejadorRenovacion siguiente)
        {
            _sucesor = siguiente;
        }

        /// <summary>
        /// Delega al siguiente eslabón de la cadena. Si no hay uno configurado, falla con
        /// un mensaje claro de "cadena mal armada" en vez de una NullReferenceException
        /// genérica — hoy BLL.Renovacion arma la cadena completa así que esto nunca
        /// dispara, pero protege contra el día en que se agregue/reordene un eslabón y se
        /// olvide un AgregarSiguiente.
        /// </summary>
        protected ResultadoRenovacion DelegarASucesor(ContextoRenovacion contexto)
        {
            if (_sucesor == null)
                throw new InvalidOperationException(
                    "Cadena de Renovación mal configurada: " + GetType().Name +
                    " no tiene un sucesor asignado (falta AgregarSiguiente).");
            return _sucesor.Procesar(contexto);
        }

        public abstract ResultadoRenovacion Procesar(ContextoRenovacion contexto);
    }
}
