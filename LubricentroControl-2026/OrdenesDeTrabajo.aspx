<%@ Page Title="Órdenes de trabajo" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="OrdenesDeTrabajo.aspx.cs" Inherits="LubricentroControl_2026.OrdenesDeTrabajo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Órdenes de trabajo</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroOrdenes" class="filtro-tabla-texto"
            placeholder="Filtrar por cliente, DNI, patente o fecha" aria-label="Filtrar órdenes" />
        <span class="opcion-barra">
            <asp:Label runat="server" AssociatedControlID="ddlFiltroEstado">Estado</asp:Label>
            <asp:DropDownList ID="ddlFiltroEstado" runat="server" AutoPostBack="true"
                OnSelectedIndexChanged="ddlFiltroEstado_SelectedIndexChanged" />
        </span>
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nueva orden"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <asp:GridView ID="gvOrdenes" runat="server" CssClass="tabla-abm" data-filtro="filtroOrdenes"
        AutoGenerateColumns="false" DataKeyNames="IdOrden" GridLines="None"
        OnRowCommand="gvOrdenes_RowCommand" OnRowDataBound="gvOrdenes_RowDataBound"
        EmptyDataText="No hay órdenes para mostrar.">
        <Columns>
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
            <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
                        CommandName="Editar" CommandArgument='<%# Eval("IdOrden") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Alta y edición de la orden en un modal grande: arriba la cabecera, y editando una orden
         ya creada, abajo el detalle de servicios e insumos. Todo el cuerpo va en un UpdatePanel:
         elegir el cliente o agregar/quitar líneas no cierra el modal. Los botones del pie
         (Guardar, Cerrar y Cancelar orden) hacen postback completo para refrescar la lista. --%>
    <div class="modal fade" id="modalOrden" tabindex="-1" aria-labelledby="tituloModalOrden"
        aria-hidden="true" data-bs-backdrop="static">
        <asp:Panel ID="pnlDialogo" runat="server" CssClass="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalOrden">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nueva orden" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                <asp:UpdatePanel ID="upnlOrden" runat="server">
                    <ContentTemplate>
                        <asp:Panel ID="pnlMensajeFormulario" runat="server" Visible="false" role="alert">
                            <asp:Literal ID="litMensajeFormulario" runat="server" />
                        </asp:Panel>

                        <asp:HiddenField ID="hdnIdOrden" runat="server" />

                        <%-- Cliente/vehículo/turno quedan fijos una vez creada la orden (no se pueden
                             reasignar): el selector y los desplegables solo están mientras se arma una
                             orden nueva. Editando una ya creada se muestran como texto fijo. --%>
                        <asp:Panel ID="pnlSeleccionNueva" runat="server" CssClass="row campos-formulario">
                            <div class="col-12 campo">
                                <asp:Label runat="server" AssociatedControlID="txtCliente">Cliente</asp:Label>
                                <div class="selector-con-boton">
                                    <div class="selector-busqueda" data-postback="true" data-opciones="<%: OpcionesClientes %>">
                                        <asp:TextBox ID="txtCliente" runat="server" CssClass="selector-texto" autocomplete="off"
                                            placeholder="Elegí el cliente o buscalo por nombre o DNI" />
                                        <button type="button" class="selector-boton" tabindex="-1" aria-label="Ver los clientes"></button>
                                        <asp:HiddenField ID="hdnIdCliente" runat="server" OnValueChanged="hdnIdCliente_ValueChanged" />
                                    </div>
                                    <asp:Button ID="btnNuevoCliente" runat="server" CssClass="boton-gris" Text="Nuevo cliente"
                                        OnClick="btnNuevoCliente_Click" CausesValidation="false" />
                                </div>
                                <asp:CustomValidator runat="server" OnServerValidate="valCliente_ServerValidate"
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Orden"
                                    ErrorMessage="Seleccioná el cliente de la orden." />
                            </div>

                            <div class="col-md-6 campo">
                                <asp:Label runat="server" AssociatedControlID="ddlVehiculo">Vehículo</asp:Label>
                                <div class="selector-con-boton">
                                    <asp:DropDownList ID="ddlVehiculo" runat="server" CssClass="flex-grow-1" />
                                    <asp:Button ID="btnNuevoVehiculo" runat="server" CssClass="boton-gris" Text="Nuevo vehículo"
                                        OnClick="btnNuevoVehiculo_Click" CausesValidation="false" />
                                </div>
                                <asp:CustomValidator runat="server" OnServerValidate="valVehiculo_ServerValidate"
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Orden"
                                    ErrorMessage="Seleccioná el vehículo de la orden." />
                            </div>

                            <div class="col-md-6 campo">
                                <asp:Label runat="server" AssociatedControlID="ddlTurno">Turno (opcional)</asp:Label>
                                <asp:DropDownList ID="ddlTurno" runat="server" />
                            </div>
                        </asp:Panel>

                        <asp:Panel ID="pnlSeleccionFija" runat="server" Visible="false" CssClass="row campos-formulario">
                            <div class="col-md-4 campo">
                                <label>Cliente</label>
                                <div class="valor-fijo"><asp:Literal ID="litClienteInfo" runat="server" /></div>
                            </div>
                            <div class="col-md-4 campo">
                                <label>Vehículo</label>
                                <div class="valor-fijo"><asp:Literal ID="litVehiculoInfo" runat="server" /></div>
                            </div>
                            <div class="col-md-4 campo">
                                <label>Turno</label>
                                <div class="valor-fijo"><asp:Literal ID="litTurnoInfo" runat="server" /></div>
                            </div>
                        </asp:Panel>

                        <div class="row campos-formulario mt-0">
                            <div class="col-md-4 campo">
                                <asp:Label runat="server" AssociatedControlID="txtKilometraje">Kilometraje (opcional)</asp:Label>
                                <asp:TextBox ID="txtKilometraje" runat="server" MaxLength="9" />
                                <asp:CompareValidator runat="server" ControlToValidate="txtKilometraje"
                                    Operator="DataTypeCheck" Type="Integer"
                                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Orden"
                                    ErrorMessage="El kilometraje debe ser un número entero." />
                            </div>

                            <asp:Panel ID="pnlEstado" runat="server" Visible="false" CssClass="col-md-4 campo">
                                <asp:Label runat="server" AssociatedControlID="ddlEstado">Estado</asp:Label>
                                <asp:DropDownList ID="ddlEstado" runat="server" />
                                <asp:Label ID="litEstadoActual" runat="server" Visible="false" CssClass="valor-fijo d-block" />
                            </asp:Panel>

                            <div class="col-12 campo">
                                <asp:Label runat="server" AssociatedControlID="txtObservaciones">Observaciones</asp:Label>
                                <asp:TextBox ID="txtObservaciones" runat="server" MaxLength="500" TextMode="MultiLine" Rows="2" />
                            </div>
                        </div>

                        <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                            <div class="separador-modal"></div>
                            <div class="row g-3">
                                <div class="col-lg-6">
                                    <div class="seccion-modal">
                                        <h3>Servicios</h3>

                                        <asp:Panel ID="pnlAgregarServicio" runat="server" DefaultButton="btnAgregarServicio" CssClass="mb-3">
                                            <div class="selector-con-boton">
                                                <asp:DropDownList ID="ddlServicio" runat="server" CssClass="flex-grow-1" />
                                                <asp:TextBox ID="txtCantidadServicio" runat="server" MaxLength="6" Text="1" Width="70px"
                                                    aria-label="Cantidad" />
                                                <asp:Button ID="btnAgregarServicio" runat="server" CssClass="boton-rojo" Text="Agregar"
                                                    OnClick="btnAgregarServicio_Click" ValidationGroup="AgregarServicio" />
                                            </div>
                                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCantidadServicio"
                                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarServicio"
                                                ErrorMessage="La cantidad es obligatoria." />
                                            <asp:CompareValidator runat="server" ControlToValidate="txtCantidadServicio"
                                                Operator="GreaterThan" ValueToCompare="0" Type="Currency"
                                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarServicio"
                                                ErrorMessage="La cantidad debe ser mayor a cero." />
                                        </asp:Panel>

                                        <asp:GridView ID="gvServicios" runat="server" data-sin-filtro="true"
                                            CssClass="tabla-abm tabla-compacta"
                                            AutoGenerateColumns="false" DataKeyNames="IdDetalle" GridLines="None"
                                            OnRowCommand="gvServicios_RowCommand"
                                            EmptyDataText="Todavía no se cargaron servicios.">
                                            <Columns>
                                                <asp:BoundField DataField="NombreServicio" HeaderText="Servicio" />
                                                <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                                                <asp:BoundField DataField="PrecioAplicado" HeaderText="Precio" DataFormatString="{0:N2}" />
                                                <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                                                    <ItemTemplate>
                                                        <asp:LinkButton runat="server"
                                                            CommandName="Quitar" CommandArgument='<%# Eval("IdDetalle") %>'
                                                            CausesValidation="false">Quitar</asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>

                                <div class="col-lg-6">
                                    <div class="seccion-modal">
                                        <h3>Insumos</h3>

                                        <asp:Panel ID="pnlAgregarInsumo" runat="server" DefaultButton="btnAgregarInsumo" CssClass="mb-3">
                                            <div class="selector-con-boton">
                                                <asp:DropDownList ID="ddlInsumo" runat="server" CssClass="flex-grow-1" />
                                                <asp:TextBox ID="txtCantidadInsumo" runat="server" MaxLength="6" Text="1" Width="70px"
                                                    aria-label="Cantidad" />
                                                <asp:Button ID="btnAgregarInsumo" runat="server" CssClass="boton-rojo" Text="Agregar"
                                                    OnClick="btnAgregarInsumo_Click" ValidationGroup="AgregarInsumo" />
                                            </div>
                                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCantidadInsumo"
                                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarInsumo"
                                                ErrorMessage="La cantidad es obligatoria." />
                                            <asp:CompareValidator runat="server" ControlToValidate="txtCantidadInsumo"
                                                Operator="GreaterThan" ValueToCompare="0" Type="Currency"
                                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="AgregarInsumo"
                                                ErrorMessage="La cantidad debe ser mayor a cero." />
                                        </asp:Panel>

                                        <asp:GridView ID="gvInsumosOrden" runat="server" data-sin-filtro="true"
                                            CssClass="tabla-abm tabla-compacta"
                                            AutoGenerateColumns="false" DataKeyNames="IdDetalle" GridLines="None"
                                            OnRowCommand="gvInsumosOrden_RowCommand"
                                            EmptyDataText="Todavía no se cargaron insumos.">
                                            <Columns>
                                                <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                                                <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                                                <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio" DataFormatString="{0:N2}" />
                                                <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                                                    <ItemTemplate>
                                                        <asp:LinkButton runat="server"
                                                            CommandName="Quitar" CommandArgument='<%# Eval("IdDetalle") %>'
                                                            CausesValidation="false"
                                                            OnClientClick="return confirm('¿Quitar este insumo de la orden? Se repone su stock.');">Quitar</asp:LinkButton>
                                                    </ItemTemplate>
                                                </asp:TemplateField>
                                            </Columns>
                                        </asp:GridView>
                                    </div>
                                </div>
                            </div>
                        </asp:Panel>
                    </ContentTemplate>
                    <Triggers>
                        <%-- Postback completo: "Nuevo cliente"/"Nuevo vehículo" hacen Response.Redirect,
                             que no funciona como trigger async normal dentro del UpdatePanel. --%>
                        <asp:PostBackTrigger ControlID="btnNuevoCliente" />
                        <asp:PostBackTrigger ControlID="btnNuevoVehiculo" />
                    </Triggers>
                </asp:UpdatePanel>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnCancelarOrden" runat="server" CssClass="boton-borde-rojo" Text="Cancelar orden" Visible="false"
                            OnClick="btnCancelarOrden_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Cancelar esta orden? Se repondrá el stock de los insumos cargados.');" />
                        <asp:Button ID="btnCerrarOrden" runat="server" CssClass="boton-gris" Text="Cerrar orden" Visible="false"
                            OnClick="btnCerrarOrden_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Cerrar esta orden? Se generará la venta correspondiente.');" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Volver a la lista</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Orden" />
                </div>
            </asp:Panel>
        </asp:Panel>
    </div>

    </div>
</asp:Content>
