using System.Collections.Generic;

namespace BLL.Interfaces
{
    /// <summary>
    /// Gestión de Clientes.
    /// Define las operaciones que la capa de presentación puede invocar
    /// sin conocer los detalles de implementación.
    /// </summary>
    public interface IClienteService
    {
        // Devuelve todos los clientes con plan y stock utilizado.
        List<BE.Cliente> ObtenerTodos();

        // Obtiene un cliente por su ID.
        BE.Cliente ObtenerPorId(int idCliente);

        // Registra un nuevo cliente validando datos y unicidad de DNI.
        void Alta(string modulo, BE.Cliente cliente);

        // Modifica los datos de un cliente existente.
        void Modificar(string modulo, BE.Cliente cliente);

        // Elimina un cliente si no tiene prendas en uso.
        void Baja(string modulo, BE.Cliente cliente);

        // Evalúa si un cliente puede crear un pedido con la cantidad de prendas indicada.
        BE.EstadoComercialCliente ObtenerEstadoComercial(BE.Cliente cliente, int prendasSolicitadas);

        // PdN1 — Activa la suscripción de un cliente: asigna plan y modalidad de cobro,
        // calcula la vigencia (patrón Builder) y persiste el vencimiento.
        BE.Builders.Suscripcion ActivarSuscripcion(
            string modulo, BE.Cliente cliente, int idPlan, BE.Builders.ModalidadCobro modalidad);
    }
}
