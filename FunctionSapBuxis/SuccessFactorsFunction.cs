using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Data;
//using System.ComponentModel.DataAnnotations.Schema;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using FunctionSAPBuxis.Models;
using Microsoft.Azure.WebJobs;
using Newtonsoft.Json;
using FunctionSAPBuxis.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos.Table;
using static FunctionSAPBuxis.Types.Enums;
using System.Linq;

namespace FunctionSapBuxis
{
    public static class SuccessFactorsFunction
    {
        private static readonly HttpClient httpClient = new HttpClient();

        [FunctionName("SuccessFactorsFunction")]
        public static async Task Run(
        [TimerTrigger("0 */20 * * * *")] TimerInfo myTimer,
        [Table("Buxis", Connection = "AzureTableStorage")] IAsyncCollector<TableStorageBuxis> outputTable,
        [Table("Buxis", Connection = "AzureTableStorage")] CloudTable inputTable,
        ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");



            string companyId = Environment.GetEnvironmentVariable("CompanyId");
            string username = Environment.GetEnvironmentVariable("UserNameSAP");
            string password = Environment.GetEnvironmentVariable("PasswordSAP");

            string authValue = $"{username}@{companyId}:{password}";
            string authHeaderValue = Convert.ToBase64String(Encoding.UTF8.GetBytes(authValue));

            httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", authHeaderValue);

            StringBuilder urlBuilder = new StringBuilder("https://api8.successfactors.com/odata/v2/EmpEmployment");
            urlBuilder.Append("?$select=personIdExternal,userId,lastModifiedDateTime,jobInfoNav/seqNumber,");
            urlBuilder.Append("jobInfoNav/startDate,jobInfoNav/event,jobInfoNav/userId,jobInfoNav/company,");
            urlBuilder.Append("jobInfoNav/lastModifiedDateTime,jobInfoNav/standardHours,personNav/personalInfoNav/firstName,");
            urlBuilder.Append("personNav/personalInfoNav/lastName,personNav/personalInfoNav/localNavGTM/customString11,");
            urlBuilder.Append("compInfoNav/empPayCompRecurringNav/paycompvalue,compInfoNav/empPayCompRecurringNav/payComponent,");
            urlBuilder.Append("personNav/personalInfoNav/maritalStatus,personNav/nationalIdNav/nationalId,");
            urlBuilder.Append("personNav/nationalIdNav/cardType");
            urlBuilder.Append("&$expand=jobInfoNav,personNav/personalInfoNav,personNav,compInfoNav/empPayCompRecurringNav,");
            urlBuilder.Append("compInfoNav,personNav/nationalIdNav,personNav/personalInfoNav/localNavGTM");
            urlBuilder.Append("&$filter=jobInfoNav/event eq '10004' and compInfoNav/empPayCompRecurringNav/payComponent eq 'CMI_BON_INC' and jobInfoNav/lastModifiedDateTime ge datetimeoffset'2023-06-21T00:00:00'");

            var response = await httpClient.GetAsync(urlBuilder.ToString());

            var responseBody = await response.Content.ReadAsStringAsync();

            var dataDBXML = XmlRead(responseBody.ToString());
            if (dataDBXML == null)
            {
                log.LogInformation($"Respuesta ResponseBody: {responseBody}");
            }


            #region TableStorage 

            TableQuery<TableStorageBuxis> query = new TableQuery<TableStorageBuxis>();
            TableContinuationToken token = null;
            List<Properties> DataStorage = new List<Properties>();

            do
            {
                TableQuerySegment<TableStorageBuxis> resultSegment = await inputTable.ExecuteQuerySegmentedAsync(query, token);
                token = resultSegment.ContinuationToken;
                #endregion
                foreach (var properties in dataDBXML)
                {
                    if (resultSegment.Results.Count > 0)
                    {
                        log.LogInformation($"C# Lectura de la extraccion: {DateTime.Now}");
                        foreach (TableStorageBuxis entity in resultSegment.Results)
                        {
                            // Deserializar ObjectJson a Properties
                            Properties existingProperties = JsonConvert.DeserializeObject<Properties>(entity.ObjectJson);

                            // Almacenar StateBuxis actual para poder restaurarlo más tarde
                            StateBuxis existingStateBuxis = existingProperties.StateBuxis;

                            // Ignorar StateBuxis durante la comparación
                            existingProperties.StateBuxis = StateBuxis.pending;

                            if (dataDBXML.Any(x => x.Equals(existingProperties))) // suponiendo que has implementado el método Equals en Properties
                            {
                                // Las propiedades existentes y las nuevas son iguales, no hacer nada
                                continue;
                            }

                            //if (properties.PersonIdExternal == "U1255298")
                            // {
                            // Las propiedades no son iguales, actualiza la entidad
                            // Restaurar StateBuxis antes de la actualización
                            properties.StateBuxis = StateBuxis.pending; // INformarle al cliente que si el valor ya esta en DB hacer que desde el SP se valide y se actualize en vez de volver a insertar.
                                                                        //TableOperation replaceOrInsertOperation = null;
                            if (existingProperties.PersonIdExternal == properties.PersonIdExternal)
                            {
                                properties.CodMf = existingProperties.CodMf;
                                entity.ObjectJson = JsonConvert.SerializeObject(properties);
                                TableOperation replaceOrInsertOperation = TableOperation.Replace(entity);
                                await inputTable.ExecuteAsync(replaceOrInsertOperation);
                                log.LogInformation($"Se Actualizo correctamente en el table storage {properties.PersonIdExternal}");
                            }
                            else if (!dataDBXML.Any(x => x.PersonIdExternal == existingProperties.PersonIdExternal))
                            {
                                var entities = new TableStorageBuxis
                                {
                                    PartitionKey = Guid.NewGuid().ToString(),
                                    RowKey = Guid.NewGuid().ToString(),
                                    ObjectJson = JsonConvert.SerializeObject(properties),
                                    Timestamp = DateTime.Now,
                                };
                                await outputTable.AddAsync(entities);
                                log.LogInformation($"Se inserto correctamente en el table storage");
                                //entity.PartitionKey = Guid.NewGuid().ToString();
                                //entity.RowKey = Guid.NewGuid().ToString();
                                //Timestamp = DateTime.Now,
                                //TableOperation replaceOrInsertOperation = TableOperation.Insert(entity);
                                //await inputTable.ExecuteAsync(replaceOrInsertOperation);
                                //log.LogInformation($"Se Inserto correctamente en el table storage {properties.PersonIdExternal}");

                            }
                            continue;
                        }
                    }
                    else
                    {
                        var entities = new TableStorageBuxis
                        {
                            PartitionKey = Guid.NewGuid().ToString(),
                            RowKey = Guid.NewGuid().ToString(),
                            ObjectJson = JsonConvert.SerializeObject(properties),
                            Timestamp = DateTime.Now,
                        };
                        await outputTable.AddAsync(entities);
                        log.LogInformation($"Se inserto correctamente en el table storage");
                    }
                }
                //if (resultSegment.Results.Count == 0)
                //{
                //    foreach (TableStorageBuxis entity in resultSegment.Results)
                //    {
                //        Properties existingProperties = JsonConvert.DeserializeObject<Properties>(entity.ObjectJson);

                //        if (existingProperties.Equals(properties)) // suponiendo que has implementado el método Equals en Properties
                //        {
                //            continue;
                //        }
                //        //if (properties.PersonIdExternal == "U1255298")
                //        //{
                //        var entities = new TableStorageBuxis
                //        {
                //            PartitionKey = Guid.NewGuid().ToString(),
                //            RowKey = Guid.NewGuid().ToString(),
                //            ObjectJson = JsonConvert.SerializeObject(properties),
                //            Timestamp = DateTime.Now,
                //        };
                //        await outputTable.AddAsync(entities);
                //        log.LogInformation($"Se inserto correctamente en el table storage");
                //        break;
                //        //}
                //    }
                //}
                //else
                //{
                //    //if (properties.PersonIdExternal == "U1255298")
                //    //{
                //    var entities = new TableStorageBuxis
                //    {
                //        PartitionKey = Guid.NewGuid().ToString(),
                //        RowKey = Guid.NewGuid().ToString(),
                //        ObjectJson = JsonConvert.SerializeObject(properties),
                //        Timestamp = DateTime.Now,
                //    };
                //    await outputTable.AddAsync(entities);
                //    log.LogInformation($"Se inserto correctamente en el table storage");
                //    break;
                //    //}
                //}
            } while (token != null);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                log.LogInformation(responseContent);
            }
            else
            {
                log.LogError("Error connecting to SuccessFactors");
            }
        }

        private static async Task<bool> ConnectAPIgateWay(string apiGateWay)
        {
            try
            {
                HttpResponseMessage response = await httpClient.GetAsync(apiGateWay);

                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Console.WriteLine(responseBody);
                if (response.IsSuccessStatusCode)
                    return true;
                else
                    return false;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return false;
            }
        }

        public static async Task ConsumoSpBuxis(string SP)
        {
            string connectionString = Environment.GetEnvironmentVariable("ConnectionStringBuxis");
            string storedProcedureName = "AltaColaborador";

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                using (SqlCommand cmd = new SqlCommand(storedProcedureName, conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Add parameters if your stored procedure needs any
                    cmd.Parameters.Add(new SqlParameter("@UserID_SSFF", "U1255298"));
                    cmd.Parameters.Add(new SqlParameter("@pri_ape_mf", "Integración"));
                    cmd.Parameters.Add(new SqlParameter("@pri_nom_mf", "Poc"));

                    conn.Open();
                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public static List<Properties> XmlRead(string xmlResponse)
        {
            try
            {
                string xml = xmlResponse;

                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(xml);

                XmlNamespaceManager nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("m", "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata");
                nsmgr.AddNamespace("d", "http://schemas.microsoft.com/ado/2007/08/dataservices");
                nsmgr.AddNamespace("atom", "http://www.w3.org/2005/Atom");
                nsmgr.AddNamespace("default", "http://www.w3.org/2005/Atom");

                //XmlNodeList propertiesNodes = xmlDoc.SelectNodes("//m:properties", nsmgr);
                XmlNodeList propertiesNodes = xmlDoc.SelectNodes("//m:inline", nsmgr);
                XmlNodeList EntryNodes = xmlDoc.SelectNodes("//atom:entry", nsmgr);
                List<Properties> propertiesList = new List<Properties>();
                Properties payList = new Properties();
                Properties propertiesFirstData = new Properties();
                string userId = "";
                string userIdValidation = "";
                int count = 0;

                // obtengo el entry
                foreach (XmlNode entryNode in EntryNodes)
                {
                    //XmlNode personIdExternalNode = entryNode.SelectSingleNode("atom:content/m:properties/d:PersonIdExternal", nsmgr);
                    string nodeContent = entryNode.InnerText;
                    string nodeContentXml = "<root>" + entryNode.InnerXml + "</root>";
                    XmlDocument xmlDocProperties = new XmlDocument();
                    xmlDocProperties.LoadXml(nodeContentXml);
                    propertiesNodes = xmlDocProperties.SelectNodes("//m:properties", nsmgr);
                    XmlNodeList linkNodes = xmlDocProperties.SelectNodes("//default:link", nsmgr);
                    var departments = new ArrayDepartamentosBuxis();

                    foreach (XmlNode linkNode in linkNodes)
                    {
                        string hrefValue = linkNode.Attributes["href"]?.OuterXml;

                        if (hrefValue.Contains("userId") || hrefValue.Contains("personIdExternal"))
                        {
                            if (!string.IsNullOrEmpty(hrefValue))
                            {
                                userId = GetLinkUserIdWithRegex(hrefValue);
                                if (userId == userIdValidation && !string.IsNullOrEmpty(userIdValidation))
                                {
                                    break;
                                }
                                if (!string.IsNullOrEmpty(userId))
                                {
                                    userIdValidation = userId;
                                    break;
                                }
                            }
                        }
                    }

                    if (propertiesFirstData!.PersonIdExternal != userIdValidation && !string.IsNullOrEmpty(userIdValidation) && !string.IsNullOrEmpty(propertiesFirstData!.PersonIdExternal))
                    {
                        foreach (var property in typeof(Properties).GetProperties())
                        {
                            if (property.GetValue(propertiesFirstData) != null)
                            {
                                if (propertiesFirstData.PersonIdExternal != userIdValidation && !string.IsNullOrEmpty(userIdValidation))
                                {
                                    propertiesFirstData.StateBuxis = FunctionSAPBuxis.Types.Enums.StateBuxis.pending;
                                    propertiesFirstData.PayComponentInc = payList!.PayComponentInc;
                                    propertiesFirstData.PayComponentBas = payList!.PayComponentBas;
                                    propertiesFirstData.CustomString11 = departments.ListDepartaments(int.Parse(propertiesFirstData.CustomString11));
                                    propertiesFirstData.MaritalStatus = departments.ListMaritalStatus(int.Parse(propertiesFirstData.MaritalStatus));
                                    propertiesFirstData.PayCompValueInc = double.IsPositiveInfinity(((double)int.Parse(payList!.PayCompValueInc) / 30 / int.Parse(propertiesFirstData.standardHours))) ? "0" : ((double)int.Parse(payList!.PayCompValueInc) / 30 / int.Parse(propertiesFirstData.standardHours)).ToString();
                                    propertiesFirstData.PayCompValueBas = double.IsPositiveInfinity(((double)int.Parse(payList!.PayCompValueBas) / 30 / int.Parse(propertiesFirstData.standardHours))) ? "0" : ((double)int.Parse(payList!.PayCompValueBas) / 30 / int.Parse(propertiesFirstData.standardHours)).ToString();
                                    propertiesFirstData.Company = propertiesFirstData.Company.Equals("DIP_AVSA") ? "1" : "2";

                                    propertiesList.Add(propertiesFirstData);
                                    propertiesFirstData = new Properties();
                                    payList = new Properties();
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        propertiesFirstData = SetPropertyValue(propertiesFirstData, "PersonIdExternal", userId);
                    }

                    // leo las propiedades del empleado
                    foreach (XmlNode propertiesNode in propertiesNodes)
                    {
                        XmlNodeList propertyNodes = propertiesNode.ChildNodes;
                        foreach (XmlNode propertyNode in propertyNodes)
                        {
                            string propertyName = propertyNode.LocalName;
                            string propertyValue = propertyNode.InnerText;

                            if ((propertyName == "payComponent") && (propertyValue == "CMI_BON_INC" || propertyValue == "CMI_SAL_BAS") ||
                                ((string.IsNullOrEmpty(payList!.PayCompValueInc) && !string.IsNullOrEmpty(payList!.PayComponentInc)) || (string.IsNullOrEmpty(payList!.PayCompValueBas) && !string.IsNullOrEmpty(payList!.PayComponentBas))))
                            {
                                if (payList!.PayComponentInc == "CMI_BON_INC" && string.IsNullOrEmpty(payList!.PayCompValueInc))
                                {
                                    payList = SetPropertyValue(payList, "PayCompValueInc", propertyValue);
                                    continue;
                                }
                                if (payList!.PayComponentBas == "CMI_SAL_BAS" && string.IsNullOrEmpty(payList!.PayCompValueBas))
                                {
                                    payList = SetPropertyValue(payList, "PayCompValueBas", propertyValue);
                                    continue;
                                }
                                if (propertyValue == "CMI_BON_INC")
                                {
                                    payList = SetPropertyValue(payList, "PayComponentInc", propertyValue);
                                    continue;
                                }
                                if (propertyValue == "CMI_SAL_BAS")
                                {
                                    payList = SetPropertyValue(payList, "PayComponentBas", propertyValue);
                                    continue;
                                }
                            }
                            else
                            {
                                propertiesFirstData = SetPropertyValue(propertiesFirstData, propertyName, propertyValue);
                            }


                        }
                    }
                    count = count + 1;
                    if (count == EntryNodes.Count)
                    {
                        foreach (var property in typeof(Properties).GetProperties())
                        {
                            if (property.GetValue(propertiesFirstData) != null)
                            {
                                if (propertiesFirstData.PersonIdExternal == userIdValidation && !string.IsNullOrEmpty(userIdValidation))
                                {
                                    propertiesFirstData.StateBuxis = FunctionSAPBuxis.Types.Enums.StateBuxis.pending;
                                    propertiesFirstData.PayComponentInc = payList!.PayComponentInc;
                                    propertiesFirstData.PayComponentBas = payList!.PayComponentBas;
                                    propertiesFirstData.CustomString11 = departments.ListDepartaments(int.Parse(propertiesFirstData.CustomString11));
                                    propertiesFirstData.MaritalStatus = departments.ListMaritalStatus(int.Parse(propertiesFirstData.MaritalStatus));
                                    propertiesFirstData.PayCompValueInc =  double.IsPositiveInfinity(((double)int.Parse(payList!.PayCompValueInc) / 30 / int.Parse(propertiesFirstData.standardHours))) ? "0" : ((double)int.Parse(payList!.PayCompValueInc) / 30 / int.Parse(propertiesFirstData.standardHours)).ToString();
                                    propertiesFirstData.PayCompValueBas = double.IsPositiveInfinity(((double)int.Parse(payList!.PayCompValueBas) / 30 / int.Parse(propertiesFirstData.standardHours))) ? "0" : ((double)int.Parse(payList!.PayCompValueBas) / 30 / int.Parse(propertiesFirstData.standardHours)).ToString();
                                    propertiesFirstData.Company = propertiesFirstData.Company.Equals("DIP_AVSA") ? "1" : "2";
                                    propertiesFirstData.StateBuxis = FunctionSAPBuxis.Types.Enums.StateBuxis.pending;
                                    propertiesList.Add(propertiesFirstData);
                                    propertiesFirstData = new Properties();
                                    break;
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }

                    // almaceno la informacion del empleado en una lista
                }
                Console.WriteLine("Todo Okey");
                return propertiesList;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                return null;
            }
        }

        public static Properties SetPropertyValue(Properties properties, string propertyName, string value)
        {
            var type = typeof(Properties);
            var propertiesList = type.GetProperties();

            foreach (var property in propertiesList)
            {
                if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase))
                {
                    property.SetValue(properties, value);

                    return properties;
                }
            }

            return properties;
        }

        public static string GetLinkUserIdWithRegex(string hrefValue)
        {
            string perPersonValue = "";

            // Define el patrón de expresión regular
            string pattern = @"personIdExternal='([^']*)'";
            string patternUserId = @"userId='([^']*)'";

            // Crea una instancia de Regex
            Regex regex = new Regex(pattern);

            // Intenta encontrar una coincidencia en hrefValue
            Match match = regex.Match(hrefValue);

            // Si hay una coincidencia, imprime el valor capturado
            if (match.Success)
            {
                perPersonValue = match.Groups[1].Value;
            }
            else
            {
                // Crea una instancia de Regex
                regex = new Regex(patternUserId);

                // Intenta encontrar una coincidencia en hrefValue
                match = regex.Match(hrefValue);
                if (match.Success)
                {
                    perPersonValue = match.Groups[1].Value;
                }
            }
            return perPersonValue;
        }
    }

}