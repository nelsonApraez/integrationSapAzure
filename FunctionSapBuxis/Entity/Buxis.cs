using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace FunctionSAPBuxis.Entity
{
    public class Buxis
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string MaritalStatus { get; set; }
        public string NationalId { get; set; }
        public string CustomString11 { get; set; }
        public string StartDate { get; set; }
        public string CompensationInformation { get; set; }
        public string PayCompValue { get; set; }
        public string PayComponent { get; set; }
        public string JobInformation { get; set; }
        public string StandardHour { get; set; }
        public string personIdExternal { get; set; }
    }
}
