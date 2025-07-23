using FunctionSAPBuxis.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using static FunctionSAPBuxis.Types.Enums;
using System.IO;
using System.Xml.Serialization;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FunctionSAPBuxis
{
    public class UpdateStateAndExecuteSP
    {
        [FunctionName("UpdateStateAndExecuteSP")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
        [Table("Buxis", Connection = "AzureTableStorage")] CloudTable inputTable,
        ILogger log)
        {

            log.LogInformation($"C# HttpTrigger function executed at: {DateTime.Now}");
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic dataResult = JsonConvert.DeserializeObject(requestBody);
                string personIdExternal = dataResult?.personIdExternal;
                string newStateBuxisStr = dataResult?.StateBuxis;

                if (string.IsNullOrEmpty(personIdExternal) || string.IsNullOrEmpty(newStateBuxisStr)
                    || !int.TryParse(newStateBuxisStr, out int newStateBuxis))
                {
                    return new BadRequestObjectResult("Must provide a valid 'personIdExternal' and 'StateBuxis' in the query parameters.");
                }

                // fetch the existing entities
                TableQuery<TableStorageBuxis> query = new TableQuery<TableStorageBuxis>();
                TableQuerySegment<TableStorageBuxis> segment = await inputTable.ExecuteQuerySegmentedAsync(query, null);

                // find the entity that matches personIdExternal
                var existingEntityData = segment.Results;

                var existingEntity = existingEntityData.FirstOrDefault(e => JsonConvert.DeserializeObject<Properties>(e.ObjectJson).PersonIdExternal == personIdExternal);

                if (existingEntity == null)
                {
                    return new NotFoundObjectResult($"No entity found with personIdExternal '{personIdExternal}'.");
                }

                var existingProperties = JsonConvert.DeserializeObject<Properties>(existingEntity.ObjectJson);
                existingProperties.StateBuxis = (StateBuxis)newStateBuxis;

                // Se ejecuta el SP del marital Status si esta tuvo algun cambio
                if (!string.IsNullOrEmpty(existingProperties.CodMf))
                {
                    try
                    {
                        string MaritalStatusSQL = GetMaritalStatus(personIdExternal, existingProperties.CodMf);

                        // Actualiza el estado marital en la entidad existente
                        if (existingProperties.MaritalStatus != MaritalStatusSQL)
                        {
                            existingEntity.ObjectJson = JsonConvert.SerializeObject(existingProperties);

                            // Realiza la operación de reemplazo en Azure Table Storage
                            TableOperation replaceOperation = TableOperation.Replace(existingEntity);
                            await inputTable.ExecuteAsync(replaceOperation);

                            using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionStringBuxis")))
                            {
                                connection.Open();
                                using (SqlCommand command = new SqlCommand("CambioEstadoCivil", connection))
                                {
                                    command.CommandType = CommandType.StoredProcedure;

                                    // asumamos que tu empleado ID es un string y estado civil también
                                    command.Parameters.AddWithValue("@cod_mf", int.Parse(existingProperties.CodMf));
                                    command.Parameters.AddWithValue("@est_civ_mf", existingProperties.MaritalStatus);
                                    command.Parameters.AddWithValue("@fecha_efectiva", existingProperties.StartDate);

                                    int affectedRows = await command.ExecuteNonQueryAsync();
                                    if (affectedRows > 0)
                                    {
                                        // La operación fue exitosa
                                        return new ContentResult
                                        {
                                            Content = existingEntity.ObjectJson,
                                            ContentType = "application/json",
                                            StatusCode = 200
                                        };
                                        //return new OkObjectResult($"Empleado {existingProperties.CodMf} con estado marital {existingProperties.MaritalStatus} procesado correctamente.");
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return null;
                    }
                }

                if (newStateBuxis == 1)
                {

                    // execute the stored procedure and get the response
                    string spResponse = await ExecuteStoredProcedure(existingProperties);

                    existingProperties.CodMf = spResponse;
                    if (existingProperties.CodMf.Length < 8)
                    {
                        existingEntity.ObjectJson = JsonConvert.SerializeObject(existingProperties);

                        // update the entity in the table
                        TableOperation replaceOperation = TableOperation.Replace(existingEntity);
                        await inputTable.ExecuteAsync(replaceOperation);

                        PropertiesXml propertiesToSerialize = new PropertiesXml
                        {
                            FirstName = existingProperties.FirstName,
                            LastName = existingProperties.LastName,
                            MaritalStatus = existingProperties.MaritalStatus,
                            NationalId = existingProperties.NationalId,
                            CustomString11 = existingProperties.CustomString11,
                            StartDate = existingProperties.StartDate,
                            CompensationInformation = existingProperties.CompensationInformation,
                            PayCompValueInc = existingProperties.PayCompValueInc,
                            PayCompValueBas = existingProperties.PayCompValueBas,
                            PayComponentBas = existingProperties.PayComponentBas,
                            PayComponentInc = existingProperties.PayComponentInc,
                            StandardHours = existingProperties.standardHours,
                            PersonIdExternal = existingProperties.PersonIdExternal,
                            Company = existingProperties.Company,
                            StateBuxis = existingProperties.StateBuxis.ToString(),
                            CodeEmployee = existingProperties.CodMf
                        };

                        //XmlSerializer xmlSerializer = new XmlSerializer(typeof(PropertiesXml));
                        //string xmlResult = "";
                        //using (var stringWriter = new StringWriter())
                        //{
                        //    xmlSerializer.Serialize(stringWriter, propertiesToSerialize);
                        //    xmlResult = stringWriter.ToString();
                        //}
                        return new ContentResult
                        {
                            Content = existingEntity.ObjectJson,
                            ContentType = "application/json",
                            StatusCode = 200
                        };
                    }
                    else
                    {
                        return new ContentResult
                        {
                            Content = "Error con la BD SQL o el SP CambioEstadoCivil",
                            ContentType = "application/xml",
                            StatusCode = 500
                        };
                    }
                }
                else if (newStateBuxis == 2)
                {
                    // just update the entity in the table
                    existingEntity.ObjectJson = JsonConvert.SerializeObject(existingProperties);
                    TableOperation replaceOperation = TableOperation.Replace(existingEntity);
                    await inputTable.ExecuteAsync(replaceOperation);
                    return new OkResult();
                }
                else
                {
                    return new BadRequestObjectResult($"Invalid StateBuxis value '{newStateBuxis}'.");
                }
            }
            catch (Exception ex)
            {
                return new BadRequestResult();
            }
        }


        private static async Task<string> ExecuteStoredProcedure(Properties entity)
        {
            try
            {
                string connectionString = Environment.GetEnvironmentVariable("ConnectionStringBuxis");
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    using (SqlCommand cmd = new SqlCommand("dbo.AltaColaborador", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.Add(new SqlParameter("@pri_nom_mf", entity.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@pri_ape_mf", entity.LastName));
                        cmd.Parameters.Add(new SqlParameter("@est_civ_mf", entity.MaritalStatus));
                        cmd.Parameters.Add(new SqlParameter("@depto_nac_mf", entity.CustomString11));
                        cmd.Parameters.Add(new SqlParameter("@fecha_efectiva", entity.StartDate));
                        cmd.Parameters.Add(new SqlParameter("@BONIFICACION_MF", entity.PayCompValueInc));
                        cmd.Parameters.Add(new SqlParameter("@sjh_mf", entity.PayCompValueBas));
                        cmd.Parameters.Add(new SqlParameter("@UserID_SSFF", entity.PersonIdExternal));
                        cmd.Parameters.Add(new SqlParameter("@cod_emp", entity.Company));
                        cmd.Parameters.Add(new SqlParameter("@dui_mf", entity.NationalId));

                        await conn.OpenAsync();
                        var result = await cmd.ExecuteScalarAsync();
                        string str = (string)result;
                        Match match = Regex.Match(str, @"\d+$");

                        if (match.Success)
                        {
                            return match.Value;  // Imprime "83933"
                        }
                        else
                        {
                            return (string)result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        public static string GetMaritalStatus(string userId, string codMf)
        {
            string estCivMf = "";

            using (SqlConnection connection = new SqlConnection(Environment.GetEnvironmentVariable("ConnectionStringBuxis")))
            {
                connection.Open();

                using (SqlCommand command = new SqlCommand("SELECT TOP 1 est_civ_mf FROM [BX_DESARROLLO_GT].[dbo].[MAEFUNC_TBL] WHERE UserID_SSFF = @UserId and COD_MF = @CodMf  ORDER BY fecha_efectiva DESC", connection))
                {
                    command.Parameters.AddWithValue("@UserId", userId);
                    command.Parameters.AddWithValue("@CodMf", codMf);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            estCivMf = reader.GetString(0).Trim();
                        }
                    }
                }
            }
            return estCivMf;
        }
    }
}