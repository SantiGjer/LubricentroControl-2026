<%@ Page Title="Cambiar contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CambiarClave.aspx.cs" Inherits="LubricentroControl_2026.CambiarClave" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Cambiar contraseña</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:Label runat="server" AssociatedControlID="txtActual">Contraseña actual</asp:Label></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:TextBox ID="txtActual" runat="server" TextMode="Password" autocomplete="current-password" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtActual"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cambiar"
                ErrorMessage="Ingresá tu contraseña actual." /></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:Label runat="server" AssociatedControlID="txtNueva">Contraseña nueva</asp:Label></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:TextBox ID="txtNueva" runat="server" TextMode="Password" autocomplete="new-password" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNueva"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cambiar"
                ErrorMessage="Ingresá la contraseña nueva." /></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:Label runat="server" AssociatedControlID="txtRepetir">Repetir contraseña nueva</asp:Label></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:TextBox ID="txtRepetir" runat="server" TextMode="Password" autocomplete="new-password" />
            <asp:CompareValidator runat="server" ControlToValidate="txtRepetir" ControlToCompare="txtNueva"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cambiar"
                ErrorMessage="Las contraseñas no coinciden." /></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                OnClick="btnGuardar_Click" ValidationGroup="Cambiar" /></div>
        <div class="col-4"></div>
    </div>
</asp:Content>
