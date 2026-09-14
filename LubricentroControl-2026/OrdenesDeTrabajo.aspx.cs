using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de órdenes de trabajo (Fase 3). Acceso completo para los 3 roles (Requerimientos §5).
    // Cliente/vehículo/turno quedan fijos una vez creada la orden (ver Docs/EstadoActual.md,
    // sesión de esta pantalla): el buscador/desplegables interactivos solo se muestran en
    // "Nueva orden"; editando una ya creada se ven como texto de solo lectura.
    public partial class OrdenesDeTrabajo : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            CargarFiltroEstado();
            CargarEstados();
            CargarCatalogos();
            LimpiarVehiculosYTurnos();
            CargarGrilla();

            // Vuelta de Clientes.aspx/Vehiculos.aspx (origen "orden", ver btnNuevoCliente_Click/
            // btnNuevoVehiculo_Click más abajo) — incluido "volver sin crear", que no manda
            // idClienteNuevo/idVehiculoNuevo pero igual necesita reponer kilometraje/
            // observaciones/turno tipeados antes de salir.
            if (Request.QueryString["kilometraje"] != null || Request.QueryString["observaciones"] != null
                || Request.QueryString["idTurno"] != null || Request.QueryString["idClienteNuevo"] != null
                || Request.QueryString["idVehiculoNuevo"] != null)
                RehidratarDesdeRetorno();
        }

        // Vuelta de Clientes.aspx/Vehiculos.aspx: repone los datos de la orden en curso y, si
        // corresponde, deja seleccionado el cliente/vehículo recién creado.
        private void RehidratarDesdeRetorno()
        {
            txtKilometraje.Text = Request.QueryString["kilometraje"];
            txtObservaciones.Text = Request.QueryString["observaciones"];

            var idVehiculoNuevo = Request.QueryString["idVehiculoNuevo"];
            var idClienteNuevo = Request.QueryString["idClienteNuevo"];
            if (idVehiculoNuevo != null)
            {
                var idCliente = LeerIdOculto(Request.QueryString["idCliente"]);
                SeleccionarCliente(idCliente);
                if (ddlVehiculo.Items.FindByValue(idVehiculoNuevo) != null)
                    ddlVehiculo.SelectedValue = idVehiculoNuevo;
            }
            else if (idClienteNuevo != null)
            {
                SeleccionarCliente(LeerIdOculto(idClienteNuevo));
            }

            var idTurno = Request.QueryString["idTurno"];
            if (!string.IsNullOrEmpty(idTurno) && ddlTurno.Items.FindByValue(idTurno) != null)
                ddlTurno.SelectedValue = idTurno;
        }

        // "Nuevo cliente"/"Nuevo vehículo": mandan a Clientes.aspx/Vehiculos.aspx los datos de
        // la orden en curso por query string (no PostBackUrl/PreviousPage — ver CLAUDE.md,
        // "Cross-page posting no funciona con FriendlyUrls").
        protected void btnNuevoCliente_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/Clientes"
                + "?origen=orden"
                + "&kilometraje=" + Server.UrlEncode(txtKilometraje.Text)
                + "&observaciones=" + Server.UrlEncode(txtObservaciones.Text)
                + "&idTurno=" + Server.UrlEncode(ddlTurno.SelectedValue));
        }

        protected void btnNuevoVehiculo_Click(object sender, EventArgs e)
        {
            var idCliente = LeerIdOculto(hdnIdCliente.Value);
            if (idCliente <= 0)
            {
                MostrarMensaje("Seleccioná un cliente antes de crear un vehículo.", false);
                return;
            }

            Response.Redirect("~/Vehiculos"
                + "?origen=orden"
                + "&idClienteActual=" + idCliente
                + "&kilometraje=" + Server.UrlEncode(txtKilometraje.Text)
                + "&observaciones=" + Server.UrlEncode(txtObservaciones.Text)
                + "&idTurno=" + Server.UrlEncode(ddlTurno.SelectedValue));
        }

        private void CargarFiltroEstado()
        {
            ddlFiltroEstado.Items.Clear();
            ddlFiltroEstado.Items.Add(new ListItem("(Todos)", ""));
            foreach (var estado in OrdenDeTrabajo.Estados)
                ddlFiltroEstado.Items.Add(new ListItem(estado, estado));
        }

        private void CargarEstados()
        {
            ddlEstado.Items.Clear();
            foreach (var estado in OrdenDeTrabajo.EstadosEditables)
                ddlEstado.Items.Add(new ListItem(estado, estado));
        }

        // Catálogos de las mini-altas de la franja de detalle. El de insumos muestra el stock
        // actual junto al nombre para que el operador vea si alcanza antes de agregar.
        private void CargarCatalogos()
        {
            ddlServicio.Items.Clear();
            foreach (var servicio in ServicioDAL.Listar(incluirInactivos: false))
                ddlServicio.Items.Add(new ListItem(servicio.Nombre, servicio.IdServicio.ToString()));

            ddlInsumo.Items.Clear();
            foreach (var insumo in InsumoDAL.Listar(incluirInactivos: false))
                ddlInsumo.Items.Add(new ListItem(
                    insumo.Nombre + " (stock: " + insumo.StockActual.ToString("N2") + ")",
                    insumo.IdInsumo.ToString()));
        }

        private void CargarGrilla()
        {
            var texto = txtBuscar.Text;
            var estado = ddlFiltroEstado.SelectedValue;

            gvOrdenes.DataSource = string.IsNullOrWhiteSpace(texto)
                ? OrdenDeTrabajoDAL.Listar(estado)
                : OrdenDeTrabajoDAL.Buscar(texto, estado);
            gvOrdenes.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void ddlFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        // --- Selector de cliente: buscador desplegable dentro del UpdatePanel -----------

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

            CargarVehiculosDelCliente(idCliente);
            CargarTurnosDelCliente(idCliente);
        }

        private void CargarVehiculosDelCliente(int idCliente)
        {
            LimpiarVehiculo();
            foreach (var vehiculo in VehiculoDAL.ListarPorCliente(idCliente, incluirInactivos: false))
                ddlVehiculo.Items.Add(new ListItem(vehiculo.Patente, vehiculo.IdVehiculo.ToString()));
        }

        private void CargarTurnosDelCliente(int idCliente)
        {
            LimpiarTurno();
            foreach (var turno in TurnoDAL.ListarPorCliente(idCliente))
                ddlTurno.Items.Add(new ListItem(
                    turno.FechaHoraAsignada.ToString("dd/MM HH:mm") + " — " + turno.Estado,
                    turno.IdTurno.ToString()));
        }

        // Dejan los desplegables con solo su placeholder. Tienen que quedar poblados así ya en
        // el primer Page_Load (sin cliente elegido todavía): un DropDownList sin ningún <option>
        // rechaza cualquier valor posteado, incluso "", con "Argumento de postback no válido"
        // (mismo bug encontrado y documentado en la sesión de Turnos.aspx).
        private void LimpiarVehiculo()
        {
            ddlVehiculo.Items.Clear();
            ddlVehiculo.Items.Add(new ListItem("(seleccioná un vehículo)", ""));
        }

        private void LimpiarTurno()
        {
            ddlTurno.Items.Clear();
            ddlTurno.Items.Add(new ListItem("(walk-in, sin turno)", ""));
        }

        private void LimpiarVehiculosYTurnos()
        {
            LimpiarVehiculo();
            LimpiarTurno();
        }

        // No usan ControlToValidate: validan la selección guardada en el hidden/dropdown, no
        // un TextBox.
        protected void valCliente_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = LeerIdOculto(hdnIdCliente.Value) > 0;
        }

        protected void valVehiculo_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = LeerIdOculto(ddlVehiculo.SelectedValue) > 0;
        }

        // --- ABM de la cabecera -----------------------------------------------------------

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            int kilometraje;
            var orden = new OrdenDeTrabajo
            {
                IdOrden = LeerIdOculto(hdnIdOrden.Value),
                IdCliente = LeerIdOculto(hdnIdCliente.Value),
                IdVehiculo = LeerIdOculto(ddlVehiculo.SelectedValue),
                IdTurno = LeerIdOculto(ddlTurno.SelectedValue) > 0 ? LeerIdOculto(ddlTurno.SelectedValue) : (int?)null,
                IdUsuario = UsuarioActual.IdUsuario,
                Kilometraje = int.TryParse(txtKilometraje.Text, out kilometraje) ? kilometraje : (int?)null,
                Observaciones = txtObservaciones.Text,
                Estado = ddlEstado.SelectedValue
            };

            var resultado = orden.IdOrden == 0
                ? OrdenDeTrabajoDAL.Crear(orden)
                : OrdenDeTrabajoDAL.Actualizar(orden);

            if (!resultado.Exito)
            {
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            CargarGrilla();
            Seleccionar(orden.IdOrden);
        }

        protected void btnCancelarOrden_Click(object sender, EventArgs e)
        {
            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var resultado = OrdenDeTrabajoDAL.Cancelar(idOrden, UsuarioActual.IdUsuario);

            MostrarMensaje(resultado.Mensaje, resultado.Exito);
            CargarGrilla();
            if (resultado.Exito) Seleccionar(idOrden);
        }

        protected void gvOrdenes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idOrden)
        {
            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            if (orden == null)
            {
                MostrarMensaje("La orden no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdOrden.Value = orden.IdOrden.ToString();
            hdnIdCliente.Value = orden.IdCliente.ToString();
            litClienteSeleccionado.Text = orden.NombreCliente + " — DNI " + orden.Dni;

            pnlSeleccionNueva.Visible = false;
            pnlSeleccionFija.Visible = true;
            litVehiculoInfo.Text = "Vehículo: " + orden.Patente;
            litTurnoInfo.Text = "Turno: " + DescribirTurno(orden.IdTurno);

            txtKilometraje.Text = orden.Kilometraje.HasValue ? orden.Kilometraje.Value.ToString() : string.Empty;
            txtObservaciones.Text = orden.Observaciones;

            var esTerminal = orden.Estado == OrdenDeTrabajo.EstadoCerrada || orden.Estado == OrdenDeTrabajo.EstadoCancelada;

            pnlEstado.Visible = true;
            ddlEstado.Visible = !esTerminal;
            litEstadoActual.Visible = esTerminal;
            if (esTerminal)
                litEstadoActual.Text = orden.Estado;
            else
                ddlEstado.SelectedValue = orden.Estado;

            btnGuardar.Visible = !esTerminal;
            btnCancelarOrden.Visible = !esTerminal;

            litTituloFormulario.Text = "Editar orden";

            pnlDetalle.Visible = true;
            pnlAgregarServicio.Visible = !esTerminal;
            pnlAgregarInsumo.Visible = !esTerminal;
            gvServicios.Columns[3].Visible = !esTerminal;
            CargarDetalle(idOrden);
        }

        private static string DescribirTurno(int? idTurno)
        {
            if (!idTurno.HasValue) return "(walk-in, sin turno)";

            var turno = TurnoDAL.ObtenerPorId(idTurno.Value);
            return turno == null
                ? "(walk-in, sin turno)"
                : turno.FechaHoraAsignada.ToString("dd/MM/yyyy HH:mm") + " — " + turno.Estado;
        }

        private void CargarDetalle(int idOrden)
        {
            gvServicios.DataSource = DetalleOrdenServicioDAL.ListarPorOrden(idOrden);
            gvServicios.DataBind();

            gvInsumosOrden.DataSource = DetalleOrdenInsumoDAL.ListarPorOrden(idOrden);
            gvInsumosOrden.DataBind();
        }

        // --- Franja de detalle: servicios ---------------------------------------------------

        protected void btnAgregarServicio_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var idServicio = LeerIdOculto(ddlServicio.SelectedValue);
            var cantidad = decimal.Parse(txtCantidadServicio.Text);

            var resultado = DetalleOrdenServicioDAL.Agregar(idOrden, idServicio, cantidad);

            MostrarMensaje(resultado.Mensaje, resultado.Exito);
            txtCantidadServicio.Text = "1";
            CargarDetalle(idOrden);
        }

        protected void gvServicios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Quitar") return;

            DetalleOrdenServicioDAL.Quitar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
            CargarDetalle(LeerIdOculto(hdnIdOrden.Value));
        }

        // --- Franja de detalle: insumos ------------------------------------------------------

        protected void btnAgregarInsumo_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var idInsumo = LeerIdOculto(ddlInsumo.SelectedValue);
            var cantidad = decimal.Parse(txtCantidadInsumo.Text);

            var resultado = DetalleOrdenInsumoDAL.Agregar(idOrden, idInsumo, cantidad, UsuarioActual.IdUsuario);

            MostrarMensaje(resultado.Mensaje, resultado.Exito);
            txtCantidadInsumo.Text = "1";
            CargarCatalogos();
            CargarDetalle(idOrden);
        }

        // 0 (ID inexistente, cae en "no existe"/valida en falso) si el campo oculto llegara
        // vacío o manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
        }

        private void LimpiarFormulario()
        {
            hdnIdOrden.Value = string.Empty;
            hdnIdCliente.Value = string.Empty;
            litClienteSeleccionado.Text = "(sin seleccionar)";
            txtBuscarCliente.Text = string.Empty;
            pnlResultadosCliente.Visible = false;
            pnlSeleccionNueva.Visible = true;
            pnlSeleccionFija.Visible = false;
            LimpiarVehiculosYTurnos();
            txtKilometraje.Text = string.Empty;
            txtObservaciones.Text = string.Empty;
            pnlEstado.Visible = false;
            btnGuardar.Visible = true;
            btnCancelarOrden.Visible = false;
            litTituloFormulario.Text = "Nueva orden";
            pnlDetalle.Visible = false;
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
