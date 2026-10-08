<%@ Page Title="Recuperar contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RecuperarClave.aspx.cs" Inherits="LubricentroControl_2026.RecuperarClave" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="login-page">
        <div class="login-card">

            <div class="login-stripe"></div>

            <div class="login-marca" aria-hidden="true">
                <svg viewBox="0 0 24 24" xmlns="http://www.w3.org/2000/svg">
                    <path d="M12 2.2c-.35 0-.66.18-.85.47C9.5 5.3 5.5 10.6 5.5 14.6a6.5 6.5 0 0 0 13 0c0-4-4-9.3-5.65-11.93A1 1 0 0 0 12 2.2z" fill="#c0272d" />
                    <path d="M9.3 14.4a.8.8 0 0 1 .8.8 2.2 2.2 0 0 0 2.2 2.2.8.8 0 0 1 0 1.6 3.8 3.8 0 0 1-3.8-3.8.8.8 0 0 1 .8-.8z" fill="#fff" opacity=".85" />
                </svg>
            </div>

            <h1 class="login-title">Recuperar contraseña</h1>
            <p class="login-subtitle">Lubricentro Control</p>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <asp:Panel ID="pnlFormulario" runat="server">
                <p class="login-texto">
                    Ingresá el mail de tu usuario y te mandamos un enlace para elegir una contraseña nueva.
                </p>

                <div class="login-campo">
                    <asp:Label runat="server" AssociatedControlID="txtEmail" CssClass="login-label">Mail</asp:Label>
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" CssClass="login-input" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Recuperar"
                        ErrorMessage="Ingresá tu mail." />
                </div>

                <asp:Button ID="btnEnviar" runat="server" Text="Enviar enlace"
                    OnClick="btnEnviar_Click" ValidationGroup="Recuperar" CssClass="login-btn" />
            </asp:Panel>

            <div class="login-separador"></div>

            <p class="login-link">
                <a runat="server" href="~/Login">Volver al ingreso</a>
            </p>

        </div>
    </div>

</asp:Content>
