using BIZ.Data;
using BIZ.Modelo;

namespace LubricentroControl_2026.Seguridad
{
    /// <summary>
    /// Base de todas las pantallas del menú: exige login y además que el rol del
    /// usuario tenga permiso sobre esta pantalla. La comprobación se hace acá y no
    /// solo escondiendo la opción del menú — si no, bastaría escribir la URL a mano.
    /// </summary>
    public class PaginaSegura : PaginaConSesion
    {
        /// <summary>
        /// True cuando el rol ve la pantalla pero no puede modificar
        /// (los "👁️ Solo consulta" de la matriz de permisos).
        /// Las pantallas deben deshabilitar sus acciones de escritura cuando vale true.
        /// </summary>
        protected bool EsSoloLectura { get; private set; }

        protected ItemMenu PermisoActual { get; private set; }

        protected override void VerificarPermisos()
        {
            PermisoActual = MenuDAL.ObtenerPermiso(UsuarioActual.IdNivel, RutaLogica);

            if (PermisoActual == null)
            {
                RedirigirAccesoDenegado();
                return;
            }

            EsSoloLectura = PermisoActual.SoloLectura;
        }
    }
}
