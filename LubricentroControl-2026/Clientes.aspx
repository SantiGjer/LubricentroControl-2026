<%@ Page Title="Clientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Clientes.aspx.cs" Inherits="LubricentroControl_2026.Clientes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Clientes</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:HiddenField ID="hdnVieneDeVehiculo" runat="server" />
    <asp:HiddenField ID="hdnVhIdVehiculo" runat="server" />
    <asp:HiddenField ID="hdnVhActivo" runat="server" />
    <asp:HiddenField ID="hdnVhIdCliente" runat="server" />
    <asp:HiddenField ID="hdnVhPatente" runat="server" />
    <asp:HiddenField ID="hdnVhMarca" runat="server" />
    <asp:HiddenField ID="hdnVhModelo" runat="server" />
    <asp:HiddenField ID="hdnVhAnio" runat="server" />
    <asp:HiddenField ID="hdnVhTipoCombustible" runat="server" />

    <asp:Panel ID="pnlVieneDeVehiculo" runat="server" Visible="false" role="alert" CssClass="alert alert-info">
        Estás creando un cliente para asignarlo a un vehículo nuevo.
        <asp:Button ID="btnVolverAVehiculos" runat="server" Text="Volver a Vehículos sin crear"
            OnClick="btnVolverAVehiculos_Click" CausesValidation="false" />
    </asp:Panel>

    <asp:HiddenField ID="hdnVieneDeOrden" runat="server" />
    <asp:HiddenField ID="hdnOrKilometraje" runat="server" />
    <asp:HiddenField ID="hdnOrObservaciones" runat="server" />
    <asp:HiddenField ID="hdnOrIdTurno" runat="server" />

    <asp:Panel ID="pnlVieneDeOrden" runat="server" Visible="false" role="alert" CssClass="alert alert-info">
        Estás creando un cliente para una orden de trabajo nueva.
        <asp:Button ID="btnVolverAOrdenes" runat="server" Text="Volver a Órdenes sin crear"
            OnClick="btnVolverAOrdenes_Click" CausesValidation="false" />
    </asp:Panel>

    <div class="row">
        <div class="col-7">
            <div class="row border border-1">
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por nombre, apellido o DNI</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" />
                    <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                        OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
                    <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label></div>
            </div>

            <asp:GridView ID="gvClientes" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdCliente" GridLines="None"
                OnRowCommand="gvClientes_RowCommand" EmptyDataText="No hay clientes que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="Apellido" HeaderText="Apellido" />
                    <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                    <asp:BoundField DataField="Dni" HeaderText="DNI" />
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
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdCliente") %>'
                                CausesValidation="false">Seleccionar</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo cliente" /></h2>
            <asp:HiddenField ID="hdnIdCliente" runat="server" />
            <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtNombre" runat="server" MaxLength="50" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                        ErrorMessage="El nombre es obligatorio." /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtApellido">Apellido</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtApellido" runat="server" MaxLength="50" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtApellido"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                        ErrorMessage="El apellido es obligatorio." /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtDni">DNI</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtDni" runat="server" MaxLength="8" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtDni"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                        ErrorMessage="El DNI es obligatorio." />
                    <asp:CustomValidator runat="server" ControlToValidate="txtDni"
                        OnServerValidate="valDni_ServerValidate"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                        ErrorMessage="El DNI debe tener 7 u 8 números, sin puntos." /></div>
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
                    <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" /></div>
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
                OnClick="btnGuardar_Click" ValidationGroup="Cliente" />
            <asp:Button ID="btnNuevo" runat="server" Text="Nuevo cliente"
                OnClick="btnNuevo_Click" CausesValidation="false" />
            <asp:Button ID="btnBorrar" runat="server" Text="Borrar" Visible="false"
                OnClick="btnBorrar_Click" CausesValidation="false"
                OnClientClick="return confirm('¿Borrar este cliente?');" />
        </div>
    </div>
</asp:Content>
