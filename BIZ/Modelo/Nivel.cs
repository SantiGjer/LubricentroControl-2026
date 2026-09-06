namespace BIZ.Modelo
{
    // Rol de usuario. Jerarquía: Admin(1) > Encargado(2) > Empleado(3).
    public class Nivel
    {
        // Ids fijos que carga 02_DatosIniciales.sql.
        public const int Admin = 1;
        public const int Encargado = 2;
        public const int Empleado = 3;

        public int IdNivel { get; set; }
        public string Nombre { get; set; }

        // Menor número = más permisos.
        public int Jerarquia { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }
}
