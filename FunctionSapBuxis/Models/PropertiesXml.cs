using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static FunctionSAPBuxis.Types.Enums;
using System.Xml.Serialization;

namespace FunctionSAPBuxis.Models
{
    public class PropertiesXml
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MaritalStatus { get; set; }
        public string NationalId { get; set; }
        public string CustomString11 { get; set; }
        public string StartDate { get; set; }
        public string CompensationInformation { get; set; }
        public string PayCompValueInc { get; set; }
        public string PayCompValueBas { get; set; }
        public string PayComponentBas { get; set; }
        public string PayComponentInc { get; set; }
        public string StandardHours { get; set; }
        public string PersonIdExternal { get; set; }
        public string Company { get; set; }
        public string StateBuxis { get; set; }
        public string CodeEmployee { get; set; }
    }
}
