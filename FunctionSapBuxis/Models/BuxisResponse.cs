using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static FunctionSAPBuxis.Types.Enums;

namespace FunctionSAPBuxis.Models
{
    [XmlRoot(ElementName = "properties", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata")]
    public class Properties
    {
        [XmlElement(ElementName = "firstname", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string FirstName { get; set; }

        [XmlElement(ElementName = "lastname", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string LastName { get; set; }

        [XmlElement(ElementName = "maritalstatus", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string MaritalStatus { get; set; }

        [XmlElement(ElementName = "nationalid", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string NationalId { get; set; }

        [XmlElement(ElementName = "customString11", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string CustomString11 { get; set; }

        [XmlElement(ElementName = "startDate", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string StartDate { get; set; }

        [XmlElement(ElementName = "compensationinformation", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string CompensationInformation { get; set; }

        [XmlElement(ElementName = "paycompvalue", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string PayCompValueInc { get; set; }
        [XmlElement(ElementName = "paycompvalue", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string PayCompValueBas { get; set; }

        [XmlElement(ElementName = "paycomponent", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string PayComponentBas { get; set; }

        [XmlElement(ElementName = "paycomponent", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string PayComponentInc { get; set; }

        [XmlElement(ElementName = "standardHours", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string standardHours { get; set; }

        [XmlElement(ElementName = "personIdExternal", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string PersonIdExternal { get; set; }
        [XmlElement(ElementName = "company", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string Company { get; set; }
        public StateBuxis StateBuxis { get; set; }
        [XmlElement(ElementName = "codmf", Namespace = "http://schemas.microsoft.com/ado/2007/08/dataservices")]
        public string CodMf { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null || !(obj is Properties))
            {
                return false;
            }

            var other = (Properties)obj;

            return
                FirstName == other.FirstName &&
                LastName == other.LastName &&
                MaritalStatus == other.MaritalStatus &&
                NationalId == other.NationalId &&
                CustomString11 == other.CustomString11 &&
                StartDate == other.StartDate &&
                CompensationInformation == other.CompensationInformation &&
                PayCompValueInc == other.PayCompValueInc &&
                PayCompValueBas == other.PayCompValueBas &&
                PayComponentBas == other.PayComponentBas &&
                PayComponentInc == other.PayComponentInc &&
                standardHours == other.standardHours &&
                PersonIdExternal == other.PersonIdExternal &&
                Company == other.Company;
            // Asegúrate de NO comparar StateBuxis aquí ya que quieres ignorarlo
        }

        // También debes anular GetHashCode() si anulas Equals().
        // Este es un ejemplo simple, pero es posible que desees algo más sofisticado en producción.
        public override int GetHashCode()
        {
            var hash1 = HashCode.Combine(FirstName, LastName, MaritalStatus, NationalId, CustomString11, StartDate, CompensationInformation, PayCompValueInc);
            var hash2 = HashCode.Combine(PayCompValueBas, PayComponentBas, PayComponentInc, standardHours, PersonIdExternal, Company);
            return HashCode.Combine(hash1, hash2);
        }
    }
}