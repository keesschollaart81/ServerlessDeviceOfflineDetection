using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
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
            await durableOrchestrationClient.SignalEntityAsync(entity, "MessageReceived");

            return new OkResult();
        }

        [FunctionName(nameof(DeviceEntity))]
        public static async Task DeviceEntity(
            [EntityTrigger] IDurableEntityContext ctx,
            [Queue("timeoutQueue", Connection = "AzureWebJobsStorage")]CloudQueue timeoutQueue,
            ILogger log)
        {
            var device = ctx.GetState<Device>();
            if (device == null)
            {
                device = new Device();

                // typically somewhere here, you would get some metadata from a device registry
                // async operations are fine here since an Entity is considered to be an activity 
                // + this only happens the first time a node needs this entity
                device.OfflineAfter = TimeSpan.FromSeconds(30);
                ctx.SetState(device);
            }

            switch (ctx.OperationName)
            {
                case "MessageReceived":
                    device.LastCommunicationDateTime = DateTime.UtcNow;
                    device.Online = true;

                    bool addTimeoutMessage = true;
                    if (device.TimeoutQueueMessageId != null)
                    {
                        try
                        {
                            // reset the timeout

                            var message = new CloudQueueMessage(device.TimeoutQueueMessageId, device.TimeoutQueueMessagePopReceipt);
                            await timeoutQueue.UpdateMessageAsync(message, device.OfflineAfter, MessageUpdateFields.Visibility);
                            addTimeoutMessage = false; 
                        }
                        catch (StorageException ex)
                        {
                            // once... there was a message, not any more
                            addTimeoutMessage = true;
                        }
                    } 

                    if (addTimeoutMessage)
                    {
                        // start timeout 

                        var message = new CloudQueueMessage(ctx.Key);
                        await timeoutQueue.AddMessageAsync(message, null, device.OfflineAfter, null, null);
                        device.TimeoutQueueMessageId = message.Id;
                        device.TimeoutQueueMessagePopReceipt = message.PopReceipt;

                        // push out online event here
                        log.LogInformation($"Device ${ctx.Key} if now online");
                        log.LogMetric("online", 1);
                    }

                    ctx.SetState(device);
                    break;
                case "GetLastMessageReceived":
                    ctx.Return(device.LastCommunicationDateTime);
                    break;
                case "GetOfflineAfter":
                    ctx.Return(device.OfflineAfter);
                    break;
                case "DeviceTimeout":
                    device.Online = false;
                    device.TimeoutQueueMessageId = null;
                    device.TimeoutQueueMessagePopReceipt = null;
                    ctx.SetState(device);
                    break;

            }
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
            await durableOrchestrationClient.SignalEntityAsync(entity, "DeviceTimeout");

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
            var device = await durableOrchestrationClient.ReadEntityStateAsync<Device>(entity);

            return new OkObjectResult(device);
        }
    }
}