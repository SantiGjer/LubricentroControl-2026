<%@ Page Title="Reporte de cuentas corrientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CuentasCorrientes.aspx.cs" Inherits="LubricentroControl_2026.Reportes.CuentasCorrientes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="pantalla-abm">

    <h1>Reporte de cuentas corrientes</h1>

    <p class="texto-nota">Clientes y proveedores con saldo pendiente (positivo = deuda, negativo = a favor).</p>

    <h2 class="titulo-seccion">Clientes</h2>

    <asp:Panel ID="pnlResumenClientes" runat="server" CssClass="alert alert-secondary" role="alert">
        <asp:Literal ID="litResumenClientes" runat="server" />
    </asp:Panel>

    <%-- Orden, filtro y paginado de las dos tablas los hace el navegador (Lubricentro.js). --%>
    <asp:GridView ID="gvClientes" runat="server" data-filas-por-pagina="30"
        CssClass="tabla-abm"
        AutoGenerateColumns="false" DataKeyNames="IdCliente" GridLines="None"
        EmptyDataText="Ningún cliente tiene saldo pendiente.">
        <Columns>
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
            <asp:BoundField DataField="Saldo" HeaderText="Saldo" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:BoundField DataField="Fecha" HeaderText="Último movimiento" DataFormatString="{0:dd/MM/yyyy}" />
        </Columns>
    </asp:GridView>

    <h2 class="titulo-seccion mt-4">Proveedores</h2>

    <asp:Panel ID="pnlResumenProveedores" runat="server" CssClass="alert alert-secondary" role="alert">
        <asp:Literal ID="litResumenProveedores" runat="server" />
    </asp:Panel>

    <asp:GridView ID="gvProveedores" runat="server" data-filas-por-pagina="30"
        CssClass="tabla-abm"
        AutoGenerateColumns="false" DataKeyNames="IdProveedor" GridLines="None"
        EmptyDataText="No hay saldo pendiente con ningún proveedor.">
        <Columns>
            <asp:BoundField DataField="RazonSocial" HeaderText="Proveedor" />
            <asp:BoundField DataField="Saldo" HeaderText="Saldo" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:BoundField DataField="Fecha" HeaderText="Último movimiento" DataFormatString="{0:dd/MM/yyyy}" />
        </Columns>
    </asp:GridView>
    </div>
</asp:Content>
