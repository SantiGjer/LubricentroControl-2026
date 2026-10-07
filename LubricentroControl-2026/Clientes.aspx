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

    <asp:HiddenField ID="hdnVieneDeOrden" runat="server" />
    <asp:HiddenField ID="hdnOrKilometraje" runat="server" />
    <asp:HiddenField ID="hdnOrObservaciones" runat="server" />
    <asp:HiddenField ID="hdnOrIdTurno" runat="server" />

    <asp:HiddenField ID="hdnVieneDeCuentaCorriente" runat="server" />

    <div class="barra-herramientas">
        <input type="search" id="filtroClientes" class="filtro-tabla-texto"
            placeholder="Filtrar por nombre, apellido, DNI, teléfono o mail" aria-label="Filtrar clientes" />
        <span class="opcion-barra">
            <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
            <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label>
        </span>
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo cliente"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <asp:GridView ID="gvClientes" runat="server" CssClass="tabla-abm" data-filtro="filtroClientes"
        AutoGenerateColumns="false" DataKeyNames="IdCliente" GridLines="None"
        OnRowCommand="gvClientes_RowCommand" EmptyDataText="No hay clientes cargados.">
        <Columns>
            <asp:BoundField DataField="Apellido" HeaderText="Apellido" />
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="Dni" HeaderText="DNI" />
            <asp:BoundField DataField="Telefono" HeaderText="Teléfono" />
            <asp:BoundField DataField="Email" HeaderText="Mail" />
            <asp:TemplateField HeaderText="Cta. cte.">
                <ItemTemplate>
                    <%# (bool)Eval("CuentaCorriente") ? "Sí" : "No" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate>
                    <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
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
                        Estás creando un cliente para asignarlo a un vehículo nuevo.
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
                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label>
                            <asp:TextBox ID="txtNombre" runat="server" MaxLength="50" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El nombre es obligatorio." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtApellido">Apellido</asp:Label>
                            <asp:TextBox ID="txtApellido" runat="server" MaxLength="50" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtApellido"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El apellido es obligatorio." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtDni">DNI</asp:Label>
                            <asp:TextBox ID="txtDni" runat="server" MaxLength="8" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtDni"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El DNI es obligatorio." />
                            <asp:CustomValidator runat="server" ControlToValidate="txtDni"
                                OnServerValidate="valDni_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Cliente"
                                ErrorMessage="El DNI debe tener 7 u 8 números, sin puntos." />
                        </div>

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
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtDireccion">Dirección</asp:Label>
                            <asp:TextBox ID="txtDireccion" runat="server" MaxLength="200" />
                        </div>

                        <div class="col-12 campo">
                            <div class="form-switch">
                                <input type="checkbox" id="chkCuentaCorriente" runat="server" class="form-check-input" role="switch" />
                                <asp:Label runat="server" AssociatedControlID="chkCuentaCorriente" CssClass="form-check-label">Cuenta corriente</asp:Label>
                            </div>
                            <p class="texto-ayuda">
                                Con cuenta corriente el cliente puede quedar debiendo. Sin ella, al cerrar su orden
                                de trabajo se pasa directo a cobrarla.
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
