namespace BIZ.Modelo
{
    // Rol de usuario. Los permisos de cada rol se editan desde la pantalla de Roles
    // (Docs/Lubricentro_Requerimientos.md §9.11); acá solo hay dos ids fijos.
    public class Nivel
    {
        // Ids que carga 02_DatosIniciales.sql y que el código necesita conocer.
        // Admin tiene siempre acceso completo a todo: su rol no se edita ni se borra, para que
        // nadie pueda dejar el sistema sin quien administre los permisos.
        public const int Admin = 1;

        // Rol de las cuentas creadas desde ~/Registro (UsuarioDAL.Registrar): no se puede borrar.
        public const int Lectura = 4;

        public const int LargoMaximoNombre = 50;

        public int IdNivel { get; set; }
        public string Nombre { get; set; }

        // Orden en que se listan los roles (menor = más arriba).
        public int Jerarquia { get; set; }

        // Para la lista de Roles; no se guardan.
        public int CantidadUsuarios { get; set; }
        public int CantidadPantallas { get; set; }

        public bool EsAdmin
        {
            get { return IdNivel == Admin; }
        }

        public bool SePuedeBorrar
        {
            get { return IdNivel != Admin && IdNivel != Lectura && CantidadUsuarios == 0; }
        }

        public ResultadoOperacion Validar()
        {
            if (string.IsNullOrWhiteSpace(Nombre))
                return ResultadoOperacion.Error("El nombre del rol es obligatorio.");

            Nombre = Nombre.Trim();
            if (Nombre.Length > LargoMaximoNombre)
                return ResultadoOperacion.Error("El nombre del rol no puede tener más de " + LargoMaximoNombre + " caracteres.");

            return ResultadoOperacion.Ok();
        }

        public override string ToString()
        {
            return Nombre;
        }
    }

    // Permiso de un rol sobre una pantalla del menú, para la matriz de la pantalla de Roles.
    public class PermisoPantalla
    {
        public const string SinAcceso = "Sin acceso";
        public const string Consulta = "Consulta";
        public const string Completo = "Completo";

        public static readonly string[] Accesos = { SinAcceso, Consulta, Completo };

        // Inicio es la pantalla a la que lleva el login: todos los roles la ven siempre.
        public const string PathInicio = "~/Default";

        public int IdMenu { get; set; }
        public string Grupo { get; set; }
        public string Pantalla { get; set; }
        public string Path { get; set; }
        public string Acceso { get; set; }

        public bool EsInicio
        {
            get { return Path == PathInicio; }
        }
    }
}
