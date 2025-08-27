# Azure Notification Hub Integration

This document describes the Azure Notification Hub integration for sending push notifications to users when media processing is complete.

## Overview

The `AzureNotificationService` implements the `INotificationService` interface and provides functionality to send push notifications using Azure Notification Hub. It uses `IConfiguration` to read settings directly from the configuration, similar to the pattern used in other services.

## Configuration

### 1. Azure Notification Hub Setup

1. Create an Azure Notification Hub in your Azure portal
2. Note down the connection string and hub name
3. Update the `local.settings.json` file with your values:

```json
{
  "Values": {
    "NotificationHub:ConnectionString": "YOUR_NOTIFICATION_HUB_CONNECTION_STRING",
    "NotificationHub:HubName": "YOUR_NOTIFICATION_HUB_NAME"
  }
}
```

### 2. Required NuGet Packages

Make sure you have the following NuGet package installed:

```xml
<PackageReference Include="Azure.NotificationHubs" Version="1.1.0" />
```

## Usage

### Basic Usage

The notification service is automatically injected into the `MediaProcessingService` and will send notifications when media processing is complete.

### Manual Usage

You can also use the notification service directly:

```csharp
// Inject INotificationService into your class
private readonly INotificationService _notificationService;

// Send notification for a media job
await _notificationService.SendNotificationAsync(job);

// Send custom notification
await _notificationService.SendNotificationAsync(
    userId: "user-guid",
    title: "Custom Title",
    message: "Custom message",
    customData: new Dictionary<string, string>
    {
        ["key"] = "value"
    }
);
```

## Notification Payload

The service sends notifications with the following structure:

```json
{
  "notification": {
    "title": "Media Processing Complete",
    "body": "Your media files have been processed successfully. Job ID: {jobId}"
  },
  "data": {
    "jobId": "job-guid",
    "attachmentId": "attachment-id",
    "attachmentType": "Post",
    "processingStatus": "completed",
    "fileCount": "1"
  }
}
```

## User Targeting

Notifications are sent to users using tags in the format `user:{userId}`. Make sure your client applications register for notifications with the appropriate user tags.

## Error Handling

The service includes comprehensive error handling and logging. Failed notifications are logged but don't throw exceptions to prevent blocking the main processing workflow.

## Testing

You can test the notification service by:

1. Setting up a test notification hub
2. Using the `TestMessageSender` to create test jobs
3. Monitoring the notification delivery in the Azure portal

## Dependencies

- `Azure.NotificationHubs` - Azure Notification Hub client library
- `Microsoft.Extensions.Options` - Configuration options
- `Microsoft.Extensions.Logging` - Logging framework
