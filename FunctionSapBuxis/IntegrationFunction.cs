using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using FunctionSAPBuxis.Models;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Xml.Linq;
using System.Threading;

namespace FunctionSAPBuxis
{
    public static class IntegrationFunction
    {
        public static String RemoveEnd(this String str, int len)
        {
            if (str.Length < len)
            {
                return string.Empty;
            }

            return str[..^len];
        }

        private static readonly HttpClient client = new HttpClient();

        [FunctionName("FtpConnection")]
        public static async Task<IActionResult> Run(
       [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
       ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            // Establece la ruta y las credenciales de FTP
            var ftpPath = Environment.GetEnvironmentVariable("FTPPath");
            var username = Environment.GetEnvironmentVariable("FTPUserName");
            var password = Environment.GetEnvironmentVariable("FTPPassword");

            // Crea una solicitud FTP para obtener los detalles del directorio
            FtpWebRequest directoryRequest = (FtpWebRequest)WebRequest.Create(ftpPath);
            directoryRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            directoryRequest.Credentials = new NetworkCredential(username, password);

            List<string> fileNames = new List<string>();

            try
            {
                log.LogInformation("Se va a conectar al FTP");
                // Obtiene la respuesta del servidor FTP
                using FtpWebResponse responseFtp = (FtpWebResponse)directoryRequest.GetResponse();
                log.LogInformation("Conexion Exitosa FTP");
                // Lee la respuesta
                using StreamReader reader = new StreamReader(responseFtp.GetResponseStream());

                // Obtiene la lista de nombres de archivos
                while (true)
                {
                    string line = await reader.ReadLineAsync();
                    if (line == null)
                    {
                        // No hay m�s l�neas para leer
                        break;
                    }

                    fileNames.Add(line);
                }
            }
            catch (WebException ex)
            {
                // Maneja cualquier error
                log.LogInformation(ex.Message);
                string message = ((FtpWebResponse)ex.Response).StatusDescription;
                return new BadRequestObjectResult(message);
            }
                        
            List<string> dataResult = new List<string>();
            // Para cada archivo en el directorio
            foreach (var fileName in fileNames)
            {
                List<string> fileContents = new List<string>();
                if (!fileName.RemoveEnd(4).EndsWith("P"))
                {
                    // Crea una solicitud FTP para descargar el archivo
                    FtpWebRequest fileRequest = (FtpWebRequest)WebRequest.Create(ftpPath + fileName);
                    fileRequest.Method = WebRequestMethods.Ftp.DownloadFile;
                    fileRequest.Credentials = new NetworkCredential(username, password);

                    // Lectura de los archivos y cambio de nombre
                    try
                    {
                        // Obtiene la respuesta del servidor FTP
                        using FtpWebResponse responses = (FtpWebResponse)fileRequest.GetResponse();

                        // Lee la respuesta
                        using StreamReader reader = new StreamReader(responses.GetResponseStream());

                        // Lee y almacena el contenido del archivo
                        fileContents.Add(await reader.ReadToEndAsync());
                    }
                    catch (WebException ex)
                    {
                        // Maneja cualquier error
                        string message = ((FtpWebResponse)ex.Response).StatusDescription;
                        return new BadRequestObjectResult(message);
                    }

                    List<Encabezado> encabezados = ProcessContent(fileContents);

                    string responseString = "";
                    var dataTime = TimeSpan.FromMinutes(10);                    
                    bool ocurrioError = false;

                    if (!encabezados.Any())
                    {
                        dataResult.Add("No hay filas para procesar o no tiene la estructura correcta para el archivo: " + fileName);
                    }

                    foreach (var encabezado in encabezados)
                    {
                        XElement elementoPrincipal = TransformContentInXml(encabezado);

                        // Serializamos el Objeto elementoPrincipal para enviarlo a la Logic App
                        var serializacionElement = JsonConvert.SerializeObject(elementoPrincipal);
                        var content = new StringContent(serializacionElement.ToString(), Encoding.UTF8, "application/json");

                        HttpResponseMessage response = null;

                        using (CancellationTokenSource cts = new CancellationTokenSource(dataTime))
                        {
                            try
                            {
                                response = await client.PostAsync(Environment.GetEnvironmentVariable("ConnectionLogicApp"), content, cts.Token);

                                if (!response.IsSuccessStatusCode)
                                {
                                    ocurrioError = true;
                                }
                            }
                            catch (OperationCanceledException)
                            {
                                dataResult.Add("Request timed out.");
                                ocurrioError = true;
                                continue;                                
                            }
                        }

                        responseString = await response.Content.ReadAsStringAsync();
                        dataResult.Add(responseString);
                    }
                    if (ocurrioError)
                    {
                        return new OkObjectResult("El archivo: " + fileName + " no fue procesado ya que ocurrio un error en SAP");
                    }
                    else 
                    {
                        ChangeNameFile(ftpPath, username, password, fileName);                        
                    } 
                }
            }

            if (dataResult.Any())
            {
                return new OkObjectResult(dataResult);
            }
            else
            {
                return new OkObjectResult("No hay archivos para procesar");
            }
            
        }

        private static void ChangeNameFile(string ftpPath, string username, string password, string fileName)
        {
            var newFileName = Path.GetFileNameWithoutExtension(fileName) + "P" + Path.GetExtension(fileName);
            FtpWebRequest renameRequest = (FtpWebRequest)WebRequest.Create(ftpPath + fileName);
            renameRequest.Method = WebRequestMethods.Ftp.Rename;
            renameRequest.RenameTo = newFileName;
            renameRequest.Credentials = new NetworkCredential(username, password);
            renameRequest.GetResponse();
        }

        private static XElement TransformContentInXml(Encabezado encabezado)
        {
            // Crear el elemento IW33 o IW20
            XElement iW33 =
                            new XElement(encabezado.TipoOrden == "33" ? "I_W33" : "I_W20",
                            new XElement(encabezado.TipoOrden == "33" ? "ZPI_CO_W_33_E" : "ZPI_CO_W_20_E",
                            new XElement("IDENTIFICADOR", encabezado.Identificador),
                            new XElement("ORDENID", encabezado.OrdenId),
                            new XElement("PROVEEDOR", encabezado.Proveedor),
                            new XElement("EMISION", encabezado.Emision),
                            new XElement("CANCELACION", encabezado.Cancelacion),
                            new XElement("ENTREGA", encabezado.Entrega),
                            new XElement("TIPO_ORDEN", encabezado.TipoOrden),
                            new XElement("DEPARTAMENTO", encabezado.Departamento),
                            new XElement("CONDICIONES", encabezado.Condiciones),
                            new XElement("PROMOCION", encabezado.Promocion),
                            new XElement("REBAJA_1", encabezado.Rebaja1),
                            new XElement("REBAJA_2", encabezado.Rebaja2),
                            new XElement("MONEDA", encabezado.Moneda),
                            new XElement("PAIS", encabezado.Pais),
                            new XElement("GLN_CLIENTE", encabezado.GlnCliente),
                            new XElement("DIRECCION", encabezado.Direccion),
                            new XElement("CLIENTE_1", encabezado.Cliente1),
                            new XElement("CLIENTE_2", encabezado.Cliente2),
                            new XElement("CAMPO_1", encabezado.Campo1),
                            new XElement("GLN_DESPACHO", encabezado.GlnDespacho),
                            new XElement("LUGAR_ENTREGA", encabezado.LugarEntrega),
                            new XElement("DIRECCION_ENTREGA", encabezado.DireccionEntrega),
                            new XElement("LINEAS", encabezado.Lineas),
                            new XElement("TOTAL_UNIDADES", encabezado.TotalUnidades),
                            new XElement("MONTO_TOTAL", encabezado.MontoTotal)
                                 )
                          );

            // Agregar los elementos Detalle 
            XElement detalles = new XElement("I_W20_W33_DET");
            foreach (var detalle in encabezado.Detalles)
            {
                XElement detalleElemento = new XElement("ZPI_CO_W_20_33_D",
                                           new XElement("IDENTIFICADOR", detalle.Identificador),
                                           new XElement("ORDENID", detalle.OrdenId),
                                           new XElement("PROVEEDOR", detalle.Proveedor),
                                           new XElement("DESCRIPCION", detalle.Descripcion),
                                           new XElement("G_TIN", detalle.GTIN),
                                           new XElement("EAN", detalle.EAN),
                                           new XElement("CANTIDAD", detalle.Cantidad),
                                           new XElement("PAQUETES", detalle.Paquetes),
                                           new XElement("PRECIO_UNITARIO", detalle.PrecioUnitario),
                                           new XElement("MONTO_PARCIAL", detalle.MontoParcial),
                                           new XElement("POSICION", detalle.Posicion)
                                           );

                detalles.Add(detalleElemento);
            }

            // Agregar IW33 o IW20 y los detalles al elemento principal "ZFM_WALLMART_PEDIDO"
            XElement elementoPrincipal = new XElement("ZFM_WALLMART_PEDIDO",
                            new XElement("I_VKORG", "2710"),
                            new XElement("I_AUART", "ZT01"));

            // A�adimos Encabezado y detalle
            elementoPrincipal.Add(iW33);
            elementoPrincipal.Add(detalles);
            return elementoPrincipal;
        }

        private static List<Encabezado> ProcessContent(List<string> fileContents)
        {
            List<Encabezado> encabezados = new List<Encabezado>();
            Encabezado encabezadoActual = null;

            // Suponemos que 'lineas' es una lista de las lineas del archivo
            foreach (var item in fileContents)
            {
                List<string> lineas = item.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

                foreach (var linea in lineas)
                {
                    var partes = linea.Split('|'); // divide cada linea por '|'
                    if (partes[0] == "E") // Encabezado
                    {
                        // Si existe un encabezado anterior, lo agrega a la lista
                        if (encabezadoActual != null)
                        {
                            encabezados.Add(encabezadoActual);
                        }

                        // Crea un nuevo encabezado con los datos de la linea actual
                        encabezadoActual = new Encabezado
                        {
                            Identificador = partes[0],
                            OrdenId = partes[1],
                            Proveedor = partes[2],
                            Emision = partes[3],
                            Cancelacion = partes[4],
                            Entrega = partes[5],
                            TipoOrden = partes[6],
                            Departamento = partes[7],
                            Condiciones = partes[8],
                            Promocion = partes[9],
                            Rebaja1 = partes[10],
                            Rebaja2 = partes[11],
                            Moneda = partes[12],
                            Pais = partes[13],
                            GlnCliente = partes[14],
                            Direccion = partes[15],
                            Cliente1 = partes[16],
                            Cliente2 = partes[17],
                            Campo1 = partes[18],
                            GlnDespacho = partes[19],
                            LugarEntrega = partes[20],
                            DireccionEntrega = partes[21],
                            Lineas = partes[22],
                            TotalUnidades = partes[23],
                            MontoTotal = partes[24],
                            Detalles = new List<DetalleOrden>() // Inicializa la lista de detalles
                        };
                    }
                    else if (partes[0] == "D") // Detalle
                    {
                        // Crea un nuevo detalle y lo agrega al encabezado actual
                        encabezadoActual.Detalles.Add(new DetalleOrden
                        {
                            Identificador = partes[0],
                            OrdenId = partes[1],
                            Proveedor = partes[2],
                            Descripcion = partes[3],
                            GTIN = partes[4],
                            EAN = partes[5],
                            Cantidad = partes[6],
                            Paquetes = partes[7],
                            PrecioUnitario = partes[8],
                            MontoParcial = partes[9],
                            Posicion = partes[10]
                        });
                    }
                }
            }

            // Agrega el �ltimo encabezado a la lista
            if (encabezadoActual != null)
            {
                encabezados.Add(encabezadoActual);
            }

            return encabezados;
        }
    }
}
