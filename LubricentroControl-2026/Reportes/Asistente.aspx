<%@ Page Title="Asistente de consultas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Asistente.aspx.cs" Inherits="LubricentroControl_2026.Reportes.Asistente" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="pantalla-abm">

    <h1 class="con-subtitulo">Asistente de consultas</h1>
    <p class="subtitulo-asistente">Modelo de IA: <asp:Literal ID="litModelo" runat="server" Mode="Encode" /></p>

    <p class="text-muted">Hacé una pregunta en tus palabras: sobre el negocio, las ventas, quién te debe, el
        stock o el uso del sistema. El asistente elige la consulta que corresponde y la responde con los
        datos reales del sistema. La primera pregunta tarda más porque la IA carga el modelo.</p>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlPregunta" runat="server" DefaultButton="btnPreguntar">
        <div class="row barra-busqueda">
            <div class="col-12 col-md-9">
                <asp:Label runat="server" AssociatedControlID="txtPregunta">Tu pregunta</asp:Label>
                <br />
                <asp:TextBox ID="txtPregunta" runat="server" MaxLength="300"
                    placeholder="Por ejemplo: ¿qué cliente me debe más? o ¿cuál es el ticket promedio de este mes?" />
            </div>
            <div class="col-12 col-md-3 d-flex align-items-end gap-2">
                <asp:Button ID="btnPreguntar" runat="server" CssClass="boton-rojo" Text="Preguntar"
                    OnClick="btnPreguntar_Click" />
                <%-- Solo con un resultado a la vista: lo muestra Mostrar y lo esconde Limpiar. --%>
                <asp:Button ID="btnLimpiar" runat="server" CssClass="boton-gris" Text="Limpiar"
                    OnClick="btnLimpiar_Click" Visible="false" />
            </div>
        </div>
    </asp:Panel>

    <%-- La respuesta va arriba de los botones: así se ve sin bajar, pregunte con IA o con un botón. --%>
    <asp:Panel ID="pnlRespuesta" runat="server" CssClass="alert alert-secondary" role="alert" Visible="false">
        <asp:Literal ID="litRespuesta" runat="server" Mode="Encode" />
    </asp:Panel>

    <%-- Cada consulta trae sus columnas, ya formateadas (AutoGenerateColumns); el orden y el filtro de
         Lubricentro.js no hacen falta. --%>
    <asp:GridView ID="gvTop" runat="server" CssClass="tabla-abm" data-sin-filtro="true"
        AutoGenerateColumns="true" GridLines="None" Visible="false" />

    <div class="separador-asistente"><span>Consultas rápidas, sin IA (mes actual)</span></div>

    <%-- Un bloque de botones por grupo del catálogo; los dos usan el mismo ItemCommand. --%>
    <p class="titulo-ejemplos">Negocio y uso del sistema</p>
    <div class="grilla-ejemplos">
        <asp:Repeater ID="rptEjemplos" runat="server" OnItemCommand="rptEjemplos_ItemCommand">
            <ItemTemplate>
                <asp:Button runat="server" CssClass="boton-ejemplo" CommandName="Ejemplo"
                    CommandArgument='<%# Eval("Id") %>'
                    Text='<%# ((BIZ.Modelo.ConsultaAsistente)Container.DataItem).Ejemplos[0] %>' />
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <p class="titulo-ejemplos">Ventas y stock</p>
    <div class="grilla-ejemplos">
        <asp:Repeater ID="rptEjemplosVentas" runat="server" OnItemCommand="rptEjemplos_ItemCommand">
            <ItemTemplate>
                <asp:Button runat="server" CssClass="boton-ejemplo" CommandName="Ejemplo"
                    CommandArgument='<%# Eval("Id") %>'
                    Text='<%# ((BIZ.Modelo.ConsultaAsistente)Container.DataItem).Ejemplos[0] %>' />
            </ItemTemplate>
        </asp:Repeater>
    </div>
    </div>
</asp:Content>
