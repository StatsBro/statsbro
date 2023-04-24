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

using StatsBro.Domain.Models.Request;

namespace StatsBro.Host.Panel.Extensions
{
    public class StatsRequestLinkBuilder
    {
        private StatsRequest _request;

        public enum StatsRequestFitlerType { ByUrl, ByCity, ByCountry, ByUtmCampaign, ByLang, ByEventName, ByReferrer };

        public StatsRequestLinkBuilder(StatsRequest request)
        {
            _request = request;
        }

        public StatsRequestLinkBuilder FilterBy(StatsRequestFitlerType filterType, string value)
        {
            var request = new StatsRequest(_request);

            switch (filterType)
            {
                case StatsRequestFitlerType.ByUrl:
                    request.Url = value;
                    break;
                case StatsRequestFitlerType.ByCity:
                    request.City = value;
                    break;
                case StatsRequestFitlerType.ByCountry:
                    request.Country = value;
                    break;
                case StatsRequestFitlerType.ByLang:
                    request.Lang = value;
                    break;
                case StatsRequestFitlerType.ByUtmCampaign:
                    request.UtmCampaign = value;
                    break;
                case StatsRequestFitlerType.ByEventName:
                    request.Event = value;
                    break;
                case StatsRequestFitlerType.ByReferrer:
                    request.Referrer = value;
                    break;
            }

            return new StatsRequestLinkBuilder(request);
        }

        public StatsRequestLinkBuilder FilterByDate(int lastDays)
        {
            var from = DateTime.Now.Date.AddDays(-1 * lastDays);
            var to = DateTime.Now.Date;

            var request = new StatsRequest(_request)
            {
                From = from,
                To = to,
            };

            return new StatsRequestLinkBuilder(request);
        }

        public string GetUrl()
        {
            var result = $"?from={_request.From.Value.ToString("yyyy-MM-dd")}&to={_request.To.Value.ToString("yyyy-MM-dd")}";
            result += $"&url={_request.Url}&city={_request.City}&country={_request.Country}&lang={_request.Lang}&utmcampaign={_request.UtmCampaign}&event={_request.Event}&referrer={_request.Referrer}";

            return result;
        }
            
    }
}
