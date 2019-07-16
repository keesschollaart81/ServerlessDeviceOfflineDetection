
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace DeviceOfflineDetection
{
    public class DeviceEntity
    {
        [JsonProperty]
        public string Id { get; set; }

        [JsonProperty]
        public TimeSpan OfflineAfter { get; set; }

        [JsonProperty]
        public DateTime? LastCommunicationDateTime { get; set; }

        [JsonProperty]
        public string TimeoutQueueMessageId { get; set; }

        [JsonProperty]
        public string TimeoutQueueMessagePopReceipt { get; set; }

        [JsonProperty]
        public bool Online { get; set; }

        private IDurableEntityContext context;
        private CloudQueue timeoutQueue;
        private ILogger log;

        [FunctionName(nameof(DeviceEntity))]
        public async Task HandleEntityOperation(
            [EntityTrigger] IDurableEntityContext context,
            [Queue("timeoutQueue", Connection = "AzureWebJobsStorage")]CloudQueue timeoutQueue,
            ILogger log)
        {
            // this does not work now:
            // https://github.com/Azure/azure-functions-durable-extension/issues/860
            this.context = context;
            this.timeoutQueue = timeoutQueue;
            this.log = log;

            if (context.IsNewlyConstructed)
            {
                context.SetState(new DeviceEntity()
                {
                    Id = context.EntityKey,
                    OfflineAfter = TimeSpan.FromSeconds(30)
                });
            }

            await context.DispatchAsync<DeviceEntity>();
        }

        public async Task MessageReceived()
        {
            // this.context, this.timeoutQueue and this.log are null now
            // https://github.com/Azure/azure-functions-durable-extension/issues/860
            this.LastCommunicationDateTime = DateTime.UtcNow;
            this.Online = true;

            bool addTimeoutMessage = true;
            if (this.TimeoutQueueMessageId != null)
            {
                try
                {
                    // reset the timeout

                    var message = new CloudQueueMessage(this.TimeoutQueueMessageId, this.TimeoutQueueMessagePopReceipt);
                    await this.timeoutQueue.UpdateMessageAsync(message, this.OfflineAfter, MessageUpdateFields.Visibility);
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
                log.LogInformation($"Device ${this.Id} if now online");
                log.LogMetric("online", 1);
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