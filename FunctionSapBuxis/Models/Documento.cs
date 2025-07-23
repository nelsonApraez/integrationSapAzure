using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace FunctionSAPBuxis.Models
{
    public class Documento
    {
        [JsonIgnore]
        public Decimal autorizacion_numero { get; set; }
        [JsonIgnore]
        public string autorizacion_serie { get; set; }
        public string codigo_empresa { get; set; }
        public string codigo_establecimiento { get; set; }
        public string serie_administrativa { get; set; }
        public string numero_administrativo { get; set; }
        public DateTime fecha_emision { get; set; }
        public string codigo_canal_venta { get; set; }
        public int caja { get; set; }
        public string serie_fel { get; set; }
        public string numero_fel { get; set; }
        public int numero_contingencia { get; set; }
        public string certificado_fel { get; set; }
        public string numero_identificacion_tributaria { get; set; }
        public Decimal total { get; set; }
        public string estado_factura { get; set; }
        public string tipo_documento { get; set; }
        public string serie_fel_referencia { get; set; }
        public string numero_fel_referencia { get; set; }
        public string certificado_fel_referencia { get; set; }
        public List<Detalle> detalles { get; set; }

    }
}
