using System;

namespace BIZ.Modelo
{
    // Línea de una compra a proveedor. A diferencia de DetalleOrdenServicio/DetalleOrdenInsumo,
    // precioUnitario NO es un snapshot de un precio de catálogo — es el precio de costo real de
    // la factura del proveedor, que no tiene ningún campo equivalente en Insumo (que solo guarda
    // precioVenta, el precio al que vendemos, no al que compramos). Lo tipea el operador a mano.
    // [Serializable]: Compras.aspx guarda la lista de líneas todavía no persistidas en
    // ViewState mientras se arma la compra (primer uso de este patrón en el proyecto).
    [Serializable]
    public class DetalleCompra
    {
        public int IdDetalle { get; set; }
        public int IdCompra { get; set; }
        public int IdInsumo { get; set; }
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        // Del JOIN con Insumo; solo para mostrar en la grilla, no se guarda.
        public string NombreInsumo { get; set; }
    }
}
