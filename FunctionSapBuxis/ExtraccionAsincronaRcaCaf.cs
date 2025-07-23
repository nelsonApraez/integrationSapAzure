using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using Microsoft.Data.SqlClient;
using System.Net.Http;
using System.Text;

namespace FunctionSAPBuxis
{
    public class ExtraccionAsincronaRcaCaf
    {
        /// <summary>
        /// Funcion ejecutando diariamente a las 5 a.m UTC (Hora del servidor en Azure)
        /// </summary>
        /// <param name="myTimer"></param>
        /// <param name="log"></param>
        [FunctionName("ExtraccionAsincronaRcaCaf")]
        public void Run([TimerTrigger("0 0 5 * * *", RunOnStartup = true)]TimerInfo myTimer, ILogger log)
        {
            var sqlConnection = Environment.GetEnvironmentVariable("ConnectionStringRTF");

            using (SqlConnection conn = new SqlConnection(sqlConnection)) 
            {
                conn.Open();
                //var text = "SELECT nit_emisor, numero_establecimiento, fecha_factura FROM rtf_factura_encabezado GROUP BY nit_emisor, numero_establecimiento, fecha_factura";
                var text = "select nit_emisor,numero_establecimiento,fecha_factura " +
                    "from rtf_factura_encabezado " +
                    "where fecha_factura = (select max(fecha_factura) from rtf_factura_encabezado) " +
                    "group by nit_emisor,numero_establecimiento,fecha_factura " +
                    "order by nit_emisor,numero_establecimiento";


                using (SqlCommand command = new SqlCommand(text, conn))
                {
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) 
                        {
                            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, Environment.GetEnvironmentVariable("UrlExtraccionRTFFunction"));
                            HttpResponseMessage httpResponse = new HttpResponseMessage();

                            httpRequest.Headers.Add("Ocp-Apim-Subscription-Key", Environment.GetEnvironmentVariable("Ocp-Apim-Subscription-Key"));
                            httpRequest.Content = new StringContent(
                                JsonConvert.SerializeObject(new 
                                {
                                    codigo_empresa = "3240",
                                    codigo_restaurante = "93001",
                                    codigo_establecimiento = reader["numero_establecimiento"].ToString(),
                                    nit = reader["nit_emisor"].ToString(),
                                    fecha = reader["fecha_factura"].ToString(),
                                    accion = "PROCESAR"
                                }),Encoding.UTF8, "application/json");

                            using (var client = new HttpClient())
                            {
                                try {
                                    httpResponse = client.SendAsync(httpRequest,HttpCompletionOption.ResponseContentRead).Result;
                                    log.LogInformation(JsonConvert.SerializeObject(httpResponse.Content.ReadAsStringAsync()));
                                    //log.LogInformation(JsonConvert.SerializeObject(httpResponse));
                                } 
                                catch (Exception ex) {
                                    log.LogError(ex.Message);
                                }
                            }

                        }
                    }
                }
            }
            
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
        }
    }
}
