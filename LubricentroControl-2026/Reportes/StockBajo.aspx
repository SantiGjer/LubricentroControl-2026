<%@ Page Title="Reporte de stock bajo" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="StockBajo.aspx.cs" Inherits="LubricentroControl_2026.Reportes.StockBajo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-3">Reporte de stock bajo</h1>

    <asp:Panel ID="pnlSoloLectura" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
        Tu rol tiene acceso de <b>solo consulta</b> a esta pantalla.
    </asp:Panel>

    <div class="alert alert-secondary" role="alert">
        Pendiente
    </div>
</asp:Content>