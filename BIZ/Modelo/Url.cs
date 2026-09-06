namespace BIZ.Modelo
{
    // Pantalla del sistema. El path va sin extensión porque FriendlyUrls está activo.
    public class Url
    {
        public int IdUrl { get; set; }
        public string Descripcion { get; set; }
        public string Path { get; set; }
    }
}
