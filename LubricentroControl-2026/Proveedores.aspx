<%@ Page Title="Proveedores" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Proveedores.aspx.cs" Inherits="LubricentroControl_2026.Proveedores" %>
<%@ Import Namespace="BIZ.Modelo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Proveedores</h1>

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
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por razón social o CUIT</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" />
                    <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                        OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
                    <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label></div>
            </div>

            <asp:GridView ID="gvProveedores" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdProveedor" GridLines="None"
                OnRowCommand="gvProveedores_RowCommand" EmptyDataText="No hay proveedores que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="RazonSocial" HeaderText="Razón social" />
                    <asp:TemplateField HeaderText="CUIT">
                        <ItemTemplate>
                            <%# Proveedor.FormatearCuit(Eval("Cuit").ToString()) %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Telefono" HeaderText="Teléfono" />
                    <asp:BoundField DataField="Email" HeaderText="Mail" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdProveedor") %>'
                                CausesValidation="false">Seleccionar</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <asp:Panel ID="pnlFormulario" runat="server">
                <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo proveedor" /></h2>
                <asp:HiddenField ID="hdnIdProveedor" runat="server" />
                <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtRazonSocial">Razón social</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="150" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtRazonSocial"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                            ErrorMessage="La razón social es obligatoria." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtCuit">CUIT</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtCuit" runat="server" MaxLength="13" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCuit"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                            ErrorMessage="El CUIT es obligatorio." />
                        <asp:CustomValidator runat="server" ControlToValidate="txtCuit"
                            OnServerValidate="valCuit_ServerValidate"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                            ErrorMessage="El CUIT debe tener 11 números, con o sin guiones." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtTelefono">Teléfono</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" />
                        <asp:CustomValidator runat="server" ControlToValidate="txtEmail"
                            OnServerValidate="valEmail_ServerValidate"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                            ErrorMessage="El mail no tiene un formato válido." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtDireccion">Dirección</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtDireccion" runat="server" MaxLength="200" /></div>
                </div>

                <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                    OnClick="btnGuardar_Click" ValidationGroup="Proveedor" />
                <asp:Button ID="btnNuevo" runat="server" Text="Nuevo proveedor"
                    OnClick="btnNuevo_Click" CausesValidation="false" />
                <asp:Button ID="btnBorrar" runat="server" Text="Borrar" Visible="false"
                    OnClick="btnBorrar_Click" CausesValidation="false"
                    OnClientClick="return confirm('¿Borrar este proveedor?');" />
            </asp:Panel>
        </div>
    </div>
</asp:Content>
