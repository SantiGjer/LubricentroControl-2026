<%@ Page Title="Inicio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="LubricentroControl_2026._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Panel de control</h1>

    <%-- Cada tarjeta ocupa todo el alto de su fila y lleva el botón abajo, separado de la tabla
         por una línea (pie-tarjeta). Las tablas se ordenan pero no llevan filtro: son cortas. --%>
    <div class="row">
        <div class="col-lg-6">
            <div class="panel-gris tarjeta-tablero">
                <h2>Turnos de hoy</h2>
                <p class="resumen-tablero"><asp:Literal ID="litResumenTurnos" runat="server" /></p>

                <asp:GridView ID="gvTurnosHoy" runat="server" CssClass="tabla-abm tabla-compacta" data-sin-filtro="true"
                    AutoGenerateColumns="false" GridLines="None"
                    EmptyDataText="No hay turnos pendientes para hoy.">
                    <EmptyDataRowStyle CssClass="tabla-vacia" />
                    <Columns>
                        <asp:BoundField DataField="FechaHoraAsignada" HeaderText="Hora" DataFormatString="{0:HH:mm}" />
                        <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
                        <asp:TemplateField HeaderText="Vehículo">
                            <ItemTemplate>
                                <%# Eval("Patente") ?? "—" %>
                            </ItemTemplate>
                        </asp:TemplateField>
                        <asp:BoundField DataField="Estado" HeaderText="Estado" />
                    </Columns>
                </asp:GridView>

                <div class="pie-tarjeta">
                    <div class="pie-tarjeta-linea">
                        <asp:HyperLink ID="lnkTurnos" runat="server" CssClass="boton-rojo enlace-boton"
                            NavigateUrl="~/Turnos" Text="Ir a Turnos" />
                    </div>
                </div>
            </div>
        </div>

        <div class="col-lg-6">
            <div class="panel-gris tarjeta-tablero">
                <h2>Stock bajo</h2>
                <p class="resumen-tablero"><asp:Literal ID="litResumenStock" runat="server" /></p>

                <asp:GridView ID="gvStockBajo" runat="server" CssClass="tabla-abm tabla-compacta" data-sin-filtro="true"
                    AutoGenerateColumns="false" GridLines="None"
                    OnRowDataBound="gvStockBajo_RowDataBound"
                    EmptyDataText="Ningún insumo está por debajo de su stock mínimo.">
                    <EmptyDataRowStyle CssClass="tabla-vacia" />
                    <Columns>
                        <asp:BoundField DataField="Nombre" HeaderText="Insumo" />
                        <asp:BoundField DataField="StockActual" HeaderText="Stock" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="StockMinimo" HeaderText="Mínimo" DataFormatString="{0:N2}" />
                        <asp:TemplateField HeaderText="Faltante">
                            <ItemTemplate>
                                <%# ((decimal)Eval("StockMinimo") - (decimal)Eval("StockActual")).ToString("N2") %>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>

                <div class="pie-tarjeta">
                    <div class="pie-tarjeta-linea">
                        <asp:HyperLink ID="lnkStock" runat="server" CssClass="boton-rojo enlace-boton"
                            NavigateUrl="~/Reportes/StockBajo" Text="Ver reporte de stock bajo" />
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="row">
        <div class="col-12">
            <div class="panel-gris tarjeta-tablero">
                <h2>Órdenes en curso</h2>
                <p class="resumen-tablero"><asp:Literal ID="litResumenOrdenes" runat="server" /></p>

                <asp:GridView ID="gvOrdenesEnCurso" runat="server" CssClass="tabla-abm" data-sin-filtro="true"
                    AutoGenerateColumns="false" GridLines="None"
                    EmptyDataText="No hay órdenes en curso.">
                    <EmptyDataRowStyle CssClass="tabla-vacia" />
                    <Columns>
                        <asp:BoundField DataField="IdOrden" HeaderText="N.º" />
                        <asp:BoundField DataField="Fecha" HeaderText="Ingreso" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                        <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
                        <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
                        <asp:BoundField DataField="Estado" HeaderText="Estado" />
                    </Columns>
                </asp:GridView>

                <div class="pie-tarjeta">
                    <div class="pie-tarjeta-linea">
                        <asp:HyperLink ID="lnkOrdenes" runat="server" CssClass="boton-rojo enlace-boton"
                            NavigateUrl="~/OrdenesDeTrabajo" Text="Ir a Órdenes de trabajo" />
                    </div>
                </div>
            </div>
        </div>
    </div>

    </div>
</asp:Content>
