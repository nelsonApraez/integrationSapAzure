using FunctionSAPBuxis.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FunctionSAPBuxis
{
    public static class RcaCafFunction
    {
        [FunctionName("ExtraerYEncolarDatosFiscales")]
        [return: ServiceBus("%InputQueueName%", ServiceBusEntityType.Queue)]
        public static async Task<string> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            //[RabbitMQ(QueueName = "cola_poc_company", ConnectionStringSetting = "RabbitMqConnection")] IAsyncCollector<string> outputEvents,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string codigo_empresa = req.Query["codigo_empresa"];
            string codigo_restaurante = req.Query["codigo_restaurante"];
            string codigo_establecimiento = req.Query["codigo_establecimiento"];
            string nit = req.Query["nit"];
            string fecha = req.Query["fecha"];
            string accion = req.Query["accion"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            codigo_empresa ??= data?.codigo_empresa;
            codigo_restaurante ??= data?.codigo_restaurante;
            codigo_establecimiento ??= data?.codigo_establecimiento;
            nit ??= data?.nit;
            fecha ??= data?.fecha;
            accion ??= data?.accion;


            RTFResponse rtfResponse = null;

            try
            {
                var sqlConnection = Environment.GetEnvironmentVariable("ConnectionStringRTF");

                using (SqlConnection conn = new SqlConnection(sqlConnection))
                {
                    conn.Open();

                    List<Detalle> lstDetalles = new List<Detalle>();

                    var cmdTextDetalles = "select " +
                        "nit_emisor," +
                        "numero_establecimiento," +
                        "fecha_factura," +
                        "autorizacion_numero," +
                        "autorizacion_serie," +
                        "numero_linea," +
                        "producto_codigo as codigo_producto," +
                        "descripcion_item," +
                        "medida," +
                        "cantidad," +
                        "precio_unitario," +
                        "precio," +
                        "monto_descuento," +
                        "importe_exento as monto_exento," +
                        "importe_total as total " +
                        "from rtf_factura_detalle " +
                        "where nit_emisor = " + nit +
                        " and numero_establecimiento = " + codigo_establecimiento +
                        " and fecha_factura = '" + fecha +"'";

                    using (SqlCommand cmdDetalles = new SqlCommand(cmdTextDetalles, conn))
                    {
                        using (var readerDetalles = await cmdDetalles.ExecuteReaderAsync())
                        {
                            while (readerDetalles.Read())
                            {
                                Detalle documentoDetalle = new Detalle();

                                documentoDetalle.nit_emisor = readerDetalles["nit_emisor"].ToString();
                                documentoDetalle.numero_establecimiento = readerDetalles["numero_establecimiento"].ToString();
                                documentoDetalle.fecha_factura = readerDetalles["fecha_factura"].ToString();
                                documentoDetalle.autorizacion_numero = (decimal)readerDetalles["autorizacion_numero"];
                                documentoDetalle.autorizacion_serie = readerDetalles["autorizacion_serie"].ToString();

                                documentoDetalle.numero_linea = (int)readerDetalles["numero_linea"];
                                documentoDetalle.codigo_producto = readerDetalles["codigo_producto"].ToString();
                                documentoDetalle.descripcion_item = readerDetalles["descripcion_item"].ToString();
                                documentoDetalle.medida = (int)readerDetalles["medida"];
                                documentoDetalle.cantidad = (decimal)readerDetalles["cantidad"];
                                documentoDetalle.precio_unitario = (decimal)readerDetalles["precio_unitario"];
                                documentoDetalle.precio = (decimal)readerDetalles["precio"];
                                documentoDetalle.monto_descuento = (decimal)readerDetalles["monto_descuento"];
                                documentoDetalle.monto_exento = (decimal)readerDetalles["monto_exento"];
                                documentoDetalle.total = (decimal)readerDetalles["total"];

                                lstDetalles.Add(documentoDetalle);
                            }
                        }
                    }


                    var cmdTextEncabezados = "SELECT top 600 " +
                        "autorizacion_numero," +
                        "autorizacion_serie," +
                        "serie_admin as serie_administrativa," +
                        "numero_admin as numero_administrativo," +
                        "fecha_factura as fecha_emision," +
                        "Substring(referencia,0,2) as codigo_canal_venta," +
                        "1 as caja," +
                        "autorizacion_serie as serie_fel," +
                        "numero_autorizacion as numero_fel," +
                        "ISNULL(numero_acceso, '') as numero_contingencia," +
                        "numero_autorizacion as certificado_fel," +
                        "nit_de_cliente as numero_identificacion_tributaria," +
                        "(total_bruto - total_descuento) as total," +
                        "estado_factura as estado_factura," +
                        "'FACTURA' as tipo_documento," +
                        "'' as serie_fel_referencia," +
                        "'' as numero_fel_referencia," +
                        "'' as certificado_fel_referencia " +
                        "FROM rtf_factura_encabezado " +
                        "where nit_emisor = " + nit +
                        " and numero_establecimiento = " + codigo_establecimiento +
                        " and fecha_factura = '" + fecha + "'";

                    using (SqlCommand cmd = new SqlCommand(cmdTextEncabezados, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            rtfResponse = new RTFResponse();
                            rtfResponse.accion = accion;
                            rtfResponse.origen = "Company";
                            rtfResponse.codigo_empresa = codigo_empresa;
                            rtfResponse.codigo_establecimiento = codigo_establecimiento; //codigo_restaurante;
                            rtfResponse.error = null;
                            rtfResponse.fecha_ejecucion = DateTime.UtcNow.ToString();
                            rtfResponse.documentos = new List<Documento>();

                            while (reader.Read())
                            {

                                Documento documento = new Documento();

                                documento.autorizacion_numero = (decimal)reader["autorizacion_numero"];
                                documento.autorizacion_serie = reader["autorizacion_serie"].ToString();
                                documento.codigo_empresa = codigo_empresa;
                                documento.codigo_establecimiento = codigo_establecimiento; //codigo_restaurante;
                                documento.serie_administrativa = reader["serie_administrativa"].ToString();
                                documento.numero_administrativo = reader["numero_administrativo"].ToString();
                                documento.fecha_emision = (DateTime)reader["fecha_emision"];
                                documento.codigo_canal_venta = reader["codigo_canal_venta"].ToString();
                                documento.caja = (int)reader["caja"];
                                documento.serie_fel = reader["serie_fel"].ToString();
                                documento.numero_fel = reader["numero_fel"].ToString();
                                documento.numero_contingencia = (int)reader["numero_contingencia"];
                                documento.certificado_fel = reader["certificado_fel"].ToString();
                                documento.numero_identificacion_tributaria = reader["numero_identificacion_tributaria"].ToString();
                                documento.total = (decimal)reader["total"];
                                documento.estado_factura = reader["estado_factura"].ToString();
                                documento.tipo_documento = reader["tipo_documento"].ToString();
                                documento.serie_fel_referencia = reader["serie_fel_referencia"].ToString();
                                documento.numero_fel_referencia = reader["numero_fel_referencia"].ToString();
                                documento.certificado_fel_referencia = reader["certificado_fel_referencia"].ToString();
                                documento.detalles = lstDetalles
                                    .Where(
                                    det => det.nit_emisor == nit
                                    && det.numero_establecimiento == codigo_establecimiento
                                    && det.fecha_factura == documento.fecha_emision.ToString()
                                    && det.autorizacion_numero == documento.autorizacion_numero
                                    && det.autorizacion_serie == documento.autorizacion_serie
                                    )
                                    .ToList();

                                rtfResponse.documentos.Add(documento);
                            }
                        }
                    }

                }

                //Enviar directamente al RabbitMQ
                //await outputEvents.AddAsync(JsonConvert.SerializeObject(jsonToReturn));
     
                return JsonConvert.SerializeObject(rtfResponse);
            }
            catch (Exception ex)
            {
                rtfResponse = new RTFResponse
                {
                    accion = accion,
                    origen = "Company",
                    codigo_empresa = codigo_empresa,
                    codigo_establecimiento = codigo_restaurante,
                    error = ex.Message.ToString(),
                    fecha_ejecucion = DateTime.UtcNow.ToString(),
                    documentos = new List<Documento>()
                };

                return JsonConvert.SerializeObject(rtfResponse);
            }
        }
    }
}
