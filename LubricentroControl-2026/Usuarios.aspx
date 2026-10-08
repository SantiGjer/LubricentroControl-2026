<%@ Page Title="Usuarios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Usuarios.aspx.cs" Inherits="LubricentroControl_2026.Usuarios" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Usuarios</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroUsuarios" class="filtro-tabla-texto"
            placeholder="Buscar por nombre, mail o rol" aria-label="Buscar usuarios" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo usuario"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesUsuarios">
        <div class="grupo-opciones" data-columna="Estado" data-inicial="Activo">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="Activo">Activos</button>
            <button type="button" class="opcion" data-valor="Inactivo">Inactivos</button>
            <button type="button" class="opcion" data-valor="">Todos</button>
        </div>
        <div class="grupo-opciones" data-columna="Rol">
            <span class="titulo-opciones">Rol</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <asp:Repeater ID="rptRolesFiltro" runat="server">
                <ItemTemplate><button type="button" class="opcion" data-valor="<%#: Eval("Nombre") %>"><%#: Eval("Nombre") %></button></ItemTemplate>
            </asp:Repeater>
        </div>
    </div>

    <asp:GridView ID="gvUsuarios" runat="server" CssClass="tabla-abm" data-filtro="filtroUsuarios"
        data-opciones-tabla="opcionesUsuarios" data-titulo-detalle="Usuario"
        AutoGenerateColumns="false" DataKeyNames="IdUsuario" GridLines="None"
        OnRowCommand="gvUsuarios_RowCommand" EmptyDataText="No hay usuarios cargados.">
        <Columns>
            <asp:BoundField DataField="Apellido" HeaderText="Apellido" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="Email" HeaderText="Mail" />
            <asp:BoundField DataField="NombreNivel" HeaderText="Rol" />
            <asp:BoundField DataField="FechaAlta" HeaderText="Alta" DataFormatString="{0:dd/MM/yyyy}"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <a href="#" class="accion-ver" data-ver-detalle>Ver</a>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdUsuario") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                    <asp:LinkButton runat="server" Visible='<%# PuedeEscribir %>'
                        CommandName="Blanquear" CommandArgument='<%# Eval("IdUsuario") %>'
                        CausesValidation="false"
                        OnClientClick="return confirm('¿Restablecer la contraseña de este usuario?');">Blanquear clave</asp:LinkButton>
                    <asp:LinkButton runat="server"
                        CommandName="Desactivar" CommandArgument='<%# Eval("IdUsuario") %>'
                        CausesValidation="false" Visible='<%# PuedeEscribir && (bool)Eval("Activo") %>'
                        OnClientClick="return confirm('¿Desactivar este usuario?');">Desactivar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalUsuario" tabindex="-1" aria-labelledby="tituloModalUsuario"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalUsuario">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo usuario" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdUsuario" runat="server" />

                    <div class="row campos-formulario">
                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="50" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Usuario"
                                ErrorMessage="El nombre es obligatorio." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtApellido">Apellido</asp:Label>
                            <asp:TextBox ID="txtApellido" runat="server" MaxLength="50" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtApellido"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Usuario"
                                ErrorMessage="El apellido es obligatorio." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtEmail"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Usuario"
                                ErrorMessage="El mail es obligatorio." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlNivel">Rol</asp:Label>
                            <asp:DropDownList ID="ddlNivel" runat="server" />
                        </div>

                        <div class="col-12 campo">
                            <asp:CheckBox ID="chkActivo" runat="server" Checked="true" />
                            <asp:Label runat="server" AssociatedControlID="chkActivo" CssClass="etiqueta-inline">Activo</asp:Label>
                            <p class="texto-ayuda">Al crear un usuario se genera una contraseña temporal y se le envía por mail.</p>
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Usuario" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
