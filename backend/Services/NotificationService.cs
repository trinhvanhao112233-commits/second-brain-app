using PersonalFinance.API.Data;
using System.Net;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PersonalFinance.API.Models;
using WebPush;

namespace PersonalFinance.API.Services
{
    public interface INotificationService
    {
        Task<bool> SendPushNotificationAsync(PushSubscriptionModel sub, string title, string message, string url = "/?tab=calendar");
    }

    public class WebPushNotificationService : INotificationService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;
        private readonly ILogger<WebPushNotificationService> _logger;

        public WebPushNotificationService(IConfiguration config, ILogger<WebPushNotificationService> logger, AppDbContext db)
        {
            _config = config;
            _db = db;
            _logger = logger;
        }

        public async Task<bool> SendPushNotificationAsync(PushSubscriptionModel sub, string title, string message, string url = "/?tab=calendar")
        {
            var publicKey = _config["Vapid:PublicKey"] 
                            ?? Environment.GetEnvironmentVariable("VAPID_PUBLIC_KEY")
                            ;
            var privateKey = _config["Vapid:PrivateKey"] 
                             ?? Environment.GetEnvironmentVariable("VAPID_PRIVATE_KEY")
                             ;
            var subject = _config["Vapid:Subject"] 
                          ?? Environment.GetEnvironmentVariable("VAPID_SUBJECT")
                          ?? "mailto:support@secondbrain.local";

            if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey)) {
                _logger.LogWarning("Chưa cấu hình VAPID cho thông báo đẩy.");
                return false;
            }
            try
            {
                var pushSubscription = new PushSubscription(sub.Endpoint, sub.P256dh, sub.Auth);
                var vapidDetails = new VapidDetails(subject, publicKey, privateKey);

                var payload = JsonSerializer.Serialize(new
                {
                    title,
                    body = message,
                    icon = "/icon.svg",
                    data = new { url }
                });

                var webPushClient = new WebPushClient();
                await webPushClient.SendNotificationAsync(pushSubscription, payload, vapidDetails);
                _logger.LogInformation("Đã gửi push đến tài khoản {UserId}", sub.UserId);
                return true;
            }
            catch (WebPushException ex)
            {
                if (ex.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound) {
                    _db.PushSubscriptions.Remove(sub);
                    await _db.SaveChangesAsync();
                }
                _logger.LogWarning("Gửi push thất bại: {StatusCode}", ex.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebPush] Ngoại lệ khi gửi push notification.");
                return false;
            }
        }
    }
}
