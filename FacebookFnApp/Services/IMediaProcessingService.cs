using FacebookFnApp.Models;

namespace FacebookFnApp.Services
{
    public interface IMediaProcessingService
    {
        Task<MediaUploadJobDto> ProcessMediaUploadAsync(MediaUploadJobDto job);
        Task<bool> DownloadFromTempStorageAsync(MediaUploadJobDto job);
        Task<bool> ProcessMediaAsync(MediaUploadJobDto job);
        Task<bool> UploadToFinalStorageAsync(MediaUploadJobDto job);
        Task<bool> UpdateDatabaseAsync(MediaUploadJobDto job);
        Task<bool> SendNotificationAsync(MediaUploadJobDto job);
    }
} 