using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading;
using System.Threading.Tasks;

namespace TestDevice
{
    class Program
    {
        private static CloudQueue Queue;
        private static Task MessageSenderTask;
        private static CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        static void Main(string[] args)
        {
            ConnectToStorageQueue();

            while(true)
            {
                Console.WriteLine("Enter number of devices, 0 to exit");
                var input = Console.ReadLine();
                var devicesCount = int.Parse(input);
                
                CancellationTokenSource.Cancel();
                MessageSenderTask?.Wait();

                if (devicesCount == 0) break;
                CancellationTokenSource = new CancellationTokenSource();

                MessageSenderTask = Task.Run(async () =>
                {
                    while (!CancellationTokenSource.IsCancellationRequested)
                    {
                        var start = DateTime.Now;

                        await Task.WhenAll(Enumerable.Range(0, devicesCount).Select(async deviceId => {
                            await Queue.AddMessageAsync(new CloudQueueMessage($"{deviceId}"));
                        }));

                        var duration = DateTime.Now - start;

                        Console.WriteLine($"{DateTime.Now:G} Send messages to {devicesCount} Devices in {duration}");

                        if (duration < TimeSpan.FromSeconds(10))
                        {
                            try
                            {
                                await Task.Delay(TimeSpan.FromSeconds(10) - duration, CancellationTokenSource.Token);
                            }
                            catch (TaskCanceledException) { }
                        }
                    }
                });

            } 
        }

        private static void ConnectToStorageQueue()
        {
            var configurationRoot = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            var storageAccount = CloudStorageAccount.Parse(configurationRoot.GetConnectionString("StorageConnectionString"));
            var queueClient = storageAccount.CreateCloudQueueClient();
            Queue = queueClient.GetQueueReference("device-messages");
            Queue.CreateIfNotExists();
        }
    }
}
