<%@ Page Title="Clientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Clientes.aspx.cs" Inherits="LubricentroControl_2026.Clientes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

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
    <asp:HiddenField ID="hdnVhCambioDueno" runat="server" />

    <asp:HiddenField ID="hdnVieneDeOrden" runat="server" />
    <asp:HiddenField ID="hdnOrKilometraje" runat="server" />
    <asp:HiddenField ID="hdnOrObservaciones" runat="server" />
    <asp:HiddenField ID="hdnOrIdTurno" runat="server" />

    <asp:HiddenField ID="hdnVieneDeCuentaCorriente" runat="server" />

    <div class="barra-herramientas">
        <input type="search" id="filtroClientes" class="filtro-tabla-texto"
            placeholder="Buscar por nombre, documento, teléfono, mail o patente" aria-label="Buscar clientes" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo cliente"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesClientes">
        <div class="grupo-opciones" data-columna="Estado" data-inicial="Activo">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="Activo">Activos</button>
            <button type="button" class="opcion" data-valor="Inactivo">Inactivos</button>
            <button type="button" class="opcion" data-valor="">Todos</button>
        </div>
        <div class="grupo-opciones" data-columna="Tipo">
            <span class="titulo-opciones">Tipo</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Persona física">Personas</button>
            <button type="button" class="opcion" data-valor="Empresa">Empresas</button>
        </div>
        <div class="grupo-opciones" data-columna="Cta. cte.">
            <span class="titulo-opciones">Cuenta corriente</span>
            <button type="button" class="opcion" data-valor="">Todos</button>
            <button type="button" class="opcion" data-valor="Sí">Con cuenta</button>
            <button type="button" class="opcion" data-valor="No">Sin cuenta</button>
        </div>
    </div>

    <%-- Las columnas con "oculta" arrancan escondidas: se suman con "Columnas" y se ven todas en "Ver". --%>
    <asp:GridView ID="gvClientes" runat="server" CssClass="tabla-abm" data-filtro="filtroClientes"
        data-opciones-tabla="opcionesClientes" data-titulo-detalle="Cliente"
        AutoGenerateColumns="false" DataKeyNames="IdCliente" GridLines="None"
        OnRowCommand="gvClientes_RowCommand" EmptyDataText="No hay clientes cargados.">
        <Columns>
            <asp:BoundField DataField="Denominacion" HeaderText="Cliente" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="TipoCliente" HeaderText="Tipo" />
            <asp:BoundField DataField="Documento" HeaderText="Documento" ItemStyle-CssClass="sin-corte" />
            <asp:BoundField DataField="CondicionIva" HeaderText="Condición IVA" />
            <asp:BoundField DataField="Telefono" HeaderText="Teléfono" ItemStyle-CssClass="sin-corte" />
            <asp:BoundField DataField="Email" HeaderText="Mail" />
            <asp:BoundField DataField="Direccion" HeaderText="Dirección" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Localidad" HeaderText="Localidad" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Provincia" HeaderText="Provincia" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="CodigoPostal" HeaderText="Código postal" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Vehículos" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta">
                <ItemTemplate><%#: PatentesDe((int)Eval("IdCliente")) %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Cta. cte.">
                <ItemTemplate><%# (bool)Eval("CuentaCorriente") ? "Sí" : "No" %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="FechaAlta" HeaderText="Alta" DataFormatString="{0:dd/MM/yyyy}"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <a href="#" class="accion-ver" data-ver-detalle>Ver</a>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdCliente") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Alta y edición en un modal sobre la lista. El servidor lo vuelve a abrir después de cada
         postback que lo necesite (Interfaz.AbrirModal): Nuevo, Editar, o un error al guardar. --%>
    <div class="modal fade" id="modalCliente" tabindex="-1" aria-labelledby="tituloModalCliente"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalCliente">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo cliente" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:Panel ID="pnlVieneDeVehiculo" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
                        <asp:Literal ID="litVieneDeVehiculo" runat="server" Text="Estás creando un cliente para asignarlo a un vehículo nuevo." />
                        <asp:Button ID="btnVolverAVehiculos" runat="server" CssClass="boton-gris boton-chico" Text="Volver a Vehículos sin crear"
                            OnClick="btnVolverAVehiculos_Click" CausesValidation="false" />
                    </asp:Panel>

                    <asp:Panel ID="pnlVieneDeOrden" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
                        Estás creando un cliente para una orden de trabajo nueva.
                        <asp:Button ID="btnVolverAOrdenes" runat="server" CssClass="boton-gris boton-chico" Text="Volver a Órdenes sin crear"
                            OnClick="btnVolverAOrdenes_Click" CausesValidation="false" />
                    </asp:Panel>

                    <asp:Panel ID="pnlVieneDeCuentaCorriente" runat="server" Visible="false" CssClass="alert alert-info" role="alert">
                        Al guardar volvés a la cuenta corriente de clientes.
                        <asp:Button ID="btnVolverACuentaCorriente" runat="server" CssClass="boton-gris boton-chico" Text="Volver sin cambios"
                            OnClick="btnVolverACuentaCorriente_Click" CausesValidation="false" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdCliente" runat="server" />
                    <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                    <div class="row campos-formulario">
                        <div class="col-12 campo">
                            <label>Tipo de cliente</label>
                            <asp:RadioButtonList ID="rblTipoCliente" runat="server" ClientIDMode="Static"
                                RepeatLayout="Flow" RepeatDirection="Horizontal" CssClass="opciones-radio" />
                        </div>

                        <%-- Persona física: nombre y apellido. Empresa: razón social. --%>
                        <div class="col-md-6 campo" data-mostrar-si="rblTipoCliente=Persona física">
                            <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="50" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtNombre" ValidateEmptyText="true"
                                OnServerValidate="valDatoDePersona_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El nombre es obligatorio." />
                        </div>

                        <div class="col-md-6 campo" data-mostrar-si="rblTipoCliente=Persona física">
                            <asp:Label runat="server" AssociatedControlID="txtApellido">Apellido</asp:Label>
                            <asp:TextBox ID="txtApellido" runat="server" MaxLength="50" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtApellido" ValidateEmptyText="true"
                                OnServerValidate="valDatoDePersona_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El apellido es obligatorio." />
                        </div>

                        <div class="col-12 campo" data-mostrar-si="rblTipoCliente=Empresa">
                            <asp:Label runat="server" AssociatedControlID="txtRazonSocial">Razón social</asp:Label>
                            <asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="150" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtRazonSocial" ValidateEmptyText="true"
                                OnServerValidate="valRazonSocial_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="La razón social es obligatoria." />
                        </div>

                        <div class="col-12"><p class="subtitulo-formulario">Datos fiscales</p></div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlTipoDocumento">Tipo de identificación</asp:Label>
                            <asp:DropDownList ID="ddlTipoDocumento" runat="server" />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="txtNumeroDocumento">Número</asp:Label>
                            <asp:TextBox ID="txtNumeroDocumento" runat="server" MaxLength="20" />
                            <asp:CustomValidator ID="valNumeroDocumento" runat="server" ControlToValidate="txtNumeroDocumento"
                                ValidateEmptyText="true" OnServerValidate="valNumeroDocumento_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente" />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlCondicionIva">Condición frente al IVA</asp:Label>
                            <asp:DropDownList ID="ddlCondicionIva" runat="server" />
                        </div>

                        <div class="col-12">
                            <p class="texto-ayuda">Una empresa, y un responsable inscripto, monotributista o exento, se identifica con su CUIT.</p>
                        </div>

                        <div class="col-12"><p class="subtitulo-formulario">Contacto y domicilio</p></div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtTelefono">Teléfono</asp:Label>
                            <asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" TextMode="Phone" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtTelefono"
                                OnServerValidate="valTelefono_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El teléfono solo puede tener números, espacios, guiones o paréntesis (entre 6 y 15 números)." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtEmail"
                                OnServerValidate="valEmail_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El mail no tiene un formato válido." />
                        </div>

                        <div class="col-md-8 campo">
                            <asp:Label runat="server" AssociatedControlID="txtDireccion">Dirección</asp:Label>
                            <asp:TextBox ID="txtDireccion" runat="server" MaxLength="200" placeholder="Calle y número" />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="txtCodigoPostal">Código postal</asp:Label>
                            <asp:TextBox ID="txtCodigoPostal" runat="server" MaxLength="8" placeholder="1638 o C1406GZA" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtCodigoPostal"
                                OnServerValidate="valCodigoPostal_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="Usá 4 números (1638) o el formato CPA (C1406GZA)." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtLocalidad">Localidad</asp:Label>
                            <asp:TextBox ID="txtLocalidad" runat="server" MaxLength="100" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlProvincia">Provincia</asp:Label>
                            <asp:DropDownList ID="ddlProvincia" runat="server" />
                        </div>

                        <div class="col-12 campo">
                            <div class="form-switch">
                                <input type="checkbox" id="chkCuentaCorriente" runat="server" class="form-check-input" role="switch" />
                                <asp:Label runat="server" AssociatedControlID="chkCuentaCorriente" CssClass="form-check-label">Cuenta corriente</asp:Label>
                            </div>
                            <p class="texto-ayuda">
                                Con cuenta corriente el cliente puede quedar debiendo: al cerrar su orden se elige si el saldo
                                va a la cuenta o se cobra en el momento. Sin ella, al cerrar su orden se pasa directo a cobrarla.
                                <asp:Literal ID="litCuentaCorrienteBloqueada" runat="server" Visible="false"
                                    Text="Solo un encargado o un administrador puede cambiarla." />
                            </p>
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnBorrar" runat="server" CssClass="boton-borde-rojo" Text="Borrar" Visible="false"
                            OnClick="btnBorrar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Borrar este cliente?');" />
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Cliente" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
