<%@ Page Title="Ingresar" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="LubricentroControl_2026.Login" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Ingresar</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" autocomplete="username" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Login"
                ErrorMessage="Ingresá tu mail." /></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:Label runat="server" AssociatedControlID="txtPassword">Contraseña</asp:Label></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:TextBox ID="txtPassword" runat="server" TextMode="Password" autocomplete="current-password" />
            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPassword"
                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Login"
                ErrorMessage="Ingresá tu contraseña." /></div>
        <div class="col-4"></div>
    </div>

    <div class="row border border-1">
        <div class="col-4"></div>
        <div class="col-4">
            <asp:Button ID="btnIngresar" runat="server" Text="Ingresar"
                OnClick="btnIngresar_Click" ValidationGroup="Login" /></div>
        <div class="col-4"></div>
    </div>

    <p>
        <a runat="server" href="~/RecuperarClave">Olvidé mi contraseña</a>
    </p>
</asp:Content>
