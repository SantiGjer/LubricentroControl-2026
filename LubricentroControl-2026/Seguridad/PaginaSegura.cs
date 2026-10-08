using BIZ.Data;
using BIZ.Modelo;

namespace LubricentroControl_2026.Seguridad
{
    // Base de todas las pantallas del menú: exige login y además que el rol del
    // usuario tenga permiso sobre esta pantalla. La comprobación se hace acá y no
    // solo escondiendo la opción del menú — si no, bastaría escribir la URL a mano.
    public class PaginaSegura : PaginaConSesion
    {
        // True cuando el rol ve la pantalla pero no puede modificar
        // (los "👁️ Solo consulta" de la matriz de permisos, que se edita en Roles).
        // Las pantallas deben deshabilitar sus acciones de escritura cuando vale true.
        protected bool EsSoloLectura { get; private set; }

        // Lo contrario, para el markup: Visible='<%# PuedeEscribir %>' en los enlaces de escritura
        // de una grilla (Editar, Borrar…), que sí deja a la vista "Ver" para todos los roles.
        protected bool PuedeEscribir
        {
            get { return !EsSoloLectura; }
        }

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
