namespace BIZ.Modelo
{
    /// <summary>Rol de usuario. Jerarquía: Admin(1) > Encargado(2) > Empleado(3).</summary>
    public class Nivel
    {
        /// <summary>Ids fijos que carga 02_DatosIniciales.sql.</summary>
        public const int Admin = 1;
        public const int Encargado = 2;
        public const int Empleado = 3;

        public int IdNivel { get; set; }
        public string Nombre { get; set; }

        /// <summary>Menor número = más permisos.</summary>
        public int Jerarquia { get; set; }

        public override string ToString()
        {
            return Nombre;
        }
    }
}
