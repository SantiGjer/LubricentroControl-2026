<%@ Page Title="Vehículos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Vehiculos.aspx.cs" Inherits="LubricentroControl_2026.Vehiculos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Vehículos</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="row">
        <div class="col-7">
            <div class="row border border-1">
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por patente, marca o modelo</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" />
                    <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                        OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
                    <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label></div>
            </div>

            <asp:GridView ID="gvVehiculos" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdVehiculo" GridLines="None"
                OnRowCommand="gvVehiculos_RowCommand" EmptyDataText="No hay vehículos que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="Patente" HeaderText="Patente" />
                    <asp:BoundField DataField="Marca" HeaderText="Marca" />
                    <asp:BoundField DataField="Modelo" HeaderText="Modelo" />
                    <asp:BoundField DataField="Anio" HeaderText="Año" />
                    <asp:BoundField DataField="NombreCliente" HeaderText="Dueño" />
                    <asp:TemplateField HeaderText="Estado">
                        <ItemTemplate>
                            <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdVehiculo") %>'
                                CausesValidation="false">Seleccionar</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo vehículo" /></h2>
            <asp:HiddenField ID="hdnIdVehiculo" runat="server" />
            <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtBuscarCliente">Dueño</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:UpdatePanel ID="upnlCliente" runat="server">
                        <ContentTemplate>
                            <asp:HiddenField ID="hdnIdCliente" runat="server" />
                            <asp:Label ID="litClienteSeleccionado" runat="server" Text="(sin seleccionar)" />
                            <br />
                            <asp:TextBox ID="txtBuscarCliente" runat="server" placeholder="Buscar por nombre, apellido o DNI" />
                            <asp:Button ID="btnBuscarCliente" runat="server" Text="Buscar"
                                OnClick="btnBuscarCliente_Click" CausesValidation="false" />
                            <asp:Button ID="btnNuevoCliente" runat="server" Text="Nuevo cliente"
                                OnClick="btnNuevoCliente_Click" CausesValidation="false" />

                            <asp:Panel ID="pnlResultadosCliente" runat="server" Visible="false"
                                style="position:relative; border:1px solid #999; max-height:200px; overflow-y:auto; background:#fff; margin-top:2px;">
                                <asp:Repeater ID="rptResultadosCliente" runat="server" OnItemCommand="rptResultadosCliente_ItemCommand">
                                    <ItemTemplate>
                                        <div style="padding:4px; border-bottom:1px solid #ddd;">
                                            <asp:LinkButton runat="server"
                                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdCliente") %>'
                                                CausesValidation="false">
                                                <%# Eval("NombreCompleto") %> — DNI <%# Eval("Dni") %>
                                            </asp:LinkButton>
                                        </div>
                                    </ItemTemplate>
                                </asp:Repeater>
                            </asp:Panel>

                            <asp:CustomValidator runat="server" OnServerValidate="valCliente_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo"
                                ErrorMessage="Seleccioná el cliente dueño del vehículo." />
                        </ContentTemplate>
                        <Triggers>
                            <%-- Postback completo: btnNuevoCliente hace Response.Redirect, que no
                                 funciona como trigger async normal dentro del UpdatePanel. --%>
                            <asp:PostBackTrigger ControlID="btnNuevoCliente" />
                        </Triggers>
                    </asp:UpdatePanel></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtPatente">Patente</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtPatente" runat="server" MaxLength="7" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPatente"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo"
                        ErrorMessage="La patente es obligatoria." />
                    <asp:CustomValidator runat="server" ControlToValidate="txtPatente"
                        OnServerValidate="valPatente_ServerValidate"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo"
                        ErrorMessage="La patente no tiene un formato válido (ej. ABC123 o AB123CD)." /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtMarca">Marca</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtMarca" runat="server" MaxLength="50" /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtModelo">Modelo</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtModelo" runat="server" MaxLength="50" /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtAnio">Año</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtAnio" runat="server" MaxLength="4" />
                    <asp:CompareValidator runat="server" ControlToValidate="txtAnio"
                        Operator="DataTypeCheck" Type="Integer"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo"
                        ErrorMessage="El año debe ser un número." /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="ddlTipoCombustible">Tipo de combustible</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:DropDownList ID="ddlTipoCombustible" runat="server" /></div>
            </div>

            <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                OnClick="btnGuardar_Click" ValidationGroup="Vehiculo" />
            <asp:Button ID="btnNuevo" runat="server" Text="Nuevo vehículo"
                OnClick="btnNuevo_Click" CausesValidation="false" />
            <asp:Button ID="btnBorrar" runat="server" Text="Borrar" Visible="false"
                OnClick="btnBorrar_Click" CausesValidation="false"
                OnClientClick="return confirm('¿Borrar este vehículo?');" />
        </div>
    </div>
</asp:Content>
