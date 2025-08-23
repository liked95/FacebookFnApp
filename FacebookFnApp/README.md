# Facebook Function App - Media Processing

This Azure Function App processes media upload jobs from Service Bus messages. It handles the complete media processing pipeline including download, processing, upload, database updates, and notifications.

## Project Structure

```
FacebookFnApp/
├── Functions/
│   └── ProcessMediaUploadFunction.cs    # Main Service Bus trigger function
├── Models/
│   └── MediaUploadJobDto.cs             # Message data model
├── Services/
│   ├── IMediaProcessingService.cs       # Service interface
│   └── MediaProcessingService.cs        # Service implementation
├── Program.cs                           # Dependency injection setup
├── host.json                            # Function App configuration
├── local.settings.json                  # Local development settings
└── FacebookFnApp.csproj                 # Project file
```

## Features

- **Service Bus Trigger**: Automatically processes messages from `media-upload-jobs` queue
- **Dependency Injection**: Clean architecture with service interfaces
- **Error Handling**: Retry logic and dead letter queue support
- **Comprehensive Logging**: Detailed logging for debugging and monitoring
- **Modular Design**: Easy to extend and maintain

## Setup Instructions

### 1. Prerequisites

- .NET 8.0 SDK
- Azure Functions Core Tools
- Azure Storage Emulator (for local development)
- Service Bus namespace and queue

### 2. Configuration

Update `local.settings.json` with your Service Bus connection string:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "ServiceBusConnection": "YOUR_SERVICE_BUS_CONNECTION_STRING_HERE"
  }
}
```

### 3. Service Bus Queue

Ensure you have a queue named `media-upload-jobs` in your Service Bus namespace.

### 4. Local Development

```bash
# Install dependencies
dotnet restore

# Run locally
func start
```

### 5. Testing

Send a test message to your Service Bus queue:

```json
{
  "id": "test-job-001",
  "userId": "user123",
  "fileName": "test-image.jpg",
  "fileType": "image/jpeg",
  "fileSize": 1024000,
  "tempStoragePath": "temp/uploads/test-image.jpg",
  "finalStoragePath": "media/processed/test-image.jpg",
  "mediaType": "image",
  "processingOptions": {
    "resize": true,
    "maxWidth": 1920,
    "maxHeight": 1080
  },
  "createdAt": "2024-01-01T00:00:00Z",
  "status": "pending"
}
```

## Message Processing Flow

1. **Message Reception**: Function receives message from Service Bus
2. **Deserialization**: Converts JSON to `MediaUploadJobDto`
3. **Download**: Downloads file from temporary storage
4. **Processing**: Processes media (resize, compress, etc.)
5. **Upload**: Uploads processed file to final storage
6. **Database Update**: Updates job status in database
7. **Notification**: Sends notification to user
8. **Completion**: Marks message as completed

## Error Handling

- **Retry Logic**: Messages are retried up to 3 times
- **Dead Letter Queue**: Failed messages are moved to DLQ after max retries
- **Graceful Degradation**: Notification failures don't fail the entire job

## Deployment

### Azure Functions

1. Create an Azure Function App
2. Configure application settings:
   - `ServiceBusConnection`: Your Service Bus connection string
   - `AzureWebJobsStorage`: Storage account connection string
3. Deploy using Azure CLI or Visual Studio

### Application Settings

```bash
# Set application settings
az functionapp config appsettings set \
  --name YOUR_FUNCTION_APP_NAME \
  --resource-group YOUR_RESOURCE_GROUP \
  --settings ServiceBusConnection="YOUR_SERVICE_BUS_CONNECTION_STRING"
```

## Monitoring

- **Application Insights**: Enable for production monitoring
- **Logs**: Check Function App logs in Azure Portal
- **Service Bus Metrics**: Monitor queue depth and message processing

## Next Steps

1. **Implement Storage Logic**: Replace placeholder methods with actual Azure Blob Storage operations
2. **Add Media Processing**: Integrate with image/video processing libraries
3. **Database Integration**: Connect to your database for job status updates
4. **Notification Service**: Implement Firebase/ANH integration
5. **Add Tests**: Create unit tests for the processing logic

## Troubleshooting

### Common Issues

1. **Connection String**: Ensure Service Bus connection string is correct
2. **Queue Name**: Verify queue name matches `media-upload-jobs`
3. **Permissions**: Check Service Bus access policies
4. **Local Storage**: Ensure Azure Storage Emulator is running for local development

### Logs

Check the function logs for detailed error information:

```bash
# View logs
func azure functionapp logstream YOUR_FUNCTION_APP_NAME
``` 