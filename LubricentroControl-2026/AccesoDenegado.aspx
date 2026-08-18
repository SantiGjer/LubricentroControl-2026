<%@ Page Title="Acceso denegado" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="AccesoDenegado.aspx.cs" Inherits="LubricentroControl_2026.AccesoDenegado" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">

    <h1>No tenés acceso a esta pantalla</h1>
    <p>
        Tu rol (<asp:Literal ID="litNivel" runat="server" />) no tiene permiso sobre esta sección.
        Si creés que es un error, consultá con un administrador.
    </p>
    <a runat="server" href="~/Default">Volver al inicio</a>
</asp:Content>
