namespace StatsBro.DataGeneratorApp;

using StatsBro.Domain.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

internal class DummyDataGenerator
{
    private readonly Uri host = new Uri("http://localhost:5070");

    public void Run()
    {
        var client = new HttpClient();
        var bogusInternet = new Bogus.DataSets.Internet();
        var random = new Bogus.Randomizer();
        var queryStringParams = new List<(string, Func<int, string>)>
            {
                { ("guid", (i) => i % 2 == 0 ? "sdfopigjsdlfgj" : "22222223ed23erd") },
                { ("utm_source", (i) => i % 3 == 0 ? "google" : "facebook") },
                { ("utm_campaign", (i) => (i % 7).ToString()) }
            };

        string AttachQueryParams(int i, string url)
        {
            if (i % 2 == 0)
            {
                var queryParamTuble = queryStringParams[i % queryStringParams.Count];
                url = $"{url}?{queryParamTuble.Item1}={queryParamTuble.Item2.Invoke(i)}";
            }

            return url;
        }

        var domains = new List<string> { "statsbro.io", "subdomain.statsbro.io", "example.com", "smile.pl", "localhost" };

        string GetDomain(int threadNumber)
        {
            return domains[threadNumber % domains.Count];
        }

        Parallel.For(0, 10, x =>
        {
            var domain = GetDomain(x);
            for (int i = 0; i < 4000; i++)
            {
                var payload = new EventPayloadContent
                {
                    EventName = "pageview",
                    Lang = "en",
                    Referrer = bogusInternet.Url(),
                    Url = AttachQueryParams(i, bogusInternet.UrlWithPath("https", domain)),
                    UserAgent = bogusInternet.UserAgent(),
                    WindowHeight = random.Number(97, 100),
                    WindowWidth = random.Number(97, 100),
                    TouchPoints = random.Number(0, 1),
                    ScriptVersion = 1,
                };

                var jsonContent = JsonSerializer.Serialize(payload);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(host, "/api/s")) { Content = stringContent };
                requestMessage.Headers.Add("X-Forwarded-For", bogusInternet.Ip());

                var response = client.SendAsync(requestMessage).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error POSTing data: {response.StatusCode}");
                }

                Console.WriteLine($"{x}: {i}");
            }
        });

        Console.WriteLine("DONE");
    }
}
