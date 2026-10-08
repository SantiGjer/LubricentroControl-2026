<%@ Page Title="Pagos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Pagos.aspx.cs" Inherits="LubricentroControl_2026.Pagos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Pagos</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroPagos" class="filtro-tabla-texto"
            placeholder="Buscar por cliente, proveedor, comprobante, fecha o medio de pago" aria-label="Buscar pagos" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Registrar pago"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesPagos">
        <div class="grupo-opciones" data-columna="Tipo">
            <span class="titulo-opciones">Tipo</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Cliente">De clientes</button>
            <button type="button" class="opcion" data-valor="Proveedor">A proveedores</button>
        </div>
        <div class="grupo-opciones" data-columna="Medio de pago">
            <span class="titulo-opciones">Medio</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Efectivo">Efectivo</button>
            <button type="button" class="opcion" data-valor="Transferencia">Transferencia</button>
            <button type="button" class="opcion" data-valor="Tarjeta">Tarjeta</button>
        </div>
    </div>

    <%-- Un pago que canceló varias deudas se guarda como una fila por comprobante (PagoDAL.Registrar). --%>
    <asp:GridView ID="gvPagos" runat="server" CssClass="tabla-abm" data-filtro="filtroPagos"
        data-opciones-tabla="opcionesPagos" data-titulo-detalle="Pago"
        AutoGenerateColumns="false" DataKeyNames="IdPago" GridLines="None"
        EmptyDataText="Todavía no hay pagos registrados.">
        <Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="TipoDescripcion" HeaderText="Tipo" />
            <asp:BoundField DataField="Titular" HeaderText="Titular" ItemStyle-CssClass="celda-titulo" />
            <asp:TemplateField HeaderText="Comprobante">
                <ItemTemplate><%#: Eval("NumeroComprobante") ?? "A cuenta" %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="MedioPago" HeaderText="Medio de pago" />
            <asp:BoundField DataField="Monto" HeaderText="Monto" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="NombreUsuario" HeaderText="Registró" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate><a href="#" class="accion-ver" data-ver-detalle>Ver</a></ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalPago" tabindex="-1" aria-labelledby="tituloModalPago"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnRegistrar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalPago">Registrar pago</h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <%-- Se llegó cerrando una orden con "cobrar ahora" (o la de un cliente sin cuenta
                         corriente): la venta se cobra ya. --%>
                    <asp:Panel ID="pnlVieneDeOrden" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
                        <asp:Literal ID="litVieneDeOrden" runat="server" />
                    </asp:Panel>
                    <asp:HiddenField ID="hdnIdVentaCobro" runat="server" />

                    <%-- Elegir el tipo o el titular es un postback parcial (muestra el saldo del
                         titular elegido) que no cierra el modal. --%>
                    <asp:UpdatePanel ID="upnlTitular" runat="server">
                        <ContentTemplate>
                            <div class="row campos-formulario">
                                <div class="col-md-4 campo">
                                    <asp:Label runat="server" AssociatedControlID="ddlTipo">Tipo</asp:Label>
                                    <asp:DropDownList ID="ddlTipo" runat="server" AutoPostBack="true"
                                        OnSelectedIndexChanged="ddlTipo_SelectedIndexChanged" />
                                </div>

                                <asp:Panel ID="pnlCliente" runat="server" CssClass="col-md-8 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtCliente">Cliente</asp:Label>
                                    <div class="selector-busqueda" data-postback="true" data-opciones="<%: OpcionesClientes %>">
                                        <asp:TextBox ID="txtCliente" runat="server" CssClass="selector-texto" autocomplete="off"
                                            placeholder="Elegí el cliente o buscalo por nombre o DNI" />
                                        <button type="button" class="selector-boton" tabindex="-1" aria-label="Ver los clientes"></button>
                                        <asp:HiddenField ID="hdnIdCliente" runat="server" OnValueChanged="hdnIdCliente_ValueChanged" />
                                    </div>
                                    <asp:CustomValidator runat="server" OnServerValidate="valCliente_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                                        ErrorMessage="Seleccioná el cliente del pago." />
                                </asp:Panel>

                                <asp:Panel ID="pnlProveedor" runat="server" CssClass="col-md-8 campo" Visible="false">
                                    <asp:Label runat="server" AssociatedControlID="txtProveedor">Proveedor</asp:Label>
                                    <div class="selector-busqueda" data-postback="true" data-opciones="<%: OpcionesProveedores %>">
                                        <asp:TextBox ID="txtProveedor" runat="server" CssClass="selector-texto" autocomplete="off"
                                            placeholder="Elegí el proveedor o buscalo por razón social o CUIT" />
                                        <button type="button" class="selector-boton" tabindex="-1" aria-label="Ver los proveedores"></button>
                                        <asp:HiddenField ID="hdnIdProveedor" runat="server" OnValueChanged="hdnIdProveedor_ValueChanged" />
                                    </div>
                                    <asp:CustomValidator runat="server" OnServerValidate="valProveedor_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                                        ErrorMessage="Seleccioná el proveedor del pago." />
                                </asp:Panel>

                                <div class="col-12 campo">
                                    <asp:Label ID="lblSaldoTitular" runat="server" Visible="false" CssClass="valor-fijo d-block" />
                                    <p class="texto-ayuda">El pago se aplica primero a las deudas más antiguas; lo que sobre queda a favor.</p>
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>

                    <div class="row campos-formulario mt-0">
                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlMedioPago">Medio de pago</asp:Label>
                            <asp:DropDownList ID="ddlMedioPago" runat="server" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtMonto">Monto</asp:Label>
                            <asp:TextBox ID="txtMonto" runat="server" MaxLength="12" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMonto"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                                ErrorMessage="El monto es obligatorio." />
                            <asp:CompareValidator runat="server" ControlToValidate="txtMonto"
                                Operator="GreaterThan" ValueToCompare="0" Type="Currency"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Pago"
                                ErrorMessage="El monto debe ser mayor a cero." />
                        </div>

                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtObservaciones">Observaciones (opcional)</asp:Label>
                            <asp:TextBox ID="txtObservaciones" runat="server" MaxLength="300" TextMode="MultiLine" Rows="2" />
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnRegistrar" runat="server" CssClass="boton-rojo" Text="Registrar pago"
                        OnClick="btnRegistrar_Click" ValidationGroup="Pago" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
