using Microsoft.Azure.NotificationHubs;
using FacebookFnApp.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FacebookFnApp.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ILogger<NotificationService> _logger;
        private readonly NotificationHubClient _notificationHubClient;
        private readonly IConfiguration _configuration;

        public NotificationService(
            ILogger<NotificationService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            string hubConnectionString = _configuration.GetValue<string>("NotificationHub:ConnectionString");
            string hubName = _configuration.GetValue<string>("NotificationHub:HubName");
            _notificationHubClient = new NotificationHubClient(hubConnectionString, hubName);
        }

        public async Task<bool> SendNotificationAsync(MediaUploadJobDto job)
        {
            try
            {
                _logger.LogInformation("Sending notification for job {JobId} to user {UserId}", job.JobId, job.UserId);

                var title = "Media Processing Complete";
                var message = $"Your media files have been processed successfully. Job ID: {job.JobId}";
                
                var customData = new Dictionary<string, string>
                {
                    ["jobId"] = job.JobId.ToString(),
                    ["attachmentId"] = job.AttachmentId,
                    ["attachmentType"] = job.AttachmentType.ToString(),
                    ["processingStatus"] = job.ProcessingStatus,
                    ["fileCount"] = job.MediaFiles.Count.ToString()
                };

                return await SendNotificationAsync(job.UserId.ToString(), title, message, customData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification for job {JobId}", job.JobId);
                return false;
            }
        }

        public async Task<bool> SendNotificationAsync(string userId, string title, string message, Dictionary<string, string> customData = null)
        {
            try
            {
                // Create notification payload for FCM (Firebase Cloud Messaging)
                var notificationPayload = new Dictionary<string, object>
                {
                    ["notification"] = new Dictionary<string, string>
                    {
                        ["title"] = title,
                        ["body"] = message
                    },
                    ["data"] = customData ?? new Dictionary<string, string>()
                };

                // Convert to JSON string
                var jsonPayload = System.Text.Json.JsonSerializer.Serialize(notificationPayload);

                // Send to specific user tag
                var tags = new List<string> { $"user:{userId}" };
                
                var result = await _notificationHubClient.SendFcmV1NativeNotificationAsync(jsonPayload, tags);

                _logger.LogInformation("Successfully sent notification to user {UserId}. Result: {Result}", userId, result);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
                return false;
            }
        }
    }
}
