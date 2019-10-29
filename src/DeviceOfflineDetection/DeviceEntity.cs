
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace DeviceOfflineDetection
{

    [JsonObject(MemberSerialization.OptIn)]
    public class DeviceEntity
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public TimeSpan? OfflineAfter { get; set; }

        [JsonProperty]
        public DateTime? LastCommunicationDateTime { get; set; }

        [JsonProperty]
        public string TimeoutQueueMessageId { get; set; }

        [JsonProperty]
        public string TimeoutQueueMessagePopReceipt { get; set; }

        private readonly ILogger logger;
        private readonly CloudQueue timeoutQueue;
        private readonly IAsyncCollector<SignalRMessage> signalRMessages;

        public DeviceEntity(string id, ILogger logger, CloudQueue timeoutQueue, IAsyncCollector<SignalRMessage> signalRMessages)
        {
            this.Id = id;
            this.logger = logger;
            this.timeoutQueue = timeoutQueue;
            this.signalRMessages = signalRMessages;

            if (!OfflineAfter.HasValue)
            {
                OfflineAfter = TimeSpan.FromSeconds(30);
            }
        }

        [FunctionName(nameof(DeviceEntity))]
        public static async Task HandleEntityOperation(
            [EntityTrigger] IDurableEntityContext context,
            [SignalR(HubName = "devicestatus")] IAsyncCollector<SignalRMessage> signalRMessages,
            [Queue("timeoutQueue", Connection = "AzureWebJobsStorage")] CloudQueue timeoutQueue,
            ILogger logger)
        {
            if (context.IsNewlyConstructed)
            {
                context.SetState(new DeviceEntity(context.EntityKey, logger, timeoutQueue, signalRMessages));
            }

            await context.DispatchAsync<DeviceEntity>(context.EntityKey, logger, timeoutQueue, signalRMessages);
        }

        public async Task MessageReceived()
        {
            this.LastCommunicationDateTime = DateTime.UtcNow;

            bool addTimeoutMessage = true;
            if (this.TimeoutQueueMessageId != null)
            {
                try
                {
                    // reset the timeout

                    var message = new CloudQueueMessage(this.TimeoutQueueMessageId, this.TimeoutQueueMessagePopReceipt);
                    await this.timeoutQueue.UpdateMessageAsync(message, this.OfflineAfter.Value, MessageUpdateFields.Visibility);
                    this.TimeoutQueueMessagePopReceipt = message.PopReceipt;
                    addTimeoutMessage = false;
                }
                catch (StorageException)
                {
                    // once... there was a message, not any more
                    addTimeoutMessage = true;
                }
            }

            if (addTimeoutMessage)
            {
                // start timeout 

                var message = new CloudQueueMessage(this.Id);
                await timeoutQueue.AddMessageAsync(message, null, this.OfflineAfter, null, null);
                this.TimeoutQueueMessageId = message.Id;
                this.TimeoutQueueMessagePopReceipt = message.PopReceipt;

                await this.ReportState("online");
                this.logger.LogInformation($"Device ${this.Id} if now online");
                this.logger.LogMetric("online", 1);
            }
        }

        private async Task ReportState(string state)
        {
            try
            {
                await this.signalRMessages.AddAsync(new SignalRMessage
                {
                    Target = "statusChanged",
                    Arguments = new[] { new { deviceId = this.Id, status = state } }
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "say what?");
            }
        }

        public async Task DeviceTimeout()
        {
            this.TimeoutQueueMessageId = null;
            this.TimeoutQueueMessagePopReceipt = null;

            await this.ReportState("offline");
        }
    }
}