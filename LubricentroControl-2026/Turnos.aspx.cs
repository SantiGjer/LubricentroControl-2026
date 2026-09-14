using System;
using System.Globalization;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de turnos (Fase 3). Acceso completo para los 3 roles (Requerimientos §5), no hay
    // modo solo-consulta que manejar acá. Mismo layout de dos columnas y mismo buscador
    // desplegable de cliente que Vehiculos.aspx — sin el atajo "Nuevo cliente" (ver plan).
    public partial class Turnos : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            pnlSoloLectura.Visible = EsSoloLectura;

            if (IsPostBack) return;

            CargarFiltroEstado();
            CargarEstados();
            LimpiarVehiculos();
            CargarGrilla();
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

        private void CargarGrilla()
        {
            var texto = txtBuscar.Text;
            var estado = ddlFiltroEstado.SelectedValue;

            gvTurnos.DataSource = string.IsNullOrWhiteSpace(texto)
                ? TurnoDAL.Listar(estado)
                : TurnoDAL.Buscar(texto, estado);
            gvTurnos.DataBind();
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
            LimpiarFormulario();
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

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
                MostrarMensaje("La fecha o la hora no tienen un formato válido.", false);
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
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvTurnos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "Seleccionar") return;

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
            txtFecha.Text = turno.FechaHoraAsignada.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            txtHora.Text = turno.FechaHoraAsignada.ToString("HH:mm", CultureInfo.InvariantCulture);
            ddlEstado.SelectedValue = turno.Estado;
            txtObservaciones.Text = turno.Observaciones;

            litTituloFormulario.Text = "Editar turno";
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
            litClienteSeleccionado.Text = "(sin seleccionar)";
            txtBuscarCliente.Text = string.Empty;
            pnlResultadosCliente.Visible = false;
            LimpiarVehiculos();
            txtFecha.Text = string.Empty;
            txtHora.Text = string.Empty;
            ddlEstado.SelectedValue = Turno.EstadoSolicitado;
            txtObservaciones.Text = string.Empty;
            litTituloFormulario.Text = "Nuevo turno";
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
