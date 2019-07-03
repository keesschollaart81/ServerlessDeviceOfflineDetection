using Newtonsoft.Json;
using System;

namespace DeviceOfflineDetection
{
    public class OrchestratorArgs
    {
        public string DeviceId { get; set; }
    }

    public class Device
    {
        public TimeSpan OfflineAfter { get; set; }
        public DateTime? LastCommunicationDateTime { get; set; }
        public string TimeoutQueueMessageId { get; set; }
        public string TimeoutQueueMessagePopReceipt { get; set; }
        public bool Online { get; set; }
    }

    public class HttpTriggerArgs
    {
        public string DeviceId { get; set; }
    }

    public class StatusUpdateArgs
    {
        public StatusUpdateArgs(string deviceId, bool online)
        {
            this.DeviceId = deviceId;
            this.Online = online;
        }
        public string DeviceId { get; set; }

        public bool Online { get; set; }
    }
}