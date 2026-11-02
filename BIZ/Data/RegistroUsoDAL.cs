using System;

namespace BIZ.Data
{
    // Registro de uso del sistema: cada inicio de sesión (RegistroSesion) y cada pantalla abierta
    // (VisitaPagina). Lo llena la capa web (SesionUsuario y PaginaConSesion) y lo lee el asistente de
    // consultas (tiempo en sesión, páginas más visitadas y usuarios que más ingresan).
    //
    // Es un registro de apoyo, no una regla de negocio: si la base falla o todavía no tiene las tablas
    // (Actualizar_2026-11-02_RegistroDeUso.sql sin correr), no se registra nada y la pantalla sigue
    // andando. Por eso cada método atrapa cualquier error y no devuelve ResultadoOperacion.
    public static class RegistroUsoDAL
    {
        // Abre el registro de una sesión nueva. Devuelve su id, o null si no se pudo registrar.
        public static int? IniciarSesion(int idUsuario)
        {
            if (idUsuario <= 0) return null;
            try
            {
                var id = AccesoDatos.Escalar(
                    @"INSERT INTO RegistroSesion (idUsuario) VALUES (@idUsuario);
                      SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    AccesoDatos.Param("@idUsuario", idUsuario));
                return id == null ? (int?)null : Convert.ToInt32(id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Cada pedido de una pantalla con sesión: mueve la última actividad de la sesión y, si es una
        // pantalla abierta (no un postback), suma la visita. Un solo viaje a la base.
        public static void RegistrarActividad(int idRegistroSesion, int idUsuario, string ruta, bool esVisita)
        {
            if (idRegistroSesion <= 0) return;
            try
            {
                AccesoDatos.Ejecutar(
                    @"UPDATE RegistroSesion SET ultimaActividad = GETDATE()
                      WHERE idRegistroSesion = @idRegistroSesion AND fin IS NULL;
                      IF @esVisita = 1 AND @@ROWCOUNT = 1
                          INSERT INTO VisitaPagina (idRegistroSesion, idUsuario, path)
                          VALUES (@idRegistroSesion, @idUsuario, @path);",
                    AccesoDatos.Param("@idRegistroSesion", idRegistroSesion),
                    AccesoDatos.Param("@idUsuario", idUsuario),
                    AccesoDatos.Param("@path", Recortar(ruta, 200)),
                    AccesoDatos.Param("@esVisita", esVisita));
            }
            catch (Exception)
            {
            }
        }

        // "Cerrar sesión": marca el fin. Una sesión que vence sola queda sin fin, y su tiempo se cuenta
        // hasta la última actividad.
        public static void CerrarSesion(int idRegistroSesion)
        {
            if (idRegistroSesion <= 0) return;
            try
            {
                AccesoDatos.Ejecutar(
                    @"UPDATE RegistroSesion SET fin = GETDATE(), ultimaActividad = GETDATE()
                      WHERE idRegistroSesion = @idRegistroSesion AND fin IS NULL",
                    AccesoDatos.Param("@idRegistroSesion", idRegistroSesion));
            }
            catch (Exception)
            {
            }
        }

        private static string Recortar(string texto, int largo)
        {
            if (string.IsNullOrEmpty(texto)) return "~/";
            return texto.Length <= largo ? texto : texto.Substring(0, largo);
        }
    }
}
