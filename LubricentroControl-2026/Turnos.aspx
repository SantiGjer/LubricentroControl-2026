<%@ Page Title="Turnos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Turnos.aspx.cs" Inherits="LubricentroControl_2026.Turnos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Turnos</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroTurnos" class="filtro-tabla-texto"
            placeholder="Buscar por cliente, documento, patente o fecha" aria-label="Buscar turnos" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo turno"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesTurnos">
        <div class="grupo-opciones" data-atributo="cuando">
            <span class="titulo-opciones">Cuándo</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Hoy">Hoy</button>
            <button type="button" class="opcion" data-valor="Próximo">Próximos</button>
            <button type="button" class="opcion" data-valor="Pasado">Pasados</button>
        </div>
        <div class="grupo-opciones" data-columna="Estado">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Solicitado|Confirmado">Pendientes</button>
            <button type="button" class="opcion" data-valor="Solicitado">Solicitados</button>
            <button type="button" class="opcion" data-valor="Confirmado">Confirmados</button>
            <button type="button" class="opcion" data-valor="Completado">Completados</button>
            <button type="button" class="opcion" data-valor="Cancelado">Cancelados</button>
        </div>
    </div>

    <%-- Vienen ordenados desde el servidor: primero los de hoy (por hora), después los próximos y
         al final los pasados (TurnoDAL.Listar). Los de hoy van marcados (fila-hoy). --%>
    <asp:GridView ID="gvTurnos" runat="server" CssClass="tabla-abm" data-filtro="filtroTurnos"
        data-opciones-tabla="opcionesTurnos" data-titulo-detalle="Turno"
        AutoGenerateColumns="false" DataKeyNames="IdTurno" GridLines="None"
        OnRowCommand="gvTurnos_RowCommand" OnRowDataBound="gvTurnos_RowDataBound"
        EmptyDataText="No hay turnos para mostrar.">
        <Columns>
            <asp:BoundField DataField="FechaHoraAsignada" HeaderText="Fecha y hora" DataFormatString="{0:dd/MM/yyyy HH:mm}"
                ItemStyle-CssClass="celda-fecha" />
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Documento" HeaderText="Documento" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Vehículo">
                <ItemTemplate><%#: Eval("Patente") ?? "—" %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="Estado" HeaderText="Estado" />
            <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="FechaSolicitud" HeaderText="Pedido el" DataFormatString="{0:dd/MM/yyyy HH:mm}"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <a href="#" class="accion-ver" data-ver-detalle>Ver</a>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdTurno") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalTurno" tabindex="-1" aria-labelledby="tituloModalTurno"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalTurno">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo turno" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdTurno" runat="server" />

                    <%-- Elegir el cliente avisa al servidor (data-postback) para cargar sus vehículos:
                         es un postback parcial, el modal no se cierra. --%>
                    <asp:UpdatePanel ID="upnlCliente" runat="server">
                        <ContentTemplate>
                            <div class="row campos-formulario">
                                <div class="col-md-7 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtCliente">Cliente</asp:Label>
                                    <div class="selector-busqueda" data-postback="true" data-opciones="<%: OpcionesClientes %>">
                                        <asp:TextBox ID="txtCliente" runat="server" CssClass="selector-texto" autocomplete="off"
                                            placeholder="Elegí el cliente o buscalo por nombre o documento" />
                                        <button type="button" class="selector-boton" tabindex="-1" aria-label="Ver los clientes"></button>
                                        <asp:HiddenField ID="hdnIdCliente" runat="server" OnValueChanged="hdnIdCliente_ValueChanged" />
                                    </div>
                                    <asp:CustomValidator runat="server" OnServerValidate="valCliente_ServerValidate"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Turno"
                                        ErrorMessage="Seleccioná el cliente del turno." />
                                </div>

                                <div class="col-md-5 campo">
                                    <asp:Label runat="server" AssociatedControlID="ddlVehiculo">Vehículo (opcional)</asp:Label>
                                    <asp:DropDownList ID="ddlVehiculo" runat="server" />
                                </div>
                            </div>
                        </ContentTemplate>
                    </asp:UpdatePanel>

                    <div class="row campos-formulario mt-0">
                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="txtFecha">Fecha</asp:Label>
                            <asp:TextBox ID="txtFecha" runat="server" TextMode="Date" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtFecha"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Turno"
                                ErrorMessage="La fecha es obligatoria." />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="txtHora">Hora</asp:Label>
                            <asp:TextBox ID="txtHora" runat="server" TextMode="Time" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtHora"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Turno"
                                ErrorMessage="La hora es obligatoria." />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlEstado">Estado</asp:Label>
                            <asp:DropDownList ID="ddlEstado" runat="server" />
                            <asp:Label ID="litEstadoNuevo" runat="server" CssClass="valor-fijo d-block" Text="Solicitado" />
                        </div>

                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtObservaciones">Observaciones</asp:Label>
                            <asp:TextBox ID="txtObservaciones" runat="server" MaxLength="500" TextMode="MultiLine" Rows="3" />
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Turno" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
