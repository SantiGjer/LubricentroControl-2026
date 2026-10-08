using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de clientes. Acceso completo para Admin, Encargado y Empleado (Requerimientos §5);
    // Lectura, solo consulta. La lista ocupa toda la pantalla y el formulario de alta/edición se
    // abre en un modal encima ("Nuevo cliente" o "Editar" en la fila). "Ver" muestra todos los
    // datos de la fila (Lubricentro.js), también para quien solo consulta. Datos fiscales y
    // domicilio completo desde el 2026-10-07 (Requerimientos §9.8).
    public partial class Clientes : PaginaSegura
    {
        private const string IdModal = "modalCliente";

        // Patentes de los vehículos de cada cliente, para la columna (escondida) "Vehículos": así
        // el buscador también encuentra al cliente por la patente (Requerimientos §6.2).
        private Dictionary<int, string> patentesPorCliente = new Dictionary<int, string>();

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
            }

            CargarListas();

            // El interruptor de cuenta corriente lo maneja solo quien puede escribir en la cuenta
            // corriente de clientes (ver PuedeCambiarCuentaCorriente).
            var puedeCambiarCuentaCorriente = PuedeCambiarCuentaCorriente;
            chkCuentaCorriente.Disabled = !puedeCambiarCuentaCorriente;
            litCuentaCorrienteBloqueada.Visible = !puedeCambiarCuentaCorriente;

            LimpiarFormulario();
            CargarGrilla();

            if (EsSoloLectura) return;

            // Se llegó con "Nuevo cliente" desde Vehiculos.aspx (ver Vehiculos.aspx.cs,
            // btnNuevoCliente_Click y btnNuevoClienteDueno_Click): guardamos el vehículo en curso
            // en nuestros propios hidden fields para no perderlo en los postbacks de esta pantalla
            // — el query string solo está disponible en esta primera carga.
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

                // Viniendo de "Cambiar dueño", el cliente nuevo es para pasarle un vehículo que ya existe.
                if (Request.QueryString["cambioDueno"] == "1")
                {
                    hdnVhCambioDueno.Value = "1";
                    litVieneDeVehiculo.Text = "Estás creando el cliente al que vas a pasarle el vehículo " +
                        Server.HtmlEncode(Request.QueryString["patente"]) + ".";
                }

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

        // Listas fijas de los desplegables (Cliente.TiposCliente, TiposDocumento, Provincias e
        // Iva.Condiciones).
        private void CargarListas()
        {
            rblTipoCliente.Items.Clear();
            foreach (var tipo in Cliente.TiposCliente)
                rblTipoCliente.Items.Add(new ListItem(tipo, tipo));

            ddlTipoDocumento.Items.Clear();
            foreach (var tipo in Cliente.TiposDocumento)
                ddlTipoDocumento.Items.Add(new ListItem(tipo, tipo));

            ddlCondicionIva.Items.Clear();
            foreach (var condicion in Iva.Condiciones)
                ddlCondicionIva.Items.Add(new ListItem(condicion, condicion));

            ddlProvincia.Items.Clear();
            ddlProvincia.Items.Add(new ListItem("(sin especificar)", ""));
            foreach (var provincia in Cliente.Provincias)
                ddlProvincia.Items.Add(new ListItem(provincia, provincia));
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
        // elegido si solo se vuelve sin crear ninguno). Si se vino de "Cambiar dueño", lo avisa
        // para que Vehículos reabra ese formulario y no el de alta.
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
                + "&idClienteNuevo=" + idCliente
                + (hdnVhCambioDueno.Value == "1" ? "&cambioDueno=1" : "");
        }

        // Trae todos los clientes, activos e inactivos: el filtro por texto y las opciones
        // (estado, tipo, cuenta corriente) los aplica la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            patentesPorCliente = VehiculoDAL.Listar(incluirInactivos: false)
                .GroupBy(v => v.IdCliente)
                .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(v => v.Patente)));

            gvClientes.DataSource = ClienteDAL.Listar(incluirInactivos: true);
            gvClientes.DataBind();
        }

        protected string PatentesDe(int idCliente)
        {
            string patentes;
            return patentesPorCliente.TryGetValue(idCliente, out patentes) ? patentes : "";
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

        private bool EsEmpresaElegida
        {
            get { return rblTipoCliente.SelectedValue == Cliente.TipoEmpresa; }
        }

        // Nombre y apellido son obligatorios solo para una persona física (para una empresa ni
        // se ven).
        protected void valDatoDePersona_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = EsEmpresaElegida || !string.IsNullOrWhiteSpace(args.Value);
        }

        protected void valRazonSocial_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = !EsEmpresaElegida || !string.IsNullOrWhiteSpace(args.Value);
        }

        // El formato depende del tipo elegido (Cliente.EsNumeroDocumentoValido), así que el
        // mensaje también.
        protected void valNumeroDocumento_ServerValidate(object source, ServerValidateEventArgs args)
        {
            var validador = (CustomValidator)source;
            if (string.IsNullOrWhiteSpace(args.Value))
            {
                validador.ErrorMessage = "El número de documento es obligatorio.";
                args.IsValid = false;
                return;
            }

            validador.ErrorMessage = Cliente.MensajeFormatoDocumento(ddlTipoDocumento.SelectedValue);
            args.IsValid = Cliente.EsNumeroDocumentoValido(ddlTipoDocumento.SelectedValue, args.Value);
        }

        protected void valTelefono_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = FormatoTelefono.EsValido(args.Value);
        }

        protected void valEmail_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Cliente.EsEmailValido(args.Value);
        }

        protected void valCodigoPostal_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Cliente.EsCodigoPostalValido(args.Value);
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
                TipoCliente = rblTipoCliente.SelectedValue,
                Nombre = txtNombre.Text,
                Apellido = txtApellido.Text,
                RazonSocial = txtRazonSocial.Text,
                TipoDocumento = ddlTipoDocumento.SelectedValue,
                NumeroDocumento = txtNumeroDocumento.Text,
                CondicionIva = ddlCondicionIva.SelectedValue,
                Telefono = txtTelefono.Text,
                Email = txtEmail.Text,
                Direccion = txtDireccion.Text,
                Localidad = txtLocalidad.Text,
                Provincia = ddlProvincia.SelectedValue,
                CodigoPostal = txtCodigoPostal.Text,
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
            rblTipoCliente.SelectedValue = cliente.TipoCliente;
            txtNombre.Text = cliente.Nombre;
            txtApellido.Text = cliente.Apellido;
            txtRazonSocial.Text = cliente.RazonSocial;
            ddlTipoDocumento.SelectedValue = cliente.TipoDocumento;
            txtNumeroDocumento.Text = cliente.NumeroDocumento;
            ddlCondicionIva.SelectedValue = cliente.CondicionIva;
            txtTelefono.Text = cliente.Telefono;
            txtEmail.Text = cliente.Email;
            txtDireccion.Text = cliente.Direccion;
            txtLocalidad.Text = cliente.Localidad;
            ddlProvincia.SelectedValue = cliente.Provincia ?? string.Empty;
            txtCodigoPostal.Text = cliente.CodigoPostal;
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

        // Un alta arranca como persona física con DNI y consumidor final, el caso más común.
        private void LimpiarFormulario()
        {
            hdnIdCliente.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            rblTipoCliente.SelectedValue = Cliente.TipoPersonaFisica;
            txtNombre.Text = string.Empty;
            txtApellido.Text = string.Empty;
            txtRazonSocial.Text = string.Empty;
            ddlTipoDocumento.SelectedValue = Cliente.DocumentoDni;
            txtNumeroDocumento.Text = string.Empty;
            ddlCondicionIva.SelectedValue = Iva.ConsumidorFinal;
            txtTelefono.Text = string.Empty;
            txtEmail.Text = string.Empty;
            txtDireccion.Text = string.Empty;
            txtLocalidad.Text = string.Empty;
            ddlProvincia.SelectedValue = string.Empty;
            txtCodigoPostal.Text = string.Empty;
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
