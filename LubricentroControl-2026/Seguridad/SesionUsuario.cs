using System.Web;
using BIZ.Modelo;

namespace LubricentroControl_2026.Seguridad
{
    /// <summary>Guarda el usuario logueado en la sesión. Único punto que toca Session["..."].</summary>
    public static class SesionUsuario
    {
        private const string ClaveSesion = "UsuarioLogueado";

        public static Usuario Actual
        {
            get
            {
                var contexto = HttpContext.Current;
                if (contexto == null || contexto.Session == null) return null;
                return contexto.Session[ClaveSesion] as Usuario;
            }
        }

        public static bool HayUsuario
        {
            get { return Actual != null; }
        }

        public static void Iniciar(Usuario usuario)
        {
            // Sesión nueva en cada login: evita fijación de sesión.
            HttpContext.Current.Session.Clear();
            HttpContext.Current.Session[ClaveSesion] = usuario;
        }

        /// <summary>Refresca los datos en sesión después de editar el propio perfil.</summary>
        public static void Actualizar(Usuario usuario)
        {
            HttpContext.Current.Session[ClaveSesion] = usuario;
        }

        public static void Cerrar()
        {
            var contexto = HttpContext.Current;
            if (contexto == null || contexto.Session == null) return;

            contexto.Session.Clear();
            contexto.Session.Abandon();
        }
    }
}
