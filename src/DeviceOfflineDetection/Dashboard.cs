using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

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
        public static HttpResponseMessage Dashboard(
            [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
            ExecutionContext context)
        { 
            var path = Path.Combine(context.FunctionAppDirectory, "dashboard.html"); 
            var content = File.ReadAllText(path);

            var result = new HttpResponseMessage(HttpStatusCode.OK);
            result.Content = new StringContent(content, Encoding.UTF8, "text/html"); 

            return result;
        }
    }
}