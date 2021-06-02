using LSL;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;

namespace TobiiProConnector
{
    class Program
    {
        private static readonly HttpClient _client = new HttpClient();
        static async Task Main(string[] args)
        {
            SendDataToLSL();
            await ConnectedToTobiiGlasses();
            await CalibrateTobiiGlassesAndRecord();
        }

        private static void SendDataToLSL()
        {
            var rnd = new Random();

            // create stream info and outlet
            var info = new liblsl.StreamInfo("Tobii Pro Glass 3", "Eye Tracking", 8, 100, liblsl.channel_format_t.cf_float32, "sourceId");
            var outlet = new liblsl.StreamOutlet(info);
            var data = new float[8];

            while (true)
            {
                // generate random data and send it
                for (int k = 0; k < data.Length; k++)
                    data[k] = rnd.Next(-100, 100);
                outlet.push_sample(data);
                Thread.Sleep(10);
            }

            Console.ReadKey();
        }

        private static async Task<bool> ConnectedToTobiiGlasses()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            var responseString = await _client.GetStringAsync("http://192.168.75.51/rest/system.recording-unit-serial");

            if (!string.IsNullOrEmpty(responseString))
            {
                Console.WriteLine("Device serial number is: " + responseString);
                return false;
            }

            Console.WriteLine("Can't connect to Tobii Glasses");
            return true;
        }

        private static async Task CalibrateTobiiGlassesAndRecord()
        {
            _client.DefaultRequestHeaders.Accept.Clear();
            HttpContent hc = new StringContent("[]");
            await _client.PostAsync("http://192.168.75.51/rest/calibrate!emit-markers", hc);
            Thread.Sleep(3000);

            var result2 = await _client.PostAsync("http://192.168.75.51/rest/calibrate!run", hc);
            var calibrateResponse = await result2.Content.ReadAsStringAsync();
            Console.WriteLine(calibrateResponse);
            if (calibrateResponse == "true")
            {
                Console.WriteLine("Starting recording");
                await _client.PostAsync("http://192.168.75.51/rest/recorder!start", hc);
                SendDataToLSL();
                // Thread.Sleep(5000);
                Console.CancelKeyPress += async delegate {
                    Console.WriteLine("Stopping recording");
                    await _client.PostAsync("http://192.168.75.51/rest/recorder!stop", hc);
                    Environment.Exit(0);
                };

                while (true) { }
            }
        }
    }
}
