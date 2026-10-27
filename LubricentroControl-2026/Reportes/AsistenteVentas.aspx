<%@ Page Title="Asistente de ventas y stock" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AsistenteVentas.aspx.cs" Inherits="LubricentroControl_2026.Reportes.AsistenteVentas" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="pantalla-abm">

    <h1 class="con-subtitulo">Asistente de ventas y stock</h1>
    <p class="subtitulo-asistente">Modelo de IA: <asp:Literal ID="litModelo" runat="server" Mode="Encode" /></p>

    <p class="text-muted">Preguntá en tus palabras por lo más vendido, quién te debe, el stock o lo que facturó
        cada cajero. El asistente elige la consulta que corresponde y la responde con los datos reales del
        sistema. La primera pregunta tarda más porque la IA carga el modelo.</p>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlPregunta" runat="server" DefaultButton="btnPreguntar">
        <div class="row barra-busqueda">
            <div class="col-9">
                <asp:Label runat="server" AssociatedControlID="txtPregunta">Tu pregunta</asp:Label>
                <br />
                <asp:TextBox ID="txtPregunta" runat="server" MaxLength="300"
                    placeholder="Por ejemplo: ¿qué cliente me debe más?" />
            </div>
            <div class="col-3 d-flex align-items-end gap-2">
                <asp:Button ID="btnPreguntar" runat="server" CssClass="boton-rojo" Text="Preguntar"
                    OnClick="btnPreguntar_Click" />
                <%-- Solo con un resultado a la vista: lo muestra Mostrar y lo esconde Limpiar. --%>
                <asp:Button ID="btnLimpiar" runat="server" CssClass="boton-gris" Text="Limpiar"
                    OnClick="btnLimpiar_Click" Visible="false" />
            </div>
        </div>
    </asp:Panel>

    <div class="separador-asistente"><span>Consultas rápidas, sin IA (ventas del mes actual)</span></div>

    <div class="mb-3">
        <asp:Repeater ID="rptEjemplos" runat="server" OnItemCommand="rptEjemplos_ItemCommand">
            <ItemTemplate>
                <asp:Button runat="server" CssClass="boton-gris boton-ejemplo" CommandName="Ejemplo"
                    CommandArgument='<%# Eval("Id") %>'
                    Text='<%# ((BIZ.Modelo.ConsultaVentasIA)Container.DataItem).Ejemplos[0] %>' />
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <asp:Panel ID="pnlRespuesta" runat="server" CssClass="alert alert-secondary" role="alert" Visible="false">
        <asp:Literal ID="litRespuesta" runat="server" Mode="Encode" />
    </asp:Panel>

    <%-- Cada consulta trae sus columnas, ya formateadas (AutoGenerateColumns); el orden y el filtro de
         Lubricentro.js no hacen falta. --%>
    <asp:GridView ID="gvResultado" runat="server" CssClass="tabla-abm" data-sin-filtro="true"
        AutoGenerateColumns="true" GridLines="None" Visible="false" />
    </div>
</asp:Content>
