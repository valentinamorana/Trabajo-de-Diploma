using BE;
using System;
using System.Data;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;

namespace Servicios
{
    /// <summary>Servicio para registrar y consultar la bitácora del sistema.</summary>
    public class Bitacora : IRegistroBitacora
    {
        private readonly DAL.Bitacora bitacoraDAL = new DAL.Bitacora();

        private const string IP_DESCONOCIDA = "IP desconocida";

        // Largos de las columnas de [Bitacora] (BD/00_Instalacion_Completa.sql): un texto más
        // largo haría fallar el INSERT y la entrada se perdería; se recorta antes de insertar.
        public const int LargoModulo    = 100;
        public const int LargoActividad = 200;
        public const int LargoDetalle   = 1000;
        public const int LargoIP        = 50;

        internal static BE.Bitacora Ajustar(BE.Bitacora b)
        {
            b.Modulo    = TextoSeguro.Recortar(b.Modulo, LargoModulo);
            b.Actividad = TextoSeguro.Recortar(b.Actividad, LargoActividad);
            b.Detalle   = TextoSeguro.Recortar(b.Detalle, LargoDetalle);
            b.IP        = TextoSeguro.Recortar(b.IP, LargoIP);
            return b;
        }

        // >Registra una actividad del usuario en sesión. No lanza si no hay sesión activa.
        public void Registrar(string modulo, string actividad, Criticidad criticidad)
        {
            if (!Seguridad.SessionManager.IsLoggedIn) return;

            var sesion = Seguridad.SessionManager.GetInstance();
            string ip  = ObtenerIPLocal();

            BE.Bitacora registro = new BE.Bitacora
            {
                Fecha      = DateTime.Now,
                IdUsuario  = (int?)sesion.Usuario.Id,
                Modulo     = modulo ?? string.Empty,
                Actividad  = actividad,
                Criticidad = criticidad,
                IP         = ip,
                Detalle    = $"Usuario '{sesion.Usuario.Username}' (ID: {sesion.Usuario.Id}) " +
                             $"realizó '{actividad}' en '{modulo}' " +
                             $"[Criticidad: {criticidad}] desde {ip} a las {DateTime.Now:HH:mm:ss}."
            };

            try
            {
                bitacoraDAL.Registrar(Ajustar(registro));
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    $"[Servicios.Bitacora.Registrar] Error al registrar: {ex.Message}");
            }
        }

        // Registra un evento sin requerir sesión activa (p. ej. intentos de login fallidos).
        public void RegistrarSinSesion(string modulo, string actividad, Criticidad criticidad,
                                        int? idUsuario = null, string detalle = null)
        {
            try
            {
                string ip = ObtenerIPLocal();
                bitacoraDAL.Registrar(Ajustar(new BE.Bitacora
                {
                    Fecha      = DateTime.Now,
                    IdUsuario  = idUsuario,
                    Modulo     = modulo ?? string.Empty,
                    Actividad  = actividad,
                    Criticidad = criticidad,
                    IP         = ip,
                    Detalle    = detalle ??
                                 $"Actividad '{actividad}' en '{modulo}' desde {ip} " +
                                 $"a las {DateTime.Now:HH:mm:ss}."
                }));
            }
            catch (Exception ex)
            {
                Trace.TraceError(
                    $"[Servicios.Bitacora.RegistrarSinSesion] Error al registrar: {ex.Message}");
            }
        }

        // Consultas 
        // Devuelve todos los registros de la bitácora.
        public DataTable ObtenerTodos()
        {
            return bitacoraDAL.ObtenerTodos();
        }

        // Devuelve los registros de los últimos N días.
        public DataTable ObtenerUltimosNDias(int dias)
        {
            if (dias < 1) dias = 1;
            return bitacoraDAL.ObtenerUltimosNDias(dias);
        }

        // Búsqueda filtrada por fecha, usuario, actividad y criticidad.
        public DataTable BuscarPorFiltros(
            DateTime? desde, DateTime? hasta,
            int idUsuario, string actividad, int criticidad)
        {
            return bitacoraDAL.BuscarPorFiltros(desde, hasta, idUsuario, actividad, criticidad);
        }

        // Obtiene la IP local del equipo.
        public static string ObtenerIPLocal()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                        return ip.ToString();
                }
            }
            catch (Exception ex)
            {
                Trace.TraceError($"[Servicios.Bitacora.ObtenerIPLocal] {ex.Message}");
            }
            return IP_DESCONOCIDA;
        }
    }
}
