<%@ Page Title="Inicio" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Default.aspx.cs" Inherits="LubricentroControl_2026._Default" %>

<asp:Content ID="BodyContent" ContentPlaceHolderID="MainContent" runat="server">

    <h1>Hola, <asp:Literal ID="litNombre" runat="server" /></h1>
    <p>
        Estás trabajando con el rol <b><asp:Literal ID="litNivel" runat="server" /></b>.
        El menú de arriba muestra solo las secciones habilitadas para ese rol.
    </p>

    <h2>Tus accesos</h2>
    <asp:Literal ID="litAccesos" runat="server" />
</asp:Content>
