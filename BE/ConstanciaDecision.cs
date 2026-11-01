using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Xml.Serialization;

namespace BE
{
    /// <summary>
    /// PN03 + A02 Serialización: «Constancia de la decisión»: foto de lo que Gerencia vio en el Centro de
    /// decisiones comerciales al Aceptar, Descartar o Posponer una recomendación. La arma
    /// BLL.Inteligencia.ConstanciasDecision, se guarda en XML en DecisionComercial.Constancia (la ven todos
    /// los usuarios y PCs) y se exporta o importa en XML o JSON con los serializadores de
    /// Servicios.Serializacion (FabricaSerializadores; sin DTD ni entidades externas, con tamaño máximo).
    ///
    /// Pensada para leerse a mano: nombres de campo explícitos y estables, orden fijo (Version primero),
    /// fechas ISO sin fracción ni zona (yyyy-MM-ddTHH:mm:ss) y textos en español, como la bitácora.
    /// Al abrir un archivo se compara campo por campo con la constancia guardada en la base (no el texto):
    /// así se detecta un archivo adulterado.
    /// </summary>
    [Serializable]
    [XmlRoot("ConstanciaDecisionWardrobeFlow")]
    [DataContract(Name = "ConstanciaDecisionWardrobeFlow", Namespace = "")]
    public class ConstanciaDecision
    {
        /// <summary>Versión del formato que escribe y admite esta aplicación.</summary>
        public const int FormatoActual = 1;

        /// <summary>Sin valor por defecto: si falta queda en 0 y la lectura la rechaza.</summary>
        [XmlAttribute("version")]
        [DataMember(Order = 1, IsRequired = true)]
        public int Version { get; set; }

        [DataMember(Order = 2, IsRequired = true)]
        public int IdDecision { get; set; }

        /// <summary>Fecha y hora de la decisión (segundos, sin zona).</summary>
        [DataMember(Order = 3)]
        public DateTime Fecha { get; set; }

        [DataMember(Order = 4)]
        public string Username { get; set; }

        [DataMember(Order = 5)]
        public string NombreUsuario { get; set; }

        /// <summary>Aceptada, Descartada o Pospuesta.</summary>
        [DataMember(Order = 6)]
        public string Accion { get; set; }

        [DataMember(Order = 7)]
        public string Motivo { get; set; }

        [DataMember(Order = 8)]
        public DateTime? PospuestaHasta { get; set; }

        [DataMember(Order = 9, IsRequired = true)]
        public ConstanciaRecomendacion Recomendacion { get; set; } = new ConstanciaRecomendacion();

        [DataMember(Order = 10)]
        public ConstanciaOpcion OpcionElegida { get; set; } = new ConstanciaOpcion();

        [XmlArray("Alternativas"), XmlArrayItem("Opcion")]
        [DataMember(Order = 11)]
        public List<ConstanciaOpcion> Alternativas { get; set; } = new List<ConstanciaOpcion>();

        [DataMember(Order = 12)]
        public ConstanciaPeriodo Periodo { get; set; } = new ConstanciaPeriodo();

        [XmlArray("Indicadores"), XmlArrayItem("Indicador")]
        [DataMember(Order = 13)]
        public List<ConstanciaIndicador> Indicadores { get; set; } = new List<ConstanciaIndicador>();

        /// <summary>Valores de los «Parámetros del análisis» (umbrales) con los que se calculó.</summary>
        [XmlArray("ParametrosDelAnalisis"), XmlArrayItem("Parametro")]
        [DataMember(Order = 14)]
        public List<ConstanciaParametro> ParametrosDelAnalisis { get; set; } = new List<ConstanciaParametro>();

        /// <summary>Sobre qué base se decidió: los parámetros y las fórmulas usadas, en texto.</summary>
        [XmlArray("Supuestos"), XmlArrayItem("Supuesto")]
        [DataMember(Order = 15)]
        public List<string> Supuestos { get; set; } = new List<string>();

        /// <summary>Avisos del panel al decidir (datos incompletos, etc.).</summary>
        [XmlArray("Advertencias"), XmlArrayItem("Advertencia")]
        [DataMember(Order = 16)]
        public List<string> Advertencias { get; set; } = new List<string>();
    }

    [Serializable]
    [DataContract(Name = "Recomendacion", Namespace = "")]
    public class ConstanciaRecomendacion
    {
        [DataMember(Order = 1)] public string Clave { get; set; }
        [DataMember(Order = 2)] public string Titulo { get; set; }
        [DataMember(Order = 3)] public string Regla { get; set; }
        [DataMember(Order = 4)] public string Prioridad { get; set; }
        [DataMember(Order = 5)] public string Confianza { get; set; }
        [DataMember(Order = 6)] public string Diagnostico { get; set; }
        [DataMember(Order = 7)] public string Motivo { get; set; }
    }

    [Serializable]
    [DataContract(Name = "Opcion", Namespace = "")]
    public class ConstanciaOpcion
    {
        [DataMember(Order = 1)] public string Descripcion { get; set; }
        [DataMember(Order = 2)] public string Accion { get; set; }
        [DataMember(Order = 3)] public decimal ImpactoEstimado { get; set; }
        [DataMember(Order = 4)] public string Formula { get; set; }
    }

    [Serializable]
    [DataContract(Name = "Periodo", Namespace = "")]
    public class ConstanciaPeriodo
    {
        [DataMember(Order = 1)] public DateTime Desde { get; set; }
        [DataMember(Order = 2)] public DateTime Hasta { get; set; }
        [DataMember(Order = 3)] public int? IdPlan { get; set; }
        [DataMember(Order = 4)] public string Plan { get; set; }
    }

    [Serializable]
    [DataContract(Name = "Indicador", Namespace = "")]
    public class ConstanciaIndicador
    {
        [DataMember(Order = 1)] public string Clave { get; set; }
        [DataMember(Order = 2)] public string Nombre { get; set; }
        [DataMember(Order = 3)] public string Unidad { get; set; }
        [DataMember(Order = 4)] public decimal Valor { get; set; }
        [DataMember(Order = 5)] public decimal? Anterior { get; set; }
        [DataMember(Order = 6)] public decimal? Variacion { get; set; }
    }

    [Serializable]
    [DataContract(Name = "Parametro", Namespace = "")]
    public class ConstanciaParametro
    {
        [DataMember(Order = 1)] public string Clave { get; set; }
        [DataMember(Order = 2)] public string Nombre { get; set; }
        [DataMember(Order = 3)] public decimal Valor { get; set; }
    }

    /// <summary>Resultado de comparar una constancia abierta desde un archivo con la registrada en el sistema.</summary>
    public enum EstadoVerificacionConstancia
    {
        /// <summary>La decisión existe y su constancia coincide campo por campo.</summary>
        Coincide = 0,
        /// <summary>La decisión existe y su constancia NO coincide: el archivo fue modificado.</summary>
        Modificada = 1,
        /// <summary>La decisión no existe en esta base.</summary>
        SinRegistro = 2,
        /// <summary>La decisión existe pero no tiene constancia guardada (anterior a la 1.6.1).</summary>
        SinConstanciaEnSistema = 3
    }

    /// <summary>Constancia leída de un archivo + su verificación contra la base.</summary>
    public class ConstanciaVerificada
    {
        public ConstanciaDecision Constancia { get; set; }
        public EstadoVerificacionConstancia Estado { get; set; }
        /// <summary>Campos que difieren (ruta del campo: valor del archivo ≠ valor registrado).</summary>
        public List<string> Diferencias { get; set; } = new List<string>();
        public string FormatoArchivo { get; set; }
    }
}
