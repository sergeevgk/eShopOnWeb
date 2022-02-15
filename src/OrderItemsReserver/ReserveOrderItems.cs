using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace OrderItemsReserver
{
    public static class ReserveOrderItems
    {
        [FunctionName("ReserveOrderItems")]
        public static async Task Run([ServiceBusTrigger("default-queue", Connection = "ServiceBusConnectionString")] string myQueueItem,
            [Blob("eshoponweb-orders-blob", FileAccess.Read)] BlobContainerClient blobContainer,
            ILogger log)
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            int counter = 0;
            log.LogInformation("C# HTTP trigger function processed a request.");

            for (counter = 0; counter < 3; counter++)
            {
                bool success = await TrySendDataToBlobAsync(blobContainer, myQueueItem, log);
                if (success)
                {
                    return;
                }
            }
            await SendEmailAsync("{ \"message\":\"Error occured accessing blob 'eshoponweb-orders-blob' in your subscription.\"}", config);
        }

        private static Task<HttpResponseMessage> SendEmailAsync(string text, IConfigurationRoot config)
        {
            HttpClient httpClient = new HttpClient();
            var message = new StringContent(text, new System.Text.UTF8Encoding(), "application/json");
            string uri = config["LogicAppUri"];
            return httpClient.PostAsync(uri, message);
        }

        private static async Task<bool> TrySendDataToBlobAsync(BlobContainerClient blobContainer, string message, ILogger log)
        {
            try
            {
                dynamic data = JsonConvert.DeserializeObject(message);
                string orderName = data.name;
                string orderData = JsonConvert.SerializeObject(data.body);

                string blobName = $"{orderName}.json";
                BlockBlobClient blob = blobContainer.GetBlockBlobClient($"{blobName}");
                if (blob == null)
                {
                    log.LogError("Cannot access blob " + blobContainer.Uri);
                    return false;
                }
                using (var stream = GenerateStreamFromString(orderData))
                {
                    await blob.UploadAsync(stream);
                }
                return true;
            }
            catch (Exception ex)
            {
                log.LogError("Error occured while uploading data to blob. " + ex.Message);
            }
            return false;
        }

        private static Stream GenerateStreamFromString(string s)
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }
    }
}
