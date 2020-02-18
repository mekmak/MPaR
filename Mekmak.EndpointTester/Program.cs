using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mekmak.EndpointTester
{
    class Program
    {
        static void Main(string[] args)
        {
            int weekNumber = 3;
            string schedule = DownloadGames(weekNumber).Result;
            File.WriteAllText($".\\response{weekNumber}.json", schedule);
            Console.WriteLine(schedule);
        }

        private static async Task<string> DownloadGames(int weekNumber)
        {
            using (var httpClient = new HttpClient())
            {
                var uri = new Uri($"https://wzavfvwgfk.execute-api.us-east-2.amazonaws.com/production/owl/paginator/schedule?stage=regular_season&season=2020&locale=en-us&page={weekNumber}");
                using (var request = new HttpRequestMessage(HttpMethod.Get, uri))
                {
                    request.Headers.Add("Referer", "https://overwatchleague.com/en-us/schedule?stage=regular_season");
                    var response = await httpClient.SendAsync(request);
                    var responseString = await response.Content.ReadAsStringAsync();
                    return responseString;
                }
            }
        }

    }
}
