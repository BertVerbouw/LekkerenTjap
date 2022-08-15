using LekkerenTsjap.Entities;
using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace LekkerenTsjap
{
    internal class ArduinoApi
    {
        internal static string baseUrl = "";

        private static HttpClient client = new HttpClient();

        private static async Task<string> SendGetRequest(string url)
        {
            string uri = baseUrl + url;
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Method = HttpMethod.Get
            };
            var result = client.SendAsync(request);
            result.Wait(500);
            return await result.Result.Content.ReadAsStringAsync();
        }

        internal static ArduinoData GetCurrentData()
        {
            var result = SendGetRequest("/arduino/digital/current");
            result.Wait(500);
            return new ArduinoData() {
                CurrentTemp = double.Parse(result.Result.Split(';')[0], CultureInfo.InvariantCulture),
                RequestedTemp = double.Parse(result.Result.Split(';')[1], CultureInfo.InvariantCulture),
                IsCooling = result.Result.Split(';')[2] == "1"
            };
        }
        internal static void RequestTemperature(double requestedtemp)
        {
            var result = SendGetRequest($"/arduino/digital/setrequestedtemp/{requestedtemp}");
            result.Wait(500);
        }
    }
}