using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StatsBro.Processor.Console.Service;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using StatsBro.Domain.Config;
using StatsBro.Processor.Console.Actions;
using StatsBro.Storage.Database;
using StatsBro.Storage.ElasticSearch;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
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
