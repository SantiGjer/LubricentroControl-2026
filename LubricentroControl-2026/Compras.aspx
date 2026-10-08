<%@ Page Title="Compras" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Compras.aspx.cs" Inherits="LubricentroControl_2026.Compras" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Compras</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroCompras" class="filtro-tabla-texto"
            placeholder="Buscar por número, proveedor, CUIT o fecha" aria-label="Buscar compras" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevaCompra" runat="server" CssClass="boton-rojo" Text="Nueva compra"
                OnClick="btnNuevaCompra_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesCompras">
        <div class="grupo-opciones" data-columna="Condición">
            <span class="titulo-opciones">Condición</span>
            <button type="button" class="opcion" data-valor="">Todas</button>
            <button type="button" class="opcion" data-valor="Contado">Contado</button>
            <button type="button" class="opcion" data-valor="Cuenta corriente">Cuenta corriente</button>
        </div>
        <div class="grupo-opciones" data-atributo="saldo">
            <span class="titulo-opciones">Saldo</span>
            <button type="button" class="opcion" data-valor="">Todas</button>
            <button type="button" class="opcion" data-valor="pendiente">Con saldo pendiente</button>
            <button type="button" class="opcion" data-valor="pagada">Pagadas</button>
        </div>
    </div>

    <asp:GridView ID="gvCompras" runat="server" CssClass="tabla-abm" data-filtro="filtroCompras"
        data-opciones-tabla="opcionesCompras"
        AutoGenerateColumns="false" DataKeyNames="IdCompra" GridLines="None"
        OnRowCommand="gvCompras_RowCommand" OnRowDataBound="gvCompras_RowDataBound"
        EmptyDataText="No hay compras para mostrar.">
        <Columns>
            <asp:BoundField DataField="NumeroComprobante" HeaderText="Número" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="RazonSocial" HeaderText="Proveedor" />
            <asp:TemplateField HeaderText="CUIT" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta">
                <ItemTemplate><%#: BIZ.Modelo.Proveedor.FormatearCuit(Convert.ToString(Eval("Cuit"))) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="CondicionPago" HeaderText="Condición" />
            <asp:BoundField DataField="MedioPago" HeaderText="Medio de pago" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Subtotal" HeaderText="Subtotal" DataFormatString="{0:N2}" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Impuestos" HeaderText="Impuestos" DataFormatString="{0:N2}" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="SaldoPendiente" HeaderText="Saldo pendiente" DataFormatString="{0:N2}" />
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server" CssClass="accion-ver"
                        CommandName="Ver" CommandArgument='<%# Eval("IdCompra") %>'
                        CausesValidation="false">Ver</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Alta de una compra (líneas armadas en memoria, se guardan todas juntas) o vista de solo
         lectura de una ya registrada, en el mismo modal. El cuerpo va en un UpdatePanel: cambiar la
         condición de pago o agregar/quitar líneas no cierra el modal. --%>
    <div class="modal fade" id="modalCompra" tabindex="-1" aria-labelledby="tituloModalCompra"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardarCompra">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalCompra">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva compra" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                <asp:UpdatePanel ID="upnlCompra" runat="server">
                    <ContentTemplate>
                        <asp:Panel ID="pnlMensajeFormulario" runat="server" Visible="false" role="alert">
                            <asp:Literal ID="litMensajeFormulario" runat="server" />
                        </asp:Panel>

                        <asp:HiddenField ID="hdnIdCompra" runat="server" />

                        <asp:Panel ID="pnlAltaCompra" runat="server">
                            <div class="row campos-formulario">
                                <div class="col-12 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtProveedor">Proveedor</asp:Label>
                                    <div class="selector-busqueda" data-opciones="<%: OpcionesProveedores %>">
                                        <asp:TextBox ID="txtProveedor" runat="server" CssClass="selector-texto" autocomplete="off"
                                            placeholder="Elegí el proveedor o buscalo por razón social o CUIT" />
                                        <button type="button" class="selector-boton" tabindex="-1" aria-label="Ver los proveedores"></button>
                                        <asp:HiddenField ID="hdnIdProveedor" runat="server" />
                                    </div>
                                    <asp:CustomValidator runat="server" OnServerValidate="valProveedor_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Compra"
                                        ErrorMessage="Seleccioná el proveedor de la compra." />
                                </div>

                                <div class="col-md-4 campo">
                                    <asp:Label runat="server" AssociatedControlID="ddlCondicionPago">Condición de pago</asp:Label>
                                    <asp:DropDownList ID="ddlCondicionPago" runat="server" AutoPostBack="true"
                                        OnSelectedIndexChanged="ddlCondicionPago_SelectedIndexChanged" />
                                </div>

                                <asp:Panel ID="pnlMedioPago" runat="server" CssClass="col-md-4 campo">
                                    <asp:Label runat="server" AssociatedControlID="ddlMedioPago">Medio de pago</asp:Label>
                                    <asp:DropDownList ID="ddlMedioPago" runat="server" />
                                    <asp:CustomValidator runat="server" OnServerValidate="valMedioPago_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Compra"
                                        ErrorMessage="Seleccioná el medio de pago." />
                                </asp:Panel>

                                <div class="col-md-4 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtImpuestos">Impuestos (opcional)</asp:Label>
                                    <asp:TextBox ID="txtImpuestos" runat="server" MaxLength="12" Text="0" />
                                    <asp:CompareValidator runat="server" ControlToValidate="txtImpuestos"
                                        Operator="DataTypeCheck" Type="Currency"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Compra"
                                        ErrorMessage="Los impuestos deben ser un número." />
                                </div>
                            </div>

                            <div class="separador-modal"></div>
                            <h3>Líneas de la compra</h3>

                            <asp:Panel ID="pnlAgregarLinea" runat="server" DefaultButton="btnAgregarLinea"
                                CssClass="row campos-formulario align-items-end mb-3">
                                <div class="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="ddlInsumo">Insumo</asp:Label>
                                    <asp:DropDownList ID="ddlInsumo" runat="server" CssClass="w-100" />
                                </div>
                                <div class="col-md-2 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtCantidadLinea">Cantidad</asp:Label>
                                    <asp:TextBox ID="txtCantidadLinea" runat="server" MaxLength="12" />
                                </div>
                                <div class="col-md-2 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtPrecioLinea">Precio unitario</asp:Label>
                                    <asp:TextBox ID="txtPrecioLinea" runat="server" MaxLength="12" />
                                </div>
                                <div class="col-md-2 campo">
                                    <asp:Button ID="btnAgregarLinea" runat="server" CssClass="boton-gris w-100 m-0" Text="Agregar"
                                        OnClick="btnAgregarLinea_Click" CausesValidation="false" />
                                </div>
                            </asp:Panel>

                            <asp:GridView ID="gvLineasPendientes" runat="server" data-sin-filtro="true"
                                CssClass="tabla-abm tabla-compacta"
                                AutoGenerateColumns="false" GridLines="None"
                                OnRowCommand="gvLineasPendientes_RowCommand"
                                EmptyDataText="Todavía no agregaste ninguna línea.">
                                <Columns>
                                    <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                                    <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:N2}" />
                                    <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                                        <ItemTemplate>
                                            <asp:LinkButton runat="server"
                                                CommandName="Quitar" CommandArgument='<%# Container.DataItemIndex %>'
                                                CausesValidation="false">Quitar</asp:LinkButton>
                                        </ItemTemplate>
                                    </asp:TemplateField>
                                </Columns>
                            </asp:GridView>
                        </asp:Panel>

                        <asp:Panel ID="pnlCompraExistente" runat="server" Visible="false">
                            <dl class="datos-resumen">
                                <dt>Proveedor</dt><dd><asp:Literal ID="litProveedorInfo" runat="server" /></dd>
                                <dt>Fecha</dt><dd><asp:Literal ID="litFechaInfo" runat="server" /></dd>
                                <dt>Condición de pago</dt><dd><asp:Literal ID="litCondicionInfo" runat="server" /></dd>
                                <dt>Medio de pago</dt><dd><asp:Literal ID="litMedioPagoInfo" runat="server" /></dd>
                                <dt>Subtotal</dt><dd><asp:Literal ID="litSubtotalInfo" runat="server" /></dd>
                                <dt>Impuestos</dt><dd><asp:Literal ID="litImpuestosInfo" runat="server" /></dd>
                                <dt>Total</dt><dd><asp:Literal ID="litTotalInfo" runat="server" /></dd>
                                <dt>Saldo pendiente</dt><dd><asp:Literal ID="litSaldoInfo" runat="server" /></dd>
                            </dl>

                            <asp:GridView ID="gvDetalleCompra" runat="server" data-sin-filtro="true"
                                CssClass="tabla-abm tabla-compacta"
                                AutoGenerateColumns="false" GridLines="None"
                                EmptyDataText="Esta compra no tiene líneas.">
                                <Columns>
                                    <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                                    <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:N2}" />
                                </Columns>
                            </asp:GridView>
                        </asp:Panel>
                    </ContentTemplate>
                </asp:UpdatePanel>
                </div>

                <div class="modal-footer">
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">
                        <asp:Literal ID="litBotonCerrar" runat="server" Text="Cancelar" /></button>
                    <asp:Button ID="btnGuardarCompra" runat="server" CssClass="boton-rojo" Text="Guardar compra"
                        OnClick="btnGuardarCompra_Click" ValidationGroup="Compra" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
