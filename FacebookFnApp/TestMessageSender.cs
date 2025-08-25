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
                JobId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                AttachmentId = "test-attachment-123",
                AttachmentType = MediaAttachmentType.Post,
                MediaFiles = new List<MediaFileInfoDto>
                {
                    new MediaFileInfoDto
                    {
                        MediaFileId = Guid.NewGuid(),
                        TempFileName = "temp-test-image.jpg",
                        OriginalFileName = "test-image.jpg",
                        FileSize = 1024000,
                        MimeType = "image/jpeg",
                        MediaType = "image",
                        DisplayOrder = 1,
                        TempBlobUrl = "https://temp.blob.core.windows.net/uploads/test-image.jpg"
                    }
                },
                CreatedAt = DateTime.UtcNow,
                RetryCount = 0,
                ProcessingStatus = "pending"
            };

            var messageBody = JsonSerializer.Serialize(testJob);
            var message = new ServiceBusMessage(messageBody)
            {
                MessageId = testJob.JobId.ToString(),
                CorrelationId = testJob.UserId.ToString(),
                ContentType = "application/json"
            };

            // Add custom properties
            message.ApplicationProperties["jobType"] = "media-upload";
            message.ApplicationProperties["priority"] = "normal";

            try
            {
                await sender.SendMessageAsync(message);
                Console.WriteLine($"Test message sent successfully!");
                Console.WriteLine($"Job ID: {testJob.JobId}");
                Console.WriteLine($"User ID: {testJob.UserId}");
                Console.WriteLine($"Attachment ID: {testJob.AttachmentId}");
                Console.WriteLine($"File: {testJob.MediaFiles.FirstOrDefault()?.OriginalFileName}");
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