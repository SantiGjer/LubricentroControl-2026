<%@ Page Title="Ingresar" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LubricentroControl_2026.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="login-page">
        <div class="login-card">

            <div class="login-stripe"></div>

            <h1 class="login-title">Ingresar</h1>
            <p class="login-subtitle">LubricentroControl</p>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert" CssClass="login-mensaje">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="login-campo">
                <asp:Label runat="server" AssociatedControlID="txtEmail" CssClass="login-label">Mail</asp:Label>
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" autocomplete="username" CssClass="login-input" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                    CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Login"
                    ErrorMessage="Ingresá tu mail." />
            </div>

            <div class="login-campo">
                <asp:Label runat="server" AssociatedControlID="txtPassword" CssClass="login-label">Contraseña</asp:Label>
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" autocomplete="current-password" CssClass="login-input" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                    CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Login"
                    ErrorMessage="Ingresá tu contraseña." />
            </div>

            <asp:Button ID="btnIngresar" runat="server" Text="Ingresar"
                OnClick="btnIngresar_Click" ValidationGroup="Login" CssClass="login-btn" />

            <p class="login-link">
                <a runat="server" href="~/RecuperarClave">Olvidé mi contraseña</a>
            </p>

        </div>
    </div>

</asp:Content>
