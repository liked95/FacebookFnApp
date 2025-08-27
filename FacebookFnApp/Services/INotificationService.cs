using FacebookFnApp.Models;

namespace FacebookFnApp.Services
{
    public interface INotificationService
    {
        Task<bool> SendNotificationAsync(MediaUploadJobDto job);
        Task<bool> SendNotificationAsync(string userId, string title, string message, Dictionary<string, string> customData = null);
    }
}
