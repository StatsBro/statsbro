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

using System;

namespace StatsBro.Domain.Models.Request
{
    public class StatsRequest
    {
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public string? Url { get; set; }
        public string? City { get; set; }
        public string? Country { get; set; }
        public string? UtmCampaign { get; set; }
        public string? Lang { get; set; }
        public string? Event { get; set; }
        public string? Referrer { get; set; }

        public StatsRequest()
        {
            From = new DateTime(DateTime.Now.Date.AddDays(-30).Ticks, DateTimeKind.Unspecified);
            To = new DateTime(DateTime.Now.Date.Ticks, DateTimeKind.Unspecified);
        }

        public StatsRequest(StatsRequest request)
        {
            From = request.From;
            To = request.To;
            Url = request.Url;
            City = request.City;
            Country = request.Country;
            UtmCampaign = request.UtmCampaign;
            Lang = request.Lang;
            Event = request.Event;
            Referrer = request.Referrer;
        }
    }
}
