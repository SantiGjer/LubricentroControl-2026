<%@ Page Title="Acceso denegado" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AccesoDenegado.aspx.cs" Inherits="LubricentroControl_2026.AccesoDenegado" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <div class="pantalla-abm">

    <h1>No tenés acceso a esta pantalla</h1>
    <p class="texto-inicio">
        Tu rol (<asp:Literal ID="litNivel" runat="server" />) no tiene permiso sobre esta sección.
        Si creés que es un error, consultá con un administrador.
    </p>
    <div class="texto-nota">
        <a runat="server" href="~/Default" class="boton-rojo enlace-boton">Volver al inicio</a>
    </div>

    </div>
</asp:Content>
