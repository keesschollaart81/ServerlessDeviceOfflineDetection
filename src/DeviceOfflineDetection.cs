using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

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
            // tricky stuff, make sure the orchestrationId is deterministic but also unique and has now weird characters
            var orchestrationId = args.DeviceId;

            var status = await durableOrchestrationClient.GetStatusAsync(orchestrationId);

            if (status?.RuntimeStatus == OrchestrationRuntimeStatus.Running)
            {
                await durableOrchestrationClient.RaiseEventAsync(orchestrationId, "MessageReceived", null);
            }
            else
            {
                await durableOrchestrationClient.StartNewAsync(nameof(WaitingOrchestrator), orchestrationId, null);
            }

            log.LogInformation("Started orchestration with ID = '{orchestrationId}'.", orchestrationId);
            var response = durableOrchestrationClient.CreateHttpManagementPayload(orchestrationId);

            return new OkObjectResult(response);
        }

        // For each message we create this waiting-orchestrator
        // It waits for a Device specific timeout
        // While waiting, there is no thread running
        // After the timeout, it fetches the Durable Entities state
        // .. and checks if that state has been updated
        // be aware: Orchestrators have some specifics like replays (google) 
        // next to that, Orchestrators are triggered from a queue but this queue is only polled every 1 minute (which you can update but that is tricky)  
        [FunctionName(nameof(WaitingOrchestrator))]
        public static async Task WaitingOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext ctx,
            ILogger log)
        {
            var orchestratorArgs = ctx.GetInput<OrchestratorArgs>();

            // Using an entity as a cache for the data of the device like
            // - OfflineAfter, static metadata, typically coming from a device registry (loaded during initialization of entity)
            // - LastMessageReceived, 'hot' data, not essential for current PoC but could be usefull later on
            var entity = new EntityId(nameof(DeviceEntity), orchestratorArgs.DeviceId);

            var offlineAfter = await ctx.CallEntityAsync<TimeSpan>(entity, "GetOfflineAfter");
            var lastActivity = await ctx.CallEntityAsync<DateTime?>(entity, "GetLastMessageReceived");

            if (!lastActivity.HasValue || DateTime.UtcNow - lastActivity > offlineAfter)
            {
                // This runs for the first message ever for a device id
                // Also, after a device has gone offline and comes back online, this orchestrator starts again  Last Activity should then be longer ago then the offline after
                log.LogInformation($"Device {orchestratorArgs.DeviceId}, was unkown or offline and is now online!");
                await ctx.CallActivityAsync(nameof(SendStatusUpdate), new StatusUpdateArgs(orchestratorArgs.DeviceId, true));
            }

            while (true)
            {
                ctx.SignalEntity(entity, "SetLastCommunicationDateTime", DateTime.UtcNow);

                try
                {
                    await ctx.WaitForExternalEvent("MessageReceived", offlineAfter);
                    log.LogInformation($"Message received for device {orchestratorArgs.DeviceId}, resetting timeout of {offlineAfter.TotalSeconds} seconds offline detection...");
                }
                catch (Exception)
                {
                    log.LogWarning($"Device {orchestratorArgs.DeviceId}, is now offline after waiting for {offlineAfter.TotalSeconds} seconds");
                    await ctx.CallActivityAsync(nameof(SendStatusUpdate), new StatusUpdateArgs(orchestratorArgs.DeviceId, false));
                    return;
                }
            }
        }


        [FunctionName(nameof(DeviceEntity))]
        public static async Task DeviceEntity([EntityTrigger] IDurableEntityContext ctx)
        {
            var device = ctx.GetState<Device>();
            if (device == null)
            {
                device = new Device();

                // typically somewhere here, you would get some metadata from a device registry
                // async operations are fine here since an Entity is considered to be an activity 
                // + this only happens the first time a node needs this entity
                device.OfflineAfter = TimeSpan.FromMinutes(3);
                ctx.SetState(device);
            }

            switch (ctx.OperationName)
            {
                case "SetLastCommunicationDateTime":
                    device.LastCommunicationDateTime = DateTime.UtcNow;
                    ctx.SetState(device);
                    break;
                case "GetLastMessageReceived":
                    ctx.Return(device.LastCommunicationDateTime);
                    break;
                case "GetOfflineAfter":
                    ctx.Return(device.OfflineAfter);
                    break;
            }
        }

        [FunctionName(nameof(SendStatusUpdate))]
        public static Task SendStatusUpdate(
            [ActivityTrigger]StatusUpdateArgs status,
            ILogger log
            )
        {
            log.LogInformation($"Device ${status.DeviceId} status update! New status is: online:{status.Online}");
            // in real, send status to topic
            return Task.CompletedTask;
        }
    }
}