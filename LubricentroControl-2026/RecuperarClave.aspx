<%@ Page Title="Recuperar contraseña" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="RecuperarClave.aspx.cs" Inherits="LubricentroControl_2026.RecuperarClave" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Recuperar contraseña</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlFormulario" runat="server">
        <p>
            Ingresá el mail de tu usuario y te mandamos un enlace para elegir una contraseña nueva.
        </p>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Recuperar"
                    ErrorMessage="Ingresá tu mail." /></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Button ID="btnEnviar" runat="server" Text="Enviar enlace"
                    OnClick="btnEnviar_Click" ValidationGroup="Recuperar" /></div>
            <div class="col-4"></div>
        </div>
    </asp:Panel>

    <p>
        <a runat="server" href="~/Login">Volver al ingreso</a>
    </p>
</asp:Content>
