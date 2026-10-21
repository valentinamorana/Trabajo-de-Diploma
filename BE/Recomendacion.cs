using System.Collections.Generic;
using System.Linq;

namespace BE
{
    public enum PrioridadRecomendacion
    {
        Alta = 0,
        Media = 1,
        Baja = 2
    }

    /// <summary>Cuánto respaldo de datos tiene la estimación (volumen de clientes, historia medida).</summary>
    public enum NivelConfianza
    {
        Alta = 0,
        Media = 1,
        Baja = 2
    }

    /// <summary>
    /// Qué hace una opción al aceptarla. TODA opción ejecuta algo con una operación que el sistema ya
    /// tiene (no hay opciones que solo registren la decisión ni que solo abran una pantalla); al
    /// concretarse, la decisión pasa a Ejecutada. La GUI pide la confirmación y llama a la BLL.
    /// Los valores numéricos son estables (los retirados no se reutilizan: 0 Informativa,
    /// 2 GestionarCobro, 5 VerPrendas y 6 VerMantenimiento).
    /// </summary>
    public enum AccionRecomendacion
    {
        /// <summary>Abre "Sugerir promoción" precargada → Administración → Contabilidad (PN03).</summary>
        SugerirPromocion = 1,
        /// <summary>Exporta la lista de contactos (CSV) para una campaña; guarda a quiénes se contactó
        /// para medir después cuántos renovaron o cambiaron de plan.</summary>
        ExportarContactos = 3,
        /// <summary>Genera la orden de compra sugerida (PDF).</summary>
        OrdenCompra = 4,
        /// <summary>Registra la «Solicitud de baja» de la promoción (camino existente de PN03).</summary>
        SolicitarBajaPromocion = 7,
        /// <summary>Extender: nueva sugerencia para el mismo plan (vuelve a pasar por Contabilidad).</summary>
        ExtenderPromocion = 8,
        /// <summary>Da de baja las prendas (Disponible → Baja, BLL.Prenda.CambiarEstado, patente de stock).</summary>
        DarDeBajaPrendas = 9,
        /// <summary>Manda las prendas a revisión (Disponible → En limpieza: abre un mantenimiento).</summary>
        EnviarAMantenimiento = 10
    }

    /// <summary>Renglón de la orden de compra sugerida.</summary>
    public class LineaOrdenCompra
    {
        public string Categoria { get; set; }
        public string Talle { get; set; }
        public int Cantidad { get; set; }
        public decimal CostoUnitario { get; set; }
        public decimal Subtotal => Cantidad * CostoUnitario;
    }

    /// <summary>Datos que la acción necesita para ejecutarse (precarga de la sugerencia, prendas, contactos…).</summary>
    public class ParametrosAccion
    {
        public int? IdPlan { get; set; }
        public decimal DescuentoPorcentaje { get; set; }
        public int DuracionDias { get; set; }
        public decimal TasaRetencion { get; set; }
        public decimal IngresoEnRiesgo { get; set; }
        /// <summary>Clientes objetivo de una opción de promoción (los que se busca retener).</summary>
        public int ClientesObjetivo { get; set; }
        public int? IdPromocion { get; set; }
        public List<int> Ids { get; set; } = new List<int>();
        public List<ClienteVencimiento> Contactos { get; set; } = new List<ClienteVencimiento>();
        public List<LineaOrdenCompra> Lineas { get; set; } = new List<LineaOrdenCompra>();
    }

    /// <summary>Una opción de decisión: la acción propuesta o una alternativa, con su impacto propio.</summary>
    public class OpcionRecomendacion
    {
        public TextoTraducible Descripcion { get; set; }
        /// <summary>Impacto económico estimado ($): positivo = gana o ahorra, negativo = inversión neta.</summary>
        public decimal ImpactoEstimado { get; set; }
        /// <summary>La fórmula con los números usados (se muestra para que el cálculo sea auditable).</summary>
        public TextoTraducible Formula { get; set; }
        public AccionRecomendacion Accion { get; set; }
        public ParametrosAccion Parametros { get; set; } = new ParametrosAccion();
    }

    /// <summary>
    /// PN03 «Recomendar / decidir» — Una recomendación accionable del motor (BLL.Inteligencia.MotorRecomendaciones):
    /// qué está pasando (diagnóstico, con números), por qué se recomienda (motivo), qué hacer (opción
    /// principal) y otras opciones con su propio impacto, para que el usuario elija. No se persiste:
    /// se recalcula en cada análisis; lo que se persiste es la decisión (BE.DecisionComercial).
    /// </summary>
    public class Recomendacion
    {
        /// <summary>Regla que la generó (ChurnPlan, Renovaciones, Escasez…).</summary>
        public string TipoRegla { get; set; }
        /// <summary>Identifica la situación (regla + objeto): así una decisión pospuesta o descartada
        /// se reconoce en el próximo análisis.</summary>
        public string Clave { get; set; }
        public PrioridadRecomendacion Prioridad { get; set; }
        public NivelConfianza Confianza { get; set; }
        public TextoTraducible Titulo { get; set; }
        public TextoTraducible Diagnostico { get; set; }
        public TextoTraducible Motivo { get; set; }
        public OpcionRecomendacion Principal { get; set; }
        public List<OpcionRecomendacion> Alternativas { get; set; } = new List<OpcionRecomendacion>();
        /// <summary>Renglones de detalle (clientes, prendas, vendedores) con datos, no texto traducible.</summary>
        public List<string> Detalle { get; set; } = new List<string>();

        /// <summary>Última decisión registrada sobre esta situación (la completa el servicio; null si no hay).</summary>
        public DecisionComercial UltimaDecision { get; set; }

        /// <summary>
        /// Por qué el motor subió la prioridad (p. ej. "la meta de % de bajas está en riesgo"); null si
        /// la prioridad es la que calculó la regla. Ver BLL.Inteligencia.MetasComerciales.ElevarPrioridad.
        /// </summary>
        public TextoTraducible PrioridadElevada { get; set; }

        public decimal ImpactoEstimado => Principal?.ImpactoEstimado ?? 0m;

        /// <summary>Opción principal y alternativas en orden (índice 0 = principal).</summary>
        public List<OpcionRecomendacion> Opciones()
        {
            var lista = new List<OpcionRecomendacion>();
            if (Principal != null) lista.Add(Principal);
            if (Alternativas != null) lista.AddRange(Alternativas.Where(a => a != null));
            return lista;
        }
    }
}
