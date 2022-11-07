namespace StatsBro.Processor.Console.Service;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using StatsBro.Domain.Config;
using StatsBro.Processor.Console.Actions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly RabbitMQConfig _queueConfig;
    private readonly SemaphoreSlim _semaphore = new(Environment.ProcessorCount, Environment.ProcessorCount);
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

    static int counter = 0;
    static int sleepCounter = 0;

    /// Based on gettnig from the queue
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await this._initializer.Initialize(stoppingToken);
        await this._sitesConfigurations.Initialize();        

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
                System.Console.WriteLine($"sleeping: {sleepCounter++}");
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
                            System.Console.WriteLine($"processedMessages: {counter++}");
                            await _actionsEntry.Act(got.Body);
                            channel.BasicAck(deliveryTag: deliveryTag, multiple: false);
                            System.Console.WriteLine($"DONE processedMessages: {counter}");
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
            _factory = new ConnectionFactory { HostName = _queueConfig.Host, Port = _queueConfig.Port };
            _connection = _factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: _queueConfig.QueueName, durable: true, exclusive: false, autoDelete: false);

            // TODO: some settings, maybe this should be adjusted from config
            // this may block getting more messages for worker who expects more
            _channel.BasicQos(prefetchSize: _queueConfig.PrefetchedSize, prefetchCount: _queueConfig.PrefetchedCount, global: _queueConfig.QosIsGlobal);
        }

        return _channel;
    }
}

