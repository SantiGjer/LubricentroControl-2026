<%@ Page Title="Proveedores" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Proveedores.aspx.cs" Inherits="LubricentroControl_2026.Proveedores" %>
<%@ Import Namespace="BIZ.Modelo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Proveedores</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroProveedores" class="filtro-tabla-texto"
            placeholder="Buscar por razón social, CUIT, teléfono o mail" aria-label="Buscar proveedores" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo proveedor"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesProveedores">
        <div class="grupo-opciones" data-columna="Estado" data-inicial="Activo">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="Activo">Activos</button>
            <button type="button" class="opcion" data-valor="Inactivo">Inactivos</button>
            <button type="button" class="opcion" data-valor="">Todos</button>
        </div>
    </div>

    <asp:GridView ID="gvProveedores" runat="server" CssClass="tabla-abm" data-filtro="filtroProveedores"
        data-opciones-tabla="opcionesProveedores" data-titulo-detalle="Proveedor"
        AutoGenerateColumns="false" DataKeyNames="IdProveedor" GridLines="None"
        OnRowCommand="gvProveedores_RowCommand" EmptyDataText="No hay proveedores cargados.">
        <Columns>
            <asp:BoundField DataField="RazonSocial" HeaderText="Razón social" ItemStyle-CssClass="celda-titulo" />
            <asp:TemplateField HeaderText="CUIT">
                <ItemTemplate><%# Proveedor.FormatearCuit(Eval("Cuit").ToString()) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Telefono" HeaderText="Teléfono" />
            <asp:BoundField DataField="Email" HeaderText="Mail" />
            <asp:BoundField DataField="Direccion" HeaderText="Dirección" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <a href="#" class="accion-ver" data-ver-detalle>Ver</a>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdProveedor") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalProveedor" tabindex="-1" aria-labelledby="tituloModalProveedor"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalProveedor">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo proveedor" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdProveedor" runat="server" />
                    <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                    <div class="row campos-formulario">
                        <div class="col-md-8 campo">
                            <asp:Label runat="server" AssociatedControlID="txtRazonSocial">Razón social</asp:Label>
                            <asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="150" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtRazonSocial"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="La razón social es obligatoria." />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="txtCuit">CUIT</asp:Label>
                            <asp:TextBox ID="txtCuit" runat="server" MaxLength="13" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCuit"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El CUIT es obligatorio." />
                            <asp:CustomValidator runat="server" ControlToValidate="txtCuit"
                                OnServerValidate="valCuit_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El CUIT debe tener 11 números, con o sin guiones." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtTelefono">Teléfono</asp:Label>
                            <asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" TextMode="Phone" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtTelefono"
                                OnServerValidate="valTelefono_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El teléfono solo puede tener números, espacios, guiones o paréntesis (entre 6 y 15 números)." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtEmail"
                                OnServerValidate="valEmail_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El mail no tiene un formato válido." />
                        </div>

                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtDireccion">Dirección</asp:Label>
                            <asp:TextBox ID="txtDireccion" runat="server" MaxLength="200" />
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnBorrar" runat="server" CssClass="boton-borde-rojo" Text="Borrar" Visible="false"
                            OnClick="btnBorrar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Borrar este proveedor?');" />
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Proveedor" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
