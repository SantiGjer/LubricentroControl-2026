<%@ Page Title="Error" Language="C#" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="LubricentroControl_2026.PaginaError" %>

<%-- Página a la que manda customErrors (Web.config) ante un error no controlado. No usa
     Site.Master a propósito: el maestro arma el menú leyendo la base, y si el error vino de la
     base, la página de error también fallaría. Solo usa los estilos de la tarjeta de ingreso. --%>
<!DOCTYPE html>

<html lang="es">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Error - Lubricentro Control</title>
    <webopt:bundlereference runat="server" path="~/Content/css" />
    <link href="~/favicon.ico" rel="shortcut icon" type="image/x-icon" />
</head>
<body class="sin-menu">
    <div class="login-page">
        <div class="login-card">

            <div class="login-stripe"></div>

            <h1 class="login-nombre">Lubricentro <strong>Control</strong></h1>

            <div class="login-mensaje" role="alert">
                Ocurrió un error inesperado y no se pudo completar lo que pediste.
            </div>

            <p class="login-texto">
                Volvé a intentarlo. Si se repite, avisá a un administrador: el detalle quedó
                registrado en el servidor.
            </p>

            <a runat="server" href="~/Default" class="login-btn d-block text-center text-decoration-none">Volver al inicio</a>

        </div>
    </div>
</body>
</html>
