<%@ Page Title="Crear cuenta" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Registro.aspx.cs" Inherits="LubricentroControl_2026.Registro" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="login-page">
        <div class="login-card">

            <div class="login-stripe"></div>

            <h1 class="login-title">Crear cuenta</h1>
            <p class="login-subtitle">LubricentroControl</p>

            <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert" CssClass="login-mensaje">
                <asp:Literal ID="litMensaje" runat="server" />
            </asp:Panel>

            <div class="login-campo">
                <asp:Label runat="server" AssociatedControlID="txtNombre" CssClass="login-label">Nombre</asp:Label>
                <asp:TextBox ID="txtNombre" runat="server" CssClass="login-input" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                    CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Registro"
                    ErrorMessage="Ingresá tu nombre." />
            </div>

            <div class="login-campo">
                <asp:Label runat="server" AssociatedControlID="txtApellido" CssClass="login-label">Apellido</asp:Label>
                <asp:TextBox ID="txtApellido" runat="server" CssClass="login-input" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtApellido"
                    CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Registro"
                    ErrorMessage="Ingresá tu apellido." />
            </div>

            <div class="login-campo">
                <asp:Label runat="server" AssociatedControlID="txtEmail" CssClass="login-label">Mail</asp:Label>
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" autocomplete="username" CssClass="login-input" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                    CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Registro"
                    ErrorMessage="Ingresá tu mail." />
            </div>

            <div class="login-campo">
                <asp:Label runat="server" AssociatedControlID="txtPassword" CssClass="login-label">Contraseña</asp:Label>
                <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" autocomplete="new-password" CssClass="login-input" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                    CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Registro"
                    ErrorMessage="Ingresá una contraseña." />
            </div>

            <div class="login-campo">
                <asp:Label runat="server" AssociatedControlID="txtRepetir" CssClass="login-label">Repetir contraseña</asp:Label>
                <asp:TextBox ID="txtRepetir" runat="server" TextMode="Password" autocomplete="new-password" CssClass="login-input" />
                <asp:CompareValidator runat="server" ControlToValidate="txtRepetir" ControlToCompare="txtPassword"
                    CssClass="text-danger small d-block mt-1" Display="Dynamic" ValidationGroup="Registro"
                    ErrorMessage="Las contraseñas no coinciden." />
            </div>

            <asp:Button ID="btnRegistrar" runat="server" Text="Crear cuenta"
                OnClick="btnRegistrar_Click" ValidationGroup="Registro" CssClass="login-btn" />

            <p class="login-link">
                <a runat="server" href="~/Login">Ya tengo cuenta</a>
            </p>

        </div>
    </div>

</asp:Content>
