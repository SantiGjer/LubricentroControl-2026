<%@ Page Title="Compras" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Compras.aspx.cs" Inherits="LubricentroControl_2026.Compras" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Compras</h1>

    <asp:Panel ID="pnlSoloLectura" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
        Tu rol tiene acceso de <b>solo consulta</b> a esta pantalla.
    </asp:Panel>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="row">
        <div class="col-7">
            <div class="row border border-1">
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por proveedor, CUIT o número</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" />
                    <asp:CheckBox ID="chkSoloConSaldo" runat="server" AutoPostBack="true"
                        OnCheckedChanged="chkSoloConSaldo_CheckedChanged" />
                    <asp:Label runat="server" AssociatedControlID="chkSoloConSaldo">Solo con saldo pendiente</asp:Label></div>
            </div>

            <asp:GridView ID="gvCompras" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdCompra" GridLines="None"
                OnRowCommand="gvCompras_RowCommand" EmptyDataText="No hay compras que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="NumeroComprobante" HeaderText="Número" />
                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:BoundField DataField="RazonSocial" HeaderText="Proveedor" />
                    <asp:BoundField DataField="CondicionPago" HeaderText="Condición" />
                    <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" />
                    <asp:BoundField DataField="SaldoPendiente" HeaderText="Saldo pendiente" DataFormatString="{0:N2}" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdCompra") %>'
                                CausesValidation="false">Ver</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <asp:Panel ID="pnlFormulario" runat="server">
                <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva compra" /></h2>
                <asp:HiddenField ID="hdnIdCompra" runat="server" />

                <asp:Panel ID="pnlAltaCompra" runat="server">
                    <div class="row border border-1">
                        <div class="col-12">
                            <asp:Label runat="server" AssociatedControlID="txtBuscarProveedor">Proveedor</asp:Label></div>
                    </div>

                    <div class="row border border-1">
                        <div class="col-12">
                            <asp:UpdatePanel ID="upnlProveedor" runat="server">
                                <ContentTemplate>
                                    <asp:HiddenField ID="hdnIdProveedor" runat="server" />
                                    <asp:Label ID="litProveedorSeleccionado" runat="server" Text="(sin seleccionar)" />
                                    <br />
                                    <asp:TextBox ID="txtBuscarProveedor" runat="server" placeholder="Buscar por razón social o CUIT" />
                                    <asp:Button ID="btnBuscarProveedor" runat="server" Text="Buscar"
                                        OnClick="btnBuscarProveedor_Click" CausesValidation="false" />

                                    <asp:Panel ID="pnlResultadosProveedor" runat="server" Visible="false"
                                        style="position:relative; border:1px solid #999; max-height:200px; overflow-y:auto; background:#fff; margin-top:2px;">
                                        <asp:Repeater ID="rptResultadosProveedor" runat="server" OnItemCommand="rptResultadosProveedor_ItemCommand">
                                            <ItemTemplate>
                                                <div style="padding:4px; border-bottom:1px solid #ddd;">
                                                    <asp:LinkButton runat="server"
                                                        CommandName="Seleccionar" CommandArgument='<%# Eval("IdProveedor") %>'
                                                        CausesValidation="false">
                                                        <%# Eval("RazonSocial") %>
                                                    </asp:LinkButton>
                                                </div>
                                            </ItemTemplate>
                                        </asp:Repeater>
                                    </asp:Panel>

                                    <asp:CustomValidator runat="server" OnServerValidate="valProveedor_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Compra"
                                        ErrorMessage="Seleccioná el proveedor de la compra." />
                                </ContentTemplate>
                            </asp:UpdatePanel></div>
                    </div>

                    <div class="row border border-1">
                        <div class="col-6">
                            <asp:Label runat="server" AssociatedControlID="ddlCondicionPago">Condición de pago</asp:Label>
                            <br />
                            <asp:DropDownList ID="ddlCondicionPago" runat="server" AutoPostBack="true"
                                OnSelectedIndexChanged="ddlCondicionPago_SelectedIndexChanged" /></div>
                        <div class="col-6">
                            <asp:Panel ID="pnlMedioPago" runat="server">
                                <asp:Label runat="server" AssociatedControlID="ddlMedioPago">Medio de pago</asp:Label>
                                <br />
                                <asp:DropDownList ID="ddlMedioPago" runat="server" />
                                <asp:CustomValidator runat="server" OnServerValidate="valMedioPago_ServerValidate"
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Compra"
                                    ErrorMessage="Seleccioná el medio de pago." />
                            </asp:Panel></div>
                    </div>

                    <div class="row border border-1">
                        <div class="col-12">
                            <asp:Label runat="server" AssociatedControlID="txtImpuestos">Impuestos (opcional)</asp:Label></div>
                    </div>

                    <div class="row border border-1">
                        <div class="col-12">
                            <asp:TextBox ID="txtImpuestos" runat="server" MaxLength="12" Text="0" />
                            <asp:CompareValidator runat="server" ControlToValidate="txtImpuestos"
                                Operator="DataTypeCheck" Type="Currency"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Compra"
                                ErrorMessage="Los impuestos deben ser un número." /></div>
                    </div>

                    <hr />
                    <h3 class="h6">Líneas de la compra</h3>
                    <asp:Label runat="server" AssociatedControlID="ddlInsumo">Insumo</asp:Label>
                    <asp:DropDownList ID="ddlInsumo" runat="server" />
                    <asp:Label runat="server" AssociatedControlID="txtCantidadLinea">Cantidad</asp:Label>
                    <asp:TextBox ID="txtCantidadLinea" runat="server" MaxLength="12" Width="70px" />
                    <asp:Label runat="server" AssociatedControlID="txtPrecioLinea">Precio unitario</asp:Label>
                    <asp:TextBox ID="txtPrecioLinea" runat="server" MaxLength="12" Width="90px" />
                    <asp:Button ID="btnAgregarLinea" runat="server" Text="Agregar línea"
                        OnClick="btnAgregarLinea_Click" CausesValidation="false" />

                    <asp:GridView ID="gvLineasPendientes" runat="server"
                        CssClass="table table-striped table-bordered table-hover"
                        AutoGenerateColumns="false" GridLines="None"
                        OnRowCommand="gvLineasPendientes_RowCommand"
                        EmptyDataText="Todavía no agregaste ninguna línea.">
                        <Columns>
                            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                            <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:N2}" />
                            <asp:TemplateField HeaderText="Acciones">
                                <ItemTemplate>
                                    <asp:LinkButton runat="server"
                                        CommandName="Quitar" CommandArgument='<%# Container.DataItemIndex %>'
                                        CausesValidation="false">Quitar</asp:LinkButton>
                                </ItemTemplate>
                            </asp:TemplateField>
                        </Columns>
                    </asp:GridView>

                    <asp:Button ID="btnGuardarCompra" runat="server" Text="Guardar compra"
                        OnClick="btnGuardarCompra_Click" ValidationGroup="Compra" />
                </asp:Panel>

                <asp:Panel ID="pnlCompraExistente" runat="server" Visible="false">
                    <p>
                        <b>Proveedor:</b> <asp:Literal ID="litProveedorInfo" runat="server" /><br />
                        <b>Fecha:</b> <asp:Literal ID="litFechaInfo" runat="server" /><br />
                        <b>Condición de pago:</b> <asp:Literal ID="litCondicionInfo" runat="server" /><br />
                        <b>Medio de pago:</b> <asp:Literal ID="litMedioPagoInfo" runat="server" /><br />
                        <b>Subtotal:</b> <asp:Literal ID="litSubtotalInfo" runat="server" /><br />
                        <b>Impuestos:</b> <asp:Literal ID="litImpuestosInfo" runat="server" /><br />
                        <b>Total:</b> <asp:Literal ID="litTotalInfo" runat="server" /><br />
                        <b>Saldo pendiente:</b> <asp:Literal ID="litSaldoInfo" runat="server" />
                    </p>

                    <asp:GridView ID="gvDetalleCompra" runat="server"
                        CssClass="table table-striped table-bordered table-hover"
                        AutoGenerateColumns="false" GridLines="None"
                        EmptyDataText="Esta compra no tiene líneas.">
                        <Columns>
                            <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                            <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:N2}" />
                        </Columns>
                    </asp:GridView>
                </asp:Panel>

                <asp:Button ID="btnNuevaCompra" runat="server" Text="Nueva compra"
                    OnClick="btnNuevaCompra_Click" CausesValidation="false" />
            </asp:Panel>
        </div>
    </div>
</asp:Content>
