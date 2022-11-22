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

using Nest;
using Microsoft.Extensions.Options;
using StatsBro.Domain.Config;
using Elasticsearch.Net;

public interface IEsFactory
{
    IElasticClient GetClient();
}

public class EsFactory : IEsFactory
{
    private readonly ESConfig _config;
    private readonly object _synchRoot = new();
    private ElasticClient _client = null!;

    public EsFactory(IOptions<ESConfig> esOptions)
    {
        _config = esOptions.Value;
    }

    /// <summary>
    /// Singleton, thread safe.
    /// </summary>
    /// <returns></returns>
    public IElasticClient GetClient()
    {
        if (_client == null)
        {
            lock (_synchRoot)
            {
                if (_client == null)
                {
                    var uris = _config.Uris.Split(";", StringSplitOptions.RemoveEmptyEntries).Select(x => new Uri(x));
                    var pool = new StaticConnectionPool(uris);
                    var connectionSettings = new ConnectionSettings(pool);

#if DEBUG
                    connectionSettings.DisableDirectStreaming(true);
#endif

                    _client = new ElasticClient(connectionSettings);
                }
            }
        }

        return _client;
    }
}