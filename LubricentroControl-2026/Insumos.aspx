<%@ Page Title="Insumos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Insumos.aspx.cs" Inherits="LubricentroControl_2026.Insumos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Insumos</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <div class="barra-herramientas">
        <input type="search" id="filtroInsumos" class="filtro-tabla-texto"
            placeholder="Filtrar por nombre, marca o unidad" aria-label="Filtrar insumos" />
        <span class="opcion-barra">
            <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
            <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label>
        </span>
        <span class="acciones-barra">
            <asp:Button ID="btnNuevo" runat="server" CssClass="boton-rojo" Text="Nuevo insumo"
                OnClick="btnNuevo_Click" CausesValidation="false" />
        </span>
    </div>

    <p class="texto-nota"><small>Las filas resaltadas tienen el stock por debajo del mínimo.</small></p>

    <asp:GridView ID="gvInsumos" runat="server" data-filtro="filtroInsumos" data-filas-por-pagina="30"
        CssClass="tabla-abm tabla-compacta"
        AutoGenerateColumns="false" DataKeyNames="IdInsumo" GridLines="None"
        OnRowCommand="gvInsumos_RowCommand" OnRowDataBound="gvInsumos_RowDataBound"
        EmptyDataText="No hay insumos cargados.">
        <Columns>
            <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
            <asp:BoundField DataField="Marca" HeaderText="Marca" />
            <asp:BoundField DataField="UnidadMedida" HeaderText="Unidad" />
            <asp:BoundField DataField="StockActual" HeaderText="Stock" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="StockMinimo" HeaderText="Stock mín." DataFormatString="{0:N2}" />
            <asp:BoundField DataField="PrecioVenta" HeaderText="Precio" DataFormatString="{0:N2}" />
            <asp:TemplateField HeaderText="Estado">
                <ItemTemplate>
                    <%# (bool)Eval("Activo") ? "Activo" : "Inactivo" %>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server"
                        CommandName="Editar" CommandArgument='<%# Eval("IdInsumo") %>'
                        CausesValidation="false">Editar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Alta y edición en un modal. Editando un insumo existente el modal se agranda y suma el
         ajuste manual de stock y el historial de movimientos (kardex). --%>
    <div class="modal fade" id="modalInsumo" tabindex="-1" aria-labelledby="tituloModalInsumo"
        aria-hidden="true" data-bs-backdrop="static">
        <asp:Panel ID="pnlDialogo" runat="server" CssClass="modal-dialog modal-lg">
            <asp:Panel ID="pnlFormulario" runat="server" CssClass="modal-content" DefaultButton="btnGuardar">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalInsumo">
                        <asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo insumo" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:Panel ID="pnlMensajeFormulario" runat="server" Visible="false" role="alert">
                        <asp:Literal ID="litMensajeFormulario" runat="server" />
                    </asp:Panel>

                    <asp:HiddenField ID="hdnIdInsumo" runat="server" />
                    <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                    <div class="row g-4">
                        <asp:Panel ID="pnlDatos" runat="server" CssClass="col-12">
                            <div class="row campos-formulario">
                                <div class="col-12 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label>
                                    <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" />
                                    <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                                        ErrorMessage="El nombre es obligatorio." />
                                </div>

                                <div class="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtMarca">Marca</asp:Label>
                                    <asp:TextBox ID="txtMarca" runat="server" MaxLength="50" />
                                </div>

                                <div class="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="ddlUnidadMedida">Unidad de medida</asp:Label>
                                    <asp:DropDownList ID="ddlUnidadMedida" runat="server" />
                                </div>

                                <div class="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtStockMinimo">Stock mínimo</asp:Label>
                                    <asp:TextBox ID="txtStockMinimo" runat="server" MaxLength="12" />
                                    <asp:CompareValidator runat="server" ControlToValidate="txtStockMinimo"
                                        Operator="DataTypeCheck" Type="Currency"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                                        ErrorMessage="El stock mínimo debe ser un número." />
                                </div>

                                <div class="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtPrecioVenta">Precio de venta</asp:Label>
                                    <asp:TextBox ID="txtPrecioVenta" runat="server" MaxLength="12" />
                                    <asp:CompareValidator runat="server" ControlToValidate="txtPrecioVenta"
                                        Operator="DataTypeCheck" Type="Currency"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                                        ErrorMessage="El precio de venta debe ser un número." />
                                </div>

                                <asp:Panel ID="pnlStockInicial" runat="server" CssClass="col-md-6 campo">
                                    <asp:Label runat="server" AssociatedControlID="txtStockInicial">Stock inicial</asp:Label>
                                    <asp:TextBox ID="txtStockInicial" runat="server" MaxLength="12" />
                                    <asp:CompareValidator runat="server" ControlToValidate="txtStockInicial"
                                        Operator="DataTypeCheck" Type="Currency"
                                        CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                                        ErrorMessage="El stock inicial debe ser un número." />
                                </asp:Panel>

                                <asp:Panel ID="pnlStockActual" runat="server" CssClass="col-md-6 campo" Visible="false">
                                    <label>Stock actual</label>
                                    <div class="valor-fijo"><asp:Literal ID="litStockActual" runat="server" /></div>
                                </asp:Panel>
                            </div>
                        </asp:Panel>

                        <%-- Solo editando: el stock se cambia con un ajuste (queda en el kardex con
                             motivo y usuario), nunca editando el número directo. --%>
                        <asp:Panel ID="pnlAjuste" runat="server" CssClass="col-lg-5" Visible="false" DefaultButton="btnRegistrarAjuste">
                            <div class="seccion-modal">
                                <h3>Ajustar stock</h3>
                                <div class="row campos-formulario">
                                    <div class="col-12 campo">
                                        <asp:Label runat="server" AssociatedControlID="txtCantidadAjuste">Cantidad (negativo resta stock, positivo suma)</asp:Label>
                                        <asp:TextBox ID="txtCantidadAjuste" runat="server" MaxLength="12" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCantidadAjuste"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="La cantidad es obligatoria." />
                                        <asp:CompareValidator runat="server" ControlToValidate="txtCantidadAjuste"
                                            Operator="NotEqual" ValueToCompare="0" Type="Currency"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="La cantidad no puede ser cero." />
                                    </div>

                                    <div class="col-12 campo">
                                        <asp:Label runat="server" AssociatedControlID="txtMotivoAjuste">Motivo</asp:Label>
                                        <asp:TextBox ID="txtMotivoAjuste" runat="server" MaxLength="300" TextMode="MultiLine" Rows="2" />
                                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMotivoAjuste"
                                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                            ErrorMessage="El motivo es obligatorio." />
                                    </div>
                                </div>

                                <asp:Button ID="btnRegistrarAjuste" runat="server" CssClass="boton-rojo" Text="Registrar ajuste"
                                    OnClick="btnRegistrarAjuste_Click" ValidationGroup="Ajuste" />
                            </div>
                        </asp:Panel>
                    </div>

                    <asp:Panel ID="pnlHistorial" runat="server" Visible="false">
                        <div class="separador-modal"></div>
                        <h3>Historial de movimientos</h3>
                        <asp:GridView ID="gvHistorial" runat="server" data-filas-por-pagina="10"
                            CssClass="tabla-abm tabla-compacta"
                            AutoGenerateColumns="false" GridLines="None"
                            EmptyDataText="Todavía no hay movimientos registrados.">
                            <Columns>
                                <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                                <asp:BoundField DataField="TipoMovimiento" HeaderText="Tipo" />
                                <asp:BoundField DataField="Entrada" HeaderText="Entrada" DataFormatString="{0:N2}" />
                                <asp:BoundField DataField="Salida" HeaderText="Salida" DataFormatString="{0:N2}" />
                                <asp:BoundField DataField="StockResultante" HeaderText="Stock" DataFormatString="{0:N2}" />
                                <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
                                <asp:BoundField DataField="Descripcion" HeaderText="Motivo" />
                            </Columns>
                        </asp:GridView>
                    </asp:Panel>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnBorrar" runat="server" CssClass="boton-borde-rojo" Text="Borrar" Visible="false"
                            OnClick="btnBorrar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Borrar este insumo?');" />
                        <asp:Button ID="btnReactivar" runat="server" CssClass="boton-gris" Text="Reactivar" Visible="false"
                            OnClick="btnReactivar_Click" CausesValidation="false" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cancelar</button>
                    <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
                        OnClick="btnGuardar_Click" ValidationGroup="Insumo" />
                </div>
            </asp:Panel>
        </asp:Panel>
    </div>

    </div>
</asp:Content>
