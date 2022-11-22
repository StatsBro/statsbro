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
﻿namespace StatsBro.Host.Data.Services;

using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;
using System.Text;

public class RabbitMQClient : IPushEventClient, IDisposable
{
    private readonly RabbitMQConfig _config;
    private readonly ILogger<RabbitMQClient> _logger;
    private readonly object _lock = new();
    private bool _disposedValue;

    private IModel _channel = null!;
    private ConnectionFactory _factory = null!;
    private IConnection _connection = null!;

    public RabbitMQClient(
        IOptions<RabbitMQConfig> options,
        ILogger<RabbitMQClient> logger
        )
    {
        this._config = options.Value;
        this._logger = logger;
    }

    public Task PushAsync(EventPayload payload)
    {
        return Task.Run(() => {
            var messageBody = Queues.BuildMessageBody(payload);
            var channel = GetChannel();
        
            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;

            channel.BasicPublish(
                exchange: "",
                routingKey: _config.QueueName,
                body: messageBody,
                basicProperties: properties);
        });
    }

    private IModel GetChannel()
    {
        if(_channel == null)
        {
            lock (_lock)
            {
                if (_channel == null)
                {
                    _factory = new ConnectionFactory { HostName = _config.Host, Port = _config.Port };
                    _connection = _factory.CreateConnection();
                    _channel = _connection.CreateModel();

                    _channel.QueueDeclare(queue: _config.QueueName, durable: true, exclusive: false, autoDelete: false);
                    
                    // TODO: some settings, maybe this should be adjusted from config
                    // this may block getting more messages for worker who expects more
                    _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                    _logger.LogDebug("RabbitMQ connection is up.");
                }
            }
        }        

       return _channel;
    }
    
    ~RabbitMQClient() => this.Dispose(false);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // Protected implementation of Dispose pattern.
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _channel.Dispose();
                _connection.Dispose();
            }

            _disposedValue = true;
        }
    }
}
