using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace DeviceOfflineDetection
{
    public static class DeviceOfflineDetectionFunctions
    {
        // HTTP Trigger as an example
        // In real life this will probably a Storage or Service Bus- queue trigger or so
        // Trigger via: http://localhost:7071/api/HttpTrigger?DeviceId=8
        [FunctionName(nameof(HttpTrigger))]
        public static async Task<IActionResult> HttpTrigger(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpTriggerArgs args,
            [OrchestrationClient] IDurableOrchestrationClient durableOrchestrationClient,
            ILogger log)
        {
            log.LogInformation($"Receiving message for device {args.DeviceId}");

            var entity = new EntityId(nameof(DeviceEntity), args.DeviceId);
            await durableOrchestrationClient.SignalEntityAsync(entity, nameof(DeviceEntity.MessageReceived));

            return new OkResult();
        }

        [FunctionName(nameof(HandleOfflineMessage))]
        public static async Task HandleOfflineMessage(
            [OrchestrationClient] IDurableOrchestrationClient durableOrchestrationClient,
            [QueueTrigger("timeoutQueue", Connection = "AzureWebJobsStorage")]CloudQueueMessage message,
            ILogger log
            )
        {
            var deviceId = message.AsString;

            var entity = new EntityId(nameof(DeviceEntity), deviceId);
            await durableOrchestrationClient.SignalEntityAsync(entity, nameof(DeviceEntity.DeviceTimeout));

            // push out Offline event here
            log.LogInformation($"Device ${deviceId} if now offline");
            log.LogMetric("offline", 1);
        }

        [FunctionName(nameof(GetStatus))]
        public static async Task<IActionResult> GetStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpTriggerArgs args,
            [OrchestrationClient] IDurableOrchestrationClient durableOrchestrationClient,
            ILogger log)
        {
            var entity = new EntityId(nameof(DeviceEntity), args.DeviceId);
            var device = await durableOrchestrationClient.ReadEntityStateAsync<DeviceEntity>(entity);

            return new OkObjectResult(device);
        }
    }
}