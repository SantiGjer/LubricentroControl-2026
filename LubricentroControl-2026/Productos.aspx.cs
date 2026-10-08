using System;
using System.Globalization;
using System.Web;
using System.Web.UI.WebControls;
using BIZ.Data;
using BIZ.Modelo;
using LubricentroControl_2026.Seguridad;
using LubricentroControl_2026.Utilidades;

namespace LubricentroControl_2026
{
    // ABM de productos: servicios e insumos juntos, como subcategorías de Producto (Requerimientos
    // §9.9 — reemplaza a las pantallas de Servicios e Insumos). Admin y Encargado, acceso completo;
    // Empleado y Lectura, solo consulta (Requerimientos §5): se les esconde el botón de alta, el
    // modal (incluidos el ajuste de stock y el historial) y "Editar", y los métodos de escritura
    // igual cortan al principio por las dudas. "Ver" queda para todos. Cada producto puede tener
    // una imagen (§9.13): se elige en el formulario y se guarda con el producto.
    public partial class Productos : PaginaSegura
    {
        private const string IdModal = "modalProducto";

        // El navegador vacía el campo de archivo en cada envío: si el producto no se guardó, la
        // imagen elegida tampoco, y hay que volver a elegirla.
        private const string AvisoImagenPerdida = "La imagen elegida no se guardó: volvé a elegirla.";

        protected void Page_Load(object sender, EventArgs e)
        {
            // Los avisos se muestran una sola vez (el Literal guarda su texto en el ViewState).
            pnlMensaje.Visible = false;
            pnlMensajeFormulario.Visible = false;

            // Los límites de la imagen salen de BIZ: con data-tamano-maximo el navegador descarta
            // un archivo más pesado sin mandarlo.
            fuImagen.Attributes["data-tamano-maximo"] = Imagen.BytesMaximos.ToString(CultureInfo.InvariantCulture);
            litAyudaImagen.Text = "PNG o JPG, hasta " + Imagen.MegabytesMaximos + " MB. Si mide más de " +
                                  Imagen.LadoImagen + " px de lado, se achica al guardarla.";

            if (IsPostBack) return;

            CargarListas();

            if (EsSoloLectura)
            {
                btnNuevo.Visible = false;
                pnlFormulario.Visible = false;
            }

            LimpiarFormulario();
            CargarGrilla();
        }

        // Listas fijas: subcategorías, tipos de IVA, alícuotas y unidades de medida.
        private void CargarListas()
        {
            rblTipo.Items.Clear();
            foreach (var tipo in Producto.Tipos)
                rblTipo.Items.Add(new ListItem(tipo, tipo));

            ddlTipoIva.Items.Clear();
            foreach (var tipo in Iva.Tipos)
                ddlTipoIva.Items.Add(new ListItem(tipo, tipo));

            ddlAlicuota.Items.Clear();
            foreach (var alicuota in Iva.Alicuotas)
                ddlAlicuota.Items.Add(new ListItem(alicuota.ToString("0.##") + " %", ValorAlicuota(alicuota)));

            ddlUnidadMedida.Items.Clear();
            ddlUnidadMedida.Items.Add(new ListItem("(sin especificar)", ""));
            foreach (var unidad in Producto.UnidadesDeMedida)
                ddlUnidadMedida.Items.Add(new ListItem(unidad, unidad));
        }

        // Valor de una alícuota en el desplegable, en formato invariante y sin ceros de más ("10.5",
        // "21"): la base la devuelve con dos decimales (10.50) y tiene que coincidir igual.
        private static string ValorAlicuota(decimal alicuota)
        {
            return alicuota.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // Todos, activos e inactivos: el filtro por texto, las opciones (tipo, estado, stock bajo) y
        // el paginado los hace la tabla en el navegador (Lubricentro.js).
        private void CargarGrilla()
        {
            gvProductos.DataSource = ProductoDAL.Listar(incluirInactivos: true);
            gvProductos.DataBind();
        }

        // Insumo activo con el stock por debajo del mínimo: fila resaltada y data-stock="bajo"
        // para la opción "Por debajo del mínimo".
        protected void gvProductos_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            if (e.Row.RowType != DataControlRowType.DataRow) return;

            var producto = (Producto)e.Row.DataItem;
            if (producto.StockBajo)
            {
                e.Row.Style.Add("background-color", "#f8d7da");
                e.Row.Attributes["data-stock"] = "bajo";
            }
        }

        // Miniatura de la lista, con la imagen entera en data-imagen para "Ver", o un recuadro
        // vacío si el producto no tiene. alt vacío: el nombre ya está al lado.
        protected string Miniatura(object item)
        {
            var producto = (Producto)item;
            if (!producto.TieneImagen) return "<span class=\"miniatura miniatura-vacia\"></span>";

            return "<img class=\"miniatura\" src=\"" + HttpUtility.HtmlAttributeEncode(ImagenProducto.Url(producto, true)) +
                   "\" data-imagen=\"" + HttpUtility.HtmlAttributeEncode(ImagenProducto.Url(producto, false)) +
                   "\" alt=\"\" loading=\"lazy\" />";
        }

        protected void btnNuevo_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            LimpiarFormulario();
            MostrarFormulario();
        }

        protected void valCodigo_ServerValidate(object source, ServerValidateEventArgs args)
        {
            args.IsValid = Producto.EsCodigoValido(args.Value);
        }

        protected void btnGuardar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;
            if (!Page.IsValid)
            {
                if (fuImagen.HasFile) MostrarMensajeFormulario(AvisoImagenPerdida, false);
                else MostrarFormulario();
                return;
            }

            // La imagen se valida antes de guardar nada: si no sirve, el producto queda como estaba.
            Imagen imagen = null;
            if (fuImagen.HasFile)
            {
                var preparada = Imagen.Preparar(fuImagen.FileBytes, out imagen);
                if (!preparada.Exito)
                {
                    MostrarMensajeFormulario(preparada.Mensaje, false);
                    return;
                }
            }

            decimal precio, stockMinimo, stockInicial, alicuota;
            decimal.TryParse(txtPrecio.Text, out precio);
            decimal.TryParse(txtStockMinimo.Text, out stockMinimo);
            decimal.TryParse(txtStockInicial.Text, out stockInicial);
            decimal.TryParse(ddlAlicuota.SelectedValue, NumberStyles.Number, CultureInfo.InvariantCulture, out alicuota);

            var idProducto = LeerIdOculto(hdnIdProducto.Value);
            var producto = new Producto
            {
                IdProducto = idProducto,
                Tipo = rblTipo.SelectedValue,
                Nombre = txtNombre.Text,
                Descripcion = txtDescripcion.Text,
                Sku = txtSku.Text,
                CodigoBarras = txtCodigoBarras.Text,
                Precio = precio,
                TipoIva = ddlTipoIva.SelectedValue,
                AlicuotaIva = alicuota,
                Marca = txtMarca.Text,
                UnidadMedida = ddlUnidadMedida.SelectedValue,
                StockMinimo = stockMinimo,
                StockActual = stockInicial,
                Activo = ActivoDesdeHidden()
            };

            var resultado = idProducto == 0
                ? ProductoDAL.Crear(producto, UsuarioActual.IdUsuario)
                : ProductoDAL.Actualizar(producto);

            if (!resultado.Exito)
            {
                MostrarMensajeFormulario(imagen == null ? resultado.Mensaje : resultado.Mensaje + " " + AvisoImagenPerdida, false);
                return;
            }

            // Después del producto: en un alta, recién ahora tiene id.
            var cambioImagen = imagen != null ? ProductoDAL.GuardarImagen(producto.IdProducto, imagen)
                : chkQuitarImagen.Checked ? ProductoDAL.QuitarImagen(producto.IdProducto)
                : ResultadoOperacion.Ok();

            MostrarMensaje(cambioImagen.Exito ? resultado.Mensaje : resultado.Mensaje + " " + cambioImagen.Mensaje,
                cambioImagen.Exito);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnBorrar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var baja = ProductoDAL.Desactivar(LeerIdOculto(hdnIdProducto.Value));
            if (!baja.Exito)
            {
                MostrarMensajeFormulario(baja.Mensaje, false);
                return;
            }

            MostrarMensaje(baja.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        protected void btnReactivar_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var alta = ProductoDAL.Reactivar(LeerIdOculto(hdnIdProducto.Value));
            if (!alta.Exito)
            {
                MostrarMensajeFormulario(alta.Mensaje, false);
                return;
            }

            MostrarMensaje(alta.Mensaje, true);
            LimpiarFormulario();
            CargarGrilla();
        }

        // El ajuste deja el modal abierto (con el stock y el historial ya actualizados): lo
        // normal es seguir mirando el mismo insumo.
        protected void btnRegistrarAjuste_Click(object sender, EventArgs e)
        {
            if (EsSoloLectura) return;

            var idProducto = LeerIdOculto(hdnIdProducto.Value);
            if (!Page.IsValid)
            {
                MostrarFormulario();
                return;
            }

            decimal cantidad;
            decimal.TryParse(txtCantidadAjuste.Text, out cantidad);

            // Un solo campo con signo: positivo suma stock, negativo resta. RegistrarAjusteManual
            // sigue pidiendo la cantidad siempre positiva + el signo aparte, así que se traduce acá.
            var resultado = MovimientoStockDAL.RegistrarAjusteManual(
                idProducto, Math.Abs(cantidad), cantidad > 0, txtMotivoAjuste.Text, UsuarioActual.IdUsuario);

            if (resultado.Exito)
            {
                txtCantidadAjuste.Text = string.Empty;
                txtMotivoAjuste.Text = string.Empty;
                Seleccionar(idProducto);
                CargarGrilla();
            }

            MostrarMensajeFormulario(resultado.Mensaje, resultado.Exito);
        }

        protected void gvProductos_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (EsSoloLectura) return;
            if (e.CommandName != "Editar") return;

            Seleccionar(LeerIdOculto(Convert.ToString(e.CommandArgument)));
        }

        private void Seleccionar(int idProducto)
        {
            var producto = ProductoDAL.ObtenerPorId(idProducto);
            if (producto == null)
            {
                MostrarMensaje("El producto no existe.", false);
                CargarGrilla();
                return;
            }

            hdnIdProducto.Value = producto.IdProducto.ToString();
            hdnActivo.Value = producto.Activo.ToString();

            // La subcategoría queda fija: se ve elegida pero no se puede cambiar.
            rblTipo.SelectedValue = producto.Tipo;
            rblTipo.Enabled = false;
            pnlTipoFijo.Visible = true;

            txtNombre.Text = producto.Nombre;
            txtDescripcion.Text = producto.Descripcion;
            txtSku.Text = producto.Sku;
            txtCodigoBarras.Text = producto.CodigoBarras;
            txtPrecio.Text = producto.Precio.ToString("N2");
            ddlTipoIva.SelectedValue = producto.TipoIva;
            if (producto.TipoIva == Iva.Gravado)
                ddlAlicuota.SelectedValue = ValorAlicuota(producto.AlicuotaIva);
            txtMarca.Text = producto.Marca;
            ddlUnidadMedida.SelectedValue = producto.UnidadMedida ?? string.Empty;
            txtStockMinimo.Text = producto.EsInsumo ? producto.StockMinimo.ToString("N2") : string.Empty;
            btnBorrar.Visible = producto.Activo;
            btnReactivar.Visible = !producto.Activo;
            MostrarImagenActual(ImagenProducto.Url(producto, false));

            pnlStockInicial.Visible = false;
            pnlStockActual.Visible = producto.EsInsumo;
            litStockActual.Text = producto.StockActual.ToString("N2");

            litTituloFormulario.Text = "Editar " + producto.Tipo.ToLowerInvariant() + (producto.Activo ? "" : " (inactivo)");

            // Editando un insumo, el modal se agranda para sumar el ajuste al costado y el
            // historial abajo.
            pnlDialogo.CssClass = producto.EsInsumo ? "modal-dialog modal-xl" : "modal-dialog modal-lg";
            pnlDatos.CssClass = producto.EsInsumo ? "col-lg-7" : "col-12";
            pnlAjuste.Visible = producto.EsInsumo;
            pnlHistorial.Visible = producto.EsInsumo;
            if (producto.EsInsumo)
            {
                gvHistorial.DataSource = MovimientoStockDAL.ListarPorInsumo(idProducto);
                gvHistorial.DataBind();
            }

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

        // Un alta arranca como insumo gravado al 21 %, el caso más común.
        private void LimpiarFormulario()
        {
            hdnIdProducto.Value = string.Empty;
            hdnActivo.Value = bool.TrueString;
            rblTipo.SelectedValue = Producto.TipoInsumo;
            rblTipo.Enabled = true;
            pnlTipoFijo.Visible = false;
            txtNombre.Text = string.Empty;
            txtDescripcion.Text = string.Empty;
            txtSku.Text = string.Empty;
            txtCodigoBarras.Text = string.Empty;
            txtPrecio.Text = string.Empty;
            ddlTipoIva.SelectedValue = Iva.Gravado;
            ddlAlicuota.SelectedIndex = 0;
            txtMarca.Text = string.Empty;
            if (ddlUnidadMedida.Items.Count > 0) ddlUnidadMedida.SelectedIndex = 0;
            txtStockMinimo.Text = string.Empty;
            btnBorrar.Visible = false;
            btnReactivar.Visible = false;
            MostrarImagenActual(null);
            litTituloFormulario.Text = "Nuevo producto";

            txtStockInicial.Text = string.Empty;
            pnlStockInicial.Visible = true;
            pnlStockActual.Visible = false;

            pnlDialogo.CssClass = "modal-dialog modal-lg";
            pnlDatos.CssClass = "col-12";
            pnlAjuste.Visible = false;
            pnlHistorial.Visible = false;
            txtCantidadAjuste.Text = string.Empty;
            txtMotivoAjuste.Text = string.Empty;
        }

        // Recuadro de la imagen en el formulario: la actual del producto, o "Sin imagen". Quitarla
        // solo se ofrece si tiene una.
        private void MostrarImagenActual(string url)
        {
            if (url == null)
            {
                imgProducto.Attributes.Remove("src");
                imgProducto.Attributes["hidden"] = "hidden";
            }
            else
            {
                imgProducto.Src = url;
                imgProducto.Attributes.Remove("hidden");
            }

            pnlQuitarImagen.Visible = url != null;
            chkQuitarImagen.Checked = false;
        }

        private void MostrarFormulario()
        {
            Interfaz.AbrirModal(this, IdModal);
        }

        // Aviso adentro del modal (error al guardar, resultado de un ajuste), que vuelve a abrirse.
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
