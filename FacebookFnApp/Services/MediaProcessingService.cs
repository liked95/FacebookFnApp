using Azure.Storage.Blobs;
using FacebookFnApp.Data;
using FacebookFnApp.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.ComponentModel;
using Xabe.FFmpeg;

namespace FacebookFnApp.Services
{
    public class MediaProcessingService : IMediaProcessingService
    {
        private readonly ILogger<MediaProcessingService> _logger;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly SqlConnectionFactory _connectionFactory;
        private readonly string _containerName;
        private readonly string _tempContainerName;

        public MediaProcessingService(
            ILogger<MediaProcessingService> logger,
            BlobServiceClient blobServiceClient,
            SqlConnectionFactory connectionFactory
            )
        {
            _logger = logger;
            _blobServiceClient = blobServiceClient;
            _connectionFactory = connectionFactory;
            _containerName = "fb-media-files";
            _tempContainerName = $"{_containerName}-temp";
        }

        public async Task<MediaUploadJobDto> ProcessMediaUploadAsync(MediaUploadJobDto job)
        {
            string tempFolder = null;
            try
            {
                var localPaths = await DownloadFromTempStorageAsync(job);
                tempFolder = Path.GetDirectoryName(localPaths.FirstOrDefault());
                var processedFiles = await ProcessMediaFilesAsync(localPaths, job);
                var finalUris = await UploadToFinalStorageAsync(processedFiles, job);
                await UpdateDatabaseAsync(job, finalUris);
                await SendNotificationAsync(job);

                job.ProcessingStatus = "completed";
            }
            catch (Exception ex)
            {
                job.ProcessingStatus = "failed";
                _logger.LogError(ex, $"Error processing media for job {job.JobId}");
            }
            finally
            {
                // Clean up local temp files
                if (!string.IsNullOrEmpty(tempFolder) && Directory.Exists(tempFolder))
                {
                    try
                    {
                        Directory.Delete(tempFolder, recursive: true);
                        _logger.LogInformation($"Cleaned up local temp folder: {tempFolder}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to clean up local temp folder: {tempFolder}");
                    }
                }

                // Clean up Azure temp blobs
                await CleanupTempBlobsAsync(job);
            }

            return job;
        }


        public async Task<List<string>> DownloadFromTempStorageAsync(MediaUploadJobDto job)
        {
            List<string> localFiles = new List<string>();
            var container = _blobServiceClient.GetBlobContainerClient(_tempContainerName);

            var tempFolder = Path.Combine(Path.GetTempPath(), "media-jobs", job.JobId.ToString());
            Directory.CreateDirectory(tempFolder);

            foreach (var file in job.MediaFiles)
            {
                var blobClient = container.GetBlobClient(file.TempFileName);
                var localPath = Path.Combine(tempFolder, file.OriginalFileName);

                await blobClient.DownloadToAsync(localPath);
                localFiles.Add(localPath);

                _logger.LogInformation($"Downloaded {file.TempFileName} to {localPath}");
            }

            return localFiles;
        }

        public async Task<List<string>> ProcessMediaFilesAsync(List<string> localPaths, MediaUploadJobDto job)
        {
            var processedFiles = new List<string>();

            foreach (var file in job.MediaFiles)
            {
                var inputPath = localPaths.First(p => p.EndsWith(file.OriginalFileName));
                var extension = Path.GetExtension(inputPath).ToLower();
                var outputPath = Path.ChangeExtension(inputPath, $".processed{extension}");

                if (file.MediaType == "image")
                {
                    using var image = await Image.LoadAsync(inputPath);

                    // Resize only if larger than max width
                    int maxWidth = 1080;
                    if (image.Width > maxWidth)
                    {
                        double scale = (double)maxWidth / image.Width;
                        int newHeight = (int)(image.Height * scale);

                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Mode = ResizeMode.Max,
                            Size = new Size(maxWidth, newHeight)
                        }));
                    }

                    // Save as JPEG with quality (better compression)
                    outputPath = Path.ChangeExtension(outputPath, ".jpg");
                    await image.SaveAsJpegAsync(outputPath, new JpegEncoder { Quality = 75 });

                    _logger.LogInformation($"Compressed image {file.OriginalFileName} → {outputPath}");
                }
                else if (file.MediaType == "video")
                {
                    try
                    {
                        // Compress video: H.264, CRF 28, scale max width 1280px (≈720p)
                        outputPath = Path.ChangeExtension(outputPath, ".mp4");

                        await FFmpeg.Conversions.New()
                            .AddParameter($"-i \"{inputPath}\" -vcodec libx264 -crf 28 -preset veryfast -vf scale=1280:-2 \"{outputPath}\"")
                            .Start();

                        _logger.LogInformation($"Compressed video {file.OriginalFileName} → {outputPath}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"FFmpeg processing failed for {file.OriginalFileName}. Using original file as fallback.");
                        // Use original file as fallback
                        outputPath = inputPath;
                    }
                }

                processedFiles.Add(outputPath); // return processed file paths
            }

            return processedFiles;
        }


        public async Task<List<Uri>> UploadToFinalStorageAsync(List<string> processedFiles, MediaUploadJobDto job)
        {
            var container = _blobServiceClient.GetBlobContainerClient(_containerName);
            var uris = new List<Uri>();

            for (int i = 0; i < job.MediaFiles.Count; i++)
            {
                var file = job.MediaFiles[i];
                var processedPath = processedFiles[i];

                // Store by user folder + unique file name
                string finalFileName = $"{job.UserId}/{Guid.NewGuid()}-{file.OriginalFileName}";
                var blobClient = container.GetBlobClient(finalFileName);

                await blobClient.UploadAsync(processedPath, overwrite: true);
                uris.Add(blobClient.Uri);


                _logger.LogInformation($"Uploaded {file.OriginalFileName} → {blobClient.Uri}");
            }

            return uris;
        }

        public async Task UpdateDatabaseAsync(MediaUploadJobDto job, List<Uri> finalUris)
        {
            using SqlConnection conn = _connectionFactory.CreateConnection();
            await conn.OpenAsync();

            for (int i = 0; i < job.MediaFiles.Count; i++)
            {
                var file = job.MediaFiles[i];
                var finalUrl = finalUris[i].ToString();

                string sql = @"
                    UPDATE MediaFiles
                    SET BlobUrl = @blobUrl,
                        IsProcessed = 1,
                        ProcessingStatus = @status,
                        UpdatedAt = SYSUTCDATETIME()
                    WHERE Id= @id";

                using SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@blobUrl", finalUrl);
                cmd.Parameters.AddWithValue("@status", "completed");
                cmd.Parameters.AddWithValue("@id", file.MediaFileId);
                int rows = await cmd.ExecuteNonQueryAsync();

                _logger.LogInformation(
                $"[MediaProcessingService] Updated MediaFile {file.MediaFileId} → {finalUrl} (Rows affected: {rows})"
            );
            }
        }

        public async Task<bool> SendNotificationAsync(MediaUploadJobDto job)
        {
            _logger.LogInformation("Sending notification for job {JobId} to user {UserId}", job.JobId, job.UserId);

            // TODO: Implement actual notification logic
            // Example: Firebase, ANH, email, etc.
            await Task.Delay(300); // Simulate notification sending time

            _logger.LogInformation("Successfully sent notification for job {JobId}", job.JobId);
            return true;
        }

        private async Task CleanupTempBlobsAsync(MediaUploadJobDto job)
        {
            try
            {
                var container = _blobServiceClient.GetBlobContainerClient(_tempContainerName);
                
                foreach (var file in job.MediaFiles)
                {
                    var blobClient = container.GetBlobClient(file.TempFileName);
                    await blobClient.DeleteIfExistsAsync();
                    _logger.LogInformation($"Deleted temp blob: {file.TempFileName}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to clean up temp blobs for job {job.JobId}");
            }
        }
    }
}