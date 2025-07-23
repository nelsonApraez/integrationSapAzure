using FunctionSAPBuxis.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using System.Xml;

namespace FunctionSAPBuxis
{
    public class GetEntitiesInStatePending
    {
        [FunctionName("GetEntitiesInStatePending")]
        public static async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", Route = null)] HttpRequest req,
        [Table("Buxis", Connection = "AzureTableStorage")] CloudTable inputTable,
        ILogger log)
        {
            // Obtén todos los registros de la tabla
            TableQuery<TableStorageBuxis> query = new TableQuery<TableStorageBuxis>();

            TableQuerySegment<TableStorageBuxis> segment = await inputTable.ExecuteQuerySegmentedAsync(query, null);

            // Filtra los registros en el código
            var filteredResults = segment.Results
                .Select(x => JsonConvert.DeserializeObject<Properties>(x.ObjectJson))
                .Where(x => x.StateBuxis == 0)
                .ToList();

            return new OkObjectResult(filteredResults);
        }
    }
}
