<%@ Page Title="Productos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Productos.aspx.cs" Inherits="LubricentroControl_2026.Productos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Productos</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroProductos" class="filtro-tabla-texto"
            placeholder="Buscar por nombre, SKU, código de barras o marca" aria-label="Buscar productos" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo producto"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesProductos">
        <div class="grupo-opciones" data-columna="Tipo">
            <span class="titulo-opciones">Tipo</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Servicio">Servicios</button>
            <button type="button" class="opcion" data-valor="Insumo">Insumos</button>
        </div>
        <div class="grupo-opciones" data-columna="Estado" data-inicial="Activo">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="Activo">Activos</button>
            <button type="button" class="opcion" data-valor="Inactivo">Inactivos</button>
            <button type="button" class="opcion" data-valor="">Todos</button>
        </div>
        <div class="grupo-opciones" data-atributo="stock">
            <span class="titulo-opciones">Stock</span>
            <button type="button" class="opcion" data-valor="">Todo</button>
            <button type="button" class="opcion" data-valor="bajo">Por debajo del mínimo</button>
        </div>
    </div>

    <p class="texto-nota"><small>Los precios son finales, con el IVA incluido. Las filas resaltadas son insumos con el stock por debajo del mínimo.</small></p>

    <asp:GridView ID="gvProductos" runat="server" data-filtro="filtroProductos" data-filas-por-pagina="30"
        data-opciones-tabla="opcionesProductos" data-titulo-detalle="Producto"
        CssClass="tabla-abm tabla-compacta"
        AutoGenerateColumns="false" DataKeyNames="IdProducto" GridLines="None"
        OnRowCommand="gvProductos_RowCommand" OnRowDataBound="gvProductos_RowDataBound"
        EmptyDataText="No hay productos cargados.">
        <Columns>
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Tipo" HeaderText="Tipo" />
            <asp:BoundField DataField="Sku" HeaderText="SKU" />
            <asp:BoundField DataField="CodigoBarras" HeaderText="Código de barras" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Marca" HeaderText="Marca" />
            <asp:BoundField DataField="UnidadMedida" HeaderText="Unidad" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Precio" HeaderText="Precio" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="IvaDescripcion" HeaderText="IVA" />
            <asp:TemplateField HeaderText="Stock">
                <ItemTemplate><%# (bool)Eval("EsInsumo") ? ((decimal)Eval("StockActual")).ToString("N2") : "" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Stock mín." HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta">
                <ItemTemplate><%# (bool)Eval("EsInsumo") ? ((decimal)Eval("StockMinimo")).ToString("N2") : "" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <a href="#" class="accion-ver" data-ver-detalle>Ver</a>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdProducto") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Alta y edición en un modal. La subcategoría (servicio o insumo) se elige en el alta y
         queda fija; un insumo suma marca, unidad y stock, y editándolo, el ajuste manual de stock
         y el historial de movimientos (kardex). --%>
    <div class="modal fade" id="modalProducto" tabindex="-1" aria-labelledby="tituloModalProducto"
        aria-hidden="true" data-bs-backdrop="static">
        <asp:Panel ID="pnlDialogo" runat="server" CssClass="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalProducto">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo producto" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlMensajeFormulario" runat="server" Visible="false" role="alert">
                        <asp:Literal ID="litMensajeFormulario" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdProducto" runat="server" />
                    <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                    <div class="row g-4">
                        <asp:Panel ID="pnlDatos" runat="server" CssClass="col-12">
                            <div class="row campos-formulario">
                                <div class="col-12 campo">
                                    <label>Tipo de producto</label>
                                    <asp:RadioButtonList ID="rblTipo" runat="server" ClientIDMode="Static"
                                        RepeatLayout="Flow" RepeatDirection="Horizontal" CssClass="opciones-radio" />
                                    <asp:Panel ID="pnlTipoFijo" runat="server" Visible="false">
                                        <p class="texto-ayuda">El tipo de un producto ya creado no se cambia.</p>
                                    </asp:Panel>
                                </div>

                                <div class="col-12 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label>
                                    <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Producto"
                                        ErrorMessage="El nombre es obligatorio." />
                                </div>

                                <div class="col-12 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtDescripcion">Descripción</asp:Label>
                                    <asp:TextBox ID="txtDescripcion" runat="server" MaxLength="300" TextMode="MultiLine" Rows="2" />
                                </div>

                                <div class="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtSku">SKU</asp:Label>
                                    <asp:TextBox ID="txtSku" runat="server" MaxLength="50" placeholder="Código interno (opcional)" />
                                    <asp:CustomValidator runat="server" ControlToValidate="txtSku"
                                        OnServerValidate="valCodigo_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Producto"
                                        ErrorMessage="Solo letras, números y guiones, sin espacios." />
                                </div>

                                <div class="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtCodigoBarras">Código de barras</asp:Label>
                                    <asp:TextBox ID="txtCodigoBarras" runat="server" MaxLength="50" placeholder="EAN o código del fabricante (opcional)" />
                                    <asp:CustomValidator runat="server" ControlToValidate="txtCodigoBarras"
                                        OnServerValidate="valCodigo_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Producto"
                                        ErrorMessage="Solo letras, números y guiones, sin espacios." />
                                </div>

                                <div class="col-12"><p class="subtitulo-formulario">Precio e IVA</p></div>

                                <div class="col-md-4 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtPrecio">Precio final (con IVA)</asp:Label>
                                    <asp:TextBox ID="txtPrecio" runat="server" MaxLength="12" />
                                    <asp:CompareValidator runat="server" ControlToValidate="txtPrecio"
                                        Operator="DataTypeCheck" Type="Currency"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Producto"
                                        ErrorMessage="El precio debe ser un número." />
                                </div>

                                <div class="col-md-4 campo">
                                    <asp:Label runat="server" AssociatedControlID="ddlTipoIva">Tipo de IVA</asp:Label>
                                    <asp:DropDownList ID="ddlTipoIva" runat="server" ClientIDMode="Static" />
                                </div>

                                <div class="col-md-4 campo" data-mostrar-si="ddlTipoIva=Gravado">
                                    <asp:Label runat="server" AssociatedControlID="ddlAlicuota">Alícuota</asp:Label>
                                    <asp:DropDownList ID="ddlAlicuota" runat="server" />
                                </div>

                                <%-- Solo insumos: lo que tiene stock. --%>
                                <div class="col-12" data-mostrar-si="rblTipo=Insumo">
                                    <div class="row campos-formulario">
                                        <div class="col-12"><p class="subtitulo-formulario">Insumo</p></div>

                                        <div class="col-md-6 campo">
                                            <asp:Label runat="server" AssociatedControlID="txtMarca">Marca</asp:Label>
                                            <asp:TextBox ID="txtMarca" runat="server" MaxLength="50" />
                                        </div>

                                        <div class="col-md-6 campo">
                                            <asp:Label runat="server" AssociatedControlID="ddlUnidadMedida">Unidad de medida</asp:Label>
                                            <asp:DropDownList ID="ddlUnidadMedida" runat="server" />
                                        </div>

                                        <div class="col-md-6 campo">
                                            <asp:Label runat="server" AssociatedControlID="txtStockMinimo">Stock mínimo</asp:Label>
                                            <asp:TextBox ID="txtStockMinimo" runat="server" MaxLength="12" />
                                            <asp:CompareValidator runat="server" ControlToValidate="txtStockMinimo"
                                                Operator="DataTypeCheck" Type="Currency"
                                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Producto"
                                                ErrorMessage="El stock mínimo debe ser un número." />
                                        </div>

                                        <asp:Panel ID="pnlStockInicial" runat="server" CssClass="col-md-6 campo">
                                            <asp:Label runat="server" AssociatedControlID="txtStockInicial">Stock inicial</asp:Label>
                                            <asp:TextBox ID="txtStockInicial" runat="server" MaxLength="12" />
                                            <asp:CompareValidator runat="server" ControlToValidate="txtStockInicial"
                                                Operator="DataTypeCheck" Type="Currency"
                                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Producto"
                                                ErrorMessage="El stock inicial debe ser un número." />
                                        </asp:Panel>

                                        <asp:Panel ID="pnlStockActual" runat="server" CssClass="col-md-6 campo" Visible="false">
                                            <label>Stock actual</label>
                                            <div class="valor-fijo"><asp:Literal ID="litStockActual" runat="server" /></div>
                                        </asp:Panel>
                                    </div>
                                </div>
                            </div>
                        </asp:Panel>

                        <%-- Solo editando un insumo: el stock se cambia con un ajuste (queda en el
                             kardex con motivo y usuario), nunca editando el número directo. --%>
                        <asp:Panel ID="pnlAjuste" runat="server" CssClass="col-lg-5" Visible="false" DefaultButton="btnRegistrarAjuste">
                            <div class="seccion-modal">
                                <h3>Ajustar stock</h3>
                                <div class="row campos-formulario">
                                    <div class="col-12 campo">
                                        <asp:Label runat="server" AssociatedControlID="txtCantidadAjuste">Cantidad (negativo resta stock, positivo suma)</asp:Label>
                                        <asp:TextBox ID="txtCantidadAjuste" runat="server" MaxLength="12" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCantidadAjuste"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="La cantidad es obligatoria." />
                                        <asp:CompareValidator runat="server" ControlToValidate="txtCantidadAjuste"
                                            Operator="NotEqual" ValueToCompare="0" Type="Currency"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="La cantidad no puede ser cero." />
                                    </div>

                                    <div class="col-12 campo">
                                        <asp:Label runat="server" AssociatedControlID="txtMotivoAjuste">Motivo</asp:Label>
                                        <asp:TextBox ID="txtMotivoAjuste" runat="server" MaxLength="300" TextMode="MultiLine" Rows="2" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMotivoAjuste"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="El motivo es obligatorio." />
                                    </div>
                                </div>

                                <asp:Button ID="btnRegistrarAjuste" runat="server" CssClass="boton-rojo" Text="Registrar ajuste"
                                    OnClick="btnRegistrarAjuste_Click" ValidationGroup="Ajuste" />
                            </div>
                        </asp:Panel>
                    </div>

                    <asp:Panel ID="pnlHistorial" runat="server" Visible="false">
                        <div class="separador-modal"></div>
                        <h3>Historial de movimientos</h3>
                        <asp:GridView ID="gvHistorial" runat="server" data-filas-por-pagina="10"
                            CssClass="tabla-abm tabla-compacta"
                            AutoGenerateColumns="false" GridLines="None"
                            EmptyDataText="Todavía no hay movimientos registrados.">
                            <Columns>
                                <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                                <asp:BoundField DataField="TipoMovimiento" HeaderText="Tipo" />
                                <asp:BoundField DataField="Entrada" HeaderText="Entrada" DataFormatString="{0:N2}" />
                                <asp:BoundField DataField="Salida" HeaderText="Salida" DataFormatString="{0:N2}" />
                                <asp:BoundField DataField="StockResultante" HeaderText="Stock" DataFormatString="{0:N2}" />
                                <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
                                <asp:BoundField DataField="Descripcion" HeaderText="Motivo" />
                            </Columns>
                        </asp:GridView>
                    </asp:Panel>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnBorrar" runat="server" CssClass="boton-borde-rojo" Text="Borrar" Visible="false"
                            OnClick="btnBorrar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Borrar este producto?');" />
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Producto" />
                </div>
            </asp:Panel>
        </asp:Panel>
    </div>

    </div>
</asp:Content>
