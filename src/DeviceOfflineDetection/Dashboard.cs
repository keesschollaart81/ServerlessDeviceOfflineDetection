using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace DeviceOfflineDetection
{
    public static class Dashboard
    {
        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
          [SignalRConnectionInfo(HubName = "devicestatus")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        } 
    }
}