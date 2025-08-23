using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FacebookFnApp.Models;
using FacebookFnApp.Services;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace FacebookFnApp.Functions
{
    public class ProcessMediaUploadFunction
    {
        private readonly ILogger<ProcessMediaUploadFunction> _logger;
        private readonly IMediaProcessingService _mediaProcessingService;

        public ProcessMediaUploadFunction(
            ILogger<ProcessMediaUploadFunction> logger,
            IMediaProcessingService mediaProcessingService)
        {
            _logger = logger;
            _mediaProcessingService = mediaProcessingService;
        }

        [Function(nameof(ProcessMediaUploadFunction))]
        public async Task Run(
            [ServiceBusTrigger("media-upload-jobs", Connection = "ServiceBusConnection")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            _logger.LogInformation("Received message ID: {MessageId}", message.MessageId);
            _logger.LogInformation("Message correlation ID: {CorrelationId}", message.CorrelationId);
            _logger.LogInformation("Message delivery count: {DeliveryCount}", message.DeliveryCount);

            try
            {
                // Deserialize the message body
                var messageBody = message.Body.ToString();
                _logger.LogInformation("Message body: {MessageBody}", messageBody);

                var mediaUploadJob = JsonSerializer.Deserialize<MediaUploadJobDto>(messageBody);
                if (mediaUploadJob == null)
                {
                    _logger.LogError("Failed to deserialize message body to MediaUploadJobDto");
                    await messageActions.DeadLetterMessageAsync(message, "Invalid message format");
                    return;
                }

                // Log user properties if available
                if (message.ApplicationProperties.Count > 0)
                {
                    _logger.LogInformation("Message user properties:");
                    foreach (var property in message.ApplicationProperties)
                    {
                        _logger.LogInformation("  {Key}: {Value}", property.Key, property.Value);
                    }
                }

                // Process the media upload job
                _logger.LogInformation("Starting to process media upload job {JobId} for user {UserId}", 
                    mediaUploadJob.Id, mediaUploadJob.UserId);

                var processedJob = await _mediaProcessingService.ProcessMediaUploadAsync(mediaUploadJob);

                // Log the result
                _logger.LogInformation("Media upload job {JobId} processed with status: {Status}", 
                    processedJob.Id, processedJob.Status);

                // Complete the message
                await messageActions.CompleteMessageAsync(message);
                _logger.LogInformation("Successfully completed message {MessageId}", message.MessageId);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize message body for message {MessageId}", message.MessageId);
                await messageActions.DeadLetterMessageAsync(message, "JSON deserialization failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error processing message {MessageId}", message.MessageId);
                
                // Check if we should retry or dead letter
                if (message.DeliveryCount >= 3)
                {
                    _logger.LogWarning("Message {MessageId} has been retried {DeliveryCount} times, moving to dead letter queue", 
                        message.MessageId, message.DeliveryCount);
                    await messageActions.DeadLetterMessageAsync(message, "Max retry attempts exceeded");
                }
                else
                {
                    // Let the message be retried
                    _logger.LogInformation("Message {MessageId} will be retried (attempt {DeliveryCount})", 
                        message.MessageId, message.DeliveryCount);
                    throw; // This will cause the message to be retried
                }
            }
        }
    }
} 