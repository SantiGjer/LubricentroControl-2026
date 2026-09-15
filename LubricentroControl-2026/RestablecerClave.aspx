<%@ Page Title="Restablecer contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RestablecerClave.aspx.cs" Inherits="LubricentroControl_2026.RestablecerClave" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="login-page">
        <div class="login-card">

            <div class="login-stripe"></div>

            <h1 class="login-title">Elegí tu contraseña nueva</h1>
            <p class="login-subtitle">LubricentroControl</p>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlFormulario" runat="server">
                <div class="login-campo">
                    <asp:Label runat="server" AssociatedControlID="txtPassword" CssClass="login-label">Contraseña nueva</asp:Label>
                    <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" autocomplete="new-password" CssClass="login-input" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                        CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Restablecer"
                        ErrorMessage="Ingresá la contraseña nueva." />
                </div>

                <div class="login-campo">
                    <asp:Label runat="server" AssociatedControlID="txtRepetir" CssClass="login-label">Repetir contraseña</asp:Label>
                    <asp:TextBox ID="txtRepetir" runat="server" TextMode="Password" autocomplete="new-password" CssClass="login-input" />
                    <asp:CompareValidator runat="server" ControlToValidate="txtRepetir" ControlToCompare="txtPassword"
                        CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Restablecer"
                        ErrorMessage="Las contraseñas no coinciden." />
                </div>

                <asp:Button ID="btnGuardar" runat="server" Text="Guardar contraseña"
                    OnClick="btnGuardar_Click" ValidationGroup="Restablecer" CssClass="login-btn" />
            </asp:Panel>

            <p class="login-link">
                <a runat="server" href="~/Login">Ir al ingreso</a>
            </p>

        </div>
    </div>

</asp:Content>
