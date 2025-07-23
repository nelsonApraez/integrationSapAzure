using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSAPBuxis.Models
{
    public class RTFResponse
    {
        public string accion { get; set; }
        public string origen { get; set; }
        public string codigo_empresa { get; set; }
        public string codigo_establecimiento { get; set; }
        public string error { get; set; }
        public string fecha_ejecucion { get; set; }
        public List<Documento> documentos { get; set; }
    }
}
