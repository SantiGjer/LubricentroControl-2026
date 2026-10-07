<%@ Page Title="Cuenta corriente de clientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CuentaCorrienteClientes.aspx.cs" Inherits="LubricentroControl_2026.CuentaCorrienteClientes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Cuenta corriente de clientes</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroClientes" class="filtro-tabla-texto"
            placeholder="Filtrar por nombre, apellido o DNI" aria-label="Filtrar clientes" />
        <span class="opcion-barra">
            <asp:CheckBox ID="chkIncluirSinCuenta" runat="server" AutoPostBack="true"
                OnCheckedChanged="chkIncluirSinCuenta_CheckedChanged" />
            <asp:Label runat="server" AssociatedControlID="chkIncluirSinCuenta">Incluir clientes sin cuenta corriente</asp:Label>
        </span>
    </div>

    <asp:GridView ID="gvClientes" runat="server" CssClass="tabla-abm" data-filtro="filtroClientes"
        AutoGenerateColumns="false" DataKeyNames="IdCliente" GridLines="None"
        OnRowCommand="gvClientes_RowCommand" EmptyDataText="No hay clientes con cuenta corriente.">
        <Columns>
            <asp:BoundField DataField="Apellido" HeaderText="Apellido" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="Dni" HeaderText="DNI" />
            <asp:TemplateField HeaderText="Cuenta corriente">
                <ItemTemplate>
                    <%# (bool)Eval("CuentaCorriente") ? "Habilitada" : "No habilitada" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
                        CommandName="Ver" CommandArgument='<%# Eval("IdCliente") %>'
                        CausesValidation="false">Ver cuenta</asp:LinkButton>
                    <asp:HyperLink runat="server" Visible='<%# PuedeEditarCliente %>'
                        NavigateUrl='<%# UrlEditarCliente((int)Eval("IdCliente")) %>'>Editar cliente</asp:HyperLink>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Historial y saldo del cliente, y para quien puede escribir, el ajuste manual. --%>
    <div class="modal fade" id="modalCuenta" tabindex="-1" aria-labelledby="tituloModalCuenta"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-xl">
            <asp:Panel ID="pnlDetalle" runat="server" CssClass="modal-content" DefaultButton="btnRegistrarAjuste">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalCuenta">
                        <asp:Literal ID="litClienteSeleccionado" runat="server" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlMensajeFormulario" runat="server" Visible="false" role="alert">
                        <asp:Literal ID="litMensajeFormulario" runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlSinCuenta" runat="server" Visible="false" CssClass="alert alert-secondary">
                        Este cliente no tiene la cuenta corriente habilitada: no puede quedar debiendo y sus órdenes
                        se cobran al cerrarlas. Para habilitarla, editá el cliente.
                    </asp:Panel>

                    <dl class="datos-resumen">
                        <dt>Saldo actual</dt><dd><asp:Literal ID="litSaldoActual" runat="server" /></dd>
                    </dl>

                    <div class="row g-4">
                        <asp:Panel ID="pnlHistorial" runat="server" CssClass="col-lg-8">
                            <h3>Movimientos</h3>
                            <asp:GridView ID="gvHistorial" runat="server" data-filas-por-pagina="10"
                                CssClass="tabla-abm tabla-compacta"
                                AutoGenerateColumns="false" GridLines="None"
                                EmptyDataText="Todavía no hay movimientos registrados.">
                                <Columns>
                                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                                    <asp:BoundField DataField="TipoMovimiento" HeaderText="Tipo" />
                                    <asp:BoundField DataField="Debe" HeaderText="Debe" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="Haber" HeaderText="Haber" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="Saldo" HeaderText="Saldo" DataFormatString="{0:N2}" />
                                    <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                                    <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
                                </Columns>
                            </asp:GridView>
                        </asp:Panel>

                        <asp:Panel ID="pnlAjuste" runat="server" CssClass="col-lg-4">
                            <div class="seccion-modal">
                                <h3>Registrar ajuste</h3>
                                <div class="row campos-formulario">
                                    <div class="col-12 campo">
                                        <asp:Label runat="server" AssociatedControlID="txtMontoAjuste">Monto (negativo reduce la deuda, positivo la aumenta)</asp:Label>
                                        <asp:TextBox ID="txtMontoAjuste" runat="server" MaxLength="12" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMontoAjuste"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="El monto es obligatorio." />
                                        <asp:CompareValidator runat="server" ControlToValidate="txtMontoAjuste"
                                            Operator="NotEqual" ValueToCompare="0" Type="Currency"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="El monto no puede ser cero." />
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
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:HyperLink ID="lnkEditarCliente" runat="server" CssClass="boton-gris enlace-boton">Editar cliente</asp:HyperLink>
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cerrar</button>
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
