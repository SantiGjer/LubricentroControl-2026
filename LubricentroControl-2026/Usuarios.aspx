<%@ Page Title="Usuarios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Usuarios.aspx.cs" Inherits="LubricentroControl_2026.Usuarios" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Usuarios</h1>
    <asp:Button ID="btnNuevo" runat="server" Text="Nuevo usuario"
        OnClick="btnNuevo_Click" CausesValidation="false" />

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlFormulario" runat="server" Visible="false">
        <h2><asp:Literal ID="litTituloFormulario" runat="server" /></h2>
        <asp:HiddenField ID="hdnIdUsuario" runat="server" />

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:TextBox ID="txtNombre" runat="server" MaxLength="50" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Usuario"
                    ErrorMessage="El nombre es obligatorio." /></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Label runat="server" AssociatedControlID="txtApellido">Apellido</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:TextBox ID="txtApellido" runat="server" MaxLength="50" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtApellido"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Usuario"
                    ErrorMessage="El apellido es obligatorio." /></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Usuario"
                    ErrorMessage="El mail es obligatorio." /></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:Label runat="server" AssociatedControlID="ddlNivel">Rol</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:DropDownList ID="ddlNivel" runat="server" /></div>
            <div class="col-4"></div>
        </div>

        <div class="row border border-1">
            <div class="col-4"></div>
            <div class="col-4">
                <asp:CheckBox ID="chkActivo" runat="server" Checked="true" />
                <asp:Label runat="server" AssociatedControlID="chkActivo">Activo</asp:Label></div>
            <div class="col-4"></div>
        </div>

        <p>
            Al crear un usuario se genera una contraseña temporal y se le envía por mail.
        </p>

        <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
            OnClick="btnGuardar_Click" ValidationGroup="Usuario" />
        <asp:Button ID="btnCancelar" runat="server" Text="Cancelar"
            OnClick="btnCancelar_Click" CausesValidation="false" />
    </asp:Panel>

    <asp:GridView ID="gvUsuarios" runat="server"
        AutoGenerateColumns="false" DataKeyNames="IdUsuario" GridLines="None"
        OnRowCommand="gvUsuarios_RowCommand" EmptyDataText="No hay usuarios cargados.">
        <Columns>
            <asp:BoundField DataField="Apellido" HeaderText="Apellido" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="Email" HeaderText="Mail" />
            <asp:BoundField DataField="NombreNivel" HeaderText="Rol" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate>
                    <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
                        CommandName="Editar" CommandArgument='<%# Eval("IdUsuario") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                    <asp:LinkButton runat="server"
                        CommandName="Blanquear" CommandArgument='<%# Eval("IdUsuario") %>'
                        CausesValidation="false"
                        OnClientClick="return confirm('¿Restablecer la contraseña de este usuario?');">Blanquear clave</asp:LinkButton>
                    <asp:LinkButton runat="server"
                        CommandName="Desactivar" CommandArgument='<%# Eval("IdUsuario") %>'
                        CausesValidation="false" Visible='<%# (bool)Eval("Activo") %>'
                        OnClientClick="return confirm('¿Desactivar este usuario?');">Desactivar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>
</asp:Content>
