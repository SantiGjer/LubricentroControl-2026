using System.Collections.Generic;
using System.Data;
using BIZ.Modelo;

namespace BIZ.Data
{
    public static class NivelDAL
    {
        public static List<Nivel> Listar()
        {
            const string sql = "SELECT idNivel, nombre, jerarquia FROM Nivel ORDER BY jerarquia";

            var lista = new List<Nivel>();
            foreach (DataRow fila in AccesoDatos.Consultar(sql).Rows)
            {
                lista.Add(new Nivel
                {
                    IdNivel = AccesoDatos.LeerInt(fila, "idNivel"),
                    Nombre = AccesoDatos.LeerString(fila, "nombre"),
                    Jerarquia = AccesoDatos.LeerInt(fila, "jerarquia")
                });
            }
            return lista;
        }
    }
}
