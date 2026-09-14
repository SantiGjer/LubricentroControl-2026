<%@ Page Title="Turnos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Turnos.aspx.cs" Inherits="LubricentroControl_2026.Turnos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Turnos</h1>

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
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por nombre, apellido o DNI del cliente</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="ddlFiltroEstado">Estado</asp:Label>
                    <asp:DropDownList ID="ddlFiltroEstado" runat="server" AutoPostBack="true"
                        OnSelectedIndexChanged="ddlFiltroEstado_SelectedIndexChanged" />
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" /></div>
            </div>

            <asp:GridView ID="gvTurnos" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdTurno" GridLines="None"
                OnRowCommand="gvTurnos_RowCommand" EmptyDataText="No hay turnos que coincidan con la búsqueda.">
                <Columns>
                    <asp:TemplateField HeaderText="Fecha y hora">
                        <ItemTemplate>
                            <%# Eval("FechaHoraAsignada", "{0:dd/MM/yyyy HH:mm}") %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
                    <asp:TemplateField HeaderText="Vehículo">
                        <ItemTemplate>
                            <%# Eval("Patente") ?? "—" %>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:BoundField DataField="Estado" HeaderText="Estado" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdTurno") %>'
                                CausesValidation="false">Seleccionar</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo turno" /></h2>
            <asp:HiddenField ID="hdnIdTurno" runat="server" />

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtBuscarCliente">Cliente</asp:Label></div>
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
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Turno"
                                ErrorMessage="Seleccioná el cliente del turno." />

                            <br />
                            <asp:Label runat="server" AssociatedControlID="ddlVehiculo">Vehículo (opcional)</asp:Label>
                            <br />
                            <asp:DropDownList ID="ddlVehiculo" runat="server" />
                        </ContentTemplate>
                    </asp:UpdatePanel></div>
            </div>

            <div class="row border border-1">
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtFecha">Fecha</asp:Label>
                    <br />
                    <asp:TextBox ID="txtFecha" runat="server" TextMode="Date" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFecha"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Turno"
                        ErrorMessage="La fecha es obligatoria." /></div>
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="txtHora">Hora</asp:Label>
                    <br />
                    <asp:TextBox ID="txtHora" runat="server" TextMode="Time" />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtHora"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Turno"
                        ErrorMessage="La hora es obligatoria." /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="ddlEstado">Estado</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:DropDownList ID="ddlEstado" runat="server" /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtObservaciones">Observaciones</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtObservaciones" runat="server" MaxLength="500" TextMode="MultiLine" Rows="3" /></div>
            </div>

            <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                OnClick="btnGuardar_Click" ValidationGroup="Turno" />
            <asp:Button ID="btnNuevo" runat="server" Text="Nuevo turno"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </div>
    </div>
</asp:Content>
