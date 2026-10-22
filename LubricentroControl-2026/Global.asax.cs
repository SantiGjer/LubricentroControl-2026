using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Optimization;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;

namespace LubricentroControl_2026
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Código que se ejecuta al iniciar la aplicación
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        // Un error no controlado: customErrors (Web.config) le muestra al usuario ~/Error, sin el
        // detalle, y acá el detalle se agrega a App_Data\Errores.log (fecha, dirección y
        // excepción completa) para quien tenga que revisarlo. Si no se puede escribir el archivo,
        // no se hace nada: un error al registrar el error no puede tapar la página de error.
        void Application_Error(object sender, EventArgs e)
        {
            var error = Server.GetLastError();
            if (error == null) return;

            try
            {
                var carpeta = Server.MapPath("~/App_Data");
                System.IO.Directory.CreateDirectory(carpeta);
                System.IO.File.AppendAllText(System.IO.Path.Combine(carpeta, "Errores.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + Request.HttpMethod + " " + Request.RawUrl +
                    Environment.NewLine + error + Environment.NewLine + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }
    }
}