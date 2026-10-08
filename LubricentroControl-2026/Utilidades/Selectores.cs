using System.Linq;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;

namespace LubricentroControl_2026.Utilidades
{
    // Opciones y textos de los selectores con búsqueda de cliente y de proveedor, que comparten
    // las pantallas donde se elige un titular (Vehículos, Turnos, Órdenes, Compras, Pagos). El
    // texto de cada opción es el mismo que se muestra una vez elegida.
    public static class Selectores
    {
        // Con el documento, para distinguir a los clientes que se llaman igual.
        public static string TextoCliente(Cliente cliente)
        {
            return cliente.Denominacion + " — " + cliente.Documento;
        }

        public static string TextoProveedor(Proveedor proveedor)
        {
            return proveedor.RazonSocial + " — CUIT " + Proveedor.FormatearCuit(proveedor.Cuit);
        }

        // Solo los activos: a uno dado de baja no se le cargan vehículos, turnos, órdenes,
        // compras ni pagos nuevos.
        public static string OpcionesClientes()
        {
            return Interfaz.OpcionesJson(ClienteDAL.Listar(incluirInactivos: false)
                .Select(c => new ListItem(TextoCliente(c), c.IdCliente.ToString())));
        }

        public static string OpcionesProveedores()
        {
            return Interfaz.OpcionesJson(ProveedorDAL.Listar(incluirInactivos: false)
                .Select(p => new ListItem(TextoProveedor(p), p.IdProveedor.ToString())));
        }
    }
}
