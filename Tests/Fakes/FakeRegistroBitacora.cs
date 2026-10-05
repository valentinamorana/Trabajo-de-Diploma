using System.Collections.Generic;

namespace Tests.Fakes
{
    /// <summary>
    /// Doble en memoria de la bitácora del sistema: guarda lo que se registraría en vez de
    /// insertar en [Bitacora]. Se instala para TODO el proyecto de tests en
    /// <see cref="ConfiguracionTests.Inicializar"/>, así ninguna prueba escribe en una base real.
    /// </summary>
    public class FakeRegistroBitacora : Servicios.IRegistroBitacora
    {
        public readonly List<(string Modulo, string Actividad, BE.Criticidad Criticidad)> Registros =
            new List<(string, string, BE.Criticidad)>();

        public void Registrar(string modulo, string actividad, BE.Criticidad criticidad)
        {
            lock (Registros) Registros.Add((modulo, actividad, criticidad));
        }

        public void RegistrarSinSesion(string modulo, string actividad, BE.Criticidad criticidad,
                                       int? idUsuario = null, string detalle = null)
        {
            lock (Registros) Registros.Add((modulo, actividad, criticidad));
        }
    }

    /// <summary>Doble en memoria de la bitácora de negocio.</summary>
    public class FakeRegistroBitacoraNegocio : Servicios.IRegistroBitacoraNegocio
    {
        public readonly List<(BE.TipoEventoNegocio Tipo, string Descripcion)> Eventos =
            new List<(BE.TipoEventoNegocio, string)>();

        public void Registrar(BE.TipoEventoNegocio tipo, string descripcion,
                              int? idPedido = null, int? idPrenda = null, int? idCliente = null)
        {
            lock (Eventos) Eventos.Add((tipo, descripcion));
        }
    }
}
