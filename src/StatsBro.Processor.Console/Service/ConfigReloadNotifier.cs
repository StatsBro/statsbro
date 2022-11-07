﻿namespace StatsBro.Processor.Console.Service;

using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StatsBro.Domain.Config;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;

public interface IConfigReloadNotifier
{
    void SubscribeToReloadConfig(Func<Task> f);
}

public class ConfigReloadNotifier : IConfigReloadNotifier, IDisposable
{
    private readonly RabbitMQConfig _queueConfig;
    private bool _disposedValue;
    private IModel _channel = null!;
    private IConnection _connection = null!;

    public ConfigReloadNotifier(IOptions<RabbitMQConfig> rabbitMQOptions)
    {
        this._queueConfig = rabbitMQOptions.Value;
    }

    public void SubscribeToReloadConfig(Func<Task> f)
    {
        var factory = new ConnectionFactory { HostName = _queueConfig.Host, Port = _queueConfig.Port };
        this._connection = factory.CreateConnection();
        this._channel = this._connection.CreateModel();
        this.EnsureExchangeExists(this._channel);

        var queueName = this._channel.QueueDeclare().QueueName;
        this._channel.QueueBind(queue: queueName, exchange: _queueConfig.ExchangeNameConfigReload, routingKey: "");
        var consumer = new EventingBasicConsumer(this._channel);
        var events = Observable.FromEventPattern<BasicDeliverEventArgs>(handler => consumer.Received += handler, handler => consumer.Received -= handler);

        events
            .Select(e => e.EventArgs) // not needed at all, but here you can extract what you need from the incomming event
            .Buffer(TimeSpan.FromSeconds(30)) // buffer when many events are incoming but release all changes every X seconds
            .Where(_ => _.Any()) // trigger only when there was at least 1 event to reload config
            .Subscribe( async _ => await f()); // Reload the config

        this._channel.BasicConsume(queueName, true, consumer);
    }

    private void EnsureExchangeExists(IModel c)
    {
        c.ExchangeDeclare(_queueConfig.ExchangeNameConfigReload, ExchangeType.Fanout, true);
    }

    ~ConfigReloadNotifier() => this.Dispose(false);

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
                if(_channel != null)
                    _channel.Dispose();

                if(_connection != null)
                    _connection.Dispose();
            }

            _disposedValue = true;
        }
    }
}
