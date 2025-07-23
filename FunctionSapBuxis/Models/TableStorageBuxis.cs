using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.Azure.Cosmos.Table;

namespace FunctionSAPBuxis.Models
{
    public class TableStorageBuxis : Microsoft.Azure.Cosmos.Table.TableEntity
    {
        public string ObjectJson { get; set; }
    }
}