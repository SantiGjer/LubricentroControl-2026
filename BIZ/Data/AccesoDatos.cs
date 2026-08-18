using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace BIZ.Data
{
    /// <summary>
    /// Helper de acceso a datos con ADO.NET puro. Toda la capa Data pasa por acá:
    /// centraliza la cadena de conexión y garantiza que el SQL vaya parametrizado.
    /// No usar concatenación de strings para armar consultas.
    /// </summary>
    public static class AccesoDatos
    {
        public const string NombreConexion = "LubricentroDB";

        public static string CadenaConexion
        {
            get
            {
                var cfg = ConfigurationManager.ConnectionStrings[NombreConexion];
                if (cfg == null)
                    throw new ConfigurationErrorsException(
                        "Falta la cadena de conexión '" + NombreConexion + "' en Web.config.");
                return cfg.ConnectionString;
            }
        }

        public static SqlConnection AbrirConexion()
        {
            var cn = new SqlConnection(CadenaConexion);
            cn.Open();
            return cn;
        }

        /// <summary>Arma un parámetro traduciendo null a DBNull.</summary>
        public static SqlParameter Param(string nombre, object valor)
        {
            return new SqlParameter(nombre, valor ?? DBNull.Value);
        }

        /// <summary>Ejecuta un SELECT y devuelve el resultado en memoria.</summary>
        public static DataTable Consultar(string sql, params SqlParameter[] parametros)
        {
            using (var cn = new SqlConnection(CadenaConexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (parametros != null) cmd.Parameters.AddRange(parametros);
                var tabla = new DataTable();
                using (var da = new SqlDataAdapter(cmd))
                {
                    da.Fill(tabla);
                }
                return tabla;
            }
        }

        /// <summary>Ejecuta un INSERT/UPDATE/DELETE y devuelve las filas afectadas.</summary>
        public static int Ejecutar(string sql, params SqlParameter[] parametros)
        {
            using (var cn = new SqlConnection(CadenaConexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (parametros != null) cmd.Parameters.AddRange(parametros);
                cn.Open();
                return cmd.ExecuteNonQuery();
            }
        }

        /// <summary>Ejecuta una consulta que devuelve un único valor.</summary>
        public static object Escalar(string sql, params SqlParameter[] parametros)
        {
            using (var cn = new SqlConnection(CadenaConexion))
            using (var cmd = new SqlCommand(sql, cn))
            {
                if (parametros != null) cmd.Parameters.AddRange(parametros);
                cn.Open();
                var resultado = cmd.ExecuteScalar();
                return resultado == DBNull.Value ? null : resultado;
            }
        }

        /* --- Lectura tolerante de columnas ----------------------------------
           Evitan repetir el chequeo de DBNull en cada mapeo de DataRow.     */

        public static string LeerString(DataRow fila, string columna)
        {
            return fila.IsNull(columna) ? null : Convert.ToString(fila[columna]);
        }

        public static int LeerInt(DataRow fila, string columna)
        {
            return fila.IsNull(columna) ? 0 : Convert.ToInt32(fila[columna]);
        }

        public static int? LeerIntNullable(DataRow fila, string columna)
        {
            return fila.IsNull(columna) ? (int?)null : Convert.ToInt32(fila[columna]);
        }

        public static bool LeerBool(DataRow fila, string columna)
        {
            return !fila.IsNull(columna) && Convert.ToBoolean(fila[columna]);
        }

        public static DateTime LeerFecha(DataRow fila, string columna)
        {
            return fila.IsNull(columna) ? DateTime.MinValue : Convert.ToDateTime(fila[columna]);
        }

        public static DateTime? LeerFechaNullable(DataRow fila, string columna)
        {
            return fila.IsNull(columna) ? (DateTime?)null : Convert.ToDateTime(fila[columna]);
        }

        /// <summary>Prueba la conexión contra la base. Se usa desde la pantalla de diagnóstico.</summary>
        public static bool ProbarConexion(out string mensaje)
        {
            try
            {
                using (var cn = AbrirConexion())
                using (var cmd = new SqlCommand("SELECT @@VERSION", cn))
                {
                    mensaje = Convert.ToString(cmd.ExecuteScalar());
                    return true;
                }
            }
            catch (Exception ex)
            {
                mensaje = ex.Message;
                return false;
            }
        }
    }
}
