namespace BLL.Estrategias
{
    /// <summary>
    /// Patrón Strategy (PdN10 — Detección de clientes en riesgo de abandono). Misma
    /// estructura que el ejemplo de cátedra (Arma / Jugador): un único método abstracto
    /// que cada estrategia concreta resuelve con su propio criterio de negocio, y un
    /// contexto (BLL.AnalisisAbandono, equivalente a Jugador) que la mantiene en un
    /// campo y la intercambia en tiempo de ejecución vía CambiarEstrategia — le permite
    /// al Gerente Comercial elegir QUÉ criterio de riesgo aplicar antes de generar el
    /// reporte, en vez de tener el criterio fijo en el código.
    /// </summary>
    public abstract class EstrategiaRiesgo
    {
        public override string ToString() => GetType().Name;

        public abstract BE.ResultadoRiesgo Evaluar(BE.DatosClienteRiesgo datos);
    }
}
