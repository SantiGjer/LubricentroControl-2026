<%@ Page Title="Cuenta corriente de clientes" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="CuentaCorrienteClientes.aspx.cs" Inherits="LubricentroControl_2026.CuentaCorrienteClientes" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Cuenta corriente de clientes</h1>

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
                    <asp:Label runat="server" AssociatedControlID="txtBuscar">Buscar por nombre, apellido o DNI</asp:Label>
                    <asp:TextBox ID="txtBuscar" runat="server" /></div>
                <div class="col-6">
                    <asp:Button ID="btnBuscar" runat="server" Text="Buscar"
                        OnClick="btnBuscar_Click" CausesValidation="false" /></div>
            </div>

            <asp:GridView ID="gvClientes" runat="server"
                AutoGenerateColumns="false" DataKeyNames="IdCliente" GridLines="None"
                OnRowCommand="gvClientes_RowCommand" EmptyDataText="No hay clientes que coincidan con la búsqueda.">
                <Columns>
                    <asp:BoundField DataField="Apellido" HeaderText="Apellido" />
                    <asp:BoundField DataField="Nombre" HeaderText="Nombre" />
                    <asp:BoundField DataField="Dni" HeaderText="DNI" />
                    <asp:TemplateField HeaderText="Acciones">
                        <ItemTemplate>
                            <asp:LinkButton runat="server"
                                CommandName="Seleccionar" CommandArgument='<%# Eval("IdCliente") %>'
                                CausesValidation="false">Ver cuenta</asp:LinkButton>
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>
        </div>

        <div class="col-5">
            <asp:Panel ID="pnlDetalle" runat="server" Visible="false">
                <h2><asp:Literal ID="litClienteSeleccionado" runat="server" /></h2>
                <p><b>Saldo actual:</b> <asp:Literal ID="litSaldoActual" runat="server" /></p>

                <asp:GridView ID="gvHistorial" runat="server"
                    CssClass="table table-striped table-bordered table-hover"
                    AutoGenerateColumns="false" GridLines="None"
                    EmptyDataText="Todavía no hay movimientos registrados.">
                    <Columns>
                        <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy HH:mm}" />
                        <asp:BoundField DataField="TipoMovimiento" HeaderText="Tipo" />
                        <asp:BoundField DataField="Debe" HeaderText="Debe" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="Haber" HeaderText="Haber" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="Saldo" HeaderText="Saldo" DataFormatString="{0:N2}" />
                        <asp:BoundField DataField="Descripcion" HeaderText="Descripción" />
                        <asp:BoundField DataField="NombreUsuario" HeaderText="Usuario" />
                    </Columns>
                </asp:GridView>

                <asp:Panel ID="pnlAjuste" runat="server">
                    <hr />
                    <h3 class="h6">Registrar ajuste</h3>

                    <div class="row border border-1">
                        <div class="col-12">
                            <asp:Label runat="server" AssociatedControlID="txtMontoAjuste">Monto (negativo reduce la deuda, positivo la aumenta)</asp:Label></div>
                    </div>

                    <div class="row border border-1">
                        <div class="col-12">
                            <asp:TextBox ID="txtMontoAjuste" runat="server" MaxLength="12" />
                            <asp:RequiredFieldValidator runat="server" ControlToValidate="txtMontoAjuste"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                ErrorMessage="El monto es obligatorio." />
                            <asp:CompareValidator runat="server" ControlToValidate="txtMontoAjuste"
                                Operator="NotEqual" ValueToCompare="0" Type="Currency"
                                CssClass="text-danger small" Display="Dynamic" ValidationGroup="Ajuste"
                                ErrorMessage="El monto no puede ser cero." /></div>
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
                </asp:Panel>
            </asp:Panel>
        </div>
    </div>
</asp:Content>
