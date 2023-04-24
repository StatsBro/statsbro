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
﻿using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using StatsBro.Domain.Config;
using StatsBro.Domain.Helpers;
using StatsBro.Domain.Models;

namespace StatsBro.Host.Panel.Services;

public interface INotificationService
{
    Task NotifySiteConfigChangedAsync(Guid siteId);
}

public class NotificationService : INotificationService
{
    private readonly RabbitMQConfig _config;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IOptions<RabbitMQConfig> options, ILogger<NotificationService> logger)
    {
        this._config = options.Value;
        this._logger = logger;
    }

    public Task NotifySiteConfigChangedAsync(Guid siteId)
    {
        return Task.Run(() => {
            var body = Queues.BuildMessageBody(new EventSiteConfigChanged() { SiteId = siteId.ToString() });
            IModel? channel = null;
            IConnection? connection = null;
            
            try
            {
                (channel, connection) = this.BuildChannel();
                var properties = channel.CreateBasicProperties();

                channel.BasicPublish(
                    exchange: _config.ExchangeNameConfigReload,
                    routingKey: "",
                    body: body,
                    basicProperties: properties);
            }
            catch (Exception exc)
            {
                _logger.LogError("Problem notifying about config change, error: {msg}", exc.ToString());
            }
            finally
            {
                channel?.Close();
                connection?.Close();
            }
        });
    }

    // return is a small hack as I am too lazy to wrap it properly 
    // when connection is not disposed, magic happens in RabbitMQ after a while
    private (IModel, IConnection) BuildChannel()
    {
        var factory = new ConnectionFactory { HostName = _config.Host, Port = _config.Port };
        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();
        return (channel, connection);
    }
}
