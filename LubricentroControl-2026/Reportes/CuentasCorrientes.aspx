<%@ Page Title="Reporte de cuentas corrientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CuentasCorrientes.aspx.cs" Inherits="LubricentroControl_2026.Reportes.CuentasCorrientes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-3">Reporte de cuentas corrientes</h1>

    <p>Clientes y proveedores con saldo pendiente (positivo = deuda, negativo = a favor).</p>

    <h2 class="h5">Clientes</h2>

    <asp:Panel ID="pnlResumenClientes" runat="server" CssClass="alert alert-secondary" role="alert">
        <asp:Literal ID="litResumenClientes" runat="server" />
    </asp:Panel>

    <asp:GridView ID="gvClientes" runat="server"
        CssClass="table table-striped table-bordered table-hover"
        AutoGenerateColumns="false" DataKeyNames="IdCliente" GridLines="None"
        AllowPaging="true" PageSize="30"
        OnPageIndexChanging="gvClientes_PageIndexChanging"
        EmptyDataText="Ningún cliente tiene saldo pendiente.">
        <Columns>
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
            <asp:BoundField DataField="Saldo" HeaderText="Saldo" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:BoundField DataField="Fecha" HeaderText="Último movimiento" DataFormatString="{0:dd/MM/yyyy}" />
        </Columns>
        <PagerStyle HorizontalAlign="Center" />
        <PagerTemplate>
            <asp:LinkButton runat="server" CommandName="Page" CommandArgument="Prev" CausesValidation="false"
                Text="◄ Anterior"
                Visible='<%# ((GridView)Container.NamingContainer).PageIndex > 0 %>' />
            &nbsp;Página <%# ((GridView)Container.NamingContainer).PageIndex + 1 %>
            de <%# ((GridView)Container.NamingContainer).PageCount %>&nbsp;
            <asp:LinkButton runat="server" CommandName="Page" CommandArgument="Next" CausesValidation="false"
                Text="Siguiente ►"
                Visible='<%# ((GridView)Container.NamingContainer).PageIndex < ((GridView)Container.NamingContainer).PageCount - 1 %>' />
        </PagerTemplate>
    </asp:GridView>

    <h2 class="h5">Proveedores</h2>

    <asp:Panel ID="pnlResumenProveedores" runat="server" CssClass="alert alert-secondary" role="alert">
        <asp:Literal ID="litResumenProveedores" runat="server" />
    </asp:Panel>

    <asp:GridView ID="gvProveedores" runat="server"
        CssClass="table table-striped table-bordered table-hover"
        AutoGenerateColumns="false" DataKeyNames="IdProveedor" GridLines="None"
        AllowPaging="true" PageSize="30"
        OnPageIndexChanging="gvProveedores_PageIndexChanging"
        EmptyDataText="No hay saldo pendiente con ningún proveedor.">
        <Columns>
            <asp:BoundField DataField="RazonSocial" HeaderText="Proveedor" />
            <asp:BoundField DataField="Saldo" HeaderText="Saldo" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:BoundField DataField="Fecha" HeaderText="Último movimiento" DataFormatString="{0:dd/MM/yyyy}" />
        </Columns>
        <PagerStyle HorizontalAlign="Center" />
        <PagerTemplate>
            <asp:LinkButton runat="server" CommandName="Page" CommandArgument="Prev" CausesValidation="false"
                Text="◄ Anterior"
                Visible='<%# ((GridView)Container.NamingContainer).PageIndex > 0 %>' />
            &nbsp;Página <%# ((GridView)Container.NamingContainer).PageIndex + 1 %>
            de <%# ((GridView)Container.NamingContainer).PageCount %>&nbsp;
            <asp:LinkButton runat="server" CommandName="Page" CommandArgument="Next" CausesValidation="false"
                Text="Siguiente ►"
                Visible='<%# ((GridView)Container.NamingContainer).PageIndex < ((GridView)Container.NamingContainer).PageCount - 1 %>' />
        </PagerTemplate>
    </asp:GridView>
</asp:Content>
