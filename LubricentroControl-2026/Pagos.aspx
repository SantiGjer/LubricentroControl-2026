<%@ Page Title="Pagos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Pagos.aspx.cs" Inherits="LubricentroControl_2026.Pagos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Pagos</h1>

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
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por cliente o proveedor</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" /></div>
            </div>

            <asp:GridView ID="gvPagos" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdPago" GridLines="None"
                EmptyDataText="No hay pagos que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:TemplateField HeaderText="Tipo">
                        <ItemTemplate>
                            <%# Eval("Tipo").ToString() == "C" ? "Cliente" : "Proveedor" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Titular">
                        <ItemTemplate>
                            <%# Eval("Tipo").ToString() == "C" ? Eval("NombreCliente") : Eval("RazonSocial") %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="MedioPago" HeaderText="Medio de pago" />
                    <asp:BoundField DataField="Monto" HeaderText="Monto" DataFormatString="{0:N2}" />
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <h2>Registrar pago</h2>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="ddlTipo">Tipo</asp:Label>
                    <br />
                    <asp:DropDownList ID="ddlTipo" runat="server" AutoPostBack="true"
                        OnSelectedIndexChanged="ddlTipo_SelectedIndexChanged" /></div>
            </div>

            <asp:UpdatePanel ID="upnlTitular" runat="server">
                <ContentTemplate>
                    <asp:Panel ID="pnlCliente" runat="server">
                        <div class="row border border-1">
                            <div class="col-12">
                                <asp:Label runat="server" AssociatedControlID="txtBuscarCliente">Cliente</asp:Label></div>
                        </div>
                        <div class="row border border-1">
                            <div class="col-12">
                                <asp:HiddenField ID="hdnIdCliente" runat="server" />
                                <asp:Label ID="litClienteSeleccionado" runat="server" Text="(sin seleccionar)" />
                                <br />
                                <asp:TextBox ID="txtBuscarCliente" runat="server" placeholder="Buscar por nombre, apellido o DNI" />
                                <asp:Button ID="btnBuscarCliente" runat="server" Text="Buscar"
                                    OnClick="btnBuscarCliente_Click" CausesValidation="false" />

                                <asp:Panel ID="pnlResultadosCliente" runat="server" Visible="false"
                                    style="position:relative; border:1px solid #999; max-height:200px; overflow-y:auto; background:#fff; margin-top:2px;">
                                    <asp:Repeater ID="rptResultadosCliente" runat="server" OnItemCommand="rptResultadosCliente_ItemCommand">
                                        <ItemTemplate>
                                            <div style="padding:4px; border-bottom:1px solid #ddd;">
                                                <asp:LinkButton runat="server"
                                                    CommandName="Seleccionar" CommandArgument='<%# Eval("IdCliente") %>'
                                                    CausesValidation="false">
                                                    <%# Eval("NombreCompleto") %> — DNI <%# Eval("Dni") %>
                                                </asp:LinkButton>
                                            </div>
                                        </ItemTemplate>
                                    </asp:Repeater>
                                </asp:Panel>

                                <asp:CustomValidator runat="server" OnServerValidate="valCliente_ServerValidate"
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                                    ErrorMessage="Seleccioná el cliente del pago." /></div>
                        </div>
                    </asp:Panel>

                    <asp:Panel ID="pnlProveedor" runat="server" Visible="false">
                        <div class="row border border-1">
                            <div class="col-12">
                                <asp:Label runat="server" AssociatedControlID="txtBuscarProveedor">Proveedor</asp:Label></div>
                        </div>
                        <div class="row border border-1">
                            <div class="col-12">
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
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                                    ErrorMessage="Seleccioná el proveedor del pago." /></div>
                        </div>
                    </asp:Panel>

                    <div class="row border border-1">
                        <div class="col-12">
                            <asp:Label runat="server" AssociatedControlID="ddlComprobante">Comprobante a pagar</asp:Label>
                            <br />
                            <asp:DropDownList ID="ddlComprobante" runat="server" /></div>
                    </div>
                </ContentTemplate>
            </asp:UpdatePanel>

            <div class="row border border-1">
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="ddlMedioPago">Medio de pago</asp:Label>
                    <br />
                    <asp:DropDownList ID="ddlMedioPago" runat="server" /></div>
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtMonto">Monto</asp:Label>
                    <br />
                    <asp:TextBox ID="txtMonto" runat="server" MaxLength="12" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMonto"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                        ErrorMessage="El monto es obligatorio." />
                    <asp:CompareValidator runat="server" ControlToValidate="txtMonto"
                        Operator="GreaterThan" ValueToCompare="0" Type="Currency"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                        ErrorMessage="El monto debe ser mayor a cero." /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtObservaciones">Observaciones (opcional)</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtObservaciones" runat="server" MaxLength="300" TextMode="MultiLine" Rows="2" /></div>
            </div>

            <asp:Button ID="btnRegistrar" runat="server" Text="Registrar pago"
                OnClick="btnRegistrar_Click" ValidationGroup="Pago" />
        </div>
    </div>
</asp:Content>
