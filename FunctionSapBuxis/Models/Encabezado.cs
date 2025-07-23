using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FunctionSAPBuxis.Models
{
    public class Encabezado
    {
        public string Identificador { get; set; }
        public string OrdenId { get; set; }
        public string Proveedor { get; set; }
        public string Emision { get; set; }
        public string Cancelacion { get; set; }
        public string Entrega { get; set; }
        public string TipoOrden { get; set; }
        public string Departamento { get; set; }
        public string Condiciones { get; set; }
        public string Promocion { get; set; }
        public string Rebaja1 { get; set; }
        public string Rebaja2 { get; set; }
        public string Moneda { get; set; }
        public string Pais { get; set; }
        public string GlnCliente { get; set; }
        public string Direccion { get; set; }
        public string Cliente1 { get; set; }
        public string Cliente2 { get; set; }
        public string Campo1 { get; set; }
        public string GlnDespacho { get; set; }
        public string LugarEntrega { get; set; }
        public string DireccionEntrega { get; set; }
        public string Lineas { get; set; }
        public string TotalUnidades { get; set; }
        public string MontoTotal { get; set; }
        public List<DetalleOrden> Detalles { get; set; }
    }

    public class DetalleOrden
    {
        public string Identificador { get; set; }
        public string OrdenId { get; set; }
        public string Proveedor { get; set; }
        public string Descripcion { get; set; }
        public string GTIN { get; set; }
        public string EAN { get; set; }
        public string Cantidad { get; set; }
        public string Paquetes { get; set; }
        public string PrecioUnitario { get; set; }
        public string MontoParcial { get; set; }
        public string Posicion { get; set; }
    }
}
