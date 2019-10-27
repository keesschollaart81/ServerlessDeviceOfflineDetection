
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
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

        [JsonProperty]
        public bool Online { get; set; }

        private readonly ILogger logger;
        private readonly CloudQueue timeoutQueue;

        public DeviceEntity(string id, ILogger logger, CloudQueue timeoutQueue)
        {
            this.Id = id;
            this.logger = logger;
            this.timeoutQueue = timeoutQueue;
            if (!OfflineAfter.HasValue)
            {
                OfflineAfter = TimeSpan.FromSeconds(30);
            }
        }

        [FunctionName(nameof(DeviceEntity))]
        public static async Task HandleEntityOperation(
            [EntityTrigger] IDurableEntityContext context,
            [Queue("timeoutQueue", Connection = "AzureWebJobsStorage")] CloudQueue timeoutQueue,
            ILogger logger)
        {
            if (context.IsNewlyConstructed)
            {
                context.SetState(new DeviceEntity(context.EntityKey, logger, timeoutQueue));
            }

            await context.DispatchAsync<DeviceEntity>(context.EntityKey, logger, timeoutQueue);
        }

        public async Task MessageReceived()
        {
            this.LastCommunicationDateTime = DateTime.UtcNow;
            this.Online = true;

            bool addTimeoutMessage = true;
            if (this.TimeoutQueueMessageId != null)
            {
                try
                {
                    // reset the timeout

                    var message = new CloudQueueMessage(this.TimeoutQueueMessageId, this.TimeoutQueueMessagePopReceipt);
                    await this.timeoutQueue.UpdateMessageAsync(message, this.OfflineAfter.Value, MessageUpdateFields.Visibility);
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

                var message = new CloudQueueMessage(this.Id);
                await timeoutQueue.AddMessageAsync(message, null, this.OfflineAfter, null, null);
                this.TimeoutQueueMessageId = message.Id;
                this.TimeoutQueueMessagePopReceipt = message.PopReceipt;

                // push out online event here
                this.logger.LogInformation($"Device ${this.Id} if now online");
                this.logger.LogMetric("online", 1);
            }
        }

        public Task DeviceTimeout()
        {
            this.Online = false;
            this.TimeoutQueueMessageId = null;
            this.TimeoutQueueMessagePopReceipt = null;

            return Task.CompletedTask;
        }
    }
}