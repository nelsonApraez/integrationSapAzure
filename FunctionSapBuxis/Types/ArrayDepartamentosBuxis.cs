using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FunctionSAPBuxis.Types
{
    public class ArrayDepartamentosBuxis
    {
        public string ListDepartaments(int customstring)
        {
            Dictionary<int, string> myArray = new Dictionary<int, string>()
            {
                {428, "01"},
                {429, "02"},
                {430, "03"},
                {431, "04"},
                {432, "05"},
                {433, "06"},
                {434, "07"},
                {435, "08"},
                {436, "09"},
                {437, "10"},
                {438, "11"},
                {439, "12"},
                {440, "13"},
                {441, "14"},
                {442, "15"},
                {443, "16"},
                {444, "17"},
                {445, "18"},
                {446, "19"},
                {447, "20"},
                {448, "21"},
                {449, "22"},
            };
            if (myArray.TryGetValue(customstring, out string value))
            {
                return myArray[customstring];
            }
            else
            {
                return customstring.ToString();
            }
        }
        public string ListMaritalStatus(int maritalNum)
        {
            var mySecondArray = new Dictionary<int, string>()
             {
               {10888, "V"},
               {10886, "D"},
               {10885, "C"},
               {10884, "S"},
               {10887, "U"},
            };

            if (mySecondArray.TryGetValue(maritalNum, out string value))
            {
                return mySecondArray[maritalNum];
            }
            else
            {
                return maritalNum.ToString();
            }
        }
    }
}
