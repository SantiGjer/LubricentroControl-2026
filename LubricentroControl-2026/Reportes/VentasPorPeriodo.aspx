<%@ Page Title="Reporte de ventas por período" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="VentasPorPeriodo.aspx.cs" Inherits="LubricentroControl_2026.Reportes.VentasPorPeriodo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-3">Reporte de ventas por período</h1>

    <asp:Panel ID="pnlSoloLectura" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
        Tu rol tiene acceso de <b>solo consulta</b> a esta pantalla.
    </asp:Panel>

    <div class="alert alert-secondary" role="alert">
        Pendiente
    </div>
</asp:Content>