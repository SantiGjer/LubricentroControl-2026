using System;
using System.Globalization;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de turnos (Fase 3). Acceso completo para Admin, Encargado y Empleado (Requerimientos
    // §5); Lectura, solo consulta. El formulario se abre en un modal sobre la lista. El cliente se
    // elige con el selector con búsqueda (mismo que el dueño en Vehículos) — sin el atajo "Nuevo
    // cliente" — y queda fijo una vez creado el turno.
    public partial class Turnos : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvTurnos.Columns.
        private const int ColumnaAcciones = 4;

        private const string IdModal = "modalTurno";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlErrorFormulario.Visible = false;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
                gvTurnos.Columns[ColumnaAcciones].Visible = false;
            }

            CargarFiltroEstado();
            CargarEstados();
            LimpiarFormulario();
            CargarGrilla();
        }

        // Opciones del selector de cliente (los clientes activos). Se evalúa al dibujar el modal.
        protected string OpcionesClientes
        {
            get { return Selectores.OpcionesClientes(); }
        }

        private void CargarFiltroEstado()
        {
            ddlFiltroEstado.Items.Clear();
            ddlFiltroEstado.Items.Add(new ListItem("(Todos)", ""));
            foreach (var estado in Turno.Estados)
                ddlFiltroEstado.Items.Add(new ListItem(estado, estado));
        }

        private void CargarEstados()
        {
            ddlEstado.Items.Clear();
            foreach (var estado in Turno.Estados)
                ddlEstado.Items.Add(new ListItem(estado, estado));
        }

        // El estado filtra en el servidor; el texto, la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvTurnos.DataSource = TurnoDAL.Listar(ddlFiltroEstado.SelectedValue);
            gvTurnos.DataBind();
        }

        // El filtro de la tabla busca también por DNI, que no es una columna: va en data-buscar.
        protected void gvTurnos_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            e.Row.Attributes["data-buscar"] = ((Turno)e.Row.DataItem).Dni;
        }

        protected void ddlFiltroEstado_SelectedIndexChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        // --- Selector de cliente -----------------------------------------------------------

        // Lo dispara el selector con búsqueda al elegir un cliente (postback parcial del
        // UpdatePanel): carga los vehículos de ese cliente.
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
                LimpiarVehiculos();
                return;
            }

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            txtCliente.Text = Selectores.TextoCliente(cliente);

            CargarVehiculosDelCliente(idCliente, null);
        }

        // Vehículos activos del cliente elegido; "(sin vehículo asociado)" primero porque
        // el vínculo Turno-Vehículo es opcional (Requerimientos §6.3/§6.4).
        private void CargarVehiculosDelCliente(int idCliente, int? idVehiculoSeleccionado)
        {
            LimpiarVehiculos();

            foreach (var vehiculo in VehiculoDAL.ListarPorCliente(idCliente, incluirInactivos: false))
                ddlVehiculo.Items.Add(new ListItem(vehiculo.Patente, vehiculo.IdVehiculo.ToString()));

            if (idVehiculoSeleccionado.HasValue &&
                ddlVehiculo.Items.FindByValue(idVehiculoSeleccionado.Value.ToString()) != null)
                ddlVehiculo.SelectedValue = idVehiculoSeleccionado.Value.ToString();
        }

        // Deja ddlVehiculo con solo el placeholder. Tiene que quedar poblado con al
        // menos esta opción ya en el primer Page_Load (sin cliente elegido todavía):
        // si el control no registra ningún <option>, ASP.NET rechaza cualquier valor
        // posteado — incluso "" — con "Argumento de postback no válido" al guardar.
        private void LimpiarVehiculos()
        {
            ddlVehiculo.Items.Clear();
            ddlVehiculo.Items.Add(new ListItem("(sin vehículo asociado)", ""));
        }

        // No usa ControlToValidate: valida la selección guardada en el hidden, no un TextBox.
        protected void valCliente_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = LeerIdOculto(hdnIdCliente.Value) > 0;
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

            // Los inputs HTML5 type="date"/"time" siempre postean en yyyy-MM-dd / HH:mm,
            // sin importar la configuración regional del navegador ni la cultura del hilo
            // del servidor — se parsean exactos e invariantes para no depender de ninguna.
            DateTime fecha, hora;
            var fechaValida = DateTime.TryParseExact(txtFecha.Text, "yyyy-MM-dd",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out fecha);
            var horaValida = DateTime.TryParseExact(txtHora.Text, "HH:mm",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out hora);

            if (!fechaValida || !horaValida)
            {
                MostrarErrorFormulario("La fecha o la hora no tienen un formato válido.");
                return;
            }

            var turno = new Turno
            {
                IdTurno = LeerIdOculto(hdnIdTurno.Value),
                IdCliente = LeerIdOculto(hdnIdCliente.Value),
                IdVehiculo = LeerIdOculto(ddlVehiculo.SelectedValue) > 0 ? LeerIdOculto(ddlVehiculo.SelectedValue) : (int?)null,
                FechaHoraAsignada = fecha.Date + hora.TimeOfDay,
                Estado = ddlEstado.SelectedValue,
                Observaciones = txtObservaciones.Text
            };

            var resultado = turno.IdTurno == 0
                ? TurnoDAL.Crear(turno)
                : TurnoDAL.Actualizar(turno);

            if (!resultado.Exito)
            {
                MostrarErrorFormulario(resultado.Mensaje);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvTurnos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idTurno)
        {
            var turno = TurnoDAL.ObtenerPorId(idTurno);
            if (turno == null)
            {
                MostrarMensaje("El turno no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdTurno.Value = turno.IdTurno.ToString();
            SeleccionarCliente(turno.IdCliente);
            CargarVehiculosDelCliente(turno.IdCliente, turno.IdVehiculo);

            // El cliente queda fijo una vez creado el turno (TurnoDAL.Actualizar no lo cambia):
            // el selector se muestra deshabilitado. El vehículo sí se puede cambiar.
            txtCliente.Enabled = false;

            txtFecha.Text = turno.FechaHoraAsignada.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            txtHora.Text = turno.FechaHoraAsignada.ToString("HH:mm", CultureInfo.InvariantCulture);
            ddlEstado.SelectedValue = turno.Estado;
            ddlEstado.Visible = true;
            litEstadoNuevo.Visible = false;
            txtObservaciones.Text = turno.Observaciones;

            litTituloFormulario.Text = "Editar turno";
            MostrarFormulario();
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
            hdnIdTurno.Value = string.Empty;
            hdnIdCliente.Value = string.Empty;
            txtCliente.Text = string.Empty;
            txtCliente.Enabled = true;
            LimpiarVehiculos();
            txtFecha.Text = string.Empty;
            txtHora.Text = string.Empty;
            // Un turno nuevo siempre arranca Solicitado (TurnoDAL.Crear lo fuerza): se muestra
            // como texto fijo y el desplegable queda solo para editar un turno existente.
            // ddlEstado conserva sus opciones cargadas aunque esté oculto.
            ddlEstado.SelectedValue = Turno.EstadoSolicitado;
            ddlEstado.Visible = false;
            litEstadoNuevo.Visible = true;
            txtObservaciones.Text = string.Empty;
            litTituloFormulario.Text = "Nuevo turno";
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
