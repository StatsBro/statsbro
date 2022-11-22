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
﻿using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using StatsBro.Domain.Config;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;
using StatsBro.Service.Processor.Actions;
using StatsBro.Service.Processor.Service;
using Serilog;

System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(context.Configuration)
            .CreateLogger();

        logging.AddSerilog(Log.Logger);

        Log.Logger.Information("ENV: {env}", context.HostingEnvironment.EnvironmentName);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IActionsEntry, ActionsEntry>();
        services.AddTransient<IPushToElastic, PushToElastic>();
        services.AddTransient<IEventPayloadDeserializer, DeserializeQueueMessage>();
        services.AddTransient<IEventPyloadFilter, FilterEventPayload>();
        services.AddTransient<IContentProcessor, ContentProcessor>();

        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddSingleton<ISitesConfigurations, SitesConfigurations>();

        services.AddTransient<IConfigReloadNotifier, ConfigReloadNotifier>();
        services.AddTransient<IDbRepository, DbRepository>();
        services.AddTransient<IBootstrapDb, BootstrapDb>();
        services.AddTransient<IInitializer, Initializer>();
        services.AddTransient<IEsFactory, EsFactory>();

        services.AddHostedService<Worker>();
        services.Configure<RabbitMQConfig>(context.Configuration.GetSection(nameof(RabbitMQConfig)));
        services.Configure<ESConfig>(context.Configuration.GetSection(nameof(ESConfig)));
        services.Configure<ServiceConfig>(context.Configuration.GetSection(nameof(ServiceConfig)));
        services.Configure<DatabaseConfig>(context.Configuration.GetSection(nameof(DatabaseConfig)));
    })
    .Build();

await host.RunAsync();
// TODO: check this minimalistyc approach: https://itnext.io/a-cleaner-startup-for-net-6s-minimal-approach-c1c05a672c6a