using System;
using Newtonsoft.Json;

namespace FunctionSAPBuxis.Models
{
    public class Detalle
    {
        [JsonIgnore]
        public string nit_emisor { get; set; }
        [JsonIgnore]
        public string numero_establecimiento { get; set; }
        [JsonIgnore]
        public string fecha_factura { get; set; }
        [JsonIgnore]
        public Decimal autorizacion_numero { get; set; }
        [JsonIgnore]
        public string autorizacion_serie { get; set; }        
        public int numero_linea { get; set; }
        public string codigo_producto { get; set; }
        public string descripcion_item { get; set; }
        public int medida { get; set; }
        public Decimal cantidad { get; set; }
        public Decimal precio_unitario { get; set; }
        public Decimal precio { get; set; }
        public Decimal monto_descuento { get; set; }
        public Decimal monto_exento { get; set; }
        public Decimal total { get; set; }

    }
}
