using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // Roles y sus permisos por pantalla (Requerimientos §9.11). Por defecto solo la ve Admin, en la
    // sección Administración. Cada pantalla del menú tiene, para cada rol, sin acceso / consulta /
    // completo (MenuNivel); el rol Admin se muestra pero no se edita, y Admin y Lectura no se
    // borran (NivelDAL). Un cambio de permisos vale desde el próximo pedido de cada usuario:
    // PaginaSegura y el menú leen MenuNivel en cada request.
    public partial class Roles : PaginaSegura
    {
        private const string IdModal = "modalRol";

        // Grupo del menú de la fila anterior de la matriz, para cortar con un encabezado cada vez
        // que cambia (rptPermisos).
        private string grupoAnterior;

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlErrorFormulario.Visible = false;

            if (IsPostBack) return;

            if (EsSoloLectura)
                btnNuevo.Visible = false;

            CargarGrilla();
            CargarMatriz(0, soloVer: true);
        }

        private void CargarGrilla()
        {
            gvRoles.DataSource = NivelDAL.Listar();
            gvRoles.DataBind();
        }

        // Matriz de permisos de un rol (0 = rol nuevo, todo sin acceso). soloVer deja todo
        // deshabilitado.
        private void CargarMatriz(int idNivel, bool soloVer)
        {
            ViewState["SoloVer"] = soloVer;
            grupoAnterior = null;
            rptPermisos.DataSource = MenuDAL.ListarPermisos(idNivel);
            rptPermisos.DataBind();
        }

        // Fila de encabezado cuando cambia el grupo del menú (Clientes, Operación…). Inicio no
        // tiene grupo.
        protected string EncabezadoDeGrupo(PermisoPantalla permiso)
        {
            var grupo = string.IsNullOrEmpty(permiso.Grupo) ? "General" : permiso.Grupo;
            if (grupo == grupoAnterior) return string.Empty;

            grupoAnterior = grupo;
            return "<tr class=\"grupo-permiso\"><td colspan=\"2\">" + HttpUtility.HtmlEncode(grupo) + "</td></tr>";
        }

        protected void rptPermisos_ItemDataBound(object sender, RepeaterItemEventArgs e)
        {
            if (e.Item.ItemType != ListItemType.Item && e.Item.ItemType != ListItemType.AlternatingItem) return;

            var permiso = (PermisoPantalla)e.Item.DataItem;
            var opciones = (RadioButtonList)e.Item.FindControl("rblAcceso");

            foreach (var acceso in PermisoPantalla.Accesos)
                opciones.Items.Add(new ListItem(acceso, acceso));

            // Inicio, siempre con acceso: es adonde lleva el login.
            opciones.SelectedValue = permiso.EsInicio ? PermisoPantalla.Completo : permiso.Acceso;
            opciones.Enabled = !permiso.EsInicio && !(bool)ViewState["SoloVer"];
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            AbrirRol(0, soloVer: false);
        }

        protected void gvRoles_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            int idNivel;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out idNivel)) return;

            switch (e.CommandName)
            {
                case "Ver":
                    AbrirRol(idNivel, soloVer: true);
                    break;

                case "Editar":
                    if (EsSoloLectura) return;
                    AbrirRol(idNivel, soloVer: idNivel == Nivel.Admin);
                    break;

                case "Borrar":
                    if (EsSoloLectura) return;
                    var resultado = NivelDAL.Borrar(idNivel);
                    MostrarMensaje(HttpUtility.HtmlEncode(resultado.Mensaje), resultado.Exito);
                    CargarGrilla();
                    break;
            }
        }

        // Abre el modal con el rol y su matriz. El rol Admin, y cualquier rol para quien tiene la
        // pantalla en solo consulta, se abren sin poder cambiar nada.
        private void AbrirRol(int idNivel, bool soloVer)
        {
            soloVer = soloVer || EsSoloLectura || idNivel == Nivel.Admin;

            Nivel nivel = null;
            if (idNivel > 0)
            {
                nivel = NivelDAL.ObtenerPorId(idNivel);
                if (nivel == null)
                {
                    MostrarMensaje("El rol no existe.", false);
                    CargarGrilla();
                    return;
                }
            }

            hdnIdNivel.Value = idNivel > 0 ? idNivel.ToString() : string.Empty;
            txtNombre.Text = nivel == null ? string.Empty : nivel.Nombre;
            txtNombre.Enabled = !soloVer;
            pnlAccesosRapidos.Visible = !soloVer;
            btnGuardar.Visible = !soloVer;
            litBotonCerrar.Text = soloVer ? "Cerrar" : "Cancelar";

            litTituloFormulario.Text = nivel == null
                ? "Nuevo rol"
                : (soloVer ? "Permisos de " : "Editar rol ") + HttpUtility.HtmlEncode(nivel.Nombre);

            pnlAvisoRol.Visible = true;
            if (idNivel == Nivel.Admin)
                litAvisoRol.Text = "El rol Admin tiene siempre acceso completo a todas las pantallas: no se edita, para que " +
                                   "siempre haya quien pueda administrar usuarios y permisos.";
            else if (idNivel == Nivel.Lectura)
                litAvisoRol.Text = "Este es el rol que reciben las cuentas creadas desde el registro público " +
                                   "(\"Crear cuenta nueva\" en el ingreso): conviene dejarlo en consulta.";
            else if (nivel != null)
                litAvisoRol.Text = HttpUtility.HtmlEncode(nivel.Nombre) + " tiene " +
                                   (nivel.CantidadUsuarios == 1 ? "un usuario" : nivel.CantidadUsuarios + " usuarios") +
                                   ". Los cambios valen desde su próximo clic, sin volver a ingresar.";
            else
                pnlAvisoRol.Visible = false;

            CargarMatriz(idNivel, soloVer);
            Interfaz.AbrirModal(this, IdModal);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                Interfaz.AbrirModal(this, IdModal);
                return;
            }

            int idNivel;
            int.TryParse(hdnIdNivel.Value, out idNivel);

            var permisos = new List<PermisoPantalla>();
            foreach (RepeaterItem item in rptPermisos.Items)
            {
                int idMenu;
                var oculto = (HiddenField)item.FindControl("hdnIdMenu");
                var opciones = (RadioButtonList)item.FindControl("rblAcceso");
                if (!int.TryParse(oculto.Value, out idMenu)) continue;

                permisos.Add(new PermisoPantalla { IdMenu = idMenu, Acceso = opciones.SelectedValue });
            }

            var nivel = new Nivel { IdNivel = idNivel, Nombre = txtNombre.Text };
            var resultado = NivelDAL.Guardar(nivel, permisos);

            if (!resultado.Exito)
            {
                litErrorFormulario.Text = HttpUtility.HtmlEncode(resultado.Mensaje);
                pnlErrorFormulario.Visible = true;
                Interfaz.AbrirModal(this, IdModal);
                return;
            }

            // Si se renombró el rol de quien está logueado, la barra lateral muestra el nombre nuevo.
            if (nivel.IdNivel == UsuarioActual.IdNivel)
            {
                var usuario = UsuarioDAL.ObtenerPorId(UsuarioActual.IdUsuario);
                if (usuario != null) SesionUsuario.Actualizar(usuario);
            }

            MostrarMensaje(resultado.Mensaje, true);
            CargarGrilla();
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
