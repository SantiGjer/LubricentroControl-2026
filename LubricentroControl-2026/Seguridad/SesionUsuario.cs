using System.Web;
using BIZ.Data;
using BIZ.Modelo;

namespace LubricentroControl_2026.Seguridad
{
    // Guarda el usuario logueado en la sesión. Único punto que toca Session["..."].
    // Desde el 2026-11-02 también abre y cierra el registro de uso de la sesión (RegistroUsoDAL),
    // que lee el asistente de consultas.
    public static class SesionUsuario
    {
        private const string ClaveSesion = "UsuarioLogueado";
        private const string ClaveRegistro = "RegistroSesion";

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

        // Id del registro de uso de esta sesión; 0 si no se pudo registrar.
        public static int IdRegistroSesion
        {
            get
            {
                var contexto = HttpContext.Current;
                if (contexto == null || contexto.Session == null) return 0;
                var valor = contexto.Session[ClaveRegistro];
                return valor is int ? (int)valor : 0;
            }
        }

        public static void Iniciar(Usuario usuario)
        {
            // Sesión nueva en cada login: evita fijación de sesión.
            HttpContext.Current.Session.Clear();
            HttpContext.Current.Session[ClaveSesion] = usuario;

            var idRegistro = RegistroUsoDAL.IniciarSesion(usuario.IdUsuario);
            if (idRegistro.HasValue)
                HttpContext.Current.Session[ClaveRegistro] = idRegistro.Value;
        }

        // Refresca los datos en sesión después de editar el propio perfil.
        public static void Actualizar(Usuario usuario)
        {
            HttpContext.Current.Session[ClaveSesion] = usuario;
        }

        // Cada pedido de una pantalla con sesión (lo llama PaginaConSesion): última actividad y, si
        // la pantalla se abre (no es un postback), una visita.
        public static void RegistrarActividad(string ruta, bool esVisita)
        {
            var usuario = Actual;
            if (usuario == null) return;
            RegistroUsoDAL.RegistrarActividad(IdRegistroSesion, usuario.IdUsuario, ruta, esVisita);
        }

        public static void Cerrar()
        {
            var contexto = HttpContext.Current;
            if (contexto == null || contexto.Session == null) return;

            RegistroUsoDAL.CerrarSesion(IdRegistroSesion);

            contexto.Session.Clear();
            contexto.Session.Abandon();
        }
    }
}
