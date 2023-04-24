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
﻿namespace StatsBro.Storage.ElasticSearch;

using Microsoft.Extensions.Options;
using Nest;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IEsRepository
{
    Task<IDictionary<string, bool>> HasDomainDocumentsAsync(IList<string> domains, TimeSpan timeSpan);
    Task<IEnumerable<ProcessedEventRaw>> GetRawDataAsync(string domain, DateTimeOffset from, DateTimeOffset to);
    
}

public class EsRepository : IEsRepository
{
    private readonly IEsFactory _esFactory;
    private readonly ESConfig _esConfig;

    public EsRepository(IEsFactory esFactory, IOptions<ESConfig> esConfigOptions)
    {
        this._esFactory = esFactory;
        this._esConfig = esConfigOptions.Value;
    }

    public async Task<IDictionary<string, bool>> HasDomainDocumentsAsync(IList<string> domains, TimeSpan timeSpan)
    {
        var result = new Dictionary<string, bool>();
        var request = new MultiSearchRequest() { Operations = new Dictionary<string, ISearchRequest>() };
        foreach (var domain in domains)
        {
            var searchRequest = new SearchRequest(Indexing.IndexName(_esConfig, domain))
            {
                Query = new DateRangeQuery { Field = "@timestamp", GreaterThanOrEqualTo = DateMath.Anchored(DateTime.UtcNow.AddDays(-7)) },
                Size = 0,
            };

            request.Operations.Add(domain, searchRequest);
            result[domain] = false;
        }

        var searchResponse = await this.Client.MultiSearchAsync(request);
        foreach (var domain in domains)
        {
            var r = searchResponse.GetResponse<object>(domain);
            if (r != null && r.Total > 0)
            {
                result[domain] = true;
            }
        }

        return result;
    }

    public async Task<IEnumerable<ProcessedEventRaw>> GetRawDataAsync(string domain, DateTimeOffset from, DateTimeOffset to)
    {
        var result = await this.Client.SearchAsync<ProcessedEventRaw>(s =>
            s.Index(Indexing.IndexName(this._esConfig, domain))
            .Size(10000)
            .Query(q => q.DateRange(drq => drq.Field("@timestamp").GreaterThan(DateMath.Anchored(from.UtcDateTime)).LessThan(DateMath.Anchored(to.UtcDateTime))))
            .Sort(s => s.Descending("@timestamp"))
        );

        return result.Hits.Select(h => h.Source);
    }

    private IElasticClient Client
    {
        get
        {
            return _esFactory.GetClient();
        }
    }
}