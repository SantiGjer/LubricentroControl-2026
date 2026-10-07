using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de clientes. Acceso completo para Admin, Encargado y Empleado (Requerimientos §5);
    // Lectura, solo consulta. La lista ocupa toda la pantalla y el formulario de alta/edición se
    // abre en un modal encima ("Nuevo cliente" o "Editar" en la fila).
    public partial class Clientes : PaginaSegura
    {
        // Índice de la columna "Acciones" en gvClientes.Columns.
        private const int ColumnaAcciones = 7;

        private const string IdModal = "modalCliente";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez: sin esto, el Literal (que guarda su texto en el
            // ViewState) repetiría el último aviso en cada postback.
            pnlMensaje.Visible = false;
            pnlErrorFormulario.Visible = false;

            if (IsPostBack) return;

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
                gvClientes.Columns[ColumnaAcciones].Visible = false;
            }

            // El interruptor de cuenta corriente lo maneja solo quien puede escribir en la cuenta
            // corriente de clientes (ver PuedeCambiarCuentaCorriente).
            var puedeCambiarCuentaCorriente = PuedeCambiarCuentaCorriente;
            chkCuentaCorriente.Disabled = !puedeCambiarCuentaCorriente;
            litCuentaCorrienteBloqueada.Visible = !puedeCambiarCuentaCorriente;

            CargarGrilla();

            if (EsSoloLectura) return;

            // Se llegó con "Nuevo cliente" desde Vehiculos.aspx (ver Vehiculos.aspx.cs,
            // btnNuevoCliente_Click): guardamos el vehículo en curso en nuestros propios
            // hidden fields para no perderlo en los postbacks de esta pantalla — el query
            // string solo está disponible en esta primera carga.
            if (Request.QueryString["origen"] == "vehiculo")
            {
                hdnVieneDeVehiculo.Value = bool.TrueString;
                hdnVhIdVehiculo.Value = Request.QueryString["idVehiculo"];
                hdnVhActivo.Value = Request.QueryString["activo"];
                hdnVhIdCliente.Value = Request.QueryString["idClienteActual"];
                hdnVhPatente.Value = Request.QueryString["patente"];
                hdnVhMarca.Value = Request.QueryString["marca"];
                hdnVhModelo.Value = Request.QueryString["modelo"];
                hdnVhAnio.Value = Request.QueryString["anio"];
                hdnVhTipoCombustible.Value = Request.QueryString["tipoCombustible"];

                pnlVieneDeVehiculo.Visible = true;
                MostrarFormulario();
            }
            // Se llegó con "Nuevo cliente" desde OrdenesDeTrabajo.aspx (origen "orden"): a
            // diferencia del caso de Vehículos, acá todavía no existe ni cliente ni vehículo —
            // solo se guardan los datos sueltos de la orden en curso para devolverlos intactos.
            else if (Request.QueryString["origen"] == "orden")
            {
                hdnVieneDeOrden.Value = bool.TrueString;
                hdnOrKilometraje.Value = Request.QueryString["kilometraje"];
                hdnOrObservaciones.Value = Request.QueryString["observaciones"];
                hdnOrIdTurno.Value = Request.QueryString["idTurno"];

                pnlVieneDeOrden.Visible = true;
                MostrarFormulario();
            }
            // "Editar cliente" desde CuentaCorrienteClientes.aspx: abre la edición de ese cliente
            // (para cambiarle la cuenta corriente) y al guardar vuelve a esa pantalla.
            else if (Request.QueryString["editar"] != null)
            {
                if (Request.QueryString["volver"] == "ctacte")
                {
                    hdnVieneDeCuentaCorriente.Value = bool.TrueString;
                    pnlVieneDeCuentaCorriente.Visible = true;
                }

                Seleccionar(LeerIdOculto(Request.QueryString["editar"]));
            }
        }

        // Habilitarle el fiado a un cliente es una decisión de cuenta corriente: la toma quien
        // puede escribir en Cuenta corriente de clientes (Admin y Encargado), no Empleado, que ahí
        // es solo consulta (Requerimientos §5) aunque en Clientes tenga acceso completo.
        private bool PuedeCambiarCuentaCorriente
        {
            get
            {
                var permiso = MenuDAL.ObtenerPermiso(UsuarioActual.IdNivel, "~/CuentaCorrienteClientes");
                return permiso != null && !permiso.SoloLectura;
            }
        }

        protected void btnVolverAVehiculos_Click(object sender, EventArgs e)
        {
            Response.Redirect(ArmarUrlVuelta(LeerIdOculto(hdnVhIdCliente.Value)));
        }

        protected void btnVolverAOrdenes_Click(object sender, EventArgs e)
        {
            Response.Redirect(ArmarUrlVueltaOrden(0));
        }

        protected void btnVolverACuentaCorriente_Click(object sender, EventArgs e)
        {
            Response.Redirect("~/CuentaCorrienteClientes");
        }

        // Arma la URL de vuelta a OrdenesDeTrabajo.aspx con los datos de la orden en curso más
        // el cliente a seleccionar (el recién creado, o ninguno si se vuelve sin crear).
        private string ArmarUrlVueltaOrden(int idClienteNuevo)
        {
            var url = "~/OrdenesDeTrabajo"
                + "?kilometraje=" + Server.UrlEncode(hdnOrKilometraje.Value)
                + "&observaciones=" + Server.UrlEncode(hdnOrObservaciones.Value)
                + "&idTurno=" + Server.UrlEncode(hdnOrIdTurno.Value);

            return idClienteNuevo > 0 ? url + "&idClienteNuevo=" + idClienteNuevo : url;
        }

        // Arma la URL de vuelta a Vehiculos.aspx con el vehículo que había quedado en curso
        // más el cliente a seleccionar como dueño (el recién creado, o el que ya estaba
        // elegido si solo se vuelve sin crear ninguno).
        private string ArmarUrlVuelta(int idCliente)
        {
            return "~/Vehiculos"
                + "?idVehiculo=" + Server.UrlEncode(hdnVhIdVehiculo.Value)
                + "&activo=" + Server.UrlEncode(hdnVhActivo.Value)
                + "&patente=" + Server.UrlEncode(hdnVhPatente.Value)
                + "&marca=" + Server.UrlEncode(hdnVhMarca.Value)
                + "&modelo=" + Server.UrlEncode(hdnVhModelo.Value)
                + "&anio=" + Server.UrlEncode(hdnVhAnio.Value)
                + "&tipoCombustible=" + Server.UrlEncode(hdnVhTipoCombustible.Value)
                + "&idClienteNuevo=" + idCliente;
        }

        // Trae todos los clientes (o solo los activos): el filtro por texto lo hace la tabla en
        // el navegador (Lubricentro.js), sin volver al servidor.
        private void CargarGrilla()
        {
            gvClientes.DataSource = ClienteDAL.Listar(chkIncluirInactivos.Checked);
            gvClientes.DataBind();
        }

        protected void chkIncluirInactivos_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            OlvidarVueltaACuentaCorriente();
            LimpiarFormulario();
            MostrarFormulario();
        }

        // Quien llegó desde Cuenta corriente y después elige otro cliente (o uno nuevo) ya no está
        // en ese recorrido: al guardar se queda acá en vez de volver.
        private void OlvidarVueltaACuentaCorriente()
        {
            hdnVieneDeCuentaCorriente.Value = string.Empty;
            pnlVieneDeCuentaCorriente.Visible = false;
        }

        protected void valDni_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Cliente.EsDniValido(args.Value);
        }

        protected void valTelefono_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = FormatoTelefono.EsValido(args.Value);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

            var idCliente = LeerIdOculto(hdnIdCliente.Value);
            var esAlta = idCliente == 0;
            var cliente = new Cliente
            {
                IdCliente = idCliente,
                Nombre = txtNombre.Text,
                Apellido = txtApellido.Text,
                Dni = txtDni.Text,
                Telefono = txtTelefono.Text,
                Email = txtEmail.Text,
                Direccion = txtDireccion.Text,
                CuentaCorriente = CuentaCorrienteElegida(idCliente),
                Activo = ActivoDesdeHidden()
            };

            var resultado = esAlta
                ? ClienteDAL.Crear(cliente)
                : ClienteDAL.Actualizar(cliente);

            if (!resultado.Exito)
            {
                MostrarErrorFormulario(resultado.Mensaje);
                return;
            }

            // Se creó (no editó) un cliente durante una visita desde "Nuevo cliente" en
            // Vehiculos.aspx: se vuelve directo con este cliente ya seleccionado como dueño.
            if (esAlta && hdnVieneDeVehiculo.Value == bool.TrueString)
            {
                Response.Redirect(ArmarUrlVuelta(cliente.IdCliente));
                return;
            }

            // Ídem, pero viniendo de "Nuevo cliente" en OrdenesDeTrabajo.aspx.
            if (esAlta && hdnVieneDeOrden.Value == bool.TrueString)
            {
                Response.Redirect(ArmarUrlVueltaOrden(cliente.IdCliente));
                return;
            }

            if (!esAlta && hdnVieneDeCuentaCorriente.Value == bool.TrueString)
            {
                Response.Redirect("~/CuentaCorrienteClientes");
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        // El interruptor solo cuenta si quien guarda puede cambiarlo; si no, se respeta lo que el
        // cliente ya tenía (y un alta queda sin cuenta corriente). La guarda real va acá y no solo
        // en el control deshabilitado, mismo criterio que EsSoloLectura.
        private bool CuentaCorrienteElegida(int idCliente)
        {
            if (PuedeCambiarCuentaCorriente) return chkCuentaCorriente.Checked;
            if (idCliente == 0) return false;

            var actual = ClienteDAL.ObtenerPorId(idCliente);
            return actual != null && actual.CuentaCorriente;
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var baja = ClienteDAL.Desactivar(LeerIdOculto(hdnIdCliente.Value));
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

            var alta = ClienteDAL.Reactivar(LeerIdOculto(hdnIdCliente.Value));
            if (!alta.Exito)
            {
                MostrarErrorFormulario(alta.Mensaje);
                return;
            }

            MostrarMensaje(alta.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvClientes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

            OlvidarVueltaACuentaCorriente();
            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idCliente)
        {
            var cliente = ClienteDAL.ObtenerPorId(idCliente);
            if (cliente == null)
            {
                MostrarMensaje("El cliente no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdCliente.Value = cliente.IdCliente.ToString();
            hdnActivo.Value = cliente.Activo.ToString();
            txtNombre.Text = cliente.Nombre;
            txtApellido.Text = cliente.Apellido;
            txtDni.Text = cliente.Dni;
            txtTelefono.Text = cliente.Telefono;
            txtEmail.Text = cliente.Email;
            txtDireccion.Text = cliente.Direccion;
            chkCuentaCorriente.Checked = cliente.CuentaCorriente;
            btnBorrar.Visible = cliente.Activo;
            btnReactivar.Visible = !cliente.Activo;

            litTituloFormulario.Text = cliente.Activo ? "Editar cliente" : "Editar cliente (inactivo)";
            MostrarFormulario();
        }

        // Por defecto activo si el campo oculto llegara vacío o manipulado
        // (ej. un POST armado a mano sin ese campo).
        private bool ActivoDesdeHidden()
        {
            bool activo;
            return !bool.TryParse(hdnActivo.Value, out activo) || activo;
        }

        // 0 (ID inexistente, cae en "no existe" en el DAL) si el campo oculto llegara
        // vacío o manipulado, en vez de reventar con FormatException.
        private static int LeerIdOculto(string valor)
        {
            int id;
            return int.TryParse(valor, out id) ? id : 0;
        }

        private void LimpiarFormulario()
        {
            hdnIdCliente.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            txtNombre.Text = string.Empty;
            txtApellido.Text = string.Empty;
            txtDni.Text = string.Empty;
            txtTelefono.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtDireccion.Text = string.Empty;
            chkCuentaCorriente.Checked = false;
            btnBorrar.Visible = false;
            btnReactivar.Visible = false;
            litTituloFormulario.Text = "Nuevo cliente";
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
