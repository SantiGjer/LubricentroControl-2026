<%@ Page Title="Reporte de stock bajo" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="StockBajo.aspx.cs" Inherits="LubricentroControl_2026.Reportes.StockBajo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="pantalla-abm">

    <h1>Reporte de stock bajo</h1>

    <p class="texto-nota">Insumos activos cuyo stock actual quedó por debajo del mínimo, ordenados por faltante de mayor a menor.</p>

    <asp:Panel ID="pnlResumen" runat="server" CssClass="alert alert-secondary" role="alert">
        <asp:Literal ID="litResumen" runat="server" />
    </asp:Panel>

    <%-- Orden, filtro y paginado los hace la tabla en el navegador (Lubricentro.js). --%>
    <asp:GridView ID="gvStockBajo" runat="server" data-filas-por-pagina="30"
        CssClass="tabla-abm"
        AutoGenerateColumns="false" DataKeyNames="IdInsumo" GridLines="None"
        OnRowDataBound="gvStockBajo_RowDataBound"
        EmptyDataText="Ningún insumo activo está por debajo de su stock mínimo.">
        <Columns>
            <asp:BoundField DataField="Nombre" HeaderText="Insumo" />
            <asp:BoundField DataField="Marca" HeaderText="Marca" />
            <asp:BoundField DataField="UnidadMedida" HeaderText="Unidad" />
            <asp:BoundField DataField="StockActual" HeaderText="Stock actual" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:BoundField DataField="StockMinimo" HeaderText="Stock mínimo" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
            <asp:TemplateField HeaderText="Faltante"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right">
                <ItemTemplate>
                    <%# ((decimal)Eval("StockMinimo") - (decimal)Eval("StockActual")).ToString("N2") %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="PrecioVenta" HeaderText="Precio venta" DataFormatString="{0:N2}"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
        </Columns>
    </asp:GridView>
    </div>
</asp:Content>
