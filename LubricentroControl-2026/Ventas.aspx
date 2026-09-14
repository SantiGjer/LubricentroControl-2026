<%@ Page Title="Ventas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Ventas.aspx.cs" Inherits="LubricentroControl_2026.Ventas" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Ventas</h1>

    <asp:Panel ID="pnlSoloLectura" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
        Tu rol tiene acceso de <b>solo consulta</b> a esta pantalla.
    </asp:Panel>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <p><small>El comprobante de venta se genera automáticamente al cerrar una orden de trabajo
        (pantalla <a href="~/OrdenesDeTrabajo" runat="server">Órdenes de trabajo</a>) — acá solo se consulta.</small></p>

    <div class="row">
        <div class="col-7">
            <div class="row border border-1">
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por cliente, patente o número</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" /></div>
            </div>

            <asp:GridView ID="gvVentas" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdVenta" GridLines="None"
                OnRowCommand="gvVentas_RowCommand" EmptyDataText="No hay ventas que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="NumeroComprobante" HeaderText="Número" />
                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
                    <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
                    <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
                    <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" />
                    <asp:BoundField DataField="SaldoPendiente" HeaderText="Saldo pendiente" DataFormatString="{0:N2}" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdVenta") %>'
                                CausesValidation="false">Ver</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <h2><asp:Literal ID="litTituloDetalle" runat="server" /></h2>
                <p>
                    <b>Cliente:</b> <asp:Literal ID="litClienteInfo" runat="server" /><br />
                    <b>Vehículo:</b> <asp:Literal ID="litVehiculoInfo" runat="server" /><br />
                    <b>Fecha:</b> <asp:Literal ID="litFechaInfo" runat="server" /><br />
                    <b>Subtotal:</b> <asp:Literal ID="litSubtotalInfo" runat="server" /><br />
                    <b>Impuestos:</b> <asp:Literal ID="litImpuestosInfo" runat="server" /><br />
                    <b>Total:</b> <asp:Literal ID="litTotalInfo" runat="server" /><br />
                    <b>Saldo pendiente:</b> <asp:Literal ID="litSaldoInfo" runat="server" />
                </p>

                <asp:GridView ID="gvDetalleVenta" runat="server"
                    CssClass="table table-striped table-bordered table-hover"
                    AutoGenerateColumns="false" GridLines="None"
                    EmptyDataText="Esta venta no tiene líneas.">
                    <Columns>
                        <asp:BoundField DataField="Descripcion" HeaderText="Ítem" />
                        <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="Subtotal" HeaderText="Subtotal" DataFormatString="{0:N2}" />
                    </Columns>
                </asp:GridView>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
