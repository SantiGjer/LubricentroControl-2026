<%@ Page Title="Insumos" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Insumos.aspx.cs" Inherits="LubricentroControl_2026.Insumos" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Insumos</h1>

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
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por nombre o marca</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" />
                    <asp:CheckBox ID="chkIncluirInactivos" runat="server" AutoPostBack="true"
                        OnCheckedChanged="chkIncluirInactivos_CheckedChanged" />
                    <asp:Label runat="server" AssociatedControlID="chkIncluirInactivos">Incluir inactivos</asp:Label></div>
            </div>

            <p><small>Las filas resaltadas tienen el stock por debajo del mínimo.</small></p>

            <asp:GridView ID="gvInsumos" runat="server"
                CssClass="table table-striped table-bordered table-hover"
                AutoGenerateColumns="false" DataKeyNames="IdInsumo" GridLines="None"
                AllowPaging="true" PageSize="30"
                OnRowCommand="gvInsumos_RowCommand" OnRowDataBound="gvInsumos_RowDataBound"
                OnPageIndexChanging="gvInsumos_PageIndexChanging"
                EmptyDataText="No hay insumos que coincidan con la búsqueda.">
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
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdInsumo") %>'
                                CausesValidation="false">Seleccionar</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
                <PagerStyle HorizontalAlign="Center" />
                <PagerTemplate>
                    <asp:LinkButton runat="server" CommandName="Page" CommandArgument="Prev" CausesValidation="false"
                        Text="◄ Anterior"
                        Visible='<%# ((GridView)Container.NamingContainer).PageIndex > 0 %>' />
                    &nbsp;Página <%# ((GridView)Container.NamingContainer).PageIndex + 1 %>
                    de <%# ((GridView)Container.NamingContainer).PageCount %>&nbsp;
                    <asp:LinkButton runat="server" CommandName="Page" CommandArgument="Next" CausesValidation="false"
                        Text="Siguiente ►"
                        Visible='<%# ((GridView)Container.NamingContainer).PageIndex < ((GridView)Container.NamingContainer).PageCount - 1 %>' />
                </PagerTemplate>
            </asp:GridView>
        </div>

        <div class="col-5">
            <asp:Panel ID="pnlFormulario" runat="server">
                <h2><asp:Literal ID="litTituloFormulario" runat="server" Text="Nuevo insumo" /></h2>
                <asp:HiddenField ID="hdnIdInsumo" runat="server" />
                <asp:HiddenField ID="hdnActivo" runat="server" Value="True" />

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtNombre">Nombre</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtNombre" runat="server" MaxLength="100" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtNombre"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                            ErrorMessage="El nombre es obligatorio." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtMarca">Marca</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtMarca" runat="server" MaxLength="50" /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="ddlUnidadMedida">Unidad de medida</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:DropDownList ID="ddlUnidadMedida" runat="server" /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtStockMinimo">Stock mínimo</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtStockMinimo" runat="server" MaxLength="12" />
                        <asp:CompareValidator runat="server" ControlToValidate="txtStockMinimo"
                            Operator="DataTypeCheck" Type="Currency"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                            ErrorMessage="El stock mínimo debe ser un número." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtPrecioVenta">Precio de venta</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtPrecioVenta" runat="server" MaxLength="12" />
                        <asp:CompareValidator runat="server" ControlToValidate="txtPrecioVenta"
                            Operator="DataTypeCheck" Type="Currency"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                            ErrorMessage="El precio de venta debe ser un número." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtStockInicial">Stock inicial</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtStockInicial" runat="server" MaxLength="12" />
                        <asp:CompareValidator runat="server" ControlToValidate="txtStockInicial"
                            Operator="DataTypeCheck" Type="Currency"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Insumo"
                            ErrorMessage="El stock inicial debe ser un número." />
                        <asp:Label ID="litStockActual" runat="server" Visible="false" Font-Bold="true" /></div>
                </div>

                <asp:Button ID="btnGuardar" runat="server" Text="Guardar"
                    OnClick="btnGuardar_Click" ValidationGroup="Insumo" />
                <asp:Button ID="btnNuevo" runat="server" Text="Nuevo insumo"
                    OnClick="btnNuevo_Click" CausesValidation="false" />
                <asp:Button ID="btnBorrar" runat="server" Text="Borrar" Visible="false"
                    OnClick="btnBorrar_Click" CausesValidation="false"
                    OnClientClick="return confirm('¿Borrar este insumo?');" />
            </asp:Panel>
        </div>
    </div>

    <asp:Panel ID="pnlAjusteHistorial" runat="server" Visible="false">
        <hr />
        <div class="row">
            <div class="col-7">
                <h2>Historial de movimientos</h2>
                <asp:GridView ID="gvHistorial" runat="server"
                    CssClass="table table-striped table-bordered table-hover"
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
            </div>

            <div class="col-5">
                <h2>Ajustar stock</h2>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtCantidadAjuste">Cantidad (negativo resta stock, positivo suma stock)</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtCantidadAjuste" runat="server" MaxLength="12" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCantidadAjuste"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                            ErrorMessage="La cantidad es obligatoria." />
                        <asp:CompareValidator runat="server" ControlToValidate="txtCantidadAjuste"
                            Operator="NotEqual" ValueToCompare="0" Type="Currency"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                            ErrorMessage="La cantidad no puede ser cero." /></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:Label runat="server" AssociatedControlID="txtMotivoAjuste">Motivo</asp:Label></div>
                </div>

                <div class="row border border-1">
                    <div class="col-12">
                        <asp:TextBox ID="txtMotivoAjuste" runat="server" MaxLength="300" TextMode="MultiLine" Rows="2" />
                        <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMotivoAjuste"
                            CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                            ErrorMessage="El motivo es obligatorio." /></div>
                </div>

                <asp:Button ID="btnRegistrarAjuste" runat="server" Text="Registrar ajuste"
                    OnClick="btnRegistrarAjuste_Click" ValidationGroup="Ajuste" />
            </div>
        </div>
    </asp:Panel>
</asp:Content>
