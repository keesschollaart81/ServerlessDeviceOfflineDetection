using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
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
            [DurableClient] IDurableEntityClient durableEntityClient,
            ILogger log)
        {
            log.LogInformation($"Receiving message for device {args.DeviceId}");

            var entity = new EntityId(nameof(DeviceEntity), args.DeviceId);
            await durableEntityClient.SignalEntityAsync(entity, nameof(DeviceEntity.MessageReceived));

            return new OkResult();
        }

        [FunctionName(nameof(HandleOfflineMessage))]
        public static async Task HandleOfflineMessage(
            [DurableClient] IDurableEntityClient durableEntityClient,
            [QueueTrigger("timeoutQueue", Connection = "AzureWebJobsStorage")]CloudQueueMessage message,
            [SignalR(HubName = "devicestatus")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger log
            )
        {
            var deviceId = message.AsString;

            var entity = new EntityId(nameof(DeviceEntity), deviceId);
            try
            {
                await durableEntityClient.SignalEntityAsync(entity, nameof(DeviceEntity.DeviceTimeout));
            }
            catch (Exception ex)
            {

            }

            await signalRMessages.AddAsync(new SignalRMessage {
                Target = "statusChanged",
                Arguments = new[] { new { deviceId = 1, status = "offline" } }
            });
            log.LogInformation($"Device ${deviceId} if now offline");
            log.LogMetric("offline", 1);
        }

        [FunctionName(nameof(GetStatus))]
        public static async Task<IActionResult> GetStatus(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get")] HttpTriggerArgs args,
            [DurableClient] IDurableEntityClient durableEntityClient,
            ILogger log)
        {
            var entity = new EntityId(nameof(DeviceEntity), args.DeviceId);
            var device = await durableEntityClient.ReadEntityStateAsync<DeviceEntity>(entity);

            return new OkObjectResult(device);
        }
    }
}