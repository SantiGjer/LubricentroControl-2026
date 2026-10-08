using System;
using System.Linq;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de órdenes de trabajo (Fase 3). Acceso completo para Admin, Encargado y Empleado
    // (Requerimientos §5); Lectura, solo consulta. La lista arranca filtrada en las órdenes
    // Abiertas, de la más nueva a la más vieja. "Ver" abre la orden completa de solo lectura, para
    // todos los roles. Alta y edición en otro modal: cliente/vehículo/turno quedan fijos una vez
    // creada la orden (ver Docs/EstadoActual.md, sesión de esta pantalla): el selector y los
    // desplegables solo se muestran en "Nueva orden"; editando una ya creada se ven como texto
    // fijo, junto con el detalle de servicios e insumos.
    public partial class OrdenesDeTrabajo : PaginaSegura
    {
        // Índice de la columna "Acciones" (Quitar) en gvServicios/gvInsumosOrden.
        private const int ColumnaQuitar = 3;

        private const string IdModal = "modalOrden";
        private const string IdModalVer = "modalVerOrden";

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
                btnEditarDesdeVer.Visible = false;
            }

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
            foreach (var servicio in ProductoDAL.Listar(Producto.TipoServicio, incluirInactivos: false))
                ddlServicio.Items.Add(new ListItem(
                    servicio.Nombre + " ($ " + servicio.Precio.ToString("N2") + ")",
                    servicio.IdProducto.ToString()));

            ddlInsumo.Items.Clear();
            foreach (var insumo in ProductoDAL.Listar(Producto.TipoInsumo, incluirInactivos: false))
                ddlInsumo.Items.Add(new ListItem(
                    insumo.Nombre + " (stock: " + insumo.StockActual.ToString("N2") + ")",
                    insumo.IdProducto.ToString()));
        }

        // Todas las órdenes, de la más nueva a la más vieja: el estado y el texto los filtra la
        // tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvOrdenes.DataSource = OrdenDeTrabajoDAL.Listar();
            gvOrdenes.DataBind();
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

        // --- Ver (solo lectura, todos los roles) ---------------------------------------------

        private void Ver(int idOrden)
        {
            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            if (orden == null)
            {
                MostrarMensaje("La orden no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdOrdenVer.Value = orden.IdOrden.ToString();
            litTituloVer.Text = "Orden #" + orden.IdOrden + " — " + Server.HtmlEncode(orden.Estado);
            litVerCliente.Text = Server.HtmlEncode(orden.NombreCliente + " — " + orden.Documento);
            litVerVehiculo.Text = Server.HtmlEncode(orden.Patente);
            litVerTurno.Text = Server.HtmlEncode(DescribirTurno(orden.IdTurno));
            litVerFecha.Text = orden.Fecha.ToString("dd/MM/yyyy HH:mm");
            litVerKilometraje.Text = orden.Kilometraje.HasValue ? orden.Kilometraje.Value.ToString("N0") + " km" : "—";
            litVerEstado.Text = Server.HtmlEncode(orden.Estado);
            litVerUsuario.Text = Server.HtmlEncode(orden.NombreUsuario);
            litVerObservaciones.Text = string.IsNullOrEmpty(orden.Observaciones) ? "—" : Server.HtmlEncode(orden.Observaciones);

            var venta = ComprobanteVentaDAL.ObtenerPorOrden(idOrden);
            litVerVenta.Text = venta == null
                ? "—"
                : Server.HtmlEncode(venta.NumeroComprobante) + " · saldo pendiente $ " + venta.SaldoPendiente.ToString("N2");

            var servicios = DetalleOrdenServicioDAL.ListarPorOrden(idOrden);
            var insumos = DetalleOrdenInsumoDAL.ListarPorOrden(idOrden);
            gvVerServicios.DataSource = servicios;
            gvVerServicios.DataBind();
            gvVerInsumos.DataSource = insumos;
            gvVerInsumos.DataBind();
            litVerTotal.Text = (servicios.Sum(s => s.Subtotal) + insumos.Sum(i => i.Subtotal)).ToString("N2");

            Interfaz.AbrirModal(this, IdModalVer);
        }

        protected void btnEditarDesdeVer_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            Seleccionar(LeerIdOculto(hdnIdOrdenVer.Value));
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

        // --- Cierre: genera la venta y, según se elija, se cobra ahora o queda en la cuenta ----

        protected void btnCerrarACuenta_Click(object sender, EventArgs e)
        {
            CerrarOrden(cobrarAhora: false);
        }

        protected void btnCerrarYCobrar_Click(object sender, EventArgs e)
        {
            CerrarOrden(cobrarAhora: true);
        }

        // Cerrar genera la venta (OrdenDeTrabajoDAL.Cerrar). Con "cobrar ahora" se pasa a Pagos con
        // el cliente y el saldo de esa venta cargados (~/Pagos?idVenta=). Un cliente sin cuenta
        // corriente no puede dejarla en la cuenta: aunque llegara ese pedido (el botón ni se
        // muestra), se cobra igual. La venta y su movimiento de cuenta corriente se generan igual
        // en los dos casos: lo único que cambia es adónde va la pantalla (Requerimientos §9.7).
        private void CerrarOrden(bool cobrarAhora)
        {
            if (EsSoloLectura) return;

            var idOrden = LeerIdOculto(hdnIdOrden.Value);
            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            if (orden != null && !orden.ClienteConCuentaCorriente) cobrarAhora = true;

            var resultado = OrdenDeTrabajoDAL.Cerrar(idOrden);
            var mensaje = resultado.Mensaje;

            if (resultado.Exito)
            {
                var venta = ComprobanteVentaDAL.ObtenerPorOrden(idOrden);
                if (cobrarAhora && venta != null && venta.SaldoPendiente > 0)
                {
                    Response.Redirect("~/Pagos?idVenta=" + venta.IdVenta);
                    return;
                }

                if (!cobrarAhora && venta != null && venta.SaldoPendiente > 0)
                    mensaje += " El saldo de $ " + venta.SaldoPendiente.ToString("N2") + " quedó en la cuenta corriente del cliente.";
            }

            CargarGrilla();
            if (resultado.Exito) Seleccionar(idOrden);
            MostrarMensajeFormulario(mensaje, resultado.Exito);
        }

        protected void gvOrdenes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            var idOrden = LeerIdOculto(Convert.ToString(e.CommandArgument));

            if (e.CommandName == "Ver")
            {
                Ver(idOrden);
                return;
            }

            if (EsSoloLectura) return;
            if (e.CommandName == "Editar") Seleccionar(idOrden);
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
            litClienteInfo.Text = Server.HtmlEncode(orden.NombreCliente + " — " + orden.Documento);
            litVehiculoInfo.Text = Server.HtmlEncode(orden.Patente);
            litTurnoInfo.Text = Server.HtmlEncode(DescribirTurno(orden.IdTurno));

            txtKilometraje.Text = orden.Kilometraje.HasValue ? orden.Kilometraje.Value.ToString() : string.Empty;
            txtObservaciones.Text = orden.Observaciones;

            var esTerminal = !orden.EsEditable;

            pnlEstado.Visible = true;
            ddlEstado.Visible = !esTerminal;
            litEstadoActual.Visible = esTerminal;
            if (esTerminal)
                litEstadoActual.Text = orden.Estado;
            else
                ddlEstado.SelectedValue = orden.Estado;

            btnGuardar.Visible = !esTerminal;
            btnCancelarOrden.Visible = !esTerminal;
            phCerrarOrden.Visible = !esTerminal;

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

        // Las líneas y el total, y con el total, el texto de la confirmación de cierre: con
        // cuenta corriente se ofrece dejar el saldo en la cuenta o cobrarlo; sin ella, solo cobrar.
        private void CargarDetalle(int idOrden)
        {
            var servicios = DetalleOrdenServicioDAL.ListarPorOrden(idOrden);
            var insumos = DetalleOrdenInsumoDAL.ListarPorOrden(idOrden);

            gvServicios.DataSource = servicios;
            gvServicios.DataBind();

            gvInsumosOrden.DataSource = insumos;
            gvInsumosOrden.DataBind();

            var total = servicios.Sum(s => s.Subtotal) + insumos.Sum(i => i.Subtotal);
            litTotalOrden.Text = total.ToString("N2");

            var orden = OrdenDeTrabajoDAL.ObtenerPorId(idOrden);
            var conCuenta = orden != null && orden.ClienteConCuentaCorriente;
            var nombre = orden == null ? "" : Server.HtmlEncode(orden.NombreCliente);

            btnCerrarACuenta.Visible = conCuenta;
            btnCerrarYCobrar.Text = conCuenta ? "Cobrar ahora" : "Cerrar y cobrar";
            litConfirmarCierre.Text = "Al cerrar la orden se genera la venta por <b>$ " + total.ToString("N2") + "</b>. " +
                (conCuenta
                    ? nombre + " tiene cuenta corriente: ¿el saldo va a su cuenta o lo cobrás ahora?"
                    : nombre + " no tiene cuenta corriente: después de cerrarla pasás a registrar el cobro.");
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
            phCerrarOrden.Visible = false;
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
