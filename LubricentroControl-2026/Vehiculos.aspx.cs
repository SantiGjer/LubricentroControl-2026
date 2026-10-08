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
    // con un selector con búsqueda (Scripts\Lubricentro.js, .selector-busqueda) en el alta; después
    // se cambia con "Cambiar dueño" (VehiculoDAL.CambiarDueno, Requerimientos §9.12), que tiene su
    // propio modal y su propio atajo "Nuevo cliente".
    public partial class Vehiculos : PaginaSegura
    {
        private const string IdModal = "modalVehiculo";
        private const string IdModalCambioDueno = "modalCambioDueno";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlErrorFormulario.Visible = false;
            pnlErrorCambioDueno.Visible = false;

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
                pnlCambioDueno.Visible = false;
            }

            CargarTiposCombustible();
            CargarGrilla();

            if (EsSoloLectura) return;

            // Vuelta de "Nuevo cliente" en Clientes.aspx: al alta de un vehículo, o al cambio de
            // dueño de uno existente (cambioDueno=1), con el cliente recién creado ya elegido.
            if (Request.QueryString["idClienteNuevo"] != null)
            {
                if (Request.QueryString["cambioDueno"] == "1")
                    PrepararCambioDueno(LeerIdOculto(Request.QueryString["idVehiculo"]),
                                        LeerIdOculto(Request.QueryString["idClienteNuevo"]));
                else
                    RehidratarDesdeRetorno();
            }

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

        // Opciones de los selectores de dueño (los clientes activos). Se evalúa al dibujar los
        // modales (data-opciones en el .aspx).
        protected string OpcionesClientes
        {
            get { return Selectores.OpcionesClientes(); }
        }

        // Vuelta de "Nuevo cliente" en Clientes.aspx (ver Clientes.aspx.cs, ArmarUrlVuelta) durante
        // el alta de un vehículo: repone los datos que estaban en curso y deja seleccionado el
        // cliente que se creó (o el que ya estaba elegido, si solo se volvió sin crear ninguno).
        private void RehidratarDesdeRetorno()
        {
            LimpiarFormulario();
            txtPatente.Text = Request.QueryString["patente"];
            txtMarca.Text = Request.QueryString["marca"];
            txtModelo.Text = Request.QueryString["modelo"];
            txtAnio.Text = Request.QueryString["anio"];

            var tipoCombustible = Request.QueryString["tipoCombustible"] ?? string.Empty;
            if (ddlTipoCombustible.Items.FindByValue(tipoCombustible) != null)
                ddlTipoCombustible.SelectedValue = tipoCombustible;

            SeleccionarCliente(LeerIdOculto(Request.QueryString["idClienteNuevo"]));
            MostrarFormulario();
        }

        // "Nuevo cliente" al lado del selector de dueño del alta: manda a Clientes.aspx los datos
        // del vehículo en curso por query string (no PostBackUrl/PreviousPage — esos rompen acá
        // porque FriendlyUrls no publica un archivo físico en la ruta amigable, y PreviousPage
        // necesita reconstruir la página de origen a partir de esa ruta).
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

        // Mismo atajo desde "Cambiar dueño": el cliente nuevo vuelve elegido en ese modal.
        protected void btnNuevoClienteDueno_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var vehiculo = VehiculoDAL.ObtenerPorId(LeerIdOculto(hdnIdVehiculoDueno.Value));
            if (vehiculo == null)
            {
                MostrarMensaje("El vehículo no existe.", false);
                return;
            }

            Response.Redirect("~/Clientes"
                + "?origen=vehiculo&cambioDueno=1"
                + "&idVehiculo=" + vehiculo.IdVehiculo
                + "&idClienteActual=" + vehiculo.IdCliente
                + "&patente=" + Server.UrlEncode(vehiculo.Patente));
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

        // Todos los vehículos, activos e inactivos: el filtro por texto y las opciones (estado,
        // combustible) los aplica la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvVehiculos.DataSource = VehiculoDAL.Listar(incluirInactivos: true);
            gvVehiculos.DataBind();
        }

        // Deja elegido un dueño desde el servidor (vuelta de Clientes/Órdenes). Cuando lo elige
        // el usuario, el selector con búsqueda completa hdnIdCliente en el navegador.
        private void SeleccionarCliente(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null) return;

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            txtCliente.Text = Selectores.TextoCliente(cliente);
        }

        // No usa ControlToValidate: valida la selección guardada en el hidden, no un TextBox.
        // Editando, el dueño no se elige acá (queda el que ya tenía).
        protected void valCliente_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = LeerIdOculto(hdnIdVehiculo.Value) > 0 || LeerIdOculto(hdnIdCliente.Value) > 0;
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

            var idVehiculo = LeerIdOculto(Convert.ToString(e.CommandArgument));
            if (e.CommandName == "Editar")
                Seleccionar(idVehiculo);
            else if (e.CommandName == "CambiarDueno")
                PrepararCambioDueno(idVehiculo, 0);
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
            hdnIdCliente.Value = vehiculo.IdCliente.ToString();
            pnlDuenoSeleccion.Visible = false;
            pnlDuenoFijo.Visible = true;
            litDuenoActual.Text = Server.HtmlEncode(vehiculo.NombreCliente + " — " + vehiculo.DocumentoCliente);
            btnCambiarDueno.Visible = vehiculo.Activo;
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

        // --- Cambio de dueño ----------------------------------------------------------------

        protected void btnCambiarDueno_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            PrepararCambioDueno(LeerIdOculto(hdnIdVehiculo.Value), 0);
        }

        // Abre el modal de cambio de dueño de un vehículo; idClienteElegido > 0 lo deja elegido
        // (vuelta de "Nuevo cliente").
        private void PrepararCambioDueno(int idVehiculo, int idClienteElegido)
        {
            var vehiculo = VehiculoDAL.ObtenerPorId(idVehiculo);
            if (vehiculo == null)
            {
                MostrarMensaje("El vehículo no existe.", false);
                return;
            }

            hdnIdVehiculoDueno.Value = vehiculo.IdVehiculo.ToString();
            litTituloCambioDueno.Text = "Cambiar dueño — " + Server.HtmlEncode(vehiculo.Patente);
            litVehiculoCambio.Text = Server.HtmlEncode(
                (vehiculo.Patente + " · " + vehiculo.Marca + " " + vehiculo.Modelo).Trim(' ', '·'));
            litDuenoAnterior.Text = Server.HtmlEncode(vehiculo.NombreCliente + " — " + vehiculo.DocumentoCliente);

            hdnIdNuevoDueno.Value = string.Empty;
            txtNuevoDueno.Text = string.Empty;
            var cliente = idClienteElegido > 0 ? ClienteDAL.ObtenerPorId(idClienteElegido) : null;
            if (cliente != null)
            {
                hdnIdNuevoDueno.Value = cliente.IdCliente.ToString();
                txtNuevoDueno.Text = Selectores.TextoCliente(cliente);
            }

            Interfaz.AbrirModal(this, IdModalCambioDueno);
        }

        protected void valNuevoDueno_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = LeerIdOculto(hdnIdNuevoDueno.Value) > 0;
        }

        protected void btnConfirmarCambioDueno_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                Interfaz.AbrirModal(this, IdModalCambioDueno);
                return;
            }

            var resultado = VehiculoDAL.CambiarDueno(
                LeerIdOculto(hdnIdVehiculoDueno.Value), LeerIdOculto(hdnIdNuevoDueno.Value));

            if (!resultado.Exito)
            {
                // El mensaje trae nombres de clientes: se escapa (los demás avisos ya vienen armados).
                litErrorCambioDueno.Text = Server.HtmlEncode(resultado.Mensaje);
                pnlErrorCambioDueno.Visible = true;
                Interfaz.AbrirModal(this, IdModalCambioDueno);
                return;
            }

            MostrarMensaje(Server.HtmlEncode(resultado.Mensaje), true);
            CargarGrilla();
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
            pnlDuenoSeleccion.Visible = true;
            pnlDuenoFijo.Visible = false;
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
