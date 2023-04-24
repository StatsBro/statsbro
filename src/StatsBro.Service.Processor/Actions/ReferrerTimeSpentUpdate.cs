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
namespace StatsBro.Service.Processor.Actions;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using StatsBro.Storage.ElasticSearch;

public interface IReferrerTimeSpentUpdate
{
    public Task Act(SiteVisitData contentItem);
}

public class ReferrerTimeSpentUpdate : IReferrerTimeSpentUpdate
{
    private readonly IEsFactory _esFactory;
    private readonly ESConfig _esConfig;
    private readonly ILogger<ReferrerTimeSpentUpdate> _logger; 

    public ReferrerTimeSpentUpdate(IEsFactory esFactory, IOptions<ESConfig> esConfigOptions, ILogger<ReferrerTimeSpentUpdate> logger)
    {
        this._esFactory = esFactory;
        this._esConfig = esConfigOptions.Value;
        this._logger = logger; 
    }
    public async Task Act(SiteVisitData contentItem)
    {
        Uri? referrerUri = null!;
        if (string.IsNullOrEmpty(contentItem.Referrer) 
            || !Uri.TryCreate(contentItem.Referrer, UriKind.Absolute, out referrerUri))
        {
            return;
        }

        try
        {
            var scriptParams = new Dictionary<string, object> { { "ts", contentItem.Timestamp } };
            var sort = new FieldSort { Field = "@timestamp", Order = SortOrder.Descending };

            var client = _esFactory.GetClient();
            var result = await client.UpdateByQueryAsync<SiteVisitData>(uq => uq
                .Index(Indexing.IndexName(this._esConfig, contentItem.Domain))
                .Script(s => s
                    .Source("ctx._source.time_spent = Math.min(ChronoUnit.MILLIS.between(ZonedDateTime.parse(ctx._source['@timestamp']), ZonedDateTime.parse(params.ts)), 900000)") // 900 000 = 15 min
                    .Params(scriptParams)
                    )
                .Query(q => q
                    .Bool(bq => bq
                        .MustNot(mn => mn.Exists(ex => ex.Field("time_spent")))
                        .Filter(fq => fq.Term(t1 => t1.Field("url.path.keyword").Value(referrerUri.AbsolutePath)),
                            fq => fq.Term(t1 => t1.Field("url.domain.keyword").Value(contentItem.Domain)),
                            fq => fq.Term(t1 => t1.Field("event").Value("pageview")),
                            fq => fq.Term(t1 => t1.Field("hash").Value(contentItem.Hash))
                        )
                    )
                )
                .Sort("@timestamp:desc")
                .MaximumDocuments(1)
            );

#if DEBUG
            this._logger.LogInformation("{message}", result.DebugInformation);
#endif
        }
        catch (Exception exc)
        {
            this._logger.LogWarning(exc, "Exception in ReferrerTimeSpentUpdate: {error}", exc.Message);
        }
    }
}
