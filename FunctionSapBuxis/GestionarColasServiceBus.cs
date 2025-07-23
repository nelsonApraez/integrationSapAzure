using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace FunctionSAPBuxis
{
    public static class GestionarColasServiceBus
    {
        [FunctionName("GestionarColasServiceBus")]
        public static async Task Run(
            [ServiceBusTrigger("rtfqueue", Connection = "AzureWebJobsServiceBus")] string myQueueItem,
            [RabbitMQ(QueueName = "cola_poc_company", ConnectionStringSetting = "RabbitMqConnection")] IAsyncCollector<string> outputEvents,
            ILogger log)
        {
            await outputEvents.AddAsync(JsonConvert.SerializeObject(myQueueItem));
        }
    }
}
