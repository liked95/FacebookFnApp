using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FacebookFnApp.Models;

namespace FacebookFnApp
{
    public static class TestMessageSender
    {
        public static async Task SendTestMessageAsync(string connectionString, string queueName = "media-upload-jobs")
        {
            var client = new ServiceBusClient(connectionString);
            var sender = client.CreateSender(queueName);

            var testJob = new MediaUploadJobDto
            {
                Id = Guid.NewGuid().ToString(),
                UserId = "test-user-123",
                FileName = "test-image.jpg",
                FileType = "image/jpeg",
                FileSize = 1024000,
                TempStoragePath = "temp/uploads/test-image.jpg",
                FinalStoragePath = "media/processed/test-image.jpg",
                MediaType = "image",
                ProcessingOptions = new Dictionary<string, object>
                {
                    ["resize"] = true,
                    ["maxWidth"] = 1920,
                    ["maxHeight"] = 1080,
                    ["quality"] = 85
                },
                CreatedAt = DateTime.UtcNow,
                Status = "pending"
            };

            var messageBody = JsonSerializer.Serialize(testJob);
            var message = new ServiceBusMessage(messageBody)
            {
                MessageId = testJob.Id,
                CorrelationId = testJob.UserId,
                ContentType = "application/json"
            };

            // Add custom properties
            message.ApplicationProperties["jobType"] = "media-upload";
            message.ApplicationProperties["priority"] = "normal";

            try
            {
                await sender.SendMessageAsync(message);
                Console.WriteLine($"Test message sent successfully!");
                Console.WriteLine($"Message ID: {testJob.Id}");
                Console.WriteLine($"User ID: {testJob.UserId}");
                Console.WriteLine($"File: {testJob.FileName}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending test message: {ex.Message}");
            }
            finally
            {
                await sender.DisposeAsync();
                await client.DisposeAsync();
            }
        }
    }
} 