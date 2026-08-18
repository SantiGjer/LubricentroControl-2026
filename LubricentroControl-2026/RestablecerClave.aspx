<%@ Page Title="Restablecer contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RestablecerClave.aspx.cs" Inherits="LubricentroControl_2026.RestablecerClave" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Elegí tu contraseña nueva</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlFormulario" runat="server">
        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Label runat="server" AssociatedControlID="txtPassword">Contraseña nueva</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" autocomplete="new-password" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Restablecer"
                    ErrorMessage="Ingresá la contraseña nueva." /></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Label runat="server" AssociatedControlID="txtRepetir">Repetir contraseña</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:TextBox ID="txtRepetir" runat="server" TextMode="Password" autocomplete="new-password" />
                <asp:CompareValidator runat="server" ControlToValidate="txtRepetir" ControlToCompare="txtPassword"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Restablecer"
                    ErrorMessage="Las contraseñas no coinciden." /></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Button ID="btnGuardar" runat="server" Text="Guardar contraseña"
                    OnClick="btnGuardar_Click" ValidationGroup="Restablecer" /></div>
            <div class="col-4"></div>
        </div>
    </asp:Panel>

    <p>
        <a runat="server" href="~/Login">Ir al ingreso</a>
    </p>
</asp:Content>
