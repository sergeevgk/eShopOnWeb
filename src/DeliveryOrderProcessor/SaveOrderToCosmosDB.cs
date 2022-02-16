using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SaveOrderToCosmosDB
{
    public static class SaveOrderToCosmosDB
    {
        [FunctionName("SaveOrderToCosmosDB")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [CosmosDB(
                databaseName: "OrdersContainer",
                collectionName: "orders",
                ConnectionStringSetting = "CosmosDBConnectionString")] IAsyncCollector<dynamic> documentsOut,
            ILogger log)
        {

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var document = new
            {
                data.Id,
                data.OrderDate,
                data.ShipToAddress,
                data.OrderItems,
                data.FinalPrice,
            };
            await documentsOut.AddAsync(document);

            var responseMessage = $"Order from '{data.OrderDate}' processed and saved to database.";
            log.LogInformation($"Order from '{data.OrderDate}' processed and saved to database.");
            return new OkObjectResult(responseMessage);
        }
    }
}
