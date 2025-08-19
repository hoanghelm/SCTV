using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonDetections.Service.Commands;
using PersonDetections.Service.Models;
using System.Text.Json;

namespace PersonDetections.Service.Services;

public class KafkaConsumerService : BackgroundService
{
    private readonly IMediator _mediator;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly IConsumer<Ignore, string> _consumer;

    public KafkaConsumerService(IMediator mediator, ILogger<KafkaConsumerService> logger, IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
        _configuration = configuration;

        var config = new ConsumerConfig
        {
            BootstrapServers = _configuration.GetConnectionString("Kafka") ?? "localhost:9092",
            GroupId = "person-detection-consumer",
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<Ignore, string>(config).Build();
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka Consumer Service started");
        
        _consumer.Subscribe("person-detection");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(stoppingToken);
                    
                    if (consumeResult?.Message?.Value != null)
                    {
                        await ProcessMessage(consumeResult.Message.Value);
                        _consumer.Commit(consumeResult);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message from Kafka");
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in Kafka consumer");
                }
            }
        }
        finally
        {
            _consumer.Close();
            _logger.LogInformation("Kafka Consumer Service stopped");
        }
    }

    private async Task ProcessMessage(string messageValue)
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var detectionMessage = JsonSerializer.Deserialize<PersonDetectionMessage>(messageValue, options);
            
            if (detectionMessage != null)
            {
                var command = new ProcessPersonDetectionCommand(detectionMessage);
                var result = await _mediator.Send(command);
                
                if (result)
                {
                    _logger.LogDebug("Successfully processed detection message for camera {CameraId}", detectionMessage.CameraId);
                }
                else
                {
                    _logger.LogWarning("Failed to process detection message for camera {CameraId}", detectionMessage.CameraId);
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error deserializing message: {Message}", messageValue);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message");
        }
    }

    public override void Dispose()
    {
        _consumer?.Dispose();
        base.Dispose();
    }
}