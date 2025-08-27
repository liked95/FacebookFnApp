using FacebookFnApp.Models;

namespace FacebookFnApp.Services
{
    public interface IMediaProcessingService
    {
        Task<MediaUploadJobDto> ProcessMediaUploadAsync(MediaUploadJobDto job);
        Task<List<string>> DownloadFromTempStorageAsync(MediaUploadJobDto job);
        Task<List<string>> ProcessMediaFilesAsync(List<string> localPaths, MediaUploadJobDto job);
        Task<List<Uri>> UploadToFinalStorageAsync(List<string> processedFiles, MediaUploadJobDto job);
        Task UpdateDatabaseAsync(MediaUploadJobDto job, List<Uri> uris);
    }
}