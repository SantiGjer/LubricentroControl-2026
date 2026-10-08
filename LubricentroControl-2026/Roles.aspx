<%@ Page Title="Roles y permisos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Roles.aspx.cs" Inherits="LubricentroControl_2026.Roles" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Roles y permisos</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <p class="texto-nota"><small>Cada rol tiene, por pantalla, <b>sin acceso</b> (no la ve ni en el menú), <b>consulta</b>
        (la ve pero no puede cambiar nada) o <b>completo</b>. Admin tiene siempre acceso completo a todo, y el rol
        Lectura es el que reciben las cuentas creadas desde el registro público: ninguno de los dos se puede borrar.</small></p>

    <div class="barra-herramientas">
        <input type="search" id="filtroRoles" class="filtro-tabla-texto"
            placeholder="Buscar un rol" aria-label="Buscar roles" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo rol"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <asp:GridView ID="gvRoles" runat="server" CssClass="tabla-abm" data-filtro="filtroRoles"
        AutoGenerateColumns="false" DataKeyNames="IdNivel" GridLines="None"
        OnRowCommand="gvRoles_RowCommand" EmptyDataText="No hay roles cargados.">
        <Columns>
            <asp:BoundField DataField="Nombre" HeaderText="Rol" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="CantidadUsuarios" HeaderText="Usuarios" />
            <asp:BoundField DataField="CantidadPantallas" HeaderText="Pantallas con acceso" />
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server" CssClass="accion-ver"
                        CommandName="Ver" CommandArgument='<%# Eval("IdNivel") %>'
                        CausesValidation="false">Ver permisos</asp:LinkButton>
                    <asp:LinkButton runat="server" Visible='<%# PuedeEscribir && !(bool)Eval("EsAdmin") %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdNivel") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                    <asp:LinkButton runat="server" Visible='<%# PuedeEscribir && (bool)Eval("SePuedeBorrar") %>'
                        CommandName="Borrar" CommandArgument='<%# Eval("IdNivel") %>'
                        CausesValidation="false"
                        OnClientClick="return confirm('¿Borrar este rol? Se pierden sus permisos.');">Borrar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Alta, edición o vista de un rol: el nombre y la matriz de permisos por pantalla. Se guarda
         todo junto (NivelDAL.Guardar). --%>
    <div class="modal fade" id="modalRol" tabindex="-1" aria-labelledby="tituloModalRol"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg modal-dialog-scrollable">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalRol">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo rol" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlAvisoRol" runat="server" Visible="false" CssClass="alert alert-secondary" role="alert">
                        <asp:Literal ID="litAvisoRol" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdNivel" runat="server" />

                    <div class="row campos-formulario mb-3">
                        <div class="col-md-8 campo">
                            <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre del rol</asp:Label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="50" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Rol"
                                ErrorMessage="El nombre del rol es obligatorio." />
                        </div>
                    </div>

                    <asp:Panel ID="pnlAccesosRapidos" runat="server" CssClass="accesos-rapidos">
                        Marcar todas las pantallas como:
                        <button type="button" class="opcion" onclick="Lubricentro.marcarPermisos('Sin acceso');">Sin acceso</button>
                        <button type="button" class="opcion" onclick="Lubricentro.marcarPermisos('Consulta');">Consulta</button>
                        <button type="button" class="opcion" onclick="Lubricentro.marcarPermisos('Completo');">Completo</button>
                    </asp:Panel>

                    <table class="tabla-abm tabla-compacta matriz-permisos" data-estatica="true">
                        <thead>
                            <tr><th>Pantalla</th><th class="sin-orden">Acceso</th></tr>
                        </thead>
                        <tbody>
                            <asp:Repeater ID="rptPermisos" runat="server" OnItemDataBound="rptPermisos_ItemDataBound">
                                <ItemTemplate>
                                    <%# EncabezadoDeGrupo((BIZ.Modelo.PermisoPantalla)Container.DataItem) %>
                                    <tr>
                                        <td>
                                            <%#: Eval("Pantalla") %>
                                            <%# (bool)Eval("EsInicio") ? "<span class=\"etiqueta etiqueta-gris\">siempre</span>" : "" %>
                                        </td>
                                        <td>
                                            <asp:HiddenField ID="hdnIdMenu" runat="server" Value='<%# Eval("IdMenu") %>' />
                                            <asp:RadioButtonList ID="rblAcceso" runat="server" CssClass="opciones-radio acceso-pantalla"
                                                RepeatLayout="Flow" RepeatDirection="Horizontal" />
                                        </td>
                                    </tr>
                                </ItemTemplate>
                            </asp:Repeater>
                        </tbody>
                    </table>
                </div>

                <div class="modal-footer">
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">
                        <asp:Literal ID="litBotonCerrar" runat="server" Text="Cancelar" /></button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Rol" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
