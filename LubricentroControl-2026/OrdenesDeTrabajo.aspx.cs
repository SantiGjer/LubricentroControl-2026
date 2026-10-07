using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de órdenes de trabajo (Fase 3). Acceso completo para Admin, Encargado y Empleado
    // (Requerimientos §5); Lectura, solo consulta. La lista arranca filtrada en las órdenes
    // Abiertas, de la más nueva a la más vieja. Alta y edición en un modal: cliente/vehículo/turno
    // quedan fijos una vez creada la orden (ver Docs/EstadoActual.md, sesión de esta pantalla):
    // el selector y los desplegables solo se muestran en "Nueva orden"; editando una ya creada se
    // ven como texto fijo, junto con el detalle de servicios e insumos.
    public partial class OrdenesDeTrabajo : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvOrdenes.Columns.
        private const int ColumnaAcciones = 4;

        // Índice de la columna "Acciones" (Quitar) en gvServicios/gvInsumosOrden.
        private const int ColumnaQuitar = 3;

        private const string IdModal = "modalOrden";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlMensajeFormulario.Visible = false;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
                gvOrdenes.Columns[ColumnaAcciones].Visible = false;
            }

            CargarFiltroEstado();
            CargarEstados();
            CargarCatalogos();
            LimpiarFormulario();
            CargarGrilla();

            if (EsSoloLectura) return;

            // Vuelta de Clientes.aspx/Vehiculos.aspx (origen "orden", ver btnNuevoCliente_Click/
            // btnNuevoVehiculo_Click más abajo) — incluido "volver sin crear", que no manda
            // idClienteNuevo/idVehiculoNuevo pero igual necesita reponer kilometraje/
            // observaciones/turno tipeados antes de salir.
            if (Request.QueryString["kilometraje"] != null || Request.QueryString["observaciones"] != null
                || Request.QueryString["idTurno"] != null || Request.QueryString["idClienteNuevo"] != null
                || Request.QueryString["idVehiculoNuevo"] != null)
                RehidratarDesdeRetorno();
        }

        // Opciones del selector de cliente (los clientes activos). Se evalúa al dibujar el modal.
        protected string OpcionesClientes
        {
            get { return Selectores.OpcionesClientes(); }
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

            MostrarFormulario();
        }

        // "Nuevo cliente"/"Nuevo vehículo": mandan a Clientes.aspx/Vehiculos.aspx los datos de
        // la orden en curso por query string (no PostBackUrl/PreviousPage — ver CLAUDE.md,
        // "Cross-page posting no funciona con FriendlyUrls").
        protected void btnNuevoCliente_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            Response.Redirect("~/Clientes"
                + "?origen=orden"
                + "&kilometraje=" + Server.UrlEncode(txtKilometraje.Text)
                + "&observaciones=" + Server.UrlEncode(txtObservaciones.Text)
                + "&idTurno=" + Server.UrlEncode(ddlTurno.SelectedValue));
        }

        protected void btnNuevoVehiculo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idCliente = LeerIdOculto(hdnIdCliente.Value);
            if (idCliente <= 0)
            {
                MostrarMensajeFormulario("Seleccioná un cliente antes de crear un vehículo.", false);
                return;
            }

            Response.Redirect("~/Vehiculos"
                + "?origen=orden"
                + "&idClienteActual=" + idCliente
                + "&kilometraje=" + Server.UrlEncode(txtKilometraje.Text)
                + "&observaciones=" + Server.UrlEncode(txtObservaciones.Text)
                + "&idTurno=" + Server.UrlEncode(ddlTurno.SelectedValue));
        }

        // Arranca en "Abierta": lo que se busca casi siempre al entrar son las órdenes nuevas que
        // todavía no se empezaron. Las demás se ven cambiando el filtro.
        private void CargarFiltroEstado()
        {
            ddlFiltroEstado.Items.Clear();
            ddlFiltroEstado.Items.Add(new ListItem("(Todos)", ""));
            foreach (var estado in OrdenDeTrabajo.Estados)
                ddlFiltroEstado.Items.Add(new ListItem(estado, estado));

            ddlFiltroEstado.SelectedValue = OrdenDeTrabajo.EstadoAbierta;
        }

        private void CargarEstados()
        {
            ddlEstado.Items.Clear();
            foreach (var estado in OrdenDeTrabajo.EstadosEditables)
                ddlEstado.Items.Add(new ListItem(estado, estado));
        }

        // Catálogos de las mini-altas del detalle. El de insumos muestra el stock actual junto
        // al nombre para que el operador vea si alcanza antes de agregar.
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

        // El estado filtra en el servidor; el texto, la tabla en el navegador (Lubricentro.js).
        // OrdenDeTrabajoDAL.Listar ya las trae de la más nueva a la más vieja.
        private void CargarGrilla()
        {
            gvOrdenes.DataSource = OrdenDeTrabajoDAL.Listar(ddlFiltroEstado.SelectedValue);
            gvOrdenes.DataBind();
        }

        // El filtro de la tabla busca también por DNI, que no es una columna: va en data-buscar.
        protected void gvOrdenes_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            e.Row.Attributes["data-buscar"] = ((OrdenDeTrabajo)e.Row.DataItem).Dni;
        }

        protected void ddlFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        // --- Selector de cliente -----------------------------------------------------------

        // Lo dispara el selector con búsqueda al elegir un cliente (postback parcial del
        // UpdatePanel): carga sus vehículos y turnos.
        protected void hdnIdCliente_ValueChanged(object sender, EventArgs e)
        {
            SeleccionarCliente(LeerIdOculto(hdnIdCliente.Value));
        }

        private void SeleccionarCliente(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null)
            {
                hdnIdCliente.Value = string.Empty;
                txtCliente.Text = string.Empty;
                LimpiarVehiculosYTurnos();
                return;
            }

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            txtCliente.Text = Selectores.TextoCliente(cliente);

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
            if (EsSoloLectura) return;

            LimpiarFormulario();
            MostrarFormulario();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

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
                MostrarMensajeFormulario(resultado.Mensaje, false);
                return;
            }

            // El modal sigue abierto con la orden ya creada: lo que sigue es cargarle el detalle.
            CargarGrilla();
            Seleccionar(orden.IdOrden);
            MostrarMensajeFormulario(resultado.Mensaje, true);
        }

        protected void btnCancelarOrden_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var resultado = OrdenDeTrabajoDAL.Cancelar(idOrden, UsuarioActual.IdUsuario);

            CargarGrilla();
            if (resultado.Exito) Seleccionar(idOrden);
            MostrarMensajeFormulario(resultado.Mensaje, resultado.Exito);
        }

        // Cerrar genera la venta. Si el cliente no tiene cuenta corriente no puede quedar
        // debiendo: se pasa directo a cobrar esa venta en Pagos (con el cliente y el monto ya
        // cargados). Con cuenta corriente, la deuda queda en su cuenta como hasta ahora.
        protected void btnCerrarOrden_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var resultado = OrdenDeTrabajoDAL.Cerrar(idOrden);

            if (resultado.Exito)
            {
                var venta = ComprobanteVentaDAL.ObtenerPorOrden(idOrden);
                var cliente = venta == null ? null : ClienteDAL.ObtenerPorId(venta.IdCliente);
                if (venta != null && cliente != null && !cliente.CuentaCorriente && venta.SaldoPendiente > 0)
                {
                    Response.Redirect("~/Pagos?idVenta=" + venta.IdVenta);
                    return;
                }
            }

            CargarGrilla();
            if (resultado.Exito) Seleccionar(idOrden);
            MostrarMensajeFormulario(resultado.Mensaje, resultado.Exito);
        }

        protected void gvOrdenes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

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

            pnlSeleccionNueva.Visible = false;
            pnlSeleccionFija.Visible = true;
            litClienteInfo.Text = Server.HtmlEncode(orden.NombreCliente + " — DNI " + orden.Dni);
            litVehiculoInfo.Text = Server.HtmlEncode(orden.Patente);
            litTurnoInfo.Text = Server.HtmlEncode(DescribirTurno(orden.IdTurno));

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
            btnCerrarOrden.Visible = !esTerminal;

            litTituloFormulario.Text = "Orden #" + orden.IdOrden + " — " + orden.Estado;

            pnlDialogo.CssClass = "modal-dialog modal-xl";
            pnlDetalle.Visible = true;
            pnlAgregarServicio.Visible = !esTerminal;
            pnlAgregarInsumo.Visible = !esTerminal;
            gvServicios.Columns[ColumnaQuitar].Visible = !esTerminal;
            gvInsumosOrden.Columns[ColumnaQuitar].Visible = !esTerminal;
            CargarDetalle(idOrden);

            MostrarFormulario();
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

        // --- Detalle: servicios (postbacks parciales del UpdatePanel, el modal no se cierra) ----

        protected void btnAgregarServicio_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var idServicio = LeerIdOculto(ddlServicio.SelectedValue);
            var cantidad = decimal.Parse(txtCantidadServicio.Text);

            var resultado = DetalleOrdenServicioDAL.Agregar(idOrden, idServicio, cantidad);

            MostrarMensajeFormulario(resultado.Mensaje, resultado.Exito);
            txtCantidadServicio.Text = "1";
            CargarDetalle(idOrden);
        }

        protected void gvServicios_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Quitar") return;

            var resultado = DetalleOrdenServicioDAL.Quitar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
            if (!resultado.Exito) MostrarMensajeFormulario(resultado.Mensaje, false);

            CargarDetalle(LeerIdOculto(hdnIdOrden.Value));
        }

        // --- Detalle: insumos ------------------------------------------------------------------

        protected void btnAgregarInsumo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid) return;

            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var idInsumo = LeerIdOculto(ddlInsumo.SelectedValue);
            var cantidad = decimal.Parse(txtCantidadInsumo.Text);

            var resultado = DetalleOrdenInsumoDAL.Agregar(idOrden, idInsumo, cantidad, UsuarioActual.IdUsuario);

            MostrarMensajeFormulario(resultado.Mensaje, resultado.Exito);
            txtCantidadInsumo.Text = "1";
            CargarCatalogos();
            CargarDetalle(idOrden);
        }

        protected void gvInsumosOrden_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Quitar") return;

            var resultado = DetalleOrdenInsumoDAL.Quitar(
                LeerIdOculto(Convert.ToString(e.CommandArgument)), UsuarioActual.IdUsuario);

            MostrarMensajeFormulario(resultado.Mensaje, resultado.Exito);
            CargarCatalogos();
            CargarDetalle(LeerIdOculto(hdnIdOrden.Value));
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
            txtCliente.Text = string.Empty;
            pnlSeleccionNueva.Visible = true;
            pnlSeleccionFija.Visible = false;
            LimpiarVehiculosYTurnos();
            txtKilometraje.Text = string.Empty;
            txtObservaciones.Text = string.Empty;
            pnlEstado.Visible = false;
            btnGuardar.Visible = true;
            btnCancelarOrden.Visible = false;
            btnCerrarOrden.Visible = false;
            litTituloFormulario.Text = "Nueva orden";
            pnlDialogo.CssClass = "modal-dialog modal-lg";
            pnlDetalle.Visible = false;
        }

        private void MostrarFormulario()
        {
            Interfaz.AbrirModal(this, IdModal);
        }

        // Aviso adentro del modal (resultado de guardar, cerrar, cancelar o de una línea del
        // detalle). En un postback parcial el modal ya está abierto y solo se actualiza el aviso.
        private void MostrarMensajeFormulario(string mensajeHtml, bool exito)
        {
            pnlMensajeFormulario.CssClass = "alert " + (exito ? "alert-success" : "alert-danger");
            litMensajeFormulario.Text = mensajeHtml;
            pnlMensajeFormulario.Visible = true;
            MostrarFormulario();
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
