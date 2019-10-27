using Newtonsoft.Json;
using System;

namespace DeviceOfflineDetection
{
    public class OrchestratorArgs
    {
        public string DeviceId { get; set; }
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