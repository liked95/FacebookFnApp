using System.Text.Json.Serialization;

namespace FacebookFnApp.Models
{
    public class MediaUploadJobDto
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;

        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonPropertyName("fileType")]
        public string FileType { get; set; } = string.Empty;

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("tempStoragePath")]
        public string TempStoragePath { get; set; } = string.Empty;

        [JsonPropertyName("finalStoragePath")]
        public string FinalStoragePath { get; set; } = string.Empty;

        [JsonPropertyName("mediaType")]
        public string MediaType { get; set; } = string.Empty; // "image" or "video"

        [JsonPropertyName("processingOptions")]
        public Dictionary<string, object>? ProcessingOptions { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = "pending";
    }
} 