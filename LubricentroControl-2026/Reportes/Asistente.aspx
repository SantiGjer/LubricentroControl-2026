<%@ Page Title="Asistente de consultas" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Asistente.aspx.cs" Inherits="LubricentroControl_2026.Reportes.Asistente" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <div class="pantalla-abm">

    <h1>Asistente de consultas</h1>

    <p class="text-muted">Hacé una pregunta sobre el negocio en tus palabras. El asistente elige la
        consulta que corresponde y la responde con los datos reales del sistema.</p>

    <asp:Panel ID="pnlMensaje" runat="server" Visible="false" role="alert">
        <asp:Literal ID="litMensaje" runat="server" />
    </asp:Panel>

    <asp:Panel ID="pnlPregunta" runat="server" DefaultButton="btnPreguntar">
        <div class="row barra-busqueda">
            <div class="col-9">
                <asp:Label runat="server" AssociatedControlID="txtPregunta">Tu pregunta</asp:Label>
                <br />
                <asp:TextBox ID="txtPregunta" runat="server" MaxLength="300"
                    placeholder="Por ejemplo: ¿cuál es el ticket promedio de este mes?" />
            </div>
            <div class="col-3 d-flex align-items-end">
                <asp:Button ID="btnPreguntar" runat="server" CssClass="boton-rojo" Text="Preguntar"
                    OnClick="btnPreguntar_Click" />
            </div>
        </div>
    </asp:Panel>

    <div class="mt-3 mb-3">
        <span class="text-muted small">Consultas de ejemplo (mes actual, sin usar la IA):</span>
        <asp:Repeater ID="rptEjemplos" runat="server" OnItemCommand="rptEjemplos_ItemCommand">
            <ItemTemplate>
                <asp:Button runat="server" CssClass="boton-gris" CommandName="Ejemplo"
                    CommandArgument='<%# Eval("Id") %>'
                    Text='<%# ((BIZ.Modelo.ConsultaAsistente)Container.DataItem).Ejemplos[0] %>' />
            </ItemTemplate>
        </asp:Repeater>
    </div>

    <asp:Panel ID="pnlRespuesta" runat="server" CssClass="alert alert-secondary" role="alert" Visible="false">
        <asp:Literal ID="litRespuesta" runat="server" Mode="Encode" />
    </asp:Panel>

    <%-- Top 5 de las consultas que devuelven un ranking; el orden y el filtro de Lubricentro.js no hacen falta. --%>
    <asp:GridView ID="gvTop" runat="server" CssClass="tabla-abm" data-sin-filtro="true"
        AutoGenerateColumns="false" GridLines="None" Visible="false">
        <Columns>
            <asp:BoundField DataField="Marca" HeaderText="Marca" />
            <asp:BoundField DataField="Modelo" HeaderText="Modelo" />
            <asp:BoundField DataField="Ordenes" HeaderText="Órdenes"
                ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right" />
        </Columns>
    </asp:GridView>

    <p class="text-muted small mt-3">
        Modelo de IA: <asp:Literal ID="litModelo" runat="server" Mode="Encode" />. Corre en esta PC
        (Ollama); si no está abierto, los botones de ejemplo siguen funcionando.
    </p>
    </div>
</asp:Content>
