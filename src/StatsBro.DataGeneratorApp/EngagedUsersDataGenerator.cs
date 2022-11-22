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
﻿using StatsBro.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StatsBro.DataGeneratorApp
{
    internal class EngagedUsersDataGenerator
    {
        Uri host = new Uri("http://localhost:5070");
        HttpClient client = new HttpClient();
        Bogus.DataSets.Internet bogusInternet = new Bogus.DataSets.Internet();
        Bogus.Randomizer random = new Bogus.Randomizer();
        List<string> userAgents = new List<string>();
        List<string> ips = new List<string>();
        List<EventPayloadContent> predefinedPayloads = new List<EventPayloadContent>();

        public EngagedUsersDataGenerator()
        {
            this.userAgents = Enumerable.Range(1, 30).Select(i => bogusInternet.UserAgent()).ToList();
            this.ips = Enumerable.Range(1, 50).Select(x => bogusInternet.Ip()).ToList();
            BuildPredefinedPayloads();
        }

        public void Run()
        {
            var domain = "statsbro.io";

            
                for (int i = 0; i < 10; i++)
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

                    
                }

            Console.WriteLine("DONE");
        }

        private (EventPayloadContent payload, string ip) GetPayload(int i, string domain)
        {
            var payload = this.predefinedPayloads[i % this.predefinedPayloads.Count];
            payload.Url = payload.Url.Replace("domain", domain);
            payload.Referrer = payload.Referrer.Replace("domain", domain);

            return (payload, this.ips[0]);
        }

        private void BuildPredefinedPayloads()
        {
            this.predefinedPayloads.Add(
                new EventPayloadContent
                {
                    EventName = "pageview",
                    Lang = "en",
                    Referrer = "",
                    Url = "https://domain/index.html",
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
                    Referrer = "https://domain/index.html",
                    Url = "https://domain/about.html",
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
                    Referrer = "https://domain/about.html",
                    Url = "https://domain/contact.html",
                    UserAgent = userAgents[0],
                    WindowHeight = 1024,
                    WindowWidth = 1200,
                    TouchPoints = 0,
                    ScriptVersion = 1
                });
        }
    }
}
