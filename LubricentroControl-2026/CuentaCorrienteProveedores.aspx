<%@ Page Title="Cuenta corriente de proveedores" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CuentaCorrienteProveedores.aspx.cs" Inherits="LubricentroControl_2026.CuentaCorrienteProveedores" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <h1 class="h3 mb-3">Cuenta corriente de proveedores</h1>

    <asp:Panel ID="pnlSoloLectura" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
        Tu rol tiene acceso de <b>solo consulta</b> a esta pantalla.
    </asp:Panel>

    <div class="alert alert-secondary" role="alert">
        Pendiente
    </div>
</asp:Content>