<%@ Page Title="Órdenes de trabajo" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="OrdenesDeTrabajo.aspx.cs" Inherits="LubricentroControl_2026.OrdenesDeTrabajo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Órdenes de trabajo</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroOrdenes" class="filtro-tabla-texto"
            placeholder="Buscar por número, cliente, documento, patente o fecha" aria-label="Buscar órdenes" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nueva orden"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <%-- Arranca en "Abiertas": lo que se busca casi siempre al entrar son las órdenes nuevas que
         todavía no se empezaron. --%>
    <div class="opciones-tabla" id="opcionesOrdenes">
        <div class="grupo-opciones" data-columna="Estado" data-inicial="Abierta">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="Abierta">Abiertas</button>
            <button type="button" class="opcion" data-valor="En proceso">En proceso</button>
            <button type="button" class="opcion" data-valor="Abierta|En proceso">En curso</button>
            <button type="button" class="opcion" data-valor="Cerrada">Cerradas</button>
            <button type="button" class="opcion" data-valor="Cancelada">Canceladas</button>
            <button type="button" class="opcion" data-valor="">Todas</button>
        </div>
    </div>

    <asp:GridView ID="gvOrdenes" runat="server" CssClass="tabla-abm" data-filtro="filtroOrdenes"
        data-opciones-tabla="opcionesOrdenes"
        AutoGenerateColumns="false" DataKeyNames="IdOrden" GridLines="None"
        OnRowCommand="gvOrdenes_RowCommand"
        EmptyDataText="No hay órdenes para mostrar.">
        <Columns>
            <asp:BoundField DataField="IdOrden" HeaderText="N.º" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Documento" HeaderText="Documento" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
            <asp:BoundField DataField="Kilometraje" HeaderText="Kilometraje" DataFormatString="{0:N0}"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:BoundField DataField="NombreUsuario" HeaderText="Abierta por" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server" CssClass="accion-ver"
                        CommandName="Ver" CommandArgument='<%# Eval("IdOrden") %>'
                        CausesValidation="false">Ver</asp:LinkButton>
                    <asp:LinkButton runat="server" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdOrden") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- "Ver": la orden completa, de solo lectura, para todos los roles. --%>
    <div class="modal fade" id="modalVerOrden" tabindex="-1" aria-labelledby="tituloModalVerOrden" aria-hidden="true">
        <div class="modal-dialog modal-xl">
            <div class="modal-content">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalVerOrden">
                        <asp:Literal ID="litTituloVer" runat="server" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <dl class="datos-resumen datos-detalle">
                        <dt>Cliente</dt><dd><asp:Literal ID="litVerCliente" runat="server" /></dd>
                        <dt>Vehículo</dt><dd><asp:Literal ID="litVerVehiculo" runat="server" /></dd>
                        <dt>Turno</dt><dd><asp:Literal ID="litVerTurno" runat="server" /></dd>
                        <dt>Fecha de ingreso</dt><dd><asp:Literal ID="litVerFecha" runat="server" /></dd>
                        <dt>Kilometraje</dt><dd><asp:Literal ID="litVerKilometraje" runat="server" /></dd>
                        <dt>Estado</dt><dd><asp:Literal ID="litVerEstado" runat="server" /></dd>
                        <dt>Abierta por</dt><dd><asp:Literal ID="litVerUsuario" runat="server" /></dd>
                        <dt>Venta</dt><dd><asp:Literal ID="litVerVenta" runat="server" /></dd>
                        <dt>Observaciones</dt><dd><asp:Literal ID="litVerObservaciones" runat="server" /></dd>
                    </dl>

                    <div class="row g-3 mt-1">
                        <div class="col-lg-6">
                            <div class="seccion-modal">
                                <h3>Servicios</h3>
                                <asp:GridView ID="gvVerServicios" runat="server" data-sin-filtro="true"
                                    CssClass="tabla-abm tabla-compacta" AutoGenerateColumns="false" GridLines="None"
                                    EmptyDataText="Sin servicios.">
                                    <Columns>
                                        <asp:BoundField DataField="NombreServicio" HeaderText="Servicio" />
                                        <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="PrecioAplicado" HeaderText="Precio" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="Subtotal" HeaderText="Subtotal" DataFormatString="{0:N2}" />
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </div>
                        <div class="col-lg-6">
                            <div class="seccion-modal">
                                <h3>Insumos</h3>
                                <asp:GridView ID="gvVerInsumos" runat="server" data-sin-filtro="true"
                                    CssClass="tabla-abm tabla-compacta" AutoGenerateColumns="false" GridLines="None"
                                    EmptyDataText="Sin insumos.">
                                    <Columns>
                                        <asp:BoundField DataField="NombreInsumo" HeaderText="Insumo" />
                                        <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio" DataFormatString="{0:N2}" />
                                        <asp:BoundField DataField="Subtotal" HeaderText="Subtotal" DataFormatString="{0:N2}" />
                                    </Columns>
                                </asp:GridView>
                            </div>
                        </div>
                    </div>

                    <div class="total-detalle">Total de la orden <strong>$ <asp:Literal ID="litVerTotal" runat="server" /></strong></div>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnEditarDesdeVer" runat="server" CssClass="boton-rojo" Text="Editar"
                            OnClick="btnEditarDesdeVer_Click" CausesValidation="false" />
                    </span>
                    <asp:HiddenField ID="hdnIdOrdenVer" runat="server" />
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cerrar</button>
                </div>
            </div>
        </div>
    </div>

    <%-- Alta y edición de la orden en un modal grande: arriba la cabecera, y editando una orden
         ya creada, abajo el detalle de servicios e insumos. Todo el cuerpo va en un UpdatePanel:
         elegir el cliente o agregar/quitar líneas no cierra el modal. Los botones del pie
         (Guardar, Cancelar orden) y los de cierre hacen postback completo para refrescar la lista. --%>
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
                                            placeholder="Elegí el cliente o buscalo por nombre o documento" />
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

                            <div class="total-detalle">Total de la orden <strong>$ <asp:Literal ID="litTotalOrden" runat="server" /></strong></div>

                            <%-- Cierre: genera la venta. Si el cliente tiene cuenta corriente se elige si el
                                 saldo queda en la cuenta o se cobra ahora; sin cuenta, se pasa a cobrar. Lo
                                 abre "Cerrar orden…" del pie (Lubricentro.alternar), sin ir al servidor. --%>
                            <div id="confirmarCierre" class="confirmacion-modal" hidden>
                                <p><asp:Literal ID="litConfirmarCierre" runat="server" /></p>
                                <div class="botones-confirmacion">
                                    <asp:Button ID="btnCerrarACuenta" runat="server" CssClass="boton-gris"
                                        Text="Dejar en cuenta corriente" OnClick="btnCerrarACuenta_Click" CausesValidation="false" />
                                    <asp:Button ID="btnCerrarYCobrar" runat="server" CssClass="boton-rojo"
                                        Text="Cerrar y cobrar ahora" OnClick="btnCerrarYCobrar_Click" CausesValidation="false" />
                                    <button type="button" class="boton-borde-rojo" onclick="Lubricentro.alternar('confirmarCierre');">Volver</button>
                                </div>
                            </div>
                        </asp:Panel>
                    </ContentTemplate>
                    <Triggers>
                        <%-- Postback completo: "Nuevo cliente"/"Nuevo vehículo" y el cierre hacen
                             Response.Redirect (o refrescan la lista), que no funciona como trigger async
                             normal dentro del UpdatePanel. --%>
                        <asp:PostBackTrigger ControlID="btnNuevoCliente" />
                        <asp:PostBackTrigger ControlID="btnNuevoVehiculo" />
                        <asp:PostBackTrigger ControlID="btnCerrarACuenta" />
                        <asp:PostBackTrigger ControlID="btnCerrarYCobrar" />
                    </Triggers>
                </asp:UpdatePanel>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnCancelarOrden" runat="server" CssClass="boton-borde-rojo" Text="Cancelar orden" Visible="false"
                            OnClick="btnCancelarOrden_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Cancelar esta orden? Se repondrá el stock de los insumos cargados.');" />
                        <asp:PlaceHolder ID="phCerrarOrden" runat="server" Visible="false">
                            <button type="button" class="boton-gris" onclick="Lubricentro.alternar('confirmarCierre');">Cerrar orden…</button>
                        </asp:PlaceHolder>
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
