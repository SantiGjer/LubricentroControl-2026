<%@ Page Title="Ventas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Ventas.aspx.cs" Inherits="LubricentroControl_2026.Ventas" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Ventas</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <p class="texto-nota"><small>La venta se genera automáticamente al cerrar una orden de trabajo
        (pantalla <a href="~/OrdenesDeTrabajo" runat="server">Órdenes de trabajo</a>). Desde acá se consulta y se factura.</small></p>

    <div class="barra-herramientas">
        <input type="search" id="filtroVentas" class="filtro-tabla-texto"
            placeholder="Buscar por número, factura, cliente, documento, patente o fecha" aria-label="Buscar ventas" />
    </div>

    <div class="opciones-tabla" id="opcionesVentas">
        <div class="grupo-opciones" data-atributo="saldo">
            <span class="titulo-opciones">Saldo</span>
            <button type="button" class="opcion" data-valor="">Todas</button>
            <button type="button" class="opcion" data-valor="pendiente">Con saldo pendiente</button>
            <button type="button" class="opcion" data-valor="saldada">Saldadas</button>
        </div>
        <div class="grupo-opciones" data-atributo="factura">
            <span class="titulo-opciones">Factura</span>
            <button type="button" class="opcion" data-valor="">Todas</button>
            <button type="button" class="opcion" data-valor="si">Facturadas</button>
            <button type="button" class="opcion" data-valor="no">Sin facturar</button>
        </div>
    </div>

    <asp:GridView ID="gvVentas" runat="server" CssClass="tabla-abm" data-filtro="filtroVentas"
        data-opciones-tabla="opcionesVentas"
        AutoGenerateColumns="false" DataKeyNames="IdVenta" GridLines="None"
        OnRowCommand="gvVentas_RowCommand" OnRowDataBound="gvVentas_RowDataBound"
        EmptyDataText="Todavía no hay ventas.">
        <Columns>
            <asp:BoundField DataField="NumeroComprobante" HeaderText="Número" ItemStyle-CssClass="celda-titulo" />
            <asp:BoundField DataField="Fecha" HeaderText="Fecha" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:BoundField DataField="NombreCliente" HeaderText="Cliente" />
            <asp:BoundField DataField="DocumentoCliente" HeaderText="Documento" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Patente" HeaderText="Vehículo" />
            <asp:BoundField DataField="Subtotal" HeaderText="Neto" DataFormatString="{0:N2}" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Impuestos" HeaderText="IVA" DataFormatString="{0:N2}" HeaderStyle-CssClass="oculta" ItemStyle-CssClass="oculta" />
            <asp:BoundField DataField="Total" HeaderText="Total" DataFormatString="{0:N2}" />
            <asp:BoundField DataField="SaldoPendiente" HeaderText="Saldo pendiente" DataFormatString="{0:N2}" />
            <asp:TemplateField HeaderText="Factura">
                <ItemTemplate><%#: Eval("NumeroFactura") ?? "—" %></ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Acciones" HeaderStyle-CssClass="sin-orden">
                <ItemTemplate>
                    <asp:LinkButton runat="server" CssClass="accion-ver"
                        CommandName="Ver" CommandArgument='<%# Eval("IdVenta") %>'
                        CausesValidation="false">Ver</asp:LinkButton>
                    <asp:LinkButton runat="server" Visible='<%# (bool)Eval("Facturada") %>'
                        CommandName="VerFactura" CommandArgument='<%# Eval("IdVenta") %>'
                        CausesValidation="false">Factura</asp:LinkButton>
                    <asp:LinkButton runat="server" Visible='<%# !(bool)Eval("Facturada") && PuedeEscribir %>'
                        CommandName="Facturar" CommandArgument='<%# Eval("IdVenta") %>'
                        CausesValidation="false"
                        OnClientClick="return confirm('¿Emitir la factura de esta venta? Toma el número siguiente y después no se puede anular.');">Facturar</asp:LinkButton>
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
    </asp:GridView>

    <%-- Detalle de una venta, con el IVA de cada línea y su factura (o el botón para generarla). --%>
    <div class="modal fade" id="modalVenta" tabindex="-1" aria-labelledby="tituloModalVenta" aria-hidden="true">
        <div class="modal-dialog modal-xl">
            <div class="modal-content">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalVenta">
                        <asp:Literal ID="litTituloDetalle" runat="server" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <asp:HiddenField ID="hdnIdVenta" runat="server" />

                    <dl class="datos-resumen datos-detalle">
                        <dt>Cliente</dt><dd><asp:Literal ID="litClienteInfo" runat="server" /></dd>
                        <dt>Vehículo</dt><dd><asp:Literal ID="litVehiculoInfo" runat="server" /></dd>
                        <dt>Fecha</dt><dd><asp:Literal ID="litFechaInfo" runat="server" /></dd>
                        <dt>Orden de trabajo</dt><dd><asp:Literal ID="litOrdenInfo" runat="server" /></dd>
                        <dt>Neto</dt><dd><asp:Literal ID="litSubtotalInfo" runat="server" /></dd>
                        <dt>IVA contenido</dt><dd><asp:Literal ID="litImpuestosInfo" runat="server" /></dd>
                        <dt>Total</dt><dd><asp:Literal ID="litTotalInfo" runat="server" /></dd>
                        <dt>Saldo pendiente</dt><dd><asp:Literal ID="litSaldoInfo" runat="server" /></dd>
                        <dt>Factura</dt><dd><asp:Literal ID="litFacturaInfo" runat="server" /></dd>
                    </dl>

                    <h3 class="mt-4">Líneas</h3>
                    <asp:GridView ID="gvDetalleVenta" runat="server" data-sin-filtro="true"
                        CssClass="tabla-abm tabla-compacta"
                        AutoGenerateColumns="false" GridLines="None"
                        EmptyDataText="Esta venta no tiene líneas.">
                        <Columns>
                            <asp:BoundField DataField="Descripcion" HeaderText="Ítem" />
                            <asp:BoundField DataField="Cantidad" HeaderText="Cantidad" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="PrecioUnitario" HeaderText="Precio unitario" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="IvaDescripcion" HeaderText="IVA" />
                            <asp:BoundField DataField="ImporteIva" HeaderText="IVA contenido" DataFormatString="{0:N2}" />
                            <asp:BoundField DataField="Subtotal" HeaderText="Subtotal" DataFormatString="{0:N2}" />
                        </Columns>
                    </asp:GridView>
                </div>

                <div class="modal-footer">
                    <span class="acciones-secundarias">
                        <asp:Button ID="btnVerFactura" runat="server" CssClass="boton-rojo" Text="Ver factura"
                            OnClick="btnVerFactura_Click" CausesValidation="false" />
                        <asp:Button ID="btnFacturar" runat="server" CssClass="boton-rojo" Text="Generar factura"
                            OnClick="btnFacturar_Click" CausesValidation="false"
                            OnClientClick="return confirm('¿Emitir la factura de esta venta? Toma el número siguiente y después no se puede anular.');" />
                    </span>
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cerrar</button>
                </div>
            </div>
        </div>
    </div>

    <%-- La factura, con el formato de una factura argentina. "Imprimir" saca solo la hoja (ver
         @media print en Site.css); desde el diálogo de impresión también se guarda como PDF. --%>
    <div class="modal fade" id="modalFactura" tabindex="-1" aria-labelledby="tituloModalFactura" aria-hidden="true">
        <div class="modal-dialog modal-xl">
            <div class="modal-content">
                <div class="modal-header">
                    <h2 class="modal-title" id="tituloModalFactura">
                        <asp:Literal ID="litTituloFactura" runat="server" /></h2>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal" aria-label="Cerrar"></button>
                </div>

                <div class="modal-body">
                    <div class="factura-hoja">
                        <div class="factura-cabecera">
                            <div class="factura-emisor">
                                <div class="factura-emisor-nombre"><asp:Literal ID="litEmisorNombre" runat="server" /></div>
                                <p><asp:Literal ID="litEmisorDomicilio" runat="server" /></p>
                                <p><b><asp:Literal ID="litEmisorCondicion" runat="server" /></b></p>
                            </div>
                            <div class="factura-letra">
                                <strong><asp:Literal ID="litFacturaLetra" runat="server" /></strong>
                                <span>COD. <asp:Literal ID="litFacturaCodigo" runat="server" /></span>
                            </div>
                            <div class="factura-comprobante">
                                <div class="factura-comprobante-titulo">FACTURA</div>
                                <p><b>N.º</b> <asp:Literal ID="litFacturaNumero" runat="server" /></p>
                                <p><b>Fecha de emisión:</b> <asp:Literal ID="litFacturaFecha" runat="server" /></p>
                                <p><b>CUIT:</b> <asp:Literal ID="litEmisorCuit" runat="server" /></p>
                                <p><b>Ingresos Brutos:</b> <asp:Literal ID="litEmisorIibb" runat="server" /></p>
                                <p><b>Inicio de actividades:</b> <asp:Literal ID="litEmisorInicio" runat="server" /></p>
                            </div>
                        </div>

                        <div class="factura-receptor">
                            <p><b>Cliente:</b> <asp:Literal ID="litReceptorNombre" runat="server" /></p>
                            <p><b><asp:Literal ID="litReceptorDocumento" runat="server" /></b></p>
                            <p><b>Condición frente al IVA:</b> <asp:Literal ID="litReceptorCondicion" runat="server" /></p>
                            <p><b>Domicilio:</b> <asp:Literal ID="litReceptorDomicilio" runat="server" /></p>
                            <p><b>Condición de venta:</b> <asp:Literal ID="litCondicionVenta" runat="server" /></p>
                            <p><b>Referencia:</b> <asp:Literal ID="litReferencia" runat="server" /></p>
                        </div>

                        <asp:Literal ID="litFacturaLineas" runat="server" />

                        <div class="factura-totales">
                            <asp:Literal ID="litFacturaTotales" runat="server" />
                        </div>

                        <asp:Panel ID="pnlTransparencia" runat="server" CssClass="factura-transparencia">
                            <b>Régimen de Transparencia Fiscal al Consumidor (Ley 27.743)</b><br />
                            IVA contenido: $ <asp:Literal ID="litIvaContenido" runat="server" /> ·
                            Otros impuestos nacionales indirectos: $ 0,00
                        </asp:Panel>

                        <div class="factura-leyenda">
                            <p>Comprobante sin validez fiscal: no tiene CAE de ARCA (ex AFIP). Emitido por el sistema de gestión Lubricentro Control.</p>
                        </div>
                    </div>
                </div>

                <div class="modal-footer">
                    <button type="button" class="boton-gris" data-bs-dismiss="modal">Cerrar</button>
                    <button type="button" class="boton-rojo" onclick="window.print();">Imprimir</button>
                </div>
            </div>
        </div>
    </div>

    </div>
</asp:Content>
