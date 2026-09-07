using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de vehículos. Acceso completo para los 3 roles (Requerimientos §5). El dueño se
    // elige con el mismo buscador de clientes que usa la pantalla Clientes, mostrado como un
    // desplegable dentro de un UpdatePanel en vez de un DropDownList con todos los clientes
    // (Requerimientos §9.2).
    public partial class Vehiculos : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CargarTiposCombustible();
            CargarGrilla();

            if (Request.QueryString["idClienteNuevo"] != null)
                RehidratarDesdeRetorno();
        }

        // Vuelta de "Nuevo cliente" en Clientes.aspx (ver Clientes.aspx.cs, ArmarUrlVuelta):
        // repone los datos del vehículo que estaba en curso y deja seleccionado el cliente
        // que se creó (o el que ya estaba elegido, si solo se volvió sin crear ninguno).
        private void RehidratarDesdeRetorno()
        {
            hdnIdVehiculo.Value = Request.QueryString["idVehiculo"];
            hdnActivo.Value = Request.QueryString["activo"];
            txtPatente.Text = Request.QueryString["patente"];
            txtMarca.Text = Request.QueryString["marca"];
            txtModelo.Text = Request.QueryString["modelo"];
            txtAnio.Text = Request.QueryString["anio"];

            var tipoCombustible = Request.QueryString["tipoCombustible"] ?? string.Empty;
            if (ddlTipoCombustible.Items.FindByValue(tipoCombustible) != null)
                ddlTipoCombustible.SelectedValue = tipoCombustible;

            SeleccionarCliente(LeerIdOculto(Request.QueryString["idClienteNuevo"]));

            var esEdicion = LeerIdOculto(hdnIdVehiculo.Value) > 0;
            btnBorrar.Visible = esEdicion && ActivoDesdeHidden();
            litTituloFormulario.Text = esEdicion ? "Editar vehículo" : "Nuevo vehículo";
        }

        // "Nuevo cliente" al lado del buscador: manda a Clientes.aspx los datos del
        // vehículo en curso por query string (no PostBackUrl/PreviousPage — esos rompen
        // acá porque FriendlyUrls no publica un archivo físico en la ruta amigable, y
        // PreviousPage necesita reconstruir la página de origen a partir de esa ruta).
        protected void btnNuevoCliente_Click(object sender, EventArgs e)
        {
            var url = "~/Clientes"
                + "?origen=vehiculo"
                + "&idVehiculo=" + Server.UrlEncode(hdnIdVehiculo.Value)
                + "&activo=" + Server.UrlEncode(hdnActivo.Value)
                + "&idClienteActual=" + LeerIdOculto(hdnIdCliente.Value)
                + "&patente=" + Server.UrlEncode(txtPatente.Text)
                + "&marca=" + Server.UrlEncode(txtMarca.Text)
                + "&modelo=" + Server.UrlEncode(txtModelo.Text)
                + "&anio=" + Server.UrlEncode(txtAnio.Text)
                + "&tipoCombustible=" + Server.UrlEncode(ddlTipoCombustible.SelectedValue);

            Response.Redirect(url);
        }

        private void CargarTiposCombustible()
        {
            ddlTipoCombustible.Items.Clear();
            ddlTipoCombustible.Items.Add(new ListItem("(sin especificar)", ""));
            foreach (var tipo in Vehiculo.TiposCombustible)
                ddlTipoCombustible.Items.Add(new ListItem(tipo, tipo));
        }

        private void CargarGrilla()
        {
            var incluirInactivos = chkIncluirInactivos.Checked;
            var texto = txtBuscar.Text;

            gvVehiculos.DataSource = string.IsNullOrWhiteSpace(texto)
                ? VehiculoDAL.Listar(incluirInactivos)
                : VehiculoDAL.Buscar(texto, incluirInactivos);
            gvVehiculos.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void chkIncluirInactivos_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        // --- Selector de dueño: buscador desplegable dentro del UpdatePanel -----------

        protected void btnBuscarCliente_Click(object sender, EventArgs e)
        {
            rptResultadosCliente.DataSource = ClienteDAL.Buscar(txtBuscarCliente.Text, incluirInactivos: false);
            rptResultadosCliente.DataBind();
            pnlResultadosCliente.Visible = true;
        }

        protected void rptResultadosCliente_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

            SeleccionarCliente(LeerIdOculto(Convert.ToString(e.CommandArgument)));

            pnlResultadosCliente.Visible = false;
            txtBuscarCliente.Text = string.Empty;
        }

        private void SeleccionarCliente(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null) return;

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            litClienteSeleccionado.Text = cliente.NombreCompleto + " — DNI " + cliente.Dni;
        }

        // No usa ControlToValidate: valida la selección guardada en el hidden, no un TextBox.
        protected void valCliente_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = LeerIdOculto(hdnIdCliente.Value) > 0;
        }

        protected void valPatente_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Vehiculo.EsPatenteValida(args.Value);
        }

        // --- ABM ------------------------------------------------------------------------

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            int anioParsed;
            int? anio = int.TryParse(txtAnio.Text, out anioParsed) ? anioParsed : (int?)null;

            var vehiculo = new Vehiculo
            {
                IdVehiculo = LeerIdOculto(hdnIdVehiculo.Value),
                IdCliente = LeerIdOculto(hdnIdCliente.Value),
                Patente = txtPatente.Text,
                Marca = txtMarca.Text,
                Modelo = txtModelo.Text,
                Anio = anio,
                TipoCombustible = ddlTipoCombustible.SelectedValue,
                Activo = ActivoDesdeHidden()
            };

            var resultado = vehiculo.IdVehiculo == 0
                ? VehiculoDAL.Crear(vehiculo)
                : VehiculoDAL.Actualizar(vehiculo);

            if (!resultado.Exito)
            {
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            var baja = VehiculoDAL.Desactivar(LeerIdOculto(hdnIdVehiculo.Value));

            MostrarMensaje(baja.Mensaje, baja.Exito);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvVehiculos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idVehiculo)
        {
            var vehiculo = VehiculoDAL.ObtenerPorId(idVehiculo);
            if (vehiculo == null)
            {
                MostrarMensaje("El vehículo no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdVehiculo.Value = vehiculo.IdVehiculo.ToString();
            hdnActivo.Value = vehiculo.Activo.ToString();
            SeleccionarCliente(vehiculo.IdCliente);
            txtPatente.Text = vehiculo.Patente;
            txtMarca.Text = vehiculo.Marca;
            txtModelo.Text = vehiculo.Modelo;
            txtAnio.Text = vehiculo.Anio.HasValue ? vehiculo.Anio.Value.ToString() : string.Empty;
            ddlTipoCombustible.SelectedValue = vehiculo.TipoCombustible ?? string.Empty;
            btnBorrar.Visible = vehiculo.Activo;

            litTituloFormulario.Text = "Editar vehículo";
        }

        // Por defecto activo si el campo oculto llegara vacío o manipulado
        // (ej. un POST armado a mano sin ese campo).
        private bool ActivoDesdeHidden()
        {
            bool activo;
            return !bool.TryParse(hdnActivo.Value, out activo) || activo;
        }

        // 0 (ID inexistente, cae en "no existe"/valida en falso) si el campo oculto
        // llegara vacío o manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
        }

        private void LimpiarFormulario()
        {
            hdnIdVehiculo.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            hdnIdCliente.Value = string.Empty;
            litClienteSeleccionado.Text = "(sin seleccionar)";
            txtBuscarCliente.Text = string.Empty;
            pnlResultadosCliente.Visible = false;
            txtPatente.Text = string.Empty;
            txtMarca.Text = string.Empty;
            txtModelo.Text = string.Empty;
            txtAnio.Text = string.Empty;
            ddlTipoCombustible.SelectedIndex = 0;
            btnBorrar.Visible = false;
            litTituloFormulario.Text = "Nuevo vehículo";
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
