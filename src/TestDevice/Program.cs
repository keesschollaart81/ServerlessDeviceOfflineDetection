using System;
using System.Net.Http;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;

namespace TestDevice
{
    class Program
    {

        private static HttpClient HttpClient = new HttpClient();
        private static int DevicesCount = 0;

        static void Main(string[] args)
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    Parallel.For(0, DevicesCount, new ParallelOptions { MaxDegreeOfParallelism = 50 }, async (id) =>
                    {
                        await HttpClient.GetAsync($"https://offlinedetection.azurewebsites.net/api/HttpTrigger?DeviceId={id}");
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
    }
}
