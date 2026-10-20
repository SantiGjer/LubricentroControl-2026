<%@ Page Title="Proveedores" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Proveedores.aspx.cs" Inherits="LubricentroControl_2026.Proveedores" %>
<%@ Import Namespace="BIZ.Modelo" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Proveedores</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroProveedores" class="filtro-tabla-texto"
            placeholder="Buscar por razón social, nombre, CUIT, rubro, contacto, teléfono o mail" aria-label="Buscar proveedores" />
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo proveedor"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <div class="opciones-tabla" id="opcionesProveedores">
        <div class="grupo-opciones" data-columna="Estado" data-inicial="Activo">
            <span class="titulo-opciones">Estado</span>
            <button type="button" class="opcion" data-valor="Activo">Activos</button>
            <button type="button" class="opcion" data-valor="Inactivo">Inactivos</button>
            <button type="button" class="opcion" data-valor="">Todos</button>
        </div>
        <div class="grupo-opciones" data-columna="Condición IVA">
            <span class="titulo-opciones">Condición IVA</span>
            <button type="button" class="opcion" data-valor="">Todas</button>
            <button type="button" class="opcion" data-valor="Responsable Inscripto">Resp. inscripto</button>
            <button type="button" class="opcion" data-valor="Monotributista|Exento">Monotributo o exento</button>
        </div>
    </div>

    <%-- Las columnas con "oculta" arrancan escondidas: se suman con "Columnas" y se ven todas en "Ver". --%>
    <asp:GridView ID="gvProveedores" runat="server" CssClass="tabla-abm" data-filtro="filtroProveedores"
        data-opciones-tabla="opcionesProveedores" data-titulo-detalle="Proveedor"
        AutoGenerateColumns="false" DataKeyNames="IdProveedor" GridLines="None"
        OnRowCommand="gvProveedores_RowCommand" EmptyDataText="No hay proveedores cargados.">
        <Columns>
            <asp:BoundField DataField="RazonSocial" HeaderText="Razón social" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="NombreFantasia" HeaderText="Nombre de fantasía"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="CUIT" ItemStyle-CssClass="sin-corte">
                <ItemTemplate><%# Proveedor.FormatearCuit(Eval("Cuit").ToString()) %></ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="CondicionIva" HeaderText="Condición IVA"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Rubro" HeaderText="Rubro" />
            <asp:BoundField DataField="Telefono" HeaderText="Teléfono" ItemStyle-CssClass="sin-corte" />
            <asp:BoundField DataField="Email" HeaderText="Mail" />
            <asp:BoundField DataField="Contacto" HeaderText="Contacto" />
            <asp:BoundField DataField="TelefonoContacto" HeaderText="Teléfono del contacto"
                HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta sin-corte" />
            <asp:BoundField DataField="Direccion" HeaderText="Dirección" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Localidad" HeaderText="Localidad" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Provincia" HeaderText="Provincia" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="CodigoPostal" HeaderText="Código postal" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Cbu" HeaderText="CBU o CVU" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta sin-corte" />
            <asp:BoundField DataField="Alias" HeaderText="Alias" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Observaciones" HeaderText="Observaciones" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate><%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <a href="#" class="accion-ver" data-ver-detalle>Ver</a>
                    <asp:LinkButton runat="server" CssClass="accion-detalle" Visible='<%# PuedeEscribir %>'
                        CommandName="Editar" CommandArgument='<%# Eval("IdProveedor") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <div class="modal fade" id="modalProveedor" tabindex="-1" aria-labelledby="tituloModalProveedor"
        aria-hidden="true" data-bs-backdrop="static">
        <div class="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalProveedor">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo proveedor" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlErrorFormulario" runat="server" Visible="false" CssClass="alert alert-danger" role="alert">
                        <asp:Literal ID="litErrorFormulario" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdProveedor" runat="server" />
                    <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                    <div class="row campos-formulario">
                        <div class="col-12"><p class="subtitulo-formulario">Datos fiscales</p></div>

                        <div class="col-md-8 campo">
                            <asp:Label runat="server" AssociatedControlID="txtRazonSocial">Razón social</asp:Label>
                            <asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="150" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtRazonSocial"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="La razón social es obligatoria." />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="txtCuit">CUIT</asp:Label>
                            <asp:TextBox ID="txtCuit" runat="server" MaxLength="13" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCuit"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El CUIT es obligatorio." />
                            <asp:CustomValidator runat="server" ControlToValidate="txtCuit"
                                OnServerValidate="valCuit_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El CUIT debe tener 11 números, con o sin guiones." />
                        </div>

                        <div class="col-md-8 campo">
                            <asp:Label runat="server" AssociatedControlID="txtNombreFantasia">Nombre de fantasía</asp:Label>
                            <asp:TextBox ID="txtNombreFantasia" runat="server" MaxLength="150"
                                placeholder="Cómo se lo conoce, si no es la razón social" />
                        </div>

                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlCondicionIva">Condición frente al IVA</asp:Label>
                            <asp:DropDownList ID="ddlCondicionIva" runat="server" />
                        </div>

                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtRubro">Rubro</asp:Label>
                            <asp:TextBox ID="txtRubro" runat="server" MaxLength="100"
                                placeholder="Qué le vende al lubricentro: lubricantes, filtros, baterías…" />
                        </div>

                        <div class="col-12"><p class="subtitulo-formulario">Contacto</p></div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtTelefono">Teléfono</asp:Label>
                            <asp:TextBox ID="txtTelefono" runat="server" MaxLength="30" TextMode="Phone" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtTelefono"
                                OnServerValidate="valTelefono_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El teléfono solo puede tener números, espacios, guiones o paréntesis (entre 6 y 15 números)." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtEmail">Mail</asp:Label>
                            <asp:TextBox ID="txtEmail" runat="server" TextMode="Email" MaxLength="150" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtEmail"
                                OnServerValidate="valEmail_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El mail no tiene un formato válido." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtContacto">Persona de contacto</asp:Label>
                            <asp:TextBox ID="txtContacto" runat="server" MaxLength="100" placeholder="El vendedor o representante" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtTelefonoContacto">Teléfono del contacto</asp:Label>
                            <asp:TextBox ID="txtTelefonoContacto" runat="server" MaxLength="30" TextMode="Phone" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtTelefonoContacto"
                                OnServerValidate="valTelefono_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El teléfono solo puede tener números, espacios, guiones o paréntesis (entre 6 y 15 números)." />
                        </div>

                        <div class="col-12"><p class="subtitulo-formulario">Domicilio</p></div>

                        <div class="col-md-8 campo">
                            <asp:Label runat="server" AssociatedControlID="txtDireccion">Dirección</asp:Label>
                            <asp:TextBox ID="txtDireccion" runat="server" MaxLength="200" placeholder="Calle y número" />
                        </div>

                        <%-- Al escribir el código postal se completan la provincia y la localidad
                             (Lubricentro.js, data-completa-localidad / data-completa-provincia). --%>
                        <div class="col-md-4 campo">
                            <asp:Label runat="server" AssociatedControlID="txtCodigoPostal">Código postal</asp:Label>
                            <asp:TextBox ID="txtCodigoPostal" runat="server" MaxLength="8" placeholder="1638 o C1406GZA"
                                data-completa-localidad="txtLocalidad" data-completa-provincia="ddlProvincia" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtCodigoPostal"
                                OnServerValidate="valCodigoPostal_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="Usá 4 números (1638) o el formato CPA (C1406GZA)." />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="txtLocalidad">Localidad</asp:Label>
                            <asp:TextBox ID="txtLocalidad" runat="server" MaxLength="100" ClientIDMode="Static" autocomplete="off" />
                        </div>

                        <div class="col-md-6 campo">
                            <asp:Label runat="server" AssociatedControlID="ddlProvincia">Provincia</asp:Label>
                            <asp:DropDownList ID="ddlProvincia" runat="server" ClientIDMode="Static" />
                        </div>

                        <div class="col-12"><p class="subtitulo-formulario">Datos para transferencias</p></div>

                        <div class="col-md-7 campo">
                            <asp:Label runat="server" AssociatedControlID="txtCbu">CBU o CVU</asp:Label>
                            <asp:TextBox ID="txtCbu" runat="server" MaxLength="30" inputmode="numeric" placeholder="22 números" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtCbu"
                                OnServerValidate="valCbu_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El CBU o CVU debe tener 22 números." />
                        </div>

                        <div class="col-md-5 campo">
                            <asp:Label runat="server" AssociatedControlID="txtAlias">Alias</asp:Label>
                            <asp:TextBox ID="txtAlias" runat="server" MaxLength="20" placeholder="FILTROS.SUR.PAGOS" />
                            <asp:CustomValidator runat="server" ControlToValidate="txtAlias"
                                OnServerValidate="valAlias_ServerValidate"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Proveedor"
                                ErrorMessage="El alias debe tener de 6 a 20 letras, números, puntos o guiones." />
                        </div>

                        <div class="col-12 campo">
                            <asp:Label runat="server" AssociatedControlID="txtObservaciones">Observaciones</asp:Label>
                            <asp:TextBox ID="txtObservaciones" runat="server" TextMode="MultiLine" Rows="3" MaxLength="500"
                                placeholder="Días de visita o de entrega, pedido mínimo, condiciones…" />
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnBorrar" runat="server" CssClass="boton-borde-rojo" Text="Borrar" Visible="false"
                            OnClick="btnBorrar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Borrar este proveedor?');" />
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Proveedor" />
                </div>
            </asp:Panel>
        </div>
    </div>

    </div>
</asp:Content>
