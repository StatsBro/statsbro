/* Copyright StatsBro.io and/or licensed to StatsBro.io under one
 * or more contributor license agreements.
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the Server Side Public License, version 1

 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * Server Side Public License for more details.

 * You should have received a copy of the Server Side Public License
 * along with this program. If not, see
 * <https://github.com/StatsBro/statsbro/blob/main/LICENSE>.
 */
﻿namespace StatsBro.DataGeneratorApp;

using StatsBro.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

internal class StaticDataGenerator
{
    Uri host = new Uri("http://localhost:5070");
    HttpClient client = new HttpClient();
    Bogus.DataSets.Internet bogusInternet = new Bogus.DataSets.Internet();
    Bogus.Randomizer random = new Bogus.Randomizer();
    List<string> userAgents = new List<string>();
    List<string> ips = new List<string>();
    List<EventPayloadContent> predefinedPayloads = new List<EventPayloadContent>();

    public StaticDataGenerator()
    {
        this.userAgents = Enumerable.Range(1, 30).Select(i => bogusInternet.UserAgent()).ToList();
        this.ips = Enumerable.Range(1, 50).Select(x => bogusInternet.Ip()).ToList();
        BuildPredefinedPayloads();
    }

    public void Run()
    {
        var domain = "statsbro.io";

        Parallel.For(0, 10, x => {
            for (int i = 0; i < 4000; i++)
            {
                var (payload, ip) = GetPayload(i, domain);
                var jsonContent = JsonSerializer.Serialize(payload);
                var stringContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, new Uri(host, "/api/s")) { Content = stringContent };
                requestMessage.Headers.Add("X-Forwarded-For", ip);

                var response = client.SendAsync(requestMessage).Result;

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Error POSTing data: {response.StatusCode}");
                }

                if (i % 100 == 0)
                {
                    Console.WriteLine($"{domain}: {x}/{i}");
                }
            }
        });

        Console.WriteLine("DONE");
    }

    private (EventPayloadContent payload, string ip) GetPayload(int i, string domain)
    {
        var payload = this.predefinedPayloads[i % this.predefinedPayloads.Count];
        payload.Url = payload.Url.Replace("domain", domain);

        return (payload, this.ips[i % ips.Count]);
    }

    private void BuildPredefinedPayloads()
    {
        this.predefinedPayloads.Add(
            new EventPayloadContent
            {
                EventName = "pageview",
                Lang = "en",
                Referrer = "https://kopalniawiedzy.pl/rak-jelita-grubego-instrukcja-chemioterapia,35740",
                Url = "https://domain/index.html?utma=12356345&ref=wp.pl&utm_term=googleshop&utm_campaign=adstest1",
                UserAgent = userAgents[0],
                WindowHeight = 1024,
                WindowWidth = 1200,
                TouchPoints = 0,
                ScriptVersion = 1
            });

        this.predefinedPayloads.Add(
            new EventPayloadContent
            {
                EventName = "pageview",
                Lang = "en",
                Referrer = "https://allegro.pl/oferta/kuchenna-deska-bambus-do-krojenia-serwowania-4-szt-11485462177?bi_s=ads&bi_m=showitem:active&bi_c=MjhmZTZhMzYtOGQzNi00M2RlLTk2YTYtZjIwODYwNmE0ZTVmAA&bi_t=ape&referrer=proxy&emission_unit_id=10459c7b-c286-4875-9779-a4c4304b339b",
                Url = "https://domain/index.html",
                UserAgent = userAgents[2],
                WindowHeight = 1024,
                WindowWidth = 460,
                TouchPoints = 2,
                ScriptVersion = 1
            });

        //this.predefinedPayloads.Add(
        //    new EventPayloadContent
        //    {
        //        EventName = "pageview",
        //        Lang = "pl",
        //        Url = "https://domain/index.html#main_index",
        //        UserAgent = userAgents[3],
        //        WindowHeight = 1024,
        //        WindowWidth = 1500,
        //        TouchPoints = 0,
        //        ScriptVersion = 1
        //    });

        //this.predefinedPayloads.Add(
        //    new EventPayloadContent
        //    {
        //        EventName = "pageview",
        //        Lang = "en",
        //        Referrer = bogusInternet.Url(),
        //        Url = "https://domain/basket.html?ref=me.com#buyme",
        //        UserAgent = userAgents[7],
        //        WindowHeight = 1024,
        //        WindowWidth = 1440,
        //        TouchPoints = 0,
        //        ScriptVersion = 1
        //    });
    }
}
