using System;
using BIZ.Data;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Cuenta corriente de proveedores (Fase 4). Admin y Encargado pueden ver el historial y
    // registrar ajustes manuales; Empleado, solo consulta (Requerimientos §5) — a diferencia de
    // Proveedores/Insumos, acá "solo consulta" no esconde toda la pantalla: Empleado igual puede
    // buscar un proveedor y ver su historial/saldo, solo se le esconde la franja de ajuste.
    public partial class CuentaCorrienteProveedores : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            CargarGrilla();
        }

        private void CargarGrilla()
        {
            var texto = txtBuscar.Text;

            gvProveedores.DataSource = string.IsNullOrWhiteSpace(texto)
                ? ProveedorDAL.Listar(incluirInactivos: true)
                : ProveedorDAL.Buscar(texto, incluirInactivos: true);
            gvProveedores.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void gvProveedores_RowCommand(object sender, System.Web.UI.WebControls.GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idProveedor)
        {
            var proveedor = ProveedorDAL.ObtenerPorId(idProveedor);
            if (proveedor == null)
            {
                MostrarMensaje("El proveedor no existe.", false);
                CargarGrilla();
                return;
            }

            ViewState["IdProveedor"] = idProveedor;

            pnlDetalle.Visible = true;
            pnlAjuste.Visible = !EsSoloLectura;
            litProveedorSeleccionado.Text = "Cuenta corriente: " + proveedor.RazonSocial;
            litSaldoActual.Text = CuentaCorrienteProveedorDAL.ObtenerSaldoActual(idProveedor).ToString("N2");

            gvHistorial.DataSource = CuentaCorrienteProveedorDAL.ListarPorProveedor(idProveedor);
            gvHistorial.DataBind();
        }

        protected void btnRegistrarAjuste_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            var idProveedor = LeerIdOculto(Convert.ToString(ViewState["IdProveedor"]));
            if (idProveedor <= 0)
            {
                MostrarMensaje("Seleccioná un proveedor antes de registrar un ajuste.", false);
                return;
            }

            decimal monto;
            decimal.TryParse(txtMontoAjuste.Text, out monto);

            var resultado = CuentaCorrienteProveedorDAL.RegistrarAjuste(
                idProveedor, monto, txtMotivoAjuste.Text, UsuarioActual.IdUsuario);

            MostrarMensaje(resultado.Mensaje, resultado.Exito);

            if (resultado.Exito)
            {
                txtMontoAjuste.Text = string.Empty;
                txtMotivoAjuste.Text = string.Empty;
                Seleccionar(idProveedor);
            }
        }

        // 0 (ID inexistente, cae en "no existe"/valida en falso) si el campo llegara vacío o
        // manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
        }

        // El mensaje ya viene con HTML armado por el llamador, no se re-escapa acá.
        private void MostrarMensaje(string mensajeHtml, bool exito)
        {
            pnlMensaje.CssClass = "alert " + (exito ? "alert-success" : "alert-danger");
            litMensaje.Text = mensajeHtml;
            pnlMensaje.Visible = true;
        }
    }
}
