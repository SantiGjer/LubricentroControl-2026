<%@ Page Title="Ventas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Ventas.aspx.cs" Inherits="LubricentroControl_2026.Ventas" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Ventas</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <p class="texto-nota"><small>El comprobante de venta se genera automáticamente al cerrar una orden de trabajo
        (pantalla <a href="~/OrdenesDeTrabajo" runat="server">Órdenes de trabajo</a>) — acá solo se consulta.</small></p>

    <div class="barra-herramientas">
        <input type="search" id="filtroVentas" class="filtro-tabla-texto"
            placeholder="Filtrar por número, cliente, patente o fecha" aria-label="Filtrar ventas" />
    </div>

    <asp:GridView ID="gvVentas" runat="server" CssClass="tabla-abm" data-filtro="filtroVentas"
        AutoGenerateColumns="false" DataKeyNames="IdVenta" GridLines="None"
        OnRowCommand="gvVentas_RowCommand" EmptyDataText="Todavía no hay ventas.">
        <Columns>
            <asp:BoundField DataField="NumeroComprobante" HeaderText="Número" />
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
            <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
            <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="SaldoPendiente" HeaderText="Saldo pendiente" DataFormatString="{0:N2}" />
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
                        CommandName="Ver" CommandArgument='<%# Eval("IdVenta") %>'
                        CausesValidation="false">Ver</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalVenta" tabindex="-1" aria-labelledby="tituloModalVenta" aria-hidden="true">
        <div class="modal-dialog modal-lg">
            <div class="modal-content">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalVenta">
                        <asp:Literal ID="litTituloDetalle" runat="server" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <dl class="datos-resumen">
                        <dt>Cliente</dt><dd><asp:Literal ID="litClienteInfo" runat="server" /></dd>
                        <dt>Vehículo</dt><dd><asp:Literal ID="litVehiculoInfo" runat="server" /></dd>
                        <dt>Fecha</dt><dd><asp:Literal ID="litFechaInfo" runat="server" /></dd>
                        <dt>Subtotal</dt><dd><asp:Literal ID="litSubtotalInfo" runat="server" /></dd>
                        <dt>Impuestos</dt><dd><asp:Literal ID="litImpuestosInfo" runat="server" /></dd>
                        <dt>Total</dt><dd><asp:Literal ID="litTotalInfo" runat="server" /></dd>
                        <dt>Saldo pendiente</dt><dd><asp:Literal ID="litSaldoInfo" runat="server" /></dd>
                    </dl>

                    <asp:GridView ID="gvDetalleVenta" runat="server" data-sin-filtro="true"
                        CssClass="tabla-abm tabla-compacta"
                        AutoGenerateColumns="false" GridLines="None"
                        EmptyDataText="Esta venta no tiene líneas.">
                        <Columns>
                            <asp:BoundField DataField="Descripcion" HeaderText="Ítem" />
                            <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="Subtotal" HeaderText="Subtotal" DataFormatString="{0:N2}" />
                        </Columns>
                    </asp:GridView>
                </div>

                <div class="modal-footer">
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cerrar</button>
                </div>
            </div>
        </div>
    </div>

    </div>
</asp:Content>
