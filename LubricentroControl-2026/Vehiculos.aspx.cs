using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de vehículos. Acceso completo para Admin, Encargado y Empleado (Requerimientos §5);
    // Lectura, solo consulta. El formulario se abre en un modal sobre la lista. El dueño se elige
    // con un selector con búsqueda: la lista desplegable trae todos los clientes activos y el
    // mismo campo filtra mientras se escribe (Scripts\Lubricentro.js, .selector-busqueda).
    public partial class Vehiculos : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvVehiculos.Columns.
        private const int ColumnaAcciones = 6;

        private const string IdModal = "modalVehiculo";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlErrorFormulario.Visible = false;

            // El rango del año sale del modelo (Vehiculo.AnioMinimo/AnioMaximo); se fija en cada
            // request porque el máximo depende de la fecha.
            valAnio.MinimumValue = Vehiculo.AnioMinimo.ToString();
            valAnio.MaximumValue = Vehiculo.AnioMaximo.ToString();
            valAnio.ErrorMessage = "El año tiene que estar entre " + Vehiculo.AnioMinimo + " y " + Vehiculo.AnioMaximo + ".";
            txtAnio.Attributes["min"] = valAnio.MinimumValue;
            txtAnio.Attributes["max"] = valAnio.MaximumValue;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
                gvVehiculos.Columns[ColumnaAcciones].Visible = false;
            }

            CargarTiposCombustible();
            CargarGrilla();

            if (EsSoloLectura) return;

            if (Request.QueryString["idClienteNuevo"] != null)
                RehidratarDesdeRetorno();

            // Se llegó con "Nuevo vehículo" desde OrdenesDeTrabajo.aspx (origen "orden"):
            // guardamos los datos sueltos de la orden en curso para devolverlos intactos, y
            // preseleccionamos el dueño si ya estaba elegido en Órdenes (sigue siendo editable
            // acá — si se cambia, se vuelve con el dueño real del vehículo creado, no con este).
            if (Request.QueryString["origen"] == "orden")
            {
                hdnVieneDeOrden.Value = bool.TrueString;
                hdnOrKilometraje.Value = Request.QueryString["kilometraje"];
                hdnOrObservaciones.Value = Request.QueryString["observaciones"];
                hdnOrIdTurno.Value = Request.QueryString["idTurno"];
                pnlVieneDeOrden.Visible = true;

                var idClienteActual = LeerIdOculto(Request.QueryString["idClienteActual"]);
                if (idClienteActual > 0)
                    SeleccionarCliente(idClienteActual);

                MostrarFormulario();
            }
        }

        // Opciones del selector de dueño (los clientes activos). Se evalúa al dibujar el modal
        // (data-opciones en el .aspx).
        protected string OpcionesClientes
        {
            get { return Selectores.OpcionesClientes(); }
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
            var activo = ActivoDesdeHidden();
            btnBorrar.Visible = esEdicion && activo;
            btnReactivar.Visible = esEdicion && !activo;
            litTituloFormulario.Text = esEdicion ? "Editar vehículo" : "Nuevo vehículo";
            MostrarFormulario();
        }

        // "Nuevo cliente" al lado del selector: manda a Clientes.aspx los datos del
        // vehículo en curso por query string (no PostBackUrl/PreviousPage — esos rompen
        // acá porque FriendlyUrls no publica un archivo físico en la ruta amigable, y
        // PreviousPage necesita reconstruir la página de origen a partir de esa ruta).
        protected void btnNuevoCliente_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

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

        protected void btnVolverAOrdenes_Click(object sender, EventArgs e)
        {
            Response.Redirect(ArmarUrlVueltaOrden(0, 0));
        }

        // Arma la URL de vuelta a OrdenesDeTrabajo.aspx con los datos de la orden en curso más
        // el cliente/vehículo a seleccionar (los recién creados, o ninguno si se vuelve sin
        // crear). Usa el dueño real del vehículo creado, no el que vino en la query string por
        // si se cambió el dueño ya estando en esta pantalla.
        private string ArmarUrlVueltaOrden(int idCliente, int idVehiculo)
        {
            var url = "~/OrdenesDeTrabajo"
                + "?kilometraje=" + Server.UrlEncode(hdnOrKilometraje.Value)
                + "&observaciones=" + Server.UrlEncode(hdnOrObservaciones.Value)
                + "&idTurno=" + Server.UrlEncode(hdnOrIdTurno.Value);

            return idVehiculo > 0
                ? url + "&idCliente=" + idCliente + "&idVehiculoNuevo=" + idVehiculo
                : url;
        }

        private void CargarTiposCombustible()
        {
            ddlTipoCombustible.Items.Clear();
            ddlTipoCombustible.Items.Add(new ListItem("(sin especificar)", ""));
            foreach (var tipo in Vehiculo.TiposCombustible)
                ddlTipoCombustible.Items.Add(new ListItem(tipo, tipo));
        }

        // El filtro por texto lo hace la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvVehiculos.DataSource = VehiculoDAL.Listar(chkIncluirInactivos.Checked);
            gvVehiculos.DataBind();
        }

        protected void chkIncluirInactivos_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        // Deja elegido un dueño desde el servidor (vuelta de Clientes/Órdenes, edición). Cuando
        // lo elige el usuario, el selector con búsqueda completa hdnIdCliente en el navegador.
        private void SeleccionarCliente(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null) return;

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            txtCliente.Text = Selectores.TextoCliente(cliente);
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

            var esAlta = vehiculo.IdVehiculo == 0;
            var resultado = esAlta
                ? VehiculoDAL.Crear(vehiculo)
                : VehiculoDAL.Actualizar(vehiculo);

            if (!resultado.Exito)
            {
                MostrarErrorFormulario(resultado.Mensaje);
                return;
            }

            // Se creó (no editó) un vehículo durante una visita desde "Nuevo vehículo" en
            // OrdenesDeTrabajo.aspx: se vuelve directo con este vehículo ya seleccionado.
            if (esAlta && hdnVieneDeOrden.Value == bool.TrueString)
            {
                Response.Redirect(ArmarUrlVueltaOrden(vehiculo.IdCliente, vehiculo.IdVehiculo));
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var baja = VehiculoDAL.Desactivar(LeerIdOculto(hdnIdVehiculo.Value));
            if (!baja.Exito)
            {
                MostrarErrorFormulario(baja.Mensaje);
                return;
            }

            MostrarMensaje(baja.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnReactivar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var alta = VehiculoDAL.Reactivar(LeerIdOculto(hdnIdVehiculo.Value));
            if (!alta.Exito)
            {
                MostrarErrorFormulario(alta.Mensaje);
                return;
            }

            MostrarMensaje(alta.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvVehiculos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

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
            btnReactivar.Visible = !vehiculo.Activo;

            litTituloFormulario.Text = vehiculo.Activo ? "Editar vehículo" : "Editar vehículo (inactivo)";
            MostrarFormulario();
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
            txtCliente.Text = string.Empty;
            txtPatente.Text = string.Empty;
            txtMarca.Text = string.Empty;
            txtModelo.Text = string.Empty;
            txtAnio.Text = string.Empty;
            ddlTipoCombustible.SelectedIndex = 0;
            btnBorrar.Visible = false;
            btnReactivar.Visible = false;
            litTituloFormulario.Text = "Nuevo vehículo";
        }

        private void MostrarFormulario()
        {
            Interfaz.AbrirModal(this, IdModal);
        }

        // Un error al guardar se muestra adentro del modal, que vuelve a abrirse con lo cargado.
        private void MostrarErrorFormulario(string mensajeHtml)
        {
            litErrorFormulario.Text = mensajeHtml;
            pnlErrorFormulario.Visible = true;
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
