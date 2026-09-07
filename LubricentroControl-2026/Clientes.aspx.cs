using System;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;

namespace LubricentroControl_2026
{
    // ABM de clientes. Acceso completo para los 3 roles (Requerimientos §5): no hay
    // modo solo-consulta que manejar acá. Layout de dos columnas: buscador+grilla a la
    // izquierda, formulario siempre visible a la derecha (sesión 2026-09-06).
    public partial class Clientes : PaginaSegura
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (IsPostBack) return;

            CargarGrilla();

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
            }
        }

        protected void btnVolverAVehiculos_Click(object sender, EventArgs e)
        {
            Response.Redirect(ArmarUrlVuelta(LeerIdOculto(hdnVhIdCliente.Value)));
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

        private void CargarGrilla()
        {
            var incluirInactivos = chkIncluirInactivos.Checked;
            var texto = txtBuscar.Text;

            gvClientes.DataSource = string.IsNullOrWhiteSpace(texto)
                ? ClienteDAL.Listar(incluirInactivos)
                : ClienteDAL.Buscar(texto, incluirInactivos);
            gvClientes.DataBind();
        }

        protected void btnBuscar_Click(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void chkIncluirInactivos_CheckedChanged(object sender, EventArgs e)
        {
            CargarGrilla();
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            LimpiarFormulario();
        }

        protected void valDni_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Cliente.EsDniValido(args.Value);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (!Page.IsValid) return;

            var esAlta = LeerIdOculto(hdnIdCliente.Value) == 0;
            var cliente = new Cliente
            {
                IdCliente = LeerIdOculto(hdnIdCliente.Value),
                Nombre = txtNombre.Text,
                Apellido = txtApellido.Text,
                Dni = txtDni.Text,
                Telefono = txtTelefono.Text,
                Email = txtEmail.Text,
                Direccion = txtDireccion.Text,
                Activo = ActivoDesdeHidden()
            };

            var resultado = esAlta
                ? ClienteDAL.Crear(cliente)
                : ClienteDAL.Actualizar(cliente);

            if (!resultado.Exito)
            {
                MostrarMensaje(resultado.Mensaje, false);
                return;
            }

            // Se creó (no editó) un cliente durante una visita desde "Nuevo cliente" en
            // Vehiculos.aspx: se vuelve directo con este cliente ya seleccionado como dueño.
            if (esAlta && hdnVieneDeVehiculo.Value == bool.TrueString)
            {
                Response.Redirect(ArmarUrlVuelta(cliente.IdCliente));
                return;
            }

            MostrarMensaje(resultado.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            var baja = ClienteDAL.Desactivar(LeerIdOculto(hdnIdCliente.Value));

            MostrarMensaje(baja.Mensaje, baja.Exito);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void gvClientes_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int idCliente;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idCliente)) return;

            if (e.CommandName == "Seleccionar")
                Seleccionar(idCliente);
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
            btnBorrar.Visible = cliente.Activo;

            litTituloFormulario.Text = "Editar cliente";
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
            btnBorrar.Visible = false;
            litTituloFormulario.Text = "Nuevo cliente";
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
