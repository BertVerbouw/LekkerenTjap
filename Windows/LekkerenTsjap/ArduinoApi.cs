using System;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;

namespace LekkerenTsjap
{
    internal class ArduinoApi
    {
        private static string baseUrl = "http://192.168.1.127";
        //private static string baseUrl = "http://demo7423295.mockable.io/";

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

        internal static double GetCurrentTemp()
        {
            var result = SendGetRequest("/arduino/digital/current");
            result.Wait(500);
            return double.Parse(result.Result, CultureInfo.InvariantCulture);
        }

        internal static double GetDesiredTemp()
        {
            var result = SendGetRequest("/arduino/digital/desired");
            result.Wait(500);
            return double.Parse(result.Result, CultureInfo.InvariantCulture);
        }

        internal static string GetTemps()
        {
            var result = SendGetRequest("/arduino/digital/temps/");
            result.Wait(500);
            return result.Result;
        }

        internal static string GetAllInfo()
        {
            var result = SendGetRequest("/arduino/digital/all/");
            result.Wait(500);
            return result.Result;
        }
    }
}