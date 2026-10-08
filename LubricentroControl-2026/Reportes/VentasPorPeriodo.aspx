<%@ Page Title="Reporte de ventas por período" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="VentasPorPeriodo.aspx.cs" Inherits="LubricentroControl_2026.Reportes.VentasPorPeriodo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="pantalla-abm">

    <h1>Reporte de ventas por período</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="row barra-busqueda barra-fechas">
        <div class="col-3">
            <asp:Label runat="server" AssociatedControlID="txtDesde">Desde</asp:Label>
            <br />
            <asp:TextBox ID="txtDesde" runat="server" TextMode="Date" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtDesde"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Filtro"
                ErrorMessage="La fecha desde es obligatoria." /></div>
        <div class="col-3">
            <asp:Label runat="server" AssociatedControlID="txtHasta">Hasta</asp:Label>
            <br />
            <asp:TextBox ID="txtHasta" runat="server" TextMode="Date" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtHasta"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Filtro"
                ErrorMessage="La fecha hasta es obligatoria." />
            <asp:CompareValidator runat="server" ControlToValidate="txtHasta" ControlToCompare="txtDesde"
                Type="Date" Operator="GreaterThanEqual"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Filtro"
                ErrorMessage="La fecha hasta no puede ser anterior a la fecha desde." /></div>
        <div class="col-3 d-flex align-items-end">
            <asp:Button ID="btnFiltrar" runat="server" CssClass="boton-rojo" Text="Filtrar" 
                OnClick="btnFiltrar_Click" ValidationGroup="Filtro" /></div>
    </div>

    <asp:Panel ID="pnlResumen" runat="server" CssClass="alert alert-secondary" role="alert">
        <asp:Literal ID="litResumen" runat="server" />
    </asp:Panel>

    <%-- Orden, filtro y paginado los hace la tabla en el navegador (Lubricentro.js). --%>
    <asp:GridView ID="gvVentas" runat="server" data-filas-por-pagina="30"
        CssClass="tabla-abm"
        AutoGenerateColumns="false" DataKeyNames="IdVenta" GridLines="None"
        OnRowDataBound="gvVentas_RowDataBound"
        EmptyDataText="No hay ventas en el período elegido.">
        <Columns>
            <asp:BoundField DataField="NumeroComprobante" HeaderText="Número" />
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
            <asp:BoundField DataField="DocumentoCliente" HeaderText="Documento" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
            <asp:BoundField DataField="Subtotal" HeaderText="Neto" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Impuestos" HeaderText="IVA" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:BoundField DataField="SaldoPendiente" HeaderText="Saldo pendiente" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:TemplateField HeaderText="Factura" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta">
                <ItemTemplate><%#: Eval("NumeroFactura") ?? "—" %></ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
    </div>
</asp:Content>
