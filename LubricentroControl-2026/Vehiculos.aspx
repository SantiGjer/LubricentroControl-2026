<%@ Page Title="Vehículos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Vehiculos.aspx.cs" Inherits="LubricentroControl_2026.Vehiculos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Vehículos</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:HiddenField ID="hdnVieneDeOrden" runat="server" />
    <asp:HiddenField ID="hdnOrKilometraje" runat="server" />
    <asp:HiddenField ID="hdnOrObservaciones" runat="server" />
    <asp:HiddenField ID="hdnOrIdTurno" runat="server" />

    <div class="barra-herramientas">
        <input type="search" id="filtroVehiculos" class="filtro-tabla-texto"
            placeholder="Buscar por patente, marca, modelo o dueño" aria-label="Buscar vehículos" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo vehículo"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesVehiculos">
        <div class="grupo-opciones" data-columna="Estado" data-inicial="Activo">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="Activo">Activos</button>
            <button type="button" class="opcion" data-valor="Inactivo">Inactivos</button>
            <button type="button" class="opcion" data-valor="">Todos</button>
        </div>
        <div class="grupo-opciones" data-columna="Combustible">
            <span class="titulo-opciones">Combustible</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Nafta">Nafta</button>
            <button type="button" class="opcion" data-valor="Diésel">Diésel</button>
            <button type="button" class="opcion" data-valor="GNC">GNC</button>
            <button type="button" class="opcion" data-valor="Eléctrico|Híbrido">Eléctrico o híbrido</button>
        </div>
    </div>

    <asp:GridView ID="gvVehiculos" runat="server" CssClass="tabla-abm" data-filtro="filtroVehiculos"
        data-opciones-tabla="opcionesVehiculos" data-titulo-detalle="Vehículo"
        AutoGenerateColumns="false" DataKeyNames="IdVehiculo" GridLines="None"
        OnRowCommand="gvVehiculos_RowCommand" EmptyDataText="No hay vehículos cargados.">
        <Columns>
            <asp:BoundField DataField="Patente" HeaderText="Patente" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Marca" HeaderText="Marca" />
            <asp:BoundField DataField="Modelo" HeaderText="Modelo" />
            <asp:BoundField DataField="Anio" HeaderText="Año" />
            <asp:BoundField DataField="TipoCombustible" HeaderText="Combustible" />
            <asp:BoundField DataField="NombreCliente" HeaderText="Dueño" />
            <asp:BoundField DataField="DocumentoCliente" HeaderText="Documento del dueño"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <a href="#" class="accion-ver" data-ver-detalle>Ver</a>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdVehiculo") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir && (bool)Eval("Activo") %>'
                        CommandName="CambiarDueno" CommandArgument='<%# Eval("IdVehiculo") %>'
                        CausesValidation="false">Cambiar dueño</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalVehiculo" tabindex="-1" aria-labelledby="tituloModalVehiculo"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalVehiculo">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo vehículo" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlVieneDeOrden" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
                        Estás creando un vehículo para una orden de trabajo nueva.
                        <asp:Button ID="btnVolverAOrdenes" runat="server" CssClass="boton-gris boton-chico" Text="Volver a Órdenes sin crear"
                            OnClick="btnVolverAOrdenes_Click" CausesValidation="false" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdVehiculo" runat="server" />
                    <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                    <div class="row campos-formulario">
                        <%-- Alta: se elige el dueño. Editando, el dueño se ve fijo: cambiarlo es
                             "Cambiar dueño", que valida que el vehículo no tenga nada en curso. --%>
                        <asp:Panel ID="pnlDuenoSeleccion" runat="server" CssClass="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtCliente">Dueño</asp:Label>
                            <div class="selector-con-boton">
                                <div class="selector-busqueda" data-opciones="<%: OpcionesClientes %>">
                                    <asp:TextBox ID="txtCliente" runat="server" CssClass="selector-texto" autocomplete="off"
                                        placeholder="Elegí el dueño o buscalo por nombre o documento" />
                                    <button type="button" class="selector-boton" tabindex="-1" aria-label="Ver los clientes"></button>
                                    <asp:HiddenField ID="hdnIdCliente" runat="server" />
                                </div>
                                <asp:Button ID="btnNuevoCliente" runat="server" CssClass="boton-gris" Text="Nuevo cliente"
                                    OnClick="btnNuevoCliente_Click" CausesValidation="false" />
                            </div>
                            <asp:CustomValidator runat="server" OnServerValidate="valCliente_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo"
                                ErrorMessage="Seleccioná el cliente dueño del vehículo." />
                        </asp:Panel>

                        <asp:Panel ID="pnlDuenoFijo" runat="server" CssClass="col-12 campo" Visible="false">
                            <label>Dueño</label>
                            <div class="valor-fijo valor-fijo-con-boton">
                                <asp:Literal ID="litDuenoActual" runat="server" />
                                <asp:Button ID="btnCambiarDueno" runat="server" CssClass="boton-borde-rojo" Text="Cambiar dueño"
                                    OnClick="btnCambiarDueno_Click" CausesValidation="false" />
                            </div>
                        </asp:Panel>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtPatente">Patente</asp:Label>
                            <asp:TextBox ID="txtPatente" runat="server" MaxLength="7" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPatente"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo"
                                ErrorMessage="La patente es obligatoria." />
                            <asp:CustomValidator runat="server" ControlToValidate="txtPatente"
                                OnServerValidate="valPatente_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo"
                                ErrorMessage="La patente no tiene un formato válido (ej. ABC123 o AB123CD)." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtAnio">Año</asp:Label>
                            <asp:TextBox ID="txtAnio" runat="server" TextMode="Number" />
                            <asp:RangeValidator ID="valAnio" runat="server" ControlToValidate="txtAnio" Type="Integer"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Vehiculo" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtMarca">Marca</asp:Label>
                            <asp:TextBox ID="txtMarca" runat="server" MaxLength="50" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtModelo">Modelo</asp:Label>
                            <asp:TextBox ID="txtModelo" runat="server" MaxLength="50" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlTipoCombustible">Tipo de combustible</asp:Label>
                            <asp:DropDownList ID="ddlTipoCombustible" runat="server" />
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnBorrar" runat="server" CssClass="boton-borde-rojo" Text="Borrar" Visible="false"
                            OnClick="btnBorrar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Borrar este vehículo?');" />
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Vehiculo" />
                </div>
            </asp:Panel>
        </div>
    </div>

    <%-- Cambio de dueño: el vehículo pasa a otro cliente. Lo hecho para el dueño anterior
         (turnos, órdenes, ventas) queda a su nombre; no se puede con una orden o un turno en curso. --%>
    <div class="modal fade" id="modalCambioDueno" tabindex="-1" aria-labelledby="tituloModalCambioDueno"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlCambioDueno" runat="server" CssClass="modal-content" DefaultButton="btnConfirmarCambioDueno">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalCambioDueno">
                        <asp:Literal ID="litTituloCambioDueno" runat="server" Text="Cambiar dueño" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorCambioDueno" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorCambioDueno" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdVehiculoDueno" runat="server" />

                    <dl class="datos-resumen">
                        <dt>Vehículo</dt><dd><asp:Literal ID="litVehiculoCambio" runat="server" /></dd>
                        <dt>Dueño actual</dt><dd><asp:Literal ID="litDuenoAnterior" runat="server" /></dd>
                    </dl>

                    <div class="row campos-formulario">
                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtNuevoDueno">Nuevo dueño</asp:Label>
                            <div class="selector-con-boton">
                                <div class="selector-busqueda" data-opciones="<%: OpcionesClientes %>">
                                    <asp:TextBox ID="txtNuevoDueno" runat="server" CssClass="selector-texto" autocomplete="off"
                                        placeholder="Elegí el nuevo dueño o buscalo por nombre o documento" />
                                    <button type="button" class="selector-boton" tabindex="-1" aria-label="Ver los clientes"></button>
                                    <asp:HiddenField ID="hdnIdNuevoDueno" runat="server" />
                                </div>
                                <asp:Button ID="btnNuevoClienteDueno" runat="server" CssClass="boton-gris" Text="Nuevo cliente"
                                    OnClick="btnNuevoClienteDueno_Click" CausesValidation="false" />
                            </div>
                            <asp:CustomValidator runat="server" OnServerValidate="valNuevoDueno_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="CambioDueno"
                                ErrorMessage="Seleccioná el nuevo dueño." />
                            <p class="texto-ayuda">
                                Los turnos, órdenes y ventas que ya tiene el vehículo quedan a nombre del dueño anterior.
                                No se puede cambiar mientras tenga una orden en el taller o un turno pendiente.
                            </p>
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnConfirmarCambioDueno" runat="server" CssClass="boton-rojo" Text="Cambiar dueño"
                        OnClick="btnConfirmarCambioDueno_Click" ValidationGroup="CambioDueno" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
