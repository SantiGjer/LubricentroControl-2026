<%@ Page Title="Recuperar contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RecuperarClave.aspx.cs" Inherits="LubricentroControl_2026.RecuperarClave" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="login-page">
        <div class="login-card">

            <div class="login-stripe"></div>

            <h1 class="login-title">Recuperar contraseña</h1>
            <p class="login-subtitle">LubricentroControl</p>

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
                        CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Recuperar"
                        ErrorMessage="Ingresá tu mail." />
                </div>

                <asp:Button ID="btnEnviar" runat="server" Text="Enviar enlace"
                    OnClick="btnEnviar_Click" ValidationGroup="Recuperar" CssClass="login-btn" />
            </asp:Panel>

            <p class="login-link">
                <a runat="server" href="~/Login">Volver al ingreso</a>
            </p>

        </div>
    </div>

</asp:Content>
