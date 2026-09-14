<%@ Page Title="Órdenes de trabajo" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="OrdenesDeTrabajo.aspx.cs" Inherits="LubricentroControl_2026.OrdenesDeTrabajo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Órdenes de trabajo</h1>

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
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por cliente, DNI o patente</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Label runat="server" AssociatedControlID="ddlFiltroEstado">Estado</asp:Label>
                    <asp:DropDownList ID="ddlFiltroEstado" runat="server" AutoPostBack="true"
                        OnSelectedIndexChanged="ddlFiltroEstado_SelectedIndexChanged" />
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" /></div>
            </div>

            <asp:GridView ID="gvOrdenes" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdOrden" GridLines="None"
                OnRowCommand="gvOrdenes_RowCommand" EmptyDataText="No hay órdenes que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                    <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
                    <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
                    <asp:BoundField DataField="Estado" HeaderText="Estado" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdOrden") %>'
                                CausesValidation="false">Seleccionar</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva orden" /></h2>
            <asp:HiddenField ID="hdnIdOrden" runat="server" />

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

                            <%-- Cliente/vehículo/turno quedan fijos una vez creada la orden (no
                                 se pueden reasignar), así que el buscador y los desplegables solo
                                 hacen falta mientras se arma una orden nueva. Editando una ya
                                 creada se muestran como texto de solo lectura más abajo. --%>
                            <asp:Panel ID="pnlSeleccionNueva" runat="server">
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
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Orden"
                                    ErrorMessage="Seleccioná el cliente de la orden." />

                                <br />
                                <asp:Label runat="server" AssociatedControlID="ddlVehiculo">Vehículo</asp:Label>
                                <asp:Button ID="btnNuevoVehiculo" runat="server" Text="Nuevo vehículo"
                                    OnClick="btnNuevoVehiculo_Click" CausesValidation="false" />
                                <br />
                                <asp:DropDownList ID="ddlVehiculo" runat="server" />
                                <asp:CustomValidator runat="server" OnServerValidate="valVehiculo_ServerValidate"
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Orden"
                                    ErrorMessage="Seleccioná el vehículo de la orden." />

                                <br />
                                <asp:Label runat="server" AssociatedControlID="ddlTurno">Turno (opcional)</asp:Label>
                                <br />
                                <asp:DropDownList ID="ddlTurno" runat="server" />
                            </asp:Panel>

                            <asp:Panel ID="pnlSeleccionFija" runat="server" Visible="false">
                                <br />
                                <asp:Label ID="litVehiculoInfo" runat="server" /><br />
                                <asp:Label ID="litTurnoInfo" runat="server" />
                            </asp:Panel>
                        </ContentTemplate>
                        <Triggers>
                            <%-- Postback completo: btnNuevoCliente/btnNuevoVehiculo hacen
                                 Response.Redirect, que no funciona como trigger async normal
                                 dentro del UpdatePanel (ver CLAUDE.md). --%>
                            <asp:PostBackTrigger ControlID="btnNuevoCliente" />
                            <asp:PostBackTrigger ControlID="btnNuevoVehiculo" />
                        </Triggers>
                    </asp:UpdatePanel></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtKilometraje">Kilometraje (opcional)</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtKilometraje" runat="server" MaxLength="9" />
                    <asp:CompareValidator runat="server" ControlToValidate="txtKilometraje"
                        Operator="DataTypeCheck" Type="Integer"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Orden"
                        ErrorMessage="El kilometraje debe ser un número entero." /></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:Label runat="server" AssociatedControlID="txtObservaciones">Observaciones</asp:Label></div>
            </div>

            <div class="row border border-1">
                <div class="col-12">
                    <asp:TextBox ID="txtObservaciones" runat="server" MaxLength="500" TextMode="MultiLine" Rows="3" /></div>
            </div>

            <asp:Panel ID="pnlEstado" runat="server" Visible="false">
                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="ddlEstado">Estado</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:DropDownList ID="ddlEstado" runat="server" />
                        <asp:Label ID="litEstadoActual" runat="server" Visible="false" Font-Bold="true" /></div>
                </div>
            </asp:Panel>

            <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                OnClick="btnGuardar_Click" ValidationGroup="Orden" />
            <asp:Button ID="btnNuevo" runat="server" Text="Nueva orden"
                OnClick="btnNuevo_Click" CausesValidation="false" />
            <asp:Button ID="btnCancelarOrden" runat="server" Text="Cancelar orden" Visible="false"
                OnClick="btnCancelarOrden_Click" CausesValidation="false"
                OnClientClick="return confirm('¿Cancelar esta orden? Se repondrá el stock de los insumos cargados.');" />
        </div>
    </div>

    <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
        <hr />
        <div class="row">
            <div class="col-6">
                <h2>Servicios</h2>

                <asp:Panel ID="pnlAgregarServicio" runat="server">
                    <asp:DropDownList ID="ddlServicio" runat="server" />
                    <asp:TextBox ID="txtCantidadServicio" runat="server" MaxLength="6" Text="1" Width="60px" />
                    <asp:Button ID="btnAgregarServicio" runat="server" Text="Agregar"
                        OnClick="btnAgregarServicio_Click" ValidationGroup="AgregarServicio" />
                    <br />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCantidadServicio"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarServicio"
                        ErrorMessage="La cantidad es obligatoria." />
                    <asp:CompareValidator runat="server" ControlToValidate="txtCantidadServicio"
                        Operator="GreaterThan" ValueToCompare="0" Type="Currency"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarServicio"
                        ErrorMessage="La cantidad debe ser mayor a cero." />
                </asp:Panel>

                <asp:GridView ID="gvServicios" runat="server"
                    CssClass="table table-striped table-bordered table-hover"
                    AutoGenerateColumns="false" DataKeyNames="IdDetalle" GridLines="None"
                    OnRowCommand="gvServicios_RowCommand"
                    EmptyDataText="Todavía no se cargaron servicios.">
                    <Columns>
                        <asp:BoundField DataField="NombreServicio" HeaderText="Servicio" />
                        <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="PrecioAplicado" HeaderText="Precio" DataFormatString="{0:N2}" />
                        <asp:TemplateField HeaderText="Acciones">
                            <ItemTemplate>
                                <asp:LinkButton runat="server"
                                    CommandName="Quitar" CommandArgument='<%# Eval("IdDetalle") %>'
                                    CausesValidation="false">Quitar</asp:LinkButton>
                            </ItemTemplate>
                        </asp:TemplateField>
                    </Columns>
                </asp:GridView>
            </div>

            <div class="col-6">
                <h2>Insumos</h2>

                <asp:Panel ID="pnlAgregarInsumo" runat="server">
                    <asp:DropDownList ID="ddlInsumo" runat="server" />
                    <asp:TextBox ID="txtCantidadInsumo" runat="server" MaxLength="6" Text="1" Width="60px" />
                    <asp:Button ID="btnAgregarInsumo" runat="server" Text="Agregar"
                        OnClick="btnAgregarInsumo_Click" ValidationGroup="AgregarInsumo" />
                    <br />
                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCantidadInsumo"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarInsumo"
                        ErrorMessage="La cantidad es obligatoria." />
                    <asp:CompareValidator runat="server" ControlToValidate="txtCantidadInsumo"
                        Operator="GreaterThan" ValueToCompare="0" Type="Currency"
                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarInsumo"
                        ErrorMessage="La cantidad debe ser mayor a cero." />
                </asp:Panel>

                <asp:GridView ID="gvInsumosOrden" runat="server"
                    CssClass="table table-striped table-bordered table-hover"
                    AutoGenerateColumns="false" GridLines="None"
                    EmptyDataText="Todavía no se cargaron insumos.">
                    <Columns>
                        <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                        <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio" DataFormatString="{0:N2}" />
                    </Columns>
                </asp:GridView>
            </div>
        </div>
    </asp:Panel>
</asp:Content>
