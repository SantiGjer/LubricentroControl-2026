<%@ Page Title="Servicios" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Servicios.aspx.cs" Inherits="LubricentroControl_2026.Servicios" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Servicios</h1>

    <asp:Panel ID="pnlSoloLectura" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
        Tu rol tiene acceso de <b>solo consulta</b> a esta pantalla.
    </asp:Panel>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="row">
        <div class="col-7">
            <div class="row border border-1">
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por nombre</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" />
                    <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                        OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
                    <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label></div>
            </div>

            <asp:GridView ID="gvServicios" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdServicio" GridLines="None"
                OnRowCommand="gvServicios_RowCommand" EmptyDataText="No hay servicios que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                    <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                    <asp:BoundField DataField="PrecioBase" HeaderText="Precio base" DataFormatString="{0:N2}" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdServicio") %>'
                                CausesValidation="false">Seleccionar</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <asp:Panel ID="pnlFormulario" runat="server">
                <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo servicio" /></h2>
                <asp:HiddenField ID="hdnIdServicio" runat="server" />
                <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Servicio"
                            ErrorMessage="El nombre es obligatorio." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtDescripcion">Descripción</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtDescripcion" runat="server" MaxLength="300" TextMode="MultiLine" Rows="3" /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtPrecioBase">Precio base</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtPrecioBase" runat="server" MaxLength="12" />
                        <asp:CompareValidator runat="server" ControlToValidate="txtPrecioBase"
                            Operator="DataTypeCheck" Type="Currency"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Servicio"
                            ErrorMessage="El precio base debe ser un número." /></div>
                </div>

                <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                    OnClick="btnGuardar_Click" ValidationGroup="Servicio" />
                <asp:Button ID="btnNuevo" runat="server" Text="Nuevo servicio"
                    OnClick="btnNuevo_Click" CausesValidation="false" />
                <asp:Button ID="btnBorrar" runat="server" Text="Borrar" Visible="false"
                    OnClick="btnBorrar_Click" CausesValidation="false"
                    OnClientClick="return confirm('¿Borrar este servicio?');" />
            </asp:Panel>
        </div>
    </div>
</asp:Content>
