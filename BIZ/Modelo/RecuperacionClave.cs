using System;

namespace BIZ.Modelo
{
    /// <summary>Token de recuperación de contraseña: de un solo uso y con vencimiento.</summary>
    public class RecuperacionClave
    {
        public int IdRecuperacion { get; set; }
        public int IdUsuario { get; set; }
        public string Token { get; set; }
        public DateTime FechaSolicitud { get; set; }
        public DateTime FechaVencimiento { get; set; }
        public bool Usado { get; set; }
        public DateTime? FechaUso { get; set; }

        public bool EstaVigente
        {
            get { return !Usado && FechaVencimiento > DateTime.Now; }
        }
    }
}
