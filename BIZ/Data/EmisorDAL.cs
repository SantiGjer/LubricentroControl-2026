using BIZ.Modelo;

namespace BIZ.Data
{
    // Datos del comercio que factura (tabla Emisor, de una sola fila: idEmisor = 1). Los edita la
    // pantalla Datos del comercio y los lee FacturaDAL.Emitir, que los copia en cada factura.
    public static class EmisorDAL
    {
        // Null si todavía no se cargaron (02_DatosIniciales.sql deja unos de ejemplo).
        public static DatosEmisor Obtener()
        {
            var tabla = AccesoDatos.Consultar(@"
                SELECT razonSocial, cuit, condicionIva, domicilio, ingresosBrutos, inicioActividades, puntoVenta
                FROM Emisor
                WHERE idEmisor = 1");

            if (tabla.Rows.Count == 0) return null;

            var fila = tabla.Rows[0];
            return new DatosEmisor
            {
                RazonSocial = AccesoDatos.LeerString(fila, "razonSocial"),
                Cuit = AccesoDatos.LeerString(fila, "cuit"),
                CondicionIva = AccesoDatos.LeerString(fila, "condicionIva"),
                Domicilio = AccesoDatos.LeerString(fila, "domicilio"),
                IngresosBrutos = AccesoDatos.LeerString(fila, "ingresosBrutos"),
                InicioActividades = AccesoDatos.LeerFechaNullable(fila, "inicioActividades"),
                PuntoVenta = AccesoDatos.LeerInt(fila, "puntoVenta")
            };
        }

        // Actualiza la fila, o la crea si todavía no existía.
        public static ResultadoOperacion Guardar(DatosEmisor emisor)
        {
            var validacion = emisor.Validar();
            if (!validacion.Exito) return validacion;

            const string sql = @"
                UPDATE Emisor
                SET razonSocial = @razonSocial, cuit = @cuit, condicionIva = @condicionIva,
                    domicilio = @domicilio, ingresosBrutos = @ingresosBrutos,
                    inicioActividades = @inicioActividades, puntoVenta = @puntoVenta
                WHERE idEmisor = 1;

                IF @@ROWCOUNT = 0
                    INSERT INTO Emisor (idEmisor, razonSocial, cuit, condicionIva, domicilio, ingresosBrutos,
                                        inicioActividades, puntoVenta)
                    VALUES (1, @razonSocial, @cuit, @condicionIva, @domicilio, @ingresosBrutos,
                            @inicioActividades, @puntoVenta);";

            AccesoDatos.Ejecutar(sql,
                AccesoDatos.Param("@razonSocial", emisor.RazonSocial),
                AccesoDatos.Param("@cuit", emisor.Cuit),
                AccesoDatos.Param("@condicionIva", emisor.CondicionIva),
                AccesoDatos.Param("@domicilio", emisor.Domicilio),
                AccesoDatos.Param("@ingresosBrutos", emisor.IngresosBrutos),
                AccesoDatos.Param("@inicioActividades", emisor.InicioActividades),
                AccesoDatos.Param("@puntoVenta", emisor.PuntoVenta));

            return ResultadoOperacion.Ok("Se guardaron los datos del comercio. Valen para las próximas facturas.");
        }
    }
}
