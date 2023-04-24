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
﻿namespace StatsBro.Service.Processor.Service;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StatsBro.Domain.Config;
using StatsBro.Service.Processor.Actions;
using System.Reactive.Linq;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMQConfig _queueConfig;
    private readonly SemaphoreSlim _semaphore;
    private readonly IActionsEntry _actionsEntry;
    private readonly IInitializer _initializer;
    private readonly ISitesConfigurations _sitesConfigurations;

    private IModel _channel = null!;
    private ConnectionFactory _factory = null!;
    private IConnection _connection = null!;

    public Worker(
        IActionsEntry actionsEntry,
        IOptions<RabbitMQConfig> rabbitMQOptions,
        IInitializer initializer,
        ISitesConfigurations sitesConfigurations,
        ILogger<Worker> logger)
    {
        this._actionsEntry = actionsEntry;
        this._queueConfig = rabbitMQOptions.Value;
        this._initializer = initializer;
        this._sitesConfigurations = sitesConfigurations;
        this._logger = logger;

        this._semaphore = new(Environment.ProcessorCount, Environment.ProcessorCount);
#if DEBUG
        this._semaphore = new SemaphoreSlim(1);
#endif
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("StartAsync was called");
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        this._logger.LogInformation("StopAsync was called");
        return base.StopAsync(cancellationToken);
    }


    /// <summary>
    /// APPROACH: Based on events
    /// </summary>
    /// <returns></returns>
    //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    //{
    //    var channel = this.GetChannel();
    //    var consumer = new EventingBasicConsumer(channel);

    //    consumer.Received += Consumer_Received;
    //    channel.BasicConsume(
    //        queue: _config.QueueName,
    //        autoAck: false,
    //        consumer: consumer);        

    //    while (!stoppingToken.IsCancellationRequested)
    //    {
    //        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
    //        await Task.Delay(1000, stoppingToken);
    //    }
    //    consumer.Received -= Consumer_Received;


    //    System.Console.WriteLine(stoppingToken.IsCancellationRequested);
    //}

    /// Based on gettnig from the queue
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this._logger.LogInformation("Worker started");

        try
        {
            await this._initializer.Initialize(stoppingToken);
            await this._sitesConfigurations.Initialize();
        }
        catch (Exception exc)
        {
            this._logger.LogError(exc, "Faild doing initialization: {message}", exc.Message);
            throw;
        }

        var channel = this.GetChannel();
        while (!stoppingToken.IsCancellationRequested)
        {
            // czy mam dostepny wątek?
            // nie - to jestem zablokowany
            // tak - to biore z kolejki
            // czy pobrana wiadomośc jest pusta
            // tak - czekam
            // nie - procesuje
            await _semaphore.WaitAsync(stoppingToken);
            var got = channel.BasicGet(_queueConfig.QueueName, autoAck: false);
            if (got == null)
            {
                _semaphore.Release();
                await Task.Delay(1000, stoppingToken); // nothing to do, thread waits
            }
            else
            {
                var deliveryTag = got.DeliveryTag;
                try
                {
                    var isScheduled = ThreadPool.QueueUserWorkItem(async _ =>
                    {
                        try
                        {
                            await _actionsEntry.Act(got.Body);
                            channel.BasicAck(deliveryTag: deliveryTag, multiple: false);
                            _logger.LogDebug("I processed msg from queue with delivery tag: {deliveryTag}", deliveryTag);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Message {deliveryTag} was not processed because of exception.", deliveryTag);

                            // TODO: put to dead letter queue
                            //channel.BasicReject(deliveryTag: got.DeliveryTag, requeue: true);
                            channel.BasicAck(deliveryTag: deliveryTag, multiple: false);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    });
                    if (!isScheduled)
                    {
                        try
                        {
                            channel.BasicReject(deliveryTag: deliveryTag, requeue: true);
                        }
                        finally
                        {
                            _semaphore.Release();
                        }
                    }
                }
                catch (NotSupportedException)
                {
                    _semaphore.Release();
                }
            }            
        }

        _logger.LogInformation("END service task execution. Was cancellation was requested? {IsCancellationRequested}", stoppingToken.IsCancellationRequested);
    }

    private IModel GetChannel()
    {
        if (_channel == null)
        {
            try
            {
                _factory = new ConnectionFactory { HostName = _queueConfig.Host, Port = _queueConfig.Port };
                _connection = _factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: _queueConfig.QueueName, durable: true, exclusive: false, autoDelete: false);

                // TODO: some settings, maybe this should be adjusted from config
                // this may block getting more messages for worker who expects more
                _channel.BasicQos(prefetchSize: _queueConfig.PrefetchedSize, prefetchCount: _queueConfig.PrefetchedCount, global: _queueConfig.QosIsGlobal);
            }
            catch(Exception exc)
            {
                this._logger.LogError(exc, "Failed creating connection to RabbitMQ: {message}", exc.Message);
                throw;
            }
        }

        return _channel;
    }
}

