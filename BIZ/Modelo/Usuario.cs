using System;

namespace BIZ.Modelo
{
    public class Usuario
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; }
        public string Apellido { get; set; }
        public string Email { get; set; }

        /// <summary>Hash PBKDF2 en Base64. Nunca se muestra ni se loguea.</summary>
        public string PasswordHash { get; set; }

        /// <summary>Salt aleatorio en Base64, propio de este usuario.</summary>
        public string PasswordSalt { get; set; }

        public int IdNivel { get; set; }
        public string NombreNivel { get; set; }
        public bool Activo { get; set; }
        public DateTime FechaAlta { get; set; }

        public string NombreCompleto
        {
            get { return (Nombre + " " + Apellido).Trim(); }
        }

        /// <summary>True si el usuario tiene al menos el nivel pedido (jerarquía menor o igual).</summary>
        public bool TieneNivelMinimo(int idNivelRequerido)
        {
            return IdNivel <= idNivelRequerido;
        }

        public bool EsAdmin
        {
            get { return IdNivel == Nivel.Admin; }
        }
    }
}
