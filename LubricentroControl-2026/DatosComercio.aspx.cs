using System;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // Datos del comercio que factura (Requerimientos §9.10): los que salen como emisor en las
    // facturas de Ventas (FacturaDAL.Emitir los copia en cada una). Por defecto solo la ve Admin,
    // en la sección Administración; con la pantalla en solo consulta se ven los datos sin poder
    // cambiarlos.
    public partial class DatosComercio : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // El aviso se muestra una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;

            valPuntoVenta.MinimumValue = DatosEmisor.PuntoVentaMinimo.ToString(CultureInfo.InvariantCulture);
            valPuntoVenta.MaximumValue = DatosEmisor.PuntoVentaMaximo.ToString(CultureInfo.InvariantCulture);
            valPuntoVenta.ErrorMessage = "El punto de venta va de " + DatosEmisor.PuntoVentaMinimo +
                                         " a " + DatosEmisor.PuntoVentaMaximo + ".";

            if (IsPostBack) return;

            foreach (var condicion in DatosEmisor.Condiciones)
                ddlCondicionIva.Items.Add(new ListItem(condicion, condicion));

            CargarDatos();

            if (EsSoloLectura)
            {
                pnlFormulario.Enabled = false;
                pnlFormulario.DefaultButton = string.Empty;
                btnGuardar.Visible = false;
            }
        }

        private void CargarDatos()
        {
            var emisor = EmisorDAL.Obtener();
            if (emisor == null)
            {
                txtPuntoVenta.Text = DatosEmisor.PuntoVentaMinimo.ToString(CultureInfo.InvariantCulture);
                MostrarMensaje("Todavía no se cargaron los datos del comercio: sin ellos no se pueden emitir facturas.", false);
                return;
            }

            txtRazonSocial.Text = emisor.RazonSocial;
            txtCuit.Text = Proveedor.FormatearCuit(emisor.Cuit);
            txtDomicilio.Text = emisor.Domicilio;
            ddlCondicionIva.SelectedValue = emisor.CondicionIva;
            txtPuntoVenta.Text = emisor.PuntoVenta.ToString(CultureInfo.InvariantCulture);
            txtIngresosBrutos.Text = emisor.IngresosBrutos;
            txtInicioActividades.Text = emisor.InicioActividades.HasValue
                ? emisor.InicioActividades.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
                : string.Empty;
        }

        protected void valCuit_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Proveedor.EsCuitValido(args.Value);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            // El input type="date" postea siempre en yyyy-MM-dd, sin importar la cultura.
            DateTime? inicioActividades = null;
            if (!string.IsNullOrWhiteSpace(txtInicioActividades.Text))
            {
                DateTime fecha;
                if (!DateTime.TryParseExact(txtInicioActividades.Text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out fecha))
                {
                    MostrarMensaje("La fecha de inicio de actividades no es válida.", false);
                    return;
                }
                inicioActividades = fecha;
            }

            int puntoVenta;
            int.TryParse(txtPuntoVenta.Text, NumberStyles.None, CultureInfo.InvariantCulture, out puntoVenta);

            var emisor = new DatosEmisor
            {
                RazonSocial = txtRazonSocial.Text,
                Cuit = txtCuit.Text,
                CondicionIva = ddlCondicionIva.SelectedValue,
                Domicilio = txtDomicilio.Text,
                IngresosBrutos = txtIngresosBrutos.Text,
                InicioActividades = inicioActividades,
                PuntoVenta = puntoVenta
            };

            var resultado = EmisorDAL.Guardar(emisor);
            MostrarMensaje(HttpUtility.HtmlEncode(resultado.Mensaje), resultado.Exito);

            // Vuelve a mostrar lo guardado ya normalizado (el CUIT con sus guiones).
            if (resultado.Exito) CargarDatos();
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
