using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.IO;

namespace DeviceOfflineDetection
{
    public static class DashboardFunctions
    {
        [FunctionName("negotiate")]
        public static SignalRConnectionInfo GetSignalRInfo(
          [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
          [SignalRConnectionInfo(HubName = "devicestatus")] SignalRConnectionInfo connectionInfo)
        {
            return connectionInfo;
        }

        [FunctionName(nameof(Dashboard))]
        public static IActionResult Dashboard(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "options", Route = "Dashboard")] HttpRequest req,
            ExecutionContext context)
        { 
            var path = Path.Combine(context.FunctionDirectory, "../dashboard.html"); 
            var content = File.ReadAllText(path);

            return new OkObjectResult(content);
        }
    }
}