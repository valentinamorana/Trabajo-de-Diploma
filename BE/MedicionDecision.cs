using System;

namespace BE
{
    /// <summary>
    /// Estado del resultado que el sistema mide para una decisión (BLL.Inteligencia.InteligenciaComercial.MedirDecisiones):
    ///   SinDato    → todavía no hay con qué medirla (p. ej. la promoción no tiene datos del período).
    ///   EnMedicion → campaña de contacto con la ventana de medición abierta: NO hay importe, solo el avance
    ///                (cuántos contactados respondieron y cuándo cierra la ventana). No se suma en los totales.
    ///   Medido     → resultado final (puede ser negativo si la campaña quedó debajo de la rutina).
    /// </summary>
    public enum EstadoMedicion
    {
        SinDato = 0,
        EnMedicion = 1,
        Medido = 2
    }

    /// <summary>Resultado medido de una decisión, con su estado explícito (ver <see cref="EstadoMedicion"/>).</summary>
    public sealed class MedicionDecision
    {
        public EstadoMedicion Estado { get; set; }
        /// <summary>Importe medido ($); solo con Estado = Medido (null en los demás estados).</summary>
        public decimal? Valor { get; set; }
        /// <summary>Campañas de contacto: clientes contactados atribuidos a la campaña (0 en promociones).</summary>
        public int Contactados { get; set; }
        /// <summary>Campañas de contacto: contactados que respondieron hasta hoy (o al cierre).</summary>
        public int Respondieron { get; set; }
        /// <summary>Campañas de contacto: día en que cierra la ventana de medición (ese día ya se muestra el importe).</summary>
        public DateTime? FechaCierre { get; set; }

        public bool EsFinal => Estado == EstadoMedicion.Medido;

        public static MedicionDecision Medida(decimal valor) =>
            new MedicionDecision { Estado = EstadoMedicion.Medido, Valor = valor };

        public static MedicionDecision EnCurso(int respondieron, int contactados, DateTime fechaCierre) =>
            new MedicionDecision { Estado = EstadoMedicion.EnMedicion, Respondieron = respondieron, Contactados = contactados, FechaCierre = fechaCierre };

        public static MedicionDecision SinDatos() => new MedicionDecision { Estado = EstadoMedicion.SinDato };
    }

    /// <summary>
    /// Totales del historial de decisiones (BLL.Inteligencia.MedicionDecisiones.Resumir). Las mediciones
    /// en curso NO entran en las sumas: solo se cuentan (<see cref="EnMedicion"/>).
    /// </summary>
    public sealed class ResumenMediciones
    {
        public int Decisiones { get; set; }
        /// <summary>Aceptadas o ejecutadas (las que admiten un resultado) y la suma de su impacto estimado.</summary>
        public int Aceptadas { get; set; }
        public decimal EstimadoAceptadas { get; set; }
        /// <summary>Con resultado real registrado a mano: cuántas, su estimado y su real.</summary>
        public int ConReal { get; set; }
        public decimal EstimadoConReal { get; set; }
        public decimal Real { get; set; }
        /// <summary>Con resultado final medido por el sistema (ventana cerrada o promoción medida).</summary>
        public int Medidas { get; set; }
        public decimal EstimadoMedidas { get; set; }
        public decimal Medido { get; set; }
        /// <summary>Campañas todavía en medición: no suman en <see cref="Medido"/> ni en <see cref="EstimadoMedidas"/>.</summary>
        public int EnMedicion { get; set; }
    }
}
