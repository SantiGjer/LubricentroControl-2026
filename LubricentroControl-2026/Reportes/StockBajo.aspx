<%@ Page Title="Reporte de stock bajo" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="StockBajo.aspx.cs" Inherits="LubricentroControl_2026.Reportes.StockBajo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-3">Reporte de stock bajo</h1>

    <p>Insumos activos cuyo stock actual quedó por debajo del mínimo, ordenados por faltante de mayor a menor.</p>

    <asp:Panel ID="pnlResumen" runat="server" CssClass="alert alert-secondary" role="alert">
        <asp:Literal ID="litResumen" runat="server" />
    </asp:Panel>

    <asp:GridView ID="gvStockBajo" runat="server"
        CssClass="table table-striped table-bordered table-hover"
        AutoGenerateColumns="false" DataKeyNames="IdInsumo" GridLines="None"
        AllowPaging="true" PageSize="30"
        OnPageIndexChanging="gvStockBajo_PageIndexChanging"
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