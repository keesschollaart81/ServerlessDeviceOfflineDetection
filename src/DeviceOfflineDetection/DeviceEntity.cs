
using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
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
        public DateTime? LastCommunicationDateTime { get; set; }

        private readonly ILogger logger;
        private readonly IDurableEntityContext context;
        private readonly IAsyncCollector<SignalRMessage> signalRMessages;
        private TimeSpan offlineAfter = TimeSpan.FromSeconds(20);

        public DeviceEntity(string id, ILogger logger, IAsyncCollector<SignalRMessage> signalRMessages, IDurableEntityContext context)
        {
            this.Id = id;
            this.logger = logger;
            this.signalRMessages = signalRMessages;
            this.context = context;
        }

        [FunctionName(nameof(DeviceEntity))]
        public static async Task HandleEntityOperation(
            [EntityTrigger] IDurableEntityContext context,
            [SignalR(HubName = "devicestatus")] IAsyncCollector<SignalRMessage> signalRMessages,
            ILogger logger)
        {
            await context.DispatchAsync<DeviceEntity>(context.EntityKey, logger, signalRMessages, context);
        }

        public async Task MessageReceived()
        {
            this.LastCommunicationDateTime = DateTime.UtcNow;

            var entityId = new EntityId(nameof(DeviceEntity), this.Id);
            this.context.SignalEntity(entityId, DateTime.UtcNow.Add(this.offlineAfter), nameof(DeviceTimeout));

            await this.ReportState("online");
            this.logger.LogInformation($"Device ${this.Id} if now online");
        }

        private async Task ReportState(string state)
        {
            await this.signalRMessages.AddAsync(new SignalRMessage
            {
                Target = "statusChanged",
                Arguments = new[] { new { deviceId = this.Id, status = state } }
            });
        }

        public async Task DeviceTimeout()
        {
            if (DateTime.UtcNow - LastCommunicationDateTime > offlineAfter)
            {
                await ReportState("offline");
            }
        }
    }
}