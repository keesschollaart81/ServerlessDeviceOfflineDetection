using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace TestDevice
{
    class Program
    {
        private static CloudQueue Queue;
        private static int DevicesCount = 0;

        static void Main(string[] args)
        {
            ConnectToStorageQueue();

            Task.Run(async () =>
            {
                while (true)
                {
                    Parallel.For(0, DevicesCount, new ParallelOptions { MaxDegreeOfParallelism = 10 }, async (deviceId) =>
                    {
                        await Queue.AddMessageAsync(new CloudQueueMessage($"{deviceId}"));
                    });
                    await Task.Delay(TimeSpan.FromSeconds(15));
                }
            });

            do
            {
                Console.WriteLine("Enter number of devices, 0 to exit");
                var input = Console.ReadLine();
                DevicesCount = int.Parse(input);

            } while (DevicesCount > 0);
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
