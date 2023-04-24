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

using Nest;
using StatsBro.Domain.Models.Request;

namespace StatsBro.Storage.ElasticSearch
{
    public static class EsQueryExtensions
    {
        public static SearchDescriptor<T> BuildQueryFromRequest<T>(this SearchDescriptor<T> search, StatsRequest request, string timeZone) where T : class
        {
            search = search.Query(q => 
                q.Bool(b =>
                    b.Filter(GetQueries<T>(request, timeZone))
                    )
            );

            return search;
        }

        private static IEnumerable<Func<QueryContainerDescriptor<T>, QueryContainer>> GetQueries<T>(StatsRequest request, string timeZone) where T : class
        {
            var result = new List<Func<QueryContainerDescriptor<T>, QueryContainer>>();

            var from = DateTime.SpecifyKind(request.From!.Value, DateTimeKind.Unspecified);
            var to = DateTime.SpecifyKind(request.To!.Value, DateTimeKind.Unspecified);
            result.Add(q => q.DateRange(d => d
                .GreaterThanOrEquals(DateMath.Anchored(from))
                .LessThanOrEquals(DateMath.Anchored(to))
                .TimeZone(timeZone)
                .Field("@timestamp")
                ));

            if (!string.IsNullOrEmpty(request.Url))
            {
                result.Add(q => q.Term(t => t.Field("url.path.keyword").Value(request.Url)));
            }

            if (!string.IsNullOrEmpty(request.Country))
            {
                result.Add(q => q.Term(t => t.Field("geo.country_name.keyword").Value(request.Country)));
            }

            if (!string.IsNullOrEmpty(request.City))
            {
                result.Add(q => q.Term(t => t.Field("geo.city_name.keyword").Value(request.City)));
            }

            if (!string.IsNullOrEmpty(request.Lang))
            {
                result.Add(q => q.Term(t => t.Field("lang").Value(request.Lang)));
            }

            if (!string.IsNullOrEmpty(request.Event))
            {
                result.Add(q => q.Term(t => t.Field("event").Value(request.Event)));
            }

            if (!string.IsNullOrEmpty(request.UtmCampaign))
            {
                result.Add(q => q.Term(t => t.Field("url.query_params.utm_campaign").Value(request.UtmCampaign)));
            }

            if (!string.IsNullOrEmpty(request.Referrer))
            {
                if(request.Referrer == "direct")
                {
                    result.Add(q => q.Bool(bq => bq.MustNot(qmn =>qmn.Exists(exs => exs.Field("referrer.domain")))));
                } 
                else
                {
                    result.Add(q => q.Term(t => t.Field("referrer.domain").Value(request.Referrer)));
                }
            }

            return result;
        }
    }
}
