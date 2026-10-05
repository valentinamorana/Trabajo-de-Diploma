using System;

namespace Servicios
{
    /// <summary>
    /// Contrato de ESCRITURA de la bitácora del sistema (tabla [Bitacora]). Lo consumen las
    /// clases de BLL para dejar constancia de cada operación; permite inyectar un doble de
    /// prueba para que los tests no escriban en la base real.
    /// </summary>
    public interface IRegistroBitacora
    {
        void Registrar(string modulo, string actividad, BE.Criticidad criticidad);
        void RegistrarSinSesion(string modulo, string actividad, BE.Criticidad criticidad,
                                int? idUsuario = null, string detalle = null);
    }

    /// <summary>Contrato de ESCRITURA de la bitácora de negocio (tabla [BitacoraNegocio]).</summary>
    public interface IRegistroBitacoraNegocio
    {
        void Registrar(BE.TipoEventoNegocio tipo, string descripcion,
                       int? idPedido = null, int? idPrenda = null, int? idCliente = null);
    }

    /// <summary>
    /// Punto único de creación de las bitácoras que usan las clases de BLL cuando no se les
    /// inyecta una por constructor. En producción devuelve las implementaciones reales; el
    /// proyecto de tests la reemplaza al iniciar (AssemblyInitialize) por dobles en memoria,
    /// de modo que ninguna prueba inserte filas en la bitácora de la base.
    /// </summary>
    public static class FabricaBitacora
    {
        private static Func<IRegistroBitacora>        _sistema = () => new Bitacora();
        private static Func<IRegistroBitacoraNegocio> _negocio = () => new BitacoraNegocio();

        public static Func<IRegistroBitacora> Sistema
        {
            get => _sistema;
            set => _sistema = value ?? (() => new Bitacora());
        }

        public static Func<IRegistroBitacoraNegocio> Negocio
        {
            get => _negocio;
            set => _negocio = value ?? (() => new BitacoraNegocio());
        }

        public static IRegistroBitacora        CrearSistema() => _sistema();
        public static IRegistroBitacoraNegocio CrearNegocio() => _negocio();
    }

    /// <summary>
    /// Recorte seguro de textos antes de insertarlos en columnas de largo fijo. Sin esto, un
    /// texto largo (por ej. un motivo extenso) hace fallar el INSERT y la entrada de bitácora
    /// se pierde en silencio (el error se captura para no cortar la operación de negocio).
    /// </summary>
    public static class TextoSeguro
    {
        private const string Sufijo = "…";

        public static string Recortar(string texto, int largoMaximo)
        {
            if (texto == null || largoMaximo <= 0 || texto.Length <= largoMaximo) return texto;
            int corte = largoMaximo - Sufijo.Length;
            // No partir un par sustituto (emoji, etc.) a la mitad.
            if (corte > 0 && char.IsHighSurrogate(texto[corte - 1])) corte--;
            return texto.Substring(0, Math.Max(corte, 0)) + Sufijo;
        }
    }
}
