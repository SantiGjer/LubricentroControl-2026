<%@ Page Title="Datos del comercio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="DatosComercio.aspx.cs" Inherits="LubricentroControl_2026.DatosComercio" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>Datos del comercio</h1>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <p class="texto-nota"><small>Son los datos con los que el lubricentro figura como emisor en las facturas que se
        generan desde Ventas. Cada factura guarda los datos del momento en que se emitió: un cambio acá vale para las
        próximas, no modifica las ya emitidas.</small></p>

    <asp:Panel ID="pnlFormulario" runat="server" CssClass="formulario-pagina" DefaultButton="btnGuardar">
        <div class="row campos-formulario">
            <div class="col-12"><p class="subtitulo-formulario">Comercio</p></div>

            <div class="col-md-8 campo">
                <asp:Label runat="server" AssociatedControlID="txtRazonSocial">Razón social</asp:Label>
                <asp:TextBox ID="txtRazonSocial" runat="server" MaxLength="150" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtRazonSocial"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Comercio"
                    ErrorMessage="La razón social es obligatoria." />
            </div>

            <div class="col-md-4 campo">
                <asp:Label runat="server" AssociatedControlID="txtCuit">CUIT</asp:Label>
                <asp:TextBox ID="txtCuit" runat="server" MaxLength="13" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtCuit"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Comercio"
                    ErrorMessage="El CUIT es obligatorio." />
                <asp:CustomValidator runat="server" ControlToValidate="txtCuit" OnServerValidate="valCuit_ServerValidate"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Comercio"
                    ErrorMessage="El CUIT debe tener 11 números, con o sin guiones." />
            </div>

            <div class="col-12 campo">
                <asp:Label runat="server" AssociatedControlID="txtDomicilio">Domicilio comercial</asp:Label>
                <asp:TextBox ID="txtDomicilio" runat="server" MaxLength="300" />
            </div>

            <div class="col-12"><p class="subtitulo-formulario">Datos fiscales</p></div>

            <div class="col-md-6 campo">
                <asp:Label runat="server" AssociatedControlID="ddlCondicionIva">Condición frente al IVA</asp:Label>
                <asp:DropDownList ID="ddlCondicionIva" runat="server" ClientIDMode="Static" />
            </div>

            <div class="col-md-6 campo">
                <asp:Label runat="server" AssociatedControlID="txtPuntoVenta">Punto de venta</asp:Label>
                <asp:TextBox ID="txtPuntoVenta" runat="server" TextMode="Number" />
                <asp:RequiredFieldValidator runat="server" ControlToValidate="txtPuntoVenta"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Comercio"
                    ErrorMessage="El punto de venta es obligatorio." />
                <asp:RangeValidator ID="valPuntoVenta" runat="server" ControlToValidate="txtPuntoVenta" Type="Integer"
                    CssClass="text-danger small" Display="Dynamic" ValidationGroup="Comercio" />
            </div>

            <div class="col-12">
                <p class="texto-ayuda" data-mostrar-si="ddlCondicionIva=Responsable Inscripto">
                    Emite factura A a los clientes responsables inscriptos y monotributistas, y B al resto.</p>
                <p class="texto-ayuda" data-mostrar-si="ddlCondicionIva=Monotributista|Exento">
                    Emite siempre factura C.</p>
                <p class="texto-ayuda">Cada letra lleva su propia numeración en cada punto de venta.</p>
            </div>

            <div class="col-md-6 campo">
                <asp:Label runat="server" AssociatedControlID="txtIngresosBrutos">Ingresos Brutos</asp:Label>
                <asp:TextBox ID="txtIngresosBrutos" runat="server" MaxLength="30" />
            </div>

            <div class="col-md-6 campo">
                <asp:Label runat="server" AssociatedControlID="txtInicioActividades">Inicio de actividades</asp:Label>
                <asp:TextBox ID="txtInicioActividades" runat="server" TextMode="Date" />
            </div>
        </div>

        <asp:Button ID="btnGuardar" runat="server" CssClass="boton-rojo" Text="Guardar"
            OnClick="btnGuardar_Click" ValidationGroup="Comercio" />
    </asp:Panel>

    </div>
</asp:Content>
