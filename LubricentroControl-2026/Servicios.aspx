<%@ Page Title="Servicios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Servicios.aspx.cs" Inherits="LubricentroControl_2026.Servicios" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Servicios</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroServicios" class="filtro-tabla-texto"
            placeholder="Filtrar por nombre o descripción" aria-label="Filtrar servicios" />
        <span class="opcion-barra">
            <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
            <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label>
        </span>
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo servicio"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <asp:GridView ID="gvServicios" runat="server" CssClass="tabla-abm" data-filtro="filtroServicios"
        AutoGenerateColumns="false" DataKeyNames="IdServicio" GridLines="None"
        OnRowCommand="gvServicios_RowCommand" EmptyDataText="No hay servicios cargados.">
        <Columns>
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
            <asp:BoundField DataField="PrecioBase" HeaderText="Precio base" DataFormatString="{0:N2}" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate>
                    <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
                        CommandName="Editar" CommandArgument='<%# Eval("IdServicio") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalServicio" tabindex="-1" aria-labelledby="tituloModalServicio"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalServicio">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo servicio" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdServicio" runat="server" />
                    <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                    <div class="row campos-formulario">
                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Servicio"
                                ErrorMessage="El nombre es obligatorio." />
                        </div>

                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtDescripcion">Descripción</asp:Label>
                            <asp:TextBox ID="txtDescripcion" runat="server" MaxLength="300" TextMode="MultiLine" Rows="3" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtPrecioBase">Precio base</asp:Label>
                            <asp:TextBox ID="txtPrecioBase" runat="server" MaxLength="12" />
                            <asp:CompareValidator runat="server" ControlToValidate="txtPrecioBase"
                                Operator="DataTypeCheck" Type="Currency"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Servicio"
                                ErrorMessage="El precio base debe ser un número." />
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnBorrar" runat="server" CssClass="boton-borde-rojo" Text="Borrar" Visible="false"
                            OnClick="btnBorrar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Borrar este servicio?');" />
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Servicio" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
