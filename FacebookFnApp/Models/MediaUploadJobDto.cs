using System;
using System.Collections.Generic;

namespace FacebookFnApp.Models
{
    public class MediaUploadJobDto
    {
        public Guid JobId { get; set; }
        public Guid UserId { get; set; }
        public string AttachmentId { get; set; } = string.Empty;
        public MediaAttachmentType AttachmentType { get; set; }
        public List<MediaFileInfoDto> MediaFiles { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public int RetryCount { get; set; } = 0;
        public string ProcessingStatus { get; set; } = "pending";
    }

    public class MediaFileInfoDto
    {
        public Guid MediaFileId { get; set; }
        public string TempFileName { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string MimeType { get; set; } = string.Empty;
        public string MediaType { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
        public string TempBlobUrl { get; set; } = string.Empty;
    }

    public enum MediaAttachmentType
    {
        Post,
        ProfilePicture,
        Message
    }
}
